using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Central VFX coordinator. Routes all visual feedback requests to the
    /// appropriate subsystem (EmotionalParticleEngine, ProceduralGlowSystem,
    /// AmbientVFXController). All callers interact with this single facade.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        // ─── Sub-systems ─────────────────────────────────────────────────────────

        private EmotionalParticleEngine _particles;
        private ProceduralGlowSystem    _glow;
        private AmbientVFXController    _ambient;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            _particles = GetComponentInChildren<EmotionalParticleEngine>(true);
            _glow      = GetComponentInChildren<ProceduralGlowSystem>(true);
            _ambient   = GetComponentInChildren<AmbientVFXController>(true);
        }

        public void Initialize(
            EmotionalParticleEngine particles,
            ProceduralGlowSystem    glow,
            AmbientVFXController    ambient)
        {
            _particles = particles;
            _glow      = glow;
            _ambient   = ambient;
        }

        // ─── Creature Emotional Events ────────────────────────────────────────────

        /// <summary>Called when a creature is happy (e.g. fed favorite treat, puzzle solved).</summary>
        public void OnCreatureHappy(RectTransform creatureRect)
        {
            if (_particles == null || creatureRect == null) return;
            _particles.SpawnHappyBurst(creatureRect.anchoredPosition + Vector2.up * 60f);
            _glow?.PulseGlow(creatureRect, Color.yellow, 0.8f);
        }

        /// <summary>Called when a creature is sleeping.</summary>
        public void OnCreatureSleep(RectTransform creatureRect)
        {
            if (_particles == null || creatureRect == null) return;
            _particles.SpawnSleepParticles(creatureRect.anchoredPosition + Vector2.up * 80f);
        }

        /// <summary>Called on joy spin gesture.</summary>
        public void OnCreatureJoy(RectTransform creatureRect)
        {
            if (_particles == null || creatureRect == null) return;
            _particles.SpawnJoyBurst(creatureRect.anchoredPosition + Vector2.up * 50f);
        }

        /// <summary>Called when creature is thankful (fed, long interaction).</summary>
        public void OnCreatureThankful(RectTransform creatureRect)
        {
            if (_particles == null || creatureRect == null) return;
            _particles.SpawnThankfulParticles(creatureRect.anchoredPosition + Vector2.up * 40f);
        }

        // ─── World Discovery Events ───────────────────────────────────────────────

        /// <summary>Called on lore discovery, rune unlock, hidden area reveal.</summary>
        public void OnDiscovery(Vector2 canvasPos)
        {
            _particles?.SpawnDiscoveryBurst(canvasPos);
            _glow?.SpawnDiscoveryRing(canvasPos);
        }

        /// <summary>Called on rare item drop, achievement unlock, rare creature sighting.</summary>
        public void OnRareReward(Vector2 canvasPos)
        {
            _particles?.SpawnRareRewardBurst(canvasPos);
            _glow?.SpawnCrystalBurst(canvasPos);
        }

        // ─── Puzzle Events ────────────────────────────────────────────────────────

        /// <summary>Called when a puzzle is solved correctly.</summary>
        public void OnPuzzleSolved(Vector2 canvasPos)
        {
            _particles?.SpawnJoyBurst(canvasPos);
            _particles?.Spawn(EmotionalParticleType.HappyPollenBurst, canvasPos, 8);
            _glow?.PulseScreen(Color.green, 0.6f);
        }

        /// <summary>Called when a puzzle step is wrong — soft, not punishing.</summary>
        public void OnPuzzleError(Vector2 canvasPos)
        {
            _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, canvasPos, 3);
        }

        /// <summary>Called when a node in a puzzle is selected correctly.</summary>
        public void OnPuzzleNodeSelect(Vector2 canvasPos)
        {
            _particles?.Spawn(EmotionalParticleType.DiscoveryRuneGlow, canvasPos, 2);
        }

        // ─── World Interaction Events ─────────────────────────────────────────────

        /// <summary>Grass footstep disturb (walking through meadow).</summary>
        public void OnGrassDisturb(Vector2 pos)
        {
            _particles?.Spawn(EmotionalParticleType.GrassDisturbDust, pos, 4);
        }

        /// <summary>Firefly appeared at night.</summary>
        public void OnFireflySpawn(Vector2 pos)
        {
            _particles?.Spawn(EmotionalParticleType.FireflyWander, pos, 1);
        }

        // ─── Ambient Control ─────────────────────────────────────────────────────

        public void SetAmbientState(TimeOfDay time, WeatherState weather)
        {
            _ambient?.UpdateAmbientState(time, weather);
        }
    }
}
