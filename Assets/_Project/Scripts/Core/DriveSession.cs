using System;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Builds and owns everything for one drive: road, car, traffic, rival, lighting, weather, camera, audio and
    /// the race rules. Destroying the session (Teardown) removes all of it, so the menus start from a clean scene.
    /// </summary>
    public sealed class DriveSession : MonoBehaviour
    {
        public RaceDef Def { get; private set; }
        public RoadModel Road { get; private set; }
        public CarController Car { get; private set; }
        public CarVisuals Visuals { get; private set; }
        public RaceManager Race { get; private set; }
        public StyleTracker Style { get; private set; }
        public TrafficSystem Traffic { get; private set; }
        public TrackGenerator Track { get; private set; }
        public ChaseCamera Chase { get; private set; }
        public NightLighting Lighting { get; private set; }
        public TutorialDirector Tutorial { get; private set; }
        public TuningPanel Tuning { get; private set; }
        public CarHandlingProfile Profile { get; private set; }
        public WeatherDef Weather { get; private set; }
        public bool Paused { get; private set; }

        SaveData save;
        Camera cam;
        InputReader input;
        TrackRecipe recipe;
        CarHandlingProfile baseProfile;
        float skillOffset;

        public static DriveSession Create(RaceDef def, SaveData saveData, Camera camera, InputReader inputReader, float skillOffset)
        {
            GameObject go = new GameObject("DriveSession");
            DriveSession s = go.AddComponent<DriveSession>();
            s.Build(def, saveData, camera, inputReader, skillOffset);
            return s;
        }

        GameObject Child(string name)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(transform, false);
            return g;
        }

        void Build(RaceDef def, SaveData saveData, Camera camera, InputReader inputReader, float skill)
        {
            Def = def;
            save = saveData;
            cam = camera;
            input = inputReader;
            skillOffset = skill;

            QualityPreset preset = GraphicsSettings.Preset;
            ThemeDef theme = Environments.Theme(def.theme);
            TimeDef time = Environments.Time(def.time);
            Weather = Environments.Weather(def.weather);

            // ---- road ----
            float ramp = def.length > 0f ? def.length * 0.9f : 2600f;
            recipe = TrackRecipe.FromTheme(theme, def.seed, def.difficulty, ramp);
            // the view is flat from above, so the road stays level
            recipe.hillAmplitude = 0f;
            Road = new RoadModel(recipe);

            // ---- handling: base profile + parts + steering sensitivity ----
            baseProfile = CarHandlingProfile.CreateDefault();
            Profile = CarHandlingProfile.CreateDefault();
            CarBuild.Apply(Profile, baseProfile, save);

            // ---- player car ----
            GameObject carGo = Child("PlayerCar");
            Car = carGo.AddComponent<CarController>();
            Car.profile = Profile;
            Car.input = input;
            Car.road = Road;
            Car.SurfaceGrip = Weather.wet ? Profile.wetGripMultiplier : 1f;
            Car.Damage01 = save != null ? save.damage : 0f;
            Visuals = carGo.AddComponent<CarVisuals>();
            Visuals.Build(Car, Cosmetics.LookFor(save), true);

            SlideEffects slide = carGo.AddComponent<SlideEffects>();
            slide.Build(Car);
            slide.Enabled = preset.smoke;
            NitroEffects nitro = carGo.AddComponent<NitroEffects>();
            nitro.Build(Car);
            nitro.Enabled = preset.smoke;

            // ---- world ----
            Traffic = Child("Traffic").AddComponent<TrafficSystem>();
            Traffic.Init(Car, Road, new TrafficConfig
            {
                level = Mathf.Clamp(Mathf.Max(1, def.traffic), 1, 3),
                oncoming = def.oncoming,
                speedScale = 1f,
                seed = def.seed
            });

            Track = Child("Track").AddComponent<TrackGenerator>();
            Track.Init(Car, Road, recipe, theme, Weather.wet);

            Style = Child("Style").AddComponent<StyleTracker>();
            Style.Init(Car, Road);

            Lighting = Child("Lighting").AddComponent<NightLighting>();
            Lighting.topDown = true;
            Lighting.Init(Visuals.HeadlightAnchor, cam);
            Lighting.ApplyEnvironment(time, Weather);
            Lighting.SetFogEnd(preset.fogEnd);

            Chase = cam.gameObject.AddComponent<ChaseCamera>();
            Chase.Init(Car, Road, cam);
            Chase.shakeScale = save != null && save.settings.reduceMotion ? 0.25f : 1f;

            if (Weather.rain)
            {
                WeatherFX fx = Child("Weather").AddComponent<WeatherFX>();
                fx.Init(cam, true, preset);
            }

            // ---- input buttons from settings ----
            SettingsData st = save != null ? save.settings : new SettingsData();
            input.Locked = false;
            input.UseScript = false;
            input.BrakeEnabled = st.brakeButton;
            input.HandbrakeEnabled = st.driftButton;
            input.NitroEnabled = st.nitroButton;
            input.ClearPresses();

            // ---- race rules ----
            Race = Child("Race").AddComponent<RaceManager>();
            Race.Init(Car, Road, Traffic, Style, Track, input, def, save,
                      ContentDB.Rival(def.rival), ContentDB.Rival("unknown"), skillOffset);

            if (def.kind == RaceKind.Tutorial)
            {
                Tutorial = Child("Tutorial").AddComponent<TutorialDirector>();
                Tutorial.Init(Car, input, Race);
            }

            DriveAudio audio = Child("Audio").AddComponent<DriveAudio>();
            audio.Init(Car, Race, Weather.rain);

            if (st.devTuning)
            {
                Tuning = Child("Tuning").AddComponent<TuningPanel>();
                Tuning.Init(Car, Profile, Chase, Restart, q => { GraphicsSettings.Apply(q, true); Track.ApplyQuality(GraphicsSettings.Preset); Lighting.SetFogEnd(GraphicsSettings.Preset.fogEnd); });
            }

            Chase.Snap();
        }

        /// <summary>Restarts the same event from the grid.</summary>
        public void Restart()
        {
            if (Race == null) return;
            Paused = false;
            Time.timeScale = 1f;
            input.Locked = false;
            Car.NitroCharge = Car.startNitro;
            Race.Restart();
            if (Tutorial != null) Tutorial.Restart();
            Chase.Snap();
        }

        /// <summary>Continues the same event from the last checkpoint (free). Falls back to a full restart when there is none.</summary>
        public void RestartFromCheckpoint()
        {
            if (Race == null) return;
            if (!Race.HasCheckpoint) { Restart(); return; }
            Paused = false;
            Time.timeScale = 1f;
            input.Locked = false;
            input.ClearPresses();
            Race.RestartFromCheckpoint();
            Chase.Snap();
        }

        public void SetPaused(bool paused)
        {
            if (Paused == paused) return;
            if (paused && (Race == null || Race.State == RaceState.Finished)) return;
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            input.Locked = paused;
            if (paused) input.ClearPresses();
        }

        public void Teardown()
        {
            Time.timeScale = 1f;
            if (input != null) { input.Locked = true; input.UseScript = false; input.ClearPresses(); }
            if (Chase != null) Destroy(Chase);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Profile != null) Destroy(Profile);
            if (baseProfile != null) Destroy(baseProfile);
            if (recipe != null) Destroy(recipe);
            Time.timeScale = 1f;
        }
    }
}
