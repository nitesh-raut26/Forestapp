# Forest Friends Quest — System Architecture

## Overview

Forest Friends Quest uses a **service-locator / dependency-injection** pattern.
A single `ForestSystemsContainer` MonoBehaviour owns and initialises all 54
production systems in a strict dependency order. There are no singletons;
all inter-system communication flows through the container or through
strongly-typed C# events.

---

## Boot Sequence

```
Application Launch
       │
       ▼
ForestQuestBootstrap  [RuntimeInitializeOnLoadMethod — BeforeSceneLoad]
       │
       ├─▶ EnsureCamera()
       ├─▶ EnsureEventSystem()
       │
       └─▶ new GameObject("ForestQuestRoot")
                │
                ├─▶ ForestSystemsContainer.InitializeAll()   ← 54 systems
                │         (see Initialization Order below)
                │
                └─▶ ForestQuestApp                           ← game loop
```

The root object is marked `DontDestroyOnLoad`. Nothing in the game
constructs any system manually — every reference goes through `ForestSystemsContainer.Instance`.

---

## System Initialization Order

| # | System | Key Dependencies |
|---|--------|-----------------|
| 1 | Canvas / UI Hierarchy | — |
| 2 | `SaveSystem` | — |
| 3 | `ObjectPoolManager` | — |
| 4 | `DayNightWeatherController` | — |
| 5 | `ProceduralAudioSystem` | — |
| 6 | `EmotionalBondingEngine` | — |
| 7 | `CognitiveAnalyticsSystem` | — |
| 8 | `DynamicDifficultySystem` | Analytics |
| 9–11 | VFX Stack (Particles, Glow, Ambient, Manager) | — |
| 12 | `CameraFeelController` | Camera.main |
| 13 | `QuestEngine` | BondingEngine, SaveSystem |
| 14 | `AchievementSystem` | SaveSystem |
| 15 | `ExplorationAnalyticsSystem` | — |
| 16 | `DailyRitualSystem` | SaveSystem |
| 17 | `InventoryCraftingSystem` | BondingEngine |
| 18 | `PuzzleManager` | Analytics, Difficulty, Audio, VFX, Quests |
| 19–21 | Sanctuary (Grid, Catalog, Campfire) | BondingEngine, Quests, Audio |
| 22 | `DynamicDialogueSystem` | Audio, BondingEngine |
| 23 | `AdaptiveVisualDensityScaler` | AmbientVFX, Particles, PuzzleManager |
| 24 | `WorldStateManager` | SaveSystem, Quests |
| 25 | `BiomeController` | TimeController, Audio |
| 26 | `SeasonalEventSystem` | SaveSystem, Achievements |
| 27 | `CreatureEvolutionSystem` | BondingEngine, SaveSystem, Achievements |
| 28 | `BossEncounterSystem` | Achievements, World, VFX, Quests, SaveSystem |
| 29 | `AccessibilityManager` | — |
| 30 | `AnimatedTransitionController` | — |
| 31–32 | UIStateController + ForestUIRouter | Transitions, SaveData |
| 33 | `ReducedMotionController` | Particles, Transitions |
| 34 | `AudioAssetLibrary` | — |
| 35 | `ForestMusicDirector` | AudioLibrary |
| 36 | `DynamicAmbientMixer` | AudioLibrary |
| 37 | `PuzzleSFXManager` | AudioLibrary |
| 38 | `CreatureMoodBrain` | BondingEngine |
| 39 | `CreatureVoiceSystem` | AudioLibrary, MoodBrain |
| 40 | `RelationshipMemorySystem` | — |
| 41 | `DynamicSeasonManager` | SaveSystem |
| 42 | `RareWorldEventSystem` | SeasonManager, SaveSystem |
| 43 | `EnvironmentalStorySystem` | SeasonManager, World, WorldEvents |
| 44 | `LivingWorldController` | SeasonManager, WorldEvents, StorySystem, MusicDirector |
| 45 | `WeeklyInsightGenerator` | Analytics, Achievements, BondingEngine |
| 46 | `PerformanceManager` | — |
| 47 | `MemoryBudgetController` | Performance, AudioLibrary |
| 48–51 | VFX Overhaul (Firefly, Water, SpriteParticles, EnvFX) | VFXLayer, Performance |
| 52 | `DeviceCapabilityProfiler` | — |
| 53 | `ReleaseConfiguration` | — |
| 54 | `ProductionLogger` | SaveSystem, Config |
| 55 | `LocalizationManager` | — |
| 56 | `ContentVersionManager` + `AddressableContentManager` | — |
| 57 | `UIAnimationSystem` | ReducedMotion |
| 58 | `NintendoFeelSystem` | UIAnim, CameraFeel, ReducedMotion |
| 59 | `RetentionPacingSystem` | SaveSystem |
| 60 | `ProgressionPacingSystem` | SaveSystem, Difficulty, World |
| 61 | `EmotionalMilestoneSystem` | VFX, Audio, Dialogue, UIAnim |
| 62 | `SanctuaryDecorationSystem` | BondingEngine, SaveSystem, VFX |
| 63 | `InteractiveCampfireController` | TimeController, BondingEngine, Audio, VFX |
| 64 | `CreatureHomeBehavior` | BondingEngine, TimeController, VFX, Audio |
| 65 | `SanctuarySeasonalVisuals` | SeasonManager, SanctuaryDecor, Particles |
| 66 | `AdaptiveTutorialBrain` + `GuidedTutorialSystem` | SaveData, VFX, Dialogue |
| 67 | `FirstBondSequence` | BondingEngine, VFX, Dialogue, Audio |
| 68–69 | Parent Systems (WeeklyReport, WellnessInsights) | Analytics, BondingEngine |
| 70–71 | Social (Scrapbook, Screenshots) | SaveSystem, Evolution, UIAnim |
| 72 | `DebugToolkit` | Container, Config |

---

## Canvas Hierarchy

```
ForestQuestRoot (DontDestroyOnLoad)
└── MainCanvas  [ScreenSpaceOverlay, 1080×1920 reference]
    ├── WorldLayer  [siblingIndex 0]  — biome backgrounds, creature homes
    ├── VFXLayer    [siblingIndex 1]  — particles, glow, fireflies, water
    └── UILayer     [siblingIndex 2]  — HUD, dialogue, menus, panels
```

---

## Cross-System Event Map

| Source System | Event | Consumers |
|---|---|---|
| `DayNightWeatherController` | `OnTimeChanged` | VFXManager, ForestMusicDirector |
| `DayNightWeatherController` | `OnWeatherChanged` | VFXManager |
| `SeasonalEventSystem` | `OnSeasonChanged` | VFXManager, AmbientVFX |
| `WorldStateManager` | `OnRegionUnlocked` | VFXManager (discovery), MusicDirector, EmotionalMilestones |
| `BossEncounterSystem` | `OnBossDefeated` | VFXManager, MusicDirector, PuzzleSFX, Scrapbook, Milestones |
| `BossEncounterSystem` | `OnBossPhaseCleared` | VFXManager (discovery) |
| `CreatureEvolutionSystem` | `OnStageEvolved` | VFXManager, Milestones, Scrapbook |
| `AchievementSystem` | `OnAchievementUnlocked` | VFXManager |
| `ExplorationAnalyticsSystem` | `OnZoneFirstVisited` | Achievements, BiomeController, EnvFXDirector |
| `ExplorationAnalyticsSystem` | `OnLoreCollected` | Achievements, Milestones, Scrapbook |
| `QuestEngine` | `OnQuestCompleted` | DynamicDialogueSystem |
| `DynamicSeasonManager` | `OnSeasonChanged` | EnvironmentalFXDirector |
| `RareWorldEventSystem` | `OnEventStarted` | EnvironmentalFXDirector |
| `RetentionPacingSystem` | `OnSessionCapReached` | DynamicDialogueSystem |
| `EmotionalMilestoneSystem` | — | Queued internally; fires VFX+Audio+Dialogue+UIAnim |
| `Seasons.OnEventAttended` | — | EmotionalMilestones |
| `ProgressionPacingSystem` | `OnMilestoneReached` | EmotionalMilestones |
| `SanctuaryDecorSystem` | `OnDecorationPlaced` | AnalyticsEventRouter |
| `EmotionalBondingEngine` | `OnBondLevelUp` | AnalyticsEventRouter |
| `IAPManager` | `OnPurchaseSuccess` | PremiumUnlockController, AnalyticsEventRouter |

---

## Folder Structure

```
Assets/
  Scripts/
    Analytics/           FirebaseAnalyticsConnector, AnalyticsEventRouter,
                         RetentionCohortTracker, FunnelAnalysisSystem
    AI/                  CreatureAIController
    Animation/           CreatureAnimController, MoodBrain, EmotionStateMachine,
                         RelationshipMemory, AmbientBehavior, EvolutionRenderer
    Architecture/        UIStateController, ForestUIRouter, PanelViewController,
                         AnimatedTransitionController, ReusableCardPool
    Audio/               ForestMusicDirector, DynamicAmbientMixer, PuzzleSFXManager,
                         CreatureVoiceSystem, AudioAssetLibrary, AdaptiveMusicTransition
    Bootstrap/           ForestQuestBootstrap
    Config/              LocalizationManager, ProductionLogger, ReleaseConfiguration
    Controllers/         OnboardingDirector, IntroCinematicController,
                         CreatureInteractionController, WorldNavigationController,
                         ParentDashboardController, RitualViewController,
                         AccessibilitySettingsPanel, PuzzleGameController
    Core/                ForestQuestApp, ForestSystemsContainer, ForestDataLoader,
                         ForestGameContent, ForestProgressData, SanctuaryDragHandler,
                         AddressableContentManager, LiveContentPipeline,
                         ContentVersionManager
    Debug/               DebugToolkit
    Gameplay/            InventoryCraftingSystem
    Monetization/        IAPManager, PremiumUnlockController, ParentPurchaseGate,
                         CosmeticCatalogSystem
    Onboarding/          FirstBondSequence, AdaptiveTutorialBrain, GuidedTutorialSystem
    Parent/              WellnessInsightEngine, WeeklyReportGenerator
    Performance/         DeviceCapabilityProfiler
    Polish/              NintendoFeelSystem
    Puzzle/              PuzzleManager, RotatingPathPuzzle, PressureGatePuzzle,
                         LightReflectionPuzzle, MusicPatternPuzzle, LogicMirrorPuzzle,
                         RuneSequencePuzzle, MemoryTrailPuzzle, TimeMemoryChallenge
    Sanctuary/           SanctuaryBuilderManager, SanctuaryPlacementGrid,
                         SanctuaryDecorationCatalog, SanctuaryDecorationSystem,
                         SanctuaryCampfireSystem, SanctuarySeasonalVisuals,
                         InteractiveCampfireController, CreatureHomeBehavior
    Save/                SaveSystem, ModularSaveSystem
    Social/              ScreenshotComposer, MemoryScrapbookMode
    Systems/             (all major game systems — 30 files)
    UI/                  GuideCharacterView, DynamicDialogueSystem, CameraFeelController,
                         UIAnimationSystem, AdaptiveVisualDensityScaler, SkillGrowthGraphRenderer
    Utilities/           ForestUiFactory
    VFX/                 WaterRippleSystem, EnvironmentalFXDirector, ProceduralGlowSystem,
                         FireflyTrailSystem, AmbientVFXController, SpriteParticleRenderer,
                         EmotionalParticleEngine, VFXManager
    Weather/             DayNightWeatherController
    WorldMap/            MapPathAnimator, FogOfWarSystem, WorldMapController,
                         RegionUnlockSequence
```

---

## Dependency Graph (condensed)

```
SaveSystem ←── QuestEngine ←── PuzzleManager
     ↑               ↑               ↑
BondingEngine    Achievements    Analytics
     ↑               ↑               ↑
Evolution      SeasonalEvents    Difficulty
     ↑               ↑
Milestones      UIRouter
     ↑
VFXManager ←── ReducedMotion ←── Accessibility
     ↑
NintendoFeel
```

All arrows point from consumer to dependency. Circular dependencies are
broken by event subscriptions (not direct method calls).
