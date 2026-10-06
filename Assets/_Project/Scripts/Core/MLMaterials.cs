using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Creates the two materials the whole prototype uses. Both use the MidnightLegacy/Stylized shader
    /// (kept in a Resources folder so Shader.Find works in builds).
    /// </summary>
    public static class MLMaterials
    {
        const string ShaderName = "MidnightLegacy/Stylized";
        static Material opaque;
        static Material blend;

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

        /// <summary>Flat alpha blended material for blob shadows and smoke. Vertex alpha = opacity.</summary>
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
                blend.SetOverrideTag("RenderType", "Transparent");
                blend.renderQueue = (int)RenderQueue.Transparent;
            }
            return blend;
        }
    }
}
