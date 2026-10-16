using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=hits</c> turns on
    /// the hit feel for that visit, <c>?spike=craters</c> blasts digging craters in the boss arena, <c>?spike=music</c> the music's first cut, <c>?spike=cutscenes</c> the opening scene
    /// before Mission one (<c>cutscenes-coop</c> adds a pretend partner who has to tap too). Spikes are solo only: a
    /// co-op game ignores them.
    /// </summary>
    public static class Spike
    {
        public const string HitsKey = "hits", CratersKey = "craters", MusicKey = "music";
        public const string CutscenesKey = "cutscenes", CutscenesCoopKey = "cutscenes-coop", NarratorKey = "narrator";

        /// <summary>Hits land with weight: sparks, flashes, and damage numbers floating off you.</summary>
        public static bool Hits;

        /// <summary>Blasts dig craters in the boss arena (<see cref="CraterGround"/>).</summary>
        public static bool Craters;

        /// <summary>The music's first cut (<see cref="MusicTracks"/>): Mission one's theme, and the boss theme from the boss intro.</summary>
        public static bool Music;

        /// <summary>The opening scene plays before Mission one (<see cref="Cutscenes.Opening"/>), each time, so it can be tried again.</summary>
        public static bool Scenes;

        /// <summary>The scene plays as if in co-op, with a pretend partner who taps a moment after you.</summary>
        public static bool ScenesCoop;

        /// <summary>The scene's narrator reads their lines aloud, in a candidate voice (<c>?voice=a</c> to <c>d</c> picks one).</summary>
        public static bool Narrator;

        /// <summary>The candidate voice the address asks for, or null for the first.</summary>
        public static string Voice;

        public static bool Any => Hits || Craters || Music || Scenes;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Hits = Array.IndexOf(keys, HitsKey) >= 0;
            Craters = Array.IndexOf(keys, CratersKey) >= 0;
            Music = Array.IndexOf(keys, MusicKey) >= 0;
            ScenesCoop = Array.IndexOf(keys, CutscenesCoopKey) >= 0;
            Narrator = Array.IndexOf(keys, NarratorKey) >= 0;
            Scenes = ScenesCoop || Narrator || Array.IndexOf(keys, CutscenesKey) >= 0;
            Voice = PageQuery.Get(url, "voice")?.Trim().ToLowerInvariant();
        }

        /// <summary>What the start banner says, or null when no spike is on.</summary>
        public static string Label
        {
            get
            {
                if (!Any) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Hits) parts.Add("HIT FEEL");
                if (Craters) parts.Add("CRATERS");
                if (Music) parts.Add("MUSIC");
                if (Narrator) parts.Add("NARRATOR");
                else if (Scenes) parts.Add("CUTSCENES");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
