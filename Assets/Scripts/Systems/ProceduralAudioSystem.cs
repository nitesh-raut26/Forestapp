using UnityEngine;

namespace ForestFriendsQuest
{
    public class ProceduralAudioSystem : MonoBehaviour
    {
        private AudioSource _ambientSource;
        private AudioSource _sfxSource;
        
        private float _musicIntensity = 0.5f;
        private float _ambientVolume = 0.3f;
        private bool _soundEnabled = true;

        private void Awake()
        {
            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.loop = true;
            _ambientSource.playOnAwake = false;
            
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }

        private void Start()
        {
            if (_soundEnabled)
            {
                StartAmbientGenerator();
            }
        }

        public void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
            if (!enabled)
            {
                _ambientSource.Stop();
                _sfxSource.Stop();
            }
            else
            {
                StartAmbientGenerator();
            }
        }

        public void SetIntensity(float intensity)
        {
            _musicIntensity = Mathf.Clamp01(intensity);
        }

        private void StartAmbientGenerator()
        {
            // Generate a procedurally synthesized soothing white/pink noise wave for wind/rain
            var clip = GenerateSoothingNoiseClip(10f); // 10 second looping clip
            _ambientSource.clip = clip;
            _ambientSource.volume = _ambientVolume;
            _ambientSource.Play();
        }

        public void UpdateAmbientStems(TimeOfDay time, WeatherState weather)
        {
            if (!_soundEnabled) return;

            // Change pitch/frequency parameters based on weather & time of day
            var targetVolume = 0.25f;
            var targetPitch = 1.0f;

            if (weather == WeatherState.Rainy)
            {
                targetVolume = 0.45f;
                targetPitch = 0.85f; // Deep, rich rain rumble
            }
            else if (weather == WeatherState.Foggy)
            {
                targetVolume = 0.18f;
                targetPitch = 0.7f; // Dampened, quiet wind
            }

            if (time == TimeOfDay.Night)
            {
                targetVolume *= 0.6f; // Softer at night
                targetPitch *= 0.8f; // Sleepy frequencies
            }

            _ambientSource.volume = targetVolume;
            _ambientSource.pitch = targetPitch;
        }

        public void PlayContextChord(bool success, CharacterProfile profile = null)
        {
            if (!_soundEnabled) return;

            float root = GetCharacterBaseFrequency(profile);
            float[] notes;

            if (success)
            {
                // Major 7th ascending cozy chords
                notes = new[] { root, root * 1.25f, root * 1.5f, root * 1.875f };
                PlayToneSequence(notes, 0.12f, 0.12f);
            }
            else
            {
                // Soft minor/diminished gentle reminder chords (no harsh crash)
                notes = new[] { root * 1.2f, root, root * 0.95f };
                PlayToneSequence(notes, 0.15f, 0.08f);
            }
        }

        public void PlayTapCue()
        {
            if (!_soundEnabled) return;
            // Short high-pitched woody chime
            PlayToneSequence(new[] { 660f, 880f }, 0.06f, 0.05f);
        }

        public void PlayItemCrafted()
        {
            if (!_soundEnabled) return;
            // Shimmering alchemical ascending sweep
            PlayToneSequence(new[] { 330f, 440f, 550f, 660f, 880f, 1100f }, 0.08f, 0.1f);
        }

        private float GetCharacterBaseFrequency(CharacterProfile profile)
        {
            if (profile == null || profile.voice == null) return 440f;
            return Mathf.Clamp(280f + (profile.voice.pitch * 160f), 180f, 880f);
        }

        public void PlayToneSequence(float[] notes, float duration, float volume)
        {
            const int sampleRate = 44100;
            var noteSamples = Mathf.RoundToInt(sampleRate * duration);
            var totalSamples = noteSamples * notes.Length;
            var data = new float[totalSamples];

            for (var noteIndex = 0; noteIndex < notes.Length; noteIndex++)
            {
                var freq = notes[noteIndex];
                for (var sampleIndex = 0; sampleIndex < noteSamples; sampleIndex++)
                {
                    var time = sampleIndex / (float)sampleRate;
                    var fade = Mathf.Clamp01(sampleIndex / (float)(noteSamples * 0.1f))
                        * Mathf.Clamp01((noteSamples - sampleIndex) / (float)(noteSamples * 0.15f));
                    
                    // Layer primary note and soft warm subharmonic octave
                    var primary = Mathf.Sin(2f * Mathf.PI * freq * time);
                    var secondary = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * time) * 0.35f;

                    data[noteIndex * noteSamples + sampleIndex] = (primary + secondary) * volume * fade;
                }
            }

            var clip = AudioClip.Create("AudioSystemCue", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            _sfxSource.PlayOneShot(clip);
        }

        private AudioClip GenerateSoothingNoiseClip(float duration)
        {
            const int sampleRate = 44100;
            var totalSamples = Mathf.RoundToInt(sampleRate * duration);
            var data = new float[totalSamples];

            var filter = 0f;
            for (var i = 0; i < totalSamples; i++)
            {
                // Generate a gentle wind rustle using low-pass filtered random values
                var rawRandom = UnityEngine.Random.Range(-1f, 1f);
                filter = Mathf.Lerp(filter, rawRandom, 0.08f); // Soft LPF
                
                // Add a very low harmonic waves to simulate waves or trees swaying
                var sway = Mathf.Sin(2f * Mathf.PI * 0.15f * (i / (float)sampleRate)) * 0.2f;
                data[i] = (filter + sway) * 0.25f;
            }

            var clip = AudioClip.Create("SoothingWindLoop", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
