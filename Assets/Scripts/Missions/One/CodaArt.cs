using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The art of the coda's gates (<c>docs/design/story.md</c>, The coda): the shutter of the motor pool's back gate
    /// that grinds open once kie's freed (<see cref="BackGate"/>), and the base's last gate (<see cref="ExitGate"/>, Design's sprite in
    /// <see cref="ExitGateArt"/>) with the rubble it leaves when smashed. The watchtower (<see cref="ToppleTower"/>) and kie asleep against the
    /// wall (<see cref="SleepingKie"/>) draw their own. Mission one is single player only, so none of them is shared
    /// over the network.
    /// </summary>
    static class CodaArt
    {
        static Sprite s_Gate, s_Rubble;

        static readonly Color32 Steel = Art.Hex(0x5a6478), SteelDark = Art.Hex(0x2e3444), SteelLight = Art.Hex(0x8a96aa);
        static readonly Color32 Hazard = Art.Hex(0xf0c030), HazardDark = Art.Hex(0x24242c);
        static readonly Color32 Snow = Art.Hex(0xf4f8ff), Ink = Art.OutlineColor;

        /// <summary>The back gate: a narrow blast shutter, hazard-striped, 1.5 tiles wide and 4 tall.</summary>
        public static Sprite BackGate => s_Gate != null ? s_Gate : s_Gate = Shutter(24, 64);

        /// <summary>The last gate: Design's blue shutter with its painted arrow (<see cref="ExitGateArt"/>), 4 tiles tall.</summary>
        public static Sprite ExitGate => ExitGateArt.Gate;

        /// <summary>What's left of a smashed gate: a heap of bent slats.</summary>
        public static Sprite Rubble
        {
            get
            {
                if (s_Rubble != null) return s_Rubble;
                var c = new PixelCanvas(34, 12);
                for (int i = 0; i < 6; i++)
                {
                    int x = 1 + i * 5, y = 1 + (i * 7) % 4;
                    c.Rect(x, y, 9, 3, i % 2 == 0 ? Steel : SteelDark);
                    c.Rect(x, y + 2, 9, 1, SteelLight);
                }
                c.Rect(4, 7, 6, 2, Hazard);
                c.Rect(20, 6, 6, 2, Hazard);
                return s_Rubble = Art.ToSprite(c, new Vector2(0.5f, 0f));
            }
        }

        static Sprite Shutter(int w, int h)
        {
            var c = new PixelCanvas(w, h);
            c.Rect(0, 0, w, h, Ink);
            c.Rect(1, 1, w - 2, h - 2, Steel);
            for (int y = 3; y < h - 6; y += 4)
            {
                c.Rect(2, y, w - 4, 1, SteelLight);
                c.Rect(2, y + 2, w - 4, 1, SteelDark);
            }
            for (int x = 1; x < w - 1; x++)
                for (int y = 1; y < 6; y++)
                    c.Set(x, y, ((x + y) / 3) % 2 == 0 ? Hazard : HazardDark); // a hazard-striped kick plate
            c.Rect(1, h - 6, w - 2, 5, SteelDark);
            c.Rect(1, h - 2, w - 2, 1, Snow);
            return Art.ToSprite(c, new Vector2(0.5f, 0f), withFlash: true);
        }
    }
}
