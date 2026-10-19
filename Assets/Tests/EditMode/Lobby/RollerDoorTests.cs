using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The den's roller door: up, through into the bay, and back out with it rolling down behind you.</summary>
    public class RollerDoorTests
    {
        const float Dt = 1f / 60f;

        [Test]
        public void ReachingIt_RollsItUpFirst_ThenYouStepThrough_IntoTheBayOnce()
        {
            var door = new RollerDoor();
            Assert.IsTrue(door.GoIn());
            Assert.IsTrue(door.TakeRolled(), "the rattle");
            Assert.IsFalse(door.TakeRolled(), "once");
            Assert.IsTrue(door.Busy);
            int through = 0;
            float t = 0f;
            for (; t < RollerDoor.UpSeconds - Dt; t += Dt) if (door.Tick(Dt)) through++;
            Assert.That(door.Lift, Is.GreaterThan(0.9f).And.LessThan(1f));
            Assert.AreEqual(0f, door.Through, "nobody steps through till it's up");
            for (; t < RollerDoor.UpSeconds + RollerDoor.StepSeconds + 0.5f; t += Dt) if (door.Tick(Dt)) through++;
            Assert.AreEqual(1, through);
            Assert.IsTrue(door.Inside);
            Assert.IsFalse(door.Busy);
            Assert.AreEqual(1f, door.Lift);
            Assert.AreEqual(1f, door.Through);
            Assert.IsFalse(door.GoIn(), "not again from inside");
        }

        [Test]
        public void ComingBack_YouStepOutFirst_ThenItRollsDownBehindYou()
        {
            var door = new RollerDoor();
            Assert.IsFalse(door.ComeOut(), "only from inside");
            door.GoIn();
            while (!door.Tick(Dt)) { }
            door.TakeRolled();
            Assert.IsTrue(door.ComeOut());
            Assert.IsTrue(door.Busy);
            for (float t = 0f; t < RollerDoor.StepSeconds - Dt; t += Dt) door.Tick(Dt);
            Assert.AreEqual(1f, door.Lift, "up while you step out");
            Assert.IsFalse(door.TakeRolled());
            for (int i = 0; i < 3; i++) door.Tick(Dt);
            Assert.AreEqual(0f, door.Through);
            Assert.IsTrue(door.TakeRolled(), "it rattles down");
            for (float t = 0f; t < RollerDoor.DownSeconds; t += Dt) door.Tick(Dt);
            Assert.AreEqual(0f, door.Lift);
            Assert.IsFalse(door.Busy);
            Assert.IsTrue(door.GoIn(), "and it can go again");
        }

        [Test]
        public void Reset_ShutsIt()
        {
            var door = new RollerDoor();
            door.GoIn();
            door.Tick(0.5f);
            door.Reset();
            Assert.AreEqual(0f, door.Lift);
            Assert.IsFalse(door.Busy);
            Assert.IsFalse(door.TakeRolled());
        }
    }
}
