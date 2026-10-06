using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>Tyre smoke behind the car while the rear is sliding. Gives instant visual feedback when tuning.</summary>
    public sealed class SlideEffects : MonoBehaviour
    {
        CarController car;
        ParticleSystem smoke;
        public bool Enabled = true;

        public void Build(CarController owner)
        {
            car = owner;
            GameObject go = new GameObject("SlideSmoke");
            go.transform.SetParent(car.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.15f, -1.4f);

            smoke = go.AddComponent<ParticleSystem>();
            smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = smoke.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.1f;
            main.startSpeed = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
            main.startColor = new Color(0.72f, 0.72f, 0.78f, 0.38f);
            main.maxParticles = 120;
            main.gravityModifier = -0.02f;

            var emission = smoke.emission;
            emission.rateOverTime = 0f;

            var shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.4f, 0.05f, 0.2f);

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
            // particles live in world space, so drop them when the origin moves
            if (smoke != null) smoke.Clear(true);
        }

        void Update()
        {
            if (smoke == null || car == null) return;
            float rate = Enabled ? car.SlideAmount * 45f * Mathf.Clamp01(car.SpeedMS / 10f) : 0f;
            var emission = smoke.emission;
            emission.rateOverTime = rate;
        }
    }
}
