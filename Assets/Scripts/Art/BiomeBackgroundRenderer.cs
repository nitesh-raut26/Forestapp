using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    // Generates and displays a procedural background image for each biome.
    // Subscribed to BiomeController.OnBiomeEntered — swaps the background whenever
    // the player enters a new zone. Textures are cached so a zone revisit is instant.
    //
    // Initialization: BiomeBackgroundRenderer.Create(biomeController, canvasRoot)
    public class BiomeBackgroundRenderer : MonoBehaviour
    {
        private const int TexW = 2048;
        private const int TexH = 4096;

        private RawImage _bg;
        private readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();

        // ─── Factory ─────────────────────────────────────────────────────────────

        public static BiomeBackgroundRenderer Create(BiomeController biome, Transform canvasRoot)
        {
            var go = new GameObject("BiomeBackground");
            go.transform.SetParent(canvasRoot, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            go.transform.SetAsFirstSibling(); // render behind all UI

            var renderer = go.AddComponent<BiomeBackgroundRenderer>();
            renderer._bg  = go.AddComponent<RawImage>();
            renderer.Initialize(biome);
            return renderer;
        }

        // ─── Init ─────────────────────────────────────────────────────────────────

        // Called by editor tools — generates a biome background without a scene/canvas.
        public static Texture2D GenerateTexture(BiomeProfile profile) => GenerateBackground(profile);

        public void Initialize(BiomeController biome)
        {
            if (biome == null) return;
            biome.OnBiomeEntered += OnBiomeEntered;

            // Show current biome immediately if one is already set
            var current = biome.GetCurrentBiome();
            if (current != null) OnBiomeEntered(current);
        }

        // ─── Biome Transition ─────────────────────────────────────────────────────

        private void OnBiomeEntered(BiomeProfile profile)
        {
            if (_bg == null || profile == null) return;

            if (!_cache.TryGetValue(profile.regionId, out var tex))
            {
                tex = GenerateBackground(profile);
                _cache[profile.regionId] = tex;
            }

            _bg.texture = tex;
        }

        // ─── Texture Generator ────────────────────────────────────────────────────

        private static Texture2D GenerateBackground(BiomeProfile p)
        {
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };

            // Layers painted bottom→top (y=0 = ground, y=4095 = zenith)
            PaintSky(tex, p);
            PaintHorizonFog(tex, p);
            PaintGround(tex, p);
            PaintBiomeElements(tex, p);

            tex.Apply();
            return tex;
        }

        // Sky gradient from mid-tone sky at horizon → deeper sky at zenith
        private static void PaintSky(Texture2D t, BiomeProfile p)
        {
            var skyLow  = p.skyTintColor;
            var skyHigh = new Color(
                p.skyTintColor.r * 0.55f,
                p.skyTintColor.g * 0.55f,
                p.skyTintColor.b * 0.70f, 1f);

            int w = t.width;
            int h = t.height;
            for (var y = h / 3; y < h; y++)
            {
                var frac  = (y - h / 3f) / (h - h / 3f);
                var color = Color.Lerp(skyLow, skyHigh, frac);
                for (var x = 0; x < w; x++)
                    t.SetPixel(x, y, color);
            }
        }

        // Fog band at the horizon
        private static void PaintHorizonFog(Texture2D t, BiomeProfile p)
        {
            int h = t.height;
            int w = t.width;
            int bandBot = h * 28 / 100;
            int bandTop = h * 42 / 100;

            for (var y = bandBot; y < bandTop; y++)
            {
                var frac  = Mathf.SmoothStep(0f, 1f, (y - bandBot) / (float)(bandTop - bandBot));
                var alpha = Mathf.Sin(frac * Mathf.PI) * Mathf.Clamp(p.fogColor.a * 3f, 0.1f, 0.8f);
                var col   = new Color(p.fogColor.r, p.fogColor.g, p.fogColor.b, alpha);
                for (var x = 0; x < w; x++)
                    BlendPixel(t, x, y, col);
            }
        }

        // Ground band with vignette
        private static void PaintGround(Texture2D t, BiomeProfile p)
        {
            int h = t.height;
            int w = t.width;
            int groundTop = h * 32 / 100;

            for (var y = 0; y < groundTop; y++)
            {
                var frac  = y / (float)groundTop;
                var color = Color.Lerp(
                    new Color(p.groundTintColor.r * 0.45f, p.groundTintColor.g * 0.45f, p.groundTintColor.b * 0.45f),
                    p.groundTintColor, frac);
                for (var x = 0; x < w; x++)
                    t.SetPixel(x, y, color);
            }
        }

        // Biome-specific silhouette elements
        private static void PaintBiomeElements(Texture2D t, BiomeProfile p)
        {
            switch (p.regionId)
            {
                case "fern-trail":        PaintFernTrail(t, p);       break;
                case "firefly-hollow":    PaintFireflyHollow(t, p);   break;
                case "river-bend":        PaintRiverBend(t, p);       break;
                case "moonlit-creek":     PaintMoonlitCreek(t, p);    break;
                case "elderwood-grove":   PaintElderwoodGrove(t, p);  break;
                case "crystal-caverns":   PaintCrystalCaverns(t, p);  break;
                case "forgotten-ruins":   PaintForgottenRuins(t, p);  break;
                case "firefly-marsh":     PaintFireflyMarsh(t, p);    break;
                case "ancient-observatory": PaintObservatory(t, p);   break;
                case "skyroot-canopy":    PaintSkyrootCanopy(t, p);   break;
            }
        }

        // ─── Biome Element Painters ───────────────────────────────────────────────

        private static void PaintFernTrail(Texture2D t, BiomeProfile p)
        {
            // Rolling meadow hills + fern fronds
            var darkGreen = new Color(p.groundTintColor.r * 0.7f, p.groundTintColor.g * 0.7f, p.groundTintColor.b * 0.7f);
            DrawHillSilhouette(t, new[] { 80, 95, 70, 105, 88, 100, 75, 110 }, darkGreen, 512 * 30 / 100);

            // Tall grass stems
            var stemC = new Color(0.20f, 0.50f, 0.22f);
            for (var i = 0; i < 18; i++)
            {
                int x = 10 + i * 13;
                int h = 30 + (x * 7 + 11) % 25;
                DrawVine(t, x, 512 * 30 / 100, h, stemC);
            }

            // Sun haze
            DrawCircleGlow(t, 256 * 3 / 4, 512 * 78 / 100, 40, new Color(1f, 0.98f, 0.75f, 0.30f));
        }

        private static void PaintFireflyHollow(Texture2D t, BiomeProfile p)
        {
            // Dark dense tree silhouettes
            var treeC = new Color(0.08f, 0.16f, 0.10f);
            DrawTreeSilhouette(t, 20,  512 * 32 / 100, 18, 80, treeC);
            DrawTreeSilhouette(t, 55,  512 * 32 / 100, 14, 65, treeC);
            DrawTreeSilhouette(t, 100, 512 * 32 / 100, 20, 90, treeC);
            DrawTreeSilhouette(t, 155, 512 * 32 / 100, 16, 70, treeC);
            DrawTreeSilhouette(t, 200, 512 * 32 / 100, 18, 85, treeC);
            DrawTreeSilhouette(t, 235, 512 * 32 / 100, 12, 55, treeC);

            // Firefly glow dots
            var ffc = new Color(0.75f, 1f, 0.55f, 0.65f);
            int[] ffx = {  30,  72, 110, 140, 168, 195, 218, 45, 88, 130, 175, 220 };
            int[] ffy = { 190, 220, 195, 240, 210, 190, 230, 260, 280, 270, 255, 300 };
            for (var i = 0; i < ffx.Length; i++)
                DrawCircleGlow(t, ffx[i], ffy[i], 6, ffc);
        }

        private static void PaintRiverBend(Texture2D t, BiomeProfile p)
        {
            int w = t.width;
            int h = t.height;
            float scaleX = w / 256f;
            float scaleY = h / 512f;

            var riverC = new Color(0.35f, 0.65f, 0.85f, 0.80f);
            int riverY = Mathf.RoundToInt(h * 0.25f);
            int halfHeight = Mathf.RoundToInt(14f * scaleY);

            for (var x = 0; x < w; x++)
            {
                float vx = x / scaleX;
                int wave = Mathf.RoundToInt(8f * scaleY * Mathf.Sin(vx * 0.06f));
                for (var y = riverY + wave - halfHeight; y < riverY + wave + halfHeight; y++)
                {
                    float fade = 1f - Mathf.Abs(y - riverY - wave) / (float)halfHeight;
                    BlendPixel(t, x, y, new Color(riverC.r, riverC.g, riverC.b, riverC.a * fade));
                }
            }

            // Reflection shimmer
            var shimmerC = new Color(0.85f, 0.95f, 1f, 0.35f);
            for (var x = 0; x < w; x += Mathf.Max(1, Mathf.RoundToInt(4f * scaleX)))
            {
                float vx = x / scaleX;
                int wave = Mathf.RoundToInt(4f * scaleY * Mathf.Sin(vx * 0.12f));
                BlendPixel(t, x, riverY + wave, shimmerC);
            }

            // Riverside trees
            var treeC = new Color(0.18f, 0.42f, 0.28f);
            DrawTreeSilhouette(t, 15,  163, 12, 55, treeC);
            DrawTreeSilhouette(t, 200, 163, 14, 60, treeC);
        }

        private static void PaintMoonlitCreek(Texture2D t, BiomeProfile p)
        {
            // Moon disc
            DrawCircleGlow(t, 256 * 3 / 4, 512 * 78 / 100, 28, new Color(0.95f, 0.98f, 1f, 0.90f));
            DrawCircleGlow(t, 256 * 3 / 4, 512 * 78 / 100, 18, new Color(1f, 1f, 1f, 0.95f));

            // Moon reflection in water
            DrawCircleGlow(t, 256 * 3 / 4, 512 * 18 / 100, 10,
                new Color(0.90f, 0.93f, 1f, 0.55f));

            // Stars
            var starC = new Color(1f, 1f, 0.90f, 0.70f);
            int[] sx = {  25,  55,  80, 115, 145, 170, 210, 38, 98, 130, 185, 225 };
            int[] sy = { 380, 420, 390, 440, 410, 450, 420, 460, 480, 500, 470, 495 };
            for (var i = 0; i < sx.Length; i++)
                DrawCircleGlow(t, sx[i], sy[i], 1, starC);

            // Dark creek shore
            var shoreC = new Color(0.12f, 0.22f, 0.38f);
            DrawHillSilhouette(t, new[] { 60, 50, 70, 45, 65, 55, 75, 48 }, shoreC, 512 * 30 / 100);
        }

        private static void PaintElderwoodGrove(Texture2D t, BiomeProfile p)
        {
            // Giant ancient tree trunk in center
            var trunkC = new Color(0.28f, 0.20f, 0.12f);
            var canopyC = new Color(0.15f, 0.35f, 0.18f);
            FillRect(t, 256/2 - 18, 0, 36, 266, trunkC);
            // Bark texture lines
            for (var y = 10; y < 266; y += 18)
                for (var x = 256/2 - 17; x < 256/2 + 17; x += 5)
                    DrawCircleGlow(t, x, y, 1, new Color(0.20f, 0.14f, 0.08f, 0.50f));

            // Canopy
            DrawCircleGlow(t, 256/2, 281, 90, canopyC);
            DrawCircleGlow(t, 256/2, 281, 70, new Color(0.18f, 0.42f, 0.22f));

            // Falling leaves
            var leafC = new Color(0.35f, 0.60f, 0.28f, 0.70f);
            int[] lx = { 30, 60, 95, 130, 165, 200, 45, 80, 120, 155, 190, 220 };
            int[] ly = { 220, 250, 200, 270, 230, 210, 300, 320, 290, 340, 310, 280 };
            for (var i = 0; i < lx.Length; i++)
                DrawEllipse(t, lx[i], ly[i], 4, 2, leafC);
        }

        private static void PaintCrystalCaverns(Texture2D t, BiomeProfile p)
        {
            int w = t.width;
            int h = t.height;
            float scaleX = w / 256f;
            float scaleY = h / 512f;

            var crystalC = new Color(0.55f, 0.85f, 1f, 0.80f);
            var glowC    = new Color(0.70f, 0.90f, 1f, 0.40f);
            int[] cx = { 20, 50, 80, 110, 140, 170, 200, 230, 35, 65, 95, 125, 155, 185, 215 };
            int[] ch = { 60, 45, 70, 55,  65,  50,  75,  42,  80, 38, 68, 52,  62,  48,  72  };
            for (var i = 0; i < cx.Length; i++)
            {
                FillRect(t, cx[i]-2, 512 - ch[i], 4, ch[i], crystalC);
                DrawCircleGlow(t, cx[i], 512 - ch[i], 8, glowC);
            }

            // Crystal reflections on floor
            for (var i = 0; i < cx.Length; i++)
                DrawEllipse(t, cx[i], 61, 5, 3,
                    new Color(crystalC.r, crystalC.g, crystalC.b, 0.35f));

            // Deep cave gradient overlay
            for (var y = 0; y < h; y++)
            {
                float d = (h - y) / (float)h;
                var col = new Color(0f, 0f, 0.08f, d * 0.40f);
                for (var x = 0; x < w; x++)
                    BlendPixel(t, x, y, col);
            }
        }

        private static void PaintForgottenRuins(Texture2D t, BiomeProfile p)
        {
            var stoneC = new Color(0.42f, 0.38f, 0.28f);
            var mossC  = new Color(0.28f, 0.40f, 0.20f);

            // Stone arch silhouette
            int archCX = 256 / 2, archY = 163;
            FillRect(t, archCX - 45, archY, 12, 70, stoneC);
            FillRect(t, archCX + 33, archY, 12, 70, stoneC);
            // Arch curve
            for (var a = 0; a <= 180; a += 4)
            {
                float rad = a * Mathf.Deg2Rad;
                int x = Mathf.RoundToInt(archCX + 38f * Mathf.Cos(rad));
                int y = Mathf.RoundToInt(archY + 70f + 30f * Mathf.Sin(rad));
                FillRect(t, x-6, y-6, 12, 12, stoneC);
            }

            // Rubble on ground + moss
            for (var i = 0; i < 12; i++)
            {
                int rx = 15 + i * 19, ry = 143 + i % 3 * 8;
                DrawEllipse(t, rx, ry, 8 + i % 4 * 2, 5 + i % 3, stoneC);
                DrawEllipse(t, rx + 3, ry + 2, 4, 2, mossC);
            }
        }

        private static void PaintFireflyMarsh(Texture2D t, BiomeProfile p)
        {
            int w = t.width;
            int h = t.height;
            float scaleX = w / 256f;
            float scaleY = h / 512f;

            // Bulrush silhouettes
            var reedC = new Color(0.12f, 0.28f, 0.16f);
            var headC = new Color(0.28f, 0.18f, 0.10f);
            int[] rx = { 20, 40, 60, 80, 120, 150, 175, 200, 225 };
            for (var i = 0; i < rx.Length; i++)
            {
                int hval = 55 + i % 4 * 15;
                FillRect(t, rx[i]-1, 128, 2, hval, reedC);
                DrawEllipse(t, rx[i], 128 + hval, 3, 10, headC);
            }

            // Murky water
            var waterC = new Color(0.12f, 0.25f, 0.18f, 0.70f);
            int waterLimitY = Mathf.RoundToInt(h * 0.22f);
            for (var x = 0; x < w; x++)
            {
                float vx = x / scaleX;
                int wave = Mathf.RoundToInt(4f * scaleY * Mathf.Sin(vx * 0.08f));
                for (var y = 0; y < waterLimitY + wave; y++)
                    BlendPixel(t, x, y, waterC);
            }

            // Dense firefly cloud
            var ffc = new Color(0.65f, 1f, 0.45f, 0.60f);
            for (var i = 0; i < 20; i++)
            {
                int fx = (i * 37 + 15) % 256;
                int fy = 153 + (i * 23) % 153;
                DrawCircleGlow(t, fx, fy, 5, ffc);
            }
        }

        private static void PaintObservatory(Texture2D t, BiomeProfile p)
        {
            // Star field
            var starC = new Color(1f, 1f, 0.85f, 0.85f);
            for (var i = 0; i < 40; i++)
            {
                int sx = (i * 41 + 7) % 256;
                int sy = 230 + (i * 31) % 256;
                DrawCircleGlow(t, sx, sy, 1, starC);
                if (i % 4 == 0)
                    DrawCircleGlow(t, sx + 1, sy, 2, new Color(1f, 1f, 0.8f, 0.40f));
            }

            // Observatory dome silhouette
            var domeC = new Color(0.18f, 0.18f, 0.35f);
            DrawCircleGlow(t, 256/2, 169, 55, domeC);
            // Dome slit
            FillRect(t, 256/2 - 3, 163, 6, 26, new Color(0.55f, 0.60f, 1f, 0.70f));
            // Base
            FillRect(t, 256/2 - 40, 128, 80, 18, domeC);

            // Telescope silhouette
            FillRect(t, 256/2 - 2, 169, 4, 35, new Color(0.35f, 0.35f, 0.55f));
        }

        private static void PaintSkyrootCanopy(Texture2D t, BiomeProfile p)
        {
            // Floating island
            var islandC = new Color(0.28f, 0.55f, 0.25f);
            var rockC   = new Color(0.50f, 0.45f, 0.32f);
            DrawEllipse(t, 256/2, 348, 75, 22, islandC);
            DrawEllipse(t, 256/2, 337, 68, 14, rockC);

            // Root tendrils hanging down
            var rootC = new Color(0.28f, 0.20f, 0.12f);
            int[] rrx = { 256/2 - 55, 256/2 - 30, 256/2, 256/2 + 30, 256/2 + 55 };
            float scaleX = t.width / 256f;
            float scaleY = t.height / 512f;
            int thickness = Mathf.Max(1, Mathf.RoundToInt(1.5f * scaleX));

            for (var i = 0; i < rrx.Length; i++)
            {
                int rootLen = 80 + i % 3 * 25;
                int baseY   = 322;
                int actualBaseY = Mathf.RoundToInt(baseY * scaleY);
                int actualRootLen = Mathf.RoundToInt(rootLen * scaleY);

                for (var y = actualBaseY; y > actualBaseY - actualRootLen; y--)
                {
                    float vy = y / scaleY;
                    int xOff = Mathf.RoundToInt(5f * Mathf.Sin((baseY - vy) * 0.08f));
                    int ax = Mathf.RoundToInt((rrx[i] + xOff) * scaleX);
                    FillRectActual(t, ax - thickness / 2, y, thickness, 1, rootC);
                }
            }

            // Pollen particles
            var pollenC = new Color(0.98f, 0.95f, 0.60f, 0.55f);
            for (var i = 0; i < 24; i++)
            {
                int px = (i * 29 + 12) % 256;
                int py = 204 + (i * 19) % 281;
                DrawCircleGlow(t, px, py, 3, pollenC);
            }

            // Canopy leaf clusters at top
            var leafC = new Color(0.22f, 0.58f, 0.24f);
            DrawCircleGlow(t, 256/2, 460, 70, leafC);
            DrawCircleGlow(t, 256/2 - 60, 450, 45, leafC);
            DrawCircleGlow(t, 256/2 + 60, 450, 45, leafC);
        }

        // ─── Shared Drawing Helpers ───────────────────────────────────────────────

        private static void DrawHillSilhouette(Texture2D t, int[] heights, Color c, int baseY)
        {
            float scaleX = t.width / 256f;
            float scaleY = t.height / 512f;
            int segW = Mathf.RoundToInt((256f / heights.Length) * scaleX);
            int actualBaseY = Mathf.RoundToInt(baseY * scaleY);

            for (var s = 0; s < heights.Length - 1; s++)
            {
                int x0 = Mathf.RoundToInt(s * (256f / heights.Length) * scaleX);
                int x1 = Mathf.RoundToInt((s + 1) * (256f / heights.Length) * scaleX);
                for (var x = x0; x < x1; x++)
                {
                    var frac = (x - x0) / (float)(x1 - x0);
                    var h    = Mathf.Lerp(heights[s], heights[s + 1], frac) * scaleY;
                    int actualH = Mathf.RoundToInt(h);
                    for (var y = actualBaseY; y < actualBaseY + actualH; y++)
                        t.SetPixel(x, y, c);
                }
            }
        }

        private static void DrawTreeSilhouette(Texture2D t, int x, int groundY, int trunkW, int height, Color c)
        {
            FillRect(t, x - trunkW/2, groundY, trunkW, height / 3, c);
            DrawEllipse(t, x, groundY + height * 2 / 3, trunkW * 3, height / 2, c);
            DrawEllipse(t, x, groundY + height * 5 / 6, trunkW * 2, height / 3, c);
        }

        private static void DrawVine(Texture2D t, int x, int baseY, int height, Color c)
        {
            float scaleX = t.width / 256f;
            float scaleY = t.height / 512f;
            int startY = Mathf.RoundToInt(baseY * scaleY);
            int endY = Mathf.RoundToInt((baseY + height) * scaleY);
            int thickness = Mathf.Max(1, Mathf.RoundToInt(scaleX));

            for (var y = startY; y < endY; y++)
            {
                float vy = y / scaleY;
                int xOff = Mathf.RoundToInt(2f * Mathf.Sin(vy * 0.3f));
                int cx = Mathf.RoundToInt((x + xOff) * scaleX);
                FillRectActual(t, cx - thickness / 2, y, thickness, 1, c);
            }
            // Frond tip
            DrawEllipse(t, x, baseY + height, 3, 2, c);
        }

        private static void DrawCircleGlow(Texture2D t, int cx, int cy, int r, Color c)
        {
            float scaleX = t.width / 256f;
            float scaleY = t.height / 512f;
            int acx = Mathf.RoundToInt(cx * scaleX);
            int acy = Mathf.RoundToInt(cy * scaleY);
            int ar = Mathf.RoundToInt(r * scaleX);

            for (var y = acy - ar; y <= acy + ar; y++)
                for (var x = acx - ar; x <= acx + ar; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(acx, acy));
                    if (d > ar) continue;
                    float fade = 1f - d / ar;
                    BlendPixel(t, x, y, new Color(c.r, c.g, c.b, c.a * fade));
                }
        }

        private static void DrawEllipse(Texture2D t, int cx, int cy, int rx, int ry, Color c)
        {
            if (rx <= 0 || ry <= 0) return;
            float scaleX = t.width / 256f;
            float scaleY = t.height / 512f;
            int acx = Mathf.RoundToInt(cx * scaleX);
            int acy = Mathf.RoundToInt(cy * scaleY);
            int arx = Mathf.RoundToInt(rx * scaleX);
            int ary = Mathf.RoundToInt(ry * scaleY);

            for (var y = acy - ary; y <= acy + ary; y++)
                for (var x = acx - arx; x <= acx + arx; x++)
                {
                    float nx = (x - acx) / (float)arx, ny = (y - acy) / (float)ary;
                    if (nx*nx + ny*ny <= 1.01f) BlendPixel(t, x, y, c);
                }
        }

        private static void FillRect(Texture2D t, int x, int y, int w, int h, Color c)
        {
            float scaleX = t.width / 256f;
            float scaleY = t.height / 512f;
            int ax = Mathf.RoundToInt(x * scaleX);
            int ay = Mathf.RoundToInt(y * scaleY);
            int aw = Mathf.RoundToInt(w * scaleX);
            int ah = Mathf.RoundToInt(h * scaleY);

            for (var py = ay; py < ay + ah; py++)
                for (var px = ax; px < ax + aw; px++)
                    BlendPixel(t, px, py, c);
        }

        private static void FillRectActual(Texture2D t, int x, int y, int w, int h, Color c)
        {
            for (var py = y; py < y + h; py++)
                for (var px = x; px < x + w; px++)
                    BlendPixel(t, px, py, c);
        }

        private static void BlendPixel(Texture2D t, int x, int y, Color s)
        {
            if (x < 0 || x >= t.width || y < 0 || y >= t.height || s.a <= 0f) return;
            if (s.a >= 1f) { t.SetPixel(x, y, s); return; }
            var d = t.GetPixel(x, y);
            var a = s.a + d.a * (1f - s.a);
            if (a < 0.001f) return;
            t.SetPixel(x, y, new Color(
                (s.r*s.a + d.r*d.a*(1f-s.a)) / a,
                (s.g*s.a + d.g*d.a*(1f-s.a)) / a,
                (s.b*s.a + d.b*d.a*(1f-s.a)) / a, a));
        }
    }
}

