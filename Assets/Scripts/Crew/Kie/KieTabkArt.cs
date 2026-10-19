using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kie's tabk, THE PROJECTIONIST (<c>docs/design/tanks.md</c>, kie's tabk), 52 × 28: the tabk's hull and tracks in his
    /// blue with a green stripe, rebuilt round a Trollogram projector from the creamery's cold store. The projector is
    /// the turret, frosted, with a copper coil and pipe; its short fat lens, glowing gold, is the gun, drawn on its own
    /// so it turns (<see cref="Lens"/>); two film reels on top spin while a shot charges (<see cref="Spun"/>). The rows
    /// are Design's (<c>concepts/kie-tabk/sprite/</c>), the second frame's spokes turned 30°.
    /// </summary>
    public static class KieTabkArt
    {
        /// <summary>Where the lens turns, in the art's pixels from the tabk's feet (it faces left): the projector's front.</summary>
        public static readonly Vector2 LensPivot = new Vector2(-6f, 18.5f);
        /// <summary>The lens's length, in pixels: where its shots leave it.</summary>
        public const float LensLength = 17f;

        // Design's palette for it: the hull's blue and shade, the stripe's greens, the gold lens, frost, copper and rust
        static readonly Dictionary<char, uint> Paint = new Dictionary<char, uint>
        {
            { 'k', 0x1c1420 }, { 'm', 0xa8b0c0 }, { 'M', 0x586078 }, { 'D', 0x454b5c }, { 'd', 0x707888 }, { 'u', 0x3a3a48 },
            { 'c', 0xd07a3a }, { 'C', 0x8a4a20 }, { 'w', 0xdff4ff }, { 'y', 0xffd06a }, { 'Y', 0xffe860 }, { 'R', 0x9a5a34 },
            { 'B', 0x4ea8ff }, { 'b', 0x2a70c0 }, { 'G', 0x46d27a }, { 'g', 0x2a9050 }, { 'P', 0x3a5f9a },
        };

        static Sprite s_Hull, s_HullSpun, s_Lens, s_Parked;

        /// <summary>The hull without its lens, its reels still and turned.</summary>
        public static Sprite Hull => s_Hull ??= Art.FromRows(Cut(Whole, false), Feet, Paint);
        public static Sprite HullSpun => s_HullSpun ??= Art.FromRows(Cut(Spun, false), Feet, Paint);
        /// <summary>The lens, pointing left from its pivot at the projector's front.</summary>
        public static Sprite Lens => s_Lens ??= Art.FromRows(Cut(Whole, true), new Vector2(17f / 18f, 0.5f), Paint);
        /// <summary>It whole, lens and all, parked (and its Trollogram).</summary>
        public static Sprite Parked => s_Parked ??= Art.FromRows(Whole, Feet, Paint);

        static readonly Vector2 Feet = new Vector2(0.5f, 0f);

        // the lens: rows 7 to 13 of columns 4 to 19 (its hood and glass), less the hull's frost along row 13
        const int LensTop = 7, LensBottom = 13, LensLeft = 4, LensRight = 19;

        static bool InLens(int r, int c) => r >= LensTop && r <= LensBottom && c >= LensLeft && c <= LensRight && !(r == LensBottom && c >= 12);

        /// <summary>The rows less the lens, or the lens alone (its own little block of rows).</summary>
        static string[] Cut(string[] rows, bool lens)
        {
            if (!lens)
            {
                var hull = (string[])rows.Clone();
                for (int r = LensTop; r <= LensBottom; r++)
                {
                    char[] row = hull[r].ToCharArray();
                    for (int c = LensLeft; c <= LensRight; c++)
                        if (InLens(r, c)) row[c] = '.';
                    hull[r] = new string(row);
                }
                return hull;
            }
            var block = new string[LensBottom - LensTop + 1];
            for (int r = LensTop; r <= LensBottom; r++)
            {
                char[] row = new char[LensRight - LensLeft + 1];
                for (int c = LensLeft; c <= LensRight; c++) row[c - LensLeft] = InLens(r, c) ? rows[r][c] : '.';
                block[r - LensTop] = new string(row);
            }
            return block;
        }

        static readonly string[] Whole =
        {
            "........................kkk.......kkk...............",
            "......................kkGmGkk...kkGmGkk.............",
            "......................kmGDGmk...kmGDGmk.............",
            ".....................kmDMDMDmk.kmDMDMDmk............",
            ".....................kGGMmMGGk.kGGMmMGGk............",
            ".....................kmDMDMDmk.kmDMDMDmk............",
            "......................kmGDGmk...kmGDGmk.............",
            "........kkkk........kkkkGmGkkkkkkkGmGkkkkk..........",
            ".....kyyDdddkkkkkkkkkwwwkkkwwwwwwwkkkwwwwkcc........",
            ".....yYyDDDDMmmmmmmmkwBwBBBwBBBwBBBcwkkkBkc.........",
            "....YyYyDDDDMMMMMMMMkBBcBBBcBBBcBBBcBBBcBkc.........",
            ".....yYyDDDDMDMDMDMMkbbbCbbbCbbbCbbbCbbbCkc.........",
            ".....kyyDDDDkkkkkkkkkbbbCbbbCbbbCbbbCbbbCkc.........",
            "........kkkk....BwBwkkkkkkkkkkkkkkkkkkkkkkcCC.......",
            "..............BBBBBBBBBBBRBBBBBBBBBBPPPmBBBBB.......",
            ".............BbRbbmPPPPbbbbbbbbbbRbbPPPPbbbbBB......",
            "............BGGGGGPPPPPGGGGGGGGGGGGGGGGGGGGGRGB.....",
            "...........BggggYYPPPPmgggggggggggggggggggYYgggg....",
            "...........BbbbbbbbbbbbbbbbbRbbbbbbbbbbbbbbbbbbB....",
            "..........uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu...",
            ".........uDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDu..",
            "........uDDdddDDDdddDDDdddDDDdddDDDdddDDDdddDDDDDDu.",
            "........uDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDDDDDu.",
            "........uDdmMmdDdmMmdDdmMmdDdmMmdDdmMmdDdmMmdDDDDDu.",
            "........uDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDDDDDu.",
            ".........uDdddDDDdddDDDdddDDDdddDDDdddDDDdddDDDDDu..",
            "..........uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu...",
            "....................................................",
        };

        static readonly string[] Spun =
        {
            "........................kkk.......kkk...............",
            "......................kkmGmkk...kkmGmkk.............",
            "......................kmDGDmk...kmDGDmk.............",
            ".....................kmGDMMGmk.kmGDMMGmk............",
            ".....................kmDMmMDmk.kmDMmMDmk............",
            ".....................kmGDMDGmk.kmGDMDGmk............",
            "......................kmDGDmk...kmDGDmk.............",
            "........kkkk........kkkkmGmkkkkkkkmGmkkkkk..........",
            ".....kyyDdddkkkkkkkkkwwwkkkwwwwwwwkkkwwwwkcc........",
            ".....yYyDDDDMmmmmmmmkwBwBBBwBBBwBBBcwkkkBkc.........",
            "....YyYyDDDDMMMMMMMMkBBcBBBcBBBcBBBcBBBcBkc.........",
            ".....yYyDDDDMDMDMDMMkbbbCbbbCbbbCbbbCbbbCkc.........",
            ".....kyyDDDDkkkkkkkkkbbbCbbbCbbbCbbbCbbbCkc.........",
            "........kkkk....BwBwkkkkkkkkkkkkkkkkkkkkkkcCC.......",
            "..............BBBBBBBBBBBRBBBBBBBBBBPPPmBBBBB.......",
            ".............BbRbbmPPPPbbbbbbbbbbRbbPPPPbbbbBB......",
            "............BGGGGGPPPPPGGGGGGGGGGGGGGGGGGGGGRGB.....",
            "...........BggggYYPPPPmgggggggggggggggggggYYgggg....",
            "...........BbbbbbbbbbbbbbbbbRbbbbbbbbbbbbbbbbbbB....",
            "..........uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu...",
            ".........uDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDu..",
            "........uDDdddDDDdddDDDdddDDDdddDDDdddDDDdddDDDDDDu.",
            "........uDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDDDDDu.",
            "........uDdmMmdDdmMmdDdmMmdDdmMmdDdmMmdDdmMmdDDDDDu.",
            "........uDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDdmmmdDDDDDu.",
            ".........uDdddDDDdddDDDdddDDDdddDDDdddDDDdddDDDDDu..",
            "..........uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu...",
            "....................................................",
        };
    }
}
