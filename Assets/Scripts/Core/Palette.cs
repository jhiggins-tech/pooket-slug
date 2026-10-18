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
        /// <summary>The pilot's lines and the stage's cool light.</summary>
        public static readonly Color Cyan = new Color(0.45f, 1f, 1f);

        // Trollograms read apart at a glance (Jack, 18 Oct): the goons' are the bad guys' reds, by kind, and kie's own
        // is a friendly green. The goons' Trollograms include kie's tabk's copies and his pilot.
        /// <summary>The goons' Trollograms: riflemen (and kie's tabk copies and pilot), knife runners, drones.</summary>
        public static readonly Color HostileRifle = new Color(1f, 0.3f, 0.32f), HostileRunner = new Color(1f, 0.3f, 0.75f), HostileDrone = new Color(1f, 0.55f, 0.2f);
        /// <summary>kie's own Trollogram: his decoys, and the popups and bursts they make.</summary>
        public static readonly Color Friendly = new Color(0.45f, 1f, 0.55f);

        /// <summary>The tint of a goon's Trollogram, by what kind of goon it is.</summary>
        public static Color Hostile(EnemyBrain brain) => brain == EnemyBrain.Runner ? HostileRunner : brain == EnemyBrain.Drone ? HostileDrone : HostileRifle;
        /// <summary>kie's colour: the roulette and the steals.</summary>
        public static readonly Color Pink = new Color(1f, 0.45f, 0.8f);
        /// <summary>Co-op's lines and the ability meter.</summary>
        public static readonly Color CoopBlue = new Color(0.6f, 0.9f, 1f);
        /// <summary>Player 1's and player 2's colours, by skin: their tags, hearts, arrows and the den's seats.</summary>
        public static readonly Color[] Players = { new Color(1f, 0.5f, 0.4f), new Color(0.45f, 0.72f, 1f) };
    }
}
