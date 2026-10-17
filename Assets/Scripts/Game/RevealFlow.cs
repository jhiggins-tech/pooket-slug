using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Two of the crew's flow (<see cref="CrewReveal"/>): played once, ever, over the title the first time it loads after
    /// Mission one's beaten solo. kie walks on, START splits and the menu grows; kie stays by tones2 until the title's left.
    /// </summary>
    public sealed class RevealFlow
    {
        readonly Game m_Game;
        readonly CrewRevealView m_View;
        bool m_Test; // ?test=crew, until two of the crew has played (once a visit)

        /// <summary>The reveal playing, or null.</summary>
        public CrewReveal Now { get; private set; }

        public RevealFlow(Game game, Transform parent, bool test)
        {
            m_Game = game;
            m_View = new CrewRevealView(parent);
            m_Test = test;
        }

        /// <summary>Two of the crew plays once, ever, the first time the title loads after Mission one's beaten solo.</summary>
        public bool Due => !m_Game.Coop && !m_View.Showing &&
                           (m_Test || m_Game.Profile.coop && !m_Game.Profile.HasSeen(Reveals.TwoOfTheCrew));

        /// <summary>kie leaves with the title screen.</summary>
        public void End() => m_View.End();

        public void Begin()
        {
            Now = new CrewReveal();
            m_View.Begin(m_Game.World);
        }

        public void Tick(float dt, MenuInput keys, SoldierInput soldier)
        {
            CrewReveal reveal = Now;
            if (m_View.TakeTap() || keys.Start || soldier.FirePressed || soldier.JumpPressed) reveal.Tap();
            CrewReveal.Shot was = reveal.Now;
            bool wasCracked = !reveal.OldStart;
            if (reveal.Tick(dt)) Sfx.Play(Sfx.Thump, 0.9f); // each new button's clunk
            if (!wasCracked && !reveal.OldStart) Sfx.Play(Sfx.Hit, 0.8f); // START splits
            if (reveal.Now != was && reveal.Now == CrewReveal.Shot.KiesLine) Sfx.Play(Sfx.Pickup, 0.5f);
            if (!reveal.Done)
            {
                m_View.Refresh(reveal, m_Game.Camera, dt);
                return;
            }
            m_View.Finish(); // kie stays, stood by tones2, until the title's left
            Now = null;
            m_Test = false; // once a visit
            Profile profile = m_Game.Profile;
            if (profile.MarkSeen(Reveals.TwoOfTheCrew)) m_Game.Store.Save(profile);
            Log.Game.Info("Two of the crew: the menu's grown");
        }
    }
}
