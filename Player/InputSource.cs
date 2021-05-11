// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using System;
using Godot;

namespace Assets.Scripts.User
{
	public class InputSource : Camera
	{
		private Tool _currentTool;

		internal RayCast rayCast;
		public Node UserNode;

		public Tool CurrentTool => _currentTool;

		public static readonly Guid UserId = new Guid();

		public override void _Ready()
		{
			rayCast = GetNode<RayCast>("RayCast");
			rayCast.CastTo = new Vector3(0, 0, -100);
			_currentTool = ToolCache.GetOrCreateTool<TargetTool>();
			_currentTool.OnToolHeld(this);
		}

		public void HoldTool(Type toolType)
		{
			if (UserNode != null)
			{
				_currentTool.OnToolDropped(this);
				ToolCache.StowTool(_currentTool);
				_currentTool = ToolCache.GetOrCreateTool(toolType);
				_currentTool.OnToolHeld(this);
			}
		}

		public void DropTool()
		{
			// We only drop a tool is it isn't the default target tool.
			if (UserNode != null && _currentTool.GetType() != typeof(TargetTool))
			{
				_currentTool.OnToolDropped(this);
				ToolCache.StowTool(_currentTool);
				_currentTool = ToolCache.GetOrCreateTool<TargetTool>();
				_currentTool.OnToolHeld(this);
			}
		}

		public override void _EnterTree()
		{
			UserNode = Owner;
			if (UserNode == null)
			{
				throw new Exception("Input source must have a MWUnityUser assigned to it.");
			}
		}

		public override void _PhysicsProcess(float delta)
		{
			_currentTool.Update(this);
		}
	}
}
