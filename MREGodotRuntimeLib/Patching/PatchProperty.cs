﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace MixedRealityExtension.Patching
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PatchProperty : Attribute
	{
		public PatchProperty()
		{
		}
	}
}
