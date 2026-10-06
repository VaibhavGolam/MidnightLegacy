using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Feeds the night look to the shader every frame: moon, ambient, fog and the headlight pool.
    /// No real-time lights are used anywhere, which is what keeps this cheap on low end phones.
    /// Colours are sRGB here and converted to linear before they reach the shader.
    /// </summary>
    public sealed class NightLighting : MonoBehaviour
    {
        [Header("Night")]
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

        Transform headlight;
        Camera cam;

        static readonly int HeadPos = Shader.PropertyToID("_MLHeadPos");
        static readonly int HeadDir = Shader.PropertyToID("_MLHeadDir");
        static readonly int HeadColor = Shader.PropertyToID("_MLHeadColor");
        static readonly int Ambient = Shader.PropertyToID("_MLAmbient");
        static readonly int MoonColor = Shader.PropertyToID("_MLMoonColor");
        static readonly int MoonDir = Shader.PropertyToID("_MLMoonDir");
        static readonly int FogColor = Shader.PropertyToID("_MLFogColor");
        static readonly int FogParams = Shader.PropertyToID("_MLFogParams");

        public void Init(Transform headlightAnchor, Camera camera)
        {
            headlight = headlightAnchor;
            cam = camera;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fogColor;
            }
            Push();
        }

        public void SetFogEnd(float end)
        {
            fogEnd = end;
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
            float cosEdge = Mathf.Cos(coneDegrees * Mathf.Deg2Rad);
            Shader.SetGlobalVector(HeadPos, new Vector4(pos.x, pos.y, pos.z, headRange));
            Shader.SetGlobalVector(HeadDir, new Vector4(dir.x, dir.y, dir.z, cosEdge));
            Shader.SetGlobalColor(HeadColor, headColor.linear * headIntensity);
            Shader.SetGlobalColor(Ambient, ambient.linear);
            Shader.SetGlobalColor(MoonColor, moonColor.linear);
            Vector3 md = moonDirection.normalized;
            Shader.SetGlobalVector(MoonDir, new Vector4(md.x, md.y, md.z, 0f));
            Shader.SetGlobalColor(FogColor, fogColor.linear);
            Shader.SetGlobalVector(FogParams, new Vector4(fogStart, fogEnd, 0f, 0f));
        }
    }
}
