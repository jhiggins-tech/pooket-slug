namespace PooketSlug
{
    /// <summary>
    /// The player's settings from the title screen's SETTINGS: sound and screen shake. They're kept in this
    /// browser as one number (<see cref="Bits"/>), so a setting added later reads as its default in an old save.
    /// </summary>
    public struct Options
    {
        const int SoundOff = 1, ShakeOff = 2;

        public bool Sound, Shake;

        public static Options Default => new Options { Sound = true, Shake = true };

        /// <summary>What's stored: a bit for each setting turned off, so 0 is everything on.</summary>
        public int Bits => (Sound ? 0 : SoundOff) | (Shake ? 0 : ShakeOff);

        public static Options FromBits(int bits) => new Options { Sound = (bits & SoundOff) == 0, Shake = (bits & ShakeOff) == 0 };

        /// <summary>How a setting reads on its button.</summary>
        public static string Label(string name, bool on) => name + (on ? "  ON" : "  OFF");
    }
}
