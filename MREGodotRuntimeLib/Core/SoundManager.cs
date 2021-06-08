// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using MixedRealityExtension.API;
using MixedRealityExtension.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace MixedRealityExtension.Core
{
	internal struct SoundInstance
	{
		public Guid id;
		public Actor actor;
		public SoundInstance(Guid id, Actor actor)
		{
			this.id = id;
			this.actor = actor;
		}
	}

	internal class SoundManager
	{
		#region Constructor

		public SoundManager(MixedRealityExtensionApp app)
		{
			_app = app;
		}

		#endregion

		#region Public Methods

		public AudioStreamPlayer3D AddSoundInstance(Actor actor, Guid id, AudioStream audioClip, MediaStateOptions options)
		{
			float offset = options.Time.GetValueOrDefault();
			if (options.Looping != null && options.Looping.Value && audioClip.GetLength() != 0.0f)
			{
				offset = offset % audioClip.GetLength();
			}
			if (offset < audioClip.GetLength())
			{
				var soundInstance = new AudioStreamPlayer3D();
				actor.AddChild(soundInstance);
				soundInstance.Stream = audioClip;
				soundInstance.Seek(offset);
				soundInstance.UnitSize = 1.0f;
				soundInstance.EmissionAngleDegrees = 45.0f;   //only affects multichannel sounds. Default to 50% spread, 50% stereo.
				soundInstance.MaxDistance = 1000000.0f;
				ApplyMediaStateOptions(actor, soundInstance, options, id, true);
				if (options.paused != null && options.paused.Value == true)
				{
					//start as paused
					soundInstance.Play();
					soundInstance.StreamPaused = true;
				}
				else
				{
					//start as unpaused
					_unpausedSoundInstances.Add(new SoundInstance(id, actor));
					soundInstance.Play();
					soundInstance.StreamPaused = false;
				}
				return soundInstance;
			}
			return null;
		}


		public void ApplyMediaStateOptions(Actor actor, AudioStreamPlayer3D soundInstance, MediaStateOptions options, Guid id, bool startSound)
		{
			if (options != null)
			{
				//pause must happen before other sound state changes
				if (options.paused != null && options.paused.Value == true)
				{
					if (_unpausedSoundInstances.RemoveAll(x => x.id == id) > 0)
					{
						soundInstance.StreamPaused = true;
					}
				}

				if (options.Volume != null)
				{
					soundInstance.UnitSize = options.Volume.Value;
				}
				if (options.Pitch != null)
				{
					//convert from halftone offset (-12/0/12/24/36) to pitch multiplier (0.5/1/2/4/8).
					soundInstance.PitchScale = Mathf.Pow(2.0f, (options.Pitch.Value / 12.0f));
				}
				if (options.Looping != null)
				{
					if (soundInstance.Stream is AudioStreamSample audioStreamSample)
					{
						audioStreamSample.LoopMode = options.Looping.Value ? AudioStreamSample.LoopModeEnum.Forward : AudioStreamSample.LoopModeEnum.Disabled;
						int d = 1;
						d *= audioStreamSample.Stereo ? 2 : 1;
						d *= audioStreamSample.Format == AudioStreamSample.FormatEnum.Format16Bits ? 2 : 1;
						audioStreamSample.LoopEnd = audioStreamSample.Data.Length / d;
					}
					else if (soundInstance.Stream is AudioStreamOGGVorbis audioStreamOGGVorbis)
					{
						//FIXME
					}
				}
				if (options.Doppler != null)
				{
					soundInstance.DopplerTracking = Mathf.IsZeroApprox(options.Doppler.Value)
						? AudioStreamPlayer3D.DopplerTrackingEnum.Disabled : AudioStreamPlayer3D.DopplerTrackingEnum.PhysicsStep;
				}
				if (options.Spread != null)
				{
					//Spread not support in Godot.
					//soundInstance.EmissionAngleDegrees = options.Spread.Value * 90.0f;
				}
				if (options.RolloffStartDistance != null)
				{
					soundInstance.MaxDistance = options.RolloffStartDistance.Value * 1000000.0f;
				}
				if (options.Time != null)
				{
					soundInstance.Seek(options.Time.Value);
				}

				//unpause must happen after other sound state changes
				if (!startSound)
				{
					if (options.paused != null && options.paused.Value == false)
					{
						if (!_unpausedSoundInstances.Exists(x => x.id == id))
						{
							soundInstance.StreamPaused = false;
							_unpausedSoundInstances.Add(new SoundInstance(id, actor));
						}
					}
				}
			}
		}

		public void Update()
		{
			//garbage collect expired sounds, one per frame
			if (_soundStoppedCheckIndex >= _unpausedSoundInstances.Count)
			{
				_soundStoppedCheckIndex = 0;
			}
			else
			{
				var soundInstance = _unpausedSoundInstances[_soundStoppedCheckIndex];
				if (!soundInstance.actor.CheckIfSoundExpired(soundInstance.id))
				{
					_soundStoppedCheckIndex++;
				}
			}
		}

		#endregion

		public void DestroySoundInstance(AudioStreamPlayer3D soundInstance, Guid id)
		{
			_unpausedSoundInstances.RemoveAll(x => x.id == id);
			soundInstance.QueueFree();
		}

		#region Private Fields

		MixedRealityExtensionApp _app;
		private List<SoundInstance> _unpausedSoundInstances = new List<SoundInstance>();
		private int _soundStoppedCheckIndex = 0;

		#endregion
	}
}
