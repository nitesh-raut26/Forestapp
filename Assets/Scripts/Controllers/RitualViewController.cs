using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Daily Ritual tab view controller.
    ///
    /// Surfaces DailyRitualSystem to players for the first time.
    /// Shows today's ritual, streak, upcoming preview, and completion state.
    /// Previously this entire system was invisible — this controller fixes that.
    /// </summary>
    public class RitualViewController : PanelViewController
    {
        // ─── UI References (never destroyed, only updated) ────────────────────────

        private Image  _heroCard;
        private Text   _ritualTitleLabel;
        private Text   _ritualDescLabel;
        private Text   _ritualRewardLabel;
        private Text   _streakLabel;
        private Button _completeButton;
        private Image  _completeButtonBg;
        private Text   _completeButtonLabel;

        private RectTransform _upcomingSection;
        private ReusableCardPool _upcomingPool;

        private RectTransform _ritualIconArea;
        private Image         _ritualTypeIcon;

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color CompletedColor  = new Color32(47, 122, 86, 255);
        private static readonly Color PendingColor    = new Color32(245, 184, 92, 255);
        private static readonly Color HeroBackground  = new Color32(15, 45, 30, 255);
        private static readonly Color TextCream       = new Color32(248, 243, 223, 255);
        private static readonly Color TextMint        = new Color32(159, 216, 168, 255);
        private static readonly Color TextAmber       = new Color32(245, 184, 92, 255);

        // ─── PanelViewController Overrides ────────────────────────────────────────

        protected override void OnBuild()
        {
            BuildRitualLayout();
        }

        protected override void OnRefresh(UIDirtyFlag dirtyFlags)
        {
            if ((dirtyFlags & UIDirtyFlag.Rituals) != 0 ||
                (dirtyFlags & UIDirtyFlag.All) != UIDirtyFlag.None)
            {
                RefreshRitualDisplay();
            }
        }

        protected override void OnShow()
        {
            RefreshRitualDisplay();
            StartCoroutine(AnimateEntrance());
        }

        // ─── Layout Construction ──────────────────────────────────────────────────

        private void BuildRitualLayout()
        {
            var scroll = ForestUiFactory.CreateUiObject("RitualScroll", RootTransform);
            ForestUiFactory.Stretch(scroll);
            ForestUiFactory.CreateScrollView(scroll, out var content);

            // ── Hero Card ──────────────────────────────────────────────────────────
            var heroGo = new GameObject("RitualHero");
            heroGo.transform.SetParent(content, false);
            var heroRt = heroGo.AddComponent<RectTransform>();
            heroRt.sizeDelta = new Vector2(0f, 280f);
            _heroCard = heroGo.AddComponent<Image>();
            _heroCard.color = HeroBackground;
            ForestUiFactory.AddLayout(heroGo, preferredHeight: 280f, flexibleWidth: 1f);

            var heroLayout = heroGo.AddComponent<VerticalLayoutGroup>();
            heroLayout.padding    = new RectOffset(24, 24, 20, 20);
            heroLayout.spacing    = 14f;
            heroLayout.childForceExpandWidth  = true;
            heroLayout.childForceExpandHeight = false;

            // Streak
            _streakLabel = ForestUiFactory.CreateText(heroGo.transform, "Streak",
                "Day Streak: 0", ForestUiFactory.GetDefaultFont(), 22,
                TextAmber, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(_streakLabel.gameObject, preferredHeight: 32f);

            // Ritual title
            _ritualTitleLabel = ForestUiFactory.CreateText(heroGo.transform, "RitualTitle",
                "Loading ritual...", ForestUiFactory.GetDefaultFont(), 34,
                TextCream, TextAnchor.UpperLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(_ritualTitleLabel.gameObject, preferredHeight: 80f);

            // Description
            _ritualDescLabel = ForestUiFactory.CreateText(heroGo.transform, "RitualDesc",
                "", ForestUiFactory.GetDefaultFont(), 22,
                new Color(0.75f, 0.9f, 0.75f), TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(_ritualDescLabel.gameObject, minHeight: 72f, flexibleWidth: 1f);

            // Reward
            _ritualRewardLabel = ForestUiFactory.CreateText(heroGo.transform, "RitualReward",
                "", ForestUiFactory.GetDefaultFont(), 20,
                TextAmber, TextAnchor.UpperLeft);
            ForestUiFactory.AddLayout(_ritualRewardLabel.gameObject, preferredHeight: 32f);

            // ── Complete Button ────────────────────────────────────────────────────
            var btnGo = new GameObject("CompleteButton");
            btnGo.transform.SetParent(content, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            _completeButtonBg    = btnGo.AddComponent<Image>();
            _completeButtonBg.color = PendingColor;
            _completeButton      = btnGo.AddComponent<Button>();
            _completeButton.targetGraphic = _completeButtonBg;
            _completeButton.onClick.AddListener(HandleCompleteRitual);
            ForestUiFactory.AddLayout(btnGo, preferredHeight: 72f, flexibleWidth: 1f);

            _completeButtonLabel = ForestUiFactory.CreateText(btnGo.transform, "BtnLabel",
                "Complete Today's Ritual", ForestUiFactory.GetDefaultFont(), 26,
                new Color(0.1f, 0.2f, 0.15f), TextAnchor.MiddleCenter, FontStyle.Bold);
            ForestUiFactory.AddLayout(_completeButtonLabel.gameObject,
                preferredHeight: 72f, flexibleWidth: 1f);

            // ── Upcoming Section ──────────────────────────────────────────────────
            var upcomingHeader = ForestUiFactory.CreateText(content, "UpcomingHeader",
                "Coming Up", ForestUiFactory.GetDefaultFont(), 26,
                TextMint, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(upcomingHeader.gameObject, preferredHeight: 48f);

            var upcomingContainer = ForestUiFactory.CreateUiObject("UpcomingCards", content);
            var upcomingLayout = upcomingContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            upcomingLayout.spacing = 10f;
            upcomingLayout.childForceExpandWidth  = true;
            upcomingLayout.childForceExpandHeight = false;
            upcomingContainer.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _upcomingPool = new ReusableCardPool(upcomingContainer, maxCapacity: 3)
                .WithDefaultHeight(90f);
        }

        // ─── Data Binding ─────────────────────────────────────────────────────────

        private void RefreshRitualDisplay()
        {
            var ritualSystem = Systems?.DailyRitual;
            if (ritualSystem == null) return;

            var today     = ritualSystem.GetTodaysRitual();
            var completed = ritualSystem.IsTodaysRitualComplete();

            // Header info
            _ritualTitleLabel.text = today.title;
            _ritualDescLabel.text  = today.description;
            _ritualRewardLabel.text = $"Reward: {today.rewardDescription}";

            // Streak
            var streak = GetStreak();
            _streakLabel.text = streak > 1
                ? $"Day Streak: {streak}"
                : "Start your daily streak!";

            // Complete button state
            if (completed)
            {
                _completeButtonBg.color    = CompletedColor;
                _completeButtonLabel.text  = "Ritual Complete!";
                _completeButton.interactable = false;
            }
            else
            {
                _completeButtonBg.color    = PendingColor;
                _completeButtonLabel.text  = "Complete Today's Ritual";
                _completeButton.interactable = true;
            }

            // Upcoming rituals
            var upcoming = ritualSystem.GetUpcomingRituals(3);
            _upcomingPool.Bind(upcoming, (card, ritual, index) =>
            {
                card.SetBackground(new Color(0.08f, 0.18f, 0.13f, 0.9f));
                card.SetTitle($"Tomorrow +{index + 1}: {ritual.title}",
                    new Color(0.8f, 0.85f, 0.75f));
                card.SetBody(ritual.rewardDescription, new Color(0.6f, 0.8f, 0.6f));
                card.SetHeight(90f);
            });
        }

        // ─── Interaction ──────────────────────────────────────────────────────────

        private void HandleCompleteRitual()
        {
            var ritualSystem = Systems?.DailyRitual;
            if (ritualSystem == null) return;
            if (ritualSystem.IsTodaysRitualComplete()) return;

            var treats = ritualSystem.CompleteRitual(SaveData);

            // VFX celebration
            Systems?.VFX?.OnDiscovery(Vector2.zero);

            // Update streak
            IncrementStreak();

            // Refresh UI to show completed state
            RefreshRitualDisplay();

            // Dirty treats so other panels update currency
            Systems?.SaveSystem?.Save(SaveData);
        }

        // ─── Streak Persistence ───────────────────────────────────────────────────

        private int GetStreak()
        {
            return PlayerPrefs.GetInt("FFQ.Streak.Count", 0);
        }

        private void IncrementStreak()
        {
            var lastDate = PlayerPrefs.GetString("FFQ.Streak.LastDate", "");
            var today    = System.DateTime.Today.ToString("yyyyMMdd");
            var yesterday = System.DateTime.Today.AddDays(-1).ToString("yyyyMMdd");

            var streak = PlayerPrefs.GetInt("FFQ.Streak.Count", 0);

            if (lastDate == today)
            {
                return; // Already incremented today
            }
            else if (lastDate == yesterday)
            {
                streak++; // Consecutive day
            }
            else
            {
                streak = 1; // Reset streak
            }

            PlayerPrefs.SetInt("FFQ.Streak.Count", streak);
            PlayerPrefs.SetString("FFQ.Streak.LastDate", today);
            PlayerPrefs.Save();

            // Check streak achievements
            if (streak >= 7)  Systems?.Achievements?.TryUnlock("sea_daily_7");
            if (streak >= 30) Systems?.Achievements?.TryUnlock("sea_daily_30");
        }

        // ─── Entrance Animation ───────────────────────────────────────────────────

        private IEnumerator AnimateEntrance()
        {
            if (_heroCard == null) yield break;

            var rt = _heroCard.GetComponent<RectTransform>();
            rt.localScale = new Vector3(0.96f, 0.96f, 1f);

            var elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / 0.3f);
                var ease = 1f - Mathf.Pow(1f - t, 3f);
                rt.localScale = Vector3.Lerp(new Vector3(0.96f, 0.96f, 1f), Vector3.one, ease);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}
