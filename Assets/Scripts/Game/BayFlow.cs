using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The tabk bay's flow (<c>docs/design/lobby.md</c>, The tabk bay), through the den's roller door: walking your
    /// soldier round it, a tabk's card as you walk up to it, TAKE IT sending the cargo drone to swap it onto the repair
    /// stands (a tap again skips to the end; you can walk off meanwhile), the drone's idle shuffles, and the door back to
    /// the den. The pick is kept with the settings (<see cref="Options.Tabk"/>). In co-op each player picks in their own
    /// bay: nothing here crosses the network. The first visit with kie's tabk won, it comes home first
    /// (<see cref="KieHomecoming"/>): it's on the stands for that, and the drone swaps the loaded one back after.
    /// </summary>
    public sealed class BayFlow
    {
        readonly Game m_Game;
        readonly BayView m_View;
        readonly System.Random m_Rng = new System.Random(); // the drone's idle is cosmetic
        Garage m_Garage;
        BayWalker m_Walker;
        DroneShow m_Show;
        bool m_Swapping;
        TabkCard m_Card;
        float m_Idle, m_Whirr, m_Tick, m_LastHose = 1f;
        TabkCard m_LastCarried;
        int m_HeldDir;
        KieHomecoming m_Home;

        public BayFlow(Game game, Transform parent)
        {
            m_Game = game;
            m_View = new BayView(parent);
        }

        public bool Open { get; private set; }

        /// <summary>Opens the bay, <paramref name="soldier"/> (in <paramref name="skin"/>) walking in from the den's roller door.</summary>
        public void Show(SoldierDef soldier, int skin)
        {
            var won = m_Game.TabksWon;
            TabkCard loaded = m_Game.LoadedTabk;
            // the tabks stay where the drone left them while the game's open, unless one's been won or loaded since
            m_Home = KieHomecoming.Due(m_Game.Profile, won, m_Game.Flags.Testing) ? new KieHomecoming() : null;
            if (m_Home != null) m_Garage = new Garage(won, TabkCards.Kie); // the stands are kie's while his tabk comes home
            else if (m_Garage == null || !m_Garage.Holds(won) || m_Garage.Loaded != loaded) m_Garage = new Garage(won, loaded);
            m_Show = null;
            m_Swapping = false;
            m_Card = null;
            m_Idle = 8f + (float)m_Rng.NextDouble() * 10f;
            m_LastHose = 1f;
            m_LastCarried = null;
            m_HeldDir = 0;
            m_Walker = new BayWalker(BayDeck.Floor, BayMap.XOf(BayPlace.Door) + 14f);
            m_View.Begin(m_Garage, soldier, skin);
            Open = true;
            Sfx.Play(Sfx.Thump, 0.4f);
            Log.Game.Info("Tabk bay open: " + m_Garage.Won.Count + " tabks, loaded: " + loaded.Name);
            if (m_Home != null)
            {
                m_View.PlayHomecoming(m_Home);
                Log.Game.Info("Tabk bay: kie's tabk comes home");
            }
        }

        /// <summary>The homecoming moves on, with its sounds; done (or skipped), it's kept as played and the drone puts the loaded tabk back on the stands.</summary>
        void Homecoming(float dt)
        {
            KieHomecoming b = m_Home;
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
            if (!b.Done) return;
            m_Home = null;
            m_View.EndHomecoming();
            Game g = m_Game;
            if (g.Profile.MarkSeen(KieHomecoming.Reveal)) g.Store.Save(g.Profile);
            Log.Game.Info("Tabk bay: kie's tabk is home");
            m_Show = m_Garage.Swap(g.LoadedTabk, DroneShow.Home); // the drone puts yours back on the stands
            m_Swapping = m_Show != null;
            m_LastHose = 1f;
            m_LastCarried = null;
        }

        /// <summary>Back through the roller door to the den. A swap still playing ends as it would.</summary>
        void Leave()
        {
            m_Show?.Skip();
            m_Show = null;
            Open = false;
            m_View.Hide();
            Log.Game.Info("Tabk bay left");
        }

        /// <summary>The bay's buttons and keys: taps walk, a tabk's card (or jump by one), TAKE IT (Enter, fire), BACK (Esc), the lift (up, down).</summary>
        public void Tick(MenuInput keys, SoldierInput soldier)
        {
            if (m_Home != null)
            {
                // a tap, a key or a button skips the homecoming, and does nothing else
                bool any = m_View.TakeBack() | m_View.TakeClose() | m_View.TakeTakeIt() | m_View.TakeSpot() != null
                    | m_View.TakePlace() != BayPlace.None | m_View.TakeWalk(out _, out _);
                any |= keys.Back || keys.Start || soldier.FirePressed || soldier.JumpPressed;
                if (any && m_Home.T > 0.3f) m_Home.Skip(); // not the step through the door
                return;
            }
            if (m_View.TakeBack() || keys.Back)
            {
                if (m_Card != null) m_Card = null;
                else Leave();
                return;
            }
            if (m_View.TakeClose()) m_Card = null;
            bool take = m_View.TakeTakeIt() || m_Card != null && (keys.Start || soldier.FirePressed);
            if (take && m_Card != null) Take(m_Card);

            BayWalker w = m_Walker;
            int? spot = m_View.TakeSpot();
            BayPlace place = m_View.TakePlace();
            bool walk = m_View.TakeWalk(out BayDeck deck, out float x);
            int dir = soldier.Left == soldier.Right ? 0 : soldier.Left ? -1 : 1;
            int ride = soldier.Up == soldier.Down ? 0 : soldier.Up ? 1 : -1;
            if (soldier.JumpPressed && w.Standing) spot = Near(w);
            if (spot != null || place != BayPlace.None || walk || dir != 0 || ride != 0) m_Card = null;
            if (spot != null) w.Visit(spot.Value);
            else if (place != BayPlace.None) w.Go(place);
            else if (walk) w.WalkTo(deck, x);
            else if (ride != 0 && !w.Riding && (ride > 0) == (w.Deck == BayDeck.Floor)) w.Ride(); // up or down: off to the lift
            else if (dir != 0) w.Walk(dir);
            else if (m_HeldDir != 0) w.Walk(0);
            m_HeldDir = spot != null || place != BayPlace.None || walk ? 0 : dir;
        }

        /// <summary>The spot of the tabk you're standing by, on your deck, or null.</summary>
        int? Near(BayWalker w)
        {
            for (int s = BayMap.Stands; s < BayMap.Spaces; s++)
                if (m_Garage.At(s) != null && BayMap.DeckOf(s) == w.Deck && Mathf.Abs(BayMap.SpotX(s) - w.X) < BayMap.TabkW / 2f + 24f) return s;
            return null;
        }

        /// <summary>TAKE IT: the drone swaps <paramref name="card"/> onto the stands. Pressed again while it does, it skips to the end.</summary>
        void Take(TabkCard card)
        {
            if (m_Swapping && m_Show != null && !m_Show.Landed)
            {
                m_Show.Skip();
                Log.Game.Info("Tabk swap skipped");
                return;
            }
            if (m_Garage.Loaded == card) return;
            m_Show?.Skip(); // an idle shuffle finishes first
            m_Show = m_Garage.Swap(card, DroneShow.Home);
            if (m_Show == null) return;
            m_Swapping = true;
            m_LastHose = 1f;
            m_LastCarried = null;
            Options o = m_Game.Options;
            o.Tabk = card.Id;
            m_Game.ChangeOptions(o);
            Sfx.Play(Sfx.Pickup, 0.6f);
            Log.Game.Info("Tabk loaded: " + card.Name + " (the drone swaps it in: " + m_Show.Length.ToString("0.0") + " s)");
        }

        /// <summary>Moves the bay on each frame: you, the drone and its sounds, the idle shuffles, and the screen.</summary>
        public void Move(float dt)
        {
            if (!Open) return;
            if (m_Home != null) Homecoming(dt);
            BayErrand done = m_Walker.Step(dt);
            if (done.Spot != null)
            {
                m_Card = m_Garage.At(done.Spot.Value);
                if (m_Card != null)
                {
                    Log.Game.Info("Tabk bay: looking over " + m_Card.Name);
                    m_Walker.Face(false);
                    Sfx.Play(Sfx.Pickup, 0.3f);
                }
            }
            else if (done.Place == BayPlace.Door)
            {
                Leave();
                return;
            }
            else if (done.Place != BayPlace.None) m_View.Note(BayMap.Note(done.Place));

            if (m_Show != null)
            {
                m_Show.Tick(dt);
                Sounds(dt);
                if (m_Show.Done)
                {
                    m_Show = null;
                    m_Swapping = false;
                }
            }
            else if (m_Home == null && (m_Idle -= dt) <= 0f)
            {
                // the drone's idle: a parked tabk moved to another free space on its deck
                m_Show = m_Garage.Shuffle(m_Rng.Next, DroneShow.Home);
                m_LastCarried = null;
                m_Idle = 18f + (float)m_Rng.NextDouble() * 20f;
            }
            if (m_Show == null && m_View.StandsInView && (m_Tick -= dt) <= 0f)
            {
                m_Tick = 0.3f;
                Sfx.Play(Sfx.EngineTick, 0.25f); // the loaded tabk ticking over
            }
            m_View.Refresh(dt, m_Walker, m_Show, m_Card, m_Swapping && m_Show != null && !m_Show.Landed, GameInput.TouchActive);
        }

        /// <summary>The drone's rotors while it flies, its cable's clunk as it takes or sets down a tabk, the hose's hiss.</summary>
        void Sounds(float dt)
        {
            if (!m_Show.Done && (m_Whirr -= dt) <= 0f)
            {
                m_Whirr = 0.22f;
                Sfx.Play(Sfx.DroneWhirr, 0.5f);
            }
            TabkCard carried = m_Show.Carried;
            if (carried != m_LastCarried) Sfx.Play(Sfx.CableClunk, 0.8f);
            m_LastCarried = carried;
            float hose = m_Show.Hose;
            if (m_LastHose >= 1f && hose < 1f || m_LastHose <= 0f && hose > 0f) Sfx.Play(Sfx.HoseHiss, 0.8f);
            m_LastHose = hose;
        }
    }
}
