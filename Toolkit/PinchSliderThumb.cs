using Godot;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using MixedRealityExtension.Patching.Types;

internal class PinchSliderThumb : Spatial, IToolkit, IMixedRealityPointerHandler
{
    private PinchSlider slider;

    public Node Parent { get; private set; }

    public void OnPointerUp(Spatial inputSource, Node userNode, Vector3 point)
    {
        slider.OnPointerUp(inputSource, userNode, point);
    }

    public void OnPointerDown(Spatial inputSource, Node userNode, Vector3 point)
    {
        slider.OnPointerDown(inputSource, userNode, point);
    }

    public void OnPointerDragged(Spatial inputSource, Node userNode, Vector3 point)
    {
        slider.OnPointerDragged(inputSource, userNode, point);
    }

    public void OnPointerClicked(Spatial inputSource, Node userNode, Vector3 point) { }

    public override void _Ready()
    {
        Parent = GetParent();
        ((IMixedRealityPointerHandler)this).RegisterPointerEvent(this, Parent);
    }

    public override void _EnterTree()
    {
        slider = Parent.GetParent() as PinchSlider;
    }

    public void ApplyPatch(ToolkitPatch toolkitPatch) {}
}
