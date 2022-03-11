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

public class Player : ARVROrigin
{
    private float speed = 1f;
    private float cameraSpeed = 0.0015f;
    private bool cameraMove = false;
    private Vector2 mouseDelta = new Vector2();
    internal Vector2 screenTouchPosition;
    private uint pressTime;
    private Vector2 pressPosition;

    [Export]
    private NodePath viewport = null;
    private string gamepadScenePath = "res://MREGodotRuntime/Scenes/Joystick2D.tscn";
    private bool ARVRInterfaceIsInitialized = false;

    [ExportEnum(typeof(PositionControlType))]
    private int positionControl = (int)PositionControlType.Keyboard;

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

    public Spatial Hand { get; private set;  }
    public Spatial ThumbTip { get; private set; }
    public Spatial IndexTip { get; private set; }
    public Camera MainCamera { get; private set; }

    [Signal]
    public delegate void gamepad_changed(string scenePath);

    private bool InitializeOpenXR()
    {
        var ARVRInterface = ARVRServer.FindInterface("OpenXR");
        if (ARVRInterface?.Initialize() == true)
        {
            GD.Print("OpenXR Interface initialized");

            //default height 1.6m
            MainCamera.Translation = Vector3.Up * 1.6f;

            Viewport vp = null;
            if (viewport != null)
                vp = GetNode<Viewport>(viewport);
            if (vp == null)
                vp = GetViewport();

            vp.Arvr = true;
            //vp.Keep3dLinear = (bool)GetNode("Configuration").Call("keep_3d_linear");

            Engine.IterationsPerSecond = 144;
            ARVRInterfaceIsInitialized = ARVRInterface.InterfaceIsInitialized;

            return true;
        }

        return false;
    }

    public override void _EnterTree()
    {
        MainCamera = GetNode<Camera>("MainCamera");
        var openXRRightHand = GetNode<Spatial>("OpenXRRightHand");
        var openXRLeftHand = GetNode<Spatial>("OpenXRLeftHand");
        var rightHand = MainCamera.GetNode<Spatial>("RightHand");

        InitializeOpenXR();

        if (ARVRInterfaceIsInitialized)
        {
            ThumbTip = openXRRightHand.FindNode("ThumbTip") as Spatial;
            IndexTip = openXRRightHand.FindNode("IndexTip") as Spatial;
            openXRRightHand.SetProcess(true);
            openXRLeftHand.SetProcess(true);
            openXRRightHand.Visible = true;
            openXRLeftHand.Visible = true;

            RemoveChild(rightHand);

            Hand = openXRRightHand;
        }
        else
        {
            ThumbTip = rightHand.FindNode("ThumbTip") as Spatial;
            IndexTip = rightHand.FindNode("IndexTip") as Spatial;
            rightHand.SetProcess(true);
            rightHand.Visible = true;

            RemoveChild(openXRRightHand);
            RemoveChild(openXRLeftHand);

            Hand = rightHand;
        }
    }

    public override void _Ready()
    {
        if (ARVRInterfaceIsInitialized)
        {
            MainCamera.Environment = ResourceLoader.Load<Godot.Environment>("res://MREGodotRuntime/arvr_env.tres");
            GetTree().Root.TransparentBg = true;
        }

        // update control property
        PositionControl = (PositionControlType)positionControl;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!cameraMove) return;
        if (Input.IsActionPressed("shift"))
        {
            Hand.Translate(new Vector3(mouseDelta.x * cameraSpeed, -mouseDelta.y * cameraSpeed, 0));
        }
        else
        {
            var newRotation = MainCamera.Rotation;
            newRotation.x -= mouseDelta.y * cameraSpeed;
            newRotation.y -= mouseDelta.x * cameraSpeed;
            MainCamera.Rotation = newRotation;
        }
        mouseDelta = Vector2.Zero;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseMotion e)
        {
            mouseDelta = e.Relative;
        }
        else if (inputEvent is InputEventMouseButton eventMouseButton)
        {
            if (!cameraMove && eventMouseButton.Pressed)
                cameraMove = true;
        }
        else if (Input.IsActionPressed("ui_cancel"))
        {
            cameraMove = false;
        }
        else if (inputEvent is InputEventScreenTouch screenTouch)
        {
            if (screenTouch.Pressed)
            {
                pressTime = OS.GetTicksMsec();
                pressPosition = screenTouch.Position;
            }
            else
            {
                if (OS.GetTicksMsec() - pressTime < 110 &&
                    pressPosition.DistanceSquaredTo(screenTouch.Position) < 200)
                {
                    screenTouchPosition = screenTouch.Position;
                    Input.ActionPress("Fire1");
                    Input.ActionRelease("Fire1");
                }
                else
                {
                    screenTouchPosition = Vector2.Zero;
                }
            }
        }
    }
}
