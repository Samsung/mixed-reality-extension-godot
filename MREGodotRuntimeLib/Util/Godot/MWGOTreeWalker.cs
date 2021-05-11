// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Godot;

namespace MixedRealityExtension.Util.GodotHelper
{
	internal static class MWGOTreeWalker
	{
		public delegate void VisitorFn(Node gameObject);

		public static void VisitTree(Node treeRoot, VisitorFn fn)
		{
			var children = treeRoot.GetChildren();
			fn(treeRoot);

			// Walk children to add to the actors flat list.
			foreach (Node child in children)
			{
				VisitTree(child, fn);
			}
		}
	}
}
