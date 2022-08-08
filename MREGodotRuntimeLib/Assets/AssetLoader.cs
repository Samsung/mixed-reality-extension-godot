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
using System.Text.RegularExpressions;
using Godot;
using MWTexture = MixedRealityExtension.Assets.Texture;
using MWMesh = MixedRealityExtension.Assets.Mesh;
using MWSound = MixedRealityExtension.Assets.Sound;
using MWVideoStream = MixedRealityExtension.Assets.VideoStream;

namespace MixedRealityExtension.Assets
{
	using LoaderFunction = Func<AssetSource, Guid, ColliderType, Task<IList<Asset>>>;

	internal class AssetLoader : ICommandHandlerContext
	{
		private readonly MixedRealityExtensionApp _app;

		private readonly Node _owner;
		private readonly static Dictionary<string, string> ShaderToNode3DProperties = new Dictionary<string, string>()
		{
			{"albedo", "albedo_color"},
			{"specular", "metallic_specular"},
			{"point_size", "params_point_size"},
			{"alpha_scissor_threshold", "params_alpha_scissor_threshold"},
			{"texture_albedo", "albedo_texture"},
			{"texture_metallic", "metallic_texture"},
			{"texture_roughness", "roughness_texture"},
			{"texture_emission", "emission_texture"},
			{"texture_normal", "normal_texture"},
			{"texture_ambient_occlusion", "ao_texture"},
		};

		private readonly static Plane[] texture_mask = new Plane[] {
			new Plane(1, 0, 0, 0),
			new Plane(0, 1, 0, 0),
			new Plane(0, 0, 1, 0),
			new Plane(0, 0, 0, 1),
			new Plane(0.3333333f, 0.3333333f, 0.3333333f, 0),
		};

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

		internal Node3D GetGameObjectFromParentId(Guid? parentId)
		{
			var parent = _app.FindActor(parentId ?? Guid.Empty) as Actor;
			return parent?.Node3D ?? _app.SceneRoot;
		}

		internal async Task<IList<Actor>> CreateFromLibrary(string resourceId, Guid? parentId)
		{
			var factory = MREAPI.AppsAPI.LibraryResourceFactory
				?? throw new ArgumentException("Cannot spawn resource from non-existent library.");

			var spawnedGO = await factory.CreateFromLibrary(resourceId, GetGameObjectFromParentId(parentId));
			//spawnedGO.layer = MREAPI.AppsAPI.LayerApplicator.DefaultLayer;
			return new List<Actor>() { Actor.Instantiate(spawnedGO) };
		}

		internal IList<Actor> CreateEmpty(Guid? parentId)
		{
			Node3D newGO = _app.AssetManager.EmptyTemplate().Duplicate() as Node3D;
			Actor actor = Actor.Instantiate(newGO);
			GetGameObjectFromParentId(parentId).AddChild(actor);

			return new List<Actor>() { actor };
		}

		internal IList<Actor> CreateFromPrefab(Guid prefabId, Guid? parentId, CollisionLayer? collisionLayer)
		{
			var asset = _app.AssetManager.GetById(prefabId)?.Asset as Node;
			Node3D prefab = asset.Duplicate() as Node3D;

			// restore current animation.
			foreach (var animationPlayer in prefab.GetChildren<Godot.AnimationPlayer>())
			{
				foreach (string animationString in animationPlayer.GetAnimationList())
				{
					animationPlayer.AssignedAnimation = animationString;
					break;
				}
			}

			Node3D parent = GetGameObjectFromParentId(parentId);
			parent.AddChild(prefab);

			// note: actor properties are set in App#ProcessCreatedActors
			var actorList = new List<Actor>();

			// if prefab only has one mesh, make it mesh instance of root actor.
			if (prefab.GetChildCount() == 1
				&& prefab.GetChild(0).IsClass("MeshInstance3D")
				&& prefab.GetChild(0).GetChildCount() == 0)
			{
				MeshInstance3D meshInstance = prefab.GetChild(0) as MeshInstance3D;
				var newActor = Actor.Instantiate((Node3D)prefab);
				newActor.MeshInstance3D = meshInstance;
				actorList.Add(newActor);
				return actorList;
			}

			MWGOTreeWalker.VisitTree(prefab, go =>
			{
				var collider = go.GetChild<Area3D>();
				if (collider != null)
				{
					MREAPI.AppsAPI.LayerApplicator.ApplyLayerToCollider(collisionLayer, collider);
				}

				if (go.GetType() == typeof(Node3D))
				{
					var meshInstance = go.GetChild<MeshInstance3D>();
					DuplicateResources(meshInstance);
					var newActor = Actor.Instantiate((Node3D)go);
					newActor.MeshInstance3D = meshInstance;
					actorList.Add(newActor);
				}
				else if (go.GetType() == typeof(MeshInstance3D))
				{
					var m = (MeshInstance3D)go;
					Node3D newGO = _app.AssetManager.EmptyTemplate().Duplicate() as Node3D;
					Actor actor = Actor.Instantiate(newGO);
					var parent = m.GetParent();

					actor.GlobalTransform = m.GlobalTransform;
					parent.RemoveChild(m);
					m.Transform = Transform3D.Identity;
					actor.AddChild(m);
					actor.MeshInstance3D = m;
					foreach (Node child in m.GetChildren())
					{
						m.RemoveChild(child);
						actor.AddChild(child);
					}

					parent.AddChild(actor);
					actorList.Add(actor);
				}
			});

			return actorList;
		}

		private void DuplicateResources(Godot.Object o)
		{
			foreach (Godot.Collections.Dictionary p in o.GetPropertyList())
			{
				var propertyName = (string)p["name"];
				if (o.Get(propertyName) is Resource resource)
				{
					var newResource = resource.Duplicate();
					o.Set(propertyName, newResource);

					DuplicateResources(newResource);
				}
			}
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
			MemoryStream stream = null;
			source.ParsedUri = new Uri(_app.ServerAssetUri, source.ParsedUri);

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
			using (var loader = new GodotWebRequest(source.ParsedUri.AbsoluteUri))
			{
				// set up loader
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
					stream = await loader.LoadStreamAsync(System.IO.Path.GetFileName(source.ParsedUri.LocalPath));
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
				assets = await LoadGltfFromStream(source.ParsedUri.ToString(), stream, colliderType);
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

		private async Task<IList<Godot.Object>> LoadGltfFromStream(string path, MemoryStream stream, ColliderType colliderType)
		{
			var assets = new List<Godot.Object>(30);

			// pre-parse glTF document so we can get a scene count
			// run this on a threadpool thread so that the Godot main thread is not blocked
			var gltf = GD.Load<NativeScript>("res://addons/godot_gltf/PackedSceneGLTF_.gdns").New() as Godot.Object;
			var gltfState = GD.Load<NativeScript>("res://addons/godot_gltf/GLTFState_.gdns").New() as Godot.Object;
			Node3D gltfRoot = null;
			try
			{
				gltfRoot = await Task.Run<Node3D>(() =>
				{
					stream.Position = 0;
					return gltf.Call("import_gltf_scene", path, stream.ToArray(), 0, 1000, gltfState) as Node3D;
				});
			}
			catch (Exception e)
			{
				GD.PrintErr(e);
			}

			if (gltfRoot != null)
			{
				// load textures
				var textures = gltfState.Get("images") as Godot.Collections.Array;
				if (textures?.Count != 0)
				{
					for (var i = 0; i < textures.Count; i++)
					{
						var texture = textures[i] as Resource;
						texture.ResourceName ??= $"texture:{i}";
						assets.Add(texture);
					}
				}

				// load meshes
				var meshs = gltfState.Get("meshes") as Godot.Collections.Array;
				if (meshs?.Count != 0)
				{
					for (var i = 0; i < meshs.Count; i++)
					{

						var mesh = ((Godot.Object)meshs[i]).Get("mesh") as ArrayMesh;
						mesh.ResourceName ??= $"mesh:{i}";

						/* there is bug related surface index.
						 * the code below is workaround to fix that bug.
						 * related page: https://github.com/godotengine/godot/issues/53654
						 */
						var surfaceCount = mesh.GetSurfaceCount();
						while (surfaceCount != RenderingServer.MeshGetSurfaceCount(mesh.GetRid()))
						{
							await _app.SceneRoot.ToSignal(_app.SceneRoot.GetTree().CreateTimer(0.02f), "timeout");
						}

						var collider = GenerateColliderToMesh(mesh, colliderType);
						mesh.SetMeta("collider", collider);

						assets.Add(mesh);
					}
				}

				// load materials
				var materialRepace = new System.Collections.Generic.Dictionary<StandardMaterial3D, ShaderMaterial>();
				var materials = gltfState.Get("materials") as Godot.Collections.Array;
				if (materials?.Count != 0)
				{
					for (var i = 0; i < materials.Count; i++)
					{
						var material = materials[i] as StandardMaterial3D;
						string shaderCode = await GetShaderCodeFromStandardMaterial3D(material);
						var newShaderMaterial = new ShaderMaterial()
						{
							Shader = new Shader() { Code = InsertClippingFunction(shaderCode) },
							ResourceName = material.ResourceName ?? $"material:{i}",
						};

						foreach (Godot.Collections.Dictionary param in RenderingServer.ShaderGetParamList(newShaderMaterial.Shader.GetRid()))
						{
							string paramName = (string)param["name"];
							if (ShaderToNode3DProperties.TryGetValue(paramName, out string Node3DParam))
							{
								newShaderMaterial.SetShaderParam(paramName, material.Get(Node3DParam));
							}
							else if (paramName.EndsWith("texture_channel"))
							{
								newShaderMaterial.SetShaderParam(paramName, texture_mask[(int)material.Get(paramName)]);
							}
							else
							{
								newShaderMaterial.SetShaderParam(paramName, material.Get(paramName));
							}
						}

						materialRepace[material] = newShaderMaterial;

						assets.Add(newShaderMaterial);
					}
				}

				// recreate animation player.
				var animationPlayer = gltfRoot.GetChild<Godot.AnimationPlayer>();
				if (animationPlayer != null)
				{
					var animations = animationPlayer.GetAnimationList();
					for (int i = 0; i < animations.Length; i++)
					{
						var anim = animationPlayer.GetAnimation(animations[i]);
						var animName = Regex.Replace(animations[i], "Animation[0-9]*$", "animation") + $"0x3A{i}";
						anim.ResourceName = animName;
						var player = new AnimationPlayer();
						player.Name = "AnimationPlayer";
						var animationLibrary = new AnimationLibrary();
						player.AddAnimationLibrary("", animationLibrary);
						animationLibrary.AddAnimation(animName, anim);
						player.AssignedAnimation = animName;
						player.CurrentAnimation = animName;
						gltfRoot.AddChild(player);
					}
					gltfRoot.RemoveChild(animationPlayer);
				}
				assets.Add(gltfRoot);

				// load prefabs
				var nodes = gltfState.Get("nodes") as Godot.Collections.Array;
				if (nodes?.Count != 0)
				{
					for (var i = 0; i < nodes.Count; i++)
					{
						Node sceneNode = gltfState.Call("get_scene_node", i) as Node;
						if (sceneNode != null)
						{
							sceneNode.Name ??= $"scene:{i}";
							assets.Add(sceneNode);
						}
					}
				}

				//replace materials
				MWGOTreeWalker.VisitTree(gltfRoot, async node =>
				{
					if (node is MeshInstance3D meshInstance)
					{
						if (meshInstance.Mesh.HasMeta("collider"))
						{
							var collider = meshInstance.Mesh.GetMeta("collider") as Area3D;
							meshInstance.AddChild(collider);
							meshInstance.Mesh.RemoveMeta("collider");
						}

						if (meshInstance.Mesh != null)
						{
							var materialCount = meshInstance.Mesh.GetSurfaceCount();

							/* there is bug related surface index.
							* the code below is workaround to fix that bug.
							* related page: https://github.com/godotengine/godot/issues/53654
							*/
							while (materialCount != RenderingServer.MeshGetSurfaceCount(meshInstance.Mesh.GetRid()))
							{
								await _app.SceneRoot.ToSignal(_app.SceneRoot.GetTree().CreateTimer(0.02f), "timeout");
							}

							for (int i = 0; i < materialCount; i++)
							{
								var material = meshInstance.Mesh.SurfaceGetMaterial(i);
								if (material is StandardMaterial3D meshMaterial)
								{
									meshInstance.SetSurfaceOverrideMaterial(i, materialRepace[meshMaterial]);
								}
							}
						}
					}
				});
			}
			else
			{
				throw new Exception("Failed to parse glTF");
			}

			return assets;
		}

		private Area3D GenerateColliderToMesh(ArrayMesh mesh, ColliderType colliderType)
		{
			Area3D area = null;
			CollisionShape3D collisionShape = null;
			if (colliderType == ColliderType.Box)
			{
				var aabb = mesh.GetAabb();
				area = new Area3D() { Name = "Area3D" };
				collisionShape = new CollisionShape3D();
				collisionShape.Position = (aabb.Position + aabb.End)/ 2;
				collisionShape.Shape = new BoxShape3D() {
					Size = aabb.Size,
				};
				area.AddChild(collisionShape);
			}
			else if (colliderType == ColliderType.Mesh)
			{
				area = new Area3D() { Name = "Area3D" };
				var concavePolygonShape = new ConcavePolygonShape3D();
				concavePolygonShape.Data = mesh.GetFaces();
				collisionShape = new CollisionShape3D();
				collisionShape.Shape = concavePolygonShape;
				area.AddChild(collisionShape);
			}

			return area;
		}

		private async Task<string> GetShaderCodeFromStandardMaterial3D(StandardMaterial3D spatialMaterial)
		{
			var shaderRID = RenderingServer.MaterialGetShader(spatialMaterial.GetRid());
			string shaderCode = RenderingServer.ShaderGetCode(shaderRID);
			while (string.IsNullOrEmpty(shaderCode))
			{
				await _app.SceneRoot.ToSignal(_app.SceneRoot.GetTree().CreateTimer(0.0416f), "timeout");
				shaderRID = RenderingServer.MaterialGetShader(spatialMaterial.GetRid());
				shaderCode = RenderingServer.ShaderGetCode(shaderRID);
			}
			return shaderCode;
		}

		private string InsertClippingFunction(string shaderCode)
		{
			var origin = shaderCode;
			string newCode;
			if (origin.Contains("clipBoxInverseTransform"))
				return origin;
			var fragmentIndex = shaderCode.IndexOf("void fragment(");
			newCode = origin.Substring(0, fragmentIndex - 1);
			newCode += "\n" + "uniform mat4 clipBoxInverseTransform;\n" +
								"float PointVsBox(vec3 worldPosition, mat4 boxInverseTransform)\n" +
								"{\n" +
								"   vec3 distance = abs(boxInverseTransform * vec4(worldPosition, 1.0)).xyz;\n" +
								"   return 1.0 - step(1.0001, max(distance.x, max(distance.y, distance.z)));\n" +
								"}\n";

			var fragmentCode = origin.Substring(fragmentIndex);
			var match = Regex.Match(fragmentCode, "(?<=\\{)[^}]*(?=\\})");
			var restCode = fragmentCode.Substring(match.Index + match.Value.Length);
			fragmentCode = fragmentCode.Substring(0, match.Index + match.Value.Length);
			if (!fragmentCode.Contains("ALPHA"))
				fragmentCode += "    ALPHA_SCISSOR=1.0;";
			fragmentCode += "\n" +	"    vec3 gv = (CAMERA_MATRIX * vec4(VERTEX, 1.0)).xyz;\n" +
									"    ALPHA *= PointVsBox(gv, clipBoxInverseTransform);\n";
			newCode += fragmentCode + restCode;

			return newCode;
		}

		[CommandHandler(typeof(AssetUpdate))]
		internal void OnAssetUpdate(AssetUpdate payload, Action onCompleteCallback)
		{
			var def = payload.Asset;
			_app.AssetManager.OnSet(def.Id, asset =>
			{

				if (def.Material != null && asset.Asset != null && asset.Asset is Godot.ShaderMaterial mat)
				{
					// make sure material reference is write-safe
					// Note: It's safe to assume existence because we're inside the OnSet callback
					mat = _app.AssetManager.GetById(def.Id, writeSafe: true).Value.Asset as Godot.ShaderMaterial;

					MREAPI.AppsAPI.MaterialPatcher.ApplyMaterialPatch(_app, mat, def.Material);
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
				var result = await AssetFetcher<Godot.Texture2D>.LoadTask(_owner, texUri);

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
			else if (unityAsset is Godot.ShaderMaterial mat)
			{
				return new Asset()
				{
					Id = id,
					Material = MREAPI.AppsAPI.MaterialPatcher.GeneratePatch(_app, mat)
				};
			}
			else if (unityAsset is Godot.Texture2D tex)
			{
				return new Asset()
				{
					Id = id,
					Texture = new MWTexture()
					{
						Resolution = new Vector2Patch(tex.GetSize()),
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
						Size = dims ?? new MWVector3(1, 1, 1)
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
