using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Everything you hear while driving: engine, skid, wind, rain, nitro, the rival's engine, and reactions
    /// to crashes, near misses, style points, the countdown and the finish.
    /// </summary>
    public sealed class DriveAudio : MonoBehaviour
    {
        CarController car;
        RaceManager race;
        ProceduralVoice engine, skid, wind, rain, nitro, rivalEngine;
        bool rainOn;
        float topMs = 52f;
        string lastBanner = "";
        float lastBannerTime;
        float sfx = 1f;

        public void Init(CarController playerCar, RaceManager raceManager, bool raining)
        {
            car = playerCar;
            race = raceManager;
            rainOn = raining;
            topMs = Mathf.Max(30f, car.profile.topSpeedKph / 3.6f);

            engine = AudioManager.AddVoice(Child("Engine"), VoiceType.Engine);
            skid = AudioManager.AddVoice(Child("Skid"), VoiceType.Skid);
            wind = AudioManager.AddVoice(Child("Wind"), VoiceType.Wind);
            nitro = AudioManager.AddVoice(Child("Nitro"), VoiceType.Nitro);
            rivalEngine = AudioManager.AddVoice(Child("RivalEngine"), VoiceType.Engine);
            if (raining) rain = AudioManager.AddVoice(Child("Rain"), VoiceType.Rain);

            if (AudioManager.I != null) AudioManager.I.SetMood(MusicMood.Race);
        }

        GameObject Child(string name)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(transform, false);
            return g;
        }

        void OnEnable()
        {
            EventBus.Subscribe<WallHit>(OnWall);
            EventBus.Subscribe<TrafficHit>(OnTraffic);
            EventBus.Subscribe<NearMiss>(OnNear);
            EventBus.Subscribe<StylePoint>(OnStyle);
            EventBus.Subscribe<RaceFinished>(OnFinished);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<WallHit>(OnWall);
            EventBus.Unsubscribe<TrafficHit>(OnTraffic);
            EventBus.Unsubscribe<NearMiss>(OnNear);
            EventBus.Unsubscribe<StylePoint>(OnStyle);
            EventBus.Unsubscribe<RaceFinished>(OnFinished);
        }

        void OnWall(WallHit e)
        {
            if (AudioManager.I == null) return;
            float v = Mathf.Clamp01(e.impactSpeed / 14f);
            AudioManager.I.Play(v > 0.6f ? Sfx.Crash : Sfx.Impact, 0.5f + 0.5f * v, Random.Range(0.92f, 1.08f));
            AudioManager.I.Vibrate(v);
        }

        void OnTraffic(TrafficHit e)
        {
            if (AudioManager.I == null) return;
            float v = Mathf.Clamp01(e.impactSpeed / 16f);
            AudioManager.I.Play(Sfx.Crash, 0.55f + 0.45f * v, Random.Range(0.9f, 1.05f));
            AudioManager.I.Vibrate(v + 0.2f);
        }

        void OnNear(NearMiss e)
        {
            if (AudioManager.I == null) return;
            AudioManager.I.Play(Sfx.NearMiss, 0.7f, Random.Range(0.95f, 1.15f));
        }

        void OnStyle(StylePoint e)
        {
            if (AudioManager.I == null) return;
            if (e.kind == StyleKind.Drift) return;   // drift points tick every frame, no sound
            AudioManager.I.Play(Sfx.Chime, 0.45f, 0.9f + Mathf.Min(8, e.combo) * 0.06f);
        }

        void OnFinished(RaceFinished e)
        {
            if (AudioManager.I == null || e.result == null) return;
            AudioManager.I.Play(e.result.won ? Sfx.Win : Sfx.Lose, 0.9f);
        }

        void Update()
        {
            if (car == null || race == null || engine == null) return;
            bool paused = Time.timeScale < 0.01f;
            sfx = AudioManager.I != null ? AudioManager.I.SfxVolume : 1f;

            float speed = car.SpeedMS;

            // fake gearbox: five gears so the engine note rises and falls like a real car
            float[] gears = { 0.22f, 0.40f, 0.58f, 0.78f, 1.01f };
            float lo = 0f, hi = gears[0] * topMs;
            for (int g = 0; g < gears.Length; g++)
            {
                hi = gears[g] * topMs;
                if (speed < hi) break;
                lo = hi;
            }
            float rpm = Mathf.Lerp(0.30f, 1f, Mathf.Clamp01((speed - lo) / Mathf.Max(1f, hi - lo)));
            if (race.State == RaceState.Countdown) rpm = 0.25f + 0.35f * Mathf.PerlinNoise(Time.unscaledTime * 6f, 0f);

            engine.pitch = rpm;
            engine.load = car.NitroActive ? 1f : Mathf.Clamp01(car.Throttle * 0.8f + 0.2f);
            engine.volume = paused ? 0f : (0.20f + 0.06f * engine.load) * sfx;

            float slide = car.SlideAmount;
            skid.pitch = slide;
            skid.volume = paused || speed < 8f ? 0f : Mathf.Clamp01((slide - 0.2f) * 1.6f) * 0.32f * sfx;

            float w = Mathf.Clamp01(speed / 60f);
            wind.pitch = w;
            wind.volume = paused ? 0f : w * w * 0.30f * sfx;

            nitro.pitch = 0.5f;
            nitro.volume = paused ? 0f : (car.NitroActive ? 0.30f : 0f) * sfx;

            if (rain != null) rain.volume = paused ? 0f : 0.12f * sfx;

            // rival (or the black car cameo): quiet engine that follows the distance to the player
            RivalController r = race.Rival != null ? race.Rival : race.Cameo;
            if (race.Cameo != null && race.Cameo.gameObject.activeInHierarchy && !race.Cameo.CameoDone) r = race.Cameo;
            if (r != null && r.gameObject.activeInHierarchy && !paused)
            {
                float dist = Mathf.Abs(r.S - car.S);
                float near = Mathf.Clamp01(1f - dist / 90f);
                rivalEngine.pitch = Mathf.Clamp01(r.Speed / topMs);
                rivalEngine.load = 0.8f;
                rivalEngine.volume = near * near * 0.16f * sfx;
            }
            else
            {
                rivalEngine.volume = 0f;
            }

            // banner driven sounds: countdown beeps, GO, checkpoints
            if (race.BannerTime > lastBannerTime + 0.01f || race.Banner != lastBanner)
            {
                if (race.BannerTime > 0.05f && AudioManager.I != null)
                {
                    string b = race.Banner;
                    if (b == "3" || b == "2" || b == "1") AudioManager.I.Play(Sfx.Beep, 0.8f);
                    else if (b == "GO!") AudioManager.I.Play(Sfx.Go, 0.9f);
                    else if (b != null && b.StartsWith("CHECKPOINT")) AudioManager.I.Play(Sfx.Checkpoint, 0.9f);
                }
                lastBanner = race.Banner;
            }
            lastBannerTime = race.BannerTime;

            if (AudioManager.I != null) AudioManager.I.SetIntensity(Mathf.Clamp01(speed / topMs));
        }

        void OnDestroy()
        {
            if (AudioManager.I != null) AudioManager.I.SetMood(MusicMood.Menu);
        }
    }
}
