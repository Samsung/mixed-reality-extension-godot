#if TOOLS
using Godot;

[Tool]
public partial class MRENodePlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        var script = GD.Load<Script>("MREGodotRuntime/Scripts/LaunchMRE.cs");
        var texture = GD.Load<Texture2D>("addons/MREGodot/icon.svg");
        AddCustomType("MRENode", "Node3D", script, texture);
    }

    public override void _ExitTree()
    {
        RemoveCustomType("MRENode");
    }
}
#endif
