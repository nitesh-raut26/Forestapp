using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Fog of war for the world map. Locked regions are visually obscured.
    ///
    /// Implementation:
    ///   - Locked regions have CanvasGroup.alpha reduced to 0.2–0.4 and a
    ///     fog overlay Image layered on top
    ///   - When a region unlocks, DispelFog() animates the fog away
    ///   - Fog color matches the current biome/season atmosphere
    ///
    /// No new textures required — fog is a colored Image with gradient alpha
    /// achieved by stacking multiple semi-transparent panels.
    /// </summary>
    public class FogOfWarSystem : MonoBehaviour
    {
        private readonly Dictionary<string, FogEntry> _fogEntries =
            new Dictionary<string, FogEntry>();

        private static readonly Color FogColor = new Color(0.05f, 0.12f, 0.08f, 0.75f);

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Register a region node and apply fog if it's locked.</summary>
        public void ApplyFog(string regionId, CanvasGroup nodeGroup)
        {
            var fog = CreateFogOverlay(nodeGroup.GetComponent<RectTransform>());

            _fogEntries[regionId] = new FogEntry
            {
                RegionId   = regionId,
                NodeGroup  = nodeGroup,
                FogOverlay = fog
            };

            // Initial state based on world
            SetFogInstant(regionId, true);
        }

        /// <summary>Called when a region is unlocked — plays dispel animation.</summary>
        public void DispelFog(string regionId, System.Action onComplete = null)
        {
            if (!_fogEntries.TryGetValue(regionId, out var entry)) return;
            StartCoroutine(AnimateDispel(entry, onComplete));
        }

        /// <summary>Refresh fog state from world state manager.</summary>
        public void RefreshAll(WorldStateManager world)
        {
            if (world == null) return;

            foreach (var region in world.GetAllRegions())
            {
                var isLocked = region.unlockState == RegionUnlockState.Locked;
                SetFogInstant(region.regionId, isLocked);
            }
        }

        // ─── Private ─────────────────────────────────────────────────────────────

        private void SetFogInstant(string regionId, bool fogged)
        {
            if (!_fogEntries.TryGetValue(regionId, out var entry)) return;

            entry.NodeGroup.alpha  = fogged ? 0.3f : 1f;
            if (entry.FogOverlay != null)
                entry.FogOverlay.gameObject.SetActive(fogged);
        }

        private IEnumerator AnimateDispel(FogEntry entry, System.Action onComplete)
        {
            if (entry.FogOverlay == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var fogImg    = entry.FogOverlay;
            var nodeGroup = entry.NodeGroup;
            var elapsed   = 0f;
            var duration  = 1.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                // Fog dissolves outward
                fogImg.color = new Color(FogColor.r, FogColor.g, FogColor.b,
                    Mathf.Lerp(FogColor.a, 0f, t));

                // Node becomes fully visible
                nodeGroup.alpha = Mathf.Lerp(0.3f, 1f, t);

                // Slight scale pulse on node
                if (nodeGroup.transform is RectTransform rt)
                    rt.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f);

                yield return null;
            }

            fogImg.gameObject.SetActive(false);
            nodeGroup.alpha = 1f;
            if (nodeGroup.transform is RectTransform finalRt)
                finalRt.localScale = Vector3.one;

            onComplete?.Invoke();
        }

        private static Image CreateFogOverlay(RectTransform nodeRect)
        {
            if (nodeRect == null) return null;

            // Expand beyond node bounds for a soft-edge feel
            var fogGo  = new GameObject("FogOverlay");
            fogGo.transform.SetParent(nodeRect, false);
            var fogRt  = fogGo.AddComponent<RectTransform>();
            fogRt.anchorMin = new Vector2(-0.3f, -0.3f);
            fogRt.anchorMax = new Vector2(1.3f, 1.3f);
            fogRt.sizeDelta = Vector2.zero;

            var fogImg   = fogGo.AddComponent<Image>();
            fogImg.color = FogColor;
            fogImg.raycastTarget = false;

            return fogImg;
        }

        // ─── Data ─────────────────────────────────────────────────────────────────

        private class FogEntry
        {
            public string      RegionId;
            public CanvasGroup NodeGroup;
            public Image       FogOverlay;
        }
    }
}
