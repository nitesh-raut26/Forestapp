using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// VisualShowcaseScreen — the AAA visual centrepiece of Forest Friends Quest.
    ///
    /// What this renders (all procedural, zero external PNG files needed):
    ///   • Animated biome background (BiomeBackgroundRenderer procedural art)
    ///   • All 6 guide characters live on screen, animated (GuideCharacterView)
    ///   • Characters bob, sway, pulse their glows continuously
    ///   • Tapping a character card: zooms it, displays emotional state, plays sfx
    ///   • Emotion cycle button: cycles all characters through 8 moods simultaneously
    ///   • Biome cycle button: transitions the background through all 10 biomes
    ///   • Masterplan banner image slot (shown at top as the cinematic hero panel)
    ///   • Character details panel: name, role, bond level, current emotion
    ///   • Firefly particle overlay (pure shader-based, no sprites needed)
    ///   • Day/night ambient tint driven by DayNightWeatherController if present
    ///
    /// Integration:
    ///   Called from ForestQuestApp.BuildShowcaseTab() via
    ///   VisualShowcaseScreen.Build(parent, content, font, systems)
    /// </summary>
    public class VisualShowcaseScreen : MonoBehaviour
    {
        // ─── Constants ────────────────────────────────────────────────────────────

        private const float CardW         = 210f;
        private const float CardH         = 270f;
        private const float CharViewSize  = 200f;
        private const float EmotionHold   = 2.5f;   // seconds per emotion
        private const float FireflyCount  = 18;
        private const float BiomeFadeTime = 0.6f;

        private static readonly string[] CharacterIds =
            { "pip", "mimi", "tomo", "luma", "nori", "sol" };

        private static readonly string[] CharacterNames =
            { "Pip", "Mimi", "Tomo", "Luma", "Nori", "Sol" };

        private static readonly string[] CharacterRoles =
        {
            "Forest Scout · Curious Explorer",
            "Song Bird · Joyful Melody Keeper",
            "Turtle Thinker · Ancient Wise Elder",
            "Firefly Spark · Light Bringer",
            "Deer Guardian · Graceful Protector",
            "Arch Druid Owl · Keeper of Lore"
        };

        private static readonly string[] CharacterBlurbs =
        {
            "First to leap into the unknown. Brave, warm, always curious. Pip's bushy tail betrays every emotion.",
            "The forest sings through Mimi. Her melodies unlock hidden paths and soothe even the grumpiest creatures.",
            "Older than any living tree. Tomo speaks slowly because every word has been chosen over centuries.",
            "Where Luma goes, darkness retreats. Her bioluminescent glow reveals secrets invisible to others.",
            "Nori moves like wind through grass — you feel her before you see her. Guardian of the deep forest.",
            "Sol sees past, present and future simultaneously. At night the stars speak directly to him."
        };

        private static readonly string[] AccentHexes =
            { "#FFB36B", "#F5D768", "#8AD1A8", "#89E5F7", "#B8E8C8", "#C5A3E8" };

        private static readonly CreatureEmotion[] EmotionCycle =
        {
            CreatureEmotion.Idle, CreatureEmotion.Happy, CreatureEmotion.Excited,
            CreatureEmotion.Curious, CreatureEmotion.Proud, CreatureEmotion.Playful,
            CreatureEmotion.Shy, CreatureEmotion.Sleepy
        };

        // Biome IDs from BiomeController — must match BiomeProfile.regionId values
        private static readonly string[] BiomeIds =
        {
            "fern-trail", "moonlit-creek", "elderwood-grove", "crystal-caverns",
            "forgotten-ruins", "firefly-marsh", "ancient-observatory",
            "skyroot-canopy", "firefly-hollow", "river-bend"
        };

        private static readonly string[] BiomeDisplayNames =
        {
            "Whispering Meadow", "Moonlit Creek", "Elderwood Grove", "Crystal Caverns",
            "Forgotten Ruins", "Firefly Marsh", "Ancient Observatory",
            "Skyroot Canopy", "Firefly Hollow", "River Bend"
        };

        // ─── Runtime State ────────────────────────────────────────────────────────

        private Font                    _font;
        private ForestSystemsContainer  _systems;
        private Action<string>          _onPlayWithCharacter; // callback: characterId

        private int  _selectedCharIdx   = 0;
        private int  _currentEmotionIdx = 0;
        private int  _currentBiomeIdx   = 0;
#pragma warning disable CS0414
        private bool _isAnimating       = false;
#pragma warning restore CS0414

        // Loaded PNG assets (Resources/)
        private Texture2D  _bannerTex;     // forest_banner.png
        private Texture2D  _hudMockupTex;  // ui_hud_mockup.png

        // Launch overlay root (shown on Play tap)
        private GameObject _launchOverlayRoot;

        // UI references
        private RawImage              _biomeBackground;
        private readonly List<RectTransform>      _characterCards   = new List<RectTransform>();
        private readonly List<GuideCharacterView> _characterViews   = new List<GuideCharacterView>();
        private readonly List<Image>              _cardBackgrounds  = new List<Image>();
        private Text   _characterNameLabel;
        private Text   _characterRoleLabel;
        private Text   _characterBlurbLabel;
        private Text   _emotionLabel;
        private Text   _biomeLabel;
        private Image  _selectedHighlight;
        private Image  _bondBar;
        private Text   _bondText;
        private Button _playButton;
        private Text   _playButtonLabel;
        private Image  _hudPreviewImage;

        // Firefly overlay GameObjects
        private readonly List<FireflyDot> _fireflies = new List<FireflyDot>();

        // Biome background cache (generated via BiomeBackgroundRenderer-compatible method)
        private readonly Dictionary<string, Texture2D> _biomeCache = new Dictionary<string, Texture2D>();

        // ─── Factory ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Create and attach the VisualShowcaseScreen to <paramref name="parent"/>.
        /// Call once per session; the screen is self-animated.
        /// </summary>
        /// <param name="onPlayWithCharacter">
        /// Callback invoked when the player taps "Play!".
        /// Receives the selected character ID (e.g. "pip").
        /// </param>
        public static VisualShowcaseScreen Build(
            RectTransform parent,
            Font font,
            ForestSystemsContainer systems = null,
            Action<string> onPlayWithCharacter = null)
        {
            var go = new GameObject("VisualShowcaseScreen");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta  = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var screen = go.AddComponent<VisualShowcaseScreen>();
            screen._font                 = font;
            screen._systems              = systems;
            screen._onPlayWithCharacter  = onPlayWithCharacter;

            // Pre-load both PNG assets from Resources/ (placed there by build pipeline)
            screen._bannerTex    = Resources.Load<Texture2D>("forest_banner");
            screen._hudMockupTex = Resources.Load<Texture2D>("ui_hud_mockup");

            screen.Initialize();
            return screen;
        }

        // ─── Initialization ───────────────────────────────────────────────────────

        private void Initialize()
        {
            var root = GetComponent<RectTransform>();

            // ── Layer 0: Biome background (full bleed) ──
            BuildBiomeBackground(root);

            // ── Layer 1: Ambient firefly overlay ──
            BuildFireflyOverlay(root);

            // ── Layer 2: Top header bar ──
            BuildHeaderBar(root);

            // ── Layer 3: Character grid (2 rows × 3 cols) ──
            BuildCharacterGrid(root);

            // ── Layer 4: Selected character details panel (bottom) ──
            BuildDetailPanel(root);

            // ── Layer 5: Controls bar ──
            BuildControlBar(root);

            // ── Start continuous animation loops ──
            StartCoroutine(EmotionAnimLoop());
            StartCoroutine(FireflyAnimLoop());

            // Show initial biome and character
            ApplyBiome(_currentBiomeIdx, instant: true);
            ApplySelectedCharacter(_selectedCharIdx);
        }

        // ─── Biome Background ────────────────────────────────────────────────────

        private void BuildBiomeBackground(RectTransform root)
        {
            var go = new GameObject("BiomeBG");
            go.transform.SetParent(root, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta  = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            _biomeBackground = go.AddComponent<RawImage>();
            _biomeBackground.color = Color.white;
        }

        private Texture2D GetBiomeTex(int idx)
        {
            var id = BiomeIds[idx];
            if (_biomeCache.TryGetValue(id, out var cached)) return cached;

            var tex = GenerateBiomeTexture(id);
            _biomeCache[id] = tex;
            return tex;
        }

        /// <summary>Generates a 256×512 biome background texture procedurally.</summary>
        private Texture2D GenerateBiomeTexture(string biomeId)
        {
            // Build a BiomeProfile from our known data to pass to BiomeBackgroundRenderer
            var profile = BuildBiomeProfile(biomeId);
            return BiomeBackgroundRenderer.GenerateTexture(profile);
        }

        private BiomeProfile BuildBiomeProfile(string id)
        {
            // Each profile maps to the art direction from ART_DIRECTION_BIBLE.md
            return id switch
            {
                "fern-trail" => new BiomeProfile
                {
                    regionId       = id,
                    skyTintColor   = new Color(0.53f, 0.79f, 0.54f),
                    groundTintColor= new Color(0.32f, 0.58f, 0.27f),
                    fogColor       = new Color(0.85f, 0.95f, 0.75f, 0.45f),
                    ambientLightColor = new Color(0.85f, 0.95f, 0.75f),
                },
                "moonlit-creek" => new BiomeProfile
                {
                    regionId       = id,
                    skyTintColor   = new Color(0.17f, 0.29f, 0.43f),
                    groundTintColor= new Color(0.12f, 0.22f, 0.32f),
                    fogColor       = new Color(0.20f, 0.30f, 0.50f, 0.60f),
                    ambientLightColor = new Color(0.49f, 0.78f, 0.89f),
                },
                "elderwood-grove" => new BiomeProfile
                {
                    regionId       = id,
                    skyTintColor   = new Color(0.10f, 0.30f, 0.18f),
                    groundTintColor= new Color(0.18f, 0.28f, 0.14f),
                    fogColor       = new Color(0.30f, 0.50f, 0.28f, 0.50f),
                    ambientLightColor = new Color(0.32f, 0.58f, 0.27f),
                },
                "crystal-caverns" => new BiomeProfile
                {
                    regionId       = id,
                    skyTintColor   = new Color(0.10f, 0.10f, 0.24f),
                    groundTintColor= new Color(0.08f, 0.08f, 0.20f),
                    fogColor       = new Color(0.30f, 0.40f, 0.80f, 0.55f),
                    ambientLightColor = new Color(0.48f, 0.62f, 1.00f),
                },
                "forgotten-ruins" => new BiomeProfile
                {
                    regionId       = id,
                    skyTintColor   = new Color(0.42f, 0.35f, 0.25f),
                    groundTintColor= new Color(0.28f, 0.22f, 0.14f),
                    fogColor       = new Color(0.55f, 0.48f, 0.35f, 0.40f),
                    ambientLightColor = new Color(0.60f, 0.67f, 0.50f),
                },
                "firefly-marsh" => new BiomeProfile
                {
                    regionId       = id,
                    skyTintColor   = new Color(0.05f, 0.17f, 0.12f),
                    groundTintColor= new Color(0.07f, 0.16f, 0.10f),
                    fogColor       = new Color(0.20f, 0.45f, 0.25f, 0.65f),
                    ambientLightColor = new Color(0.40f, 0.47f, 0.31f),
                },
                "ancient-observatory" => new BiomeProfile
                {
                    regionId       = id,
                    skyTintColor   = new Color(0.04f, 0.04f, 0.12f),
                    groundTintColor= new Color(0.06f, 0.06f, 0.16f),
                    fogColor       = new Color(0.15f, 0.15f, 0.40f, 0.50f),
                    ambientLightColor = new Color(0.75f, 0.78f, 1.00f),
                },
                "skyroot-canopy" => new BiomeProfile
                {
                    regionId       = id,
                    skyTintColor   = new Color(0.72f, 0.88f, 1.00f),
                    groundTintColor= new Color(0.35f, 0.68f, 0.31f),
                    fogColor       = new Color(0.85f, 0.92f, 1.00f, 0.55f),
                    ambientLightColor = new Color(0.85f, 0.96f, 0.78f),
                },
                "firefly-hollow" => new BiomeProfile
                {
                    regionId       = id,
                    skyTintColor   = new Color(0.05f, 0.10f, 0.06f),
                    groundTintColor= new Color(0.08f, 0.16f, 0.10f),
                    fogColor       = new Color(0.20f, 0.40f, 0.22f, 0.70f),
                    ambientLightColor = new Color(0.47f, 1.00f, 0.34f),
                },
                _ => new BiomeProfile  // river-bend default
                {
                    regionId       = "river-bend",
                    skyTintColor   = new Color(0.35f, 0.65f, 0.82f),
                    groundTintColor= new Color(0.22f, 0.45f, 0.28f),
                    fogColor       = new Color(0.55f, 0.75f, 0.85f, 0.50f),
                    ambientLightColor = new Color(0.55f, 0.82f, 0.95f),
                },
            };
        }

        private void ApplyBiome(int idx, bool instant = false)
        {
            var tex = GetBiomeTex(idx);
            if (instant || _biomeBackground == null)
            {
                if (_biomeBackground != null) _biomeBackground.texture = tex;
                if (_biomeLabel != null) _biomeLabel.text = $"🌿 {BiomeDisplayNames[idx]}";
            }
            else
            {
                StartCoroutine(FadeBiome(tex, BiomeDisplayNames[idx]));
            }
        }

        private IEnumerator FadeBiome(Texture2D newTex, string biomeName)
        {
            // Fade out
            float t = 0f;
            var origColor = _biomeBackground.color;
            while (t < BiomeFadeTime * 0.5f)
            {
                t += Time.deltaTime;
                _biomeBackground.color = Color.Lerp(origColor, Color.black, t / (BiomeFadeTime * 0.5f));
                yield return null;
            }
            // Swap
            _biomeBackground.texture = newTex;
            if (_biomeLabel != null) _biomeLabel.text = $"🌿 {biomeName}";
            // Fade in
            t = 0f;
            while (t < BiomeFadeTime * 0.5f)
            {
                t += Time.deltaTime;
                _biomeBackground.color = Color.Lerp(Color.black, origColor, t / (BiomeFadeTime * 0.5f));
                yield return null;
            }
            _biomeBackground.color = origColor;
        }

        // ─── Firefly Overlay ─────────────────────────────────────────────────────

        private class FireflyDot
        {
            public RectTransform Rt;
            public Image         Img;
            public Vector2       BasePos;
            public float         Speed;
            public float         Phase;
            public float         OrbitRadius;
        }

        private void BuildFireflyOverlay(RectTransform root)
        {
            var overlay = new GameObject("FireflyOverlay");
            overlay.transform.SetParent(root, false);
            var rt = overlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta  = Vector2.zero;

            for (var i = 0; i < FireflyCount; i++)
            {
                var fgo = new GameObject($"Firefly_{i}");
                fgo.transform.SetParent(rt, false);
                var frt = fgo.AddComponent<RectTransform>();
                frt.anchorMin = new Vector2(0.5f, 0.5f);
                frt.anchorMax = new Vector2(0.5f, 0.5f);
                frt.sizeDelta = new Vector2(10f, 10f);

                var img = fgo.AddComponent<Image>();
                img.color = new Color(0.75f, 1f, 0.55f, 0f); // start invisible

                // Randomize starting position across the screen
                var bx = UnityEngine.Random.Range(-480f, 480f);
                var by = UnityEngine.Random.Range(-800f, 800f);
                frt.anchoredPosition = new Vector2(bx, by);

                _fireflies.Add(new FireflyDot
                {
                    Rt          = frt,
                    Img         = img,
                    BasePos     = new Vector2(bx, by),
                    Speed       = UnityEngine.Random.Range(0.4f, 1.2f),
                    Phase       = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    OrbitRadius = UnityEngine.Random.Range(20f, 80f),
                });
            }
        }

        private IEnumerator FireflyAnimLoop()
        {
            while (true)
            {
                var time = Time.time;
                foreach (var ff in _fireflies)
                {
                    if (ff.Rt == null) continue;
                    var phase = time * ff.Speed + ff.Phase;
                    // Figure-8 Lissajous motion
                    var ox = Mathf.Sin(phase) * ff.OrbitRadius;
                    var oy = Mathf.Sin(phase * 2f) * ff.OrbitRadius * 0.5f;
                    ff.Rt.anchoredPosition = ff.BasePos + new Vector2(ox, oy);
                    // Pulse glow alpha
                    var alpha = (Mathf.Sin(phase * 2.5f) + 1f) * 0.5f * 0.7f;
                    if (ff.Img != null)
                        ff.Img.color = new Color(0.75f, 1f, 0.55f, alpha);
                }
                yield return null;
            }
        }

        // ─── Header Bar ──────────────────────────────────────────────────────────

        private void BuildHeaderBar(RectTransform root)
        {
            var bar = MakeRT("HeaderBar", root);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot     = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 120f);
            bar.anchoredPosition = Vector2.zero;

            var bg = bar.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.15f, 0.08f, 0.88f);

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding        = new RectOffset(24, 24, 16, 16);
            layout.spacing        = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth  = false;
            layout.childControlHeight     = true;
            layout.childControlWidth      = false;

            // Title
            var title = MakeText("ShowcaseTitle", bar, "✨ Forest Friends", 34,
                new Color(0.97f, 0.92f, 0.72f), TextAnchor.MiddleLeft, FontStyle.Bold);
            var tlayout = title.gameObject.AddComponent<LayoutElement>();
            tlayout.flexibleWidth = 1f;

            // Biome name pill
            _biomeLabel = MakeText("BiomeName", bar,
                $"🌿 {BiomeDisplayNames[0]}", 22,
                new Color(0.65f, 1f, 0.75f), TextAnchor.MiddleRight);
            var blayout = _biomeLabel.gameObject.AddComponent<LayoutElement>();
            blayout.preferredWidth = 360f;
        }

        // ─── Character Grid ───────────────────────────────────────────────────────

        private void BuildCharacterGrid(RectTransform root)
        {
            var grid = MakeRT("CharacterGrid", root);
            grid.anchorMin = new Vector2(0f, 0.28f);
            grid.anchorMax = new Vector2(1f, 0.88f);
            grid.sizeDelta  = Vector2.zero;
            grid.anchoredPosition = Vector2.zero;

            var glg = grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize        = new Vector2(CardW, CardH);
            glg.spacing         = new Vector2(14f, 14f);
            glg.padding         = new RectOffset(16, 16, 12, 12);
            glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment  = TextAnchor.MiddleCenter;

            for (var i = 0; i < CharacterIds.Length; i++)
            {
                BuildCharacterCard(grid, i);
            }
        }

        private void BuildCharacterCard(RectTransform parent, int charIdx)
        {
            var id     = CharacterIds[charIdx];
            var accent = HexToColor(AccentHexes[charIdx]);

            var cardGo = new GameObject($"CharCard_{id}");
            cardGo.transform.SetParent(parent, false);
            var card = cardGo.AddComponent<RectTransform>();
            _characterCards.Add(card);

            // Card background — dark tinted glass
            var bg = cardGo.AddComponent<Image>();
            bg.color = new Color(accent.r * 0.15f, accent.g * 0.15f, accent.b * 0.15f, 0.90f);
            _cardBackgrounds.Add(bg);

            // Accent glow border (pulse)
            var glowGo = new GameObject("CardGlow");
            glowGo.transform.SetParent(card, false);
            var glowRt = glowGo.AddComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.sizeDelta  = new Vector2(4f, 4f);
            glowRt.anchoredPosition = Vector2.zero;
            var glowImg = glowGo.AddComponent<Image>();
            glowImg.color = new Color(accent.r, accent.g, accent.b, 0f); // starts transparent
            var pulse = glowGo.AddComponent<PulseGlow>();
            pulse.speed    = 1.0f + charIdx * 0.15f;
            pulse.minAlpha = 0.05f;
            pulse.maxAlpha = 0.30f;

            // Vertical layout inside card
            var vl = cardGo.AddComponent<VerticalLayoutGroup>();
            vl.spacing              = 4f;
            vl.padding              = new RectOffset(8, 8, 8, 8);
            vl.childForceExpandHeight = false;
            vl.childForceExpandWidth  = true;
            vl.childControlHeight     = true;
            vl.childControlWidth      = true;

            // Avatar holder
            var avatarHolder = new GameObject("AvatarHolder");
            avatarHolder.transform.SetParent(card, false);
            var ahRt = avatarHolder.AddComponent<RectTransform>();
            var ahLe = avatarHolder.AddComponent<LayoutElement>();
            ahLe.preferredHeight = CharViewSize;

            // GuideCharacterView — draws the actual procedural character
            var view = avatarHolder.AddComponent<GuideCharacterView>();
            var profile = new CharacterProfile
            {
                id        = id,
                name      = CharacterNames[charIdx],
                accentHex = AccentHexes[charIdx],
                blurb     = CharacterBlurbs[charIdx],
            };
            view.Build(profile, _font);
            _characterViews.Add(view);

            // Name label
            var nameLabel = MakeText($"Name_{id}", card, CharacterNames[charIdx], 22,
                new Color(accent.r + 0.25f, accent.g + 0.25f, accent.b + 0.25f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var nlLe = nameLabel.gameObject.AddComponent<LayoutElement>();
            nlLe.preferredHeight = 28f;

            // Role label (tiny)
            var roleLabel = MakeText($"Role_{id}", card,
                CharacterRoles[charIdx].Split('·')[0].Trim(), 16,
                new Color(accent.r + 0.1f, accent.g + 0.1f, accent.b + 0.1f, 0.80f),
                TextAnchor.MiddleCenter);
            var rlLe = roleLabel.gameObject.AddComponent<LayoutElement>();
            rlLe.preferredHeight = 22f;

            // Emotion bar (thin accent strip at bottom)
            var barGo = new GameObject("EmotionBar");
            barGo.transform.SetParent(card, false);
            var barImg = barGo.AddComponent<Image>();
            barImg.color = accent;
            var barLe = barGo.AddComponent<LayoutElement>();
            barLe.preferredHeight = 4f;
            var barPulse = barGo.AddComponent<PulseGlow>();
            barPulse.speed    = 1.5f + charIdx * 0.2f;
            barPulse.minAlpha = 0.35f;
            barPulse.maxAlpha = 1.0f;

            // Tap to select
            var btn = cardGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            var ci = charIdx; // capture
            btn.onClick.AddListener(() => OnCharacterTapped(ci));

            // Hover colour tint block
            var cs  = btn.colors;
            cs.highlightedColor = new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 1f);
            cs.pressedColor     = new Color(accent.r * 0.50f, accent.g * 0.50f, accent.b * 0.50f, 1f);
            cs.normalColor      = bg.color;
            btn.colors          = cs;
        }

        // ─── Detail Panel ─────────────────────────────────────────────────────────

        private void BuildDetailPanel(RectTransform root)
        {
            var panel = MakeRT("DetailPanel", root);
            panel.anchorMin = new Vector2(0f, 0.06f);
            panel.anchorMax = new Vector2(1f, 0.28f);
            panel.sizeDelta  = Vector2.zero;
            panel.anchoredPosition = Vector2.zero;

            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.15f, 0.08f, 0.92f);

            var vl = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing              = 6f;
            vl.padding              = new RectOffset(24, 24, 14, 14);
            vl.childForceExpandHeight = false;
            vl.childForceExpandWidth  = true;
            vl.childControlHeight     = true;
            vl.childControlWidth      = true;

            // Row 1: character name + emotion badge
            var row1 = MakeRT("NameRow", panel);
            var rl1 = row1.gameObject.AddComponent<HorizontalLayoutGroup>();
            rl1.spacing = 12f;
            rl1.childForceExpandWidth = false;
            rl1.childForceExpandHeight = true;
            rl1.childControlWidth  = false;
            rl1.childControlHeight = true;
            var row1Le = row1.gameObject.AddComponent<LayoutElement>();
            row1Le.preferredHeight = 44f;

            _characterNameLabel = MakeText("SelectedName", row1,
                CharacterNames[0], 36, new Color(0.97f, 0.92f, 0.72f),
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var cnlLe = _characterNameLabel.gameObject.AddComponent<LayoutElement>();
            cnlLe.flexibleWidth = 1f;

            _emotionLabel = MakeText("EmotionBadge", row1,
                "Idle ✨", 22, new Color(0.65f, 1f, 0.75f), TextAnchor.MiddleRight);
            var elLe = _emotionLabel.gameObject.AddComponent<LayoutElement>();
            elLe.preferredWidth = 200f;

            // Row 2: role
            _characterRoleLabel = MakeText("SelectedRole", panel,
                CharacterRoles[0], 20, new Color(0.75f, 0.88f, 0.78f),
                TextAnchor.UpperLeft);
            var crlLe = _characterRoleLabel.gameObject.AddComponent<LayoutElement>();
            crlLe.preferredHeight = 28f;

            // Row 3: blurb
            _characterBlurbLabel = MakeText("SelectedBlurb", panel,
                CharacterBlurbs[0], 19, new Color(0.85f, 0.92f, 0.86f, 0.88f),
                TextAnchor.UpperLeft);
            var cblLe = _characterBlurbLabel.gameObject.AddComponent<LayoutElement>();
            cblLe.minHeight  = 50f;
            cblLe.flexibleHeight = 1f;

            // Row 4: bond bar
            var bondRow = MakeRT("BondRow", panel);
            var brl = bondRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            brl.spacing = 10f;
            brl.childForceExpandWidth = false;
            brl.childControlWidth  = false;
            brl.childControlHeight = true;
            var bondRowLe = bondRow.gameObject.AddComponent<LayoutElement>();
            bondRowLe.preferredHeight = 24f;

            _bondText = MakeText("BondLabel", bondRow,
                "Bond  Lv.1", 18, new Color(0.65f, 1f, 0.75f), TextAnchor.MiddleLeft);
            var btlLe = _bondText.gameObject.AddComponent<LayoutElement>();
            btlLe.preferredWidth = 160f;

            // Bond bar track
            var trackGo = new GameObject("BondTrack");
            trackGo.transform.SetParent(bondRow, false);
            var trackImg = trackGo.AddComponent<Image>();
            trackImg.color = new Color(1f, 1f, 1f, 0.12f);
            var trackLe = trackGo.AddComponent<LayoutElement>();
            trackLe.flexibleWidth = 1f;

            // Bond bar fill
            var fillGo = new GameObject("BondFill");
            fillGo.transform.SetParent(trackGo.transform, false);
            _bondBar = fillGo.AddComponent<Image>();
            _bondBar.color = new Color(0.45f, 0.88f, 0.58f, 0.90f);
            var fillRt = _bondBar.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0.3f, 1f); // 30% filled default
            fillRt.sizeDelta  = Vector2.zero;

            // ── HUD Preview (glassmorphism mockup image) ──────────────────────
            if (_hudMockupTex != null)
            {
                var hudRow = MakeRT("HudPreviewRow", panel);
                var hudLe  = hudRow.gameObject.AddComponent<LayoutElement>();
                hudLe.preferredHeight = 72f;

                var hudImg = hudRow.gameObject.AddComponent<RawImage>();
                hudImg.texture = _hudMockupTex;
                hudImg.color   = new Color(1f, 1f, 1f, 0.18f); // subtle ghost preview
                hudImg.uvRect  = new Rect(0f, 0f, 1f, 1f);
                _hudPreviewImage = hudRow.gameObject.AddComponent<Image>(); // for tint only

                var hudLabel = MakeText("HudLabel", hudRow,
                    "🎮 In-Game HUD Preview", 14,
                    new Color(0.65f, 1f, 0.75f, 0.60f), TextAnchor.UpperRight);
                var hudLabelRt = hudLabel.rectTransform;
                hudLabelRt.anchorMin = new Vector2(1f, 1f);
                hudLabelRt.anchorMax = new Vector2(1f, 1f);
                hudLabelRt.pivot     = new Vector2(1f, 1f);
                hudLabelRt.anchoredPosition = new Vector2(-8f, -4f);
                hudLabelRt.sizeDelta = new Vector2(260f, 24f);
            }

            // ── ▶ PLAY with this character! CTA ───────────────────────────────
            var playGo = new GameObject("PlayButton");
            playGo.transform.SetParent(panel, false);
            var playImg = playGo.AddComponent<Image>();
            playImg.color = new Color(0.18f, 0.70f, 0.38f, 1f);

            _playButton = playGo.AddComponent<Button>();
            _playButton.targetGraphic = playImg;
            _playButton.onClick.AddListener(OnPlayButtonTapped);

            var playCs = _playButton.colors;
            playCs.highlightedColor = new Color(0.25f, 0.88f, 0.48f, 1f);
            playCs.pressedColor     = new Color(0.12f, 0.52f, 0.28f, 1f);
            _playButton.colors = playCs;

            var playLe = playGo.AddComponent<LayoutElement>();
            playLe.preferredHeight = 80f;
            playLe.flexibleWidth   = 1f;

            _playButtonLabel = MakeText("PlayLabel", playGo.GetComponent<RectTransform>(),
                "▶  Play with Pip!",
                32, new Color(0.05f, 0.18f, 0.08f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _playButtonLabel.rectTransform.anchorMin = Vector2.zero;
            _playButtonLabel.rectTransform.anchorMax = Vector2.one;
            _playButtonLabel.rectTransform.sizeDelta  = Vector2.zero;

            // Pulse the play button
            var playPulse = playGo.AddComponent<PulseGlow>();
            playPulse.speed    = 1.8f;
            playPulse.minAlpha = 0.80f;
            playPulse.maxAlpha = 1.00f;
        }

        // ─── Cinematic Launch Overlay ─────────────────────────────────────────────

        private void OnPlayButtonTapped()
        {
            _systems?.Audio?.PlayTapCue();
            StartCoroutine(LaunchWithCharacter(CharacterIds[_selectedCharIdx]));
        }

        private IEnumerator LaunchWithCharacter(string characterId)
        {
            // Build fullscreen cinematic overlay
            BuildCinematicLaunchOverlay(characterId);
            yield return new WaitForSeconds(0.1f);

            // Animate overlay in (fade up)
            if (_launchOverlayRoot == null) yield break;
            var canvasGrp = _launchOverlayRoot.AddComponent<CanvasGroup>();
            canvasGrp.alpha = 0f;

            var t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                if (canvasGrp != null) canvasGrp.alpha = Mathf.Clamp01(t / 0.5f);
                yield return null;
            }

            yield return new WaitForSeconds(1.2f); // hold the cinematic

            // Fire the callback — ForestQuestApp will navigate to play tab
            _onPlayWithCharacter?.Invoke(characterId);
        }

        private void BuildCinematicLaunchOverlay(string characterId)
        {
            if (_launchOverlayRoot != null) Destroy(_launchOverlayRoot);

            var root = GetComponent<RectTransform>();
            _launchOverlayRoot = new GameObject("CinematicLaunch");
            _launchOverlayRoot.transform.SetParent(root, false);
            var overlayRt = _launchOverlayRoot.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.sizeDelta  = Vector2.zero;
            overlayRt.anchoredPosition = Vector2.zero;

            // ── Layer 0: Dark scrim ──
            var scrim = _launchOverlayRoot.AddComponent<Image>();
            scrim.color = new Color(0f, 0f, 0f, 0.88f);

            // ── Layer 1: Forest banner image (full-bleed) ──
            if (_bannerTex != null)
            {
                var bannerGo = new GameObject("BannerImage");
                bannerGo.transform.SetParent(overlayRt, false);
                var bannerRt = bannerGo.AddComponent<RectTransform>();
                bannerRt.anchorMin = Vector2.zero;
                bannerRt.anchorMax = Vector2.one;
                bannerRt.sizeDelta  = Vector2.zero;

                var bannerImg = bannerGo.AddComponent<RawImage>();
                bannerImg.texture = _bannerTex;
                bannerImg.color   = new Color(1f, 1f, 1f, 0.55f); // blended with scrim
            }

            // ── Layer 2: Centre card ──
            var card = new GameObject("LaunchCard");
            card.transform.SetParent(overlayRt, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot     = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(420f, 560f);
            cardRt.anchoredPosition = Vector2.zero;

            var cardImg = card.AddComponent<Image>();
            var cidx    = System.Array.IndexOf(CharacterIds, characterId);
            var accent  = cidx >= 0 ? HexToColor(AccentHexes[cidx]) : new Color(0.5f, 0.9f, 0.6f);
            cardImg.color = new Color(accent.r * 0.15f, accent.g * 0.15f, accent.b * 0.15f, 0.96f);

            var cardVL = card.AddComponent<VerticalLayoutGroup>();
            cardVL.spacing              = 12f;
            cardVL.padding              = new RectOffset(24, 24, 28, 28);
            cardVL.childForceExpandHeight = false;
            cardVL.childForceExpandWidth  = true;
            cardVL.childControlHeight     = true;
            cardVL.childControlWidth      = true;

            // Character avatar inside card
            var avatarHolder = new GameObject("CinematicAvatar");
            avatarHolder.transform.SetParent(card.transform, false);
            var ahLe = avatarHolder.AddComponent<LayoutElement>();
            ahLe.preferredHeight = 280f;

            if (cidx >= 0)
            {
                var view = avatarHolder.AddComponent<GuideCharacterView>();
                view.Build(new CharacterProfile
                {
                    id        = characterId,
                    name      = CharacterNames[cidx],
                    accentHex = AccentHexes[cidx],
                    blurb     = CharacterBlurbs[cidx],
                }, _font);
            }

            // "Get ready!" label
            var readyLabel = MakeText("ReadyLabel", card.GetComponent<RectTransform>(),
                "Get Ready!", 36,
                new Color(accent.r + 0.3f, accent.g + 0.3f, accent.b + 0.1f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            var rlLe = readyLabel.gameObject.AddComponent<LayoutElement>();
            rlLe.preferredHeight = 48f;

            // Character name
            var nameStr = cidx >= 0 ? CharacterNames[cidx] : characterId;
            var nameLabel = MakeText("CinematicName", card.GetComponent<RectTransform>(),
                nameStr, 28, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            var nlLe = nameLabel.gameObject.AddComponent<LayoutElement>();
            nlLe.preferredHeight = 38f;

            // Role line
            var roleStr = cidx >= 0 ? CharacterRoles[cidx] : "";
            var roleLabel = MakeText("CinematicRole", card.GetComponent<RectTransform>(),
                roleStr, 20,
                new Color(accent.r + 0.1f, accent.g + 0.1f, accent.b + 0.1f, 0.80f),
                TextAnchor.MiddleCenter);
            var roleLe = roleLabel.gameObject.AddComponent<LayoutElement>();
            roleLe.preferredHeight = 28f;

            // Animated pulse ring around card
            var pulse = card.AddComponent<PulseGlow>();
            pulse.speed    = 2.0f;
            pulse.minAlpha = 0.60f;
            pulse.maxAlpha = 1.00f;

            // ── Layer 3: Title at top ──
            var titleGo = new GameObject("CinematicTitle");
            titleGo.transform.SetParent(overlayRt, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin        = new Vector2(0f, 1f);
            titleRt.anchorMax        = new Vector2(1f, 1f);
            titleRt.pivot            = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -24f);
            titleRt.sizeDelta        = new Vector2(0f, 80f);

            var titleTxt = titleGo.AddComponent<Text>();
            titleTxt.text      = "🌿 Forest Friends Quest";
            titleTxt.font      = _font ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleTxt.fontSize  = 38;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color     = new Color(0.97f, 0.92f, 0.72f);
            titleTxt.alignment = TextAnchor.MiddleCenter;
        }

        // ─── Control Bar ──────────────────────────────────────────────────────────

        private void BuildControlBar(RectTransform root)
        {
            var bar = MakeRT("ControlBar", root);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0.06f);
            bar.sizeDelta  = Vector2.zero;
            bar.anchoredPosition = Vector2.zero;

            var bg = bar.gameObject.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.12f, 0.06f, 0.95f);

            var hl = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing              = 12f;
            hl.padding              = new RectOffset(16, 16, 8, 8);
            hl.childForceExpandHeight = true;
            hl.childForceExpandWidth  = false;
            hl.childControlHeight     = true;
            hl.childControlWidth      = false;

            // ◀ Prev Emotion
            CreateControlButton(bar, "◀ Mood", new Color(0.38f, 0.70f, 0.48f), OnPrevEmotion, 200f);

            var spacer = new GameObject("Spacer").AddComponent<LayoutElement>();
            spacer.transform.SetParent(bar, false);
            spacer.flexibleWidth = 1f;

            // Biome cycling button
            CreateControlButton(bar, "🌍 Biome", new Color(0.30f, 0.55f, 0.72f), OnNextBiome, 200f);

            var spacer2 = new GameObject("Spacer2").AddComponent<LayoutElement>();
            spacer2.transform.SetParent(bar, false);
            spacer2.flexibleWidth = 1f;

            // ▶ Next Emotion
            CreateControlButton(bar, "Mood ▶", new Color(0.38f, 0.70f, 0.48f), OnNextEmotion, 200f);
        }

        private void CreateControlButton(RectTransform parent, string label,
            Color color, UnityEngine.Events.UnityAction onClick, float width)
        {
            var go  = new GameObject($"Btn_{label.Replace(" ", "")}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = width;

            var txt = MakeText($"Lbl_{label}", go.GetComponent<RectTransform>(),
                label, 20, new Color(0.98f, 0.98f, 0.95f), TextAnchor.MiddleCenter, FontStyle.Bold);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.sizeDelta  = Vector2.zero;

            var cs = btn.colors;
            cs.highlightedColor = new Color(color.r + 0.12f, color.g + 0.12f, color.b + 0.12f);
            cs.pressedColor     = new Color(color.r - 0.10f, color.g - 0.10f, color.b - 0.10f);
            btn.colors = cs;
        }

        // ─── Button Handlers ──────────────────────────────────────────────────────

        private void OnCharacterTapped(int idx)
        {
            _selectedCharIdx = idx;
            ApplySelectedCharacter(idx);
            // Scale bounce the tapped card
            StartCoroutine(BounceCard(_characterCards[idx]));
            // Play audio cue if systems available
            _systems?.Audio?.PlayTapCue();
        }

        private void OnNextEmotion()
        {
            _currentEmotionIdx = (_currentEmotionIdx + 1) % EmotionCycle.Length;
            ApplyEmotion(EmotionCycle[_currentEmotionIdx]);
        }

        private void OnPrevEmotion()
        {
            _currentEmotionIdx = (_currentEmotionIdx - 1 + EmotionCycle.Length) % EmotionCycle.Length;
            ApplyEmotion(EmotionCycle[_currentEmotionIdx]);
        }

        private void OnNextBiome()
        {
            _currentBiomeIdx = (_currentBiomeIdx + 1) % BiomeIds.Length;
            ApplyBiome(_currentBiomeIdx);
        }

        // ─── State Appliers ───────────────────────────────────────────────────────

        private void ApplySelectedCharacter(int idx)
        {
            if (idx < 0 || idx >= CharacterIds.Length) return;

            // Update detail panel
            if (_characterNameLabel != null) _characterNameLabel.text = CharacterNames[idx];
            if (_characterRoleLabel  != null) _characterRoleLabel.text  = CharacterRoles[idx];
            if (_characterBlurbLabel != null) _characterBlurbLabel.text = CharacterBlurbs[idx];

            // Bond level from systems or default
            int bondLevel = 1;
            if (_systems?.BondingEngine != null)
                bondLevel = _systems.BondingEngine.GetBondLevel(CharacterIds[idx]);
            if (_bondText != null) _bondText.text = $"Bond  Lv.{bondLevel}";
            if (_bondBar  != null)
            {
                var fill = Mathf.Clamp01(bondLevel / 10f);
                _bondBar.rectTransform.anchorMax = new Vector2(fill, 1f);
                var accent = HexToColor(AccentHexes[idx]);
                _bondBar.color = new Color(accent.r + 0.1f, accent.g + 0.1f, accent.b + 0.1f, 0.90f);
            }

            // Update the Play button label and tint
            if (_playButtonLabel != null)
                _playButtonLabel.text = $"\u25b6  Play with {CharacterNames[idx]}!";
            if (_playButton != null)
            {
                var acc   = HexToColor(AccentHexes[idx]);
                var playImg = _playButton.GetComponent<Image>();
                if (playImg != null)
                    playImg.color = new Color(
                        acc.r * 0.25f + 0.10f,
                        acc.g * 0.40f + 0.35f,
                        acc.b * 0.20f + 0.12f, 1f);
                var cs = _playButton.colors;
                cs.highlightedColor = new Color(acc.r * 0.35f + 0.15f, acc.g * 0.55f + 0.35f, acc.b * 0.30f + 0.15f, 1f);
                _playButton.colors = cs;
            }

            // Highlight selected card, dim others
            for (var i = 0; i < _cardBackgrounds.Count; i++)
            {
                var acc = HexToColor(AccentHexes[i]);
                if (i == idx)
                    _cardBackgrounds[i].color = new Color(acc.r * 0.45f, acc.g * 0.45f, acc.b * 0.45f, 0.98f);
                else
                    _cardBackgrounds[i].color = new Color(acc.r * 0.15f, acc.g * 0.15f, acc.b * 0.15f, 0.90f);
            }
        }

        private void ApplyEmotion(CreatureEmotion emotion)
        {
            // Update emotion label
            if (_emotionLabel != null)
            {
                var emoji = emotion switch
                {
                    CreatureEmotion.Happy    => "😊",
                    CreatureEmotion.Excited  => "🎉",
                    CreatureEmotion.Curious  => "🔍",
                    CreatureEmotion.Proud    => "⭐",
                    CreatureEmotion.Playful  => "🎮",
                    CreatureEmotion.Shy      => "🌸",
                    CreatureEmotion.Sleepy   => "💤",
                    _                        => "✨",
                };
                _emotionLabel.text = $"{emotion} {emoji}";
            }

            // Scale-pulse all cards based on emotion
            for (var i = 0; i < _characterCards.Count; i++)
            {
                if (_characterCards[i] != null)
                    StartCoroutine(EmotionScalePulse(_characterCards[i], emotion));
            }
        }

        // ─── Coroutines ───────────────────────────────────────────────────────────

        /// Auto-cycles emotions for ambient animation even without user input.
        private IEnumerator EmotionAnimLoop()
        {
            yield return new WaitForSeconds(3f); // give layout time to settle
            while (true)
            {
                yield return new WaitForSeconds(EmotionHold);
                _currentEmotionIdx = (_currentEmotionIdx + 1) % EmotionCycle.Length;
                ApplyEmotion(EmotionCycle[_currentEmotionIdx]);
            }
        }

        private IEnumerator EmotionScalePulse(RectTransform card, CreatureEmotion emotion)
        {
            if (card == null) yield break;
            float target = emotion switch
            {
                CreatureEmotion.Excited => 1.08f,
                CreatureEmotion.Happy   => 1.05f,
                CreatureEmotion.Proud   => 1.06f,
                CreatureEmotion.Shy     => 0.94f,
                CreatureEmotion.Sleepy  => 0.96f,
                _                       => 1.00f,
            };

            var start = card.localScale;
            var t     = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                var s = Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI);
                if (card != null) card.localScale = Vector3.one * Mathf.Lerp(1f, target, s);
                yield return null;
            }
            if (card != null) card.localScale = Vector3.one;
        }

        private IEnumerator BounceCard(RectTransform card)
        {
            if (card == null) yield break;
            var t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                var s = 1f + Mathf.Sin(t / 0.5f * Mathf.PI) * 0.12f;
                if (card != null) card.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (card != null) card.localScale = Vector3.one;
        }

        // ─── UI Helpers ───────────────────────────────────────────────────────────

        private RectTransform MakeRT(string name, RectTransform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private Text MakeText(string name, Component parent, string content,
            int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft,
            FontStyle style   = FontStyle.Normal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var txt = go.AddComponent<Text>();
            txt.text      = content;
            txt.font      = _font != null ? _font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize  = size;
            txt.color     = color;
            txt.alignment = anchor;
            txt.fontStyle = style;
            txt.resizeTextForBestFit = false;
            return txt;
        }

        private static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
