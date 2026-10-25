using System;

namespace PooketSlug
{
    /// <summary>The den's floors, bottom to top: the den screen's ground and upper floors, the loft up the ladder, and the bunk platform in its roof.</summary>
    public enum DenFloor { Ground, Upper, Loft, Bunks }

    /// <summary>The places in the den you walk to and use (<c>docs/design/lobby.md</c>, Den 2.0: walking to a station is the menu).</summary>
    public enum DenPlace { None, Couch, Board, Gym, Trophies, Bunks, Showers, Laundry, Wintergarden, Vending, SideDoor, RollerDoor, Rooftop, Trapdoor }

    /// <summary>
    /// The den's map (<c>docs/design/lobby.md</c>, Den 2.0): its floors, the ladders that join them, and the places on
    /// them. In the den screen's art pixels (320 x 180): the ground and upper floors are the den screen, and the loft
    /// (382 x 164, drawn for the 22 px soldier) sits on top of it from 19 to 401, so the den's ladder comes up through the
    /// gym's hatch. The bunks are a platform in the loft's roof, up their own short ladder by the bathroom.
    /// </summary>
    public static class DenMap
    {
        /// <summary>The den's ladder, from the ground floor up through the loft's hatch at the gym.</summary>
        public const float LadderX = 209f;
        /// <summary>The bunks' short ladder, from the loft floor up to the platform in the roof (beside the bathroom).</summary>
        public const float BunkLadderX = 88f;
        /// <summary>How near a ladder you climb it by hand (up or down), and how wide a tap on the den's ladder is: a finger's a little wider than a soldier.</summary>
        public const float LadderReach = 12f, LadderTapReach = 16f;
        /// <summary>The loft painting's ends and bottom, and its height (the hatch, at its own x 190, is over the den's ladder).</summary>
        public const float LoftLeft = 19f, LoftRight = 401f, LoftY = 180f, LoftH = 164f;
        /// <summary>The soldier's height, which everything is drawn to (docs/design/lobby.md, Den 2.0: Scale).</summary>
        public const float SoldierHeight = 22f;

        /// <summary>Where a soldier's feet are on each floor.</summary>
        public static float FloorY(DenFloor floor)
        {
            switch (floor)
            {
                case DenFloor.Ground: return 15f;
                case DenFloor.Upper: return 118f;
                case DenFloor.Loft: return LoftY + 14f;
                default: return LoftY + 75f;
            }
        }

        /// <summary>
        /// How high each floor is clear above the feet: up to the den's ceiling, the bunk platform's underside, or the
        /// tie beam over the bunks (the soldier stands upright under it).
        /// </summary>
        public static float Headroom(DenFloor floor)
        {
            switch (floor)
            {
                case DenFloor.Ground: return 88f;
                case DenFloor.Upper: return 48f;
                case DenFloor.Loft: return 56f;
                default: return 23f;
            }
        }

        /// <summary>How far each floor lets you walk (short of the walls, and on the bunks short of where the roof comes down).</summary>
        public static float MinX(DenFloor floor) => floor == DenFloor.Bunks ? LoftLeft + 36f : floor == DenFloor.Loft ? LoftLeft + 8f : 14f;
        public static float MaxX(DenFloor floor) => floor == DenFloor.Bunks ? LoftLeft + 346f : floor == DenFloor.Loft ? LoftRight - 8f : 306f;

        public static float Clamp(DenFloor floor, float x) => Math.Max(MinX(floor), Math.Min(MaxX(floor), x));

        /// <summary>The ladder between <paramref name="lower"/> and the floor above it.</summary>
        public static float LadderUp(DenFloor lower) => lower == DenFloor.Loft ? BunkLadderX : LadderX;

        /// <summary>The floor a tap at <paramref name="y"/> is on (a tap on the room picks the floor it's on).</summary>
        public static DenFloor FloorAt(float y) => y >= LoftY + 70f ? DenFloor.Bunks : y >= LoftY ? DenFloor.Loft : y >= 100f ? DenFloor.Upper : DenFloor.Ground;

        /// <summary>
        /// Where a tap on the floor walks you, standing on <paramref name="on"/>, to: the floor it's on, at the spot tapped. The
        /// den's ladder and the hatch at its top go between the upper floor and the loft (on a touch screen, nothing else
        /// does: each is off screen from the other): below the loft, a tap on them takes you up through the hatch; in the
        /// loft (or the bunks above it), a tap on the hatch or just under it takes you down.
        /// </summary>
        public static DenFloor WalkTarget(float x, float y, DenFloor on, out float toX)
        {
            bool up = on == DenFloor.Loft || on == DenFloor.Bunks;
            if (Math.Abs(x - LadderX) <= LadderTapReach && y >= FloorY(DenFloor.Upper) + 12f && y < (up ? FloorY(DenFloor.Loft) + 10f : LoftY))
            {
                toX = LadderX;
                return up ? DenFloor.Upper : DenFloor.Loft;
            }
            toX = x;
            return FloorAt(y);
        }

        /// <summary>A door, a ladder's rungs: what the scale rule checks against the soldier (doors about 1.7 times him).</summary>
        public readonly struct Fixture
        {
            public readonly string Name;
            public readonly DenFloor Floor;
            public readonly float Height;

            public Fixture(string name, DenFloor floor, float height)
            {
                Name = name;
                Floor = floor;
                Height = height;
            }
        }

        /// <summary>The doors on each floor and how tall they're painted.</summary>
        public static readonly Fixture[] Doors =
        {
            new Fixture("the rooftop door", DenFloor.Loft, 36f),
            new Fixture("the side door", DenFloor.Ground, 62f),
            new Fixture("the roller door", DenFloor.Ground, 74f),
        };

        /// <summary>The gap between each ladder's rungs.</summary>
        public static readonly Fixture[] Rungs =
        {
            new Fixture("the bunks' ladder", DenFloor.Loft, 6.7f),
            new Fixture("the den's ladder", DenFloor.Ground, 8.8f),
        };

        /// <summary>Where a place is: its floor and where you stand to use it.</summary>
        public static DenFloor FloorOf(DenPlace place)
        {
            switch (place)
            {
                case DenPlace.Bunks: return DenFloor.Bunks;
                case DenPlace.Gym: case DenPlace.Trophies: case DenPlace.Showers: case DenPlace.Rooftop:
                case DenPlace.Laundry: case DenPlace.Wintergarden: case DenPlace.Vending:
                    return DenFloor.Loft;
                case DenPlace.Board: return DenFloor.Upper;   // kcaj's string board, in his nook
                default: return DenFloor.Ground;
            }
        }

        public static float XOf(DenPlace place)
        {
            switch (place)
            {
                case DenPlace.Couch: return 124f;
                case DenPlace.Board: return 60f;   // kcaj's string board, in his nook on the upper floor
                case DenPlace.SideDoor: return 26f;
                case DenPlace.Trapdoor: return 235f;   // in the ground floor's floor, where the board used to hang
                case DenPlace.RollerDoor: return 282f;
                case DenPlace.Rooftop: return 39f;
                case DenPlace.Showers: return 117f;
                case DenPlace.Laundry: return 153f;
                case DenPlace.Gym: return 178f;
                case DenPlace.Trophies: return 250f;
                case DenPlace.Wintergarden: return 326f;
                case DenPlace.Vending: return 380f;
                case DenPlace.Bunks: return 140f;
                default: return 160f;
            }
        }

        // each place's tap area, as x0, y0, x1, y1 (the den's art pixels: what it is in the painting); the bunks are the
        // whole platform, and the gym leaves the hatch's ladder free
        static readonly (DenPlace place, float x0, float y0, float x1, float y1)[] Areas =
        {
            (DenPlace.Couch, 80f, 14f, 166f, 52f),
            (DenPlace.Board, 12f, 118f, 108f, 160f), // kcaj's string board, in his nook on the upper floor
            (DenPlace.SideDoor, 12f, 14f, 36f, 78f),
            (DenPlace.Trapdoor, 222f, 0f, 248f, 15f),   // in the floor, at the board's old spot
            (DenPlace.RollerDoor, 254f, 14f, 306f, 90f),
            (DenPlace.Bunks, 50f, 252f, 370f, 300f),
            (DenPlace.Rooftop, 28f, 192f, 50f, 240f),
            (DenPlace.Showers, 95f, 192f, 139f, 245f),
            (DenPlace.Laundry, 142f, 192f, 165f, 240f),
            (DenPlace.Gym, 168f, 192f, 197f, 245f),
            (DenPlace.Gym, 220f, 192f, 238f, 245f),
            (DenPlace.Trophies, 241f, 192f, 260f, 245f),
            (DenPlace.Wintergarden, 291f, 192f, 362f, 245f),
            (DenPlace.Vending, 366f, 192f, 395f, 245f),
        };

        /// <summary>The place a tap at (<paramref name="x"/>, <paramref name="y"/>) is on, or None (the floor: walk there).</summary>
        public static DenPlace PlaceAt(float x, float y)
        {
            foreach (var a in Areas)
                if (x >= a.x0 && x <= a.x1 && y >= a.y0 && y <= a.y1) return a.place;
            return DenPlace.None;
        }

        /// <summary>
        /// What a place says when you use it, for the ones that only talk for now (null for the couch, the board, the gym,
        /// the trophies and the roller door to the tabk bay, which do something). Tarped rooms don't say whose they are.
        /// </summary>
        public static string Note(DenPlace place)
        {
            switch (place)
            {
                case DenPlace.Bunks: return "THE BUNKS: THE CREW ROSTER, NOT YET";
                case DenPlace.Laundry: return "THE LAUNDRY: NOT YET";
                case DenPlace.Vending: return "OUT OF ORDER";
                default: return null;
            }
        }

        /// <summary>
        /// The outer room a place opens, and the way in the house that opens it, or None for the places that don't open
        /// one (the couch, the board, the gym, the trophies, the roller door to the tabk bay).
        /// </summary>
        public static RoomWay RoomOf(DenPlace place)
        {
            switch (place)
            {
                case DenPlace.SideDoor: return RoomWay.SideDoor;
                case DenPlace.Trapdoor: return RoomWay.Trapdoor;
                case DenPlace.Rooftop: return RoomWay.RooftopDoor;
                case DenPlace.Showers: return RoomWay.ShowerDoor;
                case DenPlace.Wintergarden: return RoomWay.WintergardenDoor;
                default: return RoomWay.None;
            }
        }
    }

    /// <summary>
    /// Someone walking round the den (<see cref="DenMap"/>): along a floor, and up or down a ladder to reach another.
    /// Sent somewhere with <see cref="WalkTo"/> (or to a place, which they use on arriving), or walked by hand with
    /// <see cref="Walk"/> and <see cref="Climb"/>.
    /// </summary>
    public sealed class DenWalker
    {
        public const float WalkSpeed = 80f, ClimbSpeed = 90f; // art pixels a second (the loft is 382 wide, about 1.2 screens)

        public DenFloor Floor { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        /// <summary>On the ladder, between floors.</summary>
        public bool Climbing { get; private set; }
        public bool FaceLeft { get; private set; }
        /// <summary>Where they're headed: a floor and a spot on it.</summary>
        public DenFloor ToFloor { get; private set; }
        public float ToX { get; private set; }
        /// <summary>The place they're headed for, to use it when they get there (None for just walking).</summary>
        public DenPlace Errand { get; private set; }

        public DenWalker(DenFloor floor, float x)
        {
            Place(floor, x);
        }

        /// <summary>Puts them straight there, standing still.</summary>
        public void Place(DenFloor floor, float x)
        {
            Floor = ToFloor = floor;
            X = ToX = DenMap.Clamp(floor, x);
            Y = DenMap.FloorY(floor);
            Climbing = false;
            Errand = DenPlace.None;
        }

        /// <summary>Still, and not on the ladder.</summary>
        public bool Standing => !Climbing && Floor == ToFloor && Math.Abs(X - ToX) < 0.5f;

        public void WalkTo(DenFloor floor, float x)
        {
            ToFloor = floor;
            ToX = DenMap.Clamp(floor, x);
            Errand = DenPlace.None;
        }

        /// <summary>Off to <paramref name="place"/>, to use it when they get there.</summary>
        public void Go(DenPlace place)
        {
            WalkTo(DenMap.FloorOf(place), DenMap.XOf(place));
            Errand = place;
        }

        /// <summary>By hand (left and right held): walks <paramref name="dir"/> along the floor, or stops at 0. Not on the ladder.</summary>
        public void Walk(int dir)
        {
            if (Climbing) return;
            Errand = DenPlace.None;
            ToFloor = Floor;
            ToX = dir == 0 ? X : dir < 0 ? DenMap.MinX(Floor) : DenMap.MaxX(Floor);
        }

        /// <summary>By hand (up or down): at a ladder, climbs to the next floor that way. False when there's no ladder here or no floor that way.</summary>
        public bool Climb(int dir)
        {
            if (!CanClimb(dir, out float ladder)) return false;
            ToFloor = (DenFloor)((int)Floor + Math.Sign(dir));
            ToX = ladder;
            Errand = DenPlace.None;
            return true;
        }

        /// <summary>Whether <see cref="Climb"/> would climb that way from here: standing by a ladder to a floor there is.</summary>
        public bool CanClimb(int dir) => CanClimb(dir, out _);

        bool CanClimb(int dir, out float ladder)
        {
            ladder = 0f;
            if (Climbing || dir == 0) return false;
            int next = (int)Floor + Math.Sign(dir);
            if (next < 0 || next > (int)DenFloor.Bunks) return false;
            ladder = DenMap.LadderUp((DenFloor)Math.Min((int)Floor, next));
            return Math.Abs(X - ladder) <= DenMap.LadderReach;
        }

        // on a ladder: the floor below it (the climb is between that and the one above)
        DenFloor m_ClimbLow;

        /// <summary>Moves them on. Returns the place they've just reached on an errand, once; None otherwise.</summary>
        public DenPlace Step(float dt)
        {
            if (Climbing)
            {
                // up or down the ladder they're on: to the floor above if that's the way they're headed, else back down
                DenFloor to = ToFloor > m_ClimbLow ? m_ClimbLow + 1 : m_ClimbLow;
                float y = DenMap.FloorY(to), step = ClimbSpeed * dt;
                Y = Math.Abs(y - Y) <= step ? y : Y + Math.Sign(y - Y) * step;
                if (Y == y)
                {
                    Floor = to;
                    Climbing = false;
                }
                return DenPlace.None;
            }
            if (Floor != ToFloor)
            {
                // to the ladder towards their floor, then onto it
                DenFloor low = ToFloor > Floor ? Floor : Floor - 1;
                float ladder = DenMap.LadderUp(low);
                if (Math.Abs(X - ladder) > 0.5f) MoveX(ladder, dt);
                else
                {
                    X = ladder;
                    m_ClimbLow = low;
                    Climbing = true;
                }
                return DenPlace.None;
            }
            if (Math.Abs(X - ToX) > 0.5f)
            {
                MoveX(ToX, dt);
                return DenPlace.None;
            }
            X = ToX;
            DenPlace arrived = Errand;
            Errand = DenPlace.None;
            return arrived;
        }

        void MoveX(float to, float dt)
        {
            float step = WalkSpeed * dt;
            FaceLeft = to < X;
            X = Math.Abs(to - X) <= step ? to : X + Math.Sign(to - X) * step;
        }

        /// <summary>Faces <paramref name="left"/> (standing at a station, or turning to someone).</summary>
        public void Face(bool left) => FaceLeft = left;
    }
}
