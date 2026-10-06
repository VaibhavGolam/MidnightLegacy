using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Minimal prototype HUD drawn with IMGUI so it needs no scene setup: speed, distance, slide warning,
    /// FPS, the on-screen buttons and the TUNE button. Phase 5 replaces this with a proper UI.
    /// Everything is laid out in a 1080 wide reference space and scaled to the screen.
    /// </summary>
    public sealed class HUDView : MonoBehaviour
    {
        CarController car;
        InputReader input;
        TuningPanel tuning;

        GUIStyle bigStyle, smallStyle, buttonStyle, tuneStyle, warnStyle, fpsStyle;
        bool stylesReady;

        float timer;
        float fpsAccum;
        int fpsFrames;
        string speedText = "0";
        string distText = "0 m";
        string fpsText = "";

        public void Init(CarController carController, InputReader inputReader, TuningPanel tuningPanel)
        {
            car = carController;
            input = inputReader;
            tuning = tuningPanel;
        }

        void Update()
        {
            fpsAccum += Time.unscaledDeltaTime;
            fpsFrames++;
            timer += Time.unscaledDeltaTime;
            if (timer >= 0.15f && car != null)
            {
                speedText = Mathf.RoundToInt(car.SpeedKph).ToString();
                distText = Mathf.RoundToInt(Mathf.Max(0f, car.DistanceDriven)).ToString() + " m";
                if (fpsAccum > 0f) fpsText = Mathf.RoundToInt(fpsFrames / fpsAccum).ToString() + " fps";
                fpsAccum = 0f;
                fpsFrames = 0;
                timer = 0f;
            }
        }

        void EnsureStyles()
        {
            if (stylesReady) return;
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 120, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            bigStyle.normal.textColor = Color.white;
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
            smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            buttonStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            buttonStyle.normal.textColor = Color.white;
            tuneStyle = new GUIStyle(GUI.skin.button) { fontSize = 34, fontStyle = FontStyle.Bold };
            warnStyle = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            warnStyle.normal.textColor = new Color(1f, 0.55f, 0.25f);
            fpsStyle = new GUIStyle(smallStyle) { alignment = TextAnchor.MiddleRight, fontSize = 28 };
            stylesReady = true;
        }

        static Rect ToGui(Rect n, float w, float h)
        {
            // normalised, origin bottom left  ->  reference space, origin top left
            return new Rect(n.x * w, (1f - n.y - n.height) * h, n.width * w, n.height * h);
        }

        void DrawButton(Rect r, string label, bool pressed)
        {
            Color old = GUI.color;
            GUI.color = pressed ? new Color(0.42f, 0.47f, 0.84f, 0.60f) : new Color(1f, 1f, 1f, 0.12f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, pressed ? 0.95f : 0.55f);
            GUI.Label(r, label, buttonStyle);
            GUI.color = old;
        }

        void OnGUI()
        {
            if (car == null || input == null) return;
            EnsureStyles();

            float k = Screen.width / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(k, k, 1f));
            float w = 1080f;
            float h = Screen.height / k;

            bool tuningOpen = tuning != null && tuning.Visible;

            if (!tuningOpen)
            {
                GUI.Label(new Rect(0f, 40f, w, 150f), speedText, bigStyle);
                GUI.Label(new Rect(0f, 170f, w, 50f), "km/h", smallStyle);
                GUI.Label(new Rect(0f, 215f, w, 50f), distText, smallStyle);
                if (car.IsSliding) GUI.Label(new Rect(0f, 275f, w, 70f), "SLIDE", warnStyle);
            }

            GUI.Label(new Rect(w - 220f, 8f, 210f, 40f), fpsText, fpsStyle);
            if (!tuningOpen && tuning != null)
            {
                if (GUI.Button(new Rect(w - 190f, 56f, 170f, 70f), "TUNE", tuneStyle)) tuning.Visible = true;
            }

            // steering and aux buttons (touch handling lives in InputReader)
            Rect left = ToGui(new Rect(0.03f, 0.03f, 0.44f, 0.15f), w, h);
            Rect right = ToGui(new Rect(0.53f, 0.03f, 0.44f, 0.15f), w, h);
            DrawButton(left, "<", input.LeftDown);
            DrawButton(right, ">", input.RightDown);
            if (input.BrakeEnabled) DrawButton(ToGui(InputReader.BrakeButton, w, h), "BRAKE", input.Brake);
            if (input.HandbrakeEnabled) DrawButton(ToGui(InputReader.HandbrakeButton, w, h), "DRIFT", input.Handbrake);
        }
    }
}
