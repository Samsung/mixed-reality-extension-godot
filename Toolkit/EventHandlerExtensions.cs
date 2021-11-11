using System.Linq;
using System.Reflection;
using Godot;
using Microsoft.MixedReality.Toolkit.UI;
using MixedRealityExtension.Util.GodotHelper;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public static class EventHandlerExtensions
    {
        public static void HandleEvent<T>(this Node node, string signal, params Godot.Object[] args) where T : class
        {
            while (node != null)
            {
                var handler = node.GetChild<T>() as Node;

                handler?.EmitSignal(signal, args);
                node = node.GetParent() as Node;
            }
            foreach (var arg in args)
            {
                arg.Dispose();
            }
        }

        public static void RegisterHandler<T>(this Node node)
        {
            foreach (var signal in typeof(T).GetMembers().Where(m => m.GetCustomAttribute<SignalAttribute>() != null))
            {
                node.AddUserSignal(signal.Name);
                node.Connect(signal.Name, node, signal.Name);
            }
        }
    }
}
