// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.App;
using MixedRealityExtension.Behaviors.ActionData;
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.Messaging.Events.Types;
using MixedRealityExtension.Messaging.Payloads;
using System;

namespace MixedRealityExtension.Behaviors.Actions
{
	public class ActionHandler : IActionHandler
	{
		private readonly WeakReference<IMixedRealityExtensionApp> _appRef;

		public string ActionName { get; }

		public Guid AttachedActorId { get; }

		public ActionHandler(
			string actionName,
			WeakReference<IMixedRealityExtensionApp> appRef,
			Guid attachedActorId)
		{
			_appRef = appRef;
			ActionName = actionName;
			AttachedActorId = attachedActorId;
		}

		void IActionHandler.HandleActionPerforming(IUser user, BaseActionData actionData)
		{
			((IActionHandler)this).HandleActionStateChanged(user, ActionState.Performing, ActionState.Performing, actionData);
		}

		void IActionHandler.HandleActionStateChanged(
			IUser user,
			ActionState oldState,
			ActionState newState,
			BaseActionData actionData)
		{
			IMixedRealityExtensionApp app;
			if (!_appRef.TryGetTarget(out app))
			{
				return;
			}

			if (!app.IsInteractableForUser(user))
			{
				return;
			}

			var actionPerformed = new ActionPerformed()
			{
				UserId = user.Id,
				TargetId = AttachedActorId,
				ActionName = ActionName,
				ActionState = newState,
				ActionData = actionData
			};

			if (app is MixedRealityExtensionApp mreApp)
				mreApp.EventManager.QueueLateEvent(new BehaviorEvent(actionPerformed));
		}
	}
}
