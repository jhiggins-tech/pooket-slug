namespace PooketSlug
{
    /// <summary>
    /// Which track plays when (<c>docs/design/music.md</c>): the recordings behind the menus, the mission's theme, the boss
    /// music, the stings and the debrief's loop, and what's rendered at launch. <c>MusicFlow</c> plays what these say;
    /// when a mission's music changes as it plays is <see cref="Soundtrack"/>'s rule.
    /// </summary>
    public static partial class MusicTracks
    {

        /// <summary>
        /// The title theme: a recording (Lyria take D, a 27.7 s seamless loop), not chip, so it isn't in the notation.
        /// CI fetches it from the vendor repository's start-music release into <c>Resources/Music/title.bytes</c>.
        /// </summary>
        public const string TitleRecording = "title";

        /// <summary>
        /// The den's theme: a recording too (the console-shop bossa, take 2, cut to a 170 s seamless loop), from the
        /// vendor repository's den-music release into <c>Resources/Music/den.bytes</c>.
        /// </summary>
        public const string DenRecording = "den";

        /// <summary>
        /// The recording that loops behind a menu screen: the den's theme in the den, the title theme on the splash and the
        /// settings over it, and none anywhere else (cutscenes, missions and the debrief have their own music, or none).
        /// While one of the den's beats plays as you come back (the first visit, a crewmate moving in), its theme waits till
        /// the beat's over (the owner, 20 Oct).
        /// </summary>
        public static string MenuRecording(bool splash, bool den, bool beat = false) =>
            den ? (beat ? null : DenRecording) : splash ? TitleRecording : null;

        /// <summary>How loud a recording plays, against the title theme: the den's is background music, so about half.</summary>
        public static float Level(string recording) => recording == DenRecording ? 0.5f : 1f;

        /// <summary>How long a recording fades in as it starts: the den's theme comes up gently (Jack, 20 Oct), the title's at once.</summary>
        public static float FadeIn(string recording) => recording == DenRecording ? DenFadeIn : 0f;
        public const float DenFadeIn = 3f;

        /// <summary>How far the den's theme dips while a menu (the kit screen, the mission screen) is open over the den.</summary>
        public const float MenuDip = 0.6f;

        /// <summary>
        /// A mission's theme, after its start sting. Only Mission one has a theme yet, so every mission (the creamery too)
        /// plays Steel Night until its own is written with its map (music.md).
        /// </summary>
        public static MusicTrack MissionTheme(MissionDef mission) => SteelNight;

        /// <summary>The sting a mission starts (or restarts, or is joined) with; its theme comes in as it ends.</summary>
        public static MusicTrack StartSting => MissionStart;

        /// <summary>
        /// The boss music from the tabk's first shot: the boss theme, unless its final section is already playing, which
        /// it doesn't cut back from.
        /// </summary>
        public static MusicTrack BossTheme(MusicTrack playing) => playing == TankAlertFinal ? null : TankAlert;

        /// <summary>The boss's tabk is wrecked: this sting, then <see cref="FinalSection"/> for the pilot round.</summary>
        public static MusicTrack BossBeaten => BossVictory;

        /// <summary>The pilot round: the boss theme's faster final section, crossfading after one pass to its recording.</summary>
        public static MusicTrack FinalSection => TankAlertFinal;

        /// <summary>The orchestra's recording of <see cref="FinalSection"/>, <c>Resources/Music/tank-alert-final.bytes</c>.</summary>
        public const string FinalSectionRecording = "tank-alert-final";

        /// <summary>The coda, the run out of the base in tones2's tabk: the boss theme again (an open pick in music.md).</summary>
        public static MusicTrack CodaTheme => TankAlert;

        /// <summary>The sting a mission's moment plays (<see cref="Soundtrack.Frame"/>), or null for one with none.</summary>
        public static MusicTrack StingFor(MusicCue cue) =>
            cue == MusicCue.Countdown ? ContinueCountdown : cue == MusicCue.Complete ? MissionComplete : null;

        /// <summary>
        /// How long the debrief's loop waits, <paramref name="sinceWon"/> seconds after the win: until the mission complete
        /// sting has finished, and not at all once it has.
        /// </summary>
        public static float DebriefDelay(float sinceWon) => System.Math.Max(0f, MissionComplete.Seconds - sinceWon);

        /// <summary>The debrief's loop, the game over sting and the rank up fanfare (over the debrief's loop).</summary>
        public static MusicTrack DebriefLoop => Debrief;
        public static MusicTrack GameOverSting => GameOver;
        public static MusicTrack RankUpSting => RankUp;

        /// <summary>
        /// What's rendered at launch (with the music on, or as soon as it's turned on), so a mission starts and its boss
        /// arrives without a stutter: the first mission's theme, the boss tracks and the two stings between them. The rest
        /// renders the first time it plays.
        /// </summary>
        public static MusicTrack[] RenderedAhead => new[] { MissionTheme(Missions.One), BossTheme(null), FinalSection, StartSting, BossBeaten };
    }
}
