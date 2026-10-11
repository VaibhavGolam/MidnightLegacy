using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MidnightLegacy
{
    /// <summary>
    /// Keeps the frame rate steady on weak phones. While driving it measures the average frame rate every few
    /// seconds and lowers the render scale a little when the phone cannot keep up, and raises it back when there
    /// is room. The graphics level chosen by the player stays the ceiling.
    /// </summary>
    public sealed class AdaptiveQuality : MonoBehaviour
    {
        public const float MinScale = 0.55f;

        public bool Active;
        float timer, frames, lowWindows, highWindows;
        float scale = 1f;
        float baseScale = 1f;
        int warmup;

        public float CurrentScale { get { return scale; } }

        public void ResetToPreset()
        {
            baseScale = GraphicsSettings.Preset.renderScale;
            scale = baseScale;
            Push();
            timer = frames = 0f;
            lowWindows = highWindows = 0f;
            warmup = 0;
        }

        void Push()
        {
            UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
            if (urp != null) urp.renderScale = scale;
        }

        void Update()
        {
            if (!Active || Time.timeScale < 0.01f)
            {
                return;
            }

            // ignore the first moments after a run starts, chunks are still being built
            if (warmup < 90) { warmup++; return; }

            timer += Time.unscaledDeltaTime;
            frames += 1f;
            if (timer < 4f) return;

            float fps = frames / timer;
            timer = 0f;
            frames = 0f;

            float target = Mathf.Max(24, GraphicsSettings.Preset.targetFps);
            if (fps < target * 0.82f)
            {
                lowWindows += 1f;
                highWindows = 0f;
                if (lowWindows >= 2f && scale > MinScale)
                {
                    scale = Mathf.Max(MinScale, scale - 0.08f);
                    Push();
                    lowWindows = 0f;
                }
            }
            else if (fps > target * 0.97f)
            {
                highWindows += 1f;
                lowWindows = 0f;
                if (highWindows >= 4f && scale < baseScale)
                {
                    scale = Mathf.Min(baseScale, scale + 0.05f);
                    Push();
                    highWindows = 0f;
                }
            }
            else
            {
                lowWindows = 0f;
                highWindows = 0f;
            }
        }

        void OnDisable()
        {
            baseScale = GraphicsSettings.Preset.renderScale;
            scale = baseScale;
            Push();
        }
    }
}
