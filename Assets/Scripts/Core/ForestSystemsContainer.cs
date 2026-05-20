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

            // 19. Density Scaler (last — depends on all VFX systems)
            DensityScaler = gameObject.AddComponent<AdaptiveVisualDensityScaler>();
            DensityScaler.Initialize(AmbientVFX, Particles, PuzzleManager);

            // Subscribe ambient VFX to time/weather changes
            TimeController.OnTimeChanged    += t  => VFX.SetAmbientState(t, TimeController.CurrentWeather);
            TimeController.OnWeatherChanged += w  => VFX.SetAmbientState(TimeController.CurrentTime, w);

            // Wire audio bridge into weather controller (plan requirement)
            TimeController.SetAudioBridge(Audio);


            // Exploration events feed achievements
            Exploration.OnZoneFirstVisited += (zoneId) =>
            {
                Achievements.TryUnlock("exp_first_steps");
                if (Exploration.GetVisitedZoneCount() >= 5)  Achievements.TryUnlock("exp_5_zones");
                if (Exploration.GetVisitedZoneCount() >= 10) Achievements.TryUnlock("exp_all_zones");
            };

            Exploration.OnLoreCollected += (_) =>
            {
                if (Exploration.TotalLoreCollected >= 12) Achievements.TryUnlock("sec_lore_complete");
            };

            Debug.Log("[ForestSystemsContainer] All systems initialized successfully.");
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
