using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=craters</c> turns on
    /// blasts digging craters in the boss arena. Spikes are solo only (a co-op game ignores them). The partner arrow and
    /// own scores were spikes (<c>partner</c>, <c>dropin</c>) and are in the game now (coop-arrow-score, 19 Oct).
    /// </summary>
    public static class Spike
    {
        public const string CratersKey = "craters";

        /// <summary>Blasts dig craters in the boss arena (<see cref="CraterGround"/>).</summary>
        public static bool Craters;

        public static bool Any => Craters;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Craters = Array.IndexOf(keys, CratersKey) >= 0;
        }

        /// <summary>What the start banner says, or null when no spike is on.</summary>
        public static string Label
        {
            get
            {
                if (!Any) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Craters) parts.Add("CRATERS");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
