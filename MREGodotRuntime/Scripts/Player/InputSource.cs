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
		internal MeshInstance RayCastMesh;
		public Node UserNode;

		public CSGTorus CollisionPoint;

		public Tool CurrentTool => _currentTool;

		public static readonly Guid UserId = new Guid();

		public override void _Ready()
		{
			// Only target layers 0 (Default), 5 (UI), and 10 (Hologram).
			// You still want to hit all layers, but only interact with these.
			uint layerMask = (1 << 0) | (1 << 5) | (1 << 10);

			rayCast = (RayCast)GetParent().FindNode("HandRay");
			rayCast.CastTo = new Vector3(0, 0, -1.5f);
			rayCast.CollisionMask = layerMask;
			RayCastMesh = (MeshInstance)rayCast.GetChild(0);

			CollisionPoint = new CSGTorus()
			{
				InnerRadius = 0.06f,
				OuterRadius = 0.03f,
				Sides = 16,
			};
			CollisionPoint.Visible = false;
			CollisionPoint.MaterialOverride = new SpatialMaterial();
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
		}
	}
}
