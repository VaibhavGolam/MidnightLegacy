using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Slide feedback seen from above: dark tyre marks painted on the road behind both rear wheels, and a puff
    /// of smoke. Gives instant visual feedback when tuning the handling.
    /// </summary>
    public sealed class SlideEffects : MonoBehaviour
    {
        const float MarkOn = 0.35f;
        const float MarkMinSpeed = 8f;

        CarController car;
        ParticleSystem smoke;
        TrailRenderer[] marks = new TrailRenderer[2];
        public bool Enabled = true;

        public void Build(CarController owner)
        {
            car = owner;
            BuildSmoke();
            BuildMarks();
        }

        void BuildSmoke()
        {
            GameObject go = new GameObject("SlideSmoke");
            go.transform.SetParent(car.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.9f, -1.5f);

            smoke = go.AddComponent<ParticleSystem>();
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = smoke.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.9f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startColor = new Color(0.78f, 0.78f, 0.85f, 0.42f);
            main.maxParticles = 90;

            var emission = smoke.emission;
            emission.rateOverTime = 0f;

            var shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.5f, 0.05f, 0.3f);

            var sizeOverLife = smoke.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.8f));

            var colorOverLife = smoke.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = grad;

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MLMaterials.Blend();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            smoke.Play();
        }

        void BuildMarks()
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.02f, 0.02f, 0.03f), 0f), new GradientColorKey(new Color(0.03f, 0.03f, 0.04f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.75f, 0.06f), new GradientAlphaKey(0.55f, 0.6f), new GradientAlphaKey(0f, 1f) });

            for (int i = 0; i < 2; i++)
            {
                GameObject go = new GameObject(i == 0 ? "TyreMarkL" : "TyreMarkR");
                go.transform.SetParent(car.transform, false);
                go.transform.localPosition = new Vector3(i == 0 ? -0.72f : 0.72f, 0.11f, -1.25f);

                TrailRenderer t = go.AddComponent<TrailRenderer>();
                t.time = 4f;
                t.minVertexDistance = 0.45f;
                t.widthMultiplier = 0.3f;
                t.alignment = LineAlignment.View;
                t.colorGradient = g;
                t.sharedMaterial = MLMaterials.Trail();
                t.shadowCastingMode = ShadowCastingMode.Off;
                t.receiveShadows = false;
                t.numCapVertices = 0;
                t.emitting = false;
                marks[i] = t;
            }
        }

        void OnEnable()
        {
            EventBus.Subscribe<WorldRebased>(OnRebased);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<WorldRebased>(OnRebased);
        }

        void OnRebased(WorldRebased e)
        {
            // everything here lives in world space, so drop it when the origin moves
            if (smoke != null) smoke.Clear(true);
            for (int i = 0; i < marks.Length; i++) if (marks[i] != null) marks[i].Clear();
        }

        void Update()
        {
            if (smoke == null || car == null) return;
            float rate = Enabled ? car.SlideAmount * 38f * Mathf.Clamp01(car.SpeedMS / 10f) : 0f;
            var emission = smoke.emission;
            emission.rateOverTime = rate;

            bool paint = Enabled && car.SlideAmount > MarkOn && car.SpeedMS > MarkMinSpeed;
            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i] != null) marks[i].emitting = paint;
            }
        }
    }
}
