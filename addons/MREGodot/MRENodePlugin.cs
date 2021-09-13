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
        var buttonScript = GD.Load<Script>("Toolkit/PressableButton.cs");
        AddCustomType("PressableButton", "Spatial", buttonScript, null);

        var interactionScript = GD.Load<Script>("Toolkit/NearInteractionTouchable.cs");
        AddCustomType("NearInteractionTouchable", "Spatial", interactionScript, null);
    }

    public override void _ExitTree()
    {
        RemoveCustomType("MRENode");
        RemoveCustomType("PressableButton");
        RemoveCustomType("NearInteractionTouchable");
    }
}
#endif