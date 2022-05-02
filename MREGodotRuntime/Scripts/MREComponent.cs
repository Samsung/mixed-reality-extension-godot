using Godot;
using System;
using System.Collections.Generic;
using Assets.Scripts.Behaviors;
using Assets.TestBed_Assets.Scripts.Player;
using MixedRealityExtension.App;
using MixedRealityExtension.Assets;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.API;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Factories;
using MixedRealityExtension.RPC;
using MixedRealityExtension.Toolkit.Payloads;
using MixedRealityExtension.Toolkit.Payloads.Converters;
using MixedRealityExtension.Toolkit;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Util.GodotHelper;

class TestLogMessage
{
	public string Message { get; set; }

	public bool TestBoolean { get; set; }
}

public class MRELogger : IMRELogger
{
	public void LogDebug(string message)
	{
		GD.Print(message);
	}

	public void LogError(string message)
	{
		GD.PushError(message);
	}

	public void LogWarning(string message)
	{
		GD.PushWarning(message);
	}
}

public class MREComponent : Spatial
{
	public delegate void AppEventHandler(MREComponent app);

	public string MREURL;

	public string SessionID;

	public string AppID;

	public string EphemeralAppID;

	[Serializable]
	public class UserProperty
	{
		public string Name;
		public string Value;
	}

	public UserProperty[] UserProperties;

	public bool AutoStart = false;

	public bool AutoJoin = true;

	//[SerializeField]
	internal Permissions GrantedPermissions;

	public Spatial SceneRoot;

	public Node PlaceholderObject;

	public Node UserNode;

	public IMixedRealityExtensionApp MREApp { get; private set; }

	public DialogFactory DialogFactory;

	public event AppEventHandler OnConnecting;

	public event AppEventHandler OnConnected;

	public event AppEventHandler OnDisconnected;

	public event AppEventHandler OnAppStarted;

	public event AppEventHandler OnAppShutdown;

	private MixedRealityExtensionToolkit toolkit;
	private Guid _appId;

	private static bool _apiInitialized = false;

	private ShaderMaterial DefaultPrimMaterial = new ShaderMaterial() {
		Shader = ShaderFactory.OpaqueShader
	};

	private Dictionary<Guid, HostAppUser> hostAppUsers = new Dictionary<Guid, HostAppUser>();

	public override void _Ready()
	{
		if (!_apiInitialized)
		{
			var assetCacheGo = new Node { Name = "MRE Asset Cache" };
			var assetCache = new AssetCache();
			assetCacheGo.AddChild(assetCache);

			assetCache.CacheRootGO = new Node { Name = "Assets" };
			assetCacheGo.AddChild(assetCache.CacheRootGO);
			assetCache.CacheRootGO.SetProcess(false);

			MREAPI.InitializeAPI(
				defaultMaterial: DefaultPrimMaterial,
				layerApplicator: new SimpleLayerApplicator(0, 9, 10, 5),
				assetCache: assetCache,
				textFactory: new SimpleTextFactory(),
				permissionManager: new SimplePermissionManager(GrantedPermissions),
				behaviorFactory: new BehaviorFactory(),
				dialogFactory: DialogFactory,
				libraryFactory: new ResourceFactory(),
				//gltfImporterFactory: new VertexShadedGltfImporterFactory(),
				//materialPatcher: new VertexMaterialPatcher(),
				logger: new MRELogger()
			);
			//FIXME: I guess we need some kind of MRE Plugin structure.
			MREAPI.AppsAPI.RegisterPayloadType(typeof(ToolkitPayloadTypeRegistry));
			MREAPI.AppsAPI.RegisterJsonConverter(new ToolkitPatchConverter());
			_apiInitialized = true;
		}

		MREApp = MREAPI.AppsAPI.CreateMixedRealityExtensionApp(this, EphemeralAppID, AppID);
		//FIXME: I guess we need some kind of MRE Plugin structure.
		toolkit = new MixedRealityExtensionToolkit(MREApp);
		MREApp.RegisterCommandHandlers(new Dictionary<Type, ICommandHandlerContext>()
		{
			{ typeof(MixedRealityExtensionToolkit), toolkit },
		});

		if (SceneRoot == null)
		{
			SceneRoot = this;
		}

		MREApp.SceneRoot = SceneRoot;

		if (AutoStart)
		{
			EnableApp();
		}

		MREApp.RPC.OnReceive("log", new RPCHandler<TestLogMessage>(
			(logMessage) => GD.Print($"Log RPC of type {logMessage.GetType()} called with args [ {logMessage.Message}, {logMessage.TestBoolean} ]")
		));

		// Functional test commands
		MREApp.RPC.OnReceive("functional-test:test-started", new RPCHandler<string>((testName) =>
		{
			GD.Print($"Test started: {testName}.");
		}));

		MREApp.RPC.OnReceive("functional-test:test-complete", new RPCHandler<string, bool>((testName, success) =>
		{
			GD.Print($"Test complete: {testName}. Success: {success}.");
		}));

		MREApp.RPC.OnReceive("functional-test:close-connection", new RPCHandler(() =>
		{
			MREApp.Shutdown();
		}));

		MREApp.RPC.OnReceive("functional-test:trace-message", new RPCHandler<string, string>((testName, message) =>
		{
			GD.Print($"{testName}: {message}");
		}));
	}
	public override void _PhysicsProcess(float delta)
	{
		MREApp?.FixedUpdate();
	}

	public override void _Process(float delta)
	{
		MREApp?.Update();
	}

	private void MREApp_OnAppShutdown()
	{
		GD.Print("AppShutdown");
		OnAppShutdown?.Invoke(this);
	}

	private void MREApp_OnAppStarted()
	{
		GD.Print("AppStarted");
		OnAppStarted?.Invoke(this);

		if (AutoJoin)
		{
			UserJoin();
		}
	}

	private void MREApp_OnDisconnected()
	{
		GD.Print("Disconnected");
		OnDisconnected?.Invoke(this);
	}

	private void MREApp_OnConnected()
	{
		GD.Print("Connected");
		OnConnected?.Invoke(this);
	}

	private void MREApp_OnConnecting()
	{
		GD.Print("Connecting");
		OnConnecting?.Invoke(this);
	}

	private void MREApp_OnConnectFailed(MixedRealityExtension.IPC.ConnectFailedReason reason)
	{
		GD.Print($"ConnectFailed. reason: {reason}");
		if (reason == MixedRealityExtension.IPC.ConnectFailedReason.UnsupportedProtocol)
		{
			DisableApp();
		}
	}

	private void MRE_OnUserJoined(IUser user, bool isLocalUser)
	{
		GD.Print($"User joined with host id: {user.HostAppUser.HostUserId} and mre user id: {user.Id}");
		hostAppUsers[user.Id] = (HostAppUser)user.HostAppUser;

		if (hostAppUsers.Count == 1)
		{
			var drawingRPC = new RPCInterface(MREApp);
			MREApp.RPCChannels.SetChannelHandler(user.Id.ToString(), drawingRPC);
			drawingRPC.OnReceive("drawing:drawing-start", new RPCHandler<Guid, Guid>((userId, actorId) =>
			{
				var actor = ((MixedRealityExtensionApp)MREApp).FindActor(actorId) as Actor;
				if (actor == null) return;
				var origin = actor.GlobalTransform.origin;
				origin.z *= -1;
				drawingRPC.SendRPC(user.Id.ToString(), "drawing:drawing-start", user.Id.ToString(), userId, origin);
			}));
		}
	}

	private void MRE_OnUserLeft(IUser user, bool isLocalUser)
	{
		hostAppUsers.Remove(user.Id);
	}

	public void EnableApp()
	{
		if (PlaceholderObject != null)
		{
			PlaceholderObject.PauseMode = PauseModeEnum.Stop;
		}

		GD.Print("Connecting to MRE App.");

		var args = System.Environment.GetCommandLineArgs();
		Uri overrideUri = null;
		try
		{
			overrideUri = new Uri(args[args.Length - 1], UriKind.Absolute);
		}
		catch { }

		var uri = overrideUri != null && overrideUri.Scheme.StartsWith("ws") ? overrideUri.AbsoluteUri : MREURL;
		try
		{
			MREApp.OnConnecting += MREApp_OnConnecting;
			MREApp.OnConnectFailed += MREApp_OnConnectFailed;
			MREApp.OnConnected += MREApp_OnConnected;
			MREApp.OnDisconnected += MREApp_OnDisconnected;
			MREApp.OnAppStarted += MREApp_OnAppStarted;
			MREApp.OnAppShutdown += MREApp_OnAppShutdown;
			MREApp.OnUserJoined += MRE_OnUserJoined;
			MREApp.OnUserLeft += MRE_OnUserLeft;
			MREApp?.Startup(uri, SessionID);
		}
		catch (Exception e)
		{
			GD.Print($"Failed to connect to MRE App.  Exception thrown: {e.Message}\nStack trace: {e.StackTrace}");
		}
	}

	public void DisableApp()
	{
		MREApp?.Shutdown();
		MREApp.OnConnecting -= MREApp_OnConnecting;
		MREApp.OnConnectFailed -= MREApp_OnConnectFailed;
		MREApp.OnConnected -= MREApp_OnConnected;
		MREApp.OnDisconnected -= MREApp_OnDisconnected;
		MREApp.OnAppStarted -= MREApp_OnAppStarted;
		MREApp.OnAppShutdown -= MREApp_OnAppShutdown;
		MREApp.OnUserJoined -= MRE_OnUserJoined;
		MREApp.OnUserLeft -= MRE_OnUserLeft;

		if (PlaceholderObject != null)
		{
			PlaceholderObject.PauseMode = PauseModeEnum.Process;
		}
	}

	public void UserJoin()
	{
		var hostAppUser = new HostAppUser(LocalPlayer.PlayerId, $"TestBed User: {LocalPlayer.PlayerId}")
		{
			UserNode = UserNode
		};

		foreach (var kv in UserProperties)
		{
			hostAppUser.Properties[kv.Name] = kv.Value;
		}

		MREApp?.UserJoin(UserNode, hostAppUser, true);
	}

	public void UserLeave()
	{
		MREApp?.UserLeave(UserNode);
	}
}
