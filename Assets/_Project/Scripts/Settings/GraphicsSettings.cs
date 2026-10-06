using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MidnightLegacy
{
    public enum QualityLevel { Low = 0, Medium = 1, High = 2 }

    /// <summary>One graphics preset. Plain class so it needs no asset; move to a ScriptableObject later if wanted.</summary>
    public sealed class QualityPreset
    {
        public QualityLevel level;
        public int targetFps;
        public float renderScale;
        public int msaa;
        public int chunksAhead;       // 80 m chunks generated in front of the car
        public float treeDensity;     // multiplies the recipe's tree density
        public float fogEnd;
        public bool smoke;
    }

    public static class GraphicsSettings
    {
        const string PrefKey = "ML_Quality";

        static readonly QualityPreset[] presets =
        {
            new QualityPreset { level = QualityLevel.Low,    targetFps = 30, renderScale = 0.70f, msaa = 1, chunksAhead = 3, treeDensity = 0.55f, fogEnd = 170f, smoke = false },
            new QualityPreset { level = QualityLevel.Medium, targetFps = 60, renderScale = 0.85f, msaa = 2, chunksAhead = 4, treeDensity = 0.85f, fogEnd = 230f, smoke = true },
            new QualityPreset { level = QualityLevel.High,   targetFps = 60, renderScale = 1.00f, msaa = 4, chunksAhead = 5, treeDensity = 1.20f, fogEnd = 300f, smoke = true }
        };

        public static QualityLevel Current { get; private set; }
        public static QualityPreset Preset { get { return presets[(int)Current]; } }

        /// <summary>Loads the saved level, or guesses one from the phone's memory the first time.</summary>
        public static QualityLevel LoadSaved()
        {
            if (PlayerPrefs.HasKey(PrefKey)) return (QualityLevel)Mathf.Clamp(PlayerPrefs.GetInt(PrefKey), 0, 2);
            int ram = SystemInfo.systemMemorySize;
            if (ram > 0 && ram < 4000) return QualityLevel.Low;
            if (ram > 0 && ram < 6500) return QualityLevel.Medium;
            return QualityLevel.High;
        }

        /// <summary>Applies the engine side of a level (frame rate, render scale, MSAA). Scene objects read Preset themselves.</summary>
        public static void Apply(QualityLevel level, bool save)
        {
            Current = level;
            QualityPreset p = presets[(int)level];
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = p.targetFps;

            UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
            if (urp != null)
            {
                urp.renderScale = p.renderScale;
                urp.msaaSampleCount = p.msaa;
            }

            if (save)
            {
                PlayerPrefs.SetInt(PrefKey, (int)level);
                PlayerPrefs.Save();
            }
        }
    }
}
