// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using Godot;

namespace MREGodotRuntimeLib.Core.UnitTest
{
	[TestFixture]
	public class TestTemplate
	{
		[SetUp]
		public void Setup()
		{

		}

		[TearDown]
		public void TearDown()
		{

		}

		[Test]
		public void Test1()
		{
			Assert.Pass();
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		public void MultipleCasesTest(int cases)
		{
			Assert.Pass();
		}

		[Test]
		public void GodotTest()
		{
			var node = new Node();
			Assert.IsNotNull(node);
		}
	}
}
