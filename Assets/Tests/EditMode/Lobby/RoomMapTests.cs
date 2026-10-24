using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The outer rooms: each its own screen behind a door, the ways in and out, and walking round one.</summary>
    public class RoomMapTests
    {
        static RoomWay Run(RoomWalker w, float seconds)
        {
            RoomWay arrived = RoomWay.None;
            for (float t = 0f; t < seconds; t += 1f / 60f)
            {
                RoomWay p = w.Step(1f / 60f);
                if (p != RoomWay.None) arrived = p;
            }
            return arrived;
        }

        [Test]
        public void EveryRoom_IsAScreenAndABitWide_AndOnlyTheWintergardenIsTaller()
        {
            foreach (RoomId r in System.Enum.GetValues(typeof(RoomId)))
            {
                if (r == RoomId.None) continue;
                // a room is a screen and a bit, so the player scrolls across it; the wintergarden is one screen across
                if (r == RoomId.Wintergarden) Assert.AreEqual(RoomMap.WinterWidth, RoomMap.WidthOf(r), 0.01f, r.ToString());
                else Assert.AreEqual(RoomMap.Width, RoomMap.WidthOf(r), 0.01f, r.ToString());
                if (r == RoomId.Wintergarden) Assert.AreEqual(RoomMap.WinterHeight, RoomMap.HeightOf(r), 0.01f);
                else Assert.AreEqual(RoomMap.Height, RoomMap.HeightOf(r), 0.01f, r.ToString());
            }
        }

        // docs/design/lobby/rooms.md, Paint it: a plate cut to the wrong shape squeezes the room. The paintings come
        // back at the 420 × 180 rooms' aspect (1260 × 540), so a plate cut to 320 × 180 stood everything in it up a
        // third taller — the moon a circle in the painting came out an upright oval in the room.
        [Test]
        public void EveryRoomsPainting_IsCutToTheRoomsOwnSize()
        {
            foreach (RoomId r in System.Enum.GetValues(typeof(RoomId)))
            {
                if (r == RoomId.None) continue;
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/Den", RoomMap.PaintingOf(r) + ".bytes");
                Assert.IsTrue(File.Exists(path), "missing painting: " + path);
                byte[] png = File.ReadAllBytes(path);
                // a PNG's header: the 8-byte signature, then the IHDR chunk's width and height, big-endian
                Assert.That(png.Length, Is.GreaterThan(24), path + ": not a PNG");
                Assert.AreEqual(0x89, png[0], path + ": not a PNG");
                int w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
                int h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
                Assert.AreEqual(RoomMap.WidthOf(r), w, 0.01f, r + ": the plate is the room's own width");
                Assert.AreEqual(RoomMap.HeightOf(r), h, 0.01f, r + ": the plate is the room's own height");
            }
        }

        [Test]
        public void EveryRoom_HasAPainting()
        {
            foreach (RoomId r in System.Enum.GetValues(typeof(RoomId)))
            {
                if (r == RoomId.None) continue;
                Assert.IsNotEmpty(RoomMap.PaintingOf(r), r.ToString());
            }
        }

        [Test]
        public void TheDoorsInTheHouse_OpenTheRooms_TheMapSays()
        {
            Assert.AreEqual(RoomId.Kitchen, RoomMap.RoomOf(RoomWay.SideDoor));
            Assert.AreEqual(RoomId.Basement, RoomMap.RoomOf(RoomWay.Trapdoor));
            Assert.AreEqual(RoomId.Rooftop, RoomMap.RoomOf(RoomWay.RooftopDoor));
            Assert.AreEqual(RoomId.ShowerBlock, RoomMap.RoomOf(RoomWay.ShowerDoor));
            Assert.AreEqual(RoomId.Wintergarden, RoomMap.RoomOf(RoomWay.WintergardenDoor));
            Assert.AreEqual(RoomId.None, RoomMap.RoomOf(RoomWay.None));
        }

        [Test]
        public void TheKitchen_LeadsOnToTheBackyard_AndTheYardBackToTheKitchen()
        {
            RoomMap.Leads(RoomId.Kitchen, RoomWay.ToBackyard, out RoomId to, out RoomWay by);
            Assert.AreEqual(RoomId.Backyard, to);
            Assert.AreEqual(RoomWay.Back, by, "you arrive in the yard by its own way back");
            RoomMap.Leads(RoomId.Backyard, RoomWay.Back, out to, out by);
            Assert.AreEqual(RoomId.Kitchen, to);
            Assert.AreEqual(RoomWay.ToBackyard, by, "and in the kitchen by the back door");
        }

        [Test]
        public void TheBasement_LeadsDownToTheBunker_AndTheBunkerBackUp()
        {
            RoomMap.Leads(RoomId.Basement, RoomWay.ToBunker, out RoomId to, out RoomWay by);
            Assert.AreEqual(RoomId.Bunker, to);
            Assert.AreEqual(RoomWay.Back, by);
            RoomMap.Leads(RoomId.Bunker, RoomWay.Back, out to, out by);
            Assert.AreEqual(RoomId.Basement, to);
            Assert.AreEqual(RoomWay.ToBunker, by);
        }

        [Test]
        public void TheWayBack_LeadsToTheHouse()
        {
            foreach (RoomId r in new[] { RoomId.Kitchen, RoomId.Basement, RoomId.Rooftop, RoomId.ShowerBlock, RoomId.Wintergarden })
            {
                RoomMap.Leads(r, RoomWay.Back, out RoomId to, out RoomWay by);
                Assert.AreEqual(RoomId.None, to, r + " goes back to the house, not to a room");
                Assert.AreNotEqual(RoomWay.None, by, r + " says which door in the house it came from");
            }
        }

        [Test]
        public void EveryRoom_HasAWayIn_AndTheWayIsWhereYouStandToUseIt()
        {
            foreach (RoomId r in System.Enum.GetValues(typeof(RoomId)))
            {
                if (r == RoomId.None) continue;
                bool any = false;
                foreach (RoomWay way in RoomMap.Ways)
                {
                    if (!RoomMap.InRoom(r, way)) continue;
                    any = true;
                    float x = RoomMap.WayX(r, way);
                    Assert.That(x, Is.InRange(RoomMap.MinX(r), RoomMap.MaxX(r)), r + " " + way);
                    Assert.AreEqual(way, RoomMap.WayAt(r, x, RoomMap.FloorY + 10f), r + " " + way + " is tapped where you stand to use it");
                }
                Assert.IsTrue(any, r + " has a way in");
            }
        }

        [Test]
        public void ATapOnTheFloorIsNotAWay()
        {
            Assert.AreEqual(RoomWay.None, RoomMap.WayAt(RoomId.Kitchen, 160f, RoomMap.FloorY + 10f));
            Assert.AreEqual(RoomWay.None, RoomMap.WayAt(RoomId.Basement, 160f, RoomMap.FloorY + 10f));
        }

        [Test]
        public void WalkingToAWay_UsesItOnceOnArriving()
        {
            var w = new RoomWalker(RoomId.Kitchen, 160f);
            w.Go(RoomWay.Back);
            Assert.AreEqual(RoomWay.Back, Run(w, 6f));
            Assert.AreEqual(RoomWay.None, Run(w, 1f), "only once");
        }

        // a tap on a door walks you to it, and you go through on arriving: not at once
        [Test]
        public void GoingToAWay_WalksThereFirst_AndOnlyThenUsesIt()
        {
            var w = new RoomWalker(RoomId.Kitchen, 160f);
            float door = RoomMap.WayX(RoomId.Kitchen, RoomWay.Back);
            w.Go(RoomWay.Back);
            Assert.AreEqual(RoomWay.None, Run(w, 0.5f), "still on the way");
            Assert.AreEqual(RoomWay.Back, Run(w, 6f), "through it on arriving");
            Assert.AreEqual(door, w.X, 0.01f, "and standing at the door");
        }

        [Test]
        public void AWayYouAreAlreadyAt_IsUsedAtOnce()
        {
            var w = new RoomWalker(RoomId.Kitchen, RoomMap.WayX(RoomId.Kitchen, RoomWay.Back));
            w.Go(RoomWay.Back);
            Assert.AreEqual(RoomWay.Back, Run(w, 0.1f));
        }

        [Test]
        public void TheRooms_AreWiderThanAScreen_AndYouCanWalkTheWholeLength()
        {
            Assert.Greater(RoomMap.WidthOf(RoomId.Kitchen), 320f, "a screen and a bit: the player scrolls across it");
            foreach (RoomId r in System.Enum.GetValues(typeof(RoomId)))
            {
                if (r == RoomId.None) continue;
                Assert.That(RoomMap.MaxX(r), Is.GreaterThan(RoomMap.MinX(r)), r.ToString() + ": room to walk in");
                Assert.That(RoomMap.MaxX(r), Is.LessThanOrEqualTo(RoomMap.WidthOf(r) - 8f), r.ToString() + ": short of the far wall");
                var w = new RoomWalker(r, RoomMap.MinX(r));
                w.WalkTo(RoomMap.MaxX(r));
                Run(w, 10f);
                Assert.AreEqual(RoomMap.MaxX(r), w.X, 0.01f, r + ": the whole way across");
            }
        }

        [Test]
        public void WalkingOffSomewhereElse_DropsTheErrand()
        {
            var w = new RoomWalker(RoomId.Kitchen, 160f);
            w.Go(RoomWay.Back);
            Run(w, 0.5f);
            w.WalkTo(60f);
            Assert.AreEqual(RoomWay.None, Run(w, 6f));
        }

        [Test]
        public void WalkingByHand_StopsAtTheWalls()
        {
            var w = new RoomWalker(RoomId.Kitchen, 160f);
            w.Walk(-1);
            Run(w, 5f);
            Assert.AreEqual(RoomMap.MinX(RoomId.Kitchen), w.X, 0.01f);
            w.Walk(1);
            Run(w, 6f);
            Assert.AreEqual(RoomMap.MaxX(RoomId.Kitchen), w.X, 0.01f);
        }

        [Test]
        public void TheWayYouStandAt_IsTheOneYouCanUse()
        {
            var w = new RoomWalker(RoomId.Kitchen, 160f);
            Assert.AreEqual(RoomWay.None, w.WayHere);
            w.Place(RoomId.Kitchen, RoomMap.WayX(RoomId.Kitchen, RoomWay.Back));
            Assert.AreEqual(RoomWay.Back, w.WayHere);
            w.Place(RoomId.Kitchen, RoomMap.WayX(RoomId.Kitchen, RoomWay.Back) + RoomMap.WayReach + 2f);
            Assert.AreEqual(RoomWay.None, w.WayHere, "out of reach");
        }

        // docs/design/lobby.md, The outer rooms: the wintergarden's stair climbs to the rose at the top
        [Test]
        public void TheWintergardenStair_ClimbsToTheLanding_AndOnlyTheStairGoesUp()
        {
            var w = new RoomWalker(RoomId.Wintergarden, RoomMap.StairX);
            Assert.IsTrue(w.CanClimb(1), "the stair is here");
            Assert.IsTrue(w.Climb(1));
            bool climbed = false;
            for (int i = 0; i < 60 * 20 && !climbed; i++)
            {
                w.Step(1f / 60f);
                if (w.Climbing) climbed = true;
            }
            Assert.IsTrue(climbed);
            Run(w, 20f);
            Assert.AreEqual(RoomMap.LandingY, w.Y, 0.01f, "up at the landing");
            Assert.IsFalse(w.CanClimb(1), "the landing is the top");
        }

        [Test]
        public void TheStair_IsOnlyInTheWintergarden()
        {
            var w = new RoomWalker(RoomId.Kitchen, RoomMap.StairX);
            Assert.IsFalse(w.CanClimb(1));
            Assert.IsFalse(w.Climb(1));
        }

        [Test]
        public void TheStair_CanBeClimbedDownAgain()
        {
            var w = new RoomWalker(RoomId.Wintergarden, RoomMap.StairX);
            w.Climb(1);
            Run(w, 20f);
            Assert.AreEqual(RoomMap.LandingY, w.Y, 0.01f);
            Assert.IsTrue(w.CanClimb(-1));
            Assert.IsTrue(w.Climb(-1));
            Run(w, 20f);
            Assert.AreEqual(RoomMap.FloorY, w.Y, 0.01f, "back down at the floor");
        }

        [Test]
        public void TheStair_IsNotUpAnyOldLadder()
        {
            var w = new RoomWalker(RoomId.Wintergarden, 60f);
            Assert.IsFalse(w.CanClimb(1), "not by the stair");
            Assert.IsFalse(w.Climb(1));
        }

        // docs/design/lobby.md, Den 2.0: Scale: everything is drawn for the 22 px soldier
        [Test]
        public void TheRooms_AreDrawnToTheSoldiersScale()
        {
            foreach (RoomId r in System.Enum.GetValues(typeof(RoomId)))
            {
                if (r == RoomId.None) continue;
                // a room's floor is up from its bottom, so there's room to stand in it
                Assert.That(RoomMap.HeightOf(r) - RoomMap.FloorY, Is.GreaterThan(RoomMap.SoldierHeight), r + ": room to stand in");
                Assert.That(RoomMap.HeightOf(r), Is.GreaterThan(RoomMap.SoldierHeight * 2f), r + ": a room with headroom");
            }
        }

        // The ways in the outer rooms: what each is, and the box a tap has to land in. A hole is up the wall, so a tap
        // on it has to count; a ladder is used at its foot.
        [Test]
        public void EachWay_KnowsWhatItIs()
        {
            Assert.AreEqual(WayKind.Door, RoomMap.KindOf(RoomId.Kitchen, RoomWay.Back));
            Assert.AreEqual(WayKind.Door, RoomMap.KindOf(RoomId.Kitchen, RoomWay.ToBackyard));
            Assert.AreEqual(WayKind.Door, RoomMap.KindOf(RoomId.Backyard, RoomWay.Back));
            Assert.AreEqual(WayKind.Ladder, RoomMap.KindOf(RoomId.Basement, RoomWay.Back));
            Assert.AreEqual(WayKind.Hole, RoomMap.KindOf(RoomId.Basement, RoomWay.ToBunker));
            Assert.AreEqual(WayKind.Rope, RoomMap.KindOf(RoomId.Bunker, RoomWay.Back));
            Assert.AreEqual(WayKind.Door, RoomMap.KindOf(RoomId.Rooftop, RoomWay.Back));
            Assert.AreEqual(WayKind.Door, RoomMap.KindOf(RoomId.ShowerBlock, RoomWay.Back));
            Assert.AreEqual(WayKind.Door, RoomMap.KindOf(RoomId.Wintergarden, RoomWay.Back));
            Assert.AreEqual(WayKind.None, RoomMap.KindOf(RoomId.Kitchen, RoomWay.None));
        }

        // Every way in a room is a thing you can use: a door, a hole, a ladder or a rope. Nothing is left unnamed.
        [Test]
        public void EveryWayInARoom_KnowsWhatItIs()
        {
            foreach (RoomWay way in RoomMap.Ways)
            {
                foreach (RoomId room in System.Enum.GetValues(typeof(RoomId)))
                {
                    if (!RoomMap.InRoom(room, way)) continue;
                    Assert.AreNotEqual(WayKind.None, RoomMap.KindOf(room, way), room + "/" + way);
                }
            }
        }

        // A door and a hole have an opening: the box a tap lands in, and what cuts a soldier off as they walk into it.
        // A ladder and a rope are climbed, so there is nothing to walk into.
        [Test]
        public void ADoorAndAHole_HaveAnOpening_AndALadderAndARopeHaveNone()
        {
            foreach (RoomWay way in RoomMap.Ways)
            {
                foreach (RoomId room in System.Enum.GetValues(typeof(RoomId)))
                {
                    if (!RoomMap.InRoom(room, way)) continue;
                    Opening opening = RoomMap.OpeningOf(room, way);
                    WayKind kind = RoomMap.KindOf(room, way);
                    if (kind == WayKind.Door || kind == WayKind.Hole) Assert.IsFalse(opening.None, room + "/" + way + ": an opening");
                    else Assert.IsTrue(opening.None, room + "/" + way + ": you climb it, so there is no opening");
                }
            }
        }

        // An opening is where you stand to use its way: the doorway is where the plate paints it.
        [Test]
        public void AnOpening_IsWhereYouStandToUseItsWay()
        {
            foreach (RoomWay way in RoomMap.Ways)
            {
                foreach (RoomId room in System.Enum.GetValues(typeof(RoomId)))
                {
                    if (!RoomMap.InRoom(room, way)) continue;
                    Opening opening = RoomMap.OpeningOf(room, way);
                    if (opening.None) continue;
                    Assert.IsTrue(opening.Covers(RoomMap.WayX(room, way), RoomMap.FloorY + 10f),
                        room + "/" + way + ": you stand in the opening to use it");
                }
            }
        }

        // Every door swings, whichever way it leads: the door to another room, and the door back to the house, which
        // is the house's own door seen from the room. A hole is gone into, a ladder and a rope are climbed: nothing
        // swings but a door.
        [Test]
        public void EveryDoor_Swings_WhicheverWayItLeads()
        {
            Assert.IsTrue(RoomMap.IsDoor(RoomId.Kitchen, RoomWay.Back), "the kitchen's side door, back to the den");
            Assert.IsTrue(RoomMap.IsDoor(RoomId.Kitchen, RoomWay.ToBackyard), "the kitchen's back door, to the yard");
            Assert.IsTrue(RoomMap.IsDoor(RoomId.Backyard, RoomWay.Back), "the yard's house door, to the kitchen");
            Assert.IsTrue(RoomMap.IsDoor(RoomId.Rooftop, RoomWay.Back), "the stair hut's door, back to the loft");
            Assert.IsTrue(RoomMap.IsDoor(RoomId.ShowerBlock, RoomWay.Back), "the shower block's door, back to the loft");
            Assert.IsTrue(RoomMap.IsDoor(RoomId.Wintergarden, RoomWay.Back), "the wintergarden's door, back to the loft");
            Assert.IsFalse(RoomMap.IsDoor(RoomId.Basement, RoomWay.Back), "the ladder is climbed");
            Assert.IsFalse(RoomMap.IsDoor(RoomId.Basement, RoomWay.ToBunker), "the hole is gone into");
            Assert.IsFalse(RoomMap.IsDoor(RoomId.Bunker, RoomWay.Back), "the rope is climbed");
        }

        // The doors in a room: each way in it that is a door. The kitchen has two (its side door back to the den and
        // its back door to the yard); the yard one; the basement and the bunker none.
        [Test]
        public void TheDoorsInARoom_AreItsDoorWays()
        {
            Assert.AreEqual(new List<RoomWay> { RoomWay.Back, RoomWay.ToBackyard }, RoomMap.DoorsOf(RoomId.Kitchen));
            Assert.AreEqual(new List<RoomWay> { RoomWay.Back }, RoomMap.DoorsOf(RoomId.Backyard));
            Assert.AreEqual(new List<RoomWay>(), RoomMap.DoorsOf(RoomId.Basement));
            Assert.AreEqual(new List<RoomWay>(), RoomMap.DoorsOf(RoomId.Bunker));
            Assert.AreEqual(new List<RoomWay> { RoomWay.Back }, RoomMap.DoorsOf(RoomId.Rooftop));
            Assert.AreEqual(new List<RoomWay> { RoomWay.Back }, RoomMap.DoorsOf(RoomId.ShowerBlock));
            Assert.AreEqual(new List<RoomWay> { RoomWay.Back }, RoomMap.DoorsOf(RoomId.Wintergarden));
        }

        // A door you can walk through has a box to walk into, wherever it leads: the one back to the house too, so the
        // room can draw it and you can step into its dark.
        [Test]
        public void EveryDoorHasABox_ToWalkInto_WhicheverWayItLeads()
        {
            foreach (RoomWay way in RoomMap.DoorsOf(RoomId.Kitchen))
                Assert.IsFalse(RoomMap.OpeningOf(RoomId.Kitchen, way).None, way + ": a door to walk into");
            foreach (RoomId room in new[] { RoomId.Backyard, RoomId.Rooftop, RoomId.ShowerBlock, RoomId.Wintergarden })
                Assert.IsFalse(RoomMap.OpeningOf(room, RoomWay.Back).None, room + ": the door back to the house");
        }

        // The hole in the basement's wall: a tap on it, high up the wall, is the way down to the bunker.
        [Test]
        public void TheBasementsHole_IsTappedOnTheWall_AndTheLadderAtItsFoot()
        {
            Opening hole = RoomMap.OpeningOf(RoomId.Basement, RoomWay.ToBunker);
            Assert.AreEqual(RoomWay.ToBunker, RoomMap.WayAt(RoomId.Basement, hole.X + 4f, hole.Y + hole.H - 4f), "the hole, up the wall");
            Assert.AreEqual(RoomWay.None, RoomMap.WayAt(RoomId.Basement, hole.X - 8f, hole.Y + 4f), "the wall, beside the hole");

            // the ladder up to the den: its foot, where you stand to climb it
            float ladder = RoomMap.WayX(RoomId.Basement, RoomWay.Back);
            Assert.AreEqual(RoomWay.Back, RoomMap.WayAt(RoomId.Basement, ladder, RoomMap.FloorY + 4f), "the ladder's foot");
            Assert.AreEqual(RoomWay.None, RoomMap.WayAt(RoomId.Basement, ladder, RoomMap.FloorY + RoomMap.LadderH + 10f), "the wall above the ladder");
        }

        // The openings are where the plates paint the doors: each box was measured off its plate, so a tap on the
        // painted door counts and you stand in the painted doorway.
        [Test]
        public void TheOpenings_AreWhereThePlatesPaintTheDoors()
        {
            Assert.AreEqual(367f, RoomMap.HoleX, "the hole in the basement's right wall");
            Assert.AreEqual(395f, RoomMap.HoleX + RoomMap.HoleW, "the hole's other edge");
            Assert.AreEqual(45f, RoomMap.YardDoorX, "the house door in the yard's left wall");
            Assert.AreEqual(77f, RoomMap.YardDoorX + RoomMap.YardDoorW, "the house door's other edge");
            Assert.AreEqual(13f, RoomMap.ShowerDoorX, "the shower block's door, in its left wall");
            Assert.AreEqual(54f, RoomMap.ShowerDoorX + RoomMap.ShowerDoorW, "the shower block's door's other edge");
            Assert.AreEqual(351f, RoomMap.RopeX, "the rope in the bunker, under the hole in its roof");
            Assert.AreEqual(364f, RoomMap.WayX(RoomId.Rooftop, RoomWay.Back), "the stair hut's door, at the roof's right end");
        }

        // An opening is in the room it is in, and it reaches the floor, so you can walk into it and stand in it.
        [Test]
        public void AnOpening_IsInItsRoom_AndReachesTheFloor()
        {
            foreach (RoomWay way in RoomMap.Ways)
            {
                foreach (RoomId room in System.Enum.GetValues(typeof(RoomId)))
                {
                    if (!RoomMap.InRoom(room, way)) continue;
                    Opening opening = RoomMap.OpeningOf(room, way);
                    if (opening.None) continue;
                    Assert.That(opening.X, Is.GreaterThanOrEqualTo(0f), room + "/" + way + ": the opening is in the room");
                    Assert.That(opening.Right, Is.LessThanOrEqualTo(RoomMap.WidthOf(room)), room + "/" + way + ": the opening is in the room");
                    Assert.That(opening.Y, Is.LessThanOrEqualTo(RoomMap.FloorY + 2f), room + "/" + way + ": the opening reaches the floor");
                }
            }
        }

        // A step through a door starts in the doorway, where you stand, and ends past its far edge: you are whole in
        // the opening while the door creaks open, and the frame's far edge cuts you off as you go through.
        [Test]
        public void AStepThroughADoor_StartsInTheDoorway_AndEndsPastItsFarEdge()
        {
            Opening kitchen = RoomMap.OpeningOf(RoomId.Kitchen, RoomWay.ToBackyard);
            Assert.IsTrue(kitchen.ToRight, "the kitchen's back door is in its right wall");
            Assert.That(kitchen.Sill, Is.InRange(kitchen.X, kitchen.Right), "the step starts in the doorway, where you stand");
            Assert.That(kitchen.To, Is.GreaterThan(kitchen.Right), "the step ends past the door");

            Opening yard = RoomMap.OpeningOf(RoomId.Backyard, RoomWay.Back);
            Assert.IsFalse(yard.ToRight, "the yard's house door is in its left wall");
            Assert.That(yard.Sill, Is.InRange(yard.X, yard.Right), "the step starts in the doorway");
            Assert.That(yard.To, Is.LessThan(yard.X), "the step ends past the door");
        }

        // Every door's step is like that: it starts in the doorway and ends past the far edge, whichever way the door
        // leads. A step that started outside the opening would draw the soldier beside the door, and cut in half.
        [Test]
        public void EveryDoorsStep_StartsInItsDoorway_AndEndsPastItsFarEdge()
        {
            foreach (RoomId room in System.Enum.GetValues(typeof(RoomId)))
            {
                if (room == RoomId.None) continue;
                foreach (RoomWay way in RoomMap.DoorsOf(room))
                {
                    Opening opening = RoomMap.OpeningOf(room, way);
                    Assert.That(opening.Sill, Is.InRange(opening.X, opening.Right), room + "/" + way + ": the step starts in the doorway");
                    if (opening.ToRight)
                        Assert.That(opening.To, Is.GreaterThan(opening.Right), room + "/" + way + ": the step ends past the far edge");
                    else
                        Assert.That(opening.To, Is.LessThan(opening.X), room + "/" + way + ": the step ends past the far edge");
                }
            }
        }
    }
}
