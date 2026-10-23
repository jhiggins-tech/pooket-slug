using System;

namespace PooketSlug
{
    /// <summary>The house's outer rooms, each its own screen behind a door (<c>docs/design/lobby.md</c>, The outer rooms).</summary>
    public enum RoomId
    {
        None,
        /// <summary>The kitchen, out the den's side door.</summary>
        Kitchen,
        /// <summary>The back yard, out the kitchen's back door.</summary>
        Backyard,
        /// <summary>B1, the basement, down the trapdoor in front of the couch.</summary>
        Basement,
        /// <summary>B2, the bunker, down through the hole in B1's floor.</summary>
        Bunker,
        /// <summary>The rooftop, out the loft's far-left door.</summary>
        Rooftop,
        /// <summary>The shower block, out the loft's shower door.</summary>
        ShowerBlock,
        /// <summary>The wintergarden, out the loft's wintergarden door.</summary>
        Wintergarden,
    }

    /// <summary>
    /// The ways in and out of the outer rooms. Each way names where it leads: a way in a room leads to another room or
    /// back to the house, and a way in the house is the door that opens a room.
    /// </summary>
    public enum RoomWay
    {
        None,
        /// <summary>The den's side door, in the house: it opens the kitchen.</summary>
        SideDoor,
        /// <summary>The trapdoor, in the house: it opens the basement.</summary>
        Trapdoor,
        /// <summary>The loft's far-left door, in the house: it opens the rooftop.</summary>
        RooftopDoor,
        /// <summary>The loft's shower door, in the house: it opens the shower block.</summary>
        ShowerDoor,
        /// <summary>The loft's wintergarden door, in the house: it opens the wintergarden.</summary>
        WintergardenDoor,
        /// <summary>A room's way back to the house.</summary>
        Back,
        /// <summary>The kitchen's back door, to the back yard.</summary>
        ToBackyard,
        /// <summary>The basement's hole, down to the bunker.</summary>
        ToBunker,
    }

    /// <summary>
    /// The outer rooms' map (<c>docs/design/lobby.md</c>, The outer rooms): each room's own screen, its size, where the
    /// ways in and out are, and what is in it. In the game's art pixels: every room is 320 wide, and 180 tall except the
    /// wintergarden, which is 360 (one screen across, two up).
    /// </summary>
    public static class RoomMap
    {
        /// <summary>A room's width: one screen across, unless a room is wider and the player scrolls through it.</summary>
        public const float Width = 320f;
        /// <summary>The kitchen's width: wider than a screen, so the player scrolls through it.</summary>
        public const float KitchenWidth = 420f;
        /// <summary>A room's height, except the wintergarden's.</summary>
        public const float Height = 180f;
        /// <summary>The wintergarden's height: two screens up, for the stair to the rose.</summary>
        public const float WinterHeight = 360f;
        /// <summary>Where a soldier's feet are on a room's floor.</summary>
        public const float FloorY = 15f;
        /// <summary>The soldier's height, which the rooms are drawn to (docs/design/lobby.md, Den 2.0: Scale).</summary>
        public const float SoldierHeight = 22f;
        /// <summary>How near a way you can still use it, walking: where you come in, and a step more.</summary>
        public const float WayReach = 24f;

        /// <summary>A room's width: one screen across, unless it is wider and the player scrolls through it.</summary>
        public static float WidthOf(RoomId room) => room == RoomId.Kitchen ? KitchenWidth : Width;

        /// <summary>A room's height.</summary>
        public static float HeightOf(RoomId room) => room == RoomId.Wintergarden ? WinterHeight : Height;

        /// <summary>The painting's name in <c>Resources/Den</c> for a room.</summary>
        public static string PaintingOf(RoomId room)
        {
            switch (room)
            {
                case RoomId.Kitchen: return "room-kitchen";
                case RoomId.Backyard: return "room-backyard";
                case RoomId.Basement: return "room-basement";
                case RoomId.Bunker: return "room-bunker";
                case RoomId.Rooftop: return "room-rooftop";
                case RoomId.ShowerBlock: return "room-shower";
                default: return "room-wintergarden";
            }
        }

        /// <summary>The room a way in the house opens.</summary>
        public static RoomId RoomOf(RoomWay way)
        {
            switch (way)
            {
                case RoomWay.SideDoor: return RoomId.Kitchen;
                case RoomWay.Trapdoor: return RoomId.Basement;
                case RoomWay.RooftopDoor: return RoomId.Rooftop;
                case RoomWay.ShowerDoor: return RoomId.ShowerBlock;
                case RoomWay.WintergardenDoor: return RoomId.Wintergarden;
                default: return RoomId.None;
            }
        }

        /// <summary>The way in a room that leads back to the house, or None when it has none (the yard, the bunker).</summary>
        public static RoomWay BackOf(RoomId room)
        {
            switch (room)
            {
                case RoomId.Kitchen: return RoomWay.Back;
                case RoomId.Basement: return RoomWay.Back;
                case RoomId.Rooftop: return RoomWay.Back;
                case RoomId.ShowerBlock: return RoomWay.Back;
                case RoomId.Wintergarden: return RoomWay.Back;
                default: return RoomWay.None; // the back yard and the bunker: no way back to the house
            }
        }

        /// <summary>
        /// Where a way in a room leads: the room, and the way in that room you arrive by. A way that leads back to the
        /// house gives the room None and the way in the house.
        /// </summary>
        public static void Leads(RoomId room, RoomWay way, out RoomId to, out RoomWay by)
        {
            to = RoomId.None;
            by = RoomWay.None;
            switch (room)
            {
                case RoomId.Kitchen:
                    if (way == RoomWay.Back) { by = RoomWay.SideDoor; return; }
                    if (way == RoomWay.ToBackyard) { to = RoomId.Backyard; by = RoomWay.Back; return; }
                    break;
                case RoomId.Backyard:
                    if (way == RoomWay.Back) { to = RoomId.Kitchen; by = RoomWay.ToBackyard; return; }
                    break;
                case RoomId.Basement:
                    if (way == RoomWay.Back) { by = RoomWay.Trapdoor; return; }
                    if (way == RoomWay.ToBunker) { to = RoomId.Bunker; by = RoomWay.Back; return; }
                    break;
                case RoomId.Bunker:
                    if (way == RoomWay.Back) { to = RoomId.Basement; by = RoomWay.ToBunker; return; }
                    break;
                case RoomId.Rooftop:
                    if (way == RoomWay.Back) { by = RoomWay.RooftopDoor; return; }
                    break;
                case RoomId.ShowerBlock:
                    if (way == RoomWay.Back) { by = RoomWay.ShowerDoor; return; }
                    break;
                case RoomId.Wintergarden:
                    if (way == RoomWay.Back) { by = RoomWay.WintergardenDoor; return; }
                    break;
            }
        }

        /// <summary>Whether a way is in a room at all.</summary>
        public static bool InRoom(RoomId room, RoomWay way)
        {
            switch (room)
            {
                case RoomId.Kitchen: return way == RoomWay.Back || way == RoomWay.ToBackyard;
                case RoomId.Backyard: return way == RoomWay.Back;
                case RoomId.Basement: return way == RoomWay.Back || way == RoomWay.ToBunker;
                case RoomId.Bunker: return way == RoomWay.Back;
                case RoomId.Rooftop: case RoomId.ShowerBlock: case RoomId.Wintergarden: return way == RoomWay.Back;
                default: return false;
            }
        }

        /// <summary>The ways in a room, in the order they are in it.</summary>
        public static readonly RoomWay[] Ways = { RoomWay.Back, RoomWay.ToBackyard, RoomWay.ToBunker };

        /// <summary>
        /// Where a way is in a room: its x, in the room's own art pixels. The way back is at the room's left, and the way
        /// on at its right.
        /// </summary>
        public static float WayX(RoomId room, RoomWay way)
        {
            switch (room)
            {
                case RoomId.Kitchen:
                    return way == RoomWay.ToBackyard ? MaxX(room) : MinX(room);
                case RoomId.Backyard: return MinX(room);
                case RoomId.Basement:
                    return way == RoomWay.ToBunker ? 289f : MinX(room);
                case RoomId.Bunker: return RopeX;
                case RoomId.Rooftop: return MaxX(room);
                case RoomId.ShowerBlock: return MinX(room);
                case RoomId.Wintergarden: return MinX(room);
                default: return MinX(room);
            }
        }

        /// <summary>How far you can walk in a room: short of the walls.</summary>
        public static float MinX(RoomId room) => 16f;
        public static float MaxX(RoomId room) => WidthOf(room) - 16f;

        public static float Clamp(RoomId room, float x) => Math.Max(MinX(room), Math.Min(MaxX(room), x));

        /// <summary>
        /// The wintergarden's stair: where it is (its foot, in the room's x), and how high it climbs. The rose is on the
        /// landing at the top, and the landing is where two crew can stand for a cutscene.
        /// </summary>
        public const float StairX = 205f;
        /// <summary>The landing's height: the top of the stair.</summary>
        public const float LandingY = 296f;
        /// <summary>How fast you climb the stair.</summary>
        public const float ClimbSpeed = 90f;

        /// <summary>The bunker's rope, where the painting hangs it: under the hole in its roof, and where its way is.</summary>
        public const float RopeX = 300f;
        /// <summary>The rope's top: the hole in the bunker's roof, where you climb in and out of the room.</summary>
        public const float RopeTopY = 160f;
        /// <summary>How fast you climb the rope: the wintergarden's stair pace.</summary>
        public const float RopeSpeed = 90f;

        /// <summary>Whether a way is by the rope: the bunker is entered down it from the hole, and left up it.</summary>
        public static bool ByRope(RoomId room, RoomWay way) => room == RoomId.Bunker && way == RoomWay.Back;

        /// <summary>
        /// Where a tap at (<paramref name="x"/>, <paramref name="y"/>) in a room is: a way to use, or None (the floor:
        /// walk there). A door is tapped where you stand to use it; a rope is tapped anywhere up it.
        /// </summary>
        public static RoomWay WayAt(RoomId room, float x, float y)
        {
            foreach (RoomWay way in Ways)
            {
                if (!InRoom(room, way)) continue;
                float wx = WayX(room, way);
                float top = ByRope(room, way) ? RopeTopY : FloorY + 45f;
                if (x >= wx - 12f && x <= wx + 12f && y >= FloorY && y <= top) return way;
            }
            return RoomWay.None;
        }
    }

    /// <summary>
    /// Someone walking round one of the outer rooms (<see cref="RoomMap"/>): along its floor, and up the wintergarden's
    /// stair. Sent somewhere with <see cref="WalkTo"/> (or to a way, which they use on arriving), or walked by hand
    /// with <see cref="Walk"/> and <see cref="Climb"/>.
    /// </summary>
    public sealed class RoomWalker
    {
        public const float WalkSpeed = 80f;

        public RoomId Room { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        /// <summary>On the wintergarden's stair, between its foot and its landing.</summary>
        public bool Climbing { get; private set; }
        public bool FaceLeft { get; private set; }
        /// <summary>Where they're headed: an x on the floor.</summary>
        public float ToX { get; private set; }
        /// <summary>The way they're headed for, to use it when they get there (None for just walking).</summary>
        public RoomWay Errand { get; private set; }
        /// <summary>Which way up the stair they're going: up or down.</summary>
        bool m_Up;

        public RoomWalker(RoomId room, float x)
        {
            Place(room, x);
        }

        /// <summary>Puts them straight there, standing still.</summary>
        public void Place(RoomId room, float x)
        {
            Room = room;
            X = ToX = RoomMap.Clamp(room, x);
            Y = RoomMap.FloorY;
            Climbing = false;
            Errand = RoomWay.None;
        }

        /// <summary>Still, and not on the stair.</summary>
        public bool Standing => !Climbing && Math.Abs(X - ToX) < 0.5f;

        public void WalkTo(float x)
        {
            ToX = RoomMap.Clamp(Room, x);
            Errand = RoomWay.None;
        }

        /// <summary>Off to a way, to use it when they get there.</summary>
        public void Go(RoomWay way)
        {
            WalkTo(RoomMap.WayX(Room, way));
            Errand = way;
        }

        /// <summary>By hand (left and right held): walks <paramref name="dir"/> along the floor, or stops at 0.</summary>
        public void Walk(int dir)
        {
            if (Climbing) return;
            Errand = RoomWay.None;
            ToX = dir == 0 ? X : dir < 0 ? RoomMap.MinX(Room) : RoomMap.MaxX(Room);
        }

        /// <summary>By hand (up or down): on the wintergarden's stair, climbs up or down it. False when there's no stair here.</summary>
        public bool Climb(int dir)
        {
            if (!CanClimb(dir)) return false;
            m_Up = dir > 0;
            Climbing = true;
            return true;
        }

        /// <summary>Whether a climb is possible here: what <see cref="Climb"/> would say.</summary>
        public bool CanClimb(int dir)
        {
            if (Room != RoomId.Wintergarden) return false;
            if (dir > 0) return !Climbing && Y < RoomMap.LandingY && Math.Abs(X - RoomMap.StairX) <= RoomMap.WayReach;
            return Y > RoomMap.FloorY;   // down: from the landing, or from part way up
        }

        /// <summary>The way you're standing at, to use with jump: the way in, just inside it; None elsewhere.</summary>
        public RoomWay WayHere
        {
            get
            {
                if (Climbing) return RoomWay.None;
                foreach (RoomWay way in RoomMap.Ways)
                {
                    if (!RoomMap.InRoom(Room, way)) continue;
                    if (Math.Abs(X - RoomMap.WayX(Room, way)) <= RoomMap.WayReach) return way;
                }
                return RoomWay.None;
            }
        }

        /// <summary>Moves them on. Returns the way they've just finished walking to, once; None otherwise.</summary>
        public RoomWay Step(float dt)
        {
            if (Climbing)
            {
                float target = m_Up ? RoomMap.LandingY : RoomMap.FloorY;
                float step = RoomMap.ClimbSpeed * dt;
                Y = Math.Abs(target - Y) <= step ? target : Y + Math.Sign(target - Y) * step;
                if (Y == target) Climbing = false;
                return RoomWay.None;
            }
            if (Math.Abs(X - ToX) > 0.5f)
            {
                FaceLeft = ToX < X;
                X = Math.Abs(ToX - X) <= WalkSpeed * dt ? ToX : X + Math.Sign(ToX - X) * WalkSpeed * dt;
                return RoomWay.None;
            }
            X = ToX;
            RoomWay done = Errand;
            Errand = RoomWay.None;
            return done;
        }
    }
}
