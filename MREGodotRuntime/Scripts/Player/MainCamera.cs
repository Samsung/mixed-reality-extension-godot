using Godot;

public class MainCamera : ARVRCamera
{
    public override void _Ready()
    {
        var ARVRInterface = ARVRServer.FindInterface("OpenXR");
        if (ARVRInterface?.InterfaceIsInitialized == true)
        {
            Environment = ResourceLoader.Load<Godot.Environment>(MRERuntimeScenePath.ARVREnvironment);
            GetTree().Root.TransparentBg = true;
        }
    }
}
