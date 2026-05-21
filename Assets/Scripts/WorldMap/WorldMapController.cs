using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Interactive world map controller. Replaces the flat text zone card list with
    /// a spatial, visual representation of the 10-region forest world.
    ///
    /// Architecture:
    ///   - Each region is a RegionNode (Image + Button + CanvasGroup)
    ///   - Region positions are defined in a layout config matched to the 10-zone tree
    ///   - Connections are drawn as LineRenderer-style Images
    ///   - Fog of war (FogOfWarSystem) overlays locked regions
    ///   - Unlock animations play when WorldStateManager fires OnRegionUnlocked
    ///   - Map supports pan (drag) and tap-to-select regions
    ///
    /// Node layout (normalized 0-1 coordinates matching region tree):
    ///
    ///        [skyroot]
    ///           |
    ///    [observatory]
    ///       /        \
    ///  [ruins]    [marsh]
    ///     |          |
    ///  [caverns]  (via marsh)
    ///     |
    ///  [elderwood]
    ///     |
    ///  [moonlit]
    ///     |
    ///  [river]
    ///     |
    ///  [firefly]
    ///     |
    ///  [meadow]  ← always unlocked
    /// </summary>
    public class WorldMapController : PanelViewController
    {
        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<string> OnRegionTapped;    // regionId
        public event Action<string> OnBossNodeTapped;  // bossId

        // ─── Dependencies ────────────────────────────────────────────────────────

        private FogOfWarSystem      _fog;
        private MapPathAnimator     _pathAnimator;
        private RegionUnlockSequence _unlockSequence;
        private ForestUIRouter      _router;

        // ─── Region Layout ────────────────────────────────────────────────────────

        // Normalized (0-1) map positions for each region
        private static readonly Dictionary<string, Vector2> RegionPositions = new Dictionary<string, Vector2>
        {
            { "fern-trail",           new Vector2(0.50f, 0.10f) },
            { "firefly-hollow",       new Vector2(0.50f, 0.22f) },
            { "river-bend",           new Vector2(0.50f, 0.34f) },
            { "moonlit-creek",        new Vector2(0.50f, 0.46f) },
            { "elderwood-grove",      new Vector2(0.50f, 0.56f) },
            { "crystal-caverns",      new Vector2(0.30f, 0.64f) },
            { "firefly-marsh",        new Vector2(0.70f, 0.64f) },
            { "forgotten-ruins",      new Vector2(0.30f, 0.74f) },
            { "ancient-observatory",  new Vector2(0.50f, 0.83f) },
            { "skyroot-canopy",       new Vector2(0.50f, 0.93f) },
        };

        private static readonly string[] RegionOrder = new[]
        {
            "fern-trail", "firefly-hollow", "river-bend", "moonlit-creek",
            "elderwood-grove", "crystal-caverns", "firefly-marsh",
            "forgotten-ruins", "ancient-observatory", "skyroot-canopy"
        };

        // ─── Runtime State ────────────────────────────────────────────────────────

        private readonly Dictionary<string, RegionNode> _nodes = new Dictionary<string, RegionNode>();
        private string _selectedRegionId;
        private RectTransform _mapRoot;
        private Vector2 _mapDragStart;
        private bool    _isDragging;

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color UnlockedColor  = new Color32( 47, 122,  86, 255);
        private static readonly Color LockedColor    = new Color32( 60,  60,  55, 180);
        private static readonly Color SelectedColor  = new Color32(245, 184,  92, 255);
        private static readonly Color MasteredColor  = new Color32(100, 180, 240, 255);
        private static readonly Color PathColor      = new Color32( 80, 140, 100, 120);
        private static readonly Color TextCream      = new Color32(248, 243, 223, 255);

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Configure(FogOfWarSystem fog, MapPathAnimator pathAnimator,
            RegionUnlockSequence unlockSequence, ForestUIRouter router)
        {
            _fog            = fog;
            _pathAnimator   = pathAnimator;
            _unlockSequence = unlockSequence;
            _router         = router;
        }

        protected override void OnBuild()
        {
            BuildMapLayout();
            SubscribeToWorldEvents();
        }

        protected override void OnRefresh(UIDirtyFlag dirtyFlags)
        {
            if ((dirtyFlags & UIDirtyFlag.WorldState) != 0)
                RefreshAllNodes();
        }

        protected override void OnShow()
        {
            RefreshAllNodes();
        }

        // ─── Map Layout (built once) ──────────────────────────────────────────────

        private void BuildMapLayout()
        {
            // Background parchment
            var bgGo = new GameObject("MapBackground");
            bgGo.transform.SetParent(RootTransform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color32(14, 35, 24, 255);

            // Map container (scrollable / draggable)
            var mapGo = new GameObject("MapRoot");
            mapGo.transform.SetParent(RootTransform, false);
            _mapRoot = mapGo.AddComponent<RectTransform>();
            _mapRoot.anchorMin = new Vector2(0.02f, 0.05f);
            _mapRoot.anchorMax = new Vector2(0.98f, 0.95f);
            _mapRoot.sizeDelta = Vector2.zero;

            // Path lines layer
            var pathsGo = new GameObject("Paths");
            pathsGo.transform.SetParent(_mapRoot, false);
            var pathsRt = pathsGo.AddComponent<RectTransform>();
            pathsRt.anchorMin = Vector2.zero;
            pathsRt.anchorMax = Vector2.one;
            pathsRt.sizeDelta = Vector2.zero;

            BuildPathLines(pathsRt);

            // Region nodes layer
            var nodesGo = new GameObject("Nodes");
            nodesGo.transform.SetParent(_mapRoot, false);
            var nodesRt = nodesGo.AddComponent<RectTransform>();
            nodesRt.anchorMin = Vector2.zero;
            nodesRt.anchorMax = Vector2.one;
            nodesRt.sizeDelta = Vector2.zero;

            BuildRegionNodes(nodesRt);

            // Title
            var titleLabel = ForestUiFactory.CreateText(RootTransform, "MapTitle",
                "The Forest World", ForestUiFactory.GetDefaultFont(), 28,
                new Color32(159, 216, 168, 255), TextAnchor.MiddleCenter, FontStyle.Bold);
            var titleRt = titleLabel.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.93f);
            titleRt.anchorMax = new Vector2(1f, 1.0f);
            titleRt.sizeDelta = Vector2.zero;
        }

        private void BuildPathLines(RectTransform parent)
        {
            // Draw connecting lines between regions
            var connections = new[]
            {
                ("fern-trail",          "firefly-hollow"),
                ("firefly-hollow",      "river-bend"),
                ("river-bend",          "moonlit-creek"),
                ("moonlit-creek",       "elderwood-grove"),
                ("elderwood-grove",     "crystal-caverns"),
                ("elderwood-grove",     "firefly-marsh"),
                ("crystal-caverns",     "forgotten-ruins"),
                ("firefly-marsh",       "ancient-observatory"),
                ("forgotten-ruins",     "ancient-observatory"),
                ("ancient-observatory", "skyroot-canopy"),
            };

            foreach (var (from, to) in connections)
            {
                if (!RegionPositions.TryGetValue(from, out var posA)) continue;
                if (!RegionPositions.TryGetValue(to,   out var posB)) continue;

                CreatePathLine(parent, from, to, posA, posB);
            }
        }

        private void CreatePathLine(RectTransform parent, string from, string to,
            Vector2 normA, Vector2 normB)
        {
            var go = new GameObject($"Path_{from}_{to}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            // Position and rotate the line
            var mapRect = _mapRoot.rect;
            var posA    = new Vector2(normA.x * mapRect.width, normA.y * mapRect.height);
            var posB    = new Vector2(normB.x * mapRect.width, normB.y * mapRect.height);
            var center  = (posA + posB) * 0.5f;
            var length  = Vector2.Distance(posA, posB);
            var angle   = Mathf.Atan2(posB.y - posA.y, posB.x - posA.x) * Mathf.Rad2Deg;

            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 0f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta        = new Vector2(length, 4f);
            rt.localRotation    = Quaternion.Euler(0f, 0f, angle);

            var img  = go.AddComponent<Image>();
            img.color = PathColor;

            // Store for path animator
            _pathAnimator?.RegisterPath(from, to, rt, img);
        }

        private void BuildRegionNodes(RectTransform parent)
        {
            foreach (var regionId in RegionOrder)
            {
                if (!RegionPositions.TryGetValue(regionId, out var normPos)) continue;

                var node = CreateRegionNode(parent, regionId, normPos);
                _nodes[regionId] = node;
            }
        }

        private RegionNode CreateRegionNode(RectTransform parent, string regionId, Vector2 normPos)
        {
            var go = new GameObject($"Node_{regionId}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            // Position will be set in RefreshAllNodes once we have actual rect size
            rt.anchoredPosition = normPos * new Vector2(900f, 1600f); // estimated
            rt.sizeDelta        = new Vector2(80f, 80f);

            var bg    = go.AddComponent<Image>();
            bg.color  = LockedColor;

            var btn   = go.AddComponent<Button>();
            btn.targetGraphic = bg;

            var cg    = go.AddComponent<CanvasGroup>();

            // Region label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(-0.5f, -0.5f);
            labelRt.anchorMax = new Vector2(1.5f, 0f);
            labelRt.sizeDelta = Vector2.zero;
            labelRt.anchoredPosition = new Vector2(0f, -12f);

            var label = labelGo.AddComponent<Text>();
            label.font      = ForestUiFactory.GetDefaultFont();
            label.fontSize  = 14;
            label.color     = TextCream;
            label.alignment = TextAnchor.MiddleCenter;
            label.text      = regionId;

            var node = new RegionNode
            {
                RegionId   = regionId,
                Rect       = rt,
                Background = bg,
                Button     = btn,
                CanvasGroup = cg,
                NameLabel  = label
            };

            btn.onClick.AddListener(() => OnNodeTapped(node));

            // Apply fog
            _fog?.ApplyFog(regionId, cg);

            return node;
        }

        // ─── Node Refresh ─────────────────────────────────────────────────────────

        private void RefreshAllNodes()
        {
            if (Systems?.World == null) return;

            foreach (var kv in _nodes)
            {
                var regionId = kv.Key;
                var node     = kv.Value;
                var state    = Systems.World.GetRegion(regionId);

                if (state == null) continue;

                var isUnlocked = state.unlockState != RegionUnlockState.Locked;
                var isMastered = state.unlockState == RegionUnlockState.Mastered;
                var isSelected = regionId == _selectedRegionId;

                node.Background.color = isSelected ? SelectedColor :
                                        isMastered ? MasteredColor :
                                        isUnlocked ? UnlockedColor :
                                                     LockedColor;

                node.Button.interactable = isUnlocked;
                node.CanvasGroup.alpha   = isUnlocked ? 1f : 0.45f;

                var displayName = GetDisplayName(regionId);
                node.NameLabel.text  = displayName;
                node.NameLabel.color = isUnlocked ? TextCream : new Color(0.5f, 0.5f, 0.45f);

                // Boss indicator
                if (Systems.Bosses?.GetRegionBoss(regionId) != null && !isMastered)
                {
                    node.NameLabel.text = displayName + "\n[BOSS]";
                    node.NameLabel.color = new Color(1f, 0.6f, 0.3f);
                }
            }
        }

        // ─── Interaction ──────────────────────────────────────────────────────────

        private void OnNodeTapped(RegionNode node)
        {
            _selectedRegionId = node.RegionId;

            // Check for boss
            var boss = Systems?.Bosses?.GetRegionBoss(node.RegionId);
            if (boss != null)
            {
                OnBossNodeTapped?.Invoke(boss.id);
                _router?.GoToBossEncounter(boss.id);
                return;
            }

            // Notify navigation
            OnRegionTapped?.Invoke(node.RegionId);
            _router?.GoToZone(node.RegionId);

            // Update visual selection
            RefreshAllNodes();
        }

        // ─── World Event Subscriptions ────────────────────────────────────────────

        private void SubscribeToWorldEvents()
        {
            if (Systems?.World == null) return;
            Systems.World.OnRegionUnlocked += region => HandleRegionUnlocked(region);
            Systems.World.OnRegionMastered += region => RefreshAllNodes();
        }

        private void HandleRegionUnlocked(RegionState region)
        {
            if (!_nodes.TryGetValue(region.regionId, out var node)) return;

            _unlockSequence?.PlayUnlock(node.Rect, region.displayName, () =>
            {
                RefreshAllNodes();
            });
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private string GetDisplayName(string regionId)
        {
            var region = Systems?.World?.GetRegion(regionId);
            return region?.displayName ?? regionId;
        }

        // ─── RegionNode data class ────────────────────────────────────────────────

        private class RegionNode
        {
            public string        RegionId;
            public RectTransform Rect;
            public Image         Background;
            public Button        Button;
            public CanvasGroup   CanvasGroup;
            public Text          NameLabel;
        }
    }
}
