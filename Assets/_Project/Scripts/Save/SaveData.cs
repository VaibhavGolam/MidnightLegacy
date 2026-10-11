using System;
using System.Collections.Generic;

namespace MidnightLegacy
{
    [Serializable]
    public sealed class BestEntry
    {
        public string id;
        public float time;      // best time in seconds (0 = none)
        public int score;       // best score
        public int medal;       // best medal 0..3
        public int distance;    // best distance in metres (endless)
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.9f;
        public bool vibration = true;
        public bool brakeButton = true;
        public bool driftButton = true;
        public bool nitroButton = true;
        public float steerSensitivity = 1f;
        public bool reduceMotion;
        public bool showFps;
        public bool storyAssist = true;
        /// <summary>-1 not asked yet, 0 non-personalised ads, 1 personalised ads.</summary>
        public int adConsent = -1;
        public bool devTuning;
    }

    [Serializable]
    public sealed class LifetimeStats
    {
        public float distance;
        public int races;
        public int wins;
        public int nearMisses;
        public int cleanCorners;
        public int cleanSectors;
        public int drifts;
        public int crashes;
        public int bestDrift;
        public float topSpeedKph;
        public int cashEarned;
    }

    /// <summary>Everything that gets saved. Plain data written with JsonUtility.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string playerName = "Jun";
        public int cash = 500;
        public int rep;
        public int storyStep;
        public int storyLossStreak;
        public bool introSeen;

        public List<string> flags = new List<string>();
        public List<string> completed = new List<string>();
        public List<string> unlockedCosmetics = new List<string>();
        public List<string> readMessages = new List<string>();
        public List<BestEntry> bests = new List<BestEntry>();

        /// <summary>Installed tier (0 stock .. 3) for each garage slot, in slot order.</summary>
        public List<int> partTiers = new List<int>();

        public string paintId = "paint_white";
        public string rimId = "rim_stock";
        public string decalId = "decal_none";
        public string spoilerId = "spoiler_duck";
        public float damage;

        public int legendBestDistance;
        public int legendBestScore;
        public string dailyDate = "";
        public int dailyBestScore;

        public SettingsData settings = new SettingsData();
        public LifetimeStats lifetime = new LifetimeStats();
        public long lastSaved;

        public bool HasFlag(string f)
        {
            return !string.IsNullOrEmpty(f) && flags.Contains(f);
        }

        public void SetFlag(string f)
        {
            if (!string.IsNullOrEmpty(f) && !flags.Contains(f)) flags.Add(f);
        }

        public bool IsCompleted(string raceId)
        {
            return completed.Contains(raceId);
        }

        public void MarkCompleted(string raceId)
        {
            if (!completed.Contains(raceId)) completed.Add(raceId);
        }

        public BestEntry GetBest(string id)
        {
            for (int i = 0; i < bests.Count; i++) if (bests[i].id == id) return bests[i];
            return null;
        }

        public BestEntry GetOrCreateBest(string id)
        {
            BestEntry b = GetBest(id);
            if (b == null)
            {
                b = new BestEntry { id = id };
                bests.Add(b);
            }
            return b;
        }

        public int TierOf(int slot)
        {
            while (partTiers.Count <= slot) partTiers.Add(0);
            return partTiers[slot];
        }

        public void SetTier(int slot, int tier)
        {
            while (partTiers.Count <= slot) partTiers.Add(0);
            partTiers[slot] = tier;
        }

        /// <summary>Fills anything missing after loading an older or damaged file.</summary>
        public void Normalize()
        {
            if (flags == null) flags = new List<string>();
            if (completed == null) completed = new List<string>();
            if (unlockedCosmetics == null) unlockedCosmetics = new List<string>();
            if (readMessages == null) readMessages = new List<string>();
            if (bests == null) bests = new List<BestEntry>();
            if (partTiers == null) partTiers = new List<int>();
            if (settings == null) settings = new SettingsData();
            if (lifetime == null) lifetime = new LifetimeStats();
            if (string.IsNullOrEmpty(playerName)) playerName = "Jun";
            if (string.IsNullOrEmpty(paintId)) paintId = "paint_white";
            if (string.IsNullOrEmpty(rimId)) rimId = "rim_stock";
            if (string.IsNullOrEmpty(decalId)) decalId = "decal_none";
            if (string.IsNullOrEmpty(spoilerId)) spoilerId = "spoiler_duck";
            if (cash < 0) cash = 0;
            if (rep < 0) rep = 0;
            damage = UnityEngine.Mathf.Clamp01(damage);
            version = CurrentVersion;
        }
    }
}
