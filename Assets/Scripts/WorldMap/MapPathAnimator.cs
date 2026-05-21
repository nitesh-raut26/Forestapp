using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Animates path connections on the world map.
    ///
    /// When a region unlocks, the connecting path lights up with a traveling
    /// particle that sweeps from the previously-unlocked region to the new one.
    ///
    /// Path states:
    ///   - Locked:   dim gray, no particles
    ///   - Open:     soft green glow, idle pulse
    ///   - Traveled: bright with ambient wander particles
    /// </summary>
    public class MapPathAnimator : MonoBehaviour
    {
        private readonly Dictionary<string, PathEntry> _paths =
            new Dictionary<string, PathEntry>();

        private static readonly Color LockedPathColor   = new Color(0.2f, 0.3f, 0.22f, 0.3f);
        private static readonly Color OpenPathColor     = new Color(0.3f, 0.65f, 0.4f, 0.7f);
        private static readonly Color TraveledPathColor = new Color(0.5f, 0.9f, 0.55f, 0.9f);
        private static readonly Color ParticleColor     = new Color(0.7f, 1f, 0.6f, 0.9f);

        // ─── Public API ───────────────────────────────────────────────────────────

        public void RegisterPath(string fromId, string toId, RectTransform lineRect, Image lineImage)
        {
            var key = $"{fromId}->{toId}";
            _paths[key] = new PathEntry
            {
                FromId    = fromId,
                ToId      = toId,
                LineRect  = lineRect,
                LineImage = lineImage,
                IsOpen    = false
            };

            lineImage.color = LockedPathColor;
        }

        /// <summary>Animate a path opening — traveling light from A to B.</summary>
        public void AnimatePathOpen(string fromId, string toId, Action onComplete = null)
        {
            var key = $"{fromId}->{toId}";
            if (!_paths.TryGetValue(key, out var entry)) { onComplete?.Invoke(); return; }

            entry.IsOpen = true;
            StartCoroutine(DoAnimatePath(entry, onComplete));
        }

        /// <summary>Set all paths open/closed based on world state.</summary>
        public void RefreshAll(WorldStateManager world)
        {
            if (world == null) return;

            foreach (var path in world.GetOpenPaths())
            {
                var key = $"{path.fromRegionId}->{path.toRegionId}";
                if (_paths.TryGetValue(key, out var entry))
                {
                    entry.IsOpen       = path.isOpen;
                    entry.LineImage.color = path.isOpen ? OpenPathColor : LockedPathColor;
                }
            }
        }

        // ─── Coroutine ────────────────────────────────────────────────────────────

        private IEnumerator DoAnimatePath(PathEntry entry, Action onComplete)
        {
            // Phase 1: Line illuminates from dim to bright
            var elapsed  = 0f;
            var duration = 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                entry.LineImage.color = Color.Lerp(LockedPathColor, OpenPathColor, t);
                yield return null;
            }

            entry.LineImage.color = OpenPathColor;

            // Phase 2: Traveling particle along path
            yield return StartCoroutine(SpawnTravelParticle(entry));

            // Phase 3: Settle to traveled color
            elapsed  = 0f;
            duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                entry.LineImage.color = Color.Lerp(OpenPathColor, TraveledPathColor, t);
                yield return null;
            }
            entry.LineImage.color = TraveledPathColor;

            // Idle pulse coroutine
            StartCoroutine(IdlePulse(entry));

            onComplete?.Invoke();
        }

        private IEnumerator SpawnTravelParticle(PathEntry entry)
        {
            // Create a small glowing dot that travels along the line
            var particleGo = new GameObject("TravelParticle");
            particleGo.transform.SetParent(entry.LineRect, false);
            var particleRt = particleGo.AddComponent<RectTransform>();
            particleRt.sizeDelta = new Vector2(14f, 14f);
            particleRt.pivot     = new Vector2(0.5f, 0.5f);

            var particleImg = particleGo.AddComponent<Image>();
            particleImg.color = ParticleColor;

            var lineLength = entry.LineRect.sizeDelta.x;
            var elapsed    = 0f;
            var duration   = 0.6f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t  = Mathf.Clamp01(elapsed / duration);
                var et = 1f - Mathf.Pow(1f - t, 2f); // ease out

                particleRt.anchoredPosition = new Vector2(
                    Mathf.Lerp(-lineLength * 0.5f, lineLength * 0.5f, et),
                    0f
                );
                particleImg.color = new Color(
                    ParticleColor.r, ParticleColor.g, ParticleColor.b,
                    Mathf.Sin(t * Mathf.PI));

                yield return null;
            }

            Destroy(particleGo);
        }

        private IEnumerator IdlePulse(PathEntry entry)
        {
            while (entry.IsOpen && entry.LineImage != null)
            {
                var t = (Mathf.Sin(Time.time * 1.2f) + 1f) * 0.5f;
                var alpha = Mathf.Lerp(0.55f, 0.9f, t);
                var col = TraveledPathColor;
                entry.LineImage.color = new Color(col.r, col.g, col.b, alpha);
                yield return null;
            }
        }

        // ─── Data ─────────────────────────────────────────────────────────────────

        private class PathEntry
        {
            public string       FromId;
            public string       ToId;
            public RectTransform LineRect;
            public Image        LineImage;
            public bool         IsOpen;
        }
    }
}
