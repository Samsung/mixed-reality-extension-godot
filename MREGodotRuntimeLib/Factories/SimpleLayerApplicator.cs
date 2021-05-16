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

			switch (layer)
			{
				case CollisionLayer.Default:
					area.CollisionLayer = defaultLayer;
					break;
				case CollisionLayer.Navigation:
					area.CollisionLayer = navigationLayer;
					break;
				case CollisionLayer.Hologram:
					area.CollisionLayer = hologramLayer;
					break;
				case CollisionLayer.UI:
					area.CollisionLayer = uiLayer;
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
