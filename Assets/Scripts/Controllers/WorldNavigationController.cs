using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Manages the Play tab and zone/level selection.
    ///
    /// Uses ReusableCardPool for zone cards and level cards — no Destroy() on zone change.
    /// When the player selects a different zone, only the level cards re-bind.
    /// Zone cards only re-bind when world state changes (region unlock event).
    /// </summary>
    public class WorldNavigationController : PanelViewController
    {
        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<string> OnLevelSelected;   // levelId
        public event Action<string> OnZoneSelected;    // zoneId

        // ─── Dependencies ────────────────────────────────────────────────────────

        private ForestUIRouter _router;

        // ─── State ───────────────────────────────────────────────────────────────

        private string _selectedZoneId;
        private string _selectedLevelId;

        // ─── UI Pools ─────────────────────────────────────────────────────────────

        private ReusableCardPool _zoneCardPool;
        private ReusableCardPool _levelCardPool;

        // ─── Static UI References ─────────────────────────────────────────────────

        private Text  _zoneHeaderLabel;
        private Text  _levelHeaderLabel;
        private Image _selectedZoneAccent;

        // Colors
        private static readonly Color BackgroundDark  = new Color32(20, 54, 41, 255);
        private static readonly Color CardLight       = new Color32(248, 243, 223, 255);
        private static readonly Color CardSelected    = new Color32(231, 246, 217, 255);
        private static readonly Color CardLocked      = new Color32(200, 200, 195, 255);
        private static readonly Color TextDark        = new Color32(16, 35, 27, 255);
        private static readonly Color TextMoss        = new Color32(47, 122, 86, 255);
        private static readonly Color TextBark        = new Color32(106, 74, 53, 255);
        private static readonly Color AccentAmber     = new Color32(245, 184, 92, 255);
        private static readonly Color AccentMint      = new Color32(159, 216, 168, 255);

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Configure(ForestUIRouter router)
        {
            _router = router;
        }

        protected override void OnBuild()
        {
            BuildLayout();
        }

        protected override void OnRefresh(UIDirtyFlag dirtyFlags)
        {
            if ((dirtyFlags & UIDirtyFlag.WorldState) != 0 ||
                (dirtyFlags & UIDirtyFlag.Progress) != 0)
            {
                BindZoneCards();
            }

            BindLevelCards(_selectedZoneId);
        }

        protected override void OnShow()
        {
            EnsureValidSelection();
        }

        // ─── Layout (built once) ──────────────────────────────────────────────────

        private void BuildLayout()
        {
            var scroll = ForestUiFactory.CreateUiObject("ScrollRoot", RootTransform);
            ForestUiFactory.Stretch(scroll);
            ForestUiFactory.CreateScrollView(scroll, out var scrollContent);

            // Zone section header
            var zoneHeader = ForestUiFactory.CreateUiObject("ZoneHeader", scrollContent);
            ForestUiFactory.AddHorizontalLayout(zoneHeader.gameObject, 12f);
            zoneHeader.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _zoneHeaderLabel = ForestUiFactory.CreateText(zoneHeader, "ZoneTitle",
                "Forest Zones", ForestUiFactory.GetDefaultFont(), 26,
                AccentMint, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(_zoneHeaderLabel.gameObject, preferredHeight: 48f);

            // Zone card pool container
            var zoneContainer = ForestUiFactory.CreateUiObject("ZoneCards", scrollContent);
            var zoneRt = zoneContainer.GetComponent<RectTransform>();
            var zoneLayout = zoneContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            zoneLayout.spacing = 10f;
            zoneLayout.childForceExpandWidth  = true;
            zoneLayout.childForceExpandHeight = false;
            zoneContainer.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _zoneCardPool = new ReusableCardPool(zoneRt, maxCapacity: 12).WithDefaultHeight(110f);

            // Level section header
            _levelHeaderLabel = ForestUiFactory.CreateText(scrollContent, "LevelTitle",
                "Levels", ForestUiFactory.GetDefaultFont(), 26,
                AccentAmber, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(_levelHeaderLabel.gameObject, preferredHeight: 48f);

            // Level card pool container
            var levelContainer = ForestUiFactory.CreateUiObject("LevelCards", scrollContent);
            var levelRt = levelContainer.GetComponent<RectTransform>();
            var levelLayout = levelContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            levelLayout.spacing = 10f;
            levelLayout.childForceExpandWidth  = true;
            levelLayout.childForceExpandHeight = false;
            levelContainer.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _levelCardPool = new ReusableCardPool(levelRt, maxCapacity: 25).WithDefaultHeight(100f);
        }

        // ─── Zone Binding ─────────────────────────────────────────────────────────

        private void BindZoneCards()
        {
            if (Content?.zones == null) return;

            _zoneCardPool.Bind(
                Content.zones,
                (card, zone, index) =>
                {
                    var unlocked = IsZoneUnlocked(zone);
                    var selected = zone.id == _selectedZoneId;

                    card.SetBackground(
                        !unlocked ? CardLocked :
                        selected  ? CardSelected :
                                    CardLight);

                    card.SetTitle(zone.title, TextDark);
                    card.SetBody(zone.mood, TextMoss);
                    card.SetSubtitle(
                        unlocked
                            ? $"{GetZoneClearCount(zone.id)} clears"
                            : zone.lockMessage ?? $"Unlock: {zone.unlockAfterClears} clears",
                        unlocked ? TextBark : TextBark);

                    // Accent bar color from zone hex
                    var accent = ForestUiFactory.FromHex(zone.accentHex, AccentAmber);
                    card.SetAccentColor(accent);

                    card.SetTapAction(unlocked ? () => SelectZone(zone.id) : (Action)null);
                    card.SetHeight(110f);
                });
        }

        // ─── Level Binding ────────────────────────────────────────────────────────

        private void BindLevelCards(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || Content?.levels == null)
            {
                _levelCardPool.Clear();
                return;
            }

            var zoneLevels = new List<LevelData>();
            foreach (var level in Content.levels)
            {
                if (level.zoneId == zoneId)
                    zoneLevels.Add(level);
            }

            if (_levelHeaderLabel != null)
            {
                var zone = GetZone(zoneId);
                _levelHeaderLabel.text = zone != null ? $"{zone.title} Levels" : "Levels";
            }

            _levelCardPool.Bind(
                zoneLevels,
                (card, level, index) =>
                {
                    var isCompleted = IsLevelCompleted(level.id);
                    var isSelected  = level.id == _selectedLevelId;
                    var isUnlocked  = IsLevelUnlocked(level);

                    card.SetBackground(
                        isSelected   ? new Color(0.18f, 0.42f, 0.3f) :
                        isCompleted  ? new Color(0.12f, 0.28f, 0.20f) :
                                       new Color(0.10f, 0.22f, 0.16f));

                    card.SetTitle(
                        isCompleted ? $"✓ {level.name}" : level.name,
                        isCompleted ? AccentMint : CardLight);

                    card.SetBody(
                        $"{FormatType(level.type)} · {level.difficulty}",
                        new Color(0.65f, 0.85f, 0.65f));

                    card.SetSubtitle(level.reward ?? "", TextBark);
                    card.SetHeight(100f);

                    card.SetTapAction(isUnlocked ? () => SelectLevel(level.id) : (Action)null);
                });
        }

        // ─── Interaction ──────────────────────────────────────────────────────────

        private void SelectZone(string zoneId)
        {
            if (_selectedZoneId == zoneId) return;
            _selectedZoneId = zoneId;

            // Only re-bind level cards — zone cards just update highlight
            BindZoneCards();
            BindLevelCards(zoneId);

            OnZoneSelected?.Invoke(zoneId);
        }

        private void SelectLevel(string levelId)
        {
            _selectedLevelId = levelId;
            OnLevelSelected?.Invoke(levelId);

            BindLevelCards(_selectedZoneId); // just update highlight
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void EnsureValidSelection()
        {
            if (Content?.zones == null) return;

            if (string.IsNullOrEmpty(_selectedZoneId))
            {
                foreach (var z in Content.zones)
                {
                    if (IsZoneUnlocked(z)) { _selectedZoneId = z.id; break; }
                }
            }

            if (string.IsNullOrEmpty(_selectedLevelId) && !string.IsNullOrEmpty(_selectedZoneId))
            {
                foreach (var l in Content.levels)
                {
                    if (l.zoneId == _selectedZoneId && IsLevelUnlocked(l))
                    {
                        _selectedLevelId = l.id;
                        break;
                    }
                }
            }
        }

        private ForestZoneData GetZone(string zoneId)
        {
            if (Content?.zones == null) return null;
            foreach (var z in Content.zones)
                if (z.id == zoneId) return z;
            return null;
        }

        private bool IsZoneUnlocked(ForestZoneData zone)
        {
            if (zone == null) return false;
            if (!zone.isPremium) return true;
            return SaveData?.premiumUnlocked == true;
        }

        private bool IsLevelUnlocked(LevelData level)
        {
            return true; // Level unlock logic from zone + progress
        }

        private bool IsLevelCompleted(string levelId)
        {
            if (SaveData?.levelProgress == null) return false;
            foreach (var p in SaveData.levelProgress)
                if (p.levelId == levelId && p.completed) return true;
            return false;
        }

        private int GetZoneClearCount(string zoneId)
        {
            var count = 0;
            if (SaveData?.levelProgress == null) return 0;
            if (Content?.levels == null) return 0;

            var zoneIds = new HashSet<string>();
            foreach (var l in Content.levels)
                if (l.zoneId == zoneId) zoneIds.Add(l.id);

            foreach (var p in SaveData.levelProgress)
                if (p.completed && zoneIds.Contains(p.levelId)) count++;

            return count;
        }

        private static string FormatType(string type)
        {
            return type switch
            {
                "memory"    => "Memory",
                "path"      => "Path",
                "choice"    => "Logic",
                "cipher"    => "Cipher",
                "music"     => "Music",
                _           => type ?? "Puzzle"
            };
        }
    }
}
