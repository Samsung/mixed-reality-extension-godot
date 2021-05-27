// Licensed under the MIT License.
using MixedRealityExtension.Core.Interfaces;
using MixedRealityExtension.PluginInterfaces;

public class SimpleTextFactory : ITextFactory
{
	public IText CreateText(IActor actor)
	{
		return new SimpleText(actor);
	}
}
