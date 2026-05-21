using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Parent-facing dashboard. Replaced the old "monetization text + gate" parents tab
    /// with a genuine child development insights panel.
    ///
    /// Shows:
    ///   - Cognitive skill scores (spatial / pattern / logic) from CognitiveAnalyticsSystem
    ///   - Total play metrics (attempts, clears, time)
    ///   - Learning milestones achieved
    ///   - Healthy playtime summary
    ///   - Premium gate (preserved from original)
    ///   - Accessibility settings entry point
    /// </summary>
    public class ParentDashboardController : PanelViewController
    {
        // ─── UI References ────────────────────────────────────────────────────────

        private Text   _totalClearsLabel;
        private Text   _avgTimeLabel;
        private Text   _frustrationLabel;
        private Text   _boredomLabel;
        private Text   _playtimeSummary;

        private SkillGrowthGraphRenderer _spatialGraph;
        private SkillGrowthGraphRenderer _patternGraph;
        private SkillGrowthGraphRenderer _logicGraph;

        private Button _accessibilityButton;
        private Button _premiumButton;
        private Button _resetButton;

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color HeaderColor   = new Color32(159, 216, 168, 255);
        private static readonly Color TextCream     = new Color32(248, 243, 223, 255);
        private static readonly Color TextAmber     = new Color32(245, 184, 92, 255);
        private static readonly Color PanelDark     = new Color(0.08f, 0.16f, 0.12f, 0.9f);
        private static readonly Color GreenBar      = new Color32(47, 122, 86, 255);
        private static readonly Color AmberBar      = new Color32(200, 140, 60, 255);
        private static readonly Color RedBar        = new Color32(180, 80, 60, 255);

        // ─── PanelViewController ──────────────────────────────────────────────────

        protected override void OnBuild()
        {
            BuildParentLayout();
        }

        protected override void OnRefresh(UIDirtyFlag dirtyFlags)
        {
            RefreshMetrics();
        }

        protected override void OnShow()
        {
            RefreshMetrics();
        }

        // ─── Layout ───────────────────────────────────────────────────────────────

        private void BuildParentLayout()
        {
            var scroll = ForestUiFactory.CreateUiObject("ParentScroll", RootTransform);
            ForestUiFactory.Stretch(scroll);
            ForestUiFactory.CreateScrollView(scroll, out var content);

            // ── Header ────────────────────────────────────────────────────────────
            var header = ForestUiFactory.CreateText(content, "Header",
                "Child Learning Report", ForestUiFactory.GetDefaultFont(), 34,
                HeaderColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(header.gameObject, preferredHeight: 56f);

            var subheader = ForestUiFactory.CreateText(content, "SubHeader",
                "Powered by CognitiveAnalytics — updates after every puzzle session.",
                ForestUiFactory.GetDefaultFont(), 20, new Color(0.65f, 0.8f, 0.65f),
                TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(subheader.gameObject, preferredHeight: 48f);

            // ── Skill Score Panel ─────────────────────────────────────────────────
            var skillPanel = CreateSectionPanel(content, "Cognitive Skills");
            var skillGrid = ForestUiFactory.CreateUiObject("SkillGrid", skillPanel.transform);
            var gridLayout = skillGrid.gameObject.AddComponent<VerticalLayoutGroup>();
            gridLayout.spacing = 12f;
            gridLayout.childForceExpandWidth  = true;
            gridLayout.childForceExpandHeight = false;
            skillGrid.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _spatialGraph = BuildSkillBar(skillGrid, "Spatial Intelligence",
                new Color(0.35f, 0.7f, 0.95f));
            _patternGraph = BuildSkillBar(skillGrid, "Pattern Recognition",
                new Color(0.7f, 0.9f, 0.35f));
            _logicGraph   = BuildSkillBar(skillGrid, "Logical Reasoning",
                new Color(0.9f, 0.7f, 0.35f));

            // ── Play Metrics ──────────────────────────────────────────────────────
            var metricsPanel = CreateSectionPanel(content, "Session Metrics");

            _totalClearsLabel = ForestUiFactory.CreateText(metricsPanel.transform, "Clears",
                "Puzzles completed: —", ForestUiFactory.GetDefaultFont(), 22,
                TextCream, TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(_totalClearsLabel.gameObject, preferredHeight: 32f);

            _avgTimeLabel = ForestUiFactory.CreateText(metricsPanel.transform, "AvgTime",
                "Average solve time: —", ForestUiFactory.GetDefaultFont(), 22,
                TextCream, TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(_avgTimeLabel.gameObject, preferredHeight: 32f);

            _frustrationLabel = ForestUiFactory.CreateText(metricsPanel.transform, "Frustration",
                "Difficulty comfort: —", ForestUiFactory.GetDefaultFont(), 22,
                TextCream, TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(_frustrationLabel.gameObject, preferredHeight: 32f);

            _playtimeSummary = ForestUiFactory.CreateText(metricsPanel.transform, "Playtime",
                "", ForestUiFactory.GetDefaultFont(), 20,
                new Color(0.7f, 0.85f, 0.7f), TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(_playtimeSummary.gameObject, minHeight: 56f, flexibleWidth: 1f);

            // ── Accessibility Button ───────────────────────────────────────────────
            _accessibilityButton = ForestUiFactory.CreateButton(content, "AccessibilityBtn",
                "Accessibility Settings", ForestUiFactory.GetDefaultFont(),
                new Color(0.25f, 0.55f, 0.4f), TextCream,
                () => Systems?.UIState?.GoTo(UIStateController.UIState.Accessibility),
                24);
            ForestUiFactory.AddLayout(_accessibilityButton.gameObject,
                preferredHeight: 64f, flexibleWidth: 1f);

            // ── Premium Gate ──────────────────────────────────────────────────────
            if (SaveData?.premiumUnlocked != true)
            {
                _premiumButton = ForestUiFactory.CreateButton(content, "PremiumBtn",
                    "Unlock Full Adventure (Parent Gate)",
                    ForestUiFactory.GetDefaultFont(),
                    TextAmber, new Color(0.1f, 0.2f, 0.1f),
                    HandlePremiumTap, 24);
                ForestUiFactory.AddLayout(_premiumButton.gameObject,
                    preferredHeight: 64f, flexibleWidth: 1f);
            }
        }

        // ─── Data Binding ─────────────────────────────────────────────────────────

        private void RefreshMetrics()
        {
            var report = Systems?.Analytics?.GetReport();
            if (report == null) return;

            // Skill bars
            _spatialGraph?.SetValue(report.spatialScore / 100f);
            _patternGraph?.SetValue(report.patternScore / 100f);
            _logicGraph?.SetValue(report.logicScore / 100f);

            // Text metrics
            if (_totalClearsLabel != null)
                _totalClearsLabel.text =
                    $"Puzzles completed: {report.totalClears} of {report.totalAttempts} attempts";

            if (_avgTimeLabel != null)
            {
                var avg = report.avgCompletionTime;
                _avgTimeLabel.text = avg > 0f
                    ? $"Average solve time: {avg:F0}s"
                    : "Average solve time: —";
            }

            if (_frustrationLabel != null)
            {
                var comfort = 1f - report.frustrationScore;
                var label   = comfort > 0.7f ? "Comfortable" :
                              comfort > 0.4f ? "Challenged"  : "Struggling";
                _frustrationLabel.text  = $"Difficulty comfort: {label}";
                _frustrationLabel.color = comfort > 0.7f ? GreenBar :
                                          comfort > 0.4f ? AmberBar  : RedBar;
            }

            if (_playtimeSummary != null)
            {
                var completed   = report.totalClears;
                var achievements = Systems?.Achievements?.GetUnlockedCount() ?? 0;
                _playtimeSummary.text =
                    $"Achievements: {achievements}/{Systems?.Achievements?.GetTotalCount() ?? 0}\n" +
                    $"Creatures bonded: {GetBondedCount()}/6";
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private RectTransform CreateSectionPanel(RectTransform parent, string title)
        {
            var header = ForestUiFactory.CreateText(parent, $"Header_{title}",
                title, ForestUiFactory.GetDefaultFont(), 26,
                HeaderColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(header.gameObject, preferredHeight: 44f);

            var panelGo = new GameObject($"Panel_{title}");
            panelGo.transform.SetParent(parent, false);
            var panelRt = panelGo.AddComponent<RectTransform>();
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = PanelDark;

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.padding    = new RectOffset(20, 20, 16, 16);
            layout.spacing    = 10f;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;
            panelGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            ForestUiFactory.AddLayout(panelGo, flexibleWidth: 1f);

            return panelRt;
        }

        private SkillGrowthGraphRenderer BuildSkillBar(RectTransform parent, string label, Color barColor)
        {
            var go = new GameObject($"Skill_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 48f);
            ForestUiFactory.AddLayout(go, preferredHeight: 48f, flexibleWidth: 1f);

            var renderer = go.AddComponent<SkillGrowthGraphRenderer>();
            renderer.Initialize(label, barColor, ForestUiFactory.GetDefaultFont());
            return renderer;
        }

        private int GetBondedCount()
        {
            if (Systems?.BondingEngine == null || Content?.characters == null) return 0;
            var count = 0;
            foreach (var c in Content.characters)
            {
                var bond = Systems.BondingEngine.GetBondState(c.id);
                if (bond != null && bond.bondLevel >= 2) count++;
            }
            return count;
        }

        private void HandlePremiumTap()
        {
            // Parent gate flow — kept from original design
            Debug.Log("[ParentDashboard] Parent gate tapped.");
        }
    }
}
