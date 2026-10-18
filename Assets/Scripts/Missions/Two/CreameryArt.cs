using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Mission two's pieces, drawn in code in the creamery's pastels (concept A: pink icing, lilac, waffle) as stand-ins
    /// until Design's art lands: the conveyor belts' tiles and their moving tread, the scoop droppers and their scoops,
    /// the nitrogen vents and tanks, the boxes of cones and the cold store's doors.
    /// </summary>
    static class CreameryArt
    {
        static readonly Color32 Ink = Art.OutlineColor;
        static readonly Color32 Rubber = Art.Hex(0x2c2634), RubberLight = Art.Hex(0x4a4058), Chevron = Art.Hex(0xf6c2dc);
        static readonly Color32 Frame = Art.Hex(0x9a8cb4), FrameDark = Art.Hex(0x5e5278), Roller = Art.Hex(0xcfc6e0);
        static readonly Color32 Pink = Art.Hex(0xf48cbc), PinkLight = Art.Hex(0xffc8e2), PinkDark = Art.Hex(0xc0507e);
        static readonly Color32 Mint = Art.Hex(0x9ee8c8), Choc = Art.Hex(0x6a3e2a), Waffle = Art.Hex(0xd8a060), WaffleDark = Art.Hex(0xa06a34);
        static readonly Color32 Ice = Art.Hex(0xc8eeff), IceDark = Art.Hex(0x6ab4e0), Steel = Art.Hex(0xe8eef6), SteelDark = Art.Hex(0x98a4b8);
        static readonly Color32 Card = Art.Hex(0xc89a64), CardDark = Art.Hex(0x8a6234), Frost = Art.Hex(0xf4fbff);

        static Sprite s_Dropper, s_Scoop, s_Shadow, s_Vent, s_Box, s_Tank, s_Door, s_Shield, s_Heavy, s_BigScoop, s_Wave;
        static Sprite[] s_Tread;

        /// <summary>Draws a belt tile: the frame and rollers under a dark tread (the tread's chevrons move: <see cref="Tread"/>).</summary>
        public static void DrawBelt(PixelCanvas c, int ox, int oy)
        {
            c.Rect(ox, oy, 16, 16, FrameDark);
            c.Rect(ox, oy + 10, 16, 6, Rubber);
            c.Rect(ox, oy + 15, 16, 1, RubberLight);
            c.Rect(ox, oy + 9, 16, 1, Ink);
            for (int x = 1; x < 16; x += 5) c.Circle(ox + x + 1.5f, oy + 6.5f, 2.2f, Roller); // the rollers
            c.Rect(ox, oy, 16, 2, Frame);
        }

        /// <summary>The belt's tread, four frames of its chevrons moving right (flipped for a belt carrying left).</summary>
        public static Sprite[] Tread
        {
            get
            {
                if (s_Tread != null) return s_Tread;
                s_Tread = new Sprite[4];
                for (int f = 0; f < 4; f++)
                {
                    var c = new PixelCanvas(16, 6);
                    for (int x = 0; x < 16; x++)
                    {
                        int k = (x - f * 2 + 16) % 8; // a chevron every 8 px, 2 px a frame
                        if (k == 0 || k == 1) { c.Set(x, 2, Chevron); c.Set(x, 3, Chevron); }
                        if (k == 2) { c.Set(x, 1, Chevron); c.Set(x, 4, Chevron); }
                    }
                    s_Tread[f] = Art.ToSprite(c, Vector2.zero);
                }
                return s_Tread;
            }
        }

        /// <summary>A scoop dropper: a pink hopper with a nozzle under it, hung from the ceiling.</summary>
        public static Sprite Dropper
        {
            get
            {
                if (s_Dropper != null) return s_Dropper;
                var c = new PixelCanvas(22, 20);
                c.Rect(9, 14, 4, 6, SteelDark); // the hanger
                c.Rect(2, 7, 18, 8, Pink);
                c.Rect(2, 13, 18, 2, PinkLight);
                c.Rect(4, 9, 14, 1, PinkDark);
                c.Rect(7, 2, 8, 5, Steel);
                c.Rect(9, 0, 4, 2, SteelDark); // the nozzle
                c.Outline(Ink);
                return s_Dropper = Art.ToSprite(c, new Vector2(0.5f, 0f));
            }
        }

        /// <summary>A scoop of ice cream, falling.</summary>
        public static Sprite Scoop
        {
            get
            {
                if (s_Scoop != null) return s_Scoop;
                var c = new PixelCanvas(14, 13);
                c.Circle(7f, 6.5f, 6f, Pink);
                c.Circle(5.5f, 8f, 2.5f, PinkLight);
                c.Rect(2, 2, 10, 2, PinkDark);
                c.Set(9, 9, Mint);
                c.Set(4, 5, Choc);
                c.Outline(Ink);
                return s_Scoop = Art.ToSprite(c, new Vector2(0.5f, 0f));
            }
        }

        /// <summary>Where a scoop's about to land: a dark oval on the floor.</summary>
        public static Sprite Shadow
        {
            get
            {
                if (s_Shadow != null) return s_Shadow;
                var c = new PixelCanvas(16, 4);
                Color32 dark = new Color32(28, 20, 32, 140);
                c.Rect(2, 0, 12, 4, dark);
                c.Rect(0, 1, 16, 2, dark);
                return s_Shadow = Art.ToSprite(c, new Vector2(0.5f, 0f));
            }
        }

        /// <summary>A nitrogen vent: an icy grate in the floor.</summary>
        public static Sprite Vent
        {
            get
            {
                if (s_Vent != null) return s_Vent;
                var c = new PixelCanvas(16, 5);
                c.Rect(0, 0, 16, 5, IceDark);
                for (int x = 2; x < 15; x += 3) c.Rect(x, 1, 1, 3, Rubber);
                c.Rect(0, 4, 16, 1, Frost);
                c.Outline(Ink);
                return s_Vent = Art.ToSprite(c, new Vector2(0.5f, 0f));
            }
        }

        /// <summary>A box of cones: cardboard, a cone on its side.</summary>
        public static Sprite ConeBox
        {
            get
            {
                if (s_Box != null) return s_Box;
                var c = new PixelCanvas(16, 15);
                c.Rect(0, 0, 16, 15, Card);
                c.Rect(0, 12, 16, 3, CardDark);
                c.Rect(7, 12, 2, 3, Card); // the tape
                for (int i = 0; i < 6; i++) c.Rect(4 + i, 4 + i / 2, 1, 6 - i, i % 2 == 0 ? Waffle : WaffleDark); // the cone
                c.Circle(4f, 7f, 2.5f, Pink);
                c.Outline(Ink);
                return s_Box = Art.ToSprite(c, new Vector2(0.5f, 0f), withFlash: true);
            }
        }

        /// <summary>A nitrogen tank: a white cylinder, frosted, with a blue band.</summary>
        public static Sprite NitrogenTank
        {
            get
            {
                if (s_Tank != null) return s_Tank;
                var c = new PixelCanvas(13, 20);
                c.Rect(1, 0, 11, 18, Steel);
                c.Rect(1, 0, 3, 18, SteelDark);
                c.Rect(1, 7, 11, 4, IceDark);
                c.Rect(5, 8, 3, 2, Frost);
                c.Rect(4, 18, 5, 2, SteelDark); // the valve
                c.Rect(9, 1, 2, 5, Frost);
                c.Outline(Ink);
                return s_Tank = Art.ToSprite(c, new Vector2(0.5f, 0f), withFlash: true);
            }
        }

        /// <summary>The cold store's double doors: frosted steel, 3 tiles wide and 5 tall, icicles along the top.</summary>
        public static Sprite ColdStoreDoor
        {
            get
            {
                if (s_Door != null) return s_Door;
                var c = new PixelCanvas(48, 80);
                c.Rect(0, 0, 48, 80, SteelDark);
                c.Rect(2, 0, 21, 72, Ice);
                c.Rect(25, 0, 21, 72, Ice);
                for (int y = 6; y < 70; y += 8)
                {
                    c.Rect(3, y, 19, 1, Frost);
                    c.Rect(26, y, 19, 1, Frost);
                }
                c.Rect(19, 30, 2, 10, SteelDark); // the handles
                c.Rect(27, 30, 2, 10, SteelDark);
                c.Rect(0, 72, 48, 8, IceDark);
                for (int x = 2; x < 46; x += 5) c.Rect(x, 68 - x % 3 * 2, 2, 4 + x % 3 * 2, Frost); // icicles
                c.Outline(Ink);
                return s_Door = Art.ToSprite(c, new Vector2(0.5f, 0f));
            }
        }
        /// <summary>The shield soldier's riot shield: a tall slab with a viewport, held in front.</summary>
        public static Sprite Shield
        {
            get
            {
                if (s_Shield != null) return s_Shield;
                var c = new PixelCanvas(7, 20);
                c.Rect(0, 0, 7, 20, SteelDark);
                c.Rect(1, 1, 5, 18, Steel);
                c.Rect(2, 13, 3, 3, Rubber); // the viewport
                c.Rect(1, 6, 5, 1, SteelDark);
                c.Outline(Ink);
                return s_Shield = Art.ToSprite(c, new Vector2(0.5f, 0f), withFlash: true);
            }
        }

        /// <summary>The Big Scoop: an oversized heavy in a creamery apron, 2.5 tiles tall.</summary>
        public static Sprite Heavy
        {
            get
            {
                if (s_Heavy != null) return s_Heavy;
                var c = new PixelCanvas(28, 42);
                c.Rect(6, 0, 7, 12, SteelDark); // legs
                c.Rect(15, 0, 7, 12, SteelDark);
                c.Rect(3, 12, 22, 20, Steel); // the body
                c.Rect(7, 12, 14, 18, PinkLight); // the apron
                c.Rect(7, 26, 14, 2, Pink);
                c.Rect(0, 16, 4, 14, Steel); // arms
                c.Rect(24, 16, 4, 14, Steel);
                c.Rect(8, 32, 12, 9, Steel); // the head, a hairnet on top
                c.Rect(8, 38, 12, 3, Frost);
                c.Rect(15, 34, 3, 2, Rubber); // an eye, looking ahead
                c.Outline(Ink);
                return s_Heavy = Art.ToSprite(c, new Vector2(0.5f, 0f), withFlash: true);
            }
        }

        /// <summary>His giant scoop: a long handle and a bowl, held out in front (it turns as he swings).</summary>
        public static Sprite BigScoop
        {
            get
            {
                if (s_BigScoop != null) return s_BigScoop;
                var c = new PixelCanvas(30, 12);
                c.Rect(0, 5, 18, 3, SteelDark); // the handle, from the pivot at its left end
                c.Circle(23f, 6f, 6f, Steel);
                c.Circle(23f, 7.5f, 4.2f, Pink); // a scoop's worth in it
                c.Outline(Ink);
                return s_BigScoop = Art.ToSprite(c, new Vector2(0f, 0.5f), withFlash: true);
            }
        }

        /// <summary>The slam's shockwave: a curl of broken floor and icing, running along the ground.</summary>
        public static Sprite Shockwave
        {
            get
            {
                if (s_Wave != null) return s_Wave;
                var c = new PixelCanvas(14, 12);
                for (int x = 0; x < 14; x++)
                {
                    int h = 3 + (int)(8f * Mathf.Sin(x / 13f * Mathf.PI));
                    c.Rect(x, 0, 1, h, x % 3 == 0 ? WaffleDark : Waffle);
                    c.Set(x, h - 1, PinkLight);
                }
                c.Outline(Ink);
                return s_Wave = Art.ToSprite(c, new Vector2(0.5f, 0.5f));
            }
        }
    }
}
