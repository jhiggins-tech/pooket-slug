using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Sprites for the spikes, made in code like the rest of the art: tones2's yellow water and toxic mud, and
    /// the slices a hologram breaks into when it glitches out. Built the first time a spike asks for them.
    /// </summary>
    public static class SpikeArt
    {
        // tones2's water is yellow, as in Pooket Tabks; his mud is dark and wet, with toxic green in it.
        public static readonly Color Water = new Color32(0xff, 0xcc, 0x1f, 255);
        public static readonly Color WaterLight = new Color32(0xff, 0xf0, 0xa0, 255);
        public static readonly Color Mud = new Color32(0x5c, 0x3e, 0x24, 255);
        public static readonly Color Toxic = new Color32(0x9b, 0xe2, 0x2d, 255);

        static Sprite[] s_Drops, s_Mud;
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

        /// <summary>Three variants of a tile-wide strip of toxic mud, its bottom on the floor.</summary>
        public static Sprite[] MudStrips
        {
            get
            {
                if (s_Mud != null) return s_Mud;
                Color32 mud = Mud, wet = new Color32(0x7a, 0x56, 0x34, 255), dark = new Color32(0x3e, 0x28, 0x16, 255), toxic = Toxic;
                s_Mud = new Sprite[3];
                var rng = new System.Random(5);
                for (int v = 0; v < 3; v++)
                {
                    var c = new PixelCanvas(16, 7);
                    for (int x = 0; x < 16; x++)
                    {
                        int h = 3 + (int)(1.6f * Mathf.Sin((x + v * 5) * 0.9f)) + rng.Next(0, 2);
                        for (int y = 0; y < h; y++) c.Set(x, y, y == h - 1 ? wet : y == 0 ? dark : mud);
                        if (rng.Next(0, 5) == 0) c.Set(x, h - 1, toxic); // a glint of the poison in it
                    }
                    // a drip hanging over the edge of the strip
                    c.Rect(rng.Next(2, 14), 0, 1, 2, dark);
                    s_Mud[v] = Art.ToSprite(c, Vector2.zero);
                }
                return s_Mud;
            }
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
