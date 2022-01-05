// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using System;
using Godot;
using Assets.Scripts.Tools;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Behaviors.Actions;

namespace Microsoft.MixedReality.Toolkit.UI
{
	/// <summary>
	/// A slider that can be moved by grabbing / pinching a slider thumb
	/// </summary>
	internal class PinchSlider : Spatial, IToolkit, IMixedRealityPointerHandler, IMixedRealityTouchHandler
	{
		#region Public Properties
		private Actor thumbActor = null;
		/// <summary>
		/// The Actor that contains the slider thumb
		/// </summary>
		public Actor ThumbActor
		{
			get
			{
				return thumbActor;
			}
			set
			{
				if (thumbActor == value) return;
				if (thumbActor != null) RemoveChild(thumbActor);
				if (value != null)
				{
					thumbActor = value;
					AddChild(thumbActor);
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
		public CollisionShape ThumbCollisionShape => thumbActor.GetNode<CollisionShape>("PinchSliderThumb/Mesh/PinchSliderThumbArea/CollisionShape");

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
		protected Tool ActiveTool { get; private set; }

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
			if (UseSliderStepDivisions)
			{
				InitializeStepDivisions();
			}

			this.RegisterHandler<IMixedRealityPointerHandler>();
			this.RegisterHandler<IMixedRealityTouchHandler>();
			ToolkitAction.RegisterAction(_valueChangedAction, "value_changed", this);
			Connect(nameof(value_changed), this, nameof(_on_PinchSlider_value_changed));
			Connect(nameof(interaction_started), this, nameof(_on_PinchSlider_interaction_started));
			Connect(nameof(interaction_ended), this, nameof(_on_PinchSlider_interaction_ended));

			trackMesh = GetNode<MeshInstance>("Mesh");
			//SnapToPosition = snapToPosition;
			TouchCollisionShape.Disabled = false;
			UpdateTrackMesh();

			EmitSignal(nameof(value_changed));
		}

		private void OnDisable()
		{
			if (ActiveTool != null)
			{
				EndInteraction();
			}
		}

		private void OnValidate()
		{
			CurrentSliderAxis = sliderAxis;
		}

		private void _on_PinchSlider_value_changed()
		{
			_valueChangedAction.PerformActionUpdate(new ActionData<float>()
			{
				value = sliderValue
			});
		}

		private void _on_PinchSlider_interaction_started()
		{
			var user = GetParent<Actor>().App.LocalUser;
			if (user != null)
			{
				_valueChangedAction.StartAction(user, new ActionData<float>()
				{
					value = sliderValue
				});
			}
		}

		private void _on_PinchSlider_interaction_ended()
		{
			var user = GetParent<Actor>().App.LocalUser;
			if (user != null)
			{
				_valueChangedAction.StopAction(user, new ActionData<float>()
				{
					value = sliderValue
				});
			}
		}

		#endregion

		#region Private Methods
		private void InitializeSliderThumb()
		{
			var startToThumb = thumbActor.GlobalTransform.origin - SliderStartPosition;
			var thumbProjectedOnTrack = SliderStartPosition + startToThumb.Project(SliderTrackDirection);
			sliderThumbOffset = thumbActor.GlobalTransform.origin - thumbProjectedOnTrack;

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
		private void UpdateThumbActor()
		{
			if (ThumbActor != null)
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
				ThumbActor.Transform = newTransform;
			}
		}

		/// <summary>
		/// Update orientation of the visual components of pinch slider
		/// </summary>
		private void UpdateVisualsOrientation()
		{
			if (PreviousSliderAxis != sliderAxis)
			{
				UpdateThumbActor();
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
			thumbActor.GlobalTransform = new Transform(thumbActor.GlobalTransform.basis, newSliderPos);
		}

		private void EndInteraction()
		{
			EmitSignal(nameof(interaction_ended));
			ActiveTool = null;
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
			var actor = GetParent<Actor>();
			var thumb = actor.App.FindActor(thumbId) as Actor;
			thumb.GetParent()?.RemoveChild(thumb);
			ThumbActor = thumb;
			thumb.ParentId = actor.Id;

			UpdateThumbActor();
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

		public void OnPointerUp(MixedRealityPointerEventData eventData)
		{
			if (eventData.Tool == ActiveTool)
			{
				EndInteraction();
			}
		}

		public void OnPointerDown(MixedRealityPointerEventData eventData)
		{
			if (ActiveTool == null)
			{
				ActiveTool = eventData.Tool;
				StartPointerPosition = eventData.InputData;

				if (SnapToPosition)
				{
					CalculateSliderValueBasedOnTouchPoint(eventData.InputData);
				}
				StartSliderValue = sliderValue;
				EmitSignal(nameof(interaction_started));
			}
		}

		public virtual void OnPointerDragged(MixedRealityPointerEventData eventData)
		{
			if (eventData.Tool == ActiveTool)
			{
				var delta = eventData.InputData - StartPointerPosition;
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

		public void OnPointerClicked(MixedRealityPointerEventData eventData) { }

		#endregion


		#region IMixedRealityTouchHandler
		public void OnTouchStarted(TouchInputEventData eventData)
		{
			if (IsTouchable)
			{
				EmitSignal(nameof(interaction_started));
			}
		}

		public void OnTouchCompleted(TouchInputEventData eventData)
		{
			if (IsTouchable)
			{
				EndInteraction();
			}
		}

		/// <summary>b
		/// When the collider is touched, use the touch point to Calculate the Slider value
		/// </summary>
		public void OnTouchUpdated(TouchInputEventData eventData)
		{
			if (IsTouchable)
			{
				CalculateSliderValueBasedOnTouchPoint(eventData.InputData);
			}
		}

		#endregion IMixedRealityTouchHandler

		#region IToolkit
		public virtual void ApplyPatch(ToolkitPatch toolkitPatch)
		{
			if (toolkitPatch is PinchSliderPatch patch)
			{
				ApplyThumb(patch.ThumbId);
			}
		}
		#endregion IToolkit
	}
}
