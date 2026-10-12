using System;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Visual novel style scenes: a stylised backdrop, a placeholder portrait, a typewriter text box and choices.
    /// Node effects (flag:x, rep:+N, cash:+N, stage:uncover) are applied by AppRoot.ApplyEffect.
    /// </summary>
    public sealed class DialogueScreen : ScreenBase
    {
        DialogueDef def;
        Action onDone;
        int index;
        DialogueNode node;
        string fullText = "";
        string speakerName = "";
        CharacterDef speaker;
        float typed;
        bool finished;
        string lastSpeaker = "";
        float portraitT = 1f;          // fade in of the portrait when the speaker changes

        static Vector3[] stars;

        public void Begin(DialogueDef d, Action done)
        {
            def = d;
            onDone = done;
            finished = false;
        }

        public override void OnEnter()
        {
            if (stars == null)
            {
                System.Random r = new System.Random(77);
                stars = new Vector3[60];
                for (int i = 0; i < stars.Length; i++) stars[i] = new Vector3((float)r.NextDouble(), (float)r.NextDouble() * 0.45f, 2f + (float)r.NextDouble() * 3f);
            }

            lastSpeaker = "";
            portraitT = 0f;
            bool garage = def != null && def.bg == "garage";
            app.Stage.Show(garage);
            if (garage)
            {
                app.Stage.SetPose(StagePose.Hub);
                app.RefreshStage();
            }
            if (AudioManager.I != null) AudioManager.I.SetMood(def != null && def.mood == "tense" ? MusicMood.Tense : MusicMood.Calm);
            app.Input.Locked = true;
            if (def == null || def.nodes.Count == 0) { Finish(); return; }
            Enter(0);
        }

        void Enter(int i)
        {
            if (def == null || i < 0 || i >= def.nodes.Count) { Finish(); return; }
            index = i;
            node = def.nodes[i];
            if (node.fx != null) for (int k = 0; k < node.fx.Length; k++) app.ApplyEffect(node.fx[k]);
            string name = app.Save.playerName;
            fullText = (node.text ?? "").Replace("{name}", name);
            speaker = ContentDB.Character(node.speaker);
            speakerName = speaker != null ? (speaker.name ?? "").Replace("{name}", name) : "";
            typed = 0f;
            if (node.speaker != lastSpeaker)
            {
                lastSpeaker = node.speaker;
                portraitT = 0f;
            }
        }

        void Advance()
        {
            if (node == null) return;
            if (node.choices != null && node.choices.Length > 0) return;
            int next = index + 1;
            if (!string.IsNullOrEmpty(node.jump))
            {
                int j = def.IndexOf(node.jump);
                next = j >= 0 ? j : def.nodes.Count;
            }
            Enter(next);
        }

        void Choose(DialogueChoice c)
        {
            if (c.fx != null) for (int k = 0; k < c.fx.Length; k++) app.ApplyEffect(c.fx[k]);
            int j = def.IndexOf(c.go);
            Enter(j >= 0 ? j : index + 1);
        }

        /// <summary>Fast forwards to the next choice or the end, still applying the effects of skipped lines.</summary>
        void Skip()
        {
            if (finished || def == null) return;
            int guard = 0;
            while (guard++ < 200 && !finished && node != null && (node.choices == null || node.choices.Length == 0))
                Advance();
            typed = fullText.Length;
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            Action cb = onDone;
            onDone = null;
            node = null;
            if (cb != null) cb();     // may call Begin() again for a chained scene
        }

        public override bool OnBack()
        {
            Skip();
            return true;
        }

        // ------------------------------------------------------------------ drawing

        public override void Draw()
        {
            if (finished || def == null || node == null) return;
            DialogueDef shownDef = def;
            Rect safe = SafeRect;
            float dt = Time.unscaledDeltaTime;
            if (Event.current.type == EventType.Repaint)
            {
                typed = Mathf.Min(fullText.Length, typed + dt * 62f);
                portraitT = Mathf.Min(1f, portraitT + dt * 3.5f);
            }

            if (def.bg != "garage")
            {
                if (!DrawPaintedBackground(def.bg)) DrawBackground(def.bg);
            }
            else
            {
                // the 3D garage only fills the top; carry its floor colour down behind the text so there is no black gap
                Color floor = new Color(0.055f, 0.05f, 0.07f, 1f);
                UIKit.Fade(new Rect(0f, UIKit.H * 0.27f, UIKit.W, UIKit.H * 0.16f), floor, false);
                UIKit.Fill(new Rect(0f, UIKit.H * 0.43f, UIKit.W, UIKit.H * 0.57f), floor);
            }

            // full screen tap: finish typing, then advance
            bool hasChoices = node.choices != null && node.choices.Length > 0;
            bool done = typed >= fullText.Length;
            if (UIKit.Tap(new Rect(0f, 0f, UIKit.W, UIKit.H)))
            {
                if (!done) typed = fullText.Length;
                else if (!hasChoices) Advance();
                if (finished || def != shownDef || node == null) return;
            }

            // portrait
            bool isNarr = speaker == null || node.speaker == "narr";
            // the box fits its text (measured on the whole line, so it does not grow while typing)
            float textH = UIKit.TextHeight(fullText, 36, UIKit.W - 60f - 80f);
            float boxH = hasChoices ? textH + 120f + node.choices.Length * 128f : Mathf.Clamp(textH + 130f, 250f, 600f);
            float boxY = safe.yMax - boxH - 30f;
            Texture2D art = isNarr ? null : UIKit.Portrait(node.speaker);
            if (art != null)
            {
                // painted portrait: fills the space above the text box and fades into the scene at the top and the bottom
                UIKit.Fill(new Rect(0f, 0f, UIKit.W, UIKit.H), new Color(0f, 0f, 0.02f, 0.55f * portraitT));
                float ps = UIKit.W;
                Rect pr = new Rect(0f, boxY + 70f - ps, ps, ps);
                Color shade = new Color(0.02f, 0.025f, 0.06f, 1f);
                UIKit.Image(pr, art, new Color(1f, 1f, 1f, portraitT));
                UIKit.Fade(new Rect(0f, pr.y - 1f, ps, ps * 0.28f), new Color(shade.r, shade.g, shade.b, 0.95f * portraitT), true);
                UIKit.Fade(new Rect(0f, pr.yMax - ps * 0.34f, ps, ps * 0.34f + 1f), new Color(shade.r, shade.g, shade.b, 0.97f), false);
            }
            else if (!isNarr)
            {
                bool right = node.speaker == "jun";
                float pw = 360f;
                Rect pr = new Rect(right ? UIKit.W - pw - 40f : 40f, boxY - pw - 6f, pw, pw);
                DrawPortrait(speaker, pr);
            }

            // text box
            Rect box = new Rect(30f, boxY, UIKit.W - 60f, boxH);
            UIKit.Card(box);
            if (!isNarr && !string.IsNullOrEmpty(speakerName))
            {
                float tw = Mathf.Max(220f, speakerName.Length * 26f + 70f);
                Rect tag = new Rect(box.x + 30f, box.y - 34f, tw, 60f);
                UIKit.Round(tag, speaker.color * 0.85f);
                UIKit.Outline(tag, new Color(1f, 1f, 1f, 0.25f));
                UIKit.Label(tag, speakerName, 32, Color.white, TextAnchor.MiddleCenter, true);
                if (!string.IsNullOrEmpty(speaker.role))
                    UIKit.Label(new Rect(tag.xMax + 16f, tag.y, 420f, tag.height), speaker.role, 24, UIKit.Dim, TextAnchor.MiddleLeft);
            }

            string shown = fullText.Substring(0, Mathf.Min(fullText.Length, Mathf.FloorToInt(typed)));
            Color tc = isNarr ? new Color(0.78f, 0.82f, 0.95f) : UIKit.Text;
            UIKit.Label(new Rect(box.x + 40f, box.y + 40f, box.width - 80f, textH + 12f), shown, 36, tc, TextAnchor.UpperLeft, false, true);

            if (hasChoices && done)
            {
                float cy = box.y + 40f + textH + 36f;
                for (int i = 0; i < node.choices.Length; i++)
                {
                    if (UIKit.Button(new Rect(box.x + 40f, cy, box.width - 80f, 108f), node.choices[i].text, BtnStyle.Secondary, true, 32))
                    {
                        Choose(node.choices[i]);
                        return;
                    }
                    cy += 128f;
                }
            }
            else if (done)
            {
                float a = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
                UIKit.Label(new Rect(box.xMax - 200f, box.yMax - 70f, 160f, 50f), "tap", 28, new Color(1f, 1f, 1f, a), TextAnchor.MiddleRight);
            }

            if (UIKit.Button(new Rect(UIKit.W - 220f, safe.y + 24f, 190f, 76f), "SKIP", BtnStyle.Ghost, true, 28)) Skip();
        }

        // ------------------------------------------------------------------ portraits

        static void DrawPortrait(CharacterDef c, Rect r)
        {
            Color col = c.color;
            Color skin = new Color(0.78f, 0.62f, 0.52f);
            Color hair = col * 0.45f;
            if (c.silhouette)
            {
                // the Ghost: only a dark shape with a pale rim light
                Color rim = new Color(0.78f, 0.82f, 0.92f);
                UIKit.Circle(new Rect(r.x + r.width * 0.26f - 5f, r.y + r.height * 0.06f - 5f, r.width * 0.48f + 10f, r.width * 0.48f + 10f), rim * 0.6f);
                UIKit.Round(new Rect(r.x + r.width * 0.08f - 5f, r.y + r.height * 0.52f - 5f, r.width * 0.84f + 10f, r.height * 0.6f), rim * 0.6f);
                UIKit.Circle(new Rect(r.x + r.width * 0.26f, r.y + r.height * 0.06f, r.width * 0.48f, r.width * 0.48f), new Color(0.04f, 0.04f, 0.06f));
                UIKit.Round(new Rect(r.x + r.width * 0.08f, r.y + r.height * 0.52f, r.width * 0.84f, r.height * 0.6f), new Color(0.04f, 0.04f, 0.06f));
                return;
            }
            if (c.mask)
            {
                UIKit.Round(new Rect(r.x + r.width * 0.08f, r.y + r.height * 0.52f, r.width * 0.84f, r.height * 0.6f), new Color(0.07f, 0.07f, 0.10f));
                UIKit.Circle(new Rect(r.x + r.width * 0.26f, r.y + r.height * 0.06f, r.width * 0.48f, r.width * 0.48f), new Color(0.05f, 0.05f, 0.07f));
                float ey = r.y + r.height * 0.26f;
                UIKit.Round(new Rect(r.x + r.width * 0.36f, ey, r.width * 0.10f, 14f), new Color(0.3f, 0.9f, 1f));
                UIKit.Round(new Rect(r.x + r.width * 0.54f, ey, r.width * 0.10f, 14f), new Color(0.3f, 0.9f, 1f));
                return;
            }
            UIKit.Round(new Rect(r.x + r.width * 0.08f, r.y + r.height * 0.52f, r.width * 0.84f, r.height * 0.6f), col * 0.8f);
            UIKit.Circle(new Rect(r.x + r.width * 0.26f, r.y + r.height * 0.06f, r.width * 0.48f, r.width * 0.48f), skin);
            UIKit.Round(new Rect(r.x + r.width * 0.24f, r.y + r.height * 0.04f, r.width * 0.52f, r.height * 0.15f), hair);
            UIKit.Fill(new Rect(r.x + r.width * 0.37f, r.y + r.height * 0.29f, 12f, 12f), new Color(0.1f, 0.1f, 0.12f));
            UIKit.Fill(new Rect(r.x + r.width * 0.57f, r.y + r.height * 0.29f, 12f, 12f), new Color(0.1f, 0.1f, 0.12f));
            if (!string.IsNullOrEmpty(c.glyph))
                UIKit.Label(new Rect(r.x, r.y + r.height * 0.62f, r.width, r.height * 0.3f), c.glyph, 64, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, true);
        }

        // ------------------------------------------------------------------ backdrops

        /// <summary>The painted harbour road behind scenes that take place on the road. Returns false when the art is not there.</summary>
        static bool DrawPaintedBackground(string bg)
        {
            // a picture made for this scene (scene_room, scene_phone, scene_city, scene_road, scene_pass)
            Texture2D own = UIKit.Art("scene_" + bg);
            if (own != null)
            {
                float dr = 0.5f + 0.04f * Mathf.Sin(Time.unscaledTime * 0.12f);
                UIKit.Cover(new Rect(0f, 0f, UIKit.W, UIKit.H), own, own.width / Mathf.Max(1f, (float)own.height), Color.white, 1.04f, dr, 0.5f);
                UIKit.Vignette(0.45f, 0.55f);
                return true;
            }
            if (bg != "road" && bg != "pass") return false;
            Texture2D t = UIKit.Art("menu_bg");
            if (t == null) return false;
            float drift = 0.5f + 0.05f * Mathf.Sin(Time.unscaledTime * 0.1f);
            UIKit.Cover(new Rect(0f, 0f, UIKit.W, UIKit.H), t, ArtLoader.Aspect("menu_bg"), new Color(0.8f, 0.8f, 0.85f), 1.05f, drift, 0.5f);
            return true;
        }

        static void Sky(Color top, Color bottom, float height)
        {
            const int bands = 20;
            float h = height / bands;
            for (int i = 0; i < bands; i++)
                UIKit.Fill(new Rect(0f, i * h, UIKit.W, h + 1f), Color.Lerp(top, bottom, i / (float)(bands - 1)));
        }

        static void StarField(float alpha)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                float tw = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * stars[i].z + i);
                UIKit.Fill(new Rect(stars[i].x * UIKit.W, stars[i].y * UIKit.H, 4f, 4f), new Color(1f, 1f, 1f, alpha * tw));
            }
        }

        static void RoadPerspective(float horizon, Color asphalt, Color line, Color side)
        {
            float cx = UIKit.W * 0.5f;
            const int strips = 32;
            float total = UIKit.H - horizon;
            for (int i = 0; i < strips; i++)
            {
                float t0 = i / (float)strips, t1 = (i + 1) / (float)strips;
                float y = horizon + total * t0 * t0;
                float yn = horizon + total * t1 * t1;
                float halfW = Mathf.Lerp(40f, UIKit.W * 0.95f, t1 * t1);
                UIKit.Fill(new Rect(0f, y, UIKit.W, yn - y + 1f), side);
                UIKit.Fill(new Rect(cx - halfW, y, halfW * 2f, yn - y + 1f), Color.Lerp(asphalt, asphalt * 0.7f, (i % 2) * 0.35f));
                float scroll = (Time.unscaledTime * 0.7f) % 1f;
                float ph = (t1 * 8f + scroll * 1f) % 1f;
                if (ph < 0.5f) UIKit.Fill(new Rect(cx - 3f - halfW * 0.02f, y, 6f + halfW * 0.04f, yn - y + 1f), line);
            }
        }

        static void Skyline(float horizon, int seed, Color body, float lit)
        {
            System.Random r = new System.Random(seed);
            float x = -20f;
            while (x < UIKit.W)
            {
                float w = 60f + (float)r.NextDouble() * 110f;
                float h = 90f + (float)r.NextDouble() * 360f;
                UIKit.Fill(new Rect(x, horizon - h, w, h), body);
                for (float wy = horizon - h + 18f; wy < horizon - 24f; wy += 34f)
                    for (float wx = x + 12f; wx < x + w - 18f; wx += 26f)
                        if (r.NextDouble() < lit) UIKit.Fill(new Rect(wx, wy, 10f, 14f), new Color(1f, 0.85f, 0.45f, 0.9f));
                x += w + 4f;
            }
        }

        static void Hills(float horizon, int seed, Color c, float amp)
        {
            System.Random r = new System.Random(seed);
            float ph1 = (float)r.NextDouble() * 6f, ph2 = (float)r.NextDouble() * 6f;
            const int cols = 36;
            float w = UIKit.W / cols;
            for (int i = 0; i < cols; i++)
            {
                float h = amp * (0.55f + 0.3f * Mathf.Sin(i * 0.35f + ph1) + 0.15f * Mathf.Sin(i * 0.9f + ph2));
                UIKit.Fill(new Rect(i * w, horizon - h, w + 1f, h + 2f), c);
            }
        }

        static void DrawBackground(string bg)
        {
            float H = UIKit.H, W = UIKit.W;
            switch (bg)
            {
                case "room":
                {
                    // a night window that fills the top of the screen, a dark desk below with a warm lamp glow
                    float wb = H * 0.50f;
                    Sky(new Color(0.04f, 0.05f, 0.13f), new Color(0.16f, 0.12f, 0.26f), wb);
                    StarField(0.6f);
                    Skyline(wb, 3, new Color(0.06f, 0.07f, 0.15f), 0.34f);
                    Skyline(wb, 21, new Color(0.04f, 0.05f, 0.11f), 0.22f);
                    Color frame = new Color(0.05f, 0.045f, 0.08f);
                    UIKit.Fill(new Rect(0f, 0f, 34f, wb), frame);
                    UIKit.Fill(new Rect(W - 34f, 0f, 34f, wb), frame);
                    UIKit.Fill(new Rect(W * 0.5f - 7f, 0f, 14f, wb), frame);
                    UIKit.Fill(new Rect(0f, wb * 0.5f - 7f, W, 14f), frame);
                    UIKit.Fill(new Rect(0f, wb - 8f, W, H - wb + 8f), new Color(0.07f, 0.065f, 0.11f));
                    UIKit.Fade(new Rect(0f, wb - 8f, W, H * 0.22f), new Color(0.45f, 0.30f, 0.16f, 0.30f), true);
                    UIKit.Fill(new Rect(0f, wb - 14f, W, 14f), new Color(0.13f, 0.11f, 0.17f));
                    break;
                }
                case "garage":
                    Sky(new Color(0.12f, 0.11f, 0.14f), new Color(0.22f, 0.19f, 0.20f), H);
                    UIKit.Fill(new Rect(0f, H * 0.55f, W, H * 0.45f), new Color(0.13f, 0.13f, 0.15f));
                    break;
                case "phone":
                    Sky(new Color(0.03f, 0.03f, 0.07f), new Color(0.06f, 0.06f, 0.14f), H);
                    UIKit.Round(new Rect(W * 0.22f, H * 0.08f, W * 0.56f, H * 0.46f), new Color(0.10f, 0.11f, 0.20f));
                    UIKit.Round(new Rect(W * 0.25f, H * 0.10f, W * 0.50f, H * 0.42f), new Color(0.04f, 0.05f, 0.10f));
                    UIKit.Round(new Rect(W * 0.29f, H * 0.16f, W * 0.34f, 70f), new Color(0.2f, 0.25f, 0.45f));
                    UIKit.Round(new Rect(W * 0.37f, H * 0.23f, W * 0.34f, 70f), new Color(0.15f, 0.55f, 0.50f));
                    UIKit.Round(new Rect(W * 0.29f, H * 0.30f, W * 0.20f, 70f), new Color(0.2f, 0.25f, 0.45f));
                    break;
                case "city":
                    Sky(new Color(0.03f, 0.04f, 0.10f), new Color(0.10f, 0.08f, 0.20f), H * 0.5f);
                    StarField(0.5f);
                    Skyline(H * 0.5f, 9, new Color(0.06f, 0.07f, 0.14f), 0.4f);
                    RoadPerspective(H * 0.5f, new Color(0.10f, 0.10f, 0.14f), new Color(0.9f, 0.85f, 0.5f), new Color(0.05f, 0.05f, 0.08f));
                    break;
                case "pass":
                    Sky(new Color(0.02f, 0.03f, 0.08f), new Color(0.08f, 0.09f, 0.18f), H * 0.5f);
                    StarField(0.9f);
                    UIKit.Circle(new Rect(W * 0.68f, H * 0.08f, 120f, 120f), new Color(0.88f, 0.9f, 1f));
                    Hills(H * 0.5f, 12, new Color(0.04f, 0.05f, 0.10f), 260f);
                    RoadPerspective(H * 0.5f, new Color(0.09f, 0.09f, 0.13f), new Color(0.8f, 0.8f, 0.85f), new Color(0.03f, 0.04f, 0.07f));
                    break;
                default: // road
                    Sky(new Color(0.03f, 0.04f, 0.11f), new Color(0.09f, 0.10f, 0.22f), H * 0.5f);
                    StarField(0.8f);
                    UIKit.Circle(new Rect(W * 0.14f, H * 0.09f, 100f, 100f), new Color(0.88f, 0.9f, 1f));
                    Hills(H * 0.5f, 4, new Color(0.04f, 0.06f, 0.10f), 200f);
                    RoadPerspective(H * 0.5f, new Color(0.10f, 0.10f, 0.14f), new Color(0.85f, 0.85f, 0.9f), new Color(0.04f, 0.06f, 0.08f));
                    break;
            }
        }
    }
}
