#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace MidnightLegacy.EditorTools
{
    /// <summary>
    /// One click to set the Android player settings the brief asks for.
    /// Menu: Midnight Legacy > Apply Phase 1 Project Settings.
    /// </summary>
    public static class Phase1BuildSetup
    {
        [MenuItem("Midnight Legacy/Apply Phase 1 Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = "ST Media";
            PlayerSettings.productName = "Midnight Legacy";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.colorSpace = ColorSpace.Linear;

            NamedBuildTarget android = NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApplicationIdentifier(android, "com.stmedia.midnightlegacy");
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            EditorUtility.DisplayDialog(
                "Midnight Legacy",
                "Applied: portrait only, Linear colour space, IL2CPP, ARM64, Vulkan with OpenGLES3 fallback, package com.stmedia.midnightlegacy.\n\n" +
                "Still check by hand: Project Settings > Player > Other Settings > Active Input Handling = 'Input System Package (New)' or 'Both'.",
                "OK");
        }
    }
}
#endif
