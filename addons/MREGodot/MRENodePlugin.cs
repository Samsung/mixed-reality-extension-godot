#if TOOLS
using Godot;

[Tool]
public partial class MRENodePlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        var script = GD.Load<Script>("MREGodotRuntime/Scripts/LaunchMRE.cs");
        var texture = GD.Load<Texture>("addons/MREGodot/icon.svg");
        AddCustomType("MRENode", "Node3D", script, texture);

        var textScript = GD.Load<Script>("MREGodotRuntime/Scripts/SimpleText.cs");
        AddCustomType("SimpleText", "MeshInstance3D", textScript, null);

        var buttonScript = GD.Load<Script>("Toolkit/PressableButton.cs");
        AddCustomType("PressableButton", "Node3D", buttonScript, null);

        var buttonGodotScript = GD.Load<Script>("Toolkit/PressableButtonGodot.cs");
        AddCustomType("PressableButtonGodot", "PressableButton", buttonScript, null);
    }

    public override void _ExitTree()
    {
        RemoveCustomType("MRENode");
        RemoveCustomType("SimpleText");
        RemoveCustomType("PressableButton");
        RemoveCustomType("PressableButtonGodot");
    }
}
#endif