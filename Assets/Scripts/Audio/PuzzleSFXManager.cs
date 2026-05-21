using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Puzzle-specific sound effects manager. Replaces ForestAudioController's
    /// raw frequency calls with semantically-named methods backed by AudioAssetLibrary.
    ///
    /// All methods are null-safe and respect SFX enabled setting from accessibility.
    /// </summary>
    public class PuzzleSFXManager : MonoBehaviour
    {
        private AudioSource       _sfxSource;
        private AudioAssetLibrary _library;
        private bool              _sfxEnabled = true;

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(AudioAssetLibrary library, float masterVolume = 0.85f)
        {
            _library = library;

            var go           = new GameObject("PuzzleSFX");
            go.transform.SetParent(transform, false);
            _sfxSource       = go.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop       = false;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.volume     = masterVolume;
        }

        public void SetEnabled(bool enabled) => _sfxEnabled = enabled;
        public void SetVolume(float volume)  => _sfxSource.volume = Mathf.Clamp01(volume);

        // ─── Puzzle Events ────────────────────────────────────────────────────────

        public void OnChoiceCorrect()     => Play("puzzle_correct");
        public void OnChoiceWrong()       => Play("puzzle_wrong");
        public void OnPuzzleComplete()    => Play("puzzle_complete");
        public void OnStarEarned()        => Play("star_earn");
        public void OnHintUsed()          => Play("tap_select");

        // ─── Navigation ───────────────────────────────────────────────────────────

        public void OnTapSelect()         => Play("tap_select");
        public void OnTapBack()           => Play("tap_back");

        // ─── World Events ─────────────────────────────────────────────────────────

        public void OnRegionUnlocked()    => Play("region_unlock");
        public void OnBondLevelUp()       => Play("bond_up");
        public void OnTreatFed()          => Play("treat_feed");
        public void OnRitualStart()       => Play("ritual_start");
        public void OnRitualComplete()    => Play("ritual_complete");
        public void OnFogDispelled()      => Play("fog_dispel");

        // ─── Private ─────────────────────────────────────────────────────────────

        private void Play(string sfxId)
        {
            if (!_sfxEnabled || _library == null || _sfxSource == null) return;
            var clip = _library.GetSFX(sfxId);
            if (clip != null)
                _sfxSource.PlayOneShot(clip);
        }
    }
}
