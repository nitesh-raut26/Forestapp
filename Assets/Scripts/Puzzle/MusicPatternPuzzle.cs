using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Music Pattern Puzzle — tone sequence memory challenge.
    ///
    /// ProceduralAudioSystem plays a sequence of musical notes assigned to
    /// glowing forest "instrument" nodes. Player must reproduce the melody
    /// by tapping the nodes in the correct order.
    ///
    /// Each node has a unique pitch, a unique sprite color, and emits
    /// EmotionalParticle feedback on tap — zero emoji.
    ///
    /// Difficulty scales:
    ///   Sprout: 3 notes, slow tempo, nodes stay highlighted
    ///   Scout:  5 notes, moderate tempo
    ///   Druid:  7 notes, fast tempo, nodes fade quickly
    /// </summary>
    public class MusicPatternPuzzle : MonoBehaviour
    {
        // ─── Note Definitions ─────────────────────────────────────────────────────

        // Pentatonic scale — sounds magical and never dissonant
        private static readonly float[] NotePitches = new[]
        {
            261.63f,   // C4 — Root (Deep Forest Drum)
            293.66f,   // D4 — Mossy Bell
            329.63f,   // E4 — Dewdrop Chime
            392.00f,   // G4 — Wind Reed
            440.00f,   // A4 — Moonlit Flute
            523.25f,   // C5 — Starlight Chime
            587.33f,   // D5 — Crystal Bell
        };

        private static readonly Color[] NoteColors = new[]
        {
            new Color(0.40f, 0.90f, 0.60f),   // Forest green
            new Color(0.60f, 0.85f, 1.00f),   // Sky blue
            new Color(1.00f, 0.90f, 0.45f),   // Amber glow
            new Color(0.85f, 0.55f, 1.00f),   // Lavender
            new Color(1.00f, 0.70f, 0.50f),   // Warm coral
            new Color(0.50f, 1.00f, 0.90f),   // Teal crystal
            new Color(1.00f, 0.95f, 0.70f),   // Moonlight cream
        };

        private static readonly string[] NoteNames = new[]
        {
            "Forest Drum", "Mossy Bell", "Dewdrop", "Wind Reed",
            "Moon Flute", "Starlight", "Crystal Bell"
        };

        // ─── Config ──────────────────────────────────────────────────────────────

        [Header("Timing")]
        public float notePlayDuration  = 0.35f;
        public float noteGapDuration   = 0.18f;
        public float revealPauseBefore = 0.5f;

        [Header("Visual")]
        public float nodeSize = 85f;

        // ─── State ───────────────────────────────────────────────────────────────

        private enum Phase { Idle, Playing, PlayerInput, Solved }
        private Phase _phase = Phase.Idle;

        private PuzzleManager           _manager;
        private ProceduralAudioSystem   _audio;
        private EmotionalParticleEngine _particles;

        private List<int>  _pattern      = new List<int>();
        private List<int>  _playerInput  = new List<int>();
        private int        _notePoolSize;
        private string     _tier;

        private float  _playTimer;
        private int    _playIndex;
        private bool   _noteActive;
        private float  _revealPauseTimer;

        private RectTransform[] _nodeRects;
        private Image[]          _nodeImages;
        private Color[]          _nodeBaseColors;
        private float[]          _nodeGlowTimers;

        public event Action<bool> OnPuzzleEnd;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            PuzzleManager manager,
            ProceduralAudioSystem audio,
            EmotionalParticleEngine particles,
            RectTransform parent,
            string tier)
        {
            _manager   = manager;
            _audio     = audio;
            _particles = particles;
            _tier      = tier;

            _notePoolSize = tier == "druid" ? 7 : tier == "scout" ? 5 : 3;
            var patternLength = manager.GetAdaptedMemoryLength(
                tier == "druid" ? 7 : tier == "scout" ? 5 : 3
            );

            GeneratePattern(patternLength);
            BuildUI(parent);

            _revealPauseTimer = revealPauseBefore;
            _phase            = Phase.Playing;

            _manager.StartPuzzle(PuzzleType.MusicPattern, tier);
        }

        private void Update()
        {
            switch (_phase)
            {
                case Phase.Playing:  UpdatePlayback();    break;
            }
            UpdateNodeGlow();
        }

        // ─── Pattern Generation ───────────────────────────────────────────────────

        private void GeneratePattern(int length)
        {
            _pattern.Clear();
            _playerInput.Clear();
            for (var i = 0; i < length; i++)
            {
                _pattern.Add(UnityEngine.Random.Range(0, _notePoolSize));
            }
        }

        // ─── Playback Phase ───────────────────────────────────────────────────────

        private void UpdatePlayback()
        {
            // Initial pause before first note
            if (_revealPauseTimer > 0f)
            {
                _revealPauseTimer -= Time.deltaTime;
                return;
            }

            _playTimer += Time.deltaTime;
            var stepDuration = notePlayDuration + noteGapDuration;

            if (_playIndex >= _pattern.Count)
            {
                // All notes played — hand control to player
                if (_playTimer >= noteGapDuration)
                {
                    _phase = Phase.PlayerInput;
                    DimAllNodes();
                }
                return;
            }

            var noteStart = _playIndex * stepDuration;

            if (!_noteActive && _playTimer >= noteStart)
            {
                // Play this note
                PlayNoteVisual(_pattern[_playIndex], true);
                PlayNoteAudio(_pattern[_playIndex]);
                _noteActive = true;
            }

            if (_noteActive && _playTimer >= noteStart + notePlayDuration)
            {
                // End this note
                PlayNoteVisual(_pattern[_playIndex], false);
                _noteActive = false;
                _playIndex++;
            }
        }

        // ─── Player Input ─────────────────────────────────────────────────────────

        public void OnNoteTapped(int noteIndex)
        {
            if (_phase != Phase.PlayerInput) return;

            // Play the note sound (feedback)
            PlayNoteAudio(noteIndex);
            FlashNode(noteIndex);

            var pos = _nodeRects != null && noteIndex < _nodeRects.Length
                ? _nodeRects[noteIndex].anchoredPosition
                : Vector2.zero;

            var expected = _pattern[_playerInput.Count];

            if (noteIndex == expected)
            {
                // Correct!
                _particles?.Spawn(EmotionalParticleType.HappyPollenBurst, pos, 3);
                _manager.RecordCorrectStep(pos);
                _playerInput.Add(noteIndex);

                if (_playerInput.Count == _pattern.Count)
                {
                    _phase = Phase.Solved;
                    _particles?.SpawnJoyBurst(pos);
                    _manager.SolvePuzzle(pos);
                    OnPuzzleEnd?.Invoke(true);
                }
            }
            else
            {
                // Wrong note
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, pos, 2);
                _manager.RecordMistake(pos);
                _playerInput.Clear();
                // Replay the sequence after a short pause
                StartCoroutine(ReplayAfterDelay(1.2f));
            }
        }

        // ─── Audio ───────────────────────────────────────────────────────────────

        private void PlayNoteAudio(int noteIndex)
        {
            if (_audio == null || noteIndex >= NotePitches.Length) return;
            var freq = NotePitches[noteIndex];
            _audio.PlayToneSequence(new[] { freq }, notePlayDuration, 0.18f);
        }

        // ─── Visual Node Helpers ──────────────────────────────────────────────────

        private void PlayNoteVisual(int noteIndex, bool active)
        {
            if (_nodeImages == null || noteIndex >= _nodeImages.Length) return;
            _nodeImages[noteIndex].color = active
                ? NoteColors[noteIndex % NoteColors.Length]
                : DimColor(_nodeBaseColors[noteIndex]);

            if (active)
            {
                _nodeGlowTimers[noteIndex] = 0.4f;
                _particles?.Spawn(EmotionalParticleType.HappyGoldenWisp,
                    _nodeRects[noteIndex].anchoredPosition, 2);
            }
        }

        private void FlashNode(int noteIndex)
        {
            if (_nodeImages == null || noteIndex >= _nodeImages.Length) return;
            _nodeGlowTimers[noteIndex] = 0.3f;
        }

        private void DimAllNodes()
        {
            if (_tier == "sprout") return; // Sprout keeps notes visible
            if (_nodeImages == null) return;
            for (var i = 0; i < _notePoolSize; i++)
            {
                _nodeImages[i].color = DimColor(_nodeBaseColors[i]);
            }
        }

        private void UpdateNodeGlow()
        {
            if (_nodeGlowTimers == null) return;
            for (var i = 0; i < _notePoolSize; i++)
            {
                if (_nodeGlowTimers[i] <= 0f) continue;
                _nodeGlowTimers[i] -= Time.deltaTime;
                var t = Mathf.Clamp01(_nodeGlowTimers[i] / 0.4f);
                _nodeImages[i].color = Color.Lerp(_nodeBaseColors[i],
                    NoteColors[i % NoteColors.Length], t);
            }
        }

        private static Color DimColor(Color c)
            => new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, c.a * 0.6f);

        private System.Collections.IEnumerator ReplayAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _phase       = Phase.Playing;
            _playIndex   = 0;
            _playTimer   = 0f;
            _noteActive  = false;
            _revealPauseTimer = 0f;
        }

        // ─── UI Builder ───────────────────────────────────────────────────────────

        private void BuildUI(RectTransform parent)
        {
            _nodeRects      = new RectTransform[_notePoolSize];
            _nodeImages     = new Image[_notePoolSize];
            _nodeBaseColors = new Color[_notePoolSize];
            _nodeGlowTimers = new float[_notePoolSize];

            // Arrange notes in a gentle arc
            var arcAngle  = _notePoolSize <= 4 ? 120f : 160f;
            var startAngle = -arcAngle / 2f;
            var stepAngle = _notePoolSize > 1 ? arcAngle / (_notePoolSize - 1) : 0f;
            var radius    = 180f;

            for (var i = 0; i < _notePoolSize; i++)
            {
                var angle = (startAngle + i * stepAngle) * Mathf.Deg2Rad;
                var pos   = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.55f - 60f);

                var go  = new GameObject($"NoteNode_{i}");
                go.transform.SetParent(parent, false);

                var rt  = go.AddComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.sizeDelta        = new Vector2(nodeSize, nodeSize);

                var baseColor = NoteColors[i % NoteColors.Length];
                baseColor.a   = 0.75f;

                var img = go.AddComponent<Image>();
                img.sprite = CreateNoteSprite(i);
                img.color  = baseColor;

                var btn = go.AddComponent<Button>();
                var idx = i;
                btn.onClick.AddListener(() => OnNoteTapped(idx));

                _nodeRects[i]      = rt;
                _nodeImages[i]     = img;
                _nodeBaseColors[i] = baseColor;
            }
        }

        private static Sprite CreateNoteSprite(int index)
        {
            // Musical note shape: circle body + stem
            const int size = 56;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = new Vector2(size * 0.40f, size * 0.35f);
            var noteR  = size * 0.28f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    var alpha = 1f - Mathf.Clamp01((dist - noteR + 3f) / 3.5f);

                    // Add vertical stem
                    var stemX = (int)(center.x + noteR * 0.8f);
                    if (Mathf.Abs(x - stemX) <= 2 && y > center.y && y < size - 4)
                    {
                        alpha = Mathf.Max(alpha, 0.85f);
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
