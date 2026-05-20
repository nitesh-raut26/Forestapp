using System;

namespace ForestFriendsQuest
{
    [Serializable]
    public class ForestSaveData
    {
        public int version = 3;
        public bool premiumUnlocked;
        public bool soundEnabled = true;
        public int totalLevelAttempts;
        public int totalHintsUsed;
        public int totalWrongAnswers;
        public LevelProgressData[] levelProgress;
        public string lastSelectedZoneId;
        public string lastSelectedLevelId;

        // Multi-Age Tier Bracket Setting (sp sprout, scout, druid)
        public string explorerTier = "scout";

        // High-Retention Classic Features (Ages 4-16)
        public int sproutGrowth = 1;
        public int forestTreats = 4;
        public int pipBond  = 1;
        public int mimiBond = 1;
        public int tomoBond = 1;
        public int lumaBond = 1;
        public int noriBond = 1;
        public int solBond  = 1;
        public bool dailyTrialCleared;

        // Raw Crafting Resources (Arch-Druid Tier)
        public int elderwood = 3;
        public int riverCrystals = 1;
        public int fireflyDust = 2;
        public int ancientSap = 1;

        // Sanctuary placements and crafted rewards
        public string[]            craftedItemIds;
        public PlacedSanctuaryItem[] placedItems;

        // New grid-based placement save (ForestSystemsContainer v2)
        public PlacedItem[] sanctuaryGridItems;
    }

    [Serializable]
    public class PlacedSanctuaryItem
    {
        public string itemId;
        public float posX;
        public float posY;
        public float scale = 1f;
    }

    [Serializable]
    public class LevelProgressData
    {
        public string levelId;
        public bool completed;
        public int bestStars;
        public int timesPlayed;
        public int timesCleared;
    }
}
