using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Centralized registry of all procedurally-generated audio clips.
    ///
    /// Because Forest Friends Quest ships without audio file assets, all music
    /// and SFX are generated at runtime via the sine/harmonic engine from
    /// ForestAudioController. This library caches those clips so they are not
    /// re-synthesized every frame.
    ///
    /// Clip IDs are stable string keys used by ForestMusicDirector and PuzzleSFXManager.
    /// </summary>
    public class AudioAssetLibrary : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> _cache =
            new Dictionary<string, AudioClip>();

        private const int SampleRate = 44100;

        // ─── Music Themes ─────────────────────────────────────────────────────────

        public AudioClip GetTheme(string biomeId)
        {
            var key = $"theme_{biomeId}";
            if (!_cache.TryGetValue(key, out var clip))
            {
                clip = biomeId switch
                {
                    "fern-trail"     => SynthLoop(new[] { 261.6f, 293.7f, 329.6f, 349.2f }, 0.4f, 0.12f),
                    "firefly-hollow" => SynthLoop(new[] { 220f, 246.9f, 261.6f, 293.7f },   0.5f, 0.10f),
                    "moonlit-creek"  => SynthLoop(new[] { 196f, 220f, 246.9f, 261.6f },     0.6f, 0.09f),
                    "skyroot-canopy" => SynthLoop(new[] { 392f, 440f, 493.9f, 523.3f },     0.3f, 0.11f),
                    _                => SynthLoop(new[] { 261.6f, 329.6f, 392f },           0.45f, 0.10f)
                };
                _cache[key] = clip;
            }
            return clip;
        }

        // ─── SFX Clips ────────────────────────────────────────────────────────────

        public AudioClip GetSFX(string sfxId)
        {
            if (!_cache.TryGetValue(sfxId, out var clip))
            {
                clip = sfxId switch
                {
                    "puzzle_correct"  => SynthChord(new[] { 523.3f, 659.3f, 783.9f }, 0.25f, 0.15f),
                    "puzzle_wrong"    => SynthDescend(new[] { 440f, 370f, 311f },     0.18f, 0.13f),
                    "puzzle_complete" => SynthFanfare(0.14f),
                    "region_unlock"   => SynthSwell(523.3f, 0.8f, 0.12f),
                    "bond_up"         => SynthChord(new[] { 440f, 554.4f, 659.3f },   0.3f, 0.12f),
                    "treat_feed"      => SynthJingle(new[] { 392f, 440f, 523.3f },    0.12f, 0.10f),
                    "tap_select"      => SynthShort(523.3f, 0.06f, 0.09f),
                    "tap_back"        => SynthShort(392f, 0.06f, 0.08f),
                    "ritual_start"    => SynthSwell(349.2f, 0.5f, 0.10f),
                    "ritual_complete" => SynthFanfare(0.11f),
                    "star_earn"       => SynthChord(new[] { 659.3f, 783.9f, 880f },   0.2f, 0.13f),
                    "fog_dispel"      => SynthSwell(261.6f, 0.7f, 0.08f),
                    _                 => SynthShort(440f, 0.05f, 0.08f)
                };
                _cache[sfxId] = clip;
            }
            return clip;
        }

        // ─── Character Cue Lines ─────────────────────────────────────────────────
        // Richer multi-note phrases for "greeting", "hint", and "cheer" per character.
        // Each phrase reflects the character's personality and pitch register.

        public AudioClip GetCharacterCueLine(string characterId, string cueType)
        {
            var key = $"cue_{characterId}_{cueType}";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var clip = characterId switch
            {
                "pip"  => PipCue(cueType),
                "mimi" => MimiCue(cueType),
                "tomo" => TomoCue(cueType),
                "luma" => LumaCue(cueType),
                "nori" => NoriCue(cueType),
                "sol"  => SolCue(cueType),
                _      => null
            };

            if (clip != null) _cache[key] = clip;
            return clip;
        }

        // pip — bright fox scout (C5 = 523.3 Hz): quick, chirpy, adventurous
        private AudioClip PipCue(string cue) => cue switch
        {
            "greeting" => SynthLoop(new[] { 523.3f, 587.3f, 659.3f, 783.9f },         0.08f, 0.11f),
            "hint"     => SynthLoop(new[] { 587.3f, 523.3f, 659.3f },                 0.09f, 0.10f),
            "cheer"    => SynthLoop(new[] { 523.3f, 659.3f, 783.9f, 880f, 1046.5f }, 0.07f, 0.12f),
            _          => SynthChirp(523.3f, 2, 0.07f, 0.10f)
        };

        // mimi — sweet songbird (E5 = 659.3 Hz): melodic, harmonic, joyful
        private AudioClip MimiCue(string cue) => cue switch
        {
            "greeting" => SynthLoop(new[] { 659.3f, 783.9f, 880f, 783.9f },           0.10f, 0.13f),
            "hint"     => SynthLoop(new[] { 783.9f, 880f, 659.3f },                   0.11f, 0.12f),
            "cheer"    => SynthChord(new[] { 659.3f, 783.9f, 880f, 987.8f },          0.35f, 0.13f),
            _          => SynthChirp(659.3f, 2, 0.08f, 0.11f)
        };

        // tomo — grounded turtle (G4 = 392 Hz): calm, deliberate, wise
        private AudioClip TomoCue(string cue) => cue switch
        {
            "greeting" => SynthLoop(new[] { 392f, 440f, 392f },                        0.18f, 0.09f),
            "hint"     => SynthLoop(new[] { 440f, 392f, 349.2f },                      0.17f, 0.09f),
            "cheer"    => SynthLoop(new[] { 392f, 493.9f, 587.3f },                    0.16f, 0.10f),
            _          => SynthShort(392f, 0.15f, 0.09f)
        };

        // luma — sparkly firefly (D5 = 587.3 Hz): quick, ethereal, twinkling
        private AudioClip LumaCue(string cue) => cue switch
        {
            "greeting" => SynthLoop(new[] { 587.3f, 659.3f, 587.3f, 783.9f },         0.07f, 0.12f),
            "hint"     => SynthLoop(new[] { 659.3f, 783.9f, 587.3f },                 0.08f, 0.11f),
            "cheer"    => SynthLoop(new[] { 587.3f, 783.9f, 880f, 1046.5f },          0.07f, 0.13f),
            _          => SynthChirp(587.3f, 3, 0.06f, 0.11f)
        };

        // nori — gentle deer guardian (F4 = 349.2 Hz): soft, earthy, natural
        private AudioClip NoriCue(string cue) => cue switch
        {
            "greeting" => SynthLoop(new[] { 349.2f, 392f, 440f, 392f },               0.14f, 0.10f),
            "hint"     => SynthLoop(new[] { 392f, 349.2f, 440f },                     0.15f, 0.10f),
            "cheer"    => SynthLoop(new[] { 349.2f, 440f, 523.3f, 587.3f },           0.13f, 0.11f),
            _          => SynthShort(349.2f, 0.12f, 0.09f)
        };

        // sol — wise arch-druid owl (A4 = 440 Hz): resonant, complex, majestic
        private AudioClip SolCue(string cue) => cue switch
        {
            "greeting" => SynthLoop(new[] { 440f, 349.2f, 440f },                     0.20f, 0.11f),
            "hint"     => SynthChord(new[] { 440f, 523.3f, 392f },                    0.30f, 0.10f),
            "cheer"    => SynthLoop(new[] { 440f, 523.3f, 659.3f, 783.9f },           0.15f, 0.12f),
            _          => SynthShort(440f, 0.18f, 0.10f)
        };

        // ─── Creature Voice Clips ─────────────────────────────────────────────────

        public AudioClip GetCreatureVoice(string creatureId, CreatureEmotion emotion)
        {
            var key = $"voice_{creatureId}_{emotion}";
            if (_cache.TryGetValue(key, out var clip)) return clip;

            var baseFreq = GetCreatureFrequency(creatureId);
            clip = emotion switch
            {
                CreatureEmotion.Happy   => SynthChirp(baseFreq * 1.2f, 3, 0.06f, 0.09f),
                CreatureEmotion.Excited => SynthChirp(baseFreq * 1.35f, 4, 0.05f, 0.11f),
                CreatureEmotion.Sad     => SynthDescend(new[] { baseFreq, baseFreq * 0.88f }, 0.2f, 0.07f),
                CreatureEmotion.Hungry  => SynthChirp(baseFreq * 0.85f, 2, 0.1f, 0.08f),
                CreatureEmotion.Sleepy  => SynthShort(baseFreq * 0.75f, 0.3f, 0.05f),
                CreatureEmotion.Shy     => SynthShort(baseFreq * 0.9f, 0.08f, 0.06f),
                _                      => SynthShort(baseFreq, 0.07f, 0.07f)
            };

            _cache[key] = clip;
            return clip;
        }

        // ─── Synth Engine ─────────────────────────────────────────────────────────

        private AudioClip SynthLoop(float[] notes, float noteDuration, float volume)
        {
            var noteSamples = Mathf.RoundToInt(SampleRate * noteDuration);
            var data        = new float[noteSamples * notes.Length];

            for (var ni = 0; ni < notes.Length; ni++)
            {
                var freq = notes[ni];
                for (var si = 0; si < noteSamples; si++)
                {
                    var t     = si / (float)SampleRate;
                    var env   = Envelope(si, noteSamples, 0.1f, 0.15f);
                    // Sine + subtle second harmonic for warmth
                    data[ni * noteSamples + si] =
                        (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f +
                         Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.15f) * volume * env;
                }
            }

            return MakeClip("loop", data);
        }

        private AudioClip SynthChord(float[] freqs, float duration, float volume)
        {
            var samples = Mathf.RoundToInt(SampleRate * duration);
            var data    = new float[samples];

            for (var si = 0; si < samples; si++)
            {
                var t   = si / (float)SampleRate;
                var env = Envelope(si, samples, 0.02f, 0.25f);
                var sum = 0f;
                foreach (var f in freqs)
                    sum += Mathf.Sin(2f * Mathf.PI * f * t);
                data[si] = sum / freqs.Length * volume * env;
            }

            return MakeClip("chord", data);
        }

        private AudioClip SynthDescend(float[] notes, float noteDuration, float volume)
        {
            var noteSamples = Mathf.RoundToInt(SampleRate * noteDuration);
            var data        = new float[noteSamples * notes.Length];

            for (var ni = 0; ni < notes.Length; ni++)
            {
                var freq = notes[ni];
                for (var si = 0; si < noteSamples; si++)
                {
                    var t   = si / (float)SampleRate;
                    var env = Envelope(si, noteSamples, 0.01f, 0.2f);
                    data[ni * noteSamples + si] =
                        Mathf.Sin(2f * Mathf.PI * freq * t) * volume * env;
                }
            }

            return MakeClip("descend", data);
        }

        private AudioClip SynthFanfare(float volume)
        {
            // C major arpeggio then chord
            var notes   = new float[] { 523.3f, 659.3f, 783.9f, 880f };
            var noteLen = Mathf.RoundToInt(SampleRate * 0.12f);
            var holdLen = Mathf.RoundToInt(SampleRate * 0.4f);
            var data    = new float[noteLen * notes.Length + holdLen];

            for (var ni = 0; ni < notes.Length; ni++)
            {
                var freq = notes[ni];
                for (var si = 0; si < noteLen; si++)
                {
                    var t   = si / (float)SampleRate;
                    var env = Envelope(si, noteLen, 0.02f, 0.18f);
                    data[ni * noteLen + si] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * env;
                }
            }

            var offset = noteLen * notes.Length;
            for (var si = 0; si < holdLen; si++)
            {
                var t   = si / (float)SampleRate;
                var env = Envelope(si, holdLen, 0.02f, 0.35f);
                var sum = 0f;
                foreach (var f in notes)
                    sum += Mathf.Sin(2f * Mathf.PI * f * t);
                data[offset + si] = sum / notes.Length * volume * env;
            }

            return MakeClip("fanfare", data);
        }

        private AudioClip SynthSwell(float freq, float duration, float volume)
        {
            var samples = Mathf.RoundToInt(SampleRate * duration);
            var data    = new float[samples];

            for (var si = 0; si < samples; si++)
            {
                var t   = si / (float)SampleRate;
                var env = Mathf.Sin(Mathf.Clamp01((float)si / samples) * Mathf.PI);
                data[si] = Mathf.Sin(2f * Mathf.PI * freq * t) * volume * env;
            }

            return MakeClip("swell", data);
        }

        private AudioClip SynthJingle(float[] notes, float noteDuration, float volume)
        {
            return SynthLoop(notes, noteDuration, volume);
        }

        private AudioClip SynthShort(float freq, float duration, float volume)
        {
            return SynthSwell(freq, duration, volume);
        }

        private AudioClip SynthChirp(float freq, int chirps, float chirpDuration, float volume)
        {
            var chirpSamples = Mathf.RoundToInt(SampleRate * chirpDuration);
            var data         = new float[chirpSamples * chirps];

            for (var ci = 0; ci < chirps; ci++)
            {
                var f = freq * (1f + ci * 0.05f);
                for (var si = 0; si < chirpSamples; si++)
                {
                    var t   = si / (float)SampleRate;
                    var env = Envelope(si, chirpSamples, 0.05f, 0.2f);
                    data[ci * chirpSamples + si] =
                        Mathf.Sin(2f * Mathf.PI * f * t) * volume * env;
                }
            }

            return MakeClip("chirp", data);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static float Envelope(int sample, int total, float attackFrac, float releaseFrac)
        {
            var attack  = (int)(total * attackFrac);
            var release = (int)(total * releaseFrac);
            if (sample < attack)        return sample / (float)attack;
            if (sample > total - release) return (total - sample) / (float)release;
            return 1f;
        }

        private static AudioClip MakeClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float GetCreatureFrequency(string creatureId) => creatureId switch
        {
            "pip"  => 523.3f,  // C5 - bright
            "mimi" => 659.3f,  // E5 - sweet
            "tomo" => 392f,    // G4 - grounded
            "luma" => 587.3f,  // D5 - warm
            "nori" => 349.2f,  // F4 - earthy
            "sol"  => 440f,    // A4 - classic
            _      => 440f
        };
    }
}
