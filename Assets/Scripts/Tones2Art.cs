using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// tones2's liquids, made in code like the rest of the art: his yellow water, the toxic mud of his rocket jump,
    /// and his spew and the sludge it leaves. Built the first time they're asked for.
    /// </summary>
    public static class Tones2Art
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

        static Sprite s_TabkHull, s_TabkBarrel;
        static Sprite[] s_TabkDrone;

        /// <summary>
        /// His tabk's hull (<c>docs/design/tanks.md</c>, tones2's tabk): the tabk's shape in orange and black, with a black
        /// jack-o'-lantern face stencilled on its flank, so the hull is the pumpkin. Its gun is drawn on its own, as the boss tabk's is (<see cref="Art.TankHull"/>).
        /// </summary>
        public static Sprite TabkHull
        {
            get
            {
                if (s_TabkHull != null) return s_TabkHull;
                var rows = Recolour(ArtData.Tank, "toTOueDedDmDMo");
                for (int r = 4; r <= 5; r++) rows[r] = new string('.', 22) + rows[r].Substring(22); // the gun is drawn on its own
                string[] pumpkin =
                {
                    ".e.......e.",
                    "eee..e..eee",
                    "...........",
                    "e.eeeeeee.e",
                    ".eee.e.eee.",
                };
                for (int r = 0; r < pumpkin.Length; r++)
                {
                    char[] row = rows[8 + r].ToCharArray();
                    for (int i = 0; i < pumpkin[r].Length; i++)
                        if (pumpkin[r][i] != '.') row[24 + i] = pumpkin[r][i];
                    rows[8 + r] = new string(row);
                }
                return s_TabkHull = Art.FromRows(rows, new Vector2(0.5f, 0f));
            }
        }

        /// <summary>His tabk's gun, black, pointing left from its pivot (as <see cref="Art.TankBarrel"/>).</summary>
        public static Sprite TabkBarrel =>
            s_TabkBarrel ?? (s_TabkBarrel = Art.FromRows(new[] { new string('D', 20), new string('e', 20) }, new Vector2(20.5f / 22f, 0.5f)));

        /// <summary>The crew's cargo drone, in his orange and black: the drone's shape, two frames of its rotors.</summary>
        public static Sprite[] TabkDrone
        {
            get
            {
                if (s_TabkDrone != null) return s_TabkDrone;
                const string swap = "DodezdMery";
                var mid = new Vector2(0.5f, 0.5f);
                return s_TabkDrone = new[] { Art.FromRows(Recolour(ArtData.Drone1, swap), mid), Art.FromRows(Recolour(ArtData.Drone2, swap), mid) };
            }
        }

        /// <summary>Art rows with letters swapped, <paramref name="pairs"/> as from-to pairs ("tT" swaps t for T).</summary>
        static string[] Recolour(string[] rows, string pairs)
        {
            var copy = new string[rows.Length];
            for (int r = 0; r < rows.Length; r++)
            {
                char[] row = rows[r].ToCharArray();
                for (int i = 0; i < row.Length; i++)
                    for (int k = 0; k + 1 < pairs.Length; k += 2)
                        if (row[i] == pairs[k])
                        {
                            row[i] = pairs[k + 1];
                            break;
                        }
                copy[r] = new string(row);
            }
            return copy;
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
    }
}
