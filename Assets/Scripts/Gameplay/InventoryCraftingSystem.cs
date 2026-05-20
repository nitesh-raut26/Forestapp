using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum ItemCategory
    {
        Treat,
        Ingredient,
        Relic,
        CraftingComponent,
        SanctuaryDecor
    }

    [Serializable]
    public class InventoryItem
    {
        public string       id;
        public string       displayName;
        public string       description;
        public ItemCategory category;
        public int          quantity;
        public bool         isRare;
    }

    [Serializable]
    public class CraftingRecipe
    {
        public string   outputItemId;
        public string   outputDisplayName;
        public string   description;
        public int      outputQuantity;
        public string[] ingredientIds;
        public int[]    ingredientQuantities;
        public int      requiredBondLevel;   // bond with any creature to unlock
    }

    /// <summary>
    /// Inventory and crafting system. Manages the player's item collection,
    /// ingredient storage, and alchemical crafting recipes.
    /// Designed for Arch-Druid tier complexity with scout-accessible basics.
    /// </summary>
    public class InventoryCraftingSystem : MonoBehaviour
    {
        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<InventoryItem>  OnItemAdded;
        public event Action<InventoryItem>  OnItemRemoved;
        public event Action<string>         OnCraftingSuccess;
        public event Action<string>         OnCraftingFailed;

        // ─── State ───────────────────────────────────────────────────────────────

        private readonly Dictionary<string, InventoryItem>  _inventory = new Dictionary<string, InventoryItem>();
        private readonly List<CraftingRecipe>               _recipes   = new List<CraftingRecipe>();
        private EmotionalBondingEngine                      _bonding;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(EmotionalBondingEngine bonding)
        {
            _bonding = bonding;
            BuildItemRegistry();
            BuildRecipeCatalog();
        }

        // ─── Inventory API ────────────────────────────────────────────────────────

        public void AddItem(string itemId, int quantity = 1)
        {
            if (!_inventory.TryGetValue(itemId, out var item))
            {
                item = CreateItemFromRegistry(itemId);
                if (item == null)
                {
                    item = new InventoryItem { id = itemId, displayName = itemId, quantity = 0 };
                }
                _inventory[itemId] = item;
            }

            item.quantity += quantity;
            OnItemAdded?.Invoke(item);
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            if (!_inventory.TryGetValue(itemId, out var item)) return false;
            if (item.quantity < quantity) return false;

            item.quantity -= quantity;
            OnItemRemoved?.Invoke(item);
            return true;
        }

        public int GetQuantity(string itemId)
        {
            return _inventory.TryGetValue(itemId, out var item) ? item.quantity : 0;
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            return GetQuantity(itemId) >= quantity;
        }

        public IReadOnlyCollection<InventoryItem> GetAll()   => _inventory.Values;

        public IReadOnlyList<InventoryItem> GetByCategory(ItemCategory cat)
        {
            var result = new List<InventoryItem>();
            foreach (var item in _inventory.Values)
            {
                if (item.category == cat && item.quantity > 0) result.Add(item);
            }
            return result;
        }

        // ─── Crafting API ─────────────────────────────────────────────────────────

        public IReadOnlyList<CraftingRecipe> GetAllRecipes()         => _recipes;

        public IReadOnlyList<CraftingRecipe> GetAvailableRecipes()
        {
            var result = new List<CraftingRecipe>();
            foreach (var r in _recipes)
            {
                if (CanCraft(r)) result.Add(r);
            }
            return result;
        }

        public bool CanCraft(CraftingRecipe recipe)
        {
            if (recipe.ingredientIds == null) return false;

            // Check ingredient quantities
            for (var i = 0; i < recipe.ingredientIds.Length; i++)
            {
                var needed = i < recipe.ingredientQuantities.Length
                    ? recipe.ingredientQuantities[i] : 1;

                if (!HasItem(recipe.ingredientIds[i], needed)) return false;
            }

            // Check bond requirement
            if (recipe.requiredBondLevel > 1 && _bonding != null)
            {
                var highestBond = GetHighestBondLevel();
                if (highestBond < recipe.requiredBondLevel) return false;
            }

            return true;
        }

        /// <summary>Attempt to craft an item. Consumes ingredients on success.</summary>
        public bool TryCraft(string outputItemId)
        {
            CraftingRecipe recipe = null;
            foreach (var r in _recipes)
            {
                if (r.outputItemId == outputItemId) { recipe = r; break; }
            }

            if (recipe == null || !CanCraft(recipe))
            {
                OnCraftingFailed?.Invoke(outputItemId);
                return false;
            }

            // Consume ingredients
            for (var i = 0; i < recipe.ingredientIds.Length; i++)
            {
                var needed = i < recipe.ingredientQuantities.Length
                    ? recipe.ingredientQuantities[i] : 1;
                RemoveItem(recipe.ingredientIds[i], needed);
            }

            // Grant output
            AddItem(recipe.outputItemId, recipe.outputQuantity);
            OnCraftingSuccess?.Invoke(outputItemId);

            Debug.Log($"[Crafting] Crafted: {recipe.outputDisplayName}");
            return true;
        }

        // ─── Sync with Save ───────────────────────────────────────────────────────

        public int GetTotalTreats()
        {
            return GetQuantity("forest_treat");
        }

        // ─── Private Helpers ──────────────────────────────────────────────────────

        private int GetHighestBondLevel()
        {
            if (_bonding == null) return 1;
            var ids = new[] { "pip", "mimi", "tomo", "luma", "nori", "sol" };
            var max = 1;
            foreach (var id in ids)
            {
                var state = _bonding.GetBondState(id);
                if (state != null && state.bondLevel > max) max = state.bondLevel;
            }
            return max;
        }

        // ─── Item Registry ────────────────────────────────────────────────────────

        private readonly Dictionary<string, InventoryItem> _itemRegistry =
            new Dictionary<string, InventoryItem>();

        private void BuildItemRegistry()
        {
            Register("forest_treat",       "Forest Treat",          "A warm gift for forest friends.",            ItemCategory.Treat,             false);
            Register("amber_pinecone",     "Amber Pinecone",        "Pip's favourite snack.",                     ItemCategory.Treat,             false);
            Register("sunberry",           "Sunberry",              "A golden berry that tastes like summer.",     ItemCategory.Treat,             false);
            Register("river_moss",         "River Moss",            "Cool moss from Moonlit Creek.",              ItemCategory.Treat,             false);
            Register("glowing_sap",        "Glowing Sap",           "Luma glows brighter when near this.",        ItemCategory.Treat,             true);
            Register("wild_fern",          "Wild Fern",             "A fresh fern Nori enjoys.",                  ItemCategory.Treat,             false);
            Register("midnight_truffle",   "Midnight Truffle",      "Found only at night. Sol treasures it.",     ItemCategory.Treat,             true);

            Register("moon_petal",         "Moon Petal",            "Blooms during Moon Blossom events.",         ItemCategory.Ingredient,        true);
            Register("stardust",           "Stardust",              "Collected during meteor showers.",           ItemCategory.Ingredient,        true);
            Register("ancient_fragment",   "Ancient Fragment",      "A shard from the Forgotten Ruins.",          ItemCategory.Ingredient,        false);
            Register("crystal_shard",      "Crystal Shard",         "Glints in the Crystal Caverns.",             ItemCategory.Ingredient,        false);
            Register("light_crystal",      "Light Crystal",         "Focuses light for mirror puzzles.",          ItemCategory.Ingredient,        false);
            Register("alchemical_dust",    "Alchemical Dust",       "The residue of magical transformations.",    ItemCategory.Ingredient,        false);
            Register("eclipse_rune",       "Eclipse Rune",          "Only found during eclipse rituals.",         ItemCategory.Ingredient,        true);

            Register("ancient_lens",       "Ancient Telescope Lens","Allows reading the night sky.",              ItemCategory.Relic,             true);
            Register("forest_compass",     "Forest Compass",        "Always points toward the next discovery.",   ItemCategory.Relic,             true);
            Register("dream_key",          "Dream Forest Key",      "Opens the path to the Endless Dream Forest.",ItemCategory.Relic,             true);
            Register("elder_seal",         "Elder Seal",            "A seal of trust from the Elder Oak.",        ItemCategory.Relic,             true);

            Register("campfire_kit",       "Campfire Kit",          "Builds a cozy campfire in the sanctuary.",   ItemCategory.SanctuaryDecor,    false);
            Register("moon_lantern",       "Moon Lantern",          "Glows softly in the sanctuary at night.",    ItemCategory.SanctuaryDecor,    false);
            Register("dream_seedling_pot", "Dream Seedling Pot",    "Grows magical plants in the sanctuary.",     ItemCategory.SanctuaryDecor,    false);
        }

        private void Register(string id, string displayName, string desc,
            ItemCategory cat, bool isRare)
        {
            _itemRegistry[id] = new InventoryItem
            {
                id = id, displayName = displayName, description = desc,
                category = cat, isRare = isRare, quantity = 0
            };
        }

        private InventoryItem CreateItemFromRegistry(string id)
        {
            if (!_itemRegistry.TryGetValue(id, out var template)) return null;
            return new InventoryItem
            {
                id = template.id, displayName = template.displayName,
                description = template.description,
                category = template.category, isRare = template.isRare, quantity = 0
            };
        }

        // ─── Recipe Catalog ───────────────────────────────────────────────────────

        private void BuildRecipeCatalog()
        {
            // Scout-accessible recipes
            Recipe("campfire_kit",   "Campfire Kit",       "A cozy campfire for your sanctuary.", 1,
                new[] { "ancient_fragment", "crystal_shard" },
                new[] { 2, 1 }, bondRequired: 1);

            Recipe("moon_lantern",   "Moon Lantern",       "Softly glowing sanctuary lantern.", 1,
                new[] { "moon_petal", "glowing_sap" },
                new[] { 3, 1 }, bondRequired: 2);

            // Druid-tier recipes
            Recipe("forest_compass", "Forest Compass",     "A relic that finds hidden paths.", 1,
                new[] { "ancient_fragment", "stardust", "alchemical_dust" },
                new[] { 3, 2, 4 }, bondRequired: 3);

            Recipe("elder_seal",     "Elder Seal",         "Unlocks the Elder's hidden grove.", 1,
                new[] { "eclipse_rune", "moon_petal", "crystal_shard" },
                new[] { 1, 5, 3 }, bondRequired: 4);

            Recipe("dream_key",      "Dream Forest Key",   "Opens the path to the Endless Dream.", 1,
                new[] { "elder_seal", "stardust", "ancient_lens" },
                new[] { 1, 5, 1 }, bondRequired: 5);
        }

        private void Recipe(string outputId, string outputName, string desc, int outputQty,
            string[] ingredients, int[] quantities, int bondRequired)
        {
            _recipes.Add(new CraftingRecipe
            {
                outputItemId      = outputId,
                outputDisplayName = outputName,
                description       = desc,
                outputQuantity    = outputQty,
                ingredientIds     = ingredients,
                ingredientQuantities = quantities,
                requiredBondLevel = bondRequired
            });
        }
    }
}
