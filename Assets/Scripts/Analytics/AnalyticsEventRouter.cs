using System;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// AnalyticsEventRouter — wires game-system events to FirebaseAnalyticsConnector.
    ///
    /// This is the single point where all game events are translated into analytics
    /// calls. It subscribes to events from every major system and routes them to
    /// the connector. Centralising this here means:
    ///   • Game systems have zero awareness of analytics.
    ///   • Adding a new event tracker requires changes in only this file.
    ///   • Event subscriptions are all cleaned up from one OnDestroy.
    /// </summary>
    public class AnalyticsEventRouter : MonoBehaviour
    {
        private FirebaseAnalyticsConnector _firebase;
        private ForestSystemsContainer     _systems;
        private float _sessionStartTime;

        public void Initialize(FirebaseAnalyticsConnector firebase, ForestSystemsContainer systems)
        {
            _firebase          = firebase;
            _systems           = systems;
            _sessionStartTime  = Time.realtimeSinceStartup;

            WireEvents();

            var save = systems.SaveSystem?.ActiveData;
            _firebase.LogSessionStart(
                save?.explorerTier ?? "scout",
                save?.totalInGameDays == 0
            );
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        // ─── Wiring ──────────────────────────────────────────────────────────────

        private void WireEvents()
        {
            if (_systems == null) return;

            // Creature bonding
            if (_systems.BondingEngine != null)
                _systems.BondingEngine.OnBondLevelUp += OnBondLevelUp;

            // World progression
            if (_systems.World != null)
                _systems.World.OnRegionUnlocked += r => _firebase.LogRegionUnlocked(r.regionId);

            // Boss encounters
            if (_systems.Bosses != null)
                _systems.Bosses.OnBossDefeated += b => _firebase.LogBossDefeated(b.name);

            // Lore — OnLoreCollected fires Action<LoreEntry>; pass entry.id to Firebase
            if (_systems.Exploration != null)
                _systems.Exploration.OnLoreCollected += entry => _firebase.LogLoreDiscovered(entry.id);

            // Evolution (proxy for deep engagement)
            if (_systems.Evolution != null)
                _systems.Evolution.OnStageEvolved += (id, stage) =>
                    _firebase.LogEvent("creature_evolved", new()
                    {
                        { "creature_id", id },
                        { "stage_name",  stage.stageName }
                    });

            // Sanctuary — event is OnItemPlaced(SanctuaryItem), not OnDecorationPlaced
            if (_systems.SanctuaryDecor != null)
                _systems.SanctuaryDecor.OnItemPlaced += item =>
                    _firebase.LogSanctuaryCustomized("place", item.id);

            // Accessibility features
            if (_systems.Accessibility != null)
            {
                _systems.Accessibility.OnCalmModeChanged       += v => { if (v) _firebase.LogAccessibilityEnabled("calm_mode"); };
                _systems.Accessibility.OnColorblindModeChanged += m => { if (m != ColorblindFilter.None) _firebase.LogAccessibilityEnabled($"colorblind_{m}"); };
            }

            // Premium conversions
            // (Wired externally from IAPManager.OnPurchaseSuccess via ForestQuestApp)
        }

        private void UnwireEvents()
        {
            if (_systems?.BondingEngine != null)
                _systems.BondingEngine.OnBondLevelUp -= OnBondLevelUp;
        }

        // ─── Handlers ────────────────────────────────────────────────────────────

        private void OnBondLevelUp(string creatureId, int newLevel)
            => _firebase.LogCreatureBond(creatureId, newLevel);

        // ─── Session Lifecycle ───────────────────────────────────────────────────

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushSession();
        }

        private void OnApplicationQuit() => FlushSession();

        private void FlushSession()
        {
            var duration = (int)(Time.realtimeSinceStartup - _sessionStartTime);
            _firebase.LogSessionEnd(duration, 0 /* puzzle count tracked elsewhere */);
        }
    }
}
