using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Plays a cinematic unlock sequence when a new world region becomes available.
    ///
    /// Sequence:
    ///   1. Region node pulses white (light explosion)
    ///   2. Region name fades in above node
    ///   3. Surrounding particle burst (firefly wisps)
    ///   4. "Region Unlocked!" banner slides in from top
    ///   5. Banner auto-dismisses after 2.5 seconds
    ///
    /// Called by WorldMapController when OnRegionUnlocked fires.
    /// </summary>
    public class RegionUnlockSequence : MonoBehaviour
    {
        private static readonly Color BannerBg    = new Color32(47, 122, 86, 245);
        private static readonly Color BannerText  = new Color32(248, 243, 223, 255);
        private static readonly Color FlashColor  = new Color(0.85f, 1f, 0.7f, 1f);

        // ─── Public API ───────────────────────────────────────────────────────────

        public void PlayUnlock(RectTransform nodeRect, string regionName, Action onComplete = null)
        {
            StartCoroutine(DoUnlockSequence(nodeRect, regionName, onComplete));
        }

        // ─── Sequence ─────────────────────────────────────────────────────────────

        private IEnumerator DoUnlockSequence(RectTransform nodeRect, string regionName, Action onComplete)
        {
            // ── Step 1: Flash the node ────────────────────────────────────────────
            var nodeImg = nodeRect?.GetComponent<Image>();
            if (nodeImg != null)
            {
                var originalColor = nodeImg.color;
                yield return PulseColor(nodeImg, originalColor, FlashColor, 0.15f);
                yield return PulseColor(nodeImg, FlashColor, originalColor, 0.25f);
            }

            // ── Step 2: Scale pop ─────────────────────────────────────────────────
            if (nodeRect != null)
                yield return ScalePop(nodeRect, 1.0f, 1.3f, 0.2f);

            // ── Step 3: Particle burst ────────────────────────────────────────────
            if (nodeRect != null)
                SpawnUnlockParticles(nodeRect);

            // ── Step 4: Banner ────────────────────────────────────────────────────
            var canvas = FindRootCanvas(nodeRect);
            if (canvas != null)
            {
                var banner = CreateBanner(canvas, $"{regionName} Unlocked!");
                yield return SlideInBanner(banner);
                yield return new WaitForSeconds(2.2f);
                yield return FadeOutBanner(banner);
                Destroy(banner.gameObject);
            }

            onComplete?.Invoke();
        }

        // ─── Banner ───────────────────────────────────────────────────────────────

        private RectTransform CreateBanner(Canvas canvas, string message)
        {
            var go = new GameObject("UnlockBanner");
            go.transform.SetParent(canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.85f);
            rt.anchorMax = new Vector2(0.9f, 0.95f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = new Vector2(0f, 60f); // starts above screen

            var bg = go.AddComponent<Image>();
            bg.color = BannerBg;

            var textGo = new GameObject("BannerText");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var label = textGo.AddComponent<Text>();
            label.text      = message;
            label.font      = ForestUiFactory.GetDefaultFont();
            label.fontSize  = 28;
            label.color     = BannerText;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;

            return rt;
        }

        private IEnumerator SlideInBanner(RectTransform banner)
        {
            var startPos = new Vector2(0f, 80f);
            var endPos   = Vector2.zero;
            var elapsed  = 0f;
            var duration = 0.35f;

            var group = banner.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
                banner.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                group.alpha = t;
                yield return null;
            }

            banner.anchoredPosition = endPos;
            group.alpha = 1f;
        }

        private IEnumerator FadeOutBanner(RectTransform banner)
        {
            var group = banner.GetComponent<CanvasGroup>();
            if (group == null) yield break;

            var elapsed  = 0f;
            var duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
        }

        // ─── Particles ────────────────────────────────────────────────────────────

        private void SpawnUnlockParticles(RectTransform nodeRect)
        {
            var canvas = FindRootCanvas(nodeRect);
            if (canvas == null) return;

            for (var i = 0; i < 12; i++)
            {
                var particleGo = new GameObject($"UnlockParticle_{i}");
                particleGo.transform.SetParent(canvas.transform, false);
                var pRt   = particleGo.AddComponent<RectTransform>();
                pRt.sizeDelta = new Vector2(8f, 8f);

                Vector3 worldPos;
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    nodeRect, Vector2.zero, null, out worldPos);
                pRt.position = nodeRect.position;

                var pImg  = particleGo.AddComponent<Image>();
                pImg.color = new Color(0.7f, 1f, 0.5f, 0.9f);
                pImg.raycastTarget = false;

                var angle    = i * (360f / 12f) * Mathf.Deg2Rad;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 120f;

                StartCoroutine(AnimateParticle(pRt, pImg, velocity));
            }
        }

        private IEnumerator AnimateParticle(RectTransform rt, Image img, Vector2 velocity)
        {
            var elapsed  = 0f;
            var lifetime = 0.8f;
            var startPos = rt.anchoredPosition;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / lifetime;

                velocity.y -= 180f * Time.deltaTime;
                rt.anchoredPosition += velocity * Time.deltaTime;

                img.color = new Color(img.color.r, img.color.g, img.color.b,
                    Mathf.Lerp(0.9f, 0f, t));
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, t);

                yield return null;
            }

            Destroy(rt.gameObject);
        }

        // ─── Animation Helpers ────────────────────────────────────────────────────

        private IEnumerator PulseColor(Image img, Color from, Color to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                img.color = Color.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            img.color = to;
        }

        private IEnumerator ScalePop(RectTransform rt, float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var ease = 1f - Mathf.Pow(1f - t, 2f);
                rt.localScale = Vector3.one * Mathf.Lerp(from, to, ease);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private static Canvas FindRootCanvas(Transform t)
        {
            while (t != null)
            {
                var c = t.GetComponent<Canvas>();
                if (c != null && c.isRootCanvas) return c;
                t = t.parent;
            }
            return null;
        }
    }
}
