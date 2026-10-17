using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike drop-in: a partner joins a mission that's already going (player.md, Co-op). Joining already works: the
    /// joiner's world starts at the shared scroll (<see cref="IWorldChannel.CamX"/>) and fills from the authority's
    /// snapshot, with lives of their own. Each player also keeps their own score, from their own kills, beside the
    /// shared one (<see cref="ISoldierChannel.SendScore"/>), and the HUD shows them with <c>?spike=dropin</c>.
    /// </summary>
    public static class DropIn
    {
        /// <summary>Scrolled this far past the start, a mission is under way.</summary>
        public const float UnderWayPast = 1f;

        /// <summary>We're joining (not running the shared world) a mission the others have already scrolled into.</summary>
        public static bool UnderWay(bool authority, float camX) => !authority && camX > Tuning.Coop.HalfWidth + UnderWayPast;

        /// <summary>A score as the HUD and the debrief write it: six digits, capped at 999,999.</summary>
        public static string Points(int points) => Mathf.Clamp(points, 0, 999999).ToString("000000");
    }
}
