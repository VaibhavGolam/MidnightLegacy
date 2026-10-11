using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>Everything counted during one run. Reset at the start of each race.</summary>
    public sealed class RunStats
    {
        public int wallHits;
        public int trafficHits;
        public int nearMisses;
        public int cleanCorners;
        public int cleanSectors;
        public int drifts;
        public int driftScore;
        public int bestDrift;
        public int styleScore;
        public int maxCombo = 1;
        public float distance;
        public float time;
        public float topSpeedKph;
        public float damageTaken;

        public int Hits { get { return wallHits + trafficHits; } }
        public bool Perfect { get { return wallHits == 0 && trafficHits == 0; } }

        public void Reset()
        {
            wallHits = 0; trafficHits = 0; nearMisses = 0; cleanCorners = 0; cleanSectors = 0;
            drifts = 0; driftScore = 0; bestDrift = 0; styleScore = 0; maxCombo = 1;
            distance = 0f; time = 0f; topSpeedKph = 0f; damageTaken = 0f;
        }
    }

    /// <summary>
    /// Watches the player car and turns good driving into points: near misses, clean corners, clean 500 m
    /// sectors and drifts. A combo multiplier builds while the player keeps scoring and resets on any crash.
    /// Good driving also refills a little nitro, so clean play is rewarded with speed.
    /// </summary>
    public sealed class StyleTracker : MonoBehaviour
    {
        public RunStats Stats = new RunStats();
        public bool Active;

        public int Combo { get; private set; }
        public float ComboTimer { get; private set; }
        /// <summary>Points of the drift currently in progress (not banked yet).</summary>
        public int LiveDrift { get; private set; }

        CarController car;
        RoadModel road;

        bool inCorner;
        float cornerStartS;
        bool cornerHit;
        float nextSectorS;
        bool sectorHit;
        float driftTime;
        float driftPoints;
        float startDamage;
        float startS;

        const float ComboWindow = 7f;
        const float SectorLength = 500f;

        public void Init(CarController player, RoadModel roadModel)
        {
            car = player;
            road = roadModel;
            ResetRun();
        }

        public void ResetRun()
        {
            Stats.Reset();
            Combo = 1;
            ComboTimer = 0f;
            LiveDrift = 0;
            inCorner = false;
            cornerHit = false;
            sectorHit = false;
            driftTime = 0f;
            driftPoints = 0f;
            if (car != null)
            {
                startS = car.S;
                nextSectorS = car.S + SectorLength;
                startDamage = car.Damage01;
            }
        }

        void OnEnable()
        {
            EventBus.Subscribe<WallHit>(OnWall);
            EventBus.Subscribe<TrafficHit>(OnTraffic);
            EventBus.Subscribe<NearMiss>(OnNear);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<WallHit>(OnWall);
            EventBus.Unsubscribe<TrafficHit>(OnTraffic);
            EventBus.Unsubscribe<NearMiss>(OnNear);
        }

        void Crash()
        {
            Combo = 1;
            ComboTimer = 0f;
            cornerHit = true;
            sectorHit = true;
            driftTime = 0f;
            driftPoints = 0f;
            LiveDrift = 0;
        }

        void OnWall(WallHit e)
        {
            if (!Active || e.impactSpeed < 2f) return;
            Stats.wallHits++;
            Crash();
        }

        void OnTraffic(TrafficHit e)
        {
            if (!Active) return;
            Stats.trafficHits++;
            Crash();
        }

        void OnNear(NearMiss e)
        {
            if (!Active) return;
            Stats.nearMisses++;
            int pts = 40 + Mathf.RoundToInt(Mathf.Clamp01(1f - e.gap) * 40f);
            Award(StyleKind.NearMiss, pts, 0.06f);
        }

        void Award(StyleKind kind, int basePoints, float nitro)
        {
            int pts = basePoints * Combo;
            Stats.styleScore += pts;
            if (nitro > 0f && car != null) car.AddNitro(nitro);
            EventBus.Raise(new StylePoint { kind = kind, points = pts, combo = Combo });
            if (kind != StyleKind.Drift || basePoints > 150)
            {
                Combo = Mathf.Min(8, Combo + 1);
                if (Combo > Stats.maxCombo) Stats.maxCombo = Combo;
            }
            ComboTimer = ComboWindow;
        }

        void Update()
        {
            if (!Active || car == null || road == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Stats.time += dt;
            Stats.distance = car.S - startS;
            Stats.topSpeedKph = Mathf.Max(Stats.topSpeedKph, car.SpeedKph);
            Stats.damageTaken = Mathf.Max(0f, car.Damage01 - startDamage);

            if (ComboTimer > 0f)
            {
                ComboTimer -= dt;
                if (ComboTimer <= 0f) Combo = 1;
            }

            // ---- drift ----
            bool drifting = car.SlideAmount > 0.5f && car.SpeedKph > 45f && !car.IsOffRoad && car.WallHitTimer <= 0f;
            if (drifting)
            {
                driftTime += dt;
                driftPoints += dt * car.SpeedKph * 0.45f * (1f + Mathf.Abs(car.RearSlipDeg) / 18f);
                LiveDrift = Mathf.RoundToInt(driftPoints * Combo);
            }
            else if (driftTime > 0f)
            {
                if (driftTime > 0.8f)
                {
                    int banked = Mathf.RoundToInt(driftPoints);
                    int scored = banked * Combo;
                    Stats.drifts++;
                    Stats.driftScore += scored;
                    if (scored > Stats.bestDrift) Stats.bestDrift = scored;
                    // Award adds the same points to the overall style score; driftScore is tracked separately for drift events
                    Award(StyleKind.Drift, banked, Mathf.Clamp(banked * 0.0001f, 0.01f, 0.1f));
                }
                driftTime = 0f;
                driftPoints = 0f;
                LiveDrift = 0;
            }

            // ---- clean corners ----
            float k = Mathf.Abs(road.At(car.S).kappa);
            if (!inCorner && k > 0.009f)
            {
                inCorner = true;
                cornerStartS = car.S;
                cornerHit = false;
            }
            else if (inCorner && k < 0.0045f)
            {
                inCorner = false;
                if (!cornerHit && car.S - cornerStartS > 28f)
                {
                    Stats.cleanCorners++;
                    Award(StyleKind.CleanCorner, 120, 0.10f);
                }
            }

            // ---- clean sectors ----
            if (car.S >= nextSectorS)
            {
                if (!sectorHit)
                {
                    Stats.cleanSectors++;
                    Award(StyleKind.CleanSector, 250, 0.20f);
                }
                sectorHit = false;
                nextSectorS += SectorLength;
            }
        }
    }
}
