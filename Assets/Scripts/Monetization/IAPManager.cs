using System;
using System.Collections.Generic;
using UnityEngine;

// Compile against Unity IAP when the package is present; otherwise use a mock that
// mirrors the same surface so the game code never needs to change.
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace ForestFriendsQuest
{
    /// <summary>
    /// IAPManager — ethical, child-safe in-app purchase layer.
    ///
    /// Principles:
    ///   • Cosmetics-only: sanctuary decor, seasonal themes, premium lore packs.
    ///   • Zero pay-to-win, zero gambling, zero manipulative timers.
    ///   • All purchases require a parental PIN gate (age-gate enforced by ParentPurchaseGate).
    ///   • Offline entitlement caching so the game works without a network.
    ///   • Restore purchases is surfaced prominently and works on every platform.
    ///
    /// COPPA compliance:
    ///   • No user tracking tied to IAP.
    ///   • No behavioural advertising data collected at checkout.
    ///   • All analytics are anonymised session counts, not personal identifiers.
    /// </summary>
#if UNITY_PURCHASING
    public class IAPManager : MonoBehaviour, IDetailedStoreListener
#else
    public class IAPManager : MonoBehaviour
#endif
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<string>  OnPurchaseSuccess;   // productId
        public event Action<string>  OnPurchaseFailed;    // reason
        public event Action          OnRestoreComplete;
        public event Action          OnStoreReady;

        // ─── Product Catalogue ───────────────────────────────────────────────────

        // Non-consumable product IDs — must match App Store / Google Play console.
        public static class Products
        {
            public const string StarterPack       = "ffq.cosmetic.starter_pack";
            public const string WinterTheme       = "ffq.cosmetic.winter_theme";
            public const string SpringTheme       = "ffq.cosmetic.spring_theme";
            public const string SummerTheme       = "ffq.cosmetic.summer_theme";
            public const string AutumnTheme       = "ffq.cosmetic.autumn_theme";
            public const string DruidLorePack     = "ffq.lore.druid_pack";
            public const string AncientLorePack   = "ffq.lore.ancient_pack";
            public const string PremiumDecorPack  = "ffq.decor.premium_pack";
            public const string CreatureAlbum     = "ffq.social.creature_album";
            public const string AllAccess         = "ffq.bundle.all_access";      // bundle

            public static readonly string[] All = {
                StarterPack, WinterTheme, SpringTheme, SummerTheme, AutumnTheme,
                DruidLorePack, AncientLorePack, PremiumDecorPack, CreatureAlbum, AllAccess
            };
        }

        // ─── State ───────────────────────────────────────────────────────────────

        private bool _isReady;
        private readonly HashSet<string> _owned = new();
        private PremiumUnlockController  _unlocks;
        private ParentPurchaseGate       _parentGate;

        private const string OwnershipPrefsKey = "FFQ.IAP.Owned";

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(PremiumUnlockController unlocks, ParentPurchaseGate parentGate)
        {
            _unlocks    = unlocks;
            _parentGate = parentGate;

            LoadCachedOwnership();

#if UNITY_PURCHASING
            InitializeUnityIAP();
#else
            // Editor / test mode: treat all previously cached products as owned.
            _isReady = true;
            OnStoreReady?.Invoke();
            Debug.Log("[IAPManager] Running in mock mode (Unity IAP package not present).");
#endif
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        /// <summary>Returns true if the product is owned (including offline cache).</summary>
        public bool IsOwned(string productId) => _owned.Contains(productId);

        /// <summary>Begin a purchase flow. Always routes through the parental gate first.</summary>
        public void Purchase(string productId)
        {
            if (!_isReady)
            {
                Debug.LogWarning("[IAPManager] Store not ready.");
                OnPurchaseFailed?.Invoke("Store not initialised.");
                return;
            }

            if (IsOwned(productId))
            {
                // Already owned — silently apply and notify success.
                _unlocks?.ApplyUnlock(productId);
                OnPurchaseSuccess?.Invoke(productId);
                return;
            }

            _parentGate?.RequestAuthorization(
                onApproved: () => ExecutePurchase(productId),
                onDenied:   () => OnPurchaseFailed?.Invoke("Parental gate declined.")
            );
        }

        /// <summary>Restore purchases (required by App Store review guidelines).</summary>
        public void RestorePurchases()
        {
#if UNITY_PURCHASING && (UNITY_IOS || UNITY_STANDALONE_OSX)
            if (_controller != null && _extensions != null)
            {
                _extensions.GetExtension<IAppleExtensions>()
                    .RestoreTransactions((result, err) =>
                    {
                        Debug.Log($"[IAPManager] Restore: {result} {err}");
                        OnRestoreComplete?.Invoke();
                    });
                return;
            }
#endif
            // Fallback: re-validate cached ownership and reapply.
            foreach (var id in _owned)
                _unlocks?.ApplyUnlock(id);
            OnRestoreComplete?.Invoke();
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private void ExecutePurchase(string productId)
        {
#if UNITY_PURCHASING
            _controller?.InitiatePurchase(productId);
#else
            // Mock: immediately grant.
            GrantOwnership(productId);
#endif
        }

        private void GrantOwnership(string productId)
        {
            _owned.Add(productId);
            PersistOwnership();
            _unlocks?.ApplyUnlock(productId);
            OnPurchaseSuccess?.Invoke(productId);
            Debug.Log($"[IAPManager] Granted: {productId}");
        }

        private void LoadCachedOwnership()
        {
            var raw = PlayerPrefs.GetString(OwnershipPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var id in raw.Split(','))
                if (!string.IsNullOrEmpty(id)) _owned.Add(id);
        }

        private void PersistOwnership()
        {
            PlayerPrefs.SetString(OwnershipPrefsKey, string.Join(",", _owned));
            PlayerPrefs.Save();
        }

        // ─── Unity IAP Callbacks ─────────────────────────────────────────────────

#if UNITY_PURCHASING
        private IStoreController   _controller;
        private IExtensionProvider _extensions;

        private void InitializeUnityIAP()
        {
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var id in Products.All)
                builder.AddProduct(id, ProductType.NonConsumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            _isReady    = true;

            // Sync store receipt with local cache.
            foreach (var p in controller.products.all)
            {
                if (p.hasReceipt) _owned.Add(p.definition.id);
            }
            PersistOwnership();
            foreach (var id in _owned) _unlocks?.ApplyUnlock(id);

            OnStoreReady?.Invoke();
            Debug.Log("[IAPManager] Store initialized successfully.");
        }

        public void OnInitializeFailed(InitializationFailureReason reason)
        {
            Debug.LogWarning($"[IAPManager] Init failed: {reason}");
            _isReady = true; // still allow offline cached entitlements
            OnStoreReady?.Invoke();
        }

        public void OnInitializeFailed(InitializationFailureReason reason, string message)
            => OnInitializeFailed(reason);

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            GrantOwnership(args.purchasedProduct.definition.id);
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            var reason = failureReason.ToString();
            Debug.LogWarning($"[IAPManager] Purchase failed: {product.definition.id} — {reason}");
            OnPurchaseFailed?.Invoke(reason);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
            => OnPurchaseFailed(product, failureDescription.reason);
#endif
    }
}
