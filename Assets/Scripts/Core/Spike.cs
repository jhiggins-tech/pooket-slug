using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=hits</c> turns on
    /// the hit feel for that visit, <c>?spike=kie-weasels</c>, <c>kie-trollogram</c> or <c>kie-steal</c> playing as kie with his kit as built so far, <c>?spike=craters</c> blasts digging craters in the boss arena, <c>?spike=music</c> the music's first cut, <c>?spike=cutscenes</c> the opening scene
    /// before Mission one (<c>cutscenes-coop</c> adds a pretend partner who has to tap too). Spikes are solo only: a
    /// co-op game ignores them.
    /// </summary>
    public static class Spike
    {
        public const string HitsKey = "hits", WeaselsKey = "kie-weasels", TrollogramKey = "kie-trollogram", StealKey = "kie-steal", CratersKey = "craters", MusicKey = "music";
        public const string CutscenesKey = "cutscenes", CutscenesCoopKey = "cutscenes-coop";

        /// <summary>Hits land with weight: sparks, flashes, and damage numbers floating off you.</summary>
        public static bool Hits;

        /// <summary>You play kie, with Weasel Pop (<see cref="WeaselPopSpec"/>).</summary>
        public static bool Weasels;

        /// <summary>You play kie, with Trollogram (<see cref="TrollogramSpec"/>).</summary>
        public static bool Trollogram;

        /// <summary>You play kie, with Steal (<see cref="StealSpec"/>), and start with the super charged to try it.</summary>
        public static bool Steal;

        /// <summary>Blasts dig craters in the boss arena (<see cref="CraterGround"/>).</summary>
        public static bool Craters;

        /// <summary>The music's first cut (<see cref="MusicTracks"/>): Mission one's theme, and the boss theme from the boss intro.</summary>
        public static bool Music;

        /// <summary>The opening scene plays before Mission one (<see cref="Cutscenes.Opening"/>), each time, so it can be tried again.</summary>
        public static bool Scenes;

        /// <summary>The scene plays as if in co-op, with a pretend partner who taps a moment after you.</summary>
        public static bool ScenesCoop;

        public static bool Any => Hits || Weasels || Trollogram || Steal || Craters || Music || Scenes;

        /// <summary>The soldier a spike has you play, or null for the usual one: kie, with his kit as built so far.</summary>
        public static SoldierDef Soldier => Weasels || Trollogram || Steal ? Content.Kie : null;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Hits = Array.IndexOf(keys, HitsKey) >= 0;
            Weasels = Array.IndexOf(keys, WeaselsKey) >= 0;
            Trollogram = Array.IndexOf(keys, TrollogramKey) >= 0;
            Steal = Array.IndexOf(keys, StealKey) >= 0 || Array.IndexOf(keys, "steal") >= 0;
            Craters = Array.IndexOf(keys, CratersKey) >= 0;
            Music = Array.IndexOf(keys, MusicKey) >= 0;
            ScenesCoop = Array.IndexOf(keys, CutscenesCoopKey) >= 0;
            Scenes = ScenesCoop || Array.IndexOf(keys, CutscenesKey) >= 0;
        }

        /// <summary>What the start banner says, or null when no spike is on.</summary>
        public static string Label
        {
            get
            {
                if (!Any) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Hits) parts.Add("HIT FEEL");
                if (Weasels) parts.Add("WEASEL POP");
                if (Trollogram) parts.Add("TROLLOGRAM");
                if (Steal) parts.Add("STEAL");
                if (Craters) parts.Add("CRATERS");
                if (Music) parts.Add("MUSIC");
                if (Scenes) parts.Add("CUTSCENES");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
