using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace MidnightLegacy
{
    /// <summary>
    /// Multi touch input. Hit tests raw touches against fixed screen zones, so no EventSystem or UI is needed
    /// and holding LEFT while tapping BRAKE works. Also reads keyboard (A/D or arrows, S = brake, Space = handbrake,
    /// LeftShift = nitro) and the mouse for testing in the Editor.
    /// Layout is in normalised screen space, origin bottom left.
    /// A script (tutorial, cutscenes) can take over with UseScript.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class InputReader : MonoBehaviour
    {
        public static readonly Rect LeftZone = new Rect(0f, 0f, 0.5f, 0.30f);
        public static readonly Rect RightZone = new Rect(0.5f, 0f, 0.5f, 0.30f);
        public static readonly Rect BrakeButton = new Rect(0.04f, 0.33f, 0.26f, 0.085f);
        public static readonly Rect NitroButton = new Rect(0.37f, 0.33f, 0.26f, 0.085f);
        public static readonly Rect HandbrakeButton = new Rect(0.70f, 0.33f, 0.26f, 0.085f);

        public bool BrakeEnabled = true;
        public bool HandbrakeEnabled = true;
        public bool NitroEnabled = true;
        /// <summary>Set true to ignore all input (menus, cutscenes).</summary>
        public bool Locked;

        /// <summary>When true the Script* values replace real input.</summary>
        public bool UseScript;
        public float ScriptSteer;
        public bool ScriptBrake;
        public bool ScriptHandbrake;
        public bool ScriptNitro;

        bool left, right, brake, hand, nitro;

        public bool LeftDown { get { return UseScript ? ScriptSteer < -0.5f : left; } }
        public bool RightDown { get { return UseScript ? ScriptSteer > 0.5f : right; } }
        public bool Brake { get { return UseScript ? ScriptBrake : brake; } }
        public bool Handbrake { get { return UseScript ? ScriptHandbrake : hand; } }
        public bool Nitro { get { return UseScript ? ScriptNitro : nitro; } }

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
            bool l = false, r = false, b = false, h = false, n = false;

            if (!Locked)
            {
                pointers.Clear();
                var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
                for (int i = 0; i < touches.Count; i++) pointers.Add(touches[i].screenPosition);

                Mouse mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed) pointers.Add(mouse.position.ReadValue());

                float w = Mathf.Max(1f, Screen.width);
                float hgt = Mathf.Max(1f, Screen.height);
                for (int i = 0; i < pointers.Count; i++)
                {
                    Vector2 p = new Vector2(pointers[i].x / w, pointers[i].y / hgt);
                    if (BrakeEnabled && BrakeButton.Contains(p)) b = true;
                    else if (NitroEnabled && NitroButton.Contains(p)) n = true;
                    else if (HandbrakeEnabled && HandbrakeButton.Contains(p)) h = true;
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
                }
            }

            left = l;
            right = r;
            brake = b;
            hand = h;
            nitro = n;
        }

        /// <summary>Forget every pressed button (used when a menu opens over the game).</summary>
        public void ClearPresses()
        {
            left = right = brake = hand = nitro = false;
        }
    }
}
