using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=craters</c> turns on
    /// blasts digging craters in the boss arena, <c>?spike=partner</c> the partner arrow
    /// and warnings, <c>?spike=dropin</c> each player's own score in co-op.
    /// Spikes are solo only (a co-op game ignores them), except the partner and drop-in ones, which are about co-op.
    /// </summary>
    public static class Spike
    {
        public const string CratersKey = "craters";
        public const string PartnerKey = "partner", DropInKey = "dropin";

        /// <summary>Blasts dig craters in the boss arena (<see cref="CraterGround"/>).</summary>
        public static bool Craters;

        /// <summary>
        /// Spike coop-camera (<see cref="PartnerBeacon"/>): an arrow to a partner who's off screen, warnings when their health
        /// is low or they've switched away, and an away partner no longer holds the camera back. It works in co-op, and
        /// a solo game gets a pretend partner (<see cref="PartnerGhost"/>) to try it with.
        /// </summary>
        public static bool Partner;

        /// <summary>
        /// Spike drop-in (<see cref="DropIn"/>): in co-op, the HUD shows each player's own score (from their kills) beside
        /// the shared one. Joining a mission under way already works for everyone; this is about the scores.
        /// </summary>
        public static bool DropIn;

        public static bool Any => Craters || Partner || DropIn;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Craters = Array.IndexOf(keys, CratersKey) >= 0;
            Partner = Array.IndexOf(keys, PartnerKey) >= 0;
            DropIn = Array.IndexOf(keys, DropInKey) >= 0;
        }

        /// <summary>What the start banner says, or null when no spike is on.</summary>
        public static string Label
        {
            get
            {
                if (!Any) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Craters) parts.Add("CRATERS");
                if (Partner) parts.Add("PARTNER ARROW");
                if (DropIn) parts.Add("OWN SCORES");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
