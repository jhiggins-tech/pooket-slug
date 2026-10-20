using System;

namespace PooketSlug
{
    /// <summary>The tabk bay's two levels: its floor, and the top deck of parking over the far end of it.</summary>
    public enum BayDeck { Floor, Top }

    /// <summary>The places in the bay you walk to and use, besides the tabks themselves (<c>docs/design/lobby.md</c>, The tabk bay).</summary>
    public enum BayPlace { None, Door, Lift, Wash, Street }

    /// <summary>
    /// The tabk bay's map (<c>docs/design/lobby.md</c>, The tabk bay): the outer room through the den's roller door, a
    /// garage painted 993 x 180 (<c>Resources/Den/bay</c>) that scrolls sideways. In its art pixels, from the bottom left:
    /// the roller door back to the den, the repair stands under the crane, two decks of six parking spaces (the top one
    /// over the floor's), the lift at the top deck's end that carries soldiers between them, the wash (off for now) and
    /// the street door (shut).
    /// </summary>
    public static class BayMap
    {
        /// <summary>The painting's width.</summary>
        public const float Width = 993f;
        /// <summary>The repair stands' middle, and where a tabk on them stands (its feet).</summary>
        public const float StandX = 92f, StandY = 17f;
        /// <summary>The lift's middle, and the top deck's ends (the lift's landing is at its right end).</summary>
        public const float LiftX = 752f, DeckLeft = 190f, DeckRight = 706f;
        /// <summary>Each deck's six parking spaces, left to right (a space's index is the same on both).</summary>
        public static readonly float[] SpaceX = { 232f, 308f, 407f, 483f, 582f, 658f };
        /// <summary>Spaces on each deck; spaces 0 to 5 are the floor's, 6 to 11 the top deck's.</summary>
        public const int PerDeck = 6, Spaces = PerDeck * 2;
        /// <summary>A tabk's spot: on the stands, or a space.</summary>
        public const int Stands = -1;
        /// <summary>A tabk in the bay, drawn 1.5 times the game's (52 x 22): its size in the bay's pixels.</summary>
        public const float TabkW = 78f, TabkH = 33f;

        /// <summary>Where a soldier's feet are on each deck.</summary>
        public static float FloorY(BayDeck deck) => deck == BayDeck.Top ? 76f : 13f;

        /// <summary>How far each deck lets you walk: the floor from the den's door to the shut street door, the top deck from its left end to the lift.</summary>
        public static float MinX(BayDeck deck) => deck == BayDeck.Top ? DeckLeft + 8f : 14f;
        public static float MaxX(BayDeck deck) => deck == BayDeck.Top ? LiftX : 930f;

        public static float Clamp(BayDeck deck, float x) => Math.Max(MinX(deck), Math.Min(MaxX(deck), x));

        /// <summary>The deck a tap at (<paramref name="x"/>, <paramref name="y"/>) is on: above the top deck's floor, where there is one.</summary>
        public static BayDeck DeckAt(float x, float y) => y >= 66f && x >= DeckLeft - 4f && x <= LiftX + 30f ? BayDeck.Top : BayDeck.Floor;

        /// <summary>A spot's deck: the stands are on the floor.</summary>
        public static BayDeck DeckOf(int spot) => spot >= PerDeck ? BayDeck.Top : BayDeck.Floor;

        /// <summary>Where a tabk in <paramref name="spot"/> stands: its middle and its feet.</summary>
        public static float SpotX(int spot) => spot == Stands ? StandX : SpaceX[spot % PerDeck];
        public static float SpotY(int spot) => spot == Stands ? StandY : FloorY(DeckOf(spot));

        /// <summary>Where you stand to look a tabk over: beside it, on the den's side.</summary>
        public static float StandBy(int spot) => SpotX(spot) - TabkW / 2f - 2f;

        /// <summary>The spot a tap at (<paramref name="x"/>, <paramref name="y"/>) is on (a tabk's, if one's there), or null.</summary>
        public static int? SpotAt(float x, float y)
        {
            for (int s = Stands; s < Spaces; s++)
            {
                float sx = SpotX(s), sy = SpotY(s);
                if (Math.Abs(x - sx) <= TabkW / 2f && y >= sy - 4f && y <= sy + TabkH + 4f) return s;
            }
            return null;
        }

        // each place's tap area, as x0, y0, x1, y1
        static readonly (BayPlace place, float x0, float y0, float x1, float y1)[] Areas =
        {
            (BayPlace.Door, 0f, 10f, 22f, 130f),
            (BayPlace.Lift, 716f, 10f, 790f, 130f),
            (BayPlace.Wash, 820f, 10f, 916f, 110f),
            (BayPlace.Street, 940f, 10f, 993f, 130f),
        };

        /// <summary>The place a tap is on, or None.</summary>
        public static BayPlace PlaceAt(float x, float y)
        {
            foreach (var a in Areas)
                if (x >= a.x0 && x <= a.x1 && y >= a.y0 && y <= a.y1) return a.place;
            return BayPlace.None;
        }

        /// <summary>Where you stand to use a place (on the floor; the lift's is wherever you are, see <see cref="BayWalker.Ride"/>).</summary>
        public static float XOf(BayPlace place)
        {
            switch (place)
            {
                case BayPlace.Door: return 14f;
                case BayPlace.Lift: return LiftX;
                case BayPlace.Wash: return 868f;
                default: return 930f;
            }
        }

        /// <summary>What a place says when you use it, for the ones that only talk (null for the door and the lift, which do something).</summary>
        public static string Note(BayPlace place)
        {
            switch (place)
            {
                case BayPlace.Wash: return "THE WASH: SWITCHED OFF FOR NOW";
                case BayPlace.Street: return "THE STREET DOOR STAYS SHUT: THE DRONE TAKES TABKS OUT";
                default: return null;
            }
        }
    }

    /// <summary>Where a soldier in the bay is headed and why: a place to use, or a tabk's spot to look it over (or neither: just walking).</summary>
    public readonly struct BayErrand
    {
        public readonly BayPlace Place;
        /// <summary>The spot of the tabk to look over, or null.</summary>
        public readonly int? Spot;

        public BayErrand(BayPlace place, int? spot)
        {
            Place = place;
            Spot = spot;
        }

        public bool Any => Place != BayPlace.None || Spot != null;
        public static readonly BayErrand None = default;
    }

    /// <summary>
    /// The soldier walking round the bay (<see cref="BayMap"/>): along a deck, and between the floor and the top deck on
    /// the lift, which comes to them first if it's at the other level. Sent somewhere with <see cref="WalkTo"/>, and walked
    /// by hand with <see cref="Walk"/> and <see cref="Ride"/>.
    /// </summary>
    public sealed class BayWalker
    {
        public const float WalkSpeed = 80f, LiftSpeed = 45f;

        public BayDeck Deck { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public bool FaceLeft { get; private set; }
        /// <summary>On the lift's platform, going up or down.</summary>
        public bool Riding { get; private set; }
        /// <summary>The lift's platform: its height (it stays where it was last left).</summary>
        public float LiftY { get; private set; } = BayMap.FloorY(BayDeck.Floor);
        public BayDeck ToDeck { get; private set; }
        public float ToX { get; private set; }
        public BayErrand Errand { get; private set; }

        public BayWalker(BayDeck deck, float x)
        {
            Place(deck, x);
        }

        /// <summary>Puts them straight there, standing still.</summary>
        public void Place(BayDeck deck, float x)
        {
            Deck = ToDeck = deck;
            X = ToX = BayMap.Clamp(deck, x);
            Y = BayMap.FloorY(deck);
            Riding = false;
            Errand = BayErrand.None;
        }

        /// <summary>Still, and not on the lift.</summary>
        public bool Standing => !Riding && Deck == ToDeck && Math.Abs(X - ToX) < 0.5f;

        public void WalkTo(BayDeck deck, float x, BayErrand errand = default)
        {
            ToDeck = deck;
            ToX = BayMap.Clamp(deck, x);
            Errand = errand;
        }

        /// <summary>Off to <paramref name="place"/>, to use it. The lift takes you to the other level.</summary>
        public void Go(BayPlace place)
        {
            if (place == BayPlace.Lift) Ride();
            else WalkTo(BayDeck.Floor, BayMap.XOf(place), new BayErrand(place, null));
        }

        /// <summary>Off to the tabk in <paramref name="spot"/>, to look it over.</summary>
        public void Visit(int spot) => WalkTo(BayMap.DeckOf(spot), BayMap.StandBy(spot), new BayErrand(BayPlace.None, spot));

        /// <summary>To the lift, and on it to the other level, stepping off just beside it.</summary>
        public void Ride()
        {
            BayDeck other = Deck == BayDeck.Top ? BayDeck.Floor : BayDeck.Top;
            WalkTo(other, BayMap.LiftX - (other == BayDeck.Top ? 12f : 0f));
        }

        /// <summary>
        /// By hand: walks <paramref name="dir"/> along the deck, or stops at 0. Not on the lift. Walking left along the floor
        /// is walking into the den's roller door: reach it and you go through, as if you'd tapped it.
        /// </summary>
        public void Walk(int dir)
        {
            if (Riding) return;
            ToDeck = Deck;
            ToX = dir == 0 ? X : dir < 0 ? BayMap.MinX(Deck) : BayMap.MaxX(Deck);
            Errand = dir < 0 && Deck == BayDeck.Floor ? new BayErrand(BayPlace.Door, null) : BayErrand.None;
        }

        /// <summary>The place you're standing at, to use with jump: the den's door, just inside it; None elsewhere.</summary>
        public BayPlace PlaceHere => !Riding && Deck == BayDeck.Floor && X <= BayMap.XOf(BayPlace.Door) + DoorReach ? BayPlace.Door : BayPlace.None;

        /// <summary>How far from the den's door you can still jump to use it: where you come in, and a step more.</summary>
        public const float DoorReach = 24f;

        /// <summary>Moves them on, and the lift. Returns the errand they've just finished walking to, once; None otherwise.</summary>
        public BayErrand Step(float dt)
        {
            if (Riding)
            {
                float y = BayMap.FloorY(ToDeck);
                Y = LiftY = Toward(Y, y, LiftSpeed * dt);
                if (Y == y)
                {
                    Deck = ToDeck;
                    Riding = false;
                }
                return BayErrand.None;
            }
            if (Deck != ToDeck)
            {
                // to the lift, calling it down (or up) meanwhile, then on and away
                LiftY = Toward(LiftY, Y, LiftSpeed * dt);
                if (Math.Abs(X - BayMap.LiftX) > 0.5f) MoveX(BayMap.LiftX, dt);
                else
                {
                    X = BayMap.LiftX;
                    if (LiftY == Y) Riding = true;
                }
                return BayErrand.None;
            }
            if (Math.Abs(X - ToX) > 0.5f)
            {
                MoveX(ToX, dt);
                return BayErrand.None;
            }
            X = ToX;
            BayErrand done = Errand;
            Errand = BayErrand.None;
            return done;
        }

        static float Toward(float from, float to, float step) => Math.Abs(to - from) <= step ? to : from + Math.Sign(to - from) * step;

        void MoveX(float to, float dt)
        {
            FaceLeft = to < X;
            X = Toward(X, to, WalkSpeed * dt);
        }

        public void Face(bool left) => FaceLeft = left;
    }
}
