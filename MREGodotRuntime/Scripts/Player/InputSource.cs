// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Assets.Scripts.Tools;
using System;
using Godot;
using Godot.Collections;

namespace Assets.Scripts.User
{
	public class InputSource : Camera
	{
		private Tool _currentTool;

		internal MeshInstance RayCastMesh;
		internal Spatial PokePointer;
		public Node UserNode;

		public CSGTorus CollisionPoint;

		public Tool CurrentTool => _currentTool;

		public Spatial Hand { get; private set; }

		public static readonly Guid UserId = new Guid();

		// Only target layers 0 (Default), 5 (UI), and 10 (Hologram).
		// You still want to hit all layers, but only interact with these.
		private const uint LayerMask = (1 << 0) | (1 << 5) | (1 << 10);
		private PhysicsDirectSpaceState spaceState;
		private Spatial player;

		public override void _Ready()
		{
			Hand = GetNode<Spatial>("../Right_hand");
			PokePointer = Hand.FindNode("IndexTip") as Spatial;
			player = GetParent<Spatial>();
			spaceState = GetWorld().DirectSpaceState;

			RayCastMesh = new MeshInstance()
			{
				Mesh = new PlaneMesh()
				{
					Size = new Vector2(0.01f, 1),
				}
			};
			Hand.CallDeferred("add_child", RayCastMesh);

			CollisionPoint = new CSGTorus()
			{
				InnerRadius = 0.02f,
				OuterRadius = 0.01f,
				Sides = 16,
			};
			CollisionPoint.Visible = false;
			CollisionPoint.MaterialOverride = new SpatialMaterial()
			{
				FlagsFixedSize = true
			};
			GetTree().Root.CallDeferred("add_child", CollisionPoint);

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
			var forward = -Hand.GlobalTransform.basis.z.Normalized();
			var from = Hand.GlobalTransform.origin - forward * 0.05f;
			var to = Hand.GlobalTransform.origin + forward * 1.5f;
			return spaceState.IntersectRay(from, to, null, LayerMask, true, true);
		}

		public override void _EnterTree()
		{
			UserNode = Owner;
			if (UserNode == null)
			{
				throw new Exception("Input source must have a MWUnityUser assigned to it.");
			}
		}

		public override void _Process(float delta)
		{
			_currentTool.Update(this);
			var localPosition = RayCastMesh.ToLocal(player.GlobalTransform.origin) * Scale;
			RayCastMesh.Rotate(Transform.basis.z.Normalized(), Mathf.Atan2(localPosition.y, localPosition.x) - Mathf.Pi / 2);
		}
	}
}
