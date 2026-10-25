using System.Text;

namespace PooketSlug
{
    /// <summary>
    /// A piece of music for the chip synth (<c>docs/design/music.md</c>): its tempo and four parts in the
    /// <see cref="Notation"/> notation, for the four Game Boy voices. A loop repeats with no seam (every part the same
    /// whole number of bars); a sting plays once.
    /// </summary>
    public sealed class MusicTrack
    {
        public string Name;
        public float Bpm;
        public bool Loop;
        /// <summary>The lead (pulse, <see cref="LeadDuty"/>), harmony (pulse 50%, quieter), bass (triangle) and drums (noise).</summary>
        public string Lead = "", Harmony = "", Bass = "", Drums = "";
        public float LeadDuty = 0.25f;

        /// <summary>Seconds per step (a sixteenth note).</summary>
        public float Step => 60f / Bpm / 4f;
        /// <summary>The longest part, in steps.</summary>
        public int Steps => System.Math.Max(System.Math.Max(Notation.Steps(Lead), Notation.Steps(Harmony)), System.Math.Max(Notation.Steps(Bass), Notation.Steps(Drums)));
        public float Seconds => Steps * Step;
    }

    /// <summary>
    /// The game's chip music, as in <c>docs/design/music.md</c>: Mission one's theme, the boss theme, the debrief's loop
    /// and the stings. A bar is 16 steps. Which plays when is <c>MusicWhen.cs</c>.
    /// </summary>
    public static partial class MusicTracks
    {
        public const int Bar = 16;

        // Chords as arpeggio notes (root, third, fifth, octave) and a bass root and fifth.
        struct Chord
        {
            public string A, B, C, D, Root, Fifth;
            public Chord(string a, string b, string c, string d, string root, string fifth)
            { A = a; B = b; C = c; D = d; Root = root; Fifth = fifth; }
        }

        static readonly Chord Am = new Chord("a3", "c4", "e4", "a4", "a2", "e3");
        static readonly Chord F = new Chord("f3", "a3", "c4", "f4", "f2", "c3");
        static readonly Chord G = new Chord("g3", "b3", "d4", "g4", "g2", "d3");
        static readonly Chord E = new Chord("e3", "g#3", "b3", "e4", "e2", "b2");
        static readonly Chord Dm = new Chord("d3", "f3", "a3", "d4", "d2", "a2");
        static readonly Chord Em = new Chord("e3", "g3", "b3", "e4", "e2", "b2");
        static readonly Chord C = new Chord("c4", "e4", "g4", "c5", "c2", "g2");
        static readonly Chord D = new Chord("d4", "f#4", "a4", "d5", "d2", "a2");
        static readonly Chord B = new Chord("b3", "d#4", "f#4", "b4", "b1", "f#2");

        // the debrief's sevenths: a lounge after the base
        static readonly Chord Fmaj7 = new Chord("f3", "a3", "c4", "e4", "f2", "c3");
        static readonly Chord Dm7 = new Chord("d3", "f3", "a3", "c4", "d2", "a2");
        static readonly Chord Gm7 = new Chord("g3", "bb3", "d4", "f4", "g2", "d3");
        static readonly Chord C7 = new Chord("c3", "e3", "g3", "bb3", "c2", "g2");

        static string Join(params string[] parts) => string.Join(" ", parts);

        static string Repeat(string part, int times)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < times; i++) sb.Append(part).Append(' ');
            return sb.ToString();
        }

        /// <summary>A bar of searchlight-sweep sixteenths up and down the chord.</summary>
        static string Sweep(Chord c)
        {
            string[] n = { c.A, c.B, c.C, c.D };
            int[] order = { 0, 1, 2, 3, 2, 1, 0, 1, 2, 3, 2, 1, 0, 1, 2, 1 };
            var sb = new StringBuilder(n[order[0]] + ":1");
            for (int i = 1; i < order.Length; i++) sb.Append(' ').Append(n[order[i]]);
            return sb.ToString();
        }

        static string Sweeps(params Chord[] chords)
        {
            var sb = new StringBuilder();
            foreach (Chord c in chords) sb.Append(Sweep(c)).Append(' ');
            return sb.ToString();
        }

        /// <summary>A bar of marching quavers on the root, with the fifth on the off-beats.</summary>
        static string March(Chord c) => $"{c.Root}:2 {c.Root} {c.Fifth} {c.Root} {c.Root} {c.Root} {c.Fifth} {c.Root}";

        static string Marches(params Chord[] chords)
        {
            var sb = new StringBuilder();
            foreach (Chord c in chords) sb.Append(March(c)).Append(' ');
            return sb.ToString();
        }

        /// <summary>A bar of galloping bass (one long, two short) on the root.</summary>
        static string Gallop(Chord c) => Repeat($"{c.Root}:2 {c.Root}:1 {c.Root}:1", 4);

        static string Gallops(params Chord[] chords)
        {
            var sb = new StringBuilder();
            foreach (Chord c in chords) sb.Append(Gallop(c));
            return sb.ToString();
        }

        /// <summary>A bar of off-beat chord stabs.</summary>
        static string Stabs(Chord c) => Repeat($"r:2 {c.A}+{c.B}+{c.C}:2", 4);

        static string Stabbing(params Chord[] chords)
        {
            var sb = new StringBuilder();
            foreach (Chord c in chords) sb.Append(Stabs(c));
            return sb.ToString();
        }

        // ---------------------------------------------------------------- Mission one: "Steel Night"

        const string MarchBeat = "k:2 h:2 s:2 h:1 s:1 k:2 h:2 s:2 s:1 s:1 ";
        const string MarchFill = "s:1 s s s s:2 s:2 s:1 s s s k:2 c:2 ";

        // A part: Am Am F G | Am Am F E
        const string SteelA =
            "a4:4 e5:2 a5:2 g5:2 e5:2 c5:2 d5:2 " +
            "e5:6 d5:2 c5:2 b4:2 c5:4 " +
            "f5:4 e5:2 c5:2 a4:4 c5:4 " +
            "d5:2 d5:2 b4:2 g4:2 b4:2 d5:2 g5:4 " +
            "a4:4 e5:2 a5:2 g5:2 e5:2 c5:2 d5:2 " +
            "e5:6 g5:2 a5:2 g5:2 e5:4 " +
            "f5:4 a5:2 f5:2 c5:4 a4:4 " +
            "b4:2 c5:2 d5:2 e5:2 g#5:4 e5:4 ";

        // B part: Dm Dm Am Am F G E E
        const string SteelB =
            "d5:4 f5:2 a5:2 d6:4 a5:4 " +
            "c6:2 a5:2 f5:2 d5:2 f5:4 a5:4 " +
            "e6:4 c6:2 a5:2 e5:4 a5:4 " +
            "g5:2 a5:2 g5:2 e5:2 c5:8 " +
            "a5:4 c6:2 a5:2 f5:4 a5:4 " +
            "b5:4 d6:2 b5:2 g5:4 d6:4 " +
            "e6:8 d6:2 c6:2 b5:2 g#5:2 " +
            "e5:2 r:2 e5:2 r:2 e5:1 f#5 g#5 a5 b5:4 ";

        static readonly Chord[] SteelAChords = { Am, Am, F, G, Am, Am, F, E };
        static readonly Chord[] SteelBChords = { Dm, Dm, Am, Am, F, G, E, E };

        /// <summary>Mission one's theme: a snare-led military march in A minor, an A part and a B part, 32 bars.</summary>
        public static readonly MusicTrack SteelNight = new MusicTrack
        {
            Name = "Steel Night",
            Bpm = 150f,
            Loop = true,
            LeadDuty = 0.25f,
            Lead = SteelA + SteelA + SteelB + SteelB,
            Harmony = Repeat(Sweeps(SteelAChords), 2) + Repeat(Sweeps(SteelBChords), 2),
            Bass = Repeat(Marches(SteelAChords), 2) + Repeat(Marches(SteelBChords), 2),
            Drums = Repeat(Repeat(MarchBeat, 7) + MarchFill, 4),
        };

        // ---------------------------------------------------------------- the boss: "Tabk Alert"

        const string AlertBeat = "k:2 h:1 h s:2 h:1 h k:1 k h:1 h s:2 h:1 s ";
        const string AlertFill = "s:1 s s s k:2 s:1 s k:2 s:1 s s s c:2 ";

        // Em Em C D | Em Em C B
        const string AlertA =
            "e5:1 e5 r e5 g5:2 e5:2 b5:2 a5:2 g5:2 f#5:2 " +
            "e5:2 r:2 b4:2 d5:2 e5:8 " +
            "g5:1 g5 r g5 a5:2 g5:2 e5:2 c6:2 b5:2 g5:2 " +
            "f#5:4 a5:4 d6:4 c6:2 a5:2 " +
            "e5:1 e5 r e5 g5:2 e5:2 b5:2 a5:2 g5:2 f#5:2 " +
            "e6:2 r:2 d6:2 b5:2 g5:4 e5:4 " +
            "c6:4 b5:2 g5:2 e5:4 g5:4 " +
            "f#5:2 a5:2 b5:2 d#6:2 f#6:4 d#6:4 ";

        // the second half climbs: the same tune, the lead an octave's worth of alarm higher at the end
        const string AlertB =
            "b5:1 b5 r b5 e6:2 b5:2 g5:2 e5:2 g5:2 b5:2 " +
            "e6:2 r:2 d6:2 b5:2 e6:8 " +
            "c6:1 c6 r c6 e6:2 c6:2 g5:2 e5:2 g5:2 c6:2 " +
            "d6:4 a5:4 f#5:4 a5:2 d6:2 " +
            "e6:2 d6:2 b5:2 g5:2 e6:2 d6:2 b5:2 g5:2 " +
            "e6:2 r:2 g6:2 f#6:2 e6:8 " +
            "c6:2 e6:2 g6:2 e6:2 c6:4 g5:4 " +
            "b5:1 c6 d#6 f#6 b5 c6 d#6 f#6 b6:4 f#6:4 ";

        static readonly Chord[] AlertChords = { Em, Em, C, D, Em, Em, C, B };

        /// <summary>The boss theme: faster and in a minor key (E minor), galloping bass, alarm stabs, 16 bars.</summary>
        public static readonly MusicTrack TankAlert = new MusicTrack
        {
            Name = "Tabk Alert",
            Bpm = 172f,
            Loop = true,
            LeadDuty = 0.125f,
            Lead = AlertA + AlertB,
            Harmony = Stabbing(AlertChords) + Sweeps(AlertChords),
            Bass = Gallops(AlertChords) + Gallops(AlertChords),
            Drums = Repeat(Repeat(AlertBeat, 7) + AlertFill, 2),
        };

        // ---------------------------------------------------------------- the boss's second stage: "Tabk Alert (final)"

        // Every sixteenth a hat, kicks and snares on top: the last push.
        const string FinalBeat = "k+h:1 h s+h h k+h h s+h h k+h k+h s+h h k+h h s+h s ";

        /// <summary>
        /// The boss theme's final section, for the fight's second stage (the hologram pilot on foot): the same tune,
        /// even faster, its climbing half first, with a hat on every sixteenth and the sweeps on top. 16 bars.
        /// </summary>
        public static readonly MusicTrack TankAlertFinal = new MusicTrack
        {
            Name = "Tabk Alert (final)",
            Bpm = 200f,
            Loop = true,
            LeadDuty = 0.125f,
            Lead = AlertB + AlertA,
            Harmony = Sweeps(AlertChords) + Sweeps(AlertChords),
            Bass = Gallops(AlertChords) + Gallops(AlertChords),
            Drums = Repeat(Repeat(FinalBeat, 3) + AlertFill, 4),
        };

        // ---------------------------------------------------------------- stings

        /// <summary>Under the caption card as the mission starts, leading into Mission one's theme.</summary>
        public static readonly MusicTrack MissionStart = new MusicTrack
        {
            Name = "Mission start",
            Bpm = 150f,
            LeadDuty = 0.25f,
            Lead = "a4:1 c5 e5 a5:2 r:1 a5:1 g5:2 a5:8",
            Harmony = "e4:1 a4 c5 e5:2 r:1 e5:1 d5:2 e5:8",
            Bass = "a2:4 e3:4 a2:8",
            Drums = "s:1 s s s s:2 s:2 c:8",
        };

        /// <summary>When the boss tank is wrecked, over the boss theme.</summary>
        public static readonly MusicTrack BossVictory = new MusicTrack
        {
            Name = "Boss victory",
            Bpm = 172f,
            LeadDuty = 0.125f,
            Lead = "e5:1 g5 b5 e6:2 r:1 d6:1 e6:8",
            Harmony = "b4:1 e5 g5 b5:2 r:1 a5:1 b5:8",
            Bass = "e3:2 b2:2 e2:8",
            Drums = "s:1 s s s k:2 c:8",
        };

        /// <summary>The mission's won: the start sting's run, grown into a held major chord.</summary>
        public static readonly MusicTrack MissionComplete = new MusicTrack
        {
            Name = "Mission complete",
            Bpm = 150f,
            LeadDuty = 0.25f,
            Lead = "a4:1 c#5 e5 a5:3 r:1 e5:2 a5:2 c#6:2 b5:2 c#6:2 e6:2 a6:11",
            Harmony = "e4:1 a4 c#5 e5:3 r:1 c#5:2 e5:2 a5:2 g#5:2 a5:2 c#6:2 e5+a5+c#6:11",
            Bass = "a2:4 e3:4 a2:2 e3:2 a2:2 e2:2 a2:14",
            Drums = "s:1 s s s:3 r:1 s:2 s s s s s c:11",
        };

        /// <summary>The debrief's rank stamp: a short fanfare, over the debrief's loop.</summary>
        public static readonly MusicTrack RankUp = new MusicTrack
        {
            Name = "Rank up",
            Bpm = 150f,
            LeadDuty = 0.125f,
            Lead = "c5:1 e5 g5 c6:3 r:1 g5:1 c6:2 e6:10",
            Harmony = "g4:1 c5 e5 g5:3 r:1 e5:1 g5:2 c5+e5+g5:10",
            Bass = "c3:4 g2:4 c2:12",
            Drums = "k:4 s:2 s s:1 s c:10",
        };

        /// <summary>Out of lives: a sad run down, sliding off the end into a rattle.</summary>
        public static readonly MusicTrack GameOver = new MusicTrack
        {
            Name = "Game over",
            Bpm = 120f,
            LeadDuty = 0.25f,
            Lead = "e5:2 d5 c5 b4 a4 g#4 a4~:4 a3:8",
            Harmony = "c5:2 b4 a4 g#4 f4 e4 e4+c4:12",
            Bass = "a2:4 f2:4 e2:4 a1:12",
            Drums = "r:16 k:1 k k k k k k k",
        };

        /// <summary>While a continue is offered: a ticking bass note a second, a semitone higher each time.</summary>
        public static readonly MusicTrack ContinueCountdown = new MusicTrack
        {
            Name = "Continue countdown",
            Bpm = 60f,
            LeadDuty = 0.125f,
            Lead = "a4:1 r:3 bb4:1 r:3 b4:1 r:3 c5:1 r:3 c#5:1 r:3 d5:1 r:3 eb5:1 r:3 e5:1 r:3 f5:1 r:3",
            Harmony = "r:36",
            Bass = "a2:1 r:3 bb2:1 r:3 b2:1 r:3 c3:1 r:3 c#3:1 r:3 d3:1 r:3 eb3:1 r:3 e3:1 r:3 f3:1 r:3",
            Drums = "k:1 r:3 k:1 r:3 k:1 r:3 k:1 r:3 k:1 r:3 k:1 r:3 k:1 r:3 k:1 r:3 k:1 r:3",
        };

        /// <summary>A crewmate's freed: a bright run up in G. Written, but nothing plays it yet: the crew reveal it was for has gone, and it waits for the den's.</summary>
        public static readonly MusicTrack NewCrewmate = new MusicTrack
        {
            Name = "New crewmate",
            Bpm = 150f,
            LeadDuty = 0.25f,
            Lead = "d5:1 g5 b5 d6:2 b5:2 c6:2 d6:2 g6:9",
            Harmony = "b4:1 d5 g5 b5:2 g5:2 a5:2 b5:2 b5+d6:9",
            Bass = "g2:4 d3:4 g2:12",
            Drums = "h:2 h h h s:2 s c:8",
        };

        // ---------------------------------------------------------------- the debrief

        static readonly Chord[] DebriefChords = { Fmaj7, Dm7, Gm7, C7, Fmaj7, Dm7, Gm7, C7 };

        static string Comps(params Chord[] chords)
        {
            var sb = new StringBuilder();
            foreach (Chord c in chords) sb.Append(Repeat($"r:4 {c.A}+{c.B}+{c.C}+{c.D}:2 r:2", 2));
            return sb.ToString();
        }

        static string Walks(params Chord[] chords)
        {
            var sb = new StringBuilder();
            foreach (Chord c in chords) sb.Append($"{c.Root}:3 {c.Root}:1 {c.Fifth}:4 {c.Root}:3 {c.Root}:1 {c.Fifth}:4 ");
            return sb.ToString();
        }

        /// <summary>Under the debrief's XP and rank: easy and swung (dotted pairs), in F with sevenths, 8 bars.</summary>
        public static readonly MusicTrack Debrief = new MusicTrack
        {
            Name = "Debrief",
            Bpm = 100f,
            Loop = true,
            LeadDuty = 0.25f,
            Lead =
                "r:4 a4:3 c5:1 e5:4 c5:4 " +
                "d5:6 c5:2 a4:8 " +
                "r:4 bb4:3 d5:1 f5:4 d5:4 " +
                "e5:3 d5:1 c5:4 bb4:4 g4:4 " +
                "r:4 a4:3 c5:1 f5:4 e5:4 " +
                "d5:3 e5:1 f5:4 a5:8 " +
                "g5:3 f5:1 d5:4 bb4:4 d5:4 " +
                "c5:8 r:8",
            Harmony = Comps(DebriefChords),
            Bass = Walks(DebriefChords),
            Drums = Repeat("k:3 h:1 s:3 h:1 k:3 h:1 s:3 h:1 ", 8),
        };

        public static readonly MusicTrack[] All =
        {
            SteelNight, TankAlert, TankAlertFinal, Debrief,
            MissionStart, BossVictory, MissionComplete, RankUp, GameOver, ContinueCountdown, NewCrewmate,
        };
    }
}
