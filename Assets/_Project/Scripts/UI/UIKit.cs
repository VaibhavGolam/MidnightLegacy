using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    public enum BtnStyle { Primary, Secondary, Gold, Danger, Ghost }

    /// <summary>
    /// A tiny immediate mode UI toolkit on top of IMGUI. Everything is drawn in a fixed 1080 unit wide space that
    /// is scaled to the screen, so layouts look the same on every phone. It adds what plain IMGUI lacks on touch:
    /// a safe area, rounded panels, drag scrolling with inertia, and buttons that do not fire after a drag.
    /// Call Begin() first in OnGUI and End() last.
    /// </summary>
    public static class UIKit
    {
        public const float RefWidth = 1080f;

        // ---- palette ----
        public static readonly Color Bg = new Color(0.035f, 0.04f, 0.09f);
        public static readonly Color Panel = new Color(0.075f, 0.085f, 0.17f, 0.94f);
        public static readonly Color PanelLight = new Color(0.12f, 0.135f, 0.25f, 0.96f);
        public static readonly Color Accent = new Color(0.42f, 0.47f, 0.84f);
        public static readonly Color AccentDark = new Color(0.27f, 0.31f, 0.62f);
        public static readonly Color Gold = new Color(1f, 0.76f, 0.26f);
        public static readonly Color Good = new Color(0.32f, 0.82f, 0.52f);
        public static readonly Color Bad = new Color(0.92f, 0.34f, 0.34f);
        public static readonly Color Text = new Color(0.94f, 0.95f, 0.99f);
        public static readonly Color Dim = new Color(0.62f, 0.65f, 0.80f);
        public static readonly Color Shade = new Color(0f, 0f, 0f, 0.55f);
        /// <summary>The electric blue of panel edges and glows.</summary>
        public static readonly Color EdgeBlue = new Color(0.28f, 0.45f, 1f);

        public static float W { get; private set; }
        public static float H { get; private set; }
        public static float Scale { get; private set; }
        /// <summary>Safe area in UI units (notches, rounded corners, gesture bar).</summary>
        public static Rect Safe { get; private set; }

        static Texture2D white, roundTex, logoTex, circleTex, gradTex, gradHTex, ringTex;
        static Font headFont, bodyFont;
        static bool fontsLoaded;
        // Rajdhani has a small x-height, so body text is drawn a little larger to look the same size as the old font
        const float BodyScale = 1.14f;
        static GUIStyle boxStyle, ringStyle;
        static readonly Dictionary<int, GUIStyle> labelStyles = new Dictionary<int, GUIStyle>();
        static GUIStyle fieldStyle;
        static Matrix4x4 savedMatrix;
        static Color savedColor;

        // ---- input state ----
        static int idCounter;
        static int pressId = -1;
        static float dragTotal;
        static int sliderId = -1;
        static Rect clip;
        static bool clipActive;
        static int scrollId = -1;
        static int scrollCount;
        static readonly float[] scrollVel = new float[8];
        static bool scrollTouched;

        /// <summary>True while a finger or the mouse is held down.</summary>
        public static bool PointerDown { get; private set; }

        // ------------------------------------------------------------------ frame

        public static void Begin()
        {
            savedMatrix = GUI.matrix;
            savedColor = GUI.color;
            Scale = Screen.width / RefWidth;
            W = RefWidth;
            H = Screen.height / Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Scale, Scale, 1f));

            Rect sa = Screen.safeArea;
            float top = (Screen.height - sa.yMax) / Scale;
            float bottom = (sa.yMin) / Scale;
            Safe = new Rect(sa.xMin / Scale, top, sa.width / Scale, H - top - bottom);

            idCounter = 0;
            scrollCount = 0;
            clipActive = false;
            EnsureResources();

            Event e = Event.current;
            if (e.type == EventType.MouseDown) { dragTotal = 0f; PointerDown = true; scrollId = -1; scrollTouched = false; }
            else if (e.type == EventType.MouseDrag) dragTotal += e.delta.magnitude;
            else if (e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow) { PointerDown = false; }
        }

        public static void End()
        {
            Event e = Event.current;
            if (e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow)
            {
                pressId = -1;
                sliderId = -1;
            }
            GUI.matrix = savedMatrix;
            GUI.color = savedColor;
        }

        // ------------------------------------------------------------------ resources

        static void EnsureResources()
        {
            if (white == null)
            {
                white = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                white.Apply();
                white.hideFlags = HideFlags.HideAndDontSave;
            }
            if (roundTex == null)
            {
                const int n = 64;
                const float rad = 22f;
                roundTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                roundTex.filterMode = FilterMode.Bilinear;
                roundTex.wrapMode = TextureWrapMode.Clamp;
                Color[] px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, rad, n - rad);
                        float cy = Mathf.Clamp(y + 0.5f, rad, n - rad);
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        float a = Mathf.Clamp01(rad - d + 0.5f);
                        px[y * n + x] = new Color(1f, 1f, 1f, a);
                    }
                roundTex.SetPixels(px);
                roundTex.Apply();
                roundTex.hideFlags = HideFlags.HideAndDontSave;
            }
            if (circleTex == null)
            {
                const int n = 128;
                circleTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                circleTex.filterMode = FilterMode.Bilinear;
                circleTex.wrapMode = TextureWrapMode.Clamp;
                Color[] px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = x + 0.5f - n * 0.5f, dy = y + 0.5f - n * 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(n * 0.5f - d));
                    }
                circleTex.SetPixels(px);
                circleTex.Apply();
                circleTex.hideFlags = HideFlags.HideAndDontSave;
            }
            if (gradTex == null)
            {
                // a vertical ramp: opaque at the top, clear at the bottom
                const int n = 64;
                gradTex = new Texture2D(1, n, TextureFormat.RGBA32, false);
                gradTex.filterMode = FilterMode.Bilinear;
                gradTex.wrapMode = TextureWrapMode.Clamp;
                Color[] px = new Color[n];
                for (int y = 0; y < n; y++)
                {
                    float t = y / (float)(n - 1);          // 0 at the bottom row, 1 at the top row
                    px[y] = new Color(1f, 1f, 1f, t * t * (3f - 2f * t));
                }
                gradTex.SetPixels(px);
                gradTex.Apply();
                gradTex.hideFlags = HideFlags.HideAndDontSave;
            }
            if (ringTex == null)
            {
                // the outline of the rounded box, two units thick
                const int n = 64;
                const float rad = 22f;
                ringTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                ringTex.filterMode = FilterMode.Bilinear;
                ringTex.wrapMode = TextureWrapMode.Clamp;
                Color[] px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, rad, n - rad);
                        float cy = Mathf.Clamp(y + 0.5f, rad, n - rad);
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        float a = Mathf.Clamp01(d - (rad - 2f) + 0.5f) * Mathf.Clamp01(rad - d + 0.5f);
                        px[y * n + x] = new Color(1f, 1f, 1f, a);
                    }
                ringTex.SetPixels(px);
                ringTex.Apply();
                ringTex.hideFlags = HideFlags.HideAndDontSave;
            }
            if (gradHTex == null)
            {
                // a horizontal ramp: opaque on the left, clear on the right
                const int n = 64;
                gradHTex = new Texture2D(n, 1, TextureFormat.RGBA32, false);
                gradHTex.filterMode = FilterMode.Bilinear;
                gradHTex.wrapMode = TextureWrapMode.Clamp;
                Color[] px = new Color[n];
                for (int x = 0; x < n; x++)
                {
                    float t = 1f - x / (float)(n - 1);
                    px[x] = new Color(1f, 1f, 1f, t * t * (3f - 2f * t));
                }
                gradHTex.SetPixels(px);
                gradHTex.Apply();
                gradHTex.hideFlags = HideFlags.HideAndDontSave;
            }
            if (!fontsLoaded)
            {
                // racing look: bold italic for titles and numbers, a clean condensed face for text. Falls back to the default font.
                fontsLoaded = true;
                headFont = Resources.Load<Font>("Fonts/ChakraPetch-BoldItalic");
                bodyFont = Resources.Load<Font>("Fonts/Rajdhani-SemiBold");
            }
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle();
                boxStyle.normal.background = roundTex;
                boxStyle.border = new RectOffset(24, 24, 24, 24);
            }
            if (ringStyle == null)
            {
                ringStyle = new GUIStyle();
                ringStyle.normal.background = ringTex;
                ringStyle.border = new RectOffset(24, 24, 24, 24);
            }
            if (logoTex == null) logoTex = BuildLogo();
        }

        /// <summary>The ST Media badge: a rounded triangle in periwinkle with a white outline (drawn in code, no image file).</summary>
        static Texture2D BuildLogo()
        {
            const int n = 256;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[n * n];
            Color fill = new Color(0.42f, 0.47f, 0.84f);
            const float k = 1.7320508f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float px0 = ((x + 0.5f) / n * 2f - 1f) * 1.12f;
                    float py0 = ((y + 0.5f) / n * 2f - 1f) * 1.12f + 0.20f;
                    // signed distance to an equilateral triangle (apex up), rounded by subtracting a radius
                    float qx = Mathf.Abs(px0) - 0.62f;
                    float qy = py0 + 0.62f / k;
                    if (qx + k * qy > 0f)
                    {
                        float nx = (qx - k * qy) * 0.5f;
                        float ny = (-k * qx - qy) * 0.5f;
                        qx = nx; qy = ny;
                    }
                    qx -= Mathf.Clamp(qx, -1.24f, 0f);
                    float len = Mathf.Sqrt(qx * qx + qy * qy);
                    float d = (qy > 0f ? -len : len) - 0.16f;
                    float a = Mathf.Clamp01(0.5f - d / 0.012f);
                    float ring = Mathf.Clamp01((d + 0.075f) / 0.012f);   // 1 near the edge: white outline
                    Color c = Color.Lerp(fill, Color.white, ring);
                    px[y * n + x] = new Color(c.r, c.g, c.b, a);
                }
            t.SetPixels(px);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        // ------------------------------------------------------------------ drawing

        public static void Fill(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = Color.white;
        }

        public static void Round(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = c;
            boxStyle.Draw(r, GUIContent.none, false, false, false, false);
            GUI.color = Color.white;
        }

        public static void Circle(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = c;
            GUI.DrawTexture(r, circleTex);
            GUI.color = Color.white;
        }

        /// <summary>The thin outline of a rounded box.</summary>
        public static void Outline(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = c;
            ringStyle.Draw(r, GUIContent.none, false, false, false, false);
            GUI.color = Color.white;
        }

        public static void Card(Rect r)
        {
            if (Skin(r, "ui_panel", 44f, 0.2f, Color.white)) return;
            Round(r, Panel);
            Glow(r, EdgeBlue, 0.12f);
            Outline(r, new Color(EdgeBlue.r, EdgeBlue.g, EdgeBlue.b, 0.55f));
        }

        /// <summary>A smaller card (list rows, stat tiles): fill plus the same thin outline as Card.</summary>
        public static void Tile(Rect r, Color fill)
        {
            // the painted panel is dark and neutral: only use it for dark fills, coloured tiles keep their colour
            if (Mathf.Max(fill.r, Mathf.Max(fill.g, fill.b)) < 0.30f && Skin(r, "ui_panel", 34f, 0.2f, new Color(1f, 1f, 1f, Mathf.Clamp01(fill.a * 1.2f)))) return;
            Round(r, fill);
            Outline(r, new Color(EdgeBlue.r, EdgeBlue.g, EdgeBlue.b, fill.a < 0.5f ? 0.20f : 0.38f));
        }

        /// <summary>A soft gradient: the colour c (with its own alpha) at the top edge fading to clear at the bottom, or the other way round.</summary>
        public static void Fade(Rect r, Color c, bool opaqueAtTop)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = c;
            if (opaqueAtTop) GUI.DrawTexture(r, gradTex);
            else GUI.DrawTextureWithTexCoords(r, gradTex, new Rect(0f, 1f, 1f, -1f));
            GUI.color = Color.white;
        }

        /// <summary>Darkens the edges of the screen so text on a painted background stays readable.</summary>
        public static void Vignette(float top, float bottom)
        {
            Rect t = new Rect(0f, 0f, W, H * 0.34f);
            Fade(t, new Color(0f, 0f, 0.02f, top), true);
            Rect b = new Rect(0f, H * 0.42f, W, H * 0.58f);
            Fade(b, new Color(0f, 0f, 0.02f, bottom), false);
        }

        /// <summary>
        /// Draws a painted panel or button from Resources/Art in nine pieces, so the corners keep their shape at any size.
        /// corner = size of the corners on screen (UI units). srcFrac = how much of the picture's height the corner takes
        /// (0.2 = a fifth). Returns false, and draws nothing, when the picture is not in the project.
        /// </summary>
        public static bool Skin(Rect r, string name, float corner, float srcFrac, Color tint)
        {
            Texture2D t = ArtLoader.Tex(name);
            if (t == null) return false;
            if (Event.current.type != EventType.Repaint) return true;
            float c = Mathf.Min(corner, r.width * 0.5f, r.height * 0.5f);
            float vy = Mathf.Clamp(srcFrac, 0.02f, 0.49f);
            float vx = Mathf.Clamp(srcFrac * t.height / Mathf.Max(1f, t.width), 0.02f, 0.49f);
            float[] dx = { r.x, r.x + c, r.xMax - c, r.xMax };
            float[] dy = { r.y, r.y + c, r.yMax - c, r.yMax };                 // top to bottom
            float[] ux = { 0f, vx, 1f - vx, 1f };
            float[] uy = { 1f, 1f - vy, vy, 0f };                              // texture rows run bottom to top
            GUI.color = tint;
            for (int j = 0; j < 3; j++)
                for (int i = 0; i < 3; i++)
                {
                    Rect d = new Rect(dx[i], dy[j], dx[i + 1] - dx[i], dy[j + 1] - dy[j]);
                    if (d.width <= 0f || d.height <= 0f) continue;
                    Rect u = new Rect(ux[i], uy[j + 1], ux[i + 1] - ux[i], uy[j] - uy[j + 1]);
                    GUI.DrawTextureWithTexCoords(d, t, u);
                }
            GUI.color = Color.white;
            return true;
        }

        /// <summary>A painted icon (Resources/Art) drawn inside r, keeping its shape. Returns false when the picture is missing.</summary>
        public static bool Icon(Rect r, string name, Color tint, bool flipX = false)
        {
            Texture2D t = ArtLoader.Tex(name);
            if (t == null) return false;
            if (Event.current.type != EventType.Repaint) return true;
            float a = t.width / Mathf.Max(1f, (float)t.height);
            Rect d = r;
            if (a > r.width / r.height) { d.height = r.width / a; d.y = r.y + (r.height - d.height) * 0.5f; }
            else { d.width = r.height * a; d.x = r.x + (r.width - d.width) * 0.5f; }
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(d, t, flipX ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f));
            GUI.color = Color.white;
            return true;
        }

        /// <summary>A disc filled from the bottom up (0..1): used for the nitro tank inside its button.</summary>
        public static void DiscFill(Rect r, Color c, float fill01)
        {
            if (Event.current.type != EventType.Repaint) return;
            float f = Mathf.Clamp01(fill01);
            if (f <= 0.002f) return;
            GUI.color = c;
            Rect d = new Rect(r.x, r.yMax - r.height * f, r.width, r.height * f);
            GUI.DrawTextureWithTexCoords(d, circleTex, new Rect(0f, 0f, 1f, f));
            GUI.color = Color.white;
        }

        /// <summary>A soft outer glow round a rounded box: a few outlines growing outward and fading.</summary>
        public static void Glow(Rect r, Color c, float strength)
        {
            if (Event.current.type != EventType.Repaint) return;
            for (int i = 1; i <= 4; i++)
            {
                float g = i * 3f;
                Outline(new Rect(r.x - g, r.y - g, r.width + 2f * g, r.height + 2f * g), new Color(c.r, c.g, c.b, strength * (1f - i / 5f)));
            }
        }

        /// <summary>Same as Fade but sideways: the colour is solid on the left edge and clear on the right (or the other way round).</summary>
        public static void FadeH(Rect r, Color c, bool opaqueAtLeft)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.color = c;
            if (opaqueAtLeft) GUI.DrawTexture(r, gradHTex);
            else GUI.DrawTextureWithTexCoords(r, gradHTex, new Rect(1f, 0f, -1f, 1f));
            GUI.color = Color.white;
        }

        /// <summary>A bar made of separate blocks (rating bars, condition). The filled blocks may be partly filled.</summary>
        public static void SegBar(Rect r, float v01, int segments, Color fill, Color back)
        {
            float gap = Mathf.Max(3f, r.height * 0.22f);
            float sw = (r.width - gap * (segments - 1)) / segments;
            float v = Mathf.Clamp01(v01) * segments;
            for (int i = 0; i < segments; i++)
            {
                Rect sr = new Rect(r.x + i * (sw + gap), r.y, sw, r.height);
                Round(sr, back);
                float f = Mathf.Clamp01(v - i);
                if (f > 0.02f) Round(new Rect(sr.x, sr.y, Mathf.Max(r.height * 0.5f, sw * f), sr.height), fill);
            }
        }

        /// <summary>A straight bar of any length and angle (degrees, clockwise), centred on c. The building block of the drawn icons.</summary>
        public static void Stroke(Vector2 c, float length, float thickness, float angle, Color col)
        {
            if (Event.current.type != EventType.Repaint) return;
            Matrix4x4 saved = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, c);
            Round(new Rect(c.x - length * 0.5f, c.y - thickness * 0.5f, length, thickness), col);
            GUI.matrix = saved;
        }

        /// <summary>A section title in spaced capitals with a thin line running to the right and three slashes at the end.</summary>
        public static void SectionHeader(Rect r, string text, Color c)
        {
            string spaced = Spaced(text);
            Label(new Rect(r.x, r.y, r.width, r.height), spaced, Mathf.RoundToInt(r.height * 0.62f), c, TextAnchor.MiddleLeft, true);
            float tw = TextWidth(spaced, Mathf.RoundToInt(r.height * 0.62f), true);
            float lx = r.x + tw + 24f, lw = r.xMax - lx - 70f;
            if (lw > 20f) Fill(new Rect(lx, r.center.y - 1f, lw, 2f), new Color(c.r, c.g, c.b, 0.55f));
            Slashes(new Rect(r.xMax - 60f, r.y + r.height * 0.18f, 60f, r.height * 0.64f), c);
        }

        /// <summary>Three leaning bars, the racing stripe motif.</summary>
        public static void Slashes(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            for (int i = 0; i < 3; i++)
                Stroke(new Vector2(r.x + 12f + i * 18f, r.center.y), r.height, 7f, 22f, new Color(c.r, c.g, c.b, 0.9f - i * 0.22f));
        }

        static string Spaced(string t)
        {
            if (string.IsNullOrEmpty(t) || t.Length > 24) return t;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(t.Length * 2);
            for (int i = 0; i < t.Length; i++)
            {
                sb.Append(t[i]);
                if (i < t.Length - 1 && t[i] != ' ' && t[i + 1] != ' ') sb.Append(' ');
            }
            return sb.ToString();
        }

        public static float TextWidth(string text, int size, bool bold = false)
        {
            GUIStyle s = LabelStyle(size, bold, TextAnchor.UpperLeft, false);
            return s.CalcSize(new GUIContent(text)).x;
        }

        /// <summary>A small gold coin with a dollar sign.</summary>
        public static void Coin(Rect r)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (Icon(r, "ui_coin", Color.white)) return;
            Circle(r, new Color(1f, 0.82f, 0.34f));
            Circle(new Rect(r.x + r.width * 0.09f, r.y + r.height * 0.09f, r.width * 0.82f, r.height * 0.82f), new Color(0.96f, 0.66f, 0.14f));
            Label(r, "$", Mathf.RoundToInt(r.height * 0.58f), new Color(0.35f, 0.22f, 0.02f), TextAnchor.MiddleCenter, true);
        }

        /// <summary>Painted art by file name (Resources/Art), or null when the file is not in the project.</summary>
        public static Texture2D Art(string name)
        {
            return ArtLoader.Tex(name);
        }

        /// <summary>Character portrait for a character id (char_jun and so on), or null.</summary>
        public static Texture2D Portrait(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            return ArtLoader.Tex("char_" + characterId);
        }

        /// <summary>Draws a picture with its own shape inside r (letterboxed), alpha blended.</summary>
        public static void Image(Rect r, Texture2D t, Color tint)
        {
            if (Event.current.type != EventType.Repaint || t == null) return;
            GUI.color = tint;
            GUI.DrawTexture(r, t, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        /// <summary>
        /// Fills r with a picture, cropping what does not fit (like CSS background-size: cover).
        /// zoom 1 = just fits, above 1 zooms in. (fx, fy) is the point to keep centred, 0..1 from the left and from the top.
        /// </summary>
        public static void Cover(Rect r, Texture2D t, float aspect, Color tint, float zoom = 1f, float fx = 0.5f, float fy = 0.5f)
        {
            if (Event.current.type != EventType.Repaint || t == null) return;
            float ra = r.width / Mathf.Max(1f, r.height);
            float uw = 1f, vh = 1f;
            if (ra > aspect) vh = aspect / ra; else uw = ra / aspect;
            zoom = Mathf.Max(1f, zoom);
            uw /= zoom; vh /= zoom;
            float u0 = Mathf.Clamp(fx - uw * 0.5f, 0f, 1f - uw);
            float v0 = Mathf.Clamp((1f - fy) - vh * 0.5f, 0f, 1f - vh);
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(r, t, new Rect(u0, v0, uw, vh));
            GUI.color = Color.white;
        }

        /// <summary>A small rounded label chip (cash, rep, status).</summary>
        public static void Chip(Rect r, string text, Color fill, Color fg, int size = 26)
        {
            Round(r, fill);
            Outline(r, new Color(1f, 1f, 1f, 0.10f));
            Label(r, text, size, fg, TextAnchor.MiddleCenter, true);
        }

        static GUIStyle LabelStyle(int size, bool bold, TextAnchor anchor, bool wrap)
        {
            int key = size * 64 + (int)anchor * 4 + (bold ? 2 : 0) + (wrap ? 1 : 0);
            GUIStyle s;
            if (!labelStyles.TryGetValue(key, out s))
            {
                s = new GUIStyle();
                Font f = bold ? headFont : bodyFont;
                if (f != null)
                {
                    s.font = f;
                    s.fontSize = bold ? size : Mathf.RoundToInt(size * BodyScale);
                    s.fontStyle = FontStyle.Normal;       // the weight and slant are in the font files
                }
                else
                {
                    s.fontSize = size;
                    s.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
                }
                s.alignment = anchor;
                s.wordWrap = wrap;
                s.clipping = TextClipping.Overflow;
                s.richText = false;
                labelStyles[key] = s;
            }
            return s;
        }

        public static void Label(Rect r, string text, int size, Color c, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false, bool wrap = false)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(text)) return;
            GUIStyle s = LabelStyle(size, bold, anchor, wrap);
            // soft shadow keeps text readable over the 3D scene
            s.normal.textColor = new Color(0f, 0f, 0f, 0.5f * c.a);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, s);
            s.normal.textColor = c;
            GUI.Label(r, text, s);
        }

        public static float TextHeight(string text, int size, float width, bool bold = false)
        {
            GUIStyle s = LabelStyle(size, bold, TextAnchor.UpperLeft, true);
            return s.CalcHeight(new GUIContent(text), width);
        }

        public static void Logo(Rect r, bool withText = true)
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsureResources();
            Color c = GUI.color;     // the caller may fade the logo by setting GUI.color first
            GUI.DrawTexture(r, logoTex);
            if (withText)
                Label(new Rect(r.x, r.y + r.height * 0.40f, r.width, r.height * 0.4f), "ST", Mathf.RoundToInt(r.height * 0.30f), new Color(1f, 1f, 1f, c.a), TextAnchor.MiddleCenter, true);
        }

        public static void Bar(Rect r, float v01, Color fill, Color back)
        {
            Round(r, back);
            float w = Mathf.Clamp01(v01) * r.width;
            if (w > r.height * 0.5f) Round(new Rect(r.x, r.y, w, r.height), fill);
            else if (w > 1f) Round(new Rect(r.x, r.y, r.height * 0.5f, r.height), fill);
        }

        // ------------------------------------------------------------------ buttons

        /// <summary>True on the frame a press is released over the rect, as long as the finger did not drag.</summary>
        public static bool Click(Rect r)
        {
            int id = ++idCounter;
            Event e = Event.current;
            bool inside = r.Contains(e.mousePosition) && (!clipActive || clip.Contains(e.mousePosition));
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (inside) pressId = id;
                    break;
                case EventType.MouseUp:
                    if (pressId == id && inside && dragTotal < 14f)
                    {
                        pressId = -1;
                        e.Use();
                        return true;
                    }
                    break;
            }
            return false;
        }

        public static bool Button(Rect r, string label, BtnStyle style = BtnStyle.Primary, bool enabled = true, int fontSize = 0)
        {
            Color bg, fg = Text;
            switch (style)
            {
                case BtnStyle.Gold: bg = Gold; fg = new Color(0.12f, 0.09f, 0.02f); break;
                case BtnStyle.Danger: bg = new Color(0.62f, 0.2f, 0.22f); break;
                case BtnStyle.Secondary: bg = PanelLight; break;
                case BtnStyle.Ghost: bg = new Color(1f, 1f, 1f, 0.08f); break;
                default: bg = new Color(0.20f, 0.40f, 0.96f); break;
            }
            if (!enabled) { bg = new Color(bg.r * 0.45f, bg.g * 0.45f, bg.b * 0.5f, bg.a); fg = new Color(fg.r, fg.g, fg.b, 0.45f); }

            bool click = enabled && Click(r);
            // the id used by Click is idCounter now, so pressed state can be checked right after
            bool pressed = enabled && pressId == idCounter && PointerDown && dragTotal < 14f;
            Rect d = pressed ? new Rect(r.x + 2f, r.y + 3f, r.width - 4f, r.height - 4f) : r;
            string skin = null;
            switch (style)
            {
                case BtnStyle.Gold: skin = "ui_button_gold"; break;
                case BtnStyle.Danger: skin = "ui_button_danger"; break;
                case BtnStyle.Secondary: skin = "ui_button_secondary"; break;
                case BtnStyle.Primary: skin = "ui_button_primary"; break;
            }
            bool painted = false;
            if (skin != null && ArtLoader.Has(skin))
            {
                Color tint = !enabled ? new Color(0.5f, 0.5f, 0.55f, 0.8f) : (pressed ? new Color(0.78f, 0.78f, 0.82f, 1f) : Color.white);
                Round(new Rect(d.x, d.y + 4f, d.width, d.height), new Color(0f, 0f, 0f, 0.30f));
                painted = Skin(d, skin, Mathf.Min(46f, d.height * 0.42f), 0.3f, tint);
            }
            if (!painted)
            {
                Round(new Rect(d.x, d.y + 3f, d.width, d.height), new Color(0f, 0f, 0f, 0.35f));
                if (style == BtnStyle.Primary && enabled && !pressed) Glow(d, EdgeBlue, 0.22f);
                Round(d, pressed ? Color.Lerp(bg, Color.black, 0.2f) : bg);
                if (style == BtnStyle.Primary && enabled) Outline(d, new Color(0.8f, 0.88f, 1f, pressed ? 0.35f : 0.75f));
            }
            if (!painted && d.height >= 88f && style != BtnStyle.Ghost)
            {
                // a soft highlight on the top half and a thin edge give the flat button some body
                Round(new Rect(d.x + 3f, d.y + 3f, d.width - 6f, d.height * 0.5f), new Color(1f, 1f, 1f, pressed ? 0.04f : 0.10f));
                Outline(d, new Color(1f, 1f, 1f, enabled ? 0.14f : 0.05f));
            }
            int fs = fontSize > 0 ? fontSize : Mathf.Clamp(Mathf.RoundToInt(r.height * 0.40f), 20, 52);
            Label(d, label, fs, fg, TextAnchor.MiddleCenter, true);

            if (click && AudioManager.I != null) AudioManager.I.Play(Sfx.Click, 0.7f);
            return click;
        }

        /// <summary>A tappable area with no drawing (cards that open something).</summary>
        public static bool Tap(Rect r)
        {
            return Click(r);
        }

        public static bool Toggle(Rect r, string label, bool value, out bool changed)
        {
            changed = false;
            Card(r);
            Label(new Rect(r.x + 28f, r.y, r.width - 150f, r.height), label, 30, Text, TextAnchor.MiddleLeft);
            Rect sw = new Rect(r.xMax - 118f, r.y + r.height * 0.5f - 22f, 92f, 44f);
            if (Click(r))
            {
                value = !value;
                changed = true;
                if (AudioManager.I != null) AudioManager.I.Play(Sfx.Click, 0.7f);
            }
            Round(sw, value ? Accent : new Color(0.2f, 0.22f, 0.34f));
            float kx = value ? sw.xMax - 40f : sw.x + 4f;
            Round(new Rect(kx, sw.y + 4f, 36f, 36f), Color.white);
            return value;
        }

        /// <summary>Horizontal slider with a big thumb. Returns the new value 0..1.</summary>
        public static float Slider(Rect r, float value, string label)
        {
            int id = ++idCounter;
            Event e = Event.current;
            Rect track = new Rect(r.x + 28f, r.y + r.height * 0.62f, r.width - 56f, 12f);
            Rect hit = new Rect(r.x, r.y + r.height * 0.3f, r.width, r.height * 0.7f);

            if (e.type == EventType.MouseDown && hit.Contains(e.mousePosition) && (!clipActive || clip.Contains(e.mousePosition)))
            {
                sliderId = id;
                e.Use();
            }
            if (sliderId == id && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
            {
                value = Mathf.Clamp01((e.mousePosition.x - track.x) / track.width);
                e.Use();
            }

            Card(r);
            Label(new Rect(r.x + 28f, r.y + 6f, r.width - 56f, r.height * 0.5f), label + "  " + Mathf.RoundToInt(value * 100f) + "%", 28, Text, TextAnchor.MiddleLeft);
            Round(track, new Color(0.2f, 0.22f, 0.34f));
            Round(new Rect(track.x, track.y, Mathf.Max(12f, track.width * value), track.height), Accent);
            Round(new Rect(track.x + track.width * value - 20f, track.y - 14f, 40f, 40f), Color.white);
            return value;
        }

        // ------------------------------------------------------------------ scrolling

        /// <summary>
        /// Begins a scrolling area. Draw the content in local coordinates starting at y = -scroll.y, then call EndScroll.
        /// Drag with a finger to scroll, flick for inertia.
        /// </summary>
        public static Vector2 BeginScroll(Rect view, Vector2 scroll, float contentHeight)
        {
            int id = scrollCount++ % scrollVel.Length;
            Event e = Event.current;
            float maxScroll = Mathf.Max(0f, contentHeight - view.height);

            if (e.type == EventType.MouseDown && view.Contains(e.mousePosition))
            {
                scrollId = id;
                scrollVel[id] = 0f;
            }
            else if (e.type == EventType.MouseDrag && scrollId == id && sliderId < 0)
            {
                scroll.y -= e.delta.y;
                scrollVel[id] = -e.delta.y;
            }
            else if (e.type == EventType.Repaint && !(PointerDown && scrollId == id))
            {
                if (Mathf.Abs(scrollVel[id]) > 0.3f)
                {
                    scroll.y += scrollVel[id];
                    scrollVel[id] *= 0.93f;
                }
            }
            scroll.y = Mathf.Clamp(scroll.y, 0f, maxScroll);

            GUI.BeginGroup(view);
            clip = new Rect(0f, 0f, view.width, view.height);
            clipActive = true;
            return scroll;
        }

        public static void EndScroll()
        {
            GUI.EndGroup();
            clipActive = false;
        }

        // ------------------------------------------------------------------ text input

        public static string TextField(Rect r, string value, int maxLength)
        {
            if (fieldStyle == null)
            {
                fieldStyle = new GUIStyle(GUI.skin.textField);
                fieldStyle.fontSize = 44;
                fieldStyle.alignment = TextAnchor.MiddleCenter;
                fieldStyle.normal.textColor = Color.white;
                fieldStyle.focused.textColor = Color.white;
                fieldStyle.normal.background = roundTex;
                fieldStyle.focused.background = roundTex;
                fieldStyle.border = new RectOffset(24, 24, 24, 24);
            }
            GUI.color = new Color(0.16f, 0.18f, 0.32f, 1f);
            string v = GUI.TextField(r, value, maxLength, fieldStyle);
            GUI.color = Color.white;
            return v;
        }

        // ------------------------------------------------------------------ small helpers

        public static string Money(int v)
        {
            return "$" + v.ToString("N0");
        }

        public static string Clock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int m = (int)(seconds / 60f);
            float s = seconds - m * 60f;
            return m.ToString() + ":" + s.ToString("00.00");
        }

        /// <summary>Converts a rect in screen fractions (origin bottom left, as InputReader uses) to UI units.</summary>
        public static Rect FromNormalised(Rect n)
        {
            return new Rect(n.x * W, (1f - n.y - n.height) * H, n.width * W, n.height * H);
        }
    }
}
