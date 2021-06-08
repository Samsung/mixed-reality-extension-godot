using System.IO;

namespace MixedRealityExtension.Util.GodotHelper
{
	internal interface DownloadHandler
	{
		void ParseData(MemoryStream stream);
	}
}