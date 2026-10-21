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
        /// <summary>The villains' cold light: the pilot's lines, the stage's cool light, the riflemen's Trollograms.</summary>
        public static readonly Color Cyan = new Color(0.45f, 1f, 1f);

        // Trollograms read apart at a glance (Jack, 18 Oct; Design's palette): cold means hostile, warm means friendly.
        // The goons' are cool, by kind (the story's villains are cyan), and kie's own is a warm gold. The goons'
        // Trollograms include kie's tabk's copies and his pilot.
        /// <summary>The goons' Trollograms: riflemen (and kie's tabk copies and pilot), knife runners, drones.</summary>
        public static readonly Color HostileRifle = Cyan, HostileRunner = new Color(1f, 0.45f, 0.9f), HostileDrone = new Color(0.62f, 0.45f, 1f);
        /// <summary>kie's own Trollogram: his decoys, and the popups and bursts they make.</summary>
        public static readonly Color Friendly = new Color(1f, 0.82f, 0.42f);

        /// <summary>The tint of a goon's Trollogram, by what kind of goon it is.</summary>
        public static Color Hostile(EnemyBrain brain) => brain == EnemyBrain.Runner ? HostileRunner : brain == EnemyBrain.Drone ? HostileDrone : HostileRifle;
        /// <summary>kie's colour: the roulette and the steals.</summary>
        public static readonly Color Pink = new Color(1f, 0.45f, 0.8f);
        /// <summary>Co-op's lines and the ability meter.</summary>
        public static readonly Color CoopBlue = new Color(0.6f, 0.9f, 1f);
        /// <summary>Player 1's and player 2's colours, by seat: their tags, hearts, arrows and the den's seats (the soldiers wear their own).</summary>
        public static readonly Color[] Players = { new Color(1f, 0.5f, 0.4f), new Color(0.45f, 0.72f, 1f) };
    }
}
