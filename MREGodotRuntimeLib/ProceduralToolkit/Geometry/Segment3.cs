using System;
using Godot;

namespace MixedRealityExtension.ProceduralToolkit
{
	/// <summary>
	/// Representation of a 3D line segment
	/// </summary>
	[Serializable]
	public struct Segment3 : IEquatable<Segment3>, IFormattable
	{
		public Vector3 a;
		public Vector3 b;

		/// <summary>
		/// Returns the normalized direction of the segment
		/// </summary>
		public Vector3 direction { get { return (b - a).Normalized(); } }
		/// <summary>
		/// Returns the length of the segment
		/// </summary>
		public float length { get { return (b - a).Length(); } }

		public Segment3(Vector3 a, Vector3 b)
		{
			this.a = a;
			this.b = b;
		}

		/// <summary>
		/// Access the a or b component using [0] or [1] respectively
		/// </summary>
		public Vector3 this[int index]
		{
			get
			{
				switch (index)
				{
					case 0: return a;
					case 1: return b;
					default:
						throw new IndexOutOfRangeException("Invalid Segment3 index!");
				}
			}
			set
			{
				switch (index)
				{
					case 0:
						a = value;
						break;
					case 1:
						b = value;
						break;
					default:
						throw new IndexOutOfRangeException("Invalid Segment3 index!");
				}
			}
		}

		/// <summary>
		/// Returns a point on the segment at the given normalized position
		/// </summary>
		public Vector3 GetPoint(float position)
		{
			return a.LinearInterpolate(b, Mathf.Clamp(position, 0, 1));
		}

		/// <summary>
		/// Linearly interpolates between two segments
		/// </summary>
		public static Segment3 Lerp(Segment3 a, Segment3 b, float t)
		{
			t = Mathf.Clamp(t, 0, 1);
			return new Segment3(a.a + (b.a - a.a)*t, a.b + (b.b - a.b)*t);
		}

		/// <summary>
		/// Linearly interpolates between two segments without clamping the interpolant
		/// </summary>
		public static Segment3 LerpUnclamped(Segment3 a, Segment3 b, float t)
		{
			return new Segment3(a.a + (b.a - a.a)*t, a.b + (b.b - a.b)*t);
		}

		#region Casting operators

		public static explicit operator Line3(Segment3 segment)
		{
			return new Line3(segment.a, (segment.b - segment.a).Normalized());
		}

		public static explicit operator Ray(Segment3 segment)
		{
			return new Ray(segment.a, (segment.b - segment.a).Normalized());
		}

		public static explicit operator Segment2(Segment3 segment)
		{
			return new Segment2(new Vector2(segment.a.x, segment.a.y), new Vector2(segment.b.x, segment.b.y));
		}

		#endregion Casting operators

		public static Segment3 operator +(Segment3 segment, Vector3 vector)
		{
			return new Segment3(segment.a + vector, segment.b + vector);
		}

		public static Segment3 operator -(Segment3 segment, Vector3 vector)
		{
			return new Segment3(segment.a - vector, segment.b - vector);
		}

		public static bool operator ==(Segment3 a, Segment3 b)
		{
			return a.a == b.a && a.b == b.b;
		}

		public static bool operator !=(Segment3 a, Segment3 b)
		{
			return !(a == b);
		}

		public override int GetHashCode()
		{
			return a.GetHashCode() ^ (b.GetHashCode() << 2);
		}

		public override bool Equals(object other)
		{
			return other is Segment3 && Equals((Segment3) other);
		}

		public bool Equals(Segment3 other)
		{
			return a.Equals(other.a) && b.Equals(other.b);
		}

		public override string ToString()
		{
			return string.Format("Segment3(a: {0}, b: {1})", a, b);
		}

		public string ToString(string format)
		{
			return string.Format("Segment3(a: {0}, b: {1})", a.ToString(format), b.ToString(format));
		}

		public string ToString(string format, IFormatProvider formatProvider)
		{
			return string.Format("Segment3(a: {0}, b: {1})", a.ToString(format, formatProvider), b.ToString(format, formatProvider));
		}
	}
}
