// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using Godot;
using NUnit.Framework.Interfaces;

namespace MREGodotRuntimeLib.Core.UnitTest
{
	// TODO: This class is created temprarily to prevent runtime exception of unit test. so, its implementation is empty.
	//       The implementations can be added later if needed.
	public class TestListener : ITestListener
	{
		public void SendMessage(TestMessage message)
		{
			//TODO
		}

		public void TestFinished(ITestResult result)
		{
			//TODO
			GD.Print("Name : " + result.Name + ", ResultState : " + result.ResultState);
		}

		public void TestOutput(TestOutput output)
		{
			//TODO
		}

		public void TestStarted(ITest test)
		{
			//TODO
		}
	}
}
