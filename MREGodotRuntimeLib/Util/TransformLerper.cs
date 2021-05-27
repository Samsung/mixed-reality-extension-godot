// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Godot;

namespace MixedRealityExtension.Util
{
	/// <summary>
	/// A helper class useful for keeping a transform's position and rotation in sync with that of a transform on another computer.
	/// Idea is that the lerper would receive regular position and/or rotation updates, and would lerp between those updates so that
	/// the player sees smooth animation.
	/// </summary>
	public class TransformLerper
	{
		private readonly Spatial _spatial;

		private Vector3 _targetPosition;
		private Quat _targetRotation;

		private bool _lerpingPosition = false;
		private bool _lerpingRotation = false;

		private Vector3 _startPosition;
		private Quat _startRotation;

		private float _startTime;
		private float _updatePeriod;
		private float _percentComplete = 1f;

		/// <summary>
		/// Our default period is based off of the 10hz update cycle that we use for sending actor updates or corrections.
		/// We add in a variance to account for network lag to give the best tuned feel.
		/// </summary>
		private static readonly float DefaultUpdatePeriod = 200f;

		/// <summary>
		/// Initializes and instance of class <see cref="TransformLerper"/>
		/// </summary>
		/// <param name="spatial"></param>
		public TransformLerper(Spatial spatial)
		{
			this._spatial = spatial;
		}

		/// <summary>
		/// Called to update the target of the lerper for its given transform..
		/// </summary>
		/// <param name="position">The optional new position.</param>
		/// <param name="rotation">The optional new rotation.</param>
		/// <param name="updatePeriod">the expected amount time in seconds, between updates. This is the
		/// time the lerper will take, starting from now, to reach the target position/rotation.</param>
		public void SetTarget(Vector3? position, Quat? rotation, float updatePeriod = 0)
		{
			bool canLerp = _spatial != null && (position != null || rotation != null);
			_percentComplete = canLerp ? 0f : 1f;
			if (!canLerp)
			{
				return;
			}

			_startTime = OS.GetTicksMsec();
			updatePeriod *= 1000f;
			this._updatePeriod = updatePeriod > 0 ? updatePeriod : DefaultUpdatePeriod;

			if (position.HasValue)
			{
				_targetPosition = position.Value;
				_startPosition = _spatial.GlobalTransform.origin;
				_lerpingPosition = true;
			}
			else
			{
				_lerpingPosition = false;
			}

			if (rotation.HasValue)
			{
				_targetRotation = rotation.Value;
				_startRotation = new Quat(_spatial.GlobalTransform.basis);
				_lerpingRotation = true;
			}
			else
			{
				_lerpingRotation = false;
			}
		}

		/// <summary>
		/// Clears the target position and/or rotation.
		/// </summary>
		public void ClearTarget()
		{
			SetTarget(null, null);
		}

		/// <summary>
		/// Call this every frame.
		/// Updates the transform position/rotation by one frames-worth of lerping.
		/// </summary>
		public void Update()
		{
			if (_percentComplete < 1f)
			{
				_percentComplete = Mathf.Clamp((OS.GetTicksMsec() - _startTime) / _updatePeriod, 0f, 1f);

				Vector3 origin = Vector3.Zero;
				Basis basis = _spatial.GlobalTransform.basis;

				if (_lerpingPosition)
				{
					origin = _startPosition.LinearInterpolate(_targetPosition, _percentComplete);
				}

				if (_lerpingRotation)
				{
					basis = new Basis(_startRotation.Slerpni(_targetRotation, _percentComplete));
					basis.Scale = _spatial.GlobalTransform.basis.Scale;
				}
				_spatial.GlobalTransform = new Transform(basis, origin);
			}
		}
	}

}
