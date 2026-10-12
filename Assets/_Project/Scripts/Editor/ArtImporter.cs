#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace MidnightLegacy.EditorTools
{
    /// <summary>
    /// Import settings for the painted art, applied automatically to everything in Resources/Art:
    /// no rescaling to a power of two (sprites keep their exact shape), clamped edges, compressed for the phone,
    /// mipmaps for the world sprites (they shrink on screen) and none for the full screen pictures.
    /// Also re-imports the art once after the scripts first compile, and sets the app icon.
    /// </summary>
    public sealed class ArtImporter : AssetPostprocessor
    {
        const string ArtFolder = "Assets/_Project/Resources/Art/";
        const string IconPath = "Assets/_Project/Art_Source/app_icon.png";
        const string DoneKey = "MidnightLegacy.ArtImported.v2";

        void OnPreprocessTexture()
        {
            bool art = assetPath.StartsWith(ArtFolder);
            bool icon = assetPath == IconPath;
            if (!art && !icon) return;

            TextureImporter ti = (TextureImporter)assetImporter;
            string file = Path.GetFileNameWithoutExtension(assetPath);
            bool ui = file.StartsWith("ui_");
            ti.textureType = TextureImporterType.Default;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.isReadable = false;
            ti.maxTextureSize = ui ? 1024 : 2048;
            // interface pictures stay uncompressed: buttons and panels have soft edges that block compression would smear
            ti.textureCompression = (icon || ui) ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            ti.alphaIsTransparency = assetPath.EndsWith(".png");

            bool screenPicture = icon || ui || file.StartsWith("char_") || file.StartsWith("scene_")
                                 || file == "menu_bg" || file == "garage_bg" || file == "title_logo";
            ti.mipmapEnabled = !screenPicture;
            ti.filterMode = FilterMode.Bilinear;
        }

        [InitializeOnLoadMethod]
        static void FirstTime()
        {
            if (EditorPrefs.GetBool(DoneKey, false)) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!Directory.Exists(ArtFolder.TrimEnd('/'))) return;
                Reimport();
                EditorPrefs.SetBool(DoneKey, true);
            };
        }

        [MenuItem("Midnight Legacy/Reimport Art")]
        public static void Reimport()
        {
            AssetDatabase.ImportAsset(ArtFolder.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            if (File.Exists(IconPath)) AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[MidnightLegacy] Art re-imported with the game's import settings.");
        }

        /// <summary>Sets the default app icon from Art_Source/app_icon.png (used by Apply Project Settings).</summary>
        public static void ApplyAppIcon()
        {
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Application);
        }
    }
}
#endif
