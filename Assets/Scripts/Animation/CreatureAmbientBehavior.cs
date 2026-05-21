using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Manages ambient creature behaviors in the Sanctuary view:
    ///   - Random wander within a defined rect
    ///   - Creature-to-creature proximity reactions
    ///   - Environment interaction (sniff grass patch, look at butterfly, etc.)
    ///   - Rest poses near their favorite spots
    ///
    /// Works alongside CreatureAnimationController. Does not need to know about
    /// emotion state — that's the animator's job.
    /// </summary>
    public class CreatureAmbientBehavior : MonoBehaviour
    {
        private RectTransform _bounds;

        private readonly Dictionary<string, AmbientAgent> _agents =
            new Dictionary<string, AmbientAgent>();

        // ─── Setup ────────────────────────────────────────────────────────────────

        public void Initialize(RectTransform sanctuaryBounds)
        {
            _bounds = sanctuaryBounds;
        }

        public void RegisterCreature(string creatureId, RectTransform rt)
        {
            var agent = new AmbientAgent
            {
                CreatureId = creatureId,
                Rect       = rt,
                HomePos    = rt.anchoredPosition,
                Speed      = Random.Range(18f, 32f),
                WanderRadius = Random.Range(40f, 90f)
            };
            _agents[creatureId] = agent;
            StartCoroutine(AmbientLoop(agent));
        }

        // ─── Ambient Loop ─────────────────────────────────────────────────────────

        private IEnumerator AmbientLoop(AmbientAgent agent)
        {
            while (true)
            {
                // Pick a random behavior
                var behavior = PickBehavior(agent);
                yield return behavior;

                // Rest pause between behaviors (1-4 seconds)
                yield return new WaitForSeconds(Random.Range(1f, 4f));
            }
        }

        private IEnumerator PickBehavior(AmbientAgent agent)
        {
            var roll = Random.value;

            if (roll < 0.45f) return WanderToPoint(agent);
            if (roll < 0.70f) return ReturnHome(agent);
            if (roll < 0.85f) return SniffGround(agent);
            return LookAround(agent);
        }

        private IEnumerator WanderToPoint(AmbientAgent agent)
        {
            var target = agent.HomePos + new Vector2(
                Random.Range(-agent.WanderRadius, agent.WanderRadius),
                Random.Range(-agent.WanderRadius * 0.5f, agent.WanderRadius * 0.5f));

            target = ClampToBounds(target);
            yield return MoveToPos(agent, target, agent.Speed);

            // Flip sprite to face direction of travel (scale X)
            var dir = (target - agent.HomePos).x;
            if (Mathf.Abs(dir) > 2f)
            {
                var s = agent.Rect.localScale;
                agent.Rect.localScale = new Vector3(
                    dir > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x), s.y, s.z);
            }
        }

        private IEnumerator ReturnHome(AmbientAgent agent)
        {
            yield return MoveToPos(agent, agent.HomePos, agent.Speed * 0.7f);
        }

        private IEnumerator SniffGround(AmbientAgent agent)
        {
            // Dip head (rotate -15°), pause, return
            yield return TweenRotation(agent.Rect, 0f, -15f, 0.3f);
            yield return new WaitForSeconds(Random.Range(0.5f, 1.2f));
            yield return TweenRotation(agent.Rect, -15f, 0f, 0.3f);
        }

        private IEnumerator LookAround(AmbientAgent agent)
        {
            // Head tilt left/right
            yield return TweenRotation(agent.Rect, 0f, Random.Range(-20f, 20f), 0.25f);
            yield return new WaitForSeconds(Random.Range(0.4f, 1f));
            yield return TweenRotation(agent.Rect, agent.Rect.localEulerAngles.z, 0f, 0.3f);
        }

        // ─── Proximity Reactions ──────────────────────────────────────────────────

        private void Update()
        {
            // Check if any two creatures are close — trigger shy/curious reaction
            var ids = new List<string>(_agents.Keys);
            for (var i = 0; i < ids.Count; i++)
            {
                for (var j = i + 1; j < ids.Count; j++)
                {
                    var a = _agents[ids[i]];
                    var b = _agents[ids[j]];
                    if (a.Rect == null || b.Rect == null) continue;

                    var dist = Vector2.Distance(
                        a.Rect.anchoredPosition, b.Rect.anchoredPosition);

                    if (dist < 40f && !a.InProximityReaction && !b.InProximityReaction)
                        StartCoroutine(ProximityReact(a, b));
                }
            }
        }

        private IEnumerator ProximityReact(AmbientAgent a, AmbientAgent b)
        {
            a.InProximityReaction = true;
            b.InProximityReaction = true;

            // Both look at each other
            yield return TweenRotation(a.Rect, 0f, 8f, 0.2f);
            yield return new WaitForSeconds(0.8f);
            yield return TweenRotation(a.Rect, 8f, 0f, 0.2f);

            yield return new WaitForSeconds(0.3f);
            a.InProximityReaction = false;
            b.InProximityReaction = false;
        }

        // ─── Movement Helpers ─────────────────────────────────────────────────────

        private static IEnumerator MoveToPos(AmbientAgent agent, Vector2 target, float speed)
        {
            while (Vector2.Distance(agent.Rect.anchoredPosition, target) > 2f)
            {
                agent.Rect.anchoredPosition = Vector2.MoveTowards(
                    agent.Rect.anchoredPosition, target, speed * Time.deltaTime);
                yield return null;
            }
            agent.Rect.anchoredPosition = target;
        }

        private static IEnumerator TweenRotation(RectTransform rt, float from, float to, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(from, to, t));
                yield return null;
            }
            rt.localEulerAngles = new Vector3(0f, 0f, to);
        }

        private Vector2 ClampToBounds(Vector2 pos)
        {
            if (_bounds == null) return pos;
            var r = _bounds.rect;
            return new Vector2(
                Mathf.Clamp(pos.x, r.xMin + 40f, r.xMax - 40f),
                Mathf.Clamp(pos.y, r.yMin + 40f, r.yMax - 40f));
        }

        // ─── Data ─────────────────────────────────────────────────────────────────

        private class AmbientAgent
        {
            public string        CreatureId;
            public RectTransform Rect;
            public Vector2       HomePos;
            public float         Speed;
            public float         WanderRadius;
            public bool          InProximityReaction;
        }
    }
}
