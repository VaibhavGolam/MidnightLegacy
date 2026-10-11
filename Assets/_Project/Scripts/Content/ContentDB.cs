using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    public sealed class CharacterDef
    {
        public string id;
        public string name;
        public string role;
        public Color color;
        public string glyph;
        public bool mask;
        public bool silhouette;
    }

    public sealed class MessageDef
    {
        public string id;
        public string from;
        public string text;
        public string requiresFlag;
        public int minRep;
    }

    [Serializable]
    public sealed class RaceListJson
    {
        public List<RaceDef> races = new List<RaceDef>();
    }

    /// <summary>
    /// All game content that is plain data: characters, rivals, events and messages.
    /// Everything is defined in code so the game needs no assets, and any race can be overridden or added by dropping
    /// a JSON file at Assets/_Project/Resources/MLContent/races.json (see the sample file there).
    /// </summary>
    public static class ContentDB
    {
        public static readonly List<CharacterDef> Characters = new List<CharacterDef>();
        public static readonly List<RivalDef> Rivals = new List<RivalDef>();
        public static readonly List<RaceDef> Races = new List<RaceDef>();
        public static readonly List<MessageDef> Messages = new List<MessageDef>();
        static bool ready;

        static Color C(float r, float g, float b) { return new Color(r, g, b); }

        public static void Init()
        {
            if (ready) return;
            ready = true;
            BuildCharacters();
            BuildRivals();
            BuildRaces();
            BuildMessages();
            LoadOverrides();
        }

        public static CharacterDef Character(string id)
        {
            for (int i = 0; i < Characters.Count; i++) if (Characters[i].id == id) return Characters[i];
            return null;
        }

        public static RivalDef Rival(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Rivals.Count; i++) if (Rivals[i].id == id) return Rivals[i];
            return null;
        }

        public static RaceDef Race(string id)
        {
            for (int i = 0; i < Races.Count; i++) if (Races[i].id == id) return Races[i];
            return null;
        }

        // ------------------------------------------------------------------------------------------

        static void BuildCharacters()
        {
            Characters.Add(new CharacterDef { id = "narr", name = "", role = "" });
            Characters.Add(new CharacterDef { id = "jun", name = "{name}", role = "Student", color = C(0.42f, 0.47f, 0.84f), glyph = "" });
            Characters.Add(new CharacterDef { id = "sato", name = "Old Sato", role = "Mechanic", color = C(0.85f, 0.62f, 0.25f), glyph = "S" });
            Characters.Add(new CharacterDef { id = "mia", name = "Mia", role = "Best friend", color = C(0.20f, 0.70f, 0.65f), glyph = "M" });
            Characters.Add(new CharacterDef { id = "rook", name = "Rook", role = "Street racer", color = C(0.80f, 0.25f, 0.22f), glyph = "R" });
            Characters.Add(new CharacterDef { id = "dex", name = "Dex", role = "Redline Syndicate", color = C(0.25f, 0.45f, 0.85f), glyph = "D" });
            Characters.Add(new CharacterDef { id = "ivy", name = "Ivy", role = "Drifter", color = C(0.90f, 0.45f, 0.70f), glyph = "I" });
            Characters.Add(new CharacterDef { id = "ghost", name = "The Ghost", role = "Legend", color = C(0.82f, 0.84f, 0.90f), glyph = "", silhouette = true });
            Characters.Add(new CharacterDef { id = "unknown", name = "???", role = "Unknown Driver", color = C(0.08f, 0.08f, 0.10f), glyph = "", mask = true });
            Characters.Add(new CharacterDef { id = "rina", name = "Rina Aoki", role = "The Black Coupe", color = C(0.55f, 0.35f, 0.80f), glyph = "R" });
        }

        static RivalDef Rv(string id, string name, string character, float skill, float aggr, Color body, int style, int decal, Color decalColor)
        {
            return new RivalDef
            {
                id = id, name = name, character = character, skill = skill, aggression = aggr, body = body, style = style,
                decal = decal, decalColor = decalColor, spoiler = style == 2 ? 2 : 0, rim = C(0.65f, 0.67f, 0.72f)
            };
        }

        static void BuildRivals()
        {
            Rivals.Add(Rv("street1", "Harbor Kid", null, 0.35f, 0.2f, C(0.15f, 0.40f, 0.28f), 1, 0, Color.white));
            Rivals.Add(Rv("street2", "Night Courier", null, 0.45f, 0.3f, C(0.88f, 0.45f, 0.10f), 2, 1, C(0.05f, 0.05f, 0.06f)));
            Rivals.Add(Rv("rook", "Rook", "rook", 0.45f, 0.30f, C(0.78f, 0.16f, 0.14f), 1, 2, C(0.95f, 0.95f, 0.95f)));
            Rivals.Add(Rv("ivy", "Ivy", "ivy", 0.58f, 0.20f, C(0.90f, 0.55f, 0.75f), 0, 2, C(0.95f, 0.95f, 0.95f)));
            Rivals.Add(Rv("dex", "Dex", "dex", 0.64f, 0.55f, C(0.15f, 0.30f, 0.70f), 2, 3, C(0.05f, 0.05f, 0.06f)));
            RivalDef enforcer = Rv("enforcer", "Redline Enforcer", "dex", 0.70f, 0.95f, C(0.35f, 0.05f, 0.06f), 2, 1, C(0.05f, 0.05f, 0.06f));
            enforcer.underglow = C(0.9f, 0.1f, 0.1f);
            Rivals.Add(enforcer);

            RivalDef unknown = new RivalDef
            {
                id = "unknown", name = "The Unknown Driver", character = "unknown", skill = 0.86f, aggression = 0.5f,
                body = C(0.045f, 0.045f, 0.055f), trim = C(0.12f, 0.12f, 0.14f), rim = C(0.20f, 0.21f, 0.26f),
                style = 0, spoiler = 3, decal = 0, underglow = C(0.15f, 0.30f, 1f), unknown = true
            };
            Rivals.Add(unknown);
        }

        static RaceDef R(string id, string title, string blurb, RaceKind kind, string theme, string time, string weather, int seed,
                         float length, int cash, int rep)
        {
            return new RaceDef
            {
                id = id, title = title, blurb = blurb, kind = kind, theme = theme, time = time, weather = weather, seed = seed,
                length = length, rewardCash = cash, rewardRep = rep
            };
        }

        static void BuildRaces()
        {
            RaceDef r;

            // ---------------- Chapter 1 ----------------
            r = R("tutorial", "Harbor Lap", "Learn the controls with Old Sato.", RaceKind.Tutorial, "forest", "night", "clear", 11, 1700f, 100, 5);
            r.story = true; r.chapter = 1; r.difficulty = 0.1f;
            Races.Add(r);

            r = R("ch1_first", "First Night", "A street race against Rook on Harbor Road.", RaceKind.Sprint, "forest", "night", "clear", 21, 1800f, 300, 20);
            r.story = true; r.chapter = 1; r.rival = "rook"; r.cameo = true; r.difficulty = 0.25f;
            r.dialogueBefore = "d_first_before"; r.dialogueWin = "d_first_win"; r.dialogueLose = "d_first_lose"; r.setsFlag = "won_first";
            Races.Add(r);

            // ---------------- Chapter 2 ----------------
            r = R("ch2_coast", "Coast Time Attack", "Beat the clock on Coast Highway. Checkpoints add time.", RaceKind.TimeTrial, "coast", "dusk", "clear", 31, 2400f, 350, 25);
            r.story = true; r.chapter = 2; r.timeLimit = 55f; r.checkpointEvery = 500f; r.checkpointBonus = 14f; r.parTime = 80f; r.difficulty = 0.35f;
            r.dialogueWin = "d_c2_coast_win";
            Races.Add(r);

            r = R("ch2_city", "Outskirts Slalom", "Get through the city traffic with three crashes at most.", RaceKind.TrafficRun, "city", "midnight", "clear", 41, 2200f, 400, 25);
            r.story = true; r.chapter = 2; r.traffic = 2; r.maxHits = 3; r.targetNearMisses = 8; r.difficulty = 0.4f;
            Races.Add(r);

            r = R("ch2_rain", "Rain Rematch", "Rook wants a rematch, and it is pouring.", RaceKind.Sprint, "forest", "night", "rain", 51, 2400f, 500, 30);
            r.story = true; r.chapter = 2; r.rival = "rook"; r.traffic = 1; r.difficulty = 0.45f;
            r.dialogueBefore = "d_rook_rematch_before"; r.dialogueWin = "d_rook_rematch_win"; r.dialogueLose = "d_rook_rematch_lose"; r.setsFlag = "beat_rook";
            Races.Add(r);

            r = R("ch2_escape", "Redline Escape", "A Redline enforcer is behind you. Do not let him catch up.", RaceKind.Escape, "city", "midnight", "clear", 61, 2600f, 550, 35);
            r.story = true; r.chapter = 2; r.rival = "enforcer"; r.rivalHeadStart = -70f; r.traffic = 1; r.maxHits = 4; r.difficulty = 0.5f;
            r.dialogueBefore = "d_c2_escape_before"; r.setsFlag = "escaped_redline";
            Races.Add(r);

            // ---------------- Chapter 3 ----------------
            r = R("ch3_chase", "Catch the Black Coupe", "He is ahead on Kairo Pass. Pass him before the summit.", RaceKind.Chase, "pass", "midnight", "fog", 71, 3000f, 700, 40);
            r.story = true; r.chapter = 3; r.rival = "unknown"; r.rivalHeadStart = 110f; r.timeLimit = 100f; r.difficulty = 0.6f;
            r.dialogueBefore = "d_c3_chase_before"; r.dialogueWin = "d_c3_chase_win"; r.dialogueLose = "d_c3_chase_lose";
            Races.Add(r);

            r = R("ch3_final", "Kairo Pass, Midnight", "The race the Ghost never finished.", RaceKind.Sprint, "pass", "midnight", "fog", 81, 3200f, 1500, 80);
            r.story = true; r.chapter = 3; r.rival = "unknown"; r.difficulty = 0.7f;
            r.dialogueBefore = "d_c3_final_before"; r.dialogueWin = "d_c3_final_win"; r.dialogueLose = "d_c3_final_lose"; r.setsFlag = "beat_unknown";
            Races.Add(r);

            // ---------------- Side events ----------------
            r = R("s_harbor_dash", "Harbor Dash", "Quick sprint against a local kid.", RaceKind.Sprint, "forest", "night", "clear", 101, 1500f, 180, 10);
            r.rival = "street1"; r.difficulty = 0.2f; Races.Add(r);

            r = R("s_dusk_run", "Dusk Run", "Race Ivy along the coast at sunset.", RaceKind.Sprint, "coast", "dusk", "clear", 102, 1800f, 220, 12);
            r.rival = "ivy"; r.minRep = 10; r.difficulty = 0.3f; Races.Add(r);

            r = R("s_drift_alley", "Drift Alley", "Score 1500 drift points in 60 seconds.", RaceKind.DriftScore, "pass", "dusk", "clear", 103, 0f, 260, 15);
            r.timeLimit = 60f; r.targetScore = 1500; r.minRep = 10; r.difficulty = 0.5f; Races.Add(r);

            r = R("s_dawn_delivery", "Dawn Delivery", "Deliver a package across the forest before sunrise ends.", RaceKind.TimeTrial, "forest", "dawn", "clear", 104, 2000f, 280, 15);
            r.timeLimit = 48f; r.checkpointEvery = 500f; r.checkpointBonus = 13f; r.parTime = 70f; r.minRep = 15; r.difficulty = 0.35f; Races.Add(r);

            r = R("s_city_slalom", "Rush Hour", "Heavy traffic. Two crashes and you are out.", RaceKind.TrafficRun, "city", "night", "clear", 105, 1600f, 300, 18);
            r.traffic = 3; r.maxHits = 2; r.targetNearMisses = 6; r.minRep = 20; r.difficulty = 0.4f; Races.Add(r);

            r = R("s_wrong_way", "Wrong Way Pass", "Oncoming traffic on the mountain road. Stay sharp.", RaceKind.TrafficRun, "pass", "night", "clear", 106, 1800f, 450, 25);
            r.traffic = 1; r.oncoming = true; r.maxHits = 2; r.targetNearMisses = 6; r.minRep = 40; r.difficulty = 0.5f; Races.Add(r);

            r = R("s_rain_dex", "Wet City Duel", "Dex wants a race in the rain.", RaceKind.Sprint, "city", "night", "rain", 107, 2200f, 420, 22);
            r.rival = "dex"; r.traffic = 1; r.minRep = 35; r.difficulty = 0.5f; Races.Add(r);

            r = R("s_sunrise", "Sunrise Sprint", "Long coast sprint against Rook.", RaceKind.Sprint, "coast", "dawn", "clear", 108, 3000f, 500, 25);
            r.rival = "rook"; r.minRep = 50; r.difficulty = 0.45f; Races.Add(r);

            r = R("s_pass_trial", "Pass Trial", "A long time attack on the pass.", RaceKind.TimeTrial, "pass", "midnight", "clear", 109, 2600f, 600, 30);
            r.timeLimit = 60f; r.checkpointEvery = 450f; r.checkpointBonus = 13f; r.parTime = 92f; r.minRep = 60; r.difficulty = 0.6f; Races.Add(r);

            r = R("s_coast_escape", "Midnight Escape", "The enforcer is back. Outrun him on the coast.", RaceKind.Escape, "coast", "midnight", "clear", 110, 3000f, 650, 35);
            r.rival = "enforcer"; r.rivalHeadStart = -60f; r.maxHits = 4; r.minRep = 70; r.difficulty = 0.55f; Races.Add(r);

            r = R("s_storm_drift", "Storm Drift", "4000 drift points in 75 seconds, in a storm.", RaceKind.DriftScore, "pass", "midnight", "storm", 111, 0f, 900, 40);
            r.timeLimit = 75f; r.targetScore = 4000; r.minRep = 100; r.difficulty = 0.65f; Races.Add(r);

            // ---------------- Legend Mode (unlocked after the story) ----------------
            r = R("legend_pass", "Ghost Run: Kairo Pass", "Endless. The pass gets busier and tighter the further you go.", RaceKind.Endless, "pass", "midnight", "clear", 201, 0f, 0, 0);
            r.traffic = 1; r.legend = true; r.requiresFlag = "legend_unlocked"; r.difficulty = 0.7f; Races.Add(r);

            r = R("legend_coast", "Ghost Run: Coast", "Endless coast run with dusk light.", RaceKind.Endless, "coast", "dusk", "clear", 202, 0f, 0, 0);
            r.traffic = 1; r.legend = true; r.requiresFlag = "legend_unlocked"; r.difficulty = 0.6f; Races.Add(r);

            r = R("legend_city", "Ghost Run: Outskirts", "Endless city run in the rain.", RaceKind.Endless, "city", "night", "rain", 203, 0f, 0, 0);
            r.traffic = 2; r.legend = true; r.requiresFlag = "legend_unlocked"; r.difficulty = 0.6f; Races.Add(r);

            r = R("legend_daily", "Daily Ghost Run", "One seed per day. Everyone drives the same road.", RaceKind.Endless, "forest", "night", "clear", 204, 0f, 0, 0);
            r.traffic = 1; r.legend = true; r.requiresFlag = "legend_unlocked"; r.difficulty = 0.6f; Races.Add(r);
        }

        static MessageDef M(string id, string from, string text, string flag, int rep)
        {
            return new MessageDef { id = id, from = from, text = text, requiresFlag = flag, minRep = rep };
        }

        static void BuildMessages()
        {
            Messages.Add(M("m_sato_1", "Old Sato", "Garage is open all night. If the car is hurt, come see me. Repairs are cheaper than regrets.", null, 0));
            Messages.Add(M("m_mia_1", "Mia", "I told three people at Alder Tech about your car. Four, if you count the guy at the noodle shop.", "mia_ally", 0));
            Messages.Add(M("m_rook_1", "Rook", "Rematch when you have real tires. I mean it.", "won_first", 0));
            Messages.Add(M("m_unknown_1", "Unknown", "You drive like him.", "got_message_unknown", 0));
            Messages.Add(M("m_dex_1", "Dex", "Nice work on the rain run. We should talk about the Pass.", "beat_rook", 0));
            Messages.Add(M("m_mia_2", "Mia", "Your clean corner streak is the talk of the group chat. Do not crash. No pressure.", null, 60));
            Messages.Add(M("m_sato_2", "Old Sato", "A man who never lifts is a man who never learns. Your grandfather lifted at exactly the right moment.", "read_notebook", 0));

            // notebook pages unlock with reputation
            Messages.Add(M("n_1", "Ghost's Notebook", "Page 1. The road is the only opponent that never lies.", null, 0));
            Messages.Add(M("n_2", "Ghost's Notebook", "Page 4. Brake in a straight line. Turn when the car is quiet.", null, 20));
            Messages.Add(M("n_3", "Ghost's Notebook", "Page 9. A slide is a conversation. If you shout, the car shouts back.", null, 50));
            Messages.Add(M("n_4", "Ghost's Notebook", "Page 15. Traffic is not an obstacle. It is a rhythm.", null, 90));
            Messages.Add(M("n_5", "Ghost's Notebook", "Page 22. I raced for the sound of an engine at midnight. Winning was an accident.", null, 140));
            Messages.Add(M("n_6", "Ghost's Notebook", "Page 31. The night on Kairo Pass. I saw the black car lose its line. I lifted.", "read_notebook", 0));
        }

        static void LoadOverrides()
        {
            try
            {
                TextAsset ta = Resources.Load<TextAsset>("MLContent/races");
                if (ta == null || string.IsNullOrEmpty(ta.text)) return;
                RaceListJson list = JsonUtility.FromJson<RaceListJson>(ta.text);
                if (list == null || list.races == null) return;
                for (int i = 0; i < list.races.Count; i++)
                {
                    RaceDef o = list.races[i];
                    if (o == null || string.IsNullOrEmpty(o.id)) continue;
                    int at = Races.FindIndex(x => x.id == o.id);
                    if (at >= 0) Races[at] = o; else Races.Add(o);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Content] races.json override ignored: " + e.Message);
            }
        }

        /// <summary>A copy of a race with the daily seed applied.</summary>
        public static RaceDef DailyVariant(RaceDef src)
        {
            RaceDef d = JsonUtility.FromJson<RaceDef>(JsonUtility.ToJson(src));
            DateTime now = DateTime.UtcNow;
            d.seed = now.Year * 10000 + now.Month * 100 + now.Day;
            return d;
        }

        public static string TodayKey()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd");
        }
    }
}
