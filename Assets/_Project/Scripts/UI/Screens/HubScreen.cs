using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    public enum HubTab { Story = 0, Events, Garage, Mail, Shop, More }

    /// <summary>
    /// The main screen between races: the 3D garage on top and a panel with six tabs below
    /// (Story, Events, Garage, Mail, Shop, More).
    /// </summary>
    public sealed class HubScreen : ScreenBase
    {
        HubTab tab = HubTab.Story;
        readonly Vector2[] scrolls = new Vector2[6];
        readonly GarageTab garage = new GarageTab();
        readonly ShopTab shop = new ShopTab();
        readonly SettingsPanel settings = new SettingsPanel();

        int fdTheme, fdTime, fdWeather = 0;
        bool orbitDrag;

        static readonly string[] TabNames = { "STORY", "EVENTS", "GARAGE", "MAIL", "SHOP", "MORE" };

        public override void OnEnter()
        {
            app.Stage.Show(true);
            app.RefreshStage();
            ApplyPose();
            if (AudioManager.I != null) AudioManager.I.SetMood(MusicMood.Menu);
            app.Input.Locked = true;
            garage.Reset(app);
            // after a story step, jump to the tab that matters
            if (tab == HubTab.Mail) app.MarkMailRead();
        }

        public override void OnExit()
        {
            app.MarkMailRead();
            app.SaveNow();
        }

        public void SelectTab(HubTab t)
        {
            if (tab == HubTab.Mail && t != HubTab.Mail) app.MarkMailRead();
            if (tab == HubTab.Garage && t != HubTab.Garage) app.RefreshStage();
            tab = t;
            ApplyPose();
            garage.Reset(app);
        }

        void ApplyPose()
        {
            app.Stage.SetPose(tab == HubTab.Garage ? StagePose.Garage : StagePose.Hub);
        }

        public override bool OnBack()
        {
            if (tab != HubTab.Story) { SelectTab(HubTab.Story); return true; }
            app.Go(Scr.Menu);
            return true;
        }

        // ------------------------------------------------------------------ frame

        public override void Draw()
        {
            Rect safe = SafeRect;
            SaveData save = app.Save;

            DrawTopBar(safe, save);

            float tabH = 128f;
            float tabY = safe.yMax - tabH;
            float panelTop = Mathf.Max(safe.y + 140f, UIKit.H * (tab == HubTab.Garage ? 0.45f : 0.36f));
            Rect panel = new Rect(14f, panelTop, UIKit.W - 28f, tabY - panelTop - 8f);

            // orbit the garage car by dragging the area above the panel
            if (tab == HubTab.Garage) HandleOrbit(new Rect(0f, safe.y + 130f, UIKit.W, panelTop - safe.y - 130f));

            UIKit.Card(panel);
            Rect view = new Rect(panel.x + 18f, panel.y + 18f, panel.width - 36f, panel.height - 36f);

            switch (tab)
            {
                case HubTab.Story: scrolls[0] = DrawStory(view, scrolls[0]); break;
                case HubTab.Events: scrolls[1] = DrawEvents(view, scrolls[1]); break;
                case HubTab.Garage: scrolls[2] = garage.Draw(app, view, scrolls[2]); break;
                case HubTab.Mail: scrolls[3] = DrawMail(view, scrolls[3]); break;
                case HubTab.Shop: scrolls[4] = shop.Draw(app, view, scrolls[4]); break;
                default: scrolls[5] = DrawMore(view, scrolls[5]); break;
            }

            DrawTabBar(new Rect(0f, tabY, UIKit.W, tabH), save);
        }

        void HandleOrbit(Rect area)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown && area.Contains(e.mousePosition)) orbitDrag = true;
            else if (e.type == EventType.MouseUp || e.type == EventType.MouseLeaveWindow) orbitDrag = false;
            else if (e.type == EventType.MouseDrag && orbitDrag) app.Stage.AddOrbit(-e.delta.x * 0.35f);
        }

        void DrawTopBar(Rect safe, SaveData save)
        {
            float y = safe.y + 14f;
            Rect left = new Rect(20f, y, 540f, 96f);
            UIKit.Tile(left, new Color(0.02f, 0.03f, 0.07f, 0.72f));
            float tx = left.x + 24f;
            Texture2D face = UIKit.Portrait("jun");
            if (face != null)
            {
                Rect fr = new Rect(left.x + 10f, left.y + 10f, 76f, 76f);
                UIKit.Image(fr, face, Color.white);
                UIKit.Outline(new Rect(fr.x - 3f, fr.y - 3f, fr.width + 6f, fr.height + 6f), new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, 0.8f));
                tx = fr.xMax + 22f;
            }
            UIKit.Label(new Rect(tx, left.y + 8f, left.xMax - tx - 16f, 46f), save.playerName, 36, UIKit.Text, TextAnchor.MiddleLeft, true);
            UIKit.Label(new Rect(tx, left.y + 52f, left.xMax - tx - 16f, 34f), "REP " + save.rep, 26, UIKit.Accent, TextAnchor.MiddleLeft, true);

            Rect right = new Rect(UIKit.W - 20f - 360f, y, 360f, 96f);
            UIKit.Tile(right, new Color(0.02f, 0.03f, 0.07f, 0.72f));
            UIKit.Circle(new Rect(right.x + 22f, right.y + 28f, 40f, 40f), UIKit.Gold);
            UIKit.Label(new Rect(right.x + 22f, right.y + 28f, 40f, 40f), "$", 26, new Color(0.25f, 0.17f, 0.02f), TextAnchor.MiddleCenter, true);
            UIKit.Label(new Rect(right.x + 70f, right.y, right.width - 94f, right.height), UIKit.Money(save.cash).Replace("$", ""), 42, UIKit.Gold, TextAnchor.MiddleRight, true);
        }

        void DrawTabBar(Rect r, SaveData save)
        {
            UIKit.Fill(r, new Color(0.03f, 0.035f, 0.08f, 0.97f));
            UIKit.Fill(new Rect(r.x, r.y, r.width, 2f), new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, 0.35f));
            float w = UIKit.W / 6f;
            for (int i = 0; i < 6; i++)
            {
                Rect tr = new Rect(i * w, r.y, w, r.height);
                bool sel = (int)tab == i;
                if (UIKit.Tap(tr))
                {
                    SelectTab((HubTab)i);
                    if (AudioManager.I != null) AudioManager.I.Play(Sfx.Click, 0.6f);
                }
                if (sel)
                {
                    UIKit.Fade(new Rect(tr.x + 6f, tr.y + 2f, tr.width - 12f, tr.height - 2f), new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, 0.30f), true);
                    UIKit.Fill(new Rect(tr.x + 16f, tr.y, tr.width - 32f, 6f), UIKit.Accent);
                }
                UIKit.Label(tr, TabNames[i], 25, sel ? UIKit.Text : UIKit.Dim, TextAnchor.MiddleCenter, true);

                // badges
                int badge = 0;
                if (i == (int)HubTab.Mail) badge = app.UnreadCount();
                bool dot = false;
                if (i == (int)HubTab.Story) { StoryStep s = app.CurrentStep; dot = s != null && app.StepAvailable(s); }
                if (i == (int)HubTab.Garage) dot = save.damage > 0.15f;
                if (badge > 0)
                {
                    UIKit.Circle(new Rect(tr.xMax - 62f, tr.y + 14f, 36f, 36f), UIKit.Bad);
                    UIKit.Label(new Rect(tr.xMax - 62f, tr.y + 14f, 36f, 36f), badge.ToString(), 22, Color.white, TextAnchor.MiddleCenter, true);
                }
                else if (dot) UIKit.Circle(new Rect(tr.xMax - 52f, tr.y + 18f, 22f, 22f), i == (int)HubTab.Garage ? UIKit.Bad : UIKit.Gold);
            }
        }

        // ------------------------------------------------------------------ story

        Vector2 DrawStory(Rect view, Vector2 scroll)
        {
            SaveData save = app.Save;
            StoryStep step = app.CurrentStep;
            float content = 900f + save.storyStep * 64f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float w = view.width;
            float y = -scroll.y;

            if (step == null)
            {
                UIKit.Label(new Rect(0f, y, w, 70f), "STORY COMPLETE", 52, UIKit.Gold, TextAnchor.UpperLeft, true);
                y += 80f;
                string t = "You finished the story. Legend Mode is open: endless runs on every road, and a new road every day.";
                float h = UIKit.TextHeight(t, 32, w);
                UIKit.Label(new Rect(0f, y, w, h), t, 32, UIKit.Dim, TextAnchor.UpperLeft, false, true);
                y += h + 30f;
                if (UIKit.Button(new Rect(0f, y, w, 120f), "OPEN EVENTS", BtnStyle.Primary)) SelectTab(HubTab.Events);
                y += 150f;
            }
            else
            {
                RaceDef rd = step.kind == StepKind.Race ? ContentDB.Race(step.refId) : null;
                Texture2D hero = RivalFace(rd);
                float tw = hero != null ? w - 270f : w;
                if (hero != null)
                {
                    Rect hr = new Rect(w - 250f, y + 4f, 240f, 240f);
                    UIKit.Image(hr, hero, Color.white);
                    UIKit.Outline(new Rect(hr.x - 4f, hr.y - 4f, hr.width + 8f, hr.height + 8f), new Color(1f, 1f, 1f, 0.3f));
                    RivalDef rvd = ContentDB.Rival(rd.rival);
                    if (rvd != null)
                    {
                        Rect nm = new Rect(hr.x, hr.yMax - 50f, hr.width, 50f);
                        UIKit.Fade(new Rect(hr.x, hr.yMax - 90f, hr.width, 90f), new Color(0f, 0f, 0.02f, 0.85f), false);
                        UIKit.Label(nm, rvd.name, 26, Color.white, TextAnchor.MiddleCenter, true);
                    }
                }
                UIKit.Label(new Rect(0f, y, tw, 40f), "CHAPTER " + step.chapter, 28, UIKit.Accent, TextAnchor.UpperLeft, true);
                y += 44f;
                UIKit.Label(new Rect(0f, y, tw, 70f), step.title, hero != null ? 44 : 50, UIKit.Text, TextAnchor.UpperLeft, true);
                y += 80f;

                string desc;
                if (rd != null) desc = rd.blurb + "\n" + RaceMeta(rd);
                else desc = "A story scene.";
                float dh = UIKit.TextHeight(desc, 30, tw);
                UIKit.Label(new Rect(0f, y, tw, dh), desc, 30, UIKit.Dim, TextAnchor.UpperLeft, false, true);
                y += dh + 20f;
                if (hero != null && y < 270f) y = 270f;

                if (rd != null)
                {
                    UIKit.Label(new Rect(0f, y, w, 40f), "Reward  " + UIKit.Money(rd.rewardCash) + "   +" + rd.rewardRep + " rep", 30, UIKit.Gold, TextAnchor.UpperLeft, true);
                    y += 56f;
                }

                bool avail = app.StepAvailable(step);
                if (avail)
                {
                    string label = step.kind == StepKind.Race ? "START RACE" : "CONTINUE STORY";
                    if (UIKit.Button(new Rect(0f, y, w, 130f), label, BtnStyle.Primary)) app.ContinueStory();
                    y += 150f;
                }
                else
                {
                    UIKit.Button(new Rect(0f, y, w, 130f), "LOCKED: " + step.minRep + " REP", BtnStyle.Secondary, false);
                    y += 146f;
                    float p = step.minRep > 0 ? Mathf.Clamp01(save.rep / (float)step.minRep) : 1f;
                    UIKit.Bar(new Rect(0f, y, w, 26f), p, UIKit.Accent, new Color(1f, 1f, 1f, 0.1f));
                    y += 34f;
                    UIKit.Label(new Rect(0f, y, w, 40f), "You have " + save.rep + " of " + step.minRep + " reputation.", 28, UIKit.Dim, TextAnchor.UpperLeft);
                    y += 50f;
                    if (!string.IsNullOrEmpty(step.hint))
                    {
                        UIKit.Label(new Rect(0f, y, w, 40f), step.hint, 28, UIKit.Dim, TextAnchor.UpperLeft);
                        y += 50f;
                    }
                    if (UIKit.Button(new Rect(0f, y, w, 100f), "GO TO EVENTS", BtnStyle.Gold)) SelectTab(HubTab.Events);
                    y += 130f;
                }
            }

            // finished steps
            if (save.storyStep > 0)
            {
                UIKit.Label(new Rect(0f, y, w, 40f), "THE STORY SO FAR", 26, UIKit.Dim, TextAnchor.UpperLeft, true);
                y += 48f;
                for (int i = Mathf.Min(save.storyStep, StoryContent.Steps.Count) - 1; i >= 0; i--)
                {
                    StoryStep s = StoryContent.Steps[i];
                    UIKit.Label(new Rect(0f, y, w, 50f), "DONE   " + s.title, 28, new Color(UIKit.Good.r, UIKit.Good.g, UIKit.Good.b, 0.85f), TextAnchor.UpperLeft);
                    y += 52f;
                }
            }

            UIKit.EndScroll();
            return scroll;
        }

        // ------------------------------------------------------------------ events

        static string KindLabel(RaceKind k)
        {
            switch (k)
            {
                case RaceKind.Sprint: return "Sprint";
                case RaceKind.TimeTrial: return "Time attack";
                case RaceKind.DriftScore: return "Drift score";
                case RaceKind.TrafficRun: return "Traffic run";
                case RaceKind.Chase: return "Chase";
                case RaceKind.Escape: return "Escape";
                case RaceKind.Endless: return "Endless";
                default: return "Tutorial";
            }
        }

        public static string RaceMeta(RaceDef d)
        {
            return KindLabel(d.kind) + "  |  " + Environments.Theme(d.theme).name + "  |  " + Environments.Time(d.time).name +
                   (d.weather != "clear" ? "  |  " + Environments.Weather(d.weather).name : "");
        }

        Vector2 DrawEvents(Rect view, Vector2 scroll)
        {
            SaveData save = app.Save;
            List<RaceDef> side = app.SideEvents();
            List<RaceDef> legend = app.LegendEvents();

            const float cardH = 196f, gap = 14f;
            float content = 80f + side.Count * (cardH + gap) + 560f + 80f + legend.Count * (cardH + gap) + 120f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float w = view.width;
            float y = -scroll.y;

            UIKit.Label(new Rect(0f, y, w, 50f), "SIDE EVENTS", 30, UIKit.Accent, TextAnchor.UpperLeft, true);
            y += 56f;
            for (int i = 0; i < side.Count; i++)
            {
                DrawEventCard(new Rect(0f, y, w, cardH), side[i], save);
                y += cardH + gap;
            }

            // free drive
            y += 10f;
            UIKit.Label(new Rect(0f, y, w, 50f), "FREE DRIVE", 30, UIKit.Accent, TextAnchor.UpperLeft, true);
            y += 56f;
            Rect fd = new Rect(0f, y, w, 480f);
            UIKit.Tile(fd, UIKit.PanelLight);
            UIKit.Label(new Rect(28f, y + 14f, w - 56f, 40f), "Drive anywhere, no timer. Pays nothing, costs nothing.", 26, UIKit.Dim, TextAnchor.UpperLeft);
            string[] themes = { "forest", "pass", "coast", "city" };
            string[] times = { "night", "midnight", "dusk", "dawn" };
            string[] weathers = { "clear", "rain", "fog", "storm" };
            if (UIKit.Button(new Rect(28f, y + 66f, w - 56f, 80f), "Road: " + Environments.Theme(themes[fdTheme]).name, BtnStyle.Secondary, true, 30)) fdTheme = (fdTheme + 1) % themes.Length;
            if (UIKit.Button(new Rect(28f, y + 160f, w - 56f, 80f), "Time: " + Environments.Time(times[fdTime]).name, BtnStyle.Secondary, true, 30)) fdTime = (fdTime + 1) % times.Length;
            if (UIKit.Button(new Rect(28f, y + 254f, w - 56f, 80f), "Weather: " + Environments.Weather(weathers[fdWeather]).name, BtnStyle.Secondary, true, 30)) fdWeather = (fdWeather + 1) % weathers.Length;
            if (UIKit.Button(new Rect(28f, y + 358f, w - 56f, 100f), "DRIVE", BtnStyle.Primary)) app.StartFreeDrive(themes[fdTheme], times[fdTime], weathers[fdWeather]);
            y += 480f + gap + 20f;

            // legend mode
            UIKit.Label(new Rect(0f, y, w, 50f), "LEGEND MODE", 30, UIKit.Gold, TextAnchor.UpperLeft, true);
            y += 56f;
            if (!save.HasFlag("legend_unlocked"))
            {
                Rect lk = new Rect(0f, y, w, 130f);
                UIKit.Tile(lk, UIKit.PanelLight);
                UIKit.Label(new Rect(28f, y, w - 56f, 130f), "Finish the story to unlock endless Ghost Runs and the Daily Run.", 28, UIKit.Dim, TextAnchor.MiddleLeft, false, true);
                y += 144f;
            }
            else
            {
                if (save.legendBestDistance > 0)
                {
                    UIKit.Label(new Rect(0f, y, w, 40f), "Best distance " + save.legendBestDistance + " m   |   Best score " + save.legendBestScore, 26, UIKit.Dim, TextAnchor.UpperLeft);
                    y += 46f;
                }
                for (int i = 0; i < legend.Count; i++)
                {
                    DrawEventCard(new Rect(0f, y, w, cardH), legend[i], save);
                    y += cardH + gap;
                }
            }

            UIKit.EndScroll();
            return scroll;
        }

        static Texture2D RivalFace(RaceDef d)
        {
            if (d == null || string.IsNullOrEmpty(d.rival)) return null;
            RivalDef rv = ContentDB.Rival(d.rival);
            return rv != null ? UIKit.Portrait(rv.character) : null;
        }

        void DrawEventCard(Rect r, RaceDef d, SaveData save)
        {
            string why;
            bool ok = app.RaceUnlocked(d, out why);
            UIKit.Tile(r, ok ? UIKit.PanelLight : new Color(0.07f, 0.08f, 0.14f, 0.95f));
            Color tc = ok ? UIKit.Text : UIKit.Dim;

            // the rival's portrait on the left of the card, when there is one
            float tx = r.x + 28f, tw = r.width - 260f;
            Texture2D face = RivalFace(d);
            if (face != null)
            {
                Rect fr = new Rect(r.x + 18f, r.y + 18f, r.height - 36f, r.height - 36f);
                UIKit.Image(fr, face, ok ? Color.white : new Color(0.55f, 0.55f, 0.6f, 1f));
                UIKit.Outline(new Rect(fr.x - 3f, fr.y - 3f, fr.width + 6f, fr.height + 6f), new Color(1f, 1f, 1f, ok ? 0.28f : 0.12f));
                tx = fr.xMax + 24f;
                tw -= fr.width + 0f;
            }
            UIKit.Label(new Rect(tx, r.y + 12f, tw, 44f), d.title, 36, tc, TextAnchor.UpperLeft, true);
            UIKit.Label(new Rect(tx, r.y + 58f, tw, 70f), d.blurb, 24, UIKit.Dim, TextAnchor.UpperLeft, false, true);
            UIKit.Label(new Rect(tx, r.y + 128f, tw, 30f), RaceMeta(d), 21, new Color(UIKit.Accent.r, UIKit.Accent.g, UIKit.Accent.b, ok ? 1f : 0.5f), TextAnchor.UpperLeft);

            string reward = d.kind == RaceKind.Endless ? "Score on the board" : UIKit.Money(d.rewardCash) + "  +" + d.rewardRep + " rep";
            UIKit.Label(new Rect(tx, r.y + 158f, tw, 30f), reward, 23, UIKit.Gold, TextAnchor.UpperLeft, true);

            BestEntry best = save.GetBest(d.id);
            if (best != null && best.medal > 0)
            {
                Color mc = best.medal == 3 ? UIKit.Gold : (best.medal == 2 ? new Color(0.78f, 0.80f, 0.86f) : new Color(0.78f, 0.52f, 0.30f));
                UIKit.Circle(new Rect(r.xMax - 226f, r.y + 20f, 44f, 44f), mc);
            }

            Rect btn = new Rect(r.xMax - 200f, r.y + r.height * 0.5f - 44f, 176f, 88f);
            if (ok)
            {
                if (UIKit.Button(btn, "RACE", BtnStyle.Primary, true, 30)) app.LaunchRace(d);
            }
            else
            {
                UIKit.Button(btn, "LOCKED", BtnStyle.Secondary, false, 26);
                UIKit.Label(new Rect(r.xMax - 260f, r.yMax - 52f, 240f, 30f), why, 21, UIKit.Dim, TextAnchor.MiddleRight);
            }
        }

        // ------------------------------------------------------------------ mail

        Vector2 DrawMail(Rect view, Vector2 scroll)
        {
            SaveData save = app.Save;
            List<MessageDef> list = app.VisibleMessages();
            float w = view.width;

            float content = 120f;
            for (int i = 0; i < list.Count; i++) content += UIKit.TextHeight(list[i].text, 28, w - 80f) + 110f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float y = -scroll.y;

            if (list.Count == 0)
            {
                UIKit.Label(new Rect(0f, y, w, 80f), "No messages yet.", 32, UIKit.Dim, TextAnchor.UpperLeft);
            }

            bool notebookHeader = false, messageHeader = false;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    MessageDef m = list[i];
                    bool isNote = m.id.StartsWith("n_");
                    if (isNote != (pass == 1)) continue;
                    if (pass == 0 && !messageHeader) { UIKit.Label(new Rect(0f, y, w, 40f), "MESSAGES", 26, UIKit.Accent, TextAnchor.UpperLeft, true); y += 48f; messageHeader = true; }
                    if (pass == 1 && !notebookHeader) { UIKit.Label(new Rect(0f, y + 10f, w, 40f), "GHOST'S NOTEBOOK", 26, UIKit.Gold, TextAnchor.UpperLeft, true); y += 58f; notebookHeader = true; }

                    float th = UIKit.TextHeight(m.text, 28, w - 80f);
                    Rect r = new Rect(0f, y, w, th + 90f);
                    UIKit.Tile(r, isNote ? new Color(0.17f, 0.14f, 0.09f, 0.96f) : UIKit.PanelLight);
                    bool unread = !save.readMessages.Contains(m.id);
                    UIKit.Label(new Rect(28f, y + 12f, w - 120f, 36f), m.from, 28, isNote ? UIKit.Gold : UIKit.Accent, TextAnchor.UpperLeft, true);
                    if (unread) UIKit.Circle(new Rect(w - 52f, y + 18f, 24f, 24f), UIKit.Bad);
                    UIKit.Label(new Rect(28f, y + 52f, w - 80f, th), m.text, 28, UIKit.Text, TextAnchor.UpperLeft, false, true);
                    y += th + 106f;
                }
            }

            UIKit.EndScroll();
            return scroll;
        }

        // ------------------------------------------------------------------ more (stats + settings)

        int moreSub;

        Vector2 DrawMore(Rect view, Vector2 scroll)
        {
            float w = view.width;
            float half = (w - 12f) * 0.5f;
            GUI.BeginGroup(view);
            if (UIKit.Button(new Rect(0f, 0f, half, 84f), "CAREER", moreSub == 0 ? BtnStyle.Primary : BtnStyle.Secondary, true, 28)) moreSub = 0;
            if (UIKit.Button(new Rect(half + 12f, 0f, half, 84f), "SETTINGS", moreSub == 1 ? BtnStyle.Primary : BtnStyle.Secondary, true, 28)) moreSub = 1;
            GUI.EndGroup();

            Rect body = new Rect(view.x, view.y + 100f, view.width, view.height - 100f);
            if (moreSub == 1) return settings.Draw(app, body, scroll, false);

            LifetimeStats l = app.Save.lifetime;
            scroll = UIKit.BeginScroll(body, scroll, 640f);
            float y = -scroll.y;
            string[] k = { "Races", "Wins", "Distance", "Top speed", "Near misses", "Clean corners", "Best drift", "Cash earned" };
            string[] v =
            {
                l.races.ToString(), l.wins.ToString(), (l.distance / 1000f).ToString("0.0") + " km", Mathf.RoundToInt(l.topSpeedKph) + " km/h",
                l.nearMisses.ToString(), l.cleanCorners.ToString(), l.bestDrift.ToString(), UIKit.Money(l.cashEarned)
            };
            for (int i = 0; i < k.Length; i++)
            {
                int col = i % 2, row = i / 2;
                Rect c = new Rect(col * (half + 12f), y + row * 104f, half, 92f);
                UIKit.Tile(c, UIKit.PanelLight);
                UIKit.Label(new Rect(c.x + 20f, c.y + 6f, c.width - 40f, 34f), k[i], 24, UIKit.Dim, TextAnchor.UpperLeft);
                UIKit.Label(new Rect(c.x + 20f, c.y + 38f, c.width - 40f, 46f), v[i], 36, UIKit.Text, TextAnchor.UpperLeft, true);
            }
            y += 4 * 104f + 20f;
            UIKit.Label(new Rect(0f, y, w, 40f), "Midnight Legacy v" + AppRoot.Version + "  |  ST Media", 24, UIKit.Dim, TextAnchor.UpperLeft);
            UIKit.EndScroll();
            return scroll;
        }
    }
}
