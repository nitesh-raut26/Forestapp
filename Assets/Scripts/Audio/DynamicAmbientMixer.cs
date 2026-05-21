using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Manages layered ambient audio for each biome/region.
    ///
    /// Ambient layers (all procedurally synthesized):
    ///   Base   — low-frequency drone (wind, earth hum)
    ///   Mid    — nature bed (crickets/birds approximation via filtered noise)
    ///   Detail — occasional accents (rustling, water drops, firefly chimes)
    ///
    /// Layers are blended independently. Biome change crossfades all layers
    /// simultaneously. Time-of-day modulates mid and detail volumes.
    /// </summary>
    public class DynamicAmbientMixer : MonoBehaviour
    {
        private readonly Dictionary<string, AudioSource> _layers =
            new Dictionary<string, AudioSource>();

        private AudioAssetLibrary _library;
        private string            _currentBiome;
        private float             _masterVolume = 0.5f;

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(AudioAssetLibrary library)
        {
            _library = library;

            foreach (var layer in new[] { "base", "mid", "detail" })
            {
                var go  = new GameObject($"Ambient_{layer}");
                go.transform.SetParent(transform, false);
                var src          = go.AddComponent<AudioSource>();
                src.playOnAwake  = false;
                src.loop         = true;
                src.spatialBlend = 0f;
                src.volume       = 0f;
                _layers[layer]   = src;
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public void SetBiome(string biomeId, float transitionDuration = 1.5f)
        {
            if (_currentBiome == biomeId) return;
            _currentBiome = biomeId;
            StartCoroutine(TransitionBiome(biomeId, transitionDuration));
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            foreach (var src in _layers.Values)
                src.volume = src.volume > 0 ? _masterVolume * GetLayerTargetVolume(src) : 0f;
        }

        public void SetTimeOfDay(float normalizedTime)
        {
            // Boost mid (creatures) at dawn/dusk, quiet at night
            var isDawn = normalizedTime > 0.2f && normalizedTime < 0.35f;
            var isDusk = normalizedTime > 0.65f && normalizedTime < 0.8f;
            var isNight = normalizedTime > 0.85f || normalizedTime < 0.1f;

            var midTarget    = isNight ? 0.1f : (isDawn || isDusk) ? 0.5f : 0.3f;
            var detailTarget = isNight ? 0.05f : 0.2f;

            if (_layers.TryGetValue("mid", out var mid))
                StartCoroutine(FadeLayer(mid, midTarget * _masterVolume, 3f));
            if (_layers.TryGetValue("detail", out var detail))
                StartCoroutine(FadeLayer(detail, detailTarget * _masterVolume, 3f));
        }

        // ─── Coroutines ───────────────────────────────────────────────────────────

        private IEnumerator TransitionBiome(string biomeId, float duration)
        {
            // Fade out all current layers
            var fadeOuts = new List<Coroutine>();
            foreach (var src in _layers.Values)
                fadeOuts.Add(StartCoroutine(FadeLayer(src, 0f, duration * 0.5f)));

            yield return new WaitForSeconds(duration * 0.5f);

            // Swap clips
            var baseClip = _library?.GetTheme(biomeId);
            if (baseClip != null && _layers.TryGetValue("base", out var baseSrc))
            {
                baseSrc.clip = baseClip;
                baseSrc.Play();
            }

            // Mid and detail use filtered noise approximation
            SetLayerNoise("mid",    GetMidFrequency(biomeId), 0.02f);
            SetLayerNoise("detail", GetDetailFrequency(biomeId), 0.008f);

            // Fade in
            if (_layers.TryGetValue("base", out var b))
                StartCoroutine(FadeLayer(b, 0.18f * _masterVolume, duration * 0.5f));
            if (_layers.TryGetValue("mid", out var m))
                StartCoroutine(FadeLayer(m, 0.28f * _masterVolume, duration * 0.5f));
            if (_layers.TryGetValue("detail", out var d))
                StartCoroutine(FadeLayer(d, 0.12f * _masterVolume, duration * 0.5f));
        }

        private static IEnumerator FadeLayer(AudioSource src, float target, float duration)
        {
            var start   = src.volume;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                src.volume = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            src.volume = target;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private void SetLayerNoise(string layer, float frequency, float volume)
        {
            if (!_layers.TryGetValue(layer, out var src)) return;

            const int samples    = 44100 * 4; // 4-second looping noise
            const int sampleRate = 44100;
            var data = new float[samples];

            for (var i = 0; i < samples; i++)
            {
                var t   = i / (float)sampleRate;
                // Sine wave at frequency + brown noise for texture
                data[i] = (Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.3f +
                           (Random.value * 2f - 1f) * 0.05f) * volume;
            }

            var clip = AudioClip.Create($"noise_{layer}", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            src.clip = clip;
            if (!src.isPlaying) src.Play();
        }

        private static float GetMidFrequency(string biomeId) => biomeId switch
        {
            "fern-trail"     => 2800f,
            "firefly-hollow" => 2200f,
            "moonlit-creek"  => 1800f,
            "skyroot-canopy" => 3500f,
            _                => 2400f
        };

        private static float GetDetailFrequency(string biomeId) => biomeId switch
        {
            "moonlit-creek"  => 800f,
            "river-bend"     => 600f,
            _                => 3200f
        };

        private static float GetLayerTargetVolume(AudioSource src) => src.volume;
    }
}
