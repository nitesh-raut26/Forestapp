using System;

namespace ForestFriendsQuest
{
    [Serializable]
    public class ForestSaveData
    {
        public int version = 4;
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

        // ─── AAA Expansion Fields ──────────────────────────────────────────────

        // Seasonal event tracking
        public string[] attendedSeasonalEventIds;    // event ids the player has attended

        // Lore discovery (flat list of discovered lore page ids across all regions)
        public string[] discoveredLoreIds;

        // Boss defeated flags
        public string[] defeatedBossIds;

        // World region unlock state (region ids that are unlocked beyond default)
        public string[] unlockedRegionIds;

        // Creature evolution: serialised as "creatureId:stageIndex" strings
        public string[] creatureEvolutionState;

        // Season/day tracking for offline progression
        public int  totalInGameDays;     // incremented per real calendar day played
        public int  currentSeasonIndex;  // 0=Spring, 1=Summer, 2=Autumn, 3=Winter

        // Achievement state — stored in JSON so it survives app reinstall on Android
        // (PlayerPrefs is wiped on reinstall; persistent data path is not)
        public string[] unlockedAchievementIds;
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
