using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The bunker's rope: down from the hole in B1's floor, up from the bunker's own floor, one way at a time.</summary>
    public class RopeClimbTests
    {
        const float Dt = 1f / 60f;

        static int Run(RopeClimb rope, float seconds, RopeClimb.Way which)
        {
            int ended = 0;
            for (float t = 0f; t < seconds; t += Dt)
                if (rope.Tick(Dt) == which) ended++;
            return ended;
        }

        [Test]
        public void ComingIn_StartsAtTheHole_AndLandsOnTheFloor_SayingSoOnce()
        {
            var rope = new RopeClimb();
            Assert.IsFalse(rope.Busy);
            Assert.IsTrue(rope.GoDown());
            Assert.IsTrue(rope.Busy);
            Assert.IsFalse(rope.GoingUp);
            Assert.AreEqual(RoomMap.RopeTopY, rope.Y, 0.01f, "you start at the hole in the roof");
            Assert.AreEqual(1, Run(rope, 10f, RopeClimb.Way.Down), "it says so once");
            Assert.IsFalse(rope.Busy);
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f, "on the bunker's floor");
            Assert.AreEqual(0, Run(rope, 1f, RopeClimb.Way.Down), "not again");
        }

        [Test]
        public void GoingOut_StartsOnTheFloor_AndLeavesTheHole_SayingSoOnce()
        {
            var rope = new RopeClimb();
            Assert.IsTrue(rope.GoUp());
            Assert.IsTrue(rope.GoingUp);
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f, "you start on the floor");
            Assert.AreEqual(1, Run(rope, 10f, RopeClimb.Way.Up), "it says so once");
            Assert.IsFalse(rope.Busy);
            Assert.AreEqual(RoomMap.RopeTopY, rope.Y, 0.01f, "up and out of the hole");
        }

        [Test]
        public void ItGoesOneWayAtATime()
        {
            var rope = new RopeClimb();
            Assert.IsTrue(rope.GoDown());
            Assert.IsFalse(rope.GoUp(), "not while you're coming down");
            Assert.IsFalse(rope.GoDown(), "not twice");
            Run(rope, 10f, RopeClimb.Way.Down);
            Assert.IsTrue(rope.GoUp(), "and again once you've landed");
        }

        [Test]
        public void ItClimbsAtItsPace()
        {
            var rope = new RopeClimb();
            rope.GoUp();
            rope.Tick(0.5f);
            Assert.AreEqual(RoomMap.FloorY + RoomMap.RopeSpeed * 0.5f, rope.Y, 0.01f);
            float seconds = (RoomMap.RopeTopY - RoomMap.FloorY) / RoomMap.RopeSpeed;
            Run(rope, seconds + 1f, RopeClimb.Way.Up);
            Assert.IsFalse(rope.Busy);
        }

        [Test]
        public void Reset_LetsTheRopeStill()
        {
            var rope = new RopeClimb();
            rope.GoUp();
            rope.Tick(0.4f);
            rope.Reset();
            Assert.IsFalse(rope.Busy);
            Assert.IsFalse(rope.GoingUp);
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f);
        }

        [Test]
        public void TheBunkersWayInAndOut_IsItsRope()
        {
            Assert.IsTrue(RoomMap.ByRope(RoomId.Bunker, RoomWay.Back), "the bunker is climbed in and out");
            Assert.AreEqual(RoomMap.RopeX, RoomMap.WayX(RoomId.Bunker, RoomWay.Back), 0.01f, "where the plate hangs it");
            foreach (RoomId r in System.Enum.GetValues(typeof(RoomId)))
            {
                foreach (RoomWay way in RoomMap.Ways)
                    if (!(r == RoomId.Bunker && way == RoomWay.Back))
                        Assert.IsFalse(RoomMap.ByRope(r, way), r + " " + way + ": its ways are walked, not climbed");
            }
        }

        [Test]
        public void TheRope_IsUsedAnywhereUpIt_FromTheFloorToTheHole()
        {
            Assert.AreEqual(RoomWay.Back, RoomMap.WayAt(RoomId.Bunker, RoomMap.RopeX, RoomMap.FloorY + 10f), "at its foot");
            Assert.AreEqual(RoomWay.Back, RoomMap.WayAt(RoomId.Bunker, RoomMap.RopeX, RoomMap.RopeTopY - 10f), "up the rope");
            Assert.AreEqual(RoomWay.None, RoomMap.WayAt(RoomId.Bunker, RoomMap.RopeX - 20f, RoomMap.FloorY + 10f), "not beside it");
            Assert.AreEqual(RoomWay.None, RoomMap.WayAt(RoomId.Bunker, RoomMap.RopeX + 20f, RoomMap.FloorY + 60f), "not the wall beside it");
        }
    }
}
