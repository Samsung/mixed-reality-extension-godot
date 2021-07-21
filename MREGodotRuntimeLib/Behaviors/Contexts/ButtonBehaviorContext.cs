﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Behaviors.Actions;
using MixedRealityExtension.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MixedRealityExtension.Behaviors.Contexts
{
	public class ButtonBehaviorContext : TargetBehaviorContext
	{
		private MWAction<ButtonData> _hoverAction = new MWAction<ButtonData>();
		private MWAction<ButtonData> _clickAction = new MWAction<ButtonData>();
		private MWAction<ButtonData> _buttonAction = new MWAction<ButtonData>();
		private List<Vector3> _buttonPressedPoints = new List<Vector3>();
		private List<Vector3> _hoverPoints = new List<Vector3>();

		public bool IsPressed { get; private set; } 

		public void StartHover(IUser user, Vector3 hoverPoint)
		{
			var app = App;
			if (app == null)
			{
				return;
			}

			_hoverAction.StartAction(user, new ButtonData()
			{
				targetedPoints = new PointData[1]
				{
					PointData.CreateFromGodotVector3(hoverPoint, Behavior.Actor.node as Spatial, app.SceneRoot)
				}
			});
		}

		public void EndHover(IUser user, Vector3 hoverPoint)
		{
			var app = App;
			if (app == null)
			{
				return;
			}

			_hoverAction.StopAction(user, new ButtonData()
			{
				targetedPoints = new PointData[1]
				{
					PointData.CreateFromGodotVector3(hoverPoint, Behavior.Actor.node as Spatial, app.SceneRoot)
				}
			});
		}

		public void StartButton(IUser user, Vector3 buttonStartPoint)
		{
			var app = App;
			if (app == null)
			{
				return;
			}

			_buttonAction.StartAction(user, new ButtonData()
			{
				targetedPoints = new PointData[1]
				{
					PointData.CreateFromGodotVector3(buttonStartPoint, Behavior.Actor.node as Spatial, app.SceneRoot)
				}
			});
			IsPressed = true;
		}

		public void EndButton(IUser user, Vector3 buttonEndPoint)
		{
			var app = App;
			if (app == null)
			{
				return;
			}

			_buttonAction.StopAction(user, new ButtonData()
			{
				targetedPoints = new PointData[1]
				{
					PointData.CreateFromGodotVector3(buttonEndPoint, Behavior.Actor.node as Spatial, app.SceneRoot)
				}
			});
			IsPressed = false;
		}

		public void Click(IUser user, Vector3 clickPoint)
		{
			var app = App;
			if (app == null)
			{
				return;
			}

			_clickAction.StartAction(user, new ButtonData()
			{
				targetedPoints = new PointData[1]
				{
					PointData.CreateFromGodotVector3(clickPoint, Behavior.Actor.node as Spatial, app.SceneRoot)
				}
			});
		}

		internal ButtonBehaviorContext()
		{
			
		}

		internal override void SynchronizeBehavior()
		{
			base.SynchronizeBehavior();

			var app = App;
			if (app == null)
			{
				return;
			}

			if (_hoverPoints.Any())
			{
				_hoverAction.PerformActionUpdate(new ButtonData()
				{
					targetedPoints = _hoverPoints.Select((point) =>
					{
						return PointData.CreateFromGodotVector3(point, Behavior.Actor.node as Spatial, app.SceneRoot);
					}).ToArray()
				});

				_hoverPoints.Clear();
			}

			if (_buttonPressedPoints.Any())
			{
				_buttonAction.PerformActionUpdate(new ButtonData()
				{
					targetedPoints = _buttonPressedPoints.Select((point) =>
					{
						return PointData.CreateFromGodotVector3(point, Behavior.Actor.node as Spatial, app.SceneRoot);
					}).ToArray()
				});

				_buttonPressedPoints.Clear();
			}
		}

		protected override void OnTargetPointUpdated(Vector3 targetPoint)
		{
			if (IsPressed)
			{
				_buttonPressedPoints.Add(targetPoint);
			}
			else
			{
				_hoverPoints.Add(targetPoint);
			}
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();
			RegisterActionHandler(_hoverAction, "hover");
			RegisterActionHandler(_clickAction, "click");
			RegisterActionHandler(_buttonAction, "button");
		}
	}
}
