using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>One sample of the road centre line.</summary>
    public struct RoadPoint
    {
        public double x, z;     // world position (before origin offset)
        public float theta;     // heading in radians (0 = +Z, positive turns right)
        public float kappa;     // curvature in 1/metres, positive = turning right
        public float y;         // height
        public float slope;     // dy/ds
    }

    /// <summary>
    /// The road as a mathematical line: distance along it (s) and sideways offset (d).
    /// The car, traffic and rivals all live in (s, d) space, so the world can be rebased
    /// any time without touching gameplay. Positions are stored as doubles; the world
    /// origin is shifted by Rebase() so floats stay precise on endless runs.
    /// </summary>
    public sealed class RoadModel
    {
        public readonly TrackRecipe Recipe;
        public double OriginX { get; private set; }
        public double OriginZ { get; private set; }

        readonly List<double> xs = new List<double>(4096);
        readonly List<double> zs = new List<double>(4096);
        readonly List<float> thetas = new List<float>(4096);
        readonly List<float> kappas = new List<float>(4096);

        System.Random rng;
        // current segment
        int segStart;
        int segLen;
        float segKappa;      // peak curvature, 0 for straights
        bool lastWasCurve;
        float lastSign = 1f;

        const float Ramp = 0.3f;

        public RoadModel(TrackRecipe recipe)
        {
            Recipe = recipe;
            Reset();
        }

        public void Reset()
        {
            xs.Clear(); zs.Clear(); thetas.Clear(); kappas.Clear();
            rng = new System.Random(Recipe.seed);
            xs.Add(0.0); zs.Add(0.0); thetas.Add(0f); kappas.Add(0f);
            segStart = 0;
            segLen = Mathf.Max(10, Mathf.RoundToInt(Recipe.firstStraight));
            segKappa = 0f;
            lastWasCurve = false;
            lastSign = 1f;
        }

        float Rand(float a, float b)
        {
            return a + (float)rng.NextDouble() * (b - a);
        }

        void NextSegment(int startIndex)
        {
            segStart = startIndex;
            bool makeCurve;
            if (!lastWasCurve) makeCurve = true;
            else makeCurve = (float)rng.NextDouble() < Recipe.sBendChance;   // chance of going straight into another bend

            if (lastWasCurve && !makeCurve)
            {
                segKappa = 0f;
                segLen = Mathf.RoundToInt(Rand(Recipe.straightLength.x, Recipe.straightLength.y));
                lastWasCurve = false;
                return;
            }

            // Difficulty grows with distance: gentle corners first.
            float diff = Mathf.Clamp01(startIndex / Mathf.Max(1f, Recipe.difficultyRampDistance));
            float minR = Mathf.Lerp(Mathf.Max(Recipe.minRadius, 90f), Recipe.minRadius, diff);
            float maxR = Mathf.Max(minR + 10f, Recipe.maxRadius);
            // Bias toward medium radii, with the occasional tight one.
            float t = (float)rng.NextDouble();
            t = t * t;
            float radius = Mathf.Lerp(maxR, minR, t);

            float sign;
            if (lastWasCurve) sign = -lastSign;                           // S bend
            else sign = (rng.NextDouble() < 0.5) ? 1f : -1f;
            lastSign = sign;
            lastWasCurve = true;
            segKappa = sign / radius;
            segLen = Mathf.RoundToInt(Rand(Recipe.curveLength.x, Recipe.curveLength.y));
        }

        float KappaAtIndex(int i)
        {
            while (i >= segStart + segLen) NextSegment(segStart + segLen);
            if (segKappa == 0f) return 0f;
            float u = (i - segStart) / (float)segLen;
            float env = Mathf.SmoothStep(0f, 1f, u / Ramp) * Mathf.SmoothStep(0f, 1f, (1f - u) / Ramp);
            return segKappa * env;
        }

        void Extend()
        {
            int i = xs.Count - 1;
            float k = KappaAtIndex(i);
            kappas[i] = k;
            float th = thetas[i];
            float mid = th + k * 0.5f;
            xs.Add(xs[i] + Math.Sin(mid));
            zs.Add(zs[i] + Math.Cos(mid));
            thetas.Add(th + k);
            kappas.Add(k);
        }

        void Ensure(float s)
        {
            int need = (int)s + 4;
            while (xs.Count <= need) Extend();
        }

        float HeightAt(float s)
        {
            float amp = Recipe.hillAmplitude * Mathf.Clamp01(s / 150f);
            return amp * (0.6f * Mathf.Sin(s * 0.012f) + 0.3f * Mathf.Sin(s * 0.031f + 1.7f) + 0.1f * Mathf.Sin(s * 0.07f + 0.4f));
        }

        float SlopeAt(float s)
        {
            float amp = Recipe.hillAmplitude * Mathf.Clamp01(s / 150f);
            return amp * (0.6f * 0.012f * Mathf.Cos(s * 0.012f) + 0.3f * 0.031f * Mathf.Cos(s * 0.031f + 1.7f) + 0.1f * 0.07f * Mathf.Cos(s * 0.07f + 0.4f));
        }

        public RoadPoint At(float s)
        {
            if (s < 0f) s = 0f;
            Ensure(s);
            int i = (int)s;
            float t = s - i;
            RoadPoint p;
            p.x = xs[i] + (xs[i + 1] - xs[i]) * t;
            p.z = zs[i] + (zs[i + 1] - zs[i]) * t;
            p.theta = Mathf.Lerp(thetas[i], thetas[i + 1], t);
            p.kappa = Mathf.Lerp(kappas[i], kappas[i + 1], t);
            p.y = HeightAt(s);
            p.slope = SlopeAt(s);
            return p;
        }

        /// <summary>World position (relative to the current origin) of a point on the road at sideways offset d.</summary>
        public Vector3 WorldPos(RoadPoint p, float d)
        {
            double x = p.x + d * Math.Cos(p.theta);
            double z = p.z - d * Math.Sin(p.theta);
            return new Vector3((float)(x - OriginX), p.y, (float)(z - OriginZ));
        }

        public Vector3 WorldPos(float s, float d)
        {
            return WorldPos(At(s), d);
        }

        /// <summary>Shift the world origin by the given amount (horizontal only) and tell everything.</summary>
        public void Rebase(Vector3 shift)
        {
            OriginX += shift.x;
            OriginZ += shift.z;
            EventBus.Raise(new WorldRebased { shift = new Vector3(shift.x, 0f, shift.z) });
        }
    }
}
