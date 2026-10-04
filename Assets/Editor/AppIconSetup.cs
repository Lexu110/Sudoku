using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SudokuGame.Editor
{
    /// <summary>
    /// Sets Assets/Art/AppIcon.png as the game's icon (Player Settings > Icon). Runs once automatically
    /// when no icon is set, and any time from the menu: Sudoku > Apply App Icon.
    /// </summary>
    [InitializeOnLoad]
    public static class AppIconSetup
    {
        const string IconPath = "Assets/Art/AppIcon.png";

        static AppIconSetup()
        {
            EditorApplication.delayCall += () =>
            {
                var icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Application);
                if (icons.Length == 0 || icons[0] == null) Apply();
            };
        }

        [MenuItem("Sudoku/Apply App Icon")]
        public static void Apply()
        {
            var importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"App icon not found at {IconPath}.");
                return;
            }

            // Keep the icon sharp: no compression, no mipmaps.
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Application);
            AssetDatabase.SaveAssets();
            Debug.Log("Sudoku app icon applied (Player Settings > Icon > Default Icon).");
        }
    }
}
