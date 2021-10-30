using Godot;
using MixedRealityExtension.Util.GodotHelper;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public interface IMixedRealityEventHandler
    {
        public static T FindEventHandler<T>(Node node) where T : class
        {
            var handler = node.GetChild<T>();
            while (handler == null)
            {
                node = node.GetParent<Node>();
                if (node == null) return null;
                handler = node.GetChild<T>();
            }

            return handler;
        }
    }
}
