using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Builds flat shaded, vertex coloured meshes from primitives. Every triangle gets its own vertices and
    /// its own face normal. Winding is fixed automatically using an "inside" reference point, so callers
    /// never have to think about triangle order. Vertex alpha stores the emissive amount (0..1).
    /// </summary>
    public sealed class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>(8192);
        readonly List<Vector3> norms = new List<Vector3>(8192);
        readonly List<Color32> cols = new List<Color32>(8192);
        readonly List<Vector2> uvs = new List<Vector2>(8192);
        readonly List<int> tris = new List<int>(8192);

        readonly Vector3[] boxScratch = new Vector3[8];
        Vector3[] blobPts;

        static readonly Vector2 CenterUV = new Vector2(0.5f, 0.5f);

        public int VertexCount { get { return verts.Count; } }

        public void Clear()
        {
            verts.Clear(); norms.Clear(); cols.Clear(); uvs.Clear(); tris.Clear();
        }

        static byte ToByte(float f)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(f * 255f), 0, 255);
        }

        static Color32 Pack(Color c, float emit)
        {
            return new Color32(ToByte(c.r), ToByte(c.g), ToByte(c.b), ToByte(emit));
        }

        public static Color Vary(Color c, float amount, System.Random rng)
        {
            float f = 1f + ((float)rng.NextDouble() * 2f - 1f) * amount;
            return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);
        }

        void Push(Vector3 a, Vector3 b, Vector3 c, Vector3 n, Color32 ca, Color32 cb, Color32 cc)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            norms.Add(n); norms.Add(n); norms.Add(n);
            cols.Add(ca); cols.Add(cb); cols.Add(cc);
            uvs.Add(CenterUV); uvs.Add(CenterUV); uvs.Add(CenterUV);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        /// <summary>Triangle that always faces away from the inside point.</summary>
        public void AddTri(Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Color col, float emit = 0f)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            float m = n.sqrMagnitude;
            if (m < 1e-10f) return;
            n /= Mathf.Sqrt(m);
            if (Vector3.Dot(n, (a + b + c) * (1f / 3f) - inside) < 0f)
            {
                Vector3 t = b; b = c; c = t;
                n = -n;
            }
            Color32 p = Pack(col, emit);
            Push(a, b, c, n, p, p, p);
        }

        /// <summary>Quad (corners in perimeter order) that faces away from the inside point.</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside, Color col, float emit = 0f)
        {
            AddTri(a, b, c, inside, col, emit);
            AddTri(a, c, d, inside, col, emit);
        }

        /// <summary>Quad that faces up (road, ground, lines).</summary>
        public void AddUpQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col, float emit = 0f)
        {
            Vector3 inside = (a + b + c + d) * 0.25f - Vector3.up * 5f;
            AddQuad(a, b, c, d, inside, col, emit);
        }

        /// <summary>
        /// Eight corners: 0..3 bottom ring (back-left, back-right, front-right, front-left), 4..7 top ring in the same order.
        /// </summary>
        public void AddHexa(Vector3[] p, Color side, Color top, Color front, Color back, float emit = 0f)
        {
            Vector3 ctr = Vector3.zero;
            for (int i = 0; i < 8; i++) ctr += p[i];
            ctr /= 8f;
            AddQuad(p[4], p[5], p[6], p[7], ctr, top, emit);
            AddQuad(p[1], p[2], p[6], p[5], ctr, side, emit);
            AddQuad(p[0], p[3], p[7], p[4], ctr, side, emit);
            AddQuad(p[2], p[3], p[7], p[6], ctr, front, emit);
            AddQuad(p[0], p[1], p[5], p[4], ctr, back, emit);
            AddQuad(p[0], p[1], p[2], p[3], ctr, side, emit);
        }

        public void AddBox(Vector3 center, Vector3 size, Color col, float emit = 0f)
        {
            AddBoxRot(center, size, Quaternion.identity, col, emit);
        }

        public void AddBoxRot(Vector3 center, Vector3 size, Quaternion rot, Color col, float emit = 0f)
        {
            Vector3 h = size * 0.5f;
            boxScratch[0] = center + rot * new Vector3(-h.x, -h.y, -h.z);
            boxScratch[1] = center + rot * new Vector3(h.x, -h.y, -h.z);
            boxScratch[2] = center + rot * new Vector3(h.x, -h.y, h.z);
            boxScratch[3] = center + rot * new Vector3(-h.x, -h.y, h.z);
            boxScratch[4] = center + rot * new Vector3(-h.x, h.y, -h.z);
            boxScratch[5] = center + rot * new Vector3(h.x, h.y, -h.z);
            boxScratch[6] = center + rot * new Vector3(h.x, h.y, h.z);
            boxScratch[7] = center + rot * new Vector3(-h.x, h.y, h.z);
            AddHexa(boxScratch, col, col, col, col, emit);
        }

        /// <summary>Cylinder or cone frustum between two points. Caps get their own colours.</summary>
        public void AddCylinder(Vector3 p0, Vector3 p1, float r0, float r1, int sides, Color side, Color cap0, Color cap1, float emit = 0f)
        {
            Vector3 axis = p1 - p0;
            float len = axis.magnitude;
            if (len < 1e-5f) return;
            axis /= len;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.95f ? Vector3.up : Vector3.right).normalized;
            Vector3 w = Vector3.Cross(axis, u);
            Vector3 mid = (p0 + p1) * 0.5f;
            float step = Mathf.PI * 2f / sides;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * step, a1 = (i + 1) * step;
                Vector3 d0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0);
                Vector3 d1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
                Vector3 a = p0 + d0 * r0, b = p0 + d1 * r0, c = p1 + d1 * r1, d = p1 + d0 * r1;
                AddQuad(a, b, c, d, mid, side, emit);
                AddTri(p0, a, b, mid, cap0, emit);
                AddTri(p1, d, c, mid, cap1, emit);
            }
        }

        /// <summary>Low poly lumpy sphere used for foliage and bushes.</summary>
        public void AddBlob(Vector3 center, Vector3 radii, int segs, int rings, Color col, System.Random rng, float jitter, float shadeVar)
        {
            int need = (rings - 1) * segs + 2;
            if (blobPts == null || blobPts.Length < need) blobPts = new Vector3[need];
            int idx = 0;
            blobPts[idx++] = center + new Vector3(0f, radii.y, 0f);
            for (int r = 1; r < rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                float y = Mathf.Cos(phi);
                float rr = Mathf.Sin(phi);
                for (int s = 0; s < segs; s++)
                {
                    float th = Mathf.PI * 2f * s / segs + (r % 2) * Mathf.PI / segs;
                    float j = 1f + ((float)rng.NextDouble() * 2f - 1f) * jitter;
                    blobPts[idx++] = center + new Vector3(rr * Mathf.Cos(th) * radii.x * j, y * radii.y * j, rr * Mathf.Sin(th) * radii.z * j);
                }
            }
            blobPts[idx] = center + new Vector3(0f, -radii.y, 0f);

            int top = 0, bottom = need - 1;
            for (int s = 0; s < segs; s++)
            {
                int s1 = (s + 1) % segs;
                AddTri(blobPts[top], blobPts[1 + s], blobPts[1 + s1], center, Vary(col, shadeVar, rng));
            }
            for (int r = 1; r < rings - 1; r++)
            {
                int a0 = 1 + (r - 1) * segs;
                int b0 = 1 + r * segs;
                for (int s = 0; s < segs; s++)
                {
                    int s1 = (s + 1) % segs;
                    AddQuad(blobPts[a0 + s], blobPts[a0 + s1], blobPts[b0 + s1], blobPts[b0 + s], center, Vary(col, shadeVar, rng));
                }
            }
            int last = 1 + (rings - 2) * segs;
            for (int s = 0; s < segs; s++)
            {
                int s1 = (s + 1) % segs;
                AddTri(blobPts[bottom], blobPts[last + s1], blobPts[last + s], center, Vary(col, shadeVar, rng));
            }
        }

        /// <summary>Flat disc facing up with per-vertex alpha (blob shadow). Raw alpha, not emission.</summary>
        public void AddDisc(Vector3 center, float radius, int segs, Color col, float centerAlpha, float edgeAlpha)
        {
            Color32 cc = new Color32(ToByte(col.r), ToByte(col.g), ToByte(col.b), ToByte(centerAlpha));
            Color32 ce = new Color32(ToByte(col.r), ToByte(col.g), ToByte(col.b), ToByte(edgeAlpha));
            float step = Mathf.PI * 2f / segs;
            for (int i = 0; i < segs; i++)
            {
                Vector3 a = center + new Vector3(Mathf.Cos(i * step) * radius, 0f, Mathf.Sin(i * step) * radius);
                Vector3 b = center + new Vector3(Mathf.Cos((i + 1) * step) * radius, 0f, Mathf.Sin((i + 1) * step) * radius);
                if (Vector3.Cross(a - center, b - center).y < 0f) { Vector3 t = a; a = b; b = t; }
                Push(center, a, b, Vector3.up, cc, ce, ce);
            }
        }

        public void Build(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, true);
        }

        public Mesh ToMesh(string name)
        {
            Mesh m = new Mesh();
            m.name = name;
            Build(m);
            return m;
        }
    }
}
