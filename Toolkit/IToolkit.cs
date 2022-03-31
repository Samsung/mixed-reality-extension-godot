using Godot;
using MixedRealityExtension.Patching.Types;

namespace Microsoft.MixedReality.Toolkit.UI
{
    public interface IToolkit
    {
        Node Parent { get; }
        void ApplyPatch(ToolkitPatch toolkitPatch);
    }
}