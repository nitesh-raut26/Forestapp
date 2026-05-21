using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Generic UI card pool. Instead of Destroy()/Instantiate() on every data change,
    /// the pool keeps a fixed set of card GameObjects and rebinds their data in-place.
    ///
    /// Usage:
    ///   var pool = new ReusableCardPool(parent, maxCapacity: 20);
    ///   pool.Bind(dataList, (card, dataItem) => ConfigureCard(card, dataItem));
    ///   // Cards not needed for current data are hidden, not destroyed.
    ///
    /// CardData is a simple struct; configure the card in the bind callback.
    /// </summary>
    public class ReusableCardPool
    {
        private readonly RectTransform _parent;
        private readonly int           _maxCapacity;
        private readonly List<ForestCard> _cards = new List<ForestCard>();

        // Card visual settings
        private float _cardSpacing    = 16f;
        private float _cardPadding    = 18f;
        private float _defaultHeight  = 120f;

        public ReusableCardPool(RectTransform parent, int maxCapacity = 40)
        {
            _parent      = parent;
            _maxCapacity = maxCapacity;
        }

        public ReusableCardPool WithSpacing(float spacing)
        {
            _cardSpacing = spacing;
            return this;
        }

        public ReusableCardPool WithDefaultHeight(float height)
        {
            _defaultHeight = height;
            return this;
        }

        // ─── Bind ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Bind a list of data items to pooled cards.
        /// Grows the pool if needed (up to maxCapacity), hides excess cards.
        /// </summary>
        public void Bind<T>(IReadOnlyList<T> data, Action<ForestCard, T, int> configure)
        {
            var count = Mathf.Min(data.Count, _maxCapacity);

            // Grow pool as needed
            while (_cards.Count < count)
                _cards.Add(CreateCard());

            // Bind active cards
            for (var i = 0; i < count; i++)
            {
                _cards[i].SetVisible(true);
                configure(_cards[i], data[i], i);
            }

            // Hide excess
            for (var i = count; i < _cards.Count; i++)
                _cards[i].SetVisible(false);
        }

        /// <summary>Hide all cards (e.g., when parent panel hides).</summary>
        public void Clear()
        {
            foreach (var card in _cards)
                card.SetVisible(false);
        }

        // ─── Card Factory ─────────────────────────────────────────────────────────

        private ForestCard CreateCard()
        {
            var go = new GameObject($"PooledCard_{_cards.Count}");
            go.transform.SetParent(_parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(0f, _defaultHeight);
            rt.pivot     = new Vector2(0.5f, 1f);

            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.07f);

            var card = go.AddComponent<ForestCard>();
            card.Initialize(rt, img);
            return card;
        }
    }

    // ─── ForestCard — one pooled card view ────────────────────────────────────────

    /// <summary>
    /// A reusable card view. All label, button, and image references are cached
    /// from first build. Data updates only write to .text / .color — no Destroy().
    /// </summary>
    public class ForestCard : MonoBehaviour
    {
        public RectTransform Rect  { get; private set; }
        public Image         Background { get; private set; }

        // Pre-allocated sub-element references
        private Text   _titleLabel;
        private Text   _bodyLabel;
        private Text   _subtitleLabel;
        private Image  _accentBar;
        private Image  _badgeImage;
        private Button _tapButton;
        private List<Button> _actionButtons = new List<Button>();

        public void Initialize(RectTransform rect, Image background)
        {
            Rect       = rect;
            Background = background;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        // ─── Lazy Sub-element Accessors ───────────────────────────────────────────

        public Text TitleLabel => _titleLabel ??= CreateText("Title", 28, FontStyle.Bold);
        public Text BodyLabel  => _bodyLabel  ??= CreateText("Body", 22, FontStyle.Normal);
        public Text SubtitleLabel => _subtitleLabel ??= CreateText("Subtitle", 20, FontStyle.Normal);

        public Image AccentBar => _accentBar ??= CreateImage("AccentBar", new Vector2(4f, 0f));
        public Image BadgeImage => _badgeImage ??= CreateImage("Badge", new Vector2(40f, 40f));

        public Button TapButton => _tapButton ??= CreateTapButton();

        // ─── Convenience Setters ──────────────────────────────────────────────────

        public void SetTitle(string text, Color color)
        {
            TitleLabel.text  = text;
            TitleLabel.color = color;
        }

        public void SetBody(string text, Color color)
        {
            BodyLabel.text  = text;
            BodyLabel.color = color;
        }

        public void SetSubtitle(string text, Color color)
        {
            SubtitleLabel.text  = text;
            SubtitleLabel.color = color;
        }

        public void SetBackground(Color color)
        {
            Background.color = color;
        }

        public void SetAccentColor(Color color)
        {
            AccentBar.color   = color;
            AccentBar.gameObject.SetActive(true);
        }

        public void SetTapAction(Action onClick)
        {
            TapButton.onClick.RemoveAllListeners();
            if (onClick != null) TapButton.onClick.AddListener(() => onClick());
        }

        public void SetHeight(float height)
        {
            Rect.sizeDelta = new Vector2(Rect.sizeDelta.x, height);
        }

        // ─── Private Builders ─────────────────────────────────────────────────────

        private Text CreateText(string name, int fontSize, FontStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.04f, 0f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.sizeDelta = Vector2.zero;

            var t = go.AddComponent<Text>();
            t.font      = ForestUiFactory.GetDefaultFont();
            t.fontSize  = fontSize;
            t.fontStyle = style;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        private Image CreateImage(string name, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            return img;
        }

        private Button CreateTapButton()
        {
            var img = Background;
            var btn = gameObject.GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }
    }
}
