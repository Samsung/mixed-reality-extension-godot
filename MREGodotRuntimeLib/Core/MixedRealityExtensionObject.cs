// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using System;
using Godot;

namespace MixedRealityExtension.Core
{
	internal abstract class MixedRealityExtensionObject : Node, IMixedRealityExtensionObject
	{
		/// <inheritdoc />
		public Guid Id { get; private set; }

		/// <inheritdoc />
		public Guid AppInstanceId => App.InstanceId;

		/// <inheritdoc />
		public virtual new string Name => Name;

		/// <inheritdoc />
		public Node GameObject => this;

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

		protected abstract void InternalUpdate();

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

		#region MonoBehaviour Methods

		private void Start()
		{
			OnStart();
		}

		private void Awake()
		{
			OnAwake();
		}

		private void Update()
		{
			InternalUpdate();
		}

		private void FixedUpdate()
		{
			InternalFixedUpdate();
		}

		private void OnDestroy()
		{
			OnDestroyed();
		}

		#endregion
	}
}
