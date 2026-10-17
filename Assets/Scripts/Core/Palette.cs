using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The colours the game shares across screens and the world: popups, meters, the cutscenes' stage and the HUD.
    /// A colour that belongs to one thing (a goon, a weapon) stays with it.
    /// </summary>
    public static class Palette
    {
        /// <summary>The game's gold: big popups, the super, charge meters, the title's buttons.</summary>
        public static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
        /// <summary>Trollograms, the pilot's lines and the stage's cool light.</summary>
        public static readonly Color Cyan = new Color(0.45f, 1f, 1f);
        /// <summary>kie's colour: the roulette and the steals.</summary>
        public static readonly Color Pink = new Color(1f, 0.45f, 0.8f);
        /// <summary>Co-op's lines and the ability meter.</summary>
        public static readonly Color CoopBlue = new Color(0.6f, 0.9f, 1f);
        /// <summary>Player 1's and player 2's colours, by skin: their tags, hearts, arrows and the den's seats.</summary>
        public static readonly Color[] Players = { new Color(1f, 0.5f, 0.4f), new Color(0.45f, 0.72f, 1f) };
    }
}
