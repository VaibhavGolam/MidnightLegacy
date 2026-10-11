using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>Gantry placed across the road: 0 checkpoint, 1 finish line, 2 start line.</summary>
    public struct TrackMarker
    {
        public float s;
        public int kind;
    }

    /// <summary>
    /// Builds the visible world in 80 m chunks around the car, drawn flat as seen from above: ground, road, lane lines,
    /// guard rails and themed scenery (autumn forest, pine pass, coast, city). Each chunk is two meshes: one opaque
    /// (everything solid) and one additive (street lamp pools and neon glow). Chunks are pooled and rebuilt as the car
    /// moves forward. Also performs the floating origin rebase.
    /// Layer heights (y): ground 0, road 0.05, lines 0.08, rails 0.16, props 0.3 to 0.6. Cars are drawn above all of it.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class TrackGenerator : MonoBehaviour
    {
        public const float ChunkLength = 80f;
        const float Step = 2.5f;             // metres between cross sections
        const float RebaseDistance = 2500f;

        const float YGround = 0f;
        const float YRoad = 0.05f;
        const float YLine = 0.08f;
        const float YRailShadow = 0.06f;
        const float YRail = 0.16f;
        const float YPost = 0.2f;

        sealed class Chunk
        {
            public int index;
            public GameObject go;
            public MeshFilter mf;
            public Mesh mesh;
            public GameObject glowGo;
            public Mesh glowMesh;
        }

        CarController car;
        RoadModel road;
        TrackRecipe recipe;
        ThemeDef theme;
        bool wet;

        readonly Dictionary<int, Chunk> active = new Dictionary<int, Chunk>();
        readonly Queue<Chunk> pool = new Queue<Chunk>();
        readonly List<int> removeScratch = new List<int>();
        readonly List<TrackMarker> markers = new List<TrackMarker>();
        readonly MeshBuilder mb = new MeshBuilder();
        readonly MeshBuilder gb = new MeshBuilder();   // glow
        Material material;
        Material glowMaterial;

        int chunksAhead = 4;
        float treeScale = 1f;

        static readonly Color LineWhite = new Color(0.80f, 0.80f, 0.74f);
        static readonly Color LineYellow = new Color(0.88f, 0.70f, 0.26f);
        static readonly Color RailPost = new Color(0.22f, 0.22f, 0.25f);
        static readonly Color Reflector = new Color(1f, 0.55f, 0.1f);
        static readonly Color LampWarm = new Color(1f, 0.72f, 0.38f);
        static readonly Color Rock = new Color(0.34f, 0.35f, 0.38f);
        static readonly Color Sea = new Color(0.08f, 0.20f, 0.36f);
        static readonly Color Ink = new Color(0.02f, 0.02f, 0.03f);
        static readonly Color[] Neon =
        {
            new Color(1f, 0.25f, 0.65f), new Color(0.2f, 0.85f, 1f), new Color(1f, 0.70f, 0.25f), new Color(0.55f, 0.45f, 1f)
        };

        public void Init(CarController owner, RoadModel roadModel, TrackRecipe trackRecipe, ThemeDef themeDef, bool wetRoad)
        {
            car = owner;
            road = roadModel;
            recipe = trackRecipe;
            theme = themeDef;
            wet = wetRoad;
            material = MLMaterials.Opaque();
            glowMaterial = MLMaterials.Additive();
            ApplyQuality(GraphicsSettings.Preset);
        }

        public void ApplyQuality(QualityPreset preset)
        {
            chunksAhead = preset.chunksAhead;
            treeScale = preset.treeDensity;
            RebuildAll();
        }

        /// <summary>Gantries and the finish line. Rebuilds the visible chunks.</summary>
        public void SetMarkers(List<TrackMarker> list)
        {
            markers.Clear();
            if (list != null) markers.AddRange(list);
            RebuildAll();
        }

        public void RebuildAll()
        {
            foreach (KeyValuePair<int, Chunk> kv in active) Recycle(kv.Value);
            active.Clear();
            if (car != null) UpdateChunks(true);
        }

        void OnDestroy()
        {
            foreach (KeyValuePair<int, Chunk> kv in active)
            {
                if (kv.Value.mesh != null) Destroy(kv.Value.mesh);
                if (kv.Value.glowMesh != null) Destroy(kv.Value.glowMesh);
            }
            foreach (Chunk c in pool)
            {
                if (c.mesh != null) Destroy(c.mesh);
                if (c.glowMesh != null) Destroy(c.glowMesh);
            }
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

        static Color Shade(Color c, float f)
        {
            return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), 1f);
        }

        static Vector3 Lift(Vector3 v, float y)
        {
            v.y = y;
            return v;
        }

        /// <summary>World position (relative to the chunk anchor) of a road point at lateral offset d, at layer height y.</summary>
        Vector3 P(RoadPoint rp, float d, float y, Vector3 anchor)
        {
            Vector3 w = road.WorldPos(rp, d) - anchor;
            w.y = y;
            return w;
        }

        /// <summary>Turns a local (lateral, along) offset into a world offset for something that faces along the road.</summary>
        static Vector2 Local(float lx, float lz, float yawRad)
        {
            float c = Mathf.Cos(yawRad), s = Mathf.Sin(yawRad);
            return new Vector2(lx * c + lz * s, -lx * s + lz * c);
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

                c.glowGo = new GameObject("Glow");
                c.glowGo.transform.SetParent(c.go.transform, false);
                MeshFilter gf = c.glowGo.AddComponent<MeshFilter>();
                MeshRenderer gr = c.glowGo.AddComponent<MeshRenderer>();
                gr.sharedMaterial = glowMaterial;
                gr.shadowCastingMode = ShadowCastingMode.Off;
                gr.receiveShadows = false;
                c.glowMesh = new Mesh();
                c.glowMesh.name = "TrackGlow";
                gf.sharedMesh = c.glowMesh;
            }
            c.index = index;
            c.go.name = "Chunk_" + index;
            PositionChunk(c);

            mb.Clear();
            gb.Clear();
            BuildChunkMesh(index);
            mb.Build(c.mesh);
            gb.Build(c.glowMesh);
            c.glowGo.SetActive(gb.VertexCount > 0);
            return c;
        }

        Color StripColor(int n, System.Random rng)
        {
            bool shoulder = (n == 3 || n == 5);
            bool far = (n == 0 || n == 8);
            Color col = shoulder ? theme.shoulder : (far ? theme.groundFar : theme.ground);
            if (theme.scenery == Scenery.Coast && n >= 7) col = (n == 8) ? Sea : theme.groundFar;
            if (wet) col *= 0.8f;
            return MeshBuilder.Vary(col, theme.scenery == Scenery.Coast && n >= 7 ? 0.05f : 0.09f, rng);
        }

        void BuildChunkMesh(int index)
        {
            float s0 = index * ChunkLength;
            Vector3 anchor = road.WorldPos(s0, 0f);
            int rows = Mathf.RoundToInt(ChunkLength / Step);
            float hw = recipe.roadHalfWidth;
            float rail = hw + recipe.railOffset;
            System.Random rng = new System.Random(recipe.seed * 7919 + index * 104729);
            Color asphalt = wet ? new Color(theme.asphalt.r * 0.65f, theme.asphalt.g * 0.68f, theme.asphalt.b * 0.85f) : theme.asphalt;
            Color wear = Shade(asphalt, wet ? 1.25f : 0.88f);
            float lineEmit = wet ? 0.35f : 0f;
            Color railColor = theme.rail;
            Color railShadow = Shade(theme.ground, 0.55f);

            // lateral nodes for the ground ribbon (negative = left)
            float[] nodes = { -40f, -16f, -7f, -(hw + 0.9f), -hw, hw, hw + 0.9f, 7f, 16f, 40f };
            Vector3[] prev = new Vector3[nodes.Length];
            Vector3[] cur = new Vector3[nodes.Length];
            Vector3 prevRailL = Vector3.zero, prevRailR = Vector3.zero;
            Vector3 prevLineL = Vector3.zero, prevLineR = Vector3.zero, prevLineC = Vector3.zero;
            Vector3 prevWearL = Vector3.zero, prevWearR = Vector3.zero, prevFoam = Vector3.zero;
            bool coast = theme.scenery == Scenery.Coast;
            float lane = TrafficSystem.LaneOffset;

            for (int r = 0; r <= rows; r++)
            {
                float s = s0 + r * Step;
                RoadPoint rp = road.At(s);

                for (int n = 0; n < nodes.Length; n++) cur[n] = P(rp, nodes[n], YGround, anchor);

                Vector3 railL = P(rp, -rail, YRail, anchor);
                Vector3 railR = P(rp, rail, YRail, anchor);
                Vector3 lineL = P(rp, -(hw - 0.35f), YLine, anchor);
                Vector3 lineR = P(rp, hw - 0.35f, YLine, anchor);
                Vector3 lineC = P(rp, 0f, YLine, anchor);
                Vector3 wearL = P(rp, -lane, YRoad + 0.01f, anchor);
                Vector3 wearR = P(rp, lane, YRoad + 0.01f, anchor);
                Vector3 foam = P(rp, 7f, 0.03f, anchor);

                if (r > 0)
                {
                    // ground strips
                    for (int n = 0; n < nodes.Length - 1; n++)
                    {
                        if (n == 4) continue;   // road itself
                        mb.AddUpQuad(prev[n], prev[n + 1], cur[n + 1], cur[n], StripColor(n, rng));
                    }
                    // road surface, with darker worn tracks where the tyres run
                    mb.AddUpQuad(Lift(prev[4], YRoad), Lift(prev[5], YRoad), Lift(cur[5], YRoad), Lift(cur[4], YRoad), MeshBuilder.Vary(asphalt, 0.035f, rng));
                    Flat.Line(mb, prevWearL, wearL, 0.55f, YRoad + 0.01f, wear, wet ? 0.06f : 0f);
                    Flat.Line(mb, prevWearR, wearR, 0.55f, YRoad + 0.01f, wear, wet ? 0.06f : 0f);

                    // edge lines (solid) and centre line (dashed)
                    Flat.Line(mb, prevLineL, lineL, 0.09f, YLine, LineWhite, lineEmit);
                    Flat.Line(mb, prevLineR, lineR, 0.09f, YLine, LineWhite, lineEmit);
                    if ((((r + index * rows) / 2) & 1) == 0) Flat.Line(mb, prevLineC, lineC, 0.09f, YLine, LineYellow, lineEmit);

                    // guard rail: a shadow on the ground, then the beam
                    Flat.Line(mb, Lift(prevRailL, YRailShadow) - Local(0.35f, 0f, rp.theta).ToV3(), Lift(railL, YRailShadow) - Local(0.35f, 0f, rp.theta).ToV3(), 0.3f, YRailShadow, railShadow);
                    Flat.Line(mb, Lift(prevRailR, YRailShadow) + Local(0.35f, 0f, rp.theta).ToV3(), Lift(railR, YRailShadow) + Local(0.35f, 0f, rp.theta).ToV3(), 0.3f, YRailShadow, railShadow);
                    Flat.Line(mb, prevRailL, railL, 0.13f, YRail, railColor);
                    Flat.Line(mb, prevRailR, railR, 0.13f, YRail, railColor);

                    // posts every 5 m, reflectors on every other post
                    if (r % 2 == 0)
                    {
                        Flat.Rect(mb, railL.x, railL.z, 0.15f, 0.15f, YPost, RailPost);
                        Flat.Rect(mb, railR.x, railR.z, 0.15f, 0.15f, YPost, RailPost);
                        if ((r / 2) % 2 == 0)
                        {
                            Vector2 inL = Local(0.2f, 0f, rp.theta), inR = Local(-0.2f, 0f, rp.theta);
                            Flat.Rect(mb, railL.x + inL.x, railL.z + inL.y, 0.07f, 0.07f, YPost + 0.02f, Reflector, 0.9f);
                            Flat.Rect(mb, railR.x + inR.x, railR.z + inR.y, 0.07f, 0.07f, YPost + 0.02f, Reflector, 0.9f);
                        }
                    }

                    if (coast) Flat.Line(mb, prevFoam, foam, 0.22f, 0.03f, new Color(0.75f, 0.88f, 1f), 0.35f);
                }

                Vector3[] t = prev; prev = cur; cur = t;
                prevRailL = railL; prevRailR = railR;
                prevLineL = lineL; prevLineR = lineR; prevLineC = lineC;
                prevWearL = wearL; prevWearR = wearR; prevFoam = foam;
            }

            switch (theme.scenery)
            {
                case Scenery.Autumn: BuildTrees(s0, anchor, rng, false); break;
                case Scenery.Pine: BuildTrees(s0, anchor, rng, true); break;
                case Scenery.Coast: BuildCoast(s0, anchor, rng); break;
                case Scenery.City: BuildCity(s0, anchor, rng); break;
            }
            BuildMarkers(s0, anchor);
        }

        // ---------------- gantries and finish line ----------------

        void BuildMarkers(float s0, Vector3 anchor)
        {
            float hw = recipe.roadHalfWidth;
            for (int i = 0; i < markers.Count; i++)
            {
                TrackMarker m = markers[i];
                if (m.s < s0 || m.s >= s0 + ChunkLength) continue;
                RoadPoint rp = road.At(m.s);
                float yaw = rp.theta * Mathf.Rad2Deg;
                Color post = new Color(0.20f, 0.21f, 0.25f);

                // gantry posts either side of the road
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 pp = P(rp, side * (hw + 0.8f), 0.9f, anchor);
                    Flat.Rect(mb, pp.x, pp.z, 0.22f, 0.22f, 0.9f, post, 0f, yaw);
                    Color lamp = m.kind == 1 ? Color.white : (m.kind == 2 ? new Color(0.25f, 0.95f, 0.45f) : new Color(1f, 0.70f, 0.20f));
                    Flat.Rect(mb, pp.x, pp.z, 0.12f, 0.12f, 0.95f, lamp, 1f, yaw);
                    Flat.Glow(gb, pp.x, pp.z, 3.2f, 0.3f, lamp, 0.45f);
                }

                if (m.kind == 1)
                {
                    // finish: a checkered strip across the road
                    int cells = 10;
                    float cw = hw * 2f / cells;
                    for (int row = 0; row < 3; row++)
                    {
                        RoadPoint ra = road.At(m.s + row * 0.8f), rb = road.At(m.s + (row + 1) * 0.8f);
                        for (int c = 0; c < cells; c++)
                        {
                            float d0 = -hw + c * cw, d1 = d0 + cw;
                            Color col = ((c + row) & 1) == 0 ? new Color(0.93f, 0.93f, 0.93f) : new Color(0.06f, 0.06f, 0.07f);
                            mb.AddUpQuad(P(ra, d0, YLine + 0.01f, anchor), P(ra, d1, YLine + 0.01f, anchor), P(rb, d1, YLine + 0.01f, anchor), P(rb, d0, YLine + 0.01f, anchor), col, 0.35f);
                        }
                    }
                }
                else
                {
                    Color col = m.kind == 2 ? new Color(0.25f, 0.95f, 0.45f) : new Color(1f, 0.70f, 0.20f);
                    RoadPoint ra = road.At(m.s), rb = road.At(m.s + 1.0f);
                    mb.AddUpQuad(P(ra, -hw, YLine + 0.01f, anchor), P(ra, hw, YLine + 0.01f, anchor), P(rb, hw, YLine + 0.01f, anchor), P(rb, -hw, YLine + 0.01f, anchor), col, 0.9f);
                }

                // a faint light wall across the road
                {
                    Color col = m.kind == 1 ? Color.white : (m.kind == 2 ? new Color(0.25f, 0.95f, 0.45f) : new Color(1f, 0.70f, 0.20f));
                    RoadPoint ra = road.At(m.s - 2f), rb = road.At(m.s + 3f);
                    gb.AddUpQuad(P(ra, -hw, 0.1f, anchor), P(ra, hw, 0.1f, anchor), P(rb, hw, 0.1f, anchor), P(rb, -hw, 0.1f, anchor), col, 0.12f);
                }
            }
        }

        // ---------------- scenery ----------------

        Color Foliage(System.Random rng)
        {
            return theme.foliage[rng.Next(theme.foliage.Length)];
        }

        void BuildTrees(float s0, Vector3 anchor, System.Random rng, bool pine)
        {
            float density = Mathf.Max(0.05f, recipe.treeDensity * treeScale);
            float hw = recipe.roadHalfWidth;
            for (int sideIdx = 0; sideIdx < 2; sideIdx++)
            {
                int side = sideIdx == 0 ? -1 : 1;
                float s = s0 + (float)rng.NextDouble() * 4f;
                while (s < s0 + ChunkLength)
                {
                    float spacing = Mathf.Lerp(3.5f, 8f, (float)rng.NextDouble()) / density;
                    s += spacing;
                    if (s >= s0 + ChunkLength) break;

                    float dist = hw + 3.8f + (float)rng.NextDouble() * (rng.NextDouble() < 0.7 ? 8f : 22f);
                    RoadPoint rp = road.At(s);
                    Vector3 basePos = P(rp, side * dist, 0f, anchor);

                    float roll = (float)rng.NextDouble();
                    if (pine)
                    {
                        if (roll < 0.12f) AddRock(basePos, rng);
                        else AddPine(basePos, rng);
                    }
                    else
                    {
                        if (roll < 0.14f) AddBush(basePos, rng);
                        else AddTree(basePos, rng);
                    }
                }
            }
        }

        void AddTree(Vector3 p, System.Random rng)
        {
            float R = Mathf.Lerp(1.5f, 2.6f, (float)rng.NextDouble());
            Color leaf = MeshBuilder.Vary(Foliage(rng), 0.1f, rng);
            Flat.Circle(mb, p.x + 0.9f, p.z - 0.7f, R * 1.05f, 0.10f, Shade(theme.ground, 0.5f), 0f, 10);
            Flat.Circle(mb, p.x, p.z, R, 0.40f, Shade(leaf, 0.68f), 0f, 11);
            int blobs = 2 + rng.Next(2);
            for (int b = 0; b < blobs; b++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float off = R * 0.42f * (float)rng.NextDouble();
                float rr = R * Mathf.Lerp(0.55f, 0.75f, (float)rng.NextDouble());
                Flat.Circle(mb, p.x + Mathf.Cos(ang) * off, p.z + Mathf.Sin(ang) * off, rr, 0.44f + b * 0.02f, Shade(leaf, Mathf.Lerp(0.85f, 1.0f, (float)rng.NextDouble())), 0f, 9);
            }
            Flat.Circle(mb, p.x - R * 0.22f, p.z + R * 0.22f, R * 0.36f, 0.54f, Shade(leaf, 1.2f), 0f, 8);
        }

        void AddBush(Vector3 p, System.Random rng)
        {
            Color leaf = Foliage(rng);
            float R = Mathf.Lerp(0.55f, 1.05f, (float)rng.NextDouble());
            Flat.Circle(mb, p.x + 0.35f, p.z - 0.3f, R, 0.10f, Shade(theme.ground, 0.5f), 0f, 8);
            Flat.Circle(mb, p.x, p.z, R, 0.38f, Shade(leaf, 0.75f), 0f, 8);
            Flat.Circle(mb, p.x - R * 0.2f, p.z + R * 0.2f, R * 0.6f, 0.42f, Shade(leaf, 1.05f), 0f, 7);
        }

        void AddPine(Vector3 p, System.Random rng)
        {
            float R = Mathf.Lerp(1.2f, 2.1f, (float)rng.NextDouble());
            Color leaf = MeshBuilder.Vary(Foliage(rng), 0.1f, rng);
            float rot = (float)rng.NextDouble() * 45f;
            Flat.Star(mb, p.x + 0.9f, p.z - 0.7f, R * 1.0f, R * 0.55f, 8, 0.10f, Shade(theme.ground, 0.5f), 0f, rot);
            Flat.Star(mb, p.x, p.z, R, R * 0.52f, 8, 0.40f, Shade(leaf, 0.62f), 0f, rot);
            Flat.Star(mb, p.x, p.z, R * 0.78f, R * 0.40f, 8, 0.44f, Shade(leaf, 0.85f), 0f, rot + 22f);
            Flat.Star(mb, p.x, p.z, R * 0.52f, R * 0.26f, 8, 0.48f, Shade(leaf, 1.12f), 0f, rot + 45f);
            Flat.Circle(mb, p.x, p.z, 0.12f, 0.52f, theme.trunk * 0.5f, 0f, 6);
        }

        void AddRock(Vector3 p, System.Random rng)
        {
            float R = Mathf.Lerp(0.8f, 1.9f, (float)rng.NextDouble());
            float rot = (float)rng.NextDouble() * 180f;
            Flat.Rock(mb, p.x + 0.5f, p.z - 0.4f, R * 1.05f, R * 1.3f, 7, 0.10f, Shade(theme.ground, 0.5f), rng, rot);
            Flat.Rock(mb, p.x, p.z, R, R * 1.25f, 7, 0.34f, Rock, rng, rot);
            Flat.Rock(mb, p.x - R * 0.15f, p.z + R * 0.2f, R * 0.55f, R * 0.7f, 6, 0.38f, Shade(Rock, 1.3f), rng, rot + 20f);
        }

        /// <summary>Street lamp: a pole, an arm reaching over the road, a warm lamp head and a glow on the ground.</summary>
        void AddLamp(RoadPoint rp, int side, float dPole, Vector3 anchor, float poolRadius)
        {
            float yaw = rp.theta;
            Vector3 pole = P(rp, side * dPole, 0f, anchor);
            Vector2 armDir = Local(-side * 1.7f, 0f, yaw);
            Vector3 head = new Vector3(pole.x + armDir.x, 0.9f, pole.z + armDir.y);
            Color metal = new Color(0.22f, 0.23f, 0.27f);
            Flat.Circle(mb, pole.x, pole.z, 0.16f, 0.80f, metal, 0f, 8);
            Flat.Line(mb, new Vector3(pole.x, 0f, pole.z), head, 0.06f, 0.84f, metal);
            Flat.Rect(mb, head.x, head.z, 0.16f, 0.28f, 0.90f, LampWarm, 1f, yaw * Mathf.Rad2Deg);
            Flat.Glow(gb, head.x, head.z, poolRadius, 0.12f, LampWarm, 0.30f, 16);
            Flat.Glow(gb, head.x, head.z, poolRadius * 0.45f, 0.13f, LampWarm, 0.22f, 12);
        }

        void BuildCoast(float s0, Vector3 anchor, System.Random rng)
        {
            float density = Mathf.Max(0.05f, recipe.treeDensity * treeScale);
            float hw = recipe.roadHalfWidth;

            // scrubby bushes and rocks on the land side only
            float s = s0 + (float)rng.NextDouble() * 6f;
            while (s < s0 + ChunkLength)
            {
                s += Mathf.Lerp(5f, 12f, (float)rng.NextDouble()) / density;
                if (s >= s0 + ChunkLength) break;
                float dist = hw + 3.5f + (float)rng.NextDouble() * 16f;
                RoadPoint rp = road.At(s);
                Vector3 basePos = P(rp, -dist, 0f, anchor);
                if (rng.NextDouble() < 0.3) AddRock(basePos, rng); else AddBush(basePos, rng);
            }

            // waves on the sea side
            int waves = 7;
            for (int i = 0; i < waves; i++)
            {
                float ws = s0 + (float)rng.NextDouble() * ChunkLength;
                float wd = 10f + (float)rng.NextDouble() * 28f;
                RoadPoint rp = road.At(ws);
                Vector3 wp = P(rp, wd, 0.04f, anchor);
                Flat.Rect(mb, wp.x, wp.z, 0.05f, 0.7f + (float)rng.NextDouble() * 1.6f, 0.04f, new Color(0.55f, 0.78f, 1f), 0.35f, rp.theta * Mathf.Rad2Deg + (float)(rng.NextDouble() - 0.5) * 20f);
            }

            // street lamps every 40 m on the land side
            float lampS = Mathf.Ceil(s0 / 40f) * 40f;
            for (; lampS < s0 + ChunkLength; lampS += 40f)
                AddLamp(road.At(lampS), -1, hw + 1.9f, anchor, 7.5f);
        }

        void BuildCity(float s0, Vector3 anchor, System.Random rng)
        {
            float hw = recipe.roadHalfWidth;
            float density = Mathf.Max(0.3f, treeScale);

            for (int sideIdx = 0; sideIdx < 2; sideIdx++)
            {
                int side = sideIdx == 0 ? -1 : 1;
                float s = s0 + (float)rng.NextDouble() * 6f;
                while (s < s0 + ChunkLength)
                {
                    float len = Mathf.Lerp(9f, 20f, (float)rng.NextDouble());
                    float dep = Mathf.Lerp(7f, 14f, (float)rng.NextDouble());
                    float gap = Mathf.Lerp(2f, 7f, (float)rng.NextDouble());
                    float sc = s + len * 0.5f;
                    s += len + gap;
                    if (sc >= s0 + ChunkLength) break;
                    if (rng.NextDouble() > density + 0.35) continue;

                    float dist = hw + 3.4f + dep * 0.5f + (float)rng.NextDouble() * 2.5f;
                    RoadPoint rp = road.At(sc);
                    Vector3 c = P(rp, side * dist, 0f, anchor);
                    Color col = MeshBuilder.Vary(Foliage(rng), 0.15f, rng);
                    AddBuilding(c, dep, len, rp.theta, side, col, rng);
                }
            }

            // zebra crossing now and then, a good speed reference
            if (rng.NextDouble() < 0.3)
            {
                float zs = s0 + 15f + (float)rng.NextDouble() * (ChunkLength - 30f);
                RoadPoint rp = road.At(zs);
                int bars = 9;
                float bw = (hw * 2f - 0.4f) / bars;
                for (int k = 0; k < bars; k++)
                {
                    if ((k & 1) != 0) continue;
                    Vector3 bp = P(rp, -hw + 0.2f + bw * (k + 0.5f), YLine, anchor);
                    Flat.Rect(mb, bp.x, bp.z, bw * 0.5f, 1.5f, YLine, new Color(0.82f, 0.82f, 0.78f), wet ? 0.3f : 0f, rp.theta * Mathf.Rad2Deg);
                }
            }

            // sodium street lamps both sides every 36 m
            float lampS = Mathf.Ceil(s0 / 36f) * 36f;
            for (; lampS < s0 + ChunkLength; lampS += 36f)
            {
                RoadPoint rp = road.At(lampS);
                AddLamp(rp, -1, hw + 1.9f, anchor, 7f);
                AddLamp(rp, 1, hw + 1.9f, anchor, 7f);
            }
        }

        /// <summary>A flat rooftop seen from above: dark outline, roof, inset, rooftop units, skylights and a neon edge facing the road.</summary>
        void AddBuilding(Vector3 c, float dep, float len, float yawRad, int side, Color col, System.Random rng)
        {
            float yaw = yawRad * Mathf.Rad2Deg;
            float hd = dep * 0.5f, hl = len * 0.5f;
            Flat.Rect(mb, c.x + 0.8f, c.z - 0.7f, hd, hl, 0.10f, Shade(theme.ground, 0.45f), 0f, yaw);   // shadow on the ground
            Flat.Rect(mb, c.x, c.z, hd + 0.14f, hl + 0.14f, 0.28f, Shade(col, 0.4f), 0f, yaw);
            Flat.Rect(mb, c.x, c.z, hd, hl, 0.34f, col, 0f, yaw);
            Flat.Rect(mb, c.x, c.z, hd - 0.5f, hl - 0.5f, 0.38f, Shade(col, 0.86f), 0f, yaw);

            int units = 1 + rng.Next(3);
            for (int u = 0; u < units; u++)
            {
                Vector2 o = Local(((float)rng.NextDouble() - 0.5f) * (dep - 3f), ((float)rng.NextDouble() - 0.5f) * (len - 3f), yawRad);
                float w = Mathf.Lerp(0.6f, 1.6f, (float)rng.NextDouble());
                float l = Mathf.Lerp(0.8f, 2.2f, (float)rng.NextDouble());
                Flat.Rect(mb, c.x + o.x, c.z + o.y, w, l, 0.42f, Shade(col, rng.NextDouble() < 0.5 ? 1.25f : 0.6f), 0f, yaw);
            }
            if (rng.NextDouble() < 0.5)
            {
                Vector2 o = Local(((float)rng.NextDouble() - 0.5f) * (dep - 4f), ((float)rng.NextDouble() - 0.5f) * (len - 4f), yawRad);
                bool cool = rng.NextDouble() < 0.5;
                Flat.Rect(mb, c.x + o.x, c.z + o.y, 0.9f, 1.4f, 0.44f, cool ? new Color(0.6f, 0.85f, 1f) : new Color(1f, 0.82f, 0.45f), 0.8f, yaw);
            }

            // neon strip along the edge that faces the road, and its glow spilling onto the street
            Color neon = Neon[rng.Next(Neon.Length)];
            Vector2 edge = Local(-side * (hd - 0.1f), 0f, yawRad);
            Flat.Rect(mb, c.x + edge.x, c.z + edge.y, 0.09f, hl - 0.5f, 0.46f, neon, 0.95f, yaw);
            Vector2 spill = Local(-side * (hd + 1.2f), 0f, yawRad);
            Flat.Glow(gb, c.x + spill.x, c.z + spill.y, Mathf.Min(hl, 7f), 0.11f, neon, 0.2f, 12);
        }
    }

    static class Vec2Ext
    {
        public static Vector3 ToV3(this Vector2 v)
        {
            return new Vector3(v.x, 0f, v.y);
        }
    }
}
