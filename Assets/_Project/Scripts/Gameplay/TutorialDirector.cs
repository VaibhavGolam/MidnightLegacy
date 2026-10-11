using System;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// Guided drive for the first race. It never takes control: it watches what the player does and shows one
    /// prompt at a time (steer right, steer left, taps, brake, drift, nitro). Every step has a time limit so the
    /// player can never get stuck. The HUD reads Prompt, Body, Highlight and Progress01.
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        sealed class TStep
        {
            public string title;
            public string body;
            public Rect highlight;      // normalised screen rect, origin bottom left (same space as InputReader)
            public float need;          // seconds or counts needed
            public int kind;            // 0 hold right, 1 hold left, 2 taps, 3 brake, 4 drift, 5 nitro, 6 free
        }

        static readonly Rect BothZones = new Rect(0f, 0f, 1f, 0.30f);

        readonly TStep[] steps =
        {
            new TStep { title = "Steer right", body = "Hold the RIGHT side of the screen. The longer you hold, the harder you turn.", highlight = InputReader.RightZone, need = 0.8f, kind = 0 },
            new TStep { title = "Steer left", body = "Now hold the LEFT side.", highlight = InputReader.LeftZone, need = 0.8f, kind = 1 },
            new TStep { title = "Tap to nudge", body = "Short taps make small corrections. Tap either side 4 times.", highlight = BothZones, need = 4f, kind = 2 },
            new TStep { title = "Brake", body = "Hold BRAKE for a moment. Brake before corners, not in the middle of them.", highlight = InputReader.BrakeButton, need = 0.6f, kind = 3 },
            new TStep { title = "Drift", body = "Steer into a corner and hold DRIFT. The rear slides, release to grip again.", highlight = InputReader.HandbrakeButton, need = 0.5f, kind = 4 },
            new TStep { title = "Nitro", body = "Press NITRO for a burst of speed. It refills from near misses and clean corners.", highlight = InputReader.NitroButton, need = 0.7f, kind = 5 },
            new TStep { title = "Reach the finish", body = "That is everything. Follow the road to the finish line.", highlight = new Rect(0f, 0f, 0f, 0f), need = 0f, kind = 6 }
        };

        CarController car;
        InputReader input;
        RaceManager race;
        int index;
        float progress, stepTime, flash, freeTime;
        bool wasDown;
        float downTime;

        public bool Done { get; private set; }
        public string Prompt { get { return index < steps.Length ? steps[index].title : ""; } }
        public string Body { get { return index < steps.Length ? steps[index].body : ""; } }
        public int StepIndex { get { return index; } }
        public int StepCount { get { return steps.Length; } }
        public bool HasHighlight { get { return index < steps.Length && steps[index].highlight.width > 0f; } }
        public Rect Highlight { get { return index < steps.Length ? steps[index].highlight : new Rect(); } }
        public float Progress01
        {
            get
            {
                if (index >= steps.Length) return 1f;
                float need = steps[index].need;
                return need <= 0f ? 0f : Mathf.Clamp01(progress / need);
            }
        }
        public string FlashText { get { return flash > 0f ? "Nice!" : null; } }
        public bool Visible { get { return race != null && race.State == RaceState.Racing && index < steps.Length; } }

        public void Init(CarController playerCar, InputReader inputReader, RaceManager raceManager)
        {
            car = playerCar;
            input = inputReader;
            race = raceManager;
            index = 0;
            progress = 0f;
            stepTime = 0f;
            Done = false;
            ApplyStepRules();
        }

        /// <summary>The tutorial restarts from the beginning when the run restarts.</summary>
        public void Restart()
        {
            index = 0;
            progress = 0f;
            stepTime = 0f;
            Done = false;
            ApplyStepRules();
        }

        void ApplyStepRules()
        {
            if (input == null) return;
            int k = index < steps.Length ? steps[index].kind : 6;
            // buttons appear when they are taught
            input.BrakeEnabled = k >= 3;
            input.HandbrakeEnabled = k >= 4;
            input.NitroEnabled = k >= 5;
            if (k == 5 && car != null) car.AddNitro(0.8f);
        }

        void Next()
        {
            index++;
            progress = 0f;
            stepTime = 0f;
            flash = 1.0f;
            if (AudioManager.I != null) AudioManager.I.Play(Sfx.Confirm, 0.8f);
            ApplyStepRules();
            if (index >= steps.Length) Done = true;
        }

        void Update()
        {
            if (race == null || car == null || input == null) return;
            if (flash > 0f) flash -= Time.unscaledDeltaTime;
            if (race.State != RaceState.Racing || index >= steps.Length) return;

            float dt = Time.deltaTime;
            stepTime += dt;
            TStep s = steps[index];

            bool down = false;
            switch (s.kind)
            {
                case 0: if (input.RightDown) progress += dt; break;
                case 1: if (input.LeftDown) progress += dt; break;
                case 2:
                    down = input.LeftDown || input.RightDown;
                    if (down) downTime += dt;
                    if (wasDown && !down)
                    {
                        if (downTime < 0.3f) progress += 1f;
                        downTime = 0f;
                    }
                    wasDown = down;
                    break;
                case 3: if (input.Brake) progress += dt; break;
                case 4:
                    if (input.Handbrake && car.SpeedMS > 12f && (input.LeftDown || input.RightDown)) progress += dt;
                    else if (car.SlideAmount > 0.5f) progress += dt * 0.7f;
                    break;
                case 5: if (car.NitroActive) progress += dt; break;
                default:
                    freeTime += dt;
                    break;
            }

            if (s.kind != 6 && progress >= s.need) Next();
            else if (s.kind != 6 && stepTime > 28f) Next();   // never block the player
        }
    }
}
