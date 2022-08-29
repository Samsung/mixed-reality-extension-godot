using System;
using Godot;
using Assets.Scripts.Control;

[Flags]
public enum PositionControlType
{
    None = 0,
    Keyboard = 1 << 0,
    VirtualGamePad = 1 << 1,
}

[Flags]
public enum RotationControlType
{
    None = 0,
    Mouse = 1 << 0,
}

[Flags]
public enum ControllerType
{
    None = 0,
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

    [ExportEnum(typeof(PositionControlType))]
    private int positionControl = (int)PositionControlType.Keyboard;

    [ExportEnum(typeof(RotationControlType))]
    private int rotationControl = (int)RotationControlType.Mouse;

    [ExportEnum(typeof(ControllerType))]
    private int controller = (int)ControllerType.Hand;

    public PositionControlType PositionControl {
        get => (PositionControlType)positionControl;
        set
        {
            if (!IsInsideTree())
                return;

            positionControl = (int)value;

            // clear exist IPositionControl
            foreach (Node childNode in GetChildren())
            {
                if (childNode is IPositionControl)
                {
                    RemoveChild(childNode);
                }
            }

            if (value.HasFlag(PositionControlType.Keyboard))
            {
                var keyboardControl = new KeyboardPositionControl(MainCamera);
                AddChild(keyboardControl);
            }

            if (value.HasFlag(PositionControlType.VirtualGamePad))
            {
                var virtualGamePadControl = new VirtualGamepadPositionControl(MainCamera);
                AddChild(virtualGamePadControl);
            }
        }
    }

    public RotationControlType RotationControl {
        get => (RotationControlType)rotationControl;
        set
        {
            if (!IsInsideTree())
                return;

            rotationControl = (int)value;

            // clear exist IRotationControl
            foreach (Node childNode in GetChildren())
            {
                if (childNode is IRotationControl)
                {
                    RemoveChild(childNode);
                }
            }

            if (value.HasFlag(RotationControlType.Mouse))
            {
                var mouseControl = new MouseRotationControl(MainCamera);
                AddChild(mouseControl);
            }
        }
    }

    public ControllerType Controller {
        get => (ControllerType)controller;
        set
        {
            if (!IsInsideTree())
                return;

            controller = (int)value;

            // clear exist Controller
            foreach (Node childNode in GetChildren())
            {
                if (childNode is IController)
                {
                    RemoveChild(childNode);
                }
            }

            if (value.HasFlag(ControllerType.Mouse))
            {
                var mouseControl = new MouseController(MainCamera);
                AddChild(mouseControl);
            }
            if (value.HasFlag(ControllerType.Hand))
            {
                if (IsInsideTree())
                {
                    AddHandController();
                }
                else
                {
                    Connect("ready", readyCallable);
                }
            }
        }
    }

    [Export(PropertyHint.File, "*.tscn")]
    public string CursorScenePath {
        get => cursorScenePath;
        set {
            if (cursorScenePath == value)
                return;
            cursorScenePath = value;
            EmitSignal(nameof(cursor_changed), value);
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
                EmitSignal(nameof(ray_changed), value, MainCamera);
        }
    }

    [Export(PropertyHint.File, "*.tscn")]
    public string GamePadScenePath {
        get => gamepadScenePath;
        set {
            if (gamepadScenePath == value)
                return;
            gamepadScenePath = value;
            EmitSignal(nameof(gamepad_changed), value);
        }
    }

    public Camera3D MainCamera { get; private set; }

    [Signal]
    public delegate void cursor_changed(string scenePath);

    [Signal]
    public delegate void ray_changed(string scenePath);

    [Signal]
    public delegate void gamepad_changed(string scenePath);

    private void _on_Player_ready()
    {
        Disconnect("ready", readyCallable);
        AddHandController();
    }

    private void AddHandController()
    {
        if (openXRIsInitialized)
        {
            var rightHand = new HandController(MRERuntimeScenePath.OpenXRRightHand);
            var leftHand = new HandController(MRERuntimeScenePath.OpenXRLeftHand);
            AddChild(rightHand);
            AddChild(leftHand);
        }
        else
        {
            var rightHand = new HandController(MRERuntimeScenePath.DefaultHand);
            AddChild(rightHand);
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

            var rightHand = new HandController(MRERuntimeScenePath.OpenXRRightHand);
            var leftHand = new HandController(MRERuntimeScenePath.OpenXRLeftHand);

            CallDeferred("add_child", rightHand);
            CallDeferred("add_child", leftHand);
            return true;
        }

        return false;
    }

    public override void _EnterTree()
    {
        MainCamera = GetNode<Camera3D>("MainCamera");

        InitializeOpenXR();
    }

    public override void _Ready()
    {
        readyCallable = new Callable(this, nameof(_on_Player_ready));
        // update control property
        PositionControl = (PositionControlType)positionControl;
        RotationControl = (RotationControlType)rotationControl;
        Controller = (ControllerType)controller;
    }
}
