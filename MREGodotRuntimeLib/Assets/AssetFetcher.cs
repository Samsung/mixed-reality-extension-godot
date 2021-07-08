// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MixedRealityExtension.Util.GodotHelper;

namespace MixedRealityExtension.Assets
{
	public static class AssetFetcher<T> where T : Godot.Object
	{
		public struct FetchResult
		{
			public T Asset;
			public string FailureMessage;
			public long ReturnCode;
			public string ETag;

			public bool IsPopulated => ReturnCode != 0;
		}

		public static async Task<FetchResult> LoadTask(Node runner, Uri uri)
		{
			// acquire the exclusive right to load this asset
			if (!await MREAPI.AppsAPI.AssetCache.AcquireLoadingLock(uri))
			{
				throw new TimeoutException("Failed to acquire exclusive loading rights for " + uri);
			}

			FetchResult result = new FetchResult()
			{
				Asset = null,
				FailureMessage = null
			};
			var ifNoneMatch = MREAPI.AppsAPI.AssetCache.SupportsSync
				? MREAPI.AppsAPI.AssetCache.TryGetVersionSync(uri)
				: await MREAPI.AppsAPI.AssetCache.TryGetVersion(uri);

			// if the cached version is unversioned, i.e. the server doesn't support ETags, don't bother making request
			if (ifNoneMatch == Constants.UnversionedAssetVersion)
			{
				var assets = MREAPI.AppsAPI.AssetCache.SupportsSync
					? MREAPI.AppsAPI.AssetCache.LeaseAssetsSync(uri)
					: await MREAPI.AppsAPI.AssetCache.LeaseAssets(uri);
				result.Asset = assets.FirstOrDefault() as T;

				MREAPI.AppsAPI.AssetCache.ReleaseLoadingLock(uri);
				return result;
			}

			await LoadCoroutine(runner);

			// handle caching
			if (!string.IsNullOrEmpty(ifNoneMatch) && result.ReturnCode == 304)
			{
				var assets = MREAPI.AppsAPI.AssetCache.SupportsSync
					? MREAPI.AppsAPI.AssetCache.LeaseAssetsSync(uri)
					: await MREAPI.AppsAPI.AssetCache.LeaseAssets(uri);
				result.Asset = assets.FirstOrDefault() as T;
			}
			else if (result.Asset != null)
			{
				MREAPI.AppsAPI.AssetCache.StoreAssets(
					uri,
					new Godot.Object[] { result.Asset },
					result.ETag);
			}

			MREAPI.AppsAPI.AssetCache.ReleaseLoadingLock(uri);
			return result;

			async Task LoadCoroutine(Node runner)
			{
				DownloadHandler handler;
				if (typeof(T) == typeof(Godot.AudioStream))
				{
					handler = new DownloadHandlerAudioStream(uri, AudioType.Unknown);
				}
				else if (typeof(T) == typeof(Godot.Texture))
				{
					handler = new DownloadHandlerTexture(uri);
				}
				else
				{
					result.FailureMessage = $"Unknown download type: {typeof(T)}";
					return;
				}

				// Spin asynchronously until the load throttler would allow us through.
				while (AssetLoadThrottling.WouldThrottle())
				{
					await runner.ToSignal(Engine.GetMainLoop(), "idle_frame");
				}

				using (var scope = new AssetLoadThrottling.AssetLoadScope())
				using (var www = new GodotWebRequest(uri, HTTPClient.Method.Get, handler))
				{
					if (!string.IsNullOrEmpty(ifNoneMatch))
					{
						www.SetRequestHeader("If-None-Match", ifNoneMatch);
					}

					Error error = www.SendWebRequest();
					if (error != 0)
					{
						result.ReturnCode = -1;
						result.FailureMessage = error.ToString();
					}
					else
					{
						result.ReturnCode = www.GetResponseCode();
						result.ETag = www.GetResponseHeader("ETag") ?? Constants.UnversionedAssetVersion;

						if (result.ReturnCode >= 200 && result.ReturnCode <= 299)
						{
							if (typeof(T).IsAssignableFrom(typeof(AudioStream)))
							{
								result.Asset = ((DownloadHandlerAudioStream)handler).AudioStream as T;
							}
							else if (typeof(T).IsAssignableFrom(typeof(Godot.Texture)))
							{
								result.Asset = ((DownloadHandlerTexture)handler).Texture as T;
							}
						}
						else
						{
							result.FailureMessage = $"[{result.ReturnCode}] {uri}";
						}
					}
				}
			}
		}
	}
}
