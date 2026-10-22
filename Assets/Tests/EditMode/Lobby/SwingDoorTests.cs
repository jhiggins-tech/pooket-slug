using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>A house door that swings: open, through its dark, and shut behind you.</summary>
    public class SwingDoorTests
    {
        const float Dt = 1f / 60f;

        [Test]
        public void ReachingIt_SwingsItOpenFirst_ThenYouStepThrough_IntoTheNextRoomOnce()
        {
            var door = new SwingDoor();
            Assert.IsTrue(door.GoIn());
            Assert.IsTrue(door.TakeSwung(), "the creak");
            Assert.IsFalse(door.TakeSwung(), "once");
            Assert.IsTrue(door.Busy);
            int through = 0;
            float t = 0f;
            for (; t < SwingDoor.SwingSeconds - Dt; t += Dt) if (door.Tick(Dt)) through++;
            Assert.That(door.Swing, Is.GreaterThan(0.9f).And.LessThan(1f));
            Assert.AreEqual(0f, door.Through, "nobody steps in till it's open");
            for (; t < SwingDoor.SwingSeconds + SwingDoor.StepSeconds; t += Dt) if (door.Tick(Dt)) through++;
            Assert.AreEqual(1, through);
            Assert.IsTrue(door.Busy, "it swings shut behind you first");
            Assert.IsFalse(door.GoIn(), "not again while it's shutting");
            for (; t < SwingDoor.SwingSeconds + SwingDoor.StepSeconds + SwingDoor.ShutSeconds + 0.5f; t += Dt) door.Tick(Dt);
            Assert.IsFalse(door.Busy);
            Assert.AreEqual(0f, door.Swing);
            Assert.AreEqual(0f, door.Through);
            Assert.IsTrue(door.TakeLanded(), "it lands shut");
            Assert.IsTrue(door.GoIn(), "and it can go again");
        }

        [Test]
        public void ItSwingsShutBehindYou_AndLandsWithAThump()
        {
            var door = new SwingDoor();
            door.GoIn();
            door.TakeSwung();
            Assert.IsFalse(door.TakeLanded());
            while (!door.Tick(Dt)) { }
            Assert.IsFalse(door.TakeLanded(), "not till it's shut");
            Assert.IsFalse(door.TakeSwung(), "no second creak");
            for (float t = 0f; t < SwingDoor.ShutSeconds - Dt; t += Dt) door.Tick(Dt);
            Assert.That(door.Swing, Is.GreaterThan(0f).And.LessThan(1f), "still swinging shut");
            door.Tick(Dt);
            Assert.AreEqual(0f, door.Swing);
            Assert.IsTrue(door.TakeLanded(), "the thump");
            Assert.IsFalse(door.TakeLanded(), "once");
            Assert.IsFalse(door.Busy);
            Assert.IsTrue(door.GoIn(), "and it can go again");
        }

        [Test]
        public void Reset_ShutsIt()
        {
            var door = new SwingDoor();
            door.GoIn();
            door.Tick(0.5f);
            door.Reset();
            Assert.AreEqual(0f, door.Swing);
            Assert.AreEqual(0f, door.Through);
            Assert.IsFalse(door.Busy);
            Assert.IsFalse(door.TakeSwung());
            Assert.IsFalse(door.TakeLanded());
        }
    }
}
