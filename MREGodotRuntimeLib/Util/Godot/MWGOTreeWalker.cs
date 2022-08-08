// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Godot;
using Godot.Collections;

namespace MixedRealityExtension.Util.GodotHelper
{
	internal static class MWGOTreeWalker
	{
		public delegate void VisitorFn(Node gameObject);
		public delegate void VisitorChildrenFn(Node gameObject, Array<Node> children);

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

		public static void VisitTreeChildren(Node treeRoot, VisitorChildrenFn fn)
		{
			var children = treeRoot.GetChildren();
			fn(treeRoot, children);

			// Walk children to add to the actors flat list.
			foreach (Node child in children)
			{
				VisitTreeChildren(child, fn);
			}
		}
	}
}
