using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Two exhaust flames while nitro is burning, drawn additively so they glow against the dark road.
    /// Particles stay in world space so they trail behind the car.
    /// </summary>
    public sealed class NitroEffects : MonoBehaviour
    {
        CarController car;
        ParticleSystem[] flames = new ParticleSystem[2];
        public bool Enabled = true;

        public void Build(CarController owner)
        {
            car = owner;
            for (int i = 0; i < 2; i++)
            {
                GameObject go = new GameObject(i == 0 ? "NitroFlameL" : "NitroFlameR");
                go.transform.SetParent(car.transform, false);
                go.transform.localPosition = new Vector3(i == 0 ? -0.42f : 0.42f, 0.9f, -2.15f);
                go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                ParticleSystem ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.loop = true;
                main.playOnAwake = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.startLifetime = 0.26f;
                main.startSpeed = 7f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 0.9f);
                main.startColor = new Color(0.55f, 0.75f, 1f, 0.9f);
                main.maxParticles = 60;

                var emission = ps.emission;
                emission.rateOverTime = 0f;

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 5f;
                shape.radius = 0.05f;

                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));

                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient g = new Gradient();
                g.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.75f, 0.9f, 1f), 0f),
                        new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.45f),
                        new GradientColorKey(new Color(0.8f, 0.15f, 0.05f), 1f)
                    },
                    new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
                col.color = g;

                ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = MLMaterials.Additive();
                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;

                ps.Play();
                flames[i] = ps;
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
            for (int i = 0; i < flames.Length; i++) if (flames[i] != null) flames[i].Clear(true);
        }

        void Update()
        {
            if (car == null) return;
            float rate = Enabled && car.NitroActive ? 70f : 0f;
            for (int i = 0; i < flames.Length; i++)
            {
                if (flames[i] == null) continue;
                var e = flames[i].emission;
                e.rateOverTime = rate;
            }
        }
    }
}
