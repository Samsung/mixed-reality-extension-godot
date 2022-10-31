// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using MixedRealityExtension.App;
using MixedRealityExtension.Messaging.Commands;
using Newtonsoft.Json;

namespace MixedRealityExtension
{
	public class MREPlugin : ICommandHandlerContext
	{
		internal protected Type TypeRegistry { get; set; }

		internal protected IList<JsonConverter> JsonConverters { get; set; }

		protected IMixedRealityExtensionApp App { get; private set; }

		public MREPlugin(IMixedRealityExtensionApp app)
		{
			App = app;
		}
	}
}
