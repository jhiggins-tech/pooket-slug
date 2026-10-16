using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=craters</c> turns on
    /// blasts digging craters in the boss arena, <c>?spike=music</c> the music's first cut, <c>?spike=callin</c> a tabk to call in as if tones2's were won, <c>?spike=partner</c> the partner arrow
    /// and warnings, <c>?spike=kcaj</c> kcaj's special and super. Spikes are solo only (a co-op game ignores them), except the partner one, which is about co-op.
    /// </summary>
    public static class Spike
    {
        public const string CratersKey = "craters", MusicKey = "music";
        public const string CallInKey = "callin", PartnerKey = "partner", KcajKey = "kcaj";

        /// <summary>Blasts dig craters in the boss arena (<see cref="CraterGround"/>).</summary>
        public static bool Craters;

        /// <summary>The music's first cut (<see cref="MusicTracks"/>): Mission one's theme, and the boss theme from the boss intro.</summary>
        public static bool Music;

        /// <summary>
        /// The tabk call-in as if tones2's tabk were won, the way Mission one's coda brings it in: one to call in, the
        /// prompt showing how until you call (<see cref="CallInCoach"/>, every visit), and the cargo drone lowering it in.
        /// </summary>
        public static bool CallIn;

        /// <summary>
        /// Spike coop-camera (<see cref="PartnerBeacon"/>): an arrow to a partner who's off screen, warnings when their health
        /// is low or they've switched away, and an away partner no longer holds the camera back. It works in co-op, and
        /// a solo game gets a pretend partner (<see cref="PartnerGhost"/>) to try it with.
        /// </summary>
        public static bool Partner;

        /// <summary>
        /// kcaj's special and super (<see cref="KcajTuning"/>) in the soldier's hands: SP is Hyperfixate, SU is
        /// Unmedicated, ready from the start and again after each rain. The <c>kcaj-beam</c> and <c>kcaj-pills</c> spikes.
        /// </summary>
        public static bool Kcaj;

        public static bool Any => Craters || Music || CallIn || Partner || Kcaj;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Craters = Array.IndexOf(keys, CratersKey) >= 0;
            Music = Array.IndexOf(keys, MusicKey) >= 0;
            CallIn = Array.IndexOf(keys, CallInKey) >= 0;
            Partner = Array.IndexOf(keys, PartnerKey) >= 0;
            Kcaj = Array.IndexOf(keys, KcajKey) >= 0;
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
                if (CallIn) parts.Add("TABK CALL-IN");
                if (Partner) parts.Add("PARTNER ARROW");
                if (Kcaj) parts.Add("KCAJ: HYPERFIXATE + UNMEDICATED");
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
