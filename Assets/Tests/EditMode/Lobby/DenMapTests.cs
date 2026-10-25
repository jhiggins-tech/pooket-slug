using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The den's map and walking round it: along the floors, up and down the ladder, and using a place on arrival.</summary>
    public class DenMapTests
    {
        static DenPlace Run(DenWalker w, float seconds)
        {
            DenPlace arrived = DenPlace.None;
            for (float t = 0f; t < seconds; t += 1f / 60f)
            {
                DenPlace p = w.Step(1f / 60f);
                if (p != DenPlace.None) arrived = p;
            }
            return arrived;
        }

        [Test]
        public void WalkingAlongAFloor_StopsWhereItWasSent()
        {
            var w = new DenWalker(DenFloor.Ground, 40f);
            w.WalkTo(DenFloor.Ground, 120f);
            Run(w, 3f);
            Assert.AreEqual(120f, w.X, 0.01f);
            Assert.AreEqual(DenFloor.Ground, w.Floor);
            Assert.IsTrue(w.Standing);
            Assert.IsFalse(w.FaceLeft, "walked right");
        }

        [Test]
        public void ToTheLoft_GoesByTheLadder_AndTheCameraHeightFollows()
        {
            var w = new DenWalker(DenFloor.Ground, 40f);
            w.Go(DenPlace.Gym);
            bool climbed = false;
            for (int i = 0; i < 60 * 12 && !w.Standing; i++)
            {
                w.Step(1f / 60f);
                if (w.Climbing)
                {
                    climbed = true;
                    Assert.AreEqual(DenMap.LadderX, w.X, 0.01f, "only on the ladder");
                }
            }
            Run(w, 0.5f); // the last step onto the spot
            Assert.IsTrue(climbed);
            Assert.AreEqual(DenFloor.Loft, w.Floor);
            Assert.AreEqual(DenMap.FloorY(DenFloor.Loft), w.Y, 0.01f);
            Assert.AreEqual(DenMap.XOf(DenPlace.Gym), w.X, 0.01f);
        }

        [Test]
        public void APlace_IsUsedOnceOnArriving()
        {
            var w = new DenWalker(DenFloor.Ground, 40f);
            w.Go(DenPlace.Board);
            Assert.AreEqual(DenPlace.Board, Run(w, 6f));
            Assert.AreEqual(DenPlace.None, Run(w, 1f), "only once");
        }

        [Test]
        public void WalkingOffSomewhereElse_DropsTheErrand()
        {
            var w = new DenWalker(DenFloor.Ground, 40f);
            w.Go(DenPlace.Board);
            Run(w, 0.5f);
            w.WalkTo(DenFloor.Ground, 60f);
            Assert.AreEqual(DenPlace.None, Run(w, 6f));
        }

        [Test]
        public void ByHand_WalkingStopsAtTheWalls_AndClimbingNeedsTheLadder()
        {
            var w = new DenWalker(DenFloor.Ground, 40f);
            w.Walk(-1);
            Run(w, 3f);
            Assert.AreEqual(DenMap.MinX(DenFloor.Ground), w.X, 0.01f);
            Assert.IsFalse(w.Climb(1), "no ladder here");
            w.Place(DenFloor.Ground, DenMap.LadderX + 5f);
            Assert.IsFalse(w.Climb(-1), "nothing below the ground floor");
            Assert.IsTrue(w.Climb(1));
            Run(w, 4f);
            Assert.AreEqual(DenFloor.Upper, w.Floor);
            Assert.IsTrue(w.Climb(1));
            Run(w, 4f);
            Assert.AreEqual(DenFloor.Loft, w.Floor);
            Assert.IsFalse(w.Climb(1), "the hatch's ladder stops at the loft");
            w.Place(DenFloor.Loft, DenMap.BunkLadderX);
            Assert.IsTrue(w.Climb(1), "the bunks' ladder");
            Run(w, 4f);
            Assert.AreEqual(DenFloor.Bunks, w.Floor);
            Assert.IsFalse(w.Climb(1), "the bunks are the top");
        }

        [Test]
        public void CanClimb_AgreesWithClimb_AtBothLaddersAndTheReachsEdge()
        {
            foreach ((DenFloor floor, float ladder) in new[] { (DenFloor.Ground, DenMap.LadderX), (DenFloor.Loft, DenMap.BunkLadderX) })
                foreach (float off in new[] { 0f, DenMap.LadderReach, DenMap.LadderReach + 1f, -DenMap.LadderReach - 1f })
                {
                    var w = new DenWalker(floor, ladder + off);
                    bool can = w.CanClimb(1);
                    Assert.AreEqual(System.Math.Abs(off) <= DenMap.LadderReach, can, $"{floor}, {off} from the ladder");
                    Assert.AreEqual(can, w.Climb(1), "the flow's check and the climb agree");
                }
            Assert.That(DenMap.LadderTapReach, Is.GreaterThanOrEqualTo(DenMap.LadderReach), "a tap on the ladder is at least as wide as standing by it");
        }

        [Test]
        public void FromTheBunks_DownBothLadders_ToTheCouch()
        {
            var w = new DenWalker(DenFloor.Bunks, 300f);
            w.Go(DenPlace.Couch);
            bool onBunkLadder = false, onDenLadder = false;
            DenPlace reached = DenPlace.None;
            for (int i = 0; i < 60 * 20 && reached == DenPlace.None; i++)
            {
                reached = w.Step(1f / 60f);
                if (w.Climbing && w.X == DenMap.BunkLadderX) onBunkLadder = true;
                if (w.Climbing && w.X == DenMap.LadderX) onDenLadder = true;
            }
            Assert.AreEqual(DenPlace.Couch, reached);
            Assert.IsTrue(onBunkLadder && onDenLadder, "down the bunks' ladder, along the loft, then down the den's");
            Assert.AreEqual(DenFloor.Ground, w.Floor);
        }

        [Test]
        public void TurningBackOnTheBunksLadder_ClimbsDownItBeforeHeadingForTheOtherOne()
        {
            var w = new DenWalker(DenFloor.Loft, DenMap.BunkLadderX);
            w.WalkTo(DenFloor.Bunks, 200f);
            Run(w, 0.4f);
            Assert.IsTrue(w.Climbing);
            w.WalkTo(DenFloor.Ground, 60f);
            for (int i = 0; i < 60 * 15; i++)
            {
                w.Step(1f / 60f);
                if (w.Climbing && w.Y < DenMap.FloorY(DenFloor.Loft)) Assert.AreEqual(DenMap.LadderX, w.X, "below the loft only by the den's ladder");
            }
            Assert.AreEqual(DenFloor.Ground, w.Floor);
            Assert.AreEqual(60f, w.X, 0.01f);
        }

        // docs/design/lobby.md, Den 2.0, Scale: everything is drawn for the 22 px soldier, doors about 1.7 times him
        [Test]
        public void TheLoft_IsDrawnToTheSoldiersScale()
        {
            foreach (DenMap.Fixture d in DenMap.Doors)
                if (d.Floor >= DenFloor.Loft) Assert.That(d.Height / DenMap.SoldierHeight, Is.InRange(1.5f, 1.9f), d.Name);
            foreach (DenMap.Fixture r in DenMap.Rungs)
                if (r.Floor >= DenFloor.Loft) Assert.That(r.Height, Is.InRange(3f, 8f), r.Name + ": a rung a step apart");
            foreach (DenFloor f in System.Enum.GetValues(typeof(DenFloor)))
                Assert.That(DenMap.Headroom(f), Is.GreaterThanOrEqualTo(DenMap.SoldierHeight), f + ": room to stand");
        }

        [Test, Ignore("The den's painting predates the scale rule (its doors are about 2.8 times the soldier), and the owner kept it at its size (den2-den-rescale)")]
        public void TheDen_IsDrawnToTheSoldiersScale()
        {
            foreach (DenMap.Fixture d in DenMap.Doors)
                if (d.Floor < DenFloor.Loft) Assert.That(d.Height / DenMap.SoldierHeight, Is.InRange(1.5f, 1.9f), d.Name);
            foreach (DenMap.Fixture r in DenMap.Rungs)
                if (r.Floor < DenFloor.Loft) Assert.That(r.Height, Is.InRange(3f, 8f), r.Name);
        }

        [Test]
        public void TurningBackHalfwayUpTheLadder_ClimbsBackDown()
        {
            var w = new DenWalker(DenFloor.Ground, DenMap.LadderX);
            w.WalkTo(DenFloor.Upper, DenMap.LadderX);
            Run(w, 0.5f);
            Assert.IsTrue(w.Climbing);
            w.WalkTo(DenFloor.Ground, 100f);
            Run(w, 6f);
            Assert.AreEqual(DenFloor.Ground, w.Floor);
            Assert.AreEqual(100f, w.X, 0.01f);
        }

        [Test]
        public void EveryPlace_IsOnItsFloor_AndOnlyTheOnesThatDoNothingYetTalk()
        {
            foreach (DenPlace p in System.Enum.GetValues(typeof(DenPlace)))
            {
                if (p == DenPlace.None) continue;
                DenFloor f = DenMap.FloorOf(p);
                float x = DenMap.XOf(p);
                Assert.That(x, Is.InRange(DenMap.MinX(f), DenMap.MaxX(f)), p.ToString());
                bool does = p == DenPlace.Couch || p == DenPlace.Board || p == DenPlace.Gym || p == DenPlace.Trophies || p == DenPlace.RollerDoor
                    || p == DenPlace.SideDoor || p == DenPlace.Trapdoor || p == DenPlace.Rooftop || p == DenPlace.Showers || p == DenPlace.Wintergarden;
                Assert.AreEqual(does, DenMap.Note(p) == null, p.ToString());
                // the trapdoor is a hatch in the floor itself, so it's tapped down in the floor; the rest are tapped
                // where you stand to use them
                float tapY = p == DenPlace.Trapdoor ? DenMap.FloorY(f) - 5f : DenMap.FloorY(f) + 10f;
                Assert.AreEqual(p, DenMap.PlaceAt(x, tapY), p.ToString() + " is tapped where you use it");
            }
        }

        [Test]
        public void ATapPicksTheFloorItsOn()
        {
            Assert.AreEqual(DenFloor.Ground, DenMap.FloorAt(40f));
            Assert.AreEqual(DenFloor.Upper, DenMap.FloorAt(140f));
            Assert.AreEqual(DenFloor.Loft, DenMap.FloorAt(220f));
            Assert.AreEqual(DenFloor.Bunks, DenMap.FloorAt(270f));
            Assert.AreEqual(DenPlace.None, DenMap.PlaceAt(200f, 20f), "the floor by the ladder: just walk there");
        }

        [Test]
        public void ATapOnTheLadderOrHatch_AboveTheUpperFloor_ClimbsToTheLoft()
        {
            foreach (float y in new[] { 135f, 150f, 170f, 179f })
            {
                Assert.AreEqual(DenFloor.Loft, DenMap.WalkTarget(DenMap.LadderX + 10f, y, DenFloor.Upper, out float x), "y " + y);
                Assert.AreEqual(DenMap.LadderX, x, "up the ladder");
                Assert.AreEqual(DenPlace.None, DenMap.PlaceAt(DenMap.LadderX, y), "nothing else is there");
            }
            Assert.AreEqual(DenFloor.Upper, DenMap.WalkTarget(DenMap.LadderX, 122f, DenFloor.Upper, out float foot), "its foot, on the upper floor: just walk there");
            Assert.AreEqual(DenMap.LadderX, foot);
            Assert.AreEqual(DenFloor.Upper, DenMap.WalkTarget(DenMap.LadderX + 40f, 160f, DenFloor.Upper, out float wall), "the wall beside it");
            Assert.AreEqual(DenMap.LadderX + 40f, wall);
            Assert.AreEqual(DenFloor.Ground, DenMap.WalkTarget(DenMap.LadderX, 40f, DenFloor.Upper, out _), "the ground floor's ladder: walk to it");
            Assert.AreEqual(DenFloor.Loft, DenMap.WalkTarget(300f, 200f, DenFloor.Upper, out _), "the loft itself, as before");

            // and walking there from the ground floor takes you all the way up
            DenFloor floor = DenMap.WalkTarget(DenMap.LadderX, 160f, DenFloor.Ground, out float to);
            var w = new DenWalker(DenFloor.Ground, 60f);
            w.WalkTo(floor, to);
            Run(w, 10f);
            Assert.AreEqual(DenFloor.Loft, w.Floor);
            Assert.IsTrue(w.Standing);
        }

        [Test]
        public void InTheLoft_ATapOnTheHatchOrJustUnderIt_ClimbsDown()
        {
            foreach (DenFloor on in new[] { DenFloor.Loft, DenFloor.Bunks })
                foreach (float y in new[] { 150f, 170f, 179f, 185f, DenMap.FloorY(DenFloor.Loft) + 5f })
                {
                    Assert.AreEqual(DenFloor.Upper, DenMap.WalkTarget(DenMap.LadderX - 10f, y, on, out float x), on + " y " + y);
                    Assert.AreEqual(DenMap.LadderX, x, "down the ladder");
                }
            Assert.AreEqual(DenFloor.Loft, DenMap.WalkTarget(DenMap.LadderX, DenMap.FloorY(DenFloor.Loft) + 30f, DenFloor.Loft, out _), "above the hatch: the loft");
            Assert.AreEqual(DenFloor.Loft, DenMap.WalkTarget(DenMap.LadderX + 60f, 185f, DenFloor.Loft, out _), "the loft floor beside it");
            Assert.AreEqual(DenFloor.Loft, DenMap.WalkTarget(DenMap.LadderX, 185f, DenFloor.Upper, out _), "from below, the hatch still goes up");

            // and walking there from the loft takes you all the way down
            DenFloor floor = DenMap.WalkTarget(DenMap.LadderX, 185f, DenFloor.Loft, out float to);
            var w = new DenWalker(DenFloor.Loft, 300f);
            w.WalkTo(floor, to);
            Run(w, 10f);
            Assert.AreEqual(DenFloor.Upper, w.Floor);
            Assert.IsTrue(w.Standing);
        }

        [Test]
        public void UpOrDownAtADoor_GoesThroughIt_TheWayItLeads()
        {
            foreach (DenPlace p in System.Enum.GetValues(typeof(DenPlace)))
            {
                int way = DenMap.DoorWay(p);
                bool door = DenMap.RoomOf(p) != RoomWay.None || p == DenPlace.RollerDoor;
                Assert.AreEqual(door, way != 0, p + " is a door exactly when it opens a room or the tabk bay");
                if (way == 0) continue;
                var w = new DenWalker(DenMap.FloorOf(p), DenMap.XOf(p));
                Assert.AreEqual(p, w.DoorHere(way), p + ": the way it leads goes through it");
                Assert.AreEqual(DenPlace.None, w.DoorHere(-way), p + ": the other way doesn't");
                Assert.AreEqual(DenPlace.None, w.DoorHere(0));
                Assert.AreEqual(p, w.PlaceHere, p + ": and jump still uses it");
            }
            Assert.AreEqual(-1, DenMap.DoorWay(DenPlace.Trapdoor), "the trapdoor is in the floor: down");
            Assert.AreEqual(1, DenMap.DoorWay(DenPlace.RollerDoor), "the rest you walk in through: up");
            Assert.AreEqual(DenPlace.None, new DenWalker(DenFloor.Ground, 124f).DoorHere(1), "the couch is no door");
            Assert.AreEqual(DenPlace.None, new DenWalker(DenFloor.Upper, DenMap.XOf(DenPlace.Board)).DoorHere(1), "nor is the board");
        }

        [Test]
        public void AtALadderAndADoorAtOnce_TheLadderComesFirst()
        {
            // the bunks' ladder stands just short of the shower block's door
            float x = DenMap.BunkLadderX + DenMap.LadderReach;
            Assert.AreEqual(DenPlace.Showers, DenMap.DoorAt(DenFloor.Loft, x, 1), "the shower door is there");
            var w = new DenWalker(DenFloor.Loft, x);
            Assert.IsTrue(w.CanClimb(1));
            Assert.AreEqual(DenPlace.None, w.DoorHere(1), "up climbs to the bunks");
            Assert.AreEqual(DenPlace.None, new DenWalker(DenFloor.Loft, DenMap.LadderX).DoorHere(-1), "down the hatch is the ladder's");
            w.WalkTo(DenFloor.Bunks, 100f);
            Run(w, 0.3f);
            Assert.IsTrue(w.Climbing);
            Assert.AreEqual(DenPlace.None, w.DoorHere(1), "no door from the ladder");
        }

        [Test]
        public void ATapOnTheBoard_OpensIt_EvenWithKcajInFrontOfIt()
        {
            float x = DenMap.XOf(DenPlace.Board), y = DenMap.FloorY(DenFloor.Upper) + 10f;
            DenPlace board = DenMap.PlaceAt(x, y);
            Assert.AreEqual(DenPlace.Board, board, "kcaj's station is the board's middle");
            Assert.AreEqual(DenTap.Place, DenMap.TapOn(true, board), "the board wins over kcaj");
            Assert.AreEqual(DenTap.Place, DenMap.TapOn(false, board));
            Assert.AreEqual(DenTap.Crewmate, DenMap.TapOn(true, DenPlace.None), "kcaj anywhere off the board swaps to him");
            Assert.AreEqual(DenTap.Crewmate, DenMap.TapOn(true, DenPlace.RollerDoor), "a crewmate in front of any other place is who you tap");
            Assert.AreEqual(DenTap.Place, DenMap.TapOn(false, DenPlace.RollerDoor));
            Assert.AreEqual(DenTap.Floor, DenMap.TapOn(false, DenPlace.None));
        }

        [Test]
        public void TheTrapdoorsHatch_IsWhereItWasDrawn()
        {
            var a = DenMap.AreaOf(DenPlace.Trapdoor);
            Assert.AreEqual(224f, a.x0 + DenMap.HatchInset, "left edge");
            Assert.AreEqual(22f, a.x1 - a.x0 - 2f * DenMap.HatchInset, "width");
            Assert.AreEqual(0f, a.y0, "bottom");
            Assert.AreEqual(15f, a.y1 - a.y0, "height");
            Assert.AreEqual(DenMap.XOf(DenPlace.Trapdoor), 224f + 22f / 2f, "its middle is where you stand to go down");
        }
    }
}
