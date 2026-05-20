using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum DecorCategory
    {
        Furniture,
        Plant,
        Campfire,
        Light,
        Relic,
        Seasonal
    }

    [Serializable]
    public class DecorItem
    {
        public string        id;
        public string        displayName;
        public string        description;
        public DecorCategory category;
        public int           gridWidth;
        public int           gridHeight;

        // Unlock conditions
        public int           requiredLevelsCleared;
        public int           requiredBondLevel;
        public string        requiredQuestId;
        public bool          isSeasonal;
        public string        seasonalSeason;   // "spring","summer","autumn","winter"

        // Visual hints
        public Color         themeColor;
        public bool          emitsLight;
        public bool          isRare;
    }

    /// <summary>
    /// ScriptableObject-style catalog (implemented as MonoBehaviour for Unity
    /// compatibility without requiring a ScriptableObject asset).
    ///
    /// Contains all 30+ decoration items with unlock conditions and visual data.
    /// Queried by the SanctuaryBuilderManager to show the placement palette.
    /// </summary>
    public class SanctuaryDecorationCatalog : MonoBehaviour
    {
        // ─── System Links ─────────────────────────────────────────────────────────

        private EmotionalBondingEngine _bonding;
        private QuestEngine            _quests;

        // ─── Catalog ──────────────────────────────────────────────────────────────

        private readonly List<DecorItem> _catalog = new List<DecorItem>();

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(EmotionalBondingEngine bonding, QuestEngine quests)
        {
            _bonding = bonding;
            _quests  = quests;
            BuildCatalog();
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public IReadOnlyList<DecorItem> GetAll() => _catalog;

        /// <summary>Get all items unlocked for the current save state.</summary>
        public IReadOnlyList<DecorItem> GetUnlocked(ForestSaveData saveData)
        {
            var result  = new List<DecorItem>();
            var cleared = saveData?.levelProgress?.Length ?? 0;

            foreach (var item in _catalog)
            {
                if (IsUnlocked(item, cleared)) result.Add(item);
            }
            return result;
        }

        public IReadOnlyList<DecorItem> GetByCategory(DecorCategory cat)
        {
            var result = new List<DecorItem>();
            foreach (var item in _catalog)
            {
                if (item.category == cat) result.Add(item);
            }
            return result;
        }

        public DecorItem GetItem(string id)
        {
            foreach (var item in _catalog)
            {
                if (item.id == id) return item;
            }
            return null;
        }

        // ─── Unlock Check ─────────────────────────────────────────────────────────

        public bool IsUnlocked(DecorItem item, int clearedLevels)
        {
            // Level threshold
            if (item.requiredLevelsCleared > 0 && clearedLevels < item.requiredLevelsCleared)
                return false;

            // Bond level threshold
            if (item.requiredBondLevel > 1 && _bonding != null)
            {
                var highestBond = GetHighestBond();
                if (highestBond < item.requiredBondLevel) return false;
            }

            // Quest prerequisite
            if (!string.IsNullOrEmpty(item.requiredQuestId) && _quests != null)
            {
                if (!_quests.IsQuestComplete(item.requiredQuestId)) return false;
            }

            return true;
        }

        // ─── Catalog Builder ──────────────────────────────────────────────────────

        private void BuildCatalog()
        {
            // ── Campfire / Fire ───────────────────────────────────────────────────
            AddItem("campfire_small",  "Forest Campfire",       "A cozy crackling campfire.",                   DecorCategory.Campfire,  1, 1, 0, 1, "",             false, "",       new Color(1.0f, 0.55f, 0.20f), true,  false);
            AddItem("campfire_grand",  "Grand Bonfire",         "A larger bonfire that lights the sanctuary.",  DecorCategory.Campfire,  2, 1, 5, 2, "",             false, "",       new Color(1.0f, 0.45f, 0.10f), true,  false);
            AddItem("spirit_flame",    "Spirit Flame",          "A magical blue flame from the Dream Forest.",  DecorCategory.Campfire,  1, 1, 0, 4, "druid_q01",  false, "",       new Color(0.50f, 0.70f, 1.0f), true,  true);

            // ── Furniture ─────────────────────────────────────────────────────────
            AddItem("mossy_log",       "Mossy Log Seat",        "A cozy log perfect for resting.",              DecorCategory.Furniture, 2, 1, 1, 1, "",             false, "",       new Color(0.35f, 0.55f, 0.30f), false, false);
            AddItem("dream_hammock",   "Dream Hammock",         "Swing gently between ancient trees.",          DecorCategory.Furniture, 2, 1, 3, 2, "",             false, "",       new Color(0.80f, 0.70f, 0.50f), false, false);
            AddItem("stone_table",     "Ancient Stone Table",   "A flat stone used by forest druids.",          DecorCategory.Furniture, 2, 1, 4, 3, "scout_q01",  false, "",       new Color(0.65f, 0.65f, 0.60f), false, false);
            AddItem("crystal_bench",   "Crystal Bench",         "Carved from the Crystal Caverns.",             DecorCategory.Furniture, 2, 1, 0, 4, "scout_q03",  false, "",       new Color(0.60f, 0.90f, 1.00f), false, true);
            AddItem("elder_throne",    "Elder Throne",          "Sat in only by proven Arch-Druids.",           DecorCategory.Furniture, 2, 2, 8, 5, "druid_q03",  false, "",       new Color(0.70f, 0.55f, 0.30f), false, true);

            // ── Plants ────────────────────────────────────────────────────────────
            AddItem("seedling_pot",    "Dream Seedling Pot",    "A glowing seedling grows within.",             DecorCategory.Plant,     1, 1, 0, 1, "",             false, "",       new Color(0.50f, 0.85f, 0.45f), false, false);
            AddItem("moon_blossom",    "Moon Blossom",          "Blooms softly at night.",                      DecorCategory.Plant,     1, 1, 2, 2, "",             false, "",       new Color(0.75f, 0.60f, 1.00f), true,  false);
            AddItem("fern_cluster",    "Fern Cluster",          "Dense, swaying forest ferns.",                 DecorCategory.Plant,     2, 1, 1, 1, "",             false, "",       new Color(0.30f, 0.70f, 0.35f), false, false);
            AddItem("glowing_mushroom","Glowing Mushroom Ring", "An ancient ring of luminescent mushrooms.",    DecorCategory.Plant,     2, 2, 6, 3, "scout_q02",  false, "",       new Color(0.80f, 1.00f, 0.60f), true,  true);
            AddItem("elder_sapling",   "Elder Sapling",         "A young Elder Oak. May grow one day.",         DecorCategory.Plant,     1, 2, 0, 4, "druid_q01",  false, "",       new Color(0.40f, 0.65f, 0.30f), false, true);

            // ── Lights ────────────────────────────────────────────────────────────
            AddItem("moon_lantern",    "Moon Lantern",          "Glows with soft moon energy at night.",        DecorCategory.Light,     1, 1, 0, 2, "",             false, "",       new Color(0.90f, 0.95f, 1.00f), true,  false);
            AddItem("firefly_jar",     "Firefly Jar",           "A gentle jar full of captured fireflies.",     DecorCategory.Light,     1, 1, 2, 2, "",             false, "",       new Color(0.75f, 1.00f, 0.50f), true,  false);
            AddItem("crystal_lantern", "Crystal Lantern",       "Emits prismatic light from crystal shards.",   DecorCategory.Light,     1, 1, 0, 3, "scout_q03",  false, "",       new Color(0.55f, 0.90f, 1.00f), true,  true);
            AddItem("star_beacon",     "Star Beacon",           "A beacon that calls meteor showers closer.",   DecorCategory.Light,     1, 2, 0, 5, "druid_q02",  false, "",       new Color(0.80f, 0.80f, 1.00f), true,  true);

            // ── Relics ────────────────────────────────────────────────────────────
            AddItem("rune_pillar",     "Rune Pillar",           "An ancient carved rune stone.",                DecorCategory.Relic,     1, 2, 4, 3, "scout_q03",  false, "",       new Color(0.60f, 0.85f, 0.75f), false, false);
            AddItem("ancient_sundial", "Ancient Sundial",       "Tells time by shadow and starlight.",          DecorCategory.Relic,     1, 1, 0, 4, "druid_q01",  false, "",       new Color(0.85f, 0.75f, 0.40f), false, true);
            AddItem("dream_portal",    "Dream Portal Fragment", "A shard of the sealed Dream World portal.",    DecorCategory.Relic,     2, 2, 0, 5, "druid_q03",  false, "",       new Color(0.70f, 0.50f, 1.00f), true,  true);

            // ── Seasonal ──────────────────────────────────────────────────────────
            AddItem("spring_wreaths",  "Spring Blossom Wreath", "Petals that drift in a warm breeze.",          DecorCategory.Seasonal,  2, 1, 0, 1, "",             true,  "spring", new Color(1.00f, 0.80f, 0.85f), false, false);
            AddItem("autumn_leaves",   "Autumn Leaf Pile",      "A swirling pile of gold and amber leaves.",    DecorCategory.Seasonal,  2, 1, 0, 1, "",             true,  "autumn", new Color(0.90f, 0.55f, 0.20f), false, false);
            AddItem("winter_icicle",   "Enchanted Icicle",      "Sparkles with eternal cold magic.",            DecorCategory.Seasonal,  1, 2, 0, 1, "",             true,  "winter", new Color(0.80f, 0.95f, 1.00f), true,  false);
            AddItem("summer_sunflower","Dream Sunflower",       "Turns to face any source of light.",           DecorCategory.Seasonal,  1, 2, 0, 1, "",             true,  "summer", new Color(1.00f, 0.90f, 0.30f), false, false);
        }

        private void AddItem(string id, string name, string desc,
            DecorCategory cat, int w, int h, int levelReq, int bondReq, string questReq,
            bool seasonal, string season, Color color, bool emitsLight, bool isRare)
        {
            _catalog.Add(new DecorItem
            {
                id                    = id,
                displayName           = name,
                description           = desc,
                category              = cat,
                gridWidth             = w,
                gridHeight            = h,
                requiredLevelsCleared = levelReq,
                requiredBondLevel     = bondReq,
                requiredQuestId       = questReq,
                isSeasonal            = seasonal,
                seasonalSeason        = season,
                themeColor            = color,
                emitsLight            = emitsLight,
                isRare                = isRare
            });
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private int GetHighestBond()
        {
            if (_bonding == null) return 1;
            var ids = new[] { "pip", "mimi", "tomo", "luma", "nori", "sol" };
            var max = 1;
            foreach (var id in ids)
            {
                var s = _bonding.GetBondState(id);
                if (s != null && s.bondLevel > max) max = s.bondLevel;
            }
            return max;
        }
    }
}
