using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// The only place that changes the player's cash, reputation, bests and story flags after a race, and the
    /// only place that spends money in the garage. Keeps the rules in one file so they are easy to tune.
    /// Cash and reputation can only be earned by driving; real money never buys any of them.
    /// </summary>
    public static class Economy
    {
        public const string FreeDriveId = "free";

        /// <summary>
        /// Called as soon as a result exists, before the results screen is drawn: free drive pays nothing and the
        /// NEW BEST tag is worked out. Changes nothing in the save.
        /// </summary>
        public static void Prepare(RaceResult r, SaveData s)
        {
            if (r == null || r.def == null) return;
            RaceDef def = r.def;
            if (def.id == FreeDriveId)
            {
                if (r.cash != 0 || r.rep != 0 || r.lines.Count != 1)
                {
                    r.cash = 0; r.rep = 0; r.bonusCash = 0;
                    r.lines.Clear();
                    r.lines.Add(new RewardLine("Free drive pays nothing", 0));
                }
                return;
            }

            r.newBest = false;
            BestEntry b = s.GetBest(def.id);
            int dist = Mathf.RoundToInt(r.distance);
            if (r.won && def.kind != RaceKind.DriftScore && def.kind != RaceKind.Endless)
            {
                if (!r.usedCheckpoint && (b == null || b.time <= 0f || r.time < b.time)) r.newBest = true;
            }
            if ((def.kind == RaceKind.DriftScore || def.kind == RaceKind.Endless) && r.score > (b != null ? b.score : 0)) r.newBest = true;
            if (def.kind == RaceKind.Endless && dist > (b != null ? b.distance : 0)) r.newBest = true;
        }

        /// <summary>Applies a finished race to the save. Call once per result.</summary>
        public static void Apply(RaceResult r, SaveData s, float finalDamage)
        {
            if (r == null || s == null || r.def == null) return;
            RaceDef def = r.def;
            RunStats st = r.stats;
            bool free = def.id == FreeDriveId;
            bool tutorial = def.kind == RaceKind.Tutorial;

            if (free) Prepare(r, s);

            // wallet and reputation
            s.cash += Mathf.Max(0, r.cash);
            s.rep += Mathf.Max(0, r.rep);
            s.lifetime.cashEarned += Mathf.Max(0, r.cash);

            // damage carries over to the next race (tutorial and free drive never hurt the car for real)
            if (tutorial) s.damage = 0f;
            else if (!free) s.damage = Mathf.Clamp(finalDamage, 0f, 0.9f);

            // lifetime stats
            LifetimeStats l = s.lifetime;
            if (st != null)
            {
                l.distance += st.distance;
                l.nearMisses += st.nearMisses;
                l.cleanCorners += st.cleanCorners;
                l.cleanSectors += st.cleanSectors;
                l.drifts += st.drifts;
                l.crashes += st.Hits;
                l.bestDrift = Mathf.Max(l.bestDrift, st.bestDrift);
                l.topSpeedKph = Mathf.Max(l.topSpeedKph, st.topSpeedKph);
            }
            l.races++;
            if (r.won) l.wins++;

            if (free) return;

            // bests
            BestEntry b = s.GetOrCreateBest(def.id);
            if (r.won)
            {
                // a run continued from a checkpoint still pays out, but it never sets a best time
                if (!r.usedCheckpoint && def.kind != RaceKind.DriftScore && def.kind != RaceKind.Endless)
                {
                    if (b.time <= 0f || r.time < b.time) { b.time = r.time; r.newBest = true; }
                }
                if (r.medal > b.medal) b.medal = r.medal;
                s.MarkCompleted(def.id);
            }
            if (r.score > b.score) { b.score = r.score; if (def.kind == RaceKind.DriftScore || def.kind == RaceKind.Endless) r.newBest = true; }
            int dist = Mathf.RoundToInt(r.distance);
            if (def.kind == RaceKind.Endless && dist > b.distance) { b.distance = dist; r.newBest = true; }

            if (def.kind == RaceKind.Endless)
            {
                s.legendBestDistance = Mathf.Max(s.legendBestDistance, dist);
                s.legendBestScore = Mathf.Max(s.legendBestScore, r.score);
                if (def.id == "legend_daily")
                {
                    string today = ContentDB.TodayKey();
                    if (s.dailyDate != today) { s.dailyDate = today; s.dailyBestScore = 0; }
                    if (r.score > s.dailyBestScore) { s.dailyBestScore = r.score; r.newBest = true; }
                }
            }
        }

        /// <summary>
        /// Rewarded ad on the results screen: pays the bonus part of the reward a second time.
        /// alreadyApplied is true when the race was already written to the save (then the cash is added there too).
        /// </summary>
        public static int DoubleBonus(RaceResult r, SaveData s, bool alreadyApplied)
        {
            if (r == null || s == null || r.bonusCash <= 0) return 0;
            int extra = r.bonusCash;
            if (alreadyApplied)
            {
                s.cash += extra;
                s.lifetime.cashEarned += extra;
            }
            r.cash += extra;
            r.lines.Add(new RewardLine("Bonus doubled", extra));
            r.bonusCash = 0;
            return extra;
        }

        // ---- garage ----

        public static int RepairCost(SaveData s)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(s.damage) * 400f / 10f) * 10;
        }

        public static bool Repair(SaveData s)
        {
            int cost = RepairCost(s);
            if (s.damage <= 0.001f) return false;
            if (s.cash < cost) return false;
            s.cash -= cost;
            s.damage = 0f;
            return true;
        }

        public static void RepairFree(SaveData s)
        {
            s.damage = 0f;
        }

        /// <summary>Why a part cannot be bought right now, or null when it can.</summary>
        public static string PartBlockReason(SaveData s, PartDef p)
        {
            if (p == null) return "Maxed";
            if (s.rep < p.minRep) return "Needs " + p.minRep + " rep";
            if (s.cash < p.price) return "Need $" + (p.price - s.cash) + " more";
            return null;
        }

        public static bool BuyPart(SaveData s, int slot)
        {
            int next = s.TierOf(slot) + 1;
            PartDef p = Parts.Get(slot, next);
            if (p == null || PartBlockReason(s, p) != null) return false;
            s.cash -= p.price;
            s.SetTier(slot, next);
            return true;
        }

        public static bool BuyCosmetic(SaveData s, CosmeticItem item)
        {
            if (item == null || Cosmetics.IsUnlocked(item, s)) return false;
            if (item.pack != null || item.supporter) return false;   // those come from the shop
            if (s.cash < item.price) return false;
            s.cash -= item.price;
            s.unlockedCosmetics.Add(item.id);
            return true;
        }

        public static void Equip(SaveData s, int category, CosmeticItem item)
        {
            switch (category)
            {
                case 0: s.paintId = item.id; break;
                case 1: s.rimId = item.id; break;
                case 2: s.decalId = item.id; break;
                default: s.spoilerId = item.id; break;
            }
        }

        public static string EquippedId(SaveData s, int category)
        {
            switch (category)
            {
                case 0: return s.paintId;
                case 1: return s.rimId;
                case 2: return s.decalId;
                default: return s.spoilerId;
            }
        }
    }
}
