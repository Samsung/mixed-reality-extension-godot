using MixedRealityExtension.Toolkit.Payloads;
using MixedRealityExtension.Messaging.Commands;
using System;
using Godot;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using MixedRealityExtension.App;
using MixedRealityExtension.Util.GodotHelper;

namespace MixedRealityExtension.Toolkit
{
	public class MixedRealityExtensionToolkit : ICommandHandlerContext
	{
		private readonly static Dictionary<Type, PackedScene> packedToolkitScene = new Dictionary<Type, PackedScene>()
		{
			{ typeof(PressableButtonGodot), ResourceLoader.Load<PackedScene>("res://Toolkit/PressableButtonGodot.tscn") },
			{ typeof(TogglePressableButtonGodot), ResourceLoader.Load<PackedScene>("res://Toolkit/TogglePressableButtonGodot.tscn") },
			{ typeof(PinchSlider), ResourceLoader.Load<PackedScene>("res://Toolkit/PinchSlider.tscn") },
			{ typeof(PinchSliderThumb), ResourceLoader.Load<PackedScene>("res://Toolkit/PinchSliderThumb.tscn") },
			{ typeof(ScrollingObjectCollection), ResourceLoader.Load<PackedScene>("res://Toolkit/ScrollingObjectCollection.tscn") }
		};

		public IMixedRealityExtensionApp App { get; private set; }

		public MixedRealityExtensionToolkit(IMixedRealityExtensionApp app)
		{
			App = app;
		}

		[CommandHandler(typeof(CreateFromToolkit))]
		private void OnCreateFromToolkit(CreateFromToolkit payload, Action onCompleteCallback)
		{
			try
			{
				var actor = (Spatial)App.FindActor(payload.ActorId);
				var toolkit = packedToolkitScene[payload.Toolkit.ToolkitType].Instance();
				actor.AddChild(toolkit);
				((IToolkit)toolkit).ApplyPatch(payload.Toolkit);
			}
			catch (Exception e)
			{
				GD.PushError(e.ToString());
			}
		}

		[CommandHandler(typeof(ToolkitUpdate))]
		private void OnToolkitUpdate(ToolkitUpdate payload, Action onCompleteCallback)
		{
			try
			{
				var actor = (Spatial)App.FindActor(payload.ActorId);
				var toolkit = actor.GetChild<IToolkit>();
				toolkit.ApplyPatch(payload.Toolkit);
			}
			catch (Exception e)
			{
				GD.PushError(e.ToString());
			}
		}
	}
}