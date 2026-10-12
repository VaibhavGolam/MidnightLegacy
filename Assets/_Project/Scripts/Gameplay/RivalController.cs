using UnityEngine;

namespace MidnightLegacy
{
    public enum RivalMode
    {
        Racer,    // races to the finish
        Chaser,   // starts behind the player and tries to catch and bump them
        Cameo     // scripted pass by the Unknown Driver, no collisions, vanishes into the fog
    }

    /// <summary>
    /// A computer driven car that lives in road space like the traffic does, so it needs no physics.
    /// Speed comes from the corners ahead (the same lateral grip limit the player feels, scaled by skill),
    /// the line from the curvature, and traffic is avoided by picking the open lane.
    /// Light rubber banding keeps races close without feeling scripted: the AI slows a little when it is far
    /// ahead and finds a little more pace when it is far behind.
    /// </summary>
    public sealed class RivalController : MonoBehaviour, ICarState
    {
        public RivalDef Def { get; private set; }
        public RivalMode Mode { get; private set; }

        public float S { get; private set; }
        public float D { get; private set; }
        public float Speed { get; private set; }
        public bool Released { get; private set; }
        public bool Finished;
        public float FinishTime;
        public bool CameoDone { get; private set; }
        public bool CollisionEnabled = true;
        /// <summary>0 = no rubber banding, 1 = strong.</summary>
        public float Rubber = 0.5f;
        public float GripScale = 1f;

        // ICarState
        public float LateralAccel { get; private set; }
        public float LongAccel { get; private set; }
        public float SteerAngle { get; private set; }
        public float WheelSpinRad { get; private set; }
        public float BrakeAmount { get; private set; }

        RoadModel road;
        CarController player;
        TrafficSystem traffic;
        CarVisuals visuals;
        float skill;
        float seedPhase;
        float prevD;
        float latVel;
        float psiVis;
        float vd;                 // sideways velocity from contact
        float mistakeTimer;
        float nextMistake;
        float cameoBlinkT;
        int cameoLane;
        float rollingSpeed = 15.3f;
        float hitCooldown;
        float elapsed;

        public float AimSkill { get { return skill; } }

        public void Init(RoadModel roadModel, CarController playerCar, TrafficSystem trafficSystem, RivalDef def,
                         RivalMode mode, float startS, float startD, float skillOffset)
        {
            road = roadModel;
            player = playerCar;
            traffic = trafficSystem;
            Def = def;
            Mode = mode;
            skill = Mathf.Clamp01(def.skill + skillOffset);
            seedPhase = (def.id != null ? def.id.GetHashCode() & 255 : 0) * 0.1f;
            nextMistake = 9f + (seedPhase % 7f);
            S = startS;
            D = startD;
            prevD = D;
            Speed = rollingSpeed;
            Released = mode == RivalMode.Cameo;
            CameoDone = false;
            Finished = false;
            cameoBlinkT = -1f;

            CarLook look = new CarLook
            {
                body = def.body, trim = def.trim, rim = def.rim, style = def.style, spoiler = def.spoiler,
                decal = def.decal, decalColor = def.decalColor, underglow = def.underglow,
                rimStyle = def.unknown ? 2 : 1, darkLights = def.unknown, driverSilhouette = def.unknown
            };
            if (visuals == null) visuals = gameObject.AddComponent<CarVisuals>();
            visuals.Build(this, look, true);
            Place();
        }

        /// <summary>Moves the rival back along the road (used when a failed escape is revived).</summary>
        public void PushBack(float metres)
        {
            S = Mathf.Max(5f, S - metres);
            Speed = Mathf.Min(Speed, 22f);
        }

        /// <summary>Puts the rival back at a point on the road and holds it there until Release (restart from a checkpoint).</summary>
        public void ResetTo(float s, float d)
        {
            S = s;
            D = d;
            prevD = d;
            Speed = rollingSpeed;
            Released = false;
            Finished = false;
            vd = 0f;
            latVel = 0f;
            elapsed = 0f;
            hitCooldown = 0f;
            Place();
        }

        /// <summary>Lets the rival go (race start). Before this it holds a rolling start speed.</summary>
        public void Release()
        {
            Released = true;
            elapsed = 0f;
        }

        void Update()
        {
            if (road == null || player == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;
            elapsed += dt;
            if (hitCooldown > 0f) hitCooldown -= dt;

            if (Mode == RivalMode.Cameo)
            {
                UpdateCameo(dt);
                Place();
                return;
            }

            float target = Released ? TargetSpeed() : rollingSpeed;

            // rubber banding relative to the player
            float gap = S - player.S;   // positive = rival ahead
            if (Released && Rubber > 0f)
            {
                if (Mode == RivalMode.Racer)
                {
                    if (gap > 55f) target *= Mathf.Lerp(1f, 0.88f, Mathf.Clamp01((gap - 55f) / 120f) * Rubber);
                    else if (gap < -35f) target *= Mathf.Lerp(1f, 1.12f, Mathf.Clamp01((-gap - 35f) / 120f) * Rubber);
                }
                else
                {
                    float behind = -gap;
                    if (behind > 40f) target *= Mathf.Lerp(1f, 1.14f, Mathf.Clamp01((behind - 40f) / 100f) * Rubber);
                    else if (behind < 14f) target *= 0.97f;
                }
            }

            // mistakes for weaker drivers
            if (Released && skill < 0.75f)
            {
                if (mistakeTimer > 0f)
                {
                    mistakeTimer -= dt;
                    target *= 0.85f;
                }
                else if (elapsed > nextMistake)
                {
                    mistakeTimer = 1.4f;
                    nextMistake = elapsed + Mathf.Lerp(26f, 14f, 1f - skill) + (seedPhase % 5f);
                }
            }

            // lateral target
            float targetD = LineOffset();
            if (traffic != null && Released)
            {
                float freeD, blockerSpeed;
                float look = 12f + Speed * 0.9f;
                if (traffic.BlockedAhead(S, D, look, 0.85f, out freeD, out blockerSpeed))
                {
                    targetD = freeD;
                    if (Mathf.Abs(freeD - D) < 0.4f) target = Mathf.Min(target, Mathf.Max(6f, blockerSpeed * 0.92f));
                }
            }
            if (Mode == RivalMode.Chaser)
            {
                // line up behind the player
                targetD = Mathf.Lerp(targetD, player.D, 0.35f + Def.aggression * 0.55f);
            }
            else if (Def.aggression > 0.4f && gap > 2f && gap < 28f)
            {
                // defend the line when ahead
                targetD = Mathf.Lerp(targetD, player.D, Def.aggression * 0.5f);
            }
            targetD += Mathf.Sin(elapsed * 1.3f + seedPhase) * 0.28f * (1f - skill);

            // speed
            float accel = Mathf.Lerp(4.4f, 6.6f, skill);
            float brake = Mathf.Lerp(6.5f, 9.5f, skill);
            float prev = Speed;
            Speed = Mathf.MoveTowards(Speed, target, (target > Speed ? accel : brake) * dt);

            // lateral movement
            float rate = Mathf.Lerp(1.6f, 3.2f, skill) * Mathf.Clamp(Speed / 30f, 0.6f, 1.2f);
            float wall = road.Recipe.wallOffset - 0.35f;
            float newD = Mathf.MoveTowards(D, Mathf.Clamp(targetD, -wall + 0.5f, wall - 0.5f), rate * dt);
            newD += vd * dt;
            vd *= Mathf.Max(0f, 1f - 3f * dt);
            D = Mathf.Clamp(newD, -wall, wall);

            // integrate along the road, taking the curve into account
            float kappa = road.At(S).kappa;
            S += Speed * dt / Mathf.Max(0.3f, 1f - kappa * D);

            // read outs for visuals
            latVel = Mathf.Lerp(latVel, (D - prevD) / dt, Mathf.Min(1f, 8f * dt));
            prevD = D;
            psiVis = Mathf.Clamp(Mathf.Atan2(latVel, Mathf.Max(Speed, 3f)), -0.45f, 0.45f);
            LongAccel = Mathf.Lerp(LongAccel, (Speed - prev) / dt, Mathf.Min(1f, 6f * dt));
            BrakeAmount = Mathf.Clamp01(-LongAccel / 5f);
            LateralAccel = Speed * Speed * kappa;
            SteerAngle = Mathf.Clamp(Mathf.Atan(2.45f * kappa) + psiVis * 0.8f, -0.5f, 0.5f);
            WheelSpinRad += Speed / CarController.WheelRadius * dt;

            Place();
            if (CollisionEnabled && Released) CheckPlayer();
        }

        float TargetSpeed()
        {
            float vMax = Mathf.Lerp(30f, 40f, skill);
            float aLat = Mathf.Lerp(5.4f, 8.6f, skill) * GripScale;
            float aBrake = Mathf.Lerp(6.0f, 9.0f, skill);
            float v = vMax;
            for (float x = 6f; x <= 96f; x += 10f)
            {
                float k = Mathf.Abs(road.At(S + x).kappa);
                if (k < 0.0015f) continue;
                float vc = Mathf.Sqrt(aLat / k);
                float allowed = Mathf.Sqrt(vc * vc + 2f * aBrake * x);
                if (allowed < v) v = allowed;
            }
            return v;
        }

        float LineOffset()
        {
            float kAhead = road.At(S + 10f).kappa;
            float apex = Mathf.Sign(kAhead) * Mathf.Clamp01(Mathf.Abs(kAhead) * 55f) * 1.3f * Mathf.Lerp(0.5f, 1f, skill);
            return apex;
        }

        void UpdateCameo(float dt)
        {
            if (CameoDone) return;
            float rel = S - player.S;   // negative while behind
            if (rel < 8f) Speed = Mathf.MoveTowards(Speed, Mathf.Max(player.SpeedMS + 26f, 52f), 12f * dt);
            else Speed = Mathf.MoveTowards(Speed, player.SpeedMS + 17f, 6f * dt);

            // keep to the lane the player is not in
            if (rel < -18f) cameoLane = player.D >= 0f ? -1 : 1;
            float targetD = (cameoLane == 0 ? -1 : cameoLane) * 1.9f;
            D = Mathf.MoveTowards(D, targetD, 2.5f * dt);

            float kappa = road.At(S).kappa;
            S += Speed * dt / Mathf.Max(0.3f, 1f - kappa * D);
            WheelSpinRad += Speed / CarController.WheelRadius * dt;
            LateralAccel = Speed * Speed * kappa;
            SteerAngle = Mathf.Atan(2.45f * kappa);
            LongAccel = 0f;

            // two brake light blinks when it is far ahead, then it is gone
            BrakeAmount = 0f;
            if (rel > 80f)
            {
                if (cameoBlinkT < 0f) cameoBlinkT = 0f;
                cameoBlinkT += dt;
                if ((cameoBlinkT > 0f && cameoBlinkT < 0.25f) || (cameoBlinkT > 0.55f && cameoBlinkT < 0.8f)) BrakeAmount = 1f;
            }
            if (rel > 210f || cameoBlinkT > 2.4f)
            {
                CameoDone = true;
                gameObject.SetActive(false);
            }
        }

        void Place()
        {
            RoadPoint rp = road.At(S);
            Vector3 pos = road.WorldPos(rp, D);
            float yaw = (rp.theta + psiVis) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan(rp.slope) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, 0f));
        }

        void CheckPlayer()
        {
            if (hitCooldown > 0f) return;
            Vector2 n;
            float depth, contactFwd;
            if (!RoadCollision.Test(player, S, D, CarController.HalfLength, CarController.HalfWidth, out n, out depth, out contactFwd)) return;

            float mp = player.profile != null ? player.profile.mass : 1050f;
            float mr = 1050f;
            Vector2 vP = RoadCollision.CarRoadVelocity(player);
            Vector2 vR = new Vector2(Speed, vd);
            float relN = Vector2.Dot(vP - vR, n);
            player.Displace(n.x * depth * 0.6f, n.y * depth * 0.6f);
            S -= n.x * depth * 0.4f;
            D = Mathf.Clamp(D - n.y * depth * 0.4f, -road.Recipe.wallOffset, road.Recipe.wallOffset);

            float impact = Mathf.Max(0f, -relN);
            if (relN < 0f)
            {
                float j = -(1.2f) * relN / (1f / mp + 1f / mr);
                Vector2 dv = n * (j / mp);
                if (dv.magnitude > 9f) dv = dv.normalized * 9f;
                player.ApplyImpulse(dv, contactFwd, Mathf.Clamp(impact * 0.010f, 0.005f, 0.2f));
                Speed = Mathf.Max(5f, Speed - n.x * (j / mr));
                vd -= n.y * (j / mr);
            }
            hitCooldown = 0.5f;
            EventBus.Raise(new TrafficHit { impactSpeed = impact, kind = TrafficKind.Sedan });
        }
    }
}
