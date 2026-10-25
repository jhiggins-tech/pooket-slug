namespace PooketSlug
{
    /// <summary>
    /// The music as the game plays out (<c>docs/design/music.md</c>), solo and in co-op alike (each phone follows its own
    /// screen): the title theme on the splash, the den's own theme in the den, the start sting into the mission's theme, cut at the boss's
    /// screen lock, the boss theme from its first shot, a victory sting into the pilot round's final section, the tabk
    /// alert for the coda, the continue countdown, mission complete into the debrief's loop, and the game over sting.
    /// When it changes mid-mission is <see cref="Soundtrack"/>'s rule.
    /// </summary>
    public sealed class MusicFlow
    {
        readonly Game m_Game;
        Soundtrack m_Soundtrack = new Soundtrack();
        /// <summary>What was playing when the continue countdown started, to come back to.</summary>
        MusicTrack m_Before;

        public MusicFlow(Game game) => m_Game = game;

        /// <summary>
        /// At launch, with the music on: the mission's music is rendered then, so the mission starts without a stutter.
        /// The rest (the debrief's loop and the other stings) renders as it's first played.
        /// </summary>
        public static void Prepare()
        {
            if (!Music.Muted) Music.Prepare(MusicTracks.SteelNight, MusicTracks.TankAlert, MusicTracks.TankAlertFinal, MusicTracks.MissionStart, MusicTracks.BossVictory);
        }

        /// <summary>
        /// Each frame: the title theme loops while the splash or the settings over it is up, and the den's theme while the
        /// den is (once its beat, if one's playing, is over), quieter and dipping under a menu over it
        /// (<see cref="MusicTracks.MenuRecording"/>). Each fades as it's left, and going between them, one fades out before
        /// the other starts (<c>docs/design/music.md</c>).
        /// </summary>
        public void Menus(bool splash, bool den, bool menuOpen, bool beat)
        {
            string want = MusicTracks.MenuRecording(splash, den, beat);
            if (den && want == null)
            {
                if (!Music.Fading) Music.Stop(fade: true); // the debrief's loop too: quiet till the den's beat is over
                return;
            }
            Music.Dip = den && menuOpen ? MusicTracks.MenuDip : 1f;
            string playing = Music.Recording;
            bool ours = playing == MusicTracks.TitleRecording || playing == MusicTracks.DenRecording;
            if (ours && playing != want) Music.Stop(fade: true);
            else if (want != null && !Music.Fading) Music.Loop(want, MusicTracks.FadeIn(want)); // over the debrief's loop too, back in the den
        }

        /// <summary>
        /// A mission starting (or restarting, or joined in co-op): the start sting, then the mission's theme. Only Mission
        /// one has a theme yet, so every mission plays it.
        /// </summary>
        public void MissionStart()
        {
            m_Soundtrack = new Soundtrack();
            m_Before = null;
            Music.Silence();
            Music.Sting(MusicTracks.MissionStart);
            Music.Play(MusicTracks.SteelNight, MusicTracks.MissionStart.Seconds);
        }

        /// <summary>The screen locks for the boss and the mission's music cuts out (<c>docs/design/tabks/bosses.md</c>).</summary>
        public void BossIntro() => Music.Stop();

        /// <summary>The tank's first shot: the boss music kicks in.</summary>
        public void BossFightStarted()
        {
            if (Music.Playing != MusicTracks.TankAlertFinal) Music.Play(MusicTracks.TankAlert);
        }

        /// <summary>
        /// The tank's wrecked: a victory sting, then the boss theme's even faster final section for the pilot round,
        /// which crossfades from the chip to an orchestra playing the same notes.
        /// </summary>
        public void TankWrecked()
        {
            Music.Sting(MusicTracks.BossVictory);
            // after one pass in chip, the orchestra takes over (in builds that have it: the vendor repository's CI makes it)
            Music.Play(MusicTracks.TankAlertFinal, MusicTracks.BossVictory.Seconds, Music.Layer("tank-alert-final"), MusicTracks.TankAlertFinal.Seconds);
        }

        /// <summary>On into the coda: the run out of the base with tones2's tabk.</summary>
        public void Coda() => Music.Play(MusicTracks.TankAlert);

        /// <summary>Each frame of a mission: the continue countdown and back, the pilot's glitch-out, the win.</summary>
        public void Tick(World world)
        {
            LifeBank lives = world.Player.Lives;
            switch (m_Soundtrack.Frame(lives.Offering, lives.IsOut, world.BossMusicOff, world.Victory))
            {
                case MusicCue.Countdown:
                    m_Before = Music.Playing;
                    Music.Silence();
                    Music.Sting(MusicTracks.ContinueCountdown);
                    break;
                case MusicCue.Resume:
                    Music.Silence();
                    if (m_Before != null) Music.Play(m_Before);
                    m_Before = null;
                    break;
                case MusicCue.Cut:
                    Music.Stop();
                    break;
                case MusicCue.Complete:
                    Music.Stop(fade: true);
                    Music.Sting(MusicTracks.MissionComplete);
                    break;
            }
        }

        /// <summary>The debrief opens: its loop, once the mission complete sting has finished.</summary>
        public void Debrief(float sinceWon) =>
            Music.Play(MusicTracks.Debrief, System.Math.Max(0f, MusicTracks.MissionComplete.Seconds - sinceWon));

        /// <summary>Out of lives and continues: the music stops for the game over sting.</summary>
        public void GameOver()
        {
            Music.Silence();
            Music.Sting(MusicTracks.GameOver);
        }

        /// <summary>The debrief's rank stamp lands: the rank up fanfare, over the debrief's loop.</summary>
        public static void RankUp() => Music.Sting(MusicTracks.RankUp);
    }
}
