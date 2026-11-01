using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// What plays in the tabk bay as it opens, before you can walk it (<c>docs/design/cutscenes/den.md</c>, the tabks coming
    /// home; <c>skits.md</c>, Does it do cones?): kie's tabk coming home (<see cref="KieHomecoming"/>), or kcaj's
    /// (<see cref="KcajHomecoming"/>) with Does it do cones? straight on from it (<see cref="ConesSkit"/>, which also plays
    /// on its own on a later solo visit if it was missed). One homecoming a visit, kie's first. The stands are the
    /// homecoming tabk's while it plays (<see cref="Due"/> says whose); when it's all done the bay's flow has the drone
    /// put the loaded tabk back. kcaj's beat and the skit play on a <see cref="SkitRun"/>, so a tap skips each on its own
    /// and each is kept once it's done; kie's is timed and kept here. <c>?skit=cones</c> plays kcaj's beat and the skit the
    /// first time the bay opens on your own, seen or not (with kcaj's tabk won: <c>?unlock=crew</c>).
    /// </summary>
    sealed class BayStory
    {
        readonly Game m_Game;
        readonly BayView m_View;
        readonly SkitRun m_Run;
        KieHomecoming m_Kie;
        KcajHomecoming m_Kcaj;
        Skit m_Next;
        bool m_Asked, m_Pressed;
        float m_Whirr;

        public BayStory(Game game, BayView view)
        {
            m_Game = game;
            m_View = view;
            m_Run = new SkitRun(game);
        }

        /// <summary>Something's playing: the bay waits for it.</summary>
        public bool Playing => m_Kie != null || m_Run.Playing;

        /// <summary>
        /// Works out what plays as the bay opens on a visit in <paramref name="mode"/> with <paramref name="won"/>: the tabk
        /// whose homecoming (or skit) it is, which stands on the stands for it, or null for nothing.
        /// </summary>
        public TabkCard Due(DenMode mode, IReadOnlyList<TabkCard> won)
        {
            Stop();
            Game g = m_Game;
            Profile p = g.Profile;
            bool testing = g.Flags.Testing;
            // ?skit= naming the bay's skit, the first time the bay opens on your own: kcaj's beat, then the skit
            Skit named = mode == DenMode.Solo && !m_Asked && g.Flags.Skit != null ? Skits.Named(g.Flags.Skit) : null;
            string asked = named != null && named.Place == SkitPlace.Bay ? g.Flags.Skit : null;
            m_Asked |= asked != null;
            bool kcajWon = TabkCards.Has(won, TabkCards.Kcaj);
            if (asked != null && !kcajWon)
            {
                Log.Game.Warn("?skit=" + asked + ": kcaj's tabk isn't won here (try ?unlock=crew)");
                return null;
            }
            if (asked != null) m_Kcaj = new KcajHomecoming();
            else if (KieHomecoming.Due(p, won, testing)) m_Kie = new KieHomecoming();
            else if (KcajHomecoming.Due(p, won, mode, testing)) m_Kcaj = new KcajHomecoming();
            if (m_Kie != null) return TabkCards.Kie;
            m_Next = Skits.Due(p, new SkitVisit
            {
                Place = SkitPlace.Bay,
                Mode = mode,
                Crew = g.Freed,
                Testing = testing,
                Asked = asked,
                Tabks = won,
                Homecoming = m_Kcaj != null,
            });
            return m_Kcaj != null || m_Next != null ? TabkCards.Kcaj : null;
        }

        /// <summary>Starts what <see cref="Due"/> found, once the bay's set out.</summary>
        public void Start()
        {
            if (m_Kie != null)
            {
                m_View.PlayHomecoming(m_Kie);
                Log.Game.Info("Tabk bay: kie's tabk comes home");
            }
            else if (m_Kcaj != null)
            {
                Play(m_Kcaj);
                Log.Game.Info("Tabk bay: kcaj's tabk comes home");
            }
            else if (m_Next != null)
            {
                Play(m_Next);
                m_Next = null;
            }
        }

        void Play(Skit skit)
        {
            m_Run.Start(skit);
            m_View.PlaySkit(skit);
            m_Pressed = false;
            m_Whirr = 0f;
        }

        /// <summary>A tap, a key or a button: it skips what's playing (not in its first moment: the step through the door).</summary>
        public void Press()
        {
            if (m_Kie != null)
            {
                if (m_Kie.T > SkitRun.Settle) m_Kie.Skip();
            }
            else m_Pressed = true;
        }

        /// <summary>The bay closed under it: nothing's kept, so it plays next time.</summary>
        public void Stop()
        {
            m_Run.Stop();
            m_Kie = null;
            m_Kcaj = null;
            m_Next = null;
        }

        /// <summary>Moves it on, with its sounds. True the frame the last of it ends (the drone puts the loaded tabk back then).</summary>
        public bool Move(float dt)
        {
            if (m_Kie != null) return KieMoves(dt);
            Skit skit = m_Run.Skit;
            if (skit == null) return false;
            Skit ended = m_Run.Tick(dt, m_Pressed);
            m_Pressed = false;
            if (skit is KcajHomecoming home) KcajSounds(home, dt);
            else if (skit is ConesSkit cones) ConesSounds(cones, dt);
            if (ended == null) return false;
            if (ended is KcajHomecoming)
            {
                m_Kcaj = null;
                Log.Game.Info("Tabk bay: kcaj's tabk is home");
                if (m_Next != null)
                {
                    // straight on: Does it do cones?, with SOFT SERVE still on the stands
                    Skit next = m_Next;
                    m_Next = null;
                    Play(next);
                    return false;
                }
            }
            m_View.EndSkit();
            return true;
        }

        /// <summary>kie's homecoming moves on, with its sounds; done (or skipped), it's kept as played.</summary>
        bool KieMoves(float dt)
        {
            KieHomecoming b = m_Kie;
            b.Tick(dt);
            if (b.Sparking && (m_Whirr -= dt) <= 0f)
            {
                m_Whirr = 0.25f;
                Sfx.Play(Sfx.Sizzle, 0.35f); // a chip welding crackle, on and off
            }
            if (b.Passed(KieHomecoming.SpannerTaken, dt)) Sfx.Play(Sfx.CableClunk, 0.3f);
            if (b.Passed(KieHomecoming.Shot2, dt)) Sfx.Play(Sfx.Servo, 0.6f); // the crane
            if (b.Passed(KieHomecoming.LowerTo, dt)) Sfx.Play(Sfx.Thump, 0.6f);
            if (b.Passed(KieHomecoming.ReelsFrom, dt)) Sfx.Play(Sfx.DroneWhirr, 0.7f); // the reels whirr up
            if (b.Passed(KieHomecoming.ReelsTo, dt)) Sfx.Play(Sfx.CableClunk, 0.8f); // then a chip clunk
            if (b.Passed(KieHomecoming.HoloFrom, dt)) Sfx.Play(Sfx.Glitch, 0.35f);
            if (!b.Done) return false;
            m_Kie = null;
            m_View.EndHomecoming();
            Game g = m_Game;
            if (g.Profile.MarkSeen(KieHomecoming.Reveal)) g.Store.Save(g.Profile);
            Log.Game.Info("Tabk bay: kie's tabk is home");
            return true;
        }

        void KcajSounds(KcajHomecoming b, float dt)
        {
            if (b.Flying && (m_Whirr -= dt) <= 0f)
            {
                m_Whirr = 0.2f;
                Sfx.Play(Sfx.DroneWhirr, b.Carrying ? 0.6f : 0.4f); // the drone's whine, straining under the wreck
            }
            if (b.Passed(KcajHomecoming.LandAt, dt)) Sfx.Play(Sfx.Thump, 0.9f); // a heavy thump as the wreck sets down
            if (b.Passed(KcajHomecoming.HomeFrom, dt)) Sfx.Play(Sfx.CableClunk, 0.5f);
            if (b.Passed(KcajHomecoming.HoseFrom, dt)) Sfx.Play(Sfx.HoseHiss, 0.5f);
            if (b.Passed(KcajHomecoming.Shot2, dt)) Sfx.Play(Sfx.Servo, 0.6f); // the crane
            if (b.Passed(KcajHomecoming.LowerTo, dt)) Sfx.Play(Sfx.Thump, 0.6f);
            if (b.Passed(KcajHomecoming.NoteAt, dt)) Sfx.Play(Sfx.Pickup, 0.45f); // the note
            if (b.Passed(KcajHomecoming.LeverAt, dt)) Sfx.Play(Sfx.CableClunk, 0.35f); // the lever
            if (b.Passed(KcajHomecoming.PaintFrom, dt)) Sfx.Play(Sfx.ScoopDispense, 0.7f); // the machine's whirr
            if (b.Passed(KcajHomecoming.ClunkAt, dt)) Sfx.Play(Sfx.CableClunk, 0.8f); // a chip clunk
        }

        void ConesSounds(ConesSkit s, float dt)
        {
            if (s.Passed(ConesSkit.DoorAt, dt)) Sfx.Play(Sfx.Servo, 0.5f); // the roller door
            if (s.KieRunning && (m_Whirr -= dt) <= 0f)
            {
                m_Whirr = 0.13f;
                Sfx.Play(Sfx.Thump, 0.15f); // running feet
            }
            if (s.Passed(ConesSkit.SkidFrom, dt)) Sfx.Play(Sfx.HoseHiss, 0.45f); // a skid
            if (s.Passed(ConesSkit.SwingFrom, dt)) Sfx.Play(Sfx.Servo, 0.35f); // the nozzle swung round
            if (s.Passed(ConesSkit.WobbleFrom, dt)) Sfx.Play(Sfx.ScoopDispense, 0.7f); // the machine's whirr
            if (s.Passed(ConesSkit.FirstAt, dt)) Sfx.Play(Sfx.ConeSplat, 0.7f); // two splats
            if (s.Passed(ConesSkit.SecondAt, dt)) Sfx.Play(Sfx.ConeSplat, 0.7f);
        }
    }
}
