using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum BossPhase { Dormant, Approaching, Active, Defeated }

    public enum BossAbility
    {
        FogWall,          // temporarily obscures some puzzle nodes (Sprout: none)
        MirrorHex,        // flips two mirror tiles for 5 seconds (Scout)
        TimeReverse,      // runs puzzle timer backwards briefly (Druid)
        RuneScramble,     // randomises rune order (Druid)
        AncientRoar       // small screen shake + particle storm (all tiers)
    }

    [Serializable]
    public class BossDefinition
    {
        public string       id;
        public string       name;
        public string       lore;
        public string       regionId;          // which region this boss inhabits
        public int          healthPhases;      // how many puzzle sub-rounds to defeat it
        public BossAbility[] abilities;
        public string       victoryAchievementId;
        public string       rewardDescription;
        public int          rewardTreats;
    }

    /// <summary>
    /// Manages ancient boss encounters — rare, tier-adaptive, non-punishing
    /// multi-round puzzle gauntlets that serve as regional climax events.
    ///
    /// A boss encounter is a sequence of mini-puzzles driven by PuzzleManager.
    /// Each successful round chips away one health phase. Failing a round
    /// resets only that round (never the whole fight) — child-friendly design.
    ///
    /// Currently one boss per region; Observatory guardian is the hardest.
    ///
    /// Integration:
    ///   - AchievementSystem: awards victoryAchievementId on win
    ///   - WorldStateManager: calls MarkBossDefeated on win
    ///   - VFXManager: OnRareReward on each phase cleared
    ///   - QuestEngine: ProgressObjective("boss_defeated") on full win
    /// </summary>
    public class BossEncounterSystem : MonoBehaviour
    {
        private AchievementSystem  _achievements;
        private WorldStateManager  _world;
        private VFXManager         _vfx;
        private QuestEngine        _quests;
        private SaveSystem         _saveSystem;

        public event Action<BossDefinition>         OnBossEncounterStarted;
        public event Action<BossDefinition, int>    OnBossPhaseCleared;     // (boss, phasesRemaining)
        public event Action<BossDefinition>         OnBossDefeated;

        // ─── State ───────────────────────────────────────────────────────────────

        private BossDefinition _activeBoss;
        private BossPhase      _phase       = BossPhase.Dormant;
        private int            _phasesLeft;
        private int            _currentRoundMistakes;

        private readonly Dictionary<string, BossDefinition> _bosses =
            new Dictionary<string, BossDefinition>();

        private readonly HashSet<string> _defeatedIds = new HashSet<string>();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            AchievementSystem achievements,
            WorldStateManager world,
            VFXManager        vfx,
            QuestEngine       quests,
            SaveSystem        saveSystem)
        {
            _achievements = achievements;
            _world        = world;
            _vfx          = vfx;
            _quests       = quests;
            _saveSystem   = saveSystem;

            BuildBossRoster();
            LoadDefeatedState();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public bool IsBossDefeated(string bossId) => _defeatedIds.Contains(bossId);
        public BossPhase    CurrentPhase   => _phase;
        public BossDefinition ActiveBoss   => _activeBoss;

        /// <summary>Returns the boss for a region if present and not yet defeated.</summary>
        public BossDefinition GetRegionBoss(string regionId)
        {
            foreach (var boss in _bosses.Values)
            {
                if (boss.regionId == regionId && !_defeatedIds.Contains(boss.id))
                    return boss;
            }
            return null;
        }

        /// <summary>Begin an encounter. Returns false if no boss is available.</summary>
        public bool StartEncounter(string bossId)
        {
            if (!_bosses.TryGetValue(bossId, out var boss)) return false;
            if (_defeatedIds.Contains(bossId)) return false;
            if (_phase == BossPhase.Active) return false;

            _activeBoss   = boss;
            _phasesLeft   = boss.healthPhases;
            _phase        = BossPhase.Approaching;
            _currentRoundMistakes = 0;

            OnBossEncounterStarted?.Invoke(boss);
            Debug.Log($"[BossEncounterSystem] Encounter started: {boss.name}");
            return true;
        }

        /// <summary>Mark the encounter as truly active (after approach animation).</summary>
        public void BeginActivePhase()
        {
            if (_phase != BossPhase.Approaching) return;
            _phase = BossPhase.Active;
        }

        /// <summary>Called when the player completes one puzzle round within the fight.</summary>
        public void RecordRoundCleared(Vector2 vfxPos = default)
        {
            if (_phase != BossPhase.Active || _activeBoss == null) return;

            _phasesLeft--;
            _currentRoundMistakes = 0;

            _vfx?.OnRareReward(vfxPos);
            OnBossPhaseCleared?.Invoke(_activeBoss, _phasesLeft);

            if (_phasesLeft <= 0)
            {
                DefeatBoss(vfxPos);
            }
        }

        /// <summary>Player failed this round — reset only the round (not the full fight).</summary>
        public void RecordRoundFailed()
        {
            if (_phase != BossPhase.Active) return;
            _currentRoundMistakes++;
            // Non-punishing: boss stays at current phase count, player retries
            Debug.Log($"[BossEncounterSystem] Round failed — retry. Mistakes this fight: {_currentRoundMistakes}");
        }

        /// <summary>Player abandons the encounter voluntarily.</summary>
        public void AbandonEncounter()
        {
            if (_phase == BossPhase.Dormant) return;
            _phase      = BossPhase.Dormant;
            _activeBoss = null;
        }

        /// <summary>Returns a random ability the boss should use this round (null for Sprout).</summary>
        public BossAbility? GetNextAbility(string tier)
        {
            if (tier == "sprout"          ||
                _activeBoss == null       ||
                _activeBoss.abilities == null ||
                _activeBoss.abilities.Length == 0)
                return null;

            return _activeBoss.abilities[UnityEngine.Random.Range(0, _activeBoss.abilities.Length)];
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private void DefeatBoss(Vector2 vfxPos)
        {
            _phase = BossPhase.Defeated;
            var boss = _activeBoss;

            _defeatedIds.Add(boss.id);

            if (_saveSystem != null)
                _saveSystem.SetAchievementUnlocked($"Boss.{boss.id}", true);

            _achievements?.TryUnlock(boss.victoryAchievementId);
            _world?.MarkBossDefeated(boss.regionId);
            _quests?.ProgressObjective("boss_defeated");

            OnBossDefeated?.Invoke(boss);

            Debug.Log($"[BossEncounterSystem] Defeated: {boss.name}");

            // Clean up
            _activeBoss = null;
            _phase      = BossPhase.Dormant;
        }

        private void LoadDefeatedState()
        {
            if (_saveSystem == null) return;
            foreach (var boss in _bosses.Values)
            {
                if (_saveSystem.IsAchievementUnlocked($"Boss.{boss.id}"))
                    _defeatedIds.Add(boss.id);
            }
        }

        // ─── Boss Roster ──────────────────────────────────────────────────────────

        private void BuildBossRoster()
        {
            Add(new BossDefinition
            {
                id                   = "creek_shadow",
                name                 = "The Creek Shadow",
                lore                 = "An ancient water spirit that tests those who seek the deeper forest. Tomo has spoken of it in hushed tones.",
                regionId             = "moonlit-creek",
                healthPhases         = 2,
                abilities            = new[] { BossAbility.FogWall, BossAbility.AncientRoar },
                victoryAchievementId = "exp_ruins",
                rewardDescription    = "River Crystal x3 + path to Elderwood opened",
                rewardTreats         = 10
            });

            Add(new BossDefinition
            {
                id                   = "cavern_golem",
                name                 = "Crystal Golem",
                lore                 = "A sentient crystal formation that guards the deepest rune chamber. Sol says it was once a druid's failed experiment.",
                regionId             = "crystal-caverns",
                healthPhases         = 3,
                abilities            = new[] { BossAbility.MirrorHex, BossAbility.RuneScramble, BossAbility.AncientRoar },
                victoryAchievementId = "puz_rune_decode",
                rewardDescription    = "Crystal Lens + passage to Forgotten Ruins unlocked",
                rewardTreats         = 15
            });

            Add(new BossDefinition
            {
                id                   = "ruins_warden",
                name                 = "Ruin Warden",
                lore                 = "The forgotten druid order left a guardian behind. It speaks only in cipher. Sol has been studying its patterns for years.",
                regionId             = "forgotten-ruins",
                healthPhases         = 3,
                abilities            = new[] { BossAbility.RuneScramble, BossAbility.TimeReverse, BossAbility.AncientRoar },
                victoryAchievementId = "puz_cipher_5",
                rewardDescription    = "Ancient Tome fragment + Lore Keeper progress",
                rewardTreats         = 18
            });

            Add(new BossDefinition
            {
                id                   = "observatory_guardian",
                name                 = "Ancient Forest Guardian",
                lore                 = "The oldest living being in the forest. It has watched civilisations rise and fall from the top of the Observatory. Only the bond between a child and all six forest companions can awaken its trust.",
                regionId             = "ancient-observatory",
                healthPhases         = 4,
                abilities            = new[] { BossAbility.FogWall, BossAbility.MirrorHex, BossAbility.TimeReverse, BossAbility.RuneScramble, BossAbility.AncientRoar },
                victoryAchievementId = "sec_ancient_boss",
                rewardDescription    = "Skyroot Canopy access + Star Chart + Ancient Guardian Relic + 25 treats",
                rewardTreats         = 25
            });

            Add(new BossDefinition
            {
                id                   = "dream_weaver",
                name                 = "Dream Weaver",
                lore                 = "The final secret of the Endless Dream Forest. It is not an enemy — it is the forest itself, testing whether you are ready to become a true Arch-Druid.",
                regionId             = "skyroot-canopy",
                healthPhases         = 5,
                abilities            = new[] { BossAbility.FogWall, BossAbility.TimeReverse, BossAbility.RuneScramble, BossAbility.AncientRoar },
                victoryAchievementId = "sec_dream_unlocked",
                rewardDescription    = "Endless Dream Forest fully unlocked + Dream Weaver Relic + 40 treats",
                rewardTreats         = 40
            });
        }

        private void Add(BossDefinition boss) => _bosses[boss.id] = boss;
    }
}
