using System;
using UnityEngine;

namespace MidnightLegacy
{
    public enum Sfx { Click, Back, Confirm, Error, Coin, Impact, Crash, NearMiss, Whoosh, Beep, Go, Checkpoint, Win, Lose, Chime }

    public enum MusicMood { Off, Menu, Calm, Tense, Race }

    /// <summary>
    /// Plays every sound in the game. No audio files: the sound effects are small clips generated at start up
    /// (about 100 KB in total) and the music is a generative synthesizer (see MusicSynth).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        const int SampleRate = 22050;
        const int PoolSize = 6;

        readonly AudioClip[] clips = new AudioClip[15];
        readonly AudioSource[] pool = new AudioSource[PoolSize];
        int nextSource;
        MusicSynth music;
        AudioClip silent;

        float musicVolume = 0.7f, sfxVolume = 0.9f;
        bool vibration = true;
        MusicMood mood = MusicMood.Off;
        float intensity;

        // current mix (smoothed) and target mix
        float curPad, curArp, curBass, curDrum, curBpm = 90f;
        float tgtPad, tgtArp, tgtBass, tgtDrum, tgtBpm = 90f;

        public float SfxVolume { get { return sfxVolume; } }

        public static AudioManager Create(Transform parent)
        {
            if (I != null) return I;
            GameObject go = new GameObject("AudioManager");
            if (parent != null) go.transform.SetParent(parent, false);
            I = go.AddComponent<AudioManager>();
            I.Setup();
            return I;
        }

        /// <summary>A one sample clip that keeps an AudioSource "playing" so OnAudioFilterRead is called.</summary>
        public static AudioClip SilentClip()
        {
            if (I != null && I.silent != null) return I.silent;
            AudioClip c = AudioClip.Create("ml_silent", 2048, 1, 44100, false);
            c.SetData(new float[2048], 0);
            if (I != null) I.silent = c;
            return c;
        }

        /// <summary>Adds a looping procedural voice (engine, wind...) to a GameObject.</summary>
        public static ProceduralVoice AddVoice(GameObject host, VoiceType type)
        {
            AudioSource src = host.AddComponent<AudioSource>();
            src.clip = SilentClip();
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 1f;
            src.ignoreListenerPause = false;
            ProceduralVoice v = host.AddComponent<ProceduralVoice>();
            v.type = type;
            v.volume = 0f;
            src.Play();
            return v;
        }

        void Setup()
        {
            silent = AudioClip.Create("ml_silent", 2048, 1, 44100, false);
            silent.SetData(new float[2048], 0);

            for (int i = 0; i < PoolSize; i++)
            {
                GameObject g = new GameObject("Sfx" + i);
                g.transform.SetParent(transform, false);
                AudioSource a = g.AddComponent<AudioSource>();
                a.playOnAwake = false;
                a.spatialBlend = 0f;
                a.ignoreListenerPause = true;
                pool[i] = a;
            }

            GameObject m = new GameObject("Music");
            m.transform.SetParent(transform, false);
            AudioSource ms = m.AddComponent<AudioSource>();
            ms.clip = silent;
            ms.loop = true;
            ms.playOnAwake = false;
            ms.spatialBlend = 0f;
            ms.ignoreListenerPause = true;
            music = m.AddComponent<MusicSynth>();
            music.masterVol = 0f;
            ms.Play();

            BuildClips();
        }

        public void Apply(SettingsData s)
        {
            if (s == null) return;
            musicVolume = Mathf.Clamp01(s.musicVolume);
            sfxVolume = Mathf.Clamp01(s.sfxVolume);
            vibration = s.vibration;
        }

        public void SetMood(MusicMood m)
        {
            mood = m;
            switch (m)
            {
                case MusicMood.Menu: tgtPad = 0.65f; tgtArp = 0.25f; tgtBass = 0.28f; tgtDrum = 0f; tgtBpm = 84f; break;
                case MusicMood.Calm: tgtPad = 0.75f; tgtArp = 0.10f; tgtBass = 0.12f; tgtDrum = 0f; tgtBpm = 74f; break;
                case MusicMood.Tense: tgtPad = 0.50f; tgtArp = 0.30f; tgtBass = 0.34f; tgtDrum = 0.16f; tgtBpm = 98f; break;
                case MusicMood.Race: tgtPad = 0.30f; tgtArp = 0.38f; tgtBass = 0.42f; tgtDrum = 0.45f; tgtBpm = 116f; break;
                default: tgtPad = tgtArp = tgtBass = tgtDrum = 0f; break;
            }
        }

        /// <summary>0..1, pushes drums and tempo up while racing fast.</summary>
        public void SetIntensity(float v)
        {
            intensity = Mathf.Clamp01(v);
        }

        void Update()
        {
            float k = 1f - Mathf.Exp(-1.6f * Time.unscaledDeltaTime);
            float drum = tgtDrum;
            float bpm = tgtBpm;
            if (mood == MusicMood.Race)
            {
                drum = Mathf.Lerp(0.25f, 0.6f, intensity);
                bpm = Mathf.Lerp(108f, 126f, intensity);
            }
            curPad += (tgtPad - curPad) * k;
            curArp += (tgtArp - curArp) * k;
            curBass += (tgtBass - curBass) * k;
            curDrum += (drum - curDrum) * k;
            curBpm += (bpm - curBpm) * k;

            music.padVol = curPad;
            music.arpVol = curArp;
            music.bassVol = curBass;
            music.drumVol = curDrum;
            music.bpm = curBpm;
            music.masterVol = mood == MusicMood.Off ? 0f : 0.55f * musicVolume;
        }

        // ------------------------------------------------------------------ playing

        public void Play(Sfx s, float volume = 1f, float pitch = 1f)
        {
            AudioClip c = clips[(int)s];
            if (c == null || sfxVolume <= 0.001f) return;
            AudioSource a = pool[nextSource];
            nextSource = (nextSource + 1) % PoolSize;
            a.pitch = pitch;
            a.volume = Mathf.Clamp01(volume * sfxVolume);
            a.clip = c;
            a.Play();
        }

        public static void Ui(Sfx s)
        {
            if (I != null) I.Play(s, 0.7f);
        }

        /// <summary>Short buzz on Android. strength is only used to skip tiny hits.</summary>
        public void Vibrate(float strength)
        {
            if (!vibration || strength < 0.35f) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        // ------------------------------------------------------------------ clip synthesis

        delegate float Gen(float t, float n);

        static uint seed = 12345u;
        static float Noise()
        {
            seed = seed * 1664525u + 1013904223u;
            return ((seed >> 9) & 0xFFFF) / 32768f - 1f;
        }

        static AudioClip Make(string name, float seconds, Gen gen)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
            float[] data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float norm = i / (float)n;
                float v = gen(t, norm);
                // short fade in and out so clips never click
                float edge = Mathf.Min(1f, Mathf.Min(i / 60f, (n - i) / 120f));
                data[i] = Mathf.Clamp(v * edge, -1f, 1f);
            }
            AudioClip c = AudioClip.Create(name, n, 1, SampleRate, false);
            c.SetData(data, 0);
            return c;
        }

        static float Sin(float f, float t) { return Mathf.Sin(6.2831853f * f * t); }
        static float Sq(float f, float t) { return Mathf.Repeat(f * t, 1f) < 0.5f ? 1f : -1f; }

        void BuildClips()
        {
            clips[(int)Sfx.Click] = Make("sfx_click", 0.04f, (t, n) => Sin(1300f, t) * Mathf.Exp(-t * 90f) * 0.6f);
            clips[(int)Sfx.Back] = Make("sfx_back", 0.06f, (t, n) => Sin(760f - 200f * n, t) * Mathf.Exp(-t * 55f) * 0.6f);
            clips[(int)Sfx.Confirm] = Make("sfx_confirm", 0.18f, (t, n) =>
                (Sin(n < 0.45f ? 620f : 930f, t) * 0.5f + Sin(n < 0.45f ? 1240f : 1860f, t) * 0.12f) * Mathf.Exp(-t * 11f));
            clips[(int)Sfx.Error] = Make("sfx_error", 0.26f, (t, n) => Sq(n < 0.5f ? 190f : 150f, t) * 0.25f * (1f - n));
            clips[(int)Sfx.Coin] = Make("sfx_coin", 0.22f, (t, n) =>
                (Sin(n < 0.3f ? 1480f : 2220f, t) * 0.45f + Sin(n < 0.3f ? 2960f : 4440f, t) * 0.1f) * Mathf.Exp(-t * 12f));
            clips[(int)Sfx.Impact] = Make("sfx_impact", 0.28f, (t, n) =>
            {
                float nz = Noise();
                return (nz * 0.55f * Mathf.Exp(-t * 20f) + Sin(85f - 40f * n, t) * 0.8f * Mathf.Exp(-t * 14f));
            });
            float lp = 0f;
            clips[(int)Sfx.Crash] = Make("sfx_crash", 0.7f, (t, n) =>
            {
                lp += (Noise() - lp) * Mathf.Lerp(0.5f, 0.08f, n);
                return lp * 1.5f * Mathf.Exp(-t * 5f) + Sin(60f - 25f * n, t) * 0.9f * Mathf.Exp(-t * 8f);
            });
            float lp2 = 0f;
            clips[(int)Sfx.NearMiss] = Make("sfx_near", 0.32f, (t, n) =>
            {
                lp2 += (Noise() - lp2) * Mathf.Lerp(0.04f, 0.5f, Mathf.Sin(n * 3.14159f));
                return lp2 * 1.6f * Mathf.Sin(n * 3.14159f);
            });
            float lp3 = 0f;
            clips[(int)Sfx.Whoosh] = Make("sfx_whoosh", 0.7f, (t, n) =>
            {
                lp3 += (Noise() - lp3) * Mathf.Lerp(0.02f, 0.35f, Mathf.Sin(n * 3.14159f));
                return lp3 * 1.8f * Mathf.Sin(n * 3.14159f);
            });
            clips[(int)Sfx.Beep] = Make("sfx_beep", 0.14f, (t, n) => (Sin(880f, t) * 0.5f + Sq(880f, t) * 0.08f) * Mathf.Exp(-t * 6f));
            clips[(int)Sfx.Go] = Make("sfx_go", 0.45f, (t, n) => (Sin(1320f, t) * 0.5f + Sq(1320f, t) * 0.08f) * Mathf.Exp(-t * 3.5f));
            clips[(int)Sfx.Checkpoint] = Make("sfx_checkpoint", 0.45f, (t, n) =>
            {
                float f = n < 0.33f ? 784f : (n < 0.66f ? 988f : 1319f);
                return Sin(f, t) * 0.45f * Mathf.Exp(-(t % 0.15f) * 9f);
            });
            clips[(int)Sfx.Win] = Make("sfx_win", 1.0f, (t, n) =>
            {
                float[] f = { 523f, 659f, 784f, 1047f };
                int idx = Mathf.Min(3, (int)(t / 0.16f));
                float lt = t - idx * 0.16f;
                float env = idx == 3 ? Mathf.Exp(-lt * 2.5f) : Mathf.Exp(-lt * 9f);
                return (Sin(f[idx], t) * 0.4f + Sin(f[idx] * 2f, t) * 0.1f) * env;
            });
            clips[(int)Sfx.Lose] = Make("sfx_lose", 0.8f, (t, n) =>
            {
                float[] f = { 392f, 330f, 262f, 196f };
                int idx = Mathf.Min(3, (int)(t / 0.17f));
                float lt = t - idx * 0.17f;
                float env = idx == 3 ? Mathf.Exp(-lt * 3f) : Mathf.Exp(-lt * 8f);
                return (Sin(f[idx], t) * 0.4f + Sq(f[idx], t) * 0.06f) * env;
            });
            clips[(int)Sfx.Chime] = Make("sfx_chime", 0.35f, (t, n) => (Sin(1760f, t) * 0.35f + Sin(2640f, t) * 0.12f) * Mathf.Exp(-t * 11f));
        }

        void OnDestroy()
        {
            if (I == this) I = null;
            for (int i = 0; i < clips.Length; i++) if (clips[i] != null) Destroy(clips[i]);
            if (silent != null) Destroy(silent);
        }
    }
}
