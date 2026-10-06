# Midnight Legacy, Phase 1: Driving Prototype

## Unity setup (about 10 minutes)

1. Unity Hub: install **Unity 6 LTS** with **Android Build Support** (OpenJDK + Android SDK/NDK).
2. New project from the **Universal 3D** template (URP + Input System come with it). Name it MidnightLegacy.
3. Copy the `Assets/_Project` folder from this zip into the project's `Assets/` folder. Let Unity compile.
4. Menu **Midnight Legacy > Apply Phase 1 Project Settings** (portrait, Linear, IL2CPP, ARM64, Vulkan + GLES3).
5. **Edit > Project Settings > Player > Other Settings > Active Input Handling** must be `Input System Package (New)` or `Both`. Unity restarts if you change it.
6. Open any scene (even the sample one) and press Play. Nothing else to set up: `Phase1Bootstrap` builds the car, road, camera, HUD and tuning panel by itself.
   - Editor controls: A / D or arrow keys to steer, S brake, Space handbrake. Mouse clicks on the bottom of the Game view also work as touches.
   - Set the Game view to a portrait ratio (9:16) first.
7. Phone: enable USB debugging, **File > Build Profiles > Android**, switch platform, add the open scene, Build And Run.

## On the phone

- Bottom left / bottom right: hold to steer. Hold longer = more lock. Tap = small correction.
- BRAKE (above left) and DRIFT / handbrake (above right).
- **TUNE** (top right) opens the tuning panel over the top 58% of the screen. The steering zones stay live underneath, so you can move a slider with one thumb and drive with the other.
  - COPY VALUES puts every number on the clipboard. Paste them to me and I'll bake them in as the new defaults.
  - WET toggles wet grip. LOW / MED / HIGH switches graphics level.
  - Everything saves on the phone between runs. RESET restores defaults.

## Test checklist

1. Car appears, road scrolls, no pink or black materials. Autumn trees, headlight pool in front of the car, fog fading the distance.
2. Tap LEFT quickly: small, calm correction. Hold LEFT 1 second: hard turn.
3. At speed, hold full lock: rear steps out, smoke appears, "SLIDE" shows. Release: car straightens, doesn't snap-spin.
4. Corner, then BRAKE mid-corner: nose dips, rear gets lighter, car rotates but is catchable.
5. Hit the guard rail: speed loss, no sticking, no crazy spin.
6. DRIFT button: rear breaks loose, easy to hold a slide.
7. Drive 3+ minutes without stopping: no stutter when chunks load, nothing disappears or jumps. Around 2.5 km the world silently rebases (nothing should visibly happen).
8. FPS counter (top right): note the number on Low / Med / High and which phone.
9. Rotate the phone, lock screen, come back: still portrait, still running.

## Files

```
Assets/_Project/
  Resources/Shaders/MLStylized.shader      night look: vertex colours, headlight cone, fog
  Scripts/
    Core/       EventBus, GameServices, MeshBuilder, MLMaterials, Phase1Bootstrap
    Car/        CarHandlingProfile (all tuning numbers), CarController, CarVisuals, SlideEffects
    Input/      InputReader (multi-touch zones + keyboard)
    Track/      TrackRecipe, RoadModel, TrackGenerator
    World/      NightLighting, ChaseCamera
    UI/         HUDView, TuningPanel
    Settings/   GraphicsSettings (Low / Medium / High)
    Monetization/ Monetization (ad + IAP interfaces, stubs, ad policy)
    Editor/     Phase1BuildSetup
```

## Ads and in-app purchases (planned in, not wired to an SDK yet)

- `IAdService` / `IPurchaseService` are the only things game code will ever call. Stubs return success in the Editor and dev builds.
- `AdPolicy`: no ads while driving, interstitial only on the results screen, at most one per 3 minutes and per 2 races, never if Remove Ads is owned. Rewarded ads are always optional (double reward, free repair, revive).
- IAP product IDs are in `ProductIds`: Remove Ads, supporter pack, cosmetic paint / wheel / decal packs. Nothing sold changes handling, so no pay-to-win.
- When we add the real SDKs (Unity LevelPlay or AdMob, plus Unity IAP), you'll need: a Google Play developer account, an AdMob / LevelPlay account with app IDs, products created in Play Console with the IDs above, and a consent form (UMP) for EU users. I'll tell you exactly when.
