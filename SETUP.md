# Midnight Legacy: full build (Phases 0 to 7), 2D version

The driving view is now top-down and portrait: an overhead orthographic camera follows the car, with the direction of travel always pointing up the screen. The driving physics, story, events, garage, economy and save are the same as before. Only the drawing changed. Everything is flat shapes built in code, so there are still no art files.

The road is seen from higher up than before, so cars look smaller than in the 3D version. To change that while testing, open the Dev Tuning panel and lower `cam.viewHeightSlow` and `cam.viewHeightFast` (fewer metres of road on screen means bigger cars), or move the car with `cam.carScreenHeight`.

Everything is in `Assets/_Project`. There are no art, audio or model files: the car, roads, garage, UI, sound effects and music are all generated in code.

## 1. Put it in Unity

1. Unity Hub: Unity 6 LTS with Android Build Support (OpenJDK, SDK, NDK). New project from the **Universal 3D** template (it brings URP and the Input System). The game itself is 2D: a flat top-down view, drawn from code.
2. **Delete the old `Assets/_Project` folder first** if you used the Phase 1 zip. Old files like `Phase1Bootstrap.cs`, `HUDView.cs` and `Phase1BuildSetup.cs` are gone in this version, and leaving them in will break the compile. Then copy the new `Assets/_Project` in.
3. Let Unity compile. Menu **Midnight Legacy > Apply Project Settings** (portrait, Linear, IL2CPP, ARM64, Vulkan + GLES3, API 26+). It also switches Active Input Handling to the new Input System and creates an empty `Boot` scene in the build list. Restart Unity if it asks.
4. Press Play (Game view set to a 9:16 portrait ratio). The game builds itself at run time from `AppRoot`, so the scene stays empty.
   - Editor controls while driving: A / D or arrows steer, S brake, Space handbrake (drift), Shift or W nitro. Mouse clicks work in the menus.
5. Phone: USB debugging on, **File > Build Profiles > Android**, Build And Run.

For GitHub builds without Unity on your PC, read `GITHUB_BUILD.md`. This folder already has `Packages` and `ProjectSettings`, and the workflow applies all Android settings itself.

**This code was written without a compiler.** I checked syntax with a parser and read every file against the others, but expect a few small compile errors on the first import. Paste the Console errors to me and I will fix them in one pass.

## 2. What is in the game

- **Story:** three chapters, 16 steps. Jun (name is yours to pick) inherits a covered car, meets Old Sato, Mia and Rook, learns the car belonged to the Ghost, and ends up on Kairo Pass against the Black Coupe. Steps unlock with reputation (15, 30, 45, 60, 90, 100, 120).
- **First race cameo:** the Unknown Driver's black car passes you once on a straight in the first race.
- **Events:** sprint, time attack, drift score, traffic run (with oncoming lane), chase, escape. 11 side events, 4 Legend Mode runs after the story (one is a daily seed), and a free drive.
- **Garage:** 8 part slots with 3 tiers each (cash and reputation gated), repairs, paints, wheels, decals, spoilers, a side-view 2D garage you can pan with one finger.
- **Style:** near misses, clean corners, clean sectors, drifts and overtakes build a combo. Good driving also refills nitro.
- **Tutorial:** guided prompts (steer, taps, brake, drift, nitro). It never takes control, and every step times out so nobody gets stuck.
- **Settings:** music, effects, vibration, steering speed, which buttons show, reduce camera shake, FPS counter, story assist, Low / Medium / High graphics.
- **Frame rate:** an adaptive render scale lowers resolution a little when the phone drops below target and raises it back when there is room.

## 3. Money, fair play and ads

- Cash and reputation come only from driving. Nothing sold for real money touches handling, cash or reputation.
- **Shop:** Remove Ads, Supporter Pack (ST Indigo paint and wheels), Paint, Wheel and Decal packs. All cosmetic.
- **Ads:** none while driving. Interstitial only on the results screen, at most one per 3 minutes and per 2 races, never with Remove Ads. Rewarded ads are optional: double bonus, free repair, second chance on a failed run.
- **Right now the ad and purchase services are stubs** (`Monetization/Monetization.cs`). In the Editor and dev builds they succeed instantly so you can test every flow. A release build with the stubs shows no ads and sells nothing.
- To go live later you need: a Google Play developer account, an AdMob or Unity LevelPlay account with app IDs, the five products created in Play Console with the IDs in `ProductIds`, a real consent form (Google UMP) for EU/UK players, and a privacy policy page. Replace `AppRoot.PrivacyUrl` (it is a placeholder). I will wire the SDKs in once you have those.

## 4. Phone test checklist

1. Splash, consent, menu, name, prologue. Garage car cover comes off during the Cinder Lane scene.
2. Tutorial: each prompt appears, completes, and the NITRO / BRAKE / DRIFT buttons show up when taught.
3. First Night: the black car passes once. Rook races you. A win pays, a loss offers retry.
4. Results: medals, reward lines, double bonus and second chance buttons (stub ads work in dev builds).
5. Hub: events unlock as reputation grows, mail shows unread badges, garage upgrades change the rating bars, repairs work.
6. Traffic run and escape: traffic never forms an impassable wall; the chaser closes in when you crash.
7. Rain and fog events: grip is lower in the wet.
8. Lock the screen mid-race: the game pauses and saves. Kill the app and reopen: progress is still there.
9. Note the FPS on Low / Medium / High, and the phone model and RAM.
10. Settings: turn on **Developer tuning panel**, drive, tap **TUNE** to adjust handling live. COPY VALUES lets you paste numbers back to me.

## 5. Where to change things

| What | File |
|---|---|
| Car handling numbers | `Car/CarHandlingProfile.cs` |
| Part prices, tiers and effects | `Content/Parts.cs` |
| Events, rewards, difficulty, rivals | `Content/ContentDB.cs` (or `Resources/MLContent/races.json`, see the README beside it) |
| Story text, rep gates | `Content/StoryContent.cs` |
| Roads, weather, time of day | `Content/Environments.cs` |
| Repair cost, bests, payouts | `Gameplay/Economy.cs`, `Gameplay/RaceManager.cs` |
| Ad frequency | `Monetization/Monetization.cs` (`AdPolicy`) |
| Graphics levels | `Settings/GraphicsSettings.cs` |

## 6. Known risks

- **Uncompiled code**, see above.
- **Performance is untested on a real phone.** The first numbers I need are FPS per graphics level and which phone. Traffic level 3 with rain on Low is the heaviest case.
- **Car feel** was tuned with a simulation, not a phone. Expect to adjust `CarHandlingProfile` after your first drive.
- **Running in the Editor changes your URP asset:** the adaptive quality writes the render scale to it. Revert the URP asset in version control if it shows as modified.
- **IMGUI text field:** the name entry uses Unity's built in field. Check the Android keyboard opens on your phone.
- **Painted art:** see section 8. Every picture is optional; a missing file falls back to the flat shapes. The first import must finish before Play (Unity does this itself, or run `Midnight Legacy > Reimport Art`).

## 7. File map

```
Assets/_Project/
  Resources/Shaders/MLStylized.shader        night look: vertex colours, headlight cone, fog
  Resources/Shaders/MLSprite.shader          textured sprites (painted cars, traffic, props, garage)
  Resources/Fonts/                           Chakra Petch + Rajdhani (SIL OFL, licence files included)
  Resources/Art/                             the painted art (PNG/JPG), loaded by name
  Art_Source/                                originals that are not loaded at run time (app icon, prop sources)
  Resources/MLContent/races.json             optional event overrides
  Scripts/
    Core/        AppRoot (entry point + flow), DriveSession, EventBus, GameServices, MeshBuilder, Flat (2D shapes), MLMaterials, ArtLoader, SpriteBatch
    Car/         CarController, CarHandlingProfile, CarVisuals, CarLook (cosmetics), SlideEffects, NitroEffects
    Input/       InputReader (multi-touch zones + keyboard)
    Track/       RoadModel, TrackRecipe, TrackGenerator
    Traffic/     TrafficSystem, TrafficMeshes, RoadCollision
    Gameplay/    RaceManager, RaceTypes, RivalController, StyleTracker, TutorialDirector, Economy
    Content/     ContentDB, StoryContent, Parts, Environments
    World/       NightLighting, ChaseCamera, WeatherFX
    Garage/      GarageStage + SideCar (2D side-view garage)
    Audio/       AudioManager, DriveAudio, Synths (procedural engine, effects and music)
    Save/        SaveData, SaveSystem (JSON with backup)
    Settings/    GraphicsSettings, AdaptiveQuality
    Monetization/ Monetization (ad + IAP interfaces, stubs, ad policy)
    UI/          UIKit (drawing toolkit), UIIcons (drawn icons), TuningPanel (dev only)
    UI/Screens/  Splash, Consent, Menu, Hub (+ GarageTab, ShopTab), Dialogue, DriveHUD, Results, SettingsPanel
    Editor/      BuildSetup, ArtImporter (import settings for the art, app icon)
```

## 8. Painted art

Files live in `Assets/_Project/Resources/Art/` and are found by name. If one is missing the game draws the old flat shape instead, so you can replace pieces one at a time.

| File | Used for |
|---|---|
| `car_kairo`, `car_hatch`, `car_wedge` | Top-down player and rival cars (white art, tinted with the paint colour). Decals still draw on top. |
| `traffic_hatch`, `_sedan`, `_taxi`, `_van`, `_bus`, `_truck`, `_moto` | Traffic. Hatch, sedan and one van variant are tinted, the rest keep their own colours. |
| `props_atlas` | Trees (green, orange, red, gold), pine, bushes, rock, street lamp, rooftop. Built from the files in `Art_Source/props`. |
| `car_side_kairo`, `wheel_side_bar/five/mesh`, `car_cover` | Side-view garage car, its wheels (chosen by the rim style) and the dust cover. |
| `garage_bg` | Garage wall and floor behind the hub and garage screens. |
| `menu_bg`, `title_logo` | Title screen. `menu_bg` is also the backdrop for road scenes in the story. |
| `char_jun`, `char_sato`, `char_mia`, `char_rook`, `char_dex`, `char_ivy`, `char_ghost`, `char_unknown`, `char_rina` | Dialogue portraits, the hub header, rival cards and the results screen. |
| `Art_Source/app_icon.png` | Android app icon, set by `Midnight Legacy > Apply Project Settings`. |

Notes:
- Only the Kairo has a side-view picture. The hatch and wedge still use the flat side drawing in the garage.
- Spoilers and the wheel styles do not show on the painted top-down cars (the art already has its own). Decals do.
- `ArtLoader.Enabled = false` turns all painted art off, which is handy for checking performance.
- `store_feature` (the Play Store banner) is not in the project on purpose. Upload it in the Play Console.

## 9. Controls, reverse and checkpoints

Layout (portrait): two steering pads along the bottom edge, and round buttons just above them. BRAKE and REV on the left, DRIFT and NITRO on the right. The middle of the screen is clear and the car sits just below the middle, so nothing covers it. Keyboard in the editor: A/D steer, S brake, Space drift, Shift or W nitro, **R reverse**.

- **Reverse**: hold REV. The car brakes first, then backs up at about 23 km/h (steer to turn it round). Release and it rolls forward again. It only lets you back up about 45 m from the furthest point you reached. It is off during the countdown. Changed in `CarController` (`ReverseStep`), button in `InputReader`.
- **Checkpoints**: sprint, time trial, traffic run and escape events get gantries every `checkpointEvery` metres (default 450, set per race in `races.json`). Passing one saves the clock, damage, crash count and the rival's position. You can then use **FROM LAST CHECKPOINT** in the pause menu, or **CONTINUE FROM CHECKPOINT** on the failed screen. It is free. A run continued from a checkpoint can still win and pay out, but it gets no gold medal, no Perfect bonus and no best time. Logic is in `RaceManager.RestartFromCheckpoint`.
- **Road size**: the camera is closer. Tune it live in Dev Tuning (`cam.viewHeightSlow`, `cam.viewHeightFast`, `cam.carScreenHeight`) or change the defaults in `ChaseCamera`.

New painted UI pieces (all optional, see `UI_ART_PROMPTS.md`): `ui_panel`, `ui_button_primary/secondary/gold/danger`, `ui_pad`, `ui_btn_round`, `ui_icon_brake/nitro/drift/reverse/arrow/pause`, `ui_medal`, and the story scenes `scene_room/phone/city/road/pass`.

## 10. Menu restyle, fonts and art hooks

The hub (Story, Events, Garage, Mail, Shop, More) was restyled to the mockups: navy glass cards with an electric blue edge, icon tab bar, coin chip with a "+" that opens the Shop, thumbnails on event cards, portraits in Mail, segmented stat bars in the Garage.

Fonts: headings use Chakra Petch Bold Italic, body text uses Rajdhani SemiBold, both loaded from `Resources/Fonts`. Both are SIL Open Font Licence; keep the two `-OFL.txt` files in the project. If the fonts are missing the game falls back to Unity's default font.

Everything painted is optional. Without a file the game draws a plain version. Drop files into `Resources/Art/` and run **Midnight Legacy > Reimport Art**. Prompts for all of them are in `UI_ART_PROMPTS.md`.

| Where | File names |
|---|---|
| Tab bar | `ui_tab_story`, `ui_tab_events`, `ui_tab_garage`, `ui_tab_mail`, `ui_tab_shop`, `ui_tab_more` |
| Career tiles | `ui_stat_races`, `ui_stat_wins`, `ui_stat_distance`, `ui_stat_speed`, `ui_stat_near`, `ui_stat_corners`, `ui_stat_drift`, `ui_stat_cash` |
| Small icons | `ui_coin`, `ui_check`, `ui_lock`, `ui_gear`, `ui_notebook` |
| Event and story cards | `thumb_forest`, `thumb_pass`, `thumb_coast`, `thumb_city` |
| Shop | `shop_removeads`, `shop_supporter`, `shop_paint`, `shop_wheels`, `shop_decals` |
| Garage parts | `part_engine`, `part_tires`, `part_suspension`, `part_brakes`, `part_lightweight`, `part_aero`, `part_nitro`, `part_frame` |

Mail portraits use the existing `char_*` files (sato, mia, rook, dex, ivy, rina, ghost, jun).
