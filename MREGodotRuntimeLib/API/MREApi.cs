// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

using MixedRealityExtension.App;
using MixedRealityExtension.Factories;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.PluginInterfaces.Behaviors;
using Godot;

using AppManager = MixedRealityExtension.Util.ObjectManager<MixedRealityExtension.App.IMixedRealityExtensionApp>;

//FIXME
namespace MixedRealityExtension.API
{
	/// <summary>
	/// Static class that serves as the Mixed Reality Extension SDK API.
	/// </summary>
	public static class MREAPI
	{

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
		/// The class responsible for assigning layers to colliders.
		/// </summary>
		public ILayerApplicator LayerApplicator { get; internal set; }
		
		/// <summary>
		/// The class responsible for long-term asset caching.
		/// </summary>
		public IAssetCache AssetCache { get; internal set; }

		internal IBehaviorFactory BehaviorFactory { get; set; }

		internal IPrimitiveFactory PrimitiveFactory { get; set; } = new MWPrimitiveFactory();
		
		internal IPermissionManager PermissionManager { get; set; }

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
