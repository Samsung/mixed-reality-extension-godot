// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using Godot;
using MixedRealityExtension.App;
using MixedRealityExtension.PluginInterfaces;
using System;
using Godot.Collections;

public class DialogFactory : MeshInstance, IDialogFactory
{
	//[SerializeField] private UnityStandardAssets.Characters.FirstPerson.FirstPersonController controller;
	[Export] private NodePath inputSource;
	private Assets.Scripts.User.InputSource inputSourceNode;
	private ConfirmationDialog label;
	private TextEdit input;
	private Viewport viewport;
	private CollisionShape collisionShape;

	private class DialogQueueEntry
	{
		public string text;
		public bool allowInput;
		public Action<bool, string> callback;
	}

	private Queue<DialogQueueEntry> queue = new Queue<DialogQueueEntry>(3);
	private DialogQueueEntry activeDialog;

	public override void _Ready()
	{
		inputSourceNode = GetNode<Assets.Scripts.User.InputSource>(inputSource);
		label = GetNode<ConfirmationDialog>("Viewport/ConfirmationDialog");
		input = GetNode<TextEdit>("Viewport/ConfirmationDialog/Node2D/TextEdit");
		viewport = GetNode<Viewport>("Viewport");

		label.AnchorTop = 0.5f;
		label.AnchorLeft = 0.5f;
		label.AnchorRight = 0.5f;
		label.AnchorBottom = 0.5f;

		label.GetCancel().Connect("pressed", this, nameof(OnCancel));
		label.Connect("confirmed", this, nameof(OnOk));
		label.Connect("popup_hide", this, nameof(OnPopupHide));

		var area = new Area();
		collisionShape = new CollisionShape();
		collisionShape.Disabled = true;
		collisionShape.Shape = new BoxShape()
		{
			Extents = new Vector3(0.5f, 0.5f, 0.001f),
		};
		area.AddChild(collisionShape);
		AddChild(area);

		HideDialog();
	}

	public override void _Process(float delta)
	{
		if (Visible && Input.IsActionJustReleased("Fire1"))
		{
			Vector3 collisionPoint = Vector3.Zero;
			Dictionary RayIntersectionResult = inputSourceNode.IntersectRay();
			if (RayIntersectionResult.Count > 0)
			{
				collisionPoint = (Vector3)RayIntersectionResult["position"];
			}
			collisionPoint = ToLocal(collisionPoint);

			InputEventMouseButton inputEvent = new InputEventMouseButton();
			inputEvent.Position = new Vector2((collisionPoint.x + 0.5f) * 1000, (collisionPoint.y - 0.5f) * -1000);
			inputEvent.Pressed = true;
			inputEvent.Factor = 1;
			inputEvent.ButtonIndex = 1;

			viewport.Input(inputEvent);
			inputEvent.Pressed = false;
			viewport.Input(inputEvent);
		}
	}
	public void ShowDialog(IMixedRealityExtensionApp app, string text, bool acceptInput, Action<bool, string> callback)
	{
		queue.Enqueue(new DialogQueueEntry() { text = text, allowInput = acceptInput, callback = callback });
		ProcessQueue();
	}

	private void ProcessQueue()
	{
		if (queue.Count == 0) return;

		activeDialog = queue.Dequeue();
		label.DialogText = activeDialog.text;

		if (activeDialog.allowInput)
		{
			label.DialogText += "\n\n";
			input.Visible = true;
		}

		collisionShape.Disabled = false;

		if (label.IsConnected("resized", this, nameof(onResized)))
			label.Disconnect("resized", this, nameof(onResized));
		label.Connect("resized", this, nameof(onResized));
		label.Popup_();

		GlobalTransform = new Transform(inputSourceNode.GlobalTransform.basis, inputSourceNode.ToGlobal(new Vector3(0, 0, -0.5f)));
		Visible = true;
	}

	private void OnOk()
	{
		try
		{
			activeDialog.callback?.Invoke(true, activeDialog.allowInput ? input.Text : null);
		}
		catch (Exception e)
		{
			GD.PushError(e.ToString());
		}
		finally
		{
			activeDialog = null;
		}

		HideDialog();
	}

	private void OnCancel()
	{
		try
		{
			activeDialog.callback?.Invoke(false, null);
		}
		catch (Exception e)
		{
			GD.PushError(e.ToString());
		}
		finally
		{
			activeDialog = null;
		}

		HideDialog();
	}

	private void OnPopupHide()
	{
		HideDialog();
	}

	private void HideDialog()
	{
		collisionShape.Disabled = true;
		Visible = false;
		input.Visible = false;
		ProcessQueue();
	}

	private void onResized()
	{
		label.Disconnect("resized", this, nameof(onResized));

		var rectHalf = label.RectSize / 2;
		label.MarginTop = -rectHalf.y;
		label.MarginBottom = -rectHalf.y;
		label.MarginLeft = -rectHalf.x;
		label.MarginRight = -rectHalf.x;

		input.RectSize = new Vector2(label.RectSize.x, input.RectSize.y);

		var lines = label.DialogText.Split('\n');
		var margin = (lines.Length - 1.6f) * label.Theme.DefaultFont.GetHeight();
		input.MarginBottom = margin + label.Theme.DefaultFont.GetHeight() + 40;
		input.MarginTop = margin;
	}
}
