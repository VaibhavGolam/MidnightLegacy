#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MidnightLegacy.EditorTools
{
    /// <summary>
    /// Makes sure the project can be built straight away (also on a build server): creates an empty Boot scene
    /// when the build has none, and offers one menu item that applies the Android project settings the game needs.
    /// The game builds itself at run time from AppRoot, so the scene stays empty.
    /// </summary>
    [InitializeOnLoad]
    static class BootSceneSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/Boot.unity";

        static BootSceneSetup()
        {
            EditorApplication.delayCall += EnsureScene;
        }

        static void EnsureScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
                if (scenes[i].enabled && File.Exists(scenes[i].path)) return;   // the project already has a scene in the build

            CreateBootScene();
        }

        public static void CreateBootScene()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes");
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("[MidnightLegacy] Boot scene is in the build list: " + ScenePath);
        }
    }

    public static class BuildSetup
    {
        [MenuItem("Midnight Legacy/Apply Project Settings")]
        public static void Apply()
        {
            bool restart = ApplySettings();
            string msg = "Project settings applied: portrait, Linear, IL2CPP, ARM64, Vulkan + GLES3, min Android 8.0 (API 26), URP asset assigned.";
            if (restart) msg += "\nActive Input Handling was switched to the new Input System. Restart Unity when it asks.";
            Debug.Log("[MidnightLegacy] " + msg);
            EditorUtility.DisplayDialog("Midnight Legacy", msg, "OK");
        }

        /// <summary>Applies everything without showing a dialog (also used by the build server). Returns true when Unity needs a restart.</summary>
        public static bool ApplySettings()
        {
            PlayerSettings.companyName = "ST Media";
            PlayerSettings.productName = "Midnight Legacy";

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            NamedBuildTarget android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.stmedia.midnightlegacy");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Medium);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.gcIncremental = true;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            EnsureUrp();
            ArtImporter.ApplyAppIcon();
            bool restart = SetInputHandlerToNew();

            BootSceneSetup.CreateBootScene();
            AssetDatabase.SaveAssets();
            return restart;
        }

        /// <summary>The game's shader is URP only. A fresh project from the URP template already has an asset; a bare one gets one here.</summary>
        static void EnsureUrp()
        {
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset) return;

            const string dir = "Assets/_Project/Settings";
            Directory.CreateDirectory(dir);
            string assetPath = dir + "/ML_URP.asset";
            UniversalRenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (urp == null)
            {
                UniversalRendererData data = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(data, dir + "/ML_URP_Renderer.asset");
                urp = UniversalRenderPipelineAsset.Create(data);
                AssetDatabase.CreateAsset(urp, assetPath);
            }
            urp.supportsHDR = false;
            urp.msaaSampleCount = 1;
            urp.renderScale = 1f;
            EditorUtility.SetDirty(urp);
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = urp;
            AssetDatabase.SaveAssets();
        }

        /// <summary>Active Input Handling: 1 = Input System Package (New). Returns true when it had to be changed.</summary>
        static bool SetInputHandlerToNew()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return false;
            SerializedObject so = new SerializedObject(assets[0]);
            SerializedProperty p = so.FindProperty("activeInputHandler");
            if (p == null || p.intValue == 1 || p.intValue == 2) return false;
            p.intValue = 1;
            so.ApplyModifiedProperties();
            return true;
        }

        [MenuItem("Midnight Legacy/Create Boot Scene")]
        public static void CreateScene()
        {
            BootSceneSetup.CreateBootScene();
        }
    }

    /// <summary>Entry point for the GitHub build: applies the settings, then builds a debug-signed APK into build/Android.</summary>
    public static class CIBuild
    {
        public const string OutputPath = "build/Android/MidnightLegacy.apk";

        public static void BuildAndroid()
        {
            try
            {
                bool restart = BuildSetup.ApplySettings();
                if (restart) Debug.LogWarning("[MidnightLegacy] Input handler was changed during the build. Check ProjectSettings/ProjectSettings.asset has activeInputHandler: 1.");

                Directory.CreateDirectory("build/Android");
                EditorUserBuildSettings.buildAppBundle = false;

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/_Project/Scenes/Boot.unity" },
                    locationPathName = OutputPath,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError("[MidnightLegacy] Build failed: " + report.summary.result + ", errors: " + report.summary.totalErrors);
                    EditorApplication.Exit(1);
                    return;
                }
                Debug.Log("[MidnightLegacy] Build OK: " + OutputPath + " (" + (report.summary.totalSize / 1048576) + " MB)");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
