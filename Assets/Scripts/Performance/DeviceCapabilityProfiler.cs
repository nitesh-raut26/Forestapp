using System;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Device capability profiler — assesses hardware tier at startup
    /// and provides recommendations to all rendering and audio systems.
    ///
    /// Tier classification:
    ///   Tier 3 — High-end (60fps target): All VFX, ambient particles, full audio
    ///   Tier 2 — Mid-range (45fps target): Reduced particles, limited VFX
    ///   Tier 1 — Low-end (30fps target): Minimal VFX, basic audio, no ambient
    ///
    /// Targets:
    ///   - iPad Pro / Samsung Galaxy S: Tier 3
    ///   - iPad Air / mid-range Android: Tier 2
    ///   - Budget Android tablets: Tier 1
    /// </summary>
    public class DeviceCapabilityProfiler : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<DeviceTier> OnTierDetected;

        // ─── State ───────────────────────────────────────────────────────────────

        public DeviceTier CurrentTier     { get; private set; } = DeviceTier.Mid;
        public int        TargetFPS       { get; private set; } = 60;
        public int        MaxParticles    { get; private set; } = 150;
        public bool       AmbientVFXOk    { get; private set; } = true;
        public bool       FullAudioOk     { get; private set; } = true;
        public float      RenderScale     { get; private set; } = 1f;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            CurrentTier = ClassifyDevice();
            ApplyTierSettings(CurrentTier);
            OnTierDetected?.Invoke(CurrentTier);

            Debug.Log($"[DeviceCapabilityProfiler] Tier: {CurrentTier} | Target FPS: {TargetFPS} | RAM: {SystemInfo.systemMemorySize}MB");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns the recommended max particle count for this device.</summary>
        public int GetMaxParticles() => MaxParticles;

        /// <summary>Returns whether ambient VFX should be enabled.</summary>
        public bool IsAmbientVFXEnabled() => AmbientVFXOk;

        /// <summary>Returns whether full audio mixing should be enabled.</summary>
        public bool IsFullAudioEnabled() => FullAudioOk;

        /// <summary>Force a specific tier (for QA/testing).</summary>
        public void ForceTestTier(DeviceTier tier)
        {
            CurrentTier = tier;
            ApplyTierSettings(tier);
            Debug.Log($"[DeviceCapabilityProfiler] FORCED tier: {tier}");
        }

        // ─── Private Logic ────────────────────────────────────────────────────────

        private DeviceTier ClassifyDevice()
        {
            int ram     = SystemInfo.systemMemorySize;
            int gpuMem  = SystemInfo.graphicsMemorySize;
            int cpuCount= SystemInfo.processorCount;

            // High-end indicators
            if (ram >= 4096 && gpuMem >= 2048 && cpuCount >= 6) return DeviceTier.High;

            // Low-end indicators
            if (ram < 2048 || gpuMem < 512 || cpuCount < 4) return DeviceTier.Low;

            // Mid-range
            return DeviceTier.Mid;
        }

        private void ApplyTierSettings(DeviceTier tier)
        {
            switch (tier)
            {
                case DeviceTier.High:
                    TargetFPS    = 60;
                    MaxParticles = 300;
                    AmbientVFXOk = true;
                    FullAudioOk  = true;
                    RenderScale  = 1f;
                    break;

                case DeviceTier.Mid:
                    TargetFPS    = 45;
                    MaxParticles = 150;
                    AmbientVFXOk = true;
                    FullAudioOk  = true;
                    RenderScale  = 0.9f;
                    break;

                case DeviceTier.Low:
                    TargetFPS    = 30;
                    MaxParticles = 60;
                    AmbientVFXOk = false;
                    FullAudioOk  = false;
                    RenderScale  = 0.75f;
                    break;
            }

            Application.targetFrameRate = TargetFPS;
        }
    }

    public enum DeviceTier { Low, Mid, High }
}
