using Godot;

namespace MixedRealityExtension.ProceduralToolkit
{
	/// <summary>
	/// Collection of distance calculation algorithms
	/// </summary>
	public static partial class Distance
	{
		#region Point-Line

		/// <summary>
		/// Returns a distance to the closest point on the line
		/// </summary>
		public static float PointLine(Vector2 point, Line2 line)
		{
			return point.DistanceTo(Closest.PointLine(point, line));
		}

		/// <summary>
		/// Returns a distance to the closest point on the line
		/// </summary>
		public static float PointLine(Vector2 point, Vector2 lineOrigin, Vector2 lineDirection)
		{
			return point.DistanceTo(Closest.PointLine(point, lineOrigin, lineDirection));
		}

		#endregion Point-Line

		#region Point-Ray

		/// <summary>
		/// Returns a distance to the closest point on the ray
		/// </summary>
		public static float PointRay(Vector2 point, Ray2D ray)
		{
			return point.DistanceTo(Closest.PointRay(point, ray));
		}

		/// <summary>
		/// Returns a distance to the closest point on the ray
		/// </summary>
		/// <param name="rayDirection">Normalized direction of the ray</param>
		public static float PointRay(Vector2 point, Vector2 rayOrigin, Vector2 rayDirection)
		{
			return point.DistanceTo(Closest.PointRay(point, rayOrigin, rayDirection));
		}

		#endregion Point-Ray

		#region Point-Segment

		/// <summary>
		/// Returns a distance to the closest point on the segment
		/// </summary>
		public static float PointSegment(Vector2 point, Segment2 segment)
		{
			return point.DistanceTo(Closest.PointSegment(point, segment));
		}

		/// <summary>
		/// Returns a distance to the closest point on the segment
		/// </summary>
		public static float PointSegment(Vector2 point, Vector2 segmentA, Vector2 segmentB)
		{
			return point.DistanceTo(Closest.PointSegment(point, segmentA, segmentB));
		}

		private static float PointSegment(Vector2 point, Vector2 segmentA, Vector2 segmentB, Vector2 segmentDirection, float segmentLength)
		{
			float pointProjection = segmentDirection.Dot(point - segmentA);
			if (pointProjection < -Geometry.Epsilon)
			{
				return point.DistanceTo(segmentA);
			}
			if (pointProjection > segmentLength + Geometry.Epsilon)
			{
				return point.DistanceTo(segmentB);
			}
			return point.DistanceTo(segmentA + segmentDirection*pointProjection);
		}

		#endregion Point-Segment

		#region Point-Circle

		/// <summary>
		/// Returns a distance to the closest point on the circle
		/// </summary>
		/// <returns>Positive value if the point is outside, negative otherwise</returns>
		public static float PointCircle(Vector2 point, Circle2 circle)
		{
			return PointCircle(point, circle.center, circle.radius);
		}

		/// <summary>
		/// Returns a distance to the closest point on the circle
		/// </summary>
		/// <returns>Positive value if the point is outside, negative otherwise</returns>
		public static float PointCircle(Vector2 point, Vector2 circleCenter, float circleRadius)
		{
			return (circleCenter - point).Length() - circleRadius;
		}

		#endregion Point-Circle

		#region Line-Line

		/// <summary>
		/// Returns the distance between the closest points on the lines
		/// </summary>
		public static float LineLine(Line2 lineA, Line2 lineB)
		{
			return LineLine(lineA.origin, lineA.direction, lineB.origin, lineB.direction);
		}

		/// <summary>
		/// Returns the distance between the closest points on the lines
		/// </summary>
		public static float LineLine(Vector2 originA, Vector2 directionA, Vector2 originB, Vector2 directionB)
		{
			if (Mathf.Abs(VectorE.PerpDot(directionA, directionB)) < Geometry.Epsilon)
			{
				// Parallel
				Vector2 originBToA = originA - originB;
				if (Mathf.Abs(VectorE.PerpDot(directionA, originBToA)) > Geometry.Epsilon ||
					Mathf.Abs(VectorE.PerpDot(directionB, originBToA)) > Geometry.Epsilon)
				{
					// Not collinear
					float originBProjection = directionA.Dot(originBToA);
					float distanceSqr = originBToA.LengthSquared() - originBProjection*originBProjection;
					// distanceSqr can be negative
					return distanceSqr <= 0 ? 0 : Mathf.Sqrt(distanceSqr);
				}

				// Collinear
				return 0;
			}

			// Not parallel
			return 0;
		}

		#endregion Line-Line

		#region Line-Ray

		/// <summary>
		/// Returns the distance between the closest points on the line and the ray
		/// </summary>
		public static float LineRay(Line2 line, Ray2D ray)
		{
			return LineRay(line.origin, line.direction, ray.origin, ray.direction);
		}

		/// <summary>
		/// Returns the distance between the closest points on the line and the ray
		/// </summary>
		public static float LineRay(Vector2 lineOrigin, Vector2 lineDirection, Vector2 rayOrigin, Vector2 rayDirection)
		{
			Vector2 rayOriginToLineOrigin = lineOrigin - rayOrigin;
			float denominator = VectorE.PerpDot(lineDirection, rayDirection);
			float perpDotA = VectorE.PerpDot(lineDirection, rayOriginToLineOrigin);

			if (Mathf.Abs(denominator) < Geometry.Epsilon)
			{
				// Parallel
				float perpDotB = VectorE.PerpDot(rayDirection, rayOriginToLineOrigin);
				if (Mathf.Abs(perpDotA) > Geometry.Epsilon || Mathf.Abs(perpDotB) > Geometry.Epsilon)
				{
					// Not collinear
					float rayOriginProjection = lineDirection.Dot(rayOriginToLineOrigin);
					float distanceSqr = rayOriginToLineOrigin.LengthSquared() - rayOriginProjection*rayOriginProjection;
					// distanceSqr can be negative
					return distanceSqr <= 0 ? 0 : Mathf.Sqrt(distanceSqr);
				}
				// Collinear
				return 0;
			}

			// Not parallel
			float rayDistance = perpDotA/denominator;
			if (rayDistance < -Geometry.Epsilon)
			{
				// No intersection
				float rayOriginProjection = lineDirection.Dot(rayOriginToLineOrigin);
				Vector2 linePoint = lineOrigin - lineDirection*rayOriginProjection;
				return linePoint.DistanceTo(rayOrigin);
			}
			// Point intersection
			return 0;
		}

		#endregion Line-Ray

		#region Line-Segment

		/// <summary>
		/// Returns the distance between the closest points on the line and the segment
		/// </summary>
		public static float LineSegment(Line2 line, Segment2 segment)
		{
			return LineSegment(line.origin, line.direction, segment.a, segment.b);
		}

		/// <summary>
		/// Returns the distance between the closest points on the line and the segment
		/// </summary>
		public static float LineSegment(Vector2 lineOrigin, Vector2 lineDirection, Vector2 segmentA, Vector2 segmentB)
		{
			Vector2 segmentAToOrigin = lineOrigin - segmentA;
			Vector2 segmentDirection = segmentB - segmentA;
			float denominator = VectorE.PerpDot(lineDirection, segmentDirection);
			float perpDotA = VectorE.PerpDot(lineDirection, segmentAToOrigin);

			if (Mathf.Abs(denominator) < Geometry.Epsilon)
			{
				// Parallel
				// Normalized direction gives more stable results 
				float perpDotB = VectorE.PerpDot(segmentDirection.Normalized(), segmentAToOrigin);
				if (Mathf.Abs(perpDotA) > Geometry.Epsilon || Mathf.Abs(perpDotB) > Geometry.Epsilon)
				{
					// Not collinear
					float segmentAProjection = lineDirection.Dot(segmentAToOrigin);
					float distanceSqr = segmentAToOrigin.LengthSquared() - segmentAProjection*segmentAProjection;
					// distanceSqr can be negative
					return distanceSqr <= 0 ? 0 : Mathf.Sqrt(distanceSqr);
				}
				// Collinear
				return 0;
			}

			// Not parallel
			float segmentDistance = perpDotA/denominator;
			if (segmentDistance < -Geometry.Epsilon || segmentDistance > 1 + Geometry.Epsilon)
			{
				// No intersection
				Vector2 segmentPoint = segmentA + segmentDirection*Mathf.Clamp(segmentDistance, 0, 1);
				float segmentPointProjection = lineDirection.Dot(segmentPoint - lineOrigin);
				Vector2 linePoint = lineOrigin + lineDirection*segmentPointProjection;
				return linePoint.DistanceTo(segmentPoint);
			}
			// Point intersection
			return 0;
		}

		#endregion Line-Segment

		#region Line-Circle

		/// <summary>
		/// Returns the distance between the closest points on the line and the circle
		/// </summary>
		public static float LineCircle(Line2 line, Circle2 circle)
		{
			return LineCircle(line.origin, line.direction, circle.center, circle.radius);
		}

		/// <summary>
		/// Returns the distance between the closest points on the line and the circle
		/// </summary>
		public static float LineCircle(Vector2 lineOrigin, Vector2 lineDirection, Vector2 circleCenter, float circleRadius)
		{
			Vector2 originToCenter = circleCenter - lineOrigin;
			float centerProjection = lineDirection.Dot(originToCenter);
			float sqrDistanceToLine = originToCenter.LengthSquared() - centerProjection*centerProjection;
			float sqrDistanceToIntersection = circleRadius*circleRadius - sqrDistanceToLine;
			if (sqrDistanceToIntersection < -Geometry.Epsilon)
			{
				// No intersection
				return Mathf.Sqrt(sqrDistanceToLine) - circleRadius;
			}
			return 0;
		}

		#endregion Line-Circle

		#region Ray-Ray

		/// <summary>
		/// Returns the distance between the closest points on the rays
		/// </summary>
		public static float RayRay(Ray2D rayA, Ray2D rayB)
		{
			return RayRay(rayA.origin, rayA.direction, rayB.origin, rayB.direction);
		}

		/// <summary>
		/// Returns the distance between the closest points on the rays
		/// </summary>
		public static float RayRay(Vector2 originA, Vector2 directionA, Vector2 originB, Vector2 directionB)
		{
			Vector2 originBToA = originA - originB;
			float denominator = VectorE.PerpDot(directionA, directionB);
			float perpDotA = VectorE.PerpDot(directionA, originBToA);
			float perpDotB = VectorE.PerpDot(directionB, originBToA);

			bool codirected = directionA.Dot(directionB) > 0;
			if (Mathf.Abs(denominator) < Geometry.Epsilon)
			{
				// Parallel
				float originBProjection = -directionA.Dot(originBToA);
				if (Mathf.Abs(perpDotA) > Geometry.Epsilon || Mathf.Abs(perpDotB) > Geometry.Epsilon)
				{
					// Not collinear
					if (!codirected && originBProjection < Geometry.Epsilon)
					{
						return originA.DistanceTo(originB);
					}
					float distanceSqr = originBToA.LengthSquared() - originBProjection*originBProjection;
					// distanceSqr can be negative
					return distanceSqr <= 0 ? 0 : Mathf.Sqrt(distanceSqr);
				}
				// Collinear

				if (codirected)
				{
					// Ray intersection
					return 0;
				}
				else
				{
					if (originBProjection < Geometry.Epsilon)
					{
						// No intersection
						return originA.DistanceTo(originB);
					}
					else
					{
						// Segment intersection
						return 0;
					}
				}
			}

			// Not parallel
			float distanceA = perpDotB/denominator;
			float distanceB = perpDotA/denominator;
			if (distanceA < -Geometry.Epsilon || distanceB < -Geometry.Epsilon)
			{
				// No intersection
				if (codirected)
				{
					float originAProjection = directionB.Dot(originBToA);
					if (originAProjection > -Geometry.Epsilon)
					{
						Vector2 rayPointA = originA;
						Vector2 rayPointB = originB + directionB*originAProjection;
						return rayPointA.DistanceTo(rayPointB);
					}
					float originBProjection = -directionA.Dot(originBToA);
					if (originBProjection > -Geometry.Epsilon)
					{
						Vector2 rayPointA = originA + directionA*originBProjection;
						Vector2 rayPointB = originB;
						return rayPointA.DistanceTo(rayPointB);
					}
					return originA.DistanceTo(originB);
				}
				else
				{
					if (distanceA > -Geometry.Epsilon)
					{
						float originBProjection = -directionA.Dot(originBToA);
						if (originBProjection > -Geometry.Epsilon)
						{
							Vector2 rayPointA = originA + directionA*originBProjection;
							Vector2 rayPointB = originB;
							return rayPointA.DistanceTo(rayPointB);
						}
					}
					else if (distanceB > -Geometry.Epsilon)
					{
						float originAProjection = directionB.Dot(originBToA);
						if (originAProjection > -Geometry.Epsilon)
						{
							Vector2 rayPointA = originA;
							Vector2 rayPointB = originB + directionB*originAProjection;
							return rayPointA.DistanceTo(rayPointB);
						}
					}
					return originA.DistanceTo(originB);
				}
			}
			// Point intersection
			return 0;
		}

		#endregion Ray-Ray

		#region Ray-Segment

		/// <summary>
		/// Returns the distance between the closest points on the ray and the segment
		/// </summary>
		public static float RaySegment(Ray2D ray, Segment2 segment)
		{
			return RaySegment(ray.origin, ray.direction, segment.a, segment.b);
		}

		/// <summary>
		/// Returns the distance between the closest points on the ray and the segment
		/// </summary>
		public static float RaySegment(Vector2 rayOrigin, Vector2 rayDirection, Vector2 segmentA, Vector2 segmentB)
		{
			Vector2 segmentAToOrigin = rayOrigin - segmentA;
			Vector2 segmentDirection = segmentB - segmentA;
			float denominator = VectorE.PerpDot(rayDirection, segmentDirection);
			float perpDotA = VectorE.PerpDot(rayDirection, segmentAToOrigin);
			// Normalized direction gives more stable results 
			float perpDotB = VectorE.PerpDot(segmentDirection.Normalized(), segmentAToOrigin);

			if (Mathf.Abs(denominator) < Geometry.Epsilon)
			{
				// Parallel
				float segmentAProjection = -rayDirection.Dot(segmentAToOrigin);
				Vector2 originToSegmentB = segmentB - rayOrigin;
				float segmentBProjection = rayDirection.Dot(originToSegmentB);
				if (Mathf.Abs(perpDotA) > Geometry.Epsilon || Mathf.Abs(perpDotB) > Geometry.Epsilon)
				{
					// Not collinear
					if (segmentAProjection > -Geometry.Epsilon)
					{
						float distanceSqr = segmentAToOrigin.LengthSquared() - segmentAProjection*segmentAProjection;
						// distanceSqr can be negative
						return distanceSqr <= 0 ? 0 : Mathf.Sqrt(distanceSqr);
					}
					if (segmentBProjection > -Geometry.Epsilon)
					{
						float distanceSqr = originToSegmentB.LengthSquared() - segmentBProjection*segmentBProjection;
						// distanceSqr can be negative
						return distanceSqr <= 0 ? 0 : Mathf.Sqrt(distanceSqr);
					}

					if (segmentAProjection > segmentBProjection)
					{
						return rayOrigin.DistanceTo(segmentA);
					}
					return rayOrigin.DistanceTo(segmentB);
				}
				// Collinear
				if (segmentAProjection > -Geometry.Epsilon || segmentBProjection > -Geometry.Epsilon)
				{
					// Point or segment intersection
					return 0;
				}
				// No intersection
				return segmentAProjection > segmentBProjection ? -segmentAProjection : -segmentBProjection;
			}

			// Not parallel
			float rayDistance = perpDotB/denominator;
			float segmentDistance = perpDotA/denominator;
			if (rayDistance < -Geometry.Epsilon ||
				segmentDistance < -Geometry.Epsilon || segmentDistance > 1 + Geometry.Epsilon)
			{
				// No intersection
				bool codirected = rayDirection.Dot(segmentDirection) > 0;
				Vector2 segmentBToOrigin;
				if (!codirected)
				{
					PTUtils.Swap(ref segmentA, ref segmentB);
					segmentDirection = -segmentDirection;
					segmentBToOrigin = segmentAToOrigin;
					segmentAToOrigin = rayOrigin - segmentA;
					segmentDistance = 1 - segmentDistance;
				}
				else
				{
					segmentBToOrigin = rayOrigin - segmentB;
				}

				float segmentAProjection = -rayDirection.Dot(segmentAToOrigin);
				float segmentBProjection = -rayDirection.Dot(segmentBToOrigin);
				bool segmentAOnRay = segmentAProjection > -Geometry.Epsilon;
				bool segmentBOnRay = segmentBProjection > -Geometry.Epsilon;
				if (segmentAOnRay && segmentBOnRay)
				{
					if (segmentDistance < 0)
					{
						Vector2 rayPoint = rayOrigin + rayDirection*segmentAProjection;
						Vector2 segmentPoint = segmentA;
						return rayPoint.DistanceTo(segmentPoint);
					}
					else
					{
						Vector2 rayPoint = rayOrigin + rayDirection*segmentBProjection;
						Vector2 segmentPoint = segmentB;
						return rayPoint.DistanceTo(segmentPoint);
					}
				}
				else if (!segmentAOnRay && segmentBOnRay)
				{
					if (segmentDistance < 0)
					{
						Vector2 rayPoint = rayOrigin;
						Vector2 segmentPoint = segmentA;
						return rayPoint.DistanceTo(segmentPoint);
					}
					else if (segmentDistance > 1 + Geometry.Epsilon)
					{
						Vector2 rayPoint = rayOrigin + rayDirection*segmentBProjection;
						Vector2 segmentPoint = segmentB;
						return rayPoint.DistanceTo(segmentPoint);
					}
					else
					{
						Vector2 rayPoint = rayOrigin;
						float originProjection = segmentDirection.Dot(segmentAToOrigin);
						Vector2 segmentPoint = segmentA + segmentDirection*originProjection/segmentDirection.LengthSquared();
						return rayPoint.DistanceTo(segmentPoint);
					}
				}
				else
				{
					// Not on ray
					Vector2 rayPoint = rayOrigin;
					float originProjection = segmentDirection.Dot(segmentAToOrigin);
					float sqrSegmentLength = segmentDirection.LengthSquared();
					if (originProjection < 0)
					{
						return rayPoint.DistanceTo(segmentA);
					}
					else if (originProjection > sqrSegmentLength)
					{
						return rayPoint.DistanceTo(segmentB);
					}
					else
					{
						Vector2 segmentPoint = segmentA + segmentDirection*originProjection/sqrSegmentLength;
						return rayPoint.DistanceTo(segmentPoint);
					}
				}
			}
			// Point intersection
			return 0;
		}

		#endregion Ray-Segment

		#region Ray-Circle

		/// <summary>
		/// Returns the distance between the closest points on the ray and the circle
		/// </summary>
		public static float RayCircle(Ray2D ray, Circle2 circle)
		{
			return RayCircle(ray.origin, ray.direction, circle.center, circle.radius);
		}

		/// <summary>
		/// Returns the distance between the closest points on the ray and the circle
		/// </summary>
		public static float RayCircle(Vector2 rayOrigin, Vector2 rayDirection, Vector2 circleCenter, float circleRadius)
		{
			Vector2 originToCenter = circleCenter - rayOrigin;
			float centerProjection = rayDirection.Dot(originToCenter);
			if (centerProjection + circleRadius < -Geometry.Epsilon)
			{
				// No intersection
				return Mathf.Sqrt(originToCenter.LengthSquared()) - circleRadius;
			}

			float sqrDistanceToOrigin = originToCenter.LengthSquared();
			float sqrDistanceToLine = sqrDistanceToOrigin - centerProjection*centerProjection;
			float sqrDistanceToIntersection = circleRadius*circleRadius - sqrDistanceToLine;
			if (sqrDistanceToIntersection < -Geometry.Epsilon)
			{
				// No intersection
				if (centerProjection < -Geometry.Epsilon)
				{
					return Mathf.Sqrt(sqrDistanceToOrigin) - circleRadius;
				}
				return Mathf.Sqrt(sqrDistanceToLine) - circleRadius;
			}
			if (sqrDistanceToIntersection < Geometry.Epsilon)
			{
				if (centerProjection < -Geometry.Epsilon)
				{
					// No intersection
					return Mathf.Sqrt(sqrDistanceToOrigin) - circleRadius;
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
					return Mathf.Sqrt(sqrDistanceToOrigin) - circleRadius;
				}

				// Point intersection;
				return 0;
			}

			// Two points intersection;
			return 0;
		}

		#endregion Ray-Circle

		#region Segment-Segment

		/// <summary>
		/// Returns the distance between the closest points on the segments
		/// </summary>
		public static float SegmentSegment(Segment2 segment1, Segment2 segment2)
		{
			return SegmentSegment(segment1.a, segment1.b, segment2.a, segment2.b);
		}

		/// <summary>
		/// Returns the distance between the closest points on the segments
		/// </summary>
		public static float SegmentSegment(Vector2 segment1A, Vector2 segment1B, Vector2 segment2A, Vector2 segment2B)
		{
			Vector2 from2ATo1A = segment1A - segment2A;
			Vector2 direction1 = segment1B - segment1A;
			Vector2 direction2 = segment2B - segment2A;
			float segment1Length = direction1.Length();
			float segment2Length = direction2.Length();

			bool segment1IsAPoint = segment1Length < Geometry.Epsilon;
			bool segment2IsAPoint = segment2Length < Geometry.Epsilon;
			if (segment1IsAPoint && segment2IsAPoint)
			{
				return segment1A.DistanceTo(segment2A);
			}
			if (segment1IsAPoint)
			{
				direction2.Normalized();
				return PointSegment(segment1A, segment2A, segment2B, direction2, segment2Length);
			}
			if (segment2IsAPoint)
			{
				direction1.Normalized();
				return PointSegment(segment2A, segment1A, segment1B, direction1, segment1Length);
			}

			direction1.Normalized();
			direction2.Normalized();
			float denominator = VectorE.PerpDot(direction1, direction2);
			float perpDot1 = VectorE.PerpDot(direction1, from2ATo1A);
			float perpDot2 = VectorE.PerpDot(direction2, from2ATo1A);

			if (Mathf.Abs(denominator) < Geometry.Epsilon)
			{
				// Parallel
				if (Mathf.Abs(perpDot1) > Geometry.Epsilon || Mathf.Abs(perpDot2) > Geometry.Epsilon)
				{
					// Not collinear
					float segment2AProjection = -direction1.Dot(from2ATo1A);
					if (segment2AProjection > -Geometry.Epsilon &&
						segment2AProjection < segment1Length + Geometry.Epsilon)
					{
						float distanceSqr = from2ATo1A.LengthSquared() - segment2AProjection*segment2AProjection;
						// distanceSqr can be negative
						return distanceSqr <= 0 ? 0 : Mathf.Sqrt(distanceSqr);
					}

					Vector2 from1ATo2B = segment2B - segment1A;
					float segment2BProjection = direction1.Dot(from1ATo2B);
					if (segment2BProjection > -Geometry.Epsilon &&
						segment2BProjection < segment1Length + Geometry.Epsilon)
					{
						float distanceSqr = from1ATo2B.LengthSquared() - segment2BProjection*segment2BProjection;
						// distanceSqr can be negative
						return distanceSqr <= 0 ? 0 : Mathf.Sqrt(distanceSqr);
					}

					if (segment2AProjection < 0 && segment2BProjection < 0)
					{
						if (segment2AProjection > segment2BProjection)
						{
							return segment1A.DistanceTo(segment2A);
						}
						return segment1A.DistanceTo(segment2B);
					}
					if (segment2AProjection > 0 && segment2BProjection > 0)
					{
						if (segment2AProjection < segment2BProjection)
						{
							return segment1B.DistanceTo(segment2A);
						}
						return segment1B.DistanceTo(segment2B);
					}
					float segment1AProjection = direction2.Dot(from2ATo1A);
					Vector2 segment2Point = segment2A + direction2*segment1AProjection;
					return segment1A.DistanceTo(segment2Point);
				}
				// Collinear

				bool codirected = direction1.Dot(direction2) > 0;
				if (codirected)
				{
					// Codirected
					float segment2AProjection = -direction1.Dot(from2ATo1A);
					if (segment2AProjection > -Geometry.Epsilon)
					{
						// 1A------1B
						//     2A------2B
						return SegmentSegmentCollinear(segment1A, segment1B, segment2A);
					}
					else
					{
						//     1A------1B
						// 2A------2B
						return SegmentSegmentCollinear(segment2A, segment2B, segment1A);
					}
				}
				else
				{
					// Contradirected
					float segment2BProjection = direction1.Dot(segment2B - segment1A);
					if (segment2BProjection > -Geometry.Epsilon)
					{
						// 1A------1B
						//     2B------2A
						return SegmentSegmentCollinear(segment1A, segment1B, segment2B);
					}
					else
					{
						//     1A------1B
						// 2B------2A
						return SegmentSegmentCollinear(segment2B, segment2A, segment1A);
					}
				}
			}

			// Not parallel
			float distance1 = perpDot2/denominator;
			float distance2 = perpDot1/denominator;
			if (distance1 < -Geometry.Epsilon || distance1 > segment1Length + Geometry.Epsilon ||
				distance2 < -Geometry.Epsilon || distance2 > segment2Length + Geometry.Epsilon)
			{
				// No intersection
				bool codirected = direction1.Dot(direction2) > 0;
				Vector2 from1ATo2B;
				if (!codirected)
				{
					PTUtils.Swap(ref segment2A, ref segment2B);
					direction2 = -direction2;
					from1ATo2B = -from2ATo1A;
					from2ATo1A = segment1A - segment2A;
					distance2 = segment2Length - distance2;
				}
				else
				{
					from1ATo2B = segment2B - segment1A;
				}
				Vector2 segment1Point;
				Vector2 segment2Point;

				float segment2AProjection = -direction1.Dot(from2ATo1A);
				float segment2BProjection = direction1.Dot(from1ATo2B);

				bool segment2AIsAfter1A = segment2AProjection > -Geometry.Epsilon;
				bool segment2BIsBefore1B = segment2BProjection < segment1Length + Geometry.Epsilon;
				bool segment2AOnSegment1 = segment2AIsAfter1A && segment2AProjection < segment1Length + Geometry.Epsilon;
				bool segment2BOnSegment1 = segment2BProjection > -Geometry.Epsilon && segment2BIsBefore1B;
				if (segment2AOnSegment1 && segment2BOnSegment1)
				{
					if (distance2 < -Geometry.Epsilon)
					{
						segment1Point = segment1A + direction1*segment2AProjection;
						segment2Point = segment2A;
					}
					else
					{
						segment1Point = segment1A + direction1*segment2BProjection;
						segment2Point = segment2B;
					}
				}
				else if (!segment2AOnSegment1 && !segment2BOnSegment1)
				{
					if (!segment2AIsAfter1A && !segment2BIsBefore1B)
					{
						segment1Point = distance1 < -Geometry.Epsilon ? segment1A : segment1B;
					}
					else
					{
						// Not on segment
						segment1Point = segment2AIsAfter1A ? segment1B : segment1A;
					}
					float segment1PointProjection = direction2.Dot(segment1Point - segment2A);
					segment1PointProjection = Mathf.Clamp(segment1PointProjection, 0, segment2Length);
					segment2Point = segment2A + direction2*segment1PointProjection;
				}
				else if (segment2AOnSegment1)
				{
					if (distance2 < -Geometry.Epsilon)
					{
						segment1Point = segment1A + direction1*segment2AProjection;
						segment2Point = segment2A;
					}
					else
					{
						segment1Point = segment1B;
						float segment1PointProjection = direction2.Dot(segment1Point - segment2A);
						segment1PointProjection = Mathf.Clamp(segment1PointProjection, 0, segment2Length);
						segment2Point = segment2A + direction2*segment1PointProjection;
					}
				}
				else
				{
					if (distance2 > segment2Length + Geometry.Epsilon)
					{
						segment1Point = segment1A + direction1*segment2BProjection;
						segment2Point = segment2B;
					}
					else
					{
						segment1Point = segment1A;
						float segment1PointProjection = direction2.Dot(segment1Point - segment2A);
						segment1PointProjection = Mathf.Clamp(segment1PointProjection, 0, segment2Length);
						segment2Point = segment2A + direction2*segment1PointProjection;
					}
				}
				return segment1Point.DistanceTo(segment2Point);
			}

			// Point intersection
			return 0;
		}

		private static float SegmentSegmentCollinear(Vector2 leftA, Vector2 leftB, Vector2 rightA)
		{
			Vector2 leftDirection = leftB - leftA;
			float rightAProjection = leftDirection.Normalized().Dot(rightA - leftB);
			if (Mathf.Abs(rightAProjection) < Geometry.Epsilon)
			{
				// LB == RA
				// LA------LB
				//         RA------RB

				// Point intersection
				return 0;
			}
			if (rightAProjection < 0)
			{
				// LB > RA
				// LA------LB
				//     RARB
				//     RA--RB
				//     RA------RB

				// Segment intersection
				return 0;
			}
			// LB < RA
			// LA------LB
			//             RA------RB

			// No intersection
			return rightAProjection;
		}

		#endregion Segment-Segment

		#region Segment-Circle

		/// <summary>
		/// Returns the distance between the closest points on the segment and the circle
		/// </summary>
		public static float SegmentCircle(Segment2 segment, Circle2 circle)
		{
			return SegmentCircle(segment.a, segment.b, circle.center, circle.radius);
		}

		/// <summary>
		/// Returns the distance between the closest points on the segment and the circle
		/// </summary>
		public static float SegmentCircle(Vector2 segmentA, Vector2 segmentB, Vector2 circleCenter, float circleRadius)
		{
			Vector2 segmentAToCenter = circleCenter - segmentA;
			Vector2 fromAtoB = segmentB - segmentA;
			float segmentLength = fromAtoB.Length();
			if (segmentLength < Geometry.Epsilon)
			{
				return segmentAToCenter.Length() - circleRadius;
			}

			Vector2 segmentDirection = fromAtoB.Normalized();
			float centerProjection = segmentDirection.Dot(segmentAToCenter);
			if (centerProjection + circleRadius < -Geometry.Epsilon ||
				centerProjection - circleRadius > segmentLength + Geometry.Epsilon)
			{
				// No intersection
				if (centerProjection < 0)
				{
					return segmentAToCenter.Length() - circleRadius;
				}
				return (circleCenter - segmentB).Length() - circleRadius;
			}

			float sqrDistanceToA = segmentAToCenter.LengthSquared();
			float sqrDistanceToLine = sqrDistanceToA - centerProjection*centerProjection;
			float sqrDistanceToIntersection = circleRadius*circleRadius - sqrDistanceToLine;
			if (sqrDistanceToIntersection < -Geometry.Epsilon)
			{
				// No intersection
				if (centerProjection < -Geometry.Epsilon)
				{
					return Mathf.Sqrt(sqrDistanceToA) - circleRadius;
				}
				if (centerProjection > segmentLength + Geometry.Epsilon)
				{
					return (circleCenter - segmentB).Length() - circleRadius;
				}
				return Mathf.Sqrt(sqrDistanceToLine) - circleRadius;
			}

			if (sqrDistanceToIntersection < Geometry.Epsilon)
			{
				if (centerProjection < -Geometry.Epsilon)
				{
					// No intersection
					return Mathf.Sqrt(sqrDistanceToA) - circleRadius;
				}
				if (centerProjection > segmentLength + Geometry.Epsilon)
				{
					// No intersection
					return (circleCenter - segmentB).Length() - circleRadius;
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
				return Mathf.Sqrt(sqrDistanceToA) - circleRadius;
			}
			return (circleCenter - segmentB).Length() - circleRadius;
		}

		#endregion Segment-Circle

		#region Circle-Circle

		/// <summary>
		/// Returns the distance between the closest points on the circles
		/// </summary>
		/// <returns>
		/// Positive value if the circles do not intersect, negative otherwise.
		/// Negative value can be interpreted as depth of penetration.
		/// </returns>
		public static float CircleCircle(Circle2 circleA, Circle2 circleB)
		{
			return CircleCircle(circleA.center, circleA.radius, circleB.center, circleB.radius);
		}

		/// <summary>
		/// Returns the distance between the closest points on the circles
		/// </summary>
		/// <returns>
		/// Positive value if the circles do not intersect, negative otherwise.
		/// Negative value can be interpreted as depth of penetration.
		/// </returns>
		public static float CircleCircle(Vector2 centerA, float radiusA, Vector2 centerB, float radiusB)
		{
			return centerA.DistanceTo(centerB) - radiusA - radiusB;
		}

		#endregion Circle-Circle
	}
}
