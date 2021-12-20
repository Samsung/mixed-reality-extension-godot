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
		private bool isPinching;
		private bool pinchChaged;
		private PhysicsDirectSpaceState spaceState;
		private Spatial player;
		private float handLocalOrigin;
		private CSGTorus cursor;
		private Vector3 cursorNormal;
		private ImmediateGeometry handRayLine =  new ImmediateGeometry();

		internal Spatial PokePointer;
		public Node UserNode { get; set; }
		public Tool CurrentTool => _currentTool;

		public Spatial Hand { get; private set; }

		public Vector3 HandRayOrigin => Hand.GlobalTransform.origin;

		public Vector3 HandRayHitPoint { get; set; }

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

		public Spatial ThumbTip { get; private set; }
		public Spatial IndexTip { get; private set; }

		public static readonly Guid UserId = new Guid();

		// Only target layers 0 (Default), 5 (UI), and 10 (Hologram).
		// You still want to hit all layers, but only interact with these.
		private const uint LayerMask = (1 << 0) | (1 << 5) | (1 << 10);

		public override void _Ready()
		{
			player = GetParent<Spatial>();
			spaceState = GetWorld().DirectSpaceState;

			Hand = GetNode<Spatial>("../MRTK_R_Hand");
			ThumbTip = Hand.GetNode<Spatial>("R_Hand_MRTK_Rig2/Skeleton/Thumb_Tip");
			IndexTip = Hand.GetNode<Spatial>("R_Hand_MRTK_Rig2/Skeleton/Pointer_Tip");

			var gradient = new Gradient();
			gradient.AddPoint(0.333f, new Color(1, 1, 1, 1));
			gradient.AddPoint(0.667f, new Color(1, 1, 1, 1));
			gradient.SetColor(0, new Color(1, 1, 1, 0));
			gradient.SetColor(1, new Color(1, 1, 1, 1));
			gradient.SetColor(2, new Color(1, 1, 1, 1));
			gradient.SetColor(3, new Color(1, 1, 1, 0));

			GetTree().Root.CallDeferred("add_child", handRayLine);

			handRayLine.MaterialOverride = new SpatialMaterial() {
				AlbedoTexture = new GradientTexture() {
					Gradient = gradient
				},
				FlagsUnshaded = true,
				FlagsTransparent = true
			};

			PokePointer = new Spatial();
			Hand.AddChild(PokePointer);
			var proximityLight = new OmniLight()
			{
				OmniRange = 0.0339852f,
				OmniAttenuation = 1.46409f,
				LightEnergy = 0.66f,
				LightIndirectEnergy = 0,
				ShadowEnabled = true,
				LightCullMask = 4,
			};
			Hand.AddChild(proximityLight);
			var proximityVisibleLight = new OmniLight()
			{
				OmniRange = 0.0923046f,
				LightEnergy = 1.46f,
				LightCullMask = 2,
			};
			Hand.AddChild(proximityVisibleLight);

			cursor = new CSGTorus()
			{
				InnerRadius = 0.02f,
				OuterRadius = 0.01f,
				Sides = 16,
			};
			cursor.MaterialOverride = new SpatialMaterial()
			{
				FlagsFixedSize = true
			};
			GetTree().Root.CallDeferred("add_child", cursor);

			_currentTool = ToolCache.GetOrCreateTool<TargetTool>();
			_currentTool.OnToolHeld(this);

			handLocalOrigin = Hand.Translation.z;
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
			if ((ThumbTip.GlobalTransform.origin.DistanceTo(IndexTip.GlobalTransform.origin) < 0.03f) ^ isPinching)
			{
				IsPinching = !isPinching;
			}
			_currentTool.Update(this);
			UpdateHandRayLine();
			UpdateCursor();
		}

		public override void _PhysicsProcess(float delta)
		{
			if (Input.IsActionPressed("hand_touch"))
			{
				if (Hand.Translation.z > handLocalOrigin - 0.05f)
					Hand.Translation -= Hand.Transform.basis.z * 0.0048f;
			}
			else
			{
				if (Hand.Translation.z < handLocalOrigin)
					Hand.Translation += Hand.Transform.basis.z * 0.0048f;
			}
			if (Input.IsActionJustPressed("Fire2"))
			{
				var animationPlayer = Hand.GetNode<AnimationPlayer>("AnimationPlayer");
				animationPlayer?.Play("Pinch");
			}
			else if (Input.IsActionJustReleased("Fire2"))
			{
				var animationPlayer = Hand.GetNode<AnimationPlayer>("AnimationPlayer");
				animationPlayer?.PlayBackwards("Pinch");
			}
		}

		public void SetCursorNormal(Vector3 normal)
		{
			cursorNormal = normal;
		}

		public void SetCursorColor(Color color)
		{
			((SpatialMaterial)cursor.MaterialOverride).AlbedoColor = color;
		}

		private void UpdateHandRayLine()
		{
			var width = 1.6f;
			var startDepth = ToLocal(HandRayOrigin).Project(ProjectLocalRayNormal(OS.WindowSize / 2)).Length();
			var endDepth = ToLocal(HandRayHitPoint).Project(ProjectLocalRayNormal(OS.WindowSize / 2)).Length();
			var startPoint = UnprojectPosition(HandRayOrigin);
			var endPoint = UnprojectPosition(HandRayHitPoint);
			var normal = endPoint - startPoint;
			normal = new Vector2(-normal.y, normal.x).Normalized();
			// p# variable is a point in the 2D coordinate.
			// v# variable is a vector in the 3D coordinate.
			/* 	p4(v4)    p3(v3)
					-----
					|	|
					|	|
					|	|
					-----
				p1(v1)    p2(v2)
			*/
			var p1 = startPoint + normal * width;
			var p2 = startPoint - normal * width;
			var p3 = endPoint - normal * width;
			var p4 = endPoint + normal * width;

			var v1 = ProjectPosition(p1, startDepth);
			var v2 = ProjectPosition(p2, startDepth);
			var v3 = ProjectPosition(p3, endDepth);
			var v4 = ProjectPosition(p4, endDepth);

			handRayLine.Clear();
			handRayLine.Begin(Mesh.PrimitiveType.TriangleStrip);

			handRayLine.SetUv(new Vector2(0, 0));
			handRayLine.AddVertex(v1);
			handRayLine.SetUv(new Vector2(0, 1));
			handRayLine.AddVertex(v2);
			handRayLine.SetUv(new Vector2(1, 0));
			handRayLine.AddVertex(v4);
			handRayLine.SetUv(new Vector2(1, 1));
			handRayLine.AddVertex(v3);

			handRayLine.End();
		}

		private void UpdateCursor()
		{
			var basis = Basis.Identity;
			basis.y = cursorNormal;
			basis.x = basis.z.Cross(basis.y);
			basis = basis.Orthonormalized();

			cursor.GlobalTransform = new Transform(basis, HandRayHitPoint);
		}
	}
}
