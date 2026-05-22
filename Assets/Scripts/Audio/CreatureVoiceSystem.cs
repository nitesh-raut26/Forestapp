using System.Collections;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Plays procedural creature voice clips in response to emotion events.
    ///
    /// Each creature has a distinct pitch/timbre from AudioAssetLibrary.
    /// Voices are played through a shared AudioSource with a cooldown per
    /// creature to prevent voice spam during rapid emotion transitions.
    /// </summary>
    public class CreatureVoiceSystem : MonoBehaviour
    {
        private AudioSource       _voiceSource;
        private AudioAssetLibrary _library;
        private CreatureMoodBrain _moodBrain;

        private const float VoiceCooldown = 1.8f;
        private readonly float[] _lastVoiceTime = new float[6];

        private static readonly string[] CreatureIds =
            { "pip", "mimi", "tomo", "luma", "nori", "sol" };

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(AudioAssetLibrary library, CreatureMoodBrain moodBrain,
            float masterVolume = 0.7f)
        {
            _library   = library;
            _moodBrain = moodBrain;

            var go           = new GameObject("CreatureVoice");
            go.transform.SetParent(transform, false);
            _voiceSource     = go.AddComponent<AudioSource>();
            _voiceSource.playOnAwake = false;
            _voiceSource.loop       = false;
            _voiceSource.volume     = masterVolume;
            _voiceSource.spatialBlend = 0f;

            // Subscribe to emotion changes
            for (var i = 0; i < CreatureIds.Length; i++)
            {
                var id = CreatureIds[i];
                var index = i;
                var fsm = moodBrain?.GetFSM(id);
                if (fsm != null)
                    fsm.OnEmotionChanged += (prev, next) => OnEmotionChanged(id, index, next);
            }
        }

        // ─── Triggered Voices ─────────────────────────────────────────────────────

        /// <summary>Directly trigger a voice cue (used by UI interactions).</summary>
        public void PlayVoice(string creatureId, CreatureEmotion emotion, float volumeScale = 1f)
        {
            var clip = _library?.GetCreatureVoice(creatureId, emotion);
            if (clip == null) return;
            _voiceSource.PlayOneShot(clip, volumeScale);
        }

        public void PlayInteractionVoice(string creatureId, string interactionType)
        {
            // Try rich character cue line first (greeting, hint, cheer)
            var cueType = interactionType switch
            {
                "greet" or "greeting" => "greeting",
                "hint"                => "hint",
                "cheer" or "win"      => "cheer",
                _                     => null
            };

            if (cueType != null)
            {
                var cueClip = _library?.GetCharacterCueLine(creatureId, cueType);
                if (cueClip != null) { _voiceSource.PlayOneShot(cueClip); return; }
            }

            // Fall back to emotion-mapped voice chirp
            var emotion = interactionType switch
            {
                "pet"  => CreatureEmotion.Happy,
                "feed" => CreatureEmotion.Excited,
                "hint" => CreatureEmotion.Curious,
                _      => CreatureEmotion.Idle
            };
            PlayVoice(creatureId, emotion);
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private void OnEmotionChanged(string creatureId, int index, CreatureEmotion next)
        {
            if (Time.time - _lastVoiceTime[index] < VoiceCooldown) return;

            // Only voice-react to significant emotions
            if (next == CreatureEmotion.Idle || next == CreatureEmotion.Sleepy) return;

            _lastVoiceTime[index] = Time.time;
            PlayVoice(creatureId, next, 0.6f);
        }
    }
}
