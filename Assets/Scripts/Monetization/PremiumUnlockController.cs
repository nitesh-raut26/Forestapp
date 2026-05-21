using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// PremiumUnlockController — applies purchased entitlements to game systems.
    ///
    /// Sits between IAPManager (payment) and the actual content systems.
    /// Decouples the purchase layer from the game layer; IAPManager only calls
    /// ApplyUnlock(id) and this class knows what each product unlocks.
    /// </summary>
    public class PremiumUnlockController : MonoBehaviour
    {
        public event Action<string> OnUnlockApplied;   // productId

        private SanctuaryDecorationSystem _decor;
        private CosmeticCatalogSystem     _catalog;
        private SaveSystem                _save;
        private readonly HashSet<string>  _applied = new();

        public void Initialize(
            SanctuaryDecorationSystem decor,
            CosmeticCatalogSystem     catalog,
            SaveSystem                save)
        {
            _decor   = decor;
            _catalog = catalog;
            _save    = save;
        }

        /// <summary>Apply the entitlement for a purchased product ID.</summary>
        public void ApplyUnlock(string productId)
        {
            if (_applied.Contains(productId)) return;
            _applied.Add(productId);

            switch (productId)
            {
                case IAPManager.Products.StarterPack:
                    _catalog?.UnlockPack("starter");
                    break;

                case IAPManager.Products.WinterTheme:
                    _catalog?.UnlockSeasonalTheme("winter");
                    break;

                case IAPManager.Products.SpringTheme:
                    _catalog?.UnlockSeasonalTheme("spring");
                    break;

                case IAPManager.Products.SummerTheme:
                    _catalog?.UnlockSeasonalTheme("summer");
                    break;

                case IAPManager.Products.AutumnTheme:
                    _catalog?.UnlockSeasonalTheme("autumn");
                    break;

                case IAPManager.Products.DruidLorePack:
                    _catalog?.UnlockLorePack("druid");
                    break;

                case IAPManager.Products.AncientLorePack:
                    _catalog?.UnlockLorePack("ancient");
                    break;

                case IAPManager.Products.PremiumDecorPack:
                    _decor?.UnlockPremiumCategory();
                    _catalog?.UnlockPack("premium_decor");
                    break;

                case IAPManager.Products.CreatureAlbum:
                    _catalog?.UnlockFeature("creature_album");
                    break;

                case IAPManager.Products.AllAccess:
                    // All-access bundle: apply every individual unlock.
                    foreach (var id in IAPManager.Products.All)
                        if (id != IAPManager.Products.AllAccess)
                            ApplyUnlock(id);
                    break;

                default:
                    Debug.LogWarning($"[PremiumUnlockController] Unknown product: {productId}");
                    return;
            }

            // Persist that this product is applied.
            if (_save?.ActiveData != null)
            {
                _save.ActiveData.premiumUnlocked = true;
                _save.Save(_save.ActiveData);
            }

            OnUnlockApplied?.Invoke(productId);
            Debug.Log($"[PremiumUnlockController] Applied: {productId}");
        }

        public bool IsUnlockApplied(string productId) => _applied.Contains(productId);
    }
}
