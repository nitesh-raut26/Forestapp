using UnityEngine;

namespace ForestFriendsQuest
{
    public class ForestAudioController : MonoBehaviour
    {
        private AudioSource _source;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
        }

        public void PlaySelect(bool enabled)
        {
            if (!enabled)
            {
                return;
            }

            PlaySequence(new[] { 440f, 550f }, 0.08f, 0.07f);
        }

        public void PlayWrong(CharacterProfile profile, bool enabled)
        {
            if (!enabled)
            {
                return;
            }

            var root = GetCharacterBaseFrequency(profile);
            PlaySequence(new[] { root, root * 0.9f }, 0.11f, 0.08f);
        }

        public void PlaySuccess(CharacterProfile profile, bool enabled)
        {
            if (!enabled)
            {
                return;
            }

            var root = GetCharacterBaseFrequency(profile);
            PlaySequence(new[] { root, root * 1.16f, root * 1.33f, root * 1.48f }, 0.09f, 0.1f);
        }

        public void PlayCharacterCue(CharacterProfile profile, string cueType, bool enabled)
        {
            if (!enabled)
            {
                return;
            }

            var root = GetCharacterBaseFrequency(profile);

            switch (cueType)
            {
                case "greeting":
                    PlaySequence(new[] { root, root * 1.12f, root * 1.25f }, 0.1f, 0.08f);
                    break;
                case "hint":
                    PlaySequence(new[] { root * 1.05f, root, root * 0.96f }, 0.1f, 0.07f);
                    break;
                case "cheer":
                    PlaySuccess(profile, true);
                    break;
                default:
                    PlaySelect(true);
                    break;
            }
        }

        private float GetCharacterBaseFrequency(CharacterProfile profile)
        {
            if (profile == null || profile.voice == null)
            {
                return 440f;
            }

            return Mathf.Clamp(280f + (profile.voice.pitch * 160f), 180f, 880f);
        }

        private void PlaySequence(float[] notes, float noteDuration, float volume)
        {
            if (notes == null || notes.Length == 0)
            {
                return;
            }

            var clip = GenerateClip(notes, noteDuration, volume);
            _source.Stop();
            _source.clip = clip;
            _source.Play();
        }

        private AudioClip GenerateClip(float[] notes, float noteDuration, float volume)
        {
            const int sampleRate = 44100;
            var noteSamples = Mathf.RoundToInt(sampleRate * noteDuration);
            var totalSamples = noteSamples * notes.Length;
            var data = new float[totalSamples];

            for (var noteIndex = 0; noteIndex < notes.Length; noteIndex++)
            {
                var frequency = notes[noteIndex];
                for (var sampleIndex = 0; sampleIndex < noteSamples; sampleIndex++)
                {
                    var time = sampleIndex / (float)sampleRate;
                    var fade = Mathf.Clamp01(sampleIndex / (float)(noteSamples * 0.08f))
                        * Mathf.Clamp01((noteSamples - sampleIndex) / (float)(noteSamples * 0.12f));
                    var sampleValue = Mathf.Sin(2f * Mathf.PI * frequency * time) * volume * fade;
                    data[noteIndex * noteSamples + sampleIndex] = sampleValue;
                }
            }

            var clip = AudioClip.Create("ForestCue", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
