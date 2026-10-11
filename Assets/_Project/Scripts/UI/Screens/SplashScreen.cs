using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>ST Media badge, then straight to the consent screen or the menu.</summary>
    public sealed class SplashScreen : ScreenBase
    {
        float t;

        public override void OnEnter()
        {
            t = 0f;
            app.Stage.Show(false);
        }

        public override void Draw()
        {
            if (Event.current.type == EventType.Repaint) t += Time.unscaledDeltaTime;
            UIKit.Fill(new Rect(0f, 0f, UIKit.W, UIKit.H), Color.white);

            float a = Mathf.Clamp01(Mathf.Min(t / 0.5f, (2.3f - t) / 0.5f));
            float s = 300f;
            Rect logo = new Rect((UIKit.W - s) * 0.5f, UIKit.H * 0.5f - s * 0.62f, s, s);
            GUI.color = new Color(1f, 1f, 1f, a);
            UIKit.Logo(logo);
            GUI.color = Color.white;
            UIKit.Label(new Rect(0f, logo.yMax + 24f, UIKit.W, 60f), "ST MEDIA", 44, new Color(0.42f, 0.47f, 0.84f, a), TextAnchor.MiddleCenter, true);
            UIKit.Label(new Rect(0f, logo.yMax + 80f, UIKit.W, 44f), "presents", 28, new Color(0.45f, 0.47f, 0.60f, a), TextAnchor.MiddleCenter);

            if (t > 2.3f || UIKit.Click(new Rect(0f, 0f, UIKit.W, UIKit.H)))
                app.AfterSplash();
        }
    }
}
