namespace PooketSlug
{
    /// <summary>What the music does next, from a mission's moments (<see cref="Soundtrack.Frame"/>).</summary>
    public enum MusicCue
    {
        None,
        /// <summary>A continue's offered: the mission's music stops and the countdown ticks.</summary>
        Countdown,
        /// <summary>The continue's taken (or a partner's 1-up brought us back): the music that was playing comes back.</summary>
        Resume,
        /// <summary>The pilot's big glitch-out: the music cuts dead.</summary>
        Cut,
        /// <summary>The mission's won: the music fades under the mission complete sting.</summary>
        Complete,
    }

    /// <summary>
    /// When a mission's music changes as it plays (<c>docs/design/music.md</c>): the continue countdown and back, the
    /// boss's cut and the mission complete sting. Fed the mission's state each frame, it says what's new, once.
    /// </summary>
    public sealed class Soundtrack
    {
        bool m_Offering, m_Cut, m_Won;

        /// <param name="offering">A continue is offered to this phone's soldier.</param>
        /// <param name="out">This phone's soldier is out for good (the game over takes it from here).</param>
        /// <param name="cut">The boss's music is off (the pilot's glitch-out, until the coda opens).</param>
        /// <param name="won">The mission's won.</param>
        public MusicCue Frame(bool offering, bool @out, bool cut, bool won)
        {
            if (won)
            {
                if (m_Won) return MusicCue.None;
                m_Won = true;
                return MusicCue.Complete;
            }
            if (offering != m_Offering)
            {
                m_Offering = offering;
                if (offering) return MusicCue.Countdown;
                if (!@out) return MusicCue.Resume;
            }
            if (cut != m_Cut)
            {
                m_Cut = cut;
                if (cut) return MusicCue.Cut;
            }
            return MusicCue.None;
        }
    }
}
