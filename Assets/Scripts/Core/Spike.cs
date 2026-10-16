using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=craters</c> turns on
    /// blasts digging craters in the boss arena, <c>?spike=music</c> the music's first cut, <c>?spike=cutscenes</c> the opening scene
    /// before Mission one, <c>?spike=pilot-glitch</c> hologram kie's big glitch-out and the real kie freed, <c>?spike=callin</c> a tabk to call in as if kie's were won (<c>cutscenes-coop</c> adds a pretend partner who has to tap too). Spikes are solo only: a
    /// co-op game ignores them.
    /// </summary>
    public static class Spike
    {
        public const string CratersKey = "craters", MusicKey = "music";
        public const string CutscenesKey = "cutscenes", CutscenesCoopKey = "cutscenes-coop", NarratorKey = "narrator";
        public const string PilotGlitchKey = "pilot-glitch", CallInKey = "callin";

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

        /// <summary>Hologram kie's big glitch-out when he's beaten, and the real kie freed (<see cref="PilotFreeingSpec"/>).</summary>
        public static bool PilotGlitch;

        /// <summary>
        /// The tabk call-in as if kie's tabk were won: one to call in, the hint showing how at the first calm moment
        /// (<see cref="CallInCoach"/>, every visit), and a marker where it'll land.
        /// </summary>
        public static bool CallIn;

        public static bool Any => Craters || Music || Scenes || PilotGlitch || CallIn;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Craters = Array.IndexOf(keys, CratersKey) >= 0;
            Music = Array.IndexOf(keys, MusicKey) >= 0;
            ScenesCoop = Array.IndexOf(keys, CutscenesCoopKey) >= 0;
            Narrator = Array.IndexOf(keys, NarratorKey) >= 0;
            Scenes = ScenesCoop || Narrator || Array.IndexOf(keys, CutscenesKey) >= 0;
            Voice = PageQuery.Get(url, "voice")?.Trim().ToLowerInvariant();
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
                if (Narrator) parts.Add("NARRATOR");
                else if (Scenes) parts.Add("CUTSCENES");
                if (PilotGlitch) parts.Add("PILOT GLITCH-OUT");
                if (CallIn) parts.Add("TABK CALL-IN");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
