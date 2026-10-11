using UnityEngine;

namespace MidnightLegacy
{
    public enum PartSlot { Engine = 0, Tires, Suspension, Brakes, Lightweight, Aero, Nitro, Frame }

    public sealed class PartDef
    {
        public int slot;
        public int tier;          // 1..3
        public string name;
        public string desc;
        public int price;
        public int minRep;
    }

    /// <summary>Catalogue of garage parts: 8 slots with 3 tiers each. All of them are earned with in-game cash and reputation.</summary>
    public static class Parts
    {
        public const int SlotCount = 8;
        public const int MaxTier = 3;

        static readonly string[] slotNames = { "Engine", "Tires", "Suspension", "Brakes", "Lightweight", "Aero", "Nitro", "Frame" };

        static readonly string[,] names =
        {
            { "Tuned Intake", "Sport Cams and Exhaust", "Hachi Race Engine" },
            { "Sport Compound", "Semi Slicks", "Full Slicks" },
            { "Lowering Springs", "Coilovers", "Race Suspension" },
            { "Sport Pads", "Big Brake Kit", "Race Brakes" },
            { "Light Seats", "Carbon Panels", "Stripped Interior" },
            { "Front Splitter", "Rear Wing", "Full Aero Kit" },
            { "Bigger Bottle", "Dual Bottles", "Pro Nitro System" },
            { "Seam Welds", "Roll Cage", "Reinforced Chassis" }
        };

        static readonly string[,] descs =
        {
            { "A little more power everywhere.", "Strong mid range push and a higher cruise speed.", "The most powerful engine the Kairo can take." },
            { "More grip, a touch better in the wet.", "Real grip. The car sticks, so be ready for the snap when it lets go.", "Race rubber. Needs heat, rewards smooth steering." },
            { "Lower centre of gravity, calmer weight transfer.", "Sharper turn in and flatter cornering.", "Planted and stable through fast corners." },
            { "Stops sooner with less fade.", "Late braking becomes a real weapon.", "Brakes that bite instantly." },
            { "Loses 40 kg.", "Loses 90 kg in total.", "Loses 150 kg in total. Every input gets sharper." },
            { "Small downforce gain at speed.", "Noticeable grip in fast corners.", "Serious downforce. Best above 100 km/h." },
            { "Longer boost.", "Boost lasts much longer and recharges faster.", "The biggest tank and the strongest push." },
            { "Crashes hurt the car less.", "Much less damage, less power lost when damaged.", "Built to survive a bad night." }
        };

        static readonly int[,] prices =
        {
            { 450, 1200, 2800 }, { 300, 800, 1900 }, { 350, 900, 2100 }, { 300, 800, 1900 },
            { 400, 1000, 2400 }, { 350, 900, 2200 }, { 250, 700, 1600 }, { 300, 800, 2000 }
        };

        static readonly int[] minRep = { 0, 30, 90 };

        public static string SlotName(int slot)
        {
            return slotNames[Mathf.Clamp(slot, 0, SlotCount - 1)];
        }

        /// <summary>The part that takes a slot from tier-1 to tier. Tier is 1..3.</summary>
        public static PartDef Get(int slot, int tier)
        {
            if (slot < 0 || slot >= SlotCount || tier < 1 || tier > MaxTier) return null;
            PartDef p = new PartDef();
            p.slot = slot;
            p.tier = tier;
            p.name = names[slot, tier - 1];
            p.desc = descs[slot, tier - 1];
            p.price = prices[slot, tier - 1];
            p.minRep = minRep[tier - 1] + (slot == (int)PartSlot.Engine && tier == 3 ? 40 : 0);
            return p;
        }
    }

    /// <summary>Six bars for the garage screen, each 0..100.</summary>
    public struct CarRating
    {
        public float power, grip, braking, stability, nitro, toughness;
        public int Overall { get { return Mathf.RoundToInt((power + grip + braking + stability) * 0.25f * 3f + 100f); } }
    }

    /// <summary>
    /// Turns the base handling profile plus the installed parts into the profile the car actually drives with.
    /// Works on a copy so the base asset is never modified.
    /// </summary>
    public static class CarBuild
    {
        static readonly float[] powerMul = { 1f, 1.10f, 1.22f, 1.38f };
        static readonly float[] driveMul = { 1f, 1.06f, 1.14f, 1.24f };
        static readonly float[] topAdd = { 0f, 8f, 18f, 30f };
        static readonly float[] cruiseAdd = { 0f, 6f, 14f, 24f };
        static readonly float[] gripMul = { 1f, 1.04f, 1.08f, 1.13f };
        static readonly float[] wetAdd = { 0f, 0.03f, 0.06f, 0.10f };
        static readonly float[] offAdd = { 0f, 0.02f, 0.04f, 0.06f };
        static readonly float[] cgMul = { 1f, 0.97f, 0.94f, 0.90f };
        static readonly float[] smoothAdd = { 0f, 0.8f, 1.6f, 2.6f };
        static readonly float[] yawMul = { 1f, 1.04f, 1.08f, 1.14f };
        static readonly float[] brakeMul = { 1f, 1.10f, 1.22f, 1.38f };
        static readonly float[] rampAdd = { 0f, 0.6f, 1.2f, 2.0f };
        static readonly float[] massAdd = { 0f, -40f, -90f, -150f };
        static readonly float[] aeroAdd = { 0f, 0.04f, 0.08f, 0.13f };
        static readonly float[] nitroSecAdd = { 0f, 0.8f, 1.7f, 2.8f };
        static readonly float[] nitroForceMul = { 1f, 1.04f, 1.08f, 1.12f };
        static readonly float[] nitroRegenAdd = { 0f, 0.003f, 0.006f, 0.010f };
        static readonly float[] damageMul = { 1f, 0.90f, 0.78f, 0.65f };
        static readonly float[] lossMul = { 1f, 0.92f, 0.84f, 0.75f };

        /// <summary>effective = base + installed parts + player steering sensitivity.</summary>
        public static void Apply(CarHandlingProfile effective, CarHandlingProfile baseProfile, SaveData save)
        {
            effective.CopyFrom(baseProfile);
            if (save == null) return;

            int t;
            t = save.TierOf((int)PartSlot.Engine);
            effective.enginePowerKW *= powerMul[t];
            effective.maxDriveForce *= driveMul[t];
            effective.topSpeedKph += topAdd[t];
            effective.cruiseLimitKph += cruiseAdd[t];

            t = save.TierOf((int)PartSlot.Tires);
            effective.frontGrip *= gripMul[t];
            effective.rearGrip *= gripMul[t];
            effective.wetGripMultiplier = Mathf.Min(1f, effective.wetGripMultiplier + wetAdd[t]);
            effective.offRoadGripMultiplier = Mathf.Min(1f, effective.offRoadGripMultiplier + offAdd[t]);

            t = save.TierOf((int)PartSlot.Suspension);
            effective.cgHeight *= cgMul[t];
            effective.weightTransferSmoothing += smoothAdd[t];
            effective.yawDamping *= yawMul[t];

            t = save.TierOf((int)PartSlot.Brakes);
            effective.brakeForce *= brakeMul[t];
            effective.brakeRamp += rampAdd[t];

            t = save.TierOf((int)PartSlot.Lightweight);
            effective.mass += massAdd[t];

            t = save.TierOf((int)PartSlot.Aero);
            effective.aeroGrip += aeroAdd[t];

            t = save.TierOf((int)PartSlot.Nitro);
            effective.nitroSeconds += nitroSecAdd[t];
            effective.nitroForceMultiplier *= nitroForceMul[t];
            effective.nitroRegenPerSecond += nitroRegenAdd[t];

            t = save.TierOf((int)PartSlot.Frame);
            effective.damageTaken *= damageMul[t];
            effective.damagePerformanceLoss *= lossMul[t];

            float sens = save.settings != null ? Mathf.Clamp(save.settings.steerSensitivity, 0.5f, 1.8f) : 1f;
            effective.fullLockHoldTime = Mathf.Max(0.15f, effective.fullLockHoldTime / sens);
        }

        static float Remap(float v, float lo, float hi)
        {
            return Mathf.Clamp01((v - lo) / (hi - lo)) * 100f;
        }

        public static CarRating Rate(CarHandlingProfile p)
        {
            CarRating r = new CarRating();
            r.power = Remap(p.enginePowerKW * (0.5f + 0.5f * p.maxDriveForce / 4300f) / (p.mass / 1050f), 55f, 105f);
            r.grip = Remap((p.frontGrip + p.rearGrip) * 0.5f * (1f + p.aeroGrip), 1.0f, 1.35f);
            r.braking = Remap(p.brakeForce / p.mass, 6.5f, 12f);
            r.stability = Remap((p.yawDamping / p.mass) / Mathf.Max(0.3f, p.cgHeight), 0.8f, 1.5f);
            r.nitro = Remap(p.nitroSeconds, 3f, 7f);
            r.toughness = Remap(1f / Mathf.Max(0.3f, p.damageTaken), 1f, 1.6f);
            return r;
        }
    }
}
