using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Manages magical reveal moments — the emotional highlights of the game.
    ///
    /// Tracks and triggers:
    ///   - Creature evolution reveal cinematics
    ///   - Region unlock discovery moments
    ///   - Lore page discovery sequences
    ///   - Boss defeat celebrations
    ///   - Seasonal event reveal banners
    ///   - Hidden discovery surprises
    ///   - 100-puzzle milestone moments
    ///
    /// Philosophy: Every major milestone should feel like a Pixar "ta-da!" moment
    /// — completely unexpected and emotionally rewarding.
    /// </summary>
    public class EmotionalMilestoneSystem : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<MilestoneEvent> OnMilestoneTriggered;
        public event Action<MilestoneEvent> OnMilestoneComplete;

        // ─── Dependencies ─────────────────────────────────────────────────────────

        private VFXManager               _vfx;
        private ProceduralAudioSystem    _audio;
        private DynamicDialogueSystem    _dialogue;
        private UIAnimationSystem        _uiAnim;
        private ReducedMotionController  _reducedMotion;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Queue<MilestoneEvent> _pendingMilestones = new();
        private bool _isDisplaying;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            VFXManager              vfx,
            ProceduralAudioSystem   audio,
            DynamicDialogueSystem   dialogue,
            UIAnimationSystem       uiAnim,
            ReducedMotionController reducedMotion)
        {
            _vfx           = vfx;
            _audio         = audio;
            _dialogue      = dialogue;
            _uiAnim        = uiAnim;
            _reducedMotion = reducedMotion;
        }

        private void Update()
        {
            if (!_isDisplaying && _pendingMilestones.Count > 0)
                StartCoroutine(DisplayNextMilestone());
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void TriggerEvolutionReveal(string creatureId, string stageName)
        {
            EnqueueMilestone(new MilestoneEvent
            {
                type        = MilestoneType.Evolution,
                title       = $"{CapFirst(creatureId)} Evolved!",
                subtitle    = $"New stage: {stageName}",
                creatureId  = creatureId,
                intensity   = MilestoneIntensity.Grand,
            });
        }

        public void TriggerRegionUnlock(string regionName)
        {
            EnqueueMilestone(new MilestoneEvent
            {
                type      = MilestoneType.RegionUnlock,
                title     = "New Region Discovered!",
                subtitle  = regionName + " is now open.",
                intensity = MilestoneIntensity.Major,
            });
        }

        public void TriggerLoreDiscovery(string loreTitle)
        {
            EnqueueMilestone(new MilestoneEvent
            {
                type      = MilestoneType.LoreDiscovery,
                title     = "Ancient Lore Found!",
                subtitle  = loreTitle,
                intensity = MilestoneIntensity.Minor,
            });
        }

        public void TriggerBossDefeat(string bossName)
        {
            EnqueueMilestone(new MilestoneEvent
            {
                type      = MilestoneType.BossDefeat,
                title     = $"{bossName} Defeated!",
                subtitle  = "The forest cheers for you!",
                intensity = MilestoneIntensity.Grand,
            });
        }

        public void TriggerPuzzleMilestone(int count)
        {
            EnqueueMilestone(new MilestoneEvent
            {
                type      = MilestoneType.PuzzleMilestone,
                title     = $"{count} Puzzles Solved!",
                subtitle  = GetMilestoneQuote(count),
                intensity = count >= 100 ? MilestoneIntensity.Grand : MilestoneIntensity.Major,
            });
        }

        public void TriggerSeasonalEventReveal(string eventTitle)
        {
            EnqueueMilestone(new MilestoneEvent
            {
                type      = MilestoneType.SeasonalEvent,
                title     = "Seasonal Event!",
                subtitle  = eventTitle + " has arrived!",
                intensity = MilestoneIntensity.Major,
            });
        }

        // ─── Private Logic ────────────────────────────────────────────────────────

        private void EnqueueMilestone(MilestoneEvent milestone)
        {
            _pendingMilestones.Enqueue(milestone);
            OnMilestoneTriggered?.Invoke(milestone);
            Debug.Log($"[EmotionalMilestoneSystem] Queued: {milestone.title}");
        }

        private System.Collections.IEnumerator DisplayNextMilestone()
        {
            _isDisplaying = true;
            var milestone = _pendingMilestones.Dequeue();

            bool reduced = _reducedMotion?.IsReducedMotion ?? false;

            // VFX based on intensity
            switch (milestone.intensity)
            {
                case MilestoneIntensity.Grand:
                    _vfx?.OnRareReward(Vector2.zero);
                    yield return new WaitForSeconds(reduced ? 0f : 0.3f);
                    _vfx?.OnDiscovery(Vector2.zero);
                    break;
                case MilestoneIntensity.Major:
                    _vfx?.OnDiscovery(Vector2.zero);
                    break;
                case MilestoneIntensity.Minor:
                    break;
            }

            // Creature dialogue for evolution/bond moments
            if (!string.IsNullOrEmpty(milestone.creatureId) && _dialogue != null)
            {
                var seq = _dialogue.GetAdaptedSequence(milestone.creatureId, "cheer");
                if (seq != null) _dialogue.StartSequence(seq);
            }

            yield return new WaitForSeconds(reduced ? 0.1f : 2.5f);

            OnMilestoneComplete?.Invoke(milestone);
            _isDisplaying = false;
        }

        private static string GetMilestoneQuote(int count) => count switch
        {
            10  => "The forest stirs with your wisdom!",
            25  => "Pip is amazed by your skills!",
            50  => "The Elder Oak has noticed you.",
            100 => "A legend of the forest is born.",
            200 => "The ancient prophecy is fulfilled.",
            _   => "Your journey continues..."
        };

        private static string CapFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
    }

    // ─── Data Types ───────────────────────────────────────────────────────────────

    public enum MilestoneType { Evolution, RegionUnlock, LoreDiscovery, BossDefeat, PuzzleMilestone, SeasonalEvent, HiddenDiscovery }
    public enum MilestoneIntensity { Minor, Major, Grand }

    [Serializable]
    public class MilestoneEvent
    {
        public MilestoneType      type;
        public string             title;
        public string             subtitle;
        public string             creatureId;
        public MilestoneIntensity intensity;
    }
}
