using Godot;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using MixedRealityExtension.Patching.Types;

internal class PinchSliderThumb : Spatial, IToolkit, IMixedRealityPointerHandler
{
    private PinchSlider slider;
    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        slider.OnPointerUp(eventData);
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        slider.OnPointerDown(eventData);
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        slider.OnPointerDragged(eventData);
    }

    public override void _Ready()
    {
        this.RegisterHandler<IMixedRealityPointerHandler>();
    }

    public override void _EnterTree()
    {
        var actor = GetParent();
        slider = actor.GetParent() as PinchSlider;
    }

    public void ApplyPatch(ToolkitPatch toolkitPatch) {}
}
