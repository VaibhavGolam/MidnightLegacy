using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Top-down camera for the driving view. It looks straight down at the road with an orthographic lens, rotated so
    /// the direction of travel is always "up" on the screen. The car sits in the lower part of the screen and the
    /// view zooms out as speed rises, so there is more road to read at 150 km/h.
    /// The heading is smoothed and blended with the road direction so a slide shows up as the car turning inside
    /// the frame instead of the whole world whipping around.
    /// Adds a nitro zoom kick and a short shake on crashes (shake can be turned off in Settings).
    /// </summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        [Tooltip("Visible road length (metres from the bottom to the top of the screen) when slow.")]
        public float viewHeightSlow = 30f;
        [Tooltip("Visible road length at full speed.")]
        public float viewHeightFast = 46f;
        [Tooltip("The view is never narrower than this many metres (tall phones).")]
        public float minViewWidth = 15f;
        [Tooltip("Where the car sits, measured from the bottom of the screen (0.25 = a quarter up).")]
        [Range(0.1f, 0.5f)] public float carScreenHeight = 0.27f;
        [Tooltip("0 = camera follows the road direction, 1 = camera follows the car's heading.")]
        [Range(0f, 1f)] public float followCarHeading = 0.35f;
        public float yawSmoothing = 4.5f;
        public float zoomSmoothing = 2.5f;
        public float nitroZoom = 4f;
        /// <summary>0 turns shake off (Settings > Reduce camera motion).</summary>
        public float shakeScale = 1f;

        const float CameraHeight = 40f;

        CarController car;
        RoadModel road;
        Camera cam;
        float camYaw;
        float viewH = 30f;
        float nitroBlend;
        float shake;
        bool snap = true;

        public void Init(CarController owner, RoadModel roadModel, Camera camera)
        {
            car = owner;
            road = roadModel;
            cam = camera;
            cam.orthographic = true;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 120f;
            snap = true;
        }

        void OnEnable()
        {
            EventBus.Subscribe<WallHit>(OnWall);
            EventBus.Subscribe<TrafficHit>(OnTraffic);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<WallHit>(OnWall);
            EventBus.Unsubscribe<TrafficHit>(OnTraffic);
        }

        void OnWall(WallHit e)
        {
            shake = Mathf.Max(shake, Mathf.Clamp01(e.impactSpeed / 14f));
        }

        void OnTraffic(TrafficHit e)
        {
            shake = Mathf.Max(shake, Mathf.Clamp01(e.impactSpeed / 16f) + 0.2f);
        }

        public void Snap()
        {
            snap = true;
        }

        void LateUpdate()
        {
            if (car == null || road == null || cam == null) return;

            float dt = Time.unscaledDeltaTime;
            Vector3 carPos = car.transform.position;
            float carYaw = car.transform.eulerAngles.y;
            float roadYaw = road.At(car.S).theta * Mathf.Rad2Deg;
            float desired = Mathf.LerpAngle(roadYaw, carYaw, followCarHeading);

            float speed01 = Mathf.Clamp01(car.SpeedMS / 45f);
            nitroBlend = Mathf.MoveTowards(nitroBlend, car.NitroActive ? 1f : 0f, dt * 3f);
            float targetH = Mathf.Lerp(viewHeightSlow, viewHeightFast, speed01) + nitroBlend * nitroZoom;
            // tall, narrow phones: keep a minimum width on screen
            float aspect = Mathf.Max(0.3f, cam.aspect);
            targetH = Mathf.Max(targetH, minViewWidth / aspect);

            if (snap)
            {
                camYaw = desired;
                viewH = targetH;
                snap = false;
                nitroBlend = 0f;
                shake = 0f;
            }
            else
            {
                camYaw = Mathf.LerpAngle(camYaw, desired, 1f - Mathf.Exp(-yawSmoothing * dt));
                viewH = Mathf.Lerp(viewH, targetH, 1f - Mathf.Exp(-zoomSmoothing * dt));
            }
            shake = Mathf.Max(0f, shake - dt * 2.2f);

            Vector3 fwd = Quaternion.Euler(0f, camYaw, 0f) * Vector3.forward;

            // the camera centre sits ahead of the car so the car ends up in the lower part of the screen
            float ahead = (0.5f - carScreenHeight) * viewH;
            Vector3 centre = new Vector3(carPos.x, 0f, carPos.z) + fwd * ahead;

            float amp = shake * shakeScale;
            if (amp > 0.001f)
            {
                Vector3 jitter = new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f) * (amp * 0.9f);
                centre += jitter;
            }

            Vector3 pos = centre + Vector3.up * CameraHeight;
            Quaternion rot = Quaternion.LookRotation(Vector3.down, fwd);
            if (amp > 0.001f) rot = Quaternion.AngleAxis((Random.value - 0.5f) * amp * 3f, Vector3.up) * rot;

            transform.SetPositionAndRotation(pos, rot);
            cam.orthographicSize = viewH * 0.5f;
        }
    }
}
