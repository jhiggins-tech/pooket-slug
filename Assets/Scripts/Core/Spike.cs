using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=holo,hits</c> turns on
    /// the hologram goons and the hit feel for that visit. Spikes are solo
    /// only: a co-op game ignores them.
    /// </summary>
    public static class Spike
    {
        public const string HoloKey = "holo", HitsKey = "hits";

        /// <summary>The goons are glitchy holograms, and glitch out when they die.</summary>
        public static bool Holo;
        /// <summary>Hits land with weight: sparks, flashes, and damage numbers floating off you.</summary>
        public static bool Hits;

        public static bool Any => Holo || Hits;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Holo = Array.IndexOf(keys, HoloKey) >= 0;
            Hits = Array.IndexOf(keys, HitsKey) >= 0;
        }

        /// <summary>What the start banner says, or null when no spike is on.</summary>
        public static string Label
        {
            get
            {
                if (!Any) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Holo) parts.Add("HOLOGRAM GOONS");
                if (Hits) parts.Add("HIT FEEL");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
