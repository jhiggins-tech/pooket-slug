namespace PooketSlug
{
    /// <summary>
    /// The boss intro under way (<c>docs/design/tanks.md</c>), over the mission: seconds since it began, whether it's the
    /// full one, the name card, and the siren and the announcer's lines as they come due (<see cref="BossIntroShow"/>).
    /// </summary>
    public sealed class BossIntroFlow
    {
        readonly BossIntroShow.Cue[] m_Cues = new BossIntroShow.Cue[4];
        float m_T;
        bool m_Full;

        /// <summary>The boss's name for its card, or null with no intro under way.</summary>
        public string Name { get; private set; }

        /// <summary>Where the intro's got to, for the HUD.</summary>
        public BossIntroShow Show => new BossIntroShow(m_T, m_Full);

        public void Start(BossEnemy boss, bool firstTime)
        {
            m_T = -0.001f; // just before 0, so the first cues fire on the next tick
            m_Full = firstTime;
            Name = boss.DisplayName;
        }

        /// <summary>No intro: a new world's starting.</summary>
        public void Clear() => Name = null;

        /// <summary>Moves the intro on, and plays the siren and the announcer's lines as they come due.</summary>
        public void Tick(float dt)
        {
            if (Name == null) return;
            float from = m_T;
            m_T += dt;
            int n = BossIntroShow.Cues(m_Full, from, m_T, m_Cues);
            for (int i = 0; i < n; i++)
            {
                switch (m_Cues[i])
                {
                    case BossIntroShow.Cue.Siren: Sfx.Play(Sfx.Siren, 0.7f); break;
                    case BossIntroShow.Cue.SayWarning: Sfx.Say("warning"); break;
                    case BossIntroShow.Cue.SayBoss: Sfx.Say("boss-tank"); break;
                }
            }
            if (Show.Over) Name = null;
        }
    }
}
