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
        /// <summary>Off duty, in the den: no gun (standing, gaming, lounging), and sitting on the couch.</summary>
        public Sprite TorsoUnarmed, TorsoPad, TorsoLounge, LegsSit;
        public Sprite[] LegsRun;
    }

    /// <summary>Builds every sprite in the game procedurally at startup (no imported assets).</summary>
    public static class Art
    {
        public const float PPU = Tuning.Pixels.PerUnit;
        public static readonly Color32 OutlineColor = Hex(0x1c1420);

        /// <summary>[0] = player 1 (red bandana), [1] = player 2 (blue bandana).</summary>
        public static PlayerSkin[] PlayerSkins;
        /// <summary>Each crewmate in their paint (<see cref="CrewMember.Paint"/>): [0] player 1, [1] player 2.</summary>
        static readonly Dictionary<SoldierDef, PlayerSkin[]> s_CrewSkins = new Dictionary<SoldierDef, PlayerSkin[]>();
        public static Sprite[] SoldierLegsRun, RunnerLegsRun;
        public static Sprite SoldierLegsIdle, RunnerLegsJump;
        public static Sprite SoldierTorso, RunnerTorso;
        public static Sprite[] Drone, Explosion, Spark;
        public static Sprite Tank, Bullet, HeavyBullet, EnemyBullet, Shell, Grenade, Bomb, MuzzleFlash;
        static Sprite[] s_Projectiles, s_Pickups;
        public static Sprite CrateHeavy, CrateBombs, Medkit, OneUp, Flag, Pixel, Sky, Mountains, Ruins;
        public static Sprite Watchtower, MotorPoolGate, Barrel, SteamVent, Icicles, LiftCar, LiftRails;
        /// <summary>[0] not reached, [1] lit.</summary>
        public static Sprite[] Checkpoint;
        /// <summary>[0] off, [1] and [2] live (sparking).</summary>
        public static Sprite[] LiveFence;
        public static Texture2D HeartTex, WhiteTex, DiscTex;

        static Dictionary<char, Color32> s_Pal;
        static readonly Dictionary<Sprite, Sprite> s_Flash = new Dictionary<Sprite, Sprite>();
        static readonly Dictionary<Sprite, Sprite> s_Holo = new Dictionary<Sprite, Sprite>();
        static bool s_Ready;

        /// <summary>
        /// A sprite drawn at <paramref name="order"/> on a new child of <paramref name="parent"/>: how the world's
        /// pieces are made. <paramref name="shown"/> false starts it hidden, for something drawn only now and then.
        /// </summary>
        public static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, int order, bool shown = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.enabled = shown;
            return sr;
        }

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
                { 'v', Hex(0x6b4fb0) }, { 'V', Hex(0x45307a) }, // the den's controllers
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
            foreach (CrewMember m in Crew.All)
            {
                var skins = new PlayerSkin[m.Paint.Count];
                for (int k = 0; k < skins.Length; k++) skins[k] = BuildSkin(Swap(m.Paint[k]));
                s_CrewSkins[m.Soldier] = skins;
            }
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
            s_Projectiles[Content.BigShell.Id] = Shell;
            s_Projectiles[Content.Knife.Id] = HeavyBullet;
            s_Projectiles[Content.StolenShell.Id] = Shell;
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
            BuildScenery();
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
                TorsoUnarmed = Part(ArtData.TorsoUnarmed, ArtData.TorsoForwardTop, pal),
                TorsoPad = Part(ArtData.TorsoPad, ArtData.TorsoForwardTop, pal),
                TorsoLounge = Part(ArtData.TorsoLounge, ArtData.TorsoForwardTop, pal),
                LegsSit = Part(ArtData.LegsSit, ArtData.LegsSitTop, pal),
                LegsRun = new Sprite[4],
            };
            for (int i = 0; i < 4; i++) skin.LegsRun[i] = Part(run[i], ArtData.LegsTop, pal);
            return skin;
        }

        /// <summary>A pickup's sprite.</summary>
        public static Sprite PickupSprite(PickupDef def) => s_Pickups[def.Id];

        /// <summary>How <paramref name="soldier"/> looks as player <paramref name="skin"/> + 1.</summary>
        public static PlayerSkin SkinFor(SoldierDef soldier, int skin)
        {
            if (soldier == null || !s_CrewSkins.TryGetValue(soldier, out PlayerSkin[] skins)) skins = PlayerSkins;
            return skins[Mathf.Clamp(skin, 0, skins.Length - 1)];
        }

        /// <summary>A projectile's sprite (the content is data in Core; how it looks lives here). Ids past the table borrow the shell's.</summary>
        public static Sprite ProjectileSprite(ProjectileDef def) => def.Id < s_Projectiles.Length ? s_Projectiles[def.Id] : Shell;

        /// <summary>White silhouette of a sprite, used for the hit flash.</summary>
        public static Sprite Flash(Sprite s) => s != null && s_Flash.TryGetValue(s, out Sprite f) ? f : s;

        static Dictionary<char, Color32> Swap(IReadOnlyDictionary<char, uint> changes)
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

        /// <summary>A sprite from rows of palette letters, as the rest of the art is made (with a hit-flash copy).</summary>
        public static Sprite FromRows(string[] rows, Vector2 pivot, bool outline = true) => Simple(rows, pivot, outline);

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
                s_Holo[sprite] = Sprite.Create(ToTexture(Hologram(c)), new Rect(0, 0, c.W, c.H), pivot, PPU, 0, SpriteMeshType.FullRect);
            }
            return sprite;
        }

        /// <summary>
        /// A sprite's hologram (the goons, <see cref="HoloLook"/>): its shading in greys, scanlined, with a bright
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

        // ---- The boss tank: its hull, its swinging gun and the marker where its shell will land ----

        static Sprite s_TankHull, s_TankBarrel, s_Marker;

        /// <summary>The tank's body without its gun, for the boss tank, whose turret swings (same feet pivot as <see cref="Tank"/>).</summary>
        public static Sprite TankHull
        {
            get
            {
                if (s_TankHull != null) return s_TankHull;
                var rows = (string[])ArtData.Tank.Clone();
                for (int r = 4; r <= 5; r++) rows[r] = new string('.', 22) + rows[r].Substring(22); // the barrel is drawn on its own
                return s_TankHull = FromRows(rows, new Vector2(0.5f, 0f));
            }
        }

        /// <summary>The tank's gun on its own, pointing left from its pivot at the turret (<see cref="TankBarrelPivot"/>).</summary>
        public static Sprite TankBarrel
        {
            get
            {
                if (s_TankBarrel != null) return s_TankBarrel;
                string m = new string('m', 20), d = new string('M', 20);
                // the pivot is the barrel's right end, where it meets the turret
                return s_TankBarrel = FromRows(new[] { m, d }, new Vector2(20.5f / 22f, 0.5f));
            }
        }

        /// <summary>Where the barrel turns, relative to the tank's feet, in units.</summary>
        public static readonly Vector2 TankBarrelPivot = new Vector2(-4f / PPU, 18f / PPU);

        /// <summary>A target marker for where a charged shell will land: a red chevron over a bar.</summary>
        public static Sprite LandingMarker
        {
            get
            {
                if (s_Marker != null) return s_Marker;
                Color32 r = new Color32(0xff, 0x40, 0x30, 255), w = new Color32(0xff, 0xe0, 0xd0, 255);
                var c = new PixelCanvas(9, 7);
                for (int i = 0; i < 5; i++)
                {
                    c.Set(i, 6 - i, r);
                    c.Set(8 - i, 6 - i, r);
                    c.Set(i + 1, 6 - i, r);
                    c.Set(7 - i, 6 - i, r);
                }
                c.Set(4, 2, w);
                c.Rect(1, 0, 7, 1, r);
                return s_Marker = ToSprite(c, new Vector2(0.5f, 0f));
            }
        }

        // ---- Mission one's look: the steel night base (concept C, picked 2026-10-15) ----

        static readonly Color32 Night = Hex(0x16203c), Glow = Hex(0xffe08a), Beacon = Hex(0xd83838);
        static readonly Color32 SnowTop = Hex(0xf4f8ff), Snow = Hex(0xc8d4e8), SnowShade = Hex(0x8c9cbc);
        static readonly Color32 Wall = Hex(0x3a4058), WallSeam = Hex(0x2a3048), Wire = new Color32(150, 160, 180, 200);

        static void BuildBackground()
        {
            // Sky: tiny vertical gradient, bilinear-filtered and stretched over the view.
            var sky = new PixelCanvas(1, 32);
            Color top = new Color32(0x0a, 0x10, 0x26, 255), horizon = new Color32(0x2c, 0x3e, 0x66, 255);
            for (int y = 0; y < 32; y++) sky.Set(0, y, Color.Lerp(horizon, top, Mathf.SmoothStep(0, 1, y / 31f)));
            Sky = ToSprite(sky, Center, filter: FilterMode.Bilinear);

            // Far layer: stars, searchlight beams and the base's skyline (hangars, a radar mast, a crane).
            // Nothing crosses the strip's edges, so it tiles seamlessly.
            const int w = 320;
            var far = new PixelCanvas(w, 112);
            var rng = new System.Random(7);
            for (int i = 0; i < 70; i++)
            {
                byte a = (byte)(rng.Next(3) == 0 ? 255 : rng.Next(90, 200));
                far.Set(rng.Next(w), rng.Next(48, 112), new Color32(230, 236, 255, a));
            }
            Beam(far, 70, 40, -0.35f, 72);
            Beam(far, 230, 40, 0.45f, 72);
            int[][] hangars = { new[] { 8, 64 }, new[] { 132, 72 }, new[] { 240, 56 } };
            foreach (int[] h in hangars)
            {
                int hx = h[0], hw = h[1];
                far.Rect(hx, 0, hw, 62, Night);
                for (int i = 0; i <= hw / 2; i++) far.Rect(hx + i, 62, 1, i / 5, Night);
                for (int i = 0; i <= hw / 2; i++) far.Rect(hx + hw - 1 - i, 62, 1, i / 5, Night);
                for (int k = 0; k < 3; k++) far.Rect(hx + 8 + k * (hw / 3), 50, 3, 2, Glow);
            }
            far.Rect(100, 0, 2, 76, Night); // radar mast
            for (int y = -4; y <= 4; y++)
                for (int x = -9; x <= 9; x++)
                    if (x * x / 81f + y * y / 16f <= 1f) far.Set(101 + x, 78 + y, Night);
            far.Set(101, 84, Beacon);
            far.Rect(220, 0, 2, 78, Night); // crane
            far.Rect(200, 76, 40, 2, Night);
            far.Rect(206, 60, 1, 16, Night);
            far.Set(238, 78, Beacon);
            Mountains = ToSprite(far, new Vector2(0f, 0f));

            // Mid layer: the perimeter wall, capped with snow and razor wire, with floodlight poles.
            var mid = new PixelCanvas(w, 96);
            const int wallTop = 36;
            for (int x = 0; x < w; x++)
            {
                mid.Rect(x, 0, 1, wallTop, x % 32 == 0 ? WallSeam : Wall);
                mid.Set(x, wallTop, SnowShade);
                mid.Set(x, wallTop + 1, Snow);
                if (x % 7 == 3) mid.Set(x, wallTop - 1, Snow);
            }
            for (int x = 0; x < w; x += 5) // razor wire coils
                for (int a = 0; a < 360; a += 30)
                {
                    float r = a * Mathf.Deg2Rad;
                    mid.Set(x + 2 + Mathf.RoundToInt(2.5f * Mathf.Cos(r)), wallTop + 5 + Mathf.RoundToInt(2.5f * Mathf.Sin(r)), Wire);
                }
            foreach (int px in new[] { 90, 250 })
            {
                mid.Rect(px, wallTop, 2, 34, WallSeam);
                mid.Rect(px - 3, wallTop + 34, 8, 3, WallSeam);
                mid.Rect(px - 2, wallTop + 33, 6, 1, Glow);
            }
            Ruins = ToSprite(mid, new Vector2(0f, 0f));
        }

        /// <summary>A faint searchlight cone rising from (x, y), leaning by `lean` px per row.</summary>
        static void Beam(PixelCanvas c, int x, int y, float lean, int length)
        {
            for (int t = 0; t < length; t++)
            {
                int half = 1 + t / 8;
                int cx = x + Mathf.RoundToInt(lean * t);
                for (int dx = -half; dx <= half; dx++) c.Set(cx + dx, y + t, new Color32(255, 240, 180, 26));
            }
        }

        // ---- Scenery: big props that stand behind the action ----

        static readonly Color32 Haze = Hex(0xd0a040), Panel = Hex(0x7a808e), PanelLight = Hex(0x9aa0ac);
        static readonly Color32 PanelEdge = Hex(0x4e5260), PanelDark = Hex(0x3a3e4a), Red = Hex(0xd83838), RedLight = Hex(0xff7060);
        static readonly Color32 RedDark = Hex(0x902020), White = Hex(0xffffff), Ember = Hex(0xf08830), SparkBlue = Hex(0x90f4ff);
        static readonly Color32 HazardShade = Hex(0xb08a20), SignalGreen = Hex(0x50ff80);

        static void BuildScenery()
        {
            // A steel lattice watchtower: a lit cabin, snow on the roof, a searchlight.
            var tower = new Pen(38, 86);
            tower.Line(8, 34, 3, 85, SteelLight);
            tower.Line(9, 34, 4, 85, Steel);
            tower.Line(29, 34, 34, 85, Steel);
            tower.Line(28, 34, 33, 85, SteelLight);
            bool flip = false;
            for (int y = 36; y < 84; y += 8)
            {
                int y2 = y + 8;
                float l0 = 8 - (y - 34) * 5 / 51f, r0 = 29 + (y - 34) * 5 / 51f;
                float l1 = 8 - (y2 - 34) * 5 / 51f, r1 = 29 + (y2 - 34) * 5 / 51f;
                if (flip) tower.Line((int)l0, y, (int)r1, Mathf.Min(y2, 85), SteelDark);
                else tower.Line((int)r0, y, (int)l1, Mathf.Min(y2, 85), SteelDark);
                tower.Line((int)l0, y, (int)r0, y, SteelDark);
                flip = !flip;
            }
            for (int y = 36; y < 86; y += 3) tower.Rect(17, y, 4, 1, SteelLight); // ladder
            tower.Rect(17, 36, 1, 50, SteelDark);
            tower.Rect(21, 36, 1, 50, SteelDark);
            tower.Box(3, 14, 32, 20, Steel, SteelLight, SteelDark);
            tower.Rect(6, 18, 26, 7, Glow);
            tower.Rect(6, 24, 26, 1, Haze);
            tower.Rect(14, 18, 1, 7, SteelDark);
            tower.Rect(23, 18, 1, 7, SteelDark);
            tower.Stripes(3, 30, 32, 3, Hazard, HazardDark, 2);
            tower.Rect(1, 11, 36, 3, SteelDark);
            tower.Rect(1, 9, 36, 2, SnowTop);
            foreach (int x in new[] { 4, 9, 27, 33 }) tower.Rect(x, 14, 1, 1 + x % 3, Snow);
            tower.Box(24, 2, 9, 7, Steel, SteelLight, SteelDark);
            tower.Rect(33, 3, 2, 5, Glow);
            tower.Rect(27, 8, 3, 1, SteelDark);
            tower.Rect(8, 0, 1, 9, SteelDark);
            tower.Rect(8, 0, 1, 1, Red);
            Watchtower = tower.ToSprite();

            // The motor pool's gate (the boss door): a blast shutter in a snowy concrete wall, hazard-striped.
            var gate = new Pen(100, 66);
            gate.Box(0, 6, 100, 60, Panel, PanelLight, PanelEdge);
            gate.Speck(0, 7, 100, 58, PanelDark, 0.05f, 40, Panel);
            gate.Rect(0, 2, 100, 4, SnowTop);
            gate.Rect(0, 6, 100, 1, SnowShade);
            foreach (int x in new[] { 3, 11, 40, 77, 90 }) gate.Rect(x, 7, 1, 2 + x % 3, Snow);
            gate.Stripes(10, 18, 80, 48, Hazard, HazardDark, 4);
            gate.Rect(14, 22, 72, 44, Steel);
            for (int y = 22; y < 66; y += 4)
            {
                gate.Rect(14, y, 72, 1, SteelLight);
                gate.Rect(14, y + 3, 72, 1, SteelDark);
            }
            gate.Rect(14, 62, 72, 4, SteelDark);
            gate.Rect(34, 9, 32, 8, SteelDark);
            gate.Text(38, 10, "BAY 07", Hazard);
            foreach (int x in new[] { 4, 92 })
            {
                gate.Rect(x, 20, 4, 6, Red);
                gate.Rect(x, 20, 2, 2, RedLight);
                gate.Rect(x, 26, 4, 2, SteelDark);
            }
            gate.Box(44, 52, 12, 4, SteelDark, SteelLight, SteelDark);
            MotorPoolGate = gate.ToSprite();

            // An explosive barrel: a red fuel drum with a hazard band and a flame diamond.
            var barrel = new Pen(12, 18);
            barrel.Rect(0, 3, 12, 15, Red);
            barrel.Rect(3, 0, 6, 3, Steel);
            barrel.Rect(4, 0, 2, 1, SteelLight);
            barrel.Rect(1, 2, 10, 1, RedDark);
            barrel.Rect(2, 3, 2, 15, RedLight);
            barrel.Rect(10, 3, 1, 15, RedDark);
            barrel.Stripes(0, 6, 12, 3, Hazard, HazardDark, 2);
            for (int j = 0; j < 7; j++)
            {
                int h = 3 - Mathf.Abs(j - 3);
                barrel.Rect(6 - h, 10 + j, 2 * h + 1, 1, White);
            }
            barrel.Rect(6, 12, 1, 3, Red);
            barrel.Set(5, 13, Ember);
            Barrel = barrel.ToSprite();

            // A live fence: chain-link between posts with a high-voltage sign; sparks run over it while it's live.
            LiveFence = new Sprite[3];
            for (int k = 0; k < 3; k++)
            {
                var fence = new Pen(28, 26);
                foreach (int x in new[] { 1, 25 }) fence.Box(x, 2, 2, 24, Steel, SteelLight, SteelDark);
                for (int y = 4; y < 26; y++)
                    for (int x = 3; x < 25; x++)
                        if ((x + y) % 4 == 0 || ((x - y) % 4 + 4) % 4 == 0) fence.Set(x, y, SteelLight);
                fence.Box(7, 10, 14, 7, Hazard, Hazard, HazardShade);
                fence.Text(10, 11, "HV", HazardDark);
                if (k > 0)
                {
                    var rng = new System.Random(20 + k);
                    int x = k == 1 ? 4 : 22, y = k == 1 ? 4 : 20;
                    for (int i = 0; i < 9; i++)
                    {
                        int nx = Mathf.Clamp(x + rng.Next(-3, 4), 0, 27), ny = Mathf.Clamp(y + rng.Next(-3, 4), 0, 25);
                        fence.Line(x, y, nx, ny, SparkBlue);
                        x = nx;
                        y = ny;
                    }
                    fence.Rect(x - 1, y - 1, 3, 3, White);
                }
                LiveFence[k] = fence.ToSprite();
            }

            // A steam vent's grate, flush with the ground (the steam is particles).
            var vent = new Pen(18, 5);
            vent.Box(0, 0, 18, 5, Steel, SteelLight, SteelDark);
            for (int x = 2; x < 16; x += 3) vent.Rect(x, 1, 1, 3, SteelDark);
            SteamVent = vent.ToSprite();

            // Icicles, hanging from their top edge.
            var ice = new Pen(20, 14);
            int[][] spikes = { new[] { 1, 13 }, new[] { 5, 9 }, new[] { 8, 12 }, new[] { 12, 7 }, new[] { 15, 11 } };
            foreach (int[] sp in spikes)
            {
                for (int j = 0; j < sp[1]; j++)
                {
                    int w = Mathf.Max(1, Mathf.RoundToInt(4f * (1f - j / (float)sp[1])));
                    ice.Rect(sp[0] + (4 - w) / 2, j, w, 1, Snow);
                }
                ice.Rect(sp[0] + 1, 0, 1, sp[1] - 3, SnowTop);
            }
            ice.Rect(0, 0, 20, 2, SnowTop);
            Icicles = ice.ToSprite(new Vector2(0.5f, 1f - 1f / 16f));

            // A lift's car (hanging from its top edge, which is what soldiers stand on) and its rails.
            var car = new Pen(32, 8);
            car.Rect(0, 0, 32, 1, SnowTop);
            car.Stripes(0, 1, 32, 3, Hazard, HazardDark, 3);
            car.Box(0, 4, 32, 4, Steel, SteelLight, SteelDark);
            car.Rect(14, 4, 4, 1, Glow);
            LiftCar = car.ToSprite(new Vector2(0.5f, 1f - 1f / 10f));
            int railH = Lift.Rise * 16 + 24;
            var rails = new Pen(38, railH);
            foreach (int x in new[] { 0, 35 }) rails.Box(x, 0, 3, railH, Steel, SteelLight, SteelDark);
            rails.Box(0, 0, 38, 4, Steel, SteelLight, SteelDark);
            for (int y = 12; y < railH; y += 16)
            {
                rails.Rect(0, y, 3, 1, SteelDark);
                rails.Rect(35, y, 3, 1, SteelDark);
            }
            rails.Rect(18, 4, 2, 4, SteelDark);
            rails.Rect(0, 0, 38, 1, SnowTop);
            LiftRails = rails.ToSprite();

            // The checkpoint at the boss door: a striped post with a signal lamp, red until it's reached.
            Checkpoint = new Sprite[2];
            for (int k = 0; k < 2; k++)
            {
                var post = new Pen(16, 36);
                post.Box(2, 30, 12, 6, Panel, PanelLight, PanelEdge);
                post.Rect(2, 30, 12, 1, SnowTop);
                post.Stripes(6, 8, 4, 22, Hazard, HazardDark, 3);
                post.Box(3, 0, 10, 9, Steel, SteelLight, SteelDark);
                post.Rect(5, 2, 6, 5, k == 1 ? SignalGreen : Red);
                post.Rect(5, 2, 2, 2, White);
                Checkpoint[k] = post.ToSprite();
            }
        }

        /// <summary>
        /// Draws scenery in top-down pixel coordinates, like the concept sheets, with a 1px border for the outline.
        /// </summary>
        sealed class Pen
        {
            readonly PixelCanvas m_C;

            public Pen(int w, int h) => m_C = new PixelCanvas(w + 2, h + 2);

            public void Set(int x, int y, Color32 c) => m_C.Set(x + 1, m_C.H - 2 - y, c);
            Color32 Get(int x, int y) => m_C.Get(x + 1, m_C.H - 2 - y);

            public void Rect(int x, int y, int w, int h, Color32 c)
            {
                for (int j = y; j < y + h; j++)
                    for (int i = x; i < x + w; i++)
                        Set(i, j, c);
            }

            public void Box(int x, int y, int w, int h, Color32 c, Color32 hi, Color32 lo)
            {
                Rect(x, y, w, h, c);
                Rect(x, y, w, 1, hi);
                Rect(x, y, 1, h, hi);
                Rect(x, y + h - 1, w, 1, lo);
                Rect(x + w - 1, y, 1, h, lo);
            }

            public void Line(int x0, int y0, int x1, int y1, Color32 c)
            {
                int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
                while (true)
                {
                    Set(x0, y0, c);
                    if (x0 == x1 && y0 == y1) break;
                    int e2 = 2 * err;
                    if (e2 >= dy) { err += dy; x0 += sx; }
                    if (e2 <= dx) { err += dx; y0 += sy; }
                }
            }

            public void Stripes(int x, int y, int w, int h, Color32 a, Color32 b, int period)
            {
                for (int j = 0; j < h; j++)
                    for (int i = 0; i < w; i++)
                        Set(x + i, y + j, ((i + j) / period) % 2 == 0 ? a : b);
            }

            /// <summary>Sprinkles <paramref name="c"/> over the pixels that are <paramref name="on"/>.</summary>
            public void Speck(int x, int y, int w, int h, Color32 c, float density, int seed, Color32 on)
            {
                var rng = new System.Random(seed);
                for (int j = y; j < y + h; j++)
                    for (int i = x; i < x + w; i++)
                    {
                        Color32 at = Get(i, j);
                        if (rng.NextDouble() < density && at.r == on.r && at.g == on.g && at.b == on.b) Set(i, j, c);
                    }
            }

            static readonly Dictionary<char, string> s_Font = new Dictionary<char, string>
            {
                { 'B', "##.#.###.#.###." }, { 'A', ".#.#.####.##.#" + "#" }, { 'Y', "#.##.#.#..#..#." },
                { '0', "####.##.##.####" }, { 'H', "#.##.####.##.#" + "#" }, { 'V', "#.##.##.##.#.#." }, { '7', "###..#.#..#..#." }, { ' ', "..............." },
            };

            /// <summary>Stencils text in a 3x5 pixel font.</summary>
            public void Text(int x, int y, string s, Color32 c)
            {
                foreach (char ch in s)
                {
                    string g = s_Font[ch];
                    for (int i = 0; i < 15; i++)
                        if (g[i] == '#') Set(x + i % 3, y + i / 3, c);
                    x += 4;
                }
            }

            /// <summary>Outlines it and makes the sprite: by default its feet stand on the ground (below the outline).</summary>
            public Sprite ToSprite(Vector2? pivot = null)
            {
                m_C.Outline(OutlineColor);
                return Art.ToSprite(m_C, pivot ?? new Vector2(0.5f, 1f / m_C.H), withFlash: true);
            }
        }

        // ---- Tiles (baked into level chunk textures by LevelView) ----

        static readonly Color32 Concrete = Hex(0x7a808e), ConcreteLight = Hex(0x9aa0ac), ConcreteSeam = Hex(0x4e5260);
        static readonly Color32 ConcreteDark = Hex(0x3a3e4a);
        static readonly Color32 Bag = Hex(0xc4c8cc), BagDark = Hex(0x8a9098), BagSeam = Hex(0x6c727c);
        static readonly Color32 Steel = Hex(0x5a6478), SteelDark = Hex(0x2e3444), SteelLight = Hex(0x8a96aa);
        static readonly Color32 Hazard = Hex(0xf0c030), HazardDark = Hex(0x24242c);

        /// <summary>Writes one 16x16 tile into <paramref name="c"/> at pixel (ox, oy).</summary>
        public static void DrawTile(PixelCanvas c, int ox, int oy, char tile, bool exposedTop, int seed)
        {
            var rng = new System.Random(seed);
            switch (tile)
            {
                case '#':
                    // Concrete slabs, two courses per tile, with a snow cap where the top is open.
                    for (int y = 0; y < 16; y++)
                        for (int x = 0; x < 16; x++)
                        {
                            int depth = 15 - y;
                            bool upper = depth < 8;
                            Color32 col = Concrete;
                            if (depth == 7 || depth == 15) col = ConcreteSeam;
                            else if (depth == 0 || depth == 8) col = ConcreteLight;
                            else if (x == (upper ? 5 : 12)) col = ConcreteSeam;
                            else if ((x * 13 + y * 7 + seed) % 17 == 0) col = ConcreteDark;
                            if (exposedTop)
                            {
                                if (depth < 2) col = SnowTop;
                                else if (depth < 4) col = Snow;
                                else if (depth == 4) col = SnowShade;
                                else if (depth < 7 && (x * 5 + seed) % 7 == 0) col = Snow; // drips
                            }
                            c.Set(ox + x, oy + y, col);
                        }
                    if (rng.Next(3) == 0)
                    {
                        int cx = rng.Next(2, 12), cy = rng.Next(1, 6);
                        for (int i = 0; i < 3; i++) c.Set(ox + cx + i, oy + cy + i / 2, ConcreteDark); // a crack
                    }
                    break;

                case '=':
                    // two rows of snow-camo sandbags
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
                                    Color32 col = (x == 0 || y == 0) ? BagSeam : (y == 7 || x == 7) ? BagDark : Bag;
                                    if (y == 3 && x > 1 && x < 6) col = BagDark;
                                    if (y == 7 && x > 1 && x < 6) col = SnowTop;
                                    c.Set(gx, by + y, col);
                                }
                        }
                    }
                    if (exposedTop)
                        for (int x = 1; x < 15; x++)
                        {
                            c.Set(ox + x, oy + 15, SnowTop);
                            if (x % 4 != 0) c.Set(ox + x, oy + 14, Snow);
                        }
                    break;

                case '-':
                    // a steel catwalk: hazard-striped edge, grating, and trusses below
                    for (int x = 0; x < 16; x++)
                    {
                        c.Set(ox + x, oy + 15, SnowTop);
                        c.Set(ox + x, oy + 14, ((x + 16) / 2) % 2 == 0 ? Hazard : HazardDark);
                        c.Set(ox + x, oy + 13, x % 3 == 0 ? SteelDark : Steel);
                        c.Set(ox + x, oy + 12, x % 3 == 0 ? SteelDark : SteelLight);
                        c.Set(ox + x, oy + 11, SteelDark);
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        c.Set(ox + 3 + i, oy + 10 - i, SteelLight);
                        c.Set(ox + 12 - i, oy + 10 - i, SteelLight);
                    }
                    break;
            }
        }
    }
}
