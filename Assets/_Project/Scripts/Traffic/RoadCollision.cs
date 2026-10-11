using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Overlap test between the player car and any other body, done in road space (s along, d across).
    /// Both bodies are treated as boxes aligned with the road; the car box grows with its heading angle.
    /// This is cheap and good enough for an arcade game with bodies the size of cars.
    /// </summary>
    public static class RoadCollision
    {
        /// <summary>Returns true when the car overlaps a box centred at (s, d). Normal points from the other body to the car.</summary>
        public static bool Test(CarController car, float s, float d, float halfLen, float halfWid,
                                out Vector2 normal, out float depth, out float contactFwd)
        {
            float c = Mathf.Abs(Mathf.Cos(car.Psi));
            float sn = Mathf.Abs(Mathf.Sin(car.Psi));
            float exS = CarController.HalfLength * c + CarController.HalfWidth * sn;
            float exD = CarController.HalfLength * sn + CarController.HalfWidth * c;

            float ds = car.S - s;
            float dd = car.D - d;
            float overlapS = exS + halfLen - Mathf.Abs(ds);
            float overlapD = exD + halfWid - Mathf.Abs(dd);

            normal = Vector2.zero;
            depth = 0f;
            contactFwd = 0f;
            if (overlapS <= 0f || overlapD <= 0f) return false;

            if (overlapS < overlapD)
            {
                float sg = ds >= 0f ? 1f : -1f;
                normal = new Vector2(sg, 0f);
                depth = overlapS;
                contactFwd = ds < 0f ? CarController.HalfLength : -CarController.HalfLength;
            }
            else
            {
                float sg = dd >= 0f ? 1f : -1f;
                normal = new Vector2(0f, sg);
                depth = overlapD;
                contactFwd = Mathf.Clamp(-ds, -CarController.HalfLength, CarController.HalfLength);
            }
            return true;
        }

        /// <summary>Car velocity in road space (along, across).</summary>
        public static Vector2 CarRoadVelocity(CarController car)
        {
            float cps = Mathf.Cos(car.Psi), sps = Mathf.Sin(car.Psi);
            return new Vector2(car.Vx * cps - car.Vy * sps, car.Vx * sps + car.Vy * cps);
        }
    }
}
