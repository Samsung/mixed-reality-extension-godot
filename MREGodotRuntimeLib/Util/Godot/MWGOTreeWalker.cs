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
			fn(treeRoot);

			var childCount = treeRoot.GetChildCount(); 
			// Walk children to add to the actors flat list.
			for (int i = 0; i < childCount; ++i)
			{
				var childGO = treeRoot.GetChild(i);
				VisitTree(childGO, fn);
			}
		}
	}
}
