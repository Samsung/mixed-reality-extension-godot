// Copyright (c) Samsung Electronics Co., Ltd. All rights reserved.
// Licensed under the MIT License.

using NUnit.Framework.Interfaces;

namespace MREGodotRuntimeLib.Core.UnitTest
{
	// TODO: This class is created temprarily to prevent runtime exception of unit test. so, its implementation is empty.
	//       The implementations can be added later if needed.
	public class TestFilter : ITestFilter
	{
		public TNode AddToXml(TNode parentNode, bool recursive) => null;
		public TNode ToXml(bool recursive) => null;

		public bool IsExplicitMatch(ITest test) => true;
		public bool Pass(ITest test) => true;
	}
}
