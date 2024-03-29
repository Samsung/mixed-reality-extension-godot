// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using System;
using Godot;
using Godot.Collections;

namespace Assets.Scripts.User
{
	public partial class InputSource : Camera3D
	{
		private Tool _currentTool;
		private bool isPinching;
		private bool pinchChaged;
		private PhysicsDirectSpaceState3D spaceState;
		private Cursor cursor;

		public Node UserNode { get; set; }

		public Tool CurrentTool => _currentTool;

		public Vector3 HitPoint { get; set; }

		public Vector3 HitPointNormal { get; set; }

		public Vector3? RayCastBegin { get; set; }

		public Vector3? RayCastDirection { get; set; }

		public float RayCastDistance { get; set; }

		public Cursor Cursor
		{
			get => cursor;
			set
			{
				if (cursor == value)
					return;

				if (cursor != null)
					cursor.QueueFree();

				cursor = value;
				if (cursor != null && IsInsideTree())
					AddChild(cursor);
			}
		}

		public bool IsPinching
		{
			get => isPinching;
			set
			{
				PinchChaged = isPinching != value;
				isPinching = value;
			}
		}

		public bool PinchChaged
		{
			get
			{
				var ret = pinchChaged;
				if (pinchChaged) pinchChaged = false;
				return ret;
			}
			private set
			{
				pinchChaged = value;
			}
		}

		public static readonly Guid UserId = new Guid();

		// Only target layers 0 (Default), 5 (UI), and 10 (Hologram).
		// You still want to hit all layers, but only interact with these.
		private const uint LayerMask = (1 << 0) | (1 << 5) | (1 << 10);

		public InputSource(Node userNode)
		{
			UserNode = userNode;
		}

		public override void _Ready()
		{
			spaceState = GetWorld3d().DirectSpaceState;
			AddChild(Cursor);

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

		public Dictionary IntersectRay()
		{
			if (RayCastDirection == null || RayCastBegin == null)
				return new Godot.Collections.Dictionary();

			var forward = (Vector3)RayCastDirection?.Normalized();
			var from = (Vector3)RayCastBegin;
			var to = (Vector3)(RayCastBegin + forward * RayCastDistance);
			return spaceState.IntersectRay(new PhysicsRayQueryParameters3D() {
				From = from,
				To = to,
				CollisionMask = LayerMask,
				CollideWithBodies = true,
				CollideWithAreas = true});
		}

		public override void _EnterTree()
		{
			if (UserNode == null)
			{
				throw new Exception("Input source must have a UserNode assigned to it.");
			}
		}

		public override void _Process(double delta)
		{
			_currentTool.Update(this);
			Cursor.SetCursorTransform(HitPoint, HitPointNormal);
		}
	}
}
