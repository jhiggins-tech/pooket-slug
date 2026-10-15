using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Sprites for the spikes, made in code like the rest of the art: tones2's yellow water, toxic mud, spew and
    /// sludge, and the slices a hologram breaks into when it glitches out. Built the first time a spike asks for them.
    /// </summary>
    public static class SpikeArt
    {
        // tones2's water is yellow, as in Pooket Tabks; his mud is dark and wet, with toxic green in it.
        public static readonly Color Water = new Color32(0xff, 0xcc, 0x1f, 255);
        public static readonly Color WaterLight = new Color32(0xff, 0xf0, 0xa0, 255);
        public static readonly Color Mud = new Color32(0x5c, 0x3e, 0x24, 255);
        public static readonly Color Toxic = new Color32(0x9b, 0xe2, 0x2d, 255);
        // His spew is a sickly chartreuse; the sludge it leaves smoulders with embers.
        public static readonly Color Spew = new Color32(0xc4, 0xdc, 0x3c, 255);
        public static readonly Color SpewDark = new Color32(0x6e, 0x8a, 0x1c, 255);
        public static readonly Color Ember = new Color32(0xff, 0x8a, 0x2a, 255);

        static Sprite[] s_Drops, s_Mud, s_Gobs, s_Sludge;
        static Sprite s_Hull, s_Barrel, s_Marker;

        /// <summary>The tank's body without its gun, for the boss tank whose turret swings (same feet pivot as <see cref="Art.Tank"/>).</summary>
        public static Sprite TankHull
        {
            get
            {
                if (s_Hull != null) return s_Hull;
                var rows = (string[])ArtData.Tank.Clone();
                for (int r = 4; r <= 5; r++) rows[r] = new string('.', 22) + rows[r].Substring(22); // the barrel is drawn on its own
                return s_Hull = Art.FromRows(rows, new Vector2(0.5f, 0f));
            }
        }

        /// <summary>The tank's gun on its own, pointing left from its pivot at the turret (<see cref="BarrelPivot"/>).</summary>
        public static Sprite TankBarrel
        {
            get
            {
                if (s_Barrel != null) return s_Barrel;
                string m = new string('m', 20), d = new string('M', 20);
                // the pivot is the barrel's right end, where it meets the turret
                return s_Barrel = Art.FromRows(new[] { m, d }, new Vector2(20.5f / 22f, 0.5f));
            }
        }

        /// <summary>Where the barrel turns, relative to the tank's feet, in units.</summary>
        public static readonly Vector2 BarrelPivot = new Vector2(-4f / Art.PPU, 18f / Art.PPU);

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
                return s_Marker = Art.ToSprite(c, new Vector2(0.5f, 0f));
            }
        }
        static readonly Dictionary<Sprite, Sprite[]> s_Bands = new Dictionary<Sprite, Sprite[]>();

        /// <summary>[0] a droplet, [1] a fat one for the full jet.</summary>
        public static Sprite[] Drops
        {
            get
            {
                if (s_Drops != null) return s_Drops;
                Color32 w = Water, l = WaterLight;
                var small = new PixelCanvas(2, 2);
                small.Rect(0, 0, 2, 2, w);
                small.Set(0, 1, l);
                var fat = new PixelCanvas(4, 3);
                fat.Rect(0, 0, 4, 3, w);
                fat.Set(0, 0, default);
                fat.Set(3, 2, default);
                fat.Rect(1, 2, 2, 1, l);
                return s_Drops = new[] { Art.ToSprite(small, new Vector2(0.5f, 0.5f)), Art.ToSprite(fat, new Vector2(0.5f, 0.5f)) };
            }
        }

        /// <summary>Three lumpy gobbets of spew.</summary>
        public static Sprite[] Gobs
        {
            get
            {
                if (s_Gobs != null) return s_Gobs;
                Color32 s = Spew, d = SpewDark, l = new Color32(0xec, 0xf6, 0x9a, 255);
                var a = new PixelCanvas(3, 3);
                a.Rect(0, 0, 3, 3, s);
                a.Set(0, 0, default);
                a.Set(2, 0, d);
                a.Set(0, 2, l);
                var b = new PixelCanvas(4, 3);
                b.Rect(0, 0, 4, 3, s);
                b.Set(3, 2, default);
                b.Set(0, 0, d);
                b.Set(1, 2, l);
                var c = new PixelCanvas(2, 3);
                c.Rect(0, 0, 2, 3, s);
                c.Set(1, 0, d);
                c.Set(0, 2, l);
                var mid = new Vector2(0.5f, 0.5f);
                return s_Gobs = new[] { Art.ToSprite(a, mid), Art.ToSprite(b, mid), Art.ToSprite(c, mid) };
            }
        }

        /// <summary>Three variants of a tile-wide strip of toxic mud, its bottom on the floor.</summary>
        public static Sprite[] MudStrips => s_Mud ?? (s_Mud = Strips(Mud, new Color32(0x7a, 0x56, 0x34, 255), new Color32(0x3e, 0x28, 0x16, 255), Toxic, 5));

        /// <summary>Three variants of a strip of the sludge his spew leaves, flecked with embers.</summary>
        public static Sprite[] SludgeStrips => s_Sludge ?? (s_Sludge = Strips(SpewDark, Spew, new Color32(0x3c, 0x4a, 0x10, 255), Ember, 9));

        static Sprite[] Strips(Color32 body, Color32 wet, Color32 dark, Color32 glint, int seed)
        {
            var strips = new Sprite[3];
            var rng = new System.Random(seed);
            for (int v = 0; v < 3; v++)
            {
                var c = new PixelCanvas(16, 7);
                for (int x = 0; x < 16; x++)
                {
                    int h = 3 + (int)(1.6f * Mathf.Sin((x + v * 5) * 0.9f)) + rng.Next(0, 2);
                    for (int y = 0; y < h; y++) c.Set(x, y, y == h - 1 ? wet : y == 0 ? dark : body);
                    if (rng.Next(0, 5) == 0) c.Set(x, h - 1, glint); // a glint of the poison (or the fire) in it
                }
                // a drip hanging over the edge of the strip
                c.Rect(rng.Next(2, 14), 0, 1, 2, dark);
                strips[v] = Art.ToSprite(c, Vector2.zero);
            }
            return strips;
        }

        /// <summary>
        /// <paramref name="sprite"/> cut into horizontal bands <paramref name="band"/> pixels tall, bottom first. Each
        /// band keeps the sprite's pivot, so drawn at the same spot they make up the whole sprite again.
        /// </summary>
        public static Sprite[] Bands(Sprite sprite, int band)
        {
            if (s_Bands.TryGetValue(sprite, out Sprite[] cached)) return cached;
            Rect r = sprite.rect;
            int count = Mathf.CeilToInt(r.height / band);
            var bands = new Sprite[count];
            Vector2 pivot = sprite.pivot; // in pixels
            for (int i = 0; i < count; i++)
            {
                float y = r.y + i * band, h = Mathf.Min(band, r.yMax - y);
                var rect = new Rect(r.x, y, r.width, h);
                var p = new Vector2(pivot.x / r.width, (pivot.y - i * band) / h);
                bands[i] = Sprite.Create(sprite.texture, rect, p, Art.PPU, 0, SpriteMeshType.FullRect);
            }
            s_Bands[sprite] = bands;
            return bands;
        }
    }
}
