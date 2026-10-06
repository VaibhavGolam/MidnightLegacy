using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Phase 1 entry point. Creates the whole driving prototype in code, so any scene works:
    /// open an empty scene, press Play (or build). If you drop this component on a GameObject yourself
    /// you can assign your own handling profile and track recipe; otherwise defaults are created.
    /// Phase 5 replaces this with the Boot scene and a proper scene flow.
    /// </summary>
    public sealed class Phase1Bootstrap : MonoBehaviour
    {
        public CarHandlingProfile handlingProfile;
        public TrackRecipe trackRecipe;

        CarController car;
        ChaseCamera chase;
        TrackGenerator track;
        NightLighting night;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (FindFirstObjectByType<Phase1Bootstrap>() != null) return;
            new GameObject("MidnightLegacy_Phase1").AddComponent<Phase1Bootstrap>();
        }

        void Awake()
        {
            Application.runInBackground = false;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;

            Monetization.Init();

            // ---- graphics level ----
            GraphicsSettings.Apply(GraphicsSettings.LoadSaved(), false);

            // ---- data ----
            if (handlingProfile == null) handlingProfile = CarHandlingProfile.CreateDefault();
            if (trackRecipe == null) trackRecipe = TrackRecipe.CreateDefault();
            RoadModel road = new RoadModel(trackRecipe);

            // ---- camera ----
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            // ---- input ----
            InputReader input = gameObject.AddComponent<InputReader>();

            // ---- car ----
            GameObject carGo = new GameObject("Car_KairoHachiR");
            car = carGo.AddComponent<CarController>();
            car.profile = handlingProfile;
            car.road = road;
            car.input = input;
            CarVisuals visuals = carGo.AddComponent<CarVisuals>();
            visuals.Build(car);
            SlideEffects effects = carGo.AddComponent<SlideEffects>();
            effects.Build(car);
            car.ResetCar();

            // ---- world ----
            night = gameObject.AddComponent<NightLighting>();
            night.fogEnd = GraphicsSettings.Preset.fogEnd;
            night.Init(visuals.HeadlightAnchor, cam);

            track = new GameObject("Track").AddComponent<TrackGenerator>();
            track.Init(car, road, trackRecipe);

            chase = cam.gameObject.GetComponent<ChaseCamera>();
            if (chase == null) chase = cam.gameObject.AddComponent<ChaseCamera>();
            chase.Init(car, road, cam);

            // ---- UI ----
            TuningPanel tuning = gameObject.AddComponent<TuningPanel>();
            tuning.Init(car, handlingProfile, chase, RestartRun, OnQualityChanged);
            HUDView hud = gameObject.AddComponent<HUDView>();
            hud.Init(car, input, tuning);

            GameServices.Register(car);
            GameServices.Register(road);

            effects.Enabled = GraphicsSettings.Preset.smoke;
        }

        void OnQualityChanged(QualityLevel level)
        {
            GraphicsSettings.Apply(level, true);
            night.SetFogEnd(GraphicsSettings.Preset.fogEnd);
            track.ApplyQuality(GraphicsSettings.Preset);
            SlideEffects fx = car.GetComponent<SlideEffects>();
            if (fx != null) fx.Enabled = GraphicsSettings.Preset.smoke;
        }

        void RestartRun()
        {
            car.Damage01 = 0f;
            car.ResetCar();
            track.RebuildAll();
            chase.Snap();
            EventBus.Raise(new RunReset());
        }
    }
}
