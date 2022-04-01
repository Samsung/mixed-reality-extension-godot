using Godot;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Patching.Types;

internal class PinchSliderThumb : Spatial, IToolkit, IMixedRealityPointerHandler
{
    private PinchSlider slider;

    public IUser CurrentUser { get; }

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


    public virtual void OnInteractionStarted(Node userNode) { }
    public virtual void OnInteractionEnded() { }

    public override void _Ready()
    {
        ((IMixedRealityPointerHandler)this).RegisterPointerEvent(this, Parent);
    }

    public override void _EnterTree()
    {
        Parent = GetParent();
        slider = Parent.GetParent() as PinchSlider;
    }

    public void ApplyPatch(ToolkitPatch toolkitPatch) {}
}
