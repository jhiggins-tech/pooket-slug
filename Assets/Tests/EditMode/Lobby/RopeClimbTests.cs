using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The bunker's rope (<c>docs/design/lobby.md</c>, The outer rooms): down it coming in, up it going out.</summary>
    public class RopeClimbTests
    {
        const float Frame = 1f / 60f;

        static void Run(RopeClimb rope, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame) rope.Tick(Frame);
        }

        [Test]
        public void TheBunkersWayBack_IsItsRope()
        {
            Assert.IsTrue(RoomMap.ByRope(RoomId.Bunker, RoomWay.Back), "the way back up to the basement is the rope");
            Assert.IsFalse(RoomMap.ByRope(RoomId.Bunker, RoomWay.ToBunker), "and its way on isn't");
            Assert.IsFalse(RoomMap.ByRope(RoomId.Basement, RoomWay.ToBunker), "the basement's hole isn't a rope in the basement");
            Assert.IsFalse(RoomMap.ByRope(RoomId.Kitchen, RoomWay.Back), "the kitchen's back door is a door");
            Assert.IsFalse(RoomMap.ByRope(RoomId.None, RoomWay.None));
        }

        [Test]
        public void TheWayIsWhereTheRopeHangs_InsideTheRoom()
        {
            Assert.AreEqual(RoomMap.RopeX, RoomMap.WayX(RoomId.Bunker, RoomWay.Back), 0.01f, "you use the way where the rope is");
            Assert.That(RoomMap.RopeX, Is.InRange(RoomMap.MinX(RoomId.Bunker), RoomMap.MaxX(RoomId.Bunker)), "inside the room");
            Assert.AreEqual(RoomMap.HeightOf(RoomId.Bunker), RoomMap.RopeTop(RoomId.Bunker), 0.01f, "it hangs from the roof");
            Assert.Greater(RoomMap.RopeTop(RoomId.Bunker), RoomMap.FloorY + RoomMap.SoldierHeight, "high enough to climb out of sight");
        }

        [Test]
        public void ComingIn_YouStartAtTheHole_AndClimbDownToTheFloor()
        {
            var rope = new RopeClimb(RoomId.Bunker);
            Assert.IsFalse(rope.Busy, "off the rope");
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f);

            Assert.IsTrue(rope.GoDown());
            Assert.IsTrue(rope.Busy);
            Assert.IsFalse(rope.GoingUp);
            Assert.AreEqual(RoomMap.RopeTop(RoomId.Bunker), rope.Y, 0.01f, "at the hole, above the ceiling");
            Assert.IsFalse(rope.TakeLanded(), "not yet");

            bool landed = false;
            for (int i = 0; i < 60 * 5 && !landed; i++)
            {
                rope.Tick(Frame);
                if (rope.TakeLanded()) landed = true;
            }
            Assert.IsTrue(landed, "it says so once");
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f, "feet on the floor");
            Assert.IsFalse(rope.Busy, "the room's yours");
            Assert.IsFalse(rope.TakeLanded(), "and only once");
            Assert.IsFalse(rope.TakeLeft());
        }

        [Test]
        public void GoingOut_YouClimbUpFromTheFloor_AndLeaveTheHole()
        {
            var rope = new RopeClimb(RoomId.Bunker);
            Assert.IsTrue(rope.GoUp());
            Assert.IsTrue(rope.Busy);
            Assert.IsTrue(rope.GoingUp);
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f, "from the floor");
            Assert.IsFalse(rope.TakeLeft());

            Run(rope, 5f);
            Assert.IsFalse(rope.Busy, "out of the hole");
            Assert.AreEqual(RoomMap.RopeTop(RoomId.Bunker), rope.Y, 0.01f);
            Assert.IsTrue(rope.TakeLeft(), "it says so once");
            Assert.IsFalse(rope.TakeLeft(), "and only once");
            Assert.IsFalse(rope.TakeLanded());
        }

        [Test]
        public void TheRopeGoesOneWayAtATime()
        {
            var rope = new RopeClimb(RoomId.Bunker);
            Assert.IsTrue(rope.GoDown());
            Assert.IsFalse(rope.GoUp(), "already on it");
            Assert.IsFalse(rope.GoDown(), "already on it");
        }

        [Test]
        public void ItClimbsAtItsPace()
        {
            var rope = new RopeClimb(RoomId.Bunker);
            rope.GoDown();
            float top = rope.Y;
            Run(rope, 1f);
            Assert.AreEqual(top - RoomMap.RopeSpeed, rope.Y, RoomMap.RopeSpeed * Frame + 0.01f, "a second's climb");
        }

        [Test]
        public void ResetPutsYouBackOnTheFloor()
        {
            var rope = new RopeClimb(RoomId.Bunker);
            rope.GoDown();
            Run(rope, 0.5f);
            rope.Reset();
            Assert.IsFalse(rope.Busy);
            Assert.AreEqual(RoomMap.FloorY, rope.Y, 0.01f);
            Assert.IsFalse(rope.TakeLeft());
        }
    }
}
