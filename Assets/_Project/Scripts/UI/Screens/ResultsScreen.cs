using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>Shown after a race: medal, rewards, and the optional rewarded ads (double bonus, second chance).</summary>
    public sealed class ResultsScreen : ScreenBase
    {
        bool doubled;
        float t;
        Vector2 scroll;

        public override void OnEnter()
        {
            doubled = false;
            t = 0f;
            scroll = Vector2.zero;
            app.Stage.Show(false);
            RaceResult r = app.PendingResult;
            if (AudioManager.I != null) AudioManager.I.SetMood(r != null && r.won ? MusicMood.Calm : MusicMood.Tense);
        }

        public override bool OnBack()
        {
            return true;   // the buttons decide, back does nothing here
        }

        public override void Draw()
        {
            RaceResult r = app.PendingResult;
            if (r == null) return;
            Rect safe = SafeRect;
            if (Event.current.type == EventType.Repaint) t += Time.unscaledDeltaTime;

            UIKit.Fill(new Rect(0f, 0f, UIKit.W, UIKit.H), new Color(0.01f, 0.015f, 0.05f, 0.82f));
            // a glow behind the title: gold for a win, red for a fail
            Color glow = r.won ? UIKit.Gold : UIKit.Bad;
            UIKit.Fade(new Rect(0f, 0f, UIKit.W, UIKit.H * 0.36f), new Color(glow.r, glow.g, glow.b, 0.22f * Mathf.Clamp01(t * 3f)), true);

            float w = UIKit.W - 80f, x = 40f, y = safe.y + 50f;
            bool endless = r.def.kind == RaceKind.Endless;

            string title = r.won ? (endless ? "RUN OVER" : "VICTORY") : "FAILED";
            Color tc = r.won ? UIKit.Gold : UIKit.Bad;
            UIKit.Label(new Rect(x, y, w, 110f), title, 96, tc, TextAnchor.MiddleCenter, true);
            y += 110f;
            string sub = r.def.title;
            if (!r.won && !string.IsNullOrEmpty(r.failReason)) sub += "  |  " + r.failReason;
            UIKit.Label(new Rect(x, y, w, 44f), sub, 30, UIKit.Dim, TextAnchor.MiddleCenter);
            y += 60f;

            // the rival, when the event had one
            RivalDef rival = string.IsNullOrEmpty(r.def.rival) ? null : ContentDB.Rival(r.def.rival);
            Texture2D face = rival != null ? UIKit.Portrait(rival.character) : null;
            if (face != null)
            {
                Rect fr = new Rect(UIKit.W * 0.5f - 60f, y, 120f, 120f);
                UIKit.Image(fr, face, r.won ? Color.white : new Color(0.7f, 0.7f, 0.75f, 1f));
                UIKit.Outline(new Rect(fr.x - 4f, fr.y - 4f, fr.width + 8f, fr.height + 8f), new Color(1f, 1f, 1f, 0.35f));
                y += 134f;
                UIKit.Label(new Rect(x, y, w, 36f), (r.won ? "beat " : "lost to ") + rival.name, 26, UIKit.Dim, TextAnchor.MiddleCenter);
                y += 46f;
            }

            // medals
            if (r.won && !endless)
            {
                for (int i = 0; i < 3; i++)
                {
                    bool got = i < r.medal;
                    Color mc = i == 2 ? UIKit.Gold : (i == 1 ? new Color(0.8f, 0.82f, 0.88f) : new Color(0.8f, 0.55f, 0.32f));
                    float pop = Mathf.Clamp01((t - 0.25f - i * 0.25f) * 4f);
                    float s = 96f * (got ? pop : 1f);
                    Rect mr = new Rect(UIKit.W * 0.5f - 160f + i * 112f + (96f - s) * 0.5f, y + (96f - s) * 0.5f, s, s);
                    if (!UIKit.Icon(mr, "ui_medal", got ? mc : new Color(1f, 1f, 1f, 0.12f)))
                        UIKit.Circle(mr, got ? mc : new Color(1f, 1f, 1f, 0.1f));
                }
                y += 116f;
            }

            // headline numbers
            string main;
            if (endless) main = Mathf.RoundToInt(r.distance) + " m   |   score " + r.score;
            else if (r.def.kind == RaceKind.DriftScore) main = "Drift score " + r.score;
            else main = "Time " + UIKit.Clock(r.time);
            UIKit.Label(new Rect(x, y, w, 56f), main, 44, UIKit.Text, TextAnchor.MiddleCenter, true);
            y += 60f;
            if (r.newBest) UIKit.Label(new Rect(x, y, w, 40f), "NEW BEST", 32, UIKit.Good, TextAnchor.MiddleCenter, true);
            else if (r.usedCheckpoint) UIKit.Label(new Rect(x, y, w, 40f), "continued from a checkpoint: no gold, no best time", 24, UIKit.Dim, TextAnchor.MiddleCenter);
            y += 54f;

            // reward lines
            float listH = Mathf.Min(520f, 74f + r.lines.Count * 52f);
            Rect card = new Rect(x, y, w, listH + 20f);
            UIKit.Card(card);
            Rect view = new Rect(card.x + 24f, card.y + 14f, card.width - 48f, listH - 70f);
            scroll = UIKit.BeginScroll(view, scroll, r.lines.Count * 52f + 10f);
            float ly = -scroll.y;
            for (int i = 0; i < r.lines.Count; i++)
            {
                float show = Mathf.Clamp01((t - 0.4f - i * 0.12f) * 6f);
                UIKit.Label(new Rect(0f, ly, view.width - 200f, 48f), r.lines[i].label, 28, new Color(UIKit.Text.r, UIKit.Text.g, UIKit.Text.b, show), TextAnchor.MiddleLeft);
                if (r.lines[i].cash != 0)
                    UIKit.Label(new Rect(view.width - 200f, ly, 200f, 48f), "+" + UIKit.Money(r.lines[i].cash), 28, new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, show), TextAnchor.MiddleRight, true);
                ly += 52f;
            }
            UIKit.EndScroll();
            UIKit.Label(new Rect(card.x + 24f, card.yMax - 62f, card.width - 48f, 50f),
                "TOTAL  " + UIKit.Money(r.cash) + (r.rep > 0 ? "   +" + r.rep + " REP" : ""), 36, UIKit.Gold, TextAnchor.MiddleRight, true);
            y = card.yMax + 24f;

            // stats row
            if (r.stats != null && !endless)
            {
                string stats = "Near misses " + r.stats.nearMisses + "   Clean corners " + r.stats.cleanCorners + "   Crashes " + r.stats.Hits;
                UIKit.Label(new Rect(x, y, w, 36f), stats, 24, UIKit.Dim, TextAnchor.MiddleCenter);
                y += 50f;
            }

            // buttons
            float by = Mathf.Max(y, safe.yMax - 440f - (!r.won && r.canCheckpoint ? 126f : 0f));
            bool adBonus = r.won && r.bonusCash > 0 && !doubled && AdPolicy.CanOfferRewarded(AdPlacement.RewardedDoubleReward);
            bool adRevive = !r.won && r.canRevive && AdPolicy.CanOfferRewarded(AdPlacement.RewardedRevive);

            if (adBonus)
            {
                if (UIKit.Button(new Rect(x, by, w, 100f), "DOUBLE BONUS +" + UIKit.Money(r.bonusCash) + " (WATCH AD)", BtnStyle.Gold, true, 30))
                {
                    app.RewardedAd(AdPlacement.RewardedDoubleReward, ok =>
                    {
                        if (ok) { app.DoubleBonus(); doubled = true; }
                    });
                }
                by += 116f;
            }
            if (adRevive)
            {
                if (UIKit.Button(new Rect(x, by, w, 100f), "SECOND CHANCE (WATCH AD)", BtnStyle.Gold, true, 30))
                    app.ResultsRevive();
                by += 116f;
            }

            float half = (w - 16f) * 0.5f;
            if (!r.won && r.canCheckpoint)
            {
                if (UIKit.Button(new Rect(x, by, w, 110f), "CONTINUE FROM CHECKPOINT", BtnStyle.Primary, true, 32))
                {
                    app.ResultsCheckpoint();
                    return;
                }
                by += 126f;
            }
            if (r.won)
            {
                if (UIKit.Button(new Rect(x, by, half, 110f), "RETRY", BtnStyle.Secondary)) app.ResultsRetry();
                if (UIKit.Button(new Rect(x + half + 16f, by, half, 110f), "CONTINUE", BtnStyle.Primary)) app.ResultsContinue();
            }
            else
            {
                if (UIKit.Button(new Rect(x, by, half, 110f), "RETRY", BtnStyle.Primary)) app.ResultsRetry();
                if (UIKit.Button(new Rect(x + half + 16f, by, half, 110f), "QUIT", BtnStyle.Secondary)) app.ResultsContinue();
            }
        }
    }
}
