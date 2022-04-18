// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections;
using System.Threading.Tasks;
using Godot;

namespace MixedRealityExtension.Util.GodotHelper
{
	internal delegate void ContinuationHandler(object result);

	internal class MWContinuation
	{
		public object Result;
		private readonly Node _owner;
		private readonly IEnumerator _task;
		private readonly ContinuationHandler _then;

		public MWContinuation(Node owner, IEnumerator task, ContinuationHandler then = null)
		{
			_owner = owner;
			_task = task;
			_then = then;
		}

		public void Start()
		{
			Run();
		}

		private async void Run()
		{
			await Task.Run(() => _then?.Invoke(Result));
		}
	}
}
