using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// tones2's snacks in the cutscenes, hand-drawn (snacks-hand-drawn, the owner's pick 20 Oct: set A,
    /// <c>concepts/snacks/</c>): the crisp packet he carries (crimped ends, a golden crisp on the front) and the crisps
    /// that burst out of it when he drops it. The packet is <see cref="CutsceneCast"/>'s <c>bag</c>, the crisps
    /// <see cref="CutsceneEffects"/>' chips. And his blue raspberry slushie (the owner's pick 20 Oct, B,
    /// <c>concepts/slurpee/</c>): blue slush heaped over a white paper cup with his orange band and a red straw, the cup
    /// he crushes, and the den's tiny one (<see cref="DenCrew"/>).
    /// </summary>
    static class CutsceneSnacks
    {
        static readonly Dictionary<char, Color32> Pal = new Dictionary<char, Color32>
        {
            { 'k', Art.Hex(0x1c1420) }, { 'W', Art.Hex(0xffffff) }, { 'r', Art.Hex(0xd83838) }, { 'R', Art.Hex(0x902820) },
            { 'y', Art.Hex(0xf0c840) }, { 'Y', Art.Hex(0xffe08a) }, { 'o', Art.Hex(0xf08830) }, { 'x', Art.Hex(0xffe860) },
        };

        static readonly string[] PacketRows =
        {
            ".kkkkkkk.",
            "kRrRrRrRk",
            "krrrrrrrk",
            "krWyyyrrk",
            "kryYYYyrk",
            "kryYxYyrk",
            "krryyyrrk",
            "krrrrrrrk",
            "krrooooRk",
            "kRrRrRrRk",
            ".kkkkkkk.",
        };

        static readonly string[] CrispRows = { "oyy.", ".yYo" };

        static readonly Dictionary<char, Color32> SlushPal = new Dictionary<char, Color32>
        {
            { 'k', Art.Hex(0x1c1420) }, { 'R', Art.Hex(0xe83838) }, { 'I', Art.Hex(0xc8ecff) }, { 'B', Art.Hex(0x40a8ff) },
            { 'b', Art.Hex(0x1a68c0) }, { 'W', Art.Hex(0xffffff) }, { 'w', Art.Hex(0xc8ccd8) }, { 'o', Art.Hex(0xf08830) },
        };

        static readonly string[] SlushieRows =
        {
            "....R.",
            "....R.",
            ".kIBRk",
            "kIBBBk",
            "kBbBbk",
            "kkkkkk",
            "kWWWWk",
            "kooook",
            "kWWWwk",
            ".kWWwk",
            ".kWwwk",
            ".kkkk.",
        };

        static readonly string[] CrushedRows =
        {
            "......",
            "......",
            "......",
            "......",
            "......",
            "....RR",
            "...R..",
            ".kkRk.",
            "kWoWwk",
            "kwWoWk",
            ".kWwWk",
            ".kkkk.",
        };

        static readonly string[] TinyRows = { ".R.", "BIB", "WWw", "ooo", "Www" };

        static Sprite s_Packet, s_Crisp, s_Slushie, s_Crushed, s_Tiny;

        /// <summary>The crisp packet, 9 x 11.</summary>
        public static Sprite Packet => s_Packet != null ? s_Packet : s_Packet = Make(PacketRows);

        /// <summary>A crisp, 4 x 2 (flipped for some, so they don't all look alike).</summary>
        public static Sprite Crisp => s_Crisp != null ? s_Crisp : s_Crisp = Make(CrispRows);

        /// <summary>His slushie in his hand, 6 x 12: the opening's <c>cup</c> and freeing kcaj's <c>slushie</c>.</summary>
        public static Sprite Slushie => s_Slushie != null ? s_Slushie : s_Slushie = Make(SlushieRows, SlushPal);

        /// <summary>The cup drained and crushed, the same size (it stands where the whole one did).</summary>
        public static Sprite Crushed => s_Crushed != null ? s_Crushed : s_Crushed = Make(CrushedRows, SlushPal);

        /// <summary>The den's tiny one, 3 x 5, at his machine and in the slushie bit.</summary>
        public static Sprite Tiny => s_Tiny != null ? s_Tiny : s_Tiny = Make(TinyRows, SlushPal);

        static Sprite Make(string[] rows, Dictionary<char, Color32> pal = null)
        {
            var c = new PixelCanvas(rows[0].Length, rows.Length);
            c.Ascii(rows, pal ?? Pal, 0, 0);
            return Art.ToSprite(c, new Vector2(0.5f, 0f));
        }
    }
}
