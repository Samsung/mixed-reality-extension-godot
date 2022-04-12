// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using Godot;
using NUnit.Framework.Api;
using System.Collections.Generic;
using System.Reflection;

namespace MREGodotRuntimeLib.Core.UnitTest
{
	public class Main : Spatial
	{
		public override void _Ready()
		{
			var frameworkController = new FrameworkController(
				Assembly.GetExecutingAssembly(),
				"MREGodotRuntimeLibUnitTests",
				new Dictionary<string, object>()
			);
			frameworkController.LoadTests();
			frameworkController.Runner.Run(new TestListener(), new TestFilter());
			GetTree().Quit();
		}
	}
}