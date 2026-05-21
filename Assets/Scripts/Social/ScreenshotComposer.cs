using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Screenshot composer for creature share cards and sanctuary screenshots.
    ///
    /// Produces beautifully composed 1080x1080 share images:
    ///   - Evolved creature portrait with name and stage
    ///   - Sanctuary overview with seasonal theme
    ///   - Puzzle victory celebration card
    ///   - Memory scrapbook page
    ///
    /// Images are saved to device gallery via NativeGallery (simulated in editor).
    /// </summary>
    public class ScreenshotComposer : MonoBehaviour
    {
        // ─── Events ──────────────────────────────────────────────────────────────

        public event Action<Texture2D, string> OnScreenshotReady;   // texture, filePath
        public event Action<string>            OnShareComplete;      // destination

        // ─── State ───────────────────────────────────────────────────────────────

        private CreatureEvolutionSystem _evolution;
        private EmotionalBondingEngine  _bonding;
        private SanctuaryDecorationSystem _sanctuary;
        private UIAnimationSystem        _uiAnim;

        private bool _isCapturing;
        private const int ShareWidth  = 1080;
        private const int ShareHeight = 1080;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        public void Initialize(
            CreatureEvolutionSystem   evolution,
            EmotionalBondingEngine    bonding,
            SanctuaryDecorationSystem sanctuary,
            UIAnimationSystem         uiAnim)
        {
            _evolution = evolution;
            _bonding   = bonding;
            _sanctuary = sanctuary;
            _uiAnim    = uiAnim;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Capture the current screen as a share-ready card.</summary>
        public void CaptureEvolutionCard(string creatureId)
        {
            if (_isCapturing) return;
            StartCoroutine(CaptureCoroutine("evolution_" + creatureId, ShareCardType.Evolution, creatureId));
        }

        public void CaptureSanctuaryCard()
        {
            if (_isCapturing) return;
            StartCoroutine(CaptureCoroutine("sanctuary", ShareCardType.Sanctuary, null));
        }

        public void CapturePuzzleVictoryCard(int stars)
        {
            if (_isCapturing) return;
            StartCoroutine(CaptureCoroutine($"victory_{stars}star", ShareCardType.Victory, stars.ToString()));
        }

        public bool IsCapturing => _isCapturing;

        // ─── Capture Coroutine ────────────────────────────────────────────────────

        private IEnumerator CaptureCoroutine(string fileName, ShareCardType type, string context)
        {
            _isCapturing = true;

            // Wait for end of frame so all UI is rendered
            yield return new WaitForEndOfFrame();

            // Capture screen area
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();

            // Scale to share dimensions
            var scaled = ScaleTexture(tex, ShareWidth, ShareHeight);
            Destroy(tex);

            // Add watermark overlay (simulated — in production add actual overlay)
            AddForestWatermark(scaled);

            // Save to persistent data path (simulates device gallery)
            var path = SaveTextureToDisk(scaled, fileName);

            _isCapturing = false;
            OnScreenshotReady?.Invoke(scaled, path);

            Debug.Log($"[ScreenshotComposer] Screenshot saved: {path}");

            // Celebration animation
            _uiAnim?.CelebrationPop(transform, 1.1f);
        }

        // ─── Image Processing ─────────────────────────────────────────────────────

        private Texture2D ScaleTexture(Texture2D src, int w, int h)
        {
            var rt = RenderTexture.GetTemporary(w, h);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var dst = new Texture2D(w, h);
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            dst.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }

        private void AddForestWatermark(Texture2D tex)
        {
            // Simulated: in production, render a small Forest Friends Quest logo
            // into the bottom-right corner of the texture
            var brandColor = new Color(0.2f, 0.7f, 0.4f, 0.6f);
            for (int x = tex.width - 120; x < tex.width - 20; x++)
                for (int y = 20; y < 40; y++)
                    tex.SetPixel(x, y, Color.Lerp(tex.GetPixel(x, y), brandColor, 0.5f));
            tex.Apply();
        }

        private string SaveTextureToDisk(Texture2D tex, string name)
        {
            var bytes = tex.EncodeToPNG();
            var path  = System.IO.Path.Combine(Application.persistentDataPath, $"ffq_{name}.png");
            System.IO.File.WriteAllBytes(path, bytes);
            return path;
        }

        private enum ShareCardType { Evolution, Sanctuary, Victory }
    }
}
