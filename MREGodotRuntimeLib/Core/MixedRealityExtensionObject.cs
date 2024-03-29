// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using System;
using Godot;

namespace MixedRealityExtension.Core
{
	internal abstract partial class MixedRealityExtensionObject : Node3D, IMixedRealityExtensionObject
	{
		/// <inheritdoc />
		public Guid Id { get; private set; }

		/// <inheritdoc />
		public Guid AppInstanceId => App.InstanceId;

 		/// <inheritdoc />
		public virtual string Name => ((Node3D)this).Name;

		/// <inheritdoc />
		public Node3D Node3D => this;

		internal MixedRealityExtensionApp App { get; private set; }

		/// <summary>
		/// Gets the local user. Will be null if the local client has not joined as a user.
		/// </summary>
		public IUser LocalUser => App.LocalUser;

		public void Initialize(Guid id, MixedRealityExtensionApp app)
		{
			Id = id;
			App = app;
		}

		protected abstract void InternalUpdate(float delta);

		protected virtual void InternalFixedUpdate()
		{

		}

		protected virtual void OnStart()
		{

		}

		protected virtual void OnAwake()
		{

		}

		protected virtual void OnDestroyed()
		{

		}

		#region Node Methods

		public override void _Ready()
		{
			OnStart();
		}

		public override void _EnterTree()
		{
			OnAwake();
		}

		public override void _Process(double delta)
		{
			InternalUpdate((float)delta);
		}

		public override void _PhysicsProcess(double delta)
		{
			InternalFixedUpdate();
		}

		public override void _ExitTree()
		{
			OnDestroyed();
		}

		#endregion
	}
}
