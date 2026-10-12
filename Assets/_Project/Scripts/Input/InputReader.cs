using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace MidnightLegacy
{
    /// <summary>
    /// Multi touch input. Hit tests raw touches against fixed screen zones, so no EventSystem or UI is needed
    /// and holding LEFT while tapping BRAKE works. Also reads keyboard (A/D or arrows, S = brake, Space = drift,
    /// LeftShift or W = nitro, R = reverse) and the mouse for testing in the Editor.
    /// Layout is in normalised screen space, origin bottom left.
    ///
    /// The layout: two big steering pads along the bottom edge, and a row of round buttons just above them,
    /// BRAKE and REVERSE on the left, DRIFT and NITRO on the right. The middle of the screen stays clear so the car is
    /// never hidden behind a button. The round buttons are always circles: their normalised height is worked out from
    /// the screen shape.
    /// A script (tutorial, cutscenes) can take over with UseScript.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class InputReader : MonoBehaviour
    {
        /// <summary>Touch area of the steering. Taller than the drawn pads so a thumb that slides up still steers.</summary>
        public static Rect LeftZone { get { return new Rect(0f, 0f, 0.5f, 0.36f); } }
        public static Rect RightZone { get { return new Rect(0.5f, 0f, 0.5f, 0.36f); } }

        /// <summary>Where the steering pads are drawn.</summary>
        public static Rect LeftPad { get { return new Rect(0.025f, 0.015f, 0.455f, 0.205f); } }
        public static Rect RightPad { get { return new Rect(0.52f, 0.015f, 0.455f, 0.205f); } }

        public static Rect BrakeButton { get { return Round(0.135f, 0.305f, 0.21f); } }
        public static Rect ReverseButton { get { return Round(0.345f, 0.285f, 0.15f); } }
        public static Rect HandbrakeButton { get { return Round(0.655f, 0.285f, 0.17f); } }
        public static Rect NitroButton { get { return Round(0.865f, 0.305f, 0.21f); } }

        /// <summary>A circle: centre (cx, cy) and diameter as fractions of the screen WIDTH, returned as a normalised rect.</summary>
        static Rect Round(float cx, float cy, float diameterW)
        {
            float aspect = Mathf.Max(0.3f, (float)Screen.width / Mathf.Max(1, Screen.height));
            float h = diameterW * aspect;
            return new Rect(cx - diameterW * 0.5f, cy - h * 0.5f, diameterW, h);
        }

        public bool BrakeEnabled = true;
        public bool HandbrakeEnabled = true;
        public bool NitroEnabled = true;
        public bool ReverseEnabled = true;
        /// <summary>Set true to ignore all input (menus, cutscenes).</summary>
        public bool Locked;

        /// <summary>When true the Script* values replace real input.</summary>
        public bool UseScript;
        public float ScriptSteer;
        public bool ScriptBrake;
        public bool ScriptHandbrake;
        public bool ScriptNitro;

        bool left, right, brake, hand, nitro, reverse;

        public bool LeftDown { get { return UseScript ? ScriptSteer < -0.5f : left; } }
        public bool RightDown { get { return UseScript ? ScriptSteer > 0.5f : right; } }
        public bool Brake { get { return UseScript ? ScriptBrake : brake; } }
        public bool Handbrake { get { return UseScript ? ScriptHandbrake : hand; } }
        public bool Nitro { get { return UseScript ? ScriptNitro : nitro; } }
        public bool Reverse { get { return !UseScript && reverse; } }

        /// <summary>-1 left, 0 straight, +1 right. Both buttons together cancel out.</summary>
        public float Steer
        {
            get
            {
                if (UseScript) return Mathf.Clamp(ScriptSteer, -1f, 1f);
                return (right ? 1f : 0f) - (left ? 1f : 0f);
            }
        }

        readonly List<Vector2> pointers = new List<Vector2>(10);

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            bool l = false, r = false, b = false, h = false, n = false, rev = false;

            if (!Locked)
            {
                pointers.Clear();
                var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
                for (int i = 0; i < touches.Count; i++) pointers.Add(touches[i].screenPosition);

                Mouse mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed) pointers.Add(mouse.position.ReadValue());

                float w = Mathf.Max(1f, Screen.width);
                float hgt = Mathf.Max(1f, Screen.height);
                Rect brakeR = BrakeButton, nitroR = NitroButton, handR = HandbrakeButton, revR = ReverseButton;
                for (int i = 0; i < pointers.Count; i++)
                {
                    Vector2 p = new Vector2(pointers[i].x / w, pointers[i].y / hgt);
                    if (BrakeEnabled && brakeR.Contains(p)) b = true;
                    else if (NitroEnabled && nitroR.Contains(p)) n = true;
                    else if (HandbrakeEnabled && handR.Contains(p)) h = true;
                    else if (ReverseEnabled && revR.Contains(p)) rev = true;
                    else if (LeftZone.Contains(p)) l = true;
                    else if (RightZone.Contains(p)) r = true;
                }

                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) l = true;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) r = true;
                    if (BrakeEnabled && (kb.sKey.isPressed || kb.downArrowKey.isPressed)) b = true;
                    if (HandbrakeEnabled && kb.spaceKey.isPressed) h = true;
                    if (NitroEnabled && (kb.leftShiftKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed)) n = true;
                    if (ReverseEnabled && kb.rKey.isPressed) rev = true;
                }
            }

            left = l;
            right = r;
            brake = b;
            hand = h;
            nitro = n;
            reverse = rev;
        }

        /// <summary>Forget every pressed button (used when a menu opens over the game).</summary>
        public void ClearPresses()
        {
            left = right = brake = hand = nitro = reverse = false;
        }
    }
}
