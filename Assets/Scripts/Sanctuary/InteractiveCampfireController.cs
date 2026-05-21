using System;
using System.Collections;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Interactive campfire controller — the emotional anchor of the Sanctuary.
    ///
    /// The campfire transitions through warmth states based on time of day and
    /// player interaction. Creatures gather around it, bedtime stories unlock
    /// at dusk, and special NPC visits happen on rare evenings.
    ///
    /// Feel goal: Animal Crossing campfire warmth meets Spiritfarer emotional depth.
    /// </summary>
    public class InteractiveCampfireController : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<string>  OnStoryUnlocked;        // storyId
        public event Action<string>  OnCreatureGathered;     // creatureId
        public event Action          OnCampfireIgnited;
        public event Action          OnCampfireExtinguished;

        // ─── State ───────────────────────────────────────────────────────────────

        public enum CampfireState { Unlit, Warm, Bright, Glowing, Storytime }
        public CampfireState CurrentState { get; private set; } = CampfireState.Unlit;

        private DayNightWeatherController _timeController;
        private EmotionalBondingEngine    _bonding;
        private ProceduralAudioSystem     _audio;
        private VFXManager                _vfx;

        private float _warmthLevel; // 0–1
        private bool  _isLit;
        private float _nextStoryCheckTime;
        private const float StoryCheckInterval = 120f; // every 2 real minutes

        // Story pool for bedtime
        private readonly string[] _bedtimeStories = {
            "story_firefly_origin", "story_elder_oak", "story_river_crystal",
            "story_tomo_shell", "story_moon_lily", "story_first_winter"
        };
        private int _storyIndex;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            DayNightWeatherController time,
            EmotionalBondingEngine    bonding,
            ProceduralAudioSystem     audio,
            VFXManager                vfx)
        {
            _timeController = time;
            _bonding        = bonding;
            _audio          = audio;
            _vfx            = vfx;

            _nextStoryCheckTime = Time.time + StoryCheckInterval;

            if (_timeController != null)
            {
                _timeController.OnTimeChanged    += OnTimeChanged;
                _timeController.OnWeatherChanged += OnWeatherChanged;
            }
        }

        private void Update()
        {
            if (!_isLit) return;

            // Story unlock check at dusk / evening
            if (Time.time >= _nextStoryCheckTime)
            {
                _nextStoryCheckTime = Time.time + StoryCheckInterval;
                TryUnlockBedtimeStory();
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Player taps the campfire — ignite or interact.</summary>
        public void OnTapped()
        {
            if (!_isLit)
            {
                IgniteCampfire();
            }
            else
            {
                // Feed the fire — increase warmth
                _warmthLevel = Mathf.Min(1f, _warmthLevel + 0.25f);
                UpdateState();
                _vfx?.OnDiscovery(Vector2.zero);
                _audio?.PlayCreatureCue("campfire", "warmth");
            }
        }

        /// <summary>Player adds a treat to the campfire.</summary>
        public void AddTreat()
        {
            if (!_isLit) IgniteCampfire();
            _warmthLevel = Mathf.Min(1f, _warmthLevel + 0.4f);
            UpdateState();

            // High warmth triggers creature gathering
            if (_warmthLevel >= 0.8f)
                TriggerCreatureGathering();
        }

        public float GetWarmthLevel() => _warmthLevel;

        public bool IsLit => _isLit;

        // ─── Private Logic ────────────────────────────────────────────────────────

        private void IgniteCampfire()
        {
            _isLit = true;
            _warmthLevel = 0.3f;
            UpdateState();
            OnCampfireIgnited?.Invoke();
            _vfx?.OnDiscovery(Vector2.zero);
            _audio?.PlayCreatureCue("campfire", "ignite");
            Debug.Log("[InteractiveCampfireController] Campfire ignited.");
        }

        private void UpdateState()
        {
            var prev = CurrentState;
            CurrentState = _warmthLevel switch
            {
                >= 0.9f => CampfireState.Glowing,
                >= 0.7f => CampfireState.Bright,
                >= 0.4f => CampfireState.Warm,
                _       => CampfireState.Unlit
            };

            if (CurrentState != prev)
                Debug.Log($"[InteractiveCampfireController] State → {CurrentState}");
        }

        private void TryUnlockBedtimeStory()
        {
            var hour = _timeController?.CurrentHour ?? 0f;
            bool isDusk = hour >= 18f || hour < 6f; // evening / night

            if (isDusk && _isLit && _warmthLevel >= 0.6f)
            {
                CurrentState = CampfireState.Storytime;
                var storyId = _bedtimeStories[_storyIndex % _bedtimeStories.Length];
                _storyIndex++;
                OnStoryUnlocked?.Invoke(storyId);
                _audio?.PlayCreatureCue("campfire", "storytime");
                Debug.Log($"[InteractiveCampfireController] Story unlocked: {storyId}");
            }
        }

        private void TriggerCreatureGathering()
        {
            var creatures = new[] { "pip", "mimi", "tomo", "luma" };
            var creature = creatures[UnityEngine.Random.Range(0, creatures.Length)];
            OnCreatureGathered?.Invoke(creature);
            _bonding?.IncreaseBond(creature, 1);
            Debug.Log($"[InteractiveCampfireController] {creature} gathers at campfire.");
        }

        private void OnTimeChanged(TimeOfDay time)
        {
            // Naturally dim at dawn
            if (time == TimeOfDay.Morning && _isLit)
            {
                _warmthLevel = Mathf.Max(0f, _warmthLevel - 0.1f);
                if (_warmthLevel <= 0f)
                {
                    _isLit = false;
                    UpdateState();
                    OnCampfireExtinguished?.Invoke();
                }
            }
        }

        private void OnWeatherChanged(WeatherState weather)
        {
            // Storm extinguishes the campfire
            if (weather == WeatherState.Stormy && _isLit)
            {
                _warmthLevel *= 0.3f;
                UpdateState();
            }
        }

        private void OnDestroy()
        {
            if (_timeController != null)
            {
                _timeController.OnTimeChanged    -= OnTimeChanged;
                _timeController.OnWeatherChanged -= OnWeatherChanged;
            }
        }
    }
}
