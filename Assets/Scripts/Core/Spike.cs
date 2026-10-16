using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=craters</c> turns on
    /// blasts digging craters in the boss arena, <c>?spike=music</c> the music's first cut, <c>?spike=pilot-glitch</c> hologram kie's big glitch-out and the real kie freed, <c>?spike=callin</c> a tabk to call in as if tones2's were won. Spikes are solo only: a
    /// co-op game ignores them.
    /// </summary>
    public static class Spike
    {
        public const string CratersKey = "craters", MusicKey = "music";
        public const string PilotGlitchKey = "pilot-glitch", CallInKey = "callin";

        /// <summary>Blasts dig craters in the boss arena (<see cref="CraterGround"/>).</summary>
        public static bool Craters;

        /// <summary>The music's first cut (<see cref="MusicTracks"/>): Mission one's theme, and the boss theme from the boss intro.</summary>
        public static bool Music;

        /// <summary>Hologram kie's big glitch-out when he's beaten, and the real kie freed (<see cref="PilotFreeingSpec"/>).</summary>
        public static bool PilotGlitch;

        /// <summary>
        /// The tabk call-in as if tones2's tabk were won, the way Mission one's coda brings it in: one to call in, the
        /// prompt showing how until you call (<see cref="CallInCoach"/>, every visit), and the cargo drone lowering it in.
        /// </summary>
        public static bool CallIn;

        public static bool Any => Craters || Music || PilotGlitch || CallIn;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Craters = Array.IndexOf(keys, CratersKey) >= 0;
            Music = Array.IndexOf(keys, MusicKey) >= 0;
            PilotGlitch = Array.IndexOf(keys, PilotGlitchKey) >= 0;
            CallIn = Array.IndexOf(keys, CallInKey) >= 0;
        }

        /// <summary>What the start banner says, or null when no spike is on.</summary>
        public static string Label
        {
            get
            {
                if (!Any) return null;
                var parts = new System.Collections.Generic.List<string>();
                if (Craters) parts.Add("CRATERS");
                if (Music) parts.Add("MUSIC");
                if (PilotGlitch) parts.Add("PILOT GLITCH-OUT");
                if (CallIn) parts.Add("TABK CALL-IN");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
