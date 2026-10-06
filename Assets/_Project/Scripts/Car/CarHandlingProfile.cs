using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Every number that decides how the car drives lives here. Create one with
    /// Assets > Create > Midnight Legacy > Car Handling Profile, or just use the runtime default
    /// and tune it on the phone with the TUNE panel.
    /// Parts (Phase 4) will modify these values in small steps.
    /// </summary>
    [CreateAssetMenu(menuName = "Midnight Legacy/Car Handling Profile", fileName = "KairoHachiR_Handling")]
    public sealed class CarHandlingProfile : ScriptableObject
    {
        [Header("Chassis")]
        public float mass = 1050f;
        [Tooltip("Fraction of the weight on the front axle when standing still.")]
        public float frontWeightBias = 0.53f;
        [Tooltip("Centre of gravity height in metres. Higher = more weight transfer.")]
        public float cgHeight = 0.48f;
        public float wheelbase = 2.45f;
        [Tooltip("Higher = the car is lazier to rotate and slower to stop rotating.")]
        public float yawInertiaFactor = 1.0f;
        [Tooltip("Stability. Higher stops spins sooner, lower lets the car swing more.")]
        public float yawDamping = 450f;

        [Header("Engine (automatic throttle)")]
        public float enginePowerKW = 62f;
        public float topSpeedKph = 190f;
        public float maxDriveForce = 4300f;
        [Tooltip("The auto throttle will not push past this speed. Keeps early runs controllable.")]
        public float cruiseLimitKph = 130f;
        [Tooltip("How much throttle is lifted when the wheel is near full lock. This is what rotates the car mid corner.")]
        [Range(0f, 1f)] public float throttleLiftOnSteer = 0.5f;
        [Tooltip("Steering fraction (0..1 of full lock) where lifting starts.")]
        [Range(0f, 0.9f)] public float liftStartSteer = 0.4f;
        public float liftMinSpeedKph = 50f;
        public float engineBrakeForce = 900f;

        [Header("Steering")]
        public float maxSteerAngle = 30f;
        [Tooltip("X = speed / top speed, Y = fraction of full lock allowed.")]
        public AnimationCurve steerLockBySpeed = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.3f, 0.6f), new Keyframe(1f, 0.22f));
        [Tooltip("Degrees per second while building steering angle.")]
        public float steerSpeed = 110f;
        [Tooltip("Degrees per second while returning to centre.")]
        public float steerReturnSpeed = 240f;
        [Tooltip("Seconds of holding a button to reach full lock.")]
        public float fullLockHoldTime = 0.8f;
        [Tooltip("X = hold time / full lock time, Y = fraction of allowed lock. Starting above 0 makes a tap a small correction.")]
        public AnimationCurve holdToAngleCurve = AnimationCurve.EaseInOut(0f, 0.12f, 1f, 1f);

        [Header("Tyres")]
        public float frontGrip = 1.05f;
        public float rearGrip = 1.0f;
        [Tooltip("Slip angle (degrees) where the front tyres reach maximum grip.")]
        public float frontPeakSlipDeg = 9f;
        [Tooltip("Slide threshold: slip angle (degrees) where the rear tyres reach maximum grip. Past this the rear lets go.")]
        public float rearPeakSlipDeg = 8f;
        [Tooltip("Grip left once fully sliding (fraction of maximum). Lower = snappier, harder to catch.")]
        [Range(0.3f, 1f)] public float slideGripFraction = 0.78f;
        [Tooltip("How many peak slip angles it takes to drop to the sliding grip. Higher = more progressive.")]
        public float slideFalloffRange = 2.2f;

        [Header("Brakes")]
        public float brakeForce = 7500f;
        [Range(0.3f, 0.95f)] public float frontBrakeBias = 0.72f;
        public float brakeRamp = 7f;
        public float handbrakeForce = 3500f;
        [Range(0.05f, 1f)] public float handbrakeRearGripMultiplier = 0.38f;

        [Header("Weight transfer")]
        [Tooltip("Strength of the load moving to the front when braking or lifting.")]
        public float weightTransferFront = 0.8f;
        [Tooltip("Strength of the load moving to the rear when accelerating.")]
        public float weightTransferRear = 1.0f;
        [Tooltip("Suspension lag. Lower = slower, floatier weight transfer.")]
        public float weightTransferSmoothing = 8f;

        [Header("Conditions")]
        [Range(0.3f, 1f)] public float wetGripMultiplier = 0.72f;
        [Range(0.1f, 1f)] public float offRoadGripMultiplier = 0.55f;
        [Tooltip("At 100% damage the car loses this fraction of power and some grip.")]
        [Range(0f, 0.8f)] public float damagePerformanceLoss = 0.35f;
        public float rollingResistance = 0.015f;

        [Header("Start")]
        public float startSpeedKph = 55f;

        public static CarHandlingProfile CreateDefault()
        {
            CarHandlingProfile p = CreateInstance<CarHandlingProfile>();
            p.name = "RuntimeDefaultHandling";
            return p;
        }
    }
}
