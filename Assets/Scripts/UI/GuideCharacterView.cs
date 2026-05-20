using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    public class GuideCharacterView : MonoBehaviour
    {
        private static readonly Color DefaultAccent = new Color(1f, 0.7f, 0.42f);

        public void Build(CharacterProfile profile, Font font)
        {
            ForestUiFactory.ClearChildren(transform);

            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(220f, 220f);
            }

            var accent = ForestUiFactory.FromHex(profile?.accentHex, DefaultAccent);
            var light = Color.Lerp(accent, Color.white, 0.35f);
            var dark = Color.Lerp(accent, new Color(0.16f, 0.14f, 0.11f), 0.45f);

            var glow = CreatePart("Glow", new Vector2(0f, 6f), new Vector2(210f, 210f), new Color(accent.r, accent.g, accent.b, 0.16f));
            glow.gameObject.AddComponent<PulseGlow>();

            switch (profile?.id)
            {
                case "mimi":
                    BuildBird(accent, light, dark);
                    break;
                case "tomo":
                    BuildTurtle(accent, light, dark);
                    break;
                case "luma":
                    BuildFirefly(accent, light, dark);
                    break;
                default:
                    BuildFox(accent, light, dark);
                    break;
            }

            var label = ForestUiFactory.CreateText(transform, "Name", profile?.name ?? "Guide", font, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            label.rectTransform.sizeDelta = new Vector2(180f, 32f);

            var bob = gameObject.GetComponent<FloatBob>() ?? gameObject.AddComponent<FloatBob>();
            bob.amplitude = 7f;
            bob.speed = 1.7f;
        }

        private void BuildFox(Color accent, Color light, Color dark)
        {
            var tail = CreatePart("Tail", new Vector2(-60f, 10f), new Vector2(80f, 80f), accent);
            tail.localRotation = Quaternion.Euler(0f, 0f, -24f);
            tail.gameObject.AddComponent<SwayMotion>().rotationAmount = 10f;

            var body = CreatePart("Body", new Vector2(0f, -6f), new Vector2(120f, 120f), accent);
            var chest = CreatePart("Chest", new Vector2(0f, -20f), new Vector2(64f, 64f), new Color(1f, 0.95f, 0.86f, 1f), body);
            chest.localScale = new Vector3(1f, 1.25f, 1f);

            var head = CreatePart("Head", new Vector2(0f, 48f), new Vector2(110f, 110f), accent);
            CreateEar(head, new Vector2(-34f, 44f), light, dark, -20f);
            CreateEar(head, new Vector2(34f, 44f), light, dark, 20f);
            CreateFace(head, dark, new Vector2(18f, 10f), true);
        }

        private void BuildBird(Color accent, Color light, Color dark)
        {
            var wingLeft = CreatePart("WingLeft", new Vector2(-52f, 18f), new Vector2(70f, 70f), light);
            wingLeft.gameObject.AddComponent<SwayMotion>().rotationAmount = 8f;
            var wingRight = CreatePart("WingRight", new Vector2(52f, 18f), new Vector2(70f, 70f), light);
            wingRight.gameObject.AddComponent<SwayMotion>().rotationAmount = -8f;

            var body = CreatePart("Body", new Vector2(0f, 10f), new Vector2(118f, 118f), accent);
            body.localScale = new Vector3(1f, 1.1f, 1f);
            var head = CreatePart("Head", new Vector2(0f, 66f), new Vector2(82f, 82f), accent);
            var beak = CreatePart("Beak", new Vector2(0f, -6f), new Vector2(20f, 20f), new Color(1f, 0.66f, 0.22f, 1f), head);
            beak.localScale = new Vector3(1.2f, 0.7f, 1f);
            CreateFace(head, dark, new Vector2(16f, 10f), false);
        }

        private void BuildTurtle(Color accent, Color light, Color dark)
        {
            var shell = CreatePart("Shell", new Vector2(0f, 10f), new Vector2(138f, 124f), accent);
            shell.localScale = new Vector3(1.05f, 0.92f, 1f);
            CreatePart("ShellMark", new Vector2(0f, 0f), new Vector2(86f, 86f), light, shell);

            CreatePart("Head", new Vector2(0f, 74f), new Vector2(68f, 68f), light);
            CreatePart("FootLeft", new Vector2(-44f, -38f), new Vector2(28f, 28f), light);
            CreatePart("FootRight", new Vector2(44f, -38f), new Vector2(28f, 28f), light);
            CreatePart("HandLeft", new Vector2(-62f, 22f), new Vector2(26f, 26f), light);
            CreatePart("HandRight", new Vector2(62f, 22f), new Vector2(26f, 26f), light);

            var faceAnchor = CreatePart("FaceAnchor", new Vector2(0f, 74f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f));
            CreateFace(faceAnchor, dark, new Vector2(14f, 6f), false);
        }

        private void BuildFirefly(Color accent, Color light, Color dark)
        {
            var wingLeft = CreatePart("WingLeft", new Vector2(-44f, 34f), new Vector2(72f, 72f), new Color(1f, 1f, 1f, 0.35f));
            wingLeft.gameObject.AddComponent<PulseGlow>().speed = 2.6f;
            var wingRight = CreatePart("WingRight", new Vector2(44f, 34f), new Vector2(72f, 72f), new Color(1f, 1f, 1f, 0.35f));
            wingRight.gameObject.AddComponent<PulseGlow>().speed = 2.3f;

            var body = CreatePart("Body", new Vector2(0f, 18f), new Vector2(90f, 110f), dark);
            body.localScale = new Vector3(0.72f, 1.1f, 1f);
            var glow = CreatePart("Lantern", new Vector2(0f, -18f), new Vector2(80f, 80f), light, body);
            var glowPulse = glow.gameObject.AddComponent<PulseGlow>();
            glowPulse.maxAlpha = 1f;
            glowPulse.minAlpha = 0.52f;
            CreatePart("Head", new Vector2(0f, 64f), new Vector2(64f, 64f), accent);
            var faceAnchor = CreatePart("FaceAnchor", new Vector2(0f, 64f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f));
            CreateFace(faceAnchor, dark, new Vector2(12f, 8f), false);
        }

        private void CreateEar(Transform parent, Vector2 position, Color outer, Color inner, float rotation)
        {
            var ear = CreatePart("Ear", position, new Vector2(34f, 34f), outer, parent);
            ear.localRotation = Quaternion.Euler(0f, 0f, rotation);
            ear.localScale = new Vector3(0.8f, 1.2f, 1f);
            CreatePart("InnerEar", Vector2.zero, new Vector2(18f, 18f), inner, ear);
        }

        private void CreateFace(Transform parent, Color dark, Vector2 eyeSpacing, bool withNose)
        {
            CreatePart("EyeLeft", new Vector2(-eyeSpacing.x, eyeSpacing.y), new Vector2(12f, 12f), dark, parent);
            CreatePart("EyeRight", new Vector2(eyeSpacing.x, eyeSpacing.y), new Vector2(12f, 12f), dark, parent);

            if (withNose)
            {
                CreatePart("Nose", new Vector2(0f, -8f), new Vector2(14f, 14f), dark, parent);
            }
        }

        private RectTransform CreatePart(string name, Vector2 position, Vector2 size, Color color, Transform parentOverride = null)
        {
            var image = ForestUiFactory.CreateImage(parentOverride != null ? parentOverride : transform, name, color, true);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
