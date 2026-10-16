namespace PooketSlug
{
    /// <summary>
    /// The boss intro's timeline (<c>docs/design/tanks.md</c>, The boss intro), as a pure function of the seconds since
    /// it began: the siren and the announcer's cues, and what the HUD shows. The full intro: a siren twice, a flashing
    /// red WARNING! between hazard stripes, the screen edges flashing, then the tank's name card as the announcer calls
    /// it. The quick intro (a tank you've met this run): the banner and one siren only.
    /// </summary>
    public readonly struct BossIntroShow
    {
        public enum Cue { None, Siren, SayWarning, SayBoss }

        public const float SirenGap = 0.6f, StripesIn = 0.3f, StripesOut = 0.3f, FlashRate = 2.5f;
        public const float FullBanner = 2.8f, QuickBanner = 1.6f, NameAt = 1.4f, NameUntil = 3.4f, EdgesFor = 1.2f;

        static readonly (float at, Cue cue)[] FullCues = { (0f, Cue.Siren), (0.1f, Cue.SayWarning), (SirenGap, Cue.Siren), (NameAt + 0.1f, Cue.SayBoss) };
        static readonly (float at, Cue cue)[] QuickCues = { (0f, Cue.Siren), (0.1f, Cue.SayWarning) };

        public readonly float T;
        public readonly bool Full;

        public BossIntroShow(float t, bool full)
        {
            T = t;
            Full = full;
        }

        public float BannerFor => Full ? FullBanner : QuickBanner;
        public bool Over => T >= (Full ? NameUntil : QuickBanner);

        /// <summary>WARNING! is on screen, and this frame it's lit (it flashes).</summary>
        public bool Warning => T >= 0f && T < BannerFor;
        public bool WarningLit => Warning && (int)(T * FlashRate * 2f) % 2 == 0;

        /// <summary>How far the hazard stripes are in, 0..1: they sweep in, hold, and sweep out with the banner.</summary>
        public float Stripes
        {
            get
            {
                if (!Warning) return 0f;
                if (T < StripesIn) return T / StripesIn;
                float left = BannerFor - T;
                return left < StripesOut ? left / StripesOut : 1f;
            }
        }

        /// <summary>The red at the screen's edges, 0..1: two pulses at the start of the full intro.</summary>
        public float Edges => Full && T >= 0f && T < EdgesFor ? 0.5f + 0.5f * UnityEngine.Mathf.Cos(T / EdgesFor * 4f * UnityEngine.Mathf.PI) : 0f;

        /// <summary>The tank's name card is up (full intro only).</summary>
        public bool NameCard => Full && T >= NameAt && T < NameUntil;

        /// <summary>The cues due in (<paramref name="from"/>, <paramref name="to"/>], in order, into <paramref name="due"/>; returns how many.</summary>
        public static int Cues(bool full, float from, float to, Cue[] due)
        {
            int n = 0;
            foreach ((float at, Cue cue) in full ? FullCues : QuickCues)
                if (at > from && at <= to && n < due.Length) due[n++] = cue; // start from below 0 so the first cue fires
            return n;
        }
    }
}
