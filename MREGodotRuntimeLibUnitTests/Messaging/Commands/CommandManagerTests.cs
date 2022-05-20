// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework;
using Godot;
using System.Collections.Generic;
using System;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;

namespace MREGodotRuntimeLib.Messaging.Commands.UnitTest
{
	[TestFixture]
	public class CommandManagerTests
	{
		[OneTimeSetUp]
		public void Init()
		{
			PayloadTypeRegistry.RegisterPayloadType(typeof(TestPayloadTypeRegistry));
		}

		[Test]
		public void TestExecuteCommandPayloadWithCommandHandlerContextTarget()
		{
			var commandHandler = new CommandHandlerContextTestClass();
			var commandManager = new CommandManager(new Dictionary<Type, ICommandHandlerContext>()
			{
				{ typeof(CommandHandlerContextTestClass), commandHandler }
			});
			Assert.Throws<Exception>(() => commandManager.ExecuteCommandPayload(new TestPayloadWithoutHandler(), null));
			commandManager.ExecuteCommandPayload(new TestPayload(), null);
			Assert.AreEqual(commandHandler.TestProperty, "Tested Property");
		}

		[Test]
		public void TestExecuteCommandPayloadWithoutCommandHandlerContextTarget()
		{
			var commandHandler = new CommandHandlerContextTestClass();
			var payload = new TestPayload();
			var commandManager = new CommandManager(new Dictionary<Type, ICommandHandlerContext>()
			{
				{ typeof(CommandHandlerContextTestClass), null }
			});
			Assert.Throws<Exception>(() => commandManager.ExecuteCommandPayload(payload, null));
			commandManager.ExecuteCommandPayload(commandHandler, payload, null);
			Assert.AreEqual(commandHandler.TestProperty, "Tested Property");
		}

		[Test]
		public void TestOnCompleteCallback()
		{
			var commandHandler = new CommandHandlerContextTestClass();
			var payload = new TestPayload();
			var commandManager = new CommandManager(new Dictionary<Type, ICommandHandlerContext>()
			{
				{ typeof(CommandHandlerContextTestClass), null }
			});
			var calledCompleteCallback = false;
			commandManager.ExecuteCommandPayload(commandHandler, payload, () =>
			{
				calledCompleteCallback = true;
			});
			Assert.True(calledCompleteCallback);
		}

		class CommandHandlerContextTestClass : ICommandHandlerContext
		{
			public string TestProperty;

			[CommandHandler(typeof(TestPayload))]
			private void OnTestPayload(TestPayload payload, Action onCompleteCallback)
			{
				TestProperty = "Tested Property";
				onCompleteCallback?.Invoke();
			}
		}

		class TestPayload : NetworkCommandPayload
		{


		}

		class TestPayloadWithoutHandler : NetworkCommandPayload
		{

		}

		[PayloadType(typeof(TestPayload), "testpayload")]
		[PayloadType(typeof(TestPayloadWithoutHandler), "testpayload-without-handler")]
		class TestPayloadTypeRegistry
		{
		}
	}
}
