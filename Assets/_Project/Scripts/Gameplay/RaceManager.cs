using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Runs one event from countdown to results: sprints, time trials, drift score, traffic runs, chases,
    /// escapes, the guided tutorial and endless runs. Owns the rival, the checkpoints and the finish line,
    /// and builds the RaceResult (medal and reward lines) that the results screen shows.
    /// </summary>
    public sealed class RaceManager : MonoBehaviour
    {
        public RaceDef Def { get; private set; }
        public RaceState State { get; private set; }
        public RaceResult Result { get; private set; }
        public RivalController Rival { get; private set; }
        public RivalController Cameo { get { return cameo; } }
        public RunStats Stats { get { return style.Stats; } }

        public float Elapsed { get; private set; }
        public float TimeLeft { get; private set; }
        public float StartS { get; private set; }
        public float FinishS { get; private set; }
        public int Checkpoints { get; private set; }
        public bool Revived { get; private set; }
        public bool ReadyForResults { get; private set; }
        public string Banner { get; private set; }
        public float BannerTime { get; private set; }
        public float CountdownValue { get; private set; }
        public bool HasFinishLine { get { return FinishS > 0f; } }
        public bool HasTimer { get { return Def.timeLimit > 0f; } }

        /// <summary>Distance in metres between player and rival, positive when the player is ahead.</summary>
        public float GapToRival
        {
            get
            {
                if (Rival == null) return 0f;
                return car.S - Rival.S;
            }
        }

        /// <summary>0 to 1 progress along the route (0 when there is no finish line).</summary>
        public float Progress01
        {
            get
            {
                if (!HasFinishLine) return 0f;
                return Mathf.Clamp01((car.S - StartS) / (FinishS - StartS));
            }
        }

        CarController car;
        RoadModel road;
        TrafficSystem traffic;
        StyleTracker style;
        TrackGenerator track;
        InputReader input;
        SaveData save;
        RivalDef rivalDef;
        RivalDef unknownDef;
        float rivalSkillOffset;

        float countdownT;
        int lastCount = 99;
        float finishTimer;
        float nextCheckpointS;
        bool rivalFinished;
        float rivalFinishTime;
        float closestChaser = 999f;
        bool cameoSpawned;
        RivalController cameo;
        readonly List<TrackMarker> markers = new List<TrackMarker>();

        public void Init(CarController playerCar, RoadModel roadModel, TrafficSystem trafficSystem, StyleTracker tracker,
                         TrackGenerator trackGen, InputReader inputReader, RaceDef def, SaveData saveData,
                         RivalDef rival, RivalDef unknown, float skillOffset)
        {
            car = playerCar;
            road = roadModel;
            traffic = trafficSystem;
            style = tracker;
            track = trackGen;
            input = inputReader;
            Def = def;
            save = saveData;
            rivalDef = rival;
            unknownDef = unknown;
            rivalSkillOffset = skillOffset;
            Setup();
        }

        void Setup()
        {
            Result = null;
            ReadyForResults = false;
            Revived = false;
            cameoSpawned = false;
            rivalFinished = false;
            closestChaser = 999f;
            finishTimer = 0f;
            Checkpoints = 0;
            Elapsed = 0f;
            Banner = "";
            BannerTime = 0f;
            TimeLeft = Def.timeLimit;
            Time.timeScale = 1f;

            // ---- grid ----
            bool hasRival = rivalDef != null && Def.kind != RaceKind.Tutorial && Def.kind != RaceKind.Endless
                            && Def.kind != RaceKind.TimeTrial && Def.kind != RaceKind.DriftScore && Def.kind != RaceKind.TrafficRun;
            float startS = 80f;
            float playerD = hasRival && Def.kind == RaceKind.Sprint ? -1.6f : 0f;
            car.PlaceAt(startS, playerD);
            car.SpeedCapKph = car.profile.startSpeedKph;
            StartS = startS;
            FinishS = Def.length > 0f ? startS + Def.length : 0f;
            nextCheckpointS = startS + Def.checkpointEvery;

            if (Rival != null) Destroy(Rival.gameObject);
            Rival = null;
            if (cameo != null) Destroy(cameo.gameObject);
            cameo = null;

            if (hasRival)
            {
                GameObject go = new GameObject("Rival_" + rivalDef.id);
                Rival = go.AddComponent<RivalController>();
                RivalMode mode = RivalMode.Racer;
                float rs = startS, rd = 1.6f;
                if (Def.kind == RaceKind.Chase) { rs = startS + Mathf.Max(20f, Def.rivalHeadStart); rd = 0f; }
                else if (Def.kind == RaceKind.Escape) { mode = RivalMode.Chaser; rs = startS - Mathf.Max(30f, Mathf.Abs(Def.rivalHeadStart)); rd = 0f; }
                Rival.Init(road, car, traffic, rivalDef, mode, rs, rd, rivalSkillOffset);
                Rival.GripScale = car.SurfaceGrip;
                Rival.Rubber = rivalDef.unknown ? 0.9f : 0.55f;
            }

            // ---- gantries ----
            markers.Clear();
            if (Def.kind == RaceKind.TimeTrial && Def.checkpointEvery > 0f)
                for (float s = startS + Def.checkpointEvery; FinishS <= 0f || s < FinishS - 60f; s += Def.checkpointEvery)
                {
                    markers.Add(new TrackMarker { s = s, kind = 0 });
                    if (markers.Count > 40) break;
                }
            if (FinishS > 0f) markers.Add(new TrackMarker { s = FinishS, kind = 1 });
            track.SetMarkers(markers);

            // ---- traffic ----
            if (traffic != null)
            {
                traffic.Enabled = Def.traffic > 0 || Def.kind == RaceKind.Endless;
                traffic.ResetRun();
            }
            style.ResetRun();
            style.Active = false;

            // ---- countdown ----
            State = RaceState.Countdown;
            countdownT = 3.6f;
            lastCount = 99;
            EventBus.Raise(new RaceStateChanged { state = State });
        }

        public void Restart()
        {
            car.Damage01 = save != null ? save.damage : 0f;
            Setup();
            EventBus.Raise(new RunReset());
        }

        void PushBanner(string text, float seconds)
        {
            Banner = text;
            BannerTime = seconds;
        }

        void Update()
        {
            if (State == RaceState.Finished)
            {
                finishTimer += Time.unscaledDeltaTime;
                if (BannerTime > 0f) BannerTime -= Time.unscaledDeltaTime;
                if (finishTimer > 1.25f && !ReadyForResults) ReadyForResults = true;
                return;
            }

            float dt = Time.deltaTime;
            if (BannerTime > 0f) BannerTime -= dt;

            if (State == RaceState.Countdown)
            {
                countdownT -= dt;
                int c = Mathf.CeilToInt(countdownT - 0.6f);
                CountdownValue = Mathf.Max(0f, countdownT - 0.6f);
                if (c != lastCount && c >= 1 && c <= 3)
                {
                    lastCount = c;
                    PushBanner(c.ToString(), 0.9f);
                }
                if (countdownT <= 0.6f) StartRace();
                return;
            }

            if (State != RaceState.Racing) return;

            Elapsed += dt;
            if (Def.timeLimit > 0f && Def.kind != RaceKind.Chase) TimeLeft -= dt;
            else if (Def.kind == RaceKind.Chase) TimeLeft = Mathf.Max(0f, Def.timeLimit - Elapsed);

            if (Rival != null && !rivalFinished && FinishS > 0f && Rival.S >= FinishS)
            {
                rivalFinished = true;
                rivalFinishTime = Elapsed;
                Rival.Finished = true;
                Rival.FinishTime = Elapsed;
            }

            TryCameo();
            CheckEnd();
        }

        void StartRace()
        {
            State = RaceState.Racing;
            car.SpeedCapKph = 0f;
            Elapsed = 0f;
            style.ResetRun();
            style.Active = true;
            if (Rival != null) Rival.Release();
            PushBanner("GO!", 1.0f);
            EventBus.Raise(new RaceStateChanged { state = State });
        }

        void TryCameo()
        {
            if (!Def.cameo || cameoSpawned || unknownDef == null || FinishS <= 0f) return;
            if (car.S - StartS < Def.length * 0.5f) return;
            if (Mathf.Abs(road.At(car.S).kappa) > 0.003f || Mathf.Abs(road.At(car.S + 50f).kappa) > 0.004f) return;

            cameoSpawned = true;
            GameObject go = new GameObject("Cameo_UnknownDriver");
            cameo = go.AddComponent<RivalController>();
            cameo.Init(road, car, traffic, unknownDef, RivalMode.Cameo, car.S - 150f, car.D >= 0f ? -1.9f : 1.9f, 0f);
            cameo.CollisionEnabled = false;
        }

        void CheckEnd()
        {
            float done = car.S - StartS;
            switch (Def.kind)
            {
                case RaceKind.Tutorial:
                    if (FinishS > 0f && car.S >= FinishS) End(true, null);
                    break;

                case RaceKind.Sprint:
                    if (rivalFinished && car.S < FinishS) End(false, "Beaten to the line");
                    else if (FinishS > 0f && car.S >= FinishS) End(true, null);
                    else if (Def.timeLimit > 0f && TimeLeft <= 0f) End(false, "Out of time");
                    break;

                case RaceKind.TimeTrial:
                    if (Def.checkpointEvery > 0f && car.S >= nextCheckpointS && (FinishS <= 0f || nextCheckpointS < FinishS - 60f))
                    {
                        Checkpoints++;
                        nextCheckpointS += Def.checkpointEvery;
                        TimeLeft += Def.checkpointBonus;
                        PushBanner("CHECKPOINT  +" + Mathf.RoundToInt(Def.checkpointBonus) + "s", 1.6f);
                        car.AddNitro(0.15f);
                    }
                    if (FinishS > 0f && car.S >= FinishS) End(true, null);
                    else if (TimeLeft <= 0f) End(false, "Out of time");
                    break;

                case RaceKind.DriftScore:
                    if (Stats.driftScore >= Def.targetScore) End(true, null);
                    else if (TimeLeft <= 0f) End(false, "Out of time");
                    break;

                case RaceKind.TrafficRun:
                    if (Stats.Hits > Def.maxHits) End(false, "Too many crashes");
                    else if (FinishS > 0f && car.S >= FinishS) End(true, null);
                    else if (Def.timeLimit > 0f && TimeLeft <= 0f) End(false, "Out of time");
                    break;

                case RaceKind.Chase:
                    if (Rival != null && car.S > Rival.S + 6f && done > 40f) End(true, null);
                    else if (Rival != null && rivalFinished) End(false, "He got away");
                    else if (Def.timeLimit > 0f && TimeLeft <= 0f) End(false, "He got away");
                    break;

                case RaceKind.Escape:
                    if (Rival != null)
                    {
                        float gap = car.S - Rival.S;
                        closestChaser = Mathf.Min(closestChaser, gap);
                        if (gap < 3.2f) End(false, "Caught");
                    }
                    if (FinishS > 0f && car.S >= FinishS) End(true, null);
                    else if (Stats.Hits > Def.maxHits) End(false, "Too many crashes");
                    break;

                case RaceKind.Endless:
                    if (car.Damage01 >= 0.98f) End(true, "Wrecked");
                    break;
            }
        }

        /// <summary>Endless runs end when the player chooses to stop.</summary>
        public void EndEndless()
        {
            if (State == RaceState.Racing && Def.kind == RaceKind.Endless) End(true, "Run ended");
        }

        void End(bool won, string reason)
        {
            if (State == RaceState.Finished) return;
            State = RaceState.Finished;
            style.Active = false;
            finishTimer = 0f;
            Time.timeScale = 0.35f;
            if (cameo != null) cameo.gameObject.SetActive(false);

            Result = BuildResult(won, reason);
            PushBanner(won ? (Def.kind == RaceKind.Endless ? "RUN OVER" : "FINISH!") : "FAILED", 3f);
            EventBus.Raise(new RaceStateChanged { state = State });
            EventBus.Raise(new RaceFinished { result = Result });
        }

        RaceResult BuildResult(bool won, string reason)
        {
            RaceResult r = new RaceResult();
            r.def = Def;
            r.won = won;
            r.failReason = reason;
            r.time = Elapsed;
            r.stats = Stats;
            r.distance = Stats.distance;
            r.score = Def.kind == RaceKind.DriftScore ? Stats.driftScore : Stats.styleScore;
            r.position = (Rival != null && rivalFinished && !won) ? 2 : 1;

            bool replay = save != null && save.IsCompleted(Def.id);

            if (Def.kind == RaceKind.Endless)
            {
                int cashDist = Mathf.RoundToInt(Stats.distance / 12f);
                int cashStyle = Mathf.RoundToInt(Stats.styleScore / 40f);
                r.lines.Add(new RewardLine("Distance " + Mathf.RoundToInt(Stats.distance) + " m", cashDist));
                r.lines.Add(new RewardLine("Style score " + Stats.styleScore, cashStyle));
                r.bonusCash = cashStyle;
                r.cash = cashDist + cashStyle;
                r.rep = Mathf.RoundToInt(Stats.distance / 500f);
                r.canRevive = false;
                return r;
            }

            if (won)
            {
                // medal
                int m = 1;
                bool silver, gold;
                switch (Def.kind)
                {
                    case RaceKind.DriftScore:
                        silver = TimeLeft > Def.timeLimit * 0.25f;
                        gold = TimeLeft > Def.timeLimit * 0.5f;
                        break;
                    case RaceKind.TrafficRun:
                        silver = Stats.Hits <= 1;
                        gold = Stats.Perfect && Stats.nearMisses >= Def.targetNearMisses;
                        break;
                    case RaceKind.Chase:
                        silver = Elapsed < Def.timeLimit * 0.75f;
                        gold = Elapsed < Def.timeLimit * 0.5f && Stats.Hits <= 1;
                        break;
                    default:
                        if (Def.parTime > 0f)
                        {
                            silver = Elapsed <= Def.parTime * 1.10f;
                            gold = Elapsed <= Def.parTime;
                        }
                        else
                        {
                            silver = Stats.Hits <= 1;
                            gold = Stats.Perfect;
                        }
                        break;
                }
                if (silver) m = 2;
                if (silver && gold) m = 3;
                r.medal = m;

                float mult = replay ? 0.5f : 1f;
                int baseCash = Mathf.RoundToInt(Def.rewardCash * mult);
                r.lines.Add(new RewardLine(replay ? "Race reward (replay 50%)" : "Race reward", baseCash));
                r.cash += baseCash;
                r.rep = Mathf.RoundToInt(Def.rewardRep * (replay ? 0.3f : 1f));
                if (m >= 2)
                {
                    int mc = Mathf.RoundToInt(Def.rewardCash * (m == 3 ? 0.4f : 0.15f) * mult);
                    r.lines.Add(new RewardLine(m == 3 ? "Gold medal" : "Silver medal", mc));
                    r.cash += mc;
                }
            }
            else
            {
                int consolation = Mathf.RoundToInt(Def.rewardCash * 0.15f);
                r.lines.Add(new RewardLine("Entry payout", consolation));
                r.cash += consolation;
                r.canRevive = !Revived && (Def.kind == RaceKind.TimeTrial || Def.kind == RaceKind.TrafficRun
                                           || Def.kind == RaceKind.Escape || Def.kind == RaceKind.DriftScore);
            }

            int bonus = 0;
            int near = Mathf.Min(30, Stats.nearMisses) * 12;
            int corners = Mathf.Min(20, Stats.cleanCorners) * 25;
            int sectors = Mathf.Min(10, Stats.cleanSectors) * 75;
            int drift = Mathf.Min(1500, Stats.driftScore / 30);
            if (near > 0) { r.lines.Add(new RewardLine("Near misses x" + Stats.nearMisses, near)); bonus += near; }
            if (corners > 0) { r.lines.Add(new RewardLine("Clean corners x" + Stats.cleanCorners, corners)); bonus += corners; }
            if (sectors > 0) { r.lines.Add(new RewardLine("Clean sectors x" + Stats.cleanSectors, sectors)); bonus += sectors; }
            if (drift > 0) { r.lines.Add(new RewardLine("Drift score " + Stats.driftScore, drift)); bonus += drift; }
            if (won && Stats.Perfect && Def.kind != RaceKind.Tutorial) { r.lines.Add(new RewardLine("Perfect run", 150)); bonus += 150; }
            if (Def.kind == RaceKind.Tutorial) { bonus = 0; r.lines.Clear(); r.lines.Add(new RewardLine("Tutorial reward", Def.rewardCash)); r.cash = Def.rewardCash; }

            r.bonusCash = bonus;
            r.cash += bonus;
            return r;
        }

        /// <summary>Called after a rewarded ad on the results screen (failed race). Gives the player a second chance.</summary>
        public void Revive()
        {
            if (Result == null || Result.won || Revived) return;
            Revived = true;
            Result = null;
            ReadyForResults = false;
            Time.timeScale = 1f;
            State = RaceState.Racing;
            style.Active = true;
            car.Damage01 = Mathf.Max(0f, car.Damage01 - 0.25f);
            switch (Def.kind)
            {
                case RaceKind.TimeTrial:
                case RaceKind.DriftScore:
                    TimeLeft += 15f;
                    break;
                case RaceKind.TrafficRun:
                    style.Stats.trafficHits = 0;
                    style.Stats.wallHits = 0;
                    break;
                case RaceKind.Escape:
                    if (Rival != null) Rival.PushBack(45f);
                    style.Stats.trafficHits = 0;
                    style.Stats.wallHits = 0;
                    break;
            }
            PushBanner("SECOND CHANCE", 1.4f);
            EventBus.Raise(new RaceStateChanged { state = State });
        }

        void OnDestroy()
        {
            if (Rival != null) Destroy(Rival.gameObject);
            if (cameo != null) Destroy(cameo.gameObject);
            Time.timeScale = 1f;
        }
    }
}
