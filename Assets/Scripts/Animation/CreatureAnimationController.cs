using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Drives per-creature visual animations in response to emotion state changes.
    ///
    /// Each creature is represented by an Image (sprite) inside a RectTransform.
    /// Animations are pure coroutines — no Animator component required.
    ///
    /// Supported animations:
    ///   Idle    → gentle float bob
    ///   Happy   → bounce + color flash
    ///   Excited → rapid scale pulse
    ///   Curious → tilt and head-peek
    ///   Sleepy  → slow droop + alpha fade
    ///   Shy     → shrink + hide
    ///   Playful → spin + color cycle
    ///   Proud   → stand tall + scale up
    ///   Sad     → droop + desaturate
    ///   Hungry  → shake + red tint
    /// </summary>
    public class CreatureAnimationController : MonoBehaviour
    {
        private CreatureMoodBrain _moodBrain;

        private readonly Dictionary<string, CreatureVisual> _visuals =
            new Dictionary<string, CreatureVisual>();

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize(CreatureMoodBrain moodBrain)
        {
            _moodBrain = moodBrain;
        }

        /// <summary>Register a creature's UI Image. Must be called before any animations.</summary>
        public void RegisterCreature(string creatureId, RectTransform rt, Image img)
        {
            var visual = new CreatureVisual
            {
                CreatureId = creatureId,
                Rect       = rt,
                Image      = img,
                BaseScale  = rt.localScale,
                BaseColor  = img.color
            };

            _visuals[creatureId] = visual;

            // Subscribe to FSM
            var fsm = _moodBrain?.GetFSM(creatureId);
            if (fsm != null)
                fsm.OnEmotionChanged += (prev, next) => OnEmotionChanged(creatureId, prev, next);

            // Start idle animation immediately
            StartCoroutine(IdleBob(visual));
        }

        // ─── Emotion Routing ──────────────────────────────────────────────────────

        private void OnEmotionChanged(string creatureId, CreatureEmotion prev, CreatureEmotion next)
        {
            if (!_visuals.TryGetValue(creatureId, out var visual)) return;

            // Stop current animation, start new one
            StopAllCoroutinesForCreature(visual);

            StartCoroutine(next switch
            {
                CreatureEmotion.Happy   => BounceHappy(visual),
                CreatureEmotion.Excited => PulseExcited(visual),
                CreatureEmotion.Curious => TiltCurious(visual),
                CreatureEmotion.Sleepy  => DroopSleepy(visual),
                CreatureEmotion.Shy     => ShrinkShy(visual),
                CreatureEmotion.Playful => SpinPlayful(visual),
                CreatureEmotion.Proud   => StandProud(visual),
                CreatureEmotion.Sad     => DroopSad(visual),
                CreatureEmotion.Hungry  => ShakeHungry(visual),
                _                      => IdleBob(visual)
            });
        }

        // ─── Animation Coroutines ─────────────────────────────────────────────────

        private IEnumerator IdleBob(CreatureVisual v)
        {
            var baseY  = v.Rect.anchoredPosition.y;
            var phase  = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            v.CurrentCoroutineId = "idle";

            while (v.CurrentCoroutineId == "idle")
            {
                var t  = Mathf.Sin(Time.time * 0.9f + phase) * 4f;
                var pos = v.Rect.anchoredPosition;
                v.Rect.anchoredPosition = new Vector2(pos.x, baseY + t);
                yield return null;
            }

            v.Rect.anchoredPosition = new Vector2(v.Rect.anchoredPosition.x, baseY);
        }

        private IEnumerator BounceHappy(CreatureVisual v)
        {
            v.CurrentCoroutineId = "happy";
            const float duration = 0.8f;
            var elapsed = 0f;

            while (elapsed < duration && v.CurrentCoroutineId == "happy")
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                var bounce = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * (1f - t);
                v.Rect.localScale = v.BaseScale * (1f + bounce * 0.25f);

                var flash = Mathf.Lerp(0f, 1f, Mathf.Sin(t * Mathf.PI));
                v.Image.color = Color.Lerp(v.BaseColor,
                    new Color(1f, 0.95f, 0.6f, v.BaseColor.a), flash * 0.4f);

                yield return null;
            }

            ResetVisual(v);
            StartCoroutine(IdleBob(v));
        }

        private IEnumerator PulseExcited(CreatureVisual v)
        {
            v.CurrentCoroutineId = "excited";
            const float duration = 1.2f;
            var elapsed = 0f;

            while (elapsed < duration && v.CurrentCoroutineId == "excited")
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                var pulse = 1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.18f * (1f - t);
                v.Rect.localScale = v.BaseScale * pulse;
                yield return null;
            }

            ResetVisual(v);
            StartCoroutine(IdleBob(v));
        }

        private IEnumerator TiltCurious(CreatureVisual v)
        {
            v.CurrentCoroutineId = "curious";

            yield return TweenRotation(v, 0f, 15f, 0.3f);
            yield return new WaitForSeconds(1.2f);
            yield return TweenRotation(v, 15f, -10f, 0.4f);
            yield return new WaitForSeconds(0.8f);
            yield return TweenRotation(v, -10f, 0f, 0.3f);

            v.CurrentCoroutineId = "idle";
            StartCoroutine(IdleBob(v));
        }

        private IEnumerator DroopSleepy(CreatureVisual v)
        {
            v.CurrentCoroutineId = "sleepy";
            var elapsed = 0f;
            const float duration = 1.5f;

            while (elapsed < duration && v.CurrentCoroutineId == "sleepy")
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                v.Rect.localScale = Vector3.Lerp(v.BaseScale, v.BaseScale * 0.88f, t);
                v.Image.color     = Color.Lerp(v.BaseColor,
                    new Color(0.6f, 0.65f, 0.8f, v.BaseColor.a * 0.75f), t);
                yield return null;
            }

            // Gentle sleeping bob
            while (v.CurrentCoroutineId == "sleepy")
            {
                v.Rect.localScale = v.BaseScale * 0.88f *
                    (1f + Mathf.Sin(Time.time * 0.4f) * 0.02f);
                yield return null;
            }

            ResetVisual(v);
        }

        private IEnumerator ShrinkShy(CreatureVisual v)
        {
            v.CurrentCoroutineId = "shy";
            yield return TweenScale(v, 1f, 0.7f, 0.3f);
            yield return new WaitForSeconds(1.5f);
            yield return TweenScale(v, 0.7f, 1f, 0.4f);

            v.CurrentCoroutineId = "idle";
            StartCoroutine(IdleBob(v));
        }

        private IEnumerator SpinPlayful(CreatureVisual v)
        {
            v.CurrentCoroutineId = "playful";
            const float duration = 1.4f;
            var elapsed = 0f;

            while (elapsed < duration && v.CurrentCoroutineId == "playful")
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                v.Rect.localEulerAngles = new Vector3(0f, 0f, 360f * t);
                var hue = (t * 0.4f) % 1f;
                v.Image.color = Color.HSVToRGB(hue, 0.4f, 1f);
                yield return null;
            }

            v.Rect.localEulerAngles = Vector3.zero;
            v.Image.color           = v.BaseColor;
            ResetVisual(v);
            StartCoroutine(IdleBob(v));
        }

        private IEnumerator StandProud(CreatureVisual v)
        {
            v.CurrentCoroutineId = "proud";
            yield return TweenScale(v, 1f, 1.2f, 0.25f);

            var elapsed = 0f;
            while (elapsed < 2f && v.CurrentCoroutineId == "proud")
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Sin(Time.time * 1.5f) * 0.03f;
                v.Rect.localScale = v.BaseScale * (1.2f + t);
                yield return null;
            }

            yield return TweenScale(v, 1.2f, 1f, 0.3f);
            v.CurrentCoroutineId = "idle";
            StartCoroutine(IdleBob(v));
        }

        private IEnumerator DroopSad(CreatureVisual v)
        {
            v.CurrentCoroutineId = "sad";
            var elapsed = 0f;
            const float duration = 1f;

            while (elapsed < duration && v.CurrentCoroutineId == "sad")
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                v.Rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(0f, -8f, t));
                v.Image.color = Color.Lerp(v.BaseColor,
                    new Color(0.55f, 0.55f, 0.65f, v.BaseColor.a), t);
                yield return null;
            }

            yield return new WaitForSeconds(3f);
            yield return TweenRotation(v, -8f, 0f, 0.5f);
            v.Image.color = v.BaseColor;
            v.CurrentCoroutineId = "idle";
            StartCoroutine(IdleBob(v));
        }

        private IEnumerator ShakeHungry(CreatureVisual v)
        {
            v.CurrentCoroutineId = "hungry";
            var baseX = v.Rect.anchoredPosition.x;
            var elapsed = 0f;
            const float duration = 0.5f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var shake = Mathf.Sin(elapsed * 40f) * 5f * (1f - elapsed / duration);
                var pos   = v.Rect.anchoredPosition;
                v.Rect.anchoredPosition = new Vector2(baseX + shake, pos.y);
                v.Image.color = Color.Lerp(v.BaseColor, new Color(1f, 0.5f, 0.4f, 1f), 0.35f);
                yield return null;
            }

            v.Rect.anchoredPosition = new Vector2(baseX, v.Rect.anchoredPosition.y);

            // Stay red-tinted while hungry
            while (v.CurrentCoroutineId == "hungry")
            {
                v.Image.color = Color.Lerp(v.BaseColor, new Color(1f, 0.5f, 0.4f, 1f),
                    (Mathf.Sin(Time.time * 1.5f) + 1f) * 0.15f);
                yield return null;
            }

            v.Image.color = v.BaseColor;
        }

        // ─── Tween Helpers ────────────────────────────────────────────────────────

        private IEnumerator TweenScale(CreatureVisual v, float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 2f);
                v.Rect.localScale = v.BaseScale * Mathf.Lerp(from, to, t);
                yield return null;
            }
            v.Rect.localScale = v.BaseScale * to;
        }

        private IEnumerator TweenRotation(CreatureVisual v, float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 2f);
                v.Rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(from, to, t));
                yield return null;
            }
            v.Rect.localEulerAngles = new Vector3(0f, 0f, to);
        }

        private static void ResetVisual(CreatureVisual v)
        {
            v.Rect.localScale        = v.BaseScale;
            v.Rect.localEulerAngles  = Vector3.zero;
            v.Image.color            = v.BaseColor;
        }

        private static void StopAllCoroutinesForCreature(CreatureVisual v)
        {
            v.CurrentCoroutineId = "stopping";
        }

        // ─── Data ─────────────────────────────────────────────────────────────────

        private class CreatureVisual
        {
            public string        CreatureId;
            public RectTransform Rect;
            public Image         Image;
            public Vector3       BaseScale;
            public Color         BaseColor;
            public string        CurrentCoroutineId = "idle";
        }
    }
}
