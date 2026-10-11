using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Creates the four materials the whole game uses. All of them use the MidnightLegacy/Stylized shader
    /// (kept in a Resources folder so Shader.Find works in builds).
    /// </summary>
    public static class MLMaterials
    {
        const string ShaderName = "MidnightLegacy/Stylized";
        static Material opaque;
        static Material blend;
        static Material additive;
        static Material trail;

        static Shader FindShader()
        {
            Shader s = Shader.Find(ShaderName);
            if (s == null)
            {
                Debug.LogError("[MidnightLegacy] Shader '" + ShaderName + "' not found. Make sure Assets/_Project/Resources/Shaders/MLStylized.shader is imported without errors and the project uses URP.");
                s = Shader.Find("Universal Render Pipeline/Unlit");
            }
            return s;
        }

        /// <summary>Opaque, vertex coloured, night lit. Vertex alpha = emissive amount.</summary>
        public static Material Opaque()
        {
            if (opaque == null)
            {
                opaque = new Material(FindShader());
                opaque.name = "ML_Opaque";
                opaque.SetFloat("_SrcBlend", (float)BlendMode.One);
                opaque.SetFloat("_DstBlend", (float)BlendMode.Zero);
                opaque.SetFloat("_ZWrite", 1f);
                opaque.SetFloat("_Unlit", 0f);
                opaque.renderQueue = (int)RenderQueue.Geometry;
            }
            return opaque;
        }

        /// <summary>Flat alpha blended material for shadows, smoke and rain. Vertex alpha = opacity, soft round mask.</summary>
        public static Material Blend()
        {
            if (blend == null)
            {
                blend = new Material(FindShader());
                blend.name = "ML_Blend";
                blend.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                blend.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                blend.SetFloat("_ZWrite", 0f);
                blend.SetFloat("_Unlit", 1f);
                blend.SetFloat("_Mask", 1f);
                blend.SetOverrideTag("RenderType", "Transparent");
                blend.renderQueue = (int)RenderQueue.Transparent;
            }
            return blend;
        }

        /// <summary>Additive glow for light pools, halos and flames. Vertex alpha = strength.</summary>
        public static Material Additive()
        {
            if (additive == null)
            {
                additive = new Material(FindShader());
                additive.name = "ML_Additive";
                additive.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                additive.SetFloat("_DstBlend", (float)BlendMode.One);
                additive.SetFloat("_ZWrite", 0f);
                additive.SetFloat("_Unlit", 1f);
                additive.SetFloat("_Additive", 1f);
                additive.SetFloat("_Mask", 1f);
                additive.SetOverrideTag("RenderType", "Transparent");
                additive.renderQueue = (int)RenderQueue.Transparent + 5;
            }
            return additive;
        }

        /// <summary>Alpha blended without the round mask: tyre marks and other stretched strips.</summary>
        public static Material Trail()
        {
            if (trail == null)
            {
                trail = new Material(FindShader());
                trail.name = "ML_Trail";
                trail.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                trail.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                trail.SetFloat("_ZWrite", 0f);
                trail.SetFloat("_Unlit", 1f);
                trail.SetFloat("_Mask", 0f);
                trail.SetOverrideTag("RenderType", "Transparent");
                trail.renderQueue = (int)RenderQueue.Transparent - 10;
            }
            return trail;
        }
    }
}
