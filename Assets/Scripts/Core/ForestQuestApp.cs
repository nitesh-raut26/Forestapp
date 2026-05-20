using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    public class ForestQuestApp : MonoBehaviour
    {
        private const string SaveKey = "ForestFriendsQuest.Save";

        private readonly HashSet<string> _completedLevelIds = new HashSet<string>();
        private readonly Dictionary<string, LevelProgressData> _levelProgressById = new Dictionary<string, LevelProgressData>();
        private readonly List<string> _memoryInputs = new List<string>();
        private readonly List<string> _pathTrail = new List<string>();

        private readonly Color _forestDeep = new Color32(20, 54, 41, 255);
        private readonly Color _forest = new Color32(33, 81, 62, 255);
        private readonly Color _twilight = new Color32(33, 72, 56, 255);
        private readonly Color _moss = new Color32(47, 122, 86, 255);
        private readonly Color _mint = new Color32(159, 216, 168, 255);
        private readonly Color _cream = new Color32(248, 243, 223, 255);
        private readonly Color _amber = new Color32(245, 184, 92, 255);
        private readonly Color _fernLight = new Color32(231, 246, 217, 255);
        private readonly Color _bark = new Color32(106, 74, 53, 255);
        private readonly Color _ink = new Color32(16, 35, 27, 255);

        private ForestGameContent _content;
        private ForestSaveData _saveData;
        private Font _font;
        private ForestAudioController _audioController;
        private RectTransform _scrollContent;
        private RectTransform _modalLayer;

        private string _activeTab = "play";
        private string _selectedZoneId;
        private string _selectedLevelId;
        private string _selectedCharacterId;
        private bool _soundEnabled = true;
        private bool _premiumUnlocked;
        private string _searchQuery = "";
        private string _selectedTypeFilter = "All";
        private string _feedbackMessage;
        private bool _feedbackSuccess;
        private bool _parentGateOpen;
        private bool _parentGateUnlocked;
        private string _parentGateMessage;
        private string _parentQuestion;
        private string[] _parentAnswerChoices = new string[0];
        private string _parentCorrectAnswer;
        private string _activePuzzleLevelId;

        private ForestSystemsContainer _systems;
        private int _currentLevelMistakes;
        private bool _currentLevelHintUsed;
        private bool _currentLevelStarted;
        private bool _currentLevelSolved;

        private bool _riddle1Solved;
        private bool _riddle2Solved;
        private bool _riddle3Solved;
        private bool _logicSwitchActive;

        private void Awake()
        {
            _content = ForestDataLoader.Load();
            _font = ForestUiFactory.GetDefaultFont();
            _audioController = gameObject.AddComponent<ForestAudioController>();

            LoadProgress();
            InitializeState();
            BuildCanvas();
            Rebuild();

            _systems = ForestSystemsContainer.Instance;
        }

        private void LoadProgress()
        {
            _saveData = new ForestSaveData();
            var raw = PlayerPrefs.GetString(SaveKey, string.Empty);

            if (!string.IsNullOrEmpty(raw))
            {
                var loaded = JsonUtility.FromJson<ForestSaveData>(raw);
                if (loaded != null)
                {
                    _saveData = loaded;
                }
            }

            _soundEnabled = _saveData.soundEnabled;
            _premiumUnlocked = _saveData.premiumUnlocked;

            _riddle1Solved = PlayerPrefs.GetInt(SaveKey + ".Riddle1", 0) == 1;
            _riddle2Solved = PlayerPrefs.GetInt(SaveKey + ".Riddle2", 0) == 1;
            _riddle3Solved = PlayerPrefs.GetInt(SaveKey + ".Riddle3", 0) == 1;

            HydrateProgressMaps();
        }

        private void HydrateProgressMaps()
        {
            _completedLevelIds.Clear();
            _levelProgressById.Clear();

            if (_saveData.levelProgress == null)
            {
                return;
            }

            foreach (var progress in _saveData.levelProgress)
            {
                if (progress == null || string.IsNullOrEmpty(progress.levelId))
                {
                    continue;
                }

                _levelProgressById[progress.levelId] = progress;
                if (progress.completed)
                {
                    _completedLevelIds.Add(progress.levelId);
                }
            }
        }

        private void SaveProgress()
        {
            _saveData.premiumUnlocked = _premiumUnlocked;
            _saveData.soundEnabled = _soundEnabled;
            _saveData.lastSelectedZoneId = _selectedZoneId;
            _saveData.lastSelectedLevelId = _selectedLevelId;

            var list = new List<LevelProgressData>();
            foreach (var pair in _levelProgressById)
            {
                list.Add(pair.Value);
            }

            _saveData.levelProgress = list.ToArray();
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_saveData));
            
            PlayerPrefs.SetInt(SaveKey + ".Riddle1", _riddle1Solved ? 1 : 0);
            PlayerPrefs.SetInt(SaveKey + ".Riddle2", _riddle2Solved ? 1 : 0);
            PlayerPrefs.SetInt(SaveKey + ".Riddle3", _riddle3Solved ? 1 : 0);

            PlayerPrefs.Save();
            _systems?.SaveSystem?.Save(_saveData);
        }

        private void InitializeState()
        {
            _selectedZoneId = ResolveStartingZoneId();
            var preferredLevel = FindPreferredLevelForZone(_selectedZoneId) ?? GetFirstLevelForZone(_selectedZoneId);
            _selectedLevelId = preferredLevel != null ? preferredLevel.id : string.Empty;
            _selectedCharacterId = preferredLevel != null ? preferredLevel.characterId : GetFirstCharacterId();
            _feedbackMessage = "Welcome back. Choose a mission and help a forest friend.";
            _feedbackSuccess = false;
            _parentGateMessage = "Solve the grown-up check to manage premium content on this device.";
            _searchQuery = "";
            _selectedTypeFilter = "All";
            ResetLevelState(_selectedLevelId, true);
            SaveProgress();
        }

        private string ResolveStartingZoneId()
        {
            var savedZone = GetZone(_saveData.lastSelectedZoneId);
            if (savedZone != null && IsZoneUnlocked(savedZone))
            {
                return savedZone.id;
            }

            if (_content.zones != null)
            {
                foreach (var zone in _content.zones)
                {
                    if (IsZoneUnlocked(zone))
                    {
                        return zone.id;
                    }
                }

                if (_content.zones.Length > 0)
                {
                    return _content.zones[0].id;
                }
            }

            return string.Empty;
        }

        private void BuildCanvas()
        {
            var canvasRoot = ForestUiFactory.CreateUiObject("ForestCanvas", transform);
            ForestUiFactory.Stretch(canvasRoot);

            var canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            var scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();

            var background = ForestUiFactory.CreateImage(canvasRoot, "Background", _forestDeep);
            ForestUiFactory.Stretch(background.rectTransform);

            var scrollRoot = ForestUiFactory.CreateUiObject("ScrollRoot", background.transform);
            ForestUiFactory.Stretch(scrollRoot);
            ForestUiFactory.CreateScrollView(scrollRoot, out _scrollContent);
            ForestUiFactory.Stretch(scrollRoot);

            _modalLayer = ForestUiFactory.CreateUiObject("ModalLayer", background.transform);
            ForestUiFactory.Stretch(_modalLayer);
        }

        private void Rebuild()
        {
            EnsureSelectionIsValid();

            ForestUiFactory.ClearChildren(_scrollContent);
            ForestUiFactory.ClearChildren(_modalLayer);

            BuildHero();
            BuildTabs();

            switch (_activeTab)
            {
                case "home":
                    BuildWorldTab();
                    break;
                case "parents":
                    BuildParentsTab();
                    break;
                case "sanctuary":
                    BuildSanctuaryTab();
                    break;
                default:
                    BuildPlayTab();
                    break;
            }

            if (_parentGateOpen)
            {
                BuildParentGateModal();
            }
        }

        private void EnsureSelectionIsValid()
        {
            var selectedZone = GetZone(_selectedZoneId);
            if (selectedZone == null || !IsZoneUnlocked(selectedZone))
            {
                _selectedZoneId = ResolveStartingZoneId();
            }

            var selectedLevel = GetLevel(_selectedLevelId);
            if (selectedLevel == null
                || selectedLevel.zoneId != _selectedZoneId
                || !IsLevelUnlocked(selectedLevel))
            {
                var nextLevel = FindPreferredLevelForZone(_selectedZoneId) ?? GetFirstLevelForZone(_selectedZoneId);
                _selectedLevelId = nextLevel != null ? nextLevel.id : string.Empty;
                _selectedCharacterId = nextLevel != null ? nextLevel.characterId : GetFirstCharacterId();
                ResetLevelState(_selectedLevelId, true);
            }
        }

        private void BuildHero()
        {
            var hero = CreatePanel(_scrollContent, "Hero", _forest, 18f, 24);
            ForestUiFactory.AddLayout(hero.gameObject, flexibleWidth: 1f);

            var badgeRow = ForestUiFactory.CreateUiObject("BadgeRow", hero);
            ForestUiFactory.AddHorizontalLayout(badgeRow.gameObject, 10f);
            badgeRow.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            badgeRow.gameObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CreateBadge(badgeRow, _content.summary.ageBand, _cream, _forest);
            CreateBadge(badgeRow, _content.summary.build, _cream, _forest);
            CreateBadge(
                badgeRow,
                _premiumUnlocked ? "Full adventure unlocked" : _content.summary.model,
                _premiumUnlocked ? _mint : _amber,
                _forest
            );

            var soundButton = ForestUiFactory.CreateButton(
                hero,
                "SoundToggle",
                _soundEnabled ? "Sound: On" : "Sound: Off",
                _font,
                _soundEnabled ? _amber : new Color(1f, 1f, 1f, 0.14f),
                _ink,
                () =>
                {
                    _soundEnabled = !_soundEnabled;
                    _saveData.soundEnabled = _soundEnabled;
                    _audioController.PlaySelect(_soundEnabled);
                    SaveProgress();
                    Rebuild();
                },
                22
            );
            ForestUiFactory.AddLayout(soundButton.gameObject, preferredHeight: 72f);

            var eyebrow = ForestUiFactory.CreateText(hero, "Eyebrow", "Saved progress, unlock flow, and hands-on mini games", _font, 22, _mint, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(eyebrow.gameObject, preferredHeight: 34f);

            var title = ForestUiFactory.CreateText(hero, "Title", _content.summary.title, _font, 48, _cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(title.gameObject, preferredHeight: 60f);

            var tagline = ForestUiFactory.CreateText(hero, "Tagline", _content.summary.tagline, _font, 24, _cream, TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(tagline.gameObject, preferredHeight: 92f);

            var messageCard = CreatePanel(hero, "MessageCard", new Color(1f, 1f, 1f, 0.1f), 12f, 18);
            var messageRow = ForestUiFactory.CreateUiObject("MessageRow", messageCard);
            ForestUiFactory.AddHorizontalLayout(messageRow.gameObject, 18f);
            messageRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var guideHolder = ForestUiFactory.CreateUiObject("GuideHolder", messageRow);
            ForestUiFactory.AddLayout(guideHolder.gameObject, preferredWidth: 250f, preferredHeight: 250f);
            var guideView = guideHolder.gameObject.AddComponent<GuideCharacterView>();
            guideView.Build(GetSelectedCharacter(), _font);

            var messageColumn = ForestUiFactory.CreateUiObject("MessageColumn", messageRow);
            ForestUiFactory.AddVerticalLayout(messageColumn.gameObject, 10f);
            ForestUiFactory.AddLayout(messageColumn.gameObject, flexibleWidth: 1f);
            messageColumn.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var speaker = GetSelectedCharacter();
            var speakerLabel = ForestUiFactory.CreateText(
                messageColumn,
                "Speaker",
                $"{speaker.name} says",
                _font,
                22,
                _amber,
                TextAnchor.MiddleLeft,
                FontStyle.Bold
            );
            ForestUiFactory.AddLayout(speakerLabel.gameObject, preferredHeight: 34f);

            var feedback = ForestUiFactory.CreateText(messageColumn, "Feedback", _feedbackMessage, _font, 24, _cream, TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(feedback.gameObject, minHeight: 96f, flexibleWidth: 1f);

            var statRow = ForestUiFactory.CreateUiObject("Stats", hero);
            ForestUiFactory.AddHorizontalLayout(statRow.gameObject, 16f);
            ForestUiFactory.AddLayout(statRow.gameObject, preferredHeight: 164f);

            CreateStatCard(statRow, "Session", _content.summary.sessionLength, "Short, cheerful levels built for repeat play.");
            CreateStatCard(statRow, "Progress", $"{_completedLevelIds.Count}/{GetTotalLevelCount()} cleared", "Stars, unlocks, and rewards save on this device.");
            CreateStatCard(statRow, "Adventure", _premiumUnlocked ? "Full adventure" : "Free preview", _premiumUnlocked ? "Premium missions can unlock as you progress." : "Parent gate protects premium content.");
        }

        private void BuildTabs()
        {
            var row = ForestUiFactory.CreateUiObject("Tabs", _scrollContent);
            ForestUiFactory.AddHorizontalLayout(row.gameObject, 12f);
            row.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var tab in _content.navigationTabs)
            {
                var active = tab.id == _activeTab;
                var button = ForestUiFactory.CreateButton(
                    row,
                    $"Tab-{tab.id}",
                    tab.label,
                    _font,
                    active ? _cream : new Color(1f, 1f, 1f, 0.08f),
                    active ? _ink : _cream,
                    () =>
                    {
                        _activeTab = tab.id;
                        _audioController.PlaySelect(_soundEnabled);
                        Rebuild();
                    },
                    24
                );
                ForestUiFactory.AddLayout(button.gameObject, preferredHeight: 78f, flexibleWidth: 1f);
            }
        }

        private void BuildWorldTab()
        {
            var zonesBody = CreateSection(_scrollContent, "World design", "Forest zones", false);
            foreach (var zone in _content.zones)
            {
                var unlocked = IsZoneUnlocked(zone);
                var selected = zone.id == _selectedZoneId;
                var zoneCard = CreatePanel(
                    zonesBody,
                    $"Zone-{zone.id}",
                    !unlocked ? new Color(0.86f, 0.86f, 0.82f, 1f) : selected ? _fernLight : _cream,
                    8f,
                    18
                );
                var image = zoneCard.GetComponent<Image>();
                MakeClickable(image, () => HandleZoneTap(zone.id));

                CreateAccentBar(zoneCard, zone.accentHex);
                CreateCardTitle(zoneCard, zone.title, _ink, 28);
                CreateBodyText(zoneCard, zone.mood, _forest, 22);
                CreateBodyText(zoneCard, $"Puzzle focus: {zone.challenge}", _bark, 20);
                CreateBodyText(zoneCard, $"Reward path: {zone.reward}", _bark, 20);
                CreateBodyText(zoneCard, GetZoneStatusText(zone), unlocked ? _moss : _bark, 20);
            }

            var charactersBody = CreateSection(_scrollContent, "Hero cast", "Guide friends", false);
            foreach (var character in _content.characters)
            {
                var selected = character.id == _selectedCharacterId;
                var background = selected
                    ? ForestUiFactory.FromHex(character.accentHex, _amber)
                    : _cream;
                var card = CreatePanel(charactersBody, $"Character-{character.id}", background, 10f, 18);

                int bond = GetCharacterBond(character.id);
                CreateCardTitle(card, $"{character.name} · {character.role} (Bond lvl {bond})", _ink, 26);
                CreateBodyText(card, character.blurb, _forest, 20);

                var actionRow = ForestUiFactory.CreateUiObject("Actions", card);
                ForestUiFactory.AddHorizontalLayout(actionRow.gameObject, 10f);
                actionRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                CreateSmallActionButton(actionRow, "Hello", () => TriggerCharacterCue(character, "greeting"));
                CreateSmallActionButton(actionRow, "Hint", () => TriggerCharacterCue(character, "hint"));
                CreateSmallActionButton(actionRow, "Cheer", () => TriggerCharacterCue(character, "cheer"));

                // High-Retention Scout Feed Interaction
                var feedBtnLabel = $"Feed Treat ({_saveData.forestTreats} left)";
                var canFeed = _saveData.forestTreats > 0;
                var feedBtnColor = canFeed ? _amber : new Color(0.8f, 0.8f, 0.8f, 0.3f);
                var feedBtn = ForestUiFactory.CreateButton(actionRow, $"Feed-{character.id}", feedBtnLabel, _font, feedBtnColor, _ink, () => FeedCharacterTreat(character), 18);
                feedBtn.interactable = canFeed;
                ForestUiFactory.AddLayout(feedBtn.gameObject, preferredHeight: 48f, preferredWidth: 160f);
            }

            // Ancient Lore Riddle Decryption Book Section
            var loreBody = CreateSection(_scrollContent, "Ancient Lore & Decryption", "Druid Riddle Book", false);
            var runesCount = GetRunesDiscoveredCount();
            CreateCardTitle(loreBody, $"Ancient Runes Discovered: {runesCount}", _moss, 22);
            CreateBodyText(loreBody, "High-tier Arch-Druids discover glowing runes by completing River Bend missions. Decrypt them below to reveal the origins of the forest!", _forest, 20);

            // Riddle 1 card
            var r1Card = CreatePanel(loreBody, "Riddle1Card", new Color(1f, 1f, 1f, 0.08f), 6f, 16);
            CreateCardTitle(r1Card, "Riddle I · Twilight Spark", _cream, 22);
            if (runesCount < 1)
            {
                CreateBodyText(r1Card, "Locked. Discover at least 1 Rune to decrypt.", _bark, 18);
            }
            else if (_riddle1Solved)
            {
                CreateBodyText(r1Card, "DECRYPTED: 'I dance in the dark but have no feet. I glow with fire but have no heat.'\nAnswer: Firefly.\n\nPip's Log: Fireflies are the protectors of Mossy River bend, whispering light secrets to travelers.", _mint, 18);
            }
            else
            {
                CreateBodyText(r1Card, "'I dance in the dark but have no feet. I glow with fire but have no heat. What am I?'", _amber, 20);
                var row = ForestUiFactory.CreateUiObject("R1Row", r1Card);
                ForestUiFactory.AddHorizontalLayout(row.gameObject, 10f);
                row.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                CreateRiddleButton(row, "Campfire", 1, false);
                CreateRiddleButton(row, "Firefly", 1, true);
                CreateRiddleButton(row, "Lantern", 1, false);
            }

            // Riddle 2 card
            var r2Card = CreatePanel(loreBody, "Riddle2Card", new Color(1f, 1f, 1f, 0.08f), 6f, 16);
            CreateCardTitle(r2Card, "Riddle II · Green Canopy", _cream, 22);
            if (runesCount < 2)
            {
                CreateBodyText(r2Card, "Locked. Discover at least 2 Runes to decrypt.", _bark, 18);
            }
            else if (_riddle2Solved)
            {
                CreateBodyText(r2Card, "DECRYPTED: 'I have a green hood but I am no thief. I stay under trees but I am not a leaf.'\nAnswer: Mushroom.\n\nMimi's Diary: The elderwood grows ancient mushroom caps that speak in rhythmic hums to birds.", _mint, 18);
            }
            else
            {
                CreateBodyText(r2Card, "'I have a green hood but I am no thief. I stay under trees but I am not a leaf. What am I?'", _amber, 20);
                var row = ForestUiFactory.CreateUiObject("R2Row", r2Card);
                ForestUiFactory.AddHorizontalLayout(row.gameObject, 10f);
                row.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                CreateRiddleButton(row, "Mushroom", 2, true);
                CreateRiddleButton(row, "Tomo", 2, false);
                CreateRiddleButton(row, "Fern", 2, false);
            }

            // Riddle 3 card
            var r3Card = CreatePanel(loreBody, "Riddle3Card", new Color(1f, 1f, 1f, 0.08f), 6f, 16);
            CreateCardTitle(r3Card, "Riddle III · Singing Shimmer", _cream, 22);
            if (runesCount < 3)
            {
                CreateBodyText(r3Card, "Locked. Discover at least 3 Runes to decrypt.", _bark, 18);
            }
            else if (_riddle3Solved)
            {
                CreateBodyText(r3Card, "DECRYPTED: 'I run all day but never walk, I have a mouth but never talk.'\nAnswer: River.\n\nTomo's Journal: Mossy River Bend has run since the first seeds took root, carved by pure crystals.", _mint, 18);
            }
            else
            {
                CreateBodyText(r3Card, "'I run all day but never walk, I have a mouth but never talk. What am I?'", _amber, 20);
                var row = ForestUiFactory.CreateUiObject("R3Row", r3Card);
                ForestUiFactory.AddHorizontalLayout(row.gameObject, 10f);
                row.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                CreateRiddleButton(row, "River", 3, true);
                CreateRiddleButton(row, "Wind", 3, false);
                CreateRiddleButton(row, "Sap", 3, false);
            }

            var soundBody = CreateSection(_scrollContent, "Sound direction", "Character and app sound plan", false);
            foreach (var item in _content.soundDesignPlan)
            {
                CreateBulletRow(soundBody, item, _forest, _moss);
            }
        }

        private void SetExplorerTier(string tier)
        {
            _saveData.explorerTier = tier;
            _audioController.PlaySelect(_soundEnabled);
            SaveProgress();
            ResetLevelState(_selectedLevelId, true);
            Rebuild();
        }

        private void BuildPlayTab()
        {
            if (_systems?.DailyRitual != null)
            {
                var ritual = _systems.DailyRitual.GetTodaysRitual();
                if (ritual != null)
                {
                    var ritualBody = CreateSection(_scrollContent, "Today's Forest Ritual", "Daily magical event", false);
                    CreateCardTitle(ritualBody, ritual.title, _amber, 24);
                    CreateBodyText(ritualBody, ritual.description, _cream, 20);
                    CreateBodyText(ritualBody, $"Reward: {ritual.rewardDescription}", _mint, 18);

                    if (_systems.DailyRitual.IsTodaysRitualComplete())
                    {
                        CreateBodyText(ritualBody, "Completed today! Come back tomorrow for a new ritual.", _mint, 20);
                    }
                    else
                    {
                        var ritualBtn = ForestUiFactory.CreateButton(ritualBody, "CompleteRitual", "Complete Ritual", _font, _amber, _ink, () =>
                        {
                            _systems.DailyRitual.CompleteRitual(_saveData);
                            _systems?.Achievements?.TryUnlock("sea_daily_7", _saveData);
                            _systems?.Achievements?.TryUnlock("sea_daily_30", _saveData);
                            _systems?.VFX?.OnRareReward(Vector2.zero);
                            _feedbackMessage = $"Daily ritual complete! Forest treats added!";
                            _feedbackSuccess = true;
                            SaveProgress();
                            Rebuild();
                        }, 20);
                        ForestUiFactory.AddLayout(ritualBtn.gameObject, preferredHeight: 72f);
                    }
                }
            }

            var tierBody = CreateSection(_scrollContent, "Explorer Tier Settings", "Tailor the adventure to your age", false);
            var tierButtonRow = ForestUiFactory.CreateUiObject("TierButtons", tierBody);
            ForestUiFactory.AddHorizontalLayout(tierButtonRow.gameObject, 10f);
            tierButtonRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var currentTier = _saveData.explorerTier ?? "scout";

            // Sprout Tier Button
            var sproutActive = currentTier == "sprout";
            var sproutBtn = ForestUiFactory.CreateButton(
                tierButtonRow,
                "TierBtn-Sprout",
                "Sprout\n(Ages 4-6)",
                _font,
                sproutActive ? _mint : new Color(1f, 1f, 1f, 0.08f),
                sproutActive ? _ink : _cream,
                () => SetExplorerTier("sprout"),
                18
            );
            ForestUiFactory.AddLayout(sproutBtn.gameObject, preferredHeight: 84f, flexibleWidth: 1f);

            // Scout Tier Button
            var scoutActive = currentTier == "scout";
            var scoutBtn = ForestUiFactory.CreateButton(
                tierButtonRow,
                "TierBtn-Scout",
                "Scout\n(Ages 7-11)",
                _font,
                scoutActive ? _amber : new Color(1f, 1f, 1f, 0.08f),
                scoutActive ? _ink : _cream,
                () => SetExplorerTier("scout"),
                18
            );
            ForestUiFactory.AddLayout(scoutBtn.gameObject, preferredHeight: 84f, flexibleWidth: 1f);

            // Arch-Druid Tier Button
            var druidActive = currentTier == "druid";
            var druidBtn = ForestUiFactory.CreateButton(
                tierButtonRow,
                "TierBtn-Druid",
                "Druid\n(Ages 12-16)",
                _font,
                druidActive ? _moss : new Color(1f, 1f, 1f, 0.08f),
                druidActive ? _cream : _cream,
                () => SetExplorerTier("druid"),
                18
            );
            ForestUiFactory.AddLayout(druidBtn.gameObject, preferredHeight: 84f, flexibleWidth: 1f);

            // High-Retention Daily Druid Trials
            if (currentTier == "druid")
            {
                var dailyBody = CreateSection(_scrollContent, "Daily Druid Cryptographic Trial", "High-tier Daily Rituals", false);
                var dailyCard = CreatePanel(dailyBody, "DailyCard", _twilight, 8f, 18);
                
                CreateCardTitle(dailyCard, "Daily Ritual: The Ancient Shifting Cipher", _cream, 24);
                CreateBodyText(dailyCard, _saveData.dailyTrialCleared 
                    ? "Completed! You have synchronized the forest rift for today. Come back tomorrow!" 
                    : "Complete today's Cryptographic sequence challenge to harvest a massive cache of raw alchemical ingredients!", _mint, 18);

                if (!_saveData.dailyTrialCleared)
                {
                    var dailyBtn = ForestUiFactory.CreateButton(dailyCard, "PlayDailyBtn", "Initiate Ritual (5-Step Reverse Memory)", _font, _amber, _ink, () => PlayDailyDruidTrial(), 18);
                    ForestUiFactory.AddLayout(dailyBtn.gameObject, preferredHeight: 68f);
                }
            }

            var zoneBody = CreateSection(_scrollContent, "Quest flow", "Choose a forest zone", false);
            var zoneChipRow = ForestUiFactory.CreateUiObject("ZoneChips", zoneBody);
            ForestUiFactory.AddHorizontalLayout(zoneChipRow.gameObject, 10f);
            zoneChipRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var zone in _content.zones)
            {
                var selected = zone.id == _selectedZoneId;
                var unlocked = IsZoneUnlocked(zone);
                var label = unlocked ? zone.title : zone.isPremium && !_premiumUnlocked ? $"{zone.title} · Premium" : $"{zone.title} · Locked";
                var background = !unlocked
                    ? new Color(0.78f, 0.78f, 0.74f, 1f)
                    : selected ? ForestUiFactory.FromHex(zone.accentHex, _amber) : _fernLight;

                var button = ForestUiFactory.CreateButton(
                    zoneChipRow,
                    $"Chip-{zone.id}",
                    label,
                    _font,
                    background,
                    _ink,
                    () => HandleZoneTap(zone.id),
                    18
                );
                ForestUiFactory.AddLayout(button.gameObject, preferredHeight: 72f, preferredWidth: 250f);
            }

            var currentZone = GetSelectedZone();
            var selectedZoneCard = CreatePanel(zoneBody, "SelectedZone", _fernLight, 8f, 18);
            CreateCardTitle(selectedZoneCard, currentZone.title, _ink, 28);
            CreateBodyText(selectedZoneCard, currentZone.mood, _forest, 22);
            CreateBodyText(selectedZoneCard, $"Reward path: {currentZone.reward}", _bark, 20);
            CreateBodyText(selectedZoneCard, GetZoneStatusText(currentZone), _moss, 20);

            var searchBody = CreateSection(_scrollContent, "Level search", "Find a forest level", true);
            CreateSearchInputField(searchBody);
            CreateFilterChipsRow(searchBody);

            var levelsBody = CreateSection(_scrollContent, "Level trail", "Starter missions", true);
            var filtered = GetFilteredLevels();
            if (filtered.Length == 0)
            {
                var emptyCard = CreatePanel(levelsBody, "EmptySearchLevels", new Color(1f, 1f, 1f, 0.04f), 8f, 18);
                CreateBodyText(emptyCard, "No forest levels found matching that search. Try another query!", _cream, 20);
            }
            else
            {
                foreach (var level in filtered)
                {
                    var active = level.id == _selectedLevelId;
                    var unlocked = IsLevelUnlocked(level);
                    var done = _completedLevelIds.Contains(level.id);
                    var stars = GetBestStars(level.id);
                    var cardColor = !unlocked
                        ? new Color(1f, 1f, 1f, 0.04f)
                        : done ? new Color(0.62f, 0.85f, 0.66f, 0.28f) : new Color(1f, 1f, 1f, active ? 0.18f : 0.08f);

                    var card = CreatePanel(levelsBody, $"Level-{level.id}", cardColor, 8f, 18);
                    MakeClickable(card.GetComponent<Image>(), () => HandleLevelTap(level.id));
                    CreateCardTitle(card, $"{level.id.Replace("level-", "L")} · {level.name}", _cream, 24);
                    CreateBodyText(card, $"{level.type} · {level.reward}", new Color(_cream.r, _cream.g, _cream.b, 0.84f), 20);
                    CreateBodyText(card, GetLevelStatusText(level, unlocked, done, stars, active), _mint, 20);
                }
            }

            var puzzleBody = CreateSection(_scrollContent, "Play now", "Puzzle mission", false);
            BuildPuzzleCard(puzzleBody);

            var rewardsBody = CreateSection(_scrollContent, "Reward basket", "Progress after every win", false);
            var progressRow = ForestUiFactory.CreateUiObject("ProgressRow", rewardsBody);
            ForestUiFactory.AddHorizontalLayout(progressRow.gameObject, 14f);
            progressRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateProgressCard(progressRow, "Cleared", $"{_completedLevelIds.Count}/{GetTotalLevelCount()}", "Every clear pushes the forest further open.");
            CreateProgressCard(progressRow, "Stars", GetTotalStars().ToString(), "Clean finishes and low-hint wins earn better ratings.");

            var nextReward = GetNextReward();
            CreateProgressCard(
                progressRow,
                "Next reward",
                nextReward != null ? nextReward.title : "All starter rewards",
                nextReward != null
                    ? $"Complete {Mathf.Max(0, nextReward.levels - _completedLevelIds.Count)} more mission to earn it."
                    : "The starter reward basket is already full."
            );

            if (_completedLevelIds.Count == 0)
            {
                var empty = CreatePanel(rewardsBody, "EmptyRewards", _cream, 6f, 18);
                CreateCardTitle(empty, "No rewards yet", _ink, 24);
                CreateBodyText(empty, "Solve the first mission to collect your first forest reward.", _forest, 20);
            }
            else
            {
                foreach (var reward in _content.rewards)
                {
                    if (_completedLevelIds.Count < reward.levels)
                    {
                        continue;
                    }

                    var badge = CreatePanel(rewardsBody, $"Reward-{reward.id}", _fernLight, 6f, 18);
                    CreateCardTitle(badge, reward.title, _ink, 24);
                    CreateBodyText(badge, reward.detail, _forest, 20);
                }
            }
        }

        private void BuildParentsTab()
        {
            var dashboard = CreateSection(_scrollContent, "Family dashboard", "Progress summary", false);
            var topRow = ForestUiFactory.CreateUiObject("DashboardRowOne", dashboard);
            ForestUiFactory.AddHorizontalLayout(topRow.gameObject, 14f);
            topRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CreateProgressCard(topRow, "Adventure", $"{_completedLevelIds.Count}/{GetTotalLevelCount()} cleared", "Progress is saved locally on this device.");
            CreateProgressCard(topRow, "Stars", GetTotalStars().ToString(), "Better finishes improve a mission's saved rating.");

            var bottomRow = ForestUiFactory.CreateUiObject("DashboardRowTwo", dashboard);
            ForestUiFactory.AddHorizontalLayout(bottomRow.gameObject, 14f);
            bottomRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CreateProgressCard(bottomRow, "Hints used", _saveData.totalHintsUsed.ToString(), "Guide hints and replays keep the tone gentle for younger players.");
            CreateProgressCard(bottomRow, "Adventure status", _premiumUnlocked ? "Unlocked" : "Free preview", _premiumUnlocked ? "Premium zones are ready as your family progresses." : "Premium content stays behind a parent gate.");

            var zoneProgress = CreateSection(_scrollContent, "Zone progress", "Where your child is exploring", false);
            foreach (var zone in _content.zones)
            {
                var card = CreatePanel(zoneProgress, $"ParentZone-{zone.id}", _cream, 8f, 18);
                CreateAccentBar(card, zone.accentHex);
                CreateCardTitle(card, zone.title, _ink, 24);
                CreateBodyText(card, $"{GetZoneCompletedCount(zone.id)}/{GetZoneTotalCount(zone.id)} missions cleared", _forest, 20);
                CreateBodyText(card, GetZoneStatusText(zone), _moss, 18);
            }

            var highlights = CreateSection(_scrollContent, "Why this works", "Research highlights", false);
            foreach (var item in _content.researchHighlights)
            {
                CreateBulletRow(highlights, item, _forest, _moss);
            }

            var parentNotes = CreateSection(_scrollContent, "Family UX", "Parent-facing promises", false);
            foreach (var item in _content.parentFacingNotes)
            {
                CreateBulletRow(parentNotes, item, _forest, _moss);
            }

            var revenue = CreateSection(_scrollContent, "Revenue", "Parent gate and full adventure", false);
            foreach (var item in _content.monetizationPlan)
            {
                CreateBulletRow(revenue, item, _forest, _moss);
            }

            var previewCard = CreatePanel(revenue, "UnlockPreview", _fernLight, 10f, 18);
            CreateCardTitle(previewCard, "Device unlock flow", _ink, 26);
            CreateBodyText(
                previewCard,
                _premiumUnlocked
                    ? "The full adventure is already unlocked on this device. Premium zones can now open as progress thresholds are met."
                    : "The free preview stays child-safe. A parent check appears before any premium unlock controls.",
                _forest,
                20
            );

            var previewButtons = ForestUiFactory.CreateUiObject("PreviewActions", previewCard);
            ForestUiFactory.AddHorizontalLayout(previewButtons.gameObject, 12f);
            previewButtons.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var parentGate = ForestUiFactory.CreateButton(previewButtons, "OpenParentGate", _premiumUnlocked ? "Manage unlock" : "Open parent gate", _font, _forest, _cream, OpenParentGate, 22);
            ForestUiFactory.AddLayout(parentGate.gameObject, preferredHeight: 72f, preferredWidth: 320f);

            var resetButton = ForestUiFactory.CreateButton(previewButtons, "ResetProgress", "Reset progress", _font, _amber, _ink, ResetProgress, 22);
            ForestUiFactory.AddLayout(resetButton.gameObject, preferredHeight: 72f, preferredWidth: 240f);

            var milestones = CreateSection(_scrollContent, "Delivery path", "Build milestones", false);
            foreach (var item in _content.buildMilestones)
            {
                CreateBulletRow(milestones, item, _forest, _moss);
            }
        }

        private void BuildPuzzleCard(Transform parent)
        {
            var level = GetSelectedLevel();
            var card = CreatePanel(parent, "PuzzleCard", _twilight, 10f, 20);

            if (level == null)
            {
                CreateCardTitle(card, "No mission selected", _cream, 28);
                CreateBodyText(card, "Choose a forest zone to continue the adventure.", _cream, 22);
                return;
            }

            if (!IsLevelUnlocked(level))
            {
                CreateCardTitle(card, "Mission locked", _cream, 30);
                CreateBodyText(card, GetLevelLockMessage(level), _cream, 22);

                var actionRow = ForestUiFactory.CreateUiObject("LockedActions", card);
                ForestUiFactory.AddHorizontalLayout(actionRow.gameObject, 12f);
                actionRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var lockedZone = GetZone(level.zoneId);
                if (lockedZone != null && lockedZone.isPremium && !_premiumUnlocked)
                {
                    CreateSmallActionButton(actionRow, "Open parent gate", OpenParentGate, true);
                }

                var nextMission = FindNextMission(level.id);
                if (nextMission != null)
                {
                    CreateSmallActionButton(actionRow, "Go to next mission", () => SelectLevel(nextMission.id), true);
                }
                return;
            }

            EnsureLevelState(level.id);

            CreateCardTitle(card, $"{level.type} · {level.difficulty}", _mint, 20);
            CreateCardTitle(card, level.name, _cream, 32);
            CreateBodyText(card, level.prompt, _cream, 24);
            CreateBodyText(card, $"Hint: {level.hint}", new Color(_cream.r, _cream.g, _cream.b, 0.82f), 20);
            CreateBodyText(card, $"Best rating: {GetBestStars(level.id)}/3 stars", _mint, 18);

            var cueRow = ForestUiFactory.CreateUiObject("CueRow", card);
            ForestUiFactory.AddHorizontalLayout(cueRow.gameObject, 10f);
            cueRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CreateSmallActionButton(cueRow, "Guide hint", () => TriggerCharacterCue(GetCharacter(level.characterId), "hint"), true);
            CreateSmallActionButton(cueRow, "Guide hello", () => TriggerCharacterCue(GetCharacter(level.characterId), "greeting"), true);

            switch (GetGameplayMode(level))
            {
                case "memory":
                    BuildMemoryPuzzle(card, level);
                    break;
                case "path":
                    BuildPathPuzzle(card, level);
                    break;
                default:
                    BuildChoicePuzzle(card, level);
                    break;
            }

            var feedbackColor = _feedbackSuccess ? new Color(0.62f, 0.85f, 0.66f, 0.24f) : new Color(1f, 1f, 1f, 0.1f);
            var feedbackCard = CreatePanel(card, "Feedback", feedbackColor, 6f, 16);
            CreateBodyText(feedbackCard, _feedbackMessage, _cream, 22);

            var actionRowBottom = ForestUiFactory.CreateUiObject("PuzzleActions", card);
            ForestUiFactory.AddHorizontalLayout(actionRowBottom.gameObject, 12f);
            actionRowBottom.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (_currentLevelSolved)
            {
                var nextMission = FindNextMission(level.id);
                if (nextMission != null)
                {
                    CreateSmallActionButton(actionRowBottom, "Next mission", () => SelectLevel(nextMission.id), true);
                }

                CreateSmallActionButton(actionRowBottom, "Replay mission", ReplaySelectedLevel, true);
            }
            else if (_completedLevelIds.Contains(level.id))
            {
                CreateSmallActionButton(actionRowBottom, "Replay for more stars", ReplaySelectedLevel, true);
            }
        }

        private void BuildChoicePuzzle(Transform parent, LevelData level)
        {
            if (level.options == null)
            {
                return;
            }

            foreach (var option in level.options)
            {
                var button = ForestUiFactory.CreateButton(
                    parent,
                    $"Option-{option.id}",
                    option.label,
                    _font,
                    new Color(1f, 1f, 1f, 0.12f),
                    _cream,
                    () => HandleChoiceOption(level, option),
                    22
                );
                button.interactable = !_currentLevelSolved;
                ForestUiFactory.AddLayout(button.gameObject, preferredHeight: 78f);
            }
        }

        private void BuildMemoryPuzzle(Transform parent, LevelData level)
        {
            var sequenceLength = level.memorySequence != null ? level.memorySequence.Length : 0;
            var currentTier = _saveData.explorerTier ?? "scout";
            if (currentTier == "sprout")
            {
                sequenceLength = Mathf.Min(2, sequenceLength);
            }
            CreateBodyText(parent, $"Pattern to copy: {FormatMemorySequence(level)}", new Color(_cream.r, _cream.g, _cream.b, 0.86f), 20);
            CreateBodyText(parent, $"Progress: {_memoryInputs.Count}/{sequenceLength} lights matched", _mint, 18);

            var helperRow = ForestUiFactory.CreateUiObject("MemoryHelpers", parent);
            ForestUiFactory.AddHorizontalLayout(helperRow.gameObject, 10f);
            helperRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CreateSmallActionButton(helperRow, "Replay pattern", () => ReplayMemoryPattern(level), true);
            CreateSmallActionButton(helperRow, "Clear attempt", () => ResetMemoryAttempt(level), true);

            var gridRoot = ForestUiFactory.CreateUiObject("MemoryGrid", parent);
            ForestUiFactory.AddGridLayout(gridRoot.gameObject, new Vector2(230f, 94f), new Vector2(10f, 10f), 2);
            ForestUiFactory.AddLayout(gridRoot.gameObject, preferredHeight: Mathf.Ceil((level.options != null ? level.options.Length : 0) / 2f) * 104f);

            if (level.options == null)
            {
                return;
            }

            foreach (var option in level.options)
            {
                var nextExpected = false;
                if (!_currentLevelSolved && currentTier == "sprout" && _memoryInputs.Count < sequenceLength)
                {
                    nextExpected = level.memorySequence[_memoryInputs.Count] == option.id;
                }

                var button = ForestUiFactory.CreateButton(
                    gridRoot,
                    $"Memory-{option.id}",
                    option.label,
                    _font,
                    nextExpected ? _amber : new Color(1f, 1f, 1f, 0.12f),
                    nextExpected ? _ink : _cream,
                    () => HandleMemoryInput(level, option.id),
                    20
                );
                button.interactable = !_currentLevelSolved;
            }
        }

        private void BuildPathPuzzle(Transform parent, LevelData level)
        {
            var sequenceLength = level.pathSequence != null ? level.pathSequence.Length : 0;
            CreateBodyText(parent, $"Safe route clue: {FormatPathSequence(level)}", new Color(_cream.r, _cream.g, _cream.b, 0.86f), 20);
            CreateBodyText(parent, $"Route progress: {_pathTrail.Count}/{sequenceLength} safe steps", _mint, 18);

            var helperRow = ForestUiFactory.CreateUiObject("PathHelpers", parent);
            ForestUiFactory.AddHorizontalLayout(helperRow.gameObject, 10f);
            helperRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CreateSmallActionButton(helperRow, "Show route clue", () => ShowPathClue(level), true);
            CreateSmallActionButton(helperRow, "Reset route", () => ResetPathAttempt(level), true);

            var columns = Mathf.Max(1, level.pathColumns);
            var rows = Mathf.CeilToInt((level.pathCells != null ? level.pathCells.Length : 0) / (float)columns);
            var gridRoot = ForestUiFactory.CreateUiObject("PathGrid", parent);
            ForestUiFactory.AddGridLayout(gridRoot.gameObject, new Vector2(165f, 102f), new Vector2(10f, 10f), columns);
            ForestUiFactory.AddLayout(gridRoot.gameObject, preferredHeight: Mathf.Max(1, rows) * 112f);

            if (level.pathCells == null)
            {
                return;
            }

            foreach (var cell in level.pathCells)
            {
                var visited = _pathTrail.Contains(cell.id);
                var nextExpected = !_currentLevelSolved && GetNextPathStepId(level) == cell.id;

                var labelText = cell.label;
                var currentTier = _saveData.explorerTier ?? "scout";

                if (currentTier == "sprout" && nextExpected)
                {
                    labelText = cell.label;
                }
                else if (currentTier == "druid")
                {
                    if (IsSwitchCell(level, cell.id))
                    {
                        labelText = _logicSwitchActive ? "Switch [ACTIVE]" : "Switch [OPEN]";
                    }
                    else if (IsGateCell(level, cell.id))
                    {
                        labelText = _logicSwitchActive ? "Gate [OPEN]" : "Gate [LOCKED]";
                    }
                }

                var btnColor = visited ? _mint : nextExpected ? _amber : new Color(1f, 1f, 1f, 0.12f);
                if (currentTier == "druid")
                {
                    if (IsSwitchCell(level, cell.id))
                    {
                        btnColor = _logicSwitchActive ? _mint : _amber;
                    }
                    else if (IsGateCell(level, cell.id))
                    {
                        btnColor = _logicSwitchActive ? _mint : new Color(1f, 1f, 1f, 0.06f);
                    }
                }

                var button = ForestUiFactory.CreateButton(
                    gridRoot,
                    $"Path-{cell.id}",
                    labelText,
                    _font,
                    btnColor,
                    visited || nextExpected ? _ink : _cream,
                    () => HandlePathStep(level, cell.id),
                    18
                );
                button.interactable = !_currentLevelSolved;
            }
        }

        private void BuildParentGateModal()
        {
            var scrim = ForestUiFactory.CreateImage(_modalLayer, "Scrim", new Color(0f, 0f, 0f, 0.56f));
            ForestUiFactory.Stretch(scrim.rectTransform);
            MakeClickable(scrim, () => { });

            var card = CreatePanel(_modalLayer, "ParentGateCard", _cream, 12f, 24);
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(780f, 820f);
            card.anchoredPosition = Vector2.zero;

            CreateCardTitle(card, "Parent gate", _moss, 20);
            CreateCardTitle(card, "Quick grown-up check", _ink, 34);
            CreateBodyText(card, "Children should not land on premium controls without an adult. Answer the prompt below to continue.", _forest, 22);
            CreateBodyText(card, _parentQuestion, _forest, 26);
            CreateBodyText(card, _parentGateMessage, _forest, 20);

            var answerRow = ForestUiFactory.CreateUiObject("Answers", card);
            ForestUiFactory.AddGridLayout(answerRow.gameObject, new Vector2(210f, 86f), new Vector2(12f, 12f), 2);
            ForestUiFactory.AddLayout(answerRow.gameObject, preferredHeight: 196f);

            foreach (var answer in _parentAnswerChoices)
            {
                CreateAnswerButton(answerRow, answer);
            }

            if (_parentGateUnlocked || _premiumUnlocked)
            {
                var unlockCard = CreatePanel(card, "UnlockCard", _fernLight, 8f, 18);
                CreateCardTitle(unlockCard, _premiumUnlocked ? "Full adventure unlocked" : "Unlock all forest zones", _ink, 28);
                CreateBodyText(
                    unlockCard,
                    _premiumUnlocked
                        ? "Premium content is enabled on this device. River Bend and later missions now unlock as progress thresholds are met."
                        : "Enable the full device unlock to open premium zones once the child reaches their progress gates.",
                    _forest,
                    20
                );

                if (!_premiumUnlocked)
                {
                    var unlockButton = ForestUiFactory.CreateButton(unlockCard, "ActivatePremium", "Unlock full adventure", _font, _forest, _cream, ActivatePremiumUnlock, 22);
                    ForestUiFactory.AddLayout(unlockButton.gameObject, preferredHeight: 74f);
                }
            }

            var actionRow = ForestUiFactory.CreateUiObject("ModalActions", card);
            ForestUiFactory.AddHorizontalLayout(actionRow.gameObject, 12f);
            actionRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var refresh = ForestUiFactory.CreateButton(actionRow, "RefreshQuestion", "New question", _font, _amber, _ink, GenerateParentChallenge, 22);
            ForestUiFactory.AddLayout(refresh.gameObject, preferredHeight: 72f, preferredWidth: 220f);

            var close = ForestUiFactory.CreateButton(actionRow, "CloseParentGate", "Close", _font, _forest, _cream, () =>
            {
                _parentGateOpen = false;
                Rebuild();
            }, 22);
            ForestUiFactory.AddLayout(close.gameObject, preferredHeight: 72f, preferredWidth: 200f);
        }

        private void CreateAnswerButton(Transform parent, string answer)
        {
            var button = ForestUiFactory.CreateButton(parent, $"Answer-{answer}", answer, _font, _amber, _ink, () =>
            {
                if (answer == _parentCorrectAnswer)
                {
                    _parentGateUnlocked = true;
                    _parentGateMessage = _premiumUnlocked
                        ? "Parent gate cleared. The full adventure is already active on this device."
                        : "Parent gate cleared. You can now unlock the full adventure on this device.";
                    _audioController.PlaySuccess(GetSelectedCharacter(), _soundEnabled);
                }
                else
                {
                    _parentGateUnlocked = false;
                    _parentGateMessage = "Not quite. Try the grown-up math one more time.";
                    _audioController.PlayWrong(GetSelectedCharacter(), _soundEnabled);
                }

                Rebuild();
            }, 26);
            ForestUiFactory.AddLayout(button.gameObject, preferredHeight: 86f);
        }

        private RectTransform CreateSection(Transform parent, string eyebrow, string title, bool dark)
        {
            var card = CreatePanel(parent, $"Section-{title}", dark ? _twilight : _cream, 14f, 20);
            CreateCardTitle(card, eyebrow, dark ? _mint : _moss, 18);
            CreateCardTitle(card, title, dark ? _cream : _ink, 34);

            var body = ForestUiFactory.CreateUiObject("Body", card);
            ForestUiFactory.AddVerticalLayout(body.gameObject, 12f);
            body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return body;
        }

        private RectTransform CreatePanel(Transform parent, string name, Color color, float spacing, int padding)
        {
            var panel = ForestUiFactory.CreateImage(parent, name, color);
            ForestUiFactory.AddVerticalLayout(panel.gameObject, spacing, new RectOffset(padding, padding, padding, padding));
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            ForestUiFactory.AddLayout(panel.gameObject, flexibleWidth: 1f);
            return panel.rectTransform;
        }

        private void CreateCardTitle(Transform parent, string text, Color color, int size)
        {
            var label = ForestUiFactory.CreateText(parent, "Title", text, _font, size, color, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(label.gameObject, minHeight: size + 12f, flexibleWidth: 1f);
        }

        private void CreateBodyText(Transform parent, string text, Color color, int size)
        {
            var label = ForestUiFactory.CreateText(parent, "Body", text, _font, size, color, TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(label.gameObject, minHeight: size * 1.5f, flexibleWidth: 1f);
        }

        private void CreateBadge(Transform parent, string text, Color background, Color textColor)
        {
            var badge = CreatePanel(parent, $"Badge-{text}", background, 0f, 14);
            ForestUiFactory.AddLayout(badge.gameObject, preferredHeight: 60f);
            CreateCardTitle(badge, text, textColor, 18);
        }

        private void CreateStatCard(Transform parent, string label, string value, string detail)
        {
            var card = CreatePanel(parent, $"Stat-{label}", new Color(0f, 0f, 0f, 0.16f), 8f, 16);
            ForestUiFactory.AddLayout(card.gameObject, preferredHeight: 160f, flexibleWidth: 1f);
            CreateCardTitle(card, label, _mint, 18);
            CreateCardTitle(card, value, _cream, 24);
            CreateBodyText(card, detail, _cream, 18);
        }

        private void CreateProgressCard(Transform parent, string label, string value, string detail)
        {
            var card = CreatePanel(parent, $"Progress-{label}", _cream, 8f, 16);
            ForestUiFactory.AddLayout(card.gameObject, preferredHeight: 160f, flexibleWidth: 1f);
            CreateCardTitle(card, label, _moss, 18);
            CreateCardTitle(card, value, _ink, 24);
            CreateBodyText(card, detail, _forest, 18);
        }

        private void CreateBulletRow(Transform parent, string text, Color textColor, Color dotColor)
        {
            var row = ForestUiFactory.CreateUiObject("BulletRow", parent);
            ForestUiFactory.AddHorizontalLayout(row.gameObject, 12f);
            row.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var dot = ForestUiFactory.CreateImage(row, "Dot", dotColor, true);
            dot.rectTransform.sizeDelta = new Vector2(14f, 14f);
            ForestUiFactory.AddLayout(dot.gameObject, preferredWidth: 14f, preferredHeight: 14f);

            var label = ForestUiFactory.CreateText(row, "Text", text, _font, 20, textColor, TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(label.gameObject, minHeight: 42f, flexibleWidth: 1f);
        }

        private void CreateSmallActionButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, bool dark = false)
        {
            var button = ForestUiFactory.CreateButton(
                parent,
                $"Action-{label}",
                label,
                _font,
                dark ? _forest : _twilight,
                _cream,
                onClick,
                18
            );
            ForestUiFactory.AddLayout(button.gameObject, preferredHeight: 62f, preferredWidth: 210f);
        }

        private void CreateAccentBar(Transform parent, string hex)
        {
            var accent = ForestUiFactory.CreateImage(parent, "Accent", ForestUiFactory.FromHex(hex, _amber));
            accent.rectTransform.sizeDelta = new Vector2(0f, 12f);
            ForestUiFactory.AddLayout(accent.gameObject, preferredHeight: 12f);
            accent.gameObject.AddComponent<PulseGlow>().speed = 1.8f;
        }

        private void MakeClickable(Image image, UnityEngine.Events.UnityAction onClick)
        {
            var button = image.GetComponent<Button>() ?? image.gameObject.AddComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            var colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = image.color * 1.03f;
            colors.pressedColor = image.color * 0.94f;
            colors.selectedColor = image.color;
            button.colors = colors;
        }

        private void OpenParentGate()
        {
            _parentGateOpen = true;
            _parentGateUnlocked = false;
            _parentGateMessage = _premiumUnlocked
                ? "Solve the grown-up check to review or close premium content controls."
                : "Solve the grown-up check to manage premium content on this device.";
            GenerateParentChallenge();
            _audioController.PlaySelect(_soundEnabled);
        }

        private void GenerateParentChallenge()
        {
            _parentGateUnlocked = false;
            _parentGateMessage = _premiumUnlocked
                ? "Solve the grown-up check to review premium content controls."
                : "Solve the grown-up check to manage premium content on this device.";

            var left = Random.Range(6, 10);
            var right = Random.Range(5, 9);
            var answer = left + right;
            _parentQuestion = $"What is {left} + {right}?";
            _parentCorrectAnswer = answer.ToString();

            var choices = new List<string> { _parentCorrectAnswer };
            AddUniqueParentChoice(choices, answer - 1);
            AddUniqueParentChoice(choices, answer + 2);
            ShuffleStrings(choices);
            _parentAnswerChoices = choices.ToArray();
            Rebuild();
        }

        private void AddUniqueParentChoice(List<string> choices, int value)
        {
            var text = Mathf.Max(0, value).ToString();
            if (!choices.Contains(text))
            {
                choices.Add(text);
            }
        }

        private void ShuffleStrings(List<string> items)
        {
            for (var i = items.Count - 1; i > 0; i--)
            {
                var swapIndex = Random.Range(0, i + 1);
                var current = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = current;
            }
        }

        private void ActivatePremiumUnlock()
        {
            _premiumUnlocked = true;
            _saveData.premiumUnlocked = true;
            _parentGateUnlocked = false;
            _parentGateOpen = false;
            _feedbackMessage = "Full adventure unlocked on this device. Premium zones can now open as the child clears more missions.";
            _feedbackSuccess = true;
            SaveProgress();
            _audioController.PlaySuccess(GetSelectedCharacter(), _soundEnabled);
            Rebuild();
        }

        private void ResetProgress()
        {
            _completedLevelIds.Clear();
            _levelProgressById.Clear();
            _saveData.levelProgress = new LevelProgressData[0];
            _saveData.totalLevelAttempts = 0;
            _saveData.totalHintsUsed = 0;
            _saveData.totalWrongAnswers = 0;

            var nextZone = ResolveStartingZoneId();
            _selectedZoneId = nextZone;
            var nextLevel = FindPreferredLevelForZone(_selectedZoneId) ?? GetFirstLevelForZone(_selectedZoneId);
            _selectedLevelId = nextLevel != null ? nextLevel.id : string.Empty;
            _selectedCharacterId = nextLevel != null ? nextLevel.characterId : _selectedCharacterId;
            ResetLevelState(_selectedLevelId, true);
            _feedbackMessage = "Progress reset. The forest trail is ready for another first adventure.";
            _feedbackSuccess = false;
            SaveProgress();
            _audioController.PlaySelect(_soundEnabled);
            Rebuild();
        }

        private void HandleZoneTap(string zoneId)
        {
            var zone = GetZone(zoneId);
            if (zone == null)
            {
                return;
            }

            if (!IsZoneUnlocked(zone))
            {
                _feedbackMessage = GetZoneLockMessage(zone);
                _feedbackSuccess = false;
                if (zone.isPremium && !_premiumUnlocked)
                {
                    OpenParentGate();
                    return;
                }

                Rebuild();
                return;
            }

            _selectedZoneId = zone.id;
            _systems?.Exploration?.RecordZoneVisit(zone.id);
            _searchQuery = "";
            _selectedTypeFilter = "All";
            var nextLevel = FindPreferredLevelForZone(zone.id) ?? GetFirstLevelForZone(zone.id);
            if (nextLevel != null)
            {
                _selectedLevelId = nextLevel.id;
                _selectedCharacterId = nextLevel.characterId;
                ResetLevelState(nextLevel.id, true);
            }

            _feedbackMessage = $"{zone.title} is ready. {zone.challenge}";
            _feedbackSuccess = false;
            SaveProgress();
            _audioController.PlaySelect(_soundEnabled);
            Rebuild();
        }

        private void HandleLevelTap(string levelId)
        {
            var level = GetLevel(levelId);
            if (level == null)
            {
                return;
            }

            if (!IsLevelUnlocked(level))
            {
                _feedbackMessage = GetLevelLockMessage(level);
                _feedbackSuccess = false;
                var zone = GetZone(level.zoneId);
                if (zone != null && zone.isPremium && !_premiumUnlocked)
                {
                    OpenParentGate();
                    return;
                }

                Rebuild();
                return;
            }

            SelectLevel(level.id);
        }

        private void SelectLevel(string levelId)
        {
            var level = GetLevel(levelId);
            if (level == null)
            {
                return;
            }

            _selectedZoneId = level.zoneId;
            _selectedLevelId = level.id;
            _selectedCharacterId = level.characterId;
            ResetLevelState(level.id, true);
            _feedbackMessage = $"{level.name}: {level.prompt}";
            _feedbackSuccess = false;
            SaveProgress();
            _audioController.PlaySelect(_soundEnabled);
            Rebuild();
        }

        private void ReplaySelectedLevel()
        {
            ResetLevelState(_selectedLevelId, true);
            _feedbackMessage = "Replay ready. Try for a cleaner finish and a better star rating.";
            _feedbackSuccess = false;
            Rebuild();
        }

        private void TriggerCharacterCue(CharacterProfile character, string cueType)
        {
            if (character == null)
            {
                return;
            }

            _selectedCharacterId = character.id;
            _feedbackMessage = GetCharacterLine(character, cueType);
            _feedbackSuccess = cueType == "cheer";

            if (cueType == "hint")
            {
                MarkHintUsed();
            }

            _audioController.PlayCharacterCue(character, cueType, _soundEnabled);
            Rebuild();
        }

        private void HandleChoiceOption(LevelData level, LevelOptionData option)
        {
            if (level == null || option == null || _currentLevelSolved)
            {
                return;
            }

            BeginLevelAttempt(level.id);
            var character = GetCharacter(level.characterId);

            if (option.isCorrect)
            {
                CompleteLevel(level, level.celebration, character);
                return;
            }

            _currentLevelMistakes++;
            _saveData.totalWrongAnswers++;
            _feedbackMessage = option.reply;
            _feedbackSuccess = false;
            SaveProgress();
            _audioController.PlayWrong(character, _soundEnabled);
            Rebuild();
        }

        private void HandleMemoryInput(LevelData level, string optionId)
        {
            if (level == null || _currentLevelSolved || level.memorySequence == null || level.memorySequence.Length == 0)
            {
                return;
            }

            BeginLevelAttempt(level.id);
            var character = GetCharacter(level.characterId);
            var currentTier = _saveData.explorerTier ?? "scout";

            var sequenceLength = level.memorySequence.Length;
            if (currentTier == "sprout")
            {
                sequenceLength = Mathf.Min(2, sequenceLength);
            }

            var expectedId = level.memorySequence[_memoryInputs.Count];
            if (currentTier == "druid")
            {
                expectedId = level.memorySequence[level.memorySequence.Length - 1 - _memoryInputs.Count];
            }

            if (optionId == expectedId)
            {
                _memoryInputs.Add(optionId);
                if (_memoryInputs.Count >= sequenceLength)
                {
                    CompleteLevel(level, level.celebration, character);
                    return;
                }

                _feedbackMessage = $"Nice copy. {_memoryInputs.Count} of {sequenceLength} lights are in the right order.";
                _feedbackSuccess = false;
                _audioController.PlaySelect(_soundEnabled);
                Rebuild();
                return;
            }

            _currentLevelMistakes++;
            _saveData.totalWrongAnswers++;
            _memoryInputs.Clear();
            _feedbackMessage = currentTier == "druid"
                ? "Cryptographic rift out of sync. Remember, you must enter the lights in exact REVERSE order!"
                : "That light was out of order. Replay the pattern and try again.";
            _feedbackSuccess = false;
            SaveProgress();
            _audioController.PlayWrong(character, _soundEnabled);
            Rebuild();
        }

        private void ReplayMemoryPattern(LevelData level)
        {
            if (level == null)
            {
                return;
            }

            MarkHintUsed();
            _feedbackMessage = $"Pattern replay: {FormatMemorySequence(level)}";
            _feedbackSuccess = false;
            _audioController.PlayCharacterCue(GetCharacter(level.characterId), "hint", _soundEnabled);
            Rebuild();
        }

        private void ResetMemoryAttempt(LevelData level)
        {
            if (level == null)
            {
                return;
            }

            _memoryInputs.Clear();
            _feedbackMessage = $"{level.name}: start the light pattern from the beginning.";
            _feedbackSuccess = false;
            Rebuild();
        }

        private void HandlePathStep(LevelData level, string cellId)
        {
            if (level == null || _currentLevelSolved || level.pathSequence == null || level.pathSequence.Length == 0)
            {
                return;
            }

            BeginLevelAttempt(level.id);
            var character = GetCharacter(level.characterId);
            var currentTier = _saveData.explorerTier ?? "scout";

            if (currentTier == "druid" && IsSwitchCell(level, cellId))
            {
                _logicSwitchActive = true;
                _feedbackMessage = "Click! The ancient rune switch is ACTIVE. The path gate is now open.";
                _feedbackSuccess = true;
                _audioController.PlaySelect(_soundEnabled);
                Rebuild();
                return;
            }

            var expectedId = level.pathSequence[_pathTrail.Count];

            if (currentTier == "druid" && IsGateCell(level, cellId) && !_logicSwitchActive)
            {
                _feedbackMessage = "The Forest Gate is locked! Step on the Switch pressure plate first to open it.";
                _feedbackSuccess = false;
                _audioController.PlayWrong(character, _soundEnabled);
                Rebuild();
                return;
            }

            if (cellId == expectedId)
            {
                _pathTrail.Add(cellId);
                if (_pathTrail.Count >= level.pathSequence.Length)
                {
                    CompleteLevel(level, level.celebration, character);
                    return;
                }

                _feedbackMessage = $"Steady steps. {_pathTrail.Count} of {level.pathSequence.Length} safe stones are set.";
                _feedbackSuccess = false;
                _audioController.PlaySelect(_soundEnabled);
                Rebuild();
                return;
            }

            _currentLevelMistakes++;
            _saveData.totalWrongAnswers++;
            _pathTrail.Clear();
            _logicSwitchActive = false;
            _feedbackMessage = "That route splashed into trouble. Start again from the safe first step.";
            _feedbackSuccess = false;
            SaveProgress();
            _audioController.PlayWrong(character, _soundEnabled);
            Rebuild();
        }

        private void ShowPathClue(LevelData level)
        {
            if (level == null)
            {
                return;
            }

            MarkHintUsed();
            _feedbackMessage = $"Route clue: {FormatPathSequence(level)}";
            _feedbackSuccess = false;
            _audioController.PlayCharacterCue(GetCharacter(level.characterId), "hint", _soundEnabled);
            Rebuild();
        }

        private void ResetPathAttempt(LevelData level)
        {
            if (level == null)
            {
                return;
            }

            _pathTrail.Clear();
            _feedbackMessage = $"{level.name}: reset the route and try the stepping path again.";
            _feedbackSuccess = false;
            Rebuild();
        }

        private void BeginLevelAttempt(string levelId)
        {
            if (_currentLevelStarted)
            {
                return;
            }

            _currentLevelStarted = true;
            _saveData.totalLevelAttempts++;
            var progress = GetOrCreateLevelProgress(levelId);
            progress.timesPlayed++;
            SaveProgress();

            var startLevel = GetLevel(levelId);
            if (startLevel != null && _systems?.PuzzleManager != null)
            {
                var gm = string.IsNullOrEmpty(startLevel.gameplayMode) ? "choice" : startLevel.gameplayMode.ToLower();
                var pType = gm == "memory" ? PuzzleType.MemoryTrail
                          : gm == "path"   ? PuzzleType.ForestRouting
                          : PuzzleType.LogicMirror;
                _systems.PuzzleManager.StartPuzzle(pType, _saveData.explorerTier ?? "scout");
            }
        }

        private void MarkHintUsed()
        {
            _currentLevelHintUsed = true;
            _saveData.totalHintsUsed++;
            SaveProgress();
        }

        private void CompleteLevel(LevelData level, string celebration, CharacterProfile character)
        {
            var rewardCountBefore = GetUnlockedRewardCount();
            var wasNewClear = _completedLevelIds.Add(level.id);
            var stars = CalculateStars();
            var progress = GetOrCreateLevelProgress(level.id);
            progress.completed = true;
            progress.bestStars = Mathf.Max(progress.bestStars, stars);
            progress.timesCleared++;

            _currentLevelSolved = true;
            _feedbackSuccess = true;
            _feedbackMessage = $"{celebration} Saved rating: {progress.bestStars}/3 stars.";

            // High-Retention Scout loop treats
            _saveData.forestTreats++;
            _feedbackMessage += " +1 Forest Treat earned!";

            var currentTier = _saveData.explorerTier ?? "scout";
            if (currentTier == "druid")
            {
                var rand = UnityEngine.Random.Range(0, 4);
                string rewardedResource = "";
                if (rand == 0) { _saveData.elderwood++; rewardedResource = "Elderwood"; }
                else if (rand == 1) { _saveData.riverCrystals++; rewardedResource = "River Crystal"; }
                else if (rand == 2) { _saveData.fireflyDust++; rewardedResource = "Firefly Dust"; }
                else { _saveData.ancientSap++; rewardedResource = "Ancient Sap"; }

                _feedbackMessage += $" +1 {rewardedResource} harvested for crafting!";
            }

            // High-Retention Druid daily trial check
            if (level.id == "daily-trial-level")
            {
                _saveData.dailyTrialCleared = true;
                _saveData.elderwood += 3;
                _saveData.riverCrystals += 2;
                _saveData.fireflyDust += 2;
                _saveData.ancientSap += 1;
                _feedbackMessage += " Daily Trial Rewards: +3 Wood, +2 Crystals, +2 Dust, +1 Sap added to Cauldron!";
            }

            if (wasNewClear)
            {
                var rewardCountAfter = GetUnlockedRewardCount();
                if (rewardCountAfter > rewardCountBefore)
                {
                    var reward = GetLatestUnlockedReward();
                    if (reward != null)
                    {
                        _feedbackMessage += $" Reward unlocked: {reward.title}.";
                    }
                }
            }

            SaveProgress();

            _systems?.VFX?.OnPuzzleSolved(Vector2.zero);
            _systems?.PuzzleManager?.SolvePuzzle(Vector2.zero);

            if (_systems?.Quests != null)
            {
                var gm = string.IsNullOrEmpty(level.gameplayMode) ? "choice" : level.gameplayMode.ToLower();
                if (gm == "memory") _systems.Quests.ProgressObjective("memory_trail_complete");
                else if (gm == "path") _systems.Quests.ProgressObjective("river_trail_complete");
                else _systems.Quests.ProgressObjective("mirror_puzzle_solved");
                if (wasNewClear) _systems.Quests.ProgressObjective("level_complete");
            }

            if (_systems?.Achievements != null && wasNewClear)
            {
                _systems.Achievements.TryUnlock("puz_first_solve", _saveData);
                if (stars == 3 && !_currentLevelHintUsed)
                    _systems.Achievements.TryUnlock("puz_no_hints_5", _saveData);
            }

            _audioController.PlaySuccess(character, _soundEnabled);
            Rebuild();
        }

        private int CalculateStars()
        {
            var stars = 3;
            if (_currentLevelMistakes > 0)
            {
                stars--;
            }

            if (_currentLevelHintUsed)
            {
                stars--;
            }

            return Mathf.Max(1, stars);
        }

        private void ResetLevelState(string levelId, bool keepFeedback)
        {
            _activePuzzleLevelId = levelId;
            _currentLevelMistakes = 0;
            _currentLevelHintUsed = false;
            _currentLevelStarted = false;
            _currentLevelSolved = false;
            _memoryInputs.Clear();
            _pathTrail.Clear();

            if (!keepFeedback)
            {
                var level = GetLevel(levelId);
                if (level != null)
                {
                    _feedbackMessage = $"{level.name}: {level.prompt}";
                    _feedbackSuccess = false;
                }
            }
        }

        private void EnsureLevelState(string levelId)
        {
            if (_activePuzzleLevelId == levelId)
            {
                return;
            }

            ResetLevelState(levelId, false);
        }

        private string GetFirstCharacterId()
        {
            return _content.characters != null && _content.characters.Length > 0
                ? _content.characters[0].id
                : string.Empty;
        }

        private ForestZoneData GetSelectedZone()
        {
            return GetZone(_selectedZoneId) ?? (_content.zones != null && _content.zones.Length > 0 ? _content.zones[0] : new ForestZoneData());
        }

        private LevelData GetSelectedLevel()
        {
            return GetLevel(_selectedLevelId) ?? (_content.levels != null && _content.levels.Length > 0 ? _content.levels[0] : null);
        }

        private CharacterProfile GetSelectedCharacter()
        {
            return GetCharacter(_selectedCharacterId)
                ?? (GetSelectedLevel() != null ? GetCharacter(GetSelectedLevel().characterId) : null)
                ?? (_content.characters != null && _content.characters.Length > 0 ? _content.characters[0] : new CharacterProfile());
        }

        private ForestZoneData GetZone(string zoneId)
        {
            if (_content.zones == null || string.IsNullOrEmpty(zoneId))
            {
                return null;
            }

            foreach (var zone in _content.zones)
            {
                if (zone.id == zoneId)
                {
                    return zone;
                }
            }

            return null;
        }

        private LevelData GetLevel(string levelId)
        {
            if (_content.levels == null || string.IsNullOrEmpty(levelId))
            {
                return null;
            }

            foreach (var level in _content.levels)
            {
                if (level.id == levelId)
                {
                    return level;
                }
            }

            return null;
        }

        private CharacterProfile GetCharacter(string characterId)
        {
            if (_content.characters == null || string.IsNullOrEmpty(characterId))
            {
                return null;
            }

            foreach (var character in _content.characters)
            {
                if (character.id == characterId)
                {
                    return character;
                }
            }

            return null;
        }

        private LevelData[] GetLevelsForZone(string zoneId)
        {
            if (_content.levels == null)
            {
                return new LevelData[0];
            }

            var list = new List<LevelData>();
            foreach (var level in _content.levels)
            {
                if (level.zoneId == zoneId)
                {
                    list.Add(level);
                }
            }

            return list.ToArray();
        }

        private LevelData[] GetFilteredLevels()
        {
            if (_content.levels == null)
            {
                return new LevelData[0];
            }

            var isSearching = !string.IsNullOrEmpty(_searchQuery) || _selectedTypeFilter != "All";
            var list = new List<LevelData>();

            foreach (var level in _content.levels)
            {
                if (!isSearching && level.zoneId != _selectedZoneId)
                {
                    continue;
                }

                if (_selectedTypeFilter != "All")
                {
                    var mode = string.IsNullOrEmpty(level.gameplayMode) ? "choice" : level.gameplayMode.ToLower();
                    var filterLower = _selectedTypeFilter.ToLower();
                    if (filterLower == "choice" && mode != "choice") continue;
                    if (filterLower == "memory" && mode != "memory") continue;
                    if (filterLower == "path" && mode != "path") continue;
                }

                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    var q = _searchQuery.ToLower().Trim();
                    var name = string.IsNullOrEmpty(level.name) ? "" : level.name.ToLower();
                    var type = string.IsNullOrEmpty(level.type) ? "" : level.type.ToLower();
                    var reward = string.IsNullOrEmpty(level.reward) ? "" : level.reward.ToLower();
                    var prompt = string.IsNullOrEmpty(level.prompt) ? "" : level.prompt.ToLower();
                    
                    var charName = "";
                    if (_content.characters != null)
                    {
                        foreach (var c in _content.characters)
                        {
                            if (c.id == level.characterId)
                            {
                                charName = string.IsNullOrEmpty(c.name) ? "" : c.name.ToLower();
                                break;
                            }
                        }
                    }

                    if (!name.Contains(q) && !type.Contains(q) && !reward.Contains(q) && !prompt.Contains(q) && !charName.Contains(q))
                    {
                        continue;
                    }
                }

                list.Add(level);
            }

            return list.ToArray();
        }

        private void CreateSearchInputField(Transform parent)
        {
            var panel = CreatePanel(parent, "SearchInputRow", new Color(1f, 1f, 1f, 0.08f), 8f, 18);
            ForestUiFactory.AddLayout(panel.gameObject, preferredHeight: 80f);
            
            var row = ForestUiFactory.CreateUiObject("Row", panel);
            ForestUiFactory.Stretch(row);
            var layout = ForestUiFactory.AddHorizontalLayout(row.gameObject, 12f, new RectOffset(12, 12, 10, 10));
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            var icon = ForestUiFactory.CreateText(row, "SearchIcon", "Search", _font, 20, _cream, TextAnchor.MiddleLeft);
            ForestUiFactory.AddLayout(icon.gameObject, preferredWidth: 44f);

            var inputGo = ForestUiFactory.CreateUiObject("InputField", row);
            var inputField = inputGo.gameObject.AddComponent<InputField>();
            ForestUiFactory.AddLayout(inputGo.gameObject, preferredWidth: 400f);

            var textComponent = ForestUiFactory.CreateText(inputGo, "Text", _searchQuery, _font, 24, _cream, TextAnchor.MiddleLeft);
            ForestUiFactory.Stretch(textComponent.rectTransform, 6f, 6f, 4f, 4f);
            inputField.textComponent = textComponent;

            var placeholder = ForestUiFactory.CreateText(inputGo, "Placeholder", "Search forest levels...", _font, 24, new Color(_cream.r, _cream.g, _cream.b, 0.4f), TextAnchor.MiddleLeft);
            ForestUiFactory.Stretch(placeholder.rectTransform, 6f, 6f, 4f, 4f);
            inputField.placeholder = placeholder;

            inputField.text = _searchQuery;

            inputField.onValueChanged.AddListener((val) => {
                _searchQuery = val;
            });
            inputField.onEndEdit.AddListener((val) => {
                _searchQuery = val;
                Rebuild();
            });

            if (!string.IsNullOrEmpty(_searchQuery))
            {
                var clearButton = ForestUiFactory.CreateButton(row, "ClearSearch", "Clear", _font, new Color(0f,0f,0f,0f), _cream, () => {
                    _searchQuery = "";
                    Rebuild();
                }, 22);
                ForestUiFactory.AddLayout(clearButton.gameObject, preferredWidth: 60f);
            }
        }

        private void CreateFilterChipsRow(Transform parent)
        {
            var filterRow = ForestUiFactory.CreateUiObject("FilterChipsRow", parent);
            ForestUiFactory.AddHorizontalLayout(filterRow.gameObject, 10f);
            ForestUiFactory.AddLayout(filterRow.gameObject, preferredHeight: 74f);

            var types = new string[] { "All", "Choice", "Memory", "Path" };
            foreach (var t in types)
            {
                var selected = _selectedTypeFilter == t;
                var bg = selected ? _amber : new Color(1f, 1f, 1f, 0.08f);
                var textCol = selected ? _ink : _cream;
                
                var button = ForestUiFactory.CreateButton(
                    filterRow,
                    $"Chip-{t}",
                    t,
                    _font,
                    bg,
                    textCol,
                    () => {
                        _selectedTypeFilter = t;
                        Rebuild();
                    },
                    20
                );
                ForestUiFactory.AddLayout(button.gameObject, preferredHeight: 64f, preferredWidth: 140f);
            }
        }

        private LevelData GetFirstLevelForZone(string zoneId)
        {
            foreach (var level in GetLevelsForZone(zoneId))
            {
                return level;
            }

            return null;
        }

        private bool IsZoneUnlocked(ForestZoneData zone)
        {
            if (zone == null)
            {
                return false;
            }

            if (zone.isPremium && !_premiumUnlocked)
            {
                return false;
            }

            return _completedLevelIds.Count >= zone.unlockAfterClears;
        }

        private bool IsLevelUnlocked(LevelData level)
        {
            if (level == null)
            {
                return false;
            }

            var zone = GetZone(level.zoneId);
            if (!IsZoneUnlocked(zone))
            {
                return false;
            }

            LevelData previous = null;
            foreach (var zoneLevel in GetLevelsForZone(level.zoneId))
            {
                if (zoneLevel.id == level.id)
                {
                    return previous == null || _completedLevelIds.Contains(previous.id);
                }

                previous = zoneLevel;
            }

            return false;
        }

        private LevelData FindPreferredLevelForZone(string zoneId)
        {
            LevelData firstUnlocked = null;
            foreach (var level in GetLevelsForZone(zoneId))
            {
                if (!IsLevelUnlocked(level))
                {
                    continue;
                }

                if (firstUnlocked == null)
                {
                    firstUnlocked = level;
                }

                if (!_completedLevelIds.Contains(level.id))
                {
                    return level;
                }
            }

            return firstUnlocked;
        }

        private LevelData FindNextMission(string fromLevelId)
        {
            LevelData fallback = null;
            var passedCurrent = string.IsNullOrEmpty(fromLevelId);

            if (_content.levels == null)
            {
                return null;
            }

            foreach (var level in _content.levels)
            {
                if (!IsLevelUnlocked(level))
                {
                    continue;
                }

                if (fallback == null && !_completedLevelIds.Contains(level.id))
                {
                    fallback = level;
                }

                if (!passedCurrent)
                {
                    if (level.id == fromLevelId)
                    {
                        passedCurrent = true;
                    }
                    continue;
                }

                if (level.id != fromLevelId && !_completedLevelIds.Contains(level.id))
                {
                    return level;
                }
            }

            return fallback;
        }

        private string GetZoneStatusText(ForestZoneData zone)
        {
            if (zone == null)
            {
                return string.Empty;
            }

            if (!IsZoneUnlocked(zone))
            {
                return GetZoneLockMessage(zone);
            }

            var completed = GetZoneCompletedCount(zone.id);
            var total = GetZoneTotalCount(zone.id);
            return completed >= total
                ? "Status: Zone complete"
                : $"Status: {completed}/{total} missions cleared";
        }

        private string GetZoneLockMessage(ForestZoneData zone)
        {
            if (zone == null)
            {
                return "This zone is not ready yet.";
            }

            if (zone.isPremium && !_premiumUnlocked)
            {
                return string.IsNullOrEmpty(zone.lockMessage)
                    ? "This zone belongs to the full adventure and needs a parent unlock."
                    : zone.lockMessage;
            }

            var remaining = Mathf.Max(0, zone.unlockAfterClears - _completedLevelIds.Count);
            return remaining == 1
                ? "Clear 1 more mission to unlock this zone."
                : $"Clear {remaining} more missions to unlock this zone.";
        }

        private string GetLevelLockMessage(LevelData level)
        {
            if (level == null)
            {
                return "This mission is not ready yet.";
            }

            var zone = GetZone(level.zoneId);
            if (!IsZoneUnlocked(zone))
            {
                return GetZoneLockMessage(zone);
            }

            var previous = GetPreviousLevel(level);
            if (previous != null && !_completedLevelIds.Contains(previous.id))
            {
                return $"Clear {previous.name} first to open this mission.";
            }

            return "This mission is still locked.";
        }

        private LevelData GetPreviousLevel(LevelData target)
        {
            if (target == null)
            {
                return null;
            }

            LevelData previous = null;
            foreach (var level in GetLevelsForZone(target.zoneId))
            {
                if (level.id == target.id)
                {
                    return previous;
                }

                previous = level;
            }

            return null;
        }

        private string GetLevelStatusText(LevelData level, bool unlocked, bool done, int stars, bool active)
        {
            if (!unlocked)
            {
                return "Status: Locked";
            }

            if (done)
            {
                return $"Status: Cleared · {stars}/3 stars";
            }

            return active ? "Status: Ready now" : "Status: Next mission";
        }

        private string GetGameplayMode(LevelData level)
        {
            return string.IsNullOrEmpty(level.gameplayMode) ? "choice" : level.gameplayMode;
        }

        private string FormatMemorySequence(LevelData level)
        {
            if (level == null || level.memorySequence == null || level.memorySequence.Length == 0)
            {
                return "No pattern";
            }

            var currentTier = _saveData.explorerTier ?? "scout";
            var parts = new List<string>();
            foreach (var step in level.memorySequence)
            {
                parts.Add(GetOptionLabel(level, step));
            }

            if (currentTier == "sprout")
            {
                if (parts.Count > 2) parts.RemoveRange(2, parts.Count - 2);
                return string.Join(" -> ", parts.ToArray()) + " (Visual assistance enabled!)";
            }
            else if (currentTier == "druid")
            {
                return string.Join(" -> ", parts.ToArray()) + " (CRITICAL: Copy in REVERSE!)";
            }

            return string.Join(" -> ", parts.ToArray());
        }

        private string FormatPathSequence(LevelData level)
        {
            if (level == null || level.pathSequence == null || level.pathSequence.Length == 0)
            {
                return "No route";
            }

            var parts = new List<string>();
            foreach (var step in level.pathSequence)
            {
                parts.Add(GetPathCellLabel(level, step));
            }

            return string.Join(" -> ", parts.ToArray());
        }

        private string GetNextPathStepId(LevelData level)
        {
            if (level == null || level.pathSequence == null)
            {
                return string.Empty;
            }

            return _pathTrail.Count < level.pathSequence.Length ? level.pathSequence[_pathTrail.Count] : string.Empty;
        }

        private string GetOptionLabel(LevelData level, string optionId)
        {
            if (level.options == null)
            {
                return optionId;
            }

            foreach (var option in level.options)
            {
                if (option.id == optionId)
                {
                    return option.label;
                }
            }

            return optionId;
        }

        private string GetPathCellLabel(LevelData level, string cellId)
        {
            if (level.pathCells == null)
            {
                return cellId;
            }

            foreach (var cell in level.pathCells)
            {
                if (cell.id == cellId)
                {
                    return cell.label;
                }
            }

            return cellId;
        }

        private int GetTotalLevelCount()
        {
            return _content.levels != null ? _content.levels.Length : 0;
        }

        private int GetZoneCompletedCount(string zoneId)
        {
            var count = 0;
            foreach (var level in GetLevelsForZone(zoneId))
            {
                if (_completedLevelIds.Contains(level.id))
                {
                    count++;
                }
            }

            return count;
        }

        private int GetZoneTotalCount(string zoneId)
        {
            return GetLevelsForZone(zoneId).Length;
        }

        private int GetTotalStars()
        {
            var total = 0;
            foreach (var progress in _levelProgressById.Values)
            {
                total += progress.bestStars;
            }

            return total;
        }

        private int GetUnlockedRewardCount()
        {
            var total = 0;
            if (_content.rewards == null)
            {
                return total;
            }

            foreach (var reward in _content.rewards)
            {
                if (_completedLevelIds.Count >= reward.levels)
                {
                    total++;
                }
            }

            return total;
        }

        private RewardMilestoneData GetLatestUnlockedReward()
        {
            RewardMilestoneData latest = null;
            if (_content.rewards == null)
            {
                return null;
            }

            foreach (var reward in _content.rewards)
            {
                if (_completedLevelIds.Count >= reward.levels)
                {
                    latest = reward;
                }
            }

            return latest;
        }

        private RewardMilestoneData GetNextReward()
        {
            if (_content.rewards == null)
            {
                return null;
            }

            foreach (var reward in _content.rewards)
            {
                if (_completedLevelIds.Count < reward.levels)
                {
                    return reward;
                }
            }

            return null;
        }

        private LevelProgressData GetOrCreateLevelProgress(string levelId)
        {
            if (_levelProgressById.TryGetValue(levelId, out var existing))
            {
                return existing;
            }

            var created = new LevelProgressData { levelId = levelId };
            _levelProgressById[levelId] = created;
            return created;
        }

        private int GetBestStars(string levelId)
        {
            return _levelProgressById.TryGetValue(levelId, out var progress) ? progress.bestStars : 0;
        }

        private static string GetCharacterLine(CharacterProfile character, string cueType)
        {
            if (character == null || character.lines == null)
            {
                return string.Empty;
            }

            switch (cueType)
            {
                case "hint":
                    return character.lines.hint;
                case "cheer":
                    return character.lines.cheer;
                default:
                    return character.lines.greeting;
            }
        }

        private int GetRunesDiscoveredCount()
        {
            int count = 0;
            if (_content != null && _content.levels != null)
            {
                foreach (var level in _content.levels)
                {
                    if (level.zoneId == "river-bend" && _completedLevelIds.Contains(level.id))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private bool IsSwitchCell(LevelData level, string cellId)
        {
            return cellId == "stone" || cellId == "stump" || cellId == "stones";
        }

        private bool IsGateCell(LevelData level, string cellId)
        {
            return cellId == "bridge" || cellId == "nest" || cellId == "fern-bridge";
        }

        private void CreateRiddleButton(Transform parent, string label, int riddleIndex, bool correct)
        {
            var btn = ForestUiFactory.CreateButton(
                parent,
                $"RiddleBtn-{riddleIndex}-{label}",
                label,
                _font,
                _twilight,
                _cream,
                () => SubmitRiddleAnswer(riddleIndex, correct),
                18
            );
            ForestUiFactory.AddLayout(btn.gameObject, preferredHeight: 62f, preferredWidth: 210f);
        }

        private void SubmitRiddleAnswer(int riddleIndex, bool correct)
        {
            if (correct)
            {
                if (riddleIndex == 1) _riddle1Solved = true;
                else if (riddleIndex == 2) _riddle2Solved = true;
                else if (riddleIndex == 3) _riddle3Solved = true;

                _feedbackMessage = "Wonderful! You successfully decrypted the ancient rune message!";
                _feedbackSuccess = true;
                _audioController.PlaySuccess(GetSelectedCharacter(), _soundEnabled);
            }
            else
            {
                _feedbackMessage = "The glyph runes hum with friction. That decryption is incorrect. Try again.";
                _feedbackSuccess = false;
                _audioController.PlayWrong(GetSelectedCharacter(), _soundEnabled);
            }

            SaveProgress();
            Rebuild();
        }

        private void BuildSanctuaryTab()
        {
            var currentTier = _saveData.explorerTier ?? "scout";

            var sanctuaryBody = CreateSection(_scrollContent, "Sanctuary Sandbox", "My Cozy Forest Meadow", false);
            CreateBodyText(sanctuaryBody, "A safe place for your forest keepsakes. Drag and drop decorations to build your magical clearing!", _forest, 20);

            var meadowImage = ForestUiFactory.CreateImage(sanctuaryBody, "MeadowMeadow", _moss, false);
            ForestUiFactory.AddLayout(meadowImage.gameObject, preferredHeight: 550f);
            
            var meadowContainer = meadowImage.rectTransform;
            
            if (_saveData.placedItems == null)
            {
                _saveData.placedItems = new PlacedSanctuaryItem[0];
            }

            foreach (var item in _saveData.placedItems)
            {
                if (item == null || string.IsNullOrEmpty(item.itemId)) continue;

                var itemGo = ForestUiFactory.CreateImage(meadowContainer, "Keepsake-" + item.itemId, _cream, true);
                var rect = itemGo.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(90f, 90f);
                rect.anchoredPosition = new Vector2(item.posX, item.posY);

                string visualEmoji = GetKeepsakeEmoji(item.itemId);
                var text = ForestUiFactory.CreateText(itemGo.transform, "Emoji", visualEmoji, _font, 36, _ink, TextAnchor.MiddleCenter);
                ForestUiFactory.Stretch(text.rectTransform);

                var dragHandler = itemGo.gameObject.AddComponent<SanctuaryDragHandler>();
                var targetItem = item;
                dragHandler.onDragEnd = (pos) =>
                {
                    targetItem.posX = pos.x;
                    targetItem.posY = pos.y;
                    _audioController.PlaySelect(_soundEnabled);
                    SaveProgress();
                };
            }

            var resourceBody = CreateSection(_scrollContent, "Crafting Resources", "Gathered Components", false);
            var resRow = ForestUiFactory.CreateUiObject("ResourceRow", resourceBody);
            ForestUiFactory.AddHorizontalLayout(resRow.gameObject, 10f);
            resRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateResourceCounter(resRow, "Wood", _saveData.elderwood);
            CreateResourceCounter(resRow, "Crystal", _saveData.riverCrystals);
            CreateResourceCounter(resRow, "Dust", _saveData.fireflyDust);
            CreateResourceCounter(resRow, "Sap", _saveData.ancientSap);

            var actionBody = CreateSection(_scrollContent, "Keepsakes Basket", "Place items in the meadow", false);
            var placementRow = ForestUiFactory.CreateUiObject("PlacementRow", actionBody);
            ForestUiFactory.AddHorizontalLayout(placementRow.gameObject, 10f);
            placementRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreatePlacementButton(placementRow, "Acorn", "acorn");
            CreatePlacementButton(placementRow, "Butterfly", "butterfly");
            CreatePlacementButton(placementRow, "Mushroom", "mushroom");
            CreatePlacementButton(placementRow, "Flower", "flower");

            // High-Retention Sprout Seedling Pot
            var seedlingBody = CreateSection(_scrollContent, "Magic Sprout Pot (Ages 4-6)", "Tap to water and watch me grow!", false);
            var seedlingCard = CreatePanel(seedlingBody, "SeedlingCard", _fernLight, 8f, 18);
            
            string seedlingEmoji = "[Pot]";
            string seedlingStageName = "Empty Clay Pot";
            if (_saveData.sproutGrowth == 1) { seedlingEmoji = "[Sprout]"; seedlingStageName = "Tiny Sprout"; }
            else if (_saveData.sproutGrowth == 2) { seedlingEmoji = "[Fern]"; seedlingStageName = "Growing Fern"; }
            else if (_saveData.sproutGrowth == 3) { seedlingEmoji = "[Wild Rose]"; seedlingStageName = "Blooming Wild Rose"; }
            else if (_saveData.sproutGrowth >= 4) { seedlingEmoji = "[Golden Blossom]"; seedlingStageName = "Magical Golden Blossom"; }

            CreateCardTitle(seedlingCard, $"{seedlingEmoji} Stage: {seedlingStageName}", _ink, 24);
            CreateBodyText(seedlingCard, _saveData.sproutGrowth >= 4 
                ? "The Golden Blossom has bloomed! Tap to harvest a magical sticker keepsake!" 
                : "Give the seedling a gentle splash of water to help it bloom!", _forest, 18);

            var seedlingActionRow = ForestUiFactory.CreateUiObject("SeedlingActions", seedlingCard);
            ForestUiFactory.AddHorizontalLayout(seedlingActionRow.gameObject, 10f);
            seedlingActionRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var waterBtn = ForestUiFactory.CreateButton(seedlingActionRow, "WaterSeedlingBtn", 
                _saveData.sproutGrowth >= 4 ? "Harvest Keepsake" : "Water Seedling", 
                _font, _amber, _ink, () => WaterSproutSeedling(), 18);
            ForestUiFactory.AddLayout(waterBtn.gameObject, preferredHeight: 64f, preferredWidth: 260f);

            if (currentTier == "druid")
            {
                var craftBody = CreateSection(_scrollContent, "Alchemical Crafting Cauldron", "Druid blueprints", false);
                CreateBodyText(craftBody, "Gather raw components by completing advanced missions, then combine them below!", _forest, 20);

                var blueprintRow = ForestUiFactory.CreateUiObject("BlueprintRow", craftBody);
                ForestUiFactory.AddHorizontalLayout(blueprintRow.gameObject, 12f);
                blueprintRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var recipe1Card = CreatePanel(blueprintRow, "Recipe1Card", _cream, 6f, 16);
                ForestUiFactory.AddLayout(recipe1Card.gameObject, flexibleWidth: 1f);
                CreateCardTitle(recipe1Card, "Celestial Dream-Catcher", _ink, 22);
                CreateBodyText(recipe1Card, "Requires:\n4 Elderwood\n2 Crystals\n2 Firefly Dust", _forest, 18);
                var canCraft1 = _saveData.elderwood >= 4 && _saveData.riverCrystals >= 2 && _saveData.fireflyDust >= 2;
                var craft1Btn = ForestUiFactory.CreateButton(recipe1Card, "CraftDreamCatcher", canCraft1 ? "Craft Item" : "Missing Resources", _font, canCraft1 ? _moss : new Color(0.8f, 0.8f, 0.8f, 0.4f), canCraft1 ? _cream : _bark, () =>
                {
                    if (canCraft1)
                    {
                        CraftAlchemicalItem("dream_catcher", 4, 2, 2, 0);
                    }
                }, 20);
                craft1Btn.interactable = canCraft1;
                ForestUiFactory.AddLayout(craft1Btn.gameObject, preferredHeight: 64f);

                var recipe2Card = CreatePanel(blueprintRow, "Recipe2Card", _cream, 6f, 16);
                ForestUiFactory.AddLayout(recipe2Card.gameObject, flexibleWidth: 1f);
                CreateCardTitle(recipe2Card, "Cozy Forest Campfire", _ink, 22);
                CreateBodyText(recipe2Card, "Requires:\n3 Elderwood\n3 Firefly Dust\n1 Ancient Sap", _forest, 18);
                var canCraft2 = _saveData.elderwood >= 3 && _saveData.fireflyDust >= 3 && _saveData.ancientSap >= 1;
                var craft2Btn = ForestUiFactory.CreateButton(recipe2Card, "CraftCampfire", canCraft2 ? "Craft Item" : "Missing Resources", _font, canCraft2 ? _moss : new Color(0.8f, 0.8f, 0.8f, 0.4f), canCraft2 ? _cream : _bark, () =>
                {
                    if (canCraft2)
                    {
                        CraftAlchemicalItem("campfire", 3, 0, 3, 1);
                    }
                }, 20);
                craft2Btn.interactable = canCraft2;
                ForestUiFactory.AddLayout(craft2Btn.gameObject, preferredHeight: 64f);

                if (_saveData.craftedItemIds != null && _saveData.craftedItemIds.Length > 0)
                {
                    var craftedSectionBody = CreateSection(_scrollContent, "Your Crafted Keepsakes", "Tap to place in your meadow", false);
                    var craftedButtonsRow = ForestUiFactory.CreateUiObject("CraftedButtonsRow", craftedSectionBody);
                    ForestUiFactory.AddHorizontalLayout(craftedButtonsRow.gameObject, 10f);
                    craftedButtonsRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    var counts = new Dictionary<string, int>();
                    foreach (var itemId in _saveData.craftedItemIds)
                    {
                        if (counts.ContainsKey(itemId)) counts[itemId]++;
                        else counts[itemId] = 1;
                    }

                    foreach (var pair in counts)
                    {
                        var itemId = pair.Key;
                        var count = pair.Value;
                        var label = itemId == "dream_catcher" ? "Celestial Dream-Catcher" : "Cozy Forest Campfire";
                        var emoji = itemId == "dream_catcher" ? "[Dream-Catcher]" : "[Campfire]";
                        var placeBtn = ForestUiFactory.CreateButton(craftedButtonsRow, "PlaceCrafted-" + itemId, $"{emoji} {label} ({count}x)", _font, _amber, _ink, () =>
                        {
                            PlaceKeepsake(itemId);
                        }, 18);
                        ForestUiFactory.AddLayout(placeBtn.gameObject, preferredHeight: 70f, preferredWidth: 260f);
                    }
                }
            }

            var controlBody = CreateSection(_scrollContent, "Meadow Maintenance", "Clear clearing", false);
            var clearBtn = ForestUiFactory.CreateButton(controlBody, "ClearMeadowBtn", "Reset Meadow Clearing", _font, _amber, _ink, () =>
            {
                _saveData.placedItems = new PlacedSanctuaryItem[0];
                _audioController.PlaySelect(_soundEnabled);
                SaveProgress();
                Rebuild();
            }, 22);
            ForestUiFactory.AddLayout(clearBtn.gameObject, preferredHeight: 72f);
        }

        private void CreateResourceCounter(Transform parent, string label, int value)
        {
            var card = CreatePanel(parent, $"ResCounter-{label}", _cream, 4f, 12);
            ForestUiFactory.AddLayout(card.gameObject, preferredHeight: 120f, flexibleWidth: 1f);
            CreateCardTitle(card, label, _forest, 18);
            CreateCardTitle(card, value.ToString(), _ink, 26);
        }

        private void CreatePlacementButton(Transform parent, string label, string itemId)
        {
            var btn = ForestUiFactory.CreateButton(parent, $"PlaceBtn-{itemId}", label, _font, _twilight, _cream, () =>
            {
                PlaceKeepsake(itemId);
            }, 18);
            ForestUiFactory.AddLayout(btn.gameObject, preferredHeight: 68f, flexibleWidth: 1f);
        }

        private string GetKeepsakeEmoji(string itemId)
        {
            switch (itemId)
            {
                case "acorn": return "[Acorn]";
                case "butterfly": return "[Butterfly]";
                case "mushroom": return "[Mushroom]";
                case "flower": return "[Flower]";
                case "dream_catcher": return "[Dream-Catcher]";
                case "campfire": return "[Campfire]";
                default: return "[Sprout]";
            }
        }

        private string GetKeepsakeName(string itemId)
        {
            switch (itemId)
            {
                case "dream_catcher": return "Celestial Dream-Catcher";
                case "campfire": return "Cozy Forest Campfire";
                default: return itemId;
            }
        }

        private void PlaceKeepsake(string itemId)
        {
            if (itemId == "dream_catcher" || itemId == "campfire")
            {
                var list = new List<string>(_saveData.craftedItemIds ?? new string[0]);
                if (list.Contains(itemId))
                {
                    list.Remove(itemId);
                    _saveData.craftedItemIds = list.ToArray();
                }
                else
                {
                    return;
                }
            }

            var newItem = new PlacedSanctuaryItem
            {
                itemId = itemId,
                posX = UnityEngine.Random.Range(-250f, 250f),
                posY = UnityEngine.Random.Range(-180f, 180f),
                scale = 1f
            };

            var placedList = new List<PlacedSanctuaryItem>(_saveData.placedItems ?? new PlacedSanctuaryItem[0]);
            placedList.Add(newItem);
            _saveData.placedItems = placedList.ToArray();

            _feedbackMessage = $"Placed {GetKeepsakeEmoji(itemId)} keepsake in the meadow clearing!";
            _feedbackSuccess = true;

            _audioController.PlaySelect(_soundEnabled);
            SaveProgress();
            Rebuild();
        }

        private void CraftAlchemicalItem(string itemId, int wood, int crystal, int dust, int sap)
        {
            if (_saveData.elderwood >= wood &&
                _saveData.riverCrystals >= crystal &&
                _saveData.fireflyDust >= dust &&
                _saveData.ancientSap >= sap)
            {
                _saveData.elderwood -= wood;
                _saveData.riverCrystals -= crystal;
                _saveData.fireflyDust -= dust;
                _saveData.ancientSap -= sap;

                var list = new List<string>(_saveData.craftedItemIds ?? new string[0]);
                list.Add(itemId);
                _saveData.craftedItemIds = list.ToArray();

                _feedbackMessage = $"Success! Crafted one {GetKeepsakeName(itemId)} using the alchemical cauldron.";
                _feedbackSuccess = true;

                _audioController.PlaySuccess(GetSelectedCharacter(), _soundEnabled);
                SaveProgress();
                Rebuild();
            }
            else
            {
                _feedbackMessage = "Not enough alchemical raw components in your inventory.";
                _feedbackSuccess = false;
                _audioController.PlayWrong(GetSelectedCharacter(), _soundEnabled);
                Rebuild();
            }
        }

        // --- High-Retention Helper Methods ---

        private int GetCharacterBond(string characterId)
        {
            if (characterId == "pip") return _saveData.pipBond;
            if (characterId == "mimi") return _saveData.mimiBond;
            if (characterId == "tomo") return _saveData.tomoBond;
            if (characterId == "luma") return _saveData.lumaBond;
            return 1;
        }

        private void IncreaseCharacterBond(string characterId)
        {
            if (characterId == "pip") _saveData.pipBond++;
            else if (characterId == "mimi") _saveData.mimiBond++;
            else if (characterId == "tomo") _saveData.tomoBond++;
            else if (characterId == "luma") _saveData.lumaBond++;
        }

        private void FeedCharacterTreat(CharacterProfile character)
        {
            if (_saveData.forestTreats > 0)
            {
                _saveData.forestTreats--;
                IncreaseCharacterBond(character.id);
                _feedbackSuccess = true;
                _feedbackMessage = $"{character.name} loved the treat! Friendship Bond increased! Lvl is now {GetCharacterBond(character.id)}.";
                _systems?.VFX?.OnDiscovery(Vector2.zero);
                if (_systems?.Achievements != null)
                {
                    var bond = GetCharacterBond(character.id);
                    if (bond >= 2) _systems.Achievements.TryUnlock($"bond_{character.id}_1", _saveData);
                    if (character.id == "pip" && bond >= 5) _systems.Achievements.TryUnlock("bond_pip_5", _saveData);
                }
                _audioController.PlaySuccess(character, _soundEnabled);
                SaveProgress();
                Rebuild();
            }
        }

        private void WaterSproutSeedling()
        {
            if (_saveData.sproutGrowth >= 4)
            {
                _saveData.sproutGrowth = 1;
                var rand = UnityEngine.Random.Range(0, 4);
                string earnedSticker = "acorn";
                if (rand == 0) earnedSticker = "acorn";
                else if (rand == 1) earnedSticker = "butterfly";
                else if (rand == 2) earnedSticker = "mushroom";
                else earnedSticker = "flower";

                PlaceKeepsake(earnedSticker);
                _feedbackSuccess = true;
                _feedbackMessage = $"Harvested a magical {earnedSticker} keepsake sticker! It has been placed in your cozy meadow!";
                _audioController.PlaySuccess(GetSelectedCharacter(), _soundEnabled);
            }
            else
            {
                _saveData.sproutGrowth++;
                _feedbackSuccess = true;
                _feedbackMessage = "Splash! You watered the seedling and saw it grow taller!";
                _audioController.PlaySelect(_soundEnabled);
            }
            SaveProgress();
            Rebuild();
        }

        private void PlayDailyDruidTrial()
        {
            var dailyLevel = new LevelData
            {
                id = "daily-trial-level",
                zoneId = "river-bend",
                characterId = "luma",
                name = "Daily Druid Cipher Trial",
                type = "Memory sequence",
                difficulty = "Expert",
                reward = "Daily ingredients cache",
                prompt = "Follow the alternating cosmic lights in exact REVERSE order!",
                hint = "Use your intuition. Sequence is: Star, Fern, Berry, Lantern, Star.",
                celebration = "You successfully solved the Daily Druid Cipher!",
                gameplayMode = "memory",
                options = new[]
                {
                    new LevelOptionData { id = "lantern", label = "Lantern" },
                    new LevelOptionData { id = "fern", label = "Fern" },
                    new LevelOptionData { id = "star", label = "Star" },
                    new LevelOptionData { id = "berry", label = "Berry" }
                },
                memorySequence = new[] { "star", "fern", "berry", "lantern", "star" }
            };

            var levelsList = new List<LevelData>(_content.levels);
            if (levelsList.Find(l => l.id == "daily-trial-level") == null)
            {
                levelsList.Add(dailyLevel);
                _content.levels = levelsList.ToArray();
            }

            _saveData.explorerTier = "druid";
            SelectLevel("daily-trial-level");
        }
    }
}

