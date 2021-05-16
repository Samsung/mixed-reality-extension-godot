// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CollisionLayer = MixedRealityExtension.Core.CollisionLayer;
using Godot;

namespace MixedRealityExtension.PluginInterfaces
{
	/// <summary>
	/// Apply an MRE collision layers to Unity collision.
	/// </summary>
	public interface ILayerApplicator
	{
		/// <summary>
		/// The Unity layer new actors should be created on.
		/// </summary>
		byte DefaultLayer { get; }

		/// <summary>
		/// Apply a layer to a collision.
		/// </summary>
		/// <param name="layer">An MRE collision layer</param>
		/// <param name="area">A Godot Area</param>
		void ApplyLayerToCollider(CollisionLayer? layer, Area area);

		/// <summary>
		/// Get a collision's layer.
		/// </summary>
		/// <param name="area">The area.</param>
		/// <returns>The layer the given collision is on.</returns>
		CollisionLayer DetermineLayerOfCollider(Area area);
	}
}
