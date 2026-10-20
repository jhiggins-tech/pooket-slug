using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// tones2's snacks in the cutscenes, hand-drawn (snacks-hand-drawn, the owner's pick 20 Oct: set A,
    /// <c>concepts/snacks/</c>): the crisp packet he carries (crimped ends, a golden crisp on the front) and the crisps
    /// that burst out of it when he drops it. The packet is <see cref="CutsceneCast"/>'s <c>bag</c>, the crisps
    /// <see cref="CutsceneEffects"/>' chips.
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

        static Sprite s_Packet, s_Crisp;

        /// <summary>The crisp packet, 9 x 11.</summary>
        public static Sprite Packet => s_Packet != null ? s_Packet : s_Packet = Make(PacketRows);

        /// <summary>A crisp, 4 x 2 (flipped for some, so they don't all look alike).</summary>
        public static Sprite Crisp => s_Crisp != null ? s_Crisp : s_Crisp = Make(CrispRows);

        static Sprite Make(string[] rows)
        {
            var c = new PixelCanvas(rows[0].Length, rows.Length);
            c.Ascii(rows, Pal, 0, 0);
            return Art.ToSprite(c, new Vector2(0.5f, 0f));
        }
    }
}
