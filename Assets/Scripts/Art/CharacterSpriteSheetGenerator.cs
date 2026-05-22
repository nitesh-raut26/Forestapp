using System.Collections.Generic;
using UnityEngine;

namespace ForestFriendsQuest
{
    // Generates 8-frame animated sprite sheets (512×64) for all 6 characters.
    // Each 64×64 frame covers one CreatureEmotion state.
    // Frames are lazily generated and cached — call GetSpriteSheet / GetEmotionFrame at runtime.
    public static class CharacterSpriteSheetGenerator
    {
        private const int FrameSize  = 64;
        private const int FrameCount = 8;
        private const int SheetW     = FrameSize * FrameCount; // 512

        private static readonly CreatureEmotion[] EmotionOrder =
        {
            CreatureEmotion.Idle, CreatureEmotion.Happy, CreatureEmotion.Excited,
            CreatureEmotion.Curious, CreatureEmotion.Proud, CreatureEmotion.Playful,
            CreatureEmotion.Shy, CreatureEmotion.Sleepy,
        };

        private static readonly Dictionary<string, Texture2D> _sheets = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite[]>  _frames = new Dictionary<string, Sprite[]>();

        // ─── Public API ───────────────────────────────────────────────────────────

        // Full 512×64 sprite sheet for the character — all 8 emotion frames side by side.
        public static Texture2D GetSpriteSheet(string characterId)
        {
            if (!_sheets.TryGetValue(characterId, out var t))
                _sheets[characterId] = t = BuildSheet(characterId);
            return t;
        }

        // Single 64×64 sprite for the requested emotion.
        public static Sprite GetEmotionFrame(string characterId, CreatureEmotion emotion)
        {
            var frames = GetAllFrames(characterId);
            var idx    = System.Array.IndexOf(EmotionOrder, emotion);
            return frames[idx < 0 ? 0 : Mathf.Clamp(idx, 0, frames.Length - 1)];
        }

        // All 8 frames in EmotionOrder (Idle → Sleepy).
        public static Sprite[] GetAllFrames(string characterId)
        {
            if (!_frames.TryGetValue(characterId, out var arr))
            {
                var sheet = GetSpriteSheet(characterId);
                arr = new Sprite[FrameCount];
                for (var i = 0; i < FrameCount; i++)
                    arr[i] = Sprite.Create(sheet,
                        new Rect(i * FrameSize, 0, FrameSize, FrameSize),
                        new Vector2(0.5f, 0.5f), FrameSize);
                _frames[characterId] = arr;
            }
            return arr;
        }

        // ─── Sheet Builder ────────────────────────────────────────────────────────

        private static Texture2D BuildSheet(string id)
        {
            var tex = new Texture2D(SheetW, FrameSize, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Point };
            tex.SetPixels(new Color[SheetW * FrameSize]); // clear to transparent

            for (var e = 0; e < FrameCount; e++)
            {
                var em  = EmotionOrder[e];
                var er  = EyeRadius(em);
                var sq  = em == CreatureEmotion.Sleepy || em == CreatureEmotion.Shy;
                var bl  = em == CreatureEmotion.Playful || em == CreatureEmotion.Shy;
                var ey  = (em == CreatureEmotion.Shy || em == CreatureEmotion.Sleepy) ? -1 : 0;
                var by  = em == CreatureEmotion.Excited ? 2 : (em == CreatureEmotion.Proud ? 1 : 0);
                var ox  = e * FrameSize;

                switch (id)
                {
                    case "pip":  DrawFox(tex,     ox, er, sq, bl, ey, by);      break;
                    case "mimi": DrawBird(tex,    ox, er, sq, bl, ey, by, em);  break;
                    case "tomo": DrawTurtle(tex,  ox, er, sq, bl, ey, em);      break;
                    case "luma": DrawFirefly(tex, ox, er, sq, bl, ey, by, em);  break;
                    case "nori": DrawDeer(tex,    ox, er, sq, bl, ey, em);      break;
                    case "sol":  DrawOwl(tex,     ox, er, sq, bl, ey, by);      break;
                }
            }

            tex.Apply();
            return tex;
        }

        private static int EyeRadius(CreatureEmotion em) => em switch
        {
            CreatureEmotion.Excited => 4,
            CreatureEmotion.Happy   => 3,
            CreatureEmotion.Proud   => 3,
            CreatureEmotion.Playful => 3,
            _                       => 3,
        };

        // ─── Character Drawers ─────────────────────────────────────────────────────
        // Coordinate system: y=0 at bottom, y=63 at top (Unity texture space).
        // Characters stand upright: legs near y=8, body at y=28, head at y=42.

        private static void DrawFox(Texture2D t, int ox, int er, bool sq, bool bl, int ey, int by)
        {
            var body  = Hex("#FFB36B");
            var light = Hex("#FFF2DC");
            var dark  = Hex("#3B2A1A");
            var nose  = Hex("#C44830");
            var gold  = Hex("#FFD700");
            var blC   = new Color(1f, 0.55f, 0.45f, 0.60f);

            int cx = ox + 32, cy = 28 + by;

            // Tail (bottom-left)
            E(t, ox + 13, cy - 7, 8, 6, body);
            E(t, ox + 10, cy - 9, 4, 3, light);

            // Body + chest patch
            E(t, cx, cy, 12, 10, body);
            E(t, cx, cy - 2, 6, 5, light);
            E(t, cx, cy + 1, 3, 3, gold); // scout badge

            // Ears
            Tri(t, cx-7,cy+12, cx-11,cy+18, cx-3,cy+18, body);
            Tri(t, cx+7,cy+12, cx+3, cy+18, cx+11,cy+18, body);
            Tri(t, cx-7,cy+13, cx-10,cy+17, cx-4,cy+17, dark);
            Tri(t, cx+7,cy+13, cx+4, cy+17, cx+10,cy+17, dark);

            // Head + muzzle
            E(t, cx, cy + 13, 10, 9, body);
            E(t, cx, cy +  8, 5,  4, light);
            E(t, cx, cy +  8, 2,  1, nose);

            // Eyes
            E(t, cx-4, cy+14+ey, er, sq?1:er, dark);
            E(t, cx+4, cy+14+ey, er, sq?1:er, dark);
            P(t, cx-3, cy+15+ey, Color.white);
            P(t, cx+5, cy+15+ey, Color.white);

            if (bl)
            {
                E(t, cx-5, cy+11+ey, 3, 2, blC);
                E(t, cx+5, cy+11+ey, 3, 2, blC);
            }

            // Legs + paws
            R(t, cx-8, cy-14, 3, 6, body);
            R(t, cx+5, cy-14, 3, 6, body);
            E(t, cx-7, cy-14, 3, 2, light);
            E(t, cx+6, cy-14, 3, 2, light);
        }

        private static void DrawBird(Texture2D t, int ox, int er, bool sq, bool bl, int ey, int by,
            CreatureEmotion em)
        {
            var body  = Hex("#F5D768");
            var light = Hex("#FFFBD0");
            var dark  = Hex("#3B2A1A");
            var beak  = Hex("#FFA500");
            var blC   = new Color(1f, 0.70f, 0.55f, 0.60f);

            int cx = ox + 32, cy = 28 + by;

            // Tail feathers
            Tri(t, cx, cy-10, cx-8, cy-4, cx+8, cy-4, body);

            // Wings (raised when happy/excited)
            int wy = (em == CreatureEmotion.Excited || em == CreatureEmotion.Happy) ? cy + 5 : cy;
            E(t, cx-13, wy, 8, 12, light);
            E(t, cx+13, wy, 8, 12, light);
            E(t, cx-13, wy-2, 4, 6, new Color(body.r, body.g, body.b, 0.70f));
            E(t, cx+13, wy-2, 4, 6, new Color(body.r, body.g, body.b, 0.70f));

            // Body + belly
            E(t, cx, cy,     10, 12, body);
            E(t, cx, cy - 2,  5,  7, light);

            // Music note on body
            R(t, cx - 1, cy + 2, 2, 6, dark);
            E(t, cx - 2,  cy + 2, 3, 2, dark);

            // Head + crest
            E(t, cx, cy + 14, 9, 9, body);
            Tri(t, cx, cy+22, cx-3, cy+18, cx+3, cy+18, dark);

            // Beak
            Tri(t, cx-3, cy+11, cx+3, cy+11, cx, cy+8, beak);

            // Eyes
            E(t, cx-3, cy+15+ey, er, sq?1:er, dark);
            E(t, cx+3, cy+15+ey, er, sq?1:er, dark);
            P(t, cx-2, cy+16+ey, Color.white);
            P(t, cx+4, cy+16+ey, Color.white);

            if (bl)
            {
                E(t, cx-4, cy+12+ey, 2, 2, blC);
                E(t, cx+4, cy+12+ey, 2, 2, blC);
            }

            // Feet
            P(t, cx-4, cy-12, dark); P(t, cx-3, cy-12, dark);
            P(t, cx+3, cy-12, dark); P(t, cx+4, cy-12, dark);
        }

        private static void DrawTurtle(Texture2D t, int ox, int er, bool sq, bool bl, int ey,
            CreatureEmotion em)
        {
            var shell = Hex("#5DA444");
            var skin  = Hex("#8AD1A8");
            var dark  = Hex("#1A3B2A");
            var blC   = new Color(0.60f, 0.90f, 0.55f, 0.60f);

            int cx = ox + 32, cy = 26;

            // Shell + pattern
            E(t, cx, cy, 14, 11, shell);
            E(t, cx,   cy,   3, 3, dark);
            E(t, cx-6, cy+2, 2, 2, dark);
            E(t, cx+6, cy+2, 2, 2, dark);
            E(t, cx-3, cy-5, 2, 2, dark);
            E(t, cx+3, cy-5, 2, 2, dark);

            // Underbelly
            E(t, cx, cy - 2, 8, 5, skin);

            // Feet
            E(t, cx-14, cy-2, 3, 2, skin);
            E(t, cx+14, cy-2, 3, 2, skin);
            E(t, cx-11, cy-8, 3, 2, skin);
            E(t, cx+11, cy-8, 3, 2, skin);

            // Head (retracted when sleepy)
            int headY = em == CreatureEmotion.Sleepy ? cy + 4 : cy + 14;
            E(t, cx, headY, 7, 7, skin);
            E(t, cx, headY - 3, 3, 2, new Color(0.60f, 0.85f, 0.62f));

            // Eyes
            E(t, cx-3, headY+2+ey, er, sq?1:er, dark);
            E(t, cx+3, headY+2+ey, er, sq?1:er, dark);
            P(t, cx-2, headY+3+ey, Color.white);
            P(t, cx+4, headY+3+ey, Color.white);

            if (bl)
            {
                E(t, cx-5, headY+ey, 2, 2, blC);
                E(t, cx+5, headY+ey, 2, 2, blC);
            }
        }

        private static void DrawFirefly(Texture2D t, int ox, int er, bool sq, bool bl, int ey, int by,
            CreatureEmotion em)
        {
            var body  = Hex("#89E5F7");
            var glow  = Hex("#C8FF80");
            var wing  = new Color(0.75f, 0.95f, 1f, 0.55f);
            var dark  = Hex("#0A2030");
            var blC   = new Color(0.75f, 1f, 0.85f, 0.60f);

            int cx = ox + 32, cy = 28 + by;
            int gs = em == CreatureEmotion.Excited ? 7 : em == CreatureEmotion.Sleepy ? 3 : 5;

            // Wings
            E(t, cx-11, cy+4, 8,  5, wing);
            E(t, cx+11, cy+4, 8,  5, wing);
            E(t, cx-10, cy,   6,  4, wing);
            E(t, cx+10, cy,   6,  4, wing);

            // Glow halo
            E(t, cx, cy - 4, 11, 8, new Color(glow.r, glow.g, glow.b, 0.25f));

            // Body (thorax + bioluminescent abdomen)
            E(t, cx, cy,     6, 9, body);
            E(t, cx, cy - 4, 5, gs, glow);

            // Head
            E(t, cx, cy + 12, 6, 6, body);

            // Antennae + glow tips
            L(t, cx-2, cy+15, cx-5, cy+20, dark);
            L(t, cx+2, cy+15, cx+5, cy+20, dark);
            E(t, cx-5, cy+20, 2, 2, glow);
            E(t, cx+5, cy+20, 2, 2, glow);

            // Eyes
            E(t, cx-2, cy+12+ey, er, sq?1:er, dark);
            E(t, cx+2, cy+12+ey, er, sq?1:er, dark);
            P(t, cx-1, cy+13+ey, new Color(0.5f, 0.8f, 1f));
            P(t, cx+3, cy+13+ey, new Color(0.5f, 0.8f, 1f));

            if (bl)
            {
                E(t, cx-4, cy+10+ey, 2, 2, blC);
                E(t, cx+4, cy+10+ey, 2, 2, blC);
            }
        }

        private static void DrawDeer(Texture2D t, int ox, int er, bool sq, bool bl, int ey,
            CreatureEmotion em)
        {
            var body  = Hex("#B8E8C8");
            var light = Hex("#E8F8EE");
            var dark  = Hex("#1A3B2A");
            var nose  = Hex("#6B3018");
            var blC   = new Color(0.80f, 1f, 0.85f, 0.60f);

            int cx = ox + 32, cy = 26;

            // Body + belly + spots
            E(t, cx, cy, 12, 10, body);
            E(t, cx, cy - 1, 6, 5, light);
            E(t, cx-4, cy+3, 2, 2, light);
            E(t, cx+4, cy+4, 2, 2, light);
            E(t, cx-1, cy-4, 2, 2, light);

            // Legs
            R(t, cx-8, cy-14, 2, 8, dark);
            R(t, cx-5, cy-13, 2, 7, body);
            R(t, cx+3, cy-13, 2, 7, body);
            R(t, cx+6, cy-14, 2, 8, dark);

            // Neck + ears + head
            E(t, cx, cy + 9, 4, 5, body);
            E(t, cx-9, cy+15, 3, 6, body);
            E(t, cx+9, cy+15, 3, 6, body);
            E(t, cx-9, cy+15, 2, 4, light);
            E(t, cx+9, cy+15, 2, 4, light);
            E(t, cx, cy + 16, 8, 8, body);

            // Muzzle + nose
            E(t, cx, cy+12, 4, 3, light);
            E(t, cx, cy+12, 2, 1, nose);

            // Antlers (hidden when shy/sleepy)
            if (em != CreatureEmotion.Sleepy && em != CreatureEmotion.Shy)
            {
                L(t, cx-5, cy+20, cx-8,  cy+26, dark);
                L(t, cx-8, cy+26, cx-11, cy+29, dark);
                L(t, cx-8, cy+26, cx-6,  cy+29, dark);
                L(t, cx+5, cy+20, cx+8,  cy+26, dark);
                L(t, cx+8, cy+26, cx+11, cy+29, dark);
                L(t, cx+8, cy+26, cx+6,  cy+29, dark);
            }

            // Eyes
            E(t, cx-3, cy+17+ey, er, sq?1:er, dark);
            E(t, cx+3, cy+17+ey, er, sq?1:er, dark);
            P(t, cx-2, cy+18+ey, Color.white);
            P(t, cx+4, cy+18+ey, Color.white);

            if (bl)
            {
                E(t, cx-5, cy+14+ey, 2, 2, blC);
                E(t, cx+5, cy+14+ey, 2, 2, blC);
            }
        }

        private static void DrawOwl(Texture2D t, int ox, int er, bool sq, bool bl, int ey, int by)
        {
            var body  = Hex("#C5A3E8");
            var light = Hex("#EAD8FF");
            var dark  = Hex("#3B2A5A");
            var iris  = Hex("#D4A017");
            var pupil = Hex("#1A0A30");
            var beak  = Hex("#E8B040");
            var runeC = new Color(0.60f, 0.35f, 0.90f, 0.70f);
            var blC   = new Color(0.85f, 0.75f, 1f, 0.60f);

            int cx = ox + 32, cy = 26 + by;

            // Wings + feather lines
            E(t, cx-13, cy, 7, 12, body);
            E(t, cx+13, cy, 7, 12, body);
            L(t, cx-13, cy-2, cx-17, cy+4, dark);
            L(t, cx+13, cy-2, cx+17, cy+4, dark);

            // Body + belly
            E(t, cx, cy,     11, 13, body);
            E(t, cx, cy - 3,  6,  9, light);
            E(t, cx, cy - 1,  3,  4, runeC); // rune glow

            // Head + facial disc
            E(t, cx, cy + 16, 11, 11, body);
            E(t, cx, cy + 15,  8,  8, light);

            // Ear tufts
            Tri(t, cx-8,cy+22, cx-10,cy+28, cx-6,cy+28, dark);
            Tri(t, cx+8,cy+22, cx+6, cy+28, cx+10,cy+28, dark);

            // Large amber eyes with outline rings
            int fr = er + 1;
            E(t, cx-4, cy+16+ey, fr, sq?1:fr, iris);
            E(t, cx+4, cy+16+ey, fr, sq?1:fr, iris);
            if (!sq)
            {
                E(t, cx-4, cy+16+ey, Mathf.Max(1,er-1), Mathf.Max(1,er-1), pupil);
                E(t, cx+4, cy+16+ey, Mathf.Max(1,er-1), Mathf.Max(1,er-1), pupil);
            }
            EO(t, cx-4, cy+16+ey, fr+1, fr+1, dark);
            EO(t, cx+4, cy+16+ey, fr+1, fr+1, dark);
            P(t, cx-3, cy+15+ey, Color.white);
            P(t, cx+5, cy+15+ey, Color.white);

            // Hooked beak
            Tri(t, cx-2, cy+12, cx+2, cy+12, cx, cy+9, beak);
            P(t, cx, cy+9, new Color(0.70f, 0.50f, 0.20f));

            if (bl)
            {
                E(t, cx-5, cy+13+ey, 2, 2, blC);
                E(t, cx+5, cy+13+ey, 2, 2, blC);
            }

            // Talons
            R(t, cx-6, cy-12, 2, 5, dark);
            R(t, cx+4, cy-12, 2, 5, dark);
            P(t, cx-7, cy-12, dark); P(t, cx-5, cy-12, dark);
            P(t, cx+3, cy-12, dark); P(t, cx+5, cy-12, dark);
        }

        // ─── Drawing Primitives ───────────────────────────────────────────────────

        private static void E(Texture2D t, int cx, int cy, int rx, int ry, Color c)
        {
            if (rx <= 0 || ry <= 0) return;
            for (var y = cy - ry; y <= cy + ry; y++)
                for (var x = cx - rx; x <= cx + rx; x++)
                {
                    float nx = (x - cx) / (float)rx, ny = (y - cy) / (float)ry;
                    if (nx * nx + ny * ny <= 1.01f) Blend(t, x, y, c);
                }
        }

        private static void EO(Texture2D t, int cx, int cy, int rx, int ry, Color c)
        {
            for (var a = 0; a < 360; a += 4)
            {
                var rad = a * Mathf.Deg2Rad;
                Blend(t, Mathf.RoundToInt(cx + rx * Mathf.Cos(rad)),
                        Mathf.RoundToInt(cy + ry * Mathf.Sin(rad)), c);
            }
        }

        private static void R(Texture2D t, int x, int y, int w, int h, Color c)
        {
            for (var py = y; py < y + h; py++)
                for (var px = x; px < x + w; px++)
                    Blend(t, px, py, c);
        }

        private static void Tri(Texture2D t, int x0,int y0,int x1,int y1,int x2,int y2, Color c)
        {
            var mnX = Mathf.Min(x0, Mathf.Min(x1, x2));
            var mxX = Mathf.Max(x0, Mathf.Max(x1, x2));
            var mnY = Mathf.Min(y0, Mathf.Min(y1, y2));
            var mxY = Mathf.Max(y0, Mathf.Max(y1, y2));
            for (var py = mnY; py <= mxY; py++)
                for (var px = mnX; px <= mxX; px++)
                    if (InTri(px, py, x0, y0, x1, y1, x2, y2))
                        Blend(t, px, py, c);
        }

        private static bool InTri(int px,int py,int x0,int y0,int x1,int y1,int x2,int y2)
        {
            float d1=TS(px,py,x0,y0,x1,y1), d2=TS(px,py,x1,y1,x2,y2), d3=TS(px,py,x2,y2,x0,y0);
            return !((d1<0||d2<0||d3<0) && (d1>0||d2>0||d3>0));
        }
        private static float TS(int px,int py,int ax,int ay,int bx,int by) =>
            (px-bx)*(ay-by)-(ax-bx)*(py-by);

        private static void L(Texture2D t, int x0, int y0, int x1, int y1, Color c)
        {
            int dx=Mathf.Abs(x1-x0), sx=x0<x1?1:-1;
            int dy=-Mathf.Abs(y1-y0), sy=y0<y1?1:-1, err=dx+dy;
            while (true)
            {
                Blend(t, x0, y0, c);
                if (x0==x1 && y0==y1) break;
                int e2=2*err;
                if (e2>=dy){err+=dy; x0+=sx;}
                if (e2<=dx){err+=dx; y0+=sy;}
            }
        }

        private static void P(Texture2D t, int x, int y, Color c) => Blend(t, x, y, c);

        private static void Blend(Texture2D t, int x, int y, Color s)
        {
            if (x<0||x>=t.width||y<0||y>=t.height||s.a<=0f) return;
            if (s.a>=1f) { t.SetPixel(x,y,s); return; }
            var d = t.GetPixel(x, y);
            var a = s.a + d.a*(1f-s.a);
            if (a < 0.001f) return;
            t.SetPixel(x, y, new Color(
                (s.r*s.a+d.r*d.a*(1f-s.a))/a,
                (s.g*s.a+d.g*d.a*(1f-s.a))/a,
                (s.b*s.a+d.b*d.a*(1f-s.a))/a, a));
        }

        private static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c);
            return c;
        }
    }
}
