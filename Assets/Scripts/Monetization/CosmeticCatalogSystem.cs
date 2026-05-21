using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// CosmeticCatalogSystem — manages which premium cosmetic packs are unlocked.
    ///
    /// Tracks three categories of premium content:
    ///   1. Cosmetic packs (sanctuary furniture sets, creature accessories)
    ///   2. Seasonal themes (full seasonal visual overhaul per season)
    ///   3. Lore packs (story page collections for the Memory Scrapbook)
    ///   4. Feature flags (creature album expanded layouts, etc.)
    ///
    /// All state is persisted via PlayerPrefs with a save-migration path so
    /// purchased content survives app updates.
    /// </summary>
    public class CosmeticCatalogSystem : MonoBehaviour
    {
        public event Action<string> OnPackUnlocked;
        public event Action<string> OnThemeUnlocked;
        public event Action<string> OnLorePackUnlocked;
        public event Action<string> OnFeatureUnlocked;

        private readonly HashSet<string> _unlockedPacks    = new();
        private readonly HashSet<string> _unlockedThemes   = new();
        private readonly HashSet<string> _unlockedLore     = new();
        private readonly HashSet<string> _unlockedFeatures = new();

        private const string PacksKey    = "FFQ.Cosmetic.Packs";
        private const string ThemesKey   = "FFQ.Cosmetic.Themes";
        private const string LoreKey     = "FFQ.Cosmetic.Lore";
        private const string FeaturesKey = "FFQ.Cosmetic.Features";

        public void Initialize()
        {
            LoadFromPrefs(_unlockedPacks,    PacksKey);
            LoadFromPrefs(_unlockedThemes,   ThemesKey);
            LoadFromPrefs(_unlockedLore,     LoreKey);
            LoadFromPrefs(_unlockedFeatures, FeaturesKey);
            Debug.Log("[CosmeticCatalogSystem] Loaded cosmetic entitlements.");
        }

        // ─── Unlock API ──────────────────────────────────────────────────────────

        public void UnlockPack(string packId)
        {
            if (_unlockedPacks.Add(packId))
            {
                SaveToPrefs(_unlockedPacks, PacksKey);
                OnPackUnlocked?.Invoke(packId);
                Debug.Log($"[CosmeticCatalogSystem] Pack unlocked: {packId}");
            }
        }

        public void UnlockSeasonalTheme(string season)
        {
            if (_unlockedThemes.Add(season))
            {
                SaveToPrefs(_unlockedThemes, ThemesKey);
                OnThemeUnlocked?.Invoke(season);
                Debug.Log($"[CosmeticCatalogSystem] Theme unlocked: {season}");
            }
        }

        public void UnlockLorePack(string packId)
        {
            if (_unlockedLore.Add(packId))
            {
                SaveToPrefs(_unlockedLore, LoreKey);
                OnLorePackUnlocked?.Invoke(packId);
                Debug.Log($"[CosmeticCatalogSystem] Lore pack unlocked: {packId}");
            }
        }

        public void UnlockFeature(string featureId)
        {
            if (_unlockedFeatures.Add(featureId))
            {
                SaveToPrefs(_unlockedFeatures, FeaturesKey);
                OnFeatureUnlocked?.Invoke(featureId);
                Debug.Log($"[CosmeticCatalogSystem] Feature unlocked: {featureId}");
            }
        }

        // ─── Query API ───────────────────────────────────────────────────────────

        public bool IsPackUnlocked(string packId)       => _unlockedPacks.Contains(packId);
        public bool IsThemeUnlocked(string season)      => _unlockedThemes.Contains(season);
        public bool IsLorePackUnlocked(string packId)   => _unlockedLore.Contains(packId);
        public bool IsFeatureUnlocked(string featureId) => _unlockedFeatures.Contains(featureId);

        // ─── Catalog Metadata (displayed in the store UI) ────────────────────────

        public IReadOnlyList<CosmeticEntry> GetStoreListing()
        {
            return new List<CosmeticEntry>
            {
                new CosmeticEntry(IAPManager.Products.StarterPack,      "Starter Decor Pack",
                    "A cozy collection of 8 forest decorations for your sanctuary.",
                    CosmeticCategory.DecorPack,  "$0.99"),

                new CosmeticEntry(IAPManager.Products.WinterTheme,      "Winter Wonderland Theme",
                    "Transform your sanctuary into a snow-dusted magical forest.",
                    CosmeticCategory.SeasonTheme, "$1.99"),

                new CosmeticEntry(IAPManager.Products.SpringTheme,      "Blooming Spring Theme",
                    "Cherry blossoms and firefly lanterns fill your sanctuary.",
                    CosmeticCategory.SeasonTheme, "$1.99"),

                new CosmeticEntry(IAPManager.Products.SummerTheme,      "Sunlit Summer Theme",
                    "Golden hours and shimmering ponds for warm adventure days.",
                    CosmeticCategory.SeasonTheme, "$1.99"),

                new CosmeticEntry(IAPManager.Products.AutumnTheme,      "Autumn Harvest Theme",
                    "Amber leaves and glowing mushroom circles fill the grove.",
                    CosmeticCategory.SeasonTheme, "$1.99"),

                new CosmeticEntry(IAPManager.Products.DruidLorePack,    "Druid Lore Pack",
                    "12 ancient story pages for the Memory Scrapbook.",
                    CosmeticCategory.LorePack,   "$1.49"),

                new CosmeticEntry(IAPManager.Products.AncientLorePack,  "Ancient Ruins Lore Pack",
                    "10 forgotten rune pages from the Forgotten Ruins region.",
                    CosmeticCategory.LorePack,   "$1.49"),

                new CosmeticEntry(IAPManager.Products.PremiumDecorPack, "Premium Decor Collection",
                    "30 exclusive decorations including glowing crystals and moonlit bridges.",
                    CosmeticCategory.DecorPack,  "$2.99"),

                new CosmeticEntry(IAPManager.Products.CreatureAlbum,    "Creature Album Expansion",
                    "Unlock illustrated creature story cards for all 6 companions.",
                    CosmeticCategory.Feature,    "$1.99"),

                new CosmeticEntry(IAPManager.Products.AllAccess,        "Forest Friends All-Access",
                    "Everything included — all themes, lore packs, decor, and the creature album.",
                    CosmeticCategory.Bundle,     "$7.99"),
            };
        }

        // ─── Persistence ─────────────────────────────────────────────────────────

        private static void SaveToPrefs(HashSet<string> set, string key)
        {
            PlayerPrefs.SetString(key, string.Join(",", set));
            PlayerPrefs.Save();
        }

        private static void LoadFromPrefs(HashSet<string> set, string key)
        {
            var raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var id in raw.Split(','))
                if (!string.IsNullOrEmpty(id)) set.Add(id);
        }
    }

    // ─── Data ─────────────────────────────────────────────────────────────────────

    public enum CosmeticCategory { DecorPack, SeasonTheme, LorePack, Feature, Bundle }

    public class CosmeticEntry
    {
        public string          ProductId;
        public string          Title;
        public string          Description;
        public CosmeticCategory Category;
        public string          DisplayPrice;

        public CosmeticEntry(string productId, string title, string desc,
                             CosmeticCategory cat, string price)
        {
            ProductId    = productId;
            Title        = title;
            Description  = desc;
            Category     = cat;
            DisplayPrice = price;
        }
    }
}
