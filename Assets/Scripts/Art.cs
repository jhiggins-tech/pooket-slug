using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>Simple RGBA pixel buffer (y = 0 is the bottom row, like Texture2D).</summary>
    public sealed class PixelCanvas
    {
        public readonly int W, H;
        public readonly Color32[] Px;

        public PixelCanvas(int w, int h)
        {
            W = w;
            H = h;
            Px = new Color32[w * h];
        }

        public bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;
        public Color32 Get(int x, int y) => In(x, y) ? Px[y * W + x] : default;

        public void Set(int x, int y, Color32 c)
        {
            if (In(x, y)) Px[y * W + x] = c;
        }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Set(i, j, c);
        }

        public void Circle(float cx, float cy, float r, Color32 c)
        {
            int x0 = Mathf.FloorToInt(cx - r), x1 = Mathf.CeilToInt(cx + r);
            int y0 = Mathf.FloorToInt(cy - r), y1 = Mathf.CeilToInt(cy + r);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    if (dx * dx + dy * dy <= r * r) Set(x, y, c);
                }
        }

        /// <summary>Draws ASCII rows (top-down) with the top row landing on canvas row <paramref name="top"/> (counted from the top).</summary>
        public void Ascii(string[] rows, Dictionary<char, Color32> pal, int left, int top)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                string row = rows[r];
                int y = H - 1 - (top + r);
                for (int i = 0; i < row.Length; i++)
                {
                    char ch = row[i];
                    if (ch == '.' || ch == ' ') continue;
                    if (pal.TryGetValue(ch, out Color32 c)) Set(left + i, y, c);
                }
            }
        }

        /// <summary>Adds a 1px outline around opaque pixels (4-neighbourhood).</summary>
        public void Outline(Color32 c)
        {
            var src = (Color32[])Px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (src[y * W + x].a != 0) continue;
                    bool edge = (x > 0 && src[y * W + x - 1].a != 0) || (x < W - 1 && src[y * W + x + 1].a != 0) ||
                                (y > 0 && src[(y - 1) * W + x].a != 0) || (y < H - 1 && src[(y + 1) * W + x].a != 0);
                    if (edge) Px[y * W + x] = c;
                }
        }
    }

    /// <summary>The sprite set for one playable soldier (palette-swapped per player).</summary>
    public sealed class PlayerSkin
    {
        public Sprite TorsoForward, TorsoUp, TorsoDown, TorsoUpDiag, TorsoDownDiag, LegsIdle, LegsJump, LegsCrouch;
        public Sprite[] LegsRun;
    }

    /// <summary>Builds every sprite in the game procedurally at startup (no imported assets).</summary>
    public static class Art
    {
        public const float PPU = 16f;
        public static readonly Color32 OutlineColor = Hex(0x1c1420);

        /// <summary>[0] = player 1 (red bandana), [1] = player 2 (blue bandana).</summary>
        public static PlayerSkin[] PlayerSkins;
        /// <summary>tones2 in his Pooket Tabks colours (spike: <see cref="Spike.Tones2"/>).</summary>
        public static PlayerSkin Tones2Skin;
        public static Sprite[] SoldierLegsRun, RunnerLegsRun;
        public static Sprite SoldierLegsIdle, RunnerLegsJump;
        public static Sprite SoldierTorso, RunnerTorso;
        public static Sprite[] Drone, Explosion, Spark;
        public static Sprite Tank, Bullet, HeavyBullet, EnemyBullet, Shell, Grenade, Bomb, MuzzleFlash;
        static Sprite[] s_Projectiles, s_Pickups;
        public static Sprite CrateHeavy, CrateBombs, Medkit, OneUp, Flag, Pixel, Sky, Mountains, Ruins;
        public static Texture2D HeartTex, WhiteTex, DiscTex;

        static Dictionary<char, Color32> s_Pal;
        static readonly Dictionary<Sprite, Sprite> s_Flash = new Dictionary<Sprite, Sprite>();
        static readonly Dictionary<Sprite, Sprite> s_Holo = new Dictionary<Sprite, Sprite>();
        static bool s_Ready;

        public static Color32 Hex(uint rgb, byte a = 255) =>
            new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, a);

        public static void Init()
        {
            if (s_Ready) return;
            s_Ready = true;

            s_Pal = new Dictionary<char, Color32>
            {
                { 'k', OutlineColor }, { 'r', Hex(0xd04030) }, { 'R', Hex(0x902820) }, { 'h', Hex(0x6a4424) },
                { 's', Hex(0xf8c090) }, { 'S', Hex(0xd08c60) }, { 'e', Hex(0x201820) }, { 'c', Hex(0xe0d0a0) },
                { 'C', Hex(0xb0a070) }, { 'g', Hex(0x5c8c40) }, { 'G', Hex(0x3c6428) }, { 'b', Hex(0x4c3828) },
                { 'y', Hex(0xf0c840) }, { 'p', Hex(0xc09858) }, { 'P', Hex(0x8c6c3c) }, { 'w', Hex(0x4a3428) },
                { 'm', Hex(0xa8b0c0) }, { 'M', Hex(0x586078) }, { 'W', Hex(0xffffff) }, { 'o', Hex(0xf08830) },
                { 'O', Hex(0xc05020) }, { 'x', Hex(0xffe860) }, { 'd', Hex(0x707888) }, { 'D', Hex(0x454b5c) },
                { 't', Hex(0x8a9a4a) }, { 'T', Hex(0x5a6a30) }, { 'u', Hex(0x3a3a48) }, { 'z', Hex(0xc8d0e0) },
            };

            var soldier = Swap(new Dictionary<char, uint>
            {
                { 'r', 0x8a9a4a }, { 'R', 0x5a6a30 }, { 'h', 0x5a6a30 }, { 'c', 0x8a9a4a }, { 'C', 0x5a6a30 },
                { 'g', 0x5a6a30 }, { 'G', 0x3a3a48 }, { 'b', 0x3a3a48 }, { 'y', 0xffe860 },
                { 'p', 0x8a9a4a }, { 'P', 0x5a6a30 },
            });
            var runner = Swap(new Dictionary<char, uint>
            {
                { 'r', 0x7a2850 }, { 'R', 0x4a1830 }, { 'h', 0x2a1a14 }, { 'c', 0xb04848 }, { 'C', 0x803030 },
                { 'g', 0x803030 }, { 'G', 0x502020 }, { 'b', 0x3a3a48 }, { 'y', 0xc8d0e0 },
                { 'p', 0x5a4a6a }, { 'P', 0x3a2e48 }, { 'm', 0xe8ecf4 }, { 'M', 0x9098a8 },
            });

            var player2 = Swap(new Dictionary<char, uint>
            {
                { 'r', 0x3a7ad8 }, { 'R', 0x22508c }, { 'g', 0x3c7c8c }, { 'G', 0x285a68 },
            });

            Vector2 feet = new Vector2(0.5f, 0f);
            PlayerSkins = new[] { BuildSkin(s_Pal), BuildSkin(player2) };
            if (Spike.Tones2)
                Tones2Skin = BuildSkin(Swap(new Dictionary<char, uint>
                {
                    { 'r', 0xff7eb6 }, { 'R', 0xc0508a }, { 'g', 0xff8c42 }, { 'G', 0xc06428 }, { 'c', 0xff5a5f }, { 'C', 0xb83c40 },
                }));
            SoldierTorso = Part(ArtData.TorsoForward, ArtData.TorsoForwardTop, soldier);
            RunnerTorso = Part(ArtData.TorsoForward, ArtData.TorsoForwardTop, runner);

            string[][] run = { ArtData.LegsRun1, ArtData.LegsRun2, ArtData.LegsRun3, ArtData.LegsRun4 };
            SoldierLegsRun = new Sprite[4];
            RunnerLegsRun = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                SoldierLegsRun[i] = Part(run[i], ArtData.LegsTop, soldier);
                RunnerLegsRun[i] = Part(run[i], ArtData.LegsTop, runner);
            }
            SoldierLegsIdle = Part(ArtData.LegsIdle, ArtData.LegsTop, soldier);
            RunnerLegsJump = Part(ArtData.LegsJump, ArtData.LegsTop, runner);

            Drone = new[] { Simple(ArtData.Drone1, Center), Simple(ArtData.Drone2, Center) };
            Tank = Simple(ArtData.Tank, feet);
            Bullet = Simple(ArtData.Bullet, Center);
            HeavyBullet = Simple(ArtData.HeavyBullet, Center);
            EnemyBullet = Simple(ArtData.EnemyBullet, Center);
            Shell = Simple(ArtData.Shell, Center);
            Grenade = Simple(ArtData.Grenade, Center);
            Bomb = Simple(ArtData.Bomb, Center);
            s_Projectiles = new Sprite[Content.Projectiles.Count];
            s_Projectiles[Content.Bullet.Id] = Bullet;
            s_Projectiles[Content.HeavyBullet.Id] = HeavyBullet;
            s_Projectiles[Content.EnemyBullet.Id] = EnemyBullet;
            s_Projectiles[Content.Shell.Id] = Shell;
            s_Projectiles[Content.Grenade.Id] = Grenade;
            s_Projectiles[Content.Bomb.Id] = Bomb;
            MuzzleFlash = Simple(ArtData.MuzzleFlash, Center, outline: false);
            CrateHeavy = Simple(ArtData.CrateHeavy, feet);
            CrateBombs = Simple(ArtData.CrateBombs, feet);
            Medkit = Simple(ArtData.Medkit, feet);
            OneUp = Simple(ArtData.OneUp, feet);
            s_Pickups = new Sprite[Content.Pickups.Count];
            s_Pickups[Content.HeavyMachineGunCrate.Id] = CrateHeavy;
            s_Pickups[Content.BombCrate.Id] = CrateBombs;
            s_Pickups[Content.Medkit.Id] = Medkit;
            s_Pickups[Content.OneUp.Id] = OneUp;
            Flag = Simple(ArtData.Flag, feet);

            var px = new PixelCanvas(1, 1);
            px.Set(0, 0, new Color32(255, 255, 255, 255));
            Pixel = ToSprite(px, Center);
            WhiteTex = ToTexture(px);

            var heart = new PixelCanvas(9, 9);
            heart.Ascii(ArtData.Heart, s_Pal, 1, 1);
            heart.Outline(OutlineColor);
            HeartTex = ToTexture(heart);

            var disc = new PixelCanvas(32, 32); // the touch pad's stick and buttons
            disc.Circle(16f, 16f, 16f, new Color32(255, 255, 255, 255));
            DiscTex = ToTexture(disc);

            BuildExplosion();
            BuildSpark();
            BuildBackground();
        }

        static Vector2 Center => new Vector2(0.5f, 0.5f);

        static PlayerSkin BuildSkin(Dictionary<char, Color32> pal)
        {
            string[][] run = { ArtData.LegsRun1, ArtData.LegsRun2, ArtData.LegsRun3, ArtData.LegsRun4 };
            var skin = new PlayerSkin
            {
                TorsoForward = Part(ArtData.TorsoForward, ArtData.TorsoForwardTop, pal),
                TorsoUp = Part(ArtData.TorsoUp, ArtData.TorsoUpTop, pal),
                TorsoDown = Part(ArtData.TorsoDown, ArtData.TorsoDownTop, pal),
                TorsoUpDiag = Part(ArtData.TorsoUpDiag, ArtData.TorsoUpDiagTop, pal),
                TorsoDownDiag = Part(ArtData.TorsoDownDiag, ArtData.TorsoDownDiagTop, pal),
                LegsIdle = Part(ArtData.LegsIdle, ArtData.LegsTop, pal),
                LegsJump = Part(ArtData.LegsJump, ArtData.LegsTop, pal),
                LegsCrouch = Part(ArtData.LegsCrouch, ArtData.LegsCrouchTop, pal),
                LegsRun = new Sprite[4],
            };
            for (int i = 0; i < 4; i++) skin.LegsRun[i] = Part(run[i], ArtData.LegsTop, pal);
            return skin;
        }

        /// <summary>White silhouette of a sprite, used for the hit flash.</summary>
        /// <summary>A pickup's sprite.</summary>
        public static Sprite PickupSprite(PickupDef def) => s_Pickups[def.Id];

        /// <summary>A projectile's sprite (the content is data in Core; how it looks lives here).</summary>
        public static Sprite ProjectileSprite(ProjectileDef def) => s_Projectiles[def.Id];

        public static Sprite Flash(Sprite s) => s != null && s_Flash.TryGetValue(s, out Sprite f) ? f : s;

        static Dictionary<char, Color32> Swap(Dictionary<char, uint> changes)
        {
            var pal = new Dictionary<char, Color32>(s_Pal);
            foreach (var kv in changes) pal[kv.Key] = Hex(kv.Value);
            return pal;
        }

        static Sprite Part(string[] rows, int top, Dictionary<char, Color32> pal)
        {
            var c = new PixelCanvas(24, 32);
            c.Ascii(rows, pal, 0, top);
            c.Outline(OutlineColor);
            return ToSprite(c, new Vector2(0.5f, 0f), withFlash: true);
        }

        static Sprite Simple(string[] rows, Vector2 pivot, bool outline = true)
        {
            int w = 0;
            foreach (string r in rows) w = Mathf.Max(w, r.Length);
            var c = new PixelCanvas(w + 2, rows.Length + 2);
            c.Ascii(rows, s_Pal, 1, 1);
            if (outline) c.Outline(OutlineColor);
            return ToSprite(c, pivot, withFlash: true);
        }

        public static Texture2D ToTexture(PixelCanvas c, FilterMode filter = FilterMode.Point)
        {
            var tex = new Texture2D(c.W, c.H, TextureFormat.RGBA32, false)
            {
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(c.Px);
            tex.Apply(false, true);
            return tex;
        }

        public static Sprite ToSprite(PixelCanvas c, Vector2 pivot, bool withFlash = false, FilterMode filter = FilterMode.Point)
        {
            Texture2D tex = ToTexture(c, filter);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, c.W, c.H), pivot, PPU, 0, SpriteMeshType.FullRect);
            if (withFlash)
            {
                var white = new PixelCanvas(c.W, c.H);
                for (int i = 0; i < c.Px.Length; i++)
                    if (c.Px[i].a != 0) white.Px[i] = new Color32(255, 255, 255, 255);
                s_Flash[sprite] = Sprite.Create(ToTexture(white), new Rect(0, 0, c.W, c.H), pivot, PPU, 0, SpriteMeshType.FullRect);
                if (Spike.Holo) s_Holo[sprite] = Sprite.Create(ToTexture(Hologram(c)), new Rect(0, 0, c.W, c.H), pivot, PPU, 0, SpriteMeshType.FullRect);
            }
            return sprite;
        }

        /// <summary>
        /// A sprite's hologram (spike: <see cref="Spike.Holo"/>): its shading in greys, scanlined, with a bright
        /// edge. It's tinted by the renderer's colour, so each kind of goon can glow its own colour.
        /// </summary>
        public static Sprite Holo(Sprite s) => s != null && s_Holo.TryGetValue(s, out Sprite h) ? h : s;

        static PixelCanvas Hologram(PixelCanvas c)
        {
            var holo = new PixelCanvas(c.W, c.H);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    Color32 p = c.Px[y * c.W + x];
                    if (p.a == 0) continue;
                    float lum = (0.3f * p.r + 0.59f * p.g + 0.11f * p.b) / 255f;
                    float v = lum < 0.13f ? 1f : Mathf.Lerp(0.38f, 0.92f, lum); // the dark outline becomes a bright edge
                    bool scan = y % 2 == 0;
                    if (scan) v *= 0.62f;
                    byte b = (byte)(v * 255f);
                    holo.Px[y * c.W + x] = new Color32(b, b, b, scan ? (byte)170 : (byte)235);
                }
            return holo;
        }

        static void BuildExplosion()
        {
            Color32 white = Hex(0xfffff0), yellow = Hex(0xffe060), orange = Hex(0xf08830), red = Hex(0xc04020);
            Color32 smoke = Hex(0x5a4a48), smokeLight = Hex(0x8a7a70);
            float[] radius = { 5f, 9f, 12f, 13.5f, 14f, 13f, 11f };
            var rng = new System.Random(7);
            Explosion = new Sprite[radius.Length];
            for (int f = 0; f < radius.Length; f++)
            {
                var c = new PixelCanvas(32, 32);
                float r = radius[f];
                bool smoky = f >= 4;
                for (int i = 0; i < 9; i++)
                {
                    double a = rng.NextDouble() * Mathf.PI * 2;
                    float d = (float)rng.NextDouble() * r * 0.5f;
                    float pr = r * (0.45f + (float)rng.NextDouble() * 0.25f);
                    c.Circle(16 + Mathf.Cos((float)a) * d, 16 + Mathf.Sin((float)a) * d, pr, smoky ? smoke : red);
                }
                c.Circle(16, 16, r * 0.8f, smoky ? smokeLight : orange);
                if (f < 5) c.Circle(16, 17, r * 0.55f, f < 3 ? yellow : orange);
                if (f < 3) c.Circle(16, 17, r * 0.3f, white);
                if (smoky)
                {
                    // punch holes so the smoke dissolves
                    int holes = (f - 3) * 14;
                    for (int i = 0; i < holes; i++) c.Circle(rng.Next(4, 28), rng.Next(4, 28), 1.5f + f * 0.3f, default);
                }
                Explosion[f] = ToSprite(c, Center);
            }
        }

        static void BuildSpark()
        {
            Color32 w = Hex(0xffffff), y = Hex(0xffe860);
            Spark = new Sprite[2];
            var a = new PixelCanvas(7, 7);
            a.Rect(3, 0, 1, 7, y);
            a.Rect(0, 3, 7, 1, y);
            a.Rect(2, 2, 3, 3, w);
            Spark[0] = ToSprite(a, Center);
            var b = new PixelCanvas(7, 7);
            for (int i = 0; i < 7; i += 2)
            {
                b.Set(i, i, y);
                b.Set(6 - i, i, y);
            }
            b.Set(3, 3, w);
            Spark[1] = ToSprite(b, Center);
        }

        static void BuildBackground()
        {
            // Sky: tiny vertical gradient, bilinear-filtered and stretched over the view.
            var sky = new PixelCanvas(1, 32);
            Color top = new Color32(0x6c, 0xa8, 0xe0, 255), horizon = new Color32(0xf8, 0xdc, 0xa8, 255);
            for (int y = 0; y < 32; y++) sky.Set(0, y, Color.Lerp(horizon, top, Mathf.SmoothStep(0, 1, y / 31f)));
            Sky = ToSprite(sky, Center, filter: FilterMode.Bilinear);

            // Far mountains: sum of sines with periods that divide the width, so the strip tiles seamlessly.
            const int w = 320;
            var far = new PixelCanvas(w, 96);
            Color32 rock = Hex(0xd8a878), rockShade = Hex(0xc09068), rim = Hex(0xf0c898);
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)w * Mathf.PI * 2f;
                float h = 46 + 18 * Mathf.Sin(t * 2 + 0.6f) + 10 * Mathf.Sin(t * 5 + 1.3f) + 4 * Mathf.Sin(t * 13);
                int hi = Mathf.RoundToInt(h);
                for (int y = 0; y < hi; y++) far.Set(x, y, ((x / 3 + y / 5) % 7 == 0 && y < hi - 3) ? rockShade : rock);
                far.Set(x, hi - 1, rim);
            }
            Mountains = ToSprite(far, new Vector2(0f, 0f));

            // Mid layer: ruined skyline with palms.
            var mid = new PixelCanvas(w, 96);
            Color32 wall = Hex(0xa87850), wallDark = Hex(0x8a6040), window = Hex(0x5a3c2c);
            Color32 trunk = Hex(0x6a4a30), leaf = Hex(0x5a7a3a), sand = Hex(0xc89a68);
            var rng = new System.Random(42);
            int bx = 4;
            while (bx < w - 40)
            {
                int bw = rng.Next(22, 44), bh = rng.Next(26, 62);
                mid.Rect(bx, 0, bw, bh, wall);
                mid.Rect(bx + bw - 4, 0, 4, bh, wallDark);
                // broken roofline
                for (int i = 0; i < bw; i += 3)
                {
                    int bite = rng.Next(0, 6);
                    mid.Rect(bx + i, bh - bite, 3, bite, default);
                }
                for (int wy = 8; wy < bh - 10; wy += 12)
                    for (int wx = bx + 4; wx < bx + bw - 8; wx += 9)
                        if (rng.NextDouble() < 0.8) mid.Rect(wx, wy, 4, 6, window);
                bx += bw + rng.Next(18, 40);
                if (bx < w - 20)
                {
                    int px = bx - rng.Next(8, 14), ph = rng.Next(34, 50);
                    for (int y = 0; y < ph; y++) mid.Rect(px + (y * y) / 400, y, 3, 1, trunk);
                    int tx = px + (ph * ph) / 400 + 1;
                    for (int k = -1; k <= 1; k += 2)
                        for (int i = 0; i < 16; i++)
                            mid.Rect(tx + k * i, ph + 2 - (i * i) / 18, 2, 2, leaf);
                    mid.Circle(tx + 1, ph + 2, 3, leaf);
                }
            }
            for (int x = 0; x < w; x++)
            {
                int dune = 6 + Mathf.RoundToInt(4 * Mathf.Sin(x / (float)w * Mathf.PI * 6));
                mid.Rect(x, 0, 1, dune, sand);
            }
            Ruins = ToSprite(mid, new Vector2(0f, 0f));
        }

        // ---- Tiles (baked into level chunk textures by LevelView) ----

        static readonly Color32 SandTop = Hex(0xf0d088), SandLight = Hex(0xe0b870), Sand = Hex(0xc89858);
        static readonly Color32 Dirt = Hex(0xa87448), DirtDark = Hex(0x8a5a38), Pebble = Hex(0xd0a070);
        static readonly Color32 Bag = Hex(0xc8b080), BagDark = Hex(0x9a8058), BagSeam = Hex(0x6a5438);
        static readonly Color32 Plank = Hex(0x9a6a40), PlankDark = Hex(0x6a4428), PlankLight = Hex(0xc08a58);

        /// <summary>Writes one 16x16 tile into <paramref name="c"/> at pixel (ox, oy).</summary>
        public static void DrawTile(PixelCanvas c, int ox, int oy, char tile, bool exposedTop, int seed)
        {
            var rng = new System.Random(seed);
            switch (tile)
            {
                case '#':
                    for (int y = 0; y < 16; y++)
                        for (int x = 0; x < 16; x++)
                        {
                            Color32 col = Dirt;
                            int depth = 15 - y;
                            bool sand = exposedTop && depth < 6 + ((x * 7 + seed) % 3);
                            if (sand) col = depth == 0 ? SandTop : depth < 3 ? SandLight : Sand;
                            else if ((x * 13 + y * 7 + seed) % 11 == 0) col = DirtDark;
                            c.Set(ox + x, oy + y, col);
                        }
                    for (int i = 0; i < 3; i++)
                    {
                        int px = rng.Next(1, 14), py = rng.Next(1, exposedTop ? 9 : 14);
                        c.Rect(ox + px, oy + py, 2, 1, Pebble);
                        c.Set(ox + px, oy + py - 1, DirtDark);
                    }
                    break;

                case '=':
                    // two rows of sandbags
                    for (int row = 0; row < 2; row++)
                    {
                        int by = oy + row * 8, shift = row == 0 ? 0 : 4;
                        for (int b = -1; b < 2; b++)
                        {
                            int bx = ox + shift + b * 8;
                            for (int y = 0; y < 8; y++)
                                for (int x = 0; x < 8; x++)
                                {
                                    int gx = bx + x;
                                    if (gx < ox || gx >= ox + 16) continue;
                                    bool corner = (x == 0 || x == 7) && (y == 0 || y == 7);
                                    if (corner) continue;
                                    Color32 col = (x == 0 || y == 0) ? BagDark : (y == 7 || x == 7) ? BagSeam : Bag;
                                    if (y == 3 && x > 1 && x < 6) col = BagDark;
                                    c.Set(gx, by + y, col);
                                }
                        }
                    }
                    break;

                case '-':
                    for (int x = 0; x < 16; x++)
                    {
                        c.Set(ox + x, oy + 15, PlankLight);
                        c.Set(ox + x, oy + 14, Plank);
                        c.Set(ox + x, oy + 13, Plank);
                        c.Set(ox + x, oy + 12, PlankDark);
                    }
                    for (int x = 3; x < 16; x += 8) c.Rect(ox + x, oy + 12, 1, 4, PlankDark);
                    // rope / support struts hanging below
                    c.Rect(ox + 2, oy + 8, 1, 4, PlankDark);
                    c.Rect(ox + 13, oy + 8, 1, 4, PlankDark);
                    break;
            }
        }
    }
}
