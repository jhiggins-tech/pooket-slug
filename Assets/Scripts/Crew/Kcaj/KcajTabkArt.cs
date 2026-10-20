using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's tabk, SOFT SERVE (<c>docs/design/tanks.md</c>, kcaj's tabk), 52 × 28: an ice cream van gone to war, the
    /// tabk's hull and tracks in his purple with a gold stripe, a soft-serve machine for a turret (a serving hatch under a
    /// striped awning, a menu sign, a loudspeaker) and a twist of vanilla and strawberry on top. The gun is its chrome
    /// nozzle, drawn on its own so it turns (<see cref="Nozzle"/>); while a shot charges the swirl wobbles
    /// (<see cref="HullCharging"/>). The rows are Design's, drawn by hand (<c>concepts/kcaj-tabk/sprite/out/</c>).
    /// </summary>
    public static class KcajTabkArt
    {
        /// <summary>Where the nozzle turns, in the art's pixels from the tabk's feet (it faces left): the machine's front.</summary>
        public static readonly Vector2 NozzlePivot = new Vector2(-5f, 18.5f);
        /// <summary>The nozzle's length, in pixels: where its shots leave it.</summary>
        public const float NozzleLength = 15f;

        // Design's palette for it: kcaj's purple and gold, the machine's pale grey, chrome, cone, strawberry and cream
        static readonly Dictionary<char, uint> Paint = new Dictionary<char, uint>
        {
            { 'k', 0x1c1420 }, { 'm', 0xa8b0c0 }, { 'M', 0x586078 }, { 'd', 0x707888 }, { 'D', 0x454b5c }, { 'u', 0x3a3a48 },
            { 'W', 0xffffff }, { 'V', 0xa060e0 }, { 'v', 0x6a3aa0 }, { 'F', 0xffc53d }, { 'f', 0xb8841c }, { 'L', 0xe2e8f0 },
            { 'l', 0xa8b4c8 }, { 'H', 0xff3df2 }, { 'c', 0xe0a860 }, { 'C', 0x9a6a30 }, { 'p', 0xffb0d0 }, { 'P', 0xe0709a },
            { 'w', 0xfff6e8 }, { 'b', 0x80d8ff }, { 'B', 0xd8f4ff },
        };

        static Sprite s_Hull, s_HullCharging, s_Nozzle, s_Parked;

        /// <summary>The hull without its nozzle, at rest and while a shot charges.</summary>
        public static Sprite Hull => s_Hull ??= Art.FromRows(Cut(Rest, false), Feet, Paint);
        public static Sprite HullCharging => s_HullCharging ??= Art.FromRows(Cut(Charging, false), Feet, Paint);
        /// <summary>The nozzle, pointing left from its pivot at the machine's front.</summary>
        public static Sprite Nozzle => s_Nozzle ??= Art.FromRows(Cut(Rest, true), new Vector2(16f / 17f, 0.5f), Paint);
        /// <summary>It whole, nozzle and all, parked.</summary>
        public static Sprite Parked => s_Parked ??= Art.FromRows(Rest, Feet, Paint);

        static readonly Vector2 Feet = new Vector2(0.5f, 0f);

        // the nozzle: rows 8 to 12 of columns 6 to 20 (its spout, its fluted tip and the cream at its mouth; the second
        // frame's drip hangs below it, so the hull drops that too)
        const int Top = 8, Bottom = 12, Left = 6, Right = 20;

        /// <summary><paramref name="rows"/> less the nozzle, or (<paramref name="nozzle"/>) the nozzle alone.</summary>
        static string[] Cut(string[] rows, bool nozzle)
        {
            if (nozzle)
            {
                var gun = new string[Bottom - Top + 1];
                for (int r = Top; r <= Bottom; r++) gun[r - Top] = rows[r].Substring(Left, Right - Left + 1);
                return gun;
            }
            var hull = (string[])rows.Clone();
            for (int r = Top; r <= Bottom; r++) hull[r] = new string('.', Right + 1) + rows[r].Substring(Right + 1);
            return hull;
        }

        static readonly string[] Rest =
        {
            "..............................kk....................",
            ".............................kwwk..........pPp......",
            "............................kwPwwk.........ppp......",
            "...........................kppwwwk.........cCc......",
            "..........................kwwwppppk..kkkk...c.......",
            ".........................kppppwwwwwk.kFFfk..k.......",
            "........................kwwwwwwppppPk.kfk...k.......",
            ".....................kFVFVFVFVFVFVFVFVFVk...k.......",
            ".........kkkk........kLkkkkkkkkLLkkkkkLLk...k.......",
            ".......wkmMmMkkkkkkkkkLkBbbbbbkLLkHwHllLk...k.......",
            "......w.kMmMmmmmWmmmmkLkkkkkkkkLLkkkkkLLk...k.......",
            ".........kkkkMMMMMMMMkllllllllllllllllllk...k.......",
            ".....................kkkkkkkkkkkkkkkkkkkk...k.......",
            "................LVVVLVVVLVVVLpPpLVVVLVVVLV..........",
            "..............VVVVVVVVVVVVVVVpppVVVVVVVVVVVVV.......",
            ".............VvvvvvvvvvvvvvvvcCcvvvvvvvvvvvvVV......",
            "............FFFFFFFFFFFFFFFFFFCFFFFFFFFFFFFFFFF.....",
            "...........VfvvvFFfvvvvvfvvvvvfvvvvvfvvvvvFFvvvV....",
            "...........VvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvV....",
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

        static readonly string[] Charging =
        {
            "............................kk......................",
            "............................kwwk...........pPp......",
            "...........................kwwPwk..........ppp......",
            "...........................kwwwppk.........cCc......",
            "..........................kppppwwwk..kkkk...c.......",
            ".........................kwwwwwppppk.kFFfk..k.......",
            "........................kppppwwwwwwPk.kfk...k.......",
            ".....................kFVFVFVFVFVFVFVFVFVk...k.......",
            ".........kkkk........kLkkkkkkkkLLkkkkkLLk...k.......",
            ".......wkmMmMkkkkkkkkkLkBbbbbbkLLkHwHllLk...k.......",
            "......w.kMmMmmmmWmmmmkLkkkkkkkkLLkkkkkLLk...k.......",
            ".......w.kkkkMMMMMMMMkllllllllllllllllllk...k.......",
            ".......p.............kkkkkkkkkkkkkkkkkkkk...k.......",
            "................LVVVLVVVLVVVLpPpLVVVLVVVLV..........",
            "..............VVVVVVVVVVVVVVVpppVVVVVVVVVVVVV.......",
            ".............VvvvvvvvvvvvvvvvcCcvvvvvvvvvvvvVV......",
            "............FFFFFFFFFFFFFFFFFFCFFFFFFFFFFFFFFFF.....",
            "...........VfvvvFFfvvvvvfvvvvvfvvvvvfvvvvvFFvvvV....",
            "...........VvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvV....",
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
