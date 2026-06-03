using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForestFriendsQuest
{
    public static class PostProcessingSetup
    {
        public static Volume CreateGlobalVolume()
        {
            var go = new GameObject("PostProcessingVolume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1.0f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            // 1. Add Bloom for glowing sparks, fireflies, and ambient light
            if (profile.Add<Bloom>(out var bloom))
            {
                bloom.active = true;
                bloom.intensity.Override(1.2f);
                bloom.threshold.Override(0.85f);
                bloom.scatter.Override(0.6f);
                bloom.tint.Override(new Color(1f, 0.95f, 0.8f)); // Warm golden bloom
            }

            // 2. Add Color Adjustments for a premium, rich cinematic look
            if (profile.Add<ColorAdjustments>(out var colorAdjustments))
            {
                colorAdjustments.active = true;
                colorAdjustments.postExposure.Override(0.15f);
                colorAdjustments.contrast.Override(15f);
                colorAdjustments.saturation.Override(20f);
            }

            // 3. Add Vignette to draw focus to the center of the screen
            if (profile.Add<Vignette>(out var vignette))
            {
                vignette.active = true;
                vignette.intensity.Override(0.28f);
                vignette.smoothness.Override(0.8f);
                vignette.roundness.Override(1f);
                vignette.color.Override(new Color(0.02f, 0.1f, 0.05f)); // forest green vignette
            }

            return volume;
        }
    }
}
