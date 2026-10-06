using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace MidnightLegacy
{
    /// <summary>
    /// Multi touch input. Hit tests raw touches against fixed screen zones, so no EventSystem or UI is needed
    /// and holding LEFT while tapping BRAKE works. Also reads keyboard (A/D or arrows, S = brake, Space = handbrake)
    /// and the mouse for testing in the Editor.
    /// Layout is in normalised screen space, origin bottom left.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class InputReader : MonoBehaviour
    {
        public static readonly Rect LeftZone = new Rect(0f, 0f, 0.5f, 0.30f);
        public static readonly Rect RightZone = new Rect(0.5f, 0f, 0.5f, 0.30f);
        public static readonly Rect BrakeButton = new Rect(0.04f, 0.33f, 0.26f, 0.085f);
        public static readonly Rect HandbrakeButton = new Rect(0.70f, 0.33f, 0.26f, 0.085f);

        public bool BrakeEnabled = true;
        public bool HandbrakeEnabled = true;
        /// <summary>Set true to ignore all input (menus, cutscenes).</summary>
        public bool Locked;

        public bool LeftDown { get; private set; }
        public bool RightDown { get; private set; }
        public bool Brake { get; private set; }
        public bool Handbrake { get; private set; }

        /// <summary>-1 left, 0 straight, +1 right. Both buttons together cancel out.</summary>
        public float Steer
        {
            get { return (RightDown ? 1f : 0f) - (LeftDown ? 1f : 0f); }
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
            bool left = false, right = false, brake = false, hand = false;

            if (!Locked)
            {
                pointers.Clear();
                var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
                for (int i = 0; i < touches.Count; i++) pointers.Add(touches[i].screenPosition);

                Mouse mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed) pointers.Add(mouse.position.ReadValue());

                float w = Mathf.Max(1f, Screen.width);
                float h = Mathf.Max(1f, Screen.height);
                for (int i = 0; i < pointers.Count; i++)
                {
                    Vector2 n = new Vector2(pointers[i].x / w, pointers[i].y / h);
                    if (BrakeEnabled && BrakeButton.Contains(n)) brake = true;
                    else if (HandbrakeEnabled && HandbrakeButton.Contains(n)) hand = true;
                    else if (LeftZone.Contains(n)) left = true;
                    else if (RightZone.Contains(n)) right = true;
                }

                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) left = true;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) right = true;
                    if (BrakeEnabled && (kb.sKey.isPressed || kb.downArrowKey.isPressed)) brake = true;
                    if (HandbrakeEnabled && kb.spaceKey.isPressed) hand = true;
                }
            }

            LeftDown = left;
            RightDown = right;
            Brake = brake;
            Handbrake = hand;
        }
    }
}
