using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Full decoration placement, theming, and management system for the Sanctuary.
    ///
    /// Manages the catalogue of purchasable and craftable sanctuary items,
    /// handles seasonal visual theming, collectible display walls,
    /// and music boxes. Works alongside SanctuaryPlacementGrid.
    ///
    /// Philosophy: The Sanctuary is the emotional heart of the game —
    /// every item placed must feel meaningful and personal.
    /// </summary>
    public class SanctuaryDecorationSystem : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<SanctuaryItem> OnItemUnlocked;
        public event Action<SanctuaryItem> OnItemPlaced;
        public event Action<SanctuaryItem> OnItemRemoved;
        public event Action<string>        OnSeasonThemeApplied;  // season name

        // ─── Dependencies ─────────────────────────────────────────────────────────

        private EmotionalBondingEngine _bonding;
        private SaveSystem             _save;
        private VFXManager             _vfx;

        // ─── Catalogue ────────────────────────────────────────────────────────────

        private readonly Dictionary<string, SanctuaryItem> _catalogue  = new();
        private readonly List<string>                       _unlockedIds = new();
        private string _currentSeasonTheme = "spring";

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(EmotionalBondingEngine bonding, SaveSystem save, VFXManager vfx)
        {
            _bonding = bonding;
            _save    = save;
            _vfx     = vfx;

            RegisterAllItems();
            LoadUnlockedState();
            Debug.Log($"[SanctuaryDecorationSystem] Loaded {_catalogue.Count} items, {_unlockedIds.Count} unlocked.");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public IReadOnlyDictionary<string, SanctuaryItem> GetCatalogue() => _catalogue;

        public IReadOnlyList<string> GetUnlockedIds() => _unlockedIds;

        public SanctuaryItem GetItem(string id)
        {
            _catalogue.TryGetValue(id, out var item);
            return item;
        }

        public bool IsUnlocked(string id) => _unlockedIds.Contains(id);

        public void UnlockItem(string id)
        {
            if (!_catalogue.TryGetValue(id, out var item)) return;
            if (_unlockedIds.Contains(id)) return;

            _unlockedIds.Add(id);
            PersistUnlocked();
            OnItemUnlocked?.Invoke(item);
            _vfx?.OnDiscovery(Vector2.zero);
            Debug.Log($"[SanctuaryDecorationSystem] Unlocked: {item.displayName}");
        }

        public void NotifyPlaced(string id)
        {
            if (_catalogue.TryGetValue(id, out var item))
                OnItemPlaced?.Invoke(item);
        }

        public void NotifyRemoved(string id)
        {
            if (_catalogue.TryGetValue(id, out var item))
                OnItemRemoved?.Invoke(item);
        }

        /// <summary>Apply a seasonal visual theme to the sanctuary.</summary>
        public void ApplySeasonTheme(string season)
        {
            _currentSeasonTheme = season.ToLower();

            // Unlock seasonal exclusive items
            foreach (var item in _catalogue.Values)
            {
                if (item.seasonalSeason == _currentSeasonTheme && !_unlockedIds.Contains(item.id))
                    UnlockItem(item.id);
            }

            OnSeasonThemeApplied?.Invoke(_currentSeasonTheme);
            Debug.Log($"[SanctuaryDecorationSystem] Season theme applied: {_currentSeasonTheme}");
        }

        public string CurrentSeasonTheme => _currentSeasonTheme;

        // ─── Save / Load ──────────────────────────────────────────────────────────

        private void PersistUnlocked()
        {
            PlayerPrefs.SetString("FFQ.Sanctuary.Unlocked", string.Join(",", _unlockedIds));
            PlayerPrefs.Save();
        }

        private void LoadUnlockedState()
        {
            _unlockedIds.Clear();
            var raw = PlayerPrefs.GetString("FFQ.Sanctuary.Unlocked", "campfire_kit");
            if (!string.IsNullOrEmpty(raw))
                _unlockedIds.AddRange(raw.Split(','));

            // Always ensure starter items are unlocked
            foreach (var id in new[] { "campfire_kit", "mossy_stone", "fern_pot" })
                if (!_unlockedIds.Contains(id)) _unlockedIds.Add(id);
        }

        // ─── Item Registry ────────────────────────────────────────────────────────

        private void RegisterAllItems()
        {
            // ── Starter / Free ────────────────────────────────────────────────────
            Add("campfire_kit",       "Cozy Campfire",         SanctuaryCategory.Structure,  cost: 0,  bond: 0,  seasonal: null);
            Add("mossy_stone",        "Mossy Stone",           SanctuaryCategory.Decoration, cost: 0,  bond: 0,  seasonal: null);
            Add("fern_pot",           "Fern Pot",              SanctuaryCategory.Plant,      cost: 0,  bond: 0,  seasonal: null);

            // ── Bond-Unlocked ─────────────────────────────────────────────────────
            Add("moon_lantern",       "Moon Lantern",          SanctuaryCategory.Decoration, cost: 0,  bond: 3,  seasonal: null);
            Add("dream_seedling",     "Dream Seedling",        SanctuaryCategory.Plant,      cost: 0,  bond: 5,  seasonal: null);
            Add("crystal_fountain",   "Crystal Fountain",      SanctuaryCategory.Structure,  cost: 0,  bond: 8,  seasonal: null);
            Add("elder_oak_sapling",  "Elder Oak Sapling",     SanctuaryCategory.Plant,      cost: 0,  bond: 12, seasonal: null);
            Add("starmap_stone",      "Star Map Stone",        SanctuaryCategory.Decoration, cost: 0,  bond: 15, seasonal: null);
            Add("ancient_archway",    "Ancient Archway",       SanctuaryCategory.Structure,  cost: 0,  bond: 20, seasonal: null);

            // ── Craftable ─────────────────────────────────────────────────────────
            Add("music_box",          "Forest Music Box",      SanctuaryCategory.Magical,    cost: 0,  bond: 10, seasonal: null);
            Add("firefly_jar",        "Firefly Jar",           SanctuaryCategory.Magical,    cost: 0,  bond: 6,  seasonal: null);
            Add("creature_nest",      "Creature Nest",         SanctuaryCategory.Structure,  cost: 0,  bond: 8,  seasonal: null);
            Add("lore_wall",          "Ancient Lore Wall",     SanctuaryCategory.Collectible, cost:0,  bond: 18, seasonal: null);
            Add("trophy_shelf",       "Trophy Shelf",          SanctuaryCategory.Collectible, cost:0,  bond: 14, seasonal: null);

            // ── Seasonal: Spring ─────────────────────────────────────────────────
            Add("spring_blossoms",    "Spring Blossoms",       SanctuaryCategory.Plant,      cost: 0,  bond: 0, seasonal: "spring");
            Add("butterfly_garden",   "Butterfly Garden",      SanctuaryCategory.Magical,    cost: 0,  bond: 0, seasonal: "spring");
            Add("rain_puddle_mirror", "Rain Puddle Mirror",    SanctuaryCategory.Decoration, cost: 0,  bond: 0, seasonal: "spring");

            // ── Seasonal: Summer ─────────────────────────────────────────────────
            Add("sun_dial",           "Forest Sun Dial",       SanctuaryCategory.Structure,  cost: 0,  bond: 0, seasonal: "summer");
            Add("firefly_hammock",    "Firefly Hammock",       SanctuaryCategory.Structure,  cost: 0,  bond: 0, seasonal: "summer");
            Add("berry_basket_decor", "Berry Basket",          SanctuaryCategory.Decoration, cost: 0,  bond: 0, seasonal: "summer");

            // ── Seasonal: Autumn ─────────────────────────────────────────────────
            Add("leaf_pile",          "Golden Leaf Pile",      SanctuaryCategory.Decoration, cost: 0,  bond: 0, seasonal: "autumn");
            Add("harvest_basket",     "Harvest Basket",        SanctuaryCategory.Decoration, cost: 0,  bond: 0, seasonal: "autumn");
            Add("mushroom_ring",      "Mushroom Ring",         SanctuaryCategory.Magical,    cost: 0,  bond: 0, seasonal: "autumn");

            // ── Seasonal: Winter ─────────────────────────────────────────────────
            Add("frost_crystal",      "Frost Crystal",         SanctuaryCategory.Magical,    cost: 0,  bond: 0, seasonal: "winter");
            Add("snow_lantern",       "Snow Lantern",          SanctuaryCategory.Decoration, cost: 0,  bond: 0, seasonal: "winter");
            Add("cozy_blanket_nest",  "Cozy Blanket Nest",     SanctuaryCategory.Structure,  cost: 0,  bond: 0, seasonal: "winter");

            // ── Premium / Boss Rewards ────────────────────────────────────────────
            Add("elder_fountain",     "Elder Crystal Fountain",SanctuaryCategory.Magical,    cost: 0,  bond: 25, seasonal: null);
            Add("world_map_stone",    "World Map Stone",       SanctuaryCategory.Collectible, cost:0,  bond: 30, seasonal: null);
        }

        private void Add(string id, string name, SanctuaryCategory cat, int cost, int bond, string seasonal)
        {
            _catalogue[id] = new SanctuaryItem
            {
                id             = id,
                displayName    = name,
                category       = cat,
                treatCost      = cost,
                bondRequired   = bond,
                seasonalSeason = seasonal
            };
        }
    }

    // ─── Data Types ───────────────────────────────────────────────────────────────

    public enum SanctuaryCategory { Structure, Decoration, Plant, Magical, Collectible }

    [Serializable]
    public class SanctuaryItem
    {
        public string           id;
        public string           displayName;
        public SanctuaryCategory category;
        public int              treatCost;
        public int              bondRequired;
        public string           seasonalSeason;   // null = always available
    }
}
