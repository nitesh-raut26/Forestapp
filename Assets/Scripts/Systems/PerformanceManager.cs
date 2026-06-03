using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Detects device capability tier at startup and provides quality settings
    /// that other systems use to scale their resource usage.
    ///
    /// Tier detection uses:
    ///   - SystemInfo.systemMemorySize
    ///   - SystemInfo.processorCount
    ///   - Application.targetFrameRate
    ///
    /// Settings by tier:
    ///   Low  — 30fps cap, no ambient VFX, particle count halved, no glow
    ///   Mid  — 60fps, ambient VFX limited, particles at 75%
    ///   High — 60fps, all effects enabled, full particle count
    /// </summary>
    public class PerformanceManager : MonoBehaviour
    {
        public DeviceTier CurrentTier { get; private set; }

        public bool AmbientVFXEnabled      { get; private set; }
        public bool GlowEnabled            { get; private set; }
        public float ParticleCountScale    { get; private set; }
        public int   TargetFrameRate       { get; private set; }
        public bool  DynamicBatchingHint   { get; private set; }

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize()
        {
            CurrentTier = DetectTier();
            ApplyTierSettings(CurrentTier);
            Debug.Log($"[PerformanceManager] Device tier: {CurrentTier} | " +
                      $"RAM: {SystemInfo.systemMemorySize}MB | " +
                      $"Cores: {SystemInfo.processorCount}");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Scale a particle count based on device tier.</summary>
        public int ScaleParticles(int baseCount)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseCount * ParticleCountScale));
        }

        /// <summary>Clamp a duration — on low-end devices, skip or shorten animations.</summary>
        public float ScaleDuration(float baseDuration)
        {
            return CurrentTier == DeviceTier.Low ? baseDuration * 0.5f : baseDuration;
        }

        /// <summary>Returns the current detected device tier.</summary>
        public DeviceTier GetCurrentTier() => CurrentTier;

        // ─── Private ─────────────────────────────────────────────────────────────

        private static DeviceTier DetectTier()
        {
            var ram   = SystemInfo.systemMemorySize;
            var cores = SystemInfo.processorCount;

#if UNITY_ANDROID || UNITY_IOS
            if (ram >= 4096 && cores >= 6) return DeviceTier.High;
            if (ram >= 2048 && cores >= 4) return DeviceTier.Mid;
            return DeviceTier.Low;
#else
            // Editor / Desktop — always high
            return DeviceTier.High;
#endif
        }

        private void ApplyTierSettings(DeviceTier tier)
        {
            switch (tier)
            {
                case DeviceTier.Low:
                    AmbientVFXEnabled    = false;
                    GlowEnabled          = false;
                    ParticleCountScale   = 0.35f;
                    TargetFrameRate      = 30;
                    DynamicBatchingHint  = true;
                    break;

                case DeviceTier.Mid:
                    AmbientVFXEnabled    = true;
                    GlowEnabled          = false;
                    ParticleCountScale   = 0.65f;
                    TargetFrameRate      = 60;
                    DynamicBatchingHint  = true;
                    break;

                case DeviceTier.High:
                default:
                    AmbientVFXEnabled    = true;
                    GlowEnabled          = true;
                    ParticleCountScale   = 1.0f;
                    TargetFrameRate      = 60;
                    DynamicBatchingHint  = false;
                    break;
            }

            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
