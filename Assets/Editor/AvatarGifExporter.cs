#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest.Editor
{
    /// <summary>
    /// Unity Editor tool: Forest Friends Quest → Export Avatar GIF Frames
    ///
    /// Generates 128×128 PNG sprite sheets for all 6 characters, plus a
    /// composite 6-up PNG showing all characters side-by-side.
    /// The output folder is  Assets/GeneratedAvatars/  (created automatically).
    ///
    /// Use the menu item or call AvatarGifExporter.ExportAll() from code.
    ///
    /// GIF assembly: import the exported PNGs into any GIF tool
    /// (e.g. GIMP, Photoshop, ezgif.com) at 150 ms per frame.
    /// </summary>
    public static class AvatarGifExporter
    {
        private const string OutputFolder = "Assets/GeneratedAvatars";
        private const int    AvatarSize   = 128;

        private static readonly string[] CharacterIds = { "pip", "mimi", "tomo", "luma", "nori", "sol" };

        // ─── Menu Entry ───────────────────────────────────────────────────────────

        [MenuItem("Forest Friends Quest/Export Avatar Sprites & GIF Frames")]
        public static void ExportAll()
        {
            EnsureFolder();

            var sprites = new List<Sprite>();

            foreach (var id in CharacterIds)
            {
                var sprite = AvatarSpriteLibrary.GetSprite(id);
                if (sprite == null) { Debug.LogWarning($"[GifExporter] Sprite null for {id}"); continue; }

                var tex = DuplicateReadable(sprite.texture);
                SavePng(tex, $"{OutputFolder}/{id}_avatar.png");
                sprites.Add(sprite);
                Debug.Log($"[GifExporter] Exported {id}_avatar.png");
            }

            ExportComposite(sprites);
            ExportAnimationFrames();

            AssetDatabase.Refresh();
            Debug.Log($"[GifExporter] Done. Check {OutputFolder}/");
            EditorUtility.DisplayDialog("Avatar Export Complete",
                $"All avatar sprites and animation frames exported to:\n{OutputFolder}/\n\nTo create a GIF:\n1. Import the frame_*.png files into GIMP or Photoshop.\n2. Use File → Export As → GIF (Photoshop) or Filters → Animation → Optimise for GIF (GIMP).\n3. Set frame delay to 150 ms.",
                "OK");
        }

        // ─── Composite (all 6 side by side) ──────────────────────────────────────

        private static void ExportComposite(List<Sprite> sprites)
        {
            const int cols    = 3;
            const int rows    = 2;
            const int padding = 8;
            var compositeW = cols * AvatarSize + (cols + 1) * padding;
            var compositeH = rows * AvatarSize + (rows + 1) * padding;

            var composite = new Texture2D(compositeW, compositeH, TextureFormat.RGBA32, false);
            var clear     = new Color[compositeW * compositeH];
            for (var i = 0; i < clear.Length; i++) clear[i] = new Color(0.06f, 0.16f, 0.12f, 1f);
            composite.SetPixels(clear);

            for (var i = 0; i < sprites.Count; i++)
            {
                var col = i % cols;
                var row = i / cols;
                var destX = padding + col * (AvatarSize + padding);
                var destY = compositeH - padding - (row + 1) * AvatarSize - row * padding;

                var src = DuplicateReadable(sprites[i].texture);
                var pixels = src.GetPixels(0, 0, AvatarSize, AvatarSize);
                composite.SetPixels(destX, destY, AvatarSize, AvatarSize, pixels);
            }

            composite.Apply();
            SavePng(composite, $"{OutputFolder}/all_characters_composite.png");
            Debug.Log("[GifExporter] Exported all_characters_composite.png");
        }

        // ─── Animation Frames ─────────────────────────────────────────────────────

        private static void ExportAnimationFrames()
        {
            // Export 8 frames: one per emotion state, each showing all characters
            // with the "active" (highlighted) character cycling through them
            var emotions = new[]
            {
                "Idle", "Happy", "Excited", "Curious",
                "Proud", "Playful", "Shy", "Sleepy"
            };

            // For a simple GIF, we just export each character avatar with a colored
            // tint strip indicating the current emotion
            var emotionColors = new Dictionary<string, Color>
            {
                { "Idle",    new Color(0.55f, 0.88f, 0.65f) },
                { "Happy",   new Color(1.00f, 0.85f, 0.30f) },
                { "Excited", new Color(1.00f, 0.50f, 0.20f) },
                { "Curious", new Color(0.50f, 0.80f, 1.00f) },
                { "Proud",   new Color(0.80f, 0.50f, 1.00f) },
                { "Playful", new Color(1.00f, 0.60f, 0.80f) },
                { "Shy",     new Color(0.80f, 0.90f, 0.80f) },
                { "Sleepy",  new Color(0.60f, 0.65f, 0.80f) },
            };

            const int cols    = 3;
            const int rows    = 2;
            const int padding = 8;
            const int stripH  = 24;
            var frameW = cols * AvatarSize + (cols + 1) * padding;
            var frameH = rows * AvatarSize + (rows + 1) * padding + stripH + padding;

            for (var frameIdx = 0; frameIdx < emotions.Length; frameIdx++)
            {
                var emotion      = emotions[frameIdx];
                var emotionColor = emotionColors[emotion];
                var frame        = new Texture2D(frameW, frameH, TextureFormat.RGBA32, false);

                // Dark background
                var bgPixels = new Color[frameW * frameH];
                for (var p = 0; p < bgPixels.Length; p++)
                    bgPixels[p] = new Color(0.06f, 0.16f, 0.12f, 1f);
                frame.SetPixels(bgPixels);

                // Emotion color strip at top
                for (var y = frameH - stripH; y < frameH; y++)
                    for (var x = 0; x < frameW; x++)
                        frame.SetPixel(x, y, emotionColor);

                // Blit each character avatar, tinting featured character per emotion
                for (var i = 0; i < CharacterIds.Length; i++)
                {
                    var col  = i % cols;
                    var row  = i / cols;
                    var destX = padding + col * (AvatarSize + padding);
                    var destY = frameH - stripH - padding * 2 - (row + 1) * AvatarSize - row * padding;

                    var sprite  = AvatarSpriteLibrary.GetSprite(CharacterIds[i]);
                    if (sprite == null) continue;

                    var src     = DuplicateReadable(sprite.texture);
                    var pixels  = src.GetPixels(0, 0, AvatarSize, AvatarSize);

                    // Featured character = the one whose index matches the frame
                    var isFeatured = (i == frameIdx % CharacterIds.Length);
                    if (isFeatured)
                    {
                        // Add an emotion-colored glow tint
                        for (var p = 0; p < pixels.Length; p++)
                        {
                            var px = pixels[p];
                            pixels[p] = Color.Lerp(px,
                                new Color(emotionColor.r, emotionColor.g, emotionColor.b, px.a),
                                0.22f);
                        }
                    }

                    frame.SetPixels(destX, destY, AvatarSize, AvatarSize, pixels);

                    // Highlight border for featured
                    if (isFeatured)
                    {
                        for (var bx = destX; bx < destX + AvatarSize; bx++)
                        {
                            SetSafe(frame, bx, destY, emotionColor, frameW, frameH);
                            SetSafe(frame, bx, destY + AvatarSize - 1, emotionColor, frameW, frameH);
                        }
                        for (var by = destY; by < destY + AvatarSize; by++)
                        {
                            SetSafe(frame, destX, by, emotionColor, frameW, frameH);
                            SetSafe(frame, destX + AvatarSize - 1, by, emotionColor, frameW, frameH);
                        }
                    }
                }

                frame.Apply();
                SavePng(frame, $"{OutputFolder}/gif_frame_{frameIdx:D2}_{emotion}.png");
            }

            Debug.Log("[GifExporter] Exported 8 GIF animation frames.");
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static void SetSafe(Texture2D tex, int x, int y, Color c, int w, int h)
        {
            if (x >= 0 && x < w && y >= 0 && y < h) tex.SetPixel(x, y, c);
        }

        private static void SavePng(Texture2D tex, string path)
        {
            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        private static Texture2D DuplicateReadable(Texture2D source)
        {
            var rt  = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets", "GeneratedAvatars");
        }
    }
}
#endif
