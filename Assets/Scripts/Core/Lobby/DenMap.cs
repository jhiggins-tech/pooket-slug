using System;

namespace PooketSlug
{
    /// <summary>The den's three floors: the den screen's ground floor and upper floor, and the long loft up the ladder.</summary>
    public enum DenFloor { Ground, Upper, Loft }

    /// <summary>The places in the den you walk to and use (<c>docs/design/lobby.md</c>, Den 2.0: walking to a station is the menu).</summary>
    public enum DenPlace { None, Couch, Board, Gym, Trophies, Bunks, Showers, Laundry, Wintergarden, Vending, SideDoor, RollerDoor }

    /// <summary>
    /// The den's map (<c>docs/design/lobby.md</c>, Den 2.0): its floors, the ladder that joins them, and the places on
    /// them. In the den screen's art pixels (320 x 180): the ground and upper floors are the den screen, and the loft, three
    /// screens wide, sits on top of it, running from -350 to 610 so the den's ladder comes up through the loft's hatch.
    /// </summary>
    public static class DenMap
    {
        /// <summary>The ladder, the one way between floors (the den's, up through the loft's hatch at the gym).</summary>
        public const float LadderX = 209f;
        /// <summary>The loft's ends, and where the den screen sits under it (the loft's own pixels 350 to 670).</summary>
        public const float LoftLeft = -350f, LoftRight = 610f, LoftY = 180f;

        /// <summary>Where a soldier's feet are on each floor.</summary>
        public static float FloorY(DenFloor floor) => floor == DenFloor.Ground ? 15f : floor == DenFloor.Upper ? 118f : 196f;

        /// <summary>How far each floor lets you walk (short of the walls).</summary>
        public static float MinX(DenFloor floor) => floor == DenFloor.Loft ? LoftLeft + 14f : 14f;
        public static float MaxX(DenFloor floor) => floor == DenFloor.Loft ? LoftRight - 14f : 306f;

        public static float Clamp(DenFloor floor, float x) => Math.Max(MinX(floor), Math.Min(MaxX(floor), x));

        /// <summary>The floor whose feet are nearest <paramref name="y"/> (a tap on the room picks the floor it's on).</summary>
        public static DenFloor FloorAt(float y) => y >= LoftY ? DenFloor.Loft : y >= 100f ? DenFloor.Upper : DenFloor.Ground;

        /// <summary>Where a place is: its floor and where you stand to use it.</summary>
        public static DenFloor FloorOf(DenPlace place)
        {
            switch (place)
            {
                case DenPlace.Gym: case DenPlace.Trophies: case DenPlace.Bunks: case DenPlace.Showers:
                case DenPlace.Laundry: case DenPlace.Wintergarden: case DenPlace.Vending:
                    return DenFloor.Loft;
                default: return DenFloor.Ground;
            }
        }

        public static float XOf(DenPlace place)
        {
            switch (place)
            {
                case DenPlace.Couch: return 124f;
                case DenPlace.Board: return 234f;
                case DenPlace.SideDoor: return 26f;
                case DenPlace.RollerDoor: return 282f;
                case DenPlace.Gym: return 100f;
                case DenPlace.Trophies: return 265f;
                case DenPlace.Bunks: return -293f;
                case DenPlace.Showers: return -122f;
                case DenPlace.Laundry: return -33f;
                case DenPlace.Wintergarden: return 430f;
                case DenPlace.Vending: return 547f;
                default: return 160f;
            }
        }

        // each place's tap area, as x0, y0, x1, y1 (the den's art pixels: what it is in the painting)
        static readonly (DenPlace place, float x0, float y0, float x1, float y1)[] Areas =
        {
            (DenPlace.Couch, 80f, 14f, 166f, 52f),
            (DenPlace.Board, 222f, 14f, 248f, 78f), // and the wall under it, down to where you stand
            (DenPlace.SideDoor, 12f, 14f, 36f, 78f),
            (DenPlace.RollerDoor, 254f, 14f, 306f, 90f),
            (DenPlace.Gym, 10f, 196f, 125f, 345f),
            (DenPlace.Trophies, 240f, 196f, 292f, 340f),
            (DenPlace.Bunks, -322f, 196f, -268f, 330f),
            (DenPlace.Showers, -172f, 196f, -75f, 345f),
            (DenPlace.Laundry, -62f, 196f, -5f, 320f),
            (DenPlace.Wintergarden, 360f, 196f, 500f, 345f),
            (DenPlace.Vending, 510f, 196f, 572f, 330f),
        };

        /// <summary>The place a tap at (<paramref name="x"/>, <paramref name="y"/>) is on, or None (the floor: walk there).</summary>
        public static DenPlace PlaceAt(float x, float y)
        {
            foreach (var a in Areas)
                if (x >= a.x0 && x <= a.x1 && y >= a.y0 && y <= a.y1) return a.place;
            return DenPlace.None;
        }

        /// <summary>
        /// What a place says when you use it, for the ones that only talk for now (null for the couch, the board and
        /// the gym, which do something). Tarped rooms don't say whose they are.
        /// </summary>
        public static string Note(DenPlace place)
        {
            switch (place)
            {
                case DenPlace.Bunks: return "THE BUNK ROOM: NOT YET";
                case DenPlace.Showers: case DenPlace.Laundry: case DenPlace.Wintergarden: return "TARPED OFF: SOMEONE'S MOVING IN";
                case DenPlace.Vending: return "OUT OF ORDER";
                case DenPlace.SideDoor: return "THE KITCHEN: NOT YET";
                case DenPlace.RollerDoor: return "THE TABK BAY: NOT YET";
                default: return null;
            }
        }
    }

    /// <summary>
    /// Someone walking round the den (<see cref="DenMap"/>): along a floor, and up or down the ladder to reach another.
    /// Sent somewhere with <see cref="WalkTo"/> (or to a place, which they use on arriving), or walked by hand with
    /// <see cref="Walk"/> and <see cref="Climb"/>.
    /// </summary>
    public sealed class DenWalker
    {
        public const float WalkSpeed = 60f, ClimbSpeed = 70f;

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

        /// <summary>By hand (up or down): at the ladder, climbs to the next floor that way. False when there's no ladder here or no floor that way.</summary>
        public bool Climb(int dir)
        {
            if (Climbing || Math.Abs(X - DenMap.LadderX) > 12f) return false;
            int next = (int)Floor + Math.Sign(dir);
            if (dir == 0 || next < 0 || next > (int)DenFloor.Loft) return false;
            ToFloor = (DenFloor)next;
            ToX = DenMap.LadderX;
            Errand = DenPlace.None;
            return true;
        }

        /// <summary>Moves them on. Returns the place they've just reached on an errand, once; None otherwise.</summary>
        public DenPlace Step(float dt)
        {
            if (Floor != ToFloor || Climbing)
            {
                // to the ladder, then up or down it a floor at a time
                if (!Climbing && Math.Abs(X - DenMap.LadderX) > 0.5f) MoveX(DenMap.LadderX, dt);
                else
                {
                    X = DenMap.LadderX;
                    Climbing = true;
                    // Floor is the one they left; turned back halfway (ToFloor == Floor), they climb back to it
                    DenFloor next = (DenFloor)((int)Floor + Math.Sign((int)ToFloor - (int)Floor));
                    float to = DenMap.FloorY(next);
                    float step = ClimbSpeed * dt;
                    Y = Math.Abs(to - Y) <= step ? to : Y + Math.Sign(to - Y) * step;
                    if (Y == to)
                    {
                        Floor = next;
                        Climbing = false;
                    }
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
