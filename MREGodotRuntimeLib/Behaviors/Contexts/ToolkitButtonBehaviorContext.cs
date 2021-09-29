// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using Godot;

namespace MixedRealityExtension.Behaviors.Contexts
{
	public class ToolkitButtonBehaviorContext : TargetBehaviorContext
	{		
		private MWAction<ToolkitButtonData> _clickAction = new MWAction<ToolkitButtonData>();

		public void Click(IUser user, Vector3 clickPoint)
		{
			var app = App;
			if (app == null)
			{
				return;
			}

			_clickAction.StartAction(user, new ToolkitButtonData()
			{
				targetedPoints = new PointData[1]
				{
					PointData.CreateFromGodotVector3(clickPoint, Behavior.Actor.Node3D as Spatial, app.SceneRoot)
				}
			});
		}

		internal ToolkitButtonBehaviorContext()
		{

		}

		internal override void SynchronizeBehavior()
		{
			base.SynchronizeBehavior();
			// If there is some data to be updated while action is performing, add here.
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();
			// If "click" would give confusion with ButtonBehavior's click - needs to define new name.
			RegisterActionHandler(_clickAction, "click");
		}
	}
}