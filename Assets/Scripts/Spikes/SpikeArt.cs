using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Sprites for the spikes, made in code like the rest of the art: the boss tank's swinging turret and landing
    /// marker. Built the first time a spike asks for them.
    /// </summary>
    public static class SpikeArt
    {
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
    }
}
