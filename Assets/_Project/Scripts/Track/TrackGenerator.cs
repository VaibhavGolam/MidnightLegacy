using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Builds the visible world in 80 m chunks around the car: road, lane lines, guard rails, ground and
    /// low poly autumn trees. Each chunk is ONE mesh and ONE draw call. Chunks are pooled and rebuilt as
    /// the car moves forward. Also performs the floating origin rebase for endless runs.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class TrackGenerator : MonoBehaviour
    {
        public const float ChunkLength = 80f;
        const float Step = 2f;               // metres between cross sections
        const float RebaseDistance = 2500f;

        sealed class Chunk
        {
            public int index;
            public GameObject go;
            public MeshFilter mf;
            public Mesh mesh;
        }

        CarController car;
        RoadModel road;
        TrackRecipe recipe;

        readonly Dictionary<int, Chunk> active = new Dictionary<int, Chunk>();
        readonly Queue<Chunk> pool = new Queue<Chunk>();
        readonly List<int> removeScratch = new List<int>();
        readonly MeshBuilder mb = new MeshBuilder();
        Material material;

        int chunksAhead = 4;
        float treeScale = 1f;

        // palette (sRGB, converted in the shader)
        static readonly Color Asphalt = new Color(0.17f, 0.18f, 0.22f);
        static readonly Color LineWhite = new Color(0.78f, 0.78f, 0.72f);
        static readonly Color LineYellow = new Color(0.85f, 0.68f, 0.25f);
        static readonly Color Shoulder = new Color(0.20f, 0.19f, 0.17f);
        static readonly Color Grass = new Color(0.13f, 0.17f, 0.08f);
        static readonly Color GrassFar = new Color(0.09f, 0.12f, 0.07f);
        static readonly Color Rail = new Color(0.42f, 0.44f, 0.48f);
        static readonly Color RailPost = new Color(0.30f, 0.30f, 0.33f);
        static readonly Color Reflector = new Color(1f, 0.55f, 0.1f);
        static readonly Color Birch = new Color(0.82f, 0.80f, 0.72f);
        static readonly Color[] Autumn =
        {
            new Color(0.92f, 0.46f, 0.10f), new Color(0.70f, 0.14f, 0.10f), new Color(0.95f, 0.74f, 0.20f),
            new Color(0.80f, 0.32f, 0.08f), new Color(0.55f, 0.20f, 0.10f), new Color(0.35f, 0.42f, 0.14f)
        };

        public void Init(CarController owner, RoadModel roadModel, TrackRecipe trackRecipe)
        {
            car = owner;
            road = roadModel;
            recipe = trackRecipe;
            material = MLMaterials.Opaque();
            ApplyQuality(GraphicsSettings.Preset);
        }

        public void ApplyQuality(QualityPreset preset)
        {
            chunksAhead = preset.chunksAhead;
            treeScale = preset.treeDensity;
            RebuildAll();
        }

        public void RebuildAll()
        {
            foreach (KeyValuePair<int, Chunk> kv in active) Recycle(kv.Value);
            active.Clear();
            if (car != null) UpdateChunks(true);
        }

        void LateUpdate()
        {
            if (car == null || road == null) return;
            CheckRebase();
            UpdateChunks(false);
        }

        void CheckRebase()
        {
            Vector3 p = car.transform.position;
            if (p.x * p.x + p.z * p.z > RebaseDistance * RebaseDistance)
            {
                road.Rebase(new Vector3(p.x, 0f, p.z));
                foreach (KeyValuePair<int, Chunk> kv in active) PositionChunk(kv.Value);
            }
        }

        void UpdateChunks(bool buildAll)
        {
            int carChunk = Mathf.Max(0, Mathf.FloorToInt(car.S / ChunkLength));
            int first = Mathf.Max(0, carChunk - 1);
            int last = carChunk + chunksAhead;

            removeScratch.Clear();
            foreach (KeyValuePair<int, Chunk> kv in active)
                if (kv.Key < first || kv.Key > last) removeScratch.Add(kv.Key);
            for (int i = 0; i < removeScratch.Count; i++)
            {
                Recycle(active[removeScratch[i]]);
                active.Remove(removeScratch[i]);
            }

            // nearest missing chunk first; one per frame during play to avoid hitches
            for (int k = first; k <= last; k++)
            {
                if (active.ContainsKey(k)) continue;
                active[k] = BuildChunk(k);
                if (!buildAll) break;
            }
        }

        void Recycle(Chunk c)
        {
            c.go.SetActive(false);
            pool.Enqueue(c);
        }

        void PositionChunk(Chunk c)
        {
            c.go.transform.position = road.WorldPos(c.index * ChunkLength, 0f);
        }

        // ------------------------------------------------------------------------------------------

        static float Noise(float s, float f, float phase)
        {
            return 0.5f + 0.5f * Mathf.Sin(s * f + phase);
        }

        /// <summary>Ground height relative to the road surface at lateral distance ad from the centre. Shared by terrain and trees.</summary>
        float GroundOffset(float s, float ad, int side)
        {
            float edge = recipe.roadHalfWidth;
            float ph = side * 1.9f;
            float h1 = -0.12f + 0.25f * Noise(s, 0.045f, ph);
            float h2 = 0.7f + 2.2f * Noise(s, 0.021f, ph + 2.1f);
            float h3 = 2.5f + 5.5f * Noise(s, 0.013f, ph + 4.3f);
            float d0 = edge + 0.9f, d1 = 9f, d2 = 22f, d3 = 46f;
            if (ad <= edge) return 0f;
            if (ad <= d0) return Mathf.Lerp(0f, -0.06f, (ad - edge) / (d0 - edge));
            if (ad <= d1) return Mathf.Lerp(-0.06f, h1, (ad - d0) / (d1 - d0));
            if (ad <= d2) return Mathf.Lerp(h1, h2, (ad - d1) / (d2 - d1));
            if (ad <= d3) return Mathf.Lerp(h2, h3, (ad - d2) / (d3 - d2));
            return h3;
        }

        Chunk BuildChunk(int index)
        {
            Chunk c;
            if (pool.Count > 0)
            {
                c = pool.Dequeue();
                c.go.SetActive(true);
            }
            else
            {
                c = new Chunk();
                c.go = new GameObject("Chunk");
                c.go.transform.SetParent(transform, false);
                c.mf = c.go.AddComponent<MeshFilter>();
                MeshRenderer mr = c.go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = material;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                c.mesh = new Mesh();
                c.mesh.name = "TrackChunk";
                c.mf.sharedMesh = c.mesh;
            }
            c.index = index;
            c.go.name = "Chunk_" + index;
            PositionChunk(c);

            mb.Clear();
            BuildChunkMesh(index);
            mb.Build(c.mesh);
            return c;
        }

        void BuildChunkMesh(int index)
        {
            float s0 = index * ChunkLength;
            Vector3 anchor = road.WorldPos(s0, 0f);
            int rows = Mathf.RoundToInt(ChunkLength / Step);
            float hw = recipe.roadHalfWidth;
            float rail = hw + recipe.railOffset;
            System.Random rng = new System.Random(recipe.seed * 7919 + index * 104729);

            // lateral nodes for the ground ribbon (negative = left)
            float[] nodes = { -46f, -22f, -9f, -(hw + 0.9f), -hw, hw, hw + 0.9f, 9f, 22f, 46f };
            Vector3[] prev = new Vector3[nodes.Length];
            Vector3[] cur = new Vector3[nodes.Length];
            Vector3 prevRailL = Vector3.zero, prevRailR = Vector3.zero;
            Vector3 prevLineL = Vector3.zero, prevLineR = Vector3.zero, prevLineC = Vector3.zero;

            for (int r = 0; r <= rows; r++)
            {
                float s = s0 + r * Step;
                RoadPoint rp = road.At(s);

                for (int n = 0; n < nodes.Length; n++)
                {
                    float d = nodes[n];
                    int side = d < 0f ? -1 : 1;
                    Vector3 w = road.WorldPos(rp, d) - anchor;
                    w.y += GroundOffset(s, Mathf.Abs(d), side);
                    cur[n] = w;
                }

                Vector3 railL = road.WorldPos(rp, -rail) - anchor;
                Vector3 railR = road.WorldPos(rp, rail) - anchor;
                railL.y += GroundOffset(s, rail, -1);
                railR.y += GroundOffset(s, rail, 1);
                Vector3 lineL = road.WorldPos(rp, -(hw - 0.35f)) - anchor + Vector3.up * 0.04f;
                Vector3 lineR = road.WorldPos(rp, hw - 0.35f) - anchor + Vector3.up * 0.04f;
                Vector3 lineC = road.WorldPos(rp, 0f) - anchor + Vector3.up * 0.04f;

                if (r > 0)
                {
                    // ground strips
                    for (int n = 0; n < nodes.Length - 1; n++)
                    {
                        if (n == 4) continue;   // road itself
                        bool shoulder = (n == 3 || n == 5);
                        bool far = (n == 0 || n == 8);
                        Color col = shoulder ? Shoulder : (far ? GrassFar : Grass);
                        col = MeshBuilder.Vary(col, 0.18f, rng);
                        mb.AddUpQuad(prev[n], prev[n + 1], cur[n + 1], cur[n], col);
                    }
                    // road
                    mb.AddUpQuad(prev[4], prev[5], cur[5], cur[4], MeshBuilder.Vary(Asphalt, 0.04f, rng));

                    // edge lines (solid) and centre line (dashed)
                    AddLine(prevLineL, lineL, 0.07f, LineWhite);
                    AddLine(prevLineR, lineR, 0.07f, LineWhite);
                    if (((r + index * 40) / 3) % 2 == 0) AddLine(prevLineC, lineC, 0.07f, LineYellow);

                    // guard rail beam (two faces so it reads from both sides)
                    AddRailBeam(prevRailL, railL, -1);
                    AddRailBeam(prevRailR, railR, 1);

                    // posts every 4 m (every second row), reflectors on every other post
                    if (r % 2 == 0)
                    {
                        mb.AddBox(railL + Vector3.up * 0.38f, new Vector3(0.12f, 0.76f, 0.12f), RailPost);
                        mb.AddBox(railR + Vector3.up * 0.38f, new Vector3(0.12f, 0.76f, 0.12f), RailPost);
                        if ((r / 2) % 2 == 0)
                        {
                            mb.AddBox(railL + new Vector3(0.07f, 0.66f, 0f), new Vector3(0.03f, 0.1f, 0.1f), Reflector, 0.8f);
                            mb.AddBox(railR + new Vector3(-0.07f, 0.66f, 0f), new Vector3(0.03f, 0.1f, 0.1f), Reflector, 0.8f);
                        }
                    }
                }

                Vector3[] t = prev; prev = cur; cur = t;
                prevRailL = railL; prevRailR = railR;
                prevLineL = lineL; prevLineR = lineR; prevLineC = lineC;
            }

            BuildTrees(index, s0, anchor, rng);
        }

        void AddLine(Vector3 a, Vector3 b, float halfWidth, Color col)
        {
            Vector3 dir = (b - a);
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) return;
            Vector3 side = Vector3.Cross(Vector3.up, dir.normalized) * halfWidth;
            mb.AddUpQuad(a - side, a + side, b + side, b - side, col);
        }

        void AddRailBeam(Vector3 a, Vector3 b, int side)
        {
            // vertical face of the beam, 0.35 m to 0.70 m above the ground, plus a top face
            Vector3 lo0 = a + Vector3.up * 0.35f, hi0 = a + Vector3.up * 0.70f;
            Vector3 lo1 = b + Vector3.up * 0.35f, hi1 = b + Vector3.up * 0.70f;
            Vector3 inside = (a + b) * 0.5f + new Vector3(0f, 0.5f, 0f);
            // lateral direction pointing away from the road centre
            Vector3 along = (b - a); along.y = 0f;
            Vector3 outward = Vector3.Cross(Vector3.up, along.normalized) * side;   // away from the road centre
            Vector3 insideRoad = inside - outward * 0.5f;
            Vector3 insideOut = inside + outward * 0.5f;
            mb.AddQuad(lo0, lo1, hi1, hi0, insideOut, Rail);   // face looking at the road
            mb.AddQuad(lo0, lo1, hi1, hi0, insideRoad, Rail);  // face looking away
            mb.AddUpQuad(hi0, hi1, hi1 + outward * 0.06f, hi0 + outward * 0.06f, Rail);
        }

        void BuildTrees(int index, float s0, Vector3 anchor, System.Random rng)
        {
            float density = Mathf.Max(0.05f, recipe.treeDensity * treeScale);
            float hw = recipe.roadHalfWidth;
            for (int sideIdx = 0; sideIdx < 2; sideIdx++)
            {
                int side = sideIdx == 0 ? -1 : 1;
                float s = s0 + (float)rng.NextDouble() * 4f;
                while (s < s0 + ChunkLength)
                {
                    float spacing = Mathf.Lerp(5f, 11f, (float)rng.NextDouble()) / density;
                    s += spacing;
                    if (s >= s0 + ChunkLength) break;

                    float dist = hw + 4.5f + (float)rng.NextDouble() * (rng.NextDouble() < 0.7 ? 9f : 26f);
                    RoadPoint rp = road.At(s);
                    Vector3 basePos = road.WorldPos(rp, side * dist) - anchor;
                    basePos.y += GroundOffset(s, dist, side) - 0.1f;

                    float roll = (float)rng.NextDouble();
                    if (roll < 0.14f) AddBush(basePos, rng);
                    else AddTree(basePos, rng);
                }
            }
        }

        void AddTree(Vector3 basePos, System.Random rng)
        {
            float height = Mathf.Lerp(2.6f, 5.0f, (float)rng.NextDouble());
            float trunkR = Mathf.Lerp(0.12f, 0.22f, (float)rng.NextDouble());
            Color trunk = MeshBuilder.Vary(Birch, 0.08f, rng);
            mb.AddCylinder(basePos, basePos + Vector3.up * height, trunkR, trunkR * 0.6f, 6, trunk, trunk, trunk);

            Color leaf = Autumn[rng.Next(Autumn.Length)];
            int blobs = 2 + rng.Next(2);
            for (int i = 0; i < blobs; i++)
            {
                float rad = Mathf.Lerp(1.3f, 2.3f, (float)rng.NextDouble());
                Vector3 off = new Vector3(((float)rng.NextDouble() - 0.5f) * 1.6f,
                                          height + ((float)rng.NextDouble() - 0.2f) * 1.3f,
                                          ((float)rng.NextDouble() - 0.5f) * 1.6f);
                mb.AddBlob(basePos + off, new Vector3(rad, rad * 0.85f, rad), 7, 3, MeshBuilder.Vary(leaf, 0.12f, rng), rng, 0.18f, 0.10f);
            }
        }

        void AddBush(Vector3 basePos, System.Random rng)
        {
            Color leaf = Autumn[rng.Next(Autumn.Length)];
            float rad = Mathf.Lerp(0.5f, 1.0f, (float)rng.NextDouble());
            mb.AddBlob(basePos + Vector3.up * (rad * 0.5f), new Vector3(rad, rad * 0.7f, rad), 6, 3, leaf, rng, 0.2f, 0.12f);
        }
    }
}
