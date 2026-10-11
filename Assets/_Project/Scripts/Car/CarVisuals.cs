using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Draws the car as a flat top-down picture built from layered shapes (outline, body, glass, roof, lights,
    /// spoiler, decals, wheels). Three body styles, a spoiler, decals, rims and underglow, all driven by a CarLook.
    /// The wheels steer, the roof slides a little under cornering and braking (a cheap sense of height), and the
    /// brake lights glow while braking. Only this script draws the car, so real art can replace it later.
    /// Local axes: x right, z forward, y is the layer height (higher = drawn on top), origin in the middle of the car.
    /// </summary>
    public sealed class CarVisuals : MonoBehaviour
    {
        /// <summary>How far the roof slides sideways per g of cornering, in metres.</summary>
        public float roofShiftPerG = 0.07f;

        public Transform HeadlightAnchor { get; private set; }
        public Transform BodyRoot { get { return body; } }

        ICarState state;
        CarLook look;
        Transform body;
        Transform roof;
        GameObject brakeGlow;
        readonly Transform[] steerPivots = new Transform[2];   // front left, front right
        readonly List<Mesh> meshes = new List<Mesh>();
        Vector2 roofShift;

        sealed class Dims
        {
            public float zB, zF, w;
            public Vector2[] right;     // body outline, right half, rear to front, as (x, z)
            public Vector2[] glass;     // greenhouse outline, right half, rear to front
            public float roofZ, roofHL, roofHW;
            public float gz0, gz1;      // greenhouse start and end along z
        }

        static Dims DimsFor(int style)
        {
            Dims d = new Dims();
            switch (style)
            {
                case 1: // hatchback
                    d.zB = -1.85f; d.zF = 1.88f; d.w = 0.80f;
                    d.right = Flat.Pts(0.62f, -1.85f, 0.74f, -1.78f, 0.80f, -1.55f, 0.80f, -0.90f, 0.80f, 0.60f, 0.77f, 1.20f, 0.70f, 1.65f, 0.56f, 1.82f, 0.42f, 1.88f);
                    d.glass = Flat.Pts(0.62f, -1.55f, 0.72f, -1.20f, 0.74f, 0.00f, 0.64f, 0.45f);
                    d.roofZ = -0.55f; d.roofHL = 0.80f; d.roofHW = 0.62f; d.gz0 = -1.55f; d.gz1 = 0.45f;
                    break;
                case 2: // wedge
                    d.zB = -2.00f; d.zF = 2.18f; d.w = 0.84f;
                    d.right = Flat.Pts(0.66f, -2.00f, 0.78f, -1.90f, 0.84f, -1.60f, 0.84f, -0.50f, 0.82f, 0.60f, 0.72f, 1.40f, 0.50f, 1.90f, 0.28f, 2.18f);
                    d.glass = Flat.Pts(0.56f, -0.90f, 0.70f, -0.50f, 0.70f, 0.10f, 0.56f, 0.55f);
                    d.roofZ = -0.25f; d.roofHL = 0.38f; d.roofHW = 0.50f; d.gz0 = -0.90f; d.gz1 = 0.55f;
                    break;
                default: // coupe
                    d.zB = -2.05f; d.zF = 2.08f; d.w = 0.82f;
                    d.right = Flat.Pts(0.60f, -2.05f, 0.72f, -1.98f, 0.79f, -1.80f, 0.82f, -1.20f, 0.82f, 0.20f, 0.80f, 1.00f, 0.74f, 1.60f, 0.62f, 1.95f, 0.45f, 2.08f);
                    d.glass = Flat.Pts(0.58f, -1.18f, 0.68f, -0.78f, 0.70f, 0.12f, 0.62f, 0.62f);
                    d.roofZ = -0.33f; d.roofHL = 0.45f; d.roofHW = 0.60f; d.gz0 = -1.18f; d.gz1 = 0.62f;
                    break;
            }
            return d;
        }

        static Color Shade(Color c, float f)
        {
            return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), 1f);
        }

        static readonly Color Ink = new Color(0.02f, 0.02f, 0.03f);

        public void Build(ICarState owner, CarLook carLook, bool blobShadow = true)
        {
            Clear();
            state = owner;
            look = carLook ?? new CarLook();
            Material opaque = MLMaterials.Opaque();
            Material glow = MLMaterials.Additive();
            Dims d = DimsFor(look.style);

            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            roof = new GameObject("Roof").transform;
            roof.SetParent(body, false);

            MeshBuilder mb = new MeshBuilder();

            // ---- body shell (fixed to the car) ----
            BuildBody(mb, d);
            AddMeshObject("BodyMesh", body, mb.ToMesh("CarBody"), opaque);

            // ---- greenhouse and roof (slides a little for a sense of height) ----
            mb.Clear();
            BuildRoof(mb, d);
            AddMeshObject("RoofMesh", roof, mb.ToMesh("CarRoof"), opaque);

            // ---- brake lights (only visible while braking) ----
            mb.Clear();
            for (int s = -1; s <= 1; s += 2)
                Flat.RoundRect(mb, 0.54f * s, d.zB + 0.14f, 0.24f, 0.075f, 0.03f, 0.58f, new Color(1f, 0.12f, 0.08f), 1f);
            brakeGlow = AddMeshObject("BrakeBar", body, mb.ToMesh("BrakeBar"), opaque);
            mb.Clear();
            for (int s = -1; s <= 1; s += 2)
                Flat.Glow(mb, 0.54f * s, d.zB - 0.15f, 1.15f, 0.62f, new Color(1f, 0.1f, 0.06f), 0.55f);
            GameObject brakeHalo = AddMeshObject("BrakeHalo", brakeGlow.transform, mb.ToMesh("BrakeHalo"), glow);
            brakeHalo.transform.localPosition = Vector3.zero;
            brakeGlow.SetActive(false);

            // ---- wheels ----
            mb.Clear();
            BuildWheel(mb);
            Mesh wheelMesh = mb.ToMesh("CarWheel");
            meshes.Add(wheelMesh);
            float frontZ = d.zF - 0.80f, rearZ = d.zB + 0.72f, x = d.w - 0.03f;
            MakeWheel("WheelFL", new Vector3(-x, 0f, frontZ), true, 0, wheelMesh, opaque, false);
            MakeWheel("WheelFR", new Vector3(x, 0f, frontZ), true, 1, wheelMesh, opaque, true);
            MakeWheel("WheelRL", new Vector3(-x, 0f, rearZ), false, 2, wheelMesh, opaque, false);
            MakeWheel("WheelRR", new Vector3(x, 0f, rearZ), false, 3, wheelMesh, opaque, true);

            // ---- headlight glow and underglow ----
            mb.Clear();
            if (!look.darkLights)
            {
                Color lc = look.lightColor;
                for (int s = -1; s <= 1; s += 2) Flat.Glow(mb, 0.55f * s, d.zF + 0.1f, 1.0f, 0.60f, lc, 0.34f);
            }
            if (look.underglow.maxColorComponent > 0.05f)
                Flat.Glow(mb, 0f, 0f, 2.7f, 0.12f, look.underglow, 0.75f, 18);
            if (mb.VertexCount > 0)
            {
                GameObject g = AddMeshObject("Glow", body, mb.ToMesh("CarGlow"), glow);
                g.transform.localScale = Vector3.one;
            }

            // ---- headlight origin for the night shader ----
            GameObject hl = new GameObject("HeadlightAnchor");
            hl.transform.SetParent(body, false);
            hl.transform.localPosition = new Vector3(0f, 0.8f, d.zF - 0.18f);
            hl.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);   // aimed slightly down at the road
            HeadlightAnchor = hl.transform;

            // ---- soft shadow, offset away from the light (does not rotate with the roof) ----
            if (blobShadow)
            {
                mb.Clear();
                mb.AddDisc(Vector3.zero, 1f, 16, Color.black, 0.55f, 0f);
                GameObject sh = new GameObject("BlobShadow");
                sh.transform.SetParent(transform, false);
                sh.transform.localPosition = new Vector3(0.2f, 0.1f, -0.1f);
                sh.transform.localScale = new Vector3(1.3f, 1f, 2.55f);
                Mesh sm = mb.ToMesh("BlobShadow");
                meshes.Add(sm);
                sh.AddComponent<MeshFilter>().sharedMesh = sm;
                MeshRenderer sr = sh.AddComponent<MeshRenderer>();
                sr.sharedMaterial = MLMaterials.Blend();
                sr.shadowCastingMode = ShadowCastingMode.Off;
                sr.receiveShadows = false;
            }
        }

        void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            for (int i = 0; i < meshes.Count; i++) if (meshes[i] != null) Destroy(meshes[i]);
            meshes.Clear();
            for (int i = 0; i < steerPivots.Length; i++) steerPivots[i] = null;
            body = null;
            roof = null;
            brakeGlow = null;
            HeadlightAnchor = null;
            roofShift = Vector2.zero;
        }

        void OnDestroy()
        {
            for (int i = 0; i < meshes.Count; i++) if (meshes[i] != null) Destroy(meshes[i]);
            meshes.Clear();
        }

        GameObject AddMeshObject(string name, Transform parent, Mesh mesh, Material mat)
        {
            meshes.Add(mesh);
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        // ------------------------------------------------------------------ body

        void BuildBody(MeshBuilder mb, Dims d)
        {
            Color paint = look.body;
            Color trim = look.trim;
            Vector2[] outline = Flat.Mirror(d.right);

            // dark outline behind everything gives the flat vector look
            Flat.Poly(mb, Flat.Grow(outline, 0.07f), 0.20f, Ink);

            // lower body
            Flat.Poly(mb, outline, 0.30f, paint);

            // hood and deck panels
            float hoodZ0 = d.gz1 - 0.02f, hoodZ1 = d.zF - 0.14f;
            Flat.RoundRect(mb, 0f, (hoodZ0 + hoodZ1) * 0.5f, 0.56f, (hoodZ1 - hoodZ0) * 0.5f, 0.16f, 0.34f, Shade(paint, 0.93f));
            float deckZ0 = d.zB + 0.18f, deckZ1 = d.gz0 + 0.02f;
            Flat.RoundRect(mb, 0f, (deckZ0 + deckZ1) * 0.5f, 0.56f, (deckZ1 - deckZ0) * 0.5f, 0.14f, 0.34f, Shade(paint, 0.93f));
            // light catching the shoulder line
            for (int s = -1; s <= 1; s += 2)
                Flat.Rect(mb, (d.w - 0.13f) * s, (d.zB + d.zF) * 0.5f, 0.02f, (d.zF - d.zB) * 0.36f, 0.36f, Shade(paint, 1.12f));

            // side skirts, bumpers, grille
            for (int s = -1; s <= 1; s += 2)
                Flat.Rect(mb, (d.w - 0.045f) * s, (d.gz0 + d.gz1) * 0.5f, 0.045f, (d.gz1 - d.gz0) * 0.5f + 0.1f, 0.36f, trim);
            Flat.RoundRect(mb, 0f, d.zF - 0.10f, 0.50f, 0.075f, 0.04f, 0.40f, trim);
            Flat.RoundRect(mb, 0f, d.zF - 0.30f, 0.20f, 0.05f, 0.02f, 0.38f, trim);
            Flat.RoundRect(mb, 0f, d.zB + 0.07f, 0.62f, 0.06f, 0.03f, 0.40f, trim);

            // headlights
            Color lens = look.darkLights ? new Color(0.25f, 0.28f, 0.35f) : look.lightColor;
            float lensEmit = look.darkLights ? 0.1f : 1f;
            for (int s = -1; s <= 1; s += 2)
            {
                Flat.RoundRect(mb, 0.52f * s, d.zF - 0.30f, 0.17f, 0.075f, 0.035f, 0.50f, lens, lensEmit, 14f * s);
            }

            // tail lights (dim, the brake bar adds the bright version)
            Color tail = look.darkLights ? new Color(0.35f, 0.04f, 0.04f) : new Color(0.75f, 0.04f, 0.04f);
            for (int s = -1; s <= 1; s += 2)
                Flat.RoundRect(mb, 0.54f * s, d.zB + 0.14f, 0.2f, 0.06f, 0.025f, 0.50f, tail, look.darkLights ? 0.25f : 0.55f);

            // mirrors
            float mz = d.gz1 - 0.12f;
            for (int s = -1; s <= 1; s += 2)
                Flat.RoundRect(mb, (d.w + 0.08f) * s, mz, 0.075f, 0.055f, 0.03f, 0.46f, trim, 0f, 20f * s);

            // decals on the hood and deck
            BuildBodyDecal(mb, d, hoodZ0, hoodZ1, deckZ0, deckZ1);

            // underglow strips along the sides
            if (look.underglow.maxColorComponent > 0.05f)
            {
                for (int s = -1; s <= 1; s += 2)
                    Flat.Rect(mb, (d.w + 0.01f) * s, 0f, 0.03f, (d.zF - d.zB) * 0.36f, 0.24f, look.underglow, 1f);
            }

            // spoiler
            BuildSpoiler(mb, d, trim);
        }

        void BuildSpoiler(MeshBuilder mb, Dims d, Color trim)
        {
            float zr = d.zB + 0.30f;
            switch (look.spoiler)
            {
                case 1: // none
                    break;
                case 2: // big wing, wider than the car
                    for (int s = -1; s <= 1; s += 2)
                        Flat.Rect(mb, 0.55f * s, zr + 0.02f, 0.035f, 0.2f, 0.62f, trim);
                    Flat.RoundRect(mb, 0f, zr, 0.95f, 0.20f, 0.04f, 0.72f, Shade(trim, 1.25f));
                    Flat.Rect(mb, 0f, zr + 0.08f, 0.9f, 0.04f, 0.74f, Shade(look.body, 0.9f));
                    for (int s = -1; s <= 1; s += 2)
                        Flat.Rect(mb, 0.95f * s, zr, 0.03f, 0.24f, 0.76f, trim);
                    break;
                case 3: // lip
                    Flat.RoundRect(mb, 0f, d.zB + 0.18f, 0.72f, 0.06f, 0.03f, 0.44f, trim);
                    break;
                default: // duck tail
                    Flat.RoundRect(mb, 0f, d.zB + 0.30f, 0.74f, 0.13f, 0.05f, 0.44f, trim);
                    Flat.Rect(mb, 0f, d.zB + 0.36f, 0.70f, 0.015f, 0.46f, Shade(look.body, 0.8f));
                    break;
            }
        }

        void BuildBodyDecal(MeshBuilder mb, Dims d, float hz0, float hz1, float dz0, float dz1)
        {
            Color c = look.decalColor;
            float hoodMid = (hz0 + hz1) * 0.5f, hoodHL = (hz1 - hz0) * 0.5f;
            float deckMid = (dz0 + dz1) * 0.5f, deckHL = (dz1 - dz0) * 0.5f;
            switch (look.decal)
            {
                case 1: // centre stripe
                    Flat.Rect(mb, 0f, hoodMid, 0.17f, hoodHL, 0.37f, c);
                    Flat.Rect(mb, 0f, deckMid, 0.17f, deckHL, 0.37f, c);
                    break;
                case 2: // side stripes
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Flat.Rect(mb, 0.60f * s, 0.1f, 0.055f, (d.zF - d.zB) * 0.40f, 0.37f, c);
                        Flat.Rect(mb, 0.49f * s, 0.1f, 0.02f, (d.zF - d.zB) * 0.40f, 0.37f, c);
                    }
                    break;
                case 3: // matte hood
                    Flat.RoundRect(mb, 0f, hoodMid, 0.50f, hoodHL - 0.04f, 0.14f, 0.37f, c);
                    break;
                case 5: // twin gold stripes
                    for (int s = -1; s <= 1; s += 2)
                    {
                        Flat.Rect(mb, 0.14f * s, hoodMid, 0.05f, hoodHL, 0.37f, c);
                        Flat.Rect(mb, 0.14f * s, deckMid, 0.05f, deckHL, 0.37f, c);
                    }
                    break;
                case 7: // lightning bolt on the hood
                    Flat.Rect(mb, 0.10f, hoodMid + 0.32f, 0.06f, 0.30f, 0.37f, c, 0f, -32f);
                    Flat.Rect(mb, 0f, hoodMid, 0.26f, 0.05f, 0.37f, c, 0f, 0f);
                    Flat.Rect(mb, -0.10f, hoodMid - 0.32f, 0.06f, 0.30f, 0.37f, c, 0f, -32f);
                    break;
            }
        }

        // ------------------------------------------------------------------ greenhouse and roof

        void BuildRoof(MeshBuilder mb, Dims d)
        {
            Color paint = look.body;
            Vector2[] glassOutline = Flat.Mirror(d.glass);
            Flat.Poly(mb, Flat.Grow(glassOutline, 0.04f), 0.47f, Ink);
            Flat.Poly(mb, glassOutline, 0.50f, look.glass);
            // soft glare across the windscreen
            Flat.Rect(mb, 0f, d.gz1 - 0.12f, 0.50f, 0.035f, 0.52f, Shade(look.glass, 2.2f));

            if (look.driverSilhouette)
            {
                // the Unknown Driver: no solid roof, just a dark figure with a pale mask behind the glass
                Flat.Ellipse(mb, 0f, d.roofZ - 0.05f, 0.42f, 0.24f, 0.54f, Ink, 0f, 14);
                Flat.Circle(mb, 0f, d.roofZ + 0.02f, 0.17f, 0.56f, new Color(0.03f, 0.03f, 0.04f), 0f, 12);
                Flat.Ellipse(mb, 0f, d.roofZ + 0.10f, 0.10f, 0.07f, 0.58f, new Color(0.92f, 0.92f, 0.94f), 0.55f, 10);
                return;
            }

            Flat.RoundRect(mb, 0f, d.roofZ, d.roofHW + 0.03f, d.roofHL + 0.03f, 0.2f, 0.58f, Shade(paint, 0.75f));
            Flat.RoundRect(mb, 0f, d.roofZ, d.roofHW, d.roofHL, 0.18f, 0.62f, paint);
            Flat.RoundRect(mb, 0f, d.roofZ + 0.04f, d.roofHW - 0.14f, d.roofHL - 0.16f, 0.12f, 0.64f, Shade(paint, 1.06f));
            BuildRoofDecal(mb, d);
        }

        void BuildRoofDecal(MeshBuilder mb, Dims d)
        {
            Color c = look.decalColor;
            switch (look.decal)
            {
                case 1: // centre stripe
                    Flat.Rect(mb, 0f, d.roofZ, 0.17f, d.roofHL - 0.02f, 0.66f, c);
                    break;
                case 4: // number roundel
                    {
                        Color dark = new Color(0.05f, 0.05f, 0.06f);
                        Flat.Circle(mb, 0f, d.roofZ, 0.30f, 0.66f, c, 0f, 16);
                        Flat.Circle(mb, 0f, d.roofZ + 0.11f, 0.095f, 0.68f, dark, 0f, 10);
                        Flat.Circle(mb, 0f, d.roofZ - 0.11f, 0.095f, 0.68f, dark, 0f, 10);
                        Flat.Circle(mb, 0f, d.roofZ + 0.11f, 0.045f, 0.70f, c, 0f, 8);
                        Flat.Circle(mb, 0f, d.roofZ - 0.11f, 0.045f, 0.70f, c, 0f, 8);
                    }
                    break;
                case 5: // twin gold stripes
                    for (int s = -1; s <= 1; s += 2) Flat.Rect(mb, 0.14f * s, d.roofZ, 0.05f, d.roofHL - 0.02f, 0.66f, c);
                    break;
                case 6: // checker roof
                    {
                        int cols = 4, rows = 6;
                        float cw = (d.roofHW - 0.05f) * 2f / cols, cl = (d.roofHL - 0.05f) * 2f / rows;
                        for (int r = 0; r < rows; r++)
                            for (int k = 0; k < cols; k++)
                            {
                                if (((r + k) & 1) != 0) continue;
                                Flat.Rect(mb, -(d.roofHW - 0.05f) + cw * (k + 0.5f), d.roofZ - (d.roofHL - 0.05f) + cl * (r + 0.5f), cw * 0.5f, cl * 0.5f, 0.66f, c);
                            }
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ wheels

        void BuildWheel(MeshBuilder mb)
        {
            Color tyre = new Color(0.045f, 0.045f, 0.055f);
            Flat.RoundRect(mb, 0f, 0f, 0.14f, 0.36f, 0.05f, 0.25f, tyre);
            // the rim shows as a light line on the outer face, in the colour picked in the garage
            Flat.Rect(mb, 0.085f, 0f, 0.022f, 0.22f, 0.27f, look.rim);
            Flat.Rect(mb, -0.085f, 0f, 0.022f, 0.22f, 0.27f, look.rim);
        }

        void MakeWheel(string name, Vector3 pos, bool front, int index, Mesh mesh, Material mat, bool mirror)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(body, false);
            pivot.transform.localPosition = pos;
            GameObject spin = new GameObject("Wheel");
            spin.transform.SetParent(pivot.transform, false);
            spin.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = spin.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            if (front) steerPivots[index] = pivot.transform;
        }

        // ------------------------------------------------------------------ animation

        void LateUpdate()
        {
            if (state == null || body == null) return;
            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-10f * dt);

            // the roof slides outward in a corner and forward under braking, like a body leaning on its springs
            Vector2 target = new Vector2(
                Mathf.Clamp(-state.LateralAccel / 9.81f * roofShiftPerG, -0.09f, 0.09f),
                Mathf.Clamp(-state.LongAccel / 9.81f * roofShiftPerG * 0.7f, -0.07f, 0.07f));
            roofShift = Vector2.Lerp(roofShift, target, k);
            if (roof != null) roof.localPosition = new Vector3(roofShift.x, 0f, roofShift.y);

            float steerDeg = state.SteerAngle * Mathf.Rad2Deg;
            for (int i = 0; i < 2; i++)
                if (steerPivots[i] != null) steerPivots[i].localRotation = Quaternion.Euler(0f, steerDeg, 0f);

            if (brakeGlow != null)
            {
                bool on = state.BrakeAmount > 0.15f;
                if (brakeGlow.activeSelf != on) brakeGlow.SetActive(on);
            }
        }
    }
}
