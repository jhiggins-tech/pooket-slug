using UnityEngine;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' sets (<see cref="Staging.Sets"/>): the motor pool from outside with its blast shutter, painted pixel
    /// by pixel once, the first time a scene plays, and kept; and Design's paintings (<c>Resources/Den</c>, through
    /// <see cref="DenRoom.Painting"/>): the street outside the hangout on a snowy night (plate B-2, with the hangout's door
    /// and lit window cut from it as props, <see cref="CutsceneCast.MakeHangout"/>; the code-drawn street stands in if
    /// it's missing), and the den and the tabk bay on raid night. The
    /// bay is wider than the stage, so a shot there pans along it with its camera. The motor pool's layout (the
    /// shutter's opening, where the row's tabks park) is here too. The creamery's cold store is code-drawn too, until
    /// Design paints one, since the shutter, the work lights and the row are
    /// drawn to fit it.
    /// </summary>
    public static class CutsceneBackdrops
    {
        /// <summary>The motor pool: the shutter's opening (its left edge, width and height, in art pixels).</summary>
        public const float MotorOpenX = 100f, MotorOpenW = 216f, MotorOpenH = 100f;
        /// <summary>Where the row's tabks park (the first space is tones2's).</summary>
        public static readonly float[] MotorRow = { 186f, 236f, 286f };

        static Sprite s_Street, s_MotorPool, s_ColdStore, s_Slats;

        /// <summary>The backdrop for set <paramref name="set"/> (the street for none, or one it doesn't know).</summary>
        public static Sprite For(string set)
        {
            Make();
            Sprite painted = set == "den" ? DenRoom.Painting("raid-den") : set == "bay" ? DenRoom.Painting("raid-bay")
                : set == "motorpool" || set == "coldstore" ? null : DenRoom.Painting("street");
            return painted != null ? painted : set == "motorpool" ? s_MotorPool : set == "coldstore" ? s_ColdStore : s_Street;
        }

        /// <summary>The motor pool's blast shutter, the size of its opening.</summary>
        public static Sprite Slats
        {
            get
            {
                Make();
                return s_Slats;
            }
        }

        static void Make()
        {
            if (s_Street != null) return;
            s_Street = Backdrop(DrawStreet);
            s_MotorPool = Backdrop(DrawMotorPool);
            s_ColdStore = Backdrop(DrawColdStore);
            var slats = new PixelCanvas((int)MotorOpenW, (int)MotorOpenH);
            DrawShutter(slats);
            s_Slats = Sprite.Create(Art.ToTexture(slats), new Rect(0, 0, MotorOpenW, MotorOpenH), Vector2.zero, 1f);
        }

        static void DrawStreet(PixelCanvas c)
        {
            int street = Staging.Sets["street"];
            Sky(c, 0x0b0f24, 0x1d2448);
            var rng = new System.Random(7);
            for (int i = 0; i < 60; i++) c.Set(rng.Next(StageW), 70 + rng.Next(StageH - 70), Art.Hex(0xc8d0ff, (byte)(120 + rng.Next(120))));
            // the town behind
            for (int x = 0, i = 0; x < 150; x += 22 + i % 3 * 6, i++) c.Rect(x, street, 18 + i % 2 * 8, 30 + (i * 13) % 34, Art.Hex(0x161a33));
            for (int i = 0; i < 18; i++) c.Rect(4 + i * 8 % 140, street + 8 + i * 11 % 40, 2, 3, Art.Hex(0x6a5a2a));
            // the hangout: a garage with a roll door and a side door
            c.Rect(150, street, 156, 96, Art.Hex(0x3a2a2e));
            for (int y = street + 4; y < street + 96; y += 6)
                for (int x = 150 + (y / 6 % 2) * 6; x < 306; x += 12) c.Rect(x, y, 1, 5, Art.Hex(0x2e2024));
            c.Rect(146, street + 96, 164, 6, Art.Hex(0x241820));
            c.Rect(160, street, 50, 54, Art.Hex(0x5b5f66));
            for (int y = street + 3; y < street + 54; y += 5) c.Rect(160, y, 50, 1, Art.Hex(0x45484e));
            c.Rect(265, street, 18, 63, Art.Hex(0x1c1214)); // the side door's frame, where the plate has it
            c.Rect(302, street + 36, 18, 26, Art.Hex(0x1c1214)); // the window's frame
            // the street
            c.Rect(0, 0, StageW, street, Art.Hex(0x2a2c38));
            c.Rect(0, street - 3, StageW, 3, Art.Hex(0x4a4c5a));
            for (int x = 6; x < StageW; x += 28) c.Rect(x, 9, 12, 2, Art.Hex(0x55576a));
            // snow on the ground and roof
            c.Rect(0, street - 1, StageW, 1, Art.Hex(0xdfe6ff));
            c.Rect(146, street + 102, 164, 2, Art.Hex(0xdfe6ff));
            // a streetlight
            c.Rect(110, street, 2, 70, Art.Hex(0x22242e));
            c.Rect(106, street + 68, 10, 3, Art.Hex(0x22242e));
            c.Circle(111, street + 66, 2.5f, Art.Hex(0xffe6a0));
        }

        /// <summary>The motor pool, from outside: the base's wall at night, and the big opening the shutter covers, with the bay behind it.</summary>
        static void DrawMotorPool(PixelCanvas c)
        {
            int floor = Staging.Sets["motorpool"], ox = (int)MotorOpenX, ow = (int)MotorOpenW, oh = (int)MotorOpenH;
            Sky(c, 0x0b0f24, 0x1d2448);
            var rng = new System.Random(11);
            for (int i = 0; i < 50; i++) c.Set(rng.Next(StageW), 140 + rng.Next(StageH - 140), Art.Hex(0xc8d0ff, (byte)(120 + rng.Next(120))));
            // the base's wall: steel panels, rivets
            c.Rect(0, floor, StageW, 128, Art.Hex(0x2c3442));
            for (int x = 0; x < StageW; x += 32) c.Rect(x, floor, 1, 128, Art.Hex(0x232a36));
            for (int y = floor + 20; y < floor + 128; y += 26)
                for (int x = 6; x < StageW; x += 16) c.Set(x, y, Art.Hex(0x4a5466));
            c.Rect(0, floor + 126, StageW, 4, Art.Hex(0x1c222c));
            // the bay behind the shutter: dark, with a back wall, a gantry and the lamps' fittings
            c.Rect(ox - 6, floor, ow + 12, oh + 8, Art.Hex(0x1a1e26)); // the frame
            c.Rect(ox, floor, ow, oh, Art.Hex(0x101318));
            for (int x = ox + 4; x < ox + ow; x += 24) c.Rect(x, floor + 10, 1, oh - 10, Art.Hex(0x161a22));
            c.Rect(ox, floor + oh - 14, ow, 3, Art.Hex(0x242a34)); // the gantry
            for (int x = ox + 6; x < ox + ow; x += 10) c.Rect(x, floor + oh - 11, 1, 6, Art.Hex(0x242a34));
            foreach (float rx in MotorRow) c.Rect((int)rx + 2, floor + oh - 14, 8, 3, Art.Hex(0x5a5a50));
            // cables snaking over the bay floor, and the paint of the parking spaces
            for (int x = ox; x < ox + ow; x++) c.Set(x, floor + 3 + (int)(System.Math.Sin(x * 0.11) * 1.5), Art.Hex(0x0a0c10));
            for (int x = ox + 12; x < ox + ow; x += 50) c.Rect(x, floor, 2, 2, Art.Hex(0xc8b040));
            // hazard stripes over the opening
            for (int x = ox - 6; x < ox + ow + 6; x++)
                c.Rect(x, floor + oh + 8, 1, 6, ((x - ox) / 5) % 2 == 0 ? Art.Hex(0xe0b020) : Art.Hex(0x1a1a1a));
            c.Rect(14, floor + 54, 40, 8, Art.Hex(0x1a1e26)); // a sign plate by the door
            c.Rect(16, floor + 56, 36, 4, Art.Hex(0xc0a030));
            // the apron outside
            c.Rect(0, 0, StageW, floor, Art.Hex(0x30343e));
            c.Rect(0, floor - 1, StageW, 1, Art.Hex(0x4c5262));
            for (int x = 4; x < StageW; x += 26) c.Rect(x, 6, 14, 2, Art.Hex(0x4a4a3c));
        }

        /// <summary>
        /// The creamery's cold store, from inside: frosted steel panels, racks of tubs behind, cold strip lights, the
        /// door's frame on the left (its sliding door is a prop, <c>store-door</c>) and an icy floor.
        /// </summary>
        static void DrawColdStore(PixelCanvas c)
        {
            int floor = Staging.Sets["coldstore"];
            c.Rect(0, 0, StageW, StageH, Art.Hex(0x1b2a3a));
            // the walls: steel panels with frost creeping down from the top
            for (int x = 0; x < StageW; x += 28) c.Rect(x, floor, 1, StageH - floor, Art.Hex(0x152230));
            var rng = new System.Random(5);
            for (int x = 0; x < StageW; x++)
            {
                int drip = 6 + rng.Next(10);
                c.Rect(x, StageH - drip, 1, drip, Art.Hex(0xb8d4ea, (byte)(150 + rng.Next(80))));
            }
            // cold strip lights
            for (int x = 30; x < StageW; x += 90)
            {
                c.Rect(x, StageH - 22, 40, 3, Art.Hex(0xe8f6ff));
                c.Rect(x - 4, StageH - 26, 48, 3, Art.Hex(0x2a3c50));
            }
            // racks of tubs along the back wall, past the door
            for (int rack = 0; rack < 3; rack++)
            {
                int rx = 110 + rack * 70, ry = floor;
                c.Rect(rx, ry, 2, 92, Art.Hex(0x3a4c60));
                c.Rect(rx + 56, ry, 2, 92, Art.Hex(0x3a4c60));
                for (int shelf = 0; shelf < 4; shelf++)
                {
                    int sy = ry + 18 + shelf * 22;
                    c.Rect(rx, sy, 58, 2, Art.Hex(0x4a5e74));
                    for (int tub = 0; tub < 5; tub++)
                    {
                        uint lid = (uint)((rack + shelf + tub) % 3 == 0 ? 0xe090b0 : (rack + shelf + tub) % 3 == 1 ? 0xf0e0b0 : 0x9fd0a0);
                        c.Rect(rx + 4 + tub * 11, sy + 2, 8, 6, Art.Hex(0xd8dce4));
                        c.Rect(rx + 4 + tub * 11, sy + 8, 8, 1, Art.Hex(lid));
                    }
                }
            }
            // the door's frame, with a hazard edge
            c.Rect(14, floor, 52, 80, Art.Hex(0x0d151e));
            c.Rect(12, floor + 78, 56, 4, Art.Hex(0x3a4c60));
            for (int x = 12; x < 68; x++) c.Rect(x, floor + 82, 1, 3, ((x - 12) / 4) % 2 == 0 ? Art.Hex(0xe0b020) : Art.Hex(0x1a1a1a));
            // the floor: pale, icy, with a sheen
            c.Rect(0, 0, StageW, floor, Art.Hex(0x9fb8cc));
            c.Rect(0, floor - 1, StageW, 1, Art.Hex(0xdff0ff));
            for (int x = 8; x < StageW; x += 34) c.Rect(x, 5, 18, 1, Art.Hex(0xc8deee));
        }

        /// <summary>The motor pool's blast shutter: steel slats with a hazard edge along the bottom.</summary>
        static void DrawShutter(PixelCanvas c)
        {
            c.Rect(0, 0, c.W, c.H, Art.Hex(0x5b5f66));
            for (int y = 3; y < c.H; y += 6) c.Rect(0, y, c.W, 1, Art.Hex(0x45484e));
            for (int y = 0; y < c.H; y += 6) c.Rect(0, y + 1, c.W, 1, Art.Hex(0x6c717a));
            for (int x = 0; x < c.W; x++) c.Rect(x, 0, 1, 4, (x / 6) % 2 == 0 ? Art.Hex(0xe0b020) : Art.Hex(0x1a1a1a));
        }
    }
}
