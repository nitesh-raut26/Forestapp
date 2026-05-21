using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Master coordinator for all environmental VFX.
    /// Routes zone/season/event changes to the correct VFX subsystem.
    ///
    /// Called by ForestUIRouter when zone changes, and by LivingWorldController
    /// on season change.
    ///
    /// Manages:
    ///   - FireflyTrailSystem (Firefly Hollow, Sanctuary)
    ///   - WaterRippleSystem  (River Bend, Moonlit Creek)
    ///   - EmotionalParticleEngine (creature interactions)
    ///   - AmbientVFXController (global ambient)
    /// </summary>
    public class EnvironmentalFXDirector : MonoBehaviour
    {
        private FireflyTrailSystem    _fireflies;
        private WaterRippleSystem     _water;
        private EmotionalParticleEngine _particles;
        private AmbientVFXController  _ambient;
        private PerformanceManager    _perf;

        private string _currentZone;

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(FireflyTrailSystem fireflies, WaterRippleSystem water,
            EmotionalParticleEngine particles, AmbientVFXController ambient,
            PerformanceManager perf)
        {
            _fireflies = fireflies;
            _water     = water;
            _particles = particles;
            _ambient   = ambient;
            _perf      = perf;
        }

        // ─── Zone Changes ─────────────────────────────────────────────────────────

        public void OnZoneChanged(string zoneId)
        {
            if (zoneId == _currentZone) return;
            _currentZone = zoneId;

            // Fireflies: hollow, sanctuary, firefly marsh
            var fireflyZones = new[] { "firefly-hollow", "sanctuary", "firefly-marsh" };
            var inFireflyZone = System.Array.IndexOf(fireflyZones, zoneId) >= 0;
            _fireflies?.SetActive(inFireflyZone && _perf.AmbientVFXEnabled);

            // Water ripples: creek and river zones
            var waterZones = new[] { "river-bend", "moonlit-creek", "firefly-marsh" };
            var inWaterZone = System.Array.IndexOf(waterZones, zoneId) >= 0;
            _water?.SetActive(inWaterZone && _perf.AmbientVFXEnabled);

            Debug.Log($"[EnvFX] Zone changed: {zoneId} | Fireflies: {inFireflyZone} | Water: {inWaterZone}");
        }

        // ─── Season Changes ───────────────────────────────────────────────────────

        public void OnSeasonChanged(Season season)
        {
            // Adjust ambient VFX intensity per season
            // (Actual weather/tint controlled by DynamicSeasonManager)
            switch (season)
            {
                case Season.Winter:
                    _fireflies?.SetActive(false); // fireflies don't appear in winter
                    break;
                case Season.Spring:
                case Season.Summer:
                    if (_currentZone != null) OnZoneChanged(_currentZone); // re-apply zone state
                    break;
            }
        }

        // ─── World Events ─────────────────────────────────────────────────────────

        public void OnWorldEventStarted(WorldEvent worldEvent)
        {
            if (worldEvent == null || !_perf.AmbientVFXEnabled) return;

            switch (worldEvent.type)
            {
                case WorldEventType.MeteorShower:
                case WorldEventType.SpringBloom:
                    // Burst of particles — handled by EmotionalParticleEngine caller
                    break;
                case WorldEventType.FireflyFestival:
                    _fireflies?.SetActive(true);
                    break;
            }
        }
    }
}
