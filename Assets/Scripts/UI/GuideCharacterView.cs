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
                case "pip":
                    BuildFox(accent, light, dark);
                    break;
                case "mimi":
                    BuildBird(accent, light, dark);
                    break;
                case "tomo":
                    BuildTurtle(accent, light, dark);
                    break;
                case "luma":
                    BuildFirefly(accent, light, dark);
                    break;
                case "nori":
                    BuildDeer(accent, light, dark);
                    break;
                case "sol":
                    BuildOwl(accent, light, dark);
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

        // Nori — Deer Guardian: elegant body, long legs, antlers, gentle eyes
        private void BuildDeer(Color accent, Color light, Color dark)
        {
            // Hindquarters
            var haunch = CreatePart("Haunch", new Vector2(0f, -30f), new Vector2(110f, 80f), accent);
            haunch.localScale = new Vector3(1f, 0.85f, 1f);

            // Belly patch
            CreatePart("Belly", new Vector2(0f, 0f), new Vector2(52f, 52f), light, haunch);

            // Legs
            var legFL = CreatePart("LegFL", new Vector2(-22f, -66f), new Vector2(18f, 52f), accent);
            legFL.localScale = new Vector3(1f, 1.05f, 1f);
            var legFR = CreatePart("LegFR", new Vector2(22f, -66f), new Vector2(18f, 52f), accent);
            legFR.localScale = new Vector3(1f, 1.05f, 1f);
            CreatePart("HoofFL", new Vector2(0f, -26f), new Vector2(14f, 12f), dark, legFL);
            CreatePart("HoofFR", new Vector2(0f, -26f), new Vector2(14f, 12f), dark, legFR);

            // Neck & head
            var neck = CreatePart("Neck", new Vector2(0f, 24f), new Vector2(38f, 46f), accent);
            neck.localScale = new Vector3(0.8f, 1f, 1f);

            var head = CreatePart("Head", new Vector2(0f, 68f), new Vector2(88f, 80f), accent);
            head.localScale = new Vector3(1f, 0.95f, 1f);
            var muzzle = CreatePart("Muzzle", new Vector2(0f, -18f), new Vector2(44f, 36f), light, head);
            muzzle.localScale = new Vector3(1f, 0.75f, 1f);
            CreatePart("Nose", new Vector2(0f, 6f), new Vector2(12f, 10f), dark, muzzle);

            // Ears
            var earL = CreatePart("EarL", new Vector2(-42f, 28f), new Vector2(22f, 38f), accent, head);
            earL.localRotation = Quaternion.Euler(0f, 0f, 20f);
            CreatePart("InnerEarL", Vector2.zero, new Vector2(12f, 22f), light, earL);

            var earR = CreatePart("EarR", new Vector2(42f, 28f), new Vector2(22f, 38f), accent, head);
            earR.localRotation = Quaternion.Euler(0f, 0f, -20f);
            CreatePart("InnerEarR", Vector2.zero, new Vector2(12f, 22f), light, earR);

            // Eyes
            CreatePart("EyeL", new Vector2(-20f, 12f), new Vector2(12f, 14f), dark, head);
            CreatePart("EyeR", new Vector2(20f, 12f), new Vector2(12f, 14f), dark, head);

            // Antlers (two branching prongs each side)
            var antlerL = CreatePart("AntlerLBase", new Vector2(-32f, 52f), new Vector2(8f, 28f), dark, head);
            antlerL.localRotation = Quaternion.Euler(0f, 0f, -18f);
            CreatePart("AntlerLBranch", new Vector2(0f, 12f), new Vector2(6f, 20f), dark, antlerL).localRotation = Quaternion.Euler(0f, 0f, -30f);

            var antlerR = CreatePart("AntlerRBase", new Vector2(32f, 52f), new Vector2(8f, 28f), dark, head);
            antlerR.localRotation = Quaternion.Euler(0f, 0f, 18f);
            CreatePart("AntlerRBranch", new Vector2(0f, 12f), new Vector2(6f, 20f), dark, antlerR).localRotation = Quaternion.Euler(0f, 0f, 30f);

            // Spots on back
            CreatePart("Spot1", new Vector2(-18f, 8f), new Vector2(10f, 10f), light, haunch);
            CreatePart("Spot2", new Vector2(10f, -4f), new Vector2(8f, 8f), light, haunch);
            CreatePart("Spot3", new Vector2(-4f, -12f), new Vector2(6f, 6f), light, haunch);

            // Sway for graceful movement
            haunch.gameObject.AddComponent<SwayMotion>().rotationAmount = 3f;
        }

        // Sol — Arch Druid Owl: large eyes, beak, talons, wing feathers, rune-glow
        private void BuildOwl(Color accent, Color light, Color dark)
        {
            // Main body
            var body = CreatePart("Body", new Vector2(0f, 0f), new Vector2(120f, 130f), accent);
            body.localScale = new Vector3(0.88f, 1.0f, 1f);

            // Wing feathers (layered)
            var wingL = CreatePart("WingL", new Vector2(-60f, 0f), new Vector2(80f, 100f), accent);
            wingL.localScale = new Vector3(0.7f, 0.9f, 1f);
            wingL.localRotation = Quaternion.Euler(0f, 0f, 12f);
            CreatePart("FeatherL1", new Vector2(0f, -18f), new Vector2(50f, 22f), light, wingL);
            CreatePart("FeatherL2", new Vector2(0f, -34f), new Vector2(44f, 18f), dark, wingL);
            CreatePart("FeatherL3", new Vector2(0f, -50f), new Vector2(36f, 16f), light, wingL);
            wingL.gameObject.AddComponent<SwayMotion>().rotationAmount = 5f;

            var wingR = CreatePart("WingR", new Vector2(60f, 0f), new Vector2(80f, 100f), accent);
            wingR.localScale = new Vector3(-0.7f, 0.9f, 1f);
            wingR.localRotation = Quaternion.Euler(0f, 0f, -12f);
            CreatePart("FeatherR1", new Vector2(0f, -18f), new Vector2(50f, 22f), light, wingR);
            CreatePart("FeatherR2", new Vector2(0f, -34f), new Vector2(44f, 18f), dark, wingR);
            CreatePart("FeatherR3", new Vector2(0f, -50f), new Vector2(36f, 16f), light, wingR);
            wingR.gameObject.AddComponent<SwayMotion>().rotationAmount = -5f;

            // Belly
            CreatePart("Belly", new Vector2(0f, -10f), new Vector2(70f, 80f), light, body);

            // Talons
            var talonL = CreatePart("TalonL", new Vector2(-28f, -68f), new Vector2(14f, 22f), dark);
            CreatePart("ClawL1", new Vector2(-8f, -10f), new Vector2(8f, 14f), dark, talonL).localRotation = Quaternion.Euler(0f, 0f, -20f);
            CreatePart("ClawL2", new Vector2(8f, -10f), new Vector2(8f, 14f), dark, talonL).localRotation = Quaternion.Euler(0f, 0f, 20f);

            var talonR = CreatePart("TalonR", new Vector2(28f, -68f), new Vector2(14f, 22f), dark);
            CreatePart("ClawR1", new Vector2(-8f, -10f), new Vector2(8f, 14f), dark, talonR).localRotation = Quaternion.Euler(0f, 0f, -20f);
            CreatePart("ClawR2", new Vector2(8f, -10f), new Vector2(8f, 14f), dark, talonR).localRotation = Quaternion.Euler(0f, 0f, 20f);

            // Head — large, round
            var head = CreatePart("Head", new Vector2(0f, 76f), new Vector2(114f, 108f), accent);

            // Facial disc
            CreatePart("FacialDisc", new Vector2(0f, 0f), new Vector2(90f, 90f), light, head);

            // Large, iconic owl eyes with dark pupils
            var eyeL = CreatePart("EyeL", new Vector2(-22f, 10f), new Vector2(30f, 30f), new Color(0.95f, 0.82f, 0.2f, 1f), head);
            CreatePart("PupilL", Vector2.zero, new Vector2(14f, 14f), dark, eyeL);
            var glowL = CreatePart("EyeGlowL", Vector2.zero, new Vector2(34f, 34f), new Color(accent.r, accent.g, accent.b, 0.28f), head);
            glowL.gameObject.AddComponent<PulseGlow>().speed = 1.2f;

            var eyeR = CreatePart("EyeR", new Vector2(22f, 10f), new Vector2(30f, 30f), new Color(0.95f, 0.82f, 0.2f, 1f), head);
            CreatePart("PupilR", Vector2.zero, new Vector2(14f, 14f), dark, eyeR);
            var glowR = CreatePart("EyeGlowR", Vector2.zero, new Vector2(34f, 34f), new Color(accent.r, accent.g, accent.b, 0.28f), head);
            glowR.gameObject.AddComponent<PulseGlow>().speed = 1.4f;

            // Beak — hooked
            var beak = CreatePart("Beak", new Vector2(0f, -12f), new Vector2(20f, 18f), new Color(0.78f, 0.58f, 0.18f, 1f), head);
            beak.localScale = new Vector3(1f, 0.6f, 1f);
            CreatePart("BeakHook", new Vector2(0f, -10f), new Vector2(10f, 12f), new Color(0.58f, 0.42f, 0.1f, 1f), beak);

            // Ear tufts
            var tuftL = CreatePart("TuftL", new Vector2(-38f, 44f), new Vector2(14f, 28f), dark, head);
            tuftL.localRotation = Quaternion.Euler(0f, 0f, -15f);
            var tuftR = CreatePart("TuftR", new Vector2(38f, 44f), new Vector2(14f, 28f), dark, head);
            tuftR.localRotation = Quaternion.Euler(0f, 0f, 15f);

            // Ancient rune glow on chest
            var runeGlow = CreatePart("RuneGlow", new Vector2(0f, -4f), new Vector2(44f, 44f),
                new Color(accent.r * 0.8f, accent.g * 0.8f, 1f, 0.45f), body);
            var rPulse = runeGlow.gameObject.AddComponent<PulseGlow>();
            rPulse.speed = 0.6f;
            rPulse.minAlpha = 0.15f;
            rPulse.maxAlpha = 0.7f;
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
