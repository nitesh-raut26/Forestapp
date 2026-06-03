using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Selects and adapts tutorial step sequences based on the player's age tier.
    ///
    /// Sprout (4-6)  : maximum hand-holding, large buttons, visual-only hints, 3 steps
    /// Scout (7-11)  : guided discovery, gentle text, 6 steps
    /// Druid (12-16) : minimal guidance, challenge-first, optional skip, 4 steps
    /// </summary>
    public class AdaptiveTutorialBrain : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<string> OnTierDetected;   // tier

        // ─── State ───────────────────────────────────────────────────────────────

        private string _detectedTier = "scout";

        // ─── Public API ───────────────────────────────────────────────────────────

        public void Initialize(ForestSaveData save)
        {
            _detectedTier = save?.explorerTier ?? "scout";
            OnTierDetected?.Invoke(_detectedTier);
            Debug.Log($"[AdaptiveTutorialBrain] Tier: {_detectedTier}");
        }

        public List<TutorialStep> GetStepsForTier(string tier)
        {
            return tier switch
            {
                "sprout" => SproutSteps(),
                "druid"  => DruidSteps(),
                _        => ScoutSteps()
            };
        }

        /// <summary>
        /// Coroutine: displays tier-selection UI (or auto-detects) then calls back
        /// with the chosen tier string so OnboardingDirector can store it.
        /// </summary>
        public System.Collections.IEnumerator PromptAgeTier(RectTransform parent, System.Action<string> onTierSelected)
        {
            // Use the tier already detected from save data
            onTierSelected?.Invoke(_detectedTier);
            yield break;
        }

        public bool ShouldShowSkipButton(string tier) => tier is "scout" or "druid";
        public bool ShouldShowTextHints(string tier)  => tier != "sprout";
        public float GetTapTargetScale(string tier)   => tier == "sprout" ? 1.4f : 1f;
        public float GetDialoguePacing(string tier)   => tier == "sprout" ? 1.3f : tier == "druid" ? 0.75f : 1f;

        // ─── Per-Tier Step Libraries ───────────────────────────────────────────────

        private static List<TutorialStep> SproutSteps() => new()
        {
            new() { id="sp_forest",   title="The Forest!",            guideCharacterId="pip",  dialogueHintKey="greeting", isAutoComplete=true,  autoCompleteDuration=4f,  pauseAfterSeconds=1f },
            new() { id="sp_pip",      title="This is Pip!",           guideCharacterId="pip",  dialogueHintKey="greeting", isAutoComplete=true,  autoCompleteDuration=4f,  pauseAfterSeconds=1f },
            new() { id="sp_tap",      title="Tap the big button!",    guideCharacterId="pip",  dialogueHintKey="hint",     isAutoComplete=false, pauseAfterSeconds=0.5f },
            new() { id="sp_reward",   title="You did it!",            guideCharacterId="pip",  dialogueHintKey="cheer",    isAutoComplete=true,  autoCompleteDuration=4f,  pauseAfterSeconds=1f },
        };

        private static List<TutorialStep> ScoutSteps() => new()
        {
            new() { id="sc_welcome",   title="Welcome to the Forest",  guideCharacterId="pip",  dialogueHintKey="greeting", isAutoComplete=true,  autoCompleteDuration=3f,  pauseAfterSeconds=0.5f },
            new() { id="sc_pip",       title="Meet Pip",               guideCharacterId="pip",  dialogueHintKey="greeting", isAutoComplete=true,  autoCompleteDuration=3.5f,pauseAfterSeconds=0.5f },
            new() { id="sc_first",     title="Your First Mission",     guideCharacterId="pip",  dialogueHintKey="hint",     isAutoComplete=false, pauseAfterSeconds=0.3f },
            new() { id="sc_solve",     title="Solve the Puzzle",       guideCharacterId="pip",  dialogueHintKey="hint",     isAutoComplete=false, pauseAfterSeconds=0.5f },
            new() { id="sc_reward",    title="Your First Reward",      guideCharacterId="pip",  dialogueHintKey="cheer",    isAutoComplete=true,  autoCompleteDuration=3f,  pauseAfterSeconds=0.5f },
            new() { id="sc_sanctuary", title="Your Sanctuary",         guideCharacterId="tomo", dialogueHintKey="greeting", isAutoComplete=false, pauseAfterSeconds=0.5f },
        };

        private static List<TutorialStep> DruidSteps() => new()
        {
            new() { id="dr_world",  title="The Ancient Forest Awaits",  guideCharacterId="sol", dialogueHintKey="greeting", isAutoComplete=true,  autoCompleteDuration=3f,  pauseAfterSeconds=0.3f },
            new() { id="dr_sol",    title="Sol Speaks",                  guideCharacterId="sol", dialogueHintKey="hint",     isAutoComplete=true,  autoCompleteDuration=3f,  pauseAfterSeconds=0.3f },
            new() { id="dr_cipher", title="The Cipher Awaits",           guideCharacterId="sol", dialogueHintKey="hint",     isAutoComplete=false, pauseAfterSeconds=0.3f },
            new() { id="dr_master", title="Master of the Forest",        guideCharacterId="sol", dialogueHintKey="cheer",    isAutoComplete=true,  autoCompleteDuration=3f,  pauseAfterSeconds=0.5f },
        };
    }
}
