using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Step-by-step guided tutorial sequencer.
    ///
    /// Drives the first-time player experience through contextual, visual-only
    /// tutorial steps. NO walls of text — every step is action-based.
    ///
    /// Works with AdaptiveTutorialBrain to select age-appropriate steps
    /// and with FirstBondSequence for the emotional creature intro.
    /// </summary>
    public class GuidedTutorialSystem : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<TutorialStep>  OnStepStarted;
        public event Action<TutorialStep>  OnStepCompleted;
        public event Action               OnTutorialComplete;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly List<TutorialStep> _steps        = new();
        private int                         _currentIndex  = -1;
        private bool                        _active;
        private bool                        _completed;

        private AdaptiveTutorialBrain _brain;
        private SaveSystem            _save;
        private VFXManager            _vfx;
        private DynamicDialogueSystem _dialogue;

        private const string CompletedKey = "FFQ.Tutorial.Done";

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            AdaptiveTutorialBrain brain,
            SaveSystem            save,
            VFXManager            vfx,
            DynamicDialogueSystem dialogue)
        {
            _brain    = brain;
            _save     = save;
            _vfx      = vfx;
            _dialogue = dialogue;

            _completed = PlayerPrefs.GetInt(CompletedKey, 0) == 1;

            BuildStepSequence();
            Debug.Log($"[GuidedTutorialSystem] Initialized. Tutorial done: {_completed}");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns true if this player has never completed the tutorial.</summary>
        public bool NeedsTutorial() => !_completed;

        /// <summary>Start the tutorial from the beginning.</summary>
        public void StartTutorial()
        {
            if (_active) return;
            _active = true;
            _currentIndex = -1;
            AdvanceStep();
        }

        /// <summary>Mark the current step complete and advance.</summary>
        public void CompleteCurrentStep()
        {
            if (!_active || _currentIndex < 0 || _currentIndex >= _steps.Count) return;

            var step = _steps[_currentIndex];
            step.isCompleted = true;
            OnStepCompleted?.Invoke(step);

            _vfx?.OnDiscovery(Vector2.zero);

            StartCoroutine(DelayThenAdvance(step.pauseAfterSeconds));
        }

        /// <summary>Skip the entire tutorial (player taps skip).</summary>
        public void SkipTutorial()
        {
            _active = false;
            MarkTutorialComplete();
        }

        /// <summary>Get the current step (null if tutorial isn't active).</summary>
        public TutorialStep GetCurrentStep()
            => _active && _currentIndex >= 0 && _currentIndex < _steps.Count
                ? _steps[_currentIndex]
                : null;

        // ─── Step Sequence ────────────────────────────────────────────────────────

        private void BuildStepSequence()
        {
            _steps.Clear();

            var tier = _save?.ActiveData?.explorerTier ?? "scout";
            var steps = _brain?.GetStepsForTier(tier) ?? DefaultSteps();
            _steps.AddRange(steps);
        }

        private void AdvanceStep()
        {
            _currentIndex++;

            if (_currentIndex >= _steps.Count)
            {
                FinishTutorial();
                return;
            }

            var step = _steps[_currentIndex];
            OnStepStarted?.Invoke(step);

            // Trigger optional dialogue hint
            if (!string.IsNullOrEmpty(step.dialogueHintKey) && _dialogue != null)
            {
                var seq = _dialogue.GetAdaptedSequence(step.guideCharacterId, step.dialogueHintKey);
                if (seq != null) _dialogue.StartSequence(seq);
            }

            // Auto-complete steps that don't require player input
            if (step.isAutoComplete)
                StartCoroutine(DelayThenAdvance(step.autoCompleteDuration));
        }

        private IEnumerator DelayThenAdvance(float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, delay));
            AdvanceStep();
        }

        private void FinishTutorial()
        {
            _active = false;
            MarkTutorialComplete();
            OnTutorialComplete?.Invoke();
            _vfx?.OnRareReward(Vector2.zero);
            Debug.Log("[GuidedTutorialSystem] Tutorial complete!");
        }

        private void MarkTutorialComplete()
        {
            _completed = true;
            PlayerPrefs.SetInt(CompletedKey, 1);
            PlayerPrefs.Save();
        }

        private List<TutorialStep> DefaultSteps()
        {
            return new List<TutorialStep>
            {
                new() { id="intro_forest",    title="Welcome to the Forest",   guideCharacterId="pip", dialogueHintKey="greeting",   isAutoComplete=true,  autoCompleteDuration=3f, pauseAfterSeconds=0.5f },
                new() { id="meet_pip",        title="Meet Pip",                guideCharacterId="pip", dialogueHintKey="greeting",   isAutoComplete=true,  autoCompleteDuration=4f, pauseAfterSeconds=0.5f },
                new() { id="tap_first_level", title="Tap Your First Mission",  guideCharacterId="pip", dialogueHintKey="hint",       isAutoComplete=false, pauseAfterSeconds=0.3f },
                new() { id="solve_puzzle",    title="Solve the Puzzle",        guideCharacterId="pip", dialogueHintKey="hint",       isAutoComplete=false, pauseAfterSeconds=0.5f },
                new() { id="earn_reward",     title="Earn Your First Reward",  guideCharacterId="pip", dialogueHintKey="cheer",      isAutoComplete=true,  autoCompleteDuration=3f, pauseAfterSeconds=0.5f },
                new() { id="visit_sanctuary", title="Visit Your Sanctuary",    guideCharacterId="tomo",dialogueHintKey="greeting",   isAutoComplete=false, pauseAfterSeconds=0.5f },
                new() { id="first_bond",      title="Bond with a Creature",    guideCharacterId="pip", dialogueHintKey="cheer",      isAutoComplete=false, pauseAfterSeconds=1f   },
            };
        }
    }

    // ─── Data Types ───────────────────────────────────────────────────────────────

    [Serializable]
    public class TutorialStep
    {
        public string id;
        public string title;
        public string guideCharacterId;
        public string dialogueHintKey;
        public bool   isAutoComplete;
        public float  autoCompleteDuration = 2f;
        public float  pauseAfterSeconds    = 0.5f;
        public bool   isCompleted;
    }
}
