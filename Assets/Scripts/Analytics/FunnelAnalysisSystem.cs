using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// FunnelAnalysisSystem — tracks key conversion funnels without PII.
    ///
    /// Funnels monitored:
    ///   1. Onboarding funnel  : Start → NameCreature → FirstBond → FirstPuzzle → Tutorial Done
    ///   2. Engagement funnel  : Daily Ritual → Sanctuary Edit → Lore Collect → Boss Attempt
    ///   3. Conversion funnel  : Store Opened → Item Inspected → Parent Gate → Purchased
    ///   4. Difficulty funnel  : Puzzle Start → Hint Used → Puzzle Failed → Retry → Skipped
    ///
    /// Each funnel step is logged as a Firebase event with the step name and
    /// a funnel ID so BigQuery can reconstruct drop-off rates.
    /// </summary>
    public class FunnelAnalysisSystem : MonoBehaviour
    {
        private FirebaseAnalyticsConnector _firebase;

        // Track which funnel steps have been completed this session (prevents duplicates).
        private readonly HashSet<string> _completedSteps = new();

        public void Initialize(FirebaseAnalyticsConnector firebase)
        {
            _firebase = firebase;
        }

        // ─── Onboarding Funnel ───────────────────────────────────────────────────

        public void OnOnboardingStarted()        => LogStep("onboarding", "started");
        public void OnCreatureNamed()             => LogStep("onboarding", "creature_named");
        public void OnFirstBondFormed()           => LogStep("onboarding", "first_bond");
        public void OnFirstPuzzleAttempted()      => LogStep("onboarding", "first_puzzle");
        public void OnTutorialCompleted()         => LogStep("onboarding", "tutorial_done");

        // ─── Engagement Funnel ───────────────────────────────────────────────────

        public void OnDailyRitualStarted()        => LogStep("engagement", "ritual_started");
        public void OnSanctuaryEdited()           => LogStep("engagement", "sanctuary_edited");
        public void OnLoreCollected()             => LogStep("engagement", "lore_collected");
        public void OnBossAttempted(string bossId)=> LogStep("engagement", "boss_attempted",
                                                             new() { { "boss_id", bossId } });

        // ─── Conversion Funnel ───────────────────────────────────────────────────

        public void OnStoreOpened()               => LogStep("conversion", "store_opened");
        public void OnItemInspected(string id)    => LogStep("conversion", "item_inspected",
                                                             new() { { "product_id", id } });
        public void OnParentGateShown()           => LogStep("conversion", "gate_shown");
        public void OnParentGateApproved()        => LogStep("conversion", "gate_approved");
        public void OnPurchaseCompleted(string id)=> LogStep("conversion", "purchased",
                                                             new() { { "product_id", id } });

        // ─── Difficulty Funnel ───────────────────────────────────────────────────

        public void OnPuzzleStarted(string type)  => LogStep("difficulty", "puzzle_started",
                                                             new() { { "type", type } });
        public void OnHintUsed(string puzzleId)   => LogStep("difficulty", "hint_used",
                                                             new() { { "puzzle_id", puzzleId } });
        public void OnPuzzleFailed(string type)   => LogStep("difficulty", "puzzle_failed",
                                                             new() { { "type", type } });
        public void OnPuzzleRetried()             => LogStep("difficulty", "retried");
        public void OnPuzzleSkipped(string type)  => LogStep("difficulty", "skipped",
                                                             new() { { "type", type } });

        // ─── Core ────────────────────────────────────────────────────────────────

        private void LogStep(string funnelId, string stepName,
                             Dictionary<string, object> extra = null)
        {
            var key = $"{funnelId}_{stepName}";
            _completedSteps.Add(key);

            var parameters = new Dictionary<string, object>
            {
                { "funnel_id", funnelId },
                { "step",      stepName }
            };

            if (extra != null)
                foreach (var kv in extra)
                    parameters[kv.Key] = kv.Value;

            _firebase?.LogEvent("funnel_step", parameters);
        }

        // ─── Funnel Completion Rate Helpers ──────────────────────────────────────

        public bool IsOnboardingComplete()
            => _completedSteps.Contains("onboarding_tutorial_done");

        public float GetOnboardingCompletionRate()
        {
            int steps = 0;
            if (_completedSteps.Contains("onboarding_started"))        steps++;
            if (_completedSteps.Contains("onboarding_creature_named")) steps++;
            if (_completedSteps.Contains("onboarding_first_bond"))     steps++;
            if (_completedSteps.Contains("onboarding_first_puzzle"))   steps++;
            if (_completedSteps.Contains("onboarding_tutorial_done"))  steps++;
            return steps / 5f;
        }
    }
}
