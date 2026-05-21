using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Extracted from ForestQuestApp — manages all puzzle gameplay state.
    ///
    /// Responsibilities:
    ///   - Maintain per-level attempt state (mistakes, hints, timer)
    ///   - Build puzzle UI for the active level
    ///   - Route to correct puzzle MonoBehaviour by gameplayMode
    ///   - Call CompleteLevel() / FailLevel()
    ///   - Report to CognitiveAnalyticsSystem after each attempt
    ///   - Feed quest objective progression
    ///   - Notify World/Biome/Exploration on level start
    /// </summary>
    public class PuzzleGameController : PanelViewController
    {
        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<LevelData, int>  OnLevelCompleted;  // (level, stars)
        public event Action<LevelData>       OnLevelFailed;
        public event Action<LevelData>       OnHintRequested;

        // ─── Dependencies ────────────────────────────────────────────────────────

        private ForestUIRouter       _router;
        private ForestAudioController _audio;

        // ─── Active Level State ───────────────────────────────────────────────────

        private LevelData  _activeLevel;
        private string     _activeLevelId;
        private int        _mistakes;
        private bool       _hintUsed;
        private float      _startTime;
        private bool       _solved;
        private bool       _started;

        private readonly List<string> _memoryInputs = new List<string>();
        private readonly List<string> _pathTrail    = new List<string>();

        private TimeMemoryChallenge _activeTimeMemory;

        // ─── UI References (reused, not destroyed) ────────────────────────────────

        private RectTransform _puzzleCard;
        private Text          _promptLabel;
        private Text          _feedbackLabel;
        private Text          _hintLabel;
        private RectTransform _optionsContainer;
        private Button        _hintButton;
        private ReusableCardPool _optionPool;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Configure(ForestUIRouter router, ForestAudioController audio)
        {
            _router = router;
            _audio  = audio;
        }

        protected override void OnBuild()
        {
            BuildPuzzleLayout();
        }

        protected override void OnRefresh(UIDirtyFlag dirtyFlags)
        {
            if ((dirtyFlags & UIDirtyFlag.Progress) != 0)
                RefreshForActiveLevel();
        }

        protected override void OnShow()
        {
            if (_activeLevel != null)
                BeginLevelAttempt();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Set which level to show. Call before GoTo(LevelActive).</summary>
        public void LoadLevel(LevelData level)
        {
            _activeLevel   = level;
            _activeLevelId = level.id;
            ResetAttemptState();
            MarkDirty(UIDirtyFlag.Progress);
        }

        // ─── Layout Construction (once) ───────────────────────────────────────────

        private void BuildPuzzleLayout()
        {
            // Main puzzle card (never destroyed, reused)
            var cardGo = new GameObject("PuzzleCard");
            cardGo.transform.SetParent(RootTransform, false);
            _puzzleCard = cardGo.AddComponent<RectTransform>();
            _puzzleCard.anchorMin = new Vector2(0.04f, 0.08f);
            _puzzleCard.anchorMax = new Vector2(0.96f, 0.92f);
            _puzzleCard.sizeDelta = Vector2.zero;

            var bg = cardGo.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.16f, 0.12f, 0.95f);

            // Prompt label
            var promptGo = new GameObject("Prompt");
            promptGo.transform.SetParent(_puzzleCard, false);
            var promptRt = promptGo.AddComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0.04f, 0.72f);
            promptRt.anchorMax = new Vector2(0.96f, 0.92f);
            promptRt.sizeDelta = Vector2.zero;
            _promptLabel            = promptGo.AddComponent<Text>();
            _promptLabel.font       = ForestUiFactory.GetDefaultFont();
            _promptLabel.fontSize   = 28;
            _promptLabel.color      = new Color(0.95f, 0.95f, 0.85f);
            _promptLabel.alignment  = TextAnchor.MiddleCenter;
            _promptLabel.fontStyle  = FontStyle.Bold;

            // Options container (holds choice buttons / puzzle interactive area)
            var optGo = new GameObject("OptionsContainer");
            optGo.transform.SetParent(_puzzleCard, false);
            _optionsContainer = optGo.AddComponent<RectTransform>();
            _optionsContainer.anchorMin = new Vector2(0.04f, 0.28f);
            _optionsContainer.anchorMax = new Vector2(0.96f, 0.70f);
            _optionsContainer.sizeDelta = Vector2.zero;

            var optLayout = optGo.AddComponent<VerticalLayoutGroup>();
            optLayout.spacing            = 12f;
            optLayout.childForceExpandWidth  = true;
            optLayout.childForceExpandHeight = false;
            optGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Feedback label
            var feedGo = new GameObject("Feedback");
            feedGo.transform.SetParent(_puzzleCard, false);
            var feedRt = feedGo.AddComponent<RectTransform>();
            feedRt.anchorMin = new Vector2(0.04f, 0.14f);
            feedRt.anchorMax = new Vector2(0.96f, 0.28f);
            feedRt.sizeDelta = Vector2.zero;
            _feedbackLabel          = feedGo.AddComponent<Text>();
            _feedbackLabel.font     = ForestUiFactory.GetDefaultFont();
            _feedbackLabel.fontSize = 22;
            _feedbackLabel.color    = new Color(0.75f, 0.95f, 0.75f);
            _feedbackLabel.alignment = TextAnchor.MiddleCenter;

            // Hint button
            var hintGo = new GameObject("HintButton");
            hintGo.transform.SetParent(_puzzleCard, false);
            var hintRt = hintGo.AddComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.04f, 0.04f);
            hintRt.anchorMax = new Vector2(0.96f, 0.13f);
            hintRt.sizeDelta = Vector2.zero;
            var hintImg     = hintGo.AddComponent<Image>();
            hintImg.color   = new Color(0.9f, 0.75f, 0.35f, 0.85f);
            _hintButton     = hintGo.AddComponent<Button>();
            _hintButton.onClick.AddListener(HandleHintTap);
            _hintButton.targetGraphic = hintImg;

            var hintTextGo = new GameObject("HintText");
            hintTextGo.transform.SetParent(hintGo.transform, false);
            var hintTextRt = hintTextGo.AddComponent<RectTransform>();
            hintTextRt.anchorMin = Vector2.zero;
            hintTextRt.anchorMax = Vector2.one;
            hintTextRt.sizeDelta = Vector2.zero;
            _hintLabel          = hintTextGo.AddComponent<Text>();
            _hintLabel.font     = ForestUiFactory.GetDefaultFont();
            _hintLabel.fontSize = 20;
            _hintLabel.color    = new Color(0.2f, 0.1f, 0.05f);
            _hintLabel.alignment = TextAnchor.MiddleCenter;
            _hintLabel.text     = "Ask for a hint";
        }

        // ─── Level Start ──────────────────────────────────────────────────────────

        private void BeginLevelAttempt()
        {
            if (_activeLevel == null || _started) return;

            _started   = true;
            _startTime = Time.time;

            // Notify systems
            var gm = _activeLevel.gameplayMode ?? "";
            Systems?.World?.OnLevelCleared(GetCompletedCount());
            Systems?.Biome?.EnterBiome(_activeLevel.zoneId);
            Systems?.Exploration?.RecordZoneVisit(_activeLevel.zoneId);

            // PuzzleManager analytics start
            Systems?.PuzzleManager?.BeginLevel(_activeLevel.id, GetPuzzleType(gm));

            RefreshForActiveLevel();

            if (gm == "timememory") _activeTimeMemory?.StartChallenge();
        }

        private void RefreshForActiveLevel()
        {
            if (_activeLevel == null) return;

            _promptLabel.text = _activeLevel.prompt ?? "";
            _feedbackLabel.text = "";

            // Clear options container children but via pool (not Destroy)
            ClearOptionsContainer();
            BuildOptionsForLevel(_activeLevel);
        }

        private void ClearOptionsContainer()
        {
            // Disable children rather than destroy
            foreach (Transform child in _optionsContainer)
                child.gameObject.SetActive(false);
        }

        private void BuildOptionsForLevel(LevelData level)
        {
            var gm = level.gameplayMode ?? "choice";

            switch (gm)
            {
                case "choice":           BuildChoicePuzzle(level);              break;
                case "memory":           BuildMemoryPuzzle(level);              break;
                case "path":             BuildManagedForestRouting(level);      break;
                case "lightreflection":  BuildManagedLightReflection(level);    break;
                case "pressuregate":     BuildManagedPressureGate(level);       break;
                case "rotatingpath":     BuildManagedRotatingPath(level);       break;
                case "timememory":       BuildManagedTimeMemory(level);         break;
                case "runesequence":     BuildManagedRuneSequence(level);       break;
                case "musicpattern":     BuildManagedMusicPattern(level);       break;
                case "symbolcipher":     BuildManagedSymbolCipher(level);       break;
                case "shadowmatch":      BuildManagedShadowMatch(level);        break;
                case "pollensort":       BuildManagedPollenSort(level);         break;
                case "starconstellation":BuildManagedStarConstellation(level);  break;
                case "bridgebuilder":    BuildManagedBridgeBuilder(level);      break;
                default:                 BuildChoicePuzzle(level);              break;
            }
        }

        // ─── Puzzle Builders ──────────────────────────────────────────────────────

        private void BuildChoicePuzzle(LevelData level)
        {
            if (level.options == null) return;

            foreach (var option in level.options)
            {
                var opt = option; // capture
                var btnGo = new GameObject($"Option_{opt.id}");
                btnGo.transform.SetParent(_optionsContainer, false);

                var rt = btnGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0f, 68f);

                var bg  = btnGo.AddComponent<Image>();
                bg.color = new Color(0.15f, 0.35f, 0.25f, 0.9f);

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => HandleChoiceSelected(opt));

                var lbl = ForestUiFactory.CreateText(
                    rt, $"Label_{opt.id}", opt.label,
                    ForestUiFactory.GetDefaultFont(), 26,
                    new Color(0.95f, 0.95f, 0.85f), TextAnchor.MiddleCenter);
                ForestUiFactory.AddLayout(lbl.gameObject, preferredHeight: 68f);
            }
        }

        private void BuildMemoryPuzzle(LevelData level)
        {
            _memoryInputs.Clear();

            if (level.memorySequence == null || level.memorySequence.Length == 0) return;

            var adapted = Systems?.Difficulty?.GetAdaptedMemoryLength(
                level.memorySequence.Length, SaveData?.explorerTier ?? "scout")
                ?? level.memorySequence.Length;

            // Start encoding phase via component
            var memComp = _optionsContainer.gameObject.GetComponent<MemoryTrailPuzzle>()
                ?? _optionsContainer.gameObject.AddComponent<MemoryTrailPuzzle>();
            memComp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedForestRouting(LevelData level)
        {
            _pathTrail.Clear();
            var comp = _optionsContainer.gameObject.AddComponent<ForestRoutingPuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedRuneSequence(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<RuneSequencePuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedMusicPattern(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<MusicPatternPuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedSymbolCipher(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<SymbolCipherPuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedShadowMatch(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<ShadowMatchPuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedPollenSort(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<PollenSortPuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedStarConstellation(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<StarConstellationPuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedBridgeBuilder(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<BridgeBuilderPuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedLightReflection(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<LightReflectionPuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedPressureGate(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<PressureGatePuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedRotatingPath(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<RotatingPathPuzzle>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
        }

        private void BuildManagedTimeMemory(LevelData level)
        {
            var comp = _optionsContainer.gameObject.AddComponent<TimeMemoryChallenge>();
            comp.Initialize(Systems?.PuzzleManager, Systems?.Particles, _optionsContainer,
                SaveData?.explorerTier ?? "scout");
            comp.OnPuzzleEnd += success => HandlePuzzleEnd(success);
            _activeTimeMemory = comp;
        }

        // ─── Event Handlers ───────────────────────────────────────────────────────

        private void HandleChoiceSelected(LevelOptionData option)
        {
            if (_solved) return;

            if (option.isCorrect)
            {
                HandlePuzzleEnd(true);
            }
            else
            {
                _mistakes++;
                _feedbackLabel.text = option.reply ?? "Not quite — try again.";
                _feedbackLabel.color = new Color(1f, 0.65f, 0.4f);
                _audio?.PlayWrong(_activeLevel?.characterId);
                Systems?.Difficulty?.NotifyMistake();
            }
        }

        private void HandlePuzzleEnd(bool success)
        {
            if (_solved) return;
            _solved = true;

            var elapsed = Time.time - _startTime;
            var stars   = CalculateStars(success, _mistakes, _hintUsed, elapsed);

            // Report to analytics — FIXED: this was missing before
            Systems?.Analytics?.RecordPuzzleAttempt(
                puzzleType:   _activeLevel?.gameplayMode ?? "choice",
                success:      success,
                mistakes:     _mistakes,
                hintUsed:     _hintUsed,
                timeSeconds:  elapsed
            );

            if (success)
            {
                CompleteLevel(stars);
            }
            else
            {
                FailLevel();
            }
        }

        private void HandleHintTap()
        {
            if (_hintUsed || _activeLevel == null) return;
            _hintUsed = true;
            _hintLabel.text = _activeLevel.hint ?? "Trust the forest. Look for patterns.";
            OnHintRequested?.Invoke(_activeLevel);
            Systems?.Analytics?.RecordHintUsed(_activeLevel.id);
        }

        // ─── Completion ───────────────────────────────────────────────────────────

        private void CompleteLevel(int stars)
        {
            if (_activeLevel == null) return;

            _feedbackLabel.text  = _activeLevel.celebration ?? "Well done!";
            _feedbackLabel.color = new Color(0.6f, 1f, 0.7f);

            // Quest objectives
            ProgressQuestObjective(_activeLevel.gameplayMode);

            // Bond reward
            if (!string.IsNullOrEmpty(_activeLevel.characterId))
                Systems?.BondingEngine?.AddTrust(_activeLevel.characterId, 15 + stars * 5);

            // VFX
            Systems?.VFX?.OnDiscovery(Vector2.zero);

            // Notify boss system
            Systems?.World?.OnLevelCleared(GetCompletedCount() + 1);

            OnLevelCompleted?.Invoke(_activeLevel, stars);
            _activeTimeMemory = null;
        }

        private void FailLevel()
        {
            ResetAttemptState();
            OnLevelFailed?.Invoke(_activeLevel);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void ResetAttemptState()
        {
            _mistakes         = 0;
            _hintUsed         = false;
            _solved           = false;
            _started          = false;
            _activeTimeMemory = null;
            _memoryInputs.Clear();
            _pathTrail.Clear();
        }

        private int CalculateStars(bool success, int mistakes, bool hintUsed, float time)
        {
            if (!success) return 0;
            var stars = 3;
            if (mistakes > 0)  stars--;
            if (hintUsed)      stars--;
            if (time > 60f)    stars = Mathf.Max(stars - 1, 1);
            return Mathf.Max(stars, 1);
        }

        private int GetCompletedCount()
        {
            // Pull from save data
            var count = 0;
            if (SaveData?.levelProgress == null) return 0;
            foreach (var p in SaveData.levelProgress)
                if (p.completed) count++;
            return count;
        }

        private PuzzleType GetPuzzleType(string gameplayMode)
        {
            return gameplayMode switch
            {
                "memory"           => PuzzleType.MemoryTrail,
                "path"             => PuzzleType.ForestRouting,
                "choice"           => PuzzleType.LogicMirror,
                "pressuregate"     => PuzzleType.PressureGate,
                "lightreflection"  => PuzzleType.LightReflection,
                "rotatingpath"     => PuzzleType.RotatingPath,
                "timememory"       => PuzzleType.TimeMemory,
                "runesequence"     => PuzzleType.RuneSequence,
                "musicpattern"     => PuzzleType.MusicPattern,
                "symbolcipher"     => PuzzleType.SymbolCipher,
                "shadowmatch"      => PuzzleType.ShadowMatch,
                "pollensort"       => PuzzleType.PollenSort,
                "starconstellation"=> PuzzleType.StarConstellation,
                "bridgebuilder"    => PuzzleType.BridgeBuilder,
                _                  => PuzzleType.LogicMirror
            };
        }

        private void ProgressQuestObjective(string gameplayMode)
        {
            if (Systems?.Quests == null) return;
            switch (gameplayMode)
            {
                case "memory":
                    Systems.Quests.ProgressObjective("memory_trail_complete");    break;
                case "timememory":
                    Systems.Quests.ProgressObjective("time_memory_complete");     break;
                case "path":
                    Systems.Quests.ProgressObjective("forest_route_completed");   break;
                case "rotatingpath":
                    Systems.Quests.ProgressObjective("rotating_path_solved");     break;
                case "pressuregate":
                    Systems.Quests.ProgressObjective("pressure_gate_solved");     break;
                case "lightreflection":
                    Systems.Quests.ProgressObjective("light_path_completed");     break;
                case "runesequence":
                    Systems.Quests.ProgressObjective("rune_puzzle_solved");       break;
                case "musicpattern":
                    Systems.Quests.ProgressObjective("music_pattern_complete");   break;
                case "symbolcipher":
                    Systems.Quests.ProgressObjective("symbol_cipher_decoded");    break;
                case "shadowmatch":
                    Systems.Quests.ProgressObjective("shadow_matched");           break;
                case "pollensort":
                    Systems.Quests.ProgressObjective("pollen_sorted");            break;
                case "starconstellation":
                    Systems.Quests.ProgressObjective("constellation_traced");     break;
                case "bridgebuilder":
                    Systems.Quests.ProgressObjective("bridge_built");             break;
                default:
                    Systems.Quests.ProgressObjective("mirror_puzzle_solved");     break;
            }
        }
    }
}
