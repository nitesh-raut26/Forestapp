using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Manages emotion state machines for all 6 creatures.
    /// Receives context updates from BondingEngine and game time, then
    /// broadcasts emotion changes so CreatureAnimationController can respond.
    ///
    /// Tick() is driven by CreatureAnimationController.Update().
    /// </summary>
    public class CreatureMoodBrain : MonoBehaviour
    {
        private EmotionalBondingEngine _bonding;

        private readonly Dictionary<string, DynamicEmotionStateMachine> _machines =
            new Dictionary<string, DynamicEmotionStateMachine>();

        private static readonly string[] CreatureIds =
            { "pip", "mimi", "tomo", "luma", "nori", "sol" };

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize(EmotionalBondingEngine bonding)
        {
            _bonding = bonding;

            for (var i = 0; i < CreatureIds.Length; i++)
            {
                var id  = CreatureIds[i];
                var fsm = new DynamicEmotionStateMachine(seed: id.GetHashCode());
                _machines[id] = fsm;
            }
        }

        private void Update()
        {
            var dt = Time.deltaTime;

            // Normalized time-of-day (0 = midnight, 0.5 = noon, 1 = midnight)
            var tod = (Time.time % 86400f) / 86400f;

            foreach (var kv in _machines)
            {
                var id      = kv.Key;
                var fsm     = kv.Value;
                var bond    = _bonding?.GetBondState(id);
                var isHungry = IsCreatureHungry(id);

                fsm.SetContext(bond?.bondLevel ?? 0, tod, isHungry);
                fsm.Tick(dt);
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public CreatureEmotion GetEmotion(string creatureId)
        {
            _machines.TryGetValue(creatureId, out var fsm);
            return fsm?.CurrentEmotion ?? CreatureEmotion.Idle;
        }

        public DynamicEmotionStateMachine GetFSM(string creatureId)
        {
            _machines.TryGetValue(creatureId, out var fsm);
            return fsm;
        }

        /// <summary>Call when player feeds, pets, or completes a puzzle involving a creature.</summary>
        public void TriggerReaction(string creatureId, CreatureEmotion emotion)
        {
            if (_machines.TryGetValue(creatureId, out var fsm))
                fsm.TriggerEmotion(emotion);
        }

        /// <summary>Call after player completes a level — creatures respond to success/failure.</summary>
        public void OnLevelResult(bool success)
        {
            var emotion = success ? CreatureEmotion.Excited : CreatureEmotion.Sad;

            // Only bonded creatures react
            foreach (var kv in _machines)
            {
                var bond = _bonding?.GetBondState(kv.Key);
                if (bond != null && bond.bondLevel >= 1)
                    kv.Value.TriggerEmotion(emotion);
            }
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private bool IsCreatureHungry(string creatureId)
        {
            // Hunger = no treat interaction in 24 in-game hours (approximated by session count)
            var key = $"FFQ.LastFed.{creatureId}";
            var lastFed = PlayerPrefs.GetInt(key, 0);
            return (Time.frameCount - lastFed) > 3600; // ~1 hour of gameplay
        }

        public void RecordFeed(string creatureId)
        {
            PlayerPrefs.SetInt($"FFQ.LastFed.{creatureId}", Time.frameCount);
        }
    }
}
