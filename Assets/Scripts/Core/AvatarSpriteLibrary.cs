using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Generates a 128×128 Texture2D portrait sprite for each of the 6 guide characters.
    /// All art is procedural — no external PNG files required.
    ///
    /// Access via AvatarSpriteLibrary.Instance.GetSprite("pip") etc.
    /// Sprites are lazy-generated and cached on first access.
    /// </summary>
    public static class AvatarSpriteLibrary
    {
        private const int Size = 512;

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        // ─── Public API ───────────────────────────────────────────────────────────

        public static Sprite GetSprite(string characterId)
        {
            if (_cache.TryGetValue(characterId, out var cached))
                return cached;

            var sprite = characterId switch
            {
                "pip"  => GenerateFoxSprite(HexColor("#FFB36B"), HexColor("#FFF2DC"), HexColor("#3B2A1A")),
                "mimi" => GenerateBirdSprite(HexColor("#F5D768"), HexColor("#FFF8D0"), HexColor("#3B2A1A")),
                "tomo" => GenerateTurtleSprite(HexColor("#8AD1A8"), HexColor("#C8EDDA"), HexColor("#1A3B2A")),
                "luma" => GenerateFireflySprite(HexColor("#89E5F7"), HexColor("#D6F8FF"), HexColor("#0A2030")),
                "nori" => GenerateDeerSprite(HexColor("#B8E8C8"), HexColor("#E8F8EE"), HexColor("#1A3B2A")),
                "sol"  => GenerateOwlSprite(HexColor("#C5A3E8"), HexColor("#EAD8FF"), HexColor("#1A0A30")),
                _      => GenerateFoxSprite(HexColor("#FFB36B"), HexColor("#FFF2DC"), HexColor("#3B2A1A")),
            };

            _cache[characterId] = sprite;
            return sprite;
        }

        /// <summary>Returns all 6 character sprites in canonical order.</summary>
        public static Sprite[] GetAllSprites()
        {
            var ids = new[] { "pip", "mimi", "tomo", "luma", "nori", "sol" };
            var sprites = new Sprite[ids.Length];
            for (var i = 0; i < ids.Length; i++)
                sprites[i] = GetSprite(ids[i]);
            return sprites;
        }

        // ─── Sprite Generators ────────────────────────────────────────────────────

        // Pip — warm orange fox scout
        private static Sprite GenerateFoxSprite(Color body, Color light, Color dark)
        {
            var tex = NewTex();
            Fill(tex, new Color(0.12f, 0.24f, 0.18f, 0f));

            // Body
            FillEllipse(tex, 64, 56, 36, 40, body);
            // Chest patch
            FillEllipse(tex, 64, 44, 16, 22, light);
            // Head
            FillEllipse(tex, 64, 88, 34, 30, body);
            // Muzzle
            FillEllipse(tex, 64, 76, 14, 10, light);
            // Ears
            FillTriangle(tex, 46, 104, 38, 122, 54, 122, body);
            FillTriangle(tex, 82, 104, 74, 122, 90, 122, body);
            FillTriangle(tex, 46, 106, 40, 118, 52, 118, light);
            FillTriangle(tex, 82, 106, 76, 118, 88, 118, light);
            // Eyes
            FillEllipse(tex, 54, 92, 5, 5, dark);
            FillEllipse(tex, 74, 92, 5, 5, dark);
            // Nose
            FillEllipse(tex, 64, 76, 4, 3, dark);
            // Tail
            FillEllipse(tex, 32, 36, 20, 20, body);
            FillEllipse(tex, 26, 32, 8, 8, light);
            // Legs
            FillRect(tex, 48, 14, 18, 26, body);
            FillRect(tex, 70, 14, 18, 26, body);
            // Scout badge on chest
            FillEllipse(tex, 64, 52, 8, 8, HexColor("#FFD700"));
            FillEllipse(tex, 64, 52, 3, 3, body);

            tex.Apply();
            return ToSprite(tex);
        }

        // Mimi — golden yellow song bird
        private static Sprite GenerateBirdSprite(Color body, Color light, Color dark)
        {
            var tex = NewTex();
            Fill(tex, new Color(0f, 0f, 0f, 0f));

            // Wings
            FillEllipse(tex, 36, 68, 24, 32, light);
            FillEllipse(tex, 92, 68, 24, 32, light);
            // Body
            FillEllipse(tex, 64, 58, 32, 38, body);
            // Tail feathers
            FillTriangle(tex, 64, 18, 48, 28, 80, 28, body);
            // Head
            FillEllipse(tex, 64, 90, 28, 26, body);
            // Beak
            FillTriangle(tex, 58, 82, 70, 82, 64, 70, HexColor("#FFA500"));
            // Eyes
            FillEllipse(tex, 54, 94, 6, 6, dark);
            FillEllipse(tex, 74, 94, 6, 6, dark);
            FillEllipse(tex, 56, 96, 2, 2, light); // eye shine
            FillEllipse(tex, 76, 96, 2, 2, light);
            // Wing markings
            FillEllipse(tex, 36, 72, 12, 16, new Color(body.r, body.g, body.b, 0.6f));
            FillEllipse(tex, 92, 72, 12, 16, new Color(body.r, body.g, body.b, 0.6f));
            // Music note on body
            FillRect(tex, 58, 62, 4, 14, dark);
            FillEllipse(tex, 56, 62, 6, 5, dark);
            FillRect(tex, 62, 70, 10, 3, dark);

            tex.Apply();
            return ToSprite(tex);
        }

        // Tomo — green turtle thinker
        private static Sprite GenerateTurtleSprite(Color shell, Color light, Color dark)
        {
            var tex = NewTex();
            Fill(tex, new Color(0f, 0f, 0f, 0f));

            // Shell (dome)
            FillEllipse(tex, 64, 58, 46, 40, shell);
            // Shell pattern hex segments
            FillEllipse(tex, 64, 62, 22, 20, new Color(shell.r * 0.8f, shell.g * 0.8f, shell.b * 0.8f, 1f));
            FillEllipse(tex, 64, 62, 10, 10, light);
            DrawLine(tex, 42, 62, 64, 42, new Color(0f, 0f, 0f, 0.3f), 2);
            DrawLine(tex, 86, 62, 64, 42, new Color(0f, 0f, 0f, 0.3f), 2);
            DrawLine(tex, 42, 62, 64, 82, new Color(0f, 0f, 0f, 0.3f), 2);
            DrawLine(tex, 86, 62, 64, 82, new Color(0f, 0f, 0f, 0.3f), 2);
            // Head
            FillEllipse(tex, 64, 92, 22, 20, light);
            // Eyes
            FillEllipse(tex, 56, 96, 5, 5, dark);
            FillEllipse(tex, 72, 96, 5, 5, dark);
            FillEllipse(tex, 57, 98, 2, 2, Color.white);
            FillEllipse(tex, 73, 98, 2, 2, Color.white);
            // Smile
            DrawArc(tex, 64, 88, 8, 0, Mathf.PI, shell, 2);
            // Legs
            FillEllipse(tex, 28, 52, 14, 10, light);
            FillEllipse(tex, 100, 52, 14, 10, light);
            FillEllipse(tex, 40, 24, 12, 10, light);
            FillEllipse(tex, 88, 24, 12, 10, light);
            // Tail
            FillEllipse(tex, 64, 20, 8, 10, light);

            tex.Apply();
            return ToSprite(tex);
        }

        // Luma — cyan firefly spark
        private static Sprite GenerateFireflySprite(Color glow, Color light, Color dark)
        {
            var tex = NewTex();
            Fill(tex, new Color(0f, 0f, 0f, 0f));

            // Glow aura (largest, most transparent)
            FillEllipse(tex, 64, 64, 54, 54, new Color(glow.r, glow.g, glow.b, 0.18f));
            FillEllipse(tex, 64, 64, 44, 44, new Color(glow.r, glow.g, glow.b, 0.22f));

            // Wings (translucent)
            FillEllipse(tex, 34, 72, 26, 18, new Color(1f, 1f, 1f, 0.4f));
            FillEllipse(tex, 94, 72, 26, 18, new Color(1f, 1f, 1f, 0.4f));
            FillEllipse(tex, 30, 84, 18, 12, new Color(1f, 1f, 1f, 0.25f));
            FillEllipse(tex, 98, 84, 18, 12, new Color(1f, 1f, 1f, 0.25f));

            // Body (dark with glow lantern)
            FillEllipse(tex, 64, 64, 20, 34, dark);
            // Lantern glow orb at bottom of body
            FillEllipse(tex, 64, 42, 18, 18, glow);
            FillEllipse(tex, 64, 42, 10, 10, light);
            FillEllipse(tex, 64, 42, 5, 5, Color.white);

            // Head
            FillEllipse(tex, 64, 90, 22, 20, new Color(0.3f, 0.85f, 0.95f, 1f));
            // Eyes (large, round, shiny)
            FillEllipse(tex, 56, 94, 7, 7, dark);
            FillEllipse(tex, 72, 94, 7, 7, dark);
            FillEllipse(tex, 58, 96, 3, 3, light);
            FillEllipse(tex, 74, 96, 3, 3, light);

            // Sparkle dots radiating from lantern
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI * 2f / 8f;
                var sx = (int)(64 + Mathf.Cos(angle) * 28f);
                var sy = (int)(42 + Mathf.Sin(angle) * 28f);
                FillEllipse(tex, sx, sy, 3, 3, new Color(glow.r, glow.g, glow.b, 0.85f));
            }

            tex.Apply();
            return ToSprite(tex);
        }

        // Nori — graceful deer guardian with antlers
        private static Sprite GenerateDeerSprite(Color body, Color light, Color dark)
        {
            var tex = NewTex();
            Fill(tex, new Color(0f, 0f, 0f, 0f));

            // Legs (back pair)
            FillRect(tex, 44, 10, 14, 36, body);
            FillRect(tex, 70, 10, 14, 36, body);
            FillEllipse(tex, 51, 10, 8, 6, dark);  // hooves
            FillEllipse(tex, 77, 10, 8, 6, dark);

            // Body
            FillEllipse(tex, 64, 52, 42, 34, body);
            // Belly patch
            FillEllipse(tex, 64, 46, 22, 20, light);
            // Spots
            FillEllipse(tex, 50, 58, 5, 5, light);
            FillEllipse(tex, 68, 62, 4, 4, light);
            FillEllipse(tex, 78, 54, 5, 5, light);

            // Neck
            FillRect(tex, 56, 70, 16, 24, body);

            // Head
            FillEllipse(tex, 64, 96, 28, 24, body);
            // Muzzle
            FillEllipse(tex, 64, 88, 14, 10, light);
            FillEllipse(tex, 64, 86, 5, 4, dark); // nose

            // Ears
            FillEllipse(tex, 42, 104, 10, 16, body);
            FillEllipse(tex, 86, 104, 10, 16, body);
            FillEllipse(tex, 42, 104, 6, 10, light);
            FillEllipse(tex, 86, 104, 6, 10, light);

            // Eyes
            FillEllipse(tex, 54, 98, 6, 6, dark);
            FillEllipse(tex, 74, 98, 6, 6, dark);
            FillEllipse(tex, 56, 100, 2, 2, Color.white);
            FillEllipse(tex, 76, 100, 2, 2, Color.white);

            // Antlers
            DrawLine(tex, 46, 108, 34, 122, dark, 3);
            DrawLine(tex, 34, 122, 24, 120, dark, 2);
            DrawLine(tex, 34, 118, 28, 126, dark, 2);
            DrawLine(tex, 82, 108, 94, 122, dark, 3);
            DrawLine(tex, 94, 122, 104, 120, dark, 2);
            DrawLine(tex, 94, 118, 100, 126, dark, 2);

            tex.Apply();
            return ToSprite(tex);
        }

        // Sol — ancient purple-toned druid owl
        private static Sprite GenerateOwlSprite(Color body, Color light, Color dark)
        {
            var tex = NewTex();
            Fill(tex, new Color(0f, 0f, 0f, 0f));

            // Magical aura around body
            FillEllipse(tex, 64, 54, 50, 50, new Color(body.r, body.g, body.b, 0.18f));

            // Wing feathers (left)
            FillEllipse(tex, 26, 62, 24, 40, new Color(body.r * 0.8f, body.g * 0.8f, body.b * 0.8f, 1f));
            FillEllipse(tex, 22, 70, 16, 28, light);
            // Wing feathers (right)
            FillEllipse(tex, 102, 62, 24, 40, new Color(body.r * 0.8f, body.g * 0.8f, body.b * 0.8f, 1f));
            FillEllipse(tex, 106, 70, 16, 28, light);

            // Body
            FillEllipse(tex, 64, 54, 38, 46, body);
            // Belly feather pattern
            FillEllipse(tex, 64, 46, 22, 30, light);
            DrawLine(tex, 54, 58, 64, 30, new Color(body.r, body.g, body.b, 0.5f), 2);
            DrawLine(tex, 64, 58, 74, 30, new Color(body.r, body.g, body.b, 0.5f), 2);
            DrawLine(tex, 59, 52, 64, 30, new Color(body.r, body.g, body.b, 0.35f), 1);
            DrawLine(tex, 69, 52, 64, 30, new Color(body.r, body.g, body.b, 0.35f), 1);

            // Talons
            FillEllipse(tex, 50, 12, 12, 8, dark);
            FillEllipse(tex, 78, 12, 12, 8, dark);
            DrawLine(tex, 44, 8, 50, 16, dark, 2);
            DrawLine(tex, 56, 8, 50, 16, dark, 2);
            DrawLine(tex, 72, 8, 78, 16, dark, 2);
            DrawLine(tex, 84, 8, 78, 16, dark, 2);

            // Head (large, round)
            FillEllipse(tex, 64, 96, 38, 36, body);
            // Facial disc
            FillEllipse(tex, 64, 94, 28, 26, light);

            // LARGE iconic owl eyes
            FillEllipse(tex, 52, 98, 11, 11, HexColor("#EED020")); // iris
            FillEllipse(tex, 76, 98, 11, 11, HexColor("#EED020"));
            FillEllipse(tex, 52, 98, 7, 7, dark);  // pupil
            FillEllipse(tex, 76, 98, 7, 7, dark);
            FillEllipse(tex, 54, 100, 3, 3, Color.white); // shine
            FillEllipse(tex, 78, 100, 3, 3, Color.white);
            // Eye ring glow
            FillEllipse(tex, 52, 98, 14, 14, new Color(body.r, body.g, body.b, 0.35f));
            FillEllipse(tex, 76, 98, 14, 14, new Color(body.r, body.g, body.b, 0.35f));

            // Hooked beak
            FillTriangle(tex, 58, 86, 70, 86, 64, 76, HexColor("#B8860B"));
            FillRect(tex, 61, 74, 6, 5, HexColor("#8B6914")); // hook

            // Ear tufts
            FillTriangle(tex, 46, 110, 40, 122, 52, 122, dark);
            FillTriangle(tex, 82, 110, 76, 122, 88, 122, dark);

            // Rune glow on chest
            FillEllipse(tex, 64, 52, 12, 12, new Color(0.8f, 0.6f, 1f, 0.55f));
            FillEllipse(tex, 64, 52, 6, 6, Color.white);

            tex.Apply();
            return ToSprite(tex);
        }

        // ─── Texture Drawing Primitives ───────────────────────────────────────────

        private static Texture2D NewTex()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        private static Sprite ToSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }

        private static void Fill(Texture2D tex, Color c)
        {
            var pixels = new Color[Size * Size];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = c;
            tex.SetPixels(pixels);
        }

        private static void FillRect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            float scale = Size / 128f;
            int ax = Mathf.RoundToInt(x * scale);
            int ay = Mathf.RoundToInt(y * scale);
            int aw = Mathf.RoundToInt(w * scale);
            int ah = Mathf.RoundToInt(h * scale);

            for (var py = ay; py < ay + ah; py++)
                for (var px = ax; px < ax + aw; px++)
                    if (px >= 0 && px < Size && py >= 0 && py < Size)
                        BlendPixel(tex, px, py, c);
        }

        private static void FillEllipseActual(Texture2D tex, int acx, int acy, int arx, int ary, Color c)
        {
            if (arx <= 0 || ary <= 0) return;
            for (var py = acy - ary; py <= acy + ary; py++)
            {
                for (var px = acx - arx; px <= acx + arx; px++)
                {
                    if (px < 0 || px >= Size || py < 0 || py >= Size) continue;
                    var dx = (float)(px - acx) / arx;
                    var dy = (float)(py - acy) / ary;
                    if (dx * dx + dy * dy <= 1f)
                        BlendPixel(tex, px, py, c);
                }
            }
        }

        private static void FillEllipse(Texture2D tex, int cx, int cy, int rx, int ry, Color c)
        {
            float scale = Size / 128f;
            FillEllipseActual(tex,
                Mathf.RoundToInt(cx * scale),
                Mathf.RoundToInt(cy * scale),
                Mathf.RoundToInt(rx * scale),
                Mathf.RoundToInt(ry * scale),
                c);
        }

        private static void FillTriangle(Texture2D tex, int x0, int y0, int x1, int y1, int x2, int y2, Color c)
        {
            float scale = Size / 128f;
            int ax0 = Mathf.RoundToInt(x0 * scale);
            int ay0 = Mathf.RoundToInt(y0 * scale);
            int ax1 = Mathf.RoundToInt(x1 * scale);
            int ay1 = Mathf.RoundToInt(y1 * scale);
            int ax2 = Mathf.RoundToInt(x2 * scale);
            int ay2 = Mathf.RoundToInt(y2 * scale);

            var minX = Mathf.Max(0, Mathf.Min(ax0, Mathf.Min(ax1, ax2)));
            var maxX = Mathf.Min(Size - 1, Mathf.Max(ax0, Mathf.Max(ax1, ax2)));
            var minY = Mathf.Max(0, Mathf.Min(ay0, Mathf.Min(ay1, ay2)));
            var maxY = Mathf.Min(Size - 1, Mathf.Max(ay0, Mathf.Max(ay1, ay2)));

            for (var py = minY; py <= maxY; py++)
            {
                for (var px = minX; px <= maxX; px++)
                {
                    if (InTriangle(px, py, ax0, ay0, ax1, ay1, ax2, ay2))
                        BlendPixel(tex, px, py, c);
                }
            }
        }

        private static bool InTriangle(int px, int py, int x0, int y0, int x1, int y1, int x2, int y2)
        {
            var d1 = Sign(px, py, x0, y0, x1, y1);
            var d2 = Sign(px, py, x1, y1, x2, y2);
            var d3 = Sign(px, py, x2, y2, x0, y0);
            var hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            var hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        private static float Sign(int px, int py, int x1, int y1, int x2, int y2)
            => (px - x2) * (y1 - y2) - (x1 - x2) * (py - y2);

        private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color c, int thickness = 1)
        {
            float scale = Size / 128f;
            int ax0 = Mathf.RoundToInt(x0 * scale);
            int ay0 = Mathf.RoundToInt(y0 * scale);
            int ax1 = Mathf.RoundToInt(x1 * scale);
            int ay1 = Mathf.RoundToInt(y1 * scale);
            int athickness = Mathf.Max(1, Mathf.RoundToInt(thickness * scale));

            var dx = Mathf.Abs(ax1 - ax0);
            var dy = Mathf.Abs(ay1 - ay0);
            var sx = ax0 < ax1 ? 1 : -1;
            var sy = ay0 < ay1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                FillEllipseActual(tex, ax0, ay0, athickness, athickness, c);
                if (ax0 == ax1 && ay0 == ay1) break;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; ax0 += sx; }
                if (e2 < dx)  { err += dx; ay0 += sy; }
            }
        }

        private static void DrawArc(Texture2D tex, int cx, int cy, int r, float startAngle, float endAngle, Color c, int thickness)
        {
            float scale = Size / 128f;
            var steps = Mathf.RoundToInt(Mathf.Max(16, r * 3) * scale);
            var prev  = true;
            int lx = 0, ly = 0;
            for (var i = 0; i <= steps; i++)
            {
                var t = startAngle + (endAngle - startAngle) * i / steps;
                var nx = (int)(cx + Mathf.Cos(t) * r);
                var ny = (int)(cy + Mathf.Sin(t) * r);
                if (!prev) DrawLine(tex, lx, ly, nx, ny, c, thickness);
                lx = nx; ly = ny; prev = false;
            }
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color src)
        {
            var dst = tex.GetPixel(x, y);
            var a   = src.a + dst.a * (1f - src.a);
            if (a < 0.001f) { tex.SetPixel(x, y, Color.clear); return; }
            var r = (src.r * src.a + dst.r * dst.a * (1f - src.a)) / a;
            var g = (src.g * src.a + dst.g * dst.a * (1f - src.a)) / a;
            var b = (src.b * src.a + dst.b * dst.a * (1f - src.a)) / a;
            tex.SetPixel(x, y, new Color(r, g, b, a));
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
