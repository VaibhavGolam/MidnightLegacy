using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Draws the player's car from the side for the garage and menu background. It uses the same flat shape tools as
    /// the top-down car. Drawing space: x runs along the car (front is +x), z is up, y is the layer depth (a higher y is
    /// closer to the camera). The caller turns the finished object so the +y face points at the camera.
    /// </summary>
    public static class SideCar
    {
        /// <summary>Longest car plus a little room, used by the camera framing.</summary>
        public const float FrameLength = 4.35f;
        /// <summary>Height of the car's middle above the floor, used by the camera framing.</summary>
        public const float MidHeight = 0.7f;

        sealed class Shape
        {
            public Vector2[] outline;
            public Vector2[] window;        // bottom left, top left, top right, bottom right
            public float pillarX;           // B pillar, PositiveInfinity for none
            public float xB, xF, deckZ, beltZ, wheelR, rearWheelX, frontWheelX, handleX;
            public float[] seams;
            public Vector2 head, tail, mirror;
            public int deckFrom, roofFrom, roofTo, hoodFrom, hoodTo;   // outline index ranges for stripes, -1 for none
        }

        static readonly Color Ink = new Color(0.02f, 0.02f, 0.03f);
        static readonly Color Tyre = new Color(0.045f, 0.045f, 0.05f);

        static Color Shade(Color c, float f)
        {
            return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), 1f);
        }

        static Shape ShapeFor(int style)
        {
            Shape s = new Shape();
            switch (style)
            {
                case 1: // hatchback
                    s.outline = Flat.Pts(-1.88f, 0.30f, -1.90f, 0.70f, -1.84f, 1.12f, -1.50f, 1.42f, -0.50f, 1.46f, 0.30f, 1.44f, 0.80f, 1.00f,
                        1.35f, 0.88f, 1.82f, 0.72f, 1.90f, 0.50f, 1.86f, 0.28f, 1.50f, 0.18f, -1.50f, 0.18f);
                    s.window = Flat.Pts(-1.60f, 1.10f, -1.38f, 1.36f, 0.20f, 1.37f, 0.68f, 1.04f);
                    s.pillarX = -0.12f;
                    s.xB = -1.90f; s.xF = 1.90f; s.deckZ = 1.12f; s.beltZ = 1.04f;
                    s.wheelR = 0.31f; s.rearWheelX = -1.15f; s.frontWheelX = 1.17f;
                    s.seams = new[] { 0.62f, -0.55f }; s.handleX = -0.35f;
                    s.head = new Vector2(1.80f, 0.62f); s.tail = new Vector2(-1.89f, 0.76f); s.mirror = new Vector2(0.66f, 1.08f);
                    s.deckFrom = -1; s.roofFrom = 3; s.roofTo = 5; s.hoodFrom = 6; s.hoodTo = 8;
                    break;
                case 2: // wedge
                    s.outline = Flat.Pts(-2.00f, 0.28f, -2.00f, 0.62f, -1.95f, 0.92f, -1.40f, 1.02f, -0.80f, 1.04f, -0.25f, 1.30f, 0.35f, 1.31f,
                        0.80f, 0.98f, 1.60f, 0.74f, 2.15f, 0.52f, 2.18f, 0.36f, 2.10f, 0.26f, 1.60f, 0.16f, -1.60f, 0.16f);
                    s.window = Flat.Pts(-0.70f, 1.03f, -0.28f, 1.26f, 0.30f, 1.27f, 0.70f, 1.00f);
                    s.pillarX = float.PositiveInfinity;
                    s.xB = -2.00f; s.xF = 2.18f; s.deckZ = 0.98f; s.beltZ = 0.98f;
                    s.wheelR = 0.33f; s.rearWheelX = -1.32f; s.frontWheelX = 1.38f;
                    s.seams = new[] { 0.60f, -0.62f }; s.handleX = -0.30f;
                    s.head = new Vector2(2.05f, 0.45f); s.tail = new Vector2(-1.99f, 0.72f); s.mirror = new Vector2(0.66f, 1.03f);
                    s.deckFrom = 2; s.roofFrom = 5; s.roofTo = 6; s.hoodFrom = 7; s.hoodTo = 9;
                    break;
                default: // coupe (the Kairo)
                    s.outline = Flat.Pts(-2.03f, 0.30f, -2.05f, 0.62f, -2.02f, 0.86f, -1.55f, 0.93f, -1.18f, 0.97f, -0.55f, 1.30f, 0.05f, 1.33f,
                        0.45f, 1.28f, 0.98f, 0.93f, 1.55f, 0.84f, 1.98f, 0.70f, 2.08f, 0.50f, 2.04f, 0.28f, 1.60f, 0.18f, -1.60f, 0.18f);
                    s.window = Flat.Pts(-1.00f, 0.98f, -0.55f, 1.23f, 0.38f, 1.23f, 0.84f, 0.98f);
                    s.pillarX = -0.42f;
                    s.xB = -2.05f; s.xF = 2.08f; s.deckZ = 0.90f; s.beltZ = 0.97f;
                    s.wheelR = 0.335f; s.rearWheelX = -1.28f; s.frontWheelX = 1.30f;
                    s.seams = new[] { 0.90f, -0.85f }; s.handleX = -0.55f;
                    s.head = new Vector2(1.92f, 0.62f); s.tail = new Vector2(-2.03f, 0.70f); s.mirror = new Vector2(0.88f, 1.02f);
                    s.deckFrom = 2; s.roofFrom = 5; s.roofTo = 7; s.hoodFrom = 8; s.hoodTo = 10;
                    break;
            }
            return s;
        }

        /// <summary>The coupe's side outline, for posters and the like.</summary>
        public static Vector2[] CoupeOutline()
        {
            return ShapeFor(0).outline;
        }

        /// <summary>
        /// Fills the three builders. body: opaque shapes. glow: additive light (headlight beam and halos).
        /// under: a unit disc of underglow that the caller squashes into an ellipse on the floor.
        /// </summary>
        public static void Build(MeshBuilder body, MeshBuilder glow, MeshBuilder under, CarLook look)
        {
            if (look == null) look = new CarLook();
            Shape s = ShapeFor(look.style);
            Color paint = look.body;
            Color trim = look.trim;

            // ---- outline, body, shading ----
            Flat.Poly(body, Flat.Grow(s.outline, 0.05f), 0.00f, Ink);
            Flat.Poly(body, s.outline, 0.05f, paint);
            // darker lower body
            Flat.Quad(body, new Vector2(s.xB + 0.05f, 0.19f), new Vector2(s.xB + 0.05f, 0.45f), new Vector2(s.xF - 0.07f, 0.45f), new Vector2(s.xF - 0.07f, 0.19f), 0.08f, Shade(paint, 0.74f));
            // shoulder highlight along the belt line
            Flat.Line(body, new Vector3(s.xB + 0.10f, 0f, s.beltZ - 0.08f), new Vector3(s.xF - 0.12f, 0f, s.beltZ - 0.08f), 0.022f, 0.09f, Shade(paint, 1.22f));
            // rocker and bumpers
            Flat.Rect(body, 0f, 0.225f, s.xF - 0.45f, 0.045f, 0.10f, trim);
            Flat.Rect(body, s.xF - 0.30f, 0.255f, 0.30f, 0.075f, 0.10f, trim);
            Flat.Rect(body, s.xB + 0.28f, 0.255f, 0.28f, 0.075f, 0.10f, trim);

            // ---- glass, pillar, door lines ----
            Vector2 bl = s.window[0], tl = s.window[1], tr = s.window[2], br = s.window[3];
            Flat.Quad(body, bl, tl, tr, br, 0.14f, look.glass);
            Vector2 h0 = Vector2.Lerp(bl, br, 0.22f), h1 = Vector2.Lerp(bl, br, 0.36f);
            Vector2 h2 = Vector2.Lerp(tl, tr, 0.36f + 0.08f), h3 = Vector2.Lerp(tl, tr, 0.22f + 0.08f);
            Flat.Quad(body, h0, h1, h2, h3, 0.15f, Shade(look.glass, 2.4f));
            if (!float.IsInfinity(s.pillarX))
            {
                float tb = Mathf.InverseLerp(bl.x, br.x, s.pillarX), tt = Mathf.InverseLerp(tl.x, tr.x, s.pillarX);
                float zb = Mathf.Lerp(bl.y, br.y, tb), zt = Mathf.Lerp(tl.y, tr.y, tt);
                Flat.Line(body, new Vector3(s.pillarX, 0f, zb), new Vector3(s.pillarX, 0f, zt), 0.03f, 0.16f, paint);
            }
            Color seam = Shade(paint, 0.50f);
            for (int i = 0; i < s.seams.Length; i++)
                Flat.Line(body, new Vector3(s.seams[i], 0f, 0.27f), new Vector3(s.seams[i], 0f, s.beltZ - 0.03f), 0.011f, 0.16f, seam);
            Flat.Rect(body, s.handleX, s.beltZ - 0.13f, 0.07f, 0.014f, 0.16f, seam);
            Flat.Ellipse(body, s.mirror.x, s.mirror.y, 0.09f, 0.05f, 0.17f, trim, 0f, 10);

            // ---- decals ----
            DrawDecal(body, s, look);

            // ---- spoiler ----
            DrawSpoiler(body, s, look);

            // ---- exhaust ----
            Flat.Rect(body, s.xB - 0.01f, 0.25f, 0.06f, 0.035f, 0.12f, new Color(0.45f, 0.46f, 0.50f));

            // ---- lights ----
            Flat.Ellipse(body, s.head.x, s.head.y, 0.14f, 0.065f, 0.22f, Ink, 0f, 12);
            Flat.Ellipse(body, s.head.x + 0.01f, s.head.y, 0.105f, 0.045f, 0.23f, look.lightColor, look.darkLights ? 0f : 1f, 12);
            if (look.darkLights)
                Flat.Rect(body, s.tail.x + 0.02f, s.tail.y, 0.04f, 0.12f, 0.22f, new Color(0.20f, 0.03f, 0.03f));
            else
                Flat.Rect(body, s.tail.x + 0.02f, s.tail.y, 0.04f, 0.12f, 0.22f, new Color(1f, 0.12f, 0.08f), 1f);

            // ---- wheels ----
            DrawWheel(body, s, look, s.rearWheelX);
            DrawWheel(body, s, look, s.frontWheelX);

            // ---- glow: headlight beam and halos ----
            if (!look.darkLights)
            {
                Color lc = look.lightColor;
                Vector2 h = s.head;
                Flat.Glow(glow, h.x + 0.05f, h.y, 0.55f, 0.30f, lc, 0.50f, 14);
                Flat.Poly(glow, new[] { new Vector2(h.x, h.y + 0.05f), new Vector2(h.x + 3.4f, h.y + 0.55f), new Vector2(h.x + 3.4f, h.y - 0.65f), new Vector2(h.x, h.y - 0.05f) }, 0.30f, lc, 0.045f);
                Flat.Poly(glow, new[] { new Vector2(h.x, h.y + 0.04f), new Vector2(h.x + 2.4f, h.y + 0.30f), new Vector2(h.x + 2.4f, h.y - 0.42f), new Vector2(h.x, h.y - 0.04f) }, 0.31f, lc, 0.06f);
                Flat.Glow(glow, s.tail.x - 0.02f, s.tail.y, 0.45f, 0.30f, new Color(1f, 0.1f, 0.06f), 0.35f, 12);
            }

            // ---- underglow: unit disc, the caller squashes it ----
            if (look.underglow.maxColorComponent > 0.05f)
                Flat.Glow(under, 0f, 0f, 1f, 0f, look.underglow, 0.75f, 20);
        }

        // ------------------------------------------------------------------ parts

        static void Stripe(MeshBuilder mb, Vector2[] o, int from, int to, float drop, float halfWidth, float y, Color col)
        {
            if (from < 0) return;
            for (int i = from; i < to; i++)
                Flat.Line(mb, new Vector3(o[i].x, 0f, o[i].y - drop), new Vector3(o[i + 1].x, 0f, o[i + 1].y - drop), halfWidth, y, col);
        }

        static void DrawDecal(MeshBuilder mb, Shape s, CarLook look)
        {
            Color c = look.decalColor;
            float y = 0.18f;
            Vector2[] o = s.outline;
            switch (look.decal)
            {
                case 1: // centre stripe seen from the side: along deck, roof and hood
                    Stripe(mb, o, s.deckFrom, s.deckFrom < 0 ? -1 : s.deckFrom + 2, 0.045f, 0.026f, y, c);
                    Stripe(mb, o, s.roofFrom, s.roofTo, 0.045f, 0.026f, y, c);
                    Stripe(mb, o, s.hoodFrom, s.hoodTo, 0.045f, 0.026f, y, c);
                    break;
                case 2: // side stripe
                    Flat.Line(mb, new Vector3(s.xB + 0.12f, 0f, 0.58f), new Vector3(s.xF - 0.12f, 0f, 0.58f), 0.05f, y, c);
                    break;
                case 3: // matte hood
                    Stripe(mb, o, s.hoodFrom, s.hoodTo, 0.04f, 0.045f, y, c);
                    break;
                case 4: // number roundel
                    Flat.Circle(mb, 0.05f, 0.62f, 0.21f, y, c, 0f, 16);
                    Flat.Circle(mb, 0.05f, 0.69f, 0.085f, y + 0.01f, Ink, 0f, 10);
                    Flat.Circle(mb, 0.05f, 0.69f, 0.045f, y + 0.02f, c, 0f, 10);
                    Flat.Circle(mb, 0.05f, 0.55f, 0.095f, y + 0.01f, Ink, 0f, 10);
                    Flat.Circle(mb, 0.05f, 0.55f, 0.052f, y + 0.02f, c, 0f, 10);
                    break;
                case 5: // twin stripes
                    Flat.Line(mb, new Vector3(s.xB + 0.12f, 0f, 0.50f), new Vector3(s.xF - 0.12f, 0f, 0.50f), 0.018f, y, c);
                    Flat.Line(mb, new Vector3(s.xB + 0.12f, 0f, 0.60f), new Vector3(s.xF - 0.12f, 0f, 0.60f), 0.018f, y, c);
                    break;
                case 6: // checker band above the sill
                    for (int row = 0; row < 2; row++)
                        for (int i = 0; i < 28; i++)
                        {
                            float x = s.xB + 0.25f + i * 0.14f;
                            if (x > s.xF - 0.25f) break;
                            if (((i + row) & 1) == 0) Flat.Rect(mb, x, 0.33f + row * 0.07f, 0.07f, 0.035f, y, c);
                        }
                    break;
                case 7: // lightning bolt
                {
                    Vector3 a = new Vector3(0.58f, 0f, 0.84f), b = new Vector3(0.12f, 0f, 0.60f), cc = new Vector3(0.40f, 0f, 0.58f), d = new Vector3(-0.12f, 0f, 0.34f);
                    Flat.Line(mb, a, b, 0.05f, y, c);
                    Flat.Line(mb, b, cc, 0.05f, y, c);
                    Flat.Line(mb, cc, d, 0.05f, y, c);
                    break;
                }
            }
        }

        static void DrawSpoiler(MeshBuilder mb, Shape s, CarLook look)
        {
            Color paint = look.body;
            float x = s.xB, z = s.deckZ;
            switch (look.spoiler)
            {
                case 0: // duck tail
                    Flat.Poly(mb, new[] { new Vector2(x + 0.03f, z - 0.05f), new Vector2(x - 0.10f, z + 0.10f), new Vector2(x + 0.42f, z + 0.05f), new Vector2(x + 0.50f, z - 0.02f) }, 0.20f, Shade(paint, 0.92f));
                    break;
                case 2: // big wing on an upright
                    Flat.Rect(mb, x + 0.55f, z + 0.20f, 0.03f, 0.22f, 0.19f, look.trim);
                    Flat.Poly(mb, new[] { new Vector2(x - 0.10f, z + 0.40f), new Vector2(x + 0.78f, z + 0.46f), new Vector2(x + 0.78f, z + 0.52f), new Vector2(x - 0.12f, z + 0.50f) }, 0.24f, look.trim);
                    Flat.Poly(mb, new[] { new Vector2(x - 0.14f, z + 0.32f), new Vector2(x + 0.80f, z + 0.38f), new Vector2(x + 0.76f, z + 0.62f), new Vector2(x - 0.12f, z + 0.56f) }, 0.30f, Shade(paint, 0.88f));
                    break;
                case 3: // lip
                    Flat.Rect(mb, x + 0.20f, z + 0.02f, 0.22f, 0.028f, 0.20f, Shade(paint, 0.92f));
                    break;
            }
        }

        static void DrawWheel(MeshBuilder mb, Shape s, CarLook look, float cx)
        {
            float r = s.wheelR, cz = r;
            // wheel arch: a dark cut-out, kept above the floor
            List<Vector2> arch = new List<Vector2>(20);
            for (int i = 0; i < 20; i++)
            {
                float t = Mathf.PI * 2f * i / 20f;
                arch.Add(new Vector2(cx + Mathf.Cos(t) * (r + 0.06f), Mathf.Max(0.02f, cz + Mathf.Sin(t) * (r + 0.06f))));
            }
            Flat.Poly(mb, arch, 0.28f, Ink);

            Flat.Circle(mb, cx, cz, r, 0.34f, Tyre, 0f, 22);
            Flat.Circle(mb, cx, cz, r * 0.86f, 0.345f, new Color(0.09f, 0.09f, 0.10f), 0f, 22);
            float rr = r * 0.70f;
            Color rim = look.rim;
            Flat.Circle(mb, cx, cz, rr, 0.36f, Shade(rim, look.rimStyle == 2 ? 0.85f : 0.55f), 0f, 22);
            Flat.Circle(mb, cx, cz, rr * 0.84f, 0.365f, look.rimStyle == 2 ? Shade(rim, 0.40f) : Ink, 0f, 20);
            // brake caliper behind the spokes
            Flat.Rect(mb, cx + rr * 0.42f, cz + rr * 0.30f, 0.035f, 0.075f, 0.37f, new Color(0.75f, 0.12f, 0.10f), 0f, 30f);

            switch (look.rimStyle)
            {
                case 1: // five spoke
                    for (int k = 0; k < 5; k++)
                    {
                        float a = (90f + 72f * k) * Mathf.Deg2Rad;
                        Flat.Line(mb, new Vector3(cx, 0f, cz), new Vector3(cx + Mathf.Cos(a) * rr * 0.92f, 0f, cz + Mathf.Sin(a) * rr * 0.92f), 0.03f, 0.38f, rim);
                    }
                    break;
                case 2: // mesh
                    for (int k = 0; k < 4; k++)
                    {
                        float a = (22.5f * k + 8f) * Mathf.Deg2Rad;
                        Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr * 0.9f;
                        Flat.Line(mb, new Vector3(cx, 0f, cz) - d, new Vector3(cx, 0f, cz) + d, 0.014f, 0.38f, rim);
                    }
                    break;
                default: // bar
                    for (int k = 0; k < 2; k++)
                    {
                        float a = (35f + 90f * k) * Mathf.Deg2Rad;
                        Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rr * 0.9f;
                        Flat.Line(mb, new Vector3(cx, 0f, cz) - d, new Vector3(cx, 0f, cz) + d, 0.038f, 0.38f, rim);
                    }
                    break;
            }
            Flat.Circle(mb, cx, cz, rr * 0.20f, 0.40f, rim, 0f, 10);
        }
    }
}
