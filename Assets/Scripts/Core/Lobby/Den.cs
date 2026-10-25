using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>Why the den is open: to start a solo mission, or a co-op room's lobby. Crew (looking round the crew, no mission) was the old CREW button's; nothing opens the den in it now, but the den and its tests still handle it.</summary>
    public enum DenMode { Solo, Crew, Coop }

    /// <summary>What the den knows about the other player in a co-op room, from their replicated soldier.</summary>
    public struct DenPartner
    {
        /// <summary>Their flags in the den (<see cref="Den.InDen"/>, <see cref="Den.Seated"/>, <see cref="Den.ReadyFlag"/>).</summary>
        public int Flags;
        /// <summary>Who they picked, or are playing; null when nobody's there.</summary>
        public SoldierDef Soldier;
        /// <summary>They're already in the mission (a partner who readied up first, or one we're dropping in on).</summary>
        public bool Playing;
        /// <summary>The tabk they have loaded (<see cref="TabkCard.Id"/>), 0 before they've said: one of each in co-op (<see cref="CoopTabks"/>).</summary>
        public int Tabk;

        public bool Present => Soldier != null || Flags != 0 || Playing;
        public bool InDenNow => (Flags & Den.InDen) != 0 && !Playing;
        public bool Seated => (Flags & Den.Seated) != 0;
        public bool Ready => (Flags & Den.ReadyFlag) != 0;
    }

    /// <summary>
    /// The lobby, the crew's gaming den (<c>docs/design/lobby.md</c>): who's on the couch, who's ready, and when the
    /// camera zooms into the TV and the mission starts. Picking a soldier means sitting them on the couch with a
    /// controller; READY presses START. In co-op each phone runs its own den from its own player's crew, and the two
    /// see each other through the flags their soldiers carry; two players can't be the same soldier, so the one who
    /// came second (or isn't running the shared world) gives way. Pure logic: <c>DenView</c> draws it.
    /// </summary>
    public sealed class Den
    {
        /// <summary>The flags a soldier carries in the den.</summary>
        public const int InDen = 1, Seated = 2, ReadyFlag = 4;
        /// <summary>Both ready in co-op: the TV counts down 3, 2, 1. Then the camera zooms into the screen.</summary>
        public const float CountdownSeconds = 3f, ZoomSeconds = 0.9f;

        public readonly DenMode Mode;
        /// <summary>The soldier on our seat of the couch, or null while it's empty.</summary>
        public SoldierDef Pick { get; private set; }
        public bool Ready { get; private set; }
        /// <summary>Seconds left on the co-op countdown, or below 0 when it isn't running.</summary>
        public float Countdown { get; private set; } = -1f;
        /// <summary>Seconds into the zoom into the TV, or below 0 before it.</summary>
        public float Zoom { get; private set; } = -1f;
        /// <summary>The zoom's done: start the mission.</summary>
        public bool Go => Zoom >= ZoomSeconds;
        public DenPartner Partner { get; private set; }
        /// <summary>We run the shared world (the host): our pick wins a tie, and we sit on the left.</summary>
        public bool IsAuthority { get; private set; } = true;
        /// <summary>Our pick was undone because the partner has that soldier; the view says so once.</summary>
        public bool Undone { get; private set; }

        readonly IReadOnlyList<SoldierDef> m_Crew;

        /// <param name="crew">The crewmates freed on this phone, who have stations.</param>
        /// <param name="last">The soldier played last, already on the couch when the den opens (null for nobody).</param>
        public Den(DenMode mode, IReadOnlyList<SoldierDef> crew, SoldierDef last)
        {
            Mode = mode;
            m_Crew = crew;
            if (last != null && Has(last)) Pick = last;
        }

        public IReadOnlyList<SoldierDef> Crew => m_Crew;

        /// <summary>Our seat: 0 is player one's (left), 1 player two's.</summary>
        public int Seat => Mode == DenMode.Coop && !IsAuthority ? 1 : 0;

        /// <summary>What our soldier carries for the partner's den.</summary>
        public int Flags => InDen | (Pick != null ? Seated : 0) | (Ready ? ReadyFlag : 0);

        /// <summary>The partner has <paramref name="soldier"/>: on their seat, or in the mission.</summary>
        public bool Taken(SoldierDef soldier) =>
            soldier != null && Mode == DenMode.Coop && Partner.Soldier == soldier && (Partner.Playing || Partner.InDenNow && Partner.Seated);

        /// <summary>A crewmate tapped: they take our seat. False if they can't (not ours, taken, or the zoom's begun).</summary>
        public bool TryPick(SoldierDef soldier)
        {
            if (Zoom >= 0f || !Has(soldier) || Taken(soldier)) return false;
            if (Pick == soldier) return true;
            Pick = soldier;
            Ready = false;
            Undone = false;
            return true;
        }

        /// <summary>The next crewmate who's free, after the one on the couch (the keyboard's way to pick).</summary>
        public SoldierDef NextFree()
        {
            int from = 0;
            for (int i = 0; i < m_Crew.Count; i++)
                if (m_Crew[i] == Pick) from = i + 1;
            for (int k = 0; k < m_Crew.Count; k++)
            {
                SoldierDef s = m_Crew[(from + k) % m_Crew.Count];
                if (!Taken(s)) return s;
            }
            return null;
        }

        /// <summary>
        /// Sitting down on the couch (ready) or getting up (not), as the soldier walks to it or away. False when it
        /// can't: CREW has no mission, nobody's picked, or the zoom's begun.
        /// </summary>
        public bool SetReady(bool ready)
        {
            if (Mode == DenMode.Crew || Zoom >= 0f || Pick == null) return false;
            if (Ready == ready) return true;
            Ready = ready;
            if (!Ready) Countdown = -1f;
            return true;
        }

        /// <summary>Takes the "pick undone" note, once.</summary>
        public bool TakeUndone()
        {
            bool u = Undone;
            Undone = false;
            return u;
        }

        /// <summary>
        /// Moves the den on: gives way if the partner has our soldier, runs the countdown while everyone in the den
        /// is ready, and the zoom once it's out (straight away when nobody else is in the den: a partner already in the
        /// mission, or an empty room, doesn't wait).
        /// </summary>
        public void Tick(float dt, DenPartner partner, bool authority)
        {
            Partner = partner;
            IsAuthority = authority;
            if (Mode != DenMode.Coop) Partner = default;
            if (Zoom >= 0f)
            {
                Zoom += dt;
                return;
            }
            // two players can't be the same soldier: whoever's in the mission keeps them, else the host
            if (Pick != null && Mode == DenMode.Coop && Partner.Soldier == Pick && (Partner.Playing || Partner.InDenNow && Partner.Seated && !authority))
            {
                Pick = null;
                Ready = false;
                Undone = true;
            }
            bool partnerHere = Mode == DenMode.Coop && Partner.InDenNow;
            bool go = Ready && Pick != null && (!partnerHere || Partner.Ready && Partner.Seated);
            if (!go)
            {
                Countdown = -1f;
                return;
            }
            if (!partnerHere)
            {
                Zoom = 0f;
                return;
            }
            if (Countdown < 0f) Countdown = CountdownSeconds;
            Countdown -= dt;
            if (Countdown <= 0f)
            {
                Countdown = -1f;
                Zoom = 0f;
            }
        }

        bool Has(SoldierDef soldier)
        {
            if (soldier == null) return false;
            foreach (SoldierDef s in m_Crew)
                if (s == soldier) return true;
            return false;
        }
    }

    /// <summary>One line in a bit: who says it (0 the bit's first crewmate, 1 its second), and when.</summary>
    public readonly struct DenLine
    {
        public readonly int Who;
        public readonly float At;
        public readonly string Text;

        public DenLine(int who, float at, string text)
        {
            Who = who;
            At = at;
            Text = text;
        }
    }

    /// <summary>
    /// A bit two crewmates in the den do together (<c>docs/design/lobby.md</c>, Crew interactions): the first walks over
    /// to the second, they play it out, and the first goes back. <see cref="OnCouch"/> bits are for a crewmate who's
    /// idle and one who's on the couch (they wander over and lean on its arm).
    /// </summary>
    public sealed class DenBit
    {
        public readonly string Id;
        public readonly SoldierDef First, Second;
        public readonly bool OnCouch;
        public readonly float Seconds;
        public readonly DenLine[] Lines;

        public DenBit(string id, SoldierDef first, SoldierDef second, bool onCouch, float seconds, params DenLine[] lines)
        {
            Id = id;
            First = first;
            Second = second;
            OnCouch = onCouch;
            Seconds = seconds;
            Lines = lines;
        }

        /// <summary>A wave across the room (<see cref="DenWave"/>): nobody walks over, and nobody speaks.</summary>
        public bool Wave => Id == DenWave.Id;

        /// <summary>The same bit by the same two (either way round, for a wave): the den doesn't play one twice in a row.</summary>
        public bool Same(DenBit other) =>
            other != null && other.Id == Id &&
            (other.First == First && other.Second == Second || Wave && other.First == Second && other.Second == First);
    }

    /// <summary>The bits, and how the den picks the next one. The lines are placeholders for the owner to rewrite.</summary>
    public static class DenBits
    {
        /// <summary>Seconds between bits, chosen in this range each time.</summary>
        public const float MinGap = 12f, MaxGap = 20f;

        public static readonly DenBit[] All =
        {
            // tones2 holds out a slushie; kie's Trollogram takes it, and it falls through and splashes on the floor
            new DenBit("slushie", Content.Tones2, Content.Kie, false, 7f,
                new DenLine(0, 1.6f, "WANT ONE?"), new DenLine(1, 4.2f, "THAT WASN'T ME.")),
            // whoever's idle leans on the couch's arm beside whoever's playing
            new DenBit("move-over", Content.Tones2, Content.Kie, true, 5f, new DenLine(0, 1.4f, "MOVE OVER")),
            new DenBit("im-next", Content.Kie, Content.Tones2, true, 5f, new DenLine(0, 1.4f, "I'M NEXT")),
        };

        /// <summary>
        /// The next bit for the crew as they are: <paramref name="idle"/> at their stations, <paramref name="seated"/> on
        /// the couch. Two idle crewmates with no bit of their own just wave (<see cref="DenWave"/>), and a wave is one of
        /// the choices like any bit. Never the one just played (<paramref name="last"/>) if another fits. Null when none fits.
        /// </summary>
        public static DenBit Choose(IReadOnlyList<SoldierDef> idle, IReadOnlyList<SoldierDef> seated, DenBit last, System.Random rng)
        {
            var fits = new List<DenBit>();
            foreach (DenBit b in All)
                if (Contains(idle, b.First) && Contains(b.OnCouch ? seated : idle, b.Second)) fits.Add(b);
            for (int i = 0; i < idle.Count; i++)
                for (int j = i + 1; j < idle.Count; j++)
                    if (!HaveABit(idle[i], idle[j])) fits.Add(rng.Next(2) == 0 ? DenWave.Of(idle[i], idle[j]) : DenWave.Of(idle[j], idle[i]));
            if (fits.Count > 1 && fits.Exists(b => !b.Same(last))) fits.RemoveAll(b => b.Same(last));
            return fits.Count == 0 ? null : fits[rng.Next(fits.Count)];
        }

        /// <summary>Whether <paramref name="a"/> and <paramref name="b"/> have a bit of their own, either way round, anywhere in the den.</summary>
        public static bool HaveABit(SoldierDef a, SoldierDef b)
        {
            foreach (DenBit bit in All)
                if (bit.First == a && bit.Second == b || bit.First == b && bit.Second == a) return true;
            return false;
        }

        static bool Contains(IReadOnlyList<SoldierDef> list, SoldierDef s)
        {
            foreach (SoldierDef x in list)
                if (x == s) return true;
            return false;
        }
    }

    /// <summary>
    /// A crewmate's idle at their station (<c>docs/design/lobby.md</c>, Who lives where): one of their two short loops at
    /// random, then a plain breath for a few seconds, then another loop. A breath always comes between two loops, so a
    /// loop never runs straight into the next. Cosmetic: the den gives it its own randomness, never the game's.
    /// </summary>
    public static class DenIdle
    {
        /// <summary>A crewmate's loops are 0 and 1 (tones2: the slushie, the spray; kie: the projector, the weasel; kcaj: <see cref="KcajIdle"/>).</summary>
        public const int Loops = 2;
        /// <summary>The plain breathing idle between loops.</summary>
        public const int Breath = Loops;
        /// <summary>A loop's length, and the range a breath's is chosen in.</summary>
        public const float LoopSeconds = 4.5f, BreathMin = 2.5f, BreathMax = 4.5f;

        /// <summary>What follows <paramref name="last"/>: a breath after a loop, and a loop at random after a breath.</summary>
        public static int Next(int last, System.Random rng) => last == Breath ? rng.Next(Loops) : Breath;

        /// <summary>How long <paramref name="cycle"/> runs: a loop its set length, a breath a few seconds at random.</summary>
        public static float SecondsOf(int cycle, System.Random rng) =>
            cycle == Breath ? BreathMin + (float)rng.NextDouble() * (BreathMax - BreathMin) : LoopSeconds;
    }

    /// <summary>
    /// A wave across the room (<c>docs/design/lobby.md</c>, Crew interactions: a pair with no bit just waves). Both stay
    /// at their stations and turn to each other; the first waves, and the second waves back as the first finishes.
    /// No lines. Its times are seconds into the wave.
    /// </summary>
    public static class DenWave
    {
        public const string Id = "wave";
        /// <summary>The whole wave, the first's hand up and down, and the second's.</summary>
        public const float Seconds = 3f;
        public const float FirstFrom = 0.2f, FirstTo = 1.8f, SecondFrom = 1.2f, SecondTo = 2.8f;
        /// <summary>The hand goes up and down this fast while waving (up on the first step).</summary>
        public static readonly Pulse Hand = new Pulse(4f);

        /// <summary>A wave from <paramref name="first"/> to <paramref name="second"/>, who waves back.</summary>
        public static DenBit Of(SoldierDef first, SoldierDef second) => new DenBit(Id, first, second, false, Seconds);

        /// <summary>Whether <paramref name="who"/> (0 the first, 1 the second) is waving <paramref name="t"/> seconds in.</summary>
        public static bool Waving(int who, float t) => who == 0 ? t >= FirstFrom && t < FirstTo : t >= SecondFrom && t < SecondTo;

        /// <summary>Whether <paramref name="who"/>'s hand is up <paramref name="t"/> seconds in: up and down while they wave, down otherwise.</summary>
        public static bool HandUp(int who, float t) => Waving(who, t) && Hand.On(t - (who == 0 ? FirstFrom : SecondFrom));
    }

    /// <summary>
    /// kcaj's two loops at his station (<c>docs/design/lobby.md</c>, Who lives where), each <see cref="DenIdle.LoopSeconds"/>
    /// long: he eats two cones at once, both up to his mouth together for three bites; or he stares at the string board,
    /// takes a sticky note out, sticks it up, and the screen's edges dim for a moment (Hyperfixate). Times are seconds into the loop.
    /// </summary>
    public static class KcajIdle
    {
        public const int Cones = 0, Note = 1;

        /// <summary>The bites: both cones at his mouth from each start to each end.</summary>
        public static readonly float[] BiteFrom = { 0.6f, 1.8f, 3.0f }, BiteTo = { 1.2f, 2.4f, 3.6f };
        /// <summary>The note's in his hand from <see cref="NoteInHand"/>, and up on the board from <see cref="NoteUp"/> to the loop's end.</summary>
        public const float NoteInHand = 0.8f, NoteUp = 1.6f;
        /// <summary>The dim fades in from <see cref="DimFrom"/>, is full from <see cref="DimFull"/> to <see cref="DimHold"/>, and gone by <see cref="DimTo"/>.</summary>
        public const float DimFrom = 1.8f, DimFull = 2.3f, DimHold = 3.3f, DimTo = 4.1f;

        /// <summary>Both cones are at his mouth <paramref name="t"/> seconds into the cones loop.</summary>
        public static bool Biting(float t)
        {
            for (int i = 0; i < BiteFrom.Length; i++)
                if (t >= BiteFrom[i] && t < BiteTo[i]) return true;
            return false;
        }

        /// <summary>Where the note is <paramref name="t"/> seconds into the note loop: 0 not out yet, 1 in his hand, 2 on the board.</summary>
        public static int NoteAt(float t) => t < NoteInHand ? 0 : t < NoteUp ? 1 : 2;

        /// <summary>How dim the screen's edges are <paramref name="t"/> seconds into the note loop, from 0 (not at all) to 1.</summary>
        public static float Dim(float t)
        {
            if (t <= DimFrom || t >= DimTo) return 0f;
            if (t < DimFull) return (t - DimFrom) / (DimFull - DimFrom);
            if (t <= DimHold) return 1f;
            return (DimTo - t) / (DimTo - DimHold);
        }
    }
}
