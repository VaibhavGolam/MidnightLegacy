using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    /// <summary>
    /// Rain for the top-down view. One particle system in local space that sits over the middle of the screen and
    /// turns with the camera, so the streaks always run down the screen no matter which way the road bends.
    /// Drops move against the direction of travel, which also gives a sense of speed.
    /// </summary>
    public sealed class WeatherFX : MonoBehaviour
    {
        ParticleSystem rain;
        Transform camTr;
        bool enabledRain;
        int maxParticles = 260;

        public void Init(Camera camera, bool rainOn, QualityPreset preset)
        {
            camTr = camera.transform;
            enabledRain = rainOn;
            maxParticles = preset.level == QualityLevel.Low ? 120 : (preset.level == QualityLevel.Medium ? 220 : 340);
            if (!rainOn) return;

            GameObject go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            rain = go.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            const float life = 0.6f;
            var main = rain.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = life;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            main.startColor = new Color(0.72f, 0.80f, 0.98f, 0.5f);
            main.maxParticles = maxParticles;

            var emission = rain.emission;
            emission.rateOverTime = maxParticles / life;

            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 0.2f, 64f);

            // local +z points up the screen, so the rain falls toward -z
            var vel = rain.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(-26f);

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MLMaterials.Blend();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.06f;
            r.lengthScale = 2.2f;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            rain.Play();
        }

        void LateUpdate()
        {
            if (!enabledRain || rain == null || camTr == null) return;
            // the camera's up axis is the direction of travel
            Vector3 up = camTr.up;
            up.y = 0f;
            if (up.sqrMagnitude < 0.01f) up = Vector3.forward;
            up.Normalize();
            Vector3 p = camTr.position;
            rain.transform.SetPositionAndRotation(new Vector3(p.x, 6f, p.z), Quaternion.LookRotation(up, Vector3.up));
        }
    }
}
