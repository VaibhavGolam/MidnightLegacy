using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>Everything drawn while driving: speed, timer, nitro, banners, style popups, buttons, tutorial prompts and the pause menu.</summary>
    public sealed class DriveHUD : ScreenBase
    {
        struct Popup
        {
            public string text;
            public Color color;
            public float t;
        }

        const int PopupCount = 4;
        readonly Popup[] popups = new Popup[PopupCount];
        int popupHead;
        float hitFlash;
        float fps = 60f;
        bool showSettings;
        Vector2 settingsScroll;
        readonly SettingsPanel settings = new SettingsPanel();
        bool confirmQuit;

        public override void OnEnter()
        {
            app.Stage.Show(false);
            for (int i = 0; i < PopupCount; i++) popups[i].t = 0f;
            hitFlash = 0f;
            showSettings = false;
            confirmQuit = false;
            EventBus.Subscribe<StylePoint>(OnStyle);
            EventBus.Subscribe<WallHit>(OnWall);
            EventBus.Subscribe<TrafficHit>(OnTraffic);
        }

        public override void OnExit()
        {
            EventBus.Unsubscribe<StylePoint>(OnStyle);
            EventBus.Unsubscribe<WallHit>(OnWall);
            EventBus.Unsubscribe<TrafficHit>(OnTraffic);
        }

        void OnStyle(StylePoint e)
        {
            string label;
            Color c = UIKit.Good;
            switch (e.kind)
            {
                case StyleKind.NearMiss: label = "NEAR MISS"; c = UIKit.Gold; break;
                case StyleKind.CleanCorner: label = "CLEAN CORNER"; break;
                case StyleKind.CleanSector: label = "CLEAN SECTOR"; c = UIKit.Accent; break;
                case StyleKind.Drift: label = "DRIFT"; c = new Color(1f, 0.55f, 0.25f); break;
                case StyleKind.Overtake: label = "OVERTAKE"; break;
                default: label = "PERFECT"; c = UIKit.Gold; break;
            }
            popups[popupHead].text = label + "  +" + e.points + (e.combo > 1 ? "  x" + e.combo : "");
            popups[popupHead].color = c;
            popups[popupHead].t = 1.6f;
            popupHead = (popupHead + 1) % PopupCount;
        }

        void OnWall(WallHit e) { hitFlash = Mathf.Max(hitFlash, Mathf.Clamp01(e.impactSpeed / 12f) * 0.6f); }
        void OnTraffic(TrafficHit e) { hitFlash = Mathf.Max(hitFlash, 0.7f); }

        public override bool OnBack()
        {
            DriveSession s = app.Session;
            if (s == null) return false;
            if (showSettings) { showSettings = false; app.SaveNow(); return true; }
            s.SetPaused(!s.Paused);
            return true;
        }

        // ------------------------------------------------------------------ main

        public override void Draw()
        {
            DriveSession s = app.Session;
            if (s == null || s.Race == null || s.Car == null) return;
            RaceManager race = s.Race;
            CarController car = s.Car;
            Rect safe = SafeRect;
            float dt = Time.unscaledDeltaTime;
            bool repaint = Event.current.type == EventType.Repaint;   // OnGUI runs several times a frame, animate once
            if (repaint)
            {
                if (dt > 0f) fps = Mathf.Lerp(fps, 1f / dt, 0.05f);
                if (hitFlash > 0f) hitFlash -= dt * 1.8f;
            }

            // red flash when something is hit
            if (hitFlash > 0f)
            {
                Color c = new Color(0.9f, 0.1f, 0.1f, hitFlash * 0.35f);
                UIKit.Fill(new Rect(0f, 0f, UIKit.W, 90f), c);
                UIKit.Fill(new Rect(0f, UIKit.H - 90f, UIKit.W, 90f), c);
                UIKit.Fill(new Rect(0f, 0f, 60f, UIKit.H), c);
                UIKit.Fill(new Rect(UIKit.W - 60f, 0f, 60f, UIKit.H), c);
            }

            DrawControls(s);
            DrawTop(s, safe);
            DrawStyle(s, safe);
            DrawBanner(race);
            if (s.Tutorial != null) DrawTutorial(s, safe);

            // pause button
            Rect pb = new Rect(UIKit.W - 24f - 104f, safe.y + 16f, 104f, 104f);
            if (!s.Paused && race.State != RaceState.Finished)
            {
                UIKit.Round(pb, new Color(0f, 0f, 0f, 0.5f));
                UIKit.Label(pb, "II", 44, UIKit.Text, TextAnchor.MiddleCenter, true);
                if (UIKit.Tap(pb)) { s.SetPaused(true); AudioManager.Ui(Sfx.Click); }
            }

            if (s.Tuning != null && !s.Paused && !s.Tuning.Visible)
            {
                Rect tb = new Rect(UIKit.W - 24f - 190f, UIKit.H * 0.50f, 190f, 80f);
                UIKit.Round(tb, new Color(0f, 0f, 0f, 0.5f));
                UIKit.Label(tb, "TUNE", 30, UIKit.Accent, TextAnchor.MiddleCenter, true);
                if (UIKit.Tap(tb)) s.Tuning.Visible = true;
            }

            if (app.Save.settings.showFps)
                UIKit.Label(new Rect(24f, safe.y + 200f, 400f, 36f), Mathf.RoundToInt(fps) + " fps  scale " + app.Adaptive.CurrentScale.ToString("0.00"), 24, new Color(1f, 1f, 1f, 0.7f));

            if (s.Paused) DrawPause(s, safe);
        }

        // ------------------------------------------------------------------ controls

        void DrawControls(DriveSession s)
        {
            InputReader input = app.Input;
            CarController car = s.Car;
            TutorialDirector tut = s.Tutorial;

            // steering zone hints
            Rect left = UIKit.FromNormalised(InputReader.LeftZone);
            Rect right = UIKit.FromNormalised(InputReader.RightZone);
            float a = 0.07f + (input.LeftDown ? 0.10f : 0f);
            UIKit.Round(new Rect(left.x + 14f, left.y + 14f, left.width - 28f, left.height - 28f), new Color(1f, 1f, 1f, a));
            a = 0.07f + (input.RightDown ? 0.10f : 0f);
            UIKit.Round(new Rect(right.x + 14f, right.y + 14f, right.width - 28f, right.height - 28f), new Color(1f, 1f, 1f, a));
            UIKit.Label(left, "<", 120, new Color(1f, 1f, 1f, 0.22f), TextAnchor.MiddleCenter, true);
            UIKit.Label(right, ">", 120, new Color(1f, 1f, 1f, 0.22f), TextAnchor.MiddleCenter, true);

            if (input.BrakeEnabled) DrawButton(UIKit.FromNormalised(InputReader.BrakeButton), "BRAKE", input.Brake, 0f, new Color(0.85f, 0.25f, 0.25f));
            if (input.NitroEnabled) DrawButton(UIKit.FromNormalised(InputReader.NitroButton), "NITRO", input.Nitro || car.NitroActive, car.NitroCharge, new Color(0.3f, 0.7f, 1f));
            if (input.HandbrakeEnabled) DrawButton(UIKit.FromNormalised(InputReader.HandbrakeButton), "DRIFT", input.Handbrake, 0f, new Color(1f, 0.6f, 0.2f));

            // tutorial highlight
            if (tut != null && tut.Visible && tut.HasHighlight)
            {
                Rect h = UIKit.FromNormalised(tut.Highlight);
                float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                Color c = new Color(1f, 0.85f, 0.3f, 0.35f + 0.45f * p);
                float t = 6f;
                UIKit.Fill(new Rect(h.x, h.y, h.width, t), c);
                UIKit.Fill(new Rect(h.x, h.yMax - t, h.width, t), c);
                UIKit.Fill(new Rect(h.x, h.y, t, h.height), c);
                UIKit.Fill(new Rect(h.xMax - t, h.y, t, h.height), c);
            }
        }

        static void DrawButton(Rect r, string label, bool pressed, float fill, Color tint)
        {
            UIKit.Round(r, new Color(0f, 0f, 0f, 0.45f));
            if (fill > 0.001f)
            {
                float w = Mathf.Max(r.height * 0.5f, r.width * Mathf.Clamp01(fill));
                UIKit.Round(new Rect(r.x, r.y, w, r.height), new Color(tint.r, tint.g, tint.b, 0.35f));
            }
            if (pressed) UIKit.Round(r, new Color(tint.r, tint.g, tint.b, 0.55f));
            bool ready = fill >= 0.99f;
            UIKit.Label(r, label, 34, ready ? Color.white : new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, true);
        }

        // ------------------------------------------------------------------ top area

        void DrawTop(DriveSession s, Rect safe)
        {
            RaceManager race = s.Race;
            CarController car = s.Car;
            RaceDef def = race.Def;
            float y = safe.y + 14f;

            // speed
            UIKit.Label(new Rect(28f, y, 400f, 100f), Mathf.RoundToInt(car.SpeedKph).ToString(), 92, UIKit.Text, TextAnchor.UpperLeft, true);
            UIKit.Label(new Rect(32f, y + 92f, 200f, 34f), "KM/H", 24, UIKit.Dim, TextAnchor.UpperLeft, true);

            // nitro and condition bars under the speed
            UIKit.Bar(new Rect(28f, y + 132f, 300f, 14f), car.NitroCharge, car.NitroActive ? Color.white : new Color(0.3f, 0.7f, 1f), new Color(0f, 0f, 0f, 0.5f));
            UIKit.Bar(new Rect(28f, y + 154f, 300f, 10f), 1f - car.Damage01, car.Damage01 > 0.6f ? UIKit.Bad : UIKit.Good, new Color(0f, 0f, 0f, 0.5f));

            // timer in the middle
            string timer = null;
            Color tcol = UIKit.Text;
            if (race.State == RaceState.Racing || race.State == RaceState.Finished)
            {
                if (race.HasTimer && def.kind != RaceKind.Chase)
                {
                    timer = UIKit.Clock(Mathf.Max(0f, race.TimeLeft));
                    if (race.TimeLeft < 10f) tcol = UIKit.Bad;
                }
                else if (def.kind == RaceKind.Chase) timer = UIKit.Clock(Mathf.Max(0f, race.TimeLeft));
                else timer = UIKit.Clock(race.Elapsed);
            }
            if (timer != null) UIKit.Label(new Rect(UIKit.W * 0.5f - 200f, y, 400f, 70f), timer, 56, tcol, TextAnchor.UpperCenter, true);

            // mode specific line under the timer
            string line = null;
            Color lc = UIKit.Dim;
            switch (def.kind)
            {
                case RaceKind.Sprint:
                case RaceKind.Chase:
                    if (race.Rival != null)
                    {
                        float g = race.GapToRival;
                        line = (g >= 0f ? "+" : "") + Mathf.RoundToInt(g) + " m  " + race.Rival.Def.name;
                        lc = g >= 0f ? UIKit.Good : UIKit.Bad;
                    }
                    break;
                case RaceKind.Escape:
                    if (race.Rival != null)
                    {
                        float g = race.GapToRival;
                        line = "CHASER " + Mathf.RoundToInt(g) + " m";
                        lc = g < 15f ? UIKit.Bad : UIKit.Good;
                    }
                    break;
                case RaceKind.DriftScore:
                    line = "DRIFT " + (race.Stats.driftScore + s.Style.LiveDrift) + " / " + def.targetScore;
                    lc = UIKit.Gold;
                    break;
                case RaceKind.TrafficRun:
                    line = "CRASHES " + race.Stats.Hits + " / " + def.maxHits + "   NEAR MISSES " + race.Stats.nearMisses;
                    lc = race.Stats.Hits >= def.maxHits ? UIKit.Bad : UIKit.Dim;
                    break;
                case RaceKind.TimeTrial:
                    line = "CHECKPOINTS " + race.Checkpoints;
                    break;
                case RaceKind.Endless:
                    line = Mathf.RoundToInt(race.Stats.distance) + " m   SCORE " + race.Stats.styleScore;
                    lc = UIKit.Gold;
                    break;
            }
            if (line != null && race.State != RaceState.Countdown)
                UIKit.Label(new Rect(UIKit.W * 0.5f - 330f, y + 66f, 660f, 40f), line, 28, lc, TextAnchor.UpperCenter, true);

            // progress bar to the finish
            if (race.HasFinishLine && race.State != RaceState.Countdown)
            {
                Rect pr = new Rect(UIKit.W * 0.5f - 250f, y + 112f, 500f, 14f);
                UIKit.Bar(pr, race.Progress01, UIKit.Accent, new Color(0f, 0f, 0f, 0.5f));
                if (race.Rival != null && def.kind != RaceKind.Escape)
                {
                    float rp = Mathf.Clamp01((race.Rival.S - race.StartS) / (race.FinishS - race.StartS));
                    UIKit.Circle(new Rect(pr.x + pr.width * rp - 9f, pr.y - 2f, 18f, 18f), UIKit.Bad);
                }
            }
        }

        // ------------------------------------------------------------------ style

        void DrawStyle(DriveSession s, Rect safe)
        {
            StyleTracker st = s.Style;
            float y = safe.y + 150f;
            if (st.Active && st.Stats.styleScore > 0)
            {
                UIKit.Label(new Rect(UIKit.W - 24f - 420f, y + 0f, 420f, 40f), "STYLE " + st.Stats.styleScore.ToString("N0"), 28, UIKit.Gold, TextAnchor.UpperRight, true);
                if (st.Combo > 1)
                {
                    UIKit.Label(new Rect(UIKit.W - 24f - 420f, y + 36f, 420f, 44f), "x" + st.Combo, 38, Color.white, TextAnchor.UpperRight, true);
                    UIKit.Bar(new Rect(UIKit.W - 24f - 200f, y + 82f, 200f, 10f), Mathf.Clamp01(st.ComboTimer / 4f), UIKit.Gold, new Color(0f, 0f, 0f, 0.5f));
                }
            }
            if (st.LiveDrift > 30)
                UIKit.Label(new Rect(0f, UIKit.H * 0.36f, UIKit.W, 70f), "DRIFT  " + st.LiveDrift, 56, new Color(1f, 0.6f, 0.25f), TextAnchor.MiddleCenter, true);

            float py = UIKit.H * 0.43f;
            if (Event.current.type == EventType.Repaint)
                for (int i = 0; i < PopupCount; i++)
                    if (popups[i].t > 0f) popups[i].t -= Time.unscaledDeltaTime;
            // draw newest first
            for (int k = 0; k < PopupCount; k++)
            {
                int i = (popupHead - 1 - k + PopupCount * 2) % PopupCount;
                if (popups[i].t <= 0f) continue;
                float a = Mathf.Clamp01(popups[i].t / 0.5f);
                Color c = popups[i].color;
                UIKit.Label(new Rect(0f, py, UIKit.W, 50f), popups[i].text, 36, new Color(c.r, c.g, c.b, a), TextAnchor.MiddleCenter, true);
                py += 50f;
            }
        }

        // ------------------------------------------------------------------ banner

        void DrawBanner(RaceManager race)
        {
            if (race.BannerTime <= 0f || string.IsNullOrEmpty(race.Banner)) return;
            float a = Mathf.Clamp01(race.BannerTime / 0.35f);
            bool big = race.Banner.Length <= 4;
            int size = big ? 200 : 76;
            Color c = race.Banner == "GO!" ? UIKit.Good : (race.Banner == "FAILED" ? UIKit.Bad : UIKit.Text);
            UIKit.Label(new Rect(0f, UIKit.H * 0.22f, UIKit.W, 240f), race.Banner, size, new Color(c.r, c.g, c.b, a), TextAnchor.MiddleCenter, true);
        }

        // ------------------------------------------------------------------ tutorial

        void DrawTutorial(DriveSession s, Rect safe)
        {
            TutorialDirector t = s.Tutorial;
            if (t == null) return;
            if (t.FlashText != null)
                UIKit.Label(new Rect(0f, UIKit.H * 0.31f, UIKit.W, 80f), t.FlashText, 64, UIKit.Good, TextAnchor.MiddleCenter, true);
            if (!t.Visible) return;
            Rect r = new Rect(60f, safe.y + 230f, UIKit.W - 120f, 250f);
            UIKit.Card(r);
            UIKit.Label(new Rect(r.x + 30f, r.y + 14f, r.width - 60f, 36f), "TUTORIAL  " + (t.StepIndex + 1) + "/" + t.StepCount, 22, UIKit.Accent, TextAnchor.UpperLeft, true);
            UIKit.Label(new Rect(r.x + 30f, r.y + 46f, r.width - 60f, 56f), t.Prompt, 44, UIKit.Text, TextAnchor.UpperLeft, true);
            UIKit.Label(new Rect(r.x + 30f, r.y + 104f, r.width - 60f, 90f), t.Body, 28, UIKit.Dim, TextAnchor.UpperLeft, false, true);
            if (t.Progress01 > 0f) UIKit.Bar(new Rect(r.x + 30f, r.yMax - 30f, r.width - 60f, 12f), t.Progress01, UIKit.Good, new Color(1f, 1f, 1f, 0.12f));
        }

        // ------------------------------------------------------------------ pause

        void DrawPause(DriveSession s, Rect safe)
        {
            UIKit.Fill(new Rect(0f, 0f, UIKit.W, UIKit.H), new Color(0f, 0f, 0.03f, 0.78f));
            RaceDef def = s.Def;

            if (showSettings)
            {
                UIKit.Label(new Rect(0f, safe.y + 40f, UIKit.W, 80f), "SETTINGS", 56, UIKit.Text, TextAnchor.MiddleCenter, true);
                Rect view = new Rect(40f, safe.y + 140f, UIKit.W - 80f, safe.height - 320f);
                settingsScroll = settings.Draw(app, view, settingsScroll, true);
                if (UIKit.Button(new Rect(60f, safe.yMax - 150f, UIKit.W - 120f, 110f), "BACK", BtnStyle.Primary))
                {
                    showSettings = false;
                    app.SaveNow();
                }
                return;
            }

            float w = 760f, x = (UIKit.W - w) * 0.5f, y = safe.y + 330f;
            UIKit.Label(new Rect(0f, y - 140f, UIKit.W, 90f), "PAUSED", 80, UIKit.Text, TextAnchor.MiddleCenter, true);
            UIKit.Label(new Rect(0f, y - 60f, UIKit.W, 40f), def.title, 30, UIKit.Dim, TextAnchor.MiddleCenter);

            if (confirmQuit)
            {
                UIKit.Label(new Rect(x, y + 20f, w, 100f), "Leave this event? Progress in this run is lost.", 32, UIKit.Text, TextAnchor.UpperCenter, false, true);
                if (UIKit.Button(new Rect(x, y + 140f, w, 110f), "LEAVE", BtnStyle.Danger)) { confirmQuit = false; app.QuitToHub(); }
                if (UIKit.Button(new Rect(x, y + 270f, w, 100f), "KEEP DRIVING", BtnStyle.Secondary)) confirmQuit = false;
                return;
            }

            if (UIKit.Button(new Rect(x, y, w, 120f), "RESUME", BtnStyle.Primary)) s.SetPaused(false);
            y += 140f;
            if (def.kind == RaceKind.Endless && s.Race.State == RaceState.Racing)
            {
                if (UIKit.Button(new Rect(x, y, w, 110f), "END RUN", BtnStyle.Gold))
                {
                    s.SetPaused(false);
                    s.Race.EndEndless();
                }
                y += 130f;
            }
            if (UIKit.Button(new Rect(x, y, w, 110f), "RESTART", BtnStyle.Secondary)) { s.Restart(); }
            y += 130f;
            if (UIKit.Button(new Rect(x, y, w, 110f), "SETTINGS", BtnStyle.Secondary)) showSettings = true;
            y += 130f;
            if (UIKit.Button(new Rect(x, y, w, 110f), def.id == Economy.FreeDriveId ? "EXIT" : "QUIT EVENT", BtnStyle.Danger))
            {
                if (def.id == Economy.FreeDriveId) app.QuitToHub();
                else confirmQuit = true;
            }
        }
    }
}
