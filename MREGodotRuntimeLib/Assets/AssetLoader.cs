// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.Animation;
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using MixedRealityExtension.Core;
using MixedRealityExtension.Core.Types;
using MixedRealityExtension.Messaging;
using MixedRealityExtension.Messaging.Commands;
using MixedRealityExtension.Messaging.Payloads;
using MixedRealityExtension.Patching.Types;
using MixedRealityExtension.Util;
using MixedRealityExtension.Util.GodotHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MWMaterial = MixedRealityExtension.Assets.Material;
using MWTexture = MixedRealityExtension.Assets.Texture;
using MWMesh = MixedRealityExtension.Assets.Mesh;
using MWSound = MixedRealityExtension.Assets.Sound;
using MWVideoStream = MixedRealityExtension.Assets.VideoStream;
using GodotGLTF.Loader;
using GodotGLTF;

namespace MixedRealityExtension.Assets
{
	using LoaderFunction = Func<AssetSource, Guid, ColliderType, Task<IList<Asset>>>;

	internal class AssetLoader : ICommandHandlerContext
	{
		private readonly MixedRealityExtensionApp _app;

		private readonly Node _owner;

		//private readonly AsyncCoroutineHelper _asyncHelper;

		public readonly HashSet<Guid> ActiveContainers = new HashSet<Guid>();

		internal AssetLoader(Node owner, MixedRealityExtensionApp app)
		{
			_owner = owner ?? throw new ArgumentException("Asset loader requires an owner Node script to be assigned to it.");
			_app = app ?? throw new ArgumentException("Asset loader requires a MixedRealityExtensionApp to be associated with.");
			/*
			_asyncHelper = _owner.gameObject.GetComponent<AsyncCoroutineHelper>() ??
						   _owner.gameObject.AddComponent<AsyncCoroutineHelper>();
			*/
		}

		internal Spatial GetGameObjectFromParentId(Guid? parentId)
		{
			var parent = _app.FindActor(parentId ?? Guid.Empty) as Actor;
			return parent?.Node3D ?? _app.SceneRoot;
		}
/*FIXME
		internal async Task<IList<Actor>> CreateFromLibrary(string resourceId, Guid? parentId)
		{
			var factory = MREAPI.AppsAPI.LibraryResourceFactory
				?? throw new ArgumentException("Cannot spawn resource from non-existent library.");

			var spawnedGO = await factory.CreateFromLibrary(resourceId, GetGameObjectFromParentId(parentId));
			spawnedGO.layer = MREAPI.AppsAPI.LayerApplicator.DefaultLayer;
			return new List<Actor>() { spawnedGO.AddComponent<Actor>() };
		}
*/
		internal IList<Actor> CreateEmpty(Guid? parentId)
		{
			Spatial newGO = _app.AssetManager.EmptyTemplate().Duplicate() as Spatial;
			GetGameObjectFromParentId(parentId).AddChild(newGO);

			Actor actor = Actor.Instantiate(newGO);
			return new List<Actor>() { actor };
		}

		internal IList<Actor> CreateFromPrefab(Guid prefabId, Guid? parentId, CollisionLayer? collisionLayer)
		{
			var asset = _app.AssetManager.GetById(prefabId)?.Asset as Node;
			Spatial prefab = asset.Duplicate() as Spatial;

			// restore assigned animation.
			var animationPlayer = asset.GetChild<AnimationPlayer>();
			if (animationPlayer != null)
			{
				var prefabAnimationPlayer = prefab.GetNode<AnimationPlayer>(animationPlayer.Name);
				prefabAnimationPlayer.AssignedAnimation = animationPlayer.AssignedAnimation;
				prefabAnimationPlayer.Play();
			}

			Spatial parent = GetGameObjectFromParentId(parentId);
			parent.AddChild(prefab);
/*
			// copy animation target mapping
			var sourceMap = prefab.GetComponent<PrefabAnimationTargets>();
			var destMap = instance.GetComponent<PrefabAnimationTargets>();
			if (sourceMap != null && destMap != null)
			{
				destMap.AnimationTargets = sourceMap.AnimationTargets;
			}
*/
			// note: actor properties are set in App#ProcessCreatedActors
			var actorList = new List<Actor>();
			MWGOTreeWalker.VisitTree(prefab, go =>
			{
				var collider = go.GetChild<Area>();
				if (collider != null)
				{
					MREAPI.AppsAPI.LayerApplicator.ApplyLayerToCollider(collisionLayer, collider);
				}

				if (go is Spatial)
				{
					var newActor = Actor.Instantiate((Spatial)go);
					actorList.Add(newActor);
				}
			});

			return actorList;
		}

		[CommandHandler(typeof(LoadAssets))]
		private async Task LoadAssets(LoadAssets payload, Action onCompleteCallback)
		{
			LoaderFunction loader;

			switch (payload.Source.ContainerType)
			{
				case AssetContainerType.GLTF:
					loader = LoadAssetsFromGLTF;
					break;
				default:
					throw new Exception(
						$"Cannot load assets from unknown container type {payload.Source.ContainerType}");
			}

			IList<Asset> assets = null;
			string failureMessage = null;

			// attempt to get cached assets instead of loading
			try
			{
				assets = await loader(payload.Source, payload.ContainerId, payload.ColliderType);
				ActiveContainers.Add(payload.ContainerId);
			}
			catch (Exception e)
			{
				failureMessage = UtilMethods.FormatException(
					$"An unexpected error occurred while loading the asset [{payload.Source.Uri}].", e);
			}

			_app.Protocol.Send(new Message()
			{
				ReplyToId = payload.MessageId,
				Payload = new AssetsLoaded()
				{
					FailureMessage = failureMessage,
					Assets = assets?.ToArray()
				}
			});
			onCompleteCallback?.Invoke();
		}

		private async Task<IList<Asset>> LoadAssetsFromGLTF(AssetSource source, Guid containerId, ColliderType colliderType)
		{
			WebRequestLoader loader = null;
			Stream stream = null;
			source.ParsedUri = new Uri(_app.ServerAssetUri, source.ParsedUri);
			var rootUri = URIHelper.GetDirectoryName(source.ParsedUri.AbsoluteUri);

			// acquire the exclusive right to load this asset
			if (!await MREAPI.AppsAPI.AssetCache.AcquireLoadingLock(source.ParsedUri))
			{
				throw new TimeoutException("Failed to acquire exclusive loading rights for " + source.ParsedUri);
			}

			var cachedVersion = MREAPI.AppsAPI.AssetCache.SupportsSync
				? MREAPI.AppsAPI.AssetCache.TryGetVersionSync(source.ParsedUri)
				: await MREAPI.AppsAPI.AssetCache.TryGetVersion(source.ParsedUri);

			// Wait asynchronously until the load throttler lets us through.
			using (var scope = await AssetLoadThrottling.AcquireLoadScope())
			{
				// set up loader
				loader = new WebRequestLoader(rootUri);
				if (cachedVersion != Constants.UnversionedAssetVersion && !string.IsNullOrEmpty(cachedVersion))
				{
					loader.BeforeRequestCallback += (msg) =>
					{
						if (msg.RequestUri == source.ParsedUri)
						{
							msg.Headers.Add("If-None-Match", cachedVersion);
						}
					};
				}

				// download root gltf file, check for cache hit
				try
				{
					stream = await loader.LoadStreamAsync(URIHelper.GetFileFromUri(source.ParsedUri));
					source.Version = loader.LastResponse.Headers.ETag?.Tag ?? Constants.UnversionedAssetVersion;
				}
				catch (HttpRequestException)
				{
					if (loader.LastResponse != null
						&& loader.LastResponse.StatusCode == System.Net.HttpStatusCode.NotModified)
					{
						source.Version = cachedVersion;
					}
					else
					{
						throw;
					}
				}
			}

			IList<Asset> assetDefs = new List<Asset>(30);
			DeterministicGuids guidGenerator = new DeterministicGuids(UtilMethods.StringToGuid(
				$"{containerId}:{source.ParsedUri.AbsoluteUri}"));
			IList<Godot.Object> assets;

			// fetch assets from glTF stream or cache
			if (source.Version != cachedVersion)
			{
				assets = await LoadGltfFromStream(loader, stream, colliderType);
				MREAPI.AppsAPI.AssetCache.StoreAssets(source.ParsedUri, assets, source.Version);
			}
			else
			{
				var assetsEnum = MREAPI.AppsAPI.AssetCache.SupportsSync
					? MREAPI.AppsAPI.AssetCache.LeaseAssetsSync(source.ParsedUri)
					: await MREAPI.AppsAPI.AssetCache.LeaseAssets(source.ParsedUri);
				assets = assetsEnum.ToList();
			}

			// the cache is updated, release the lock
			MREAPI.AppsAPI.AssetCache.ReleaseLoadingLock(source.ParsedUri);

			// catalog assets
			int textureIndex = 0, meshIndex = 0, materialIndex = 0, prefabIndex = 0;
			foreach (var asset in assets)
			{
				var assetDef = GenerateAssetPatch(asset, guidGenerator.Next());

				string internalId = null;
				if (asset is Godot.Texture)
				{
					assetDef.Name = ((Resource)asset).ResourceName;
					internalId = $"texture:{textureIndex++}";
				}
				else if (asset is Godot.Mesh)
				{
					assetDef.Name = ((Resource)asset).ResourceName;
					var arrayMesh = (ArrayMesh)asset;
					var t = arrayMesh.GetAabb();
					internalId = $"mesh:{meshIndex++}";
				}
				else if (asset is Godot.Material)
				{
					assetDef.Name = ((Resource)asset).ResourceName;
					internalId = $"material:{materialIndex++}";
				}
				else if (asset is Node)
				{
					assetDef.Name = ((Node)asset).Name;
					internalId = $"scene:{prefabIndex++}";
				}
				assetDef.Source = new AssetSource(source.ContainerType, source.ParsedUri.AbsoluteUri, internalId, source.Version);

				ColliderGeometry colliderGeo = null;

				if (asset is Godot.Mesh mesh)
				{
					colliderGeo = colliderType == ColliderType.Mesh ?
						(ColliderGeometry)new MeshColliderGeometry() { MeshId = assetDef.Id } :
						(ColliderGeometry)new BoxColliderGeometry()
						{
							Size = (mesh.GetAabb().Size * 0.8f).CreateMWVector3(),
							Center = mesh.GetAabb().Position.CreateMWVector3()
						};
				}

				_app.AssetManager.Set(assetDef.Id, containerId, asset, colliderGeo, assetDef.Source);
				assetDefs.Add(assetDef);
			}

			return assetDefs;
		}

		private async Task<IList<Godot.Object>> LoadGltfFromStream(WebRequestLoader loader, Stream stream, ColliderType colliderType)
		{
			var assets = new List<Godot.Object>(30);

			// pre-parse glTF document so we can get a scene count
			// run this on a threadpool thread so that the Unity main thread is not blocked
			GLTF.Schema.GLTFRoot gltfRoot = null;
			try
			{
				await Task.Run(() =>
				{
					GLTF.GLTFParser.ParseJson(stream, out gltfRoot);
				});
			}
			catch (Exception e)
			{
				GD.PrintErr(e);
			}
			if (gltfRoot == null)
			{
				throw new GLTFLoadException("Failed to parse glTF");
			}
			stream.Position = 0;

			using (GLTFSceneImporter importer =
				MREAPI.AppsAPI.GLTFImporterFactory.CreateImporter(gltfRoot, loader, null /* FIXME _asyncHelper*/, stream))
			{
				importer.SceneParent = MREAPI.AppsAPI.AssetCache.CacheRootGO;
				importer.Collider = colliderType.ToGLTFColliderType();

				if (gltfRoot.Textures != null)
				{
					for (var i = 0; i < gltfRoot.Textures.Count; i++)
					{
						await importer.LoadTextureAsync(gltfRoot.Textures[i], i, true);
						var texture = importer.GetTexture(i);
						texture.ResourceName = gltfRoot.Textures[i].Name ?? $"texture:{i}";
						assets.Add(texture);
					}
				}

				// load meshes
				if (gltfRoot.Meshes != null)
				{
					var cancellationSource = new System.Threading.CancellationTokenSource();
					for (var i = 0; i < gltfRoot.Meshes.Count; i++)
					{
						var mesh = await importer.LoadMeshAsync(i, cancellationSource.Token);
						mesh.ResourceName = gltfRoot.Meshes[i].Name ?? $"mesh:{i}";
						assets.Add(mesh);
					}
				}

				// load materials
				if (gltfRoot.Materials != null)
				{
					for (var i = 0; i < gltfRoot.Materials.Count; i++)
					{
						var matdef = gltfRoot.Materials[i];
						var material = await importer.LoadMaterialAsync(i);
						material.ResourceName = matdef.Name ?? $"material:{i}";
						assets.Add(material);
					}
				}

				// load prefabs
				if (gltfRoot.Scenes != null)
				{
					for (var i = 0; i < gltfRoot.Scenes.Count; i++)
					{
						await importer.LoadSceneAsync(i).ConfigureAwait(true);

						Node rootObject = importer.LastLoadedScene;
						rootObject.Name = gltfRoot.Scenes[i].Name ?? $"scene:{i}";
						/* FIXME
						var animation = rootObject.GetComponent<UnityEngine.Animation>();
						if (animation != null)
						{
							animation.playAutomatically = false;

							// initialize mapping so we know which gameobjects are targeted by which animation clips
							var mapping = rootObject.AddComponent<PrefabAnimationTargets>();
							mapping.Initialize(gltfRoot, i);
						}
						*/

						assets.Add(rootObject);
					}
				}
			}

			return assets;
		}

		[CommandHandler(typeof(AssetUpdate))]
		internal void OnAssetUpdate(AssetUpdate payload, Action onCompleteCallback)
		{
			var def = payload.Asset;
			_app.AssetManager.OnSet(def.Id, asset =>
			{

				if (def.Material != null && asset.Asset != null && asset.Asset is Godot.SpatialMaterial mat)
				{
					// make sure material reference is write-safe
					// Note: It's safe to assume existence because we're inside the OnSet callback
					mat = _app.AssetManager.GetById(def.Id, writeSafe: true).Value.Asset as Godot.SpatialMaterial;

					MREAPI.AppsAPI.MaterialPatcher.ApplyMaterialPatch(_app, mat, def.Material.Value);
				}
				else if (def.Texture != null && asset.Asset != null && asset.Asset is Godot.Texture tex)
				{
					var texdef = def.Texture.Value;

					// make sure texture reference actually needs to be updated
					if (texdef.Flags.HasValue && texdef.Flags.Value != tex.Flags)
					{
						// make texture write-safe
						// Note: It's safe to assume existence because we're inside the OnSet callback
						tex = _app.AssetManager.GetById(def.Id, writeSafe: true).Value.Asset as Godot.Texture;

						if (texdef.Flags.HasValue)
							tex.Flags = texdef.Flags.Value;
					}
					// otherwise, the shared texture is in exactly the state we want, so use it
				}
				else if (def.Sound != null)
				{
					// do nothing; sound asset properties are immutable
				}
				else if (def.VideoStream != null)
				{
					// do nothing; sound asset properties are immutable
				}
				
				else if (def.Mesh != null)
				{
					// do nothing; mesh properties are immutable
				}
				else if (def.AnimationData != null)
				{
					// do nothing; animation data are immutable
				}
				else
				{
					_app.Logger.LogError($"Asset {def.Id} is not patchable, or not of the right type!");
				}
				onCompleteCallback?.Invoke();
			});
		}

		[CommandHandler(typeof(CreateAsset))]
		internal async void OnCreateAsset(CreateAsset payload, Action onCompleteCallback)
		{
			var def = payload.Definition;
			var response = new AssetsLoaded();
			var unityAsset = _app.AssetManager.GetById(def.Id)?.Asset;
			ColliderGeometry colliderGeo = null;
			AssetSource source = null;

			ActiveContainers.Add(payload.ContainerId);

			// create materials
			if (unityAsset == null && def.Material != null)
			{
				unityAsset = MREAPI.AppsAPI.DefaultMaterial.Duplicate();
			}
			// create textures
			else if (unityAsset == null && def.Texture != null)
			{
				var texUri = new Uri(_app.ServerAssetUri, def.Texture.Value.Uri);
				source = new AssetSource(AssetContainerType.None, texUri.AbsoluteUri);
				var result = await AssetFetcher<Godot.Texture>.LoadTask(_owner, texUri);

				// this is a newly loaded texture, so we decide initial settings for the shared asset
				if (result.ReturnCode == 200)
				{
					result.Asset.Flags = def.Texture.Value.Flags ?? (uint)Godot.Texture.FlagsEnum.Default;
				}

				source.Version = result.ETag;
				unityAsset = result.Asset;
				if (result.FailureMessage != null)
				{
					response.FailureMessage = result.FailureMessage;
				}
			}
			// create meshes
			else if (unityAsset == null && def.Mesh != null)
			{
				if (def.Mesh.Value.PrimitiveDefinition != null)
				{
					var factory = MREAPI.AppsAPI.PrimitiveFactory;
					try
					{
						unityAsset = factory.CreatePrimitive(def.Mesh.Value.PrimitiveDefinition.Value);
						colliderGeo = ConvertPrimToCollider(def.Mesh.Value.PrimitiveDefinition.Value, def.Id);
					}
					catch (Exception e)
					{
						response.FailureMessage = e.Message;
						MREAPI.Logger.LogError(response.FailureMessage);
					}
				}
				else
				{
					response.FailureMessage = $"Cannot create mesh {def.Id} without a primitive definition";
				}
			}

			// create sounds
			else if (unityAsset == null && def.Sound != null)
			{
				var soundUri = new Uri(_app.ServerAssetUri, def.Sound.Value.Uri);
				source = new AssetSource(AssetContainerType.None, soundUri.AbsoluteUri);
				var result = await AssetFetcher<AudioStream>.LoadTask(_owner, soundUri);
				unityAsset = result.Asset;
				source.Version = result.ETag;
				if (result.FailureMessage != null)
				{
					response.FailureMessage = result.FailureMessage;
				}
			}
/*
			// create video streams
			else if (unityAsset == null && def.VideoStream != null)
			{
				if (MREAPI.AppsAPI.VideoPlayerFactory != null)
				{
					string videoString;

					// These youtube "URIs" are not valid URIs because they are case-sensitive. Don't parse, and
					// deprecate this URL scheme as soon as feasible.
					if (def.VideoStream.Value.Uri.StartsWith("youtube://"))
					{
						videoString = def.VideoStream.Value.Uri;
					}
					else
					{
						var videoUri = new Uri(_app.ServerAssetUri, def.VideoStream.Value.Uri);
						videoString = videoUri.AbsoluteUri;
					}

					PluginInterfaces.FetchResult result2 = MREAPI.AppsAPI.VideoPlayerFactory.PreloadVideoAsset(videoString);
					unityAsset = result2.Asset;
					if (result2.FailureMessage != null)
					{
						response.FailureMessage = result2.FailureMessage;
					}
				}
				else
				{
					response.FailureMessage = "VideoPlayerFactory not implemented";
				}
			}
*/
			// create animation data
			else if (unityAsset == null && def.AnimationData != null)
			{
				var animDataCache = new AnimationDataCached();
				animDataCache.Tracks = def.AnimationData.Value.Tracks;
				unityAsset = animDataCache;
			}

			_app.AssetManager.Set(def.Id, payload.ContainerId, unityAsset, colliderGeo, source);

			// verify creation and apply initial patch
			if (unityAsset != null)
			{
				//unityAsset.Name = def.Name;
				OnAssetUpdate(new AssetUpdate()
				{
					Asset = def
				}, null);

				try
				{
					response.Assets = new Asset[] { GenerateAssetPatch(unityAsset, def.Id) };
				}
				catch (Exception e)
				{
					response.FailureMessage = e.Message;
					_app.Logger.LogError(response.FailureMessage);
				}
			}
			else
			{
				if (response.FailureMessage == null)
				{
					response.FailureMessage = $"Not implemented: CreateAsset of new asset type";
				}
				_app.Logger.LogError(response.FailureMessage);
			}

			_app.Protocol.Send(new Message()
			{
				ReplyToId = payload.MessageId,
				Payload = response
			});

			onCompleteCallback?.Invoke();
		}

		[CommandHandler(typeof(UnloadAssets))]
		internal void UnloadAssets(UnloadAssets payload, Action onCompleteCallback)
		{
			_app.AssetManager.Unload(payload.ContainerId);
			ActiveContainers.Remove(payload.ContainerId);

			onCompleteCallback?.Invoke();
		}

		private Asset GenerateAssetPatch(Godot.Object unityAsset, Guid id)
		{
			if (unityAsset is Node go)
			{
				int actorCount = 0;
				MWGOTreeWalker.VisitTree(go, _ =>
				{
					actorCount++;
				});

				return new Asset
				{
					Id = id,
					Prefab = new Prefab()
					{
						ActorCount = actorCount
					}
				};
			}
			else if (unityAsset is Godot.SpatialMaterial mat)
			{
				return new Asset()
				{
					Id = id,
					Material = MREAPI.AppsAPI.MaterialPatcher.GeneratePatch(_app, mat)
				};
			}
			else if (unityAsset is Godot.Texture tex)
			{
				return new Asset()
				{
					Id = id,
					Texture = new MWTexture()
					{
						Resolution = new Vector2Patch(tex.GetSize()),
						Flags = tex.Flags,
					}
				};
			}
			else if (unityAsset is Godot.Mesh mesh)
			{
				ArrayMesh arrayMesh = mesh as ArrayMesh;
				Godot.Collections.Array array = arrayMesh.SurfaceGetArrays(0);
				var aabb = arrayMesh.GetAabb();
				var vertexCount = (array[(int)ArrayMesh.ArrayType.Vertex] as Array).Length;
				

				return new Asset()
				{
					Id = id,
					Mesh = new MWMesh()
					{
						VertexCount = vertexCount,
						//FIXME
						//TriangleCount = mesh.triangles.Length / 3,

						BoundingBoxDimensions = new Vector3Patch()
						{
							X = aabb.Size.x,
							Y = aabb.Size.y,
							Z = aabb.Size.z
						},
						BoundingBoxCenter = new Vector3Patch()
						{
							X = aabb.Position.x,
							Y = aabb.Position.y,
							Z = aabb.Position.z
						}

					}
				};
			}
			else if (unityAsset is AudioStream sound)
			{
				return new Asset()
				{
					Id = id,
					Sound = new MWSound()
					{
						Duration = sound.GetLength()
					}
				};
			}
			/*
			else if (unityAsset is VideoStreamDescription videoStream)
			{
				return new Asset()
				{
					Id = id,
					VideoStream = new MWVideoStream()
					{
						Duration = videoStream.Duration
					}
				};
			}
			*/
			else if (unityAsset is AnimationDataCached animData)
			{
				return new Asset()
				{
					Id = id
				};
			}

			else
			{
				throw new Exception($"Asset {id} is not patchable, or not of the right type!");
			}
		}

		internal ColliderGeometry ConvertPrimToCollider(PrimitiveDefinition prim, Guid meshId)
		{
			MWVector3 dims = prim.Dimensions;
			switch (prim.Shape)
			{
				case PrimitiveShape.Sphere:
					return new SphereColliderGeometry()
					{
						Radius = dims.SmallestComponentValue() / 2
					};

				case PrimitiveShape.Box:
					return new BoxColliderGeometry()
					{
						Size = new MWVector3(dims.X / 2, dims.Y / 2, dims.Z / 2) ?? new MWVector3(0.5f, 0.5f, 0.5f)
					};

				case PrimitiveShape.Capsule:
					return new CapsuleColliderGeometry()
					{
						Size = dims
					};
				case PrimitiveShape.Cylinder:
					dims = dims ?? new MWVector3(0.2f, 1, 0.2f);
					return new CylinderColliderGeometry()
					{
						Dimensions = dims
					};
				case PrimitiveShape.Plane:
					dims = dims ?? new MWVector3(1, 0, 1);
					return new BoxColliderGeometry()
					{
						Size = new MWVector3(Mathf.Max(dims.X, 0.01f), Mathf.Max(dims.Y, 0.01f), Mathf.Max(dims.Z, 0.01f))
					};

				default:
					return null;
			}
		}
	}
}
