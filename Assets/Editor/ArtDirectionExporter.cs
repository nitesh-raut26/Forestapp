#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForestFriendsQuest.Editor
{
    // Forest Friends Quest — Art Direction Exporter
    // Menu: Forest Friends Quest → Export All Art Direction Assets
    //
    // Exports to Assets/GeneratedArt/:
    //   sprites/   — 512×64 emotion sprite sheets for all 6 characters
    //   biomes/    — 256×512 background PNGs for all 10 biomes
    //   audio/     — 44.1kHz mono WAV voice cue lines per character
    public static class ArtDirectionExporter
    {
        private const string OutDir = "Assets/GeneratedArt";

        private static readonly string[] CharacterIds =
            { "pip", "mimi", "tomo", "luma", "nori", "sol" };

        private static readonly string[] CueTypes =
            { "greeting", "hint", "cheer" };

        private static readonly string[] BiomeIds =
        {
            "fern-trail", "firefly-hollow", "river-bend", "moonlit-creek",
            "elderwood-grove", "crystal-caverns", "forgotten-ruins",
            "firefly-marsh", "ancient-observatory", "skyroot-canopy"
        };

        // ─── Menu Items ───────────────────────────────────────────────────────────

        [MenuItem("Forest Friends Quest/Export All Art Direction Assets")]
        public static void ExportAll()
        {
            EnsureFolders();
            ExportSpriteSheets();
            ExportBiomeBackgrounds();
            ExportVoiceCues();
            AssetDatabase.Refresh();

            Debug.Log("[ArtDirectionExporter] All assets exported to " + OutDir);
            EditorUtility.DisplayDialog("Art Direction Export Complete",
                $"All art direction assets saved to:\n{OutDir}/\n\n" +
                "  sprites/   — 6 emotion sprite sheets\n" +
                "  biomes/    — 10 background PNGs\n" +
                "  audio/     — 18 voice cue WAV files (6 chars × 3 cues)",
                "OK");
        }

        [MenuItem("Forest Friends Quest/Export Sprite Sheets Only")]
        public static void ExportSpriteSheetsMenu()
        {
            EnsureFolders();
            ExportSpriteSheets();
            AssetDatabase.Refresh();
            Debug.Log("[ArtDirectionExporter] Sprite sheets exported.");
        }

        [MenuItem("Forest Friends Quest/Export Biome Backgrounds Only")]
        public static void ExportBiomeBackgroundsMenu()
        {
            EnsureFolders();
            ExportBiomeBackgrounds();
            AssetDatabase.Refresh();
            Debug.Log("[ArtDirectionExporter] Biome backgrounds exported.");
        }

        // ─── Sprite Sheet Export ──────────────────────────────────────────────────

        private static void ExportSpriteSheets()
        {
            foreach (var id in CharacterIds)
            {
                var sheet = CharacterSpriteSheetGenerator.GetSpriteSheet(id);
                SavePng(sheet, $"{OutDir}/sprites/{id}_spritesheet.png");
                Debug.Log($"[ArtDirectionExporter] Sprite sheet: {id} (512×64, 8 frames)");

                // Also export each individual emotion frame
                var frames = CharacterSpriteSheetGenerator.GetAllFrames(id);
                var emotions = new[]
                    { "idle", "happy", "excited", "curious", "proud", "playful", "shy", "sleepy" };
                for (var i = 0; i < frames.Length; i++)
                {
                    var frameTex = ExtractFrameTexture(sheet, i * 64, 0, 64, 64);
                    SavePng(frameTex, $"{OutDir}/sprites/{id}_{emotions[i]}.png");
                    Object.DestroyImmediate(frameTex);
                }
            }
        }

        // ─── Biome Background Export ──────────────────────────────────────────────

        private static void ExportBiomeBackgrounds()
        {
            foreach (var biomeId in BiomeIds)
            {
                var profile = BuildBiomeProfile(biomeId);
                if (profile == null) continue;

                var tex = BiomeBackgroundRenderer.GenerateTexture(profile);
                SavePng(tex, $"{OutDir}/biomes/{biomeId.Replace('-', '_')}_background.png");
                Object.DestroyImmediate(tex);
                Debug.Log($"[ArtDirectionExporter] Biome background: {biomeId} (256×512)");
            }
        }

        // ─── Voice Cue WAV Export ─────────────────────────────────────────────────

        private static void ExportVoiceCues()
        {
            var go  = new GameObject("TmpAudioLibrary");
            var lib = go.AddComponent<AudioAssetLibrary>();

            foreach (var id in CharacterIds)
            {
                foreach (var cue in CueTypes)
                {
                    var clip = lib.GetCharacterCueLine(id, cue);
                    if (clip == null) continue;
                    SaveWav(clip, $"{OutDir}/audio/{id}_{cue}.wav");
                    Debug.Log($"[ArtDirectionExporter] Voice cue: {id}/{cue}");
                }
            }

            Object.DestroyImmediate(go);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static void SavePng(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }

        private static void SaveWav(AudioClip clip, string path)
        {
            const int sampleRate = 44100;
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            using var fs = new FileStream(path, FileMode.Create);
            using var bw = new BinaryWriter(fs);

            int dataLen = samples.Length * 2;

            // WAV header
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataLen);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);                        // PCM
            bw.Write((short)clip.channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * clip.channels * 2);  // byte rate
            bw.Write((short)(clip.channels * 2));      // block align
            bw.Write((short)16);                       // bits per sample
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataLen);

            foreach (var s in samples)
                bw.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
        }

        private static Texture2D ExtractFrameTexture(Texture2D sheet, int sx, int sy, int w, int h)
        {
            var frame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            frame.SetPixels(sheet.GetPixels(sx, sy, w, h));
            frame.Apply();
            return frame;
        }

        // Creates a temporary BiomeController, builds all profiles, and returns the one for biomeId.
        private static BiomeProfile BuildBiomeProfile(string biomeId)
        {
            var go      = new GameObject("TmpBiome");
            var ctrl    = go.AddComponent<BiomeController>();
            ctrl.Initialize(null, null); // nulls are safe: Initialize only stores refs + builds profiles
            var profile = ctrl.GetBiome(biomeId);
            Object.DestroyImmediate(go);
            return profile;
        }

        private static void EnsureFolders()
        {
            foreach (var sub in new[] { OutDir, $"{OutDir}/sprites", $"{OutDir}/biomes", $"{OutDir}/audio" })
            {
                if (!AssetDatabase.IsValidFolder(sub))
                {
                    var parent = Path.GetDirectoryName(sub)?.Replace('\\', '/') ?? "Assets";
                    var name   = Path.GetFileName(sub);
                    AssetDatabase.CreateFolder(parent, name);
                }
            }
        }
    }
}
#endif
