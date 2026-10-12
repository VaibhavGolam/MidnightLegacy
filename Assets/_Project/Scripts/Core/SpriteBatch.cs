using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Collects textured quads that lie in the XZ plane (image top = +z) and turns them into one mesh.
    /// The colour of each quad is the tint (it multiplies the texture), and alpha is the opacity.
    /// </summary>
    public sealed class SpriteBatch
    {
        readonly List<Vector3> verts = new List<Vector3>(256);
        readonly List<Vector2> uvs = new List<Vector2>(256);
        readonly List<Color32> cols = new List<Color32>(256);
        readonly List<int> tris = new List<int>(384);

        public int VertexCount { get { return verts.Count; } }

        public void Clear()
        {
            verts.Clear(); uvs.Clear(); cols.Clear(); tris.Clear();
        }

        /// <summary>
        /// One sprite. (cx, cz) is the centre, y the layer height, halfW and halfL the half size across and along,
        /// yawDeg turns it clockwise seen from above (like a car's yaw), flipX mirrors it. uv is the texture rectangle.
        /// </summary>
        public void Add(float cx, float cz, float y, float halfW, float halfL, float yawDeg, Rect uv, Color tint, bool flipX = false)
        {
            float a = yawDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            float u0 = uv.xMin, u1 = uv.xMax;
            if (flipX) { float t = u0; u0 = u1; u1 = t; }
            int i = verts.Count;
            AddCorner(cx, cz, y, -halfW, -halfL, c, s, new Vector2(u0, uv.yMin));
            AddCorner(cx, cz, y, -halfW, halfL, c, s, new Vector2(u0, uv.yMax));
            AddCorner(cx, cz, y, halfW, halfL, c, s, new Vector2(u1, uv.yMax));
            AddCorner(cx, cz, y, halfW, -halfL, c, s, new Vector2(u1, uv.yMin));
            Color32 col = tint;
            cols.Add(col); cols.Add(col); cols.Add(col); cols.Add(col);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        /// <summary>An upright (no turn) sprite whose tint fades from topTint at the far edge to bottomTint at the near edge.</summary>
        public void AddFade(float cx, float cz, float y, float halfW, float halfL, Rect uv, Color topTint, Color bottomTint)
        {
            int i = verts.Count;
            verts.Add(new Vector3(cx - halfW, y, cz - halfL)); uvs.Add(new Vector2(uv.xMin, uv.yMin)); cols.Add(bottomTint);
            verts.Add(new Vector3(cx - halfW, y, cz + halfL)); uvs.Add(new Vector2(uv.xMin, uv.yMax)); cols.Add(topTint);
            verts.Add(new Vector3(cx + halfW, y, cz + halfL)); uvs.Add(new Vector2(uv.xMax, uv.yMax)); cols.Add(topTint);
            verts.Add(new Vector3(cx + halfW, y, cz - halfL)); uvs.Add(new Vector2(uv.xMax, uv.yMin)); cols.Add(bottomTint);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        /// <summary>A sprite with an anchor that is not its centre. anchor is (0..1, 0..1) inside the image, (0,0) = bottom left.</summary>
        public void AddAnchored(float px, float pz, float y, float width, float length, Vector2 anchor, float yawDeg, Rect uv, Color tint, bool flipX = false)
        {
            float ox = (0.5f - anchor.x) * width * (flipX ? -1f : 1f);
            float oz = (0.5f - anchor.y) * length;
            float a = yawDeg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            float cx = px + ox * c + oz * s;
            float cz = pz - ox * s + oz * c;
            Add(cx, cz, y, width * 0.5f, length * 0.5f, yawDeg, uv, tint, flipX);
        }

        void AddCorner(float cx, float cz, float y, float lx, float lz, float c, float s, Vector2 uv)
        {
            verts.Add(new Vector3(cx + lx * c + lz * s, y, cz - lx * s + lz * c));
            uvs.Add(uv);
        }

        public void Build(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateBounds();
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
