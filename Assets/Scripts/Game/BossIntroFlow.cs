namespace PooketSlug
{
    /// <summary>
    /// The boss intro under way (<c>docs/design/cutscenes/mission-one.md</c>, The boss intro), over the mission: seconds since it began, whether it's the
    /// full one, the name card, and the siren and the announcer's lines as they come due (<see cref="BossIntroShow"/>);
    /// and, where the mission's story has one and its reveal hasn't just played, its pilot's part: the Trollogram up in the boss tabk's hatch, their intro
    /// band and their line.
    /// </summary>
    public sealed class BossIntroFlow
    {
        readonly BossIntroShow.Cue[] m_Cues = new BossIntroShow.Cue[6];
        float m_T;
        bool m_Full;
        TankBoss m_Tank;

        /// <summary>The boss's name for its card, or null with no intro under way.</summary>
        public string Name { get; private set; }

        /// <summary>The pilot's name for their intro band, and their line, or null with no pilot's part.</summary>
        public string PilotName { get; private set; }
        public string PilotLine { get; private set; }

        /// <summary>Where the intro's got to, for the HUD.</summary>
        public BossIntroShow Show => new BossIntroShow(m_T, m_Full, PilotName != null);

        /// <summary>The pilot's part is on (solo, the game holds still for it).</summary>
        public bool Holding => Name != null && Show.Holding;

        /// <param name="revealed">The mission's reveal just played: its pilot was met there, so no pilot's part.</param>
        public void Start(BossEnemy boss, bool firstTime, MissionStory story = null, bool revealed = false)
        {
            m_T = -0.001f; // just before 0, so the first cues fire on the next tick
            m_Full = firstTime;
            Name = boss.DisplayName;
            m_Tank = boss as TankBoss;
            bool pilot = firstTime && !revealed && m_Tank != null && story?.PilotIntro != null;
            PilotName = pilot ? story.PilotIntro : null;
            PilotLine = pilot ? story.PilotIntroLine : null;
        }

        /// <summary>No intro: a new world's starting.</summary>
        public void Clear()
        {
            Name = PilotName = PilotLine = null;
            m_Tank = null;
        }

        /// <summary>Moves the intro on, and plays the siren and the announcer's lines as they come due.</summary>
        public void Tick(float dt)
        {
            if (Name == null) return;
            if (PilotName != null && (m_Tank == null || m_Tank.Dying || m_Tank.Removed))
            {
                // the tabk's wrecked before its pilot looked out (?test=pilot): no hatch to look out of
                PilotName = PilotLine = null;
                if (m_T >= BossIntroShow.NameUntil) { Clear(); return; }
            }
            float from = m_T;
            m_T += dt;
            int n = BossIntroShow.Cues(m_Full, PilotName != null, from, m_T, m_Cues);
            for (int i = 0; i < n; i++)
            {
                switch (m_Cues[i])
                {
                    case BossIntroShow.Cue.Siren: Sfx.Play(Sfx.Siren, 0.7f); break;
                    case BossIntroShow.Cue.SayWarning: Sfx.Say("warning"); break;
                    case BossIntroShow.Cue.SayBoss: Sfx.Say("boss-tank"); break;
                    case BossIntroShow.Cue.Hatch: Sfx.Play(Sfx.Thump, 0.5f); break;
                    case BossIntroShow.Cue.Riser: Sfx.Play(Sfx.IntroRise, 0.5f); break;
                    case BossIntroShow.Cue.Hit: Sfx.Play(Sfx.Intro, 0.5f); break;
                }
            }
            if (m_Tank != null && PilotName != null) m_Tank.HatchPilot = Show.Hatch;
            if (Show.Over) Clear();
        }
    }
}
