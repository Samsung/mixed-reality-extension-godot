using Godot;

namespace MixedRealityExtension.ProceduralToolkit
{
	/// <summary>
	/// Collection of distance calculation algorithms
	/// </summary>
	internal static partial class Distance
	{
		#region Point-Line

		/// <summary>
		/// Returns a distance to the closest point on the line
		/// </summary>
		public static float PointLine(Vector3 point, Line3 line)
		{

			return point.DistanceTo(Closest.PointLine(point, line));
		}

		/// <summary>
		/// Returns a distance to the closest point on the line
		/// </summary>
		public static float PointLine(Vector3 point, Vector3 lineOrigin, Vector3 lineDirection)
		{
			return point.DistanceTo(Closest.PointLine(point, lineOrigin, lineDirection));
		}

		#endregion Point-Line

		#region Point-Ray

		/// <summary>
		/// Returns a distance to the closest point on the ray
		/// </summary>
		public static float PointRay(Vector3 point, Ray ray)
		{
			return point.DistanceTo(Closest.PointRay(point, ray));
		}

		/// <summary>
		/// Returns a distance to the closest point on the ray
		/// </summary>
		public static float PointRay(Vector3 point, Vector3 rayOrigin, Vector3 rayDirection)
		{
			return point.DistanceTo(Closest.PointRay(point, rayOrigin, rayDirection));
		}

		#endregion Point-Ray

		#region Point-Segment

		/// <summary>
		/// Returns a distance to the closest point on the segment
		/// </summary>
		public static float PointSegment(Vector3 point, Segment3 segment)
		{
			return point.DistanceTo(Closest.PointSegment(point, segment));
		}

		/// <summary>
		/// Returns a distance to the closest point on the segment
		/// </summary>
		public static float PointSegment(Vector3 point, Vector3 segmentA, Vector3 segmentB)
		{
			return point.DistanceTo(Closest.PointSegment(point, segmentA, segmentB));
		}

		#endregion Point-Segment

		#region Point-Sphere

		/// <summary>
		/// Returns a distance to the closest point on the sphere
		/// </summary>
		/// <returns>Positive value if the point is outside, negative otherwise</returns>
		public static float PointSphere(Vector3 point, Sphere sphere)
		{
			return PointSphere(point, sphere.center, sphere.radius);
		}

		/// <summary>
		/// Returns a distance to the closest point on the sphere
		/// </summary>
		/// <returns>Positive value if the point is outside, negative otherwise</returns>
		public static float PointSphere(Vector3 point, Vector3 sphereCenter, float sphereRadius)
		{
			return (sphereCenter - point).Length() - sphereRadius;
		}

		#endregion Point-Sphere

		#region Line-Sphere

		/// <summary>
		/// Returns the distance between the closest points on the line and the sphere
		/// </summary>
		public static float LineSphere(Line3 line, Sphere sphere)
		{
			return LineSphere(line.origin, line.direction, sphere.center, sphere.radius);
		}

		/// <summary>
		/// Returns the distance between the closest points on the line and the sphere
		/// </summary>
		public static float LineSphere(Vector3 lineOrigin, Vector3 lineDirection, Vector3 sphereCenter, float sphereRadius)
		{
			Vector3 originToCenter = sphereCenter - lineOrigin;
			float centerProjection = lineDirection.Dot(originToCenter);
			float sqrDistanceToLine = originToCenter.LengthSquared() - centerProjection*centerProjection;
			float sqrDistanceToIntersection = sphereRadius*sphereRadius - sqrDistanceToLine;
			if (sqrDistanceToIntersection < -Geometry.Epsilon)
			{
				// No intersection
				return Mathf.Sqrt(sqrDistanceToLine) - sphereRadius;
			}
			return 0;
		}

		#endregion Line-Sphere

		#region Ray-Sphere

		/// <summary>
		/// Returns the distance between the closest points on the ray and the sphere
		/// </summary>
		public static float RaySphere(Ray ray, Sphere sphere)
		{
			return RaySphere(ray.origin, ray.direction, sphere.center, sphere.radius);
		}

		/// <summary>
		/// Returns the distance between the closest points on the ray and the sphere
		/// </summary>
		public static float RaySphere(Vector3 rayOrigin, Vector3 rayDirection, Vector3 sphereCenter, float sphereRadius)
		{
			Vector3 originToCenter = sphereCenter - rayOrigin;
			float centerProjection = rayDirection.Dot(originToCenter);
			if (centerProjection + sphereRadius < -Geometry.Epsilon)
			{
				// No intersection
				return Mathf.Sqrt(originToCenter.LengthSquared()) - sphereRadius;
			}

			float sqrDistanceToOrigin = originToCenter.LengthSquared();
			float sqrDistanceToLine = sqrDistanceToOrigin - centerProjection*centerProjection;
			float sqrDistanceToIntersection = sphereRadius*sphereRadius - sqrDistanceToLine;
			if (sqrDistanceToIntersection < -Geometry.Epsilon)
			{
				// No intersection
				if (centerProjection < -Geometry.Epsilon)
				{
					return Mathf.Sqrt(sqrDistanceToOrigin) - sphereRadius;
				}
				return Mathf.Sqrt(sqrDistanceToLine) - sphereRadius;
			}
			if (sqrDistanceToIntersection < Geometry.Epsilon)
			{
				if (centerProjection < -Geometry.Epsilon)
				{
					// No intersection
					return Mathf.Sqrt(sqrDistanceToOrigin) - sphereRadius;
				}
				// Point intersection
				return 0;
			}

			// Line intersection
			float distanceToIntersection = Mathf.Sqrt(sqrDistanceToIntersection);
			float distanceA = centerProjection - distanceToIntersection;
			float distanceB = centerProjection + distanceToIntersection;

			if (distanceA < -Geometry.Epsilon)
			{
				if (distanceB < -Geometry.Epsilon)
				{
					// No intersection
					return Mathf.Sqrt(sqrDistanceToOrigin) - sphereRadius;
				}

				// Point intersection;
				return 0;
			}

			// Two points intersection;
			return 0;
		}

		#endregion Ray-Sphere

		#region Segment-Sphere

		/// <summary>
		/// Returns the distance between the closest points on the segment and the sphere
		/// </summary>
		public static float SegmentSphere(Segment3 segment, Sphere sphere)
		{
			return SegmentSphere(segment.a, segment.b, sphere.center, sphere.radius);
		}

		/// <summary>
		/// Returns the distance between the closest points on the segment and the sphere
		/// </summary>
		public static float SegmentSphere(Vector3 segmentA, Vector3 segmentB, Vector3 sphereCenter, float sphereRadius)
		{
			Vector3 segmentAToCenter = sphereCenter - segmentA;
			Vector3 fromAtoB = segmentB - segmentA;
			float segmentLength = fromAtoB.Length();
			if (segmentLength < Geometry.Epsilon)
			{
				return segmentAToCenter.Length() - sphereRadius;
			}

			Vector3 segmentDirection = fromAtoB.Normalized();
			float centerProjection = segmentDirection.Dot(segmentAToCenter);
			if (centerProjection + sphereRadius < -Geometry.Epsilon ||
				centerProjection - sphereRadius > segmentLength + Geometry.Epsilon)
			{
				// No intersection
				if (centerProjection < 0)
				{
					return segmentAToCenter.Length() - sphereRadius;
				}
				return (sphereCenter - segmentB).Length() - sphereRadius;
			}

			float sqrDistanceToA = segmentAToCenter.LengthSquared();
			float sqrDistanceToLine = sqrDistanceToA - centerProjection*centerProjection;
			float sqrDistanceToIntersection = sphereRadius*sphereRadius - sqrDistanceToLine;
			if (sqrDistanceToIntersection < -Geometry.Epsilon)
			{
				// No intersection
				if (centerProjection < -Geometry.Epsilon)
				{
					return Mathf.Sqrt(sqrDistanceToA) - sphereRadius;
				}
				if (centerProjection > segmentLength + Geometry.Epsilon)
				{
					return (sphereCenter - segmentB).Length() - sphereRadius;
				}
				return Mathf.Sqrt(sqrDistanceToLine) - sphereRadius;
			}

			if (sqrDistanceToIntersection < Geometry.Epsilon)
			{
				if (centerProjection < -Geometry.Epsilon)
				{
					// No intersection
					return Mathf.Sqrt(sqrDistanceToA) - sphereRadius;
				}
				if (centerProjection > segmentLength + Geometry.Epsilon)
				{
					// No intersection
					return (sphereCenter - segmentB).Length() - sphereRadius;
				}
				// Point intersection
				return 0;
			}

			// Line intersection
			float distanceToIntersection = Mathf.Sqrt(sqrDistanceToIntersection);
			float distanceA = centerProjection - distanceToIntersection;
			float distanceB = centerProjection + distanceToIntersection;

			bool pointAIsAfterSegmentA = distanceA > -Geometry.Epsilon;
			bool pointBIsBeforeSegmentB = distanceB < segmentLength + Geometry.Epsilon;

			if (pointAIsAfterSegmentA && pointBIsBeforeSegmentB)
			{
				// Two points intersection
				return 0;
			}
			if (!pointAIsAfterSegmentA && !pointBIsBeforeSegmentB)
			{
				// The segment is inside, but no intersection
				distanceB = -(distanceB - segmentLength);
				return distanceA > distanceB ? distanceA : distanceB;
			}

			bool pointAIsBeforeSegmentB = distanceA < segmentLength + Geometry.Epsilon;
			if (pointAIsAfterSegmentA && pointAIsBeforeSegmentB)
			{
				// Point A intersection
				return 0;
			}
			bool pointBIsAfterSegmentA = distanceB > -Geometry.Epsilon;
			if (pointBIsAfterSegmentA && pointBIsBeforeSegmentB)
			{
				// Point B intersection
				return 0;
			}

			// No intersection
			if (centerProjection < 0)
			{
				return Mathf.Sqrt(sqrDistanceToA) - sphereRadius;
			}
			return (sphereCenter - segmentB).Length() - sphereRadius;
		}

		#endregion Segment-Sphere

		#region Sphere-Sphere

		/// <summary>
		/// Returns the distance between the closest points on the spheres
		/// </summary>
		/// <returns>
		/// Positive value if the spheres do not intersect, negative otherwise.
		/// Negative value can be interpreted as depth of penetration.
		/// </returns>
		public static float SphereSphere(Sphere sphereA, Sphere sphereB)
		{
			return SphereSphere(sphereA.center, sphereA.radius, sphereB.center, sphereB.radius);
		}

		/// <summary>
		/// Returns the distance between the closest points on the spheres
		/// </summary>
		/// <returns>
		/// Positive value if the spheres do not intersect, negative otherwise.
		/// Negative value can be interpreted as depth of penetration.
		/// </returns>
		public static float SphereSphere(Vector3 centerA, float radiusA, Vector3 centerB, float radiusB)
		{
			return centerA.DistanceTo(centerB) - radiusA - radiusB;
		}

		#endregion Sphere-Sphere
	}
}
