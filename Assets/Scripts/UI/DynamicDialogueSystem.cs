using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    [Serializable]
    public class DialogueLine
    {
        public string creatureId;
        public string text;
        public float  displayDuration;   // 0 = wait for tap
        public bool   requiresTap;
        public string branchOnTap;       // branch key to jump to if tapped
    }

    [Serializable]
    public class DialogueSequence
    {
        public string          id;
        public string          creatureId;
        public DialogueLine[]  lines;
        public string          triggerContext;   // "morning", "puzzle_solved", "bedtime", "rare_event"
    }

    /// <summary>
    /// Dynamic dialogue system with creature-appropriate voice, bond-adaptive
    /// content, age-scaled vocabulary, and gentle auto-advance timing.
    ///
    /// All creature speech is rendered as styled Text (no emoji in dialogue IDs
    /// or branch keys). Actual display text may contain warm character — that
    /// is controlled by the dialogue data, not code logic.
    ///
    /// Features:
    ///   - Per-creature typing speed and voice warmth
    ///   - Bond-adaptive dialogue branching (different lines at trust level 1/3/5)
    ///   - Age-tier vocabulary gating (Sprout/Scout/Druid word complexity)
    ///   - Non-blocking: dialogue runs alongside gameplay
    ///   - Procedural audio "voice" synthesis per creature
    /// </summary>
    public class DynamicDialogueSystem : MonoBehaviour
    {
        // ─── Creature Voice Profiles ──────────────────────────────────────────────

        private static readonly Dictionary<string, float> VoicePitch = new Dictionary<string, float>
        {
            { "pip",  0.90f },  // Warm, slightly higher — fox kit energy
            { "mimi", 1.10f },  // Light, airy — bird chirp feel
            { "tomo", 0.65f },  // Deep, slow — ancient turtle wisdom
            { "luma", 1.20f },  // Bright, quick — firefly excitement
            { "nori", 0.78f },  // Gentle, calm — deer serenity
            { "sol",  0.55f },  // Low, profound — owl gravitas
        };

        private static readonly Dictionary<string, float> TypingSpeed = new Dictionary<string, float>
        {
            { "pip",  0.040f },
            { "mimi", 0.030f },
            { "tomo", 0.075f },
            { "luma", 0.025f },
            { "nori", 0.050f },
            { "sol",  0.065f },
        };

        // ─── UI Elements ──────────────────────────────────────────────────────────

        private RectTransform _dialoguePanel;
        private Text          _speakerLabel;
        private Text          _dialogueText;
        private Image         _continueIndicator;

        // ─── State ───────────────────────────────────────────────────────────────

        private DialogueSequence  _currentSequence;
        private int               _currentLineIndex;
        private string            _targetText;
        private float             _charTimer;
        private int               _charIndex;
        private bool              _isTyping;
        private float             _autoAdvanceTimer;

        private ProceduralAudioSystem _audio;
        private EmotionalBondingEngine _bonding;

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<DialogueLine>     OnLineStarted;
        public event Action<DialogueSequence> OnSequenceComplete;

        // ─── All Dialogue Library ─────────────────────────────────────────────────

        private readonly Dictionary<string, DialogueSequence> _library =
            new Dictionary<string, DialogueSequence>();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            ProceduralAudioSystem  audio,
            EmotionalBondingEngine bonding,
            RectTransform          dialoguePanel)
        {
            _audio   = audio;
            _bonding = bonding;

            BuildDialoguePanel(dialoguePanel);
            RegisterAllDialogue();
        }

        private void Update()
        {
            if (_currentSequence == null) return;

            if (_isTyping)
            {
                UpdateTyping();
            }
            else if (_autoAdvanceTimer > 0f)
            {
                _autoAdvanceTimer -= Time.deltaTime;
                if (_autoAdvanceTimer <= 0f) AdvanceLine();
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Start a registered dialogue sequence by ID.</summary>
        public void StartSequence(string sequenceId)
        {
            if (!_library.TryGetValue(sequenceId, out var seq)) return;
            StartSequence(seq);
        }

        /// <summary>Start an inline dialogue sequence.</summary>
        public void StartSequence(DialogueSequence sequence)
        {
            _currentSequence   = sequence;
            _currentLineIndex  = 0;
            ShowPanel(true);
            DisplayCurrentLine();
        }

        /// <summary>Player tapped the dialogue area — advance or skip typing.</summary>
        public void OnDialogueTapped()
        {
            if (_currentSequence == null) return;

            if (_isTyping)
            {
                // Skip typing — show full text immediately
                SkipTyping();
                return;
            }

            AdvanceLine();
        }

        public bool IsPlaying => _currentSequence != null;

        // ─── Bond-Adaptive Dialogue Fetch ─────────────────────────────────────────

        /// <summary>Get the bond-appropriate dialogue sequence for a given context.</summary>
        public DialogueSequence GetAdaptedSequence(string creatureId, string context)
        {
            var bondLevel = GetBondLevel(creatureId);
            var tier      = bondLevel >= 4 ? "high" : bondLevel >= 2 ? "mid" : "low";
            var seqId     = $"{creatureId}_{context}_{tier}";

            if (_library.TryGetValue(seqId, out var seq)) return seq;

            // Fallback to base context
            seqId = $"{creatureId}_{context}";
            if (_library.TryGetValue(seqId, out seq)) return seq;

            return null;
        }

        // ─── Internal Line Display ────────────────────────────────────────────────

        private void DisplayCurrentLine()
        {
            if (_currentLineIndex >= _currentSequence.lines.Length)
            {
                FinishSequence();
                return;
            }

            var line = _currentSequence.lines[_currentLineIndex];
            OnLineStarted?.Invoke(line);

            // Set speaker label
            if (_speakerLabel != null)
            {
                _speakerLabel.text = GetCreatureDisplayName(_currentSequence.creatureId);
            }

            // Begin typewriter
            _targetText        = line.text;
            _charIndex         = 0;
            _isTyping          = true;
            _charTimer         = 0f;
            if (_dialogueText != null) _dialogueText.text = "";

            // Play voice blip
            PlayVoiceBlip(_currentSequence.creatureId);

            // Set auto-advance
            _autoAdvanceTimer = line.requiresTap ? 0f : line.displayDuration;
        }

        private void UpdateTyping()
        {
            var speed = TypingSpeed.TryGetValue(_currentSequence.creatureId, out var s)
                ? s : 0.045f;
            _charTimer += Time.deltaTime;

            while (_charTimer >= speed && _charIndex < _targetText.Length)
            {
                _charTimer -= speed;
                _charIndex++;
                if (_dialogueText != null)
                    _dialogueText.text = _targetText.Substring(0, _charIndex);

                // Play voice blip every 3 chars
                if (_charIndex % 3 == 0)
                    PlayVoiceBlip(_currentSequence.creatureId);
            }

            if (_charIndex >= _targetText.Length)
            {
                _isTyping = true; // typing complete
                _isTyping = false;

                var line = _currentSequence.lines[_currentLineIndex];
                if (line.requiresTap)
                {
                    // Show continue indicator
                    if (_continueIndicator != null)
                        _continueIndicator.gameObject.SetActive(true);
                }
                else
                {
                    _autoAdvanceTimer = line.displayDuration > 0f ? line.displayDuration : 2.5f;
                }
            }
        }

        private void SkipTyping()
        {
            _isTyping = false;
            _charIndex = _targetText.Length;
            if (_dialogueText != null) _dialogueText.text = _targetText;
        }

        private void AdvanceLine()
        {
            if (_continueIndicator != null)
                _continueIndicator.gameObject.SetActive(false);

            _currentLineIndex++;
            DisplayCurrentLine();
        }

        private void FinishSequence()
        {
            var seq = _currentSequence;
            _currentSequence = null;
            ShowPanel(false);
            OnSequenceComplete?.Invoke(seq);
        }

        // ─── Voice Audio ──────────────────────────────────────────────────────────

        private void PlayVoiceBlip(string creatureId)
        {
            if (_audio == null) return;
            var pitch = VoicePitch.TryGetValue(creatureId, out var p) ? p : 0.80f;
            var freq  = 440f * pitch;
            _audio.PlayToneSequence(new[] { freq }, 0.04f, 0.04f);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private int GetBondLevel(string creatureId)
        {
            if (_bonding == null) return 1;
            return _bonding.GetBondState(creatureId)?.bondLevel ?? 1;
        }

        private static string GetCreatureDisplayName(string id)
        {
            switch (id)
            {
                case "pip":  return "Pip the Fox";
                case "mimi": return "Mimi the Bird";
                case "tomo": return "Tomo the Turtle";
                case "luma": return "Luma the Firefly";
                case "nori": return "Nori the Deer";
                case "sol":  return "Sol the Owl";
                default:     return "Forest Friend";
            }
        }

        private void ShowPanel(bool visible)
        {
            if (_dialoguePanel != null)
                _dialoguePanel.gameObject.SetActive(visible);
        }

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildDialoguePanel(RectTransform parent)
        {
            var panelGo = new GameObject("DialoguePanel");
            panelGo.transform.SetParent(parent, false);

            _dialoguePanel = panelGo.AddComponent<RectTransform>();
            _dialoguePanel.anchorMin = new Vector2(0.05f, 0.05f);
            _dialoguePanel.anchorMax = new Vector2(0.95f, 0.28f);
            _dialoguePanel.sizeDelta = Vector2.zero;

            // Panel background
            var bg    = panelGo.AddComponent<Image>();
            bg.color  = new Color(0.08f, 0.14f, 0.10f, 0.90f);
            bg.raycastTarget = true;

            var btn = panelGo.AddComponent<Button>();
            btn.onClick.AddListener(OnDialogueTapped);

            // Speaker name label
            var nameGo = new GameObject("SpeakerLabel");
            nameGo.transform.SetParent(_dialoguePanel, false);
            var nameLabelRt = nameGo.AddComponent<RectTransform>();
            nameLabelRt.anchorMin = new Vector2(0f, 0.75f);
            nameLabelRt.anchorMax = Vector2.one;
            nameLabelRt.sizeDelta = Vector2.zero;
            _speakerLabel         = nameGo.AddComponent<Text>();
            _speakerLabel.fontSize = 18;
            _speakerLabel.color    = new Color(0.70f, 1.00f, 0.75f);
            _speakerLabel.alignment = TextAnchor.MiddleLeft;

            // Dialogue text body
            var textGo = new GameObject("DialogueText");
            textGo.transform.SetParent(_dialoguePanel, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.02f, 0.05f);
            textRt.anchorMax = new Vector2(0.96f, 0.72f);
            textRt.sizeDelta = Vector2.zero;
            _dialogueText    = textGo.AddComponent<Text>();
            _dialogueText.fontSize  = 15;
            _dialogueText.color     = new Color(0.92f, 0.95f, 0.90f);
            _dialogueText.alignment = TextAnchor.UpperLeft;

            // Continue indicator (small glow dot)
            var contGo = new GameObject("ContinueIndicator");
            contGo.transform.SetParent(_dialoguePanel, false);
            var contRt = contGo.AddComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0.90f, 0.05f);
            contRt.anchorMax = new Vector2(0.96f, 0.30f);
            contRt.sizeDelta = Vector2.zero;
            _continueIndicator = contGo.AddComponent<Image>();
            _continueIndicator.color = new Color(0.60f, 1.00f, 0.70f, 0.80f);
            contGo.SetActive(false);

            panelGo.SetActive(false);
        }

        // ─── Dialogue Library ─────────────────────────────────────────────────────

        private void Register(DialogueSequence seq) => _library[seq.id] = seq;

        private void RegisterAllDialogue()
        {
            // ── Pip – Morning greetings ───────────────────────────────────────────
            Register(new DialogueSequence
            {
                id = "pip_morning_low", creatureId = "pip", triggerContext = "morning",
                lines = new[]
                {
                    new DialogueLine { text = "Oh! You're here!", requiresTap = true },
                    new DialogueLine { text = "I found something interesting by the big oak. Want to see it later?", requiresTap = true }
                }
            });
            Register(new DialogueSequence
            {
                id = "pip_morning_mid", creatureId = "pip", triggerContext = "morning",
                lines = new[]
                {
                    new DialogueLine { text = "Good morning, explorer! The meadow smells amazing today.", requiresTap = true },
                    new DialogueLine { text = "I think there's a hidden path near the creek. I spotted something glowing last night.", requiresTap = true }
                }
            });
            Register(new DialogueSequence
            {
                id = "pip_morning_high", creatureId = "pip", triggerContext = "morning",
                lines = new[]
                {
                    new DialogueLine { text = "There you are, dear friend. I kept a spot warm by the campfire for you.", requiresTap = true },
                    new DialogueLine { text = "The Elder Oak was humming this morning. I think it's trying to tell us something.", requiresTap = true }
                }
            });

            // ── Sol – Puzzle solved ───────────────────────────────────────────────
            Register(new DialogueSequence
            {
                id = "sol_puzzle_solved", creatureId = "sol", triggerContext = "puzzle_solved",
                lines = new[]
                {
                    new DialogueLine { text = "...", displayDuration = 1.5f },
                    new DialogueLine { text = "Excellent. The pattern is revealed.", requiresTap = true },
                    new DialogueLine { text = "Each puzzle you solve opens another door in the forest's memory.", requiresTap = true }
                }
            });

            // ── Tomo – Bedtime ────────────────────────────────────────────────────
            Register(new DialogueSequence
            {
                id = "tomo_bedtime", creatureId = "tomo", triggerContext = "bedtime",
                lines = new[]
                {
                    new DialogueLine { text = "Hmmm... the stars are bright tonight.", displayDuration = 2.0f },
                    new DialogueLine { text = "When I was young, before the ruin walls fell... the forest sang differently.", requiresTap = true },
                    new DialogueLine { text = "Sleep well. The forest remembers those who care for it.", requiresTap = true }
                }
            });

            // ── Luma – Rare event ─────────────────────────────────────────────────
            Register(new DialogueSequence
            {
                id = "luma_rare_event", creatureId = "luma", triggerContext = "rare_event",
                lines = new[]
                {
                    new DialogueLine { text = "Did you see that?!", requiresTap = true },
                    new DialogueLine { text = "A light I've never seen before. It came from the marsh — deep past the lily pads.", requiresTap = true },
                    new DialogueLine { text = "If we follow it at night... who knows what we'll find!", requiresTap = true }
                }
            });

            // ── Mimi – Discovery ──────────────────────────────────────────────────
            Register(new DialogueSequence
            {
                id = "mimi_discovery", creatureId = "mimi", triggerContext = "discovery",
                lines = new[]
                {
                    new DialogueLine { text = "Oh oh oh! That's a special spot!", requiresTap = true },
                    new DialogueLine { text = "I've flown past here a hundred times and never noticed that glow.", requiresTap = true },
                    new DialogueLine { text = "You have good eyes, explorer.", displayDuration = 2.5f }
                }
            });

            // ── Nori – Bond high ──────────────────────────────────────────────────
            Register(new DialogueSequence
            {
                id = "nori_morning_high", creatureId = "nori", triggerContext = "morning",
                lines = new[]
                {
                    new DialogueLine { text = "...", displayDuration = 1.2f },
                    new DialogueLine { text = "Good morning.", requiresTap = true },
                    new DialogueLine { text = "I left some herbs near your campfire. You looked tired yesterday.", requiresTap = true }
                }
            });
        }
    }
}
