using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Plays the Forest Friends Quest opening cinematic.
    ///
    /// No video assets required — the intro is 100% procedural:
    ///   Scene 1: Deep darkness. Firefly appears, drifts across screen.
    ///   Scene 2: Title "Forest Friends Quest" fades in letter-by-letter.
    ///   Scene 3: Forest silhouette rises from the bottom (parallax layers).
    ///   Scene 4: Stars appear one by one across the canopy.
    ///   Scene 5: A small creature (Pip) peeks out from behind a root.
    ///   Scene 6: Pip looks at camera. Blinks. Tilts head.
    ///   Scene 7: "Your adventure begins..." fades in.
    ///
    /// Total runtime: ~8 seconds. Can be skipped with tap after scene 2.
    ///
    /// Visual approach: all Images on Canvas, animated purely with coroutines.
    /// </summary>
    public class IntroCinematicController : MonoBehaviour
    {
        private bool _skipRequested;

        private static readonly Color DeepForest  = new Color(0.04f, 0.10f, 0.07f, 1f);
        private static readonly Color StarColor    = new Color(0.95f, 1f, 0.88f, 0.85f);
        private static readonly Color TitleColor   = new Color32(159, 216, 168, 255);
        private static readonly Color SubtitleColor = new Color(0.7f, 0.85f, 0.65f, 0.85f);

        // ─── Public API ───────────────────────────────────────────────────────────

        public IEnumerator PlayIntro(RectTransform container)
        {
            _skipRequested = false;
            var tap = container.gameObject.AddComponent<SkipTapListener>();
            tap.OnTap = () => _skipRequested = true;

            // Scene 1: darkness + firefly
            yield return Scene1_Darkness(container);
            if (_skipRequested) { yield return SkipToEnd(container); yield break; }

            // Scene 2: title
            yield return Scene2_Title(container);
            if (_skipRequested) { yield return SkipToEnd(container); yield break; }

            // Scene 3: forest silhouette
            yield return Scene3_ForestRise(container);

            // Scene 4: stars
            yield return Scene4_Stars(container);

            // Scene 5-6: Pip peeks out
            yield return Scene5_PipPeek(container);

            // Scene 7: subtitle
            yield return Scene7_Subtitle(container);

            yield return new WaitForSeconds(1.2f);

            // Clean up
            Destroy(tap);
        }

        // ─── Scenes ───────────────────────────────────────────────────────────────

        private IEnumerator Scene1_Darkness(RectTransform parent)
        {
            var firefly = CreateDot(parent, new Color(0.7f, 1f, 0.55f, 0f), 12f);
            var rt      = firefly.GetComponent<RectTransform>();

            // Drift from left edge to center
            rt.anchoredPosition = new Vector2(parent.rect.xMin + 60f,
                parent.rect.yMin + parent.rect.height * 0.4f);

            yield return FadeImage(firefly, 0f, 0.9f, 0.8f);

            var elapsed = 0f;
            while (elapsed < 2.5f && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / 2.5f;
                rt.anchoredPosition = new Vector2(
                    Mathf.Lerp(parent.rect.xMin + 60f, 0f, t),
                    rt.anchoredPosition.y + Mathf.Sin(elapsed * 2.5f) * 1.5f);

                // Pulse
                firefly.color = new Color(0.7f, 1f, 0.55f,
                    0.5f + Mathf.Sin(elapsed * 4f) * 0.4f);
                yield return null;
            }

            yield return FadeImage(firefly, firefly.color.a, 0f, 0.6f);
            Destroy(firefly.gameObject);
        }

        private IEnumerator Scene2_Title(RectTransform parent)
        {
            const string title = "Forest Friends Quest";
            var label = CreateText(parent, "", 52, TitleColor, TextAnchor.MiddleCenter,
                FontStyle.Bold, new Vector2(0f, 60f));

            // Letter-by-letter reveal
            for (var i = 1; i <= title.Length; i++)
            {
                label.text = title.Substring(0, i);
                yield return new WaitForSeconds(0.04f);
                if (_skipRequested) { label.text = title; break; }
            }

            yield return new WaitForSeconds(0.8f);
        }

        private IEnumerator Scene3_ForestRise(RectTransform parent)
        {
            // Three silhouette layers: dark trees rising from bottom
            var layers = new Image[3];
            for (var i = 0; i < 3; i++)
            {
                var depth  = 0.08f + i * 0.06f;
                var height = parent.rect.height * (0.3f + i * 0.08f);
                var img    = CreateRect(parent,
                    new Color(depth, depth * 1.5f, depth, 1f),
                    new Vector2(0f, -parent.rect.height * 0.5f),
                    new Vector2(parent.rect.width, height));
                layers[i] = img;
            }

            var elapsed = 0f;
            while (elapsed < 1.8f && !_skipRequested)
            {
                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / 1.8f), 3f);
                for (var i = 0; i < layers.Length; i++)
                {
                    var offset = i * 0.12f;
                    var tLayer = Mathf.Clamp01((elapsed / 1.8f - offset) / (1f - offset));
                    var rt     = layers[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0f,
                        Mathf.Lerp(-parent.rect.height * 0.5f,
                            -parent.rect.height * 0.15f + i * 20f,
                            1f - Mathf.Pow(1f - tLayer, 3f)));
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);
        }

        private IEnumerator Scene4_Stars(RectTransform parent)
        {
            for (var i = 0; i < 24; i++)
            {
                var star = CreateDot(parent, new Color(StarColor.r, StarColor.g, StarColor.b, 0f), 4f);
                var rt   = star.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(
                    UnityEngine.Random.Range(parent.rect.xMin * 0.9f, parent.rect.xMax * 0.9f),
                    UnityEngine.Random.Range(parent.rect.height * 0.1f, parent.rect.height * 0.45f));
                StartCoroutine(FadeImage(star, 0f, UnityEngine.Random.Range(0.5f, 0.9f),
                    UnityEngine.Random.Range(0.2f, 0.8f)));
                yield return new WaitForSeconds(0.06f);
                if (_skipRequested) break;
            }
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator Scene5_PipPeek(RectTransform parent)
        {
            // Pip = small green oval peeking from bottom-right
            var pip = CreateDot(parent, new Color(0.35f, 0.8f, 0.45f, 0f), 40f);
            var rt  = pip.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(
                parent.rect.width * 0.3f,
                parent.rect.yMin + 20f);

            yield return FadeImage(pip, 0f, 0.9f, 0.4f);
            yield return new WaitForSeconds(0.3f);

            // Tilt (curiosity)
            yield return TweenRotation(rt, 0f, 15f, 0.3f);
            yield return new WaitForSeconds(0.4f);
            yield return TweenRotation(rt, 15f, 0f, 0.25f);
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator Scene7_Subtitle(RectTransform parent)
        {
            var label = CreateText(parent, "Your adventure begins...", 26,
                SubtitleColor, TextAnchor.MiddleCenter, FontStyle.Italic,
                new Vector2(0f, -80f));
            label.color = new Color(SubtitleColor.r, SubtitleColor.g, SubtitleColor.b, 0f);
            yield return FadeText(label, 0f, 1f, 1.0f);
            yield return new WaitForSeconds(1.5f);
        }

        private IEnumerator SkipToEnd(RectTransform parent)
        {
            yield return new WaitForSeconds(0.2f);
        }

        // ─── Factory Helpers ──────────────────────────────────────────────────────

        private static Image CreateDot(RectTransform parent, Color color, float size)
        {
            var go  = new GameObject("CinDot");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Image CreateRect(RectTransform parent, Color color,
            Vector2 pos, Vector2 size)
        {
            var go  = new GameObject("CinRect");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private Text CreateText(RectTransform parent, string content, int size,
            Color color, TextAnchor anchor, FontStyle style, Vector2 offset)
        {
            var go  = new GameObject("CinText");
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.5f);
            rt.anchorMax = new Vector2(0.95f, 0.5f);
            rt.sizeDelta = new Vector2(0f, size * 2f);
            rt.anchoredPosition = offset;
            var txt = go.AddComponent<Text>();
            txt.font      = ForestUiFactory.GetDefaultFont();
            txt.fontSize  = size;
            txt.color     = color;
            txt.alignment = anchor;
            txt.fontStyle = style;
            txt.text      = content;
            txt.raycastTarget = false;
            return txt;
        }

        private static IEnumerator FadeImage(Image img, float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }
        }

        private static IEnumerator FadeText(Text txt, float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var c = txt.color;
                txt.color = new Color(c.r, c.g, c.b, Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }
        }

        private static IEnumerator TweenRotation(RectTransform rt, float from, float to, float d)
        {
            var elapsed = 0f;
            while (elapsed < d)
            {
                elapsed += Time.deltaTime;
                rt.localEulerAngles = new Vector3(0f, 0f,
                    Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / d)));
                yield return null;
            }
        }

        // ─── Skip Component ───────────────────────────────────────────────────────

        private class SkipTapListener : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
        {
            public System.Action OnTap;
            public void OnPointerClick(UnityEngine.EventSystems.PointerEventData _) => OnTap?.Invoke();
        }
    }
}
