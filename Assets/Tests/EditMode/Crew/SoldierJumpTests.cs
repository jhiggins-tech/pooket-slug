using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class SoldierJumpTests
    {
        const float Dt = 1f / 60f;

        [Test]
        public void APressOnTheGroundJumpsOnce()
        {
            var j = new SoldierJump();
            Assert.IsTrue(j.Tick(Dt, grounded: true, pressed: true));
            Assert.IsFalse(j.Tick(Dt, grounded: true, pressed: false));
        }

        [Test]
        public void AJumpJustAfterRunningOffALedgeStillCounts()
        {
            var j = new SoldierJump();
            j.Tick(Dt, true, false);
            float t = 0f;
            while (t + Dt < Tuning.Soldier.CoyoteTime * 0.5f) { j.Tick(Dt, false, false); t += Dt; }
            Assert.IsTrue(j.Tick(Dt, false, true));
        }

        [Test]
        public void TooLongOffTheLedgeAndItDoesnt()
        {
            var j = new SoldierJump();
            j.Tick(Dt, true, false);
            for (float t = 0f; t < Tuning.Soldier.CoyoteTime + 0.05f; t += Dt) j.Tick(Dt, false, false);
            Assert.IsFalse(j.Tick(Dt, false, true));
        }

        [Test]
        public void APressJustBeforeLandingJumpsOnLanding()
        {
            var j = new SoldierJump();
            Assert.IsFalse(j.Tick(Dt, false, true));
            Assert.IsTrue(j.Tick(Dt, true, false));
        }

        [Test]
        public void AnOldPressIsForgotten()
        {
            var j = new SoldierJump();
            j.Tick(Dt, false, true);
            for (float t = 0f; t < Tuning.Soldier.JumpBuffer + 0.05f; t += Dt) j.Tick(Dt, false, false);
            Assert.IsFalse(j.Tick(Dt, true, false));
        }

        [Test]
        public void RootedHoldsTheJump()
        {
            var j = new SoldierJump();
            Assert.IsFalse(j.Tick(Dt, true, true, can: false));
        }

        [Test]
        public void DroppingThroughLastsItsTime()
        {
            var j = new SoldierJump();
            Assert.IsTrue(j.Tick(Dt, true, true));
            j.Drop();
            Assert.IsTrue(j.DroppingThrough);
            for (float t = 0f; t < Tuning.Soldier.DropThroughTime + 0.05f; t += Dt) j.Tick(Dt, false, false);
            Assert.IsFalse(j.DroppingThrough);
        }

        [Test]
        public void LettingGoEarlyMakesAShortHop()
        {
            float g = Tuning.Physics.Gravity;
            Assert.AreEqual(g * Tuning.Soldier.ShortHopGravity, SoldierJump.Gravity(5f, held: false));
            Assert.AreEqual(g, SoldierJump.Gravity(5f, held: true));
            Assert.AreEqual(g, SoldierJump.Gravity(5f, held: false, carried: true));
            Assert.AreEqual(g, SoldierJump.Gravity(-1f, held: false));
        }
    }
}
