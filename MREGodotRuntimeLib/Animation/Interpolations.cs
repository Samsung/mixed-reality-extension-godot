﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using CubicBezier = MixedRealityExtension.Util.CubicBezier;
using Newtonsoft.Json.Linq;
using Quaternion = Godot.Quaternion;
using GodotMath = Godot.Mathf;

namespace MixedRealityExtension.Animation
{
	internal static class Interpolations
	{
		private static Quaternion tempA;
		private static Quaternion tempB;
		private static Quaternion tempMix;

		internal static void Interpolate(JToken a, JToken b, float linearT, ref JToken mix, CubicBezier easing)
		{
			var easedT = easing.Sample(linearT);

			// compound types
			if (a.Type == JTokenType.Object)
			{
				JObject A = (JObject)a;
				JObject B = (JObject)b;
				JObject Mix = (JObject)mix;

				if (A.ContainsKey("x") && A.ContainsKey("y"))
				{
					if (A.ContainsKey("z"))
					{
						// quaternion
						if (A.ContainsKey("w"))
						{
							tempA = new Quaternion(A.ForceFloat("x"), A.ForceFloat("y"), A.ForceFloat("z"), A.ForceFloat("w"));
							tempB = new Quaternion(B.ForceFloat("x"), B.ForceFloat("y"), B.ForceFloat("z"), B.ForceFloat("w"));
							tempMix = tempA.Slerp(tempB, easedT);
							Mix.SetOrAdd("x", tempMix.X);
							Mix.SetOrAdd("y", tempMix.Y);
							Mix.SetOrAdd("z", tempMix.Z);
							Mix.SetOrAdd("w", tempMix.W);
						}
						// Vector3
						else
						{
							Mix.SetOrAdd("x", GodotMath.Lerp(A.ForceFloat("x"), B.ForceFloat("x"), easedT));
							Mix.SetOrAdd("y", GodotMath.Lerp(A.ForceFloat("y"), B.ForceFloat("y"), easedT));
							Mix.SetOrAdd("z", GodotMath.Lerp(A.ForceFloat("z"), B.ForceFloat("z"), easedT));
						}
					}
					// Vector2
					else
					{
						Mix.SetOrAdd("x", GodotMath.Lerp(A.ForceFloat("x"), B.ForceFloat("x"), easedT));
						Mix.SetOrAdd("y", GodotMath.Lerp(A.ForceFloat("y"), B.ForceFloat("y"), easedT));
					}
				}
				// Color3
				else if (A.ContainsKey("r") && A.ContainsKey("g") && A.ContainsKey("b"))
				{
					Mix.SetOrAdd("r", GodotMath.Lerp(A.ForceFloat("r"), B.ForceFloat("r"), easedT));
					Mix.SetOrAdd("g", GodotMath.Lerp(A.ForceFloat("g"), B.ForceFloat("g"), easedT));
					Mix.SetOrAdd("b", GodotMath.Lerp(A.ForceFloat("b"), B.ForceFloat("b"), easedT));
					// Color4
					if (A.ContainsKey("a"))
					{
						Mix.SetOrAdd("a", GodotMath.Lerp(A.ForceFloat("a"), B.ForceFloat("a"), easedT));
					}
				}
				// TODO: other compound types (color3, color4)
			}
			// simple types
			else
			{
				JValue A = (JValue)a;
				JValue B = (JValue)b;
				JValue Mix = (JValue)mix;

				// numeric types
				if (a.Type == JTokenType.Float || a.Type == JTokenType.Integer)
				{
					Mix.Value = GodotMath.Lerp(A.ForceFloat(), B.ForceFloat(), easedT);
				}
				// no interpolation available, just use A
				else
				{
					Mix.Value = A.Value;
				}
			}
		}

		internal static void ResolveRelativeValue(JToken reference, JToken relative, ref JToken result)
		{
			// compound types
			if (reference.Type == JTokenType.Object)
			{
				JObject Reference = (JObject)reference;
				JObject Relative = (JObject)relative;
				JObject Result = (JObject)result;

				if (Reference.ContainsKey("x") && Reference.ContainsKey("y"))
				{
					if (Reference.ContainsKey("z"))
					{
						// quaternion
						if (Reference.ContainsKey("w"))
						{
							tempA = new Quaternion(
								Reference.ForceFloat("x"),
								Reference.ForceFloat("y"),
								Reference.ForceFloat("z"),
								Reference.ForceFloat("w"));
							tempB = new Quaternion(
								Relative.ForceFloat("x"),
								Relative.ForceFloat("y"),
								Relative.ForceFloat("z"),
								Relative.ForceFloat("w"));
							// equivalent to applying rotations in sequence: reference, then relative
							tempMix = tempA * tempB;
							Result.SetOrAdd("x", tempMix.X);
							Result.SetOrAdd("y", tempMix.Y);
							Result.SetOrAdd("z", tempMix.Z);
							Result.SetOrAdd("w", tempMix.W);
						}
						// Vector3
						else
						{
							Result.SetOrAdd("x", Reference.ForceFloat("x") + Relative.ForceFloat("x"));
							Result.SetOrAdd("y", Reference.ForceFloat("y") + Relative.ForceFloat("y"));
							Result.SetOrAdd("z", Reference.ForceFloat("z") + Relative.ForceFloat("z"));
						}
					}
					// Vector2
					else
					{
						Result.SetOrAdd("x", Reference.ForceFloat("x") + Relative.ForceFloat("x"));
						Result.SetOrAdd("y", Reference.ForceFloat("y") + Relative.ForceFloat("y"));
					}
				}
				// TODO: other compound types (color3, color4)
			}
			// simple types
			else
			{
				var Reference = (JValue)reference;
				var Relative = (JValue)relative;
				var Result = (JValue)result;

				// numeric types
				if (Reference.Type == JTokenType.Float || Reference.Type == JTokenType.Integer)
				{
					Result.Value = Reference.ForceFloat() + Relative.ForceFloat();
				}
				// can't blend types
				else
				{
					Result.Value = Reference.Value;
				}
			}
		}

		internal static void SetOrAdd(this JObject _this, string key, object value)
		{
			if (_this.ContainsKey(key))
			{
				((JValue)_this[key]).Value = value;
			}
			else
			{
				_this.Add(new JProperty(key, value));
			}
		}

		internal static float ForceFloat(this JToken _this, string name = null)
		{
			if (_this.Type == JTokenType.Object && !string.IsNullOrEmpty(name))
			{
				var child = ((JObject)_this).GetValue(name);
				return child.ForceFloat();
			}
			else
			{
				var This = (JValue)_this;
				if (This.Value.GetType() != typeof(double))
				{
					// for some reason Json.net really doesn't want to store a float here
					This.Value = This.Value<double>();
				}
				return (float)(double)This.Value; // weird but necessary
			}
		}
	}
}
