using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Flat 2D drawing on top of MeshBuilder: polygons, rounded rectangles, ellipses, stars and lines that lie in
    /// the XZ plane and face up. The y value is the layer height: a higher y is drawn on top of a lower one.
    /// Positions are (x, z) pairs. Rotations are in degrees and turn clockwise when seen from above, like a car's yaw.
    /// The garage side view uses the same calls with x as "along the car" and z as "up", then turns the object
    /// to face the camera.
    /// </summary>
    public static class Flat
    {
        static readonly List<Vector2> pts = new List<Vector2>(96);
        static readonly float[] CornerX = { 1f, 1f, -1f, -1f };
        static readonly float[] CornerZ = { 1f, -1f, -1f, 1f };
        static readonly float[] CornerStart = { 0f, 90f, 180f, 270f };

        static Vector2 Rot(float x, float z, float c, float s)
        {
            return new Vector2(x * c + z * s, -x * s + z * c);
        }

        /// <summary>Fills a polygon. It may be concave as long as it is star shaped around its centre.</summary>
        public static void Poly(MeshBuilder mb, IList<Vector2> p, float y, Color col, float emit = 0f)
        {
            int n = p.Count;
            if (n < 3) return;
            Vector2 c = Vector2.zero;
            for (int i = 0; i < n; i++) c += p[i];
            c /= n;
            Vector3 ctr = new Vector3(c.x, y, c.y);
            Vector3 inside = new Vector3(c.x, y - 5f, c.y);
            for (int i = 0; i < n; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % n];
                mb.AddTri(ctr, new Vector3(a.x, y, a.y), new Vector3(b.x, y, b.y), inside, col, emit);
            }
        }

        /// <summary>Four corners in perimeter order.</summary>
        public static void Quad(MeshBuilder mb, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float y, Color col, float emit = 0f)
        {
            mb.AddUpQuad(new Vector3(a.x, y, a.y), new Vector3(b.x, y, b.y), new Vector3(c.x, y, c.y), new Vector3(d.x, y, d.y), col, emit);
        }

        public static void Rect(MeshBuilder mb, float cx, float cz, float hw, float hl, float y, Color col, float emit = 0f, float rotDeg = 0f)
        {
            float a = rotDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            Vector2 o = new Vector2(cx, cz);
            Quad(mb, o + Rot(-hw, -hl, c, s), o + Rot(hw, -hl, c, s), o + Rot(hw, hl, c, s), o + Rot(-hw, hl, c, s), y, col, emit);
        }

        public static void RoundRect(MeshBuilder mb, float cx, float cz, float hw, float hl, float r, float y, Color col, float emit = 0f, float rotDeg = 0f, int segs = 4)
        {
            r = Mathf.Clamp(r, 0.001f, Mathf.Min(hw, hl));
            float a = rotDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            Vector2 o = new Vector2(cx, cz);
            pts.Clear();
            // corners in perimeter order: front right, rear right, rear left, front left
            for (int k = 0; k < 4; k++)
            {
                float ccx = CornerX[k] * (hw - r), ccz = CornerZ[k] * (hl - r);
                for (int i = 0; i <= segs; i++)
                {
                    float ang = (CornerStart[k] + 90f * i / segs) * Mathf.Deg2Rad;
                    // angle measured from +z toward +x
                    float px = ccx + Mathf.Sin(ang) * r;
                    float pz = ccz + Mathf.Cos(ang) * r;
                    pts.Add(o + Rot(px, pz, c, s));
                }
            }
            Poly(mb, pts, y, col, emit);
        }

        public static void Ellipse(MeshBuilder mb, float cx, float cz, float rx, float rz, float y, Color col, float emit = 0f, int segs = 14, float rotDeg = 0f)
        {
            float a = rotDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            Vector2 o = new Vector2(cx, cz);
            pts.Clear();
            for (int i = 0; i < segs; i++)
            {
                float t = Mathf.PI * 2f * i / segs;
                pts.Add(o + Rot(Mathf.Cos(t) * rx, Mathf.Sin(t) * rz, c, s));
            }
            Poly(mb, pts, y, col, emit);
        }

        public static void Circle(MeshBuilder mb, float cx, float cz, float r, float y, Color col, float emit = 0f, int segs = 12)
        {
            Ellipse(mb, cx, cz, r, r, y, col, emit, segs, 0f);
        }

        /// <summary>Star or spiky disc (pine trees, burst shapes).</summary>
        public static void Star(MeshBuilder mb, float cx, float cz, float outer, float inner, int points, float y, Color col, float emit = 0f, float rotDeg = 0f)
        {
            float a = rotDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            Vector2 o = new Vector2(cx, cz);
            pts.Clear();
            int n = points * 2;
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.PI * 2f * i / n;
                float r = (i & 1) == 0 ? outer : inner;
                pts.Add(o + Rot(Mathf.Cos(t) * r, Mathf.Sin(t) * r, c, s));
            }
            Poly(mb, pts, y, col, emit);
        }

        /// <summary>Lumpy polygon for rocks. Radii vary with the rng.</summary>
        public static void Rock(MeshBuilder mb, float cx, float cz, float rx, float rz, int sides, float y, Color col, System.Random rng, float rotDeg = 0f)
        {
            float a = rotDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            Vector2 o = new Vector2(cx, cz);
            pts.Clear();
            for (int i = 0; i < sides; i++)
            {
                float t = Mathf.PI * 2f * i / sides;
                float j = 0.72f + 0.28f * (float)rng.NextDouble();
                pts.Add(o + Rot(Mathf.Cos(t) * rx * j, Mathf.Sin(t) * rz * j, c, s));
            }
            Poly(mb, pts, y, col, 0f);
        }

        /// <summary>Straight strip between two points (3D points, only x and z are used), with a layer height.</summary>
        public static void Line(MeshBuilder mb, Vector3 a, Vector3 b, float halfWidth, float y, Color col, float emit = 0f)
        {
            Vector2 d = new Vector2(b.x - a.x, b.z - a.z);
            if (d.sqrMagnitude < 1e-8f) return;
            d.Normalize();
            Vector2 n = new Vector2(d.y, -d.x) * halfWidth;
            Vector2 pa = new Vector2(a.x, a.z), pb = new Vector2(b.x, b.z);
            Quad(mb, pa - n, pa + n, pb + n, pb - n, y, col, emit);
        }

        /// <summary>Soft light disc for the additive material: bright in the middle, fading to nothing at the edge.</summary>
        public static void Glow(MeshBuilder mb, float cx, float cz, float radius, float y, Color col, float centerAlpha, int segs = 14)
        {
            mb.AddDisc(new Vector3(cx, y, cz), radius, segs, col, centerAlpha, 0f);
        }

        /// <summary>Convenience: a list of (x, z) pairs from a flat float array.</summary>
        public static Vector2[] Pts(params float[] xz)
        {
            Vector2[] r = new Vector2[xz.Length / 2];
            for (int i = 0; i < r.Length; i++) r[i] = new Vector2(xz[i * 2], xz[i * 2 + 1]);
            return r;
        }

        /// <summary>Mirrors half an outline (right side, listed rear to front) into a full closed outline.</summary>
        public static Vector2[] Mirror(IList<Vector2> right)
        {
            int n = right.Count;
            Vector2[] r = new Vector2[n * 2];
            for (int i = 0; i < n; i++) r[i] = right[i];
            for (int i = 0; i < n; i++) r[n + i] = new Vector2(-right[n - 1 - i].x, right[n - 1 - i].y);
            return r;
        }

        /// <summary>Grows a convex outline outwards from its centre (used for the dark outline behind a car).</summary>
        public static Vector2[] Grow(IList<Vector2> p, float amount)
        {
            Vector2 c = Vector2.zero;
            for (int i = 0; i < p.Count; i++) c += p[i];
            c /= p.Count;
            Vector2[] r = new Vector2[p.Count];
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 d = p[i] - c;
                float len = d.magnitude;
                r[i] = len < 1e-4f ? p[i] : c + d * ((len + amount) / len);
            }
            return r;
        }
    }
}
