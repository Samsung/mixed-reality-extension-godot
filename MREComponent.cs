using Godot;
using System;
using MixedRealityExtension.Core;
using MixedRealityExtension.App;

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

public class MREComponent : Node
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
	private Permissions GrantedPermissions;

	public Transform SceneRoot;

	public Spatial PlaceholderObject;

	public Spatial UserSpatial;

	public IMixedRealityExtensionApp MREApp { get; private set; }

	public event AppEventHandler OnConnecting;

	public event AppEventHandler OnConnected;

	public event AppEventHandler OnDisconnected;

	public event AppEventHandler OnAppStarted;

	public event AppEventHandler OnAppShutdown;

	private Guid _appId;

	private static bool _apiInitialized = false;

	[SerializeField]
	private TMP_FontAsset DefaultFont;

	[SerializeField]
	private TMP_FontAsset SerifFont;

	[SerializeField]
	private TMP_FontAsset SansSerifFont;

	[SerializeField]
	private TMP_FontAsset MonospaceFont;

	[SerializeField]
	private TMP_FontAsset CursiveFont;

	[SerializeField]
	private UnityEngine.Material DefaultPrimMaterial;

	[SerializeField]
	private DialogFactory DialogFactory;

	private Dictionary<Guid, HostAppUser> hostAppUsers = new Dictionary<Guid, HostAppUser>();

	// Declare member variables here. Examples:
	// private int a = 2;
	// private string b = "text";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (!_apiInitialized)
		{
			var assetCacheGo = new GameObject("MRE Asset Cache");
			var assetCache = assetCacheGo.AddComponent<AssetCache>();
			assetCache.CacheRootGO = new GameObject("Assets");
			assetCache.CacheRootGO.transform.SetParent(assetCacheGo.transform, false);
			assetCache.CacheRootGO.SetActive(false);

			MREAPI.InitializeAPI(
				defaultMaterial: DefaultPrimMaterial,
				layerApplicator: new SimpleLayerApplicator(0, 9, 10, 5),
				assetCache: assetCache,
				textFactory: new TmpTextFactory()
				{
					DefaultFont = DefaultFont,
					SerifFont = SerifFont,
					SansSerifFont = SansSerifFont,
					MonospaceFont = MonospaceFont,
					CursiveFont = CursiveFont
				},
				permissionManager: new SimplePermissionManager(GrantedPermissions),
				behaviorFactory: new BehaviorFactory(),
				dialogFactory: DialogFactory,
				libraryFactory: new ResourceFactory(),
				gltfImporterFactory: new VertexShadedGltfImporterFactory(),
				materialPatcher: new VertexMaterialPatcher(),
				logger: new MRELogger()
			);
			_apiInitialized = true;
		}

		MREApp = MREAPI.AppsAPI.CreateMixedRealityExtensionApp(this, EphemeralAppID, AppID);

		if (SceneRoot == null)
		{
			SceneRoot = transform;
		}

		MREApp.SceneRoot = SceneRoot.gameObject;

		if (AutoStart)
		{
			EnableApp();
		}

		MREApp.RPC.OnReceive("log", new RPCHandler<TestLogMessage>(
			(logMessage) => Debug.Log($"Log RPC of type {logMessage.GetType()} called with args [ {logMessage.Message}, {logMessage.TestBoolean} ]")
		));

		// Functional test commands
		MREApp.RPC.OnReceive("functional-test:test-started", new RPCHandler<string>((testName) =>
		{
			Debug.Log($"Test started: {testName}.");
		}));

		MREApp.RPC.OnReceive("functional-test:test-complete", new RPCHandler<string, bool>((testName, success) =>
		{
			Debug.Log($"Test complete: {testName}. Success: {success}.");
		}));

		MREApp.RPC.OnReceive("functional-test:close-connection", new RPCHandler(() =>
		{
			MREApp.Shutdown();
		}));

		MREApp.RPC.OnReceive("functional-test:trace-message", new RPCHandler<string, string>((testName, message) =>
		{
			Debug.Log($"{testName}: {message}");
		}));
	}

	public void EnableApp()
	{
		if (PlaceholderObject != null)
		{
			PlaceholderObject.gameObject.SetActive(false);
		}

		Debug.Log("Connecting to MRE App.");

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
			Debug.Log($"Failed to connect to MRE App.  Exception thrown: {e.Message}\nStack trace: {e.StackTrace}");
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
			PlaceholderObject.gameObject.SetActive(true);
		}
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
