using Photon.Voice;
using System;
using UnityEngine;

namespace FortniteEmoteWheel
{
    public class EmoteVoiceSource : IAudioReader<float>
    {
        public static EmoteVoiceSource Instance;

        public int SamplingRate { get; }
        public int Channels { get; }
        public string Error { get; private set; }

        public volatile bool IsPlayingEmote;

        private float[] emoteSamples;
        private bool emoteLooping;
        private int emoteReadPos;

        public EmoteVoiceSource(int samplingRate = 48000, int channels = 1)
        {
            SamplingRate = samplingRate;
            Channels = channels;
            Instance = this;
        }

        public void SetEmoteClip(AudioClip clip, bool looping)
        {
            emoteLooping = looping;
            emoteReadPos = 0;

            if (clip == null)
            {
                emoteSamples = null;
                return;
            }

            float[] rawSamples = new float[clip.samples * clip.channels];
            clip.GetData(rawSamples, 0);

            if (clip.channels > 1)
            {
                int totalFrames = clip.samples;
                int channels = clip.channels;
                emoteSamples = new float[totalFrames];

                for (int i = 0; i < totalFrames; i++)
                {
                    float sum = 0f;
                    int offset = i * channels;

                    for (int ch = 0; ch < channels; ch++)
                    {
                        sum += rawSamples[offset + ch];
                    }
                    emoteSamples[i] = sum / channels;
                }
            }
            else
            {
                emoteSamples = rawSamples;
            }
        }

        public bool Read(float[] buf)
        {
            if (IsPlayingEmote && emoteSamples != null)
            {
                return ReadFromEmote(buf);
            }

            // When not playing an emote, provide silence so we don't lock the mic device
            Array.Clear(buf, 0, buf.Length);
            return true;
        }

        private bool ReadFromEmote(float[] buf)
        {
            int len = emoteSamples.Length;
            if (len == 0)
            {
                Array.Clear(buf, 0, buf.Length);
                return true;
            }

            for (int i = 0; i < buf.Length; i++)
            {
                if (emoteReadPos >= len)
                {
                    if (emoteLooping)
                    {
                        emoteReadPos = 0;
                    }
                    else
                    {
                        buf[i] = 0f;
                        continue;
                    }
                }
                buf[i] = emoteSamples[emoteReadPos];
                emoteReadPos++;
            }
            return true;
        }

        public void Dispose() { }
    }
}
