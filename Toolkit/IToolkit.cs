using Godot;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Patching.Types;

namespace Microsoft.MixedReality.Toolkit.UI
{
	public interface IToolkit
	{
		// The user interacting with this toolkit.
		IUser CurrentUser { get; }
		Node Parent { get; }

		void ApplyPatch(ToolkitPatch toolkitPatch);

		void OnInteractionStarted(Node userNode);

		void OnInteractionEnded();
	}
}
