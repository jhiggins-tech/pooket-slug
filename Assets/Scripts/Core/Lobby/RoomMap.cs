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

    /// <summary>What a way in a room is: a door to walk through, a hole to go into, a ladder, a rope.</summary>
    public enum WayKind { None, Door, Hole, Ladder, Rope }

    /// <summary>
    /// A way's opening in a room: the box it is in (in the room's art pixels), and which way you go through it. The box
    /// is what cuts a soldier off as they walk into it: the frame's edge swallows them, so they go *into* the opening
    /// instead of off the side of the screen.
    /// </summary>
    public readonly struct Opening
    {
        public readonly float X, Y, W, H;
        /// <summary>Which way you go through it: to the right (a door in the right wall) or to the left.</summary>
        public readonly bool ToRight;

        public Opening(float x, float y, float w, float h, bool toRight)
        {
            X = x; Y = y; W = w; H = h; ToRight = toRight;
        }

        /// <summary>No opening: the way has none (a ladder, a rope).</summary>
        public bool None => W <= 0f || H <= 0f;
        public float Right => X + W;
        /// <summary>Where the soldier stands to step into it: the middle of the opening.</summary>
        public float Sill => X + W / 2f;
        /// <summary>Where the step starts: a shoulder's width before the opening's near edge.</summary>
        public float From => ToRight ? X - 4f : Right + 4f;
        /// <summary>Where the step ends: a shoulder's width past the far edge, out of sight.</summary>
        public float To => ToRight ? Right + 4f : X - 4f;
        /// <summary>Whether a point in the room is in the opening (the box the soldier is cut off by).</summary>
        public bool Covers(float x, float y) => x >= X && x <= Right && y >= Y && y <= Y + H;
    }

    /// <summary>
    /// The outer rooms' map (<c>docs/design/lobby.md</c>, The outer rooms): each room's own screen, its size, where the
    /// ways in and out are, and what is in it. In the game's art pixels: every room is 420 wide and 180 tall (a screen
    /// and a bit, so the player scrolls across it), except the wintergarden, which is one screen across and two up
    /// (320 × 360).
    /// </summary>
    public static class RoomMap
    {
        /// <summary>
        /// A room's width: a screen and a bit, so the player scrolls across it. It is the shape the paintings come back
        /// in (1260 × 540), cut to a third (<c>docs/design/lobby/rooms.md</c>, Paint it).
        /// </summary>
        public const float Width = 420f;
        /// <summary>The wintergarden's width: one screen across, under its own tall plate.</summary>
        public const float WinterWidth = 320f;
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

        /// <summary>A room's width: a screen and a bit, except the wintergarden's one screen.</summary>
        public static float WidthOf(RoomId room) => room == RoomId.Wintergarden ? WinterWidth : Width;

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
        /// Where a way is in a room: its x, in the room's own art pixels. It is on the door, ladder, hole or rope the
        /// painting has for it: the ways at a room's ends sit at its walls, and the ways that are painted inside it
        /// (the basement's hole, the bunker's rope) are where the painting puts them. The bunker's way back is its
        /// rope, under the hole in its roof.
        /// </summary>
        public static float WayX(RoomId room, RoomWay way)
        {
            switch (room)
            {
                case RoomId.Kitchen:
                    return way == RoomWay.ToBackyard ? MaxX(room) : MinX(room); // the den's side door, the back door
                case RoomId.Backyard: return YardDoorX + YardDoorW / 2f;       // the house door, in the yard's left wall
                case RoomId.Basement:
                    return way == RoomWay.ToBunker ? HoleX + HoleW / 2f : MinX(room); // the hole in its wall, the ladder up
                case RoomId.Bunker: return RopeX;                                // the rope, under its hatch
                case RoomId.Rooftop: return RoofHutX + RoofHutW / 2f;              // the stair hut's door, at its right end
                case RoomId.ShowerBlock: return ShowerDoorX + ShowerDoorW / 2f;     // the door back to the loft, in the painted door
                case RoomId.Wintergarden: return MinX(room);                     // the door back to the loft
                default: return MinX(room);
            }
        }

        /// <summary>The kitchen's back door, in its right wall (the frame the painting has, showing the night beyond).</summary>
        public const float KitchenDoorX = 389.5f, KitchenDoorY = 16f, KitchenDoorW = 25f, KitchenDoorH = 50f;
        /// <summary>
        /// The house door in the yard's left wall (the plate's own door, by the fence): measured off the plate, and
        /// down to the floor, so you can stand in it and go in.
        /// </summary>
        public const float YardDoorX = 45f, YardDoorY = 15f, YardDoorW = 32f, YardDoorH = 100f;
        /// <summary>
        /// The hole in the basement's right wall, down to the bunker: where the plate paints it (measured off the
        /// plate), and down to the floor, so you can stand in it and go down.
        /// </summary>
        public const float HoleX = 367f, HoleY = 15f, HoleW = 28f, HoleH = 100f;
        /// <summary>The basement's ladder, up its left wall to the den: how wide the ladder is, and how tall.</summary>
        public const float LadderW = 32f, LadderH = 60f;
        /// <summary>The kitchen's side door, in its left wall: the den's side door, at the room's own left end.</summary>
        public const float KitchenSideX = 0f, KitchenSideY = 15f, KitchenSideW = 31f, KitchenSideH = 37f;
        /// <summary>The shower block's door back to the loft, in its left wall: measured off the plate.</summary>
        public const float ShowerDoorX = 13f, ShowerDoorY = 15f, ShowerDoorW = 41f, ShowerDoorH = 105f;
        /// <summary>
        /// The wintergarden's door back to the loft, in its left wall. The plate paints no door there yet (that wall is
        /// glass), so the door is at the wall until the plate is edited.
        /// </summary>
        public const float WinterDoorX = 0f, WinterDoorY = 15f, WinterDoorW = 31f, WinterDoorH = 37f;
        /// <summary>The rooftop's stair hut's door, at the roof's right end: the way down to the loft.</summary>
        public const float RoofHutX = 348f, RoofHutY = 15f, RoofHutW = 32f, RoofHutH = 48f;

        /// <summary>
        /// What a way in a room is: a door you walk through, a hole you go into, a ladder you climb, or the rope. Every
        /// way in a room is one of them: the way back to the house is the house's own door, but the room paints its end
        /// of it, so the room knows it as a door too.
        /// </summary>
        public static WayKind KindOf(RoomId room, RoomWay way)
        {
            switch (room)
            {
                case RoomId.Kitchen: return way == RoomWay.ToBackyard || way == RoomWay.Back ? WayKind.Door : WayKind.None;
                case RoomId.Backyard: return way == RoomWay.Back ? WayKind.Door : WayKind.None;
                case RoomId.Basement: return way == RoomWay.ToBunker ? WayKind.Hole : way == RoomWay.Back ? WayKind.Ladder : WayKind.None;
                case RoomId.Bunker: return way == RoomWay.Back ? WayKind.Rope : WayKind.None;
                case RoomId.Rooftop: case RoomId.ShowerBlock: case RoomId.Wintergarden: return way == RoomWay.Back ? WayKind.Door : WayKind.None;
                default: return WayKind.None;
            }
        }

        /// <summary>
        /// A way's opening in its room: the box it is in, which is what a tap has to land in and what cuts a soldier off as
        /// they walk into it. A ladder and a rope have no opening (the ladder is a ladder, the rope is the whole wall).
        /// </summary>
        public static Opening OpeningOf(RoomId room, RoomWay way)
        {
            switch (KindOf(room, way))
            {
                case WayKind.Door: return DoorOf(room, way);
                case WayKind.Hole: return new Opening(HoleX, HoleY, HoleW, HoleH, true);
                default: return new Opening(0f, 0f, 0f, 0f, false);
            }
        }

        /// <summary>A room's door: the box it is in, and which way you go through it.</summary>
        static Opening DoorOf(RoomId room, RoomWay way)
        {
            switch (room)
            {
                case RoomId.Kitchen:
                    // the back door, in the right wall, to the yard; the side door, in the left wall, to the den
                    return way == RoomWay.ToBackyard
                        ? new Opening(KitchenDoorX, KitchenDoorY, KitchenDoorW, KitchenDoorH, true)
                        : new Opening(KitchenSideX, KitchenSideY, KitchenSideW, KitchenSideH, false);
                case RoomId.Backyard:
                    return new Opening(YardDoorX, YardDoorY, YardDoorW, YardDoorH, false); // the house door, in the left wall
                case RoomId.Rooftop:
                    return new Opening(RoofHutX, RoofHutY, RoofHutW, RoofHutH, true);    // the stair hut's door, at the right end
                case RoomId.ShowerBlock:
                    return new Opening(ShowerDoorX, ShowerDoorY, ShowerDoorW, ShowerDoorH, false); // the loft's door, in the left wall
                case RoomId.Wintergarden:
                    return new Opening(WinterDoorX, WinterDoorY, WinterDoorW, WinterDoorH, false); // the loft's door, in the left wall
                default:
                    return new Opening(0f, 0f, 0f, 0f, false);
            }
        }

        /// <summary>
        /// The door in a room that leads to another room (the one that swings and swallows you), or None when the room
        /// has none: the way back to the house is the house's own door, not this room's.
        /// </summary>
        public static RoomWay DoorWayOf(RoomId room)
        {
            foreach (RoomWay way in Ways)
            {
                if (KindOf(room, way) != WayKind.Door) continue;
                Leads(room, way, out RoomId to, out RoomWay _);
                if (to != RoomId.None) return way;
            }
            return RoomWay.None;
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

        /// <summary>
        /// The bunker's rope (<see cref="RopeClimb"/>): where it hangs in the bunker, how high the hole in its roof is,
        /// and how fast you climb it. The plate paints the rope where <see cref="RopeX"/> is, under the hole.
        /// </summary>
        public const float RopeX = 351f;
        /// <summary>The top of the rope: the hole in the bunker's roof, so you climb out of sight.</summary>
        public const float RopeTopY = Height;
        /// <summary>How fast you climb the rope: the wintergarden stair's pace.</summary>
        public const float RopeSpeed = ClimbSpeed;

        /// <summary>
        /// Whether a way is climbed rather than walked through: the bunker's way out is its rope, up through the hole in
        /// B1's floor, and its way in is the same rope, climbed down.
        /// </summary>
        public static bool ByRope(RoomId room, RoomWay way) => room == RoomId.Bunker && way == RoomWay.Back;

        /// <summary>
        /// Where a tap at (<paramref name="x"/>, <paramref name="y"/>) in a room is: a way to use, or None (the floor:
        /// walk there). A door and a hole are used anywhere on them (the hole is up the wall, the door in its frame); a
        /// rope is used anywhere up it, from the floor to the hole; a ladder is used at its foot, where you stand to
        /// climb it.
        /// </summary>
        public static RoomWay WayAt(RoomId room, float x, float y)
        {
            foreach (RoomWay way in Ways)
            {
                if (!InRoom(room, way)) continue;
                Opening opening = OpeningOf(room, way);
                if (!opening.None)
                {
                    if (opening.Covers(x, y)) return way;
                    continue;
                }
                float wx = WayX(room, way);
                if (KindOf(room, way) == WayKind.Ladder)
                {
                    // the ladder: its foot, where you stand to climb it
                    if (x >= wx - LadderW / 2f && x <= wx + LadderW / 2f && y >= FloorY && y <= FloorY + LadderH) return way;
                    continue;
                }
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
