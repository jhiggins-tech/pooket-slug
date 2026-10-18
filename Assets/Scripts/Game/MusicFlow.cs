namespace PooketSlug
{
    /// <summary>
    /// The title theme on the splash, and the mission's music as it plays out (<c>?spike=music</c>, solo only): the start sting into the mission's theme,
    /// cut at the boss's screen lock, the boss theme from its first shot, a victory sting into the pilot round's final
    /// section, and the tabk alert for the coda.
    /// </summary>
    public sealed class MusicFlow
    {
        readonly Game m_Game;

        public MusicFlow(Game game) => m_Game = game;

        /// <summary>?spike=music, which is solo only.</summary>
        public bool On => Spike.Music && !m_Game.Coop;

        /// <summary>At launch: rendered then, so the mission starts without a stutter.</summary>
        public static void Prepare()
        {
            if (Spike.Music) Music.Prepare(MusicTracks.All);
        }

        /// <summary>
        /// Each frame: the title theme loops while the splash (or the settings over it) is up, and fades as it's left
        /// (<c>docs/design/music.md</c>, The title theme). It plays without <c>?spike=music</c>.
        /// </summary>
        public void Title(bool showing)
        {
            if (showing) Music.Loop(MusicTracks.TitleRecording);
            else if (Music.Recording == MusicTracks.TitleRecording) Music.Stop(fade: true);
        }

        /// <summary>A solo mission starting: the start sting, then the mission's theme.</summary>
        public void MissionStart()
        {
            if (!Spike.Music) return;
            Music.Silence();
            Music.Sting(MusicTracks.MissionStart);
            Music.Play(MusicTracks.SteelNight, MusicTracks.MissionStart.Seconds);
        }

        /// <summary>The screen locks for the boss and the mission's music cuts out (<c>docs/design/tanks.md</c>).</summary>
        public void BossIntro()
        {
            if (On) Music.Stop();
        }

        /// <summary>The tank's first shot: the boss music kicks in.</summary>
        public void BossFightStarted()
        {
            if (On && Music.Playing != MusicTracks.TankAlertFinal) Music.Play(MusicTracks.TankAlert);
        }

        /// <summary>
        /// The tank's wrecked: a victory sting, then the boss theme's even faster final section for the pilot round,
        /// which crossfades from the chip to an orchestra playing the same notes.
        /// </summary>
        public void TankWrecked()
        {
            if (!On) return;
            Music.Sting(MusicTracks.BossVictory);
            // after one pass in chip, the orchestra takes over (in builds that have it: the vendor repository's CI makes it)
            Music.Play(MusicTracks.TankAlertFinal, MusicTracks.BossVictory.Seconds, Music.Layer("tank-alert-final"), MusicTracks.TankAlertFinal.Seconds);
        }

        /// <summary>On into the coda: the run out of the base with tones2's tabk.</summary>
        public void Coda()
        {
            if (On) Music.Play(MusicTracks.TankAlert);
        }

        /// <summary>Each frame of a solo mission: the pilot glitched out (cut dead by his big glitch-out), or it's won.</summary>
        public void Tick(World world)
        {
            if (On && (world.Victory || world.BossMusicOff) && Music.Playing != null) Music.Stop(fade: !world.BossMusicOff);
        }
    }
}
