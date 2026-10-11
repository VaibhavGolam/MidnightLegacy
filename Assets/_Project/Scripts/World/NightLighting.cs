using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Feeds the look to the shader every frame: moon or sun, ambient, fog and the headlight pool.
    /// No real-time lights are used anywhere, which is what keeps this cheap on low end phones.
    /// Colours are sRGB here and converted to linear before they reach the shader.
    /// ApplyEnvironment switches between night, midnight, dusk, dawn and the garage.
    /// In the top-down driving view (topDown = true) the fog becomes a soft vignette around the screen centre,
    /// the light is lifted a little so the flat colours stay readable, and the headlight pool is shorter.
    /// </summary>
    public sealed class NightLighting : MonoBehaviour
    {
        [Header("Look")]
        public Color ambient = new Color(0.30f, 0.30f, 0.46f);
        public Color moonColor = new Color(0.30f, 0.34f, 0.55f);
        public Vector3 moonDirection = new Vector3(-0.35f, 0.8f, 0.45f);
        public Color fogColor = new Color(0.07f, 0.08f, 0.17f);
        public float fogStart = 35f;
        public float fogEnd = 230f;

        [Header("Headlights")]
        public Color headColor = new Color(1f, 0.80f, 0.50f);
        public float headIntensity = 2.2f;
        public float headRange = 42f;
        [Range(15f, 60f)] public float coneDegrees = 36f;

        [Header("Top-down view")]
        public bool topDown;
        public float topDownLightBoost = 1.75f;
        public float topDownFogStart = 11f;
        public float topDownFogEnd = 31f;
        public float topDownHeadRangeScale = 0.65f;

        Transform headlight;
        Camera cam;
        float baseFogEnd = 230f;
        TimeDef time;
        WeatherDef weather;

        static readonly int HeadPos = Shader.PropertyToID("_MLHeadPos");
        static readonly int HeadDir = Shader.PropertyToID("_MLHeadDir");
        static readonly int HeadColor = Shader.PropertyToID("_MLHeadColor");
        static readonly int Ambient = Shader.PropertyToID("_MLAmbient");
        static readonly int MoonColor = Shader.PropertyToID("_MLMoonColor");
        static readonly int MoonDir = Shader.PropertyToID("_MLMoonDir");
        static readonly int FogColor = Shader.PropertyToID("_MLFogColor");
        static readonly int FogParams = Shader.PropertyToID("_MLFogParams");
        static readonly int ViewParams = Shader.PropertyToID("_MLView");

        public void Init(Transform headlightAnchor, Camera camera)
        {
            headlight = headlightAnchor;
            cam = camera;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fogColor;
            }
            RecomputeFog();
            Push();
        }

        public void SetHeadlight(Transform anchor)
        {
            headlight = anchor;
        }

        /// <summary>Distance where the fog is fully opaque for the current graphics level (before time and weather scaling). Ignored in the top-down view.</summary>
        public void SetFogEnd(float end)
        {
            baseFogEnd = end;
            RecomputeFog();
        }

        public void ApplyEnvironment(TimeDef t, WeatherDef w)
        {
            time = t;
            weather = w;
            if (t != null)
            {
                float mul = w != null ? w.lightMultiplier : 1f;
                ambient = t.ambient * mul;
                moonColor = t.moon * mul;
                moonDirection = t.moonDirection;
                fogColor = t.fog;
                fogStart = t.fogStart;
                headIntensity = t.headIntensity;
                headRange = t.headRange;
            }
            RecomputeFog();
            Push();
        }

        void RecomputeFog()
        {
            float mul = 1f;
            if (time != null) mul *= time.fogEndMultiplier;
            if (weather != null) mul *= weather.fogMultiplier;

            if (topDown)
            {
                // clear nights keep a gentle edge; fog and storms close the view in
                fogStart = topDownFogStart * Mathf.Min(1f, 0.55f + 0.45f * mul);
                fogEnd = Mathf.Max(fogStart + 4f, topDownFogEnd * mul);
                return;
            }
            fogEnd = Mathf.Max(fogStart + 30f, baseFogEnd * mul);
        }

        void LateUpdate()
        {
            Push();
        }

        void Push()
        {
            if (cam != null) cam.backgroundColor = fogColor;

            Vector3 pos = Vector3.zero, dir = Vector3.forward;
            if (headlight != null)
            {
                pos = headlight.position;
                dir = headlight.forward;
            }
            float boost = topDown ? topDownLightBoost : 1f;
            float range = topDown ? headRange * topDownHeadRangeScale : headRange;
            float cone = topDown ? Mathf.Max(coneDegrees, 42f) : coneDegrees;
            float cosEdge = Mathf.Cos(cone * Mathf.Deg2Rad);

            Shader.SetGlobalVector(HeadPos, new Vector4(pos.x, pos.y, pos.z, range));
            Shader.SetGlobalVector(HeadDir, new Vector4(dir.x, dir.y, dir.z, cosEdge));
            Shader.SetGlobalColor(HeadColor, headColor.linear * headIntensity);
            Shader.SetGlobalColor(Ambient, ambient.linear * boost);
            Shader.SetGlobalColor(MoonColor, moonColor.linear * boost);
            Vector3 md = moonDirection.normalized;
            Shader.SetGlobalVector(MoonDir, new Vector4(md.x, md.y, md.z, 0f));
            Shader.SetGlobalColor(FogColor, fogColor.linear);
            Shader.SetGlobalVector(FogParams, new Vector4(fogStart, fogEnd, 0f, 0f));
            Shader.SetGlobalVector(ViewParams, new Vector4(topDown ? 1f : 0f, 0f, 0f, 0f));
        }
    }
}
