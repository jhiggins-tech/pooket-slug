using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Mission one's last gate (<see cref="ExitGate"/>), 34 × 64: a blue blast shutter with a red lamp on top, a
    /// hazard-striped kick plate and an arrow painted on its foot pointing the way out. Design's sprite, picked by Jack
    /// on 20 Oct (<c>concepts/m1-gate/picked-gate.png</c>, sheet 2, gate 4), as rows and its 24 colours, drawn as it
    /// is: it has its own outline.
    /// </summary>
    static class ExitGateArt
    {
        static Sprite s_Gate;

        /// <summary>The gate, its feet at the bottom centre, with a white copy for the hit flash.</summary>
        public static Sprite Gate
        {
            get
            {
                if (s_Gate != null) return s_Gate;
                var pal = new Dictionary<char, Color32>();
                foreach (KeyValuePair<char, uint> p in Paint) pal[p.Key] = Art.Hex(p.Value);
                var c = new PixelCanvas(Rows[0].Length, Rows.Length);
                c.Ascii(Rows, pal, 0, 0);
                return s_Gate = Art.ToSprite(c, new Vector2(0.5f, 0f), withFlash: true);
            }
        }

        static readonly Dictionary<char, uint> Paint = new Dictionary<char, uint>
        {
            { 'a', 0x09255c }, { 'b', 0x2b5296 }, { 'c', 0x050c2b }, { 'd', 0x486798 }, { 'e', 0x6c98e1 }, { 'f', 0x8cb0ed },
            { 'g', 0xd79d09 }, { 'h', 0xe1e3f1 }, { 'i', 0xa2cbf4 }, { 'j', 0x294878 }, { 'm', 0x4d77c9 }, { 'n', 0x153b84 },
            { 'o', 0x5b0672 }, { 'p', 0x6c8bb6 }, { 'q', 0xa10609 }, { 'r', 0x5c542c }, { 's', 0x5d0408 }, { 't', 0xfd1afd },
            { 'u', 0xe41910 }, { 'v', 0xa5944b }, { 'w', 0x99740c }, { 'x', 0xb5932d }, { 'y', 0xdaaa4d }, { 'z', 0x720985 },
        };

        static readonly string[] Rows =
        {
            "...............qquq...............",
            "...............quqs...............",
            "...............quqs...............",
            "...............sqsc...............",
            "..............cccccc..............",
            ".......hhh....abaaac.....hh.......",
            "......hhhhhhhhabaaaafhhhhhhh......",
            "......teeiiiaaaccccaabmeiieef.....",
            ".....ebbbbbbabbbbbbbbabbbbbbb.....",
            ".....eaabbbbaebbbbbbaajbbbaab.....",
            ".....ebbbbababbbbbbbbbajabbbb.....",
            ".....bbbbbbbabbbbbbbaaabbbbba.....",
            "......aaccccccccccccccccccaao.....",
            "......aacccaaaccccccccccccac......",
            "......nncaaaaaaaaaaaaaaaacbno.....",
            "......bbccccccccccccccccccmbo.....",
            "......bbcaaanaaaaaaaaaaaacbbo.....",
            "......bnccccccccccccccccccen......",
            "......bmcaaaaaaaaaaaaaaaacfbo.....",
            "......bmceefieeeeeeeeeeedjfbo.....",
            "......becaaaaaaaaaaaaaaaacibo.....",
            "......edcaaaaaaaaaaaaaaaacfdz.....",
            "......dmcaaaaaaaaaaaaaaaacfbo.....",
            "......bmcabbbbbbnanbbnbbbcfdo.....",
            "......bbcacaacaacaaccccccceao.....",
            "......bdcabbbaaaaaaaaabnacmbo.....",
            "......dmceeeffeeeeeeeeeefbfbo.....",
            "......bmcaaaaaaaaaaaaaaaacebo.....",
            "......bmcaaaaaaaaaaaaaaaacfbo.....",
            "......bmceeefefffffffepedaebo.....",
            "......becbbbbaannbnbbabbacfdo.....",
            "....h.mmcccaaaaaaaaaaaaaacfbohh...",
            "..hhhibmcaaaaaaaaaaaaaaaacfbahhh..",
            ".iiiiebmcfiffiieefffefeiibfbnfiie.",
            ".hdpdaabccaaaaaaaaaaaaaaacfaajfde.",
            ".ddddabmcaaaaaaaaaaaaaaaacebajdbj.",
            ".bdddabdcbddebddbbddbbbbbafbaabdj.",
            ".pdddabbcbbbbbbbabbbbbbaacebadddd.",
            ".pdddabmcaaaaaaaaaaaaaaaacebabddd.",
            ".pddbaabcabbbabbaaaaaabbaceaaddbb.",
            ".ddddabbcebemeedbbbeeetdbbebaddda.",
            ".ddddaabcagggcaaagggraaaggejadddd.",
            ".ajddabbcrggvaaagggwaaagggebaddda.",
            ".pdddabbcgggaaaggggaaagggwfbcdddd.",
            ".ddddabdcggaaaxygwaaaxggxcfbcdddd.",
            ".ddjaabbcgaaavgggaaaygggacfbcaadd.",
            ".ddaaabecaaacgggaaatgggaacebaaadd.",
            ".abbbaabcaacyggaaaagggaaavebajjdc.",
            ".pbddabdcaagggaaaagggaaacgmbabbdj.",
            ".dbdbabbcnrccaaaartraaaaacmbabddd.",
            ".dddbabbjiififfififefiififmbcbddj.",
            ".dddaabbmabbbbbbbbbbabbbabbbaaddd.",
            ".dabbabbbbbbbbbbbbbbbbbbbbabajbbb.",
            ".ajbaaabbabbbbbbbbbbbbbbbbnbaajjc.",
            ".dbababbdbbbbbbbbbbbibbbbbbacabaa.",
            ".dbababbbbbfhhiifffhhhdbbbbbabbdp.",
            ".dbdbanbbabfhfhihhhhhhhbbbbacdbbb.",
            ".aaajabbbbbfihhhihhhhhhbbbbbcaaab.",
            "ceiiibbbbbbbbbbbbbbbhibbbbbbaefiic",
            "cddddbbbbabbbbbbbbbbfbbbbbbbadjddc",
            "cdjddbbbbbbbbbbbbbbbbbbbbbbbcddjdc",
            "cddddbbbbbbbbbbbbbbbbbbabbbbcdjddc",
            "ieeddbbbbabdbbbabbabbabbbbnacddbdp",
            "ddjjjacacaccccacaaaaaaccccccaaaaaa",
        };
    }
}
