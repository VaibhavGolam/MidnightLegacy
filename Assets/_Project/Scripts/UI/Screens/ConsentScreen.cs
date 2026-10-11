using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Asks once how ads may be personalised. The answer is stored in the save and passed to the ad service.
    /// For players in the EU/UK a real consent form (Google UMP) must replace this screen before launch.
    /// </summary>
    public sealed class ConsentScreen : ScreenBase
    {
        public override void OnEnter()
        {
            app.Stage.Show(false);
        }

        public override void Draw()
        {
            DrawBackdrop(new Color(0.05f, 0.06f, 0.14f), new Color(0.02f, 0.02f, 0.06f));
            Rect safe = SafeRect;
            float x = 60f, w = UIKit.W - 120f;
            float y = safe.y + 160f;

            UIKit.Logo(new Rect(UIKit.W * 0.5f - 70f, y - 40f, 140f, 140f));
            y += 150f;
            UIKit.Label(new Rect(x, y, w, 70f), "Before you drive", 54, UIKit.Text, TextAnchor.MiddleCenter, true);
            y += 90f;

            string body = "Midnight Legacy is free. Ads only appear on the results screen, never while you drive, and you can remove them for good in the shop.\n\n" +
                          "Personalised ads use your device's advertising ID to show ads you might like. Non-personalised ads do not. You can change this any time in Settings.";
            float h = UIKit.TextHeight(body, 32, w);
            UIKit.Label(new Rect(x, y, w, h), body, 32, UIKit.Dim, TextAnchor.UpperLeft, false, true);
            y += h + 60f;

            if (UIKit.Button(new Rect(x, y, w, 120f), "PERSONALISED ADS", BtnStyle.Primary))
                app.SetConsent(1);
            y += 140f;
            if (UIKit.Button(new Rect(x, y, w, 120f), "NON-PERSONALISED ADS", BtnStyle.Secondary))
                app.SetConsent(0);
            y += 150f;
            if (UIKit.Button(new Rect(x, y, w, 90f), "Privacy policy", BtnStyle.Ghost))
                Application.OpenURL(AppRoot.PrivacyUrl);
        }
    }
}
