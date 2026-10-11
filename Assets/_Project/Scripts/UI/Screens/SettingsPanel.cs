using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>The settings list. Used by the main menu, the hub and the pause menu.</summary>
    public sealed class SettingsPanel
    {
        bool confirmDelete;

        /// <summary>Draws all settings in a scroll view and returns the new scroll position. inGame hides the items that need a restart.</summary>
        public Vector2 Draw(AppRoot app, Rect view, Vector2 scroll, bool inGame)
        {
            SettingsData s = app.Save.settings;
            const float rowH = 110f, gap = 16f;
            int rows = inGame ? 7 : 16;
            float content = rows * (rowH + gap) + 420f;
            scroll = UIKit.BeginScroll(view, scroll, content);
            float w = view.width;
            float y = -scroll.y;
            bool ch;
            bool any = false;

            float v;
            v = UIKit.Slider(new Rect(0f, y, w, rowH), s.musicVolume, "Music");
            if (!Mathf.Approximately(v, s.musicVolume)) { s.musicVolume = v; any = true; }
            y += rowH + gap;
            v = UIKit.Slider(new Rect(0f, y, w, rowH), s.sfxVolume, "Sound effects");
            if (!Mathf.Approximately(v, s.sfxVolume)) { s.sfxVolume = v; any = true; }
            y += rowH + gap;

            bool b = UIKit.Toggle(new Rect(0f, y, w, rowH), "Vibration", s.vibration, out ch);
            if (ch) { s.vibration = b; any = true; }
            y += rowH + gap;

            if (!inGame)
            {
                // steering sensitivity: 0.5 .. 1.8
                float sens01 = Mathf.InverseLerp(0.5f, 1.8f, s.steerSensitivity);
                float ns = UIKit.Slider(new Rect(0f, y, w, rowH), sens01, "Steering speed");
                if (!Mathf.Approximately(ns, sens01)) { s.steerSensitivity = Mathf.Lerp(0.5f, 1.8f, ns); any = true; }
                y += rowH + gap;

                b = UIKit.Toggle(new Rect(0f, y, w, rowH), "Brake button", s.brakeButton, out ch);
                if (ch) { s.brakeButton = b; any = true; }
                y += rowH + gap;
                b = UIKit.Toggle(new Rect(0f, y, w, rowH), "Drift button", s.driftButton, out ch);
                if (ch) { s.driftButton = b; any = true; }
                y += rowH + gap;
                b = UIKit.Toggle(new Rect(0f, y, w, rowH), "Nitro button", s.nitroButton, out ch);
                if (ch) { s.nitroButton = b; any = true; }
                y += rowH + gap;
            }

            b = UIKit.Toggle(new Rect(0f, y, w, rowH), "Reduce camera shake", s.reduceMotion, out ch);
            if (ch) { s.reduceMotion = b; any = true; }
            y += rowH + gap;
            b = UIKit.Toggle(new Rect(0f, y, w, rowH), "Show FPS", s.showFps, out ch);
            if (ch) { s.showFps = b; any = true; }
            y += rowH + gap;

            if (!inGame)
            {
                b = UIKit.Toggle(new Rect(0f, y, w, rowH), "Story assist (easier rivals after losses)", s.storyAssist, out ch);
                if (ch) { s.storyAssist = b; any = true; }
                y += rowH + gap;

                // graphics level
                UIKit.Card(new Rect(0f, y, w, rowH + 20f));
                UIKit.Label(new Rect(28f, y + 6f, w, 46f), "Graphics", 28, UIKit.Text, TextAnchor.MiddleLeft);
                string[] names = { "LOW", "MEDIUM", "HIGH" };
                float bw = (w - 56f - 2f * 12f) / 3f;
                for (int i = 0; i < 3; i++)
                {
                    bool sel = (int)GraphicsSettings.Current == i;
                    Rect r = new Rect(28f + i * (bw + 12f), y + 54f, bw, 64f);
                    if (UIKit.Button(r, names[i], sel ? BtnStyle.Primary : BtnStyle.Secondary, true, 26))
                    {
                        GraphicsSettings.Apply((QualityLevel)i, true);
                        any = true;
                    }
                }
                y += rowH + 20f + gap;

                // ads
                string ad = s.adConsent == 1 ? "Personalised ads: ON" : "Personalised ads: OFF";
                b = UIKit.Toggle(new Rect(0f, y, w, rowH), ad, s.adConsent == 1, out ch);
                if (ch) { s.adConsent = b ? 1 : 0; app.ApplyConsent(); any = true; }
                y += rowH + gap;

                b = UIKit.Toggle(new Rect(0f, y, w, rowH), "Developer tuning panel", s.devTuning, out ch);
                if (ch) { s.devTuning = b; any = true; }
                y += rowH + gap;

                if (UIKit.Button(new Rect(0f, y, w, 96f), "Privacy policy", BtnStyle.Secondary)) Application.OpenURL(AppRoot.PrivacyUrl);
                y += 96f + gap;

                if (!confirmDelete)
                {
                    if (UIKit.Button(new Rect(0f, y, w, 96f), "Delete save data", BtnStyle.Danger)) confirmDelete = true;
                }
                else
                {
                    UIKit.Label(new Rect(0f, y, w, 40f), "Really delete all progress?", 28, UIKit.Bad, TextAnchor.MiddleCenter, true);
                    if (UIKit.Button(new Rect(0f, y + 46f, w * 0.48f, 90f), "YES, DELETE", BtnStyle.Danger))
                    {
                        confirmDelete = false;
                        app.ResetSave();
                        app.Go(Scr.Menu);
                    }
                    if (UIKit.Button(new Rect(w * 0.52f, y + 46f, w * 0.48f, 90f), "CANCEL", BtnStyle.Secondary)) confirmDelete = false;
                    y += 140f;
                }
                y += 110f;
            }

            UIKit.EndScroll();
            if (any) app.ApplySettings();
            return scroll;
        }
    }
}
