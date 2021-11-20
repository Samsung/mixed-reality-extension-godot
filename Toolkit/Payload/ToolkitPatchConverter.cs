// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.UI;
using MixedRealityExtension.API;
using MixedRealityExtension.Core;
using MixedRealityExtension.Patching.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace MixedRealityExtension.Messaging.Payloads.Converters
{
	/// <summary>
	/// Json converter for collision geometry serialization data.
	/// </summary>
	public class ToolkitPatchConverter : JsonConverter
	{
		/// <inheritdoc />
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(ToolkitPatch);
		}

		/// <inheritdoc />
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			try
			{
				JObject jObject = JObject.Load(reader);
				var toolkitType = jObject["type"].ToObject<string>();

				ToolkitPatch toolkitPatch = null;
				switch (toolkitType)
				{
					case "button":
						toolkitPatch = new ButtonPatch();
						toolkitPatch.ToolkitType = typeof(PressableButtonGodot);
						break;
					case "toggle-button":
						toolkitPatch = new ToggleButtonPatch();
						toolkitPatch.ToolkitType = typeof(TogglePressableButtonGodot);
						break;
					case "pinch-slider":
						toolkitPatch = new PinchSliderPatch();
						toolkitPatch.ToolkitType = typeof(PinchSlider);
						break;
					case "pinch-slider-thumb":
						toolkitPatch = new PinchSliderThumbPatch();
						toolkitPatch.ToolkitType = typeof(PinchSliderThumb);
						break;
					default:
						MREAPI.Logger.LogError($"Failed to deserialize toolkit patch.  Invalid toolkit type <{toolkitType}>.");
						break;
				}

				serializer.Populate(jObject.CreateReader(), toolkitPatch);

				return toolkitPatch;
			}
			catch (Exception e)
			{
				MREAPI.Logger.LogError($"Failed to create toolkit patch from json.  Exception: {e.Message}\nStack Trace: {e.StackTrace}");
				throw;
			}
		}

		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(value);
		}
	}
}
