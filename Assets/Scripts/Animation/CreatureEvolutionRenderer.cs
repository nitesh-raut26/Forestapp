using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Handles the visual evolution transition when a creature reaches a new bond tier.
    ///
    /// Bond tiers → visual stages:
    ///   0: Egg / silhouette        (gray, small)
    ///   1: Hatchling               (desaturated, 80% scale)
    ///   2: Young                   (partial color, 90% scale)
    ///   3: Friend                  (full color, 100% scale)
    ///   4: Companion               (slight glow, 105% scale)
    ///   5: Soul Bond               (animated glow + star burst, 110% scale)
    ///
    /// No sprite assets needed — achieved through color overlay and scale.
    /// Sprite substitution is possible by swapping Image.sprite externally.
    /// </summary>
    public class CreatureEvolutionRenderer : MonoBehaviour
    {
        private Image         _creatureImage;
        private RectTransform _creatureRect;
        private Image         _glowRing;

        private static readonly Color EggTint       = new Color(0.35f, 0.35f, 0.4f, 0.6f);
        private static readonly Color HatchlingTint  = new Color(0.65f, 0.65f, 0.7f, 0.8f);
        private static readonly Color YoungTint      = new Color(0.82f, 0.88f, 0.78f, 0.9f);
        private static readonly Color FriendTint     = Color.white;
        private static readonly Color CompanionTint  = new Color(1f, 0.98f, 0.88f, 1f);
        private static readonly Color SoulBondTint   = new Color(0.92f, 1f, 0.78f, 1f);

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(Image creatureImage, RectTransform creatureRect)
        {
            _creatureImage = creatureImage;
            _creatureRect  = creatureRect;
            _glowRing      = CreateGlowRing(creatureRect);
        }

        /// <summary>Apply visual stage instantly (called on load from save).</summary>
        public void ApplyStageInstant(int bondLevel)
        {
            var (tint, scale, glowAlpha) = GetStageParams(bondLevel);
            _creatureImage.color = tint;
            _creatureRect.localScale = Vector3.one * scale;
            SetGlowAlpha(glowAlpha);
        }

        /// <summary>Play the evolution cinematic from oldLevel to newLevel.</summary>
        public void PlayEvolution(int oldLevel, int newLevel)
        {
            StartCoroutine(DoEvolution(oldLevel, newLevel));
        }

        // ─── Coroutine ────────────────────────────────────────────────────────────

        private IEnumerator DoEvolution(int oldLevel, int newLevel)
        {
            var (fromTint, fromScale, fromGlow) = GetStageParams(oldLevel);
            var (toTint,   toScale,   toGlow)   = GetStageParams(newLevel);

            // Phase 1: white flash
            yield return FlashWhite(0.4f);

            // Phase 2: scale down slightly (anticipation)
            yield return TweenScale(_creatureRect, fromScale, fromScale * 0.85f, 0.15f);

            // Phase 3: scale up to new size + color transition
            const float duration = 0.7f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
                _creatureImage.color = Color.Lerp(fromTint, toTint, t);
                _creatureRect.localScale = Vector3.one * Mathf.Lerp(fromScale * 0.85f, toScale, t);
                SetGlowAlpha(Mathf.Lerp(fromGlow, toGlow, t));
                yield return null;
            }

            _creatureImage.color = toTint;
            _creatureRect.localScale = Vector3.one * toScale;

            // Phase 4: Soul Bond sparkle burst
            if (newLevel >= 5)
                yield return SoulBondBurst();

            // Phase 5: settle with gentle pulse
            yield return TweenScale(_creatureRect, toScale, toScale * 1.05f, 0.2f);
            yield return TweenScale(_creatureRect, toScale * 1.05f, toScale, 0.2f);
        }

        private IEnumerator FlashWhite(float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI);
                _creatureImage.color = Color.Lerp(_creatureImage.color, Color.white, t * 0.6f);
                yield return null;
            }
        }

        private IEnumerator SoulBondBurst()
        {
            // Animated glow ring expansion
            if (_glowRing != null)
            {
                var rt = _glowRing.GetComponent<RectTransform>();
                var elapsed = 0f;
                const float duration = 0.6f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    rt.localScale = Vector3.one * Mathf.Lerp(1f, 2.5f, t);
                    _glowRing.color = new Color(0.8f, 1f, 0.6f,
                        Mathf.Lerp(0.8f, 0f, t));
                    yield return null;
                }

                rt.localScale = Vector3.one;
                SetGlowAlpha(0.5f);
            }
        }

        // ─── Glow Ring ────────────────────────────────────────────────────────────

        private static Image CreateGlowRing(RectTransform parent)
        {
            var go = new GameObject("GlowRing");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(-0.25f, -0.25f);
            rt.anchorMax = new Vector2(1.25f, 1.25f);
            rt.sizeDelta = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.7f, 1f, 0.5f, 0f);
            img.raycastTarget = false;

            // Move behind creature (sibling index 0)
            go.transform.SetAsFirstSibling();
            return img;
        }

        private void SetGlowAlpha(float alpha)
        {
            if (_glowRing == null) return;
            var c = _glowRing.color;
            _glowRing.color = new Color(c.r, c.g, c.b, alpha);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static (Color tint, float scale, float glowAlpha) GetStageParams(int bondLevel)
        {
            return bondLevel switch
            {
                0 => (EggTint,      0.70f, 0.0f),
                1 => (HatchlingTint, 0.80f, 0.0f),
                2 => (YoungTint,    0.90f, 0.05f),
                3 => (FriendTint,   1.00f, 0.10f),
                4 => (CompanionTint, 1.05f, 0.30f),
                _ => (SoulBondTint,  1.10f, 0.50f)
            };
        }

        private static IEnumerator TweenScale(RectTransform rt, float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                rt.localScale = Vector3.one * Mathf.Lerp(from, to, t);
                yield return null;
            }
            rt.localScale = Vector3.one * to;
        }
    }
}
