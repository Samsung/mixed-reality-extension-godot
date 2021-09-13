// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Diagnostics;
using Godot;


namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Add a NearInteractionTouchable to your scene and configure a touchable surface
    /// in order to get PointerDown and PointerUp events whenever a PokePointer touches this surface.
    /// </summary>
    public class NearInteractionTouchable : NearInteractionTouchableSurface
    {
        [Export]
        protected Vector3 localForward = Vector3.Forward;

        /// <summary>
        /// Local space forward direction
        /// </summary>

        public Vector3 LocalForward => localForward;

        [Export]
        protected Vector3 localUp = Vector3.Up;

        /// <summary>
        /// Local space up direction
        /// </summary>
        public Vector3 LocalUp { get => localUp; }

        /// <summary>
        /// Returns true if the LocalForward and LocalUp vectors are orthogonal.
        /// </summary>
        /// <remarks>
        /// LocalRight is computed using the cross product and is always orthogonal to LocalForward and LocalUp.
        /// </remarks>
        public bool AreLocalVectorsOrthogonal => localForward.Dot(localUp) == 0;

        [Export]
        protected Vector3 localCenter = Vector3.Zero;

        /// <summary>
        /// Local space object center
        /// </summary>
        public override Vector3 LocalCenter { get => localCenter; }

        /// <summary>
        /// Local space and gameObject right
        /// </summary>
        public Vector3 LocalRight
        {
            get
            {
                Vector3 cross = localUp.Cross(localForward);
                if (cross == Vector3.Zero)
                {
                    // vectors are collinear return default right
                    return Vector3.Right;
                }
                else
                {
                    return cross;
                }
            }
        }

        /// <summary>
        /// Forward direction of the gameObject
        /// </summary>
        public Vector3 Forward => -node.GlobalTransform.basis.z;

        /// <summary>
        /// Forward direction of the NearInteractionTouchable plane, the press direction needs to face the
        /// camera.
        /// </summary>
        public override Vector3 LocalPressDirection => -localForward;

        [Export]
        protected Vector2 bounds = Vector2.Zero;

        /// <summary>
        /// Bounds or size of the 2D NearInteractionTouchablePlane
        /// </summary>
        public override Vector2 Bounds { get => bounds; }

        /// <summary>
        /// Check if the touchableCollider is enabled and in the gameObject hierarchy
        /// </summary>
        public bool ColliderEnabled { get { return !TouchableCollider.Disabled; } }


        [Export]
        private NodePath touchableCollider;

        /// <summary>
        /// BoxCollider used to calculate bounds and local center, if not set before runtime the gameObjects's BoxCollider will be used by default
        /// </summary>
        public CollisionShape TouchableCollider => GetNode<CollisionShape>(touchableCollider);


        [Export]
        private NodePath baseToolkit;

        public IMixedRealityTouchHandler BaseToolkit => GetNode<IMixedRealityTouchHandler>(baseToolkit);

        public bool Pressed { get; private set;}
        private bool entered = false;
        private Spatial currentTouchableObjectDown;
        private Vector3 previousPoint = Vector3.Zero;
        Area area;
        public override void _Ready()
        {
            area = TouchableCollider.GetParent<Area>();
            area.Connect("area_shape_entered", this, nameof(OnAreaShapeEnter));
            area.Connect("area_shape_exited", this, nameof(OnAreaShapeExit));
        }

        public override void _PhysicsProcess(float delta)
        {
            if (currentTouchableObjectDown == null)
            {
                return;
            }
            var collisionPoint = currentTouchableObjectDown.GlobalTransform.origin - currentTouchableObjectDown.GlobalTransform.basis.z.Normalized() * 0.01f;
            if (entered)
            {
                if (!Pressed)
                {
                    Pressed = true;
                    //BaseToolkit.OnTouchStarted(new HandTrackingInputEventData(this, collisionPoint, previousPoint));
                }
                else
                {
                    /*
                    var previousPointLocal = area.ToLocal(previousPoint);
                    var collisionPointLocal = area.ToLocal(collisionPoint);
                    var touchMoveVector = collisionPointLocal - previousPointLocal;
                    if (area.Transform.basis.z.Dot(touchMoveVector) < 0) //push
                    {

                    }
                    else
                    {

                    }
                    */
                    BaseToolkit.OnTouchUpdated(new HandTrackingInputEventData(this, collisionPoint, previousPoint));
                }
            }
            else
            {
                Pressed = false;
                //BaseToolkit.OnTouchCompleted(new HandTrackingInputEventData(this, collisionPoint, previousPoint));
            }
            previousPoint = collisionPoint;
        }

        private void OnAreaShapeEnter(int areaId, Area otherArea, int areaShape, int localShape)
        {
            currentTouchableObjectDown = otherArea;
            var collisionPoint = otherArea.GlobalTransform.origin - otherArea.GlobalTransform.basis.z.Normalized() * 0.01f;

            entered = true;
            BaseToolkit.OnTouchStarted(new HandTrackingInputEventData(this, collisionPoint, collisionPoint));
            previousPoint = collisionPoint;
        }

        private void OnAreaShapeExit(int areaId, Area otherArea, int areaShape, int localShape)
        {
            currentTouchableObjectDown = otherArea;
            var collisionPoint = otherArea.GlobalTransform.origin - otherArea.GlobalTransform.basis.z.Normalized() * 0.01f;

            entered = false;
            BaseToolkit.OnTouchCompleted(new HandTrackingInputEventData(this, collisionPoint, previousPoint));
            previousPoint = collisionPoint;
        }

        internal protected override void OnValidate()
        {
            if (!Engine.EditorHint)
            {   // Don't validate during play mode
                return;
            }

            base.OnValidate();

            touchableCollider = GetCollisionShape().GetPath();

            Debug.Assert(localForward.Length() > 0);
            Debug.Assert(localUp.Length() > 0);
            string hierarchy = "";//gameObject.transform.EnumerateAncestors(true).Aggregate("", (result, next) => next.gameObject.name + "=>" + result);
            if (localUp.LengthSquared() == 1 && localForward.LengthSquared() == 1)
            {
                Debug.Assert(localForward.Dot(localUp) == 0, $"localForward and localUp not perpendicular for object {hierarchy}. Did you set Local Forward correctly?");
            }

            localForward = localForward.Normalized();
            localUp = localUp.Normalized();

            bounds.x = Mathf.Max(bounds.x, 0);
            bounds.y = Mathf.Max(bounds.y, 0);
            PropertyListChangedNotify();
        }

        internal void OnEnable()
        {
            if (touchableCollider == null)
            {
                SetTouchableCollider(GetCollisionShape().GetPath());
            }
        }

        /// <summary>
        /// Set local forward direction and ensure that local up is perpendicular to the new local forward and
        /// local right direction.  The forward position should be facing the camera. The direction is indicated in scene view by a
        /// white arrow in the center of the plane.
        /// </summary>
        public void SetLocalForward(Vector3 newLocalForward)
        {
            localForward = newLocalForward;
            localUp = localForward.Cross(LocalRight).Normalized();
        }

        /// <summary>
        /// Set new local up direction and ensure that local forward is perpendicular to the new local up and
        /// local right direction.
        /// </summary>
        public void SetLocalUp(Vector3 newLocalUp)
        {
            localUp = newLocalUp;
            localForward = LocalRight.Cross(localUp).Normalized();
        }

        /// <summary>
        /// Set the position (center) of the NearInteractionTouchable plane relative to the gameObject.
        /// The position of the plane should be in front of the gameObject.
        /// </summary>
        public void SetLocalCenter(Vector3 newLocalCenter)
        {
            localCenter = newLocalCenter;
        }

        /// <summary>
        /// Set the size (bounds) of the 2D NearInteractionTouchable plane.
        /// </summary>
        public void SetBounds(Vector2 newBounds)
        {
            bounds = newBounds;
        }

        /// <summary>
        /// Adjust the bounds, local center and local forward to match a given box collider.  This method
        /// also changes the size of the box collider attached to the gameObject.
        /// Default Behavior:  if touchableCollider is null at runtime, the object's box collider will be used
        /// to size and place the NearInteractionTouchable plane in front of the gameObject
        /// </summary>
        public void SetTouchableCollider(NodePath newColliderNodePath)
        {
            CollisionShape newCollider = GetNode<CollisionShape>(newColliderNodePath);
            if (newCollider != null && newCollider.Shape is BoxShape boxShape)
            {
                // Set touchableCollider for possible reference in the future
                touchableCollider = newColliderNodePath;

                SetLocalForward(Vector3.Forward);

                Vector2 adjustedSize = new Vector2(
                            Math.Abs(boxShape.Extents.Dot(LocalRight)),
                            Math.Abs(boxShape.Extents.Dot(LocalUp)));

                SetBounds(adjustedSize);
/*
                // Set x and y center to match the newCollider but change the position of the
                // z axis so the plane is always in front of the object
                SetLocalCenter(newCollider.center + Vector3.Scale(newCollider.size / 2.0f, LocalForward));

                // Set size and center of the gameObject's box collider to match the collider given, if there
                // is no box collider behind the NearInteractionTouchable plane, an event will not be raised
                BoxCollider attachedBoxCollider = GetComponent<BoxCollider>();
                attachedBoxCollider.size = newCollider.size;
                attachedBoxCollider.center = newCollider.center;
                */
            }
            else
            {
                GD.PushWarning("BoxCollider is null, cannot set bounds of NearInteractionTouchable plane");
            }
        }

        /// <inheritdoc />
        public override float DistanceToTouchable(Vector3 samplePoint, out Vector3 normal)
        {
            normal = Forward;

            Vector3 localPoint = node.ToLocal(samplePoint) - localCenter;

            // Get surface coordinates
            Vector3 planeSpacePoint = new Vector3(
                localPoint.Dot(LocalRight),
                localPoint.Dot(localUp),
                localPoint.Dot(localForward));

            // touchables currently can only be touched within the bounds of the rectangle.
            // We return infinity to ensure that any point outside the bounds does not get touched.
            if (planeSpacePoint.x < -bounds.x / 2 ||
                planeSpacePoint.x > bounds.x / 2 ||
                planeSpacePoint.y < -bounds.y / 2 ||
                planeSpacePoint.y > bounds.y / 2)
            {
                return float.PositiveInfinity;
            }

            // Scale back to 3D space
            planeSpacePoint = node.GlobalTransform.basis.Scale * planeSpacePoint;

            return Math.Abs(planeSpacePoint.z);
        }

        private CollisionShape GetCollisionShape()
        {
            foreach (var child in node.GetChildren())
            {
                if (child is Area area)
                {
                    return area.GetChild<CollisionShape>(0);
                }
            }
            return null;
        }
    }
}