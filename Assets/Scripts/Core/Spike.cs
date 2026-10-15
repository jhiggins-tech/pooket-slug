using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=hits</c> turns on
    /// the hit feel for that visit, <c>?spike=kie</c> kie's tank boss (add <c>&amp;test=boss</c> to start at it). Spikes are solo
    /// only: a co-op game ignores them.
    /// </summary>
    public static class Spike
    {
        public const string HitsKey = "hits", KieKey = "kie";

        /// <summary>Hits land with weight: sparks, flashes, and damage numbers floating off you.</summary>
        public static bool Hits;

        /// <summary>The boss tank fights as kie's: weasels, hologram copies and a roulette that steals (<see cref="KieTankSpec"/>).</summary>
        public static bool Kie;

        public static bool Any => Hits || Kie;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Hits = Array.IndexOf(keys, HitsKey) >= 0;
            Kie = Array.IndexOf(keys, KieKey) >= 0;
        }

        /// <summary>What the start banner says, or null when no spike is on.</summary>
        public static string Label
        {
            get
            {
                if (!Any) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Hits) parts.Add("HIT FEEL");
                if (Kie) parts.Add("KIE'S TANK");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
