using UnityEngine;

namespace MidnightLegacy
{
    public enum Scr { Splash, Consent, Menu, Hub, Dialogue, Drive, Results }

    /// <summary>One full screen of the game. Plain class (not a MonoBehaviour): AppRoot calls Draw from OnGUI.</summary>
    public abstract class ScreenBase
    {
        protected AppRoot app;

        public void Attach(AppRoot root)
        {
            app = root;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public abstract void Draw();

        /// <summary>Android back button. Return true when handled.</summary>
        public virtual bool OnBack() { return false; }

        // ---- shared layout helpers ----

        protected static Rect SafeRect { get { return UIKit.Safe; } }

        /// <summary>Dark gradient behind a screen that has no 3D scene.</summary>
        protected static void DrawBackdrop(Color top, Color bottom)
        {
            const int bands = 24;
            float h = UIKit.H / bands;
            for (int i = 0; i < bands; i++)
                UIKit.Fill(new Rect(0f, i * h, UIKit.W, h + 1f), Color.Lerp(top, bottom, i / (float)(bands - 1)));
        }
    }
}
