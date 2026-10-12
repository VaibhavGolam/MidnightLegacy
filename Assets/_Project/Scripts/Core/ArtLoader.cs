using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>The painted props that live in the shared props atlas (Resources/Art/props_atlas).</summary>
    public enum Prop
    {
        TreeGreen, TreeOrange, TreeRed, TreeGold, Pine, BushGreen, BushAutumn, BushOlive, Rock, Lamp, Roof
    }

    /// <summary>
    /// Loads the painted art from Resources/Art and hands it to the rest of the game. Every lookup is optional:
    /// when a file is missing the caller gets null and keeps drawing its flat procedural shapes, so art can be
    /// dropped in piece by piece. Sprite materials use the MidnightLegacy/Sprite shader.
    /// </summary>
    public static class ArtLoader
    {
        /// <summary>Master switch (Settings could expose it). Off = the original flat shapes everywhere.</summary>
        public static bool Enabled = true;

        const string SpriteShaderName = "MidnightLegacy/Sprite";
        const string Folder = "Art/";

        // pixel size of every sprite, so the aspect ratio never depends on how Unity imported the texture
        static readonly Dictionary<string, Vector2> Sizes = new Dictionary<string, Vector2>
        {
            {"garage_bg", new Vector2(1672, 941)},
            {"menu_bg", new Vector2(941, 1672)},
            {"props_atlas", new Vector2(2048, 1024)},
            {"car_cover", new Vector2(1200, 360)},
            {"car_hatch", new Vector2(393, 720)},
            {"car_kairo", new Vector2(338, 720)},
            {"car_side_kairo", new Vector2(1400, 399)},
            {"car_wedge", new Vector2(378, 720)},
            {"title_logo", new Vector2(1024, 256)},
            {"traffic_bus", new Vector2(285, 900)},
            {"traffic_hatch", new Vector2(295, 560)},
            {"traffic_moto", new Vector2(168, 400)},
            {"traffic_sedan", new Vector2(278, 600)},
            {"traffic_taxi", new Vector2(310, 600)},
            {"traffic_truck", new Vector2(346, 800)},
            {"traffic_van", new Vector2(355, 640)},
            {"wheel_side_bar", new Vector2(256, 256)},
            {"wheel_side_five", new Vector2(256, 256)},
            {"wheel_side_mesh", new Vector2(254, 256)}
        };

        // rectangles in the props atlas (u, v, width, height), same order as the Prop enum
        static readonly Rect[] PropUv =
        {
            new Rect(0.00244f, 0.68457f, 0.15137f, 0.31055f), // TreeGreen  312x320
            new Rect(0.15869f, 0.68457f, 0.15137f, 0.31055f), // TreeOrange
            new Rect(0.31494f, 0.68457f, 0.15137f, 0.31055f), // TreeRed
            new Rect(0.47119f, 0.68457f, 0.15137f, 0.31055f), // TreeGold
            new Rect(0.62744f, 0.68457f, 0.15430f, 0.31055f), // Pine  318x320
            new Rect(0.00244f, 0.46484f, 0.10840f, 0.20996f), // BushGreen  224x217
            new Rect(0.11572f, 0.46484f, 0.10840f, 0.20996f), // BushAutumn
            new Rect(0.22900f, 0.46484f, 0.10840f, 0.20996f), // BushOlive
            new Rect(0.34229f, 0.45801f, 0.10400f, 0.21680f), // Rock  215x224
            new Rect(0.45117f, 0.60449f, 0.12402f, 0.07031f), // Lamp  256x74
            new Rect(0.58008f, 0.24414f, 0.21777f, 0.43066f)  // Roof  448x443
        };

        // width / height of each prop in pixels
        static readonly float[] PropAspect =
        {
            312f / 320f, 312f / 320f, 312f / 320f, 312f / 320f, 318f / 320f,
            224f / 217f, 224f / 217f, 224f / 217f, 215f / 224f, 256f / 74f, 448f / 443f
        };

        static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        static readonly HashSet<string> missing = new HashSet<string>();
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static Shader spriteShader;
        static bool shaderChecked;

        // ------------------------------------------------------------------ textures

        /// <summary>The texture Resources/Art/&lt;name&gt;, or null when art is off or the file is not there.</summary>
        public static Texture2D Tex(string name)
        {
            if (!Enabled) return null;
            Texture2D t;
            if (textures.TryGetValue(name, out t)) return t;
            if (missing.Contains(name)) return null;
            t = Resources.Load<Texture2D>(Folder + name);
            if (t == null)
            {
                missing.Add(name);
                return null;
            }
            t.wrapMode = TextureWrapMode.Clamp;
            textures[name] = t;
            return t;
        }

        public static bool Has(string name)
        {
            return Tex(name) != null;
        }

        /// <summary>Pixel size of a sprite from the table, or the texture size for files that are not in it.</summary>
        public static Vector2 PixelSize(string name)
        {
            Vector2 v;
            if (Sizes.TryGetValue(name, out v)) return v;
            Texture2D t = Tex(name);
            return t != null ? new Vector2(t.width, t.height) : new Vector2(1f, 1f);
        }

        /// <summary>width / height of a sprite.</summary>
        public static float Aspect(string name)
        {
            Vector2 v = PixelSize(name);
            return v.x / Mathf.Max(1f, v.y);
        }

        /// <summary>
        /// Width and length (in metres) for a vehicle sprite that should be `length` long. The width follows the picture's
        /// own shape, pulled toward `width` by widthBlend (0 = the picture's shape, 1 = exactly `width`).
        /// </summary>
        public static Vector2 FitVehicle(string name, float length, float width, float widthBlend)
        {
            float own = length * Aspect(name);
            return new Vector2(Mathf.Lerp(own, width, widthBlend), length);
        }

        // ------------------------------------------------------------------ props atlas

        public static Rect PropRect(Prop p)
        {
            return PropUv[(int)p];
        }

        public static float PropAspectOf(Prop p)
        {
            return PropAspect[(int)p];
        }

        public static bool HasProps
        {
            get { return Has("props_atlas") && SpriteShader() != null; }
        }

        // ------------------------------------------------------------------ materials

        static Shader SpriteShader()
        {
            if (!shaderChecked)
            {
                shaderChecked = true;
                spriteShader = Shader.Find(SpriteShaderName);
                if (spriteShader == null)
                    Debug.LogWarning("[MidnightLegacy] Shader '" + SpriteShaderName + "' not found. Painted art is switched off, the flat shapes are used instead.");
            }
            return spriteShader;
        }

        /// <summary>
        /// Alpha blended sprite material for a texture. queueOffset is added to the Transparent queue:
        /// scenery -20, the road cars and traffic +1 (after their soft shadows). light = how much night lighting applies.
        /// Returns null when the texture or the shader is missing.
        /// </summary>
        public static Material Sprite(string name, int queueOffset, float light = 1f)
        {
            Texture2D tex = Tex(name);
            if (tex == null) return null;
            Shader sh = SpriteShader();
            if (sh == null) return null;
            string key = name + "|" + queueOffset + "|" + light;
            Material m;
            if (materials.TryGetValue(key, out m) && m != null) return m;
            m = new Material(sh);
            m.name = "ML_Sprite_" + name;
            m.mainTexture = tex;
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Light", light);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent + queueOffset;
            materials[key] = m;
            return m;
        }

        // ------------------------------------------------------------------ small helpers

        /// <summary>A one sprite mesh for a vehicle: centred, `size` = (width, length) in metres, tinted.</summary>
        public static Mesh VehicleMesh(string name, Vector2 size, float y, Color tint)
        {
            SpriteBatch sb = new SpriteBatch();
            sb.Add(0f, 0f, y, size.x * 0.5f, size.y * 0.5f, 0f, new Rect(0f, 0f, 1f, 1f), tint);
            return sb.ToMesh(name);
        }

        /// <summary>Clears the caches (used when the whole game restarts in the editor).</summary>
        public static void ResetCaches()
        {
            textures.Clear();
            missing.Clear();
            materials.Clear();
            shaderChecked = false;
            spriteShader = null;
        }
    }
}
