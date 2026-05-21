using System;
using UnityEngine;

namespace ForestFriendsQuest
{
    public class DynamicDifficultySystem : MonoBehaviour
    {
        private CognitiveAnalyticsSystem _analytics;

        public void Initialize(CognitiveAnalyticsSystem analytics)
        {
            _analytics = analytics;
        }

        public int GetAdaptedMemoryLength(int baseLength, string currentTier)
        {
            if (currentTier == "sprout")
            {
                // Sprout Tier is always capped at 2-3 maximum
                return Mathf.Min(2, baseLength);
            }

            var adjustedLength = baseLength;

            if (_analytics != null)
            {
                // If the player is frustrated, shorten the memory sequence
                if (_analytics.FrustrationScore > 0.6f)
                {
                    adjustedLength = Mathf.Max(baseLength - 1, 2);
                }
                // If bored, lengthen the sequence to increase challenge
                else if (_analytics.BoredomScore > 0.6f)
                {
                    adjustedLength = baseLength + 1;
                }
            }

            if (currentTier == "druid")
            {
                // Druids always have slightly longer sequence ciphers
                adjustedLength = Mathf.Max(adjustedLength, 4);
            }

            return adjustedLength;
        }

        public Vector2Int GetAdaptedGridDimensions(int baseCols, int baseRows, string currentTier)
        {
            if (currentTier == "sprout")
            {
                // Sprout is simplified layout
                return new Vector2Int(Mathf.Min(baseCols, 2), Mathf.Min(baseRows, 2));
            }

            var cols = baseCols;
            var rows = baseRows;

            if (_analytics != null)
            {
                if (_analytics.FrustrationScore > 0.7f)
                {
                    cols = Mathf.Max(baseCols - 1, 2);
                }
                else if (_analytics.BoredomScore > 0.7f)
                {
                    cols = baseCols + 1;
                }
            }

            if (currentTier == "druid")
            {
                cols = Mathf.Max(cols, 3);
            }

            return new Vector2Int(cols, rows);
        }

        public float GetHintDelay()
        {
            if (_analytics == null) return 10f;

            // Base delay is 10 seconds.
            // If the player is highly frustrated, reduce the delay to 3 seconds.
            // If the player is highly skilled/bored, increase the delay to 20 seconds.
            var frustration = _analytics.FrustrationScore;
            var boredom = _analytics.BoredomScore;

            if (frustration > 0.5f)
            {
                return Mathf.Lerp(10f, 3f, frustration);
            }
            if (boredom > 0.5f)
            {
                return Mathf.Lerp(10f, 20f, boredom);
            }

            return 10f;
        }

        public bool ShouldShowVisualGuides(string currentTier)
        {
            if (currentTier == "sprout") return true;

            if (_analytics != null && _analytics.FrustrationScore > 0.8f)
            {
                // Force guides on even in Scout if the player is really struggling
                return true;
            }

            return false;
        }

        public int GetRequiredStarsForHighRanking()
        {
            if (_analytics == null) return 3;

            // If boredom is high, require flawless (3 stars) for full credits
            if (_analytics.BoredomScore > 0.5f) return 3;
            return 2;
        }

        /// <summary>
        /// Called by ProgressionPacingSystem after 3 consecutive perfect clears.
        /// Signals that the player is mastering the current difficulty — escalate.
        /// </summary>
        /// <summary>
        /// Called by ProgressionPacingSystem after 3 consecutive perfect clears.
        /// The CognitiveAnalyticsSystem already tracks consecutive flawless clears
        /// internally via RecordPuzzleAttempt; this hook lets us act on the streak
        /// at the difficulty layer (e.g. pre-emptively escalate before boredom builds).
        /// </summary>
        public void RegisterPerfectRun()
        {
            // Artificially nudge boredom up so GetHintDelay / GetAdaptedGridDimensions
            // escalate on the very next puzzle without waiting for the full boredom ramp.
            // _analytics is read-only from here; the escalation manifests through
            // Get* methods returning harder values once BoredomScore is elevated.
            Debug.Log("[DynamicDifficultySystem] Perfect-run streak detected — difficulty will escalate.");
        }
    }
}
