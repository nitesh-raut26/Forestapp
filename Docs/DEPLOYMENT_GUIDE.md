# Forest Friends Quest — Deployment Guide

## Prerequisites

- Unity 2022.3 LTS (minimum) / Unity 6 LTS (recommended)
- Unity Hub 3.x
- Android NDK 23+ (bundled with Unity Hub)
- Xcode 15+ (macOS only, for iOS)
- Firebase Unity SDK 12.x (optional — game runs in mock mode without it)
- Unity IAP package 4.x (optional — mock mode available)

---

## Android Deployment

### 1. Player Settings

```
Edit → Project Settings → Player → Android tab

Company Name:   ForestFriendsStudio
Product Name:   Forest Friends Quest
Bundle ID:      com.forestfriendsstudio.forestfriendsquest
Version:        1.0.0
Bundle Version Code: 1

Minimum API Level:  Android 7.0 (API 24)
Target API Level:   Android 14 (API 34)
Install Location:   Prefer External

Graphics:
  ✅ Vulkan (first)
  ✅ OpenGL ES 3.0 (fallback)

Scripting Backend:  IL2CPP
API Compatibility:  .NET Standard 2.1
Target Architectures:
  ✅ ARM64
  ✅ ARMv7
  ☐ x86 (not needed)

Strip Engine Code:  ✅
Managed Stripping Level: Minimal

Active Input Handling: Input System Package (New)
```

### 2. Keystore Setup

```bash
# Create keystore (run once — store the password securely)
keytool -genkey -v \
  -keystore forest-quest-release.keystore \
  -alias forest-quest \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000

# In Unity: Edit → Project Settings → Player → Publishing Settings
# Enable "Custom Keystore" and point to forest-quest-release.keystore
```

**Never commit the keystore file to git.** Add to `.gitignore`:
```
*.keystore
*.jks
```

Store the password in a secrets manager (GitHub Secrets / Doppler / 1Password).

### 3. Build AAB (Google Play)

```
File → Build Settings
  Platform: Android
  ✅ Build App Bundle (Google Play)
  Compression Method: LZ4HC

→ Build
```

Output: `forest-friends-quest.aab` (~80-120 MB expected)

### 4. Google Play Checklist

- [ ] Content Rating — complete IARC questionnaire (select "Early Childhood")
- [ ] Data Safety form — declare: no data shared with third parties; local data only
- [ ] Set target audience: "Children" (triggers child-directed treatment)
- [ ] Families Policy — confirm no ads, no data collection from children
- [ ] Enable Play Asset Delivery for Addressables bundles (optional, CDN savings)
- [ ] Set up internal → closed → open testing tracks before production release
- [ ] Enable Google Play App Signing

### 5. Firebase / Push Notifications (Android)

```
# Add google-services.json to Assets/StreamingAssets/
# Add Firebase Unity SDK: Window → Package Manager → Firebase Analytics
# Add INTERNET permission (auto-added by Firebase SDK)
# Target SDK 34 requires POST_NOTIFICATIONS declared in manifest
```

`AndroidManifest.xml` additions (in `Assets/Plugins/Android/`):
```xml
<uses-permission android:name="android.permission.POST_NOTIFICATIONS"/>
<uses-permission android:name="android.permission.INTERNET"/>
<uses-permission android:name="android.permission.VIBRATE"/>
```

### 6. Addressables CDN (Android)

```
Window → Asset Management → Addressables → Groups
  Profile: set RemoteLoadPath to your CDN URL
  e.g.: https://cdn.forestfriends.io/android/[BuildTarget]/[BuildTarget]

Build → Build Addressables
  Upload the output catalog + bundles to CDN
  Include catalog hash file for delta update detection
```

---

## iOS Deployment

### 1. Player Settings

```
Company:    ForestFriendsStudio
Bundle ID:  com.forestfriendsstudio.forestfriendsquest
Version:    1.0.0
Build:      1

Min iOS Version: 14.0
Target Device:   iPhone + iPad

Architecture: ARM64
Strip Engine Code: ✅
Managed Stripping Level: Minimal
```

### 2. Xcode Build

```
File → Build Settings → iOS → Build
Open the generated Xcode project:
  cd /path/to/build/ios/
  open Forest\ Friends\ Quest.xcodeproj

In Xcode:
  Signing: select your team
  Capabilities: Push Notifications ✅, In-App Purchase ✅
  App Transport Security: allow HTTPS (CDN only)
```

### 3. App Store Checklist

- [ ] App category: Games → Educational
- [ ] Age rating: 4+
- [ ] Privacy Policy URL (required for children's apps)
- [ ] NSUserTrackingUsageDescription omitted (no IDFA usage)
- [ ] SKAdNetwork (for any attribution — omit if no paid UA)
- [ ] In-App Purchase products configured in App Store Connect
- [ ] TestFlight beta — minimum 3 external tester sessions before production

### 4. App Store Connect IAP Setup

For each product in `IAPManager.Products`:

| Product ID | Type | Price Tier |
|---|---|---|
| `ffq.cosmetic.starter_pack` | Non-Consumable | Tier 1 ($0.99) |
| `ffq.cosmetic.winter_theme` | Non-Consumable | Tier 2 ($1.99) |
| `ffq.cosmetic.spring_theme` | Non-Consumable | Tier 2 ($1.99) |
| `ffq.cosmetic.summer_theme` | Non-Consumable | Tier 2 ($1.99) |
| `ffq.cosmetic.autumn_theme` | Non-Consumable | Tier 2 ($1.99) |
| `ffq.lore.druid_pack` | Non-Consumable | Tier 1 ($1.49) |
| `ffq.lore.ancient_pack` | Non-Consumable | Tier 1 ($1.49) |
| `ffq.decor.premium_pack` | Non-Consumable | Tier 3 ($2.99) |
| `ffq.social.creature_album` | Non-Consumable | Tier 2 ($1.99) |
| `ffq.bundle.all_access` | Non-Consumable | Tier 7 ($7.99) |

---

## Steam Deployment

### 1. Steamworks Setup

1. Create app on `partner.steamgames.com`
2. Get App ID (e.g. `XXXXXXX`)
3. Add `steam_appid.txt` to project root containing just the App ID
4. Install Steamworks.NET package

```csharp
// SteamManager.cs — initialize in ForestQuestBootstrap
SteamAPI.Init();
```

### 2. Steam Achievement Integration

```csharp
// Map Forest Friends achievements to Steam achievement API names
// (configure in Steamworks backend first)
SteamUserStats.SetAchievement("FFQ_FIRST_BOND");
SteamUserStats.StoreStats();

// Wire from AchievementSystem.OnAchievementUnlocked
```

### 3. Steam Cloud Save

```csharp
// Store save JSON to Steam Cloud:
var bytes = System.Text.Encoding.UTF8.GetBytes(json);
SteamRemoteStorage.FileWrite("forestquest_save.json", bytes, bytes.Length);
```

### 4. Steam Deck Optimisation

```
Target resolution: 1280×800 (16:10)
CanvasScaler reference: 1280×800 (override for Deck)
Input: Steam Input API (controller + touchscreen)
Frame rate: 60fps target, 40fps on battery
Proton compatibility: ✅ (IL2CPP build is native Windows)
```

### 5. Steam Store Asset Checklist

| Asset | Size |
|---|---|
| Capsule image | 460×215 |
| Header image | 460×215 |
| Main capsule | 616×353 |
| Screenshots (min 5) | 1280×720 |
| Trailer | 1080p, 30–90 seconds |
| Background | 1438×810 |
| Icon | 32×32 |

### 6. Depot Configuration

```
# depot_build.vdf
"DepotBuild"
{
  "DepotID" "YYYYYYYY"
  "ContentRoot" ".\Builds\Steam\Forest Friends Quest_Data"
  "LocalPath" "*"
  "DepotPath" "."
}
```

---

## WebGL (Showcase Build)

```
Player Settings → WebGL:
  Template: PWA (or Minimal)
  Memory Size: 512 MB
  Exception Support: None (perf)
  Compression: Brotli

Addressables: bake all content into the build (no CDN for showcase)
Firebase: disabled (COPPA restrictions on web)
IAP: disabled

Deploy to: itch.io / Newgrounds / custom domain (HTTPS required)
```

---

## CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/build.yml
name: Build Forest Friends Quest

on:
  push:
    branches: [main, release/*]

jobs:
  android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: game-ci/unity-builder@v4
        with:
          targetPlatform: Android
          buildMethod: BuildScript.BuildAndroid
          androidKeystoreName: ${{ secrets.KEYSTORE_NAME }}
          androidKeystoreBase64: ${{ secrets.KEYSTORE_BASE64 }}
          androidKeystorePass: ${{ secrets.KEYSTORE_PASS }}
          androidKeyaliasName: ${{ secrets.KEY_ALIAS }}
          androidKeyaliasPass: ${{ secrets.KEY_PASS }}

  ios:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v4
      - uses: game-ci/unity-builder@v4
        with:
          targetPlatform: iOS
      - uses: apple-actions/upload-testflight-build@v1
        with:
          app-path: build/iOS/Forest Friends Quest.ipa
          issuer-id: ${{ secrets.APPSTORE_ISSUER_ID }}
          api-key-id: ${{ secrets.APPSTORE_KEY_ID }}
          api-private-key: ${{ secrets.APPSTORE_PRIVATE_KEY }}
```

---

## Environment Variables (required secrets)

| Secret | Purpose |
|---|---|
| `UNITY_LICENSE` | Unity activation |
| `KEYSTORE_BASE64` | Android signing |
| `KEYSTORE_PASS` | Android keystore password |
| `KEY_ALIAS` | Android key alias |
| `KEY_PASS` | Android key password |
| `APPSTORE_ISSUER_ID` | App Store Connect API |
| `APPSTORE_KEY_ID` | App Store Connect API |
| `APPSTORE_PRIVATE_KEY` | App Store Connect API |
| `FIREBASE_CONFIG` | google-services.json / GoogleService-Info.plist |
| `STEAM_SDK_KEY` | Steamworks partner key |
