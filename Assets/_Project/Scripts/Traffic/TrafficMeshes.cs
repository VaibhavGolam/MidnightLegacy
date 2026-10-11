using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    public enum TrafficKind { Hatch, Sedan, Taxi, Van, Bus, Truck, Moto }

    /// <summary>Size, weight and typical speed of each traffic vehicle.</summary>
    public struct TrafficSpec
    {
        public float halfLen, halfWid, mass, baseSpeed;
    }

    /// <summary>
    /// Procedural top-down traffic (hatchback, sedan, taxi, delivery van, bus, box truck, motorbike) drawn as flat
    /// layered shapes. Meshes are built once per kind and colour variant and shared by every pooled vehicle.
    /// Local axes: x right, z forward, y is the layer height, origin in the middle of the vehicle.
    /// </summary>
    public static class TrafficMeshes
    {
        public const int Variants = 4;

        static readonly Dictionary<int, Mesh> cache = new Dictionary<int, Mesh>();

        static readonly Color Glass = new Color(0.07f, 0.09f, 0.13f);
        static readonly Color Black = new Color(0.05f, 0.05f, 0.06f);
        static readonly Color Ink = new Color(0.02f, 0.02f, 0.03f);
        static readonly Color TailRed = new Color(0.85f, 0.05f, 0.05f);
        static readonly Color HeadLens = new Color(1f, 0.92f, 0.65f);

        static readonly Color[] CarColors =
        {
            new Color(0.55f, 0.10f, 0.10f), new Color(0.12f, 0.22f, 0.42f),
            new Color(0.55f, 0.56f, 0.58f), new Color(0.10f, 0.11f, 0.13f),
            new Color(0.80f, 0.78f, 0.70f), new Color(0.15f, 0.35f, 0.25f)
        };

        public static TrafficSpec Spec(TrafficKind k)
        {
            switch (k)
            {
                case TrafficKind.Hatch: return new TrafficSpec { halfLen = 1.95f, halfWid = 0.86f, mass = 1100f, baseSpeed = 17f };
                case TrafficKind.Sedan: return new TrafficSpec { halfLen = 2.25f, halfWid = 0.92f, mass = 1400f, baseSpeed = 19f };
                case TrafficKind.Taxi: return new TrafficSpec { halfLen = 2.25f, halfWid = 0.92f, mass = 1400f, baseSpeed = 15f };
                case TrafficKind.Van: return new TrafficSpec { halfLen = 2.6f, halfWid = 0.98f, mass = 2000f, baseSpeed = 16f };
                case TrafficKind.Bus: return new TrafficSpec { halfLen = 5.3f, halfWid = 1.28f, mass = 11000f, baseSpeed = 12f };
                case TrafficKind.Truck: return new TrafficSpec { halfLen = 4.2f, halfWid = 1.22f, mass = 8500f, baseSpeed = 13.5f };
                default: return new TrafficSpec { halfLen = 1.05f, halfWid = 0.36f, mass = 250f, baseSpeed = 22f };
            }
        }

        public static Mesh Get(TrafficKind kind, int variant)
        {
            int key = (int)kind * 16 + (variant % Variants);
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            MeshBuilder mb = new MeshBuilder();
            Build(mb, kind, variant % Variants);
            m = mb.ToMesh("Traffic_" + kind + "_" + variant);
            cache[key] = m;
            return m;
        }

        static Color Shade(Color c, float f)
        {
            return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), 1f);
        }

        static void Wheels(MeshBuilder mb, float x, float halfLen, float inset, params float[] zs)
        {
            for (int i = 0; i < zs.Length; i++)
                for (int s = -1; s <= 1; s += 2)
                    Flat.RoundRect(mb, (x + 0.0f) * s, zs[i], 0.13f, Mathf.Max(0.3f, halfLen * 0.17f), 0.05f, 0.25f, Black);
        }

        static void Lights(MeshBuilder mb, float hw, float zBack, float zFront, float size)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                Flat.RoundRect(mb, (hw - size * 0.95f) * s, zBack, size * 0.5f, size * 0.17f, size * 0.07f, 0.52f, TailRed, 0.85f);
                Flat.RoundRect(mb, (hw - size * 0.95f) * s, zFront, size * 0.45f, size * 0.2f, size * 0.07f, 0.52f, HeadLens, 0.9f, 10f * s);
            }
        }

        /// <summary>Generic passenger car seen from above: outline, body, hood and deck, glass, roof and lights.</summary>
        static void PassengerCar(MeshBuilder mb, Color body, float halfLen, float hw, float cz0, float cz1)
        {
            Vector2[] right = Flat.Pts(
                hw * 0.74f, -halfLen, hw * 0.90f, -halfLen + 0.10f, hw, -halfLen + 0.40f,
                hw, halfLen - 1.0f, hw * 0.93f, halfLen - 0.40f, hw * 0.74f, halfLen - 0.10f, hw * 0.52f, halfLen);
            Vector2[] outline = Flat.Mirror(right);
            Flat.Poly(mb, Flat.Grow(outline, 0.07f), 0.20f, Ink);
            Flat.Poly(mb, outline, 0.30f, body);
            Flat.RoundRect(mb, 0f, (cz1 + halfLen - 0.12f) * 0.5f, hw * 0.62f, (halfLen - 0.12f - cz1) * 0.5f, 0.14f, 0.34f, Shade(body, 0.93f));
            Flat.RoundRect(mb, 0f, (cz0 - halfLen + 0.16f) * 0.5f, hw * 0.62f, (cz0 - (-halfLen + 0.16f)) * 0.5f, 0.12f, 0.34f, Shade(body, 0.93f));

            Vector2[] glass = Flat.Mirror(Flat.Pts(hw * 0.78f, cz0, hw * 0.88f, cz0 + 0.35f, hw * 0.88f, cz1 - 0.4f, hw * 0.74f, cz1));
            Flat.Poly(mb, glass, 0.50f, Glass);
            float rl = (cz1 - cz0) * 0.5f - 0.22f;
            Flat.RoundRect(mb, 0f, (cz0 + cz1) * 0.5f, hw * 0.70f, Mathf.Max(0.2f, rl), 0.16f, 0.62f, body);
            Flat.RoundRect(mb, 0f, (cz0 + cz1) * 0.5f, hw * 0.50f, Mathf.Max(0.15f, rl - 0.14f), 0.1f, 0.64f, Shade(body, 1.07f));

            Flat.RoundRect(mb, 0f, halfLen - 0.10f, hw * 0.55f, 0.07f, 0.03f, 0.40f, Black);
            Flat.RoundRect(mb, 0f, -halfLen + 0.07f, hw * 0.65f, 0.06f, 0.03f, 0.40f, Black);
            Wheels(mb, hw - 0.03f, halfLen, 0f, halfLen - 0.85f, -halfLen + 0.8f);
            Lights(mb, hw, -halfLen + 0.13f, halfLen - 0.22f, 0.42f);
        }

        static void Build(MeshBuilder mb, TrafficKind kind, int variant)
        {
            Color pick = CarColors[(variant * 2 + (int)kind) % CarColors.Length];
            switch (kind)
            {
                case TrafficKind.Hatch:
                    PassengerCar(mb, pick, 1.95f, 0.85f, -1.45f, 0.50f);
                    break;

                case TrafficKind.Sedan:
                    PassengerCar(mb, pick, 2.25f, 0.90f, -1.00f, 0.70f);
                    break;

                case TrafficKind.Taxi:
                    {
                        Color yellow = new Color(0.95f, 0.72f, 0.10f);
                        PassengerCar(mb, yellow, 2.25f, 0.90f, -1.00f, 0.70f);
                        Flat.RoundRect(mb, 0f, -0.15f, 0.26f, 0.12f, 0.04f, 0.70f, new Color(1f, 0.95f, 0.7f), 1f);
                        for (int s = -1; s <= 1; s += 2)
                            for (int k = 0; k < 7; k++)
                                if ((k & 1) == 0) Flat.Rect(mb, 0.79f * s, -1.3f + k * 0.32f, 0.045f, 0.16f, 0.37f, Black);
                    }
                    break;

                case TrafficKind.Van:
                    {
                        Color body = (variant & 1) == 0 ? new Color(0.88f, 0.88f, 0.86f) : new Color(0.22f, 0.32f, 0.55f);
                        float hw = 0.95f, L = 2.6f;
                        Flat.RoundRect(mb, 0f, 0f, hw + 0.07f, L + 0.07f, 0.34f, 0.20f, Ink);
                        Flat.RoundRect(mb, 0f, 0f, hw, L, 0.30f, 0.30f, body);
                        Flat.Poly(mb, Flat.Mirror(Flat.Pts(hw * 0.84f, 1.35f, hw * 0.9f, 1.65f, hw * 0.74f, 2.28f)), 0.50f, Glass);
                        Flat.RoundRect(mb, 0f, -0.55f, hw - 0.12f, 1.78f, 0.2f, 0.60f, Shade(body, 1.05f));
                        Flat.Rect(mb, 0f, -0.55f, 0.14f, 1.7f, 0.64f, new Color(0.9f, 0.45f, 0.1f));
                        Flat.Rect(mb, 0f, 0.75f, hw - 0.14f, 0.025f, 0.64f, Shade(body, 0.7f));
                        Wheels(mb, 0.92f, L, 0f, 1.7f, -1.7f);
                        Lights(mb, hw, -L + 0.1f, L - 0.14f, 0.40f);
                    }
                    break;

                case TrafficKind.Bus:
                    {
                        Color body = (variant & 1) == 0 ? new Color(0.78f, 0.20f, 0.14f) : new Color(0.15f, 0.40f, 0.62f);
                        float hw = 1.25f, L = 5.3f;
                        Flat.RoundRect(mb, 0f, 0f, hw + 0.08f, L + 0.08f, 0.45f, 0.20f, Ink);
                        Flat.RoundRect(mb, 0f, 0f, hw, L, 0.4f, 0.30f, body);
                        Flat.RoundRect(mb, 0f, -0.2f, hw - 0.12f, L - 0.55f, 0.3f, 0.60f, Shade(body, 1.08f));
                        Flat.Rect(mb, 0f, 4.7f, hw - 0.2f, 0.28f, 0.52f, Glass);
                        for (int i = 0; i < 3; i++)
                            Flat.RoundRect(mb, 0f, -3.2f + i * 2.4f, 0.55f, 0.35f, 0.08f, 0.64f, new Color(0.72f, 0.74f, 0.78f));
                        for (int s = -1; s <= 1; s += 2)
                            for (int k = 0; k < 9; k++)
                                Flat.Rect(mb, (hw - 0.04f) * s, -4.2f + k * 1.0f, 0.04f, 0.38f, 0.40f, Glass);
                        Flat.Rect(mb, 0f, 4.35f, 0.4f, 0.07f, 0.66f, new Color(1f, 0.85f, 0.4f), 0.9f);
                        Wheels(mb, 1.2f, 3.4f, 0f, 3.6f, -3.4f);
                        Lights(mb, hw, -L + 0.12f, L - 0.14f, 0.45f);
                    }
                    break;

                case TrafficKind.Truck:
                    {
                        Color cab = (variant & 1) == 0 ? new Color(0.20f, 0.35f, 0.62f) : new Color(0.70f, 0.55f, 0.12f);
                        Color cargo = new Color(0.82f, 0.82f, 0.80f);
                        float hw = 1.2f;
                        Flat.Rect(mb, 0f, 0.1f, 0.5f, 4.0f, 0.22f, Black);
                        Flat.RoundRect(mb, 0f, -0.9f, hw + 0.07f, 3.37f, 0.22f, 0.20f, Ink);
                        Flat.RoundRect(mb, 0f, -0.9f, hw, 3.3f, 0.18f, 0.32f, cargo);
                        for (int k = 0; k < 6; k++)
                            Flat.Rect(mb, 0f, -3.5f + k * 1.2f, hw - 0.1f, 0.025f, 0.36f, Shade(cargo, 0.82f));
                        Flat.Rect(mb, 0f, -4.1f, hw - 0.08f, 0.12f, 0.40f, cab);
                        Flat.RoundRect(mb, 0f, 3.35f, hw * 0.9f + 0.07f, 0.9f, 0.3f, 0.20f, Ink);
                        Flat.RoundRect(mb, 0f, 3.35f, hw * 0.9f, 0.85f, 0.28f, 0.34f, cab);
                        Flat.Poly(mb, Flat.Mirror(Flat.Pts(hw * 0.78f, 3.55f, hw * 0.82f, 3.8f, hw * 0.66f, 4.1f)), 0.52f, Glass);
                        Flat.RoundRect(mb, 0f, 3.0f, hw * 0.72f, 0.28f, 0.1f, 0.62f, Shade(cab, 1.12f));
                        Wheels(mb, 1.12f, 3.0f, 0f, 3.4f, -2.6f, -3.8f);
                        Lights(mb, hw, -4.18f, 4.12f, 0.40f);
                    }
                    break;

                default: // motorbike with a dark rider
                    {
                        Color bike = pick;
                        Flat.Ellipse(mb, 0f, 0.05f, 0.2f, 1.05f, 0.20f, Ink, 0f, 12);
                        Flat.RoundRect(mb, 0f, 0f, 0.1f, 0.98f, 0.06f, 0.26f, Black);
                        Flat.Ellipse(mb, 0f, 0.35f, 0.16f, 0.36f, 0.34f, bike, 0f, 12);
                        Flat.Ellipse(mb, 0f, -0.15f, 0.2f, 0.34f, 0.46f, new Color(0.08f, 0.08f, 0.1f), 0f, 12);
                        Flat.Rect(mb, 0f, 0.72f, 0.36f, 0.035f, 0.44f, Black);
                        Flat.Circle(mb, 0f, -0.05f, 0.14f, 0.58f, new Color(0.75f, 0.2f, 0.12f), 0f, 10);
                        Flat.Rect(mb, 0f, -1.0f, 0.1f, 0.04f, 0.50f, TailRed, 0.9f);
                        Flat.Rect(mb, 0f, 1.0f, 0.08f, 0.04f, 0.50f, HeadLens, 1f);
                    }
                    break;
            }
        }
    }
}
