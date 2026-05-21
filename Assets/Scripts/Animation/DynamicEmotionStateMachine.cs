using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum CreatureEmotion
    {
        Idle,
        Happy,
        Excited,
        Curious,
        Sleepy,
        Shy,
        Playful,
        Proud,
        Sad,
        Hungry
    }

    /// <summary>
    /// Lightweight FSM for a single creature's emotional state.
    /// Transitions are driven by bond level, time-of-day, recent interactions,
    /// and hunger state. No MonoBehaviour — instantiated per-creature by CreatureMoodBrain.
    /// </summary>
    public class DynamicEmotionStateMachine
    {
        public CreatureEmotion CurrentEmotion  { get; private set; } = CreatureEmotion.Idle;
        public CreatureEmotion PreviousEmotion { get; private set; } = CreatureEmotion.Idle;
        public float           EmotionTime     { get; private set; }  // seconds in current state

        public event Action<CreatureEmotion, CreatureEmotion> OnEmotionChanged; // (prev, next)

        private readonly Dictionary<CreatureEmotion, float> _emotionWeights =
            new Dictionary<CreatureEmotion, float>();

        private float _idleTimer;
        private readonly System.Random _rng;

        public DynamicEmotionStateMachine(int seed = 0)
        {
            _rng = new System.Random(seed);
            ResetWeights();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void Tick(float deltaTime)
        {
            EmotionTime += deltaTime;
            _idleTimer  += deltaTime;

            // Spontaneous ambient transitions every 8-20 seconds in Idle
            if (CurrentEmotion == CreatureEmotion.Idle && _idleTimer > GetIdleThreshold())
            {
                _idleTimer = 0f;
                TransitionToWeighted();
            }

            // Auto-return to Idle from transient states
            if (EmotionTime > GetStateDuration(CurrentEmotion) &&
                CurrentEmotion != CreatureEmotion.Idle)
            {
                TransitionTo(CreatureEmotion.Idle);
            }
        }

        /// <summary>Feed a one-shot emotion trigger (overrides weighting temporarily).</summary>
        public void TriggerEmotion(CreatureEmotion emotion)
        {
            TransitionTo(emotion);
        }

        /// <summary>Update baseline weights from bond level (0-5) and time-of-day (0-1).</summary>
        public void SetContext(int bondLevel, float timeOfDay, bool isHungry)
        {
            ResetWeights();

            // Higher bond = more expressive, happier baseline
            var bondFactor = bondLevel / 5f;
            _emotionWeights[CreatureEmotion.Happy]    += bondFactor * 0.4f;
            _emotionWeights[CreatureEmotion.Playful]  += bondFactor * 0.25f;
            _emotionWeights[CreatureEmotion.Excited]  += bondFactor * 0.15f;

            // Night-time → sleepy
            var isNight = timeOfDay > 0.75f || timeOfDay < 0.1f;
            if (isNight)
                _emotionWeights[CreatureEmotion.Sleepy] += 0.5f;

            // Hunger overrides
            if (isHungry)
            {
                _emotionWeights[CreatureEmotion.Hungry] += 0.6f;
                _emotionWeights[CreatureEmotion.Sad]    += 0.2f;
            }
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private void TransitionTo(CreatureEmotion next)
        {
            if (next == CurrentEmotion) return;
            PreviousEmotion = CurrentEmotion;
            CurrentEmotion  = next;
            EmotionTime     = 0f;
            OnEmotionChanged?.Invoke(PreviousEmotion, CurrentEmotion);
        }

        private void TransitionToWeighted()
        {
            var total = 0f;
            foreach (var w in _emotionWeights.Values) total += w;
            if (total <= 0f) return;

            var roll = (float)(_rng.NextDouble() * total);
            foreach (var kv in _emotionWeights)
            {
                roll -= kv.Value;
                if (roll <= 0f) { TransitionTo(kv.Key); return; }
            }
        }

        private void ResetWeights()
        {
            _emotionWeights[CreatureEmotion.Idle]    = 1.0f;
            _emotionWeights[CreatureEmotion.Happy]   = 0.2f;
            _emotionWeights[CreatureEmotion.Curious] = 0.2f;
            _emotionWeights[CreatureEmotion.Playful] = 0.1f;
            _emotionWeights[CreatureEmotion.Shy]     = 0.1f;
            _emotionWeights[CreatureEmotion.Sleepy]  = 0.05f;
            _emotionWeights[CreatureEmotion.Excited] = 0.05f;
            _emotionWeights[CreatureEmotion.Sad]     = 0.02f;
            _emotionWeights[CreatureEmotion.Hungry]  = 0.02f;
            _emotionWeights[CreatureEmotion.Proud]   = 0.05f;
        }

        private float GetIdleThreshold()
        {
            // Low bond creatures are less expressive, trigger ambient state less often
            return 10f + (float)(_rng.NextDouble() * 12f);
        }

        private static float GetStateDuration(CreatureEmotion e)
        {
            return e switch
            {
                CreatureEmotion.Excited => 3.5f,
                CreatureEmotion.Happy   => 5f,
                CreatureEmotion.Playful => 6f,
                CreatureEmotion.Curious => 4f,
                CreatureEmotion.Proud   => 4f,
                CreatureEmotion.Sad     => 6f,
                CreatureEmotion.Hungry  => 999f, // persists until fed
                CreatureEmotion.Sleepy  => 999f, // persists until morning
                CreatureEmotion.Shy     => 3f,
                _                      => float.MaxValue
            };
        }
    }
}
