using System;

namespace ForestFriendsQuest
{
    [Serializable]
    public class ForestGameContent
    {
        public GameSummary summary;
        public NavigationTabData[] navigationTabs;
        public CharacterProfile[] characters;
        public ForestZoneData[] zones;
        public LevelData[] levels;
        public RewardMilestoneData[] rewards;
        public BiomeData[] biomes;
        public LorePageData[] lore;
        public SeasonalEventData[] seasonalEvents;
        public CreatureEvolutionData[] creatureEvolution;
        public BossData[] bosses;
        public string[] soundDesignPlan;
        public string[] researchHighlights;
        public string[] parentFacingNotes;
        public string[] monetizationPlan;
        public string[] buildMilestones;
    }

    [Serializable]
    public class GameSummary
    {
        public string ageBand;
        public string build;
        public string model;
        public string title;
        public string tagline;
        public string sessionLength;
        public string launchShape;
    }

    [Serializable]
    public class NavigationTabData
    {
        public string id;
        public string label;
    }

    [Serializable]
    public class CharacterProfile
    {
        public string id;
        public string name;
        public string role;
        public string accentHex;
        public string blurb;
        public CharacterVoiceData voice;
        public CharacterLineData lines;
    }

    [Serializable]
    public class CharacterVoiceData
    {
        public float pitch;
        public float rate;
    }

    [Serializable]
    public class CharacterLineData
    {
        public string greeting;
        public string hint;
        public string cheer;
    }

    [Serializable]
    public class ForestZoneData
    {
        public string id;
        public string title;
        public string mood;
        public string challenge;
        public string reward;
        public string accentHex;
        public int unlockAfterClears;
        public bool isPremium;
        public string lockMessage;
    }

    [Serializable]
    public class LevelData
    {
        public string id;
        public string zoneId;
        public string characterId;
        public string name;
        public string type;
        public string difficulty;
        public string reward;
        public string prompt;
        public string hint;
        public string celebration;
        public string gameplayMode;
        public LevelOptionData[] options;
        public string[] memorySequence;
        public int pathColumns;
        public PathCellData[] pathCells;
        public string[] pathSequence;
    }

    [Serializable]
    public class LevelOptionData
    {
        public string id;
        public string label;
        public bool isCorrect;
        public string reply;
    }

    [Serializable]
    public class PathCellData
    {
        public string id;
        public string label;
    }

    [Serializable]
    public class RewardMilestoneData
    {
        public string id;
        public int levels;
        public string title;
        public string detail;
    }

    // ─── AAA Expansion Data Models ────────────────────────────────────────────

    [Serializable]
    public class BiomeData
    {
        public string regionId;
        public string displayName;
        public string ambientTrackId;
        public float  musicTempo;
        public string fogColorHex;
        public string ambientLightHex;
        public string groundTintHex;
        public string skyTintHex;
        public float  pollenDensity;
        public float  fireflydensity;
        public float  mistDensity;
        public float  leafDensity;
        public string[] residentCreatureIds;
    }

    // NOTE: The runtime LoreEntry class (id, title, content, zoneId, collected) is
    // defined in ExplorationAnalyticsSystem.cs. This content-file model is named
    // LorePageData to avoid CS0101 duplicate-type error.
    [Serializable]
    public class LorePageData
    {
        public string id;
        public string regionId;
        public string title;
        public string body;            // the lore page text shown in-game
        public string creatureNarrator; // which guide reads it aloud
        public bool   isSecret;
    }

    [Serializable]
    public class SeasonalEventData
    {
        public string id;
        public string title;
        public string description;
        public string season;          // "Spring", "Summer", "Autumn", "Winter"
        public int    triggerDay;      // day within the season when it starts (0-29)
        public int    durationDays;
        public int    rewardTreats;
        public string rewardDescription;
        public string achievementId;
    }

    [Serializable]
    public class CreatureEvolutionData
    {
        public string  creatureId;
        public string  stageName;
        public int     requiredBondLevel;
        public string  description;
        public string  spriteVariantId;
        public string[] unlockedGestures;
        public int     treatBonus;
    }

    [Serializable]
    public class BossData
    {
        public string   id;
        public string   name;
        public string   lore;
        public string   regionId;
        public int      healthPhases;
        public string[] abilities;         // BossAbility enum names as strings
        public string   rewardDescription;
        public int      rewardTreats;
        public string   victoryAchievementId;
    }
}
