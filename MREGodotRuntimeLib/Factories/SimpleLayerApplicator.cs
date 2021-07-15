// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CollisionLayer = MixedRealityExtension.Core.CollisionLayer;
using ILayerApplicator = MixedRealityExtension.PluginInterfaces.ILayerApplicator;
using Godot;

namespace MixedRealityExtension.Factories
{
	/// <summary>
	/// A simple implementation of ILayerApplicator that simply sets collision actors' `layer` properties.
	/// </summary>
	public class SimpleLayerApplicator : ILayerApplicator
	{
		protected readonly byte defaultLayer;
		protected readonly byte navigationLayer;
		protected readonly byte hologramLayer;
		protected readonly byte uiLayer;

		/// <inheritdoc />
		public byte DefaultLayer => defaultLayer;

		/// <summary>
		/// Apply the given Unity layers to MRE colliders.
		/// </summary>
		/// <param name="defaultLayer"></param>
		/// <param name="navigationLayer"></param>
		/// <param name="hologramLayer"></param>
		/// <param name="uiLayer"></param>
		public SimpleLayerApplicator(byte defaultLayer, byte navigationLayer, byte hologramLayer, byte uiLayer)
		{
			this.defaultLayer = defaultLayer;
			this.navigationLayer = navigationLayer;
			this.hologramLayer = hologramLayer;
			this.uiLayer = uiLayer;
		}

		/// <inheritdoc />
		public virtual void ApplyLayerToCollider(CollisionLayer? layer, Area area)
		{
			if (!layer.HasValue) return;

			//clear collision layer and mask.
			area.CollisionMask = 0;
			area.CollisionLayer = 0;
			switch (layer)
			{
				case CollisionLayer.Default:
					area.SetCollisionLayerBit(defaultLayer, true);
					area.SetCollisionMaskBit(defaultLayer, true);
					area.SetCollisionMaskBit(navigationLayer, true);
					break;
				case CollisionLayer.Navigation:
					area.SetCollisionLayerBit(navigationLayer, true);
					area.SetCollisionMaskBit(navigationLayer, true);
					break;
				case CollisionLayer.Hologram:
					area.SetCollisionLayerBit(hologramLayer, true);
					area.SetCollisionMaskBit(hologramLayer, true);
					break;
				case CollisionLayer.UI:
					break;
			}
		}

		/// <inheritdoc />
		public virtual void ApplyLayerToCollider(CollisionLayer? layer, PhysicsBody physicsBody)
		{
			if (!layer.HasValue) return;

			//clear collision layer and mask.
			physicsBody.CollisionMask = 0;
			physicsBody.CollisionLayer = 0;
			switch (layer)
			{
				case CollisionLayer.Default:
					physicsBody.SetCollisionLayerBit(defaultLayer, true);
					physicsBody.SetCollisionMaskBit(defaultLayer, true);
					physicsBody.SetCollisionMaskBit(navigationLayer, true);
					break;
				case CollisionLayer.Navigation:
					physicsBody.SetCollisionLayerBit(navigationLayer, true);
					physicsBody.SetCollisionMaskBit(navigationLayer, true);
					break;
				case CollisionLayer.Hologram:
					physicsBody.SetCollisionLayerBit(hologramLayer, true);
					physicsBody.SetCollisionMaskBit(hologramLayer, true);
					break;
				case CollisionLayer.UI:
					break;
			}
		}

		/// <inheritdoc />
		public virtual CollisionLayer DetermineLayerOfCollider(Area area)
		{
			if (area.CollisionLayer == navigationLayer)
			{
				return CollisionLayer.Navigation;
			}
			else if (area.CollisionLayer == hologramLayer)
			{
				return CollisionLayer.Hologram;
			}
			else if (area.CollisionLayer == uiLayer)
			{
				return CollisionLayer.UI;
			}
			else
			{
				return CollisionLayer.Default;
			}
		}
	}
}
