using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace _Game.EditorTools
{
    /// <summary>
    /// Applies the Arrows launcher icon at build time.
    ///
    /// The icon slots in ProjectSettings are left empty in the repository on purpose: they are
    /// serialized as platform-specific layer arrays and are easy to corrupt by hand. Setting
    /// them through the public PlayerSettings API from a pre-build hook keeps the committed
    /// project file untouched and reproduces the same icon on CI.
    ///
    /// Adaptive icons take two layers in the order the Android platform expects:
    /// background first, foreground second.
    /// </summary>
    public sealed class AppIconSetup : IPreprocessBuildWithReport
    {
        private const string IconFolder = "Assets/_Game/Art/AppIcon/";
        private const string LegacyFile = "AppIcon_Legacy";
        private const string BackgroundFile = "AppIcon_Background";
        private const string ForegroundFile = "AppIcon_Foreground";

        // Ahead of GameCI's own callbacks so nothing can overwrite the slots afterwards.
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;
            Apply();
        }

        [MenuItem("Arrows/Reapply App Icon")]
        private static void Apply()
        {
            Texture2D legacy = Load(LegacyFile);
            Texture2D background = Load(BackgroundFile);
            Texture2D foreground = Load(ForegroundFile);

            if (legacy == null || background == null || foreground == null)
            {
                Debug.LogWarning("[AppIcon] Icon textures are missing under " + IconFolder +
                                 "- keeping the current launcher icon.");
                return;
            }

            try
            {
                SetIcons(AndroidPlatformIconKind.Legacy, legacy);
                SetIcons(AndroidPlatformIconKind.Round, legacy);
                SetIcons(AndroidPlatformIconKind.Adaptive, background, foreground);
                Debug.Log("[AppIcon] Applied legacy, round and adaptive launcher icons.");
            }
            catch (System.Exception e)
            {
                // Branding must never break a release build.
                Debug.LogWarning("[AppIcon] Could not apply the launcher icon: " + e);
            }
        }

        private static Texture2D Load(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(IconFolder + fileName + ".png");
        }

        private static void SetIcons(PlatformIconKind kind, params Texture2D[] layers)
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            if (icons == null || icons.Length == 0) return;

            int expected = icons[0].maxLayerCount;
            if (expected != layers.Length)
            {
                Debug.LogWarning("[AppIcon] " + kind + " expects " + expected + " layer(s) but " +
                                 layers.Length + " were supplied - slot skipped.");
                return;
            }

            foreach (PlatformIcon icon in icons)
            {
                icon.SetTextures(layers);
            }

            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }
    }
}
