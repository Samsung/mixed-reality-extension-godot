// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.Assets;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;

namespace MixedRealityExtension.Messaging.Payloads
{
	/// <summary>
	/// App => Engine
	/// Payload instructing the engine to preload the listed asset container.
	/// </summary>
	public class LoadAssets : NetworkCommandPayload
	{
		/// <summary>
		/// The logical container that the new assets should be assigned to.
		/// </summary>
		public Guid ContainerId;

		/// <summary>
		/// The asset container to load.
		/// </summary>
		public AssetSource Source;

		/// <summary>
		/// The type of collider to add to the actor upon creation.
		/// </summary>
		public ColliderType ColliderType;
	}

	/// <summary>
	/// Engine => App
	/// Replies to LoadAssetRequests with the contents of the loaded bundle.
	/// </summary>
	public class AssetsLoaded : NetworkCommandPayload
	{
		/// <summary>
		/// If the load failed, this string contains the reason why.
		/// </summary>
		public string FailureMessage { get; set; }

		/// <summary>
		/// The loaded assets.
		/// </summary>
		public Asset[] Assets { get; set; }
	}

	/// <summary>
	/// App => Engine
	/// An asset has updated.
	/// </summary>
	public class AssetUpdate : NetworkCommandPayload
	{
		/// <summary>
		/// The asset that was updated
		/// </summary>
		public Asset Asset;
	}

	/// <summary>
	/// App => Engine
	/// Instructs the engine to instantiate the prefab with the given ID.
	/// </summary>
	public class CreateFromPrefab : CreateActor
	{
		/// <summary>
		/// The ID of an already-loaded asset
		/// </summary>
		public Guid PrefabId;

		/// <summary>
		/// Assign all colliders in this prefab to this layer
		/// </summary>
		public CollisionLayer? CollisionLayer;
	}

	/// <summary>
	/// App => Engine
	/// Instructs the engine to instantiate the Toolkit Button.
	/// </summary>
	public class CreateFromToolkitButton : CreateActor
	{
		/// <summary>
		/// The main text of the button
		/// </summary>
		public string text;

		/// <summary>
		/// The main color of the button
		/// </summary>
		public ColorPatch Color;
	}

	/// <summary>
	/// App => Engine
	/// Instructs the engine to instantiate the Toolkit Toggle Button.
	/// </summary>
	public class CreateFromToolkitToggleButton : CreateFromToolkitButton
	{
		/// <summary>
		/// Determines whether the Button is toggled or not.
		/// </summary>
		public Boolean IsToggled;
	}

	/// <summary>
	/// App => Engine
	/// Instructs the engine to instantiate the Toolkit Pinch Slider.
	/// </summary>
	public class CreateFromToolkitPinchSlider : CreateActor
	{
		public Guid ThumbId;
	}

	/// <summary>
	/// App => Engine
	/// Instructs the engine to instantiate the Toolkit Pinch Slider Thumb.
	/// </summary>
	public class CreateFromToolkitPinchSliderThumb : CreateActor
	{
	}

	/// <summary>
	/// App => Engine
	/// Instructs the engine to instantiate the Toolkit Scrolling Object Collection.
	/// </summary>
	public class CreateFromToolkitScrollingObjectCollection : CreateActor
	{
		public Guid[] ScrollContents;
		public Microsoft.MixedReality.Toolkit.UI.ScrollingObjectCollection.ScrollDirectionType? ScrollDirectionType;
		public int? CellsPerTier;
		public int? TiersPerPage;
		public float? CellWidth;
		public float? CellHeight;
		public float? CellDepth;
	}

	/// <summary>
	/// App => Engine
	/// Generate a new native asset with the included properties
	/// </summary>
	public class CreateAsset : NetworkCommandPayload
	{
		/// <summary>
		/// The logical container that the new assets should be assigned to.
		/// </summary>
		public Guid ContainerId;

		/// <summary>
		/// Initial properties of the newly created asset
		/// </summary>
		public Asset Definition;
	}

	/// <summary>
	/// App => Engine
	/// Destroy all assets in the given container
	/// </summary>
	public class UnloadAssets : NetworkCommandPayload
	{
		/// <summary>
		/// The container to unload
		/// </summary>
		public Guid ContainerId;
	}
}
