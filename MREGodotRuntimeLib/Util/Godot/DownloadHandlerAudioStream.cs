using System;
using System.IO;
using Godot;

namespace MixedRealityExtension.Util.GodotHelper
{
    internal class DownloadHandlerAudioStream : DownloadHandler
    {
        public AudioStream AudioStream { get; set; }
        private Uri uri;
        private AudioType audioType;
        public DownloadHandlerAudioStream(Uri uri, AudioType audioType)
        {
            this.uri = uri;
            this.audioType = audioType;
        }

        public void ParseData(MemoryStream stream)
        {
            switch (audioType)
            {
                case AudioType.Wav:
                    ToWav(stream);
                    break;
                case AudioType.OggVorbis:
                    ToOgg(stream);
                    break;
                case AudioType.Unknown:
                    if (System.IO.Path.GetExtension(uri.AbsolutePath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        ToWav(stream);
                    }
                    else if (System.IO.Path.GetExtension(uri.AbsolutePath).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
                    {
                        ToOgg(stream);
                    }
                    break;
            }
        }

        private void ToOgg(MemoryStream stream)
        {
            var audioStream = new AudioStreamOGGVorbis();
            audioStream.Data = stream.ToArray();
            AudioStream = audioStream;
        }

        private void ToWav(MemoryStream stream)
        {
            float[] data = null;
            using (BinaryReader reader = new BinaryReader(stream))
            {
                bool formatFound = false;
                bool dataFound = false;

                int compressionCode = 0;
                int channel = 0;
                int sampleRate = 0;
                int bitForSample = 0;

                /* Check RIFF */
                byte[] riff = new byte[12];
                reader.Read(riff, 0, 12);
                if (riff[0] != 'R' || riff[1] != 'I' || riff[2] != 'F' || riff[3] != 'F')
                {
                    throw new FileLoadException();
                }

                /* Check WAVE */
                if (riff[8] != 'W' || riff[9] != 'A' || riff[10] != 'V' || riff[11] != 'E')
                {
                    throw new FileLoadException("Not a WAV file (no WAVERIFF header).");
                }

                while (reader.BaseStream.Position != reader.BaseStream.Length) {
                    var oldPosition = reader.BaseStream.Position;
                    byte[] chunkID = new byte[4];
                    reader.Read(chunkID, 0, 4);
                    var chunkSize = reader.ReadInt32();
                    
                    /* Check format */
                    if (chunkID[0] == 'f' && chunkID[1] == 'm' && chunkID[2] == 't' && chunkID[3] == ' ' && !formatFound)
                    {
                        /* Check audio format */
                        compressionCode = reader.ReadInt16();
                        if (compressionCode != 1 && compressionCode != 3)
                        {
                            throw new FileLoadException("Format not supported for WAVE file (not PCM). Save WAVE files as uncompressed PCM instead.");
                        }

                        /* Check Channel */
                        channel = reader.ReadInt16();
                        if (channel != 1 && channel != 2)
                        {
                            throw new FileLoadException("Format not supported for WAVE file (not stereo or mono).");
                        }

                        /* Get Sample Rate */
                        sampleRate = reader.ReadInt32();
                        reader.ReadBytes(6);

                        bitForSample = reader.ReadUInt16();
                        if (bitForSample % 8 != 0 || bitForSample == 0)
                        {
                            throw new FileLoadException("Invalid amount of bits in the sample (should be one of 8, 16, 24 or 32).");
                        }
                        formatFound = true;
                    }


                    /* Get Data */
                    if (chunkID[0] == 'd' && chunkID[1] == 'a' && chunkID[2] == 't' && chunkID[3] == 'a' && !dataFound)
                    {
                        dataFound = true;

                        if (!formatFound)
                        {
                            throw new FileLoadException("'data' chunk before 'format' chunk found.");
                        }

                        int frames = chunkSize;
                        if (channel == 0)
                        {
                            throw new FileLoadException();
                        }
                        frames /= channel;
                        frames /= (bitForSample >> 3);

                        data = new float[frames * channel];

                        if (bitForSample == 8)
                        {
                            for (int i = 0; i < frames * channel; i++)
                            {
                                // 8 bit samples are UNSIGNED
                                data[i] = (reader.ReadByte() - 128) / 128f;
                            }
                        } else if (bitForSample == 32 && compressionCode == 3) {
                            for (int i = 0; i < frames * channel; i++) {
                                //32 bit IEEE Float
                                data[i] = reader.ReadSingle();
                            }
                        } else if (bitForSample == 16) {
                            for (int i = 0; i < frames * channel; i++) {
                                //16 bit SIGNED
                                data[i] = reader.ReadInt16() / 32768f;
                            }
                        } else {
                            for (int i = 0; i < frames * channel; i++) {
                                //16+ bits samples are SIGNED
                                // if sample is > 16 bits, just read extra bytes
                                Int32 s = 0;
                                for (int b = 0; b < (bitForSample >> 3); b++) {

                                    s |= reader.ReadByte() << (b * 8);
                                }
                                s <<= (32 - bitForSample);

                                data[i] = (s >> 16) / 32768f;
                            }
                        }
                    }

                    if (oldPosition + chunkSize + 8 > reader.BaseStream.Position)
                    {
                        reader.ReadBytes((int)(oldPosition + chunkSize + 8 - reader.BaseStream.Position));
                    }
                }
                var sample = new AudioStreamSample();
                bool is16 = bitForSample != 8;
                var dstData = new byte[data.Length * (is16 ? 2 : 1)];
                for (int i = 0; i < data.Length; i++)
                {
                    if (is16)
                    {
                        var v = Mathf.Clamp(data[i] * 32768, - 32768, 32767);
                        var b = BitConverter.GetBytes(Convert.ToInt16(v));
                        dstData[i * 2] = b[0];
                        dstData[i * 2 + 1] = b[1];
                    }
                    else
                    {
                        var v = Mathf.Clamp(data[i] * 128, -128, 127);
                        dstData[i] = Convert.ToByte(v);
                    }
                }
                sample.Data = dstData;
                sample.Format = bitForSample == 8 ? AudioStreamSample.FormatEnum.Format8Bits : AudioStreamSample.FormatEnum.Format16Bits;
                sample.MixRate = sampleRate;
                sample.Stereo = channel == 2;

                AudioStream = sample;
            }
        }
    }
}