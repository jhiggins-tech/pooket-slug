using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// A crewmate's tabk as the bay's card shows it (<c>docs/design/lobby.md</c>, The tabk bay): whose it is, its name, its
    /// paint (over the tabk's two body colours) and its three guns with their rounds, and the parking space it lives in.
    /// Its id is its crewmate's soldier id, which is what the settings keep (<see cref="Options.Tabk"/>).
    /// </summary>
    public sealed class TabkCard
    {
        public readonly SoldierDef Owner;
        public readonly string Name;
        /// <summary>Its body colours, light and dark (the tabk's <c>t</c> and <c>T</c>).</summary>
        public readonly uint Paint, Shade;
        public readonly IReadOnlyList<(string name, int rounds)> Guns;
        /// <summary>Its parking space (<see cref="BayMap"/>: 0 to 5 the floor's, 6 to 11 the top deck's).</summary>
        public readonly int Home;

        public TabkCard(SoldierDef owner, string name, uint paint, uint shade, int home, params (string, int)[] guns)
        {
            Owner = owner;
            Name = name;
            Paint = paint;
            Shade = shade;
            Home = home;
            Guns = guns;
        }

        public int Id => Owner.Id;
        public override string ToString() => Name;

        /// <summary>
        /// The HUD's line while it's driven: its name over its guns' rounds left (<c>KIE'S TABK  WEASEL 5   TROLLOGRAM 3
        /// STEAL 1</c>). Loot kie stole, while it lasts, takes the first gun's place: it's what FIRE shoots.
        /// </summary>
        public string HudLine(int main, int specials, int supers, string loot = null, int lootAmmo = 0)
        {
            string first = loot != null && lootAmmo > 0 ? loot + " " + lootAmmo : Guns[0].name + " " + main;
            return Name + "  " + first + "   " + Guns[1].name + " " + specials + "   " + Guns[2].name + " " + supers;
        }
    }

    /// <summary>
    /// The crew's tabks and who has won which (<c>docs/design/tanks.md</c>): tones2's from Mission one's coda, kie's
    /// rebuilt straight after Mission one (owner, 2026-10-20: it parks downstairs, and comes home with a beat in the bay,
    /// <see cref="KieHomecoming"/>), and kcaj's once Mission two is beaten (on the top deck). All three are drivable; a tabk
    /// that's won but not built yet drops tones2's in its place (the owner, 19 Oct; <see cref="Built"/>).
    /// </summary>
    public static class TabkCards
    {
        static readonly int Rounds5 = Content.PlayerTank.MainShots, Rounds3 = Content.PlayerTank.Specials, Rounds1 = Content.PlayerTank.Supers;

        public static readonly TabkCard Tones2 = new TabkCard(Content.Tones2, "TONES2'S TABK", 0xff8c1a, 0xb05a10, 0,
            ("WATER", Rounds5), ("MUD", Rounds3), ("SPEW", Rounds1));
        // kie's is THE PROJECTIONIST, in his blue with a green stripe
        public static readonly TabkCard Kie = new TabkCard(Content.Kie, "KIE'S TABK", 0x4ea8ff, 0x2a70c0, 1,
            ("WEASEL", Rounds5), ("TROLLOGRAM", Rounds3), ("STEAL", Rounds1));
        // kcaj's is SOFT SERVE, in his purple with a gold stripe
        public static readonly TabkCard Kcaj = new TabkCard(Content.Kcaj, "KCAJ'S TABK", 0xa060e0, 0x6a3aa0, BayMap.PerDeck,
            ("CONES", Rounds5), ("FIXATE", Rounds3), ("PILLS", Rounds1));

        public static readonly IReadOnlyList<TabkCard> All = new[] { Tones2, Kie, Kcaj };

        /// <summary>Whether <paramref name="card"/>'s tabk is built, so a mission drops it when it's loaded (else tones2's).</summary>
        public static bool Built(TabkCard card) => card == Tones2 || card == Kie || card == Kcaj;

        /// <summary>The tabk a mission drops for <paramref name="loaded"/>: it, if it's built, else tones2's.</summary>
        public static TabkCard Dropped(TabkCard loaded) => loaded != null && Built(loaded) ? loaded : Tones2;

        /// <summary>The tabk with id <paramref name="id"/> (its crewmate's soldier id), or null.</summary>
        public static TabkCard ById(int id)
        {
            foreach (TabkCard c in All)
                if (c.Id == id) return c;
            return null;
        }

        /// <summary>The tabks <paramref name="profile"/> has won, in crew order; <paramref name="everything"/> gives them all (for testing).</summary>
        public static IReadOnlyList<TabkCard> Won(Profile profile, bool everything = false)
        {
            var won = new List<TabkCard> { Tones2 };
            if (everything || MissionBoard.Cleared(profile, Missions.One)) won.Add(Kie);
            if (everything || MissionBoard.Cleared(profile, Missions.Two)) won.Add(Kcaj);
            return won;
        }

        /// <summary>The tabk that's loaded: the one picked, if it's been won, else tones2's (0, never picked, reads as his).</summary>
        public static TabkCard Loaded(int id, IReadOnlyList<TabkCard> won)
        {
            foreach (TabkCard c in won)
                if (c.Id == id) return c;
            return Tones2;
        }
    }

    /// <summary>
    /// Where each won tabk is in the bay (<c>docs/design/lobby.md</c>, The tabk bay): the loaded one on the repair stands,
    /// the rest in their spaces, and the drone's moves that change that (<see cref="DroneShow"/>). A swap puts the old pick
    /// back in its space and the new one on the stands; with nothing to do, the drone now and then moves a parked tabk to a
    /// free space on its deck. The spaces are only for show, so they aren't saved: each bay starts with every tabk at home.
    /// </summary>
    public sealed class Garage
    {
        readonly List<TabkCard> m_Won;
        readonly Dictionary<int, int> m_Spot = new Dictionary<int, int>();

        public Garage(IReadOnlyList<TabkCard> won, TabkCard loaded)
        {
            m_Won = new List<TabkCard>(won);
            foreach (TabkCard c in m_Won) m_Spot[c.Id] = c == loaded ? BayMap.Stands : c.Home;
        }

        public IReadOnlyList<TabkCard> Won => m_Won;

        /// <summary>The tabk on the stands: the loaded one.</summary>
        public TabkCard Loaded => At(BayMap.Stands);

        public int SpotOf(TabkCard tabk) => m_Spot.TryGetValue(tabk.Id, out int s) ? s : tabk.Home;

        /// <summary>The tabk in <paramref name="spot"/>, or null for an empty one.</summary>
        public TabkCard At(int spot)
        {
            foreach (TabkCard c in m_Won)
                if (m_Spot[c.Id] == spot) return c;
            return null;
        }

        /// <summary>Whether this bay's tabks are the ones in <paramref name="won"/> (a tabk won since it was set out needs a new one).</summary>
        public bool Holds(IReadOnlyList<TabkCard> won)
        {
            if (won.Count != m_Won.Count) return false;
            for (int i = 0; i < won.Count; i++)
                if (won[i] != m_Won[i]) return false;
            return true;
        }

        /// <summary>
        /// Loads <paramref name="pick"/>: the drone takes the old one off the stands to its space (or the first free one on
        /// the floor, if its own's been taken) and brings the new one over. Null if it's already loaded or not won.
        /// </summary>
        public DroneShow Swap(TabkCard pick, DroneShow.Point drone)
        {
            TabkCard old = Loaded;
            if (pick == null || pick == old || !m_Spot.ContainsKey(pick.Id)) return null;
            int from = m_Spot[pick.Id];
            int park = old == null ? from : At(old.Home) == null ? old.Home : FirstFree(BayDeck.Floor);
            var moves = new List<DroneShow.Move>();
            if (old != null)
            {
                moves.Add(new DroneShow.Move(old, BayMap.Stands, park));
                m_Spot[old.Id] = park;
            }
            moves.Add(new DroneShow.Move(pick, from, BayMap.Stands));
            m_Spot[pick.Id] = BayMap.Stands;
            return new DroneShow(drone, moves, hose: true);
        }

        int FirstFree(BayDeck deck)
        {
            int first = deck == BayDeck.Top ? BayMap.PerDeck : 0;
            for (int s = first; s < first + BayMap.PerDeck; s++)
                if (At(s) == null) return s;
            return deck == BayDeck.Floor ? FirstFree(BayDeck.Top) : BayMap.Spaces - 1;
        }

        /// <summary>
        /// The drone's idle: a parked tabk moved to a free space on its deck, picked with <paramref name="roll"/> (cosmetic
        /// randomness, 0 up to but not including the count it's asked for). Null when there's nothing parked or nowhere to go.
        /// </summary>
        public DroneShow Shuffle(Func<int, int> roll, DroneShow.Point drone)
        {
            var parked = new List<TabkCard>();
            foreach (TabkCard c in m_Won)
                if (m_Spot[c.Id] != BayMap.Stands) parked.Add(c);
            if (parked.Count == 0) return null;
            TabkCard tabk = parked[roll(parked.Count)];
            int from = m_Spot[tabk.Id];
            var free = new List<int>();
            int first = BayMap.DeckOf(from) == BayDeck.Top ? BayMap.PerDeck : 0;
            for (int s = first; s < first + BayMap.PerDeck; s++)
                if (At(s) == null) free.Add(s);
            if (free.Count == 0) return null;
            int to = free[roll(free.Count)];
            m_Spot[tabk.Id] = to;
            return new DroneShow(drone, new[] { new DroneShow.Move(tabk, from, to) }, hose: false);
        }
    }

    /// <summary>
    /// The cargo drone moving tabks round the bay, worked out ahead as a timeline so the view only asks where everything is
    /// at a time (and a tap can skip to its end). For a swap: the fuel hose comes out, the drone flies to each tabk in turn,
    /// dips, takes it on its cable, carries it and sets it down, and the hose goes back in; then the drone flies home over
    /// the stands. It flies under the top deck on the floor and over it on the deck, changing level past the deck's left end.
    /// </summary>
    public sealed class DroneShow
    {
        /// <summary>A position in the bay's art pixels.</summary>
        public readonly struct Point
        {
            public readonly float X, Y;
            public Point(float x, float y)
            {
                X = x;
                Y = y;
            }
            public override string ToString() => "(" + X + ", " + Y + ")";
        }

        /// <summary>One tabk carried from one spot to another.</summary>
        public readonly struct Move
        {
            public readonly TabkCard Tabk;
            public readonly int From, To;
            public Move(TabkCard tabk, int from, int to)
            {
                Tabk = tabk;
                From = from;
                To = to;
            }
        }

        /// <summary>Where the drone waits: over the repair stands, under the crane.</summary>
        public static readonly Point Home = new Point(BayMap.StandX + 34f, 122f);
        /// <summary>Speeds, px a second: flying light and carrying.</summary>
        public const float FlySpeed = 150f, CarrySpeed = 110f;
        /// <summary>The fuel hose coming out or going back, a dip to take or set down a tabk, and how high a tabk's lifted to be carried.</summary>
        public const float HoseSeconds = 0.8f, DipSeconds = 0.45f, Lift = 6f;
        /// <summary>From the drone's middle down to the top of a tabk on its cable (the cable's short and fixed).</summary>
        public const float Hang = 16f;
        /// <summary>Where the drone changes level: just past the top deck's left end.</summary>
        public const float EdgeX = BayMap.DeckLeft - 40f;

        /// <summary>What the show does when, and the cable's load: (from, to, start, end, carried tabk or null).</summary>
        readonly List<(Point from, Point to, float t0, float t1, TabkCard load)> m_Legs = new List<(Point, Point, float, float, TabkCard)>();
        readonly List<(TabkCard tabk, int from, int to, float up, float down)> m_Moves = new List<(TabkCard, int, int, float, float)>();
        readonly float m_HoseOut, m_HoseIn;
        float m_T;

        public DroneShow(Point drone, IReadOnlyList<Move> moves, bool hose)
        {
            m_T = 0f;
            float t = hose ? HoseSeconds : 0f;
            m_HoseOut = t;
            Point at = drone;
            foreach (Move m in moves)
            {
                // to the tabk, flying light, then a dip to take it on the cable
                Point over = Over(m.From, true);
                at = FlyTo(at, over, ref t, null);
                Leg(at, Over(m.From, false), ref t, DipSeconds, null);
                float up = t;
                Leg(Over(m.From, false), over, ref t, DipSeconds, m.Tabk);
                // carried over, and a dip to set it down
                Point drop = Over(m.To, true);
                at = FlyTo(over, drop, ref t, m.Tabk);
                Leg(drop, Over(m.To, false), ref t, DipSeconds, m.Tabk);
                m_Moves.Add((m.Tabk, m.From, m.To, up, t));
                Leg(Over(m.To, false), drop, ref t, DipSeconds, null);
                at = drop;
            }
            m_HoseIn = t;
            if (hose) t += HoseSeconds;
            Length = t;
            FlyTo(at, Home, ref t, null);
            Total = t;
        }

        /// <summary>Seconds until the swap's done and the hose is back in; <see cref="Total"/> includes the drone's flight home.</summary>
        public readonly float Length;
        public readonly float Total;

        public float Time => m_T;
        public bool Done => m_T >= Total;
        /// <summary>The tabks are where they're going and the hose is in (the drone may still be flying home).</summary>
        public bool Landed => m_T >= Length;

        public void Tick(float dt) => m_T = Math.Min(Total, m_T + dt);

        /// <summary>A tap again: straight to the end, with the drone home.</summary>
        public void Skip() => m_T = Total;

        /// <summary>How far the hose is in the tabk on the stands, 0 (out, hanging on the wall) to 1 (in).</summary>
        public float Hose
        {
            get
            {
                if (m_HoseOut <= 0f) return 1f; // a shuffle never touches it
                if (m_T < m_HoseOut) return 1f - m_T / m_HoseOut;
                if (m_T < m_HoseIn) return 0f;
                return Math.Min(1f, (m_T - m_HoseIn) / HoseSeconds);
            }
        }

        /// <summary>The drone's middle now.</summary>
        public Point Drone
        {
            get
            {
                foreach (var l in m_Legs)
                    if (m_T <= l.t1) return Lerp(l.from, l.to, l.t1 > l.t0 ? Smooth((m_T - l.t0) / (l.t1 - l.t0)) : 1f);
                return m_Legs.Count > 0 ? m_Legs[m_Legs.Count - 1].to : Home;
            }
        }

        /// <summary>The tabk on the drone's cable now, or null.</summary>
        public TabkCard Carried
        {
            get
            {
                foreach (var m in m_Moves)
                    if (m_T > m.up && m_T < m.down) return m.tabk;
                return null;
            }
        }

        /// <summary>
        /// Where <paramref name="tabk"/> is now, if the show moves it (its middle and its feet): before it's taken, in its
        /// old spot; on the cable, under the drone; after, in its new one. False for a tabk the show doesn't touch.
        /// </summary>
        public bool Where(TabkCard tabk, out Point feet)
        {
            feet = default;
            bool any = false;
            foreach (var m in m_Moves)
            {
                if (m.tabk != tabk) continue;
                any = true;
                if (m_T <= m.up)
                {
                    feet = SpotFeet(m.from);
                    return true;
                }
                if (m_T < m.down)
                {
                    Point d = Drone;
                    feet = new Point(d.X, d.Y - Hang - BayMap.TabkH);
                    return true;
                }
                feet = SpotFeet(m.to);
            }
            return any;
        }

        /// <summary>The spot a moved tabk ends in, or null if this show doesn't move it.</summary>
        public int? EndSpot(TabkCard tabk)
        {
            int? end = null;
            foreach (var m in m_Moves)
                if (m.tabk == tabk) end = m.to;
            return end;
        }

        static Point SpotFeet(int spot) => new Point(BayMap.SpotX(spot), BayMap.SpotY(spot));

        /// <summary>The drone over a tabk in <paramref name="spot"/>: lifted clear to carry it, or dipped to its roof.</summary>
        static Point Over(int spot, bool lifted) =>
            new Point(BayMap.SpotX(spot), BayMap.SpotY(spot) + (lifted ? Lift : 0f) + BayMap.TabkH + Hang);

        void Leg(Point from, Point to, ref float t, float seconds, TabkCard load)
        {
            m_Legs.Add((from, to, t, t + seconds, load));
            t += seconds;
        }

        /// <summary>Flies from one point to another, changing level only past the top deck's left end. Returns where it got to.</summary>
        Point FlyTo(Point from, Point to, ref float t, TabkCard load)
        {
            float speed = load != null ? CarrySpeed : FlySpeed;
            Point at = from;
            if (Math.Abs(at.Y - to.Y) > 0.5f)
            {
                if (at.X > EdgeX) at = Go(at, new Point(EdgeX, at.Y), ref t, speed, load);
                at = Go(at, new Point(at.X, to.Y), ref t, speed, load);
            }
            return Go(at, to, ref t, speed, load);
        }

        Point Go(Point from, Point to, ref float t, float speed, TabkCard load)
        {
            float d = Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y);
            if (d < 0.5f) return to;
            Leg(from, to, ref t, d / speed, load);
            return to;
        }

        static float Smooth(float k)
        {
            k = Math.Max(0f, Math.Min(1f, k));
            return k * k * (3f - 2f * k);
        }

        static Point Lerp(Point a, Point b, float k) => new Point(a.X + (b.X - a.X) * k, a.Y + (b.Y - a.Y) * k);
    }
}
