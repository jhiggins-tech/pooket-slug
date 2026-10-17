using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike tests: small throwaway experiments switched on from the page address, so they ride along in the
    /// normal build without changing the game anyone else plays. <c>?spike=craters</c> turns on
    /// blasts digging craters in the boss arena, <c>?spike=music</c> the music's first cut, <c>?spike=partner</c> the partner arrow
    /// and warnings, <c>?spike=kcaj</c> kcaj's special and super, <c>?spike=dropin</c> each player's own score in co-op,
    /// <c>?spike=sheets</c> the riflemen walking with a generated sprite sheet (<c>&amp;take=</c> picks another model's).
    /// Spikes are solo only (a co-op game ignores them), except the partner and drop-in ones, which are about co-op.
    /// </summary>
    public static class Spike
    {
        public const string CratersKey = "craters", MusicKey = "music";
        public const string PartnerKey = "partner", KcajKey = "kcaj", DropInKey = "dropin", SheetsKey = "sheets";

        /// <summary>Blasts dig craters in the boss arena (<see cref="CraterGround"/>).</summary>
        public static bool Craters;

        /// <summary>The music's first cut (<see cref="MusicTracks"/>): Mission one's theme, and the boss theme from the boss intro.</summary>
        public static bool Music;

        /// <summary>
        /// Spike coop-camera (<see cref="PartnerBeacon"/>): an arrow to a partner who's off screen, warnings when their health
        /// is low or they've switched away, and an away partner no longer holds the camera back. It works in co-op, and
        /// a solo game gets a pretend partner (<see cref="PartnerGhost"/>) to try it with.
        /// </summary>
        public static bool Partner;

        /// <summary>
        /// kcaj's special and super (<see cref="KcajTuning"/>) in the soldier's hands: SP is Hyperfixate, SU is
        /// Unmedicated, ready from the start and again after each rain.
        /// </summary>
        public static bool Kcaj;

        /// <summary>
        /// Spike drop-in (<see cref="DropIn"/>): in co-op, the HUD shows each player's own score (from their kills) beside
        /// the shared one. Joining a mission under way already works for everyone; this is about the scores.
        /// </summary>
        public static bool DropIn;

        /// <summary>
        /// Spike gen-sprites: riflemen are drawn from a generated sprite sheet (Resources/Sheets, fetched from the vendor
        /// repository's sheets release) instead of the hand-made parts (<see cref="GeneratedSheets"/>).
        /// </summary>
        public static bool Sheets;

        /// <summary>Which sheet to show (<c>&amp;take=</c>: a model, or a model and take like <c>seedream-5-0-flash__2</c>), or null for the first.</summary>
        public static string SheetsTake;

        public static bool Any => Craters || Music || Partner || Kcaj || DropIn || Sheets;

        /// <summary>Reads <c>?spike=</c> from the page address.</summary>
        public static void Configure(string url)
        {
            string[] keys = (PageQuery.Get(url, "spike") ?? "").ToLowerInvariant().Split(',');
            Craters = Array.IndexOf(keys, CratersKey) >= 0;
            Music = Array.IndexOf(keys, MusicKey) >= 0;
            Partner = Array.IndexOf(keys, PartnerKey) >= 0;
            Kcaj = Array.IndexOf(keys, KcajKey) >= 0;
            DropIn = Array.IndexOf(keys, DropInKey) >= 0;
            Sheets = Array.IndexOf(keys, SheetsKey) >= 0;
            SheetsTake = PageQuery.Get(url, "take");
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
                if (Partner) parts.Add("PARTNER ARROW");
                if (Kcaj) parts.Add("KCAJ: HYPERFIXATE + UNMEDICATED");
                if (DropIn) parts.Add("OWN SCORES");
                if (Sheets) parts.Add("GENERATED SPRITE SHEETS" + (string.IsNullOrEmpty(SheetsTake) ? "" : " (" + SheetsTake.ToUpperInvariant() + ")"));
                return "SPIKE TEST · " + string.Join(" + ", parts);
            }
        }
    }
}
