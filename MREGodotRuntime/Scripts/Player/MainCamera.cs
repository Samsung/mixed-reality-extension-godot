using Godot;

public partial class MainCamera : XRCamera3D
{
    public override void _Ready()
    {
        var ARVRInterface = XRServer.FindInterface("OpenXR");
        if (ARVRInterface?.InterfaceIsInitialized == true)
        {
            Environment = ResourceLoader.Load<Godot.Environment>(MRERuntimeScenePath.ARVREnvironment);
            GetTree().Root.TransparentBg = true;
        }
    }
}
