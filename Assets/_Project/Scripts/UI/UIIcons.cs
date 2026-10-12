using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Small icons for the menus, drawn in code from rectangles, circles and rotated bars so the menus look right with
    /// no pictures at all. If a painted icon with the same name exists in Resources/Art (ui_tab_story, ui_stat_races and so on)
    /// it is used instead. All icons take one colour, so they can be dimmed or lit up.
    /// </summary>
    public static class UIIcons
    {
        static readonly Color Cut = new Color(0.03f, 0.04f, 0.10f, 1f);

        /// <summary>Draws the icon called name inside r. Unknown names draw nothing.</summary>
        public static void Draw(string name, Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (UIKit.Icon(r, "ui_" + name, c)) return;

            float s = Mathf.Min(r.width, r.height);
            Rect q = new Rect(r.center.x - s * 0.5f, r.center.y - s * 0.5f, s, s);
            switch (name)
            {
                case "tab_story": Book(q, c); break;
                case "tab_events": Flags(q, c); break;
                case "tab_garage": Garage(q, c); break;
                case "tab_mail": Mail(q, c); break;
                case "tab_shop": Cart(q, c); break;
                case "tab_more": Menu(q, c); break;
                case "check": Check(q, c); break;
                case "lock": Lock(q, c); break;
                case "gear": Gear(q, c); break;
                case "stat_races": Flags(q, c); break;
                case "stat_wins": Cup(q, c); break;
                case "stat_distance": Road(q, c); break;
                case "stat_speed": Dial(q, c); break;
                case "stat_near": Cars(q, c); break;
                case "stat_corners": Bend(q, c); break;
                case "stat_drift": Skid(q, c); break;
                case "stat_cash": Coins(q, c); break;
            }
        }

        static Rect In(Rect q, float x, float y, float w, float h)
        {
            return new Rect(q.x + q.width * x, q.y + q.height * y, q.width * w, q.height * h);
        }

        static Vector2 P(Rect q, float x, float y)
        {
            return new Vector2(q.x + q.width * x, q.y + q.height * y);
        }

        static void Menu(Rect q, Color c)
        {
            for (int i = 0; i < 3; i++) UIKit.Round(In(q, 0.12f, 0.20f + i * 0.26f, 0.76f, 0.12f), c);
        }

        static void Mail(Rect q, Color c)
        {
            UIKit.Round(In(q, 0.06f, 0.20f, 0.88f, 0.62f), c);
            UIKit.Stroke(P(q, 0.28f, 0.38f), q.width * 0.36f, q.height * 0.07f, 35f, Cut);
            UIKit.Stroke(P(q, 0.72f, 0.38f), q.width * 0.36f, q.height * 0.07f, -35f, Cut);
        }

        static void Book(Rect q, Color c)
        {
            UIKit.Round(In(q, 0.06f, 0.20f, 0.42f, 0.62f), c);
            UIKit.Round(In(q, 0.52f, 0.20f, 0.42f, 0.62f), c);
            UIKit.Fill(In(q, 0.485f, 0.26f, 0.03f, 0.56f), Cut);
            UIKit.Fill(In(q, 0.14f, 0.34f, 0.26f, 0.04f), Cut);
            UIKit.Fill(In(q, 0.60f, 0.34f, 0.26f, 0.04f), Cut);
        }

        static void Flags(Rect q, Color c)
        {
            UIKit.Round(In(q, 0.10f, 0.12f, 0.08f, 0.78f), c);
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 4; col++)
                {
                    bool on = (row + col) % 2 == 0;
                    Color cc = on ? c : new Color(c.r, c.g, c.b, c.a * 0.28f);
                    UIKit.Fill(In(q, 0.22f + col * 0.17f, 0.14f + row * 0.17f, 0.17f, 0.17f), cc);
                }
        }

        static void Garage(Rect q, Color c)
        {
            UIKit.Stroke(P(q, 0.28f, 0.28f), q.width * 0.52f, q.height * 0.09f, -28f, c);
            UIKit.Stroke(P(q, 0.72f, 0.28f), q.width * 0.52f, q.height * 0.09f, 28f, c);
            UIKit.Round(In(q, 0.12f, 0.34f, 0.76f, 0.52f), c);
            for (int i = 0; i < 3; i++) UIKit.Fill(In(q, 0.26f, 0.50f + i * 0.11f, 0.48f, 0.04f), Cut);
        }

        static void Cart(Rect q, Color c)
        {
            UIKit.Stroke(P(q, 0.14f, 0.20f), q.width * 0.22f, q.height * 0.07f, 0f, c);
            UIKit.Stroke(P(q, 0.26f, 0.30f), q.width * 0.24f, q.height * 0.07f, 62f, c);
            UIKit.Round(In(q, 0.26f, 0.36f, 0.62f, 0.30f), c);
            UIKit.Circle(In(q, 0.30f, 0.70f, 0.15f, 0.15f), c);
            UIKit.Circle(In(q, 0.64f, 0.70f, 0.15f, 0.15f), c);
        }

        static void Check(Rect q, Color c)
        {
            UIKit.Circle(q, new Color(c.r, c.g, c.b, c.a * 0.25f));
            UIKit.Stroke(P(q, 0.40f, 0.60f), q.width * 0.20f, q.height * 0.10f, 45f, c);
            UIKit.Stroke(P(q, 0.58f, 0.52f), q.width * 0.40f, q.height * 0.10f, -50f, c);
        }

        static void Lock(Rect q, Color c)
        {
            UIKit.Round(In(q, 0.22f, 0.44f, 0.56f, 0.44f), c);
            UIKit.Round(In(q, 0.32f, 0.12f, 0.36f, 0.40f), new Color(c.r, c.g, c.b, c.a * 0.8f));
            UIKit.Round(In(q, 0.40f, 0.22f, 0.20f, 0.30f), Cut);
            UIKit.Circle(In(q, 0.44f, 0.58f, 0.12f, 0.12f), Cut);
        }

        static void Gear(Rect q, Color c)
        {
            for (int i = 0; i < 4; i++) UIKit.Stroke(P(q, 0.5f, 0.5f), q.width * 0.94f, q.height * 0.18f, i * 45f, c);
            UIKit.Circle(In(q, 0.16f, 0.16f, 0.68f, 0.68f), c);
            UIKit.Circle(In(q, 0.36f, 0.36f, 0.28f, 0.28f), Cut);
        }

        static void Cup(Rect q, Color c)
        {
            UIKit.Round(In(q, 0.26f, 0.12f, 0.48f, 0.42f), c);
            UIKit.Round(In(q, 0.06f, 0.18f, 0.16f, 0.22f), new Color(c.r, c.g, c.b, c.a * 0.7f));
            UIKit.Round(In(q, 0.78f, 0.18f, 0.16f, 0.22f), new Color(c.r, c.g, c.b, c.a * 0.7f));
            UIKit.Fill(In(q, 0.44f, 0.54f, 0.12f, 0.20f), c);
            UIKit.Round(In(q, 0.28f, 0.74f, 0.44f, 0.12f), c);
        }

        static void Road(Rect q, Color c)
        {
            UIKit.Stroke(P(q, 0.30f, 0.52f), q.height * 0.86f, q.width * 0.08f, 16f, c);
            UIKit.Stroke(P(q, 0.70f, 0.52f), q.height * 0.86f, q.width * 0.08f, -16f, c);
            for (int i = 0; i < 3; i++) UIKit.Fill(In(q, 0.47f, 0.18f + i * 0.26f, 0.06f, 0.16f), c);
        }

        static void Dial(Rect q, Color c)
        {
            UIKit.Circle(In(q, 0.06f, 0.10f, 0.88f, 0.88f), new Color(c.r, c.g, c.b, c.a * 0.30f));
            UIKit.Circle(In(q, 0.14f, 0.18f, 0.72f, 0.72f), Cut);
            UIKit.Stroke(P(q, 0.58f, 0.46f), q.width * 0.38f, q.height * 0.07f, -45f, c);
            UIKit.Circle(In(q, 0.43f, 0.55f, 0.14f, 0.14f), c);
        }

        static void Cars(Rect q, Color c)
        {
            UIKit.Round(In(q, 0.04f, 0.46f, 0.38f, 0.36f), c);
            UIKit.Round(In(q, 0.58f, 0.46f, 0.38f, 0.36f), c);
            UIKit.Stroke(P(q, 0.5f, 0.24f), q.width * 0.30f, q.height * 0.06f, 0f, new Color(1f, 0.76f, 0.26f, c.a));
            UIKit.Stroke(P(q, 0.5f, 0.24f), q.width * 0.30f, q.height * 0.06f, 90f, new Color(1f, 0.76f, 0.26f, c.a));
        }

        static void Bend(Rect q, Color c)
        {
            UIKit.Stroke(P(q, 0.32f, 0.62f), q.height * 0.62f, q.width * 0.08f, -62f, c);
            UIKit.Stroke(P(q, 0.56f, 0.30f), q.height * 0.50f, q.width * 0.08f, -18f, c);
            UIKit.Stroke(P(q, 0.74f, 0.58f), q.height * 0.42f, q.width * 0.07f, 70f, new Color(c.r, c.g, c.b, c.a * 0.7f));
        }

        static void Skid(Rect q, Color c)
        {
            UIKit.Round(In(q, 0.30f, 0.20f, 0.46f, 0.24f), c);
            UIKit.Stroke(P(q, 0.34f, 0.66f), q.width * 0.60f, q.height * 0.07f, -12f, new Color(c.r, c.g, c.b, c.a * 0.8f));
            UIKit.Stroke(P(q, 0.40f, 0.82f), q.width * 0.62f, q.height * 0.07f, -12f, new Color(c.r, c.g, c.b, c.a * 0.5f));
        }

        static void Coins(Rect q, Color c)
        {
            for (int i = 0; i < 3; i++)
                UIKit.Round(In(q, 0.16f, 0.58f - i * 0.17f, 0.52f, 0.14f), i == 0 ? c : new Color(c.r, c.g, c.b, c.a * 0.8f));
            for (int i = 0; i < 2; i++)
                UIKit.Round(In(q, 0.52f, 0.66f - i * 0.17f, 0.34f, 0.14f), new Color(c.r, c.g, c.b, c.a * 0.6f));
        }
    }
}
