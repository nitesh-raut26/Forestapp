using System;
using System.Collections;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Handles smooth crossfades between two AudioSources so music transitions
    /// are seamless and never cut abruptly.
    ///
    /// Transitions:
    ///   CrossFade    — equal-power crossfade over a configurable duration
    ///   HardCut      — immediate swap on next downbeat (approximated)
    ///   StingAndFade — play a sting clip over the fade (boss entrance, region unlock)
    ///
    /// Used exclusively by ForestMusicDirector — not a public API.
    /// </summary>
    public class AdaptiveMusicTransition : MonoBehaviour
    {
        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private AudioSource _stingSource;
        private bool        _aIsActive = true;

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize()
        {
            _sourceA    = CreateSource("MusicA");
            _sourceB    = CreateSource("MusicB");
            _stingSource = CreateSource("MusicSting");
        }

        public AudioSource ActiveSource => _aIsActive ? _sourceA : _sourceB;

        // ─── Transitions ──────────────────────────────────────────────────────────

        public void CrossFade(AudioClip incoming, float duration, float targetVolume,
            Action onComplete = null)
        {
            StartCoroutine(DoCrossFade(incoming, duration, targetVolume, onComplete));
        }

        public void HardCut(AudioClip incoming, float targetVolume)
        {
            var outgoing = _aIsActive ? _sourceA : _sourceB;
            var next     = _aIsActive ? _sourceB : _sourceA;

            outgoing.Stop();
            next.clip   = incoming;
            next.volume = targetVolume;
            next.loop   = true;
            next.Play();

            _aIsActive = !_aIsActive;
        }

        public void PlaySting(AudioClip sting, float volume = 0.9f)
        {
            if (sting == null) return;
            _stingSource.volume = volume;
            _stingSource.PlayOneShot(sting);
        }

        public void SetVolume(float volume)
        {
            ActiveSource.volume = volume;
        }

        public void FadeOut(float duration, Action onComplete = null)
        {
            StartCoroutine(DoFadeOut(duration, onComplete));
        }

        // ─── Coroutines ───────────────────────────────────────────────────────────

        private IEnumerator DoCrossFade(AudioClip incoming, float duration, float targetVolume,
            Action onComplete)
        {
            var outgoing    = _aIsActive ? _sourceA : _sourceB;
            var next        = _aIsActive ? _sourceB : _sourceA;
            var startVolOut = outgoing.volume;

            next.clip   = incoming;
            next.volume = 0f;
            next.loop   = true;
            next.Play();

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                // Equal-power crossfade
                outgoing.volume = startVolOut * Mathf.Cos(t * Mathf.PI * 0.5f);
                next.volume     = targetVolume * Mathf.Sin(t * Mathf.PI * 0.5f);
                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;
            next.volume     = targetVolume;
            _aIsActive      = !_aIsActive;

            onComplete?.Invoke();
        }

        private IEnumerator DoFadeOut(float duration, Action onComplete)
        {
            var active    = ActiveSource;
            var startVol  = active.volume;
            var elapsed   = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                active.volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            active.Stop();
            onComplete?.Invoke();
        }

        // ─── Factory ─────────────────────────────────────────────────────────────

        private AudioSource CreateSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var src         = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop        = true;
            src.spatialBlend = 0f;
            src.volume      = 0f;
            return src;
        }
    }
}
