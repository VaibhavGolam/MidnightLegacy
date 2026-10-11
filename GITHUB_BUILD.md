# Build the APK on GitHub (no Unity on your PC)

This folder is a complete Unity project (Assets, Packages, ProjectSettings), so GitHub can build it by itself.

## One time setup

1. **Unity licence file.** Install Unity Hub (the editor is not needed for this), sign in with a free Unity account,
   and add a free Personal licence under Preferences > Licenses. Then find the file `Unity_lic.ulf`:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - Mac: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
   If the file is not there, Hub did not create it without an editor installed. In that case install any Unity 6
   editor once, open a project once so it licenses itself, and look again.
2. **GitHub repo.** Create a new private repository. Upload everything in this folder, including the hidden
   `.github` folder. Easiest: install GitHub Desktop, add this folder, commit and push to `main`.
3. **Secrets.** Repo > Settings > Secrets and variables > Actions > New repository secret. Add three:
   - `UNITY_LICENSE`: paste the whole text of `Unity_lic.ulf`
   - `UNITY_EMAIL`: your Unity account email
   - `UNITY_PASSWORD`: your Unity account password

## Every build

1. Repo > Actions > "Build Android" > Run workflow (it also runs on every push to `main`).
2. First run takes 25 to 40 minutes (it downloads the Unity image and imports the project). Later runs are faster.
3. When it is green, open the run and download **MidnightLegacy-Android** from Artifacts. Unzip it to get
   `MidnightLegacy.apk`.
4. Copy the APK to your phone, open it, and allow "install unknown apps" when Android asks.

## If the build fails

Open the failed run, open the Build step, and copy the lines that contain `error CS` (compile errors) or
`[MidnightLegacy] Build failed`. Paste them to Claude. Fixed files come back whole, you upload them, and run again.

## Things worth knowing

- The APK is debug signed. That is fine for testing on your own phone. For Google Play you need a release keystore
  and an AAB; that comes later.
- Unity version is pinned in `ProjectSettings/ProjectVersion.txt` (6000.0.40f1). If GitHub cannot find that image,
  change it to another Unity 6 LTS version listed at https://game.ci/docs/docker/versions
- Package versions are in `Packages/manifest.json` (URP 17.0.3, Input System 1.11.2). They match Unity 6000.0.
  If you change the Unity version to a newer 6.x, these may need to change too.
