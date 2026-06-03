using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Creature interaction view. Shows creature roster, bond states, treat feeding,
    /// memory gallery, and triggers dialogue. Replaces the inline character section
    /// in ForestQuestApp.BuildWorldTab().
    ///
    /// Zero Destroy() calls — creature cards are pooled and re-bound on data change.
    /// </summary>
    public class CreatureInteractionController : PanelViewController
    {
        // ─── Dependencies ────────────────────────────────────────────────────────

        private CreatureMoodBrain       _moodBrain;
        private CreatureEvolutionRenderer _evolutionRenderer;

        // ─── UI Elements ─────────────────────────────────────────────────────────

        private ReusableCardPool _creatureCardPool;
        private Text             _treatCountLabel;

        // ─── Colors ───────────────────────────────────────────────────────────────

        private static readonly Color CardBase    = new Color32(248, 243, 223, 255);
        private static readonly Color TextDark    = new Color32(16, 35, 27, 255);
        private static readonly Color TextMoss    = new Color32(47, 122, 86, 255);
        private static readonly Color TextAmber   = new Color32(245, 184, 92, 255);
        private static readonly Color TextMint    = new Color32(159, 216, 168, 255);
        private static readonly Color BtnBg       = new Color32(33, 81, 62, 255);

        // ─── PanelViewController ──────────────────────────────────────────────────

        public void Configure(CreatureMoodBrain moodBrain, CreatureEvolutionRenderer evolutionRenderer)
        {
            _moodBrain        = moodBrain;
            _evolutionRenderer = evolutionRenderer;
        }

        protected override void OnBuild()
        {
            BuildCreatureLayout();
        }

        protected override void OnRefresh(UIDirtyFlag dirtyFlags)
        {
            if ((dirtyFlags & UIDirtyFlag.BondLevels) != 0 ||
                (dirtyFlags & UIDirtyFlag.Treats)     != 0 ||
                (dirtyFlags & UIDirtyFlag.Evolution)  != 0)
            {
                BindCreatureCards();
            }
        }

        protected override void OnShow()
        {
            BindCreatureCards();
        }

        // ─── Layout ───────────────────────────────────────────────────────────────

        private void BuildCreatureLayout()
        {
            var scroll = ForestUiFactory.CreateUiObject("CreatureScroll", RootTransform);
            ForestUiFactory.Stretch(scroll);
            ForestUiFactory.CreateScrollView(scroll, out var content);

            // Treat balance header
            var treatRow = ForestUiFactory.CreateUiObject("TreatRow", content);
            ForestUiFactory.AddHorizontalLayout(treatRow.gameObject, 16f);
            treatRow.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _treatCountLabel = ForestUiFactory.CreateText(treatRow, "TreatCount",
                "Forest Treats: 0", ForestUiFactory.GetDefaultFont(), 24,
                TextAmber, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(_treatCountLabel.gameObject, preferredHeight: 48f);

            // Creature pool container
            var header = ForestUiFactory.CreateText(content, "CreatureHeader",
                "Forest Friends", ForestUiFactory.GetDefaultFont(), 28,
                TextMint, TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(header.gameObject, preferredHeight: 48f);

            var container = ForestUiFactory.CreateUiObject("CreatureCards", content);
            var layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;
            container.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _creatureCardPool = new ReusableCardPool(container, maxCapacity: 8)
                .WithDefaultHeight(180f);
        }

        // ─── Data Binding ─────────────────────────────────────────────────────────

        private void BindCreatureCards()
        {
            if (Content?.characters == null) return;

            if (_treatCountLabel != null)
                _treatCountLabel.text = $"Forest Treats: {SaveData?.forestTreats ?? 0}";

            _creatureCardPool.Bind(
                Content.characters,
                (card, character, index) =>
                {
                    var accent      = ForestUiFactory.FromHex(character.accentHex, TextAmber);
                    var bondState   = Systems?.BondingEngine?.GetBondState(character.id);
                    var bondLevel   = bondState?.bondLevel ?? 1;
                    var mood        = bondState?.currentMood.ToString() ?? "Cozy";
                    var evolutionStage = Systems?.Evolution?.GetCurrentStage(character.id);
                    var displayName = evolutionStage != null ? evolutionStage.stageName : character.name;

                    card.SetBackground(new Color(0.1f, 0.22f, 0.16f));
                    card.SetAccentColor(accent);
                    card.SetTitle($"{displayName} · {character.role}", TextMint);
                    card.SetBody($"Bond Level {bondLevel} · {mood}", new Color(0.7f, 0.85f, 0.7f));
                    card.SetSubtitle(character.blurb, new Color(0.6f, 0.75f, 0.6f));
                    card.SetHeight(180f);

                    // Tap for detail
                    card.SetTapAction(() => ShowCreatureDetail(character.id));

                    BuildCreatureActionRow(card, character);
                });
        }

        private void BuildCreatureActionRow(ForestCard card, CharacterProfile character)
        {
            // Find or create the action row on this card
            var rowName = "ActionRow";
            var existingRow = card.Rect.Find(rowName);
            RectTransform rowRt;

            if (existingRow == null)
            {
                var rowGo = new GameObject(rowName);
                rowGo.transform.SetParent(card.Rect, false);
                rowRt = rowGo.AddComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0.04f, 0f);
                rowRt.anchorMax = new Vector2(0.96f, 0.38f);
                rowRt.sizeDelta = Vector2.zero;

                var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 10f;
                rowLayout.childForceExpandWidth  = false;
                rowLayout.childForceExpandHeight = true;
                rowGo.AddComponent<ContentSizeFitter>().verticalFit =
                    ContentSizeFitter.FitMode.PreferredSize;
            }
            else
            {
                rowRt = existingRow as RectTransform;
                // Clear existing buttons
                foreach (Transform child in rowRt) child.gameObject.SetActive(false);
            }

            // Hello button
            CreateSmallBtn(rowRt, "Hello",
                () => TriggerCue(character, "greeting"));

            // Hint button
            CreateSmallBtn(rowRt, "Hint",
                () => TriggerCue(character, "hint"));

            // Feed button
            var canFeed  = (SaveData?.forestTreats ?? 0) > 0;
            var feedLabel = $"Feed ({SaveData?.forestTreats ?? 0})";
            var feedBtn  = CreateSmallBtn(rowRt, feedLabel,
                canFeed ? () => FeedTreat(character) : (System.Action)null);
            if (feedBtn != null) feedBtn.interactable = canFeed;
        }

        private Button CreateSmallBtn(RectTransform parent, string label, System.Action onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 44f);

            var img = go.AddComponent<Image>();
            img.color = BtnBg;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var lbl = ForestUiFactory.CreateText(
                rt, "Label", label, ForestUiFactory.GetDefaultFont(),
                18, TextMint, TextAnchor.MiddleCenter);

            return btn;
        }

        // ─── Interactions ─────────────────────────────────────────────────────────

        private void ShowCreatureDetail(string creatureId)
        {
            // Trigger bond-adaptive dialogue
            var seq = Systems?.Dialogue?.GetAdaptedSequence(creatureId, "morning");
            if (seq != null) Systems.Dialogue.StartSequence(seq);
        }

        private void TriggerCue(CharacterProfile character, string cueType)
        {
            if (Systems?.Audio == null) return;
            // Play voice blip at character pitch
            // Full audio system handles this
        }

        private void FeedTreat(CharacterProfile character)
        {
            if (SaveData == null || SaveData.forestTreats <= 0) return;

            var lovedIt = false;
            if (Systems?.BondingEngine != null)
                Systems.BondingEngine.FeedTreat(character.id,
                    Systems.BondingEngine.GetFavoriteTreat(character.id),
                    out lovedIt);

            SaveData.forestTreats--;

            // VFX
            if (lovedIt)
                Systems?.VFX?.OnDiscovery(Vector2.zero);

            // Dirty cards
            MarkDirty(UIDirtyFlag.BondLevels | UIDirtyFlag.Treats);
            Systems?.SaveSystem?.Save(SaveData);
        }
    }
}
