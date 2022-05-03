using Godot;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util.GodotHelper;

internal class PinchSliderThumb : Spatial, IToolkit, IMixedRealityPointerHandler
{
    private PinchSlider slider;
    private Tween tween => GetNode<Tween>("Tween");

    public IUser CurrentUser { get; }

    public Node Parent { get; private set; }

    private void TweenInterpolateProperty(Object obj, NodePath property, object finalValue, Tween.TransitionType transType = Tween.TransitionType.Back, Tween.EaseType easeType = Tween.EaseType.InOut)
    {
        var duration = 0.2f;
        tween.InterpolateProperty(obj, property, null, finalValue, duration, transType);
    }

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

    public void OnFocused()
    {
        TweenInterpolateProperty(GetNode("Mesh"), "scale", Vector3.One * 1.3f);
        TweenInterpolateProperty(GetNode("Mesh"), "material/0:shader_param/albedo", new Color(0.3f, 0.66f, 1f));
        tween.Start();
    }

    public void OnUnfocused()
    {
        TweenInterpolateProperty(GetNode("Mesh"), "scale", Vector3.One);
        TweenInterpolateProperty(GetNode("Mesh"), "material/0:shader_param/albedo", new Color(1, 1, 1));
        tween.Start();
    }

    public virtual void OnInteractionStarted(Node userNode) {
        TweenInterpolateProperty(GetNode("Mesh"), "scale", Vector3.One);
        TweenInterpolateProperty(GetNode("Mesh"), "material/0:shader_param/albedo", new Color(0.26f, 0.95f, 1));
        tween.Start();
    }
    public virtual void OnInteractionEnded() {
        TweenInterpolateProperty(GetNode("Mesh"), "scale", Vector3.One * 1.3f);
        TweenInterpolateProperty(GetNode("Mesh"), "material/0:shader_param/albedo", new Color(0.3f, 0.66f, 1f));
        tween.Start();
    }

    public override void _Ready()
    {
        ((IMixedRealityPointerHandler)this).RegisterPointerEvent(this, Parent);
        GetNode<MeshInstance>("Mesh").SetSurfaceMaterial(0, new ShaderMaterial() {
            Shader = ShaderFactory.OpaqueShader
        });
    }

    public override void _EnterTree()
    {
        Parent = GetParent();
        slider = Parent.GetParent() as PinchSlider;
    }

    public void ApplyPatch(ToolkitPatch toolkitPatch) {}
}
