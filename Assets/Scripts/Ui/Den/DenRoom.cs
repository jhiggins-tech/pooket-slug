using System.Collections.Generic;
using UnityEngine;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The den's room (<c>docs/design/lobby.md</c>): its layout (the loft, the couch's seats, the TV, each crewmate's
    /// station) and its painting, side-on in two floors, with the freed crew's stations lit and lived in and the rest
    /// wrecked and dark from the raid.
    /// </summary>
    public static class DenRoom
    {
        /// <summary>
        /// Where things are in the room: the drawn den's, or (gen-lobby-bg, <c>?spike=denbg</c>) the generated
        /// backdrop's, where the painting leads and the room's pieces move to fit it (Jack, 18 Oct).
        /// </summary>
        public sealed class Layout
        {
            public int Loft;
            public float[] SeatX;
            public Vector2 TvCentre, Sign, Board, Ports, Holo, Weasel;
            public Station[] Stations;
        }

        // docs/design/lobby.md, Who lives where (tones2's corner is the slushie machine and a beanbag: the couch is the players')
        public static readonly Layout Drawn = new Layout
        {
            Loft = 100, SeatX = new[] { 101f, 131f }, TvCentre = new Vector2(116f, 69f), Sign = new Vector2(299f, 97f),
            Board = new Vector2(250f, 69f), Ports = new Vector2(66f, Floor + 12f), Holo = new Vector2(22f, Floor + 26f), Weasel = new Vector2(14f, Floor + 5f),
            Stations = new[]
            {
                new Station("kie", Content.Kie, false, 4, 58, 40f),
                new Station("tones2", Content.Tones2, false, 156, 194, 184f),
                new Station("kiwicore", null, false, 198, 236, 216f),
                new Station("garyoldmancorp", null, false, 238, 262, 250f),
                new Station("kcaj", null, true, 4, 62, 34f),
                new Station("torikloud", null, true, 68, 126, 96f),
                new Station("ciarra", null, true, 132, 190, 160f),
                new Station("larinovsky", null, true, 196, 256, 226f),
            },
        };

        /// <summary>The generated backdrop (vendor art/den.json, combo-ladder__1: seedream-combo__1 with one ladder): the loft higher, the TV and couch right of centre.</summary>
        public static readonly Layout Painted = new Layout
        {
            Loft = 140, SeatX = new[] { 134f, 167f }, TvCentre = new Vector2(144f, 78f), Sign = new Vector2(298f, 104f),
            Board = new Vector2(233f, 76f), Ports = new Vector2(96f, Floor + 16f), Holo = new Vector2(45f, Floor + 32f), Weasel = new Vector2(27f, 72f),
            Stations = new[]
            {
                new Station("kie", Content.Kie, false, 8, 88, 45f),
                new Station("tones2", Content.Tones2, false, 188, 240, 210f),
                new Station("kiwicore", null, false, 240, 258, 250f),
                new Station("garyoldmancorp", null, false, 284, 318, 300f),
                new Station("kcaj", null, true, 4, 80, 36f),
                new Station("torikloud", null, true, 84, 160, 120f),
                new Station("ciarra", null, true, 164, 240, 200f),
                new Station("larinovsky", null, true, 290, 318, 305f),
            },
        };

        public static Layout Now => Spike.DenBg ? Painted : Drawn;
        public static int Loft => Now.Loft;
        public static float SeatY => Floor + 12f;
        public static float[] SeatX => Now.SeatX;
        public static Vector2 TvCentre => Now.TvCentre;
        public static Station[] Stations => Now.Stations;
        /// <summary>The controllers' purple, on the couch and in hand.</summary>
        public static readonly Color Pad = new Color(0.42f, 0.31f, 0.69f);

        /// <summary>A station: whose it is, which floor, where it spans, and where they stand at it.</summary>
        public sealed class Station
        {
            public readonly string Crewmate;
            public readonly SoldierDef Soldier; // null for a crewmate the game doesn't have yet
            public readonly bool Upstairs;
            public readonly int X0, X1;
            public readonly float Spot;

            public Station(string crewmate, SoldierDef soldier, bool upstairs, int x0, int x1, float spot)
            {
                Crewmate = crewmate;
                Soldier = soldier;
                Upstairs = upstairs;
                X0 = x0;
                X1 = x1;
                Spot = spot;
            }
        }

        public static bool Has(IReadOnlyList<SoldierDef> crew, SoldierDef s)
        {
            foreach (SoldierDef x in crew)
                if (x == s) return true;
            return false;
        }

        /// <summary>The den, side-on: the loft up a ladder, the couch, TV and console in the middle, the roller door on the right.</summary>
        public static void Draw(PixelCanvas c, IReadOnlyList<SoldierDef> freed)
        {
            // warm plank walls, with the raid's cyan scorch marks painted over badly
            for (int y = 0; y < StageH; y++) c.Rect(0, y, StageW, 1, (y / 6) % 2 == 0 ? Art.Hex(0x4a3330) : Art.Hex(0x3e2a28));
            for (int x = 0; x < StageW; x += 23) c.Rect(x, 0, 1, StageH, Art.Hex(0x33221f));
            var rng = new System.Random(5);
            for (int i = 0; i < 4; i++)
            {
                int sx = 30 + rng.Next(200), sy = 40 + rng.Next(110);
                for (int k = 0; k < 18; k++) c.Set(sx + rng.Next(-7, 8), sy + rng.Next(-4, 5), Art.Hex(0x3a5a60, 150));
                for (int k = 0; k < 30; k++) c.Set(sx + rng.Next(-6, 7), sy + rng.Next(-3, 4), Art.Hex(0x5a4a46));
            }
            // string lights under the loft and along the ceiling
            for (int row = 0; row < 2; row++)
            {
                int y0 = row == 0 ? StageH - 6 : Loft - 9;
                for (int x = 4; x < 262; x += 2) c.Set(x, y0 - (int)(2f * Mathf.Abs((x % 40) - 20) / 20f), Art.Hex(0x2a1c1a));
                uint[] bulbs = { 0xffd166, 0xff7eb6, 0x7cf7d4, 0xff8c42 };
                for (int i = 0, x = 8; x < 260; x += 14, i++) c.Circle(x, y0 - 1 - (int)(2f * Mathf.Abs((x % 40) - 20) / 20f), 1.3f, Art.Hex(bulbs[i % 4]));
            }

            // the floor
            c.Rect(0, 0, StageW, Floor, Art.Hex(0x3a3436));
            c.Rect(0, Floor - 1, StageW, 1, Art.Hex(0x5a5256));
            for (int x = 0; x < StageW; x += 16) c.Rect(x, 0, 1, Floor - 1, Art.Hex(0x302a2c));
            // the loft: its floor, railing, and the ladder up
            c.Rect(0, Loft - 6, 264, 6, Art.Hex(0x6b4a38));
            c.Rect(0, Loft - 1, 264, 1, Art.Hex(0x8a6448));
            c.Rect(0, Loft - 8, 264, 2, Art.Hex(0x2a1c1a));
            for (int x = 6; x < 264; x += 40) c.Rect(x, Loft - 14, 3, 6, Art.Hex(0x2a1c1a));
            c.Rect(0, Loft + 13, 264, 1, Art.Hex(0x6b4a38));
            for (int x = 0; x < 264; x += 12) c.Rect(x, Loft, 1, 14, Art.Hex(0x6b4a38));
            c.Rect(266, Floor, 2, Loft - Floor + 2, Art.Hex(0x8a6448));
            c.Rect(274, Floor, 2, Loft - Floor + 2, Art.Hex(0x8a6448));
            for (int y = Floor + 6; y < Loft; y += 8) c.Rect(266, y, 10, 1, Art.Hex(0xa07a58));

            // the roller door, the POOKET sign and a window on the night
            c.Rect(278, Floor, 42, 64, Art.Hex(0x1c1a22));
            for (int y = Floor + 2; y < Floor + 62; y += 4)
            {
                c.Rect(280, y, 38, 1, Art.Hex(0x5e6474));
                c.Rect(280, y + 1, 38, 1, Art.Hex(0x7a8090));
            }
            c.Rect(276, Floor + 62, 44, 4, Art.Hex(0x2a2a30));
            c.Rect(280, 90, 38, 14, Art.Hex(0x2a1c1a));
            c.Rect(281, 91, 36, 12, Art.Hex(0xc0503c));
            c.Rect(284, 112, 32, 36, Art.Hex(0x2a1c1a));
            c.Rect(286, 114, 28, 32, Art.Hex(0x0e1630));
            for (int i = 0; i < 7; i++) c.Set(288 + i * 4, 120 + (i * 7) % 22, Art.Hex(0xffffff));
            for (int y = 114; y < 146; y++) c.Set(292 + (y - 114) / 4, y, Art.Hex(0x9ab0ff, 90));
            c.Rect(299, 114, 1, 32, Art.Hex(0x2a1c1a));
            c.Rect(286, 129, 28, 1, Art.Hex(0x2a1c1a));

            // the mission board, a corkboard with the mission's card (its words are drawn over it)
            c.Rect(238, 58, 24, 22, Art.Hex(0x2a1c1a));
            c.Rect(239, 59, 22, 20, Art.Hex(0xb08850));
            c.Rect(241, 62, 18, 14, Art.Hex(0xe8e0d0));
            c.Circle(250, 76, 1.2f, Art.Hex(0xd04030));

            // the big TV, the cube console on its side table, and the controllers' cables to the couch
            c.Rect(84, 52, 64, 36, Art.Hex(0x1c1a22));
            c.Rect(86, 54, 60, 32, Art.Hex(0x1a3a5a));
            c.Rect(86, 54, 60, 10, Art.Hex(0x2a2050));
            c.Rect(114, 44, 4, 8, Art.Hex(0x2a2a30));
            c.Rect(60, Floor, 18, 10, Art.Hex(0x5a3e34));
            c.Rect(60, Floor + 9, 18, 1, Art.Hex(0x7a5a44));
            c.Rect(62, Floor + 10, 14, 12, Art.Hex(0x4a3a8a));
            c.Rect(62, Floor + 21, 14, 1, Art.Hex(0x6a5ab0));
            c.Rect(65, Floor + 22, 8, 3, Art.Hex(0x2a2050));
            c.Rect(66, Floor + 23, 6, 1, Art.Hex(0x4a3a8a)); // the handle
            c.Circle(69, Floor + 17, 1.8f, Art.Hex(0x8f74d8));
            Cable(c, (int)Drawn.Ports.x, Floor + 11, (int)SeatX[0], (int)SeatY);
            Cable(c, (int)Drawn.Ports.x + 8, Floor + 11, (int)SeatX[1], (int)SeatY);
            // the couch, facing us: two seats
            c.Rect(84, Floor, 64, 12, Art.Hex(0x8a3a5a));
            c.Rect(84, Floor + 11, 64, 1, Art.Hex(0xa8507a));
            c.Rect(86, Floor + 12, 60, 12, Art.Hex(0x7a3050));
            c.Rect(86, Floor + 23, 60, 1, Art.Hex(0x9a4a6e));
            c.Rect(115, Floor + 12, 2, 11, Art.Hex(0x6a2846));
            c.Rect(80, Floor, 6, 18, Art.Hex(0x8a3a5a));
            c.Rect(80, Floor + 17, 6, 1, Art.Hex(0xa8507a));
            c.Rect(146, Floor, 6, 18, Art.Hex(0x8a3a5a));
            c.Rect(146, Floor + 17, 6, 1, Art.Hex(0xa8507a));

            // the stations: a freed crewmate's is lit and lived in; the rest stay wrecked from the raid, dark, with no name
            foreach (Station s in Stations)
            {
                bool lit = s.Soldier != null && Has(freed, s.Soldier);
                if (lit) DrawStation(c, s);
                else Wrecked(c, s);
            }
        }

        static void Cable(PixelCanvas c, int x0, int y0, int x1, int y1)
        {
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f;
                c.Set((int)Mathf.Lerp(x0, x1, t), (int)(Mathf.Lerp(y0, y1, t) - 5f * Mathf.Sin(t * Mathf.PI)), Art.Hex(0x2a2050));
            }
        }

        static void DrawStation(PixelCanvas c, Station s)
        {
            switch (s.Crewmate)
            {
                case "kie": // a workbench with the Trollogram projector half taken apart, and a weasel hutch
                    c.Rect(8, Floor + 16, 48, 3, Art.Hex(0x8a6448));
                    c.Rect(10, Floor, 3, 16, Art.Hex(0x5a3e34));
                    c.Rect(51, Floor, 3, 16, Art.Hex(0x5a3e34));
                    c.Rect(16, Floor + 19, 12, 8, Art.Hex(0x454b5c));
                    c.Rect(16, Floor + 26, 12, 1, Art.Hex(0x707888));
                    c.Circle(30, Floor + 23, 2.5f, Art.Hex(0x7cf7d4));
                    for (int i = 0; i < 4; i++) c.Set(34 + i * 3, Floor + 19, Art.Hex(0xc0c4cc));
                    c.Rect(4, Floor, 20, 11, Art.Hex(0xa07a58));
                    c.Rect(6, Floor + 2, 16, 7, Art.Hex(0x2a1c1a));
                    for (int x = 7; x < 22; x += 3) c.Rect(x, Floor + 2, 1, 7, Art.Hex(0x7a5a44));
                    break;
                case "tones2": // a slushie machine and a beanbag beside the couch
                    c.Rect(158, Floor, 13, 30, Art.Hex(0xd0d4dc));
                    c.Rect(158, Floor + 29, 13, 1, Art.Hex(0xffffff));
                    c.Rect(160, Floor + 16, 9, 11, Art.Hex(0xff5a5f));
                    c.Rect(160, Floor + 10, 9, 5, Art.Hex(0x3ec0ff));
                    c.Rect(163, Floor + 5, 3, 3, Art.Hex(0x454b5c));
                    c.Circle(184, Floor + 4, 7f, Art.Hex(0xff8c42));
                    c.Rect(176, Floor, 17, 4, Art.Hex(0xc06428));
                    break;
            }
        }

        static void Wrecked(PixelCanvas c, Station s)
        {
            int y0 = s.Upstairs ? Loft : Floor, w = s.X1 - s.X0;
            // dark: no lights reach it
            for (int y = y0; y < y0 + 44; y++)
                for (int x = s.X0; x < s.X1; x++)
                {
                    Color32 p = c.Get(x, y);
                    c.Set(x, y, new Color32((byte)(p.r * 0.45f), (byte)(p.g * 0.42f), (byte)(p.b * 0.5f), 255));
                }
            // a tarp over whatever was there, and the mess
            int peak = s.X0 + w / 2 + (s.X0 % 7) - 3;
            for (int y = 0; y < 22; y++)
            {
                int half = (int)((22 - y) * w * 0.42f / 22f);
                c.Rect(peak - half, y0 + y, half * 2, 1, y % 5 == 0 ? Art.Hex(0x2c3a2c) : Art.Hex(0x324232));
            }
            c.Rect(s.X0 + 3, y0, 6, 2, Art.Hex(0x3a2a20));
            c.Rect(s.X1 - 10, y0 + 1, 4, 1, Art.Hex(0x4a4a4a));
            for (int k = 0; k < 10; k++) c.Set(s.X0 + (k * 13 + s.X0) % Mathf.Max(1, w), y0 + 26 + (k * 7) % 14, Art.Hex(0x2a5a66, 150));
        }
    }
}
