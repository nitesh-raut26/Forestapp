using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Orchestrates the complete first-run experience for Forest Friends Quest.
    ///
    /// Flow:
    ///   1. IntroCinematicController — procedural title card + forest awakening
    ///   2. AdaptiveTutorialBrain  — age-tier selection (Sprout / Scout / Druid)
    ///   3. GuidedTutorialSystem   — guided first puzzle (choice puzzle, no fail state)
    ///   4. FirstBondSequence      — meet Pip, first bond moment, sanctuary reveal
    ///   5. Normal play begins     — UIStateController.GoTo(Play)
    ///
    /// State:
    ///   Onboarding completion stored in ModularSaveSystem "onboarding" module.
    ///   IsComplete() returns true once finished — checked on every app launch.
    ///
    /// Skip:
    ///   Debug builds can skip via PlayerPrefs "FFQ.SkipOnboarding" = 1.
    /// </summary>
    public class OnboardingDirector : MonoBehaviour
    {
        private IntroCinematicController _intro;
        private GuidedTutorialSystem     _tutorial;
        private AdaptiveTutorialBrain    _brain;
        private FirstBondSequence        _bond;
        private UIStateController        _uiState;
        private SaveModule               _module;
        private ForestMusicDirector      _music;

        private RectTransform  _overlayRoot;
        private CanvasGroup    _overlay;
        private bool           _running;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize(UIStateController uiState, ModularSaveSystem modSave,
            ForestMusicDirector music, RectTransform canvasRoot)
        {
            _uiState = uiState;
            _music   = music;
            _module  = modSave?.RegisterModule("onboarding", version: 1);

            _overlayRoot = CreateOverlay(canvasRoot);

            _intro    = _overlayRoot.gameObject.AddComponent<IntroCinematicController>();
            _brain    = _overlayRoot.gameObject.AddComponent<AdaptiveTutorialBrain>();
            _tutorial = _overlayRoot.gameObject.AddComponent<GuidedTutorialSystem>();
            _bond     = _overlayRoot.gameObject.AddComponent<FirstBondSequence>();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public bool IsComplete() =>
            _module?.GetBool("complete", false) ?? false;

        public bool ShouldSkip() =>
            PlayerPrefs.GetInt("FFQ.SkipOnboarding", 0) == 1;

        public void StartOnboarding()
        {
            if (_running) return;
            _running = true;
            _overlayRoot.gameObject.SetActive(true);
            StartCoroutine(RunFlow());
        }

        // ─── Flow ─────────────────────────────────────────────────────────────────

        private IEnumerator RunFlow()
        {
            // Fade in overlay
            yield return FadeOverlay(0f, 1f, 0.5f);

            // 1. Cinematic intro
            _music?.SetContext("ritual"); // soft atmospheric music
            yield return _intro.PlayIntro(_overlayRoot);

            // 2. Age-tier selection
            string tier = null;
            yield return _brain.PromptAgeTier(_overlayRoot, t => tier = t);
            SaveTier(tier);

            // 3. Guided first puzzle
            yield return _tutorial.RunTutorial(_overlayRoot, tier);

            // 4. First creature bond
            yield return _bond.PlayFirstBondSequence(_overlayRoot);

            // Complete
            _module?.SetBool("complete", true);
            _module?.Set("completedDate", DateTime.UtcNow.ToString("O"));

            // Fade to game
            yield return FadeOverlay(1f, 0f, 0.8f);
            _overlayRoot.gameObject.SetActive(false);
            _running = false;

            _uiState?.GoTo(UIStateController.UIState.Play);
            _music?.SetContext("explore_meadow");

            Debug.Log("[OnboardingDirector] Onboarding complete. Welcome to the forest.");
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private void SaveTier(string tier)
        {
            if (string.IsNullOrEmpty(tier)) tier = "scout";
            _module?.Set("tier", tier);
            PlayerPrefs.SetString("FFQ.Tier", tier);
        }

        private IEnumerator FadeOverlay(float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _overlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _overlay.alpha = to;
        }

        private RectTransform CreateOverlay(RectTransform parent)
        {
            var go  = new GameObject("OnboardingOverlay");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.12f, 0.08f, 1f);

            _overlay = go.AddComponent<CanvasGroup>();
            _overlay.alpha = 0f;

            go.SetActive(false);
            return rt;
        }
    }
}
