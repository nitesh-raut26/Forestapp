using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Manages creature homes within the Sanctuary — where each guide creature lives.
    ///
    /// Each creature unlocks their home at a specific bond level.
    /// The home upgrades visually as the bond grows.
    /// Creatures "return home" at certain times of day, greeting the player.
    ///
    /// Bond Level → Home State progression:
    ///   0-2  : Nest (basic)
    ///   3-7  : Cozy den
    ///   8-14 : Decorated home
    ///   15+  : Magical sanctuary home
    /// </summary>
    public class CreatureHomeBehavior : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<string, CreatureHomeState> OnHomeUpgraded;    // creatureId, new state
        public event Action<string>                    OnCreatureReturned; // creatureId

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Dictionary<string, CreatureHome> _homes = new();

        private EmotionalBondingEngine   _bonding;
        private DayNightWeatherController _time;
        private VFXManager               _vfx;
        private ProceduralAudioSystem    _audio;

        private float _lastHomeCheckTime;
        private const float HomeCheckInterval = 60f;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            EmotionalBondingEngine    bonding,
            DayNightWeatherController time,
            VFXManager                vfx,
            ProceduralAudioSystem    audio)
        {
            _bonding = bonding;
            _time    = time;
            _vfx     = vfx;
            _audio   = audio;

            RegisterCreatureHomes();

            if (_bonding != null)
                _bonding.OnBondLevelChanged += OnBondLevelChanged;

            Debug.Log($"[CreatureHomeBehavior] {_homes.Count} creature homes registered.");
        }

        private void Update()
        {
            if (Time.time - _lastHomeCheckTime < HomeCheckInterval) return;
            _lastHomeCheckTime = Time.time;
            CheckCreatureReturns();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public CreatureHome GetHome(string creatureId)
        {
            _homes.TryGetValue(creatureId, out var home);
            return home;
        }

        public bool IsHomeUnlocked(string creatureId)
        {
            if (!_homes.TryGetValue(creatureId, out var home)) return false;
            var bond = _bonding?.GetBondLevel(creatureId) ?? 0;
            return bond >= home.bondToUnlock;
        }

        public CreatureHomeState GetHomeState(string creatureId)
        {
            var bond = _bonding?.GetBondLevel(creatureId) ?? 0;
            return bond switch { >= 15 => CreatureHomeState.Magical, >= 8 => CreatureHomeState.Decorated, >= 3 => CreatureHomeState.CozyDen, _ => CreatureHomeState.Nest };
        }

        public IEnumerable<CreatureHome> GetAllHomes() => _homes.Values;

        // ─── Private Logic ────────────────────────────────────────────────────────

        private void RegisterCreatureHomes()
        {
            Add("pip",  "Pip's Scout Burrow",       bondToUnlock: 2,  returnHour: 18f);
            Add("mimi", "Mimi's Song Branch",        bondToUnlock: 3,  returnHour: 7f);
            Add("tomo", "Tomo's Stone Shell Home",   bondToUnlock: 3,  returnHour: 20f);
            Add("luma", "Luma's Glow Hollow",        bondToUnlock: 4,  returnHour: 21f);
            Add("nori", "Nori's Grove Clearing",     bondToUnlock: 5,  returnHour: 6f);
            Add("sol",  "Sol's Observatory Perch",   bondToUnlock: 8,  returnHour: 23f);
        }

        private void Add(string id, string name, int bondToUnlock, float returnHour)
        {
            _homes[id] = new CreatureHome { creatureId = id, displayName = name, bondToUnlock = bondToUnlock, homeReturnHour = returnHour };
        }

        private void OnBondLevelChanged(string creatureId, int newLevel)
        {
            var state = GetHomeState(creatureId);
            if (_homes.TryGetValue(creatureId, out var home) && home.lastKnownState != state)
            {
                home.lastKnownState = state;
                OnHomeUpgraded?.Invoke(creatureId, state);
                _vfx?.OnDiscovery(Vector2.zero);
                Debug.Log($"[CreatureHomeBehavior] {creatureId} home upgraded to {state}");
            }
        }

        private void CheckCreatureReturns()
        {
            var hour = _time?.CurrentTime ?? 12f;
            foreach (var (id, home) in _homes)
            {
                if (!IsHomeUnlocked(id)) continue;
                float diff = Mathf.Abs(hour - home.homeReturnHour);
                if (diff < 0.6f && !home.returnedToday)
                {
                    home.returnedToday = true;
                    OnCreatureReturned?.Invoke(id);
                    _audio?.PlayCreatureCue(id, "greeting");
                    Debug.Log($"[CreatureHomeBehavior] {id} returned home at {hour:F1}h");
                }
                // Reset at midnight
                if (hour < 0.5f) home.returnedToday = false;
            }
        }

        private void OnDestroy()
        {
            if (_bonding != null)
                _bonding.OnBondLevelChanged -= OnBondLevelChanged;
        }
    }

    // ─── Data Types ───────────────────────────────────────────────────────────────

    public enum CreatureHomeState { Nest, CozyDen, Decorated, Magical }

    [Serializable]
    public class CreatureHome
    {
        public string           creatureId;
        public string           displayName;
        public int              bondToUnlock;
        public float            homeReturnHour;    // hour (0-23) when creature returns home
        public CreatureHomeState lastKnownState;
        public bool             returnedToday;
    }
}
