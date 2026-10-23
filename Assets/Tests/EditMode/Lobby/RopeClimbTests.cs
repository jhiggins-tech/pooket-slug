using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The bunker's rope: down from the hole on the way in, and up and out on the way back.</summary>
    public class RopeClimbTests
    {
        const float Dt = 1f / 60f;

        static RopeClimb.Way Run(RopeClimb rope, float seconds)
        {
            RopeClimb.Way done = RopeClimb.Way.None;
            for (float t = 0f; t < seconds; t += Dt)
            {
                RopeClimb.Way c = rope.Tick(Dt);
                if (c != RopeClimb.Way.None) done = c;
            }
            return done;
        }

        [Test]
        public void ComingIn_StartsYouAtTheHole_AndClimbsYouDownToTheFloor()
        {
            var rope = new RopeClimb();
            Assert.IsFalse(rope.Busy);
            Assert.IsTrue(rope.GoDown());
            Assert.IsTrue(rope.Busy);
            Assert.AreEqual(RopeClimb.Way.Down, rope.Going);
            Assert.AreEqual(RoomMap.RopeTopY, rope.Y, 0.01f, "you start at the hole in the roof");
            Assert.AreEqual(RopeClimb.Way.Down, Run(rope, 5f), "you land once");
            Assert.IsFalse(rope.Busy);
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f, "on the bunker's floor");
            Assert.AreEqual(RopeClimb.Way.None, Run(rope, 1f), "and not again");
        }

        [Test]
        public void GoingOut_ClimbsYouUpTheRope_AndOutOfTheHole()
        {
            var rope = new RopeClimb();
            Assert.IsTrue(rope.GoUp());
            Assert.IsTrue(rope.Busy);
            Assert.AreEqual(RopeClimb.Way.Up, rope.Going);
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f, "you start at the foot of the rope");
            Assert.AreEqual(RopeClimb.Way.Up, Run(rope, 5f), "you leave the hole once");
            Assert.IsFalse(rope.Busy);
            Assert.AreEqual(RoomMap.RopeTopY, rope.Y, 0.01f, "up at the hole");
            Assert.AreEqual(RopeClimb.Way.None, Run(rope, 1f), "and not again");
        }

        [Test]
        public void TheRope_GoesOneWayAtATime()
        {
            var rope = new RopeClimb();
            Assert.IsTrue(rope.GoDown());
            Assert.IsFalse(rope.GoUp(), "not while you're climbing down");
            Assert.IsFalse(rope.GoDown(), "nor twice");
            Assert.AreEqual(RopeClimb.Way.Down, rope.Going);
            Run(rope, 5f);
            Assert.IsTrue(rope.GoUp(), "and it can go again");
        }

        [Test]
        public void TheClimb_GoesAtItsPace()
        {
            var rope = new RopeClimb();
            rope.GoDown();
            rope.Tick(Dt);
            Assert.AreEqual(RoomMap.RopeTopY - RoomMap.RopeSpeed * Dt, rope.Y, 0.01f, "a frame down the rope");
            float seconds = (RoomMap.RopeTopY - RoomMap.FloorY) / RoomMap.RopeSpeed;
            var again = new RopeClimb();
            again.GoDown();
            Assert.AreEqual(RopeClimb.Way.None, Run(again, seconds - 0.1f), "not there yet");
            Assert.AreEqual(RopeClimb.Way.Down, Run(again, 0.2f), "there in its time");
        }

        [Test]
        public void Reset_LeavesYouStandingOnTheFloor()
        {
            var rope = new RopeClimb();
            rope.GoDown();
            rope.Tick(0.5f);
            rope.Reset();
            Assert.IsFalse(rope.Busy);
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f);
            Assert.AreEqual(RopeClimb.Way.None, rope.Going);
        }

        // docs/design/lobby.md, The outer rooms: the bunker is entered and left by its rope
        [Test]
        public void TheBunkersWay_IsTheRope_AndTheRopeIsWhereTheWayIs()
        {
            Assert.IsTrue(RoomMap.ByRope(RoomId.Bunker, RoomWay.Back));
            Assert.IsFalse(RoomMap.ByRope(RoomId.Basement, RoomWay.ToBunker), "the hole is in the basement: the rope is in the bunker");
            Assert.IsFalse(RoomMap.ByRope(RoomId.Kitchen, RoomWay.Back), "the other rooms are doors");
            Assert.AreEqual(RoomMap.RopeX, RoomMap.WayX(RoomId.Bunker, RoomWay.Back), 0.01f, "the rope is where you use the way");
            Assert.That(RoomMap.RopeTopY, Is.GreaterThan(RoomMap.FloorY).And.LessThan(RoomMap.Height), "the hole is in the roof");
        }

        // a rope is a tall thing to tap, so a tap anywhere up it uses it, not just where you stand
        [Test]
        public void TheRope_IsTappedAnywhereUpIt()
        {
            Assert.AreEqual(RoomWay.Back, RoomMap.WayAt(RoomId.Bunker, RoomMap.RopeX, RoomMap.FloorY + 10f), "at its foot");
            Assert.AreEqual(RoomWay.Back, RoomMap.WayAt(RoomId.Bunker, RoomMap.RopeX, (RoomMap.FloorY + RoomMap.RopeTopY) / 2f), "halfway up");
            Assert.AreEqual(RoomWay.None, RoomMap.WayAt(RoomId.Bunker, RoomMap.RopeX - 20f, RoomMap.RopeTopY), "the ceiling beside it isn't the rope");
        }
    }
}
