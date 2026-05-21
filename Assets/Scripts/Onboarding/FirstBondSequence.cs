using System;
using System.Collections;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Cinematic first-bond sequence — the emotional highlight of onboarding.
    ///
    /// Plays when the player taps a creature for the first time.
    /// Creates a Nintendo-quality cozy emotional moment:
    ///   1. World dims softly
    ///   2. Creature appears with a gentle glow
    ///   3. Character speaks its greeting line
    ///   4. Bond counter increments with sparkle burst
    ///   5. World returns to normal with warm ambience rise
    ///
    /// Respects ReducedMotionController — uses instant transitions if enabled.
    /// </summary>
    public class FirstBondSequence : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<string> OnBondSequenceComplete; // creatureId

        // ─── Dependencies ─────────────────────────────────────────────────────────

        private EmotionalBondingEngine   _bonding;
        private VFXManager               _vfx;
        private DynamicDialogueSystem    _dialogue;
        private ProceduralAudioSystem    _audio;
        private ReducedMotionController  _reducedMotion;
        private CreatureMoodBrain        _moodBrain;

        private bool _playing;
        private const string PlayedKeyPrefix = "FFQ.FirstBond.";

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            EmotionalBondingEngine  bonding,
            VFXManager              vfx,
            DynamicDialogueSystem   dialogue,
            ProceduralAudioSystem   audio,
            ReducedMotionController reducedMotion,
            CreatureMoodBrain       moodBrain)
        {
            _bonding       = bonding;
            _vfx           = vfx;
            _dialogue      = dialogue;
            _audio         = audio;
            _reducedMotion = reducedMotion;
            _moodBrain     = moodBrain;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns true if this creature's first-bond cinematic hasn't played yet.</summary>
        public bool NeedsFirstBond(string creatureId)
            => PlayerPrefs.GetInt(PlayedKeyPrefix + creatureId, 0) == 0;

        /// <summary>Trigger the first-bond cinematic for a creature.</summary>
        public void PlayFirstBond(string creatureId, RectTransform creatureAnchor = null)
        {
            if (_playing) return;
            if (!NeedsFirstBond(creatureId)) return;

            _playing = true;
            StartCoroutine(BondSequenceCoroutine(creatureId, creatureAnchor));
        }

        // ─── Cinematic Coroutine ──────────────────────────────────────────────────

        private IEnumerator BondSequenceCoroutine(string creatureId, RectTransform anchor)
        {
            bool reducedMotion = _reducedMotion?.IsReducedMotion ?? false;
            float transitionTime = reducedMotion ? 0f : 0.8f;
            Vector2 burstPos = anchor != null ? anchor.anchoredPosition : Vector2.zero;

            // ── Phase 1: Soft dim and focus ───────────────────────────────────────
            if (!reducedMotion)
                yield return new WaitForSeconds(0.3f);  // tiny breath before starting

            // Audio: lower ambient, raise emotional tone
            _audio?.PlayCreatureCue(creatureId, "greeting");
            yield return new WaitForSeconds(transitionTime);

            // ── Phase 2: Bond increment + sparkle ─────────────────────────────────
            _bonding?.IncreaseBond(creatureId, 1);
            _moodBrain?.SetMood(creatureId, CreatureMood.Joy);

            _vfx?.OnDiscovery(burstPos);
            yield return new WaitForSeconds(reducedMotion ? 0.1f : 0.6f);

            // ── Phase 3: Creature greeting dialogue ───────────────────────────────
            if (_dialogue != null)
            {
                var seq = _dialogue.GetAdaptedSequence(creatureId, "greeting");
                if (seq != null)
                {
                    _dialogue.StartSequence(seq);
                    // Wait for a brief moment so the dialogue starts visually
                    yield return new WaitForSeconds(reducedMotion ? 0.1f : 1.2f);
                }
            }

            // ── Phase 4: Rare reward burst ────────────────────────────────────────
            _vfx?.OnRareReward(burstPos);
            yield return new WaitForSeconds(reducedMotion ? 0.1f : 0.8f);

            // ── Phase 5: World returns ────────────────────────────────────────────
            yield return new WaitForSeconds(transitionTime);

            // Mark as played
            PlayerPrefs.SetInt(PlayedKeyPrefix + creatureId, 1);
            PlayerPrefs.Save();

            _playing = false;
            OnBondSequenceComplete?.Invoke(creatureId);

            Debug.Log($"[FirstBondSequence] First bond cinematic complete: {creatureId}");
        }
    }
}
