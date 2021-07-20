// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Collections.Generic;
using MixedRealityExtension.Animation;
using MixedRealityExtension.Core.Physics;
using Newtonsoft.Json.Linq;
using Godot;

namespace MixedRealityExtension.Patching.Types
{
	/// type of the motion that helps on the other side to predict its trajectory
	[Flags]
	public enum MotionType : byte
	{
		/// body that is simulated and should react to impacts
		Dynamic = 1,
		/// body is key framed, has infinite mass used for animation or mouse pick 
		Keyframed = 2,
		/// special flag to signal that this body is now sleeping and will not move (can become key framed stationary until collision)
		Sleeping = 4
	};

	public class TransformPatchInfo
	{
		public TransformPatchInfo() { }

		internal TransformPatchInfo(Guid id, RigidBodyTransform transform, MotionType mType)
		{
			Id = id;
			motionType = mType;
			Transform = new TransformPatch();
			Transform.Position = new Vector3Patch(transform.Position);
			Transform.Rotation = new QuaternionPatch(transform.Rotation);
		}

		/// <summary>
		/// ID of the actor (of the RB)
		/// </summary>
		public Guid Id { get; set; }

		/// the type of the motion
		public MotionType motionType { get; set; }

		public TransformPatch Transform { get; set; }
	}

	public class PhysicsBridgePatch : Patchable<PhysicsBridgePatch>
	{
		public PhysicsBridgePatch()
		{
			TransformCount = 0;
			TransformsBLOB = null;
		}

		internal PhysicsBridgePatch(Guid sourceId, Snapshot snapshot)
		{
			Id = sourceId;
			Time = snapshot.Time;
			Flags = snapshot.Flags;
			TransformCount = snapshot.Transforms.Count;

			if (TransformCount > 0)
			{
				TransformsBLOB = ConvertTransformList(snapshot.Transforms);
			}
		}

		internal Snapshot ToSnapshot()
		{
			if (TransformCount > 0)
			{
				return new Snapshot(Time, ConvertTransformBLOB(TransformsBLOB), Flags);
			}
 
			return new Snapshot(Time, new List<Snapshot.TransformInfo>(), Flags);
		}

		/// returns true if this snapshot should be send even if it has no transforms
		public bool DoSendThisPatch() { return (TransformCount > 0 || Flags != Snapshot.SnapshotFlags.NoFlags); }

		private List<Snapshot.TransformInfo> ConvertTransformBLOB(byte[] blob)
		{
			List<Snapshot.TransformInfo> transformInfoList = new List<Snapshot.TransformInfo>();
			using (MemoryStream memoryStream = new MemoryStream(blob))
			using (BinaryReader binaryReader = new BinaryReader(memoryStream))
			{
				for (int i = 0; i < memoryStream.Length / 48; i++)
				{
					var guidBytes = binaryReader.ReadBytes(16);
					Guid guid = new Guid(guidBytes);

					MotionType motionType = (MotionType)binaryReader.ReadByte();

					//skip 3 bytes
					binaryReader.ReadBytes(3);

					Vector3 position = new Vector3();
					position.x = binaryReader.ReadSingle();
					position.y = binaryReader.ReadSingle();
					position.z = -binaryReader.ReadSingle();

					Quat rotation = new Quat();
					rotation.x = -binaryReader.ReadSingle();
					rotation.y = -binaryReader.ReadSingle();
					rotation.z = binaryReader.ReadSingle();
					rotation.w = binaryReader.ReadSingle();

					var rigidBodyTransform = new RigidBodyTransform()
					{
						Position = position,
						Rotation = rotation,
					};
					var transformInfo = new Snapshot.TransformInfo(guid, rigidBodyTransform, motionType);
					transformInfoList.Add(transformInfo);
				}
			}
			return transformInfoList;
		}

		private byte[] ConvertTransformList(List<Snapshot.TransformInfo> list)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				foreach (var transformInfo in list)
				{
					var blob = new byte[48];
					Buffer.BlockCopy(transformInfo.RigidBodyId.ToByteArray(), 0, blob, 0, 16);
					blob[16] = (byte)transformInfo.MotionType;
					blob[17] = 0;
					blob[18] = 0;
					blob[19] = 0;
					Buffer.BlockCopy(System.BitConverter.GetBytes(transformInfo.Transform.Position.x), 0, blob, 20, 4);
					Buffer.BlockCopy(System.BitConverter.GetBytes(transformInfo.Transform.Position.y), 0, blob, 24, 4);
					Buffer.BlockCopy(System.BitConverter.GetBytes(-transformInfo.Transform.Position.z), 0, blob, 28, 4);
					Buffer.BlockCopy(System.BitConverter.GetBytes(-transformInfo.Transform.Rotation.x), 0, blob, 32, 4);
					Buffer.BlockCopy(System.BitConverter.GetBytes(-transformInfo.Transform.Rotation.y), 0, blob, 36, 4);
					Buffer.BlockCopy(System.BitConverter.GetBytes(transformInfo.Transform.Rotation.z), 0, blob, 40, 4);
					Buffer.BlockCopy(System.BitConverter.GetBytes(transformInfo.Transform.Rotation.w), 0, blob, 44, 4);
					binaryWriter.Write(blob);
				}

				return memoryStream.ToArray();
			}
		}

		/// <summary>
		/// source app id (of the sender)
		/// </summary>
		public Guid Id { get; set; }

		public float Time { get; set; }

		public int TransformCount { get; set; }

		public Snapshot.SnapshotFlags Flags { get; set; }

		/// <summary>
		/// Serialized as a string in Json.
		/// https://www.newtonsoft.com/json/help/html/SerializationGuide.htm
		/// </summary>
		public byte[] TransformsBLOB { get; set; }
	}

	public class PhysicsTranformServerUploadPatch : IPatchable
	{
		public struct OneActorUpdate
		{
			public OneActorUpdate(Guid id,Godot.Vector3 localPos, Godot.Quat localQuat,
				Godot.Vector3 appPos, Godot.Quat appQuat)
			{
				localTransform = new TransformPatch();
				appTransform = new TransformPatch();

				localTransform.Position = new Vector3Patch(localPos);
				localTransform.Rotation = new QuaternionPatch(localQuat);

				appTransform.Position = new Vector3Patch(appPos);
				appTransform.Rotation = new QuaternionPatch(appQuat);
				
				actorGuid = id;
			}

			public OneActorUpdate(OneActorUpdate copyIn)
			{
				localTransform = copyIn.localTransform;
				appTransform = copyIn.appTransform;
				actorGuid = copyIn.actorGuid;
			}

			/// test if the two actor updates are equal
			public bool isEqual(OneActorUpdate inUpdate, float eps = 0.0001F)
			{
				return (inUpdate.actorGuid == actorGuid)
					&& TransformPatch.areTransformsEqual(localTransform, inUpdate.localTransform, eps)
					&& TransformPatch.areTransformsEqual(appTransform, inUpdate.appTransform, eps);
			}

			public TransformPatch localTransform { get; set; }
			public TransformPatch appTransform { get; set; }
			public Guid actorGuid { get; set; }
		}

		/// <summary>
		/// source app id (of the sender)
		/// </summary>
		public Guid Id { get; set; }

		public int TransformCount { get; set; }

		public OneActorUpdate[] updates;

		public bool IsPatched()
		{
			return (TransformCount > 0);
		}

		public void WriteToPath(TargetPath path, JToken value, int depth)
		{
		}

		public bool ReadFromPath(TargetPath path, ref JToken value, int depth)
		{
			return false;
		}

		public void Clear()
		{

		}

		public void Restore(TargetPath path, int depth)
		{

		}

		public void RestoreAll()
		{

		}
	}
}
