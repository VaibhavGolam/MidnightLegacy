using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Builds the placeholder car (original boxy 80s coupe, white body, black trim) from primitives and
    /// animates body roll, pitch, steering and wheel spin. Replace the generated meshes with a real
    /// model later: only this script touches the visuals, the physics never does.
    /// Local axes: x right, y up, z forward, origin on the ground under the middle of the car.
    /// </summary>
    public sealed class CarVisuals : MonoBehaviour
    {
        public float rollDegreesPerG = 3.2f;
        public float pitchDegreesPerG = 3.5f;

        public Transform HeadlightAnchor { get; private set; }

        CarController car;
        Transform body;
        Transform[] steerPivots = new Transform[2];   // front left, front right
        Transform[] spinNodes = new Transform[4];     // FL, FR, RL, RR
        float bodyRoll, bodyPitch;

        static readonly Color White = new Color(0.88f, 0.88f, 0.86f);
        static readonly Color WhiteWorn = new Color(0.80f, 0.80f, 0.78f);
        static readonly Color Black = new Color(0.055f, 0.055f, 0.065f);
        static readonly Color Glass = new Color(0.07f, 0.09f, 0.13f);
        static readonly Color Grey = new Color(0.55f, 0.56f, 0.60f);

        public void Build(CarController owner)
        {
            car = owner;
            Material opaque = MLMaterials.Opaque();

            body = new GameObject("Body").transform;
            body.SetParent(transform, false);

            // ---- body shell ----
            MeshBuilder mb = new MeshBuilder();
            BuildBody(mb);
            GameObject shell = new GameObject("BodyMesh");
            shell.transform.SetParent(body, false);
            shell.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("KairoBody");
            MeshRenderer mr = shell.AddComponent<MeshRenderer>();
            mr.sharedMaterial = opaque;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // ---- wheels ----
            mb.Clear();
            BuildWheel(mb);
            Mesh wheelMesh = mb.ToMesh("KairoWheel");
            float frontZ = 1.2f, rearZ = -1.25f, x = 0.76f, y = CarController.WheelRadius;
            MakeWheel("WheelFL", new Vector3(-x, y, frontZ), true, 0, wheelMesh, opaque);
            MakeWheel("WheelFR", new Vector3(x, y, frontZ), true, 1, wheelMesh, opaque);
            MakeWheel("WheelRL", new Vector3(-x, y, rearZ), false, 2, wheelMesh, opaque);
            MakeWheel("WheelRR", new Vector3(x, y, rearZ), false, 3, wheelMesh, opaque);

            // ---- headlight origin for the night shader ----
            GameObject hl = new GameObject("HeadlightAnchor");
            hl.transform.SetParent(body, false);
            hl.transform.localPosition = new Vector3(0f, 0.8f, 1.9f);
            hl.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);   // aimed slightly down at the road
            HeadlightAnchor = hl.transform;

            // ---- blob shadow (does not tilt with the body) ----
            mb.Clear();
            mb.AddDisc(Vector3.zero, 1f, 14, Color.black, 0.65f, 0f);
            GameObject sh = new GameObject("BlobShadow");
            sh.transform.SetParent(transform, false);
            sh.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            sh.transform.localScale = new Vector3(1.25f, 1f, 2.5f);
            sh.AddComponent<MeshFilter>().sharedMesh = mb.ToMesh("BlobShadow");
            MeshRenderer sr = sh.AddComponent<MeshRenderer>();
            sr.sharedMaterial = MLMaterials.Blend();
            sr.shadowCastingMode = ShadowCastingMode.Off;
            sr.receiveShadows = false;
        }

        void BuildBody(MeshBuilder mb)
        {
            // undercarriage, hides gaps between wheels and body
            mb.AddBox(new Vector3(0f, 0.24f, 0f), new Vector3(1.5f, 0.12f, 3.9f), Black);

            // lower body (z back .. front), slightly tapered nose
            Vector3[] lower =
            {
                new Vector3(-0.80f, 0.22f, -2.05f), new Vector3(0.80f, 0.22f, -2.05f),
                new Vector3(0.80f, 0.22f, 2.08f),   new Vector3(-0.80f, 0.22f, 2.08f),
                new Vector3(-0.82f, 0.78f, -2.00f), new Vector3(0.82f, 0.78f, -2.00f),
                new Vector3(0.80f, 0.74f, 1.95f),   new Vector3(-0.80f, 0.74f, 1.95f)
            };
            mb.AddHexa(lower, White, White, White, WhiteWorn);

            // cabin: glass sides, white roof, raked windscreen and hatch
            Vector3[] cabin =
            {
                new Vector3(-0.78f, 0.78f, -1.15f), new Vector3(0.78f, 0.78f, -1.15f),
                new Vector3(0.78f, 0.78f, 0.62f),   new Vector3(-0.78f, 0.78f, 0.62f),
                new Vector3(-0.66f, 1.30f, -0.85f), new Vector3(0.66f, 1.30f, -0.85f),
                new Vector3(0.66f, 1.30f, 0.08f),   new Vector3(-0.66f, 1.30f, 0.08f)
            };
            mb.AddHexa(cabin, Glass, White, Glass, Glass);

            // white C pillars so the glass reads as windows
            mb.AddBoxRot(new Vector3(-0.73f, 1.03f, -1.00f), new Vector3(0.08f, 0.56f, 0.34f), Quaternion.Euler(-14f, 0f, 0f), White);
            mb.AddBoxRot(new Vector3(0.73f, 1.03f, -1.00f), new Vector3(0.08f, 0.56f, 0.34f), Quaternion.Euler(-14f, 0f, 0f), White);

            // bumpers
            mb.AddBox(new Vector3(0f, 0.38f, 2.07f), new Vector3(1.66f, 0.26f, 0.16f), Black);
            mb.AddBox(new Vector3(0f, 0.38f, -2.06f), new Vector3(1.66f, 0.26f, 0.16f), Black);
            // side skirts
            mb.AddBox(new Vector3(-0.82f, 0.30f, 0f), new Vector3(0.05f, 0.12f, 2.2f), Black);
            mb.AddBox(new Vector3(0.82f, 0.30f, 0f), new Vector3(0.05f, 0.12f, 2.2f), Black);
            // grille
            mb.AddBox(new Vector3(0f, 0.62f, 2.0f), new Vector3(0.7f, 0.1f, 0.06f), Black);

            // pop up headlights (raised), lenses glow
            for (int s = -1; s <= 1; s += 2)
            {
                mb.AddBox(new Vector3(0.52f * s, 0.85f, 1.78f), new Vector3(0.38f, 0.16f, 0.26f), White);
                mb.AddBox(new Vector3(0.52f * s, 0.85f, 1.925f), new Vector3(0.32f, 0.12f, 0.03f), new Color(1f, 0.92f, 0.65f), 1f);
            }

            // tail lights
            for (int s = -1; s <= 1; s += 2)
                mb.AddBox(new Vector3(0.55f * s, 0.64f, -2.09f), new Vector3(0.5f, 0.14f, 0.04f), new Color(0.9f, 0.04f, 0.04f), 0.9f);

            // rear spoiler
            mb.AddBox(new Vector3(0f, 1.02f, -1.92f), new Vector3(1.5f, 0.04f, 0.34f), Black);
            mb.AddBox(new Vector3(-0.55f, 0.9f, -1.9f), new Vector3(0.05f, 0.2f, 0.05f), Black);
            mb.AddBox(new Vector3(0.55f, 0.9f, -1.9f), new Vector3(0.05f, 0.2f, 0.05f), Black);

            // mirrors
            mb.AddBox(new Vector3(-0.9f, 0.98f, 0.42f), new Vector3(0.12f, 0.1f, 0.18f), Black);
            mb.AddBox(new Vector3(0.9f, 0.98f, 0.42f), new Vector3(0.12f, 0.1f, 0.18f), Black);
        }

        void BuildWheel(MeshBuilder mb)
        {
            float w = 0.21f;
            float r = CarController.WheelRadius;
            Vector3 a = new Vector3(-w * 0.5f, 0f, 0f), b = new Vector3(w * 0.5f, 0f, 0f);
            mb.AddCylinder(a, b, r, r, 10, Black, Black, Black);
            Vector3 ra = new Vector3(-w * 0.5f - 0.005f, 0f, 0f), rb = new Vector3(w * 0.5f + 0.005f, 0f, 0f);
            mb.AddCylinder(ra, rb, r * 0.64f, r * 0.64f, 10, Grey, Grey, Grey);
            // a bar across the rim so spinning is visible
            mb.AddBox(Vector3.zero, new Vector3(w + 0.02f, r * 1.1f, 0.06f), new Color(0.25f, 0.26f, 0.3f));
        }

        void MakeWheel(string name, Vector3 pos, bool front, int index, Mesh mesh, Material mat)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(body, false);
            pivot.transform.localPosition = pos;
            GameObject spin = new GameObject("Spin");
            spin.transform.SetParent(pivot.transform, false);
            spin.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = spin.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            spinNodes[index] = spin.transform;
            if (front) steerPivots[index] = pivot.transform;
        }

        void LateUpdate()
        {
            if (car == null || body == null) return;
            float dt = Time.deltaTime;

            // body roll leans outward in a corner, pitch dives under braking
            float targetRoll = Mathf.Clamp(car.LateralAccel / 9.81f * rollDegreesPerG, -6f, 6f);
            float targetPitch = Mathf.Clamp(-car.LongAccel / 9.81f * pitchDegreesPerG, -4f, 4f);
            float k = 1f - Mathf.Exp(-12f * dt);
            bodyRoll = Mathf.Lerp(bodyRoll, targetRoll, k);
            bodyPitch = Mathf.Lerp(bodyPitch, targetPitch, k);
            body.localRotation = Quaternion.Euler(bodyPitch, 0f, bodyRoll);

            float steerDeg = car.SteerAngle * Mathf.Rad2Deg;
            for (int i = 0; i < 2; i++)
                if (steerPivots[i] != null) steerPivots[i].localRotation = Quaternion.Euler(0f, steerDeg, 0f);

            float spinDeg = car.WheelSpinRad * Mathf.Rad2Deg;
            for (int i = 0; i < 4; i++)
                if (spinNodes[i] != null) spinNodes[i].localRotation = Quaternion.Euler(spinDeg, 0f, 0f);
        }
    }
}
