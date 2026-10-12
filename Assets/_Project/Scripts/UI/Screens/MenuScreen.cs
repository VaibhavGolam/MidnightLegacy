using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>Title screen over the 3D garage. Also holds the name entry and the reset confirmation.</summary>
    public sealed class MenuScreen : ScreenBase
    {
        bool naming;
        bool confirmNew;
        bool showSettings;
        string nameText = "Jun";
        Vector2 settingsScroll;
        float t;
        readonly SettingsPanel settings = new SettingsPanel();

        public override void OnEnter()
        {
            naming = false;
            confirmNew = false;
            showSettings = false;
            t = 0f;
            // the painted harbour road is the title picture; without it the garage scene is shown behind the menu
            bool art = UIKit.Art("menu_bg") != null;
            app.Stage.Show(!art);
            if (!art)
            {
                app.Stage.SetPose(StagePose.Menu);
                app.RefreshStage();
            }
            if (AudioManager.I != null) AudioManager.I.SetMood(MusicMood.Menu);
        }

        public override bool OnBack()
        {
            if (showSettings) { showSettings = false; app.SaveNow(); return true; }
            if (naming) { naming = false; return true; }
            if (confirmNew) { confirmNew = false; return true; }
            return false;
        }

        public override void Draw()
        {
            Rect safe = SafeRect;
            SaveData save = app.Save;

            if (Event.current.type == EventType.Repaint) t += Time.unscaledDeltaTime;

            // painted background with a slow drift, darkened top and bottom so the title and buttons read well
            Texture2D bg = UIKit.Art("menu_bg");
            if (bg != null)
            {
                float zoom = 1.08f + 0.04f * Mathf.Sin(t * 0.12f);
                float fx = 0.5f + 0.08f * Mathf.Sin(t * 0.07f);
                UIKit.Cover(new Rect(0f, 0f, UIKit.W, UIKit.H), bg, ArtLoader.Aspect("menu_bg"), Color.white, zoom, fx, 0.55f);
                UIKit.Vignette(0.85f, 0.92f);
            }

            // title
            float top = safe.y + 70f;
            Texture2D logo = UIKit.Art("title_logo");
            if (logo != null)
            {
                float lw = 900f, lh = lw / ArtLoader.Aspect("title_logo");
                UIKit.Image(new Rect((UIKit.W - lw) * 0.5f, top + 30f, lw, lh), logo, Color.white);
                UIKit.Label(new Rect(0f, top + 30f + lh + 10f, UIKit.W, 44f), "Legacy is inherited. Greatness is earned.", 30, new Color(0.86f, 0.89f, 1f, 0.9f), TextAnchor.MiddleCenter);
            }
            else
            {
                UIKit.Logo(new Rect(UIKit.W * 0.5f - 55f, top, 110f, 110f));
                UIKit.Label(new Rect(0f, top + 120f, UIKit.W, 90f), "MIDNIGHT", 84, UIKit.Text, TextAnchor.MiddleCenter, true);
                UIKit.Label(new Rect(0f, top + 200f, UIKit.W, 90f), "LEGACY", 84, UIKit.Accent, TextAnchor.MiddleCenter, true);
                UIKit.Label(new Rect(0f, top + 290f, UIKit.W, 44f), "Legacy is inherited. Greatness is earned.", 28, UIKit.Dim, TextAnchor.MiddleCenter);
            }

            float bw = 700f, bx = (UIKit.W - bw) * 0.5f;
            float by = safe.yMax - 640f;

            if (showSettings) { DrawSettings(); return; }
            if (naming) { DrawNaming(); return; }
            if (confirmNew) { DrawConfirm(); return; }

            if (save.introSeen)
            {
                if (UIKit.Button(new Rect(bx, by, bw, 130f), "CONTINUE", BtnStyle.Primary)) app.Go(Scr.Hub);
                by += 150f;
                if (UIKit.Button(new Rect(bx, by, bw, 100f), "NEW GAME", BtnStyle.Secondary)) confirmNew = true;
            }
            else
            {
                if (UIKit.Button(new Rect(bx, by, bw, 130f), "START", BtnStyle.Primary))
                {
                    naming = true;
                    nameText = save.playerName;
                }
                by += 150f;
            }
            by += 120f;
            if (UIKit.Button(new Rect(bx, by, bw, 100f), "SETTINGS", BtnStyle.Secondary)) showSettings = true;

            UIKit.Label(new Rect(0f, safe.yMax - 70f, UIKit.W, 40f), "ST Media  |  v" + AppRoot.Version, 24, new Color(1f, 1f, 1f, 0.35f), TextAnchor.MiddleCenter);
        }

        void DrawNaming()
        {
            Rect safe = SafeRect;
            UIKit.Fill(new Rect(0f, 0f, UIKit.W, UIKit.H), new Color(0f, 0f, 0f, 0.7f));
            float w = 880f, x = (UIKit.W - w) * 0.5f, y = safe.y + 420f;
            UIKit.Card(new Rect(x - 20f, y - 40f, w + 40f, 640f));
            UIKit.Label(new Rect(x, y, w, 70f), "What's your name?", 48, UIKit.Text, TextAnchor.MiddleCenter, true);
            UIKit.Label(new Rect(x, y + 70f, w, 80f), "You play a college student who just inherited a car. Pick the name people will call you on the road.", 28, UIKit.Dim, TextAnchor.UpperCenter, false, true);
            nameText = UIKit.TextField(new Rect(x + 60f, y + 190f, w - 120f, 110f), nameText, 14);
            bool ok = nameText.Trim().Length > 0;
            if (UIKit.Button(new Rect(x + 60f, y + 340f, w - 120f, 120f), "BEGIN", BtnStyle.Primary, ok))
            {
                naming = false;
                app.StartNewGame(nameText.Trim());
            }
            if (UIKit.Button(new Rect(x + 60f, y + 480f, w - 120f, 80f), "Back", BtnStyle.Ghost)) naming = false;
        }

        void DrawConfirm()
        {
            Rect safe = SafeRect;
            UIKit.Fill(new Rect(0f, 0f, UIKit.W, UIKit.H), new Color(0f, 0f, 0f, 0.7f));
            float w = 840f, x = (UIKit.W - w) * 0.5f, y = safe.y + 520f;
            UIKit.Card(new Rect(x, y, w, 520f));
            UIKit.Label(new Rect(x + 40f, y + 30f, w - 80f, 70f), "Start a new game?", 46, UIKit.Text, TextAnchor.MiddleCenter, true);
            UIKit.Label(new Rect(x + 50f, y + 110f, w - 100f, 140f), "Your current progress, cash and car upgrades will be deleted. Purchases made with real money are kept.", 30, UIKit.Dim, TextAnchor.UpperCenter, false, true);
            if (UIKit.Button(new Rect(x + 50f, y + 280f, w - 100f, 100f), "DELETE AND START OVER", BtnStyle.Danger))
            {
                confirmNew = false;
                app.ResetSave();
                naming = true;
                nameText = "Jun";
            }
            if (UIKit.Button(new Rect(x + 50f, y + 400f, w - 100f, 90f), "Cancel", BtnStyle.Secondary)) confirmNew = false;
        }

        void DrawSettings()
        {
            Rect safe = SafeRect;
            UIKit.Fill(new Rect(0f, 0f, UIKit.W, UIKit.H), new Color(0f, 0f, 0.03f, 0.88f));
            UIKit.Label(new Rect(0f, safe.y + 40f, UIKit.W, 80f), "SETTINGS", 56, UIKit.Text, TextAnchor.MiddleCenter, true);
            Rect view = new Rect(40f, safe.y + 140f, UIKit.W - 80f, safe.height - 320f);
            settingsScroll = settings.Draw(app, view, settingsScroll, false);
            if (UIKit.Button(new Rect(60f, safe.yMax - 150f, UIKit.W - 120f, 110f), "BACK", BtnStyle.Primary))
            {
                showSettings = false;
                app.SaveNow();
            }
        }
    }
}
