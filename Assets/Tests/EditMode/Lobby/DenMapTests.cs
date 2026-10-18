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
            Assert.IsFalse(w.Climb(1), "the loft's the top");
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
                bool does = p == DenPlace.Couch || p == DenPlace.Board || p == DenPlace.Gym || p == DenPlace.Trophies;
                Assert.AreEqual(does, DenMap.Note(p) == null, p.ToString());
                Assert.AreEqual(p, DenMap.PlaceAt(x, DenMap.FloorY(f) + 10f), p.ToString() + " is tapped where you stand to use it");
            }
        }

        [Test]
        public void ATapPicksTheFloorItsOn()
        {
            Assert.AreEqual(DenFloor.Ground, DenMap.FloorAt(40f));
            Assert.AreEqual(DenFloor.Upper, DenMap.FloorAt(140f));
            Assert.AreEqual(DenFloor.Loft, DenMap.FloorAt(250f));
            Assert.AreEqual(DenPlace.None, DenMap.PlaceAt(200f, 20f), "the floor by the ladder: just walk there");
        }
    }
}
