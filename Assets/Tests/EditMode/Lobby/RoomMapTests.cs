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
        public void EveryRoom_IsOneScreenAcross_AndOnlyTheWintergardenIsTaller()
        {
            foreach (RoomId r in System.Enum.GetValues(typeof(RoomId)))
            {
                if (r == RoomId.None) continue;
                Assert.AreEqual(RoomMap.Width, RoomMap.WidthOf(r), 0.01f, r.ToString());
                if (r == RoomId.Wintergarden) Assert.AreEqual(RoomMap.WinterHeight, RoomMap.HeightOf(r), 0.01f);
                else Assert.AreEqual(RoomMap.Height, RoomMap.HeightOf(r), 0.01f, r.ToString());
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
    }
}
