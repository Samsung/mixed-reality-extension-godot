// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.PluginInterfaces;

//FIXME
namespace MixedRealityExtension.API
{
	/// <summary>
	/// Static class that serves as the Mixed Reality Extension SDK API.
	/// </summary>
	public static class MREAPI
	{

		/// <summary>
		/// Gets the apps API for the Mixed Reality Extension SDK.
		/// </summary>
		public static MREAppsAPI AppsAPI { get; } = new MREAppsAPI();

		/// <summary>
		/// Gets the logger to use within the MRE SDK.
		/// </summary>
		public static IMRELogger Logger { get; private set; }

		// TODO @tombu - Re-visit this with the upcoming user design and implementation.
		//public static MWIUsersAPI UsersAPI { get; } = new MWIUsersAPI();
	}

	/// <summary>
	/// Class that contains the mixed reality extension application API.
	/// </summary>
	public class MREAppsAPI
	{
		internal IPermissionManager PermissionManager { get; set; }
	}
}
