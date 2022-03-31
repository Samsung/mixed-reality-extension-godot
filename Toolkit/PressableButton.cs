// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using System.Collections.Generic;
using Godot;
using MixedRealityExtension.Core;
using MixedRealityExtension.PluginInterfaces;
using MixedRealityExtension.Util.GodotHelper;
using MixedRealityExtension.Behaviors.Actions;

namespace Microsoft.MixedReality.Toolkit.UI
{
    ///<summary>
    /// A button that can be pushed via direct touch.
    /// You can use <see cref="Microsoft.MixedReality.Toolkit.PhysicalPressEventRouter"/> to route these events to <see cref="Microsoft.MixedReality.Toolkit.UI.Interactable"/>.
    ///</summary>
    public class PressableButton : Spatial, IMixedRealityTouchHandler
    {
        const string InitialMarkerTransformName = "Initial Marker";

        bool hasStarted = false;

        /// <summary>
        /// The object that is being pushed.
        /// </summary>
        [Export]
        protected NodePath movingButtonVisualsNodePath = null;
        protected Spatial movingButtonVisuals => GetNode<Spatial>(movingButtonVisualsNodePath);

        /// <summary>
        /// Enum for defining space of plane distances.
        /// </summary>
        public enum SpaceMode
        {
            World,
            Local
        }

        [Export]
        private SpaceMode distanceSpaceMode = SpaceMode.Local;

        /// <summary>
        /// Describes in which coordinate space the plane distances are stored and calculated
        /// </summary>
        public SpaceMode DistanceSpaceMode
        {
            get => distanceSpaceMode;
            set
            {
                // Convert world to local distances and vice versa whenever we switch the mode
                if (value != distanceSpaceMode)
                {
                    distanceSpaceMode = value;
                    float scale = (distanceSpaceMode == SpaceMode.Local) ? WorldToLocalScale : LocalToWorldScale;

                    startPushDistance *= scale;
                    maxPushDistance *= scale;
                    pressDistance *= scale;
                    releaseDistanceDelta *= scale;
                }
            }
        }

        [Export]
        protected float startPushDistance = 0.0f;

        /// <summary>
        /// The offset at which pushing starts. Offset is relative to the pivot of either the moving visuals if there's any or the button itself.
        /// </summary>
        public float StartPushDistance { get => startPushDistance; set => startPushDistance = value; }

        [Export]
        private float maxPushDistance = 0.2f;

        /// <summary>
        /// Maximum push distance. Distance is relative to the pivot of either the moving visuals if there's any or the button itself.
        /// </summary>
        public float MaxPushDistance { get => maxPushDistance; set => maxPushDistance = value; }

        [Export]
        private float pressDistance = 0.02f;

        /// <summary>
        /// Distance the button must be pushed until it is considered pressed. Distance is relative to the pivot of either the moving visuals if there's any or the button itself.
        /// </summary>
        public float PressDistance { get => pressDistance; set => pressDistance = value; }

        [Export]
        private float releaseDistanceDelta = 0.01f;

        /// <summary>
        ///  Withdraw amount needed to transition from Pressed to Released.
        /// </summary>
        public float ReleaseDistanceDelta { get => releaseDistanceDelta; set => releaseDistanceDelta = value; }

        /// <summary>
        ///  Speed for retracting the moving button visuals on release.
        /// </summary>
        [Export]
        private float returnSpeed = 25.0f;

        [Export]
        private bool releaseOnTouchEnd = true;

        /// <summary>
        ///  Button will send the release event on touch end after successful press even if release plane hasn't been passed.
        /// </summary>
        public bool ReleaseOnTouchEnd { get => releaseOnTouchEnd; set => releaseOnTouchEnd = value; }

        [Export]
        private bool enforceFrontPush = true;

        /// <summary>
        /// Ensures that the button can only be pushed from the front. Touching the button from the back or side is prevented.
        /// </summary>
        public bool EnforceFrontPush { get => enforceFrontPush; private set => enforceFrontPush = value; }

        protected ITouchableSurface touchableSurface => Parent.GetChild<ITouchableSurface>();

        private MWAction _touchAction = new MWAction();

        [Signal]
        public delegate void touch_started();

        [Signal]
        public delegate void touch_ended();

        [Signal]
        public delegate void button_pressed();

        [Signal]
        public delegate void button_released();

        #region Private Members

        // The maximum distance before the button is reset to its initial position when retracting.
        private const float MaxRetractDistanceBeforeReset = 0.0001f;

        private Dictionary<Spatial, Vector3> touchPoints = new Dictionary<Spatial, Vector3>();

        private float currentPushDistance = 0.0f;

        /// <summary>
        /// Current push distance relative to the start push plane.
        /// </summary>
        public float CurrentPushDistance { get => currentPushDistance; protected set => currentPushDistance = value; }

        private bool isTouching = false;

        protected Spatial Parent { get; set; }

        ///<summary>
        /// Represents the state of whether or not a finger is currently touching this button.
        ///</summary>
        public bool IsTouching
        {
            get => isTouching;
            private set
            {
                if (value != isTouching)
                {
                    isTouching = value;

                    if (isTouching)
                    {
                        EmitSignal(nameof(touch_started));
                    }
                    else
                    {
                        // Abort press.
                        if (!releaseOnTouchEnd)
                        {
                            IsPressing = false;
                        }
                        EmitSignal(nameof(touch_ended));
                    }
                }
            }
        }

        /// <summary>
        /// Represents the state of whether the button is currently being pressed.
        /// </summary>
        public bool IsPressing { get; private set; }

        /// <summary>
        /// Transform for local to world space in the world direction of a press
        /// Multiply local scale positions by this value to convert to world space
        /// </summary>
        public float LocalToWorldScale => (WorldToLocalScale != 0) ? 1.0f / WorldToLocalScale : 0.0f;

        /// <summary>
        /// The press direction of the button as defined by a touchableSurface.
        /// </summary>
        private Vector3 WorldSpacePressDirection
        {
            get
            {
                if (touchableSurface != null)
                {
                    return Parent.GlobalTransform.basis.Orthonormalized().Xform(touchableSurface.LocalPressDirection);
                }

                return -GlobalTransform.basis.z;
            }
        }

        /// <summary>
        /// The press direction of the button as defined by a touchableSurface, in local space,
        /// using Vector3.forward as an optional fallback when no touchableSurface is defined.
        /// </summary>
        private Vector3 LocalSpacePressDirection
        {
            get
            {
                if (touchableSurface != null)
                {
                    return touchableSurface.LocalPressDirection;
                }

                return Vector3.Forward;
            }
        }

        private Spatial PushSpaceSourceTransform
        {
            get => movingButtonVisuals != null ? movingButtonVisuals : this;
        }

        /// <summary>
        /// Transform for world to local space in the world direction of press
        /// Multiply world scale positions by this value to convert to local space
        /// </summary>
        private float WorldToLocalScale => Transform.basis.XformInv(WorldSpacePressDirection).Length();

        /// <summary>
        /// Initial offset from moving visuals to button
        /// </summary>
        private Vector3 movingVisualsInitialLocalPosition = Vector3.Zero;

        /// <summary>
        /// The position from where the button starts to move.  Projected into world space based on the button's current world space position.
        /// </summary>
        private Vector3 InitialWorldPosition
        {
            get
            {
                if (!Engine.EditorHint && movingButtonVisuals != null) // we're using a cached position in play mode as the moving visuals will be moved during button interaction
                {
                    var parentTransform = (Spatial)PushSpaceSourceTransform.GetParent();
                    var localPosition = (parentTransform == null) ? movingVisualsInitialLocalPosition : parentTransform.GlobalTransform.basis.Xform(movingVisualsInitialLocalPosition);
                    return PushSpaceSourceParentPosition + localPosition;
                }
                else
                {
                    return PushSpaceSourceTransform.GlobalTransform.origin;
                }
            }
        }

        /// <summary>
        /// The position from where the button starts to move.  In local space, relative to button root.
        /// </summary>
        private Vector3 InitialLocalPosition
        {
            get
            {
                if (!Engine.EditorHint && movingButtonVisuals != null) // we're using a cached position in play mode as the moving visuals will be moved during button interaction
                {
                    return movingVisualsInitialLocalPosition;
                }
                else
                {
                    return PushSpaceSourceTransform.GlobalTransform.origin;
                }
            }
        }

        #endregion

        public override void _EnterTree()
        {
            currentPushDistance = startPushDistance;
        }

        private Vector3 PushSpaceSourceParentPosition => (PushSpaceSourceTransform.GetParent() != null) ? PushSpaceSourceTransform.GetParent<Spatial>().GlobalTransform.origin : Vector3.Zero;

        public override void _Ready()
        {
            hasStarted = true;
            Parent = GetParent<Spatial>();
            ((IMixedRealityTouchHandler)this).RegisterTouchEvent(this, Parent);

/*
            if (gameObject.layer == 2)
            {
                Debug.LogWarning("PressableButton will not work if game object layer is set to 'Ignore Raycast'.");
            }
*/
            movingVisualsInitialLocalPosition = movingButtonVisuals.Transform.origin;

            // Ensure everything is set to initial positions correctly.
            UpdateMovingVisualsPosition();

            this.RegisterAction(_touchAction, "touch");
            Connect(nameof(touch_started), this, nameof(_on_PressableButton_touch_started));
            Connect(nameof(touch_ended), this, nameof(_on_PressableButton_touch_ended));
        }

        public override void _ExitTree()
        {
            // clear touch points in case we get disabled and can't receive the touch end event anymore
            touchPoints.Clear();

            if (hasStarted)
            {
                // make sure button doesn't stay in a pressed state in case we disable the button while pressing it
                currentPushDistance = startPushDistance;
                UpdateMovingVisualsPosition();
            }
        }

        public override void _Process(float delta)
        {
            if (IsTouching)
            {
                UpdateTouch();
            }
            else if (currentPushDistance < startPushDistance)
            {
                RetractButton(delta);
            }
        }

        private void UpdateTouch()
        {
            currentPushDistance = GetFarthestDistanceAlongPressDirection();

            UpdateMovingVisualsPosition();

            // Hand press is only allowed to happen while touching.
            UpdatePressedState(currentPushDistance);
        }

        private void RetractButton(float delta)
        {
            float retractDistance = currentPushDistance - startPushDistance;
            retractDistance -= retractDistance * returnSpeed * delta;

            // Apply inverse scale of local z-axis. This constant should always have the same value in world units.
            float localMaxRetractDistanceBeforeReset = MaxRetractDistanceBeforeReset * WorldSpacePressDirection.Length();
            if (retractDistance < localMaxRetractDistanceBeforeReset)
            {
                currentPushDistance = startPushDistance;
            }
            else
            {
                currentPushDistance = startPushDistance + retractDistance;
            }

            UpdateMovingVisualsPosition();

            if (releaseOnTouchEnd && IsPressing)
            {
                UpdatePressedState(currentPushDistance);
            }
        }

        #region IMixedRealityTouchHandler implementation
/*
        private void PulseProximityLight()
        {
            // Pulse each proximity light on pointer cursors' interacting with this button.
            if (currentInputSources.Count != 0)
            {
                foreach (var pointer in currentInputSources[currentInputSources.Count - 1].Pointers)
                {
                    if (!pointer.BaseCursor.TryGetMonoBehaviour(out MonoBehaviour baseCursor))
                    {
                        return;
                    }

                    GameObject cursorGameObject = baseCursor.gameObject;
                    if (cursorGameObject == null)
                    {
                        return;
                    }

                    ProximityLight[] proximityLights = cursorGameObject.GetComponentsInChildren<ProximityLight>();

                    if (proximityLights != null)
                    {
                        foreach (var proximityLight in proximityLights)
                        {
                            proximityLight.Pulse();
                        }
                    }
                }
            }
        }
*/

        public void OnTouchStarted(Spatial inputSource, Node userNode, Vector3 point)
        {
            if (touchPoints.ContainsKey(inputSource))
            {
                return;
            }

            touchPoints.Add(inputSource, point);

            IsTouching = true;
        }

        public void OnTouchUpdated(Spatial inputSource, Node userNode, Vector3 point)
        {
            if (touchPoints.ContainsKey(inputSource))
            {
                touchPoints[inputSource] = point;
            }
        }

        public void OnTouchCompleted(Spatial inputSource, Node userNode, Vector3 point)
        {
            if (touchPoints.ContainsKey(inputSource))
            {
                // When focus is lost, before removing controller, update the respective touch point to give a last chance for checking if pressed occurred
                touchPoints[inputSource] = point;
                UpdateTouch();

                touchPoints.Remove(inputSource);

                IsTouching = (touchPoints.Count > 0);
            }
        }

        #endregion OnTouch

        #region public transform utils

        /// <summary>
        /// Returns world space position along the push direction for the given local distance
        /// </summary>
        ///
        public Vector3 GetWorldPositionAlongPushDirection(float localDistance)
        {
            float distance = (distanceSpaceMode == SpaceMode.Local) ? localDistance * LocalToWorldScale : localDistance;
            return InitialWorldPosition + WorldSpacePressDirection.Normalized() * distance;
        }

        /// <summary>
        /// Returns local position along the push direction for the given local distance
        /// </summary>
        ///
        public Vector3 GetLocalPositionAlongPushDirection(float localDistance)
        {
            return InitialLocalPosition + LocalSpacePressDirection.Normalized() * localDistance;
        }

        /// <summary>
        /// Returns the local distance along the push direction for the passed in world position
        /// </summary>
        public float GetDistanceAlongPushDirection(Vector3 positionWorldSpace)
        {
            Vector3 localPosition = positionWorldSpace - InitialWorldPosition;
            float distance = localPosition.Dot(WorldSpacePressDirection.Normalized());
            return (distanceSpaceMode == SpaceMode.Local) ? distance * WorldToLocalScale : distance;
        }

        #endregion

        #region private Methods

        protected virtual void UpdateMovingVisualsPosition()
        {
            if (movingButtonVisuals != null)
            {
                // Always move relative to startPushDistance
                movingButtonVisuals.Transform = new Transform(movingButtonVisuals.Transform.basis, GetLocalPositionAlongPushDirection(currentPushDistance - startPushDistance));
            }
        }

        // This function projects the current touch positions onto the 1D press direction of the button.
        // It will output the farthest pushed distance from the button's initial position.
        private float GetFarthestDistanceAlongPressDirection()
        {
            float farthestDistance = startPushDistance;

            foreach (var touchEntry in touchPoints)
            {
                float testDistance = GetDistanceAlongPushDirection(touchEntry.Value);
                farthestDistance = Mathf.Min(testDistance, farthestDistance);
            }

            return Mathf.Clamp(farthestDistance, maxPushDistance, startPushDistance);
        }

        private void UpdatePressedState(float pushDistance)
        {
            // If we aren't in a press and can't start a simple one.
            if (!IsPressing)
            {
                // Compare to our previous push depth. Use previous push distance to handle back-presses.
                if (pushDistance <= pressDistance)
                {
                    IsPressing = true;
                    EmitSignal(nameof(button_pressed));
                    //PulseProximityLight();
                }
            }
            // If we're in a press, check if the press is released now.
            else
            {
                float releaseDistance = pressDistance + releaseDistanceDelta;

                if (pushDistance >= releaseDistance)
                {
                    IsPressing = false;
                    EmitSignal(nameof(button_released));
                }
            }
        }

        private void _on_PressableButton_touch_started()
        {
            var user = GetParent<Actor>().App.LocalUser;

            if (user != null)
            {
                _touchAction.StartAction(user);
            }
        }

        private void _on_PressableButton_touch_ended()
        {
            var user = GetParent<Actor>().App.LocalUser;

            if (user != null)
            {
                _touchAction.StopAction(user);
            }
        }

        #endregion
    }
}