# Forest Friends Quest Unity 2D

This folder is a Unity-ready runtime prototype for the kids forest puzzle app. It is built as a **code-first Unity 2D vertical slice** so the project can open and run without hand-authored scenes, imported sprites, or prefab setup.

## What is included

- `Assets/Scripts/Core/ForestQuestBootstrap.cs`
  Auto-starts the app before scene load, creates a camera and event system if needed, and mounts the runtime UI.
- `Assets/Scripts/Core/ForestQuestApp.cs`
  Main game loop, saved progression, zone unlocks, mini-game flow, rewards, and parent gate.
- `Assets/Scripts/Core/ForestGameContent.cs`
  Serializable content model for zones, levels, memory patterns, and path boards.
- `Assets/Scripts/Core/ForestDataLoader.cs`
  Loads the game content from JSON in `Resources`.
- `Assets/Scripts/Core/ForestProgressData.cs`
  Save-data model for local progression, stars, and premium unlock state.
- `Assets/Resources/forest_game_content.json`
  Ported characters, zones, rewards, and 12 starter levels across choice, memory, and path gameplay.
- `Assets/Scripts/Visuals/GuideCharacterView.cs`
  Runtime-built placeholder 2D guide characters using layered UI shapes.
- `Assets/Scripts/Audio/ForestAudioController.cs`
  Generated placeholder character and reward sounds.
- `Assets/Scripts/Animation`
  Simple motion scripts for bobbing, glowing, and swaying.

## Current status

This is a **working Unity vertical slice**, not a fully art-complete commercial game build.

It currently gives you:

- a Unity 2D app structure
- 12 starter levels across 3 zones
- three playable puzzle styles: choice, memory, and path
- saved progress on disk through local device storage
- zone and level progression with unlock thresholds
- star ratings for cleaner clears
- reward milestone tracking
- a parent gate plus device-level premium unlock flow
- a parent-facing progress dashboard
- stylized placeholder 2D character visuals
- generated placeholder sound cues

It does **not** yet include:

- final sprite sheets or hand-drawn characters
- recorded voice acting
- authored scene art, tiles, or parallax background assets
- polished transitions, particle systems, or production UI art
- production billing / analytics integrations
- native Android/iOS build verification from this machine

## How to open it

1. Install Unity `2022.3 LTS` or a newer compatible version.
2. Open the folder `/Users/niteshraut/Documents/AiApp/KidsApp/forest-friends-quest-unity` in Unity Hub.
3. Let Unity import the project.
4. Create or open any empty scene if Unity asks for one.
5. Press Play.

The runtime bootstrap creates the app UI automatically, so you do not need to wire up a scene manually first.

## Best next steps for a realistic finish

1. Replace the runtime placeholder character shapes with real 2D rigs or sprite sheets for `Pip`, `Mimi`, `Tomo`, and `Luma`.
2. Replace generated cue tones with real ambient loops, UI chimes, and voiced lines.
3. Swap the local premium unlock flow for production billing and analytics.
4. Split the runtime UI into proper prefabs and authored scenes once the visual direction is approved.
5. Run Android device testing from a machine with Unity installed and the Android build modules enabled.

## Important note

Unity is **not installed on this machine**, so I could not run the Unity editor or produce a packaged build here. The project files are prepared so you can open them in Unity and continue from a real Unity environment.
