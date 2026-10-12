using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace MidnightLegacy
{
    /// <summary>
    /// The game's single entry point. It creates itself when the first scene loads (no scene setup needed), starts
    /// every system, owns the screens and the flow between them (menu, hub, dialogue, race, results), and handles
    /// saving, ads and purchases on behalf of the screens.
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        public const string Version = "0.7.0";
        /// <summary>Replace with the real privacy policy page before publishing (Google Play requires one).</summary>
        public const string PrivacyUrl = "https://shubhamtople.com/midnight-legacy-privacy";

        public static AppRoot I { get; private set; }

        public SaveData Save { get { return SaveSystem.Data; } }
        public Camera Cam { get; private set; }
        public InputReader Input { get; private set; }
        public GarageStage Stage { get; private set; }
        public AdaptiveQuality Adaptive { get; private set; }
        public DriveSession Session { get; private set; }
        public RaceResult PendingResult { get; private set; }

        readonly Dictionary<Scr, ScreenBase> screens = new Dictionary<Scr, ScreenBase>();
        ScreenBase current;
        Scr currentId = Scr.Splash;
        bool hasPending;
        Scr pendingId;

        DialogueScreen dialogue;
        HubScreen hub;
        bool committed;

        bool saveDirty;
        float saveTimer;
        string toast;
        float toastTime;

        List<RaceDef> sideCache, legendCache;
        readonly List<MessageDef> mailList = new List<MessageDef>();

        // ------------------------------------------------------------------ start up

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (I != null) return;
            GameObject go = new GameObject("MidnightLegacy");
            go.AddComponent<AppRoot>();
        }

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            Application.runInBackground = false;
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Physics.simulationMode = SimulationMode.Script;   // the game uses no physics, nothing should be simulated

            SaveSystem.Load();
            ContentDB.Init();
            StoryContent.Init();
            Monetization.Init();
            ApplyConsent();

            GraphicsSettings.Apply(GraphicsSettings.LoadSaved(), false);
            BuildCategoryCaches();

            SetupCamera();

            AudioManager.Create(transform);
            AudioManager.I.Apply(Save.settings);
            AudioManager.I.SetMood(MusicMood.Menu);

            GameObject ig = new GameObject("Input");
            ig.transform.SetParent(transform, false);
            Input = ig.AddComponent<InputReader>();
            Input.Locked = true;

            GameObject sg = new GameObject("Stage");
            sg.transform.SetParent(transform, false);
            Stage = sg.AddComponent<GarageStage>();
            Stage.Build(Cam);
            Stage.Show(false);

            Adaptive = gameObject.AddComponent<AdaptiveQuality>();
            Adaptive.ResetToPreset();

            dialogue = new DialogueScreen();
            hub = new HubScreen();
            Register(Scr.Splash, new SplashScreen());
            Register(Scr.Consent, new ConsentScreen());
            Register(Scr.Menu, new MenuScreen());
            Register(Scr.Hub, hub);
            Register(Scr.Dialogue, dialogue);
            Register(Scr.Drive, new DriveHUD());
            Register(Scr.Results, new ResultsScreen());

            RefreshStage();
            Go(Scr.Splash);
        }

        void Register(Scr id, ScreenBase s)
        {
            s.Attach(this);
            screens[id] = s;
        }

        void SetupCamera()
        {
            Cam = Camera.main;
            if (Cam == null)
            {
                GameObject cg = new GameObject("MainCamera");
                cg.tag = "MainCamera";
                Cam = cg.AddComponent<Camera>();
            }
            if (Cam.GetComponent<AudioListener>() == null) Cam.gameObject.AddComponent<AudioListener>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.03f, 0.04f, 0.09f);
            Cam.orthographic = true;
            Cam.orthographicSize = 8f;
            Cam.nearClipPlane = 1f;
            Cam.farClipPlane = 120f;
            Cam.allowHDR = false;
            Cam.allowMSAA = true;
            UniversalAdditionalCameraData data = Cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = false;
                data.renderShadows = false;
                data.antialiasing = AntialiasingMode.None;
            }
            // other cameras and listeners from a sample scene would double render and double audio
            Camera[] all = Camera.allCameras;
            for (int i = 0; i < all.Length; i++) if (all[i] != Cam) all[i].enabled = false;
            AudioListener[] ls = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            for (int i = 0; i < ls.Length; i++) if (ls[i].gameObject != Cam.gameObject) ls[i].enabled = false;
        }

        void BuildCategoryCaches()
        {
            sideCache = new List<RaceDef>();
            legendCache = new List<RaceDef>();
            for (int i = 0; i < ContentDB.Races.Count; i++)
            {
                RaceDef d = ContentDB.Races[i];
                if (d.story || d.kind == RaceKind.Tutorial) continue;
                if (d.legend) legendCache.Add(d);
                else sideCache.Add(d);
            }
            sideCache.Sort((a, b) => a.minRep.CompareTo(b.minRep));
        }

        // ------------------------------------------------------------------ update loop

        void OnEnable()
        {
            EventBus.Subscribe<RaceFinished>(OnRaceFinished);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<RaceFinished>(OnRaceFinished);
        }

        void OnRaceFinished(RaceFinished e)
        {
            // the results screen opens after the slow motion finish (RaceManager.ReadyForResults)
        }

        void Update()
        {
            if (hasPending)
            {
                hasPending = false;
                if (current != null) current.OnExit();
                currentId = pendingId;
                current = screens[currentId];
                current.OnEnter();
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && current != null) current.OnBack();

            if (currentId == Scr.Drive && Session != null && Session.Race != null)
            {
                RaceManager rm = Session.Race;
                if (rm.ReadyForResults && rm.Result != null && PendingResult == null)
                {
                    PendingResult = rm.Result;
                    committed = false;
                    Economy.Prepare(PendingResult, Save);
                    AdPolicy.RegisterRaceFinished();
                    Time.timeScale = 0f;      // freeze the world behind the results screen
                    Go(Scr.Results);
                }
            }

            Adaptive.Active = currentId == Scr.Drive && Session != null && !Session.Paused;

            if (toastTime > 0f) toastTime -= Time.unscaledDeltaTime;
            if (saveDirty)
            {
                saveTimer -= Time.unscaledDeltaTime;
                if (saveTimer <= 0f && !UIKit.PointerDown) SaveNow();
            }
        }

        void OnGUI()
        {
            if (current == null) return;
            UIKit.Begin();
            current.Draw();
            DrawToast();
            UIKit.End();
        }

        void DrawToast()
        {
            if (toastTime <= 0f || string.IsNullOrEmpty(toast)) return;
            float a = Mathf.Clamp01(toastTime / 0.4f);
            Rect r = new Rect(120f, UIKit.Safe.yMax - 330f, UIKit.W - 240f, 84f);
            UIKit.Round(r, new Color(0.02f, 0.02f, 0.06f, 0.92f * a));
            UIKit.Label(r, toast, 30, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, true);
        }

        public void Toast(string message)
        {
            toast = message;
            toastTime = 2.4f;
        }

        // ------------------------------------------------------------------ navigation

        public void Go(Scr id)
        {
            pendingId = id;
            hasPending = true;
        }

        public void AfterSplash()
        {
            Go(Save.settings.adConsent < 0 ? Scr.Consent : Scr.Menu);
        }

        public void SetConsent(int value)
        {
            Save.settings.adConsent = value;
            ApplyConsent();
            SaveNow();
            Go(Scr.Menu);
        }

        public void ApplyConsent()
        {
            if (Monetization.Ads != null) Monetization.Ads.SetPersonalisedAdsAllowed(Save.settings.adConsent == 1);
        }

        public void ApplySettings()
        {
            if (AudioManager.I != null) AudioManager.I.Apply(Save.settings);
            if (Adaptive != null) Adaptive.ResetToPreset();
            if (Session != null && Session.Chase != null) Session.Chase.shakeScale = Save.settings.reduceMotion ? 0.25f : 1f;
            MarkDirty();
        }

        void MarkDirty()
        {
            saveDirty = true;
            saveTimer = 1.5f;
        }

        public void SaveNow()
        {
            saveDirty = false;
            SaveSystem.Save();
        }

        public void ResetSave()
        {
            SettingsData keep = Save.settings;
            SaveSystem.Delete();
            Save.settings = keep;
            SaveNow();
            RefreshStage();
        }

        // ------------------------------------------------------------------ garage stage

        CarLook preview;

        public void RefreshStage()
        {
            preview = null;
            if (Stage == null) return;
            Stage.SetLook(Cosmetics.LookFor(Save));
            Stage.SetCover(!Save.HasFlag("car_uncovered"));
        }

        public void PreviewCosmetic(int category, CosmeticItem item)
        {
            CarLook look = Cosmetics.LookFor(Save);
            switch (category)
            {
                case 0: look.body = item.color; break;
                case 1: look.rim = item.color; look.rimStyle = item.value; break;
                case 2: look.decal = item.value; look.decalColor = item.color; break;
                default: look.spoiler = item.value; break;
            }
            preview = look;
            Stage.SetLook(look);
        }

        public void OpenShop()
        {
            if (currentId == Scr.Hub) hub.SelectTab(HubTab.Shop);
        }

        // ------------------------------------------------------------------ story

        public StoryStep CurrentStep
        {
            get { return Save.storyStep >= 0 && Save.storyStep < StoryContent.Steps.Count ? StoryContent.Steps[Save.storyStep] : null; }
        }

        public bool StepAvailable(StoryStep s)
        {
            return s != null && Save.rep >= s.minRep;
        }

        public void StartNewGame(string playerName)
        {
            Save.playerName = string.IsNullOrEmpty(playerName) ? "Jun" : playerName;
            Save.introSeen = true;
            Save.storyStep = 0;
            SaveNow();
            RefreshStage();
            RunStep(CurrentStep);
        }

        public void ContinueStory()
        {
            StoryStep s = CurrentStep;
            if (s == null || !StepAvailable(s)) return;
            RunStep(s);
        }

        void RunStep(StoryStep step)
        {
            if (step == null) { Go(Scr.Hub); return; }
            if (step.kind == StepKind.Dialogue)
            {
                PlayDialogue(step.refId, () =>
                {
                    if (CurrentStep == step) Save.storyStep++;
                    SaveNow();
                    AutoNext(true);
                });
            }
            else
            {
                RaceDef d = ContentDB.Race(step.refId);
                if (d == null) { Go(Scr.Hub); return; }
                LaunchRace(d);
            }
        }

        /// <summary>After a story scene: carry straight on when the next step is a scene, or follows one.</summary>
        void AutoNext(bool lastWasDialogue)
        {
            StoryStep n = CurrentStep;
            if (n != null && StepAvailable(n) && (n.kind == StepKind.Dialogue || lastWasDialogue)) RunStep(n);
            else Go(Scr.Hub);
        }

        public void PlayDialogue(string id, Action onDone)
        {
            DialogueDef d = StoryContent.Dialogue(id);
            if (d == null)
            {
                if (onDone != null) onDone();
                return;
            }
            dialogue.Begin(d, onDone);
            Go(Scr.Dialogue);
        }

        public void ApplyEffect(string fx)
        {
            if (string.IsNullOrEmpty(fx)) return;
            int c = fx.IndexOf(':');
            if (c < 0) return;
            string key = fx.Substring(0, c);
            string val = fx.Substring(c + 1);
            int n;
            switch (key)
            {
                case "flag":
                    Save.SetFlag(val);
                    break;
                case "rep":
                    if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                    {
                        Save.rep = Mathf.Max(0, Save.rep + n);
                        if (n > 0) Toast("+" + n + " reputation");
                    }
                    break;
                case "cash":
                    if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                    {
                        Save.cash = Mathf.Max(0, Save.cash + n);
                        if (n > 0) Toast("+" + UIKit.Money(n));
                    }
                    break;
                case "stage":
                    if (val == "uncover")
                    {
                        Save.SetFlag("car_uncovered");
                        if (Stage != null) Stage.SetCover(false);
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ events and messages

        public List<RaceDef> SideEvents() { return sideCache; }
        public List<RaceDef> LegendEvents() { return legendCache; }

        public bool RaceUnlocked(RaceDef d, out string why)
        {
            why = null;
            if (!string.IsNullOrEmpty(d.requiresFlag) && !Save.HasFlag(d.requiresFlag)) { why = "Finish the story"; return false; }
            if (Save.rep < d.minRep) { why = "Needs " + d.minRep + " rep"; return false; }
            return true;
        }

        public List<MessageDef> VisibleMessages()
        {
            mailList.Clear();
            List<MessageDef> all = ContentDB.Messages;
            for (int i = 0; i < all.Count; i++)
            {
                MessageDef m = all[i];
                if (!string.IsNullOrEmpty(m.requiresFlag) && !Save.HasFlag(m.requiresFlag)) continue;
                if (Save.rep < m.minRep) continue;
                mailList.Add(m);
            }
            return mailList;
        }

        public int UnreadCount()
        {
            int n = 0;
            List<MessageDef> all = ContentDB.Messages;
            for (int i = 0; i < all.Count; i++)
            {
                MessageDef m = all[i];
                if (!string.IsNullOrEmpty(m.requiresFlag) && !Save.HasFlag(m.requiresFlag)) continue;
                if (Save.rep < m.minRep) continue;
                if (!Save.readMessages.Contains(m.id)) n++;
            }
            return n;
        }

        public void MarkMailRead()
        {
            List<MessageDef> list = VisibleMessages();
            bool any = false;
            for (int i = 0; i < list.Count; i++)
                if (!Save.readMessages.Contains(list[i].id)) { Save.readMessages.Add(list[i].id); any = true; }
            if (any) MarkDirty();
        }

        // ------------------------------------------------------------------ racing

        public void LaunchRace(RaceDef def)
        {
            string why;
            if (!RaceUnlocked(def, out why)) { Toast(why); return; }
            if (def.id == "legend_daily") def = ContentDB.DailyVariant(def);

            string seen = "seen_" + def.id;
            if (def.story && !string.IsNullOrEmpty(def.dialogueBefore) && !Save.HasFlag(seen))
            {
                Save.SetFlag(seen);
                RaceDef d = def;
                PlayDialogue(def.dialogueBefore, () => StartRaceNow(d));
                return;
            }
            StartRaceNow(def);
        }

        public void StartFreeDrive(string theme, string time, string weather)
        {
            RaceDef d = new RaceDef
            {
                id = Economy.FreeDriveId, title = "Free Drive", kind = RaceKind.Endless,
                theme = theme, time = time, weather = weather, seed = UnityEngine.Random.Range(1, 90000),
                length = 0f, traffic = 1, difficulty = 0.4f
            };
            StartRaceNow(d);
        }

        void StartRaceNow(RaceDef def)
        {
            TeardownSession();
            PendingResult = null;
            committed = false;
            Stage.Show(false);

            float offset = 0f;
            if (def.story && Save.settings.storyAssist) offset = -0.05f * Mathf.Clamp(Save.storyLossStreak - 1, 0, 4);

            Session = DriveSession.Create(def, Save, Cam, Input, offset);
            Adaptive.ResetToPreset();
            Go(Scr.Drive);
        }

        void TeardownSession()
        {
            if (Session != null)
            {
                Session.Teardown();
                Session = null;
            }
            Time.timeScale = 1f;
        }

        public void QuitToHub()
        {
            CommitResult();
            PendingResult = null;
            TeardownSession();
            Go(Scr.Hub);
        }

        /// <summary>Writes the finished race to the save. Safe to call more than once.</summary>
        public void CommitResult()
        {
            if (PendingResult == null || committed) return;
            committed = true;
            RaceResult r = PendingResult;
            float dmg = Session != null && Session.Car != null ? Session.Car.Damage01 : Save.damage;
            Economy.Apply(r, Save, dmg);

            RaceDef def = r.def;
            if (r.won && !string.IsNullOrEmpty(def.setsFlag)) Save.SetFlag(def.setsFlag);
            if (def.story)
            {
                if (r.won)
                {
                    Save.storyLossStreak = 0;
                    StoryStep cur = CurrentStep;
                    if (cur != null && cur.kind == StepKind.Race && cur.refId == def.id) Save.storyStep++;
                }
                else Save.storyLossStreak++;
            }
            SaveNow();
        }

        public void DoubleBonus()
        {
            if (PendingResult == null) return;
            int extra = Economy.DoubleBonus(PendingResult, Save, committed);
            if (extra > 0)
            {
                Toast("+" + UIKit.Money(extra) + " bonus");
                if (AudioManager.I != null) AudioManager.I.Play(Sfx.Coin);
                if (committed) SaveNow();
            }
        }

        public void ResultsContinue()
        {
            CommitResult();
            Action next = AfterResults;
            if (AdPolicy.CanShowInterstitial())
            {
                AdPolicy.MarkInterstitialShown();
                Monetization.Ads.ShowInterstitial(next);
            }
            else next();
        }

        void AfterResults()
        {
            RaceResult r = PendingResult;
            PendingResult = null;
            committed = false;
            TeardownSession();
            if (r == null) { Go(Scr.Hub); return; }

            RaceDef def = r.def;
            string dlg = r.won ? def.dialogueWin : def.dialogueLose;
            if (def.story && !string.IsNullOrEmpty(dlg) && StoryContent.Dialogue(dlg) != null)
                PlayDialogue(dlg, () => AutoNext(false));
            else if (def.story) AutoNext(false);
            else Go(Scr.Hub);
        }

        public void ResultsRetry()
        {
            CommitResult();
            RaceDef def = PendingResult != null ? PendingResult.def : (Session != null ? Session.Def : null);
            PendingResult = null;
            committed = false;
            if (def == null) { Go(Scr.Hub); return; }
            if (def.id == "legend_daily") { }   // same daily seed again
            StartRaceNow(def);
        }

        public void ResultsRevive()
        {
            if (Session == null || Session.Race == null) return;
            RewardedAd(AdPlacement.RewardedRevive, ok =>
            {
                if (!ok || Session == null) return;
                PendingResult = null;
                committed = false;
                Session.Race.Revive();
                Go(Scr.Drive);
            });
        }

        /// <summary>Failed run: carry on from the last checkpoint instead of the start. Free.</summary>
        public void ResultsCheckpoint()
        {
            if (Session == null || Session.Race == null) return;
            PendingResult = null;
            committed = false;
            Session.RestartFromCheckpoint();
            Go(Scr.Drive);
        }

        // ------------------------------------------------------------------ ads and purchases

        public void RewardedAd(AdPlacement placement, Action<bool> done)
        {
            if (!AdPolicy.CanOfferRewarded(placement))
            {
                Toast("No ad available right now");
                if (done != null) done(false);
                return;
            }
            Monetization.Ads.ShowRewarded(placement, ok =>
            {
                if (!ok) Toast("Ad not completed");
                if (done != null) done(ok);
            });
        }

        public void Buy(string productId)
        {
            Monetization.Iap.Purchase(productId, ok =>
            {
                Toast(ok ? "Purchase complete. Thank you!" : "Purchase not completed");
                if (ok && AudioManager.I != null) AudioManager.I.Play(Sfx.Coin);
                RefreshStage();
            });
        }

        public void RestorePurchases()
        {
            Monetization.Iap.RestorePurchases(ok =>
            {
                Toast(ok ? "Purchases restored" : "Nothing to restore");
                RefreshStage();
            });
        }

        // ------------------------------------------------------------------ lifecycle

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            if (currentId == Scr.Drive && Session != null) Session.SetPaused(true);
            CommitResult();
            SaveNow();
        }

        void OnApplicationQuit()
        {
            CommitResult();
            SaveNow();
        }
    }
}
