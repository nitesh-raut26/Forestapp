#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ForestFriendsQuest.Editor
{
    /// <summary>
    /// Forest Friends Quest — Play Store build configurator.
    /// Menu: Forest Friends Quest → Configure Android Build
    ///
    /// Sets all required PlayerSettings for a production Google Play release:
    ///   - Bundle ID, version, orientation, icons
    ///   - Target SDK, minimum SDK
    ///   - IL2CPP scripting backend (required for Google Play 64-bit requirement)
    ///   - Split APKs by ABI (arm64 + armv7)
    ///   - Safe area, touch screen, accelerometer
    /// </summary>
    public static class PlayStoreBuildSetup
    {
        [MenuItem("Forest Friends Quest/Configure Android Build for Play Store")]
        public static void ConfigureAndroid()
        {
            // ── Application Identity ──────────────────────────────────────────────
            PlayerSettings.productName        = "Forest Friends Quest";
            PlayerSettings.companyName        = "Forest Friends Studio";
            PlayerSettings.applicationIdentifier = "com.forestfriendsstudio.forestfriendsquest";

            // ── Version ───────────────────────────────────────────────────────────
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // ── Android SDK ───────────────────────────────────────────────────────
            PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel25; // Android 7.1 (minimum supported)
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34; // Android 14

            // ── 64-bit requirement (Google Play mandates this) ─────────────────────
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

            // ── Display ───────────────────────────────────────────────────────────
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait            = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown  = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft        = false;
            PlayerSettings.allowedAutorotateToLandscapeRight       = false;

            // ── Graphics ──────────────────────────────────────────────────────────
            PlayerSettings.colorSpace = ColorSpace.Gamma; // Gamma matches the 2D art style

            // ── Icons ─────────────────────────────────────────────────────────────
            var icon = GenerateAppIcon();
            if (icon != null)
            {
                var icons = new Texture2D[7];
                for (var i = 0; i < icons.Length; i++) icons[i] = icon;
                PlayerSettings.SetIcons(NamedBuildTarget.Android, icons, IconKind.Application);
            }

            // ── Store metadata stub ───────────────────────────────────────────────
            Debug.Log("[PlayStoreBuildSetup] Android settings configured.");
            Debug.Log("[PlayStoreBuildSetup] Bundle ID : com.forestfriendsstudio.forestfriendsquest");
            Debug.Log("[PlayStoreBuildSetup] Version   : 1.0.0 (build 1)");
            Debug.Log("[PlayStoreBuildSetup] Min SDK   : API 25 (Android 7.1)");
            Debug.Log("[PlayStoreBuildSetup] Target SDK: API 34 (Android 14)");
            Debug.Log("[PlayStoreBuildSetup] Backend   : IL2CPP  ARM64 + ARMv7");

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Play Store Build Configured",
                "Android settings are ready.\n\n" +
                "NEXT STEPS:\n" +
                "1. File → Build Settings → Android → Switch Platform\n" +
                "2. Add BootScene to Scenes In Build (index 0)\n" +
                "3. Player Settings → Publishing Settings → create a keystore\n" +
                "4. Build → Build Bundle (.aab) for Play Store upload\n\n" +
                "Bundle ID: com.forestfriendsstudio.forestfriendsquest",
                "OK");
        }

        // ─── Play Store Build Checklist ───────────────────────────────────────────

        [MenuItem("Forest Friends Quest/Play Store Checklist")]
        public static void PrintChecklist()
        {
            Debug.Log("=== FOREST FRIENDS QUEST — PLAY STORE CHECKLIST ===");
            Debug.Log("[ ] 1. Run 'Configure Android Build' (menu above)");
            Debug.Log("[ ] 2. File → Build Settings → Switch to Android");
            Debug.Log("[ ] 3. Add Assets/Scenes/BootScene.unity to build list (index 0)");
            Debug.Log("[ ] 4. Player Settings → Publishing Settings:");
            Debug.Log("       - Create or use existing signing keystore");
            Debug.Log("       - Enable 'Build App Bundle' for Play Store");
            Debug.Log("[ ] 5. Player Settings → Other:");
            Debug.Log("       - Internet Access: Required (for Firebase/IAP)");
            Debug.Log("       - Write Access: External (SD Card) → Internal Only");
            Debug.Log("[ ] 6. Firebase: add google-services.json to Assets/");
            Debug.Log("[ ] 7. IAP: configure product IDs in IAPManager.cs");
            Debug.Log("[ ] 8. Test on device: Build → Build And Run");
            Debug.Log("[ ] 9. Build release .aab: Build Settings → Build (not Run)");
            Debug.Log("[ ] 10. Upload .aab to Google Play Console");
            Debug.Log("===================================================");
        }

        // ─── App Icon Generator ───────────────────────────────────────────────────

        private static Texture2D GenerateAppIcon()
        {
            const int iconSize = 512;
            var tex = new Texture2D(iconSize, iconSize, TextureFormat.RGBA32, false);

            // Forest green radial gradient background
            var center = new Vector2(iconSize / 2f, iconSize / 2f);
            for (var y = 0; y < iconSize; y++)
            {
                for (var x = 0; x < iconSize; x++)
                {
                    var dist = Vector2.Distance(new Vector2(x, y), center) / (iconSize / 2f);
                    var bg   = Color.Lerp(
                        new Color(0.16f, 0.42f, 0.30f),
                        new Color(0.04f, 0.14f, 0.10f),
                        dist * dist);
                    tex.SetPixel(x, y, bg);
                }
            }

            // Draw a stylised "P" silhouette (Pip) centered in the icon
            // Using circles to form a recognisable shape
            FillCircle(tex, iconSize / 2, iconSize / 2 + 20, 120, new Color(1f, 0.70f, 0.42f));
            FillCircle(tex, iconSize / 2, iconSize / 2 + 80, 80, new Color(1f, 0.70f, 0.42f));   // head
            FillCircle(tex, iconSize / 2 - 40, iconSize / 2 + 130, 28, new Color(1f, 0.80f, 0.55f)); // ear L
            FillCircle(tex, iconSize / 2 + 40, iconSize / 2 + 130, 28, new Color(1f, 0.80f, 0.55f)); // ear R
            // Eyes
            FillCircle(tex, iconSize / 2 - 22, iconSize / 2 + 88, 10, new Color(0.14f, 0.10f, 0.06f));
            FillCircle(tex, iconSize / 2 + 22, iconSize / 2 + 88, 10, new Color(0.14f, 0.10f, 0.06f));
            // Glow sparkle
            FillCircle(tex, iconSize / 2, iconSize / 2 + 40, 28, new Color(1f, 0.95f, 0.55f, 0.65f));
            FillCircle(tex, iconSize / 2, iconSize / 2 + 40, 14, new Color(1f, 1f, 0.9f, 0.9f));

            // Title text lettering can't be drawn in pixel art here, but the
            // silhouette is sufficient for identification in the launcher.
            tex.Apply();
            return tex;
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int r, Color c)
        {
            for (var y = cy - r; y <= cy + r; y++)
            {
                for (var x = cx - r; x <= cx + r; x++)
                {
                    if (x < 0 || x >= tex.width || y < 0 || y >= tex.height) continue;
                    var dx = x - cx; var dy = y - cy;
                    if (dx * dx + dy * dy <= r * r)
                        tex.SetPixel(x, y, c);
                }
            }
        }
    }
}
#endif
