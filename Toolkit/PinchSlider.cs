// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using System;
using Godot;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;

namespace Microsoft.MixedReality.Toolkit.UI
{
	/// <summary>
	/// A slider that can be moved by grabbing / pinching a slider thumb
	/// </summary>
	internal class PinchSlider : Spatial, IToolkit, IMixedRealityPointerHandler, IMixedRealityTouchHandler
	{
		#region Public Properties
		private IActor thumbActor = null;
		/// <summary>
		/// The Actor that contains the slider thumb
		/// </summary>
		public Spatial Thumb
		{
			get
			{
				return (Spatial)thumbActor;
			}
			set
			{
				if (thumbActor == value) return;
				if (thumbActor != null) RemoveChild((Spatial)thumbActor);
				if (value != null)
				{
					this.thumbActor = value as IActor;
					AddChild((Spatial)thumbActor);
				}
			}
		}

		/// <summary>
		/// This will get rotated to match the slider axis.
		/// </summary>
		private MeshInstance trackMesh = null;

		/// <summary>
		/// Determines whether or not this slider is controllable via touch events
		/// </summary>
		public bool IsTouchable { get; set; }

		private bool snapToPosition = true;

		/// <summary>
		/// Determines whether or not this slider snaps to the designated position on the slider
		/// </summary>
		public bool SnapToPosition
		{
			get { return snapToPosition; }
			set
			{
				snapToPosition = value;
				TouchCollisionShape.Disabled = !value;
				ThumbCollisionShape.Disabled = value;
			}
		}

		/// <summary>
		/// Used to control the slider on the track when snapToPosition is false
		/// </summary>
		public CollisionShape ThumbCollisionShape => Thumb.GetNode<CollisionShape>("PinchSliderThumb/Mesh/PinchSliderThumbArea/CollisionShape");

		/// <summary>
		/// Used to determine the position we snap the slider do when snapToPosition is true
		/// </summary>
		public CollisionShape TouchCollisionShape => trackMesh.GetNode<CollisionShape>("Area/CollisionShape");

		[Export]
		private float sliderValue = 0.5f;
		public float SliderValue
		{
			get { return sliderValue; }
			set
			{
				var oldSliderValue = sliderValue;
				sliderValue = value;
				UpdateUI();

				_valueChangedAction.PerformActionUpdate(new ActionData<float>()
				{
					value = sliderValue
				});
				EmitSignal(nameof(value_changed));
			}
		}

		/// <summary>
		/// It determines whether the slider steps according to subdivisions
		/// </summary>
		[Export]
		public bool UseSliderStepDivisions { get; set; }

		/// <summary>
		/// It holds the number of subdivisions the slider is split into.
		/// </summary>
		[Export]
		public int SliderStepDivisions { get ; set; } = 1;

		[Export]
		private SliderAxis sliderAxis = SliderAxis.XAxis;
		/// <summary>
		/// Property accessor of sliderAxis. The axis the slider moves along.
		/// </summary>
		public SliderAxis CurrentSliderAxis
		{
			get { return sliderAxis; }
			set
			{
				sliderAxis = value;
				UpdateVisualsOrientation();
			}
		}

		/// <summary>
		/// Previous value of slider axis, is used in order to detect change in current slider axis value
		/// </summary>
		private SliderAxis? previousSliderAxis = null;
		/// <summary>
		/// Property accessor for previousSliderAxis that is used also to initialize the property with the current value in case of null value.
		/// </summary>
		private SliderAxis PreviousSliderAxis
		{
			get
			{
				if (previousSliderAxis == null)
				{
					previousSliderAxis = CurrentSliderAxis;
				}
				return previousSliderAxis.Value;
			}
			set
			{
				previousSliderAxis = value;
			}
		}

		[Export]
		public float SliderStartDistance { get; set; } = -.125f;

		[Export]
		public float SliderEndDistance { get; set; } = .125f;

		/// <summary>
		/// Gets the start position of the slider, in world space, or zero if invalid.
		/// Sets the start position of the slider, in world space, projected to the slider's axis.
		/// </summary>
		public Vector3 SliderStartPosition
		{
			get { return ToGlobal(GetSliderAxis() * SliderStartDistance); }
			set { SliderStartDistance = ToLocal(value).Dot(GetSliderAxis()); }
		}

		/// <summary>
		/// Gets the end position of the slider, in world space, or zero if invalid.
		/// Sets the end position of the slider, in world space, projected to the slider's axis.
		/// </summary>
		public Vector3 SliderEndPosition
		{
			get { return ToGlobal(GetSliderAxis() * SliderEndDistance); }
			set { SliderEndDistance = ToLocal(value).Dot(GetSliderAxis()); }
		}

		/// <summary>
		/// Returns the vector from the slider start to end positions
		/// </summary>
		public Vector3 SliderTrackDirection
		{
			get { return SliderEndPosition - SliderStartPosition; }
		}

		private IUser currentUser;
		public IUser CurrentUser
		{
			get => currentUser ?? (Parent as IActor).App.LocalUser;
			set => currentUser = value;
		}

		#endregion

		#region Event Handlers

		private MWAction<ActionData<float>> _valueChangedAction = new MWAction<ActionData<float>>();

		[Signal]
		public delegate void value_changed();

		[Signal]
		public delegate void interaction_started();

		[Signal]
		public delegate void interaction_ended();
		/*
		public SliderEvent OnHoverEntered = new SliderEvent();
		public SliderEvent OnHoverExited = new SliderEvent();
		*/
		#endregion

		#region Private Fields

		/// <summary>
		/// Position offset for slider handle in world space.
		/// </summary>
		private Vector3 sliderThumbOffset = Vector3.Zero;


		/// <summary>
		/// Private member used to adjust slider values
		/// </summary>
		private float sliderStepVal => (maxVal - minVal) / SliderStepDivisions;

		#endregion

		#region Protected Properties

		/// <summary>
		/// Float value that holds the starting value of the slider.
		/// </summary>
		protected float StartSliderValue { get; private set; }

		/// <summary>
		/// Starting position of mixed reality pointer in world space
		/// Used to track pointer movement
		/// </summary>
		protected Vector3 StartPointerPosition { get; private set; }

		/// <summary>
		/// Interface for handling tool being used in UX interaction.
		/// </summary>
		protected Spatial ActiveInputSource { get; private set; }

		#endregion

		#region Constants
		/// <summary>
		/// Minimum distance between start and end of slider, in world space
		/// </summary>
		private const float MinSliderLength = 0.001f;

		/// <summary>
		/// The minimum value that the slider can take on
		/// </summary>
		private const float minVal = 0.0f;

		/// <summary>
		/// The maximum value that the slider can take on
		/// </summary>
		private const float maxVal = 1.0f;

		#endregion

		#region Node Virtual Methods
		public override void _Ready()
		{
			Parent = GetParent();

			if (UseSliderStepDivisions)
			{
				InitializeStepDivisions();
			}

			this.RegisterAction(_valueChangedAction, "value_changed");

			trackMesh = GetNode<MeshInstance>("Mesh");
			//SnapToPosition = snapToPosition;
			TouchCollisionShape.Disabled = false;
			UpdateTrackMesh();

			_valueChangedAction.PerformActionUpdate(new ActionData<float>()
			{
				value = sliderValue
			});
			EmitSignal(nameof(value_changed));

			((IMixedRealityTouchHandler)this).RegisterTouchEvent(this, Parent);
			((IMixedRealityPointerHandler)this).RegisterPointerEvent(this, Parent);
		}

		private void OnDisable()
		{
			if (ActiveInputSource != null)
			{
				OnInteractionEnded();
			}
		}

		private void OnValidate()
		{
			CurrentSliderAxis = sliderAxis;
		}

		#endregion

		#region Private Methods
		private void InitializeSliderThumb()
		{
			var startToThumb = Thumb.GlobalTransform.origin - SliderStartPosition;
			var thumbProjectedOnTrack = SliderStartPosition + startToThumb.Project(SliderTrackDirection);
			sliderThumbOffset = Thumb.GlobalTransform.origin - thumbProjectedOnTrack;

			UpdateUI();
		}

		/// <summary>
		/// Private method used to adjust initial slider value to stepwise values
		/// </summary>
		private void InitializeStepDivisions()
		{
			SliderValue = SnapSliderToStepPositions(SliderValue);
		}

		/// <summary>
		/// Update orientation of track mesh based on slider axis orientation
		/// </summary>
		private void UpdateTrackMesh()
		{
			if (trackMesh != null)
			{
				Transform newTransform = Transform.Identity;
				switch (sliderAxis)
				{
					case SliderAxis.XAxis:
						newTransform.basis = newTransform.basis.Rotated(Vector3.Forward, Mathf.Pi / 2);
						break;
					case SliderAxis.YAxis:
						break;
					case SliderAxis.ZAxis:
						newTransform.basis = newTransform.basis.Rotated(Vector3.Right, Mathf.Pi / 2);
						break;
				}
				trackMesh.Transform = newTransform;
			}
		}

		/// <summary>
		/// Update orientation of thumb mesh based on slider axis orientation
		/// </summary>
		private void UpdateThumb()
		{
			if (Thumb != null)
			{
				Transform newTransform = Transform.Identity;
				switch (sliderAxis)
				{
					case SliderAxis.XAxis:
						newTransform.basis = newTransform.basis.Rotated(Vector3.Forward, Mathf.Pi / 2);
						break;
					case SliderAxis.YAxis:
						break;
					case SliderAxis.ZAxis:
						newTransform.basis = newTransform.basis.Rotated(Vector3.Right, Mathf.Pi / 2);
						break;
				}
				Thumb.Transform = newTransform;
			}
		}

		/// <summary>
		/// Update orientation of the visual components of pinch slider
		/// </summary>
		private void UpdateVisualsOrientation()
		{
			if (PreviousSliderAxis != sliderAxis)
			{
				UpdateThumb();
				UpdateTrackMesh();
				PreviousSliderAxis = sliderAxis;
			}
		}

		private Vector3 GetSliderAxis()
		{
			switch (sliderAxis)
			{
				case SliderAxis.XAxis:
					return Vector3.Right;
				case SliderAxis.YAxis:
					return Vector3.Up;
				case SliderAxis.ZAxis:
					return Vector3.Forward;
				default:
					throw new ArgumentOutOfRangeException("Invalid slider axis");
			}
		}

		private void UpdateUI()
		{
			var newSliderPos = SliderStartPosition + sliderThumbOffset + SliderTrackDirection * sliderValue;
			Thumb.GlobalTransform = new Transform(Thumb.GlobalTransform.basis, newSliderPos);
		}

		private float SnapSliderToStepPositions(float value)
		{
			var stepCount = value / sliderStepVal;
			var snappedValue = sliderStepVal * Mathf.RoundToInt(stepCount);
			Mathf.Clamp(snappedValue, minVal, maxVal);
			return snappedValue;
		}

		private void CalculateSliderValueBasedOnTouchPoint(Vector3 touchPoint)
		{
			var sliderTouchPoint = touchPoint - SliderStartPosition;
			var sliderVector = SliderEndPosition - SliderStartPosition;

			// If our touch point goes off the start side of the slider, set it's value to minVal and return immediately
			// Explanation of the math here: https://www.quora.com/Can-scalar-projection-be-negative
			if (sliderTouchPoint.Dot(sliderVector) < 0)
			{
				SliderValue = minVal;
				return;
			}

			float sliderProgress = sliderTouchPoint.Project(sliderVector).Length();
			float result = sliderProgress / sliderVector.Length();
			float clampedResult = result;
			if (UseSliderStepDivisions)
			{
				clampedResult = SnapSliderToStepPositions(result);
			}
			clampedResult = Mathf.Clamp(clampedResult, minVal, maxVal);

			SliderValue = clampedResult;
		}

		private void ApplyThumb(Guid thumbId)
		{
			var actor = Parent as IActor;
			var thumb = actor.App.FindActor(thumbId) as Spatial;
			thumb.GetParent()?.RemoveChild(thumb);
			Thumb = thumb;

			UpdateThumb();
			InitializeSliderThumb();
			ThumbCollisionShape.Disabled = true;
		}

		#endregion

		#region IMixedRealityFocusHandler
		/*
		public void OnFocusEnter(FocusEventData eventData)
		{
			OnHoverEntered.Invoke(new SliderEventData(sliderValue, sliderValue, eventData.Pointer, this));
		}

		public void OnFocusExit(FocusEventData eventData)
		{
			OnHoverExited.Invoke(new SliderEventData(sliderValue, sliderValue, eventData.Pointer, this));
		}
		*/
		#endregion

		#region IMixedRealityPointerHandler

		public virtual void OnPointerUp(Spatial inputSource, Node userNode, Vector3 point)
		{
			if (inputSource == ActiveInputSource)
			{
				OnInteractionEnded();
			}
		}

		public virtual void OnPointerDown(Spatial inputSource, Node userNode, Vector3 point)
		{
			if (ActiveInputSource == null)
			{
				ActiveInputSource = inputSource;
				StartPointerPosition = point;

				if (SnapToPosition)
				{
					CalculateSliderValueBasedOnTouchPoint(point);
				}
				StartSliderValue = sliderValue;

				OnInteractionStarted(userNode);
			}
		}

		public virtual void OnPointerDragged(Spatial inputSource, Node userNode, Vector3 point)
		{
			if (inputSource == ActiveInputSource)
			{
				var delta = point - StartPointerPosition;
				var handDelta = SliderTrackDirection.Normalized().Dot(delta);

				if (UseSliderStepDivisions)
				{
					var stepVal = (handDelta / SliderTrackDirection.Length() > 0) ? sliderStepVal : (sliderStepVal * -1);
					var stepMag = Mathf.Floor(Mathf.Abs(handDelta / SliderTrackDirection.Length()) / sliderStepVal);
					SliderValue = Mathf.Clamp(StartSliderValue + (stepVal * stepMag), 0, 1);
				}
				else
				{
					SliderValue = Mathf.Clamp(StartSliderValue + handDelta / SliderTrackDirection.Length(), 0, 1);
				}
			}
		}

		public virtual void OnPointerClicked(Spatial inputSource, Node userNode, Vector3 point) { }

		#endregion


		#region IMixedRealityTouchHandler
		public virtual void OnTouchStarted(Spatial inputSource, Node userNode, Vector3 point)
		{
			if (IsTouchable)
			{
				OnInteractionStarted(userNode);
			}
		}

		public virtual void OnTouchCompleted(Spatial inputSource, Node userNode, Vector3 point)
		{
			if (IsTouchable)
			{
				OnInteractionEnded();
			}
		}

		/// <summary>
		/// When the collider is touched, use the touch point to Calculate the Slider value
		/// </summary>
		public virtual void OnTouchUpdated(Spatial inputSource, Node userNode, Vector3 point)
		{
			if (IsTouchable)
			{
				CalculateSliderValueBasedOnTouchPoint(point);
			}
		}

		#endregion IMixedRealityTouchHandler

		#region IToolkit

		public Node Parent { get; private set; }

		public virtual void ApplyPatch(ToolkitPatch toolkitPatch)
		{
			if (toolkitPatch is PinchSliderPatch patch)
			{
				ApplyThumb(patch.ThumbId);
			}
		}

		public virtual void OnInteractionStarted(Node userNode)
		{
			CurrentUser = this.GetMREUser(userNode);

			_valueChangedAction.StartAction(CurrentUser, new ActionData<float>()
			{
				value = sliderValue
			});
			EmitSignal(nameof(interaction_started));
		}

		public virtual void OnInteractionEnded()
		{
			_valueChangedAction.StopAction(CurrentUser, new ActionData<float>()
			{
				value = sliderValue
			});
			CurrentUser = null;
			EmitSignal(nameof(interaction_ended));
			ActiveInputSource = null;
		}

		#endregion IToolkit
	}
}
