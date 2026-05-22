using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Renders all 6 guide characters in an animated showcase panel.
    /// Each character cycles through their emotion states automatically —
    /// producing a live "GIF" preview of the full cast with all animations.
    ///
    /// Usage: call Build(content, font, parent) once; the showcase runs forever.
    /// Attach this to any RectTransform to embed in any UI screen.
    /// </summary>
    public class CharacterShowcaseAnimator : MonoBehaviour
    {
        // Seconds each emotion state is held before transitioning
        private const float EmotionHoldTime   = 2.2f;
        // Seconds between characters in the active highlight rotation
        private const float CharacterCycleTime = 4.0f;
        // Seconds each character spends being "featured" (enlarged)
        private const float FeaturedHoldTime   = 3.5f;

        private static readonly string[] CharacterOrder = { "pip", "mimi", "tomo", "luma", "nori", "sol" };

        private static readonly CreatureEmotion[] EmotionCycle =
        {
            CreatureEmotion.Idle,
            CreatureEmotion.Happy,
            CreatureEmotion.Excited,
            CreatureEmotion.Curious,
            CreatureEmotion.Proud,
            CreatureEmotion.Playful,
            CreatureEmotion.Shy,
            CreatureEmotion.Sleepy,
        };

        // Per-character accent colors matching the JSON data
        private static readonly Dictionary<string, string> AccentHex = new Dictionary<string, string>
        {
            { "pip",  "#FFB36B" },
            { "mimi", "#F5D768" },
            { "tomo", "#8AD1A8" },
            { "luma", "#89E5F7" },
            { "nori", "#B8E8C8" },
            { "sol",  "#C5A3E8" },
        };

        private static readonly Dictionary<string, string> CharacterRole = new Dictionary<string, string>
        {
            { "pip",  "Forest Scout"    },
            { "mimi", "Song Bird"       },
            { "tomo", "Turtle Thinker"  },
            { "luma", "Firefly Spark"   },
            { "nori", "Deer Guardian"   },
            { "sol",  "Arch Druid Owl"  },
        };

        // ─── Runtime State ────────────────────────────────────────────────────────

        private Font   _font;
        private int    _featuredIndex;
        private int    _emotionIndex;
        private float  _emotionTimer;
        private float  _cycleTimer;

        private readonly Dictionary<string, RectTransform> _cards   = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, GuideCharacterView> _views = new Dictionary<string, GuideCharacterView>();
        private readonly Dictionary<string, Text>          _labels  = new Dictionary<string, Text>();

        private RectTransform _container;
        private Text          _featuredLabel;
        private Text          _emotionLabel;

        // ─── Public Builder ───────────────────────────────────────────────────────

        /// <summary>
        /// Build the showcase inside <paramref name="parent"/>.
        /// The showcase stretches to fill the parent rect and self-animates.
        /// </summary>
        public void Build(Font font, RectTransform parent)
        {
            _font = font;
            ForestUiFactory.ClearChildren(transform);

            var bg = ForestUiFactory.CreateImage(parent, "ShowcaseBG",
                new Color(0.06f, 0.16f, 0.12f, 0.97f));
            ForestUiFactory.Stretch(bg.rectTransform);

            // Title banner
            var banner = ForestUiFactory.CreateUiObject("Banner", bg.transform);
            banner.anchorMin = new Vector2(0f, 1f);
            banner.anchorMax = new Vector2(1f, 1f);
            banner.pivot     = new Vector2(0.5f, 1f);
            banner.anchoredPosition = Vector2.zero;
            banner.sizeDelta = new Vector2(0f, 110f);
            ForestUiFactory.AddHorizontalLayout(banner.gameObject, 16f, new RectOffset(20, 20, 16, 0));

            var titleText = ForestUiFactory.CreateText(banner, "Title",
                "Forest Friends — Guide Cast", font, 34,
                new Color(0.97f, 0.95f, 0.86f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            ForestUiFactory.AddLayout(titleText.gameObject, flexibleWidth: 1f);

            _emotionLabel = ForestUiFactory.CreateText(banner, "Emotion", "Idle",
                font, 26, new Color(0.55f, 0.88f, 0.65f, 1f), TextAnchor.MiddleRight, FontStyle.Bold);
            ForestUiFactory.AddLayout(_emotionLabel.gameObject, preferredWidth: 220f);

            // Featured character strip
            _featuredLabel = ForestUiFactory.CreateText(bg.transform, "FeaturedName", "",
                font, 42, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            var fl = _featuredLabel.rectTransform;
            fl.anchorMin = new Vector2(0f, 0.76f);
            fl.anchorMax = new Vector2(1f, 0.86f);
            fl.sizeDelta  = Vector2.zero;
            fl.anchoredPosition = Vector2.zero;

            // 6-character grid (2 rows × 3 columns)
            _container = ForestUiFactory.CreateUiObject("CharGrid", bg.transform);
            _container.anchorMin = new Vector2(0f, 0f);
            _container.anchorMax = new Vector2(1f, 0.76f);
            _container.sizeDelta  = Vector2.zero;
            _container.anchoredPosition = Vector2.zero;

            var grid = _container.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220f, 280f);
            grid.spacing  = new Vector2(16f, 16f);
            grid.padding  = new RectOffset(16, 16, 16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment  = TextAnchor.MiddleCenter;

            foreach (var id in CharacterOrder)
            {
                var accent = ForestUiFactory.FromHex(AccentHex[id], Color.white);
                BuildCharacterCard(id, accent);
            }

            // Kick off the animation loop
            StartCoroutine(AnimationLoop());
        }

        // ─── Card Builder ─────────────────────────────────────────────────────────

        private void BuildCharacterCard(string id, Color accent)
        {
            var cardGo = new GameObject($"Card_{id}");
            cardGo.transform.SetParent(_container, false);
            var card = cardGo.AddComponent<RectTransform>();
            _cards[id] = card;

            var bg = cardGo.AddComponent<Image>();
            bg.sprite = null;
            bg.color  = new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.22f, 0.92f);

            var layout = cardGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing              = 4f;
            layout.padding              = new RectOffset(8, 8, 8, 8);
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth  = true;
            layout.childControlHeight     = true;
            layout.childControlWidth      = true;

            // Avatar view holder
            var avatarHolder = ForestUiFactory.CreateUiObject("AvatarHolder", card);
            ForestUiFactory.AddLayout(avatarHolder.gameObject, preferredHeight: 186f);

            var profile = new CharacterProfile
            {
                id        = id,
                name      = char.ToUpper(id[0]) + id.Substring(1),
                accentHex = AccentHex[id],
            };

            var view = avatarHolder.gameObject.AddComponent<GuideCharacterView>();
            view.Build(profile, _font);
            _views[id] = view;

            // Name label
            var nameLabel = ForestUiFactory.CreateText(card, "Name",
                profile.name, _font, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            ForestUiFactory.AddLayout(nameLabel.gameObject, preferredHeight: 28f);
            _labels[id] = nameLabel;

            // Role label
            var roleLabel = ForestUiFactory.CreateText(card, "Role",
                CharacterRole[id], _font, 17,
                new Color(accent.r + 0.2f, accent.g + 0.2f, accent.b + 0.2f, 0.85f),
                TextAnchor.MiddleCenter);
            ForestUiFactory.AddLayout(roleLabel.gameObject, preferredHeight: 22f);

            // Animated accent bar at bottom of card
            var bar = ForestUiFactory.CreateImage(card, "Bar", accent);
            ForestUiFactory.AddLayout(bar.gameObject, preferredHeight: 5f);
            var pulse = bar.gameObject.AddComponent<PulseGlow>();
            pulse.speed    = 1.2f + id.GetHashCode() % 8 * 0.15f;
            pulse.minAlpha = 0.4f;
            pulse.maxAlpha = 1.0f;
        }

        // ─── Animation Loop ───────────────────────────────────────────────────────

        private IEnumerator AnimationLoop()
        {
            while (true)
            {
                yield return StartCoroutine(RunEmotionCycle());
            }
        }

        private IEnumerator RunEmotionCycle()
        {
            // Cycle through each emotion, holding for EmotionHoldTime
            for (var e = 0; e < EmotionCycle.Length; e++)
            {
                var emotion = EmotionCycle[e];
                if (_emotionLabel != null)
                    _emotionLabel.text = emotion.ToString();

                // Apply emotion animation to all character views
                ApplyEmotionToAllCards(emotion);

                // Highlight a featured character
                var featured = CharacterOrder[_featuredIndex % CharacterOrder.Length];
                UpdateFeaturedHighlight(featured);

                var elapsed = 0f;
                while (elapsed < EmotionHoldTime)
                {
                    elapsed += Time.deltaTime;

                    // Rotate featured character each CharacterCycleTime
                    _cycleTimer += Time.deltaTime;
                    if (_cycleTimer >= CharacterCycleTime)
                    {
                        _cycleTimer = 0f;
                        _featuredIndex = (_featuredIndex + 1) % CharacterOrder.Length;
                        featured = CharacterOrder[_featuredIndex];
                        UpdateFeaturedHighlight(featured);
                    }

                    yield return null;
                }
            }
        }

        private void ApplyEmotionToAllCards(CreatureEmotion emotion)
        {
            foreach (var kv in _cards)
                StartCoroutine(CardEmotionPulse(kv.Value, emotion));
        }

        private IEnumerator CardEmotionPulse(RectTransform card, CreatureEmotion emotion)
        {
            var baseScale = card.localScale;
            float targetScale;

            switch (emotion)
            {
                case CreatureEmotion.Excited: targetScale = 1.06f; break;
                case CreatureEmotion.Happy:   targetScale = 1.04f; break;
                case CreatureEmotion.Proud:   targetScale = 1.05f; break;
                case CreatureEmotion.Shy:     targetScale = 0.94f; break;
                case CreatureEmotion.Sleepy:  targetScale = 0.97f; break;
                default:                      targetScale = 1.00f; break;
            }

            // Ping-pong to target and back
            var duration = 0.4f;
            var elapsed  = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                if (card != null)
                    card.localScale = baseScale * Mathf.Lerp(1f, targetScale, t);
                yield return null;
            }

            if (card != null)
                card.localScale = baseScale;
        }

        private void UpdateFeaturedHighlight(string featuredId)
        {
            if (_featuredLabel != null)
            {
                var name = char.ToUpper(featuredId[0]) + featuredId.Substring(1);
                _featuredLabel.text = $"{name}  ·  {CharacterRole[featuredId]}";
                var accent = ForestUiFactory.FromHex(AccentHex[featuredId], Color.white);
                _featuredLabel.color = accent;
            }

            foreach (var kv in _cards)
            {
                var accent = ForestUiFactory.FromHex(AccentHex[kv.Key], Color.white);
                var isFeatured = kv.Key == featuredId;
                var bg = kv.Value.GetComponent<Image>();
                if (bg != null)
                {
                    bg.color = isFeatured
                        ? new Color(accent.r * 0.42f, accent.g * 0.42f, accent.b * 0.42f, 0.98f)
                        : new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.22f, 0.92f);
                }

                // Scale the featured card up slightly
                var cardRt = kv.Value;
                if (cardRt != null)
                    StartCoroutine(ScaleCard(cardRt,
                        isFeatured ? new Vector3(1.07f, 1.07f, 1f) : Vector3.one, 0.25f));
            }
        }

        private IEnumerator ScaleCard(RectTransform card, Vector3 target, float duration)
        {
            if (card == null) yield break;
            var start   = card.localScale;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (card != null)
                    card.localScale = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            if (card != null)
                card.localScale = target;
        }

        // ─── Static Factory ───────────────────────────────────────────────────────

        /// <summary>
        /// Instantiate a CharacterShowcaseAnimator inside <paramref name="parent"/> and start it.
        /// Returns the animator component so callers can Destroy/disable it later.
        /// </summary>
        public static CharacterShowcaseAnimator Create(Font font, RectTransform parent)
        {
            var go  = new GameObject("CharacterShowcase");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            ForestUiFactory.Stretch(rt);
            var anim = go.AddComponent<CharacterShowcaseAnimator>();
            anim.Build(font, rt);
            return anim;
        }
    }
}
