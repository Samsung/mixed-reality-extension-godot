#if TOOLS
using Godot;

[Tool]
public class MRENodePlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        var script = GD.Load<Script>("MREGodotRuntime/Scripts/LaunchMRE.cs");
        var texture = GD.Load<Texture>("addons/MREGodot/icon.svg");
        AddCustomType("MRENode", "Spatial", script, texture);
    }

    public override void _ExitTree()
    {
        RemoveCustomType("MRENode");
    }
}
#endif