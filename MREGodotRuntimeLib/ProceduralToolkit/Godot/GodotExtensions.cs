using System;
using Godot;

namespace Godot
{
    public static class GodotExtensions
    {
        /// <summary>
        /// Returns the signed angle to the given vector, in radians.
        /// The sign of the angle is positive in a counter-clockwise
        /// direction and negative in a clockwise direction when viewed
        /// from the side specified by the `axis`.
        /// This code is from https://github.com/godotengine/godot/pull/45807 (Godot 4.0)
        /// </summary>
        /// <param name="to">The other vector to compare this vector to.</param>
        /// <param name="axis">The reference axis to use for the angle sign.</param>
        /// <returns>The signed angle between the two vectors, in radians.</returns>
        public static float SignedAngleTo(this Vector3 v, Vector3 to, Vector3 axis)
        {
            Vector3 crossTo = v.Cross(to);
            float unsignedAngle = Mathf.Atan2(crossTo.Length(), v.Dot(to));
            float sign = crossTo.Dot(axis);
            return (sign < 0) ? -unsignedAngle : unsignedAngle;
        }
    }
}