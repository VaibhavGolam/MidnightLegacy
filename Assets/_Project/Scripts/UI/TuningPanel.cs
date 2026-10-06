using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// On device tuning. Opens over the top 58% of the screen so the steering buttons at the bottom stay
    /// usable: change a slider with one thumb, drive with the other. Values are saved on the phone, and
    /// COPY VALUES puts them on the clipboard so they can be pasted into chat or into the ScriptableObject.
    /// Debug tool only: remove or hide behind a debug flag before release.
    /// </summary>
    public sealed class TuningPanel : MonoBehaviour
    {
        sealed class Tunable
        {
            public string name;
            public float min, max;
            public float def;
            public bool persist;
            public Func<float> get;
            public Action<float> set;
        }

        public bool Visible;

        readonly List<Tunable> tunables = new List<Tunable>(48);
        CarController car;
        CarHandlingProfile profile;
        Action restart;
        Action<QualityLevel> applyQuality;
        bool wet;
        Vector2 scroll;

        GUIStyle labelStyle, buttonStyle, headerStyle;
        bool stylesReady;

        public void Init(CarController carController, CarHandlingProfile handling, ChaseCamera cam,
                         Action restartRun, Action<QualityLevel> qualityChanged)
        {
            car = carController;
            profile = handling;
            restart = restartRun;
            applyQuality = qualityChanged;
            Build(cam);
            LoadSaved();
        }

        void Add(string name, float min, float max, Func<float> get, Action<float> set, bool persist = true)
        {
            tunables.Add(new Tunable { name = name, min = min, max = max, get = get, set = set, def = get(), persist = persist });
        }

        void Build(ChaseCamera cam)
        {
            CarHandlingProfile p = profile;
            Add("mass", 800f, 1600f, () => p.mass, v => p.mass = v);
            Add("frontWeightBias", 0.42f, 0.62f, () => p.frontWeightBias, v => p.frontWeightBias = v);
            Add("cgHeight", 0.3f, 0.7f, () => p.cgHeight, v => p.cgHeight = v);
            Add("yawInertiaFactor", 0.6f, 1.8f, () => p.yawInertiaFactor, v => p.yawInertiaFactor = v);
            Add("yawDamping", 0f, 1500f, () => p.yawDamping, v => p.yawDamping = v);

            Add("enginePowerKW", 30f, 140f, () => p.enginePowerKW, v => p.enginePowerKW = v);
            Add("topSpeedKph", 120f, 260f, () => p.topSpeedKph, v => p.topSpeedKph = v);
            Add("maxDriveForce", 2500f, 8000f, () => p.maxDriveForce, v => p.maxDriveForce = v);
            Add("cruiseLimitKph", 60f, 250f, () => p.cruiseLimitKph, v => p.cruiseLimitKph = v);
            Add("throttleLiftOnSteer", 0f, 1f, () => p.throttleLiftOnSteer, v => p.throttleLiftOnSteer = v);
            Add("liftStartSteer", 0f, 0.9f, () => p.liftStartSteer, v => p.liftStartSteer = v);
            Add("liftMinSpeedKph", 20f, 120f, () => p.liftMinSpeedKph, v => p.liftMinSpeedKph = v);
            Add("engineBrakeForce", 0f, 3000f, () => p.engineBrakeForce, v => p.engineBrakeForce = v);

            Add("maxSteerAngle", 15f, 45f, () => p.maxSteerAngle, v => p.maxSteerAngle = v);
            Add("steerSpeed", 30f, 300f, () => p.steerSpeed, v => p.steerSpeed = v);
            Add("steerReturnSpeed", 60f, 500f, () => p.steerReturnSpeed, v => p.steerReturnSpeed = v);
            Add("fullLockHoldTime", 0.2f, 2f, () => p.fullLockHoldTime, v => p.fullLockHoldTime = v);

            Add("frontGrip", 0.6f, 1.6f, () => p.frontGrip, v => p.frontGrip = v);
            Add("rearGrip", 0.6f, 1.6f, () => p.rearGrip, v => p.rearGrip = v);
            Add("frontPeakSlipDeg", 4f, 16f, () => p.frontPeakSlipDeg, v => p.frontPeakSlipDeg = v);
            Add("rearPeakSlipDeg (slide threshold)", 4f, 16f, () => p.rearPeakSlipDeg, v => p.rearPeakSlipDeg = v);
            Add("slideGripFraction", 0.4f, 1f, () => p.slideGripFraction, v => p.slideGripFraction = v);
            Add("slideFalloffRange", 0.5f, 5f, () => p.slideFalloffRange, v => p.slideFalloffRange = v);

            Add("brakeForce", 3000f, 16000f, () => p.brakeForce, v => p.brakeForce = v);
            Add("frontBrakeBias", 0.4f, 0.9f, () => p.frontBrakeBias, v => p.frontBrakeBias = v);
            Add("brakeRamp", 2f, 15f, () => p.brakeRamp, v => p.brakeRamp = v);
            Add("handbrakeForce", 0f, 8000f, () => p.handbrakeForce, v => p.handbrakeForce = v);
            Add("handbrakeRearGripMultiplier", 0.1f, 1f, () => p.handbrakeRearGripMultiplier, v => p.handbrakeRearGripMultiplier = v);

            Add("weightTransferFront", 0f, 3f, () => p.weightTransferFront, v => p.weightTransferFront = v);
            Add("weightTransferRear", 0f, 3f, () => p.weightTransferRear, v => p.weightTransferRear = v);
            Add("weightTransferSmoothing", 2f, 20f, () => p.weightTransferSmoothing, v => p.weightTransferSmoothing = v);

            Add("wetGripMultiplier", 0.4f, 1f, () => p.wetGripMultiplier, v => p.wetGripMultiplier = v);
            Add("offRoadGripMultiplier", 0.2f, 1f, () => p.offRoadGripMultiplier, v => p.offRoadGripMultiplier = v);
            Add("damagePerformanceLoss", 0f, 0.8f, () => p.damagePerformanceLoss, v => p.damagePerformanceLoss = v);
            Add("rollingResistance", 0.005f, 0.05f, () => p.rollingResistance, v => p.rollingResistance = v);
            Add("startSpeedKph (next restart)", 20f, 100f, () => p.startSpeedKph, v => p.startSpeedKph = v);

            if (cam != null)
            {
                Add("cam.distance", 4f, 14f, () => cam.distance, v => cam.distance = v);
                Add("cam.height", 2f, 12f, () => cam.height, v => cam.height = v);
                Add("cam.lookAhead", 0f, 20f, () => cam.lookAhead, v => cam.lookAhead = v);
                Add("cam.followCarHeading", 0f, 1f, () => cam.followCarHeading, v => cam.followCarHeading = v);
                Add("cam.yawSmoothing", 1f, 12f, () => cam.yawSmoothing, v => cam.yawSmoothing = v);
                Add("cam.fovSlow", 40f, 80f, () => cam.fovSlow, v => cam.fovSlow = v);
                Add("cam.fovFast", 40f, 90f, () => cam.fovFast, v => cam.fovFast = v);
            }

            Add("car damage (0..1, not saved)", 0f, 1f, () => car.Damage01, v => car.Damage01 = v, false);
        }

        void LoadSaved()
        {
            for (int i = 0; i < tunables.Count; i++)
            {
                Tunable t = tunables[i];
                if (!t.persist) continue;
                string key = "ML_T_" + t.name;
                if (PlayerPrefs.HasKey(key)) t.set(Mathf.Clamp(PlayerPrefs.GetFloat(key), t.min, t.max));
            }
        }

        void ResetAll()
        {
            for (int i = 0; i < tunables.Count; i++)
            {
                Tunable t = tunables[i];
                t.set(t.def);
                PlayerPrefs.DeleteKey("ML_T_" + t.name);
            }
        }

        string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Midnight Legacy tuning");
            for (int i = 0; i < tunables.Count; i++)
                sb.AppendLine(tunables[i].name + " = " + tunables[i].get().ToString("0.###"));
            return sb.ToString();
        }

        void Update()
        {
            if (car != null && profile != null) car.SurfaceGrip = wet ? profile.wetGripMultiplier : 1f;
        }

        void EnsureStyles()
        {
            if (stylesReady) return;
            labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.MiddleLeft };
            labelStyle.normal.textColor = Color.white;
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
            headerStyle.normal.textColor = new Color(0.62f, 0.68f, 1f);
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
            GUI.skin.horizontalSlider.fixedHeight = 24f;
            GUI.skin.horizontalSliderThumb.fixedHeight = 56f;
            GUI.skin.horizontalSliderThumb.fixedWidth = 56f;
            GUI.skin.verticalScrollbar.fixedWidth = 36f;
            GUI.skin.verticalScrollbarThumb.fixedWidth = 36f;
            stylesReady = true;
        }

        void OnGUI()
        {
            if (!Visible || car == null) return;
            EnsureStyles();

            float k = Screen.width / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f));
            float h = Screen.height / k;
            Rect panel = new Rect(0f, 0f, 1080f, h * 0.58f);

            Color old = GUI.color;
            GUI.color = new Color(0.03f, 0.03f, 0.08f, 0.88f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = old;

            GUILayout.BeginArea(new Rect(panel.x + 16f, panel.y + 12f, panel.width - 32f, panel.height - 24f));

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CLOSE", buttonStyle, GUILayout.Height(70f))) Visible = false;
            if (GUILayout.Button("RESTART", buttonStyle, GUILayout.Height(70f))) { if (restart != null) restart(); }
            if (GUILayout.Button(wet ? "WET: ON" : "WET: OFF", buttonStyle, GUILayout.Height(70f))) wet = !wet;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("LOW", buttonStyle, GUILayout.Height(70f))) SetQuality(QualityLevel.Low);
            if (GUILayout.Button("MED", buttonStyle, GUILayout.Height(70f))) SetQuality(QualityLevel.Medium);
            if (GUILayout.Button("HIGH", buttonStyle, GUILayout.Height(70f))) SetQuality(QualityLevel.High);
            if (GUILayout.Button("COPY VALUES", buttonStyle, GUILayout.Height(70f))) GUIUtility.systemCopyBuffer = Dump();
            if (GUILayout.Button("RESET", buttonStyle, GUILayout.Height(70f))) ResetAll();
            GUILayout.EndHorizontal();

            scroll = GUILayout.BeginScrollView(scroll, false, true);
            for (int i = 0; i < tunables.Count; i++)
            {
                Tunable t = tunables[i];
                float v = t.get();
                GUILayout.Label(t.name + ":  " + v.ToString("0.###"), labelStyle);
                float nv = GUILayout.HorizontalSlider(v, t.min, t.max, GUILayout.Height(60f));
                if (!Mathf.Approximately(nv, v))
                {
                    t.set(nv);
                    if (t.persist) PlayerPrefs.SetFloat("ML_T_" + t.name, nv);
                }
                GUILayout.Space(8f);
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        void SetQuality(QualityLevel level)
        {
            if (applyQuality != null) applyQuality(level);
        }
    }
}
