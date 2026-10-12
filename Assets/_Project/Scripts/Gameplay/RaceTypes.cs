using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    public enum RaceKind
    {
        Tutorial,    // guided drive
        Sprint,      // first to the finish line (against a rival, or against the par time)
        TimeTrial,   // beat the clock, checkpoints add time
        DriftScore,  // reach a drift score before time runs out
        TrafficRun,  // reach the finish through traffic with a limited number of hits
        Chase,       // catch and pass the rival ahead
        Escape,      // stay ahead of a chaser until the finish
        Endless      // free run, Legend Mode
    }

    public enum RaceState { Intro, Countdown, Racing, Finished }

    /// <summary>One event the player can drive. Plain data: can be overridden from JSON in Resources/MLContent/races.json.</summary>
    [Serializable]
    public sealed class RaceDef
    {
        public string id;
        public string title;
        public string blurb;
        public RaceKind kind = RaceKind.Sprint;
        public string theme = "forest";
        public string time = "night";
        public string weather = "clear";
        public int seed = 1;
        [Tooltip("Finish distance in metres. 0 = endless.")]
        public float length = 1800f;
        public float timeLimit;
        public float checkpointEvery = 450f;
        public float checkpointBonus = 12f;
        public int targetScore;
        public int maxHits = 99;
        public int targetNearMisses;
        public string rival;
        public float rivalHeadStart;
        public int traffic;
        public bool oncoming;
        public int rewardCash = 200;
        public int rewardRep = 15;
        public int minRep;
        public bool story;
        public int chapter;
        public bool cameo;
        [Range(0f, 1f)] public float difficulty = 0.3f;
        public float parTime;
        public string dialogueBefore;
        public string dialogueWin;
        public string dialogueLose;
        public string setsFlag;
        public string requiresFlag;
        public bool legend;
    }

    [Serializable]
    public sealed class RivalDef
    {
        public string id;
        public string name;
        public string car;
        public string character;       // portrait id for dialogue
        [Range(0f, 1f)] public float skill = 0.5f;
        [Range(0f, 1f)] public float aggression = 0.3f;
        public Color body = Color.red;
        public Color trim = new Color(0.05f, 0.05f, 0.06f);
        public Color rim = new Color(0.6f, 0.6f, 0.65f);
        public int style;
        public int spoiler;
        public int decal;
        public Color decalColor = Color.white;
        public Color underglow = Color.black;
        public bool unknown;
    }

    public sealed class RewardLine
    {
        public string label;
        public int cash;
        public RewardLine(string l, int c) { label = l; cash = c; }
    }

    /// <summary>What the results screen shows and what gets saved.</summary>
    public sealed class RaceResult
    {
        public RaceDef def;
        public bool won;
        public string failReason;
        public float time;
        public int position = 1;
        public int medal;                 // 0 none, 1 bronze, 2 silver, 3 gold
        public int cash;                  // total cash paid out
        public int bonusCash;             // part of cash that a rewarded ad doubles
        public int rep;
        public int score;                 // drift score or endless score
        public float distance;
        public bool newBest;
        public bool canRevive;
        public bool canCheckpoint;        // failed, but a checkpoint was passed: the player may continue from it
        public bool usedCheckpoint;       // the run was continued from a checkpoint (medal capped, no best time)
        public RunStats stats;
        public readonly List<RewardLine> lines = new List<RewardLine>();
    }
}
