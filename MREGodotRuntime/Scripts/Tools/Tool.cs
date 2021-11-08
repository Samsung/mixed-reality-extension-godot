// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.User;
using Godot;

namespace Assets.Scripts.Tools
{
	public abstract class Tool
	{
		public bool IsHeld { get; private set; }

		public Vector3 Position { get; protected set; }

		public void Update(InputSource inputSource)
		{
			if (IsHeld)
			{
				UpdateTool(inputSource);
			}
		}

		public abstract void CleanUp();

		public virtual void OnToolHeld(InputSource inputSource)
		{
			IsHeld = true;
		}

		public virtual void OnToolDropped(InputSource inputSource)
		{
			IsHeld = false;
		}

		protected abstract void UpdateTool(InputSource inputSource);
	}
}
