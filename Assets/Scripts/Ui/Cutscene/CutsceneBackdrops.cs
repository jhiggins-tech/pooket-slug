using UnityEngine;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' sets (<see cref="Staging.Sets"/>), each painted pixel by pixel once, the first time a scene plays,
    /// and kept: the motor pool from outside with its blast shutter, on Mission one's plates with the hangar drawn over
    /// them; the creamery's loading yard before the cold store, put together from Mission two's plates (each drawn in
    /// code instead for a build without its plates); and Design's paintings (<c>Resources/Den</c>, through
    /// <see cref="DenRoom.Painting"/>): the street outside the hangout on a snowy night (plate B-2, with the hangout's door
    /// and lit window cut from it as props, <see cref="CutsceneCast.MakeHangout"/>; the code-drawn street stands in if
    /// it's missing), and the den and the tabk bay on raid night. The
    /// bay is wider than the stage, so a shot there pans along it with its camera. The motor pool's layout (the
    /// shutter's opening, where the row's tabks park) is here too, since the shutter, the work lights and the row are
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
            s_MotorPool = PaintedMotorPool() ?? Backdrop(DrawMotorPool);
            s_ColdStore = PaintedColdStore() ?? Backdrop(DrawColdStore);
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

        /// <summary>
        /// The motor pool, from outside, on Mission one's own plates (Design's hand-drawn set, motor-pool-set-build): the
        /// sky plate, the far plate's hills, the middle plate's wall on the left, and the hangar drawn over it in the plates'
        /// blues: its speckled facade with snow on the parapet, the mouth (the shutter's opening, x 100-316, 100 high) with
        /// the dark bay, racks, a hoist and the gantry's lamps over the row behind it, and the snowy apron. Null if the
        /// plates aren't in the build.
        /// </summary>
        static Sprite PaintedMotorPool()
        {
            Texture2D sky = PlateTexture("sky-0", 1), far = PlateTexture("far-2", 1), mid = PlateTexture("mid-6", 1);
            if (sky == null || far == null || mid == null) return null;
            int floor = Staging.Sets["motorpool"];
            return Backdrop(c =>
            {
                var rng = new System.Random(3);
                Blit(c, sky, 0, 0);
                Blit(c, far, 0, floor + 30);
                Blit(c, mid, -150, floor);
                DrawHangar(c, floor, rng);
                DrawApron(c, floor, rng);
            });
        }

        // the plates' blues, picked by Design for the hand-drawn hangar
        static readonly Color32 HHi = Art.Hex(0x4366a4), HWall = Art.Hex(0x2b4f8e), HWall2 = Art.Hex(0x234a8e), HMid = Art.Hex(0x1c3f81),
            HSh = Art.Hex(0x103172), HDeep = Art.Hex(0x062970), HDk = Art.Hex(0x071b4f), HDkr = Art.Hex(0x05123f), HInk = Art.Hex(0x03092a),
            HSnow = Art.Hex(0xd8e2f2), HSnow2 = Art.Hex(0x7c93b7), HGrey = Art.Hex(0x647594), HGrey2 = Art.Hex(0x415170),
            HPlum = Art.Hex(0x302848), HNavy = Art.Hex(0x0f1f4a), HLamp = Art.Hex(0xffe6a0), HHz = Art.Hex(0xb08a28), HHz2 = Art.Hex(0x6a5a30);

        /// <summary>Fills a box with pixels picked at random from <paramref name="tones"/>, each as often as its weight: the plates' speckle.</summary>
        static void Noisy(PixelCanvas c, System.Random rng, int x0, int y0, int w, int h, Color32[] tones, int[] weights)
        {
            int total = 0;
            foreach (int wt in weights) total += wt;
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++)
                {
                    int r = rng.Next(total), i = 0;
                    while (r >= weights[i]) r -= weights[i++];
                    c.Set(x, y, tones[i]);
                }
        }

        static void DrawHangar(PixelCanvas c, int floor, System.Random rng)
        {
            int ox = (int)MotorOpenX, ow = (int)MotorOpenW, oh = (int)MotorOpenH;
            int top = floor + 132, l = ox - 12, r = StageW;
            // the facade: panels with the plate's speckle, pilasters, a parapet with snow
            Noisy(c, rng, l, floor, r - l, top - floor, new[] { HWall, HWall2, HMid }, new[] { 6, 3, 2 });
            for (int x = l; x < r; x += 38)
            {
                Noisy(c, rng, x, floor, 6, top - floor, new[] { HHi, HWall }, new[] { 3, 1 });
                c.Rect(x + 5, floor, 1, top - floor, HSh);
                c.Rect(x, top - 2, 6, 2, HSnow2);
            }
            for (int y = floor + 112; y < top - 4; y += 7) c.Rect(l, y, r - l, 1, HMid); // the panel seams above the mouth
            c.Rect(l, top, r - l, 4, HGrey);
            c.Rect(l, top + 4, r - l, 2, HSnow);
            for (int x = l; x < r; x++)
            {
                if (rng.NextDouble() < 0.35) c.Set(x, top + 6, HSnow);
                if (rng.NextDouble() < 0.15) c.Set(x, top + 3, HSnow2);
            }
            c.Rect(l, floor, 2, top - floor + 6, HInk);
            c.Rect(l + 2, floor, 1, top - floor + 4, HHi);
            // the mouth's frame: dark speckled steel, like the plate's door frames, snow on its lintel
            int fx0 = ox - 6, fx1 = ox + ow + 6, fy1 = floor + oh + 6;
            Noisy(c, rng, fx0, floor, ow + 12, oh + 6, new[] { HPlum, HGrey2, HInk, HNavy }, new[] { 4, 3, 2, 2 });
            c.Rect(fx0, fy1, ow + 12, 2, HGrey);
            c.Rect(fx0, fy1 + 2, ow + 12, 1, HSnow);
            for (int x = fx0; x < fx1; x++) if (rng.NextDouble() < 0.4) c.Set(x, fy1 + 3, HSnow);
            // a thin, worn hazard band over the mouth
            for (int x = fx0 + 4; x < fx1 - 4; x++) c.Rect(x, fy1 - 4, 1, 2, ((x + 2) / 4) % 2 == 0 ? HHz : HInk);
            // inside: the bay's dark
            for (int y = floor; y < floor + oh; y++) c.Rect(ox, y, ow, 1, (y - floor) / (float)oh > 0.35f ? HDkr : HDk);
            // the back wall, set in from the mouth, with racks and a chain hoist in silhouette
            int bw0 = ox + 18, bw1 = ox + ow - 18, bf = floor + 10;
            Noisy(c, rng, bw0, bf, bw1 - bw0, oh - 24, new[] { HNavy, HDk }, new[] { 3, 2 });
            for (int x = bw0; x < bw1; x += 26) c.Rect(x, bf, 1, oh - 24, HDkr);
            foreach (int x0 in new[] { bw0 + 10, bw0 + 70, bw0 + 140 }) // racks of tyres and parts
            {
                c.Rect(x0, bf, 2, 34, HInk);
                c.Rect(x0 + 22, bf, 2, 34, HInk);
                foreach (int y in new[] { bf + 10, bf + 22, bf + 33 }) c.Rect(x0, y, 24, 1, HInk);
                for (int i = 0; i < 3; i++)
                {
                    c.Rect(x0 + 4 + i * 6, bf + 11, 4, 4, HPlum);
                    c.Rect(x0 + 3 + i * 7, bf + 23, 5, 3, HGrey2);
                }
            }
            c.Rect(bw0 + 112, bf + 40, 1, oh - 66, HGrey2); // the hoist's chain and hook
            c.Rect(bw0 + 109, bf + 37, 7, 3, HGrey2);
            // the side walls, angled in to the back wall
            for (int y = floor; y < floor + oh; y++)
            {
                c.Rect(ox, y, 18, 1, HInk);
                c.Rect(ox + ow - 18, y, 18, 1, HInk);
            }
            for (int i = 0; i < 18; i++)
            {
                int h = System.Math.Max(0, 10 - i / 2);
                c.Rect(ox + i, floor, 1, h, HDk);
                c.Rect(ox + ow - 1 - i, floor, 1, h, HDk);
                c.Set(ox + i, floor + 10 + i / 2, HNavy);
                c.Set(ox + ow - 1 - i, floor + 10 + i / 2, HNavy);
            }
            Noisy(c, rng, ox + 18, floor, ow - 36, 10, new[] { HDk, HDeep }, new[] { 4, 1 });
            // roof trusses across the top of the mouth, the gantry, and its lamps over the row
            for (int x = ox; x < ox + ow; x += 12)
                for (int k = 0; k < 6; k++)
                {
                    c.Set(x + k, floor + oh - 20 + k, HNavy);
                    c.Set(x + 11 - k, floor + oh - 20 + k, HNavy);
                }
            c.Rect(ox, floor + oh - 14, ow, 3, HGrey2);
            c.Rect(ox, floor + oh - 14, ow, 1, HGrey);
            for (int x = ox + 6; x < ox + ow; x += 10) c.Rect(x, floor + oh - 11, 1, 5, HGrey2);
            foreach (float rx in MotorRow)
            {
                c.Rect((int)rx + 2, floor + oh - 14, 8, 3, HSnow2);
                c.Rect((int)rx + 4, floor + oh - 12, 4, 1, HLamp);
            }
            // the bay floor's lip and painted lines
            c.Rect(ox, floor, ow, 3, HDeep);
            c.Rect(ox, floor + 3, ow, 1, HNavy);
            for (int x = ox + 12; x < ox + ow; x += 50) c.Rect(x, floor + 1, 10, 1, HHz2);
            // the lamp box over the mouth, like the plate's over its doors, and a stencilled bay number
            int bx = ox + ow / 2 - 10;
            c.Rect(bx, fy1 + 10, 20, 7, HInk);
            c.Rect(bx + 1, fy1 + 11, 18, 5, HSnow2);
            for (int i = 0; i < 3; i++) c.Rect(bx + 4 + i * 5, fy1 + 13, 2, 1, HSnow);
            int[] zero = { 0, 0, 1, 0, 2, 0, 0, 1, 2, 1, 0, 2, 2, 2, 0, 3, 2, 3, 0, 4, 1, 4, 2, 4 }, seven = { 0, 4, 1, 4, 2, 4, 2, 3, 2, 2, 1, 1, 1, 0 };
            for (int i = 0; i < zero.Length; i += 2) c.Set(ox + 20 + zero[i], fy1 + 16 - zero[i + 1], HGrey); // "07"
            for (int i = 0; i < seven.Length; i += 2) c.Set(ox + 26 + seven[i], fy1 + 16 - seven[i + 1], HGrey);
            // a wall light by the mouth, and barrels at its foot like the plate's
            c.Rect(ox - 11, floor + 70, 4, 3, HInk);
            c.Rect(ox - 10, floor + 71, 2, 1, HLamp);
            foreach (int b in new[] { ox - 30, ox - 22 })
            {
                Noisy(c, rng, b, floor, 7, 10, new[] { HPlum, HGrey2 }, new[] { 3, 2 });
                c.Rect(b, floor + 10, 7, 1, HSnow);
                c.Rect(b, floor + 3, 7, 1, HInk);
                c.Rect(b, floor + 7, 7, 1, HInk);
            }
        }

        /// <summary>The apron in front of the hangar: speckled dark tarmac, a snowy edge, faded lines and expansion joints.</summary>
        static void DrawApron(PixelCanvas c, int floor, System.Random rng)
        {
            Noisy(c, rng, 0, 0, StageW, floor, new[] { HNavy, HDk, HDeep }, new[] { 5, 3, 1 });
            c.Rect(0, floor - 1, StageW, 1, HSnow2);
            c.Rect(0, floor - 2, StageW, 1, HGrey2);
            for (int x = 0; x < StageW; x++) if (rng.NextDouble() < 0.3) c.Set(x, floor - 1, HSnow);
            for (int x = 8; x < StageW; x += 32) c.Rect(x, 7, 12, 1, HHz2);
            for (int x = 0; x < StageW; x += 16) c.Rect(x, 0, 1, floor - 2, HInk);
        }

        /// <summary>The motor pool, drawn, for a build without Mission one's plates: the base's wall at night, and the big opening the shutter covers, with the bay behind it.</summary>
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
        /// The creamery's loading yard before the cold store, from Mission two's own plates (Design: arena A1 is the
        /// freeing scene's set): the far plate behind, the arena's middle plate (the wall, the drip sign, the cold store's
        /// shut doors, centred at x 261) over it, both standing on the floor, the sky plate above, and the yard's icy floor
        /// drawn below. Null if the plates aren't in the build.
        /// </summary>
        static Sprite PaintedColdStore()
        {
            Texture2D sky = PlateTexture("sky-0"), far = PlateTexture("far-0"), mid = PlateTexture("mid-2");
            if (sky == null || far == null || mid == null) return null;
            int floor = Staging.Sets["coldstore"];
            return Backdrop(c =>
            {
                Blit(c, sky, 0, 0);
                Blit(c, far, 0, floor);
                Blit(c, mid, 0, floor);
                c.Rect(0, 0, StageW, floor, Art.Hex(0x9fb0c8));
                c.Rect(0, floor - 1, StageW, 1, Art.Hex(0xe4f0ff));
                for (int x = 8; x < StageW; x += 34) c.Rect(x, 5, 18, 1, Art.Hex(0xc0d2e6));
            });
        }

        static Texture2D PlateTexture(string name, int mission = 2)
        {
            var file = Resources.Load<TextAsset>("Plates/mission-" + mission + "/" + name);
            if (file == null) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return tex.LoadImage(file.bytes) ? tex : null;
        }

        /// <summary>Draws <paramref name="tex"/> with its bottom left at (<paramref name="x0"/>, <paramref name="y0"/>), its see-through pixels left out.</summary>
        static void Blit(PixelCanvas c, Texture2D tex, int x0, int y0)
        {
            Color32[] px = tex.GetPixels32();
            for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                {
                    Color32 p = px[y * tex.width + x];
                    if (p.a > 127) c.Set(x0 + x, y0 + y, p);
                }
        }

        /// <summary>
        /// The creamery's cold store, drawn, for a build without Mission two's plates: frosted steel panels, racks of tubs behind, cold strip lights, the
        /// cold store's door frame on the right where the plate has it (its opening is a prop, <c>store-opening</c>) and an
        /// icy floor.
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
            // racks of tubs along the wall
            for (int rack = 0; rack < 2; rack++)
            {
                int rx = 30 + rack * 76, ry = floor;
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
            // the cold store's shut doors in their frame, with a hazard edge
            c.Rect(207, floor + 9, 108, 92, Art.Hex(0x0d151e));
            c.Rect(209, floor + 11, 104, 87, Art.Hex(0xd8e6f2));
            c.Rect(260, floor + 11, 2, 87, Art.Hex(0x0d151e));
            for (int x = 207; x < 315; x++) c.Rect(x, floor + 101, 1, 3, ((x - 207) / 4) % 2 == 0 ? Art.Hex(0xe0b020) : Art.Hex(0x1a1a1a));
            // the floor: pale, icy, with a sheen
            c.Rect(0, 0, StageW, floor, Art.Hex(0x9fb8cc));
            c.Rect(0, floor - 1, StageW, 1, Art.Hex(0xdff0ff));
            for (int x = 8; x < StageW; x += 34) c.Rect(x, 5, 18, 1, Art.Hex(0xc8deee));
        }

        /// <summary>The motor pool's blast shutter: steel slats with a hazard edge along the bottom.</summary>
        static void DrawShutter(PixelCanvas c)
        {
            // steel slats in the hangar's blues, a worn hazard strip along the bottom
            c.Rect(0, 0, c.W, c.H, HGrey);
            for (int y = 3; y < c.H; y += 6) c.Rect(0, y, c.W, 1, HGrey2);
            for (int y = 0; y < c.H; y += 6) c.Rect(0, y + 1, c.W, 1, HSnow2);
            for (int x = 0; x < c.W; x++) c.Rect(x, 0, 1, 4, (x / 6) % 2 == 0 ? HHz : HInk);
        }
    }
}
