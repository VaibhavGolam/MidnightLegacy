using UnityEngine;

namespace MidnightLegacy
{
    public enum VoiceType { Engine, Skid, Wind, Rain, Nitro }

    /// <summary>
    /// A procedural sound source: the sound is generated sample by sample on the audio thread, so the game
    /// needs no audio files. Parameters are plain floats written from the main thread.
    /// Put it on a GameObject with an AudioSource that is playing (AudioManager does this for you).
    /// </summary>
    public sealed class ProceduralVoice : MonoBehaviour
    {
        public VoiceType type;
        public volatile float volume;
        /// <summary>Engine: 0..1 across the rev range. Others: brightness 0..1.</summary>
        public volatile float pitch;
        /// <summary>Engine: throttle load 0..1.</summary>
        public volatile float load;

        double sr = 48000.0;
        float phase, phase2, noiseLp, noiseLp2, smoothVol, smoothPitch;
        uint seed = 22222u;

        void Awake()
        {
            sr = AudioSettings.outputSampleRate;
        }

        float Noise()
        {
            seed = seed * 1664525u + 1013904223u;
            return ((seed >> 9) & 0xFFFF) / 32768f - 1f;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            float vTarget = volume;
            float pTarget = pitch;
            float ld = load;
            float srf = (float)sr;
            int frames = data.Length / channels;

            for (int i = 0; i < frames; i++)
            {
                smoothVol += (vTarget - smoothVol) * 0.0012f;
                smoothPitch += (pTarget - smoothPitch) * 0.0025f;
                float s = 0f;
                switch (type)
                {
                    case VoiceType.Engine:
                        {
                            float f = Mathf.Lerp(34f, 175f, smoothPitch);
                            phase += f / srf; if (phase >= 1f) phase -= 1f;
                            phase2 += f * 2.01f / srf; if (phase2 >= 1f) phase2 -= 1f;
                            float saw = phase * 2f - 1f;
                            float sq = phase < 0.45f ? 0.7f : -0.7f;
                            float h2 = Mathf.Sin(phase2 * 6.2831853f);
                            float n = Noise();
                            noiseLp += (n - noiseLp) * 0.15f;
                            s = (saw * 0.42f + sq * 0.22f + h2 * 0.25f + noiseLp * (0.15f + ld * 0.4f)) * (0.55f + 0.45f * ld);
                            // simple low pass that opens with revs
                            noiseLp2 += (s - noiseLp2) * Mathf.Lerp(0.10f, 0.45f, smoothPitch);
                            s = noiseLp2;
                        }
                        break;

                    case VoiceType.Skid:
                        {
                            float n = Noise();
                            noiseLp += (n - noiseLp) * 0.35f;           // low pass
                            float hp = n - noiseLp;                      // high pass: the screech band
                            phase += (520f + smoothPitch * 380f) / srf; if (phase >= 1f) phase -= 1f;
                            s = hp * 0.55f + (phase * 2f - 1f) * 0.12f;
                        }
                        break;

                    case VoiceType.Wind:
                        {
                            float n = Noise();
                            float a = Mathf.Lerp(0.015f, 0.28f, smoothPitch);
                            noiseLp += (n - noiseLp) * a;
                            noiseLp2 += (noiseLp - noiseLp2) * a;
                            s = noiseLp2 * 2.2f;
                        }
                        break;

                    case VoiceType.Rain:
                        {
                            float n = Noise();
                            noiseLp += (n - noiseLp) * 0.5f;
                            s = (n - noiseLp) * 0.35f;
                        }
                        break;

                    default: // nitro hiss and roar
                        {
                            float n = Noise();
                            noiseLp += (n - noiseLp) * 0.22f;
                            phase += 95f / srf; if (phase >= 1f) phase -= 1f;
                            s = noiseLp * 0.9f + (phase * 2f - 1f) * 0.2f;
                        }
                        break;
                }

                s *= smoothVol;
                for (int c = 0; c < channels; c++) data[i * channels + c] = s;
            }
        }
    }

    /// <summary>
    /// Generative synthwave style music: a bass line, an arpeggio, a soft pad and drums, over an A minor chord loop.
    /// AudioManager changes the mix per mood (menu, calm story, tense story, race).
    /// </summary>
    public sealed class MusicSynth : MonoBehaviour
    {
        public volatile float masterVol = 0.6f;
        public volatile float padVol = 0.5f;
        public volatile float arpVol = 0.3f;
        public volatile float bassVol = 0.3f;
        public volatile float drumVol = 0f;
        public volatile float bpm = 92f;

        double sr = 48000.0;
        double samplesPerStep;
        double stepClock;
        int step;
        int bar;

        float bassPhase, arpPhase, kickPhase;
        float bassEnv, arpEnv, kickEnv, hatEnv, snareEnv;
        float bassLp, noiseLp;
        float padPhase0, padPhase1, padPhase2;
        float padF0 = 220f, padF1 = 261f, padF2 = 329f;
        float bassF = 55f, arpF = 440f;
        uint seed = 777u;

        static readonly int[] roots = { 57, 53, 60, 55 };
        static readonly int[][] chords =
        {
            new[] { 0, 3, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 }, new[] { 0, 4, 7 }
        };
        static readonly int[] arpPattern = { 0, 1, 2, 1, 2, 1, 0, 2, 1, 2, 0, 1, 2, 1, 2, 0 };

        void Awake()
        {
            sr = AudioSettings.outputSampleRate;
        }

        static float Hz(float midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69f) / 12f);
        }

        float Noise()
        {
            seed = seed * 1664525u + 1013904223u;
            return ((seed >> 9) & 0xFFFF) / 32768f - 1f;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            float srf = (float)sr;
            int frames = data.Length / channels;
            samplesPerStep = sr * 60.0 / (bpm * 4.0);
            float master = masterVol, pv = padVol, av = arpVol, bv = bassVol, dv = drumVol;

            for (int i = 0; i < frames; i++)
            {
                stepClock -= 1.0;
                if (stepClock <= 0.0)
                {
                    stepClock += samplesPerStep;
                    OnStep();
                }

                // pad: three soft sines that glide to the chord tones
                int ci = bar & 3;
                int[] ch = chords[ci];
                float r = roots[ci];
                padF0 += (Hz(r + 12 + ch[0]) - padF0) * 0.0004f;
                padF1 += (Hz(r + 12 + ch[1]) - padF1) * 0.0004f;
                padF2 += (Hz(r + 12 + ch[2]) - padF2) * 0.0004f;
                padPhase0 += padF0 / srf; if (padPhase0 >= 1f) padPhase0 -= 1f;
                padPhase1 += padF1 / srf; if (padPhase1 >= 1f) padPhase1 -= 1f;
                padPhase2 += padF2 / srf; if (padPhase2 >= 1f) padPhase2 -= 1f;
                float pad = (Mathf.Sin(padPhase0 * 6.2831853f) + Mathf.Sin(padPhase1 * 6.2831853f) + Mathf.Sin(padPhase2 * 6.2831853f)) * 0.16f;

                // bass: saw through a low pass with a decaying envelope
                bassPhase += bassF / srf; if (bassPhase >= 1f) bassPhase -= 1f;
                float bassRaw = (bassPhase * 2f - 1f);
                bassLp += (bassRaw - bassLp) * 0.06f;
                float bass = bassLp * bassEnv * 1.6f;
                bassEnv *= 0.99985f;

                // arp: narrow pulse with a quick decay
                arpPhase += arpF / srf; if (arpPhase >= 1f) arpPhase -= 1f;
                float arp = (arpPhase < 0.3f ? 1f : -1f) * arpEnv * 0.5f;
                arpEnv *= 0.9992f;

                // drums
                kickPhase += (45f + kickEnv * 110f) / srf; if (kickPhase >= 1f) kickPhase -= 1f;
                float kick = Mathf.Sin(kickPhase * 6.2831853f) * kickEnv * 1.1f;
                kickEnv *= 0.9993f;
                float n = Noise();
                noiseLp += (n - noiseLp) * 0.3f;
                float hp = n - noiseLp;
                float hat = hp * hatEnv * 0.45f;
                hatEnv *= 0.9972f;
                float snare = n * snareEnv * 0.5f;
                snareEnv *= 0.9988f;

                float mix = pad * pv + bass * bv + arp * av + (kick + hat + snare) * dv;
                mix *= master;
                mix = mix / (1f + Mathf.Abs(mix));    // soft clip
                for (int c = 0; c < channels; c++) data[i * channels + c] = mix;
            }
        }

        void OnStep()
        {
            int s = step & 15;
            int ci = bar & 3;
            int[] ch = chords[ci];
            float r = roots[ci];

            // bass on a syncopated pattern
            if (s == 0 || s == 3 || s == 6 || s == 8 || s == 11 || s == 14)
            {
                bassF = Hz(r - 24 + (s == 6 || s == 14 ? 7 : 0));
                bassEnv = 1f;
            }

            // arpeggio on every 16th, an octave up on the second half of the bar
            int idx = arpPattern[s];
            float note = r + 12 + ch[idx % 3] + (s >= 8 ? 12 : 0);
            arpF = Hz(note);
            arpEnv = (s % 2 == 0) ? 1f : 0.55f;

            // drums
            if ((s & 3) == 0) kickEnv = 1f;
            if (s == 4 || s == 12) snareEnv = 1f;
            if ((s & 3) == 2) hatEnv = 1f;

            step++;
            if ((step & 15) == 0) bar++;
        }
    }
}
