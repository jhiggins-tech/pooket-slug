namespace PooketSlug
{
    /// <summary>
    /// The player's settings from the title screen's SETTINGS (sound and screen shake), and who they play (the title's
    /// CREW, <see cref="PooketSlug.Crew"/>). They're kept in this browser as one number (<see cref="Bits"/>), so a setting
    /// added later reads as its default in an old save.
    /// </summary>
    public struct Options
    {
        const int SoundOff = 1, ShakeOff = 2, CrewShift = 2, CrewMask = 15 << CrewShift;

        public bool Sound, Shake;
        /// <summary>The soldier picked with CREW (a <see cref="SoldierDef"/> id; <see cref="PooketSlug.Crew.Pick"/> reads 0, never picked, as tones2).</summary>
        public int Crew;

        public static Options Default => new Options { Sound = true, Shake = true };

        /// <summary>What's stored: a bit for each setting turned off, so 0 is everything on, and the crew pick.</summary>
        public int Bits => (Sound ? 0 : SoundOff) | (Shake ? 0 : ShakeOff) | ((Crew & 15) << CrewShift);

        public static Options FromBits(int bits) =>
            new Options { Sound = (bits & SoundOff) == 0, Shake = (bits & ShakeOff) == 0, Crew = (bits & CrewMask) >> CrewShift };

        /// <summary>How a setting reads on its button.</summary>
        public static string Label(string name, bool on) => name + (on ? "  ON" : "  OFF");
    }
}
