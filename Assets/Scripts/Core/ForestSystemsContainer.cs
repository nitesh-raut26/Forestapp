using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// ForestSystemsContainer — the production-grade service locator and
    /// dependency injector for Forest Friends Quest.
    ///
    /// All systems created here are wired together in the correct dependency
    /// order. This single MonoBehaviour sits on the root GameObject alongside
    /// ForestQuestApp, which delegates system initialization here.
    ///
    /// Initialization order (hard dependency chain):
    ///   1. Canvas / UI hierarchy
    ///   2. SaveSystem
    ///   3. ObjectPoolManager
    ///   4. DayNightWeatherController
    ///   5. ProceduralAudioSystem
    ///   6. EmotionalBondingEngine
    ///   7. CognitiveAnalyticsSystem
    ///   8. DynamicDifficultySystem
    ///   9. VFX layer (EmotionalParticleEngine, ProceduralGlowSystem,
    ///                 AmbientVFXController, VFXManager)
    ///  10. CameraFeelController
    ///  11. QuestEngine
    ///  12. AchievementSystem
    ///  13. ExplorationAnalyticsSystem
    ///  14. DailyRitualSystem
    ///  15. InventoryCraftingSystem
    ///  16. PuzzleManager
    ///  17. Sanctuary (PlacementGrid, DecorationCatalog, CampfireSystem)
    ///  18. DynamicDialogueSystem
    ///  19. AdaptiveVisualDensityScaler
    ///  20. WorldStateManager
    ///  21. BiomeController
    ///  22. SeasonalEventSystem
    ///  23. CreatureEvolutionSystem
    ///  24. BossEncounterSystem
    /// </summary>
    public class ForestSystemsContainer : MonoBehaviour
    {
        // ─── All Systems (exposed as readonly properties) ─────────────────────────

        public SaveSystem                   SaveSystem          { get; private set; }
        public ObjectPoolManager            PoolManager         { get; private set; }
        public DayNightWeatherController    TimeController      { get; private set; }
        public ProceduralAudioSystem        Audio               { get; private set; }
        public EmotionalBondingEngine       BondingEngine       { get; private set; }
        public CognitiveAnalyticsSystem     Analytics           { get; private set; }
        public DynamicDifficultySystem      Difficulty          { get; private set; }
        public EmotionalParticleEngine      Particles           { get; private set; }
        public ProceduralGlowSystem         Glow                { get; private set; }
        public AmbientVFXController         AmbientVFX          { get; private set; }
        public VFXManager                   VFX                 { get; private set; }
        public CameraFeelController         CameraFeel          { get; private set; }
        public QuestEngine                  Quests              { get; private set; }
        public AchievementSystem            Achievements        { get; private set; }
        public ExplorationAnalyticsSystem   Exploration         { get; private set; }
        public DailyRitualSystem            DailyRitual         { get; private set; }
        public InventoryCraftingSystem      Inventory           { get; private set; }
        public PuzzleManager                PuzzleManager       { get; private set; }
        public SanctuaryPlacementGrid       SanctuaryGrid       { get; private set; }
        public SanctuaryDecorationCatalog   DecorCatalog        { get; private set; }
        public SanctuaryCampfireSystem      Campfire            { get; private set; }
        public DynamicDialogueSystem        Dialogue            { get; private set; }
        public AdaptiveVisualDensityScaler  DensityScaler       { get; private set; }

        // ─── AAA Expansion Systems ────────────────────────────────────────────────
        public WorldStateManager            World               { get; private set; }
        public BiomeController              Biome               { get; private set; }
        public SeasonalEventSystem          Seasons             { get; private set; }
        public CreatureEvolutionSystem      Evolution           { get; private set; }
        public BossEncounterSystem          Bosses              { get; private set; }

        // ─── Phase 2 AAA Systems ──────────────────────────────────────────────────
        public UIStateController            UIState             { get; private set; }
        public ForestUIRouter               UIRouter            { get; private set; }
        public AnimatedTransitionController Transitions         { get; private set; }
        public AccessibilityManager         Accessibility       { get; private set; }
        public ReducedMotionController      ReducedMotion       { get; private set; }
        public ForestMusicDirector          MusicDirector       { get; private set; }
        public AudioAssetLibrary            AudioLibrary        { get; private set; }
        public DynamicAmbientMixer          AmbientMixer        { get; private set; }
        public PuzzleSFXManager             PuzzleSFX           { get; private set; }
        public CreatureVoiceSystem          CreatureVoice       { get; private set; }
        public CreatureMoodBrain            MoodBrain           { get; private set; }
        public RelationshipMemorySystem     RelationshipMemory  { get; private set; }
        public DynamicSeasonManager         SeasonManager       { get; private set; }
        public RareWorldEventSystem         WorldEvents         { get; private set; }
        public EnvironmentalStorySystem     StorySystem         { get; private set; }
        public LivingWorldController        LivingWorld         { get; private set; }
        public WeeklyInsightGenerator       WeeklyInsights      { get; private set; }

        // ─── Performance + VFX Overhaul ───────────────────────────────────────────
        public PerformanceManager           Performance         { get; private set; }
        public MemoryBudgetController       MemoryBudget        { get; private set; }
        public FireflyTrailSystem           FireflyFX           { get; private set; }
        public WaterRippleSystem            WaterFX             { get; private set; }
        public SpriteParticleRenderer       SpriteParticles     { get; private set; }
        public EnvironmentalFXDirector      EnvFX               { get; private set; }

        // ─── Task 7: Performance Utilities ────────────────────────────────────────
        public UIPoolManager                UIPool              { get; private set; }
        public AsyncContentLoader           ContentLoader       { get; private set; }

        // ─── Phase 3: Production Completion Systems ───────────────────────────────
        public AddressableContentManager    AddressableContent  { get; private set; }
        public ContentVersionManager        VersionManager      { get; private set; }
        public GuidedTutorialSystem         Tutorial            { get; private set; }
        public AdaptiveTutorialBrain        TutorialBrain       { get; private set; }
        public FirstBondSequence            FirstBond           { get; private set; }
        public SanctuaryDecorationSystem    SanctuaryDecor      { get; private set; }
        public InteractiveCampfireController CampfireCtrl       { get; private set; }
        public CreatureHomeBehavior         CreatureHomes       { get; private set; }
        public SanctuarySeasonalVisuals     SanctuarySeasonals  { get; private set; }
        public UIAnimationSystem            UIAnim              { get; private set; }
        public RetentionPacingSystem        Retention           { get; private set; }
        public ProgressionPacingSystem      Progression         { get; private set; }
        public EmotionalMilestoneSystem     Milestones          { get; private set; }
        public WeeklyReportGenerator        WeeklyReport        { get; private set; }
        public WellnessInsightEngine        WellnessInsights    { get; private set; }
        public ScreenshotComposer           Screenshots         { get; private set; }
        public MemoryScrapbookMode          Scrapbook           { get; private set; }
        public NintendoFeelSystem           NintendoFeel        { get; private set; }
        public DeviceCapabilityProfiler     DeviceProfiler      { get; private set; }
        public LocalizationManager          Localization        { get; private set; }
        public ProductionLogger             Logger              { get; private set; }
        public ReleaseConfiguration         Config              { get; private set; }
        public DebugToolkit                 DebugTools          { get; private set; }

        // ─── Canvases ─────────────────────────────────────────────────────────────

        public Canvas      MainCanvas     { get; private set; }
        public RectTransform CanvasRoot   { get; private set; }
        private RectTransform _vfxLayer;
        private RectTransform _worldLayer;
        private RectTransform _uiLayer;

        // ─── Initialization ───────────────────────────────────────────────────────

        public void InitializeAll()
        {
            BuildCanvasHierarchy();

            // 2. Save System
            SaveSystem = gameObject.AddComponent<SaveSystem>();

            // 3. Object Pool
            PoolManager = gameObject.AddComponent<ObjectPoolManager>();

            // 4. Time / Weather
            TimeController = gameObject.AddComponent<DayNightWeatherController>();

            // 5. Audio
            Audio = gameObject.AddComponent<ProceduralAudioSystem>();

            // 6. Bonding Engine
            BondingEngine = gameObject.AddComponent<EmotionalBondingEngine>();

            // 7. Cognitive Analytics
            Analytics = gameObject.AddComponent<CognitiveAnalyticsSystem>();

            // 8. Dynamic Difficulty
            Difficulty = gameObject.AddComponent<DynamicDifficultySystem>();
            Difficulty.Initialize(Analytics);

            // 9. VFX Layer
            var vfxGo = new GameObject("VFXSystems");
            vfxGo.transform.SetParent(transform, false);

            Particles = vfxGo.AddComponent<EmotionalParticleEngine>();
            Particles.particleCanvas = _vfxLayer;

            Glow = vfxGo.AddComponent<ProceduralGlowSystem>();
            Glow.glowCanvas = _vfxLayer;

            AmbientVFX = vfxGo.AddComponent<AmbientVFXController>();
            AmbientVFX.ambientCanvas = _vfxLayer;

            VFX = vfxGo.AddComponent<VFXManager>();
            VFX.Initialize(Particles, Glow, AmbientVFX);

            // 10. Camera Feel
            CameraFeel = Camera.main != null
                ? Camera.main.gameObject.AddComponent<CameraFeelController>()
                : gameObject.AddComponent<CameraFeelController>();

            // 11. Quest Engine
            Quests = gameObject.AddComponent<QuestEngine>();
            Quests.Initialize(BondingEngine, SaveSystem);

            // 12. Achievements
            Achievements = gameObject.AddComponent<AchievementSystem>();
            Achievements.Initialize(SaveSystem);

            // Wire achievement events to VFX
            Achievements.OnAchievementUnlocked += (achievement) =>
            {
                VFX.OnRareReward(CanvasRoot.rect.center);
            };

            // 13. Exploration Analytics
            Exploration = gameObject.AddComponent<ExplorationAnalyticsSystem>();
            Exploration.Initialize();

            // 14. Daily Ritual
            DailyRitual = gameObject.AddComponent<DailyRitualSystem>();
            DailyRitual.Initialize(SaveSystem);

            // 15. Inventory & Crafting
            Inventory = gameObject.AddComponent<InventoryCraftingSystem>();
            Inventory.Initialize(BondingEngine);

            // 16. Puzzle Manager
            PuzzleManager = gameObject.AddComponent<PuzzleManager>();
            PuzzleManager.Initialize(Analytics, Difficulty, Audio, VFX, Quests);

            // 17. Sanctuary Systems
            InitializeSanctuarySystems();

            // 18. Dialogue
            Dialogue = gameObject.AddComponent<DynamicDialogueSystem>();
            Dialogue.Initialize(Audio, BondingEngine, _uiLayer);

            // Wire dialogue events
            Quests.OnQuestCompleted += (quest) =>
            {
                var seq = Dialogue.GetAdaptedSequence(quest.creatureId, "puzzle_solved");
                if (seq != null) Dialogue.StartSequence(seq);
            };

            // 19. Density Scaler (last VFX dependent)
            DensityScaler = gameObject.AddComponent<AdaptiveVisualDensityScaler>();
            DensityScaler.Initialize(AmbientVFX, Particles, PuzzleManager);

            // 20. World State Manager
            World = gameObject.AddComponent<WorldStateManager>();
            World.Initialize(SaveSystem, Quests);

            // 21. Biome Controller
            Biome = gameObject.AddComponent<BiomeController>();
            Biome.Initialize(TimeController, Audio);

            // 22. Seasonal Event System
            Seasons = gameObject.AddComponent<SeasonalEventSystem>();
            Seasons.Initialize(SaveSystem, Achievements);

            // 23. Creature Evolution System
            Evolution = gameObject.AddComponent<CreatureEvolutionSystem>();
            Evolution.Initialize(BondingEngine, SaveSystem, Achievements);

            // 24. Boss Encounter System (depends on World, Achievements, VFX, Quests, Save)
            Bosses = gameObject.AddComponent<BossEncounterSystem>();
            Bosses.Initialize(Achievements, World, VFX, Quests, SaveSystem);

            // ─── Cross-System Event Wiring ────────────────────────────────────────

            // Ambient VFX follows day/night and weather
            TimeController.OnTimeChanged    += t  => VFX.SetAmbientState(t, TimeController.CurrentWeather);
            TimeController.OnWeatherChanged += w  => VFX.SetAmbientState(TimeController.CurrentTime, w);

            // Audio bridge
            TimeController.SetAudioBridge(Audio);

            // Season changes update weather controller tint
            Seasons.OnSeasonChanged += season =>
            {
                var weather = Seasons.GetSeasonWeather();
                VFX.SetAmbientState(TimeController.CurrentTime, weather);
            };

            // Region unlocked → discovery burst
            World.OnRegionUnlocked += region =>
            {
                VFX.OnDiscovery(CanvasRoot != null ? CanvasRoot.rect.center : Vector2.zero);
            };

            // Boss phase cleared → discovery burst; boss defeated → rare reward
            Bosses.OnBossPhaseCleared += (boss, phases) =>
            {
                VFX.OnDiscovery(Vector2.zero);
            };
            Bosses.OnBossDefeated += boss =>
            {
                VFX.OnRareReward(Vector2.zero);
            };

            // Creature stage evolution → rare reward VFX
            Evolution.OnStageEvolved += (creatureId, stage) =>
            {
                VFX.OnRareReward(Vector2.zero);
                Debug.Log($"[ForestSystemsContainer] {creatureId} evolved to: {stage.stageName}");
            };

            // Exploration events feed achievements + biome
            Exploration.OnZoneFirstVisited += (zoneId) =>
            {
                Achievements.TryUnlock("exp_first_steps");
                if (Exploration.GetVisitedZoneCount() >= 5)  Achievements.TryUnlock("exp_5_zones");
                if (Exploration.GetVisitedZoneCount() >= 10) Achievements.TryUnlock("exp_all_zones");
                Biome.EnterBiome(zoneId);
            };

            Exploration.OnLoreCollected += (_) =>
            {
                if (Exploration.TotalLoreCollected >= 12) Achievements.TryUnlock("sec_lore_complete");
            };

            // ─── Phase 2 AAA Systems ───────────────────────────────────────────────

            // 25. Accessibility
            Accessibility = gameObject.AddComponent<AccessibilityManager>();
            Accessibility.Initialize(ForestUiFactory.GetDefaultFont(), ForestUiFactory.GetDefaultFont());

            // 26. Animated Transitions
            Transitions = gameObject.AddComponent<AnimatedTransitionController>();

            // 27-28. UI State Controller + Router (mutually dependent — router created first)
            UIRouter = gameObject.AddComponent<ForestUIRouter>();
            UIState  = gameObject.AddComponent<UIStateController>();
            UIState.Initialize(Transitions, UIRouter);
            UIRouter.Initialize(UIState, this, SaveSystem?.ActiveData);

            // 29. Reduced Motion
            ReducedMotion = gameObject.AddComponent<ReducedMotionController>();
            ReducedMotion.Initialize(Particles, Transitions);

            // Wire accessibility calm mode to reduced motion
            var savedCalmMode = UnityEngine.PlayerPrefs.GetInt("FFQ.Access.CalmMode", 0) == 1;
            if (savedCalmMode) { Accessibility.SetCalmMode(true); ReducedMotion.SetReducedMotion(true); }

            // 30. Audio Library + Music Director
            AudioLibrary = gameObject.AddComponent<AudioAssetLibrary>();

            var audioGo = new GameObject("MusicDirector");
            audioGo.transform.SetParent(transform, false);
            MusicDirector = audioGo.AddComponent<ForestMusicDirector>();
            MusicDirector.Initialize(AudioLibrary);

            AmbientMixer = gameObject.AddComponent<DynamicAmbientMixer>();
            AmbientMixer.Initialize(AudioLibrary);

            PuzzleSFX = gameObject.AddComponent<PuzzleSFXManager>();
            PuzzleSFX.Initialize(AudioLibrary);

            // 31. Creature Systems
            MoodBrain = gameObject.AddComponent<CreatureMoodBrain>();
            MoodBrain.Initialize(BondingEngine);

            CreatureVoice = gameObject.AddComponent<CreatureVoiceSystem>();
            CreatureVoice.Initialize(AudioLibrary, MoodBrain);

            RelationshipMemory = gameObject.AddComponent<RelationshipMemorySystem>();
            RelationshipMemory.Initialize();

            // 32. Living World
            SeasonManager = gameObject.AddComponent<DynamicSeasonManager>();
            SeasonManager.Initialize(SaveSystem);

            WorldEvents = gameObject.AddComponent<RareWorldEventSystem>();
            WorldEvents.Initialize(SeasonManager, SaveSystem);

            StorySystem = gameObject.AddComponent<EnvironmentalStorySystem>();
            StorySystem.Initialize(SeasonManager, World, WorldEvents);

            LivingWorld = gameObject.AddComponent<LivingWorldController>();
            LivingWorld.Initialize(SeasonManager, WorldEvents, StorySystem, MusicDirector);

            // Wire region unlock to music director
            World.OnRegionUnlocked += r => MusicDirector.OnRegionUnlocked(r.regionId);

            // Wire boss events to music director
            Bosses.OnBossDefeated += boss =>
            {
                MusicDirector.OnBossDefeated();
                PuzzleSFX.OnPuzzleComplete();
            };

            // 33. Weekly Insights
            WeeklyInsights = gameObject.AddComponent<WeeklyInsightGenerator>();
            WeeklyInsights.Initialize(Analytics, Achievements, BondingEngine);

            // 34. Performance
            Performance = gameObject.AddComponent<PerformanceManager>();
            Performance.Initialize();

            // Apply performance caps to existing systems
            if (!Performance.AmbientVFXEnabled)
            {
                AmbientVFX.gameObject.SetActive(false);
                Glow.gameObject.SetActive(false);
            }

            // 35. Memory Budget
            MemoryBudget = gameObject.AddComponent<MemoryBudgetController>();
            MemoryBudget.Initialize(Performance, AudioLibrary);

            // 36. VFX Overhaul systems (attached to VFX layer)
            var vfxOverhaulGo = new GameObject("VFXOverhaul");
            vfxOverhaulGo.transform.SetParent(transform, false);

            FireflyFX = vfxOverhaulGo.AddComponent<FireflyTrailSystem>();
            FireflyFX.Initialize(_vfxLayer, Performance);

            WaterFX = vfxOverhaulGo.AddComponent<WaterRippleSystem>();
            WaterFX.Initialize(_vfxLayer, Performance);

            SpriteParticles = vfxOverhaulGo.AddComponent<SpriteParticleRenderer>();
            SpriteParticles.Initialize(_vfxLayer, Performance);

            EnvFX = vfxOverhaulGo.AddComponent<EnvironmentalFXDirector>();
            EnvFX.Initialize(FireflyFX, WaterFX, Particles, AmbientVFX, Performance);

            // Wire zone changes to environmental FX
            Exploration.OnZoneFirstVisited += zoneId => EnvFX.OnZoneChanged(zoneId);

            // Wire season changes to environmental FX
            SeasonManager.OnSeasonChanged += (prev, next) => EnvFX.OnSeasonChanged(next);

            // Wire world events to environmental FX
            WorldEvents.OnEventStarted += e => EnvFX.OnWorldEventStarted(e);

            // Task 7 — Performance Utilities
            UIPool = new UIPoolManager();

            ContentLoader = gameObject.AddComponent<AsyncContentLoader>();

            // ─── Phase 3: Production Completion Systems ───────────────────────────

            // 37. Device profiler (must be before performance)
            DeviceProfiler = gameObject.AddComponent<DeviceCapabilityProfiler>();
            DeviceProfiler.Initialize();

            // 38. Release Configuration
            Config = gameObject.AddComponent<ReleaseConfiguration>();
            Config.Initialize();

            // 39. Production Logger
            Logger = gameObject.AddComponent<ProductionLogger>();
            Logger.Initialize(SaveSystem, !Config.IsDebug);

            // 40. Localization
            Localization = gameObject.AddComponent<LocalizationManager>();
            Localization.Initialize();

            // 41. Addressable Content Manager
            VersionManager = gameObject.AddComponent<ContentVersionManager>();
            VersionManager.Initialize();
            if (SaveSystem?.ActiveData != null)
                VersionManager.MigrateSave(SaveSystem.ActiveData);

            AddressableContent = gameObject.AddComponent<AddressableContentManager>();
            AddressableContent.Initialize(VersionManager);

            // 42. UI Animation System (Nintendo feel foundation)
            UIAnim = gameObject.AddComponent<UIAnimationSystem>();
            UIAnim.Initialize(ReducedMotion);

            // 43. Nintendo Feel System
            NintendoFeel = gameObject.AddComponent<NintendoFeelSystem>();
            NintendoFeel.Initialize(UIAnim, CameraFeel, ReducedMotion);

            // 44. Retention Pacing
            Retention = gameObject.AddComponent<RetentionPacingSystem>();
            Retention.Initialize(SaveSystem);
            Retention.OnSessionCapReached += () => Dialogue.GetAdaptedSequence("pip", "break_reminder");

            // 45. Progression Pacing
            Progression = gameObject.AddComponent<ProgressionPacingSystem>();
            Progression.Initialize(SaveSystem, Difficulty, World);

            // 46. Emotional Milestone System
            Milestones = gameObject.AddComponent<EmotionalMilestoneSystem>();
            Milestones.Initialize(VFX, Audio, Dialogue, UIAnim, ReducedMotion);

            // Wire milestone triggers
            Evolution.OnStageEvolved     += (id, stage) => Milestones.TriggerEvolutionReveal(id, stage.stageName);
            World.OnRegionUnlocked       += r           => Milestones.TriggerRegionUnlock(r.displayName);
            Bosses.OnBossDefeated        += b           => Milestones.TriggerBossDefeat(b.name);
            Exploration.OnLoreCollected  += loreId      => Milestones.TriggerLoreDiscovery(loreId);
            Seasons.OnEventAttended      += ev          => Milestones.TriggerSeasonalEventReveal(ev.title);
            Progression.OnMilestoneReached += count    => Milestones.TriggerPuzzleMilestone(count);

            // 47. Sanctuary Decoration System (full 30-item catalogue)
            SanctuaryDecor = gameObject.AddComponent<SanctuaryDecorationSystem>();
            SanctuaryDecor.Initialize(BondingEngine, SaveSystem, VFX);

            // 48. Interactive Campfire Controller
            CampfireCtrl = gameObject.AddComponent<InteractiveCampfireController>();
            CampfireCtrl.Initialize(TimeController, BondingEngine, Audio, VFX);
            CampfireCtrl.OnStoryUnlocked  += storyId => Scrapbook?.RecordSeasonalEvent(storyId, SeasonManager?.CurrentSeason ?? "spring");

            // 49. Creature Home Behavior
            CreatureHomes = gameObject.AddComponent<CreatureHomeBehavior>();
            CreatureHomes.Initialize(BondingEngine, TimeController, VFX, Audio);
            CreatureHomes.OnHomeUpgraded  += (id, state) => Milestones.TriggerEvolutionReveal(id, state.ToString());

            // 50. Sanctuary Seasonal Visuals
            SanctuarySeasonals = gameObject.AddComponent<SanctuarySeasonalVisuals>();
            SanctuarySeasonals.Initialize(SeasonManager, SanctuaryDecor, Particles, TimeController, ReducedMotion);

            // 51. Onboarding systems
            TutorialBrain = gameObject.AddComponent<AdaptiveTutorialBrain>();
            TutorialBrain.Initialize(SaveSystem?.ActiveData);

            Tutorial = gameObject.AddComponent<GuidedTutorialSystem>();
            Tutorial.Initialize(TutorialBrain, SaveSystem, VFX, Dialogue);

            FirstBond = gameObject.AddComponent<FirstBondSequence>();
            FirstBond.Initialize(BondingEngine, VFX, Dialogue, Audio, ReducedMotion, MoodBrain);
            FirstBond.OnBondSequenceComplete += id => Scrapbook?.RecordBondMoment(id, BondingEngine?.GetBondLevel(id) ?? 1);

            // 52. Parent premium systems
            WeeklyReport = gameObject.AddComponent<WeeklyReportGenerator>();
            WeeklyReport.Initialize(Analytics, BondingEngine, Achievements, Retention);

            WellnessInsights = gameObject.AddComponent<WellnessInsightEngine>();
            WellnessInsights.Initialize(Analytics, Difficulty, BondingEngine, Retention);

            // 53. Social systems
            Scrapbook = gameObject.AddComponent<MemoryScrapbookMode>();
            Scrapbook.Initialize(SaveSystem, UIAnim);

            Screenshots = gameObject.AddComponent<ScreenshotComposer>();
            Screenshots.Initialize(Evolution, BondingEngine, SanctuaryDecor, UIAnim);

            // Wire scrapbook to progression events
            Bosses.OnBossDefeated  += b => Scrapbook.RecordBossDefeat(b.name, "unknown");
            Evolution.OnStageEvolved += (id, stage) => Scrapbook.RecordEvolution(id, stage.stageName);
            Exploration.OnLoreCollected += loreId => Scrapbook.RecordLoreDiscovery(loreId, "unknown");

            // 54. Debug Toolkit (Release-builds auto-disable)
            DebugTools = gameObject.AddComponent<DebugToolkit>();
            DebugTools.Initialize(this, Config);

            Debug.Log("[ForestSystemsContainer] All 54 production systems initialized successfully.");
        }

        // ─── Canvas Hierarchy ─────────────────────────────────────────────────────

        private void BuildCanvasHierarchy()
        {
            var canvasGo = new GameObject("MainCanvas");
            canvasGo.transform.SetParent(transform, false);

            MainCanvas = canvasGo.AddComponent<Canvas>();
            MainCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            MainCanvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            CanvasRoot = canvasGo.GetComponent<RectTransform>();

            // World layer — game world content
            _worldLayer = CreateLayer("WorldLayer", 0);

            // VFX layer — particles and glow effects (drawn above world)
            _vfxLayer = CreateLayer("VFXLayer", 1);

            // UI layer — dialogue, HUD, menus (drawn above VFX)
            _uiLayer = CreateLayer("UILayer", 2);
        }

        private RectTransform CreateLayer(string name, int siblingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(CanvasRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.SetSiblingIndex(siblingOrder);
            return rt;
        }

        // ─── Sanctuary ────────────────────────────────────────────────────────────

        private void InitializeSanctuarySystems()
        {
            var sanctuaryGo = new GameObject("SanctuarySystems");
            sanctuaryGo.transform.SetParent(transform, false);

            SanctuaryGrid = sanctuaryGo.AddComponent<SanctuaryPlacementGrid>();

            DecorCatalog = sanctuaryGo.AddComponent<SanctuaryDecorationCatalog>();
            DecorCatalog.Initialize(BondingEngine, Quests);

            // Campfire positioned in the sanctuary center
            var campfireRt = new GameObject("CampfireAnchor").AddComponent<RectTransform>();
            campfireRt.transform.SetParent(_worldLayer, false);
            campfireRt.anchoredPosition = new Vector2(0f, -80f);
            campfireRt.sizeDelta        = new Vector2(80f, 100f);

            Campfire = sanctuaryGo.AddComponent<SanctuaryCampfireSystem>();
            Campfire.Initialize(Audio, Particles, TimeController, BondingEngine, campfireRt);

            // Wire campfire events
            Campfire.OnBedtimeStoryUnlocked += (storyId) =>
            {
                var seq = Dialogue.GetAdaptedSequence("tomo", "bedtime");
                if (seq != null) Dialogue.StartSequence(seq);
            };

            Campfire.OnNPCVisitTriggered += (creatureId) =>
            {
                var seq = Dialogue.GetAdaptedSequence(creatureId, "rare_event");
                if (seq != null) Dialogue.StartSequence(seq);
            };
        }

        // ─── Runtime System Access ────────────────────────────────────────────────

        /// <summary>Static shortcut — find the active container in scene.</summary>
        private static ForestSystemsContainer _instance;
        public static ForestSystemsContainer Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<ForestSystemsContainer>();
                return _instance;
            }
        }
    }
}
