// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Reflection;
using System.Collections.Generic;

using MixedRealityExtension.App;
using MixedRealityExtension.Factories;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using Godot;

using AppManager = MixedRealityExtension.Util.ObjectManager<MixedRealityExtension.App.IMixedRealityExtensionApp>;
using MixedRealityExtension.Util.Logging;
using Newtonsoft.Json;
using MixedRealityExtension.Messaging.Commands;
using System.IO;

namespace MixedRealityExtension.API
{
	/// <summary>
	/// Static class that serves as the Mixed Reality Extension SDK API.
	/// </summary>
	public static class MREAPI
	{
		/// <summary>
		/// Initializes the Mixed Reality Extension SDK API.
		/// </summary>
		/// <param name="defaultMaterial">The material template used for all SDK-spawned meshes.</param>
		/// <param name="layerApplicator">The class used to apply MRE layers to Unity colliders.</param>
		/// <param name="assetCache">The class responsible for long-term asset caching.</param>
		/// <param name="textFactory">The text factory to use within the runtime.</param>
		/// <param name="permissionManager">The instance responsible for presenting users with permission requests.</param>
		/// <param name="behaviorFactory">The behavior factory to use within the runtime.</param>
		/// <param name="dialogFactory"></param>
		/// <param name="libraryFactory">The library resource factory to use within the runtime.</param>
		/// <param name="videoPlayerFactory"></param>
		/// <param name="primitiveFactory">The primitive factory to use within the runtime.</param>
		/// <param name="gltfImporterFactory">The glTF loader factory. Uses default GLTFSceneImporter if omitted.</param>
		/// <param name="materialPatcher">Overrides default material property map (color and mainTexture only).</param>
		/// <param name="userInfoProvider">Provides appId/sessionId scoped IUserInfo instances.</param>
		/// <param name="logger">The logger to be used by the MRE SDK.</param>
		public static void InitializeAPI(
			// required properties
			ShaderMaterial defaultMaterial,
			ILayerApplicator layerApplicator,
			IAssetCache assetCache,
			ITextFactory textFactory,
			IPermissionManager permissionManager,
			// missing features if omitted
			IBehaviorFactory behaviorFactory = null,
			IDialogFactory dialogFactory = null,
			ILibraryResourceFactory libraryFactory = null,
			//IVideoPlayerFactory videoPlayerFactory = null,
			// reasonable defaults provided
			IPrimitiveFactory primitiveFactory = null,
			IMaterialPatcher materialPatcher = null,
			IMRELogger logger = null)
		{
			// required properties
			AppsAPI.DefaultMaterial = defaultMaterial;
			AppsAPI.LayerApplicator = layerApplicator;
			AppsAPI.AssetCache = assetCache;
			AppsAPI.TextFactory = textFactory;
			AppsAPI.PermissionManager = permissionManager;

			// missing features if omitted
			AppsAPI.BehaviorFactory = behaviorFactory;
			AppsAPI.DialogFactory = dialogFactory;
			AppsAPI.LibraryResourceFactory = libraryFactory;
			//AppsAPI.VideoPlayerFactory = videoPlayerFactory;

			// reasonable defaults provided
			AppsAPI.PrimitiveFactory = primitiveFactory ?? new MWPrimitiveFactory();
			AppsAPI.MaterialPatcher = materialPatcher ?? new DefaultMaterialPatcher();

#if ANDROID_DEBUG
			Logger = logger ?? new UnityLogger(null);
#else
			Logger = logger ?? new ConsoleLogger(null);
#endif
		}

		/// <summary>
		/// Gets the apps API for the Mixed Reality Extension SDK.
		/// </summary>
		public static MREAppsAPI AppsAPI { get; } = new MREAppsAPI();

		/// <summary>
		/// Gets the logger to use within the MRE SDK.
		/// </summary>
		public static IMRELogger Logger { get; private set; }

		// TODO @tombu - Re-visit this with the upcoming user design and implementation.
		//public static MWIUsersAPI UsersAPI { get; } = new MWIUsersAPI();
	}

	/// <summary>
	/// Class that contains the mixed reality extension application API.
	/// </summary>
	public class MREAppsAPI
	{
		private AppManager _apps = new AppManager();

		/// <summary>
		/// The material template used for all SDK-spawned materials.
		/// </summary>
		public ShaderMaterial DefaultMaterial { get; internal set; }

		/// <summary>
		/// The class responsible for assigning layers to colliders.
		/// </summary>
		public ILayerApplicator LayerApplicator { get; internal set; }

		/// <summary>
		/// The class responsible for long-term asset caching.
		/// </summary>
		public IAssetCache AssetCache { get; internal set; }

		internal IBehaviorFactory BehaviorFactory { get; set; }

		internal ITextFactory TextFactory { get; set; }

		internal IPrimitiveFactory PrimitiveFactory { get; set; }

		internal ILibraryResourceFactory LibraryResourceFactory { get; set; }

		internal IMaterialPatcher MaterialPatcher { get; set; }

		internal IDialogFactory DialogFactory { get; set; }

		internal IPermissionManager PermissionManager { get; set; }

		private string GetMREPluginPath(string pluginName)
		{
			if (OS.HasFeature("editor"))
				return System.IO.Path.Join(ProjectSettings.GlobalizePath("res://addons/"), pluginName);
			else
				return OS.GetExecutablePath().GetBaseDir();
		}

		public void RegisterPayloadType(Type type)
		{
			MixedRealityExtension.Messaging.Payloads.PayloadTypeRegistry.RegisterPayloadType(type);
		}

		public void RegisterJsonConverter(JsonConverter jsonConverter)
		{
			MixedRealityExtension.Constants.SerializerSettings.Converters.Add(jsonConverter);
		}

		public void LoadMREPlugin(IMixedRealityExtensionApp app, string pluginName)
		{
			if (app == null)
				throw new ArgumentNullException(nameof(app));
			if (string.IsNullOrWhiteSpace(pluginName))
				throw new ArgumentNullException(nameof(pluginName));

			var pluginPath = GetMREPluginPath(pluginName);
			var dllPath = System.IO.Path.Join(pluginPath, $"{pluginName}.dll");
			if (!System.IO.File.Exists(dllPath))
				throw new FileNotFoundException($"plugin not found", nameof(dllPath));

			var loadContext = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(GetType().Assembly);
			var dll = loadContext.LoadFromAssemblyPath(dllPath);

			foreach (var type in dll.GetTypes())
			{
				if (typeof(MREPlugin).IsAssignableFrom(type))
				{
					MREPlugin MREPlugin;
					try
					{
						MREPlugin = (MREPlugin)Activator.CreateInstance(type, new object[] { app });
					}
					catch (Exception e)
					{
						GD.PushError(e.ToString());
						continue;
					}
					app.RegisterCommandHandlers(new Dictionary<Type, ICommandHandlerContext>()
					{
						{ type, MREPlugin },
					});
					if (MREPlugin.JsonConverters != null)
					{
						foreach (var jsonConverter in MREPlugin.JsonConverters)
						{
							RegisterJsonConverter(jsonConverter);
						}
					}
					if (MREPlugin.TypeRegistry != null)
					{
						RegisterPayloadType(MREPlugin.TypeRegistry);
					}
				}
			}
		}

		/// <summary>
		/// Creates a new mixed reality extension app and adds it to the MRE runtime.
		/// </summary>
		/// <param name="ownerScript">The owner unity script for the app.</param>
		/// <param name="ephemeralAppId">A unique identifier for the MRE behind this instance's URL, in the absence
		/// of a global app ID. Used for generating user IDs that are consistent within this session across clients,
		/// but not reliable across time. Must be synchronized across all clients in this session, and must be
		/// periodically rotated.</param>
		/// <param name="globalAppId">A unique identifier for the MRE behind this instance's URL. Used for generating
		/// consistent user IDs for this MRE. Would typically come from an app registry or similar. If supplied, must
		/// be synchronized across all clients in this session.
		/// </param>
		/// <returns>Returns the newly created mixed reality extension app.</returns>
		public IMixedRealityExtensionApp CreateMixedRealityExtensionApp(
			Node ownerScript,
			string ephemeralAppId,
			string globalAppId)
		{
			var mreApp = new MixedRealityExtensionApp(globalAppId ?? string.Empty, ephemeralAppId, ownerScript)
			{
				InstanceId = Guid.NewGuid()
			};

			_apps.Add(mreApp.InstanceId, mreApp);
			return mreApp;
		}
	}
}
