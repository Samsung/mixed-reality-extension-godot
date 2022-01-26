using Godot;

public class Player : ARVROrigin
{
    private float speed = 1f;
    private float cameraSpeed = 0.0015f;
    private bool cameraMove = false;
    private Vector2 mouseDelta = new Vector2();

    [Export]
    private NodePath viewport = null;
    private bool ARVRInterfaceIsInitialized = false;
    public Spatial Hand { get; private set;  }
    public Spatial ThumbTip { get; private set; }
    public Spatial IndexTip { get; private set; }

    private bool InitializeOpenXR()
    {
        var ARVRInterface = ARVRServer.FindInterface("OpenXR");
        if (ARVRInterface?.Initialize() == true)
        {
            GD.Print("OpenXR Interface initialized");

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
        InitializeOpenXR();
        var openXRRightHand = GetNode<Spatial>("OpenXRRightHand");
        var openXRLeftHand = GetNode<Spatial>("OpenXRLeftHand");
        var rightHand = GetNode<Spatial>("RightHand");
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
            var worldEnvironment = GetNode<WorldEnvironment>("../WorldEnvironment");
            worldEnvironment.Environment.BackgroundMode = Godot.Environment.BGMode.Color;
            worldEnvironment.Environment.BackgroundColor = new Color(0, 0, 0, 0);
            GetTree().Root.TransparentBg = true;
       }
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!cameraMove) return;
        if (Input.IsKeyPressed((int)Godot.KeyList.Space))
            ARVRServer.CenterOnHmd(ARVRServer.RotationMode.ResetButKeepTilt, true);

        if (Input.IsActionPressed("move_right"))
        {
            Translation += Transform.basis.x * delta * speed;
        }
        else if (Input.IsActionPressed("move_left"))
        {
            Translation -= Transform.basis.x * delta * speed;
        }
        if (Input.IsActionPressed("move_back"))
        {
            Translation += Transform.basis.z * delta * speed;
        }
        else if (Input.IsActionPressed("move_forward"))
        {
            Translation -= Transform.basis.z * delta * speed;
        }

        if (Input.IsActionPressed("shift"))
        {
            Hand.Translate(new Vector3(mouseDelta.x * cameraSpeed, -mouseDelta.y * cameraSpeed, 0));
        }
        else
        {
            var newRotation = Rotation;
            newRotation.x -= mouseDelta.y * cameraSpeed;
            newRotation.y -= mouseDelta.x * cameraSpeed;
            Rotation = newRotation;
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
    }
}
