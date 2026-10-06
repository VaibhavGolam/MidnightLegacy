using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// The driving model. A single track bicycle model (front and rear axle) simulated in car space,
    /// then converted to road space (s = distance along the road, d = sideways offset, psi = heading
    /// relative to the road). No WheelColliders: the tyre curve, weight transfer and friction circle
    /// are written out so they can be tuned from CarHandlingProfile.
    ///
    /// Axes: x right, z forward, positive yaw / steering / curvature = turning right.
    /// </summary>
    public sealed class CarController : MonoBehaviour
    {
        public CarHandlingProfile profile;
        public InputReader input;
        public RoadModel road;

        public float startS = 5f;

        // ---- state (road space) ----
        public float S { get; private set; }
        public float D { get; private set; }
        public float Psi { get; private set; }

        // ---- state (car space) ----
        public float Vx { get; private set; }        // forward speed, m/s
        public float Vy { get; private set; }        // sideways speed, m/s (right positive)
        public float Omega { get; private set; }     // yaw rate, rad/s
        public float SteerAngle { get; private set; } // front wheel angle, rad

        // ---- conditions ----
        public float Damage01 { get; set; }
        public float SurfaceGrip { get; set; }       // 1 dry, wetGripMultiplier when wet

        // ---- read outs for HUD, camera, visuals, effects ----
        public float SpeedMS { get { return Mathf.Sqrt(Vx * Vx + Vy * Vy); } }
        public float SpeedKph { get { return SpeedMS * 3.6f; } }
        public float DistanceDriven { get { return S - startS; } }
        public float LateralAccel { get; private set; }
        public float LongAccel { get; private set; }
        public float RearSlipDeg { get; private set; }
        public float FrontSlipDeg { get; private set; }
        public float SlideAmount { get; private set; }  // 0..1
        public bool IsSliding { get { return SlideAmount > 0.3f; } }
        public bool IsOffRoad { get; private set; }
        public float Throttle { get; private set; }
        public float BrakeAmount { get; private set; }
        public float HandbrakeAmount { get; private set; }
        public float WheelSpinRad { get; private set; }
        public float WallHitTimer { get; private set; }

        public const float WheelRadius = 0.31f;

        float holdTime;
        int holdDir;

        void Awake()
        {
            SurfaceGrip = 1f;
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
            if (road != null) ApplyTransform();
        }

        public void ResetCar()
        {
            S = startS;
            D = 0f;
            Psi = 0f;
            Vx = profile.startSpeedKph / 3.6f;
            Vy = 0f;
            Omega = 0f;
            SteerAngle = 0f;
            LongAccel = 0f;
            LateralAccel = 0f;
            SlideAmount = 0f;
            BrakeAmount = 0f;
            HandbrakeAmount = 0f;
            holdTime = 0f;
            holdDir = 0;
            WallHitTimer = 0f;
            ApplyTransform();
        }

        void Update()
        {
            if (road == null || profile == null) return;

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / (1f / 120f)));
            float h = dt / steps;
            for (int i = 0; i < steps; i++) Step(h);
            if (WallHitTimer > 0f) WallHitTimer -= dt;
            ApplyTransform();
        }

        // ------------------------------------------------------------------------------------------

        void UpdateSteering(float dt, int dir)
        {
            CarHandlingProfile p = profile;
            if (dir != 0)
            {
                if (dir != holdDir) { holdTime = 0f; holdDir = dir; }
                holdTime += dt;
            }
            else
            {
                holdDir = 0;
                holdTime = 0f;
            }

            float maxRad = p.maxSteerAngle * Mathf.Deg2Rad;
            float speed01 = Mathf.Clamp01(SpeedMS / Mathf.Max(1f, p.topSpeedKph / 3.6f));
            float lockRad = maxRad * Mathf.Clamp01(p.steerLockBySpeed.Evaluate(speed01));
            float target = 0f;
            if (dir != 0)
            {
                float hold01 = Mathf.Clamp01(holdTime / Mathf.Max(0.05f, p.fullLockHoldTime));
                target = dir * lockRad * Mathf.Clamp01(p.holdToAngleCurve.Evaluate(hold01));
            }

            bool building = dir != 0
                && Mathf.Abs(target) > Mathf.Abs(SteerAngle)
                && (Mathf.Abs(SteerAngle) < 1e-4f || Mathf.Sign(target) == Mathf.Sign(SteerAngle));
            float rate = (building ? p.steerSpeed : p.steerReturnSpeed) * Mathf.Deg2Rad;
            SteerAngle = Mathf.MoveTowards(SteerAngle, target, rate * dt);
        }

        static float TireLateral(float alpha, float peak, float cap, float fx, CarHandlingProfile p)
        {
            float maxF = Mathf.Sqrt(Mathf.Max(0f, cap * cap - fx * fx));   // friction circle
            float s = alpha / Mathf.Max(0.01f, peak);
            float a = Mathf.Abs(s);
            float f;
            if (a < 1f) f = a * (2f - a);                                    // smooth rise to the peak
            else f = Mathf.Lerp(1f, p.slideGripFraction, Mathf.Clamp01((a - 1f) / Mathf.Max(0.1f, p.slideFalloffRange)));
            return -Mathf.Sign(s) * f * maxF;
        }

        void Step(float dt)
        {
            CarHandlingProfile p = profile;
            const float g = 9.81f;
            float m = p.mass;
            float L = p.wheelbase;
            float b = L * p.frontWeightBias;        // centre of gravity to rear axle
            float a = L - b;                        // centre of gravity to front axle
            float Iz = m * a * b * p.yawInertiaFactor;
            float fz0f = m * g * p.frontWeightBias;
            float fz0r = m * g * (1f - p.frontWeightBias);
            float roadHalf = road.Recipe.roadHalfWidth;

            // ---- steering ----
            int dir = (input != null) ? Mathf.RoundToInt(input.Steer) : 0;
            UpdateSteering(dt, dir);

            // ---- automatic throttle, brake, handbrake ----
            float speedMS = Mathf.Max(Vx, 0f);
            float steerNorm = Mathf.Abs(SteerAngle) / Mathf.Max(0.01f, p.maxSteerAngle * Mathf.Deg2Rad);
            float lift = Mathf.Clamp01((steerNorm - p.liftStartSteer) / Mathf.Max(0.01f, 1f - p.liftStartSteer))
                         * p.throttleLiftOnSteer
                         * Mathf.Clamp01((speedMS * 3.6f - p.liftMinSpeedKph) / 40f);
            bool brakeIn = input != null && input.Brake;
            bool handIn = input != null && input.Handbrake;
            BrakeAmount = Mathf.MoveTowards(BrakeAmount, brakeIn ? 1f : 0f, dt * p.brakeRamp);
            HandbrakeAmount = Mathf.MoveTowards(HandbrakeAmount, handIn ? 1f : 0f, dt * 8f);

            float cruise = Mathf.Clamp01((p.cruiseLimitKph / 3.6f - Vx) / 3f);
            Throttle = Mathf.Clamp01(1f - lift) * (1f - BrakeAmount) * cruise;

            // ---- longitudinal forces ----
            float perf = 1f - Mathf.Clamp01(Damage01) * p.damagePerformanceLoss;
            float powerW = p.enginePowerKW * 1000f * perf;
            float vTop = Mathf.Max(10f, p.topSpeedKph / 3.6f);
            float dragK = (p.enginePowerKW * 1000f) / (vTop * vTop * vTop);
            float vEff = Mathf.Max(Vx, 6f);
            float fDrive = Mathf.Min(powerW / vEff, p.maxDriveForce * perf) * Throttle;
            float fDrag = dragK * Vx * Mathf.Abs(Vx);
            IsOffRoad = Mathf.Abs(D) > roadHalf;
            float fRoll = p.rollingResistance * m * g * (IsOffRoad ? 3.5f : 1f) * Mathf.Clamp01(Vx);
            float fEngBrake = p.engineBrakeForce * (1f - Throttle) * (1f - BrakeAmount) * Mathf.Clamp01(Vx / 5f);

            float brakeFade = Mathf.Clamp01((Vx - 2f) / 4f);   // brakes fade out near a crawl so the car never stops dead
            float fBrakeF = p.brakeForce * BrakeAmount * brakeFade * p.frontBrakeBias;
            float fBrakeR = p.brakeForce * BrakeAmount * brakeFade * (1f - p.frontBrakeBias)
                            + p.handbrakeForce * HandbrakeAmount * brakeFade;

            // ---- weight transfer ----
            float axRaw = (fDrive - fDrag - fRoll - fEngBrake - fBrakeF - fBrakeR) / m;
            LongAccel += (axRaw - LongAccel) * Mathf.Min(1f, p.weightTransferSmoothing * dt);
            float shift = (LongAccel >= 0f)
                ? m * LongAccel * p.cgHeight / L * p.weightTransferRear     // accelerating: load moves rearward
                : m * LongAccel * p.cgHeight / L * p.weightTransferFront;   // braking or lifting: load moves forward (shift < 0)
            float fzf = Mathf.Max(fz0f * 0.2f, fz0f - shift);
            float fzr = Mathf.Max(fz0r * 0.2f, fz0r + shift);

            // ---- grip ----
            float damageGrip = 1f - Mathf.Clamp01(Damage01) * p.damagePerformanceLoss * 0.5f;
            float surf = SurfaceGrip * (IsOffRoad ? p.offRoadGripMultiplier : 1f) * damageGrip;
            float muF = p.frontGrip * surf;
            float muR = p.rearGrip * surf * Mathf.Lerp(1f, p.handbrakeRearGripMultiplier, HandbrakeAmount);
            float capF = muF * fzf;
            float capR = muR * fzr;

            float fxF = Mathf.Clamp(-fBrakeF, -capF * 0.9f, capF * 0.9f);
            float fxR = Mathf.Clamp(fDrive - fEngBrake - fBrakeR, -capR * 0.85f, capR * 0.85f);

            // ---- slip angles and tyre forces ----
            float delta = SteerAngle;
            float vxs = Mathf.Max(Vx, 2f);
            float alphaF = Mathf.Atan2(Vy + a * Omega, vxs) - delta;
            float alphaR = Mathf.Atan2(Vy - b * Omega, vxs);
            float peakF = p.frontPeakSlipDeg * Mathf.Deg2Rad;
            float peakR = p.rearPeakSlipDeg * Mathf.Deg2Rad;
            float fyF = TireLateral(alphaF, peakF, capF, fxF, p);
            float fyR = TireLateral(alphaR, peakR, capR, fxR, p);

            FrontSlipDeg = alphaF * Mathf.Rad2Deg;
            RearSlipDeg = alphaR * Mathf.Rad2Deg;
            float rearRatio = Mathf.Abs(alphaR) / Mathf.Max(0.01f, peakR);
            float slideTarget = Mathf.Clamp01((rearRatio - 0.85f) / 0.6f);
            slideTarget = Mathf.Max(slideTarget, HandbrakeAmount * Mathf.Clamp01(Vx / 10f) * 0.8f);
            SlideAmount = Mathf.MoveTowards(SlideAmount, slideTarget, dt * 6f);

            // ---- body forces and integration ----
            float cd = Mathf.Cos(delta), sd = Mathf.Sin(delta);
            float fyFront = fyF * cd + fxF * sd;
            float fxBody = fxF * cd - fyF * sd + fxR - fDrag - fRoll;
            float fyBody = fyFront + fyR;
            float torque = a * fyFront - b * fyR - p.yawDamping * Omega;

            float ax = fxBody / m + Vy * Omega;
            float ay = fyBody / m - Vx * Omega;
            Vx += ax * dt;
            Vy += ay * dt;
            Omega += torque / Iz * dt;
            if (Vx < 3f) Vx = 3f;   // creep floor: no stopping, no reversing
            LateralAccel = Mathf.Lerp(LateralAccel, fyBody / m, Mathf.Min(1f, 10f * dt));

            // ---- convert to road space ----
            float cps = Mathf.Cos(Psi), sps = Mathf.Sin(Psi);
            float vT = Vx * cps - Vy * sps;
            float vN = Vx * sps + Vy * cps;
            float kappa = road.At(S).kappa;
            float denom = Mathf.Max(0.2f, 1f - kappa * D);
            float sDot = vT / denom;
            S += sDot * dt;
            D += vN * dt;
            Psi += (Omega - kappa * sDot) * dt;
            if (Psi > Mathf.PI) Psi -= 2f * Mathf.PI;
            else if (Psi < -Mathf.PI) Psi += 2f * Mathf.PI;

            WheelSpinRad += Vx / WheelRadius * dt;

            // ---- guard rail ----
            float wall = road.Recipe.wallOffset;
            if (Mathf.Abs(D) > wall)
            {
                float side = Mathf.Sign(D);
                D = side * wall;
                cps = Mathf.Cos(Psi); sps = Mathf.Sin(Psi);
                vT = Vx * cps - Vy * sps;
                vN = Vx * sps + Vy * cps;
                if (vN * side > 0f)
                {
                    float impact = vN * side;
                    vN = -vN * 0.15f;
                    vT *= 1f - Mathf.Clamp01(impact * 0.03f);
                    Vx = vT * cps + vN * sps;
                    Vy = -vT * sps + vN * cps;
                    Omega *= 0.8f;
                    Damage01 = Mathf.Clamp01(Damage01 + impact * 0.004f);
                    WallHitTimer = 0.3f;
                    EventBus.Raise(new WallHit { impactSpeed = impact, side = side });
                }
            }
        }

        void ApplyTransform()
        {
            RoadPoint rp = road.At(S);
            Vector3 pos = road.WorldPos(rp, D);
            float yaw = (rp.theta + Psi) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan(rp.slope) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, 0f));
        }
    }
}
