using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Elevated chase camera in the Art of Rally style: high behind the car, looking down the road.
    /// The heading is smoothed and blended with the road direction so slides show up as the car
    /// rotating inside the frame instead of the whole view whipping around.
    /// </summary>
    public sealed class ChaseCamera : MonoBehaviour
    {
        public float distance = 8.0f;
        public float height = 5.6f;
        public float lookAhead = 7.0f;
        public float lookHeight = 0.3f;
        [Tooltip("0 = camera follows the road direction, 1 = camera follows the car's heading.")]
        [Range(0f, 1f)] public float followCarHeading = 0.35f;
        public float yawSmoothing = 4.5f;
        public float heightSmoothing = 4f;
        public float fovSlow = 56f;
        public float fovFast = 66f;
        public float extraDistanceAtSpeed = 2.5f;

        CarController car;
        RoadModel road;
        Camera cam;
        float camYaw;
        float camY;
        bool snap = true;

        public void Init(CarController owner, RoadModel roadModel, Camera camera)
        {
            car = owner;
            road = roadModel;
            cam = camera;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 450f;
            snap = true;
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

            if (snap)
            {
                camYaw = desired;
                camY = carPos.y;
                snap = false;
            }
            else
            {
                camYaw = Mathf.LerpAngle(camYaw, desired, 1f - Mathf.Exp(-yawSmoothing * dt));
                camY = Mathf.Lerp(camY, carPos.y, 1f - Mathf.Exp(-heightSmoothing * dt));
            }

            float speed01 = Mathf.Clamp01(car.SpeedMS / 45f);
            float dist = distance + extraDistanceAtSpeed * speed01;
            Vector3 fwd = Quaternion.Euler(0f, camYaw, 0f) * Vector3.forward;

            Vector3 focus = new Vector3(carPos.x, camY, carPos.z);
            Vector3 pos = focus - fwd * dist + Vector3.up * height;
            Vector3 look = focus + fwd * lookAhead + Vector3.up * lookHeight;

            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
            cam.fieldOfView = Mathf.Lerp(fovSlow, fovFast, speed01);
        }
    }
}
