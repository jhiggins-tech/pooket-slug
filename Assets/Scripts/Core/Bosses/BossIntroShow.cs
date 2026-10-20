namespace PooketSlug
{
    /// <summary>
    /// The boss intro's timeline (<c>docs/design/tanks.md</c>, The boss intro), as a pure function of the seconds since
    /// it began: the siren and the announcer's cues, and what the HUD shows. The full intro: a siren twice, a flashing
    /// red WARNING! between hazard stripes, the screen edges flashing, then the tank's name card as the announcer calls
    /// it. The quick intro (a tank you've met this run): the banner and one siren only.
    /// <para>
    /// A full intro can end with its pilot's (<c>docs/design/cutscenes/mission-two.md</c>, kcaj's tabk arrives): as the name card
    /// goes, the hatch pops and the Trollogram looks out (a riser under it), then their name slams in on the intro band
    /// with the musical hit, and they say their line. It's the one time an intro holds the action: solo, the game
    /// stands still from the hatch to the end (<see cref="Holding"/>).
    /// </para>
    /// </summary>
    public readonly struct BossIntroShow
    {
        public enum Cue { None, Siren, SayWarning, SayBoss, Hatch, Riser, Hit }

        public const float SirenGap = 0.6f, StripesIn = 0.3f, StripesOut = 0.3f, FlashRate = 2.5f;
        public const float FullBanner = 2.8f, QuickBanner = 1.6f, NameAt = 1.4f, NameUntil = 3.4f, EdgesFor = 1.2f;

        static readonly (float at, Cue cue)[] FullCues = { (0f, Cue.Siren), (0.1f, Cue.SayWarning), (SirenGap, Cue.Siren), (NameAt + 0.1f, Cue.SayBoss) };
        static readonly (float at, Cue cue)[] QuickCues = { (0f, Cue.Siren), (0.1f, Cue.SayWarning) };

        /// <summary>The pilot's part, from the name card going: the hatch rising, the band landing, the line, the end.</summary>
        public const float PilotFrom = NameUntil, HatchFor = 0.5f, BandAt = PilotFrom + 1.1f, BandFor = 1.8f, LineAt = BandAt + 0.3f, PilotUntil = BandAt + BandFor + 0.4f;
        static readonly (float at, Cue cue)[] PilotCues = { (PilotFrom, Cue.Hatch), (PilotFrom + 0.05f, Cue.Riser), (BandAt, Cue.Hit) };

        public readonly float T;
        public readonly bool Full;
        /// <summary>A full intro with its pilot's part.</summary>
        public readonly bool Pilot;

        public BossIntroShow(float t, bool full, bool pilot = false)
        {
            T = t;
            Full = full;
            Pilot = full && pilot;
        }

        public float BannerFor => Full ? FullBanner : QuickBanner;
        public bool Over => T >= (Pilot ? PilotUntil : Full ? NameUntil : QuickBanner);

        /// <summary>The pilot's part is on: solo, the game holds still for it.</summary>
        public bool Holding => Pilot && T >= PilotFrom && T < PilotUntil;

        /// <summary>How far the pilot's up out of the hatch, 0..1 (0 outside the pilot's part).</summary>
        public float Hatch => Holding ? UnityEngine.Mathf.Clamp01((T - PilotFrom) / HatchFor) : 0f;

        /// <summary>The pilot's intro band is up, with their name.</summary>
        public bool Band => Pilot && T >= BandAt && T < BandAt + BandFor;

        /// <summary>How the band's landing is going, 0..1 over its first moment (the flash and the slam), 1 after.</summary>
        public float BandIn => Band ? UnityEngine.Mathf.Clamp01((T - BandAt) / 0.2f) : 0f;

        /// <summary>The pilot's line is up.</summary>
        public bool Line => Pilot && T >= LineAt && T < PilotUntil;

        /// <summary>WARNING! is on screen, and this frame it's lit (it flashes).</summary>
        public bool Warning => T >= 0f && T < BannerFor;
        public bool WarningLit => Warning && Fx.Blink(T, FlashRate * 2f);

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
        public static int Cues(bool full, float from, float to, Cue[] due) => Cues(full, false, from, to, due);

        /// <summary>The same, with the pilot's part if <paramref name="pilot"/> (a full intro only).</summary>
        public static int Cues(bool full, bool pilot, float from, float to, Cue[] due)
        {
            int n = 0;
            foreach ((float at, Cue cue) in full ? FullCues : QuickCues)
                if (at > from && at <= to && n < due.Length) due[n++] = cue; // start from below 0 so the first cue fires
            if (full && pilot)
                foreach ((float at, Cue cue) in PilotCues)
                    if (at > from && at <= to && n < due.Length) due[n++] = cue;
            return n;
        }
    }
}
