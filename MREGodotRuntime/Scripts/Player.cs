using System;
using Godot;
using Assets.Scripts.Control;
using MixedRealityExtension.Util.GodotHelper;

[Flags]
public enum PositionControlType
{
    Keyboard = 1 << 0,
    VirtualGamePad = 1 << 1,
}

[Flags]
public enum RotationControlType
{
    Mouse = 1 << 0,
}

[Flags]
public enum ControllerType
{
    Hand = 1 << 0,
    Mouse = 1 << 1,
}

public partial class Player : XROrigin3D
{
    private string cursorScenePath = MRERuntimeScenePath.DefaultCursor;
    private string rayScenePath = MRERuntimeScenePath.DefaultRay;
    private string gamepadScenePath = MRERuntimeScenePath.Joypad;
    private bool openXRIsInitialized = false;
    private Callable readyCallable;

    [Export(hint: PropertyHint.Enum, "None,Keyboard,VirtualGamePad")]
    private PositionControlType positionControl = PositionControlType.Keyboard;

    [Export(hint: PropertyHint.Enum, "None,Mouse")]
    private RotationControlType rotationControl = RotationControlType.Mouse;

    [Export(hint: PropertyHint.Enum, "None,Hand,Mouse")]
    private ControllerType controller = ControllerType.Hand;

    [Export(PropertyHint.File, "*.tscn")]
    public string CursorScenePath {
        get => cursorScenePath;
        set {
            if (cursorScenePath == value)
                return;
            cursorScenePath = value;
            EmitSignal(nameof(CursorChangedEventHandler), value);
        }
    }

    [Export(PropertyHint.File, "*.tscn")]
    public string RayScenePath {
        get => rayScenePath;
        set {
            if (rayScenePath == value)
                return;
            rayScenePath = value;
            if (MainCamera != null)
                EmitSignal(nameof(RayChangedEventHandler), value, MainCamera);
        }
    }

    [Export(PropertyHint.File, "*.tscn")]
    public string GamePadScenePath {
        get => gamepadScenePath;
        set {
            if (gamepadScenePath == value)
                return;
            gamepadScenePath = value;
            EmitSignal(nameof(GamepadChangedEventHandler), value);
        }
    }

    public Camera3D MainCamera { get; private set; }

    [Signal]
    public delegate void CursorChangedEventHandler(string scenePath);

    [Signal]
    public delegate void RayChangedEventHandler(string scenePath, Camera3D camera);

    [Signal]
    public delegate void GamepadChangedEventHandler(string scenePath);

    private void AddHandController()
    {
        AddChild(new HandController(HandController.Hands.Right));
        if (openXRIsInitialized)
        {
            AddChild(new HandController(HandController.Hands.Left));
        }
    }

    private void AddDefaultHandNodes()
    {
        var rightHand = ResourceLoader.Load<PackedScene>(MRERuntimeScenePath.DefaultRightHand).Instantiate<DefaultHand>();
        var rightThumbTip = GetOrCreateSocket("right-thumb");
        var rightIndexTip = GetOrCreateSocket("right-index");
        var rightMiddleTip = GetOrCreateSocket("right-middle");
        var rightWrist = GetOrCreateSocket("right-hand");

        if (!openXRIsInitialized)
        {
            rightHand.Position = new Vector3(0.081f, -0.006f, -0.151f);
            MainCamera.AddChild(rightHand);
        }
        else
        {
            var leftHand = this.AddNode(ResourceLoader.Load<PackedScene>(MRERuntimeScenePath.DefaultLeftHand).Instantiate<DefaultHand>());
            var leftThumbTip = GetOrCreateSocket("left-thumb");
            var leftIndexTip = GetOrCreateSocket("left-index");
            leftHand.SetThumbTip(leftThumbTip);
            leftHand.SetIndexTip(leftIndexTip);

            AddChild(rightHand);

            // Add XRHands
            var rightXRHand = new OpenXRHand() { Name = "XRHand_R" };
            rightXRHand.Hand = OpenXRHand.Hands.Right;
            AddChild(rightXRHand);
            rightXRHand.HandSkeleton = rightXRHand.GetPathTo(rightHand.FindChild("Skeleton3D"));

            var leftXRHand = new OpenXRHand() { Name = "XRHand_L" };
            leftXRHand.Hand = OpenXRHand.Hands.Left;
            AddChild(leftXRHand);
            leftXRHand.HandSkeleton = leftXRHand.GetPathTo(leftHand.FindChild("Skeleton3D"));
        }

        rightHand.SetThumbTip(rightThumbTip);
        rightHand.SetIndexTip(rightIndexTip);
        rightHand.SetMiddleTip(rightMiddleTip);
        rightHand.SetWrist(rightWrist);
    }

    private void InitializeController()
    {
        switch (controller)
        {
            case ControllerType.Mouse:
                AddChild(new MouseController(MainCamera));
                break;
            case ControllerType.Hand:
                AddDefaultHandNodes();
                AddHandController();
                break;
        }
    }

    private void InitializePositionControl()
    {
        switch (positionControl)
        {
            case PositionControlType.Keyboard:
                AddChild(new KeyboardPositionControl(MainCamera));
                break;
            case PositionControlType.VirtualGamePad:
                AddChild(new VirtualGamepadPositionControl(MainCamera));
                break;
        }
    }

    private void InitializeRotationControl()
    {
        switch (rotationControl)
        {
            case RotationControlType.Mouse:
                AddChild(new MouseRotationControl(MainCamera));
                break;
        }
    }

    private bool InitializeOpenXR()
    {
        var ARVRInterface = XRServer.FindInterface("OpenXR");
        if (ARVRInterface?.Initialize() == true)
        {
            GD.Print("OpenXR Interface initialized");

            //default height 1.6m
            MainCamera.Position = Vector3.Up * 1.6f;

            GetViewport().UseXr = true;
            //vp.Keep3dLinear = (bool)GetNode("Configuration").Call("keep_3d_linear");

            Engine.TargetFps = 144;
            openXRIsInitialized = ARVRInterface.IsInitialized();
            return true;
        }

        return false;
    }

    public Node3D GetOrCreateSocket(string name)
    {
        var socket = GetNodeOrNull<Node3D>($"socket-{name}") ?? this.AddNode(new Node3D(){ Name = $"socket-{name}" });
        socket.Owner = this;
        return socket;
    }

    public override void _EnterTree()
    {
        MainCamera = GetNode<Camera3D>("MainCamera");

        InitializeOpenXR();
    }

    public override void _Ready()
    {
        // initialize control property
        InitializeController();
        InitializePositionControl();
        InitializeRotationControl();
    }
}
