using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy
{
    public sealed class TrafficConfig
    {
        /// <summary>0 none, 1 light, 2 busy, 3 heavy.</summary>
        public int level = 1;
        /// <summary>Left lane carries oncoming traffic, right lane is empty.</summary>
        public bool oncoming;
        public float speedScale = 1f;
        public int seed = 1;
    }

    public sealed class TrafficVehicle
    {
        public TrafficKind kind;
        public GameObject go;
        public Transform tr;
        public MeshFilter mf;
        public bool active;
        public bool oncoming;
        public float s, d;
        public float speed;          // signed, along s. Negative = oncoming
        public float baseSpeed;
        public float vd;             // sideways drift after a hit
        public float yaw, yawRate;   // extra rotation after a hit, radians
        public float halfLen, halfWid, mass;
        public bool nearMissed;
        public bool crashed;
        public float hitCooldown;
    }

    /// <summary>
    /// Pooled traffic in road space. Vehicles are spawned ahead of the player in staggered positions so one lane
    /// is always open, follow each other without overtaking, and are recycled once they are behind.
    /// Detects collisions (impulse exchange by mass) and near misses with the player car.
    /// Update runs just after the car (execution order 10) so it sees the car's final state for the frame.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class TrafficSystem : MonoBehaviour
    {
        public const float LaneOffset = 1.7f;

        CarController car;
        RoadModel road;
        TrafficConfig config;
        System.Random rng;

        readonly List<TrafficVehicle> vehicles = new List<TrafficVehicle>(24);
        readonly Stack<TrafficVehicle>[] pools = new Stack<TrafficVehicle>[7];
        Material material;
        float nextSpawnS;
        float spawnAhead = 200f;
        public bool Enabled = true;

        public List<TrafficVehicle> Vehicles { get { return vehicles; } }
        public int Level { get { return config != null ? config.level : 0; } }

        public void Init(CarController player, RoadModel roadModel, TrafficConfig cfg)
        {
            car = player;
            road = roadModel;
            config = cfg;
            material = MLMaterials.Opaque();
            for (int i = 0; i < pools.Length; i++) pools[i] = new Stack<TrafficVehicle>();
            ResetRun();
        }

        public void SetLevel(int level)
        {
            if (config != null) config.level = level;
        }

        public void ResetRun()
        {
            rng = new System.Random(config.seed * 31 + 7);
            for (int i = 0; i < vehicles.Count; i++) Deactivate(vehicles[i]);
            vehicles.Clear();
            nextSpawnS = car.S + 110f;
        }

        void OnDestroy()
        {
            // the shared meshes live in TrafficMeshes, nothing owned here
        }

        float Gap()
        {
            float lo, hi;
            switch (config.level)
            {
                case 1: lo = 130f; hi = 200f; break;
                case 2: lo = 85f; hi = 135f; break;
                default: lo = 55f; hi = 90f; break;
            }
            return Mathf.Lerp(lo, hi, (float)rng.NextDouble());
        }

        TrafficKind PickKind()
        {
            double r = rng.NextDouble();
            if (r < 0.26) return TrafficKind.Hatch;
            if (r < 0.50) return TrafficKind.Sedan;
            if (r < 0.62) return TrafficKind.Taxi;
            if (r < 0.74) return TrafficKind.Van;
            if (r < 0.82) return TrafficKind.Bus;
            if (r < 0.90) return TrafficKind.Truck;
            return TrafficKind.Moto;
        }

        TrafficVehicle Obtain(TrafficKind kind)
        {
            Stack<TrafficVehicle> pool = pools[(int)kind];
            TrafficVehicle v;
            if (pool.Count > 0)
            {
                v = pool.Pop();
            }
            else
            {
                v = new TrafficVehicle();
                v.kind = kind;
                v.go = new GameObject("Traffic_" + kind);
                v.go.transform.SetParent(transform, false);
                v.tr = v.go.transform;
                v.mf = v.go.AddComponent<MeshFilter>();
                MeshRenderer mr = v.go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = TrafficMeshes.MaterialFor(kind, material);
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            return v;
        }

        void Spawn()
        {
            TrafficKind kind = PickKind();
            TrafficSpec spec = TrafficMeshes.Spec(kind);
            TrafficVehicle v = Obtain(kind);

            bool oncoming = false;
            float lane;
            if (config.oncoming)
            {
                oncoming = true;
                lane = -LaneOffset;
            }
            else
            {
                lane = rng.NextDouble() < 0.5 ? -LaneOffset : LaneOffset;
            }
            if (kind == TrafficKind.Moto && !config.oncoming) lane += ((float)rng.NextDouble() - 0.5f) * 1.2f;

            v.mf.sharedMesh = TrafficMeshes.Get(kind, rng.Next(TrafficMeshes.Variants));
            v.oncoming = oncoming;
            v.s = nextSpawnS;
            v.d = lane;
            v.baseSpeed = spec.baseSpeed * Mathf.Lerp(0.92f, 1.08f, (float)rng.NextDouble()) * config.speedScale;
            v.speed = oncoming ? -v.baseSpeed * 1.1f : v.baseSpeed;
            v.vd = 0f;
            v.yaw = 0f;
            v.yawRate = 0f;
            v.halfLen = spec.halfLen;
            v.halfWid = spec.halfWid;
            v.mass = spec.mass;
            v.nearMissed = false;
            v.crashed = false;
            v.hitCooldown = 0f;
            v.active = true;
            v.go.SetActive(true);
            vehicles.Add(v);
            Place(v);
        }

        void Deactivate(TrafficVehicle v)
        {
            v.active = false;
            v.go.SetActive(false);
            pools[(int)v.kind].Push(v);
        }

        void Place(TrafficVehicle v)
        {
            RoadPoint rp = road.At(v.s);
            Vector3 pos = road.WorldPos(rp, v.d);
            float yaw = rp.theta * Mathf.Rad2Deg + (v.oncoming ? 180f : 0f) + v.yaw * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan(rp.slope) * Mathf.Rad2Deg * (v.oncoming ? -1f : 1f);
            v.tr.SetPositionAndRotation(pos, Quaternion.Euler(pitch, yaw, 0f));
        }

        void Update()
        {
            if (car == null || road == null || !Enabled || config == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;

            // spawn ahead
            if (config.level > 0)
            {
                int guard = 0;
                while (nextSpawnS < car.S + spawnAhead && guard++ < 4)
                {
                    Spawn();
                    nextSpawnS += Gap();
                }
            }
            else if (nextSpawnS < car.S) nextSpawnS = car.S + 100f;

            float wall = road.Recipe.wallOffset + 1.2f;

            for (int i = vehicles.Count - 1; i >= 0; i--)
            {
                TrafficVehicle v = vehicles[i];

                // speed: follow whatever is ahead in any lane so vehicles never end up side by side
                float want = v.oncoming ? -v.baseSpeed * 1.1f : v.baseSpeed;
                if (!v.oncoming && !v.crashed)
                {
                    for (int j = 0; j < vehicles.Count; j++)
                    {
                        TrafficVehicle o = vehicles[j];
                        if (o == v || o.oncoming) continue;
                        float ds = o.s - v.s;
                        if (ds > 0f && ds < 26f + v.halfLen + o.halfLen) want = Mathf.Min(want, Mathf.Max(0f, o.speed) * 0.97f);
                    }
                }
                if (v.crashed) want = v.speed * 0.985f;
                v.speed = Mathf.MoveTowards(v.speed, want, 7f * dt);

                v.s += v.speed * dt;
                if (Mathf.Abs(v.vd) > 0.01f || v.crashed)
                {
                    v.d = Mathf.Clamp(v.d + v.vd * dt, -wall, wall);
                    v.vd *= Mathf.Max(0f, 1f - 1.6f * dt);
                    v.yaw += v.yawRate * dt;
                    v.yawRate *= Mathf.Max(0f, 1f - 1.2f * dt);
                }
                if (v.hitCooldown > 0f) v.hitCooldown -= dt;

                // recycle
                bool behind = v.oncoming ? v.s < car.S - 70f : v.s < car.S - 45f;
                if (behind || v.s > car.S + spawnAhead + 80f)
                {
                    Deactivate(v);
                    vehicles.RemoveAt(i);
                    continue;
                }

                Place(v);
                CheckPlayer(v);
            }
        }

        void CheckPlayer(TrafficVehicle v)
        {
            float ds = car.S - v.s;
            if (Mathf.Abs(ds) > 9f + v.halfLen) return;

            Vector2 n;
            float depth, contactFwd;
            if (v.hitCooldown <= 0f && RoadCollision.Test(car, v.s, v.d, v.halfLen, v.halfWid, out n, out depth, out contactFwd))
            {
                ResolveHit(v, n, depth, contactFwd);
                return;
            }

            if (!v.nearMissed && !v.crashed && Mathf.Abs(ds) < 2.4f && car.SpeedKph > 60f)
            {
                float c = Mathf.Abs(Mathf.Cos(car.Psi));
                float sn = Mathf.Abs(Mathf.Sin(car.Psi));
                float exD = CarController.HalfLength * sn + CarController.HalfWidth * c;
                float clearance = Mathf.Abs(car.D - v.d) - exD - v.halfWid;
                float rel = Mathf.Abs(RoadCollision.CarRoadVelocity(car).x - v.speed);
                if (clearance > 0f && clearance < 1.0f && rel > 5f)
                {
                    v.nearMissed = true;
                    EventBus.Raise(new NearMiss { gap = clearance, relSpeed = rel });
                }
            }
        }

        void ResolveHit(TrafficVehicle v, Vector2 n, float depth, float contactFwd)
        {
            Vector2 vCar = RoadCollision.CarRoadVelocity(car);
            Vector2 vOther = new Vector2(v.speed, v.vd);
            float relN = Vector2.Dot(vCar - vOther, n);
            float mc = car.profile != null ? car.profile.mass : 1050f;
            float impact = Mathf.Max(0f, -relN);

            // separate the bodies first
            car.Displace(n.x * depth, n.y * depth);

            if (relN < 0f)
            {
                const float restitution = 0.25f;
                float j = -(1f + restitution) * relN / (1f / mc + 1f / v.mass);
                Vector2 dvCar = n * (j / mc);
                if (dvCar.magnitude > 14f) dvCar = dvCar.normalized * 14f;
                float damage = Mathf.Clamp(impact * 0.012f, 0.01f, 0.28f);
                car.ApplyImpulse(dvCar, contactFwd, damage);

                Vector2 dvOther = -n * (j / v.mass);
                v.speed += dvOther.x;
                v.vd += dvOther.y;
                v.yawRate += Mathf.Clamp(n.y * impact * 0.08f * Mathf.Sign(contactFwd + 0.01f), -2.5f, 2.5f);
                v.crashed = true;
            }
            v.hitCooldown = 0.6f;
            EventBus.Raise(new TrafficHit { impactSpeed = impact, kind = v.kind });
        }

        /// <summary>
        /// Used by rival AI. Returns true when a vehicle blocks the lane at lateral position d within range ahead of s.
        /// freeD is the centre of the lane that looks clear (or the same d when both are blocked) and blockerSpeed the speed of the blocker.
        /// </summary>
        public bool BlockedAhead(float s, float d, float range, float halfWid, out float freeD, out float blockerSpeed)
        {
            freeD = d;
            blockerSpeed = 99f;
            bool blocked = false;
            for (int i = 0; i < vehicles.Count; i++)
            {
                TrafficVehicle v = vehicles[i];
                float ds = v.s - s;
                if (ds < -v.halfLen || ds > range + v.halfLen) continue;
                if (Mathf.Abs(v.d - d) < v.halfWid + halfWid + 0.35f)
                {
                    blocked = true;
                    blockerSpeed = Mathf.Min(blockerSpeed, v.speed);
                }
            }
            if (!blocked) return false;

            float best = d;
            float bestScore = -1f;
            for (int k = 0; k < 2; k++)
            {
                float lane = k == 0 ? -LaneOffset : LaneOffset;
                float clear = 999f;
                for (int i = 0; i < vehicles.Count; i++)
                {
                    TrafficVehicle v = vehicles[i];
                    float ds = v.s - s;
                    if (ds < -v.halfLen - 3f || ds > range + v.halfLen) continue;
                    if (Mathf.Abs(v.d - lane) < v.halfWid + halfWid + 0.25f) clear = Mathf.Min(clear, Mathf.Max(0f, ds));
                }
                if (clear > bestScore) { bestScore = clear; best = lane; }
            }
            freeD = best;
            return true;
        }
    }
}
