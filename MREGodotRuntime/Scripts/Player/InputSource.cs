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
		private Player player;
		private float handLocalOrigin;
		private Cursor cursor;
		private User.Ray ray;

		internal Spatial PokePointer;
		public Node UserNode { get; set; }
		public Tool CurrentTool => _currentTool;

		public Spatial Hand { get; private set; }

		public Vector3 HandRayOrigin => Hand.GlobalTransform.origin;

		public Vector3 HandRayHitPoint { get; set; }

		public Vector3 HitPointNormal { get; set; }

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

		public User.Ray Ray
		{
			get => ray;
			set
			{
				if (ray == value)
					return;

				if (ray != null)
					ray.QueueFree();

				ray = value;
				if (ray != null && IsInsideTree())
					GetTree().Root.CallDeferred("add_child", ray);
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

		public Spatial ThumbTip { get; private set; }
		public Spatial IndexTip { get; private set; }

		public static readonly Guid UserId = new Guid();

		// Only target layers 0 (Default), 5 (UI), and 10 (Hologram).
		// You still want to hit all layers, but only interact with these.
		private const uint LayerMask = (1 << 0) | (1 << 5) | (1 << 10);

		public override void _Ready()
		{
			player = GetParent<Player>();
			spaceState = GetWorld().DirectSpaceState;

			Hand = player.Hand;
			ThumbTip = player.ThumbTip;
			IndexTip = player.IndexTip;

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

			var cursorScene = ResourceLoader.Load<PackedScene>("res://MREGodotRuntime/Scenes/RingCursor.tscn");
			Cursor = cursorScene.Instance<Cursor>();
			AddChild(Cursor);

			var ray = ResourceLoader.Load<PackedScene>("res://MREGodotRuntime/Scenes/Ray.tscn");
			Ray = ray.Instance<Ray>();
			AddChild(Ray);

			_currentTool = ToolCache.GetOrCreateTool<TargetTool>();
			_currentTool.OnToolHeld(this);

			handLocalOrigin = Hand.Translation.z;

			if (OS.GetName() == "Android")
			{
				Hand.Visible = false;
				Cursor.Visible = false;
				Ray.Visible = false;
			}
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
			if (OS.GetName() == "Android")
			{
				if (player.screenTouchPosition == Vector2.Zero)
					return new Dictionary();
				forward = ProjectRayNormal(player.screenTouchPosition);
				from = ProjectRayOrigin(player.screenTouchPosition);
				to = from + forward * 10000;
			}
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
			// reset 'pinchChanged' if the pinching doesn't interact with any target.
			if (_currentTool is TargetTool targetTool && targetTool.Target == null)
				pinchChaged = false;

			Cursor.SetCursorTransform(HandRayHitPoint, HitPointNormal);
			Ray.DrawRay(GlobalTransform.origin, HandRayHitPoint);
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
			if (Input.IsActionJustPressed("Fire1"))
			{
				var animationPlayer = Hand.GetNode<AnimationPlayer>("AnimationPlayer");
				animationPlayer?.Play("Pinch");
			}
			else if (Input.IsActionJustReleased("Fire1"))
			{
				var animationPlayer = Hand.GetNode<AnimationPlayer>("AnimationPlayer");
				if (OS.GetName() == "Android")
				{
					animationPlayer.Stop(true);
					animationPlayer.PlaybackSpeed = 0.2f;
				}
				animationPlayer?.PlayBackwards("Pinch");
			}
		}
	}
}
