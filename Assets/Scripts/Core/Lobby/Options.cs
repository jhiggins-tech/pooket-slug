namespace PooketSlug
{
    /// <summary>A volume setting's steps, loudest first, so a setting never touched reads as full volume.</summary>
    public enum Volume { High, Mid, Low, Off }

    /// <summary>
    /// The player's settings from the title screen's SETTINGS (sound, the effects', voices' and music's volumes, screen shake and
    /// the left-handed pad), who they play (the title's CREW, <see cref="PooketSlug.Crew"/>) and the tabk they've loaded. They're kept in this
    /// browser as one number (<see cref="Bits"/>), so a setting added later reads as its default in an old save.
    /// </summary>
    public struct Options
    {
        const int SoundOff = 1, ShakeOff = 2, CrewShift = 2, CrewMask = 15 << CrewShift;
        const int EffectsShift = 6, VoicesShift = 8, VolumeMask = 3, Lefty = 1 << 10, MusicShift = 11, TabkShift = 13, TabkMask = 15 << TabkShift;

        /// <summary>Everything on or off at once (M mutes it too).</summary>
        public bool Sound, Shake;
        /// <summary>How loud the game's sounds are, and the recorded voices (the narrator and the announcer).</summary>
        public Volume Effects, Voices;
        /// <summary>How loud the music is (<c>docs/design/music.md</c>): its own setting, under SOUND.</summary>
        public Volume Music;
        /// <summary>The touch pad swapped round: the stick under the right thumb, the buttons under the left.</summary>
        public bool LeftHanded;
        /// <summary>The soldier picked with CREW (a <see cref="SoldierDef"/> id; <see cref="PooketSlug.Crew.Pick"/> reads 0, never picked, as tones2).</summary>
        public int Crew;
        /// <summary>The tabk loaded on the bay's repair stands (its crewmate's soldier id, <see cref="TabkCards"/>; 0, never picked, reads as tones2's).</summary>
        public int Tabk;

        public static Options Default => new Options { Sound = true, Shake = true };

        /// <summary>What's stored: a bit for each setting turned off, each volume's steps down, the left-handed pad, the crew pick and the tabk, so 0 is the defaults.</summary>
        public int Bits => (Sound ? 0 : SoundOff) | (Shake ? 0 : ShakeOff) | ((Crew & 15) << CrewShift) |
                           (((int)Effects & VolumeMask) << EffectsShift) | (((int)Voices & VolumeMask) << VoicesShift) | (LeftHanded ? Lefty : 0) |
                           (((int)Music & VolumeMask) << MusicShift) | ((Tabk & 15) << TabkShift);

        public static Options FromBits(int bits) => new Options
        {
            Sound = (bits & SoundOff) == 0,
            Shake = (bits & ShakeOff) == 0,
            Crew = (bits & CrewMask) >> CrewShift,
            Effects = (Volume)((bits >> EffectsShift) & VolumeMask),
            Voices = (Volume)((bits >> VoicesShift) & VolumeMask),
            LeftHanded = (bits & Lefty) != 0,
            Music = (Volume)((bits >> MusicShift) & VolumeMask),
            Tabk = (bits & TabkMask) >> TabkShift,
        };

        /// <summary>How a setting reads on its button.</summary>
        public static string Label(string name, bool on) => name + (on ? "  ON" : "  OFF");

        /// <summary>How a volume setting reads on its button, e.g. "EFFECTS  MID".</summary>
        public static string Label(string name, Volume volume) => name + "  " + volume.ToString().ToUpperInvariant();

        /// <summary>A volume step as a gain: full, about two thirds, about a third, silent.</summary>
        public static float Gain(Volume volume)
        {
            switch (volume)
            {
                case Volume.High: return 1f;
                case Volume.Mid: return 0.6f;
                case Volume.Low: return 0.3f;
                default: return 0f;
            }
        }

        /// <summary>The next step a volume button goes to: quieter each tap, then back round to high.</summary>
        public static Volume Next(Volume volume) => volume == Volume.Off ? Volume.High : volume + 1;
    }
}
