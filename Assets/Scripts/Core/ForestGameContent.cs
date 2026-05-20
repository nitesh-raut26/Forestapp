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
}
