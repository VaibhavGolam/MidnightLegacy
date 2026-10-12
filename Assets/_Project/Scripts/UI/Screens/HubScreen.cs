using System.Collections.Generic;
using UnityEngine;

namespace MidnightLegacy
{
    public enum HubTab { Story = 0, Events, Garage, Mail, Shop, More }

    /// <summary>
    /// The main screen between races: the garage on top and a glowing panel with six tabs below
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
        static readonly string[] TabIcons = { "tab_story", "tab_events", "tab_garage", "tab_mail", "tab_shop", "tab_more" };
        static readonly string[] CharacterIds = { "sato", "mia", "rook", "dex", "ivy", "rina", "ghost", "jun" };

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

            float tabH = 132f;
            float tabY = safe.yMax - tabH;
            float panelTop = Mathf.Max(safe.y + 140f, UIKit.H * (tab == HubTab.Garage ? 0.45f : 0.36f));
            Rect panel = new Rect(14f, panelTop, UIKit.W - 28f, tabY - panelTop - 10f);

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

        // ------------------------------------------------------------------ header

        void DrawTopBar(Rect safe, SaveData save)
        {
            float y = safe.y + 14f;
            Color plate = new Color(0.02f, 0.03f, 0.08f, 0.78f);

            // player: portrait, name, rep
            Rect left = new Rect(20f, y, 520f, 100f);
            UIKit.Tile(left, plate);
            float tx = left.x + 24f;
            Texture2D face = UIKit.Portrait("jun");
            if (face != null)
            {
                Rect fr = new Rect(left.x + 10f, left.y + 10f, 80f, 80f);
                UIKit.Image(fr, face, Color.white);
                UIKit.Outline(new Rect(fr.x - 2f, fr.y - 2f, fr.width + 4f, fr.height + 4f), new Color(UIKit.EdgeBlue.r, UIKit.EdgeBlue.g, UIKit.EdgeBlue.b, 0.9f));
                tx = fr.xMax + 22f;
            }
            UIKit.Label(new Rect(tx, left.y + 8f, left.xMax - tx - 16f, 48f), save.playerName, 38, UIKit.Text, TextAnchor.MiddleLeft, true);
            UIKit.Label(new Rect(tx, left.y + 54f, left.xMax - tx - 16f, 34f), "REP " + save.rep, 27, new Color(0.45f, 0.58f, 1f), TextAnchor.MiddleLeft, true);

            // cash and a plus button that opens the shop
            Rect right = new Rect(UIKit.W - 20f - 380f, y + 6f, 380f, 88f);
            UIKit.Tile(right, plate);
            UIKit.Coin(new Rect(right.x + 18f, right.y + 16f, 56f, 56f));
            UIKit.Label(new Rect(right.x + 80f, right.y, right.width - 80f - 96f, right.height), UIKit.Money(save.cash).Replace("$", ""), 46, UIKit.Gold, TextAnchor.MiddleRight, true);
            Rect plus = new Rect(right.xMax - 80f, right.y + 14f, 62f, 60f);
            UIKit.Tile(plus, new Color(0.08f, 0.11f, 0.24f, 0.95f));
            UIKit.Label(plus, "+", 44, UIKit.Text, TextAnchor.MiddleCenter, true);
            if (UIKit.Tap(plus)) SelectTab(HubTab.Shop);
        }

        // ------------------------------------------------------------------ tab bar

        void DrawTabBar(Rect r, SaveData save)
        {
            UIKit.Fill(r, new Color(0.025f, 0.03f, 0.075f, 0.98f));
            UIKit.Fill(new Rect(r.x, r.y, r.width, 2f), new Color(UIKit.EdgeBlue.r, UIKit.EdgeBlue.g, UIKit.EdgeBlue.b, 0.45f));
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
                if (i > 0) UIKit.Fill(new Rect(tr.x, tr.y + 24f, 1f, tr.height - 48f), new Color(1f, 1f, 1f, 0.06f));
                if (sel)
                {
                    UIKit.Fade(new Rect(tr.x + 4f, tr.y + 2f, tr.width - 8f, tr.height - 2f), new Color(UIKit.EdgeBlue.r, UIKit.EdgeBlue.g, UIKit.EdgeBlue.b, 0.30f), true);
                    UIKit.Fill(new Rect(tr.x + 20f, tr.y, tr.width - 40f, 6f), new Color(0.45f, 0.62f, 1f));
                    UIKit.Fill(new Rect(tr.x + 40f, tr.yMax - 14f, tr.width - 80f, 4f), new Color(0.75f, 0.85f, 1f, 0.9f));
                }
                Color ic = sel ? Color.white : new Color(0.50f, 0.58f, 0.92f, 0.95f);
                UIIcons.Draw(TabIcons[i], new Rect(tr.center.x - 30f, tr.y + 18f, 60f, 60f), ic);
                UIKit.Label(new Rect(tr.x, tr.y + 76f, tr.width, 40f), TabNames[i], 25, sel ? Color.white : new Color(0.62f, 0.68f, 0.90f), TextAnchor.MiddleCenter, true);

                // badges
                int badge = 0;
                if (i == (int)HubTab.Mail) badge = app.UnreadCount();
                bool dot = false;
                if (i == (int)HubTab.Story) { StoryStep s = app.CurrentStep; dot = s != null && app.StepAvailable(s); }
                if (i == (int)HubTab.Garage) dot = save.damage > 0.15f;
                if (badge > 0)
                {
                    UIKit.Circle(new Rect(tr.xMax - 62f, tr.y + 10f, 36f, 36f), UIKit.Bad);
                    UIKit.Label(new Rect(tr.xMax - 62f, tr.y + 10f, 36f, 36f), badge.ToString(), 22, Color.white, TextAnchor.MiddleCenter, true);
                }
                else if (dot) UIKit.Circle(new Rect(tr.xMax - 52f, tr.y + 14f, 22f, 22f), i == (int)HubTab.Garage ? UIKit.Bad : UIKit.Gold);
            }
        }

        // ------------------------------------------------------------------ shared pieces

        static Color ThemeTop(string theme)
        {
            switch (theme)
            {
                case "pass": return new Color(0.34f, 0.22f, 0.50f);
                case "coast": return new Color(0.95f, 0.42f, 0.30f);
                case "city": return new Color(0.16f, 0.30f, 0.52f);
                default: return new Color(0.16f, 0.26f, 0.46f);
            }
        }

        /// <summary>
        /// A picture of the road for an event, filling r and fading out toward the left so text can sit on it.
        /// Uses Resources/Art/thumb_&lt;theme&gt; when it exists, otherwise a simple painted gradient.
        /// </summary>
        static void DrawThumb(Rect r, string theme, Color cardColor, float dim)
        {
            Texture2D t = UIKit.Art("thumb_" + theme);
            if (t != null)
                UIKit.Cover(r, t, t.width / Mathf.Max(1f, (float)t.height), new Color(dim, dim, dim, 1f));
            else
            {
                Color top = ThemeTop(theme);
                UIKit.Fill(r, new Color(0.03f, 0.04f, 0.10f, 1f));
                UIKit.Fade(r, new Color(top.r * dim, top.g * dim, top.b * dim, 1f), true);
                UIKit.Fill(new Rect(r.x, r.yMax - r.height * 0.30f, r.width, r.height * 0.30f), new Color(0.02f, 0.03f, 0.07f, 0.9f));
            }
            UIKit.FadeH(new Rect(r.x - 1f, r.y, r.width * 0.78f, r.height), new Color(cardColor.r, cardColor.g, cardColor.b, 1f), true);
        }

        static void RewardPill(Rect r, int cash, int rep)
        {
            UIKit.Tile(r, new Color(0.02f, 0.03f, 0.08f, 0.75f));
            UIKit.Coin(new Rect(r.x + 12f, r.center.y - 20f, 40f, 40f));
            UIKit.Label(new Rect(r.x + 64f, r.y, 130f, r.height), "Reward", 24, UIKit.Dim, TextAnchor.MiddleLeft);
            string money = UIKit.Money(cash);
            float mw = UIKit.TextWidth(money, 32, true);
            UIKit.Label(new Rect(r.x + 180f, r.y, mw + 10f, r.height), money, 32, UIKit.Gold, TextAnchor.MiddleLeft, true);
            float bx = r.x + 180f + mw + 24f;
            UIKit.Fill(new Rect(bx, r.y + 12f, 2f, r.height - 24f), new Color(1f, 1f, 1f, 0.2f));
            UIKit.Label(new Rect(bx + 16f, r.y, 220f, r.height), "+" + rep + " rep", 30, UIKit.Gold, TextAnchor.MiddleLeft, true);
        }

        // ------------------------------------------------------------------ story

        Vector2 DrawStory(Rect view, Vector2 scroll)
        {
            SaveData save = app.Save;
            StoryStep step = app.CurrentStep;
            float content = 1000f + save.storyStep * 104f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float w = view.width;
            float y = -scroll.y;

            if (step == null)
            {
                UIKit.Label(new Rect(0f, y, w, 70f), "STORY COMPLETE", 54, UIKit.Gold, TextAnchor.UpperLeft, true);
                y += 84f;
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

                // the road behind the top right of the chapter card
                float heroH = 380f;
                Rect hero = new Rect(0f, y, w, heroH);
                if (rd != null) DrawThumb(new Rect(w * 0.30f, y, w * 0.70f, heroH * 0.80f), rd.theme, new Color(0.075f, 0.085f, 0.17f), 0.85f);

                UIKit.SectionHeader(new Rect(4f, y + 4f, w * 0.62f, 40f), "CHAPTER " + step.chapter, new Color(0.50f, 0.60f, 1f));
                UIKit.Label(new Rect(4f, y + 50f, w, 90f), step.title, 66, UIKit.Text, TextAnchor.UpperLeft, true);
                string desc = rd != null ? rd.blurb : "A story scene.";
                float dh = UIKit.TextHeight(desc, 32, w * 0.62f);
                UIKit.Label(new Rect(4f, y + 148f, w * 0.62f, dh), desc, 32, new Color(0.78f, 0.82f, 0.96f), TextAnchor.UpperLeft, false, true);
                if (rd != null)
                    UIKit.Label(new Rect(4f, y + 150f + dh + 6f, w, 34f), RaceMeta(rd), 26, new Color(0.50f, 0.62f, 1f), TextAnchor.UpperLeft);
                y += heroH - 90f;

                if (rd != null)
                {
                    RewardPill(new Rect(0f, y, w, 84f), rd.rewardCash, rd.rewardRep);
                    y += 104f;
                }

                bool avail = app.StepAvailable(step);
                if (avail)
                {
                    string label = step.kind == StepKind.Race ? "START RACE" : "CONTINUE STORY";
                    Rect b = new Rect(0f, y, w, 130f);
                    if (UIKit.Button(b, label, BtnStyle.Primary, true, 48)) app.ContinueStory();
                    UIIcons.Draw("tab_events", new Rect(b.x + 44f, b.center.y - 36f, 72f, 72f), new Color(1f, 1f, 1f, 0.95f));
                    UIKit.Label(new Rect(b.xMax - 100f, b.y, 70f, b.height), ">", 56, Color.white, TextAnchor.MiddleCenter, true);
                    y += 154f;
                }
                else
                {
                    UIKit.Button(new Rect(0f, y, w, 130f), "LOCKED: " + step.minRep + " REP", BtnStyle.Secondary, false);
                    y += 146f;
                    float p = step.minRep > 0 ? Mathf.Clamp01(save.rep / (float)step.minRep) : 1f;
                    UIKit.SegBar(new Rect(0f, y, w, 26f), p, 20, new Color(0.35f, 0.55f, 1f), new Color(1f, 1f, 1f, 0.10f));
                    y += 38f;
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
                y += 6f;
                UIKit.SectionHeader(new Rect(4f, y, w, 40f), "THE STORY SO FAR", new Color(0.62f, 0.70f, 1f));
                y += 56f;
                for (int i = Mathf.Min(save.storyStep, StoryContent.Steps.Count) - 1; i >= 0; i--)
                {
                    StoryStep s = StoryContent.Steps[i];
                    Rect row = new Rect(0f, y, w, 88f);
                    UIKit.Tile(row, new Color(0.04f, 0.06f, 0.14f, 0.85f));
                    UIIcons.Draw("check", new Rect(row.x + 20f, row.center.y - 20f, 40f, 40f), UIKit.Good);
                    UIKit.Label(new Rect(row.x + 74f, row.y, 130f, row.height), "DONE", 30, UIKit.Good, TextAnchor.MiddleLeft, true);
                    UIKit.Fill(new Rect(row.x + 206f, row.y + 20f, 2f, row.height - 40f), new Color(1f, 1f, 1f, 0.15f));
                    UIKit.Label(new Rect(row.x + 230f, row.y, row.width - 330f, row.height), s.title, 32, UIKit.Text, TextAnchor.MiddleLeft);
                    UIKit.Slashes(new Rect(row.xMax - 76f, row.y + 18f, 60f, row.height - 36f), new Color(0.35f, 0.5f, 1f, 0.5f));
                    y += 102f;
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

            const float cardH = 222f, gap = 16f;
            float content = 80f + side.Count * (cardH + gap) + 600f + 80f + legend.Count * (cardH + gap) + 160f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float w = view.width;
            float y = -scroll.y;

            UIKit.SectionHeader(new Rect(4f, y, w, 44f), "SIDE EVENTS", new Color(0.50f, 0.60f, 1f));
            y += 60f;
            bool firstOpen = true;
            for (int i = 0; i < side.Count; i++)
            {
                string why0;
                bool open = app.RaceUnlocked(side[i], out why0);
                DrawEventCard(new Rect(0f, y, w, cardH), side[i], save, open && firstOpen);
                if (open) firstOpen = false;
                y += cardH + gap;
            }

            // free drive
            y += 10f;
            UIKit.SectionHeader(new Rect(4f, y, w, 44f), "FREE DRIVE", new Color(0.50f, 0.60f, 1f));
            y += 60f;
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
            UIKit.SectionHeader(new Rect(4f, y, w, 44f), "LEGEND MODE", UIKit.Gold);
            y += 60f;
            if (!save.HasFlag("legend_unlocked"))
            {
                Rect lk = new Rect(0f, y, w, 130f);
                UIKit.Tile(lk, UIKit.PanelLight);
                UIIcons.Draw("lock", new Rect(26f, y + 36f, 58f, 58f), UIKit.Dim);
                UIKit.Label(new Rect(110f, y, w - 140f, 130f), "Finish the story to unlock endless Ghost Runs and the Daily Run.", 28, UIKit.Dim, TextAnchor.MiddleLeft, false, true);
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
                    DrawEventCard(new Rect(0f, y, w, cardH), legend[i], save, false);
                    y += cardH + gap;
                }
            }

            UIKit.EndScroll();
            return scroll;
        }

        void DrawEventCard(Rect r, RaceDef d, SaveData save, bool highlight)
        {
            string why;
            bool ok = app.RaceUnlocked(d, out why);
            Color card = ok ? new Color(0.075f, 0.09f, 0.20f, 0.97f) : new Color(0.05f, 0.06f, 0.13f, 0.97f);

            if (highlight) UIKit.Glow(r, UIKit.EdgeBlue, 0.30f);
            UIKit.Round(r, card);
            DrawThumb(new Rect(r.x + r.width * 0.30f, r.y, r.width * 0.70f, r.height), d.theme, card, ok ? 0.95f : 0.50f);
            UIKit.Outline(r, highlight ? new Color(0.55f, 0.72f, 1f, 0.95f) : new Color(UIKit.EdgeBlue.r, UIKit.EdgeBlue.g, UIKit.EdgeBlue.b, ok ? 0.45f : 0.30f));

            Color tc = ok ? UIKit.Text : new Color(0.80f, 0.83f, 0.95f);
            float tx = r.x + 24f, tw = r.width - 280f;
            UIKit.Label(new Rect(tx, r.y + 8f, tw, 52f), d.title, 42, tc, TextAnchor.UpperLeft, true);
            UIKit.Label(new Rect(tx, r.y + 62f, tw, 34f), d.blurb, 24, new Color(0.82f, 0.86f, 0.98f), TextAnchor.UpperLeft);
            UIKit.Label(new Rect(tx, r.y + 98f, tw, 30f), RaceMeta(d), 21, new Color(0.50f, 0.62f, 1f, ok ? 1f : 0.65f), TextAnchor.UpperLeft);

            // reward line
            Rect rw = new Rect(tx - 8f, r.y + 134f, Mathf.Min(tw, 470f), 66f);
            UIKit.Tile(rw, new Color(0.02f, 0.03f, 0.08f, 0.80f));
            UIKit.Coin(new Rect(rw.x + 10f, rw.center.y - 18f, 36f, 36f));
            if (d.kind == RaceKind.Endless)
                UIKit.Label(new Rect(rw.x + 58f, rw.y, rw.width - 66f, rw.height), "Score on the board", 25, UIKit.Gold, TextAnchor.MiddleLeft, true);
            else
            {
                UIKit.Label(new Rect(rw.x + 58f, rw.y, 110f, rw.height), "Reward", 22, UIKit.Dim, TextAnchor.MiddleLeft);
                string money = UIKit.Money(d.rewardCash);
                float mw = UIKit.TextWidth(money, 29, true);
                UIKit.Label(new Rect(rw.x + 150f, rw.y, mw + 8f, rw.height), money, 29, UIKit.Gold, TextAnchor.MiddleLeft, true);
                float bx = rw.x + 150f + mw + 18f;
                UIKit.Fill(new Rect(bx, rw.y + 12f, 2f, rw.height - 24f), new Color(1f, 1f, 1f, 0.2f));
                UIKit.Label(new Rect(bx + 14f, rw.y, 160f, rw.height), "+" + d.rewardRep + " rep", 27, UIKit.Gold, TextAnchor.MiddleLeft, true);
            }

            BestEntry best = save.GetBest(d.id);
            if (best != null && best.medal > 0)
            {
                Color mc = best.medal == 3 ? UIKit.Gold : (best.medal == 2 ? new Color(0.78f, 0.80f, 0.86f) : new Color(0.78f, 0.52f, 0.30f));
                Rect mr = new Rect(r.xMax - 240f, r.y + 14f, 40f, 40f);
                if (!UIKit.Icon(mr, "ui_medal", mc)) UIKit.Circle(mr, mc);
            }

            Rect btn = new Rect(r.xMax - 250f, r.y + 76f, 232f, 82f);
            if (ok)
            {
                if (UIKit.Button(btn, "RACE", BtnStyle.Primary, true, 38)) app.LaunchRace(d);
                UIIcons.Draw("tab_events", new Rect(btn.x + 22f, btn.center.y - 22f, 44f, 44f), Color.white);
                UIKit.Label(new Rect(btn.xMax - 56f, btn.y, 40f, btn.height), ">", 40, Color.white, TextAnchor.MiddleCenter, true);
            }
            else
            {
                UIKit.Tile(btn, new Color(0.03f, 0.04f, 0.10f, 0.92f));
                UIIcons.Draw("lock", new Rect(btn.x + 18f, btn.center.y - 20f, 40f, 40f), UIKit.Dim);
                UIKit.Label(new Rect(btn.x + 64f, btn.y, btn.width - 70f, btn.height), "LOCKED", 28, new Color(0.80f, 0.84f, 1f), TextAnchor.MiddleLeft, true);
                UIKit.Label(new Rect(btn.x - 10f, btn.yMax + 6f, btn.width + 20f, 32f), why, 22, UIKit.Dim, TextAnchor.MiddleCenter);
            }
        }

        // ------------------------------------------------------------------ mail

        static string PortraitFor(string from)
        {
            if (string.IsNullOrEmpty(from)) return null;
            string f = from.ToLowerInvariant();
            for (int i = 0; i < CharacterIds.Length; i++)
                if (f.Contains(CharacterIds[i]) && UIKit.Portrait(CharacterIds[i]) != null) return CharacterIds[i];
            return null;
        }

        Vector2 DrawMail(Rect view, Vector2 scroll)
        {
            SaveData save = app.Save;
            List<MessageDef> list = app.VisibleMessages();
            float w = view.width;

            float content = 160f;
            for (int i = 0; i < list.Count; i++) content += Mathf.Max(210f, UIKit.TextHeight(list[i].text, 30, w - 330f) + 130f) + 30f;
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
                    if (pass == 0 && !messageHeader)
                    {
                        UIIcons.Draw("tab_mail", new Rect(8f, y + 2f, 52f, 44f), new Color(0.85f, 0.9f, 1f));
                        UIKit.SectionHeader(new Rect(76f, y, w - 76f, 48f), "MESSAGES", new Color(0.62f, 0.70f, 1f));
                        y += 62f; messageHeader = true;
                    }
                    if (pass == 1 && !notebookHeader)
                    {
                        y += 14f;
                        UIIcons.Draw("tab_story", new Rect(8f, y + 2f, 52f, 44f), UIKit.Gold);
                        UIKit.SectionHeader(new Rect(76f, y, w - 76f, 48f), "GHOST'S NOTEBOOK", UIKit.Gold);
                        y += 62f; notebookHeader = true;
                    }

                    bool unread = !save.readMessages.Contains(m.id);
                    float textW = w - 330f;
                    float th = UIKit.TextHeight(m.text, 30, textW);
                    float h = Mathf.Max(210f, th + 130f);
                    Rect r = new Rect(0f, y, w, h);
                    Color edge = isNote ? new Color(0.95f, 0.72f, 0.25f) : UIKit.EdgeBlue;
                    if (unread && !isNote) UIKit.Glow(r, edge, 0.28f);
                    UIKit.Round(r, isNote ? new Color(0.11f, 0.10f, 0.12f, 0.97f) : new Color(0.07f, 0.09f, 0.20f, 0.97f));
                    UIKit.Outline(r, new Color(edge.r, edge.g, edge.b, isNote ? 0.75f : (unread ? 0.95f : 0.45f)));

                    // picture: the sender's portrait, or a notebook
                    Rect pic = new Rect(r.x + 20f, r.y + 20f, 190f, Mathf.Min(h - 40f, 190f));
                    string pid = isNote ? null : PortraitFor(m.from);
                    if (pid != null)
                    {
                        UIKit.Image(pic, UIKit.Portrait(pid), Color.white);
                        UIKit.Outline(new Rect(pic.x - 3f, pic.y - 3f, pic.width + 6f, pic.height + 6f), new Color(edge.r, edge.g, edge.b, 0.9f));
                    }
                    else if (isNote)
                    {
                        if (!UIKit.Icon(pic, "ui_notebook", Color.white))
                        {
                            UIKit.Round(new Rect(pic.x + 24f, pic.y + 4f, pic.width - 40f, pic.height - 8f), new Color(0.30f, 0.18f, 0.10f));
                            UIKit.Outline(new Rect(pic.x + 24f, pic.y + 4f, pic.width - 40f, pic.height - 8f), new Color(0.85f, 0.62f, 0.25f, 0.8f));
                            for (int k = 0; k < 6; k++) UIKit.Circle(new Rect(pic.x + 12f, pic.y + 16f + k * 26f, 22f, 22f), new Color(0.75f, 0.7f, 0.65f));
                        }
                    }
                    else
                    {
                        UIKit.Tile(pic, new Color(0.04f, 0.06f, 0.14f, 0.9f));
                        UIIcons.Draw("tab_mail", new Rect(pic.x + 40f, pic.y + 50f, pic.width - 80f, pic.height - 100f), UIKit.Dim);
                    }
                    if (unread && !isNote)
                    {
                        Rect badge = new Rect(r.x + 12f, r.y + 12f, 46f, 42f);
                        UIKit.Round(badge, new Color(0.20f, 0.40f, 0.96f));
                        UIIcons.Draw("tab_mail", new Rect(badge.x + 8f, badge.y + 6f, badge.width - 16f, badge.height - 12f), Color.white);
                    }

                    float tx = r.x + 236f;
                    Color nameC = isNote ? UIKit.Gold : UIKit.Text;
                    UIKit.Label(new Rect(tx, r.y + 14f, r.width - 236f - 190f, 52f), m.from, 42, nameC, TextAnchor.MiddleLeft, true);
                    UIKit.Label(new Rect(r.xMax - 210f, r.y + 14f, 190f, 52f), isNote ? "Page" : "Just now", 24, new Color(0.55f, 0.62f, 0.95f), TextAnchor.MiddleRight);
                    UIKit.Fill(new Rect(tx, r.y + 72f, r.width - 236f - 24f, 2f), new Color(edge.r, edge.g, edge.b, 0.35f));
                    UIKit.Label(new Rect(tx, r.y + 84f, textW, th), m.text, 30, new Color(0.90f, 0.92f, 1f), TextAnchor.UpperLeft, false, true);
                    y += h + 30f;
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
            float half = (w - 14f) * 0.5f;

            Rect career = new Rect(view.x, view.y, half, 92f);
            Rect sett = new Rect(view.x + half + 14f, view.y, half, 92f);
            if (UIKit.Button(career, "CAREER", moreSub == 0 ? BtnStyle.Primary : BtnStyle.Secondary, true, 36)) moreSub = 0;
            if (UIKit.Button(sett, "SETTINGS", moreSub == 1 ? BtnStyle.Primary : BtnStyle.Secondary, true, 36)) moreSub = 1;
            UIIcons.Draw("tab_events", new Rect(career.x + 40f, career.center.y - 28f, 56f, 56f), Color.white);
            UIIcons.Draw("gear", new Rect(sett.x + 40f, sett.center.y - 26f, 52f, 52f), moreSub == 1 ? Color.white : new Color(0.55f, 0.65f, 1f));

            Rect body = new Rect(view.x, view.y + 112f, view.width, view.height - 112f);
            if (moreSub == 1) return settings.Draw(app, body, scroll, false);

            LifetimeStats l = app.Save.lifetime;
            scroll = UIKit.BeginScroll(body, scroll, 4 * 166f + 150f);
            float y = -scroll.y;
            string[] k = { "Races", "Wins", "Distance", "Top speed", "Near misses", "Clean corners", "Best drift", "Cash earned" };
            string[] icons = { "stat_races", "stat_wins", "stat_distance", "stat_speed", "stat_near", "stat_corners", "stat_drift", "stat_cash" };
            string[] v =
            {
                l.races.ToString(), l.wins.ToString(), (l.distance / 1000f).ToString("0.0") + " km", Mathf.RoundToInt(l.topSpeedKph) + " km/h",
                l.nearMisses.ToString(), l.cleanCorners.ToString(), l.bestDrift.ToString(), UIKit.Money(l.cashEarned)
            };
            for (int i = 0; i < k.Length; i++)
            {
                int col = i % 2, row = i / 2;
                Rect c = new Rect(col * (half + 14f), y + row * 166f, half, 150f);
                UIKit.Tile(c, new Color(0.05f, 0.07f, 0.16f, 0.92f));
                UIIcons.Draw(icons[i], new Rect(c.x + 22f, c.center.y - 32f, 64f, 64f), new Color(0.50f, 0.64f, 1f));
                UIKit.Fill(new Rect(c.x + 106f, c.y + 24f, 2f, c.height - 48f), new Color(1f, 1f, 1f, 0.18f));
                UIKit.Label(new Rect(c.x + 126f, c.y + 14f, c.width - 140f, 40f), k[i], 27, UIKit.Dim, TextAnchor.UpperLeft);
                UIKit.Label(new Rect(c.x + 126f, c.y + 52f, c.width - 140f, 80f), v[i], 58, UIKit.Text, TextAnchor.UpperLeft, true);
            }
            y += 4 * 166f + 14f;
            UIKit.Fill(new Rect(0f, y, w, 2f), new Color(UIKit.EdgeBlue.r, UIKit.EdgeBlue.g, UIKit.EdgeBlue.b, 0.35f));
            UIKit.Label(new Rect(4f, y + 12f, w, 40f), "Midnight Legacy v" + AppRoot.Version + "   |   ST Media", 25, UIKit.Dim, TextAnchor.UpperLeft);
            UIKit.EndScroll();
            return scroll;
        }
    }
}
