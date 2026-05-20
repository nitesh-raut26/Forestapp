using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    public enum CampfireState
    {
        Unlit,
        Kindling,
        Burning,
        Embers
    }

    /// <summary>
    /// Manages the sanctuary campfire — the emotional heart of the player's home.
    ///
    /// Features:
    ///   - Procedurally synthesized crackling fire audio
    ///   - Sprite-based animated flame particles (no emoji)
    ///   - NPC visit triggers at night when campfire is lit
    ///   - Bedtime story narrative unlocked after burning for 60 seconds
    ///   - State machine: Unlit → Kindling → Burning → Embers
    ///   - Integrates with DayNightWeatherController for night-only events
    /// </summary>
    public class SanctuaryCampfireSystem : MonoBehaviour
    {
        // ─── System Links ─────────────────────────────────────────────────────────

        private ProceduralAudioSystem     _audio;
        private EmotionalParticleEngine   _particles;
        private DayNightWeatherController _timeController;
        private EmotionalBondingEngine    _bonding;

        // ─── Visual Elements ──────────────────────────────────────────────────────

        private RectTransform _campfireRect;
        private Image         _campfireBase;
        private Image         _flameImage;

        // ─── State ───────────────────────────────────────────────────────────────

        private CampfireState _state       = CampfireState.Unlit;
        private float         _burnTimer;
        private float         _kindlingTimer;
        private float         _particleTimer;
        private float         _storyTimer;
        private bool          _storyTriggered;
        private bool          _npcVisitTriggered;

        // ─── Audio ───────────────────────────────────────────────────────────────

        private float _crackleCooldown;

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action                OnCampfireLit;
        public event Action                OnCampfireEmbers;
        public event Action<string>        OnBedtimeStoryUnlocked;  // story id
        public event Action<string>        OnNPCVisitTriggered;     // creature id

        // ─── Constants ────────────────────────────────────────────────────────────

        private const float KindlingDuration    = 3.0f;
        private const float StoryUnlockTime     = 60f;
        private const float EmberTime           = 180f;
        private const float ParticleInterval    = 0.25f;
        private const float CrackleInterval     = 2.5f;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            ProceduralAudioSystem     audio,
            EmotionalParticleEngine   particles,
            DayNightWeatherController timeController,
            EmotionalBondingEngine    bonding,
            RectTransform             campfireRect)
        {
            _audio          = audio;
            _particles      = particles;
            _timeController = timeController;
            _bonding        = bonding;
            _campfireRect   = campfireRect;

            BuildVisuals();
        }

        private void Update()
        {
            switch (_state)
            {
                case CampfireState.Kindling: UpdateKindling(); break;
                case CampfireState.Burning:  UpdateBurning();  break;
                case CampfireState.Embers:   UpdateEmbers();   break;
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Light the campfire. Triggers the Kindling → Burning sequence.</summary>
        public bool TryLight()
        {
            if (_state == CampfireState.Burning || _state == CampfireState.Kindling)
                return false;

            _state          = CampfireState.Kindling;
            _kindlingTimer  = 0f;
            _burnTimer      = 0f;
            _storyTriggered = false;
            _npcVisitTriggered = false;

            // Play kindling start audio
            _audio?.PlayToneSequence(new[] { 220f, 196f, 174f }, 0.3f, 0.08f);

            // Spawn smoke particles (pre-light)
            if (_campfireRect != null)
            {
                _particles?.Spawn(EmotionalParticleType.GrassDisturbDust,
                    _campfireRect.anchoredPosition + Vector2.up * 30f, 4);
            }

            UpdateFlameVisual(CampfireState.Kindling);
            return true;
        }

        /// <summary>Extinguish the campfire immediately.</summary>
        public void Extinguish()
        {
            _state = CampfireState.Unlit;
            UpdateFlameVisual(CampfireState.Unlit);
        }

        public CampfireState CurrentState => _state;
        public bool IsLit => _state == CampfireState.Burning || _state == CampfireState.Kindling;
        public float BurnProgress => _burnTimer / EmberTime;

        // ─── State Updates ────────────────────────────────────────────────────────

        private void UpdateKindling()
        {
            _kindlingTimer += Time.deltaTime;

            // Flicker color during kindling
            var flicker = 0.5f + Mathf.Sin(Time.time * 8f) * 0.2f;
            if (_flameImage != null)
            {
                _flameImage.color = new Color(1f, 0.55f * flicker, 0.10f, flicker);
            }

            if (_kindlingTimer >= KindlingDuration)
            {
                _state = CampfireState.Burning;
                UpdateFlameVisual(CampfireState.Burning);
                OnCampfireLit?.Invoke();
                _audio?.PlayContextChord(true);
            }
        }

        private void UpdateBurning()
        {
            _burnTimer += Time.deltaTime;

            // Animate flame flicker
            AnimateFlicker();

            // Emit fire particles periodically
            _particleTimer += Time.deltaTime;
            if (_particleTimer >= ParticleInterval)
            {
                _particleTimer = 0f;
                EmitFireParticles();
            }

            // Crackling audio
            _crackleCooldown -= Time.deltaTime;
            if (_crackleCooldown <= 0f)
            {
                _crackleCooldown = CrackleInterval + UnityEngine.Random.Range(-0.5f, 0.5f);
                PlayCrackle();
            }

            // Bedtime story unlock
            if (!_storyTriggered && _burnTimer >= StoryUnlockTime
                && _timeController?.CurrentTime == TimeOfDay.Night)
            {
                _storyTriggered = true;
                TriggerBedtimeStory();
            }

            // NPC visit at night
            if (!_npcVisitTriggered && _burnTimer >= 30f
                && _timeController?.CurrentTime == TimeOfDay.Night)
            {
                _npcVisitTriggered = true;
                TriggerNPCVisit();
            }

            // Transition to embers
            if (_burnTimer >= EmberTime)
            {
                _state = CampfireState.Embers;
                UpdateFlameVisual(CampfireState.Embers);
                OnCampfireEmbers?.Invoke();
            }
        }

        private void UpdateEmbers()
        {
            // Slow ember particle emission
            _particleTimer += Time.deltaTime;
            if (_particleTimer >= ParticleInterval * 3f)
            {
                _particleTimer = 0f;
                if (_campfireRect != null)
                {
                    _particles?.Spawn(EmotionalParticleType.GrassDisturbDust,
                        _campfireRect.anchoredPosition + Vector2.up * 20f, 1);
                }
            }

            // Ember glow flicker
            if (_flameImage != null)
            {
                var ember = 0.25f + Mathf.Sin(Time.time * 2f) * 0.1f;
                _flameImage.color = new Color(1f, 0.35f, 0.05f, ember);
            }
        }

        // ─── VFX & Audio ─────────────────────────────────────────────────────────

        private void EmitFireParticles()
        {
            if (_campfireRect == null || _particles == null) return;

            var basePos = _campfireRect.anchoredPosition + Vector2.up * 25f;

            // Warm golden fire wisps
            _particles.Spawn(EmotionalParticleType.HappyGoldenWisp, basePos, 2);

            // Occasionally emit a firefly-like spark
            if (UnityEngine.Random.value < 0.3f)
            {
                _particles.Spawn(EmotionalParticleType.JoySparkle, basePos, 1);
            }
        }

        private void AnimateFlicker()
        {
            if (_flameImage == null) return;

            var flicker = 0.80f + Mathf.Sin(Time.time * 7.3f) * 0.12f
                        + Mathf.Sin(Time.time * 13.7f) * 0.06f;
            var scaleX  = 1f + Mathf.Sin(Time.time * 5f) * 0.08f;
            var scaleY  = 1f + Mathf.Sin(Time.time * 4.1f) * 0.12f;

            _flameImage.color = new Color(1f, 0.60f * flicker, 0.15f, flicker);

            if (_campfireRect != null)
            {
                _campfireRect.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }

        private void PlayCrackle()
        {
            // Crackle = short noise burst at wood-snap frequencies
            var freq = UnityEngine.Random.Range(180f, 320f);
            _audio?.PlayToneSequence(new[] { freq, freq * 1.4f }, 0.04f, 0.05f);
        }

        private void TriggerBedtimeStory()
        {
            // Select story based on highest bond creature
            var storyId = "story_pip_01"; // default
            OnBedtimeStoryUnlocked?.Invoke(storyId);
            Debug.Log("[Campfire] Bedtime story unlocked: " + storyId);
        }

        private void TriggerNPCVisit()
        {
            // Pick a creature not currently sleeping
            var creature = _timeController?.CurrentTime == TimeOfDay.Night ? "luma" : "pip";
            OnNPCVisitTriggered?.Invoke(creature);
            Debug.Log($"[Campfire] NPC visit: {creature}");
        }

        // ─── Visual Setup ─────────────────────────────────────────────────────────

        private void BuildVisuals()
        {
            if (_campfireRect == null) return;

            // Base stone circle
            var baseGo = new GameObject("CampfireBase");
            baseGo.transform.SetParent(_campfireRect, false);
            var baseRt = baseGo.AddComponent<RectTransform>();
            baseRt.sizeDelta = new Vector2(70f, 35f);
            baseRt.anchoredPosition = Vector2.zero;
            _campfireBase = baseGo.AddComponent<Image>();
            _campfireBase.color = new Color(0.35f, 0.28f, 0.22f, 1f);
            _campfireBase.raycastTarget = false;

            // Flame image
            var flameGo = new GameObject("Flame");
            flameGo.transform.SetParent(_campfireRect, false);
            var flameRt = flameGo.AddComponent<RectTransform>();
            flameRt.sizeDelta = new Vector2(45f, 70f);
            flameRt.anchoredPosition = new Vector2(0f, 30f);
            _flameImage = flameGo.AddComponent<Image>();
            _flameImage.sprite = CreateFlameSprite();
            _flameImage.color  = Color.clear;
            _flameImage.raycastTarget = false;
        }

        private void UpdateFlameVisual(CampfireState state)
        {
            if (_flameImage == null) return;

            switch (state)
            {
                case CampfireState.Unlit:
                    _flameImage.color = Color.clear;
                    break;
                case CampfireState.Kindling:
                    _flameImage.color = new Color(1f, 0.5f, 0.1f, 0.4f);
                    break;
                case CampfireState.Burning:
                    _flameImage.color = new Color(1f, 0.60f, 0.15f, 0.90f);
                    break;
                case CampfireState.Embers:
                    _flameImage.color = new Color(1f, 0.35f, 0.05f, 0.35f);
                    break;
            }
        }

        private static Sprite CreateFlameSprite()
        {
            const int size = 48;
            var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Tear-drop / flame shape: wide at base, narrow at top
                    var nx      = (x - size / 2f) / (size / 2f);
                    var ny      = (float)y / size;
                    var width   = Mathf.Lerp(0.85f, 0.1f, ny);   // narrows upward
                    var inFlame = Mathf.Abs(nx) < width;
                    var alpha   = inFlame ? Mathf.Lerp(1f, 0f, (Mathf.Abs(nx) / width) * (Mathf.Abs(nx) / width)) : 0f;
                    alpha      *= ny < 0.15f ? ny / 0.15f : 1f; // fade at base

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f), size);
        }
    }
}
