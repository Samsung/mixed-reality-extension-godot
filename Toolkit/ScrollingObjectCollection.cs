// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Godot;
using MixedRealityExtension.Util.GodotHelper;
using System.Threading.Tasks;
using System.Threading;
using MixedRealityExtension.Core;

using MixedRealityExtension.Patching;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Core.Components;
using MixedRealityExtension.Behaviors.Actions;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// A scrollable frame where content scroll is triggered by manual controller click and drag or according to pagination settings.
    //// </summary>
    ///<remarks>Executing also in edit mode to properly catch and mask any new content added to scroll container.</remarks>
    public class ScrollingObjectCollection : Spatial, IToolkit,
            IMixedRealityPointerHandler,
            IMixedRealityTouchHandler
    {
        /// <summary>
        /// How velocity is applied to a <see cref="ScrollingObjectCollection"/> when a scroll is released.
        /// </summary>
        public enum VelocityType
        {
            FalloffPerFrame = 0,
            FalloffPerItem,
            NoVelocitySnapToItem,
            None
        }

        /// <summary>
        /// The direction in which a <see cref="ScrollingObjectCollection"/> can scroll.
        /// </summary>
        public enum ScrollDirectionType
        {
            UpAndDown = 0,
            LeftAndRight,
        }

        /// <summary>
        /// Enables/disables scrolling with near/far interaction.
        /// </summary>
        /// <remarks>Helpful for controls where you may want pagination or list movement without freeform scrolling.</remarks>
        public bool CanScroll { get; set; } = true;

        /// <summary>
        /// Edit modes for defining scroll viewable area and scroll interaction boundaries.
        /// </summary>
        public enum EditMode
        {
            Auto = 0, // Use pagination values
            Manual, // Use direct manipulation of the object
        }

        /// <summary>
        /// Edit modes for defining the clipping box masking boundaries. Choose 'Auto' to automatically use pagination values. Choose 'Manual' for enabling direct manipulation of the clipping box object.
        /// </summary>
        public EditMode MaskEditMode { get; set; }

        /// <summary>
        /// Edit modes for defining the scroll interaction collider boundaries. Choose 'Auto' to automatically use pagination values. Choose 'Manual' for enabling direct manipulation of the collider.
        /// </summary>
        public EditMode ColliderEditMode { get; set; }

        private bool maskEnabled = true;

        /// <summary>
        /// Visibility mode of scroll content. Default value will mask all objects outside of the scroll viewable area.
        /// </summary>
        public bool MaskEnabled
        {
            get { return maskEnabled; }
            set
            {
                if (maskEnabled == value) return;
                if (!value)
                {
                    RestoreContentVisibility();
                }
                maskEnabled = value;
            }
        }

        /// <summary>
        /// The distance, in meters, the current pointer can travel along the scroll direction before triggering a scroll drag.
        /// </summary>
        public float HandDeltaScrollThreshold { get; set; } = 0.002f;

        /// <summary>
        /// Withdraw amount, in meters, from the front of the scroll boundary needed to transition from touch engaged to released.
        /// </summary>
        public float ReleaseThresholdFront { get; set; } = 0.03f;

        /// <summary>
        /// Withdraw amount, in meters, from the back of the scroll boundary needed to transition from touch engaged to released.
        /// </summary>
        public float ReleaseThresholdBack { get; set; } = 0.20f;

        /// <summary>
        /// Withdraw amount, in meters, from the right or left of the scroll boundary needed to transition from touch engaged to released.
        /// </summary>
        public float ReleaseThresholdLeftRight { get; set; } = 0.20f;

        /// <summary>
        /// Withdraw amount, in meters, from the top or bottom of the scroll boundary needed to transition from touch engaged to released.
        /// </summary>
        public float ReleaseThresholdTopBottom { get; set; } = 0.20f;

        /// <summary>
        /// Distance, in meters, to position a local xy plane used to verify if a touch interaction started in the front of the scroll view.
        /// </summary>
        public float FrontTouchDistance { get; set; } = 0.005f;

        /// <summary>
        /// The direction in which content should scroll.
        /// </summary>
        public ScrollDirectionType ScrollDirection { get; set; }

        /// <summary>
        /// Amount of (extra) velocity to be applied to scroller.
        /// </summary>
        /// <remarks>Helpful if you want a small movement to fling the list.</remarks>
        public float VelocityMultiplier { get; set; } = 0.008f;

        /// <summary>
        /// Amount of drag applied to velocity.
        /// </summary>
        /// <remarks>This can't be 0.0f since that won't allow ANY velocity - set <see cref="TypeOfVelocity"/> to <see cref="VelocityType.None"/>. It can't be 1.0f since that won't allow ANY drag.</remarks>
        public float VelocityDampen { get; set; } = 0.90f;

        /// <summary>
        /// The desired type of velocity for the scroller.
        /// </summary>
        public VelocityType TypeOfVelocity { get; set; }

        private float animationLength = 0.25f;

        /// <summary>
        /// The amount of time (in seconds) the <see cref="PaginationCurve"/> will take to evaluate.
        /// </summary>
        public float AnimationLength
        {
            get { return (animationLength < 0) ? 0 : animationLength; }
            set { animationLength = value; }
        }

        private int cellsPerTier = 4;

        /// <summary>
        /// Number of cells in a row on up-down scroll or number of cells in a column on left-right scroll.
        /// </summary>
        public int CellsPerTier
        {
            get
            {
                return cellsPerTier;
            }
            set
            {
                Debug.Assert(value > 0, "Cells per tier should have a positive non zero value");
                cellsPerTier = Mathf.Max(1, value);
            }
        }

        private int tiersPerPage = 3;

        /// <summary>
        /// Number of visible tiers in the scrolling area.
        /// </summary>
        public int TiersPerPage
        {
            get
            {
                return tiersPerPage;
            }
            set
            {
                Debug.Assert(value > 0, "Tiers per page should have a positive non zero value");
                tiersPerPage = Mathf.Max(1, value);
            }
        }

        private float cellWidth = 0.036f;

        /// <summary>
        /// Width of the pagination cell.
        /// </summary>
        public float CellWidth
        {
            get
            {
                return cellWidth;
            }
            set
            {
                Debug.Assert(value > 0, "Cell width should have a positive non zero value");
                cellWidth = Mathf.Max(0.001f, value);
            }
        }

        private float cellHeight = 0.036f;

        /// <summary>
        /// Height of the pagination cell.Hhide
        /// </summary>
        public float CellHeight
        {
            get
            {
                return cellHeight;
            }
            set
            {
                Debug.Assert(cellHeight > 0, "Cell height should have a positive non zero value");
                cellHeight = Mathf.Max(0.001f, value);
            }
        }

        private float cellDepth = 0.024f;

        /// <summary>
        /// Depth of cell used for masking out content renderers that are out of bounds.
        /// </summary>
        public float CellDepth
        {
            get
            {
                return cellDepth;
            }
            set
            {
                Debug.Assert(value > 0, "Cell depth should have a positive non zero value");
                cellDepth = Mathf.Max(0.001f, value);
            }
        }

        /// <summary>
        /// Multiplier to add more bounce to the overscroll of a list when using <see cref="VelocityType.FalloffPerFrame"/> or <see cref="VelocityType.FalloffPerItem"/>.
        /// </summary>
        public float BounceMultiplier { get; set; } = 0.1f;

        // Lerping time interval used for smoothing between positions during scroll drag. Number was empirically defined.
        private const float DragLerpInterval = 0.5f;

        // Lerping time interval used for smoothing between positions during scroll drag passed max and min scroll positions. Number was empirically defined.
        private const float OverDampLerpInterval = 0.9f;

        // Lerping time interval used for smoothing between positions during bouncing. Number was empirically defined.
        private const float BounceLerpInterval = 0.2f;


        private MWAction _touchAction = new MWAction();
        private MWAction _scrollAction = new MWAction();

        /// <summary>
        /// Event that is fired on the target object when the ScrollingObjectCollection deems event as a Click.
        /// </summary>
        [Signal]
        public delegate void clicked();

        /// <summary>
        /// Event that is fired on the target object when the ScrollingObjectCollection is touched.
        /// </summary>
        [Signal]
        public delegate void touch_started();

        /// <summary>
        /// Event that is fired on the target object when the ScrollingObjectCollection is no longer touched.
        /// </summary>
        [Signal]
        public delegate void touch_ended();

        /// <summary>
        /// Event that is fired on the target object when the ScrollingObjectCollection is starting motion with velocity.
        /// </summary>
        [Signal]
        public delegate void scroll_started();

        /// <summary>
        /// Event that is fired on the target object when the ScrollingObjectCollection is no longer in motion from velocity
        /// </summary>
        [Signal]
        public delegate void scroll_ended();

        /// <summary>
        /// Event that is fired on the target object when the ScrollingObjectCollection is scrolling.
        /// </summary>
        [Signal]
        public delegate void scrolling();

        [Signal]
        private delegate void _Processed();

        // Maximum amount the scroller can travel (vertically)
        private float MaxY
        {
            get
            {
                var max = (contentAABB == null || contentAABB.Size.y <= 0) ? 0 :
                        Mathf.Max(0, contentAABB.Size.y - TiersPerPage * CellHeight);

                if (MaskEditMode == EditMode.Auto)
                {
                    // Making it a multiple of cell height
                    max = Mathf.Round(SafeDivisionFloat(max, CellHeight)) * CellHeight;
                }

                return max;
            }
        }

        // Minimum amount the scroller can travel (vertically) - this will always be zero. Here for readability
        private readonly float minY = 0.0f;

        // Maximum amount the scroller can travel (horizontally) - this will always be zero. Here for readability
        private readonly float maxX = 0.0f;

        // Minimum amount the scroller can travel (horizontally)
        private float MinX
        {
            get
            {
                var max = (contentAABB == null || contentAABB.Size.x <= 0) ? 0 :
                     Mathf.Max(0, contentAABB.Size.x - TiersPerPage * CellWidth);

                if (MaskEditMode == EditMode.Auto)
                {
                    // Making it a multiple of cell width
                    max = Mathf.Round(SafeDivisionFloat(max, CellWidth)) * CellWidth;
                }

                return max * -1.0f;
            }
        }

        // Size that wrap all scroll container content. Used for calculating MinX and MaxY.
        private AABB contentAABB;

        /// <summary>
        /// Index of the first visible cell.
        /// </summary>
        public int FirstVisibleCellIndex
        {
            get
            {
                if (ScrollDirection == ScrollDirectionType.UpAndDown)
                {
                    return (int)Mathf.Ceil(ScrollContainer.Transform.origin.y / CellHeight) * CellsPerTier;
                }
                else
                {
                    // Scroll container most to the right local position has x component equals to zero. This value goes negative as scroll container moves to the left.
                    return ((int)Mathf.Ceil(Mathf.Abs(ScrollContainer.Transform.origin.x / CellWidth)) * CellsPerTier);
                }
            }
        }

        /// <summary>
        /// Index of the first hidden cell.
        /// </summary>
        public int FirstHiddenCellIndex
        {
            get
            {
                if (ScrollDirection == ScrollDirectionType.UpAndDown)
                {
                    return ((int)Mathf.Floor(ScrollContainer.Transform.origin.y / CellHeight) * CellsPerTier) + (TiersPerPage * CellsPerTier);
                }
                else
                {
                    return ((int)Mathf.Floor(-ScrollContainer.Transform.origin.x / CellWidth) * CellsPerTier) + (TiersPerPage * CellsPerTier);
                }
            }
        }

        /// <summary>
        /// Scrolling interaction collider used to catch pointer and touch events on empty spaces.
        /// </summary>
        public BoxShape ScrollingCollisionBoxShape { get; private set; }

        // Depth of the scrolling interaction collider. Used for defining a plane depth if 'Auto' collider edit mode is selected.
        private const float ScrollingColliderDepth = 0.001f;

        /// <summary>
        /// Scrolling interaction touchable used to catch touch events on empty spaces.
        /// </summary>
        internal TouchablePlane ScrollingTouchable { get; private set; }

        // The empty Spatial that contains our nodes and be scrolled
        private Spatial scrollContainer;

        private Spatial ScrollContainer
        {
            get
            {
                if (scrollContainer == null)
                {
                    Spatial oldContainer = FindNode("Container", false) as Spatial;

                    if (oldContainer != null)
                    {
                        scrollContainer = oldContainer;
                        GD.PushWarning(Name + " ScrollingObjectCollection found an existing Container object, using it for the list");
                    }
                    else
                    {
                        scrollContainer = new Spatial() { Name = "Container" };
                        AddChild(scrollContainer);
                    }

                    scrollContainer.Transform = Transform.Identity;
                }

                return scrollContainer;
            }
        }

        private ClippingComponent clippingComponent;

        /// <summary>
        /// The ScrollingObjectCollection's ClippingBox.
        /// that is used for clipping items in and out of the list.
        /// </summary>
        internal ClippingComponent ClippingComponent
        {
            get
            {
                if (clippingComponent == null)
                {
                    ClippingComponent oldClippingComponent = parentActor.GetChild<ClippingComponent>();

                    if (oldClippingComponent != null)
                    {
                        clippingComponent = oldClippingComponent;
                    }
                    clippingComponent.Transform = Transform.Identity;
                }

                return clippingComponent;
            }
        }

        // Ratio that defines the outer clipping bounds size relative to the actual clipping bounds.
        // The outer clipping bounds is used for ensuring that content collider that are mostly visible can still stay interactable.
        private readonly float contentVisibilityThresholdRatio = 1.025f;

        private bool oldIsTargetPositionLockedOnFocusLock;

        private readonly HashSet<MeshInstance> clippedMeshInstances = new HashSet<MeshInstance>();
        private readonly HashSet<Actor> clippedActors = new HashSet<Actor>();

        #region scroll state variables

        /// <summary>
        /// Tracks whether content or scroll background is being interacted with.
        /// </summary>
        public bool IsEngaged { get; private set; } = false;

        /// <summary>
        /// Tracks whether the scroll is being dragged due to a controller movement.
        /// </summary>
        public bool IsDragging { get; private set; } = false;

        /// <summary>
        /// Tracks whether the scroll content or background is touched by a near pointer.
        /// Remains true while the same near pointer does not cross the scrolling release boundaries.
        /// </summary>
        public bool IsTouched { get; private set; } = false;

        // The position of the scollContainer before we do any updating to it
        private Vector3 initialScrollerPos;

        // The new of the scollContainer before we've set the position / finished the updateloop
        private Vector3 workingScrollerPos;

        // A list of content renderers that need to be added to the clippingBox
        private List<MeshInstance> renderersToClip = new List<MeshInstance>();

        // A list of content renderers that need to be removed from the clippingBox
        private List<MeshInstance> renderersToUnclip = new List<MeshInstance>();

        private Spatial currentInputSource;

        private Actor parentActor;

        #endregion scroll state variables

        #region drag position calculation variables

        // Hand position when starting a motion
        private Vector3 initialPointerPos;

        // Hand position previous frame
        private Vector3 lastPointerPos;

        #endregion drag position calculation variables

        #region velocity calculation variables

        // Simple velocity of the scroller: current - last / timeDelta
        private float scrollVelocity = 0.0f;

        // Filtered weight of scroll velocity
        private float avgVelocity = 0.0f;

        // How much we should filter the velocity - yes this is a magic number. Its been tuned so lets leave it.
        private readonly float velocityFilterWeight = 0.97f;

        // Simple state enum to handle velocity falloff logic
        private enum VelocityState
        {
            None = 0,
            Resolving,
            Calculating,
            Bouncing,
            Dragging,
            Animating,
        }

        // Internal enum for tracking the velocity state of the list
        private VelocityState currentVelocityState;

        private VelocityState CurrentVelocityState
        {
            get => currentVelocityState;

            set
            {
                if (value != currentVelocityState)
                {
                    if (value == VelocityState.None)
                    {
                        EmitSignal(nameof(scroll_ended));
                    }
                    else if (currentVelocityState == VelocityState.None)
                    {
                        EmitSignal(nameof(scroll_started));
                    }
                    previousVelocityState = currentVelocityState;
                    currentVelocityState = value;
                }
            }
        }

        private VelocityState previousVelocityState;

        // Pre calculated destination with velocity and falloff when using per item snapping
        private Vector3 velocityDestinationPos;

        // Velocity container for storing previous filtered velocity
        private float velocitySnapshot;

        #endregion velocity calculation variables

        // The Animation Task
        private Task animateScroller;
        private CancellationTokenSource animateScrollerToken = new CancellationTokenSource();

        /// <summary>
        /// Scroll pagination modes.
        /// </summary>
        public enum PaginationMode
        {
            ByTier = 0, // By number of tiers
            ByPage, // By number of pages
            ToCellIndex // To selected cell
        }

        #region performance variables

        /// <summary>
        /// Disables Actors with MeshInstance which are clipped by the clipping box.
        /// Improves performance significantly by reducing the number of Actors that need to be managed in engine.
        /// </summary>
        [Export]
        public bool DisableClippedActors { get; set; } = true;

        /// <summary>
        /// Disables the Meshintances which are clipped by the clipping box.
        /// Improves performance by reducing the number of mesh instances that need to be tracked, while still allowing the
        /// Node associated with those mesh instances to continue processing. Less performant compared to using DisableClippedGameObjects
        /// </summary>
        [Export]
        public bool DisableClippedRenderers { get; set; } = false;

        #endregion performance variables

        #region Setup methods

        /// <summary>
        /// Sets up the scroll clipping object and the interactable components according to the scroll content and chosen settings.
        /// </summary>
        public void UpdateContent()
        {
            UpdateContentBounds();
            SetupScrollingInteractionCollider();
            SetupClippingObject();
            ManageVisibility();
        }

        private void UpdateContentBounds()
        {
            var childrenMeshInstances = ScrollContainer.GetChildrenAll<MeshInstance>();
            if (childrenMeshInstances != null)
            {
                contentAABB = new AABB();

                foreach (var meshInstance in childrenMeshInstances)
                {
                    contentAABB = contentAABB.Merge(meshInstance.GetTransformedAabb());
                }
            }
        }

        // Setting up the initial transform values for the scrolling interaction collider and near touchable.
        private void SetupScrollingInteractionCollider()
        {
            // Boundaries will be defined by direct manipulation of the scroll interaction components
            if (ColliderEditMode == EditMode.Manual)
            {
                return;
            }

            if (ScrollDirection == ScrollDirectionType.UpAndDown)
            {
                ScrollingCollisionBoxShape.Extents = new Vector3(CellWidth * CellsPerTier * 0.5f, CellHeight * TiersPerPage * 0.5f, ScrollingColliderDepth);
            }
            else
            {
                ScrollingCollisionBoxShape.Extents = new Vector3(CellWidth * TiersPerPage * 0.5f, CellHeight * CellsPerTier * 0.5f, ScrollingColliderDepth);
            }

            Vector3 colliderPosition;
            colliderPosition.x = ScrollingCollisionBoxShape.Extents.x / 2;
            colliderPosition.y = -ScrollingCollisionBoxShape.Extents.y / 2;
            colliderPosition.z = cellDepth / 2 + ScrollingColliderDepth;

            Vector3 touchablePosition = colliderPosition;
            touchablePosition.z = -cellDepth / 2;

            ScrollingTouchable.SetLocalCenter(touchablePosition);
        }

        /// <summary>
        /// Setting up the initial transform values for the clippingBox.
        /// </summary>
        private void SetupClippingObject()
        {
            // Boundaries will be defined by direct manipulation of the clipping object
            if (MaskEditMode == EditMode.Manual)
            {
                return;
            }
            // Adjust scale and position of clipping box
            switch (ScrollDirection)
            {
                case ScrollDirectionType.UpAndDown:
                default:

                    // Apply the viewable area and column/row multiplier
                    // Use a dummy bounds of one to get the local scale to match;
                    ClippingComponent.Scale = new Vector3(CellWidth * CellsPerTier, CellHeight * TiersPerPage, CellDepth);

                    break;

                case ScrollDirectionType.LeftAndRight:

                    // Same as above for L <-> R
                    ClippingComponent.Scale = new Vector3(CellWidth * TiersPerPage, CellHeight * CellsPerTier, CellDepth);

                    break;
            }
            // Apply new values
            ClippingComponent.Transform = new Transform(ClippingComponent.Transform.basis, Vector3.Zero);
        }

        #endregion Setup methods

        #region Node Implementation

        public override void _Ready()
        {
            parentActor = GetParent<Actor>();
            ((IMixedRealityTouchHandler)this).RegisterTouchEvent(this, parentActor);
            ((IMixedRealityPointerHandler)this).RegisterPointerEvent(this, parentActor);
            ScrollingCollisionBoxShape = GetNode<CollisionShape>("CollisionShape").Shape as BoxShape;

            this.RegisterAction(_touchAction, "touch");
            this.RegisterAction(_scrollAction, "scroll");
            Connect(nameof(touch_started), this, nameof(_on_ScrollingObjectCollection_touch_started));
            Connect(nameof(touch_ended), this, nameof(_on_ScrollingObjectCollection_touch_ended));
            Connect(nameof(scroll_started), this, nameof(_on_ScrollingObjectCollection_scroll_started));
            Connect(nameof(scroll_ended), this, nameof(_on_ScrollingObjectCollection_scroll_ended));
            Connect(nameof(scrolling), this, nameof(_on_ScrollingObjectCollection_scrolling));
        }

        public override void _Process(float delta)
        {
            // Force the scroll container position if no content
            if (!ScrollContainer.GetChildrenAll<MeshInstance>().Any())
            {
                workingScrollerPos = Vector3.Zero;
                ApplyPosition(workingScrollerPos);

                return;
            }

            // The scroller has detected input and has a valid pointer
            if (IsEngaged && TryGetPointerPositionOnPlane(out Vector3 currentPointerPos))
            {
                Vector3 handDelta = initialPointerPos - currentPointerPos;
                handDelta = GlobalTransform.basis.XformInv(handDelta);

/*
                if (IsDragging && currentPointer != null) // Changing lock after drag started frame to allow for focus provider to move pointer focus to scroll background before locking
                {
                    currentPointer.IsFocusLocked = true;
                }
*/
                // Lets see if this is gonna be a click or a drag
                // Check the scroller's length state to prevent resetting calculation
                if (!IsDragging)
                {
                    // Grab the delta value we care about
                    float absAxisHandDelta = (ScrollDirection == ScrollDirectionType.UpAndDown) ? Mathf.Abs(handDelta.y) : Mathf.Abs(handDelta.x);

                    // Catch an intentional finger in scroller to stop momentum, this isn't a drag its definitely a stop
                    if (absAxisHandDelta > HandDeltaScrollThreshold)
                    {
                        scrollVelocity = 0.0f;
                        avgVelocity = 0.0f;

                        IsDragging = true;
                        handDelta = Vector3.Zero;

                        CurrentVelocityState = VelocityState.Dragging;

                        // Reset initialHandPos to prevent the scroller from jumping
                        initialScrollerPos = workingScrollerPos = ScrollContainer.Transform.origin;
                        initialPointerPos = currentPointerPos;
                    }
                }

                if (IsTouched && DetectScrollRelease(currentPointerPos))
                {
                    // We're on the other side of the original touch position. This is a release.
                    if (IsDragging)
                    {
                        // Its a drag release
                        initialScrollerPos = workingScrollerPos;
                        CurrentVelocityState = VelocityState.Calculating;
                    }
                    else
                    {
                        // Its a click release
                        EmitSignal(nameof(clicked));
                    }

                    ResetInteraction();
                }
                else if (IsDragging && CanScroll)
                {
                    if (ScrollDirection == ScrollDirectionType.UpAndDown)
                    {
                        // Lock X, clamp Y
                        float handLocalDelta = SafeDivisionFloat(handDelta.y, Transform.basis.Scale.y);

                        // Over damp if scroll position out of bounds
                        if (workingScrollerPos.y > MaxY || workingScrollerPos.y < minY)
                        {
                            workingScrollerPos.y = CLampLerp(initialScrollerPos.y - handLocalDelta, minY, MaxY, OverDampLerpInterval);
                        }
                        else
                        {
                            workingScrollerPos.y = CLampLerp(initialScrollerPos.y - handLocalDelta, minY, MaxY, DragLerpInterval);
                        }
                        workingScrollerPos.x = 0.0f;
                    }
                    else
                    {
                        // Lock Y, clamp X
                        float handLocalDelta = SafeDivisionFloat(handDelta.x, Transform.basis.Scale.x);

                        // Over damp if scroll position out of bounds
                        if (workingScrollerPos.x > maxX || workingScrollerPos.x < MinX)
                        {
                            workingScrollerPos.x = CLampLerp(initialScrollerPos.x - handLocalDelta, MinX, maxX, OverDampLerpInterval);
                        }
                        else
                        {
                            workingScrollerPos.x = CLampLerp(initialScrollerPos.x - handLocalDelta, MinX, maxX, DragLerpInterval);
                        }
                        workingScrollerPos.y = 0.0f;
                    }

                    // Update the scrollContainer Position
                    ApplyPosition(workingScrollerPos);

                    CalculateVelocity();

                    // Update the prev val for velocity
                    lastPointerPos = currentPointerPos;
                }
            }
            else if ((CurrentVelocityState != VelocityState.None
                      || previousVelocityState != VelocityState.None)
                      && CurrentVelocityState != VelocityState.Animating) // Prevent the Animation coroutine from being overridden
            {
                // We're not engaged, so handle any not touching behavior
                HandleVelocityFalloff();

                // Apply our position
                ApplyPosition(workingScrollerPos);
            }

            previousVelocityState = CurrentVelocityState;
            EmitSignal(nameof(_Processed));
        }

        public override void _PhysicsProcess(float delta)
        {
            ManageVisibility();
        }

        private void OnDisable()
        {
            // Currently in editor duplicating prefab GameObject containing both TMP and non-TMP children inside the Scrolling Object Collection container causes material life cycle management issues
            // https://github.com/microsoft/MixedRealityToolkit-Unity/issues/9481
            // Thus we do not automatically destroy material controlled by Material Instance if the OnDisable comes from pasting in editor
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                bool? isCalledFromPastingGameObject = new System.Diagnostics.StackFrame(1)?.GetMethod()?.Name?.Contains("Paste");
                RestoreContentVisibility(!isCalledFromPastingGameObject.GetValueOrDefault());
            }
            else
            {
                RestoreContentVisibility();
            }
#else
            RestoreContentVisibility();
#endif
        }

        #endregion Node Implementation

        #region private methods

        /// <summary>
        /// Clamps via a lerp for a "soft" clamp effect
        /// </summary>
        /// <param name="pos">number to clamp</param>
        /// <param name="min">if pos is less than min, then lerp clamps to this value</param>
        /// <param name="max">if pos is more than max, lerp clamps to this value</param>
        /// <param name="clampFactor"> Range from 0.0f to 1.0f of how close to snap to min and max </param>
        /// <returns>A soft clamped value</returns>
        private float CLampLerp(float pos, float min, float max, float clampFactor)
        {
            clampFactor = Mathf.Clamp(clampFactor, 0.0f, 1.0f);
            if (pos < min)
            {
                return Mathf.Lerp(pos, min, clampFactor);
            }
            else if (pos > max)
            {
                return Mathf.Lerp(pos, max, clampFactor);
            }

            return pos;
        }

        private Vector3 SmoothTo(Vector3 source, Vector3 goal, float deltaTime, float lerpTime)
        {
            return source.LinearInterpolate(goal, lerpTime.Equals(0.0f) ? 1f : deltaTime / lerpTime);
        }

        // Add or remove renderers from clipping primitive
        private void ReconcileClippingContent()
        {
            if (renderersToClip.Count > 0)
            {
                AddRenderersToClippingObject(renderersToClip);

                renderersToClip.Clear();
            }

            if (renderersToUnclip.Count > 0)
            {
                RemoveRenderersFromClippingObject(renderersToUnclip);

                renderersToUnclip.Clear();
            }

            foreach (var actor in clippedActors)
            {
                if (actor.PauseMode == PauseModeEnum.Stop)
                {
                    actor.PauseMode = PauseModeEnum.Inherit;
                    actor.Visible = true;
                }
            }
        }

        /// <summary>
        /// Gets the cursor position (pointer end point) on the scrollable plane,
        /// projected onto the direction being scrolled if far pointer.
        /// Returns false if the pointer is null.
        /// </summary>
        private bool TryGetPointerPositionOnPlane(out Vector3 result)
        {
            result = Vector3.Zero;

            if (currentInputSource == null)
            {
                return false;
            }

            if (IsTouched)
            {
                result = currentInputSource.GlobalTransform.origin;
                return true;
            }

            var scrollVector = (ScrollDirection == ScrollDirectionType.UpAndDown) ? GlobalTransform.basis.y : GlobalTransform.basis.x;

            result = GlobalTransform.origin + (currentInputSource.GlobalTransform.origin - GlobalTransform.origin).Project(scrollVector);
            return true;
        }

        /// <summary>
        /// Calculates our <see cref="VelocityType"/> falloff
        /// </summary>
        private void HandleVelocityFalloff()
        {
            switch (TypeOfVelocity)
            {
                case VelocityType.FalloffPerFrame:

                    HandleFalloffPerFrame();
                    break;

                case VelocityType.FalloffPerItem:
                default:

                    HandleFalloffPerItem();
                    break;

                case VelocityType.NoVelocitySnapToItem:

                    CurrentVelocityState = VelocityState.None;

                    avgVelocity = 0.0f;

                    // Round to the nearest cell
                    if (ScrollDirection == ScrollDirectionType.UpAndDown)
                    {
                        workingScrollerPos.y = Mathf.Round(ScrollContainer.Transform.origin.y / CellHeight) * CellHeight;
                    }
                    else
                    {
                        workingScrollerPos.x = Mathf.Round(ScrollContainer.Transform.origin.x / CellWidth) * CellWidth;
                    }

                    initialScrollerPos = workingScrollerPos;
                    break;

                case VelocityType.None:

                    CurrentVelocityState = VelocityState.None;

                    avgVelocity = 0.0f;
                    break;
            }

            if (CurrentVelocityState == VelocityState.None)
            {
                workingScrollerPos.y = Mathf.Clamp(workingScrollerPos.y, minY, MaxY);
                workingScrollerPos.x = Mathf.Clamp(workingScrollerPos.x, MinX, maxX);
            }
        }

        /// <summary>
        /// Handles <see cref="ScrollingObjectCollection"/> drag release behavior when <see cref="TypeOfVelocity"/> is set to <see cref="VelocityType.FalloffPerItem"/>
        /// </summary>
        private void HandleFalloffPerItem()
        {
            switch (CurrentVelocityState)
            {
                case VelocityState.Calculating:

                    int numSteps;
                    float newPosAfterVelocity;
                    if (ScrollDirection == ScrollDirectionType.UpAndDown)
                    {
                        if (avgVelocity == 0.0f)
                        {
                            // Velocity was cleared out so we should just snap
                            newPosAfterVelocity = ScrollContainer.Transform.origin.y;
                        }
                        else
                        {
                            // Precalculate where the velocity falloff would land our scrollContainer, then round it to the nearest cell so it feels natural
                            velocitySnapshot = IterateFalloff(avgVelocity, out numSteps);
                            newPosAfterVelocity = initialScrollerPos.y - velocitySnapshot;
                        }

                        velocityDestinationPos.y = (Mathf.Round(newPosAfterVelocity / CellHeight)) * CellHeight;

                        CurrentVelocityState = VelocityState.Resolving;
                    }
                    else
                    {
                        if (avgVelocity == 0.0f)
                        {
                            // Velocity was cleared out so we should just snap
                            newPosAfterVelocity = ScrollContainer.Transform.origin.x;
                        }
                        else
                        {
                            // Precalculate where the velocity falloff would land our scrollContainer, then round it to the nearest cell so it feels natural
                            velocitySnapshot = IterateFalloff(avgVelocity, out numSteps);
                            newPosAfterVelocity = initialScrollerPos.x + velocitySnapshot;
                        }

                        velocityDestinationPos.x = (Mathf.Round(newPosAfterVelocity / CellWidth)) * CellWidth;

                        CurrentVelocityState = VelocityState.Resolving;
                    }

                    workingScrollerPos = SmoothTo(scrollContainer.Transform.origin, velocityDestinationPos, GetProcessDeltaTime(), BounceLerpInterval);

                    // Clear the velocity now that we've applied a new position
                    avgVelocity = 0.0f;
                    break;

                case VelocityState.Resolving:

                    if (ScrollDirection == ScrollDirectionType.UpAndDown)
                    {
                        if (ScrollContainer.Transform.origin.y > MaxY
                            || ScrollContainer.Transform.origin.y < minY)
                        {
                            CurrentVelocityState = VelocityState.Bouncing;
                            velocitySnapshot = 0.0f;
                            break;
                        }
                        else
                        {
                            workingScrollerPos = SmoothTo(ScrollContainer.Transform.origin, velocityDestinationPos, GetProcessDeltaTime(), BounceLerpInterval);

                            SnapVelocityFinish();
                        }
                    }
                    else
                    {
                        if (ScrollContainer.Transform.origin.x > maxX + (FrontTouchDistance * BounceMultiplier)
                            || ScrollContainer.Transform.origin.x < MinX - (FrontTouchDistance * BounceMultiplier))
                        {
                            CurrentVelocityState = VelocityState.Bouncing;
                            velocitySnapshot = 0.0f;
                            break;
                        }
                        else
                        {
                            workingScrollerPos = SmoothTo(ScrollContainer.Transform.origin, velocityDestinationPos, GetProcessDeltaTime(), BounceLerpInterval);

                            SnapVelocityFinish();
                        }
                    }
                    break;

                case VelocityState.Bouncing:

                    HandleBounceState();
                    break;

                case VelocityState.None:
                default:
                    // clean up our position for next frame
                    initialScrollerPos = workingScrollerPos;
                    break;

            }
        }

        /// <summary>
        /// Handles <see cref="ScrollingObjectCollection"/> drag release behavior when <see cref="TypeOfVelocity"/> is set to <see cref="VelocityType.FalloffPerFrame"/>
        /// </summary>
        private void HandleFalloffPerFrame()
        {
            switch (CurrentVelocityState)
            {
                case VelocityState.Calculating:

                    if (ScrollDirection == ScrollDirectionType.UpAndDown)
                    {
                        workingScrollerPos.y = initialScrollerPos.y + avgVelocity;
                    }
                    else
                    {
                        workingScrollerPos.x = initialScrollerPos.x + avgVelocity;
                    }

                    CurrentVelocityState = VelocityState.Resolving;

                    // clean up our position for next frame
                    initialScrollerPos = workingScrollerPos;
                    break;

                case VelocityState.Resolving:

                    if (ScrollDirection == ScrollDirectionType.UpAndDown)
                    {
                        if (ScrollContainer.Transform.origin.y > MaxY + (FrontTouchDistance * BounceMultiplier)
                            || ScrollContainer.Transform.origin.y < minY - (FrontTouchDistance * BounceMultiplier))
                        {
                            CurrentVelocityState = VelocityState.Bouncing;
                            avgVelocity = 0.0f;
                            break;
                        }
                        else
                        {
                            avgVelocity *= VelocityDampen;
                            workingScrollerPos.y = initialScrollerPos.y + avgVelocity;

                            SnapVelocityFinish();

                        }
                    }
                    else
                    {
                        if (ScrollContainer.Transform.origin.x > maxX + (FrontTouchDistance * BounceMultiplier)
                            || ScrollContainer.Transform.origin.x < MinX - (FrontTouchDistance * BounceMultiplier))
                        {
                            CurrentVelocityState = VelocityState.Bouncing;
                            avgVelocity = 0.0f;
                            break;
                        }
                        else
                        {
                            avgVelocity *= VelocityDampen;
                            workingScrollerPos.x = initialScrollerPos.x + avgVelocity;

                            SnapVelocityFinish();
                        }
                    }

                    // clean up our position for next frame
                    initialScrollerPos = workingScrollerPos;

                    break;

                case VelocityState.Bouncing:

                    HandleBounceState();

                    break;
            }
        }

        /// <summary>
        /// Smooths <see cref="ScrollContainer"/>'s position to the proper clamped edge
        /// while <see cref="CurrentVelocityState"/> is <see cref="VelocityState.Bouncing"/>.
        /// </summary>
        private void HandleBounceState()
        {
            Vector3 clampedDest = new Vector3(Mathf.Clamp(ScrollContainer.Transform.origin.x, MinX, maxX), Mathf.Clamp(ScrollContainer.Transform.origin.y, minY, MaxY), 0.0f);
            if ((ScrollDirection == ScrollDirectionType.UpAndDown && Mathf.IsEqualApprox(ScrollContainer.Transform.origin.y, clampedDest.y))
                || (ScrollDirection == ScrollDirectionType.LeftAndRight && Mathf.IsEqualApprox(ScrollContainer.Transform.origin.x, clampedDest.x)))
            {
                CurrentVelocityState = VelocityState.None;

                // clean up our position for next frame
                initialScrollerPos = workingScrollerPos = clampedDest;
                return;
            }
            workingScrollerPos.y = SmoothTo(ScrollContainer.Transform.origin, clampedDest, GetProcessDeltaTime(), BounceLerpInterval).y;
            workingScrollerPos.x = SmoothTo(ScrollContainer.Transform.origin, clampedDest, GetProcessDeltaTime(), BounceLerpInterval).x;
        }

        /// <summary>
        /// Snaps to the final position of the <see cref="ScrollContainer"/> once velocity as resolved.
        /// </summary>
        private void SnapVelocityFinish()
        {
            if (ScrollContainer.Transform.origin.DistanceTo(workingScrollerPos) > Mathf.Epsilon)
            {
                return;
            }

            if (TypeOfVelocity == VelocityType.FalloffPerItem)
            {
                if (ScrollDirection == ScrollDirectionType.UpAndDown)
                {
                    // Ensure we've actually snapped the position to prevent an extreme in-between state
                    workingScrollerPos.y = (Mathf.Round(ScrollContainer.Transform.origin.y / CellHeight)) * CellHeight;
                }
                else
                {
                    workingScrollerPos.x = (Mathf.Round(ScrollContainer.Transform.origin.x / CellWidth)) * CellWidth;
                }
            }

            CurrentVelocityState = VelocityState.None;
            avgVelocity = 0.0f;

            // clean up our position for next frame
            initialScrollerPos = workingScrollerPos;
        }

        /// <summary>
        /// Wrapper for per frame velocity calculation and filtering.
        /// </summary>
        private void CalculateVelocity()
        {
            // Update simple velocity
            TryGetPointerPositionOnPlane(out Vector3 newPos);

            scrollVelocity = (ScrollDirection == ScrollDirectionType.UpAndDown)
                             ? (newPos.y - lastPointerPos.y) / GetProcessDeltaTime() * VelocityMultiplier
                             : (newPos.x - lastPointerPos.x) / GetProcessDeltaTime() * VelocityMultiplier;

            // And filter it...
            avgVelocity = (avgVelocity * (1.0f - velocityFilterWeight)) + (scrollVelocity * velocityFilterWeight);
        }

        /// <summary>
        /// The Animation Override to position our scroller based on manual movement <see cref="PageBy(int, bool)"/>, <see cref="MoveTo(int, bool)"/>,
        /// </summary>
        /// <param name="initialPos">The start position of the scrollContainer</param>
        /// <param name="finalPos">Where we want the scrollContainer to end up, typically this should be <see cref="workingScrollerPos"/></param>
        /// <param name="curve"><see cref="AnimationCurve"/> representing the easing desired</param>
        /// <param name="time">Time for animation, in seconds</param>
        /// <param name="callback">Optional callback action to be invoked after animation coroutine has finished</param>
        private async Task AnimateTo(Vector3 initialPos, Vector3 finalPos, CancellationToken token, float? time = null, System.Action callback = null)
        {

            if (time == null)
            {
                time = animationLength;
            }

            float counter = 0.0f;
            while (counter <= time)
            {
                if (token.IsCancellationRequested)
                    return;
                workingScrollerPos = initialPos.LinearInterpolate(finalPos, counter / (float)time);
                ScrollContainer.GlobalTransform = new Transform(ScrollContainer.GlobalTransform.basis, workingScrollerPos);

                counter += GetProcessDeltaTime();
                await ToSignal(this, nameof(_Processed));
            }

            // Update our values so they stick
            if (ScrollDirection == ScrollDirectionType.UpAndDown)
            {
                workingScrollerPos.y = initialScrollerPos.y = finalPos.y;
            }
            else
            {
                workingScrollerPos.x = initialScrollerPos.x = finalPos.x;
            }

            if (callback != null)
            {
                callback?.Invoke();
            }

            CurrentVelocityState = VelocityState.None;
            animateScroller = null;
        }

        /// <summary>
        /// Checks if the engaged joint has released the scrollable list
        /// </summary>
        private bool DetectScrollRelease(Vector3 pointerPos)
        {
            Vector3 scrollToPointerVector = pointerPos - ClippingComponent.GlobalTransform.origin;

            // Projecting vector onto every clip box space coordinate and using clip box lossy scale as reference to dimensions to scroll view visible bounds
            // Using dot product to check if pointer is in front or behind the scroll view plane
            bool isScrollRelease = scrollToPointerVector.Project(ClippingComponent.GlobalTransform.basis.y).Length() > ClippingComponent.GlobalTransform.basis.Scale.y / 2 + ReleaseThresholdTopBottom
                                || scrollToPointerVector.Project(ClippingComponent.GlobalTransform.basis.x).Length() > ClippingComponent.GlobalTransform.basis.Scale.x / 2 + ReleaseThresholdLeftRight

                                || (scrollToPointerVector.Dot(-GlobalTransform.basis.z) > 0 ?
                                        scrollToPointerVector.Project(-ClippingComponent.GlobalTransform.basis.z).Length() > ClippingComponent.GlobalTransform.basis.Scale.z / 2 + ReleaseThresholdBack :
                                        scrollToPointerVector.Project(-ClippingComponent.GlobalTransform.basis.z).Length() > ClippingComponent.GlobalTransform.basis.Scale.z / 2 + ReleaseThresholdFront);
            return isScrollRelease;
        }

        /// <summary>
        /// Adds list of renderers to the ClippingBox
        /// </summary>
        private void AddRenderersToClippingObject(List<MeshInstance> meshInstances)
        {
            foreach (var meshInstance in meshInstances)
            {
                ClippingComponent.AddMeshInstance(meshInstance);
            }
        }

        /// <summary>
        /// Removes list of renderers from the ClippingBox
        /// </summary>
        private void RemoveRenderersFromClippingObject(List<MeshInstance> meshInstances)
        {
            foreach (var meshInstance in meshInstances)
            {
                ClippingComponent.RemoveMeshInstance(meshInstance);
            }
        }

        /// <summary>
        /// Removes all renderers currently being clipped by the clipping box
        /// </summary>
        private void ClearClippingBox()
        {
            ClippingComponent.ClearMeshInstances();
        }

        /// <summary>
        /// Helper to perform division operations and prevent division by 0.
        /// </summary>
        private static int SafeDivisionInt(int numerator, int denominator)
        {
            return (denominator != 0) ? numerator / denominator : 0;
        }

        private float SafeDivisionFloat(float numerator, float denominator)
        {
            return (denominator != 0) ? numerator / denominator : 0;
        }

        /// <summary>
        /// Checks visibility of scroll content by iterating through all content renderers and colliders.
        /// All inactive content objects and colliders are reactivated during visibility restoration.
        /// </summary>
        private void ManageVisibility(bool isRestoringVisibility = false)
        {
            if (!MaskEnabled && !isRestoringVisibility)
            {
                return;
            }

            AABB clippingThresholdAABB = ClippingComponent.Bounds;
            var contentMeshInstances = ScrollContainer.GetChildrenAll<MeshInstance>();
            clippedMeshInstances.Clear();
            clippedMeshInstances.UnionWith(ClippingComponent.GetNodesCopy());
            clippedActors.Clear();

            // Remove all renderers from clipping primitive that are not part of scroll content
            foreach (var clippedMeshInstance in clippedMeshInstances)
            {
                if (clippedMeshInstance != null && !ScrollContainer.IsAParentOf(clippedMeshInstance))
                {
                    if (DisableClippedActors)
                    {
                        var actor = GetActor(clippedMeshInstance);
                        clippedActors.Add(actor);
                    }
                    if (DisableClippedRenderers)
                    {
                        if (!clippedMeshInstance.Visible)
                            clippedMeshInstance.Visible = true;
                    }

                    renderersToUnclip.Add(clippedMeshInstance);
                }
            }

            // Check render visibility
            foreach (var meshInstance in contentMeshInstances)
            {
                // All content renderers should be added to clipping primitive
                if (!isRestoringVisibility && MaskEnabled && !clippedMeshInstances.Contains(meshInstance))
                {
                    renderersToClip.Add(meshInstance);
                }

                // Complete or partially visible renders should be clipped and its game object should be active
                var meshInstanceAABB = meshInstance.GetTransformedAabb();
                if (isRestoringVisibility
                    || clippingThresholdAABB.Encloses(meshInstanceAABB)
                    || clippingThresholdAABB.Intersects(meshInstanceAABB))
                {
                    if (DisableClippedActors)
                    {
                        var actor = GetActor(meshInstance);
                        clippedActors.Add(actor);
                    }
                    if (DisableClippedRenderers)
                    {
                        if (!meshInstance.Visible)
                            meshInstance.Visible = true;
                    }
                }

                // Hidden renderer game objects should be inactive
                else
                {
                    if (DisableClippedActors)
                    {
                        var actor = GetActor(meshInstance);
                        if (!clippedActors.Contains(actor) && actor.PauseMode != PauseModeEnum.Stop)
                        {
                            actor.PauseMode = PauseModeEnum.Stop;
                            actor.Visible = false;
                        }
                    }
                    if (DisableClippedRenderers)
                    {
                        if (meshInstance.Visible)
                            meshInstance.Visible = false;
                    }
                }
            }

            // Check collider visibility
            // Outer clipping bounds is used to ensure collider has minimum visibility to stay enabled
            AABB outerClippingThresholdBounds = ClippingComponent.Bounds;
            outerClippingThresholdBounds.Size *= contentVisibilityThresholdRatio;

            var collisionShapes = ScrollContainer.GetChildrenAll<CollisionShape>();
            foreach (var shape in collisionShapes)
            {
                // Disabling content colliders during drag to stop interaction even if game object is inactive
                if (!isRestoringVisibility && IsDragging)
                {
                    if (!shape.Disabled)
                    {
                        shape.Disabled = true;
                    }

                    continue;
                }

                // No need to manage collider visibility in case game object is inactive and no pointer is dragging the scroll
                if (!isRestoringVisibility && shape.PauseMode == PauseModeEnum.Stop)
                {
                    continue;
                }

                // Completely or partially visible colliders should be enabled if scroll is not drag engaged
                var aabb = shape.Shape.GetDebugMesh().GetAabb();
                Vector3 min = aabb.Position;
                Vector3 max = aabb.Position + aabb.Size;
                Vector3 tmin = new Vector3(), tmax = new Vector3();
                for (int i = 0; i < 3; i++) {
                    tmin[i] = tmax[i] = shape.GlobalTransform.origin[i];
                    for (int j = 0; j < 3; j++) {
                        float e = shape.GlobalTransform.basis[i][j] * min[j];
                        float f = shape.GlobalTransform.basis[i][j] * max[j];
                        if (e < f) {
                            tmin[i] += e;
                            tmax[i] += f;
                        } else {
                            tmin[i] += f;
                            tmax[i] += e;
                        }
                    }
                }
                AABB transformedAabb = new AABB();
                transformedAabb.Position = tmin;
                transformedAabb.Size = tmax - tmin;
                if (isRestoringVisibility || outerClippingThresholdBounds.Encloses(transformedAabb))
                {
                    if (shape.Disabled)
                    {
                        shape.Disabled = false;
                    }
                }
                // Hidden colliders should be disabled
                else
                {
                    if (!shape.Disabled)
                    {
                        shape.Disabled = true;
                    }
                }
            }

            if (!isRestoringVisibility)
            {
                ReconcileClippingContent();
            }
        }

        /// <summary>
        /// Precalculates the total amount of travel given the scroller's current average velocity and drag.
        /// </summary>
        /// <param name="steps"><see cref="out"/> Number of steps to get our <see cref="avgVelocity"/> to effectively "zero" (0.00001).</param>
        /// <returns>The total distance the <see cref="avgVelocity"/> with <see cref="VelocityDampen"/> as drag would travel.</returns>
        private float IterateFalloff(float vel, out int steps)
        {
            // Some day this should be a falloff formula, below is the number of steps. Just can't figure out how to get the right velocity.
            // float numSteps = (Mathf.Log(0.00001f)  - Mathf.Log(Mathf.Abs(avgVelocity))) / Mathf.Log(velocityFalloff);

            float newVal = 0.0f;
            float v = vel;
            steps = 0;

            while (Mathf.Abs(v) > 0.00001)
            {
                v *= VelocityDampen;
                newVal += v;
                steps++;
            }

            return newVal;
        }

        /// <summary>
        /// Applies <paramref name="workingPos"/> to the <see cref="Transform.origin"/> of our <see cref="scrollContainer"/>
        /// </summary>
        /// <param name="workingPos">The new desired position for <see cref="scrollContainer"/> in local space</param>
        private void ApplyPosition(Vector3 workingPos)
        {
            Vector3 newScrollPos;

            switch (ScrollDirection)
            {
                case ScrollDirectionType.UpAndDown:
                default:

                    newScrollPos = new Vector3(ScrollContainer.Transform.origin.x, workingPos.y, 0.0f);
                    break;

                case ScrollDirectionType.LeftAndRight:

                    newScrollPos = new Vector3(workingPos.x, ScrollContainer.Transform.origin.y, 0.0f);

                    break;
            }
            ScrollContainer.Transform = new Transform(ScrollContainer.Transform.basis, newScrollPos);
            if (CurrentVelocityState != VelocityState.None)
                EmitSignal(nameof(scrolling));
        }

        /// <summary>
        /// Resets the interaction state of the ScrollingObjectCollection for the next scroll.
        /// </summary>
        private void ResetInteraction()
        {
            if (IsTouched) EmitSignal(nameof(touch_ended));

            // Release the pointer
            currentInputSource = null;

            // Clear our states
            IsTouched = false;
            IsEngaged = false;
            IsDragging = false;
        }

        /// <summary>
        /// Resets the scroll offset state of the ScrollingObjectCollection.
        /// </summary>
        private void ResetScrollOffset()
        {
            MoveToIndex(0, false);
            workingScrollerPos = Vector3.Zero;
            ApplyPosition(workingScrollerPos);
        }

        /// <summary>
        /// All inactive content objects and colliders are reactivated and renderers are unclipped.
        /// </summary>
        private void RestoreContentVisibility()
        {
            ClearClippingBox();
            ManageVisibility(true);
        }

        /// <summary>
        /// Moves the scroll container to the position that makes the tier with the tierIndex the first in the viewable area
        /// </summary>
        private void MoveToTier(int tierIndex, bool animateToPosition = true, System.Action callback = null)
        {
            if (animateScroller != null)
            {
                CurrentVelocityState = VelocityState.None;
                animateScrollerToken.Cancel();
            }

            if (ScrollDirection == ScrollDirectionType.UpAndDown)
            {
                workingScrollerPos.y = tierIndex * CellHeight;

                // Clamp the working pos since we already have calculated it
                workingScrollerPos.y = Mathf.Clamp(workingScrollerPos.y, minY, MaxY);

                // Zero out the other axes
                workingScrollerPos = workingScrollerPos * Vector3.Up;
            }
            else
            {
                workingScrollerPos.x = tierIndex * CellWidth * -1.0f;

                // Clamp the working pos since we already have calculated it
                workingScrollerPos.x = Mathf.Clamp(workingScrollerPos.x, MinX, maxX);

                // Zero out the other axes
                workingScrollerPos = workingScrollerPos * Vector3.Right;
            }

            if (initialScrollerPos != workingScrollerPos)
            {
                CurrentVelocityState = VelocityState.Animating;

                if (animateToPosition)
                {
                    animateScroller = AnimateTo(ScrollContainer.Transform.origin, workingScrollerPos, animateScrollerToken.Token, animationLength, callback);
                }
                else
                {
                    CurrentVelocityState = VelocityState.None; // Flagging the instant position change to trigger momentum events
                    initialScrollerPos = workingScrollerPos;
                }

                if (callback != null)
                {
                    callback?.Invoke();
                }
            }
        }

        private Actor GetActor(MeshInstance meshInstance)
        {
            var parent = meshInstance.GetParent();
            while (parent != null)
            {
                if (parent is Actor actor)
                    return actor;
                parent = parent.GetParent();
            }

            throw new ArgumentException("Failed to find an actor parent.", nameof(meshInstance));
        }

        private void _on_ScrollingObjectCollection_touch_started()
        {
            var user = GetParent<Actor>().App.LocalUser;
            if (user != null)
            {
                _touchAction.StartAction(user);
            }
        }

        private void _on_ScrollingObjectCollection_touch_ended()
        {
            var user = GetParent<Actor>().App.LocalUser;
            if (user != null)
            {
                _touchAction.StopAction(user);
            }
        }

        private void _on_ScrollingObjectCollection_scroll_started()
        {
            var user = GetParent<Actor>().App.LocalUser;
            if (user != null)
            {
                _scrollAction.StartAction(user);
            }
        }

        private void _on_ScrollingObjectCollection_scroll_ended()
        {
            var user = GetParent<Actor>().App.LocalUser;
            if (user != null)
            {
                _scrollAction.StopAction(user);
            }
        }

        private void _on_ScrollingObjectCollection_scrolling()
        {
            _scrollAction.PerformActionUpdate();
        }

        #endregion private methods

        #region public methods

        /// <summary>
        /// Resets the ScrollingObjectCollection
        /// </summary>
        public void Reset()
        {
            ResetInteraction();
            UpdateContent();
            ResetScrollOffset();
        }

        /// <summary>
        /// Safely adds a child game object to scroll collection.
        /// </summary>
        public void AddContent(Spatial content)
        {
            ScrollContainer.AddChild(content);
            Reset();
        }

        /// <summary>
        /// Safely removes a child game object from scroll content and clipping box.
        /// </summary>
        public void RemoveItem(Spatial item)
        {
            if (item == null)
            {
                return;
            }

            foreach (var meshInstance in item.GetChildrenAll<MeshInstance>())
            {
                renderersToUnclip.Add(meshInstance);
            }

            item.GetParent<Node>().RemoveChild(item);
            Reset();
        }

        /// <summary>
        /// Checks whether the given cell is visible relative to viewable area or page.
        /// </summary>
        /// <param name="cellIndex">the index of the pagination cell</param>
        /// <returns>true when cell is visible</returns>
        public bool IsCellVisible(int cellIndex)
        {
            bool isCellVisible = true;

            if (cellIndex < FirstVisibleCellIndex)
            {
                // It's above the visible area
                isCellVisible = false;
            }
            else if (cellIndex >= FirstHiddenCellIndex)
            {
                // It's below the visible area
                isCellVisible = false;
            }
            return isCellVisible;
        }

        /// <summary>
        /// Moves scroller container by a multiplier of the number of tiers in the viewable area.
        /// </summary>
        /// <param name="numberOfPages">Amount of pages to move by</param>
        /// <param name="animate"> If true, scroller will animate to new position</param>
        /// <param name="callback"> An optional action to pass in to get notified that the <see cref="ScrollingObjectCollection"/> is finished moving</param>
        public void MoveByPages(int numberOfPages, bool animate = true, System.Action callback = null)
        {
            int tierIndex = SafeDivisionInt(FirstVisibleCellIndex, CellsPerTier) + (numberOfPages * TiersPerPage);

            MoveToTier(tierIndex, animate, callback);
        }

        /// <summary>
        /// Moves scroller container a relative number of tiers of cells.
        /// </summary>
        /// <param name="numberOfTiers">Amount of tiers to move by</param>
        /// <param name="animate">if true, scroller will animate to new position</param>
        /// <param name="callback"> An optional action to pass in to get notified that the <see cref="ScrollingObjectCollection"/> is finished moving</param>
        public void MoveByTiers(int numberOfTiers, bool animate = true, System.Action callback = null)
        {
            int tierIndex = SafeDivisionInt(FirstVisibleCellIndex, CellsPerTier) + numberOfTiers;

            MoveToTier(tierIndex, animate, callback);
        }

        /// <summary>
        /// Moves scroller container to a position where the selected cell is in the first tier of the viewable area.
        /// </summary>
        /// <param name="cellIndex">Index of the cell to move to</param>
        /// <param name="animate">if true, scroller will animate to new position</param>
        /// <param name="callback"> An optional action to pass in to get notified that the <see cref="ScrollingObjectCollection"/> is finished moving</param>
        public void MoveToIndex(int cellIndex, bool animateToPosition = true, System.Action callback = null)
        {
            cellIndex = (cellIndex < 0) ? 0 : cellIndex;
            int tierIndex = SafeDivisionInt(cellIndex, CellsPerTier);

            MoveToTier(tierIndex, animateToPosition, callback);
        }

        #endregion public methods

        #region IMixedRealityPointerHandler implementation

        /// <inheritdoc/>
        public void OnPointerUp(Spatial inputSource, Node userNode, Vector3 point)
        {
            if (currentInputSource == null)
            {
                return;
            }

            // Release the pointer
            //currentPointer.IsTargetPositionLockedOnFocusLock = oldIsTargetPositionLockedOnFocusLock;

            if (!IsTouched && IsEngaged && animateScroller == null)
            {
                if (IsDragging)
                {
                    // Its a drag release
                    initialScrollerPos = workingScrollerPos;
                    CurrentVelocityState = VelocityState.Calculating;
                }

                ResetInteraction();
            }
        }

        /// <inheritdoc/>
        public void OnPointerDown(Spatial inputSource, Node userNode, Vector3 point)
        {
            // Current pointer owns scroll interaction until scroll release happens. Ignoring any interaction with other pointers.
            if (currentInputSource != null)
            {
                return;
            }

            currentInputSource = inputSource;
            //oldIsTargetPositionLockedOnFocusLock = currentPointer.IsTargetPositionLockedOnFocusLock;
/*
            if (!(currentPointer is IMixedRealityNearPointer) && currentPointer.Controller.IsRotationAvailable)
            {
                currentPointer.IsTargetPositionLockedOnFocusLock = false;
            }

            currentTool.IsFocusLocked = false; // Unwanted focus locked on children items
*/
            // Reset the scroll state
            scrollVelocity = 0.0f;

            if (TryGetPointerPositionOnPlane(out initialPointerPos))
            {
                initialScrollerPos = ScrollContainer.Transform.origin;
                CurrentVelocityState = VelocityState.None;

                IsTouched = false;
                IsEngaged = true;
                IsDragging = false;
            }
        }

        /// <inheritdoc/>
        /// Pointer Click handled during Update.
        public void OnPointerClicked(Spatial inputSource, Node userNode, Vector3 point) { }

        /// <inheritdoc/>
        public void OnPointerDragged(Spatial inputSource, Node userNode, Vector3 point) { }

        #endregion IMixedRealityPointerHandler implementation

        #region IMixedRealityTouchHandler implementation

        /// <inheritdoc/>
        public void OnTouchStarted(Spatial inputSource, Node userNode, Vector3 point)
        {
            // Current pointer owns scroll interaction until scroll release happens. Ignoring any interaction with other pointers.
            if (currentInputSource != null)
            {
                return;
            }

            currentInputSource = inputSource;

            animateScrollerToken.Cancel();
            CurrentVelocityState = VelocityState.None;
            animateScroller = null;

            if (!IsTouched && !IsEngaged)
            {
                initialPointerPos = currentInputSource.GlobalTransform.origin;
                initialScrollerPos = ScrollContainer.Transform.origin;

                IsTouched = true;
                IsEngaged = true;
                IsDragging = false;
                EmitSignal(nameof(touch_started));
            }
        }

        /// <inheritdoc/>
        /// Touch release handled during Update.
        public void OnTouchCompleted(Spatial inputSource, Node userNode, Vector3 point) { }

        /// <inheritdoc/>
        public void OnTouchUpdated(Spatial inputSource, Node userNode, Vector3 point)
        {
            if (currentInputSource == null)
            {
                return;
            }

            if (IsDragging)
            {
                //eventData.Use();
            }
        }

        #endregion IMixedRealityTouchHandler implementation

        #region IToolkit
        public void ApplyPatch(ToolkitPatch toolkitPatch)
        {
            if (toolkitPatch is ScrollingObjectCollectionPatch patch)
            {
                ScrollingTouchable = parentActor.GetChild<TouchablePlane>();
                foreach (var scrollContent in patch.ScrollContents)
                {
                    var content = parentActor.App.FindActor(scrollContent) as Spatial;
                    content.GetParent()?.RemoveChild(content);
                    AddContent(content);
                }
                TiersPerPage = TiersPerPage.ApplyPatch(patch.TiersPerPage);
                CellsPerTier = CellsPerTier.ApplyPatch(patch.CellsPerTier);
                CellWidth = CellWidth.ApplyPatch(patch.CellWidth);
                CellHeight = CellHeight.ApplyPatch(patch.CellHeight);
                CellDepth = CellDepth.ApplyPatch(patch.CellDepth);
                ScrollDirection = ScrollDirection.ApplyPatch(patch.ScrollDirectionType);
                UpdateContent();
            }
        }

        #endregion IToolkit
    }
}