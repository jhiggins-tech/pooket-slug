using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>Frozen by nitrogen (Core/World/Frost.cs): how long it holds, and the grace after.</summary>
    public class FrostTests
    {
        [Test]
        public void AFreezeHoldsForItsTimeThenThaws()
        {
            var f = new Frost();
            Assert.IsTrue(f.Freeze(1f, 0.5f));
            Assert.IsTrue(f.Frozen);
            f.Tick(0.6f);
            Assert.IsTrue(f.Frozen);
            f.Tick(0.5f);
            Assert.IsFalse(f.Frozen);
            Assert.IsTrue(f.Thawing);
        }

        [Test]
        public void NothingRefreezesWhileFrozenOrJustThawed()
        {
            var f = new Frost();
            f.Freeze(1f, 0.5f);
            Assert.IsFalse(f.Freeze(5f, 0.5f), "already frozen: the first freeze stands");
            f.Tick(0.5f);
            Assert.AreEqual(0.5f, f.Left, 1e-5f);
            f.Tick(0.6f);
            Assert.IsFalse(f.Freeze(1f, 0.5f), "just thawed: a moment to walk out of the vent");
            f.Tick(0.6f);
            Assert.IsTrue(f.Freeze(1f, 0.5f), "and then it can freeze again");
        }

        [Test]
        public void ClearingThawsAtOnceWithNoGrace()
        {
            var f = new Frost();
            f.Freeze(3f, 2f);
            f.Clear();
            Assert.IsFalse(f.Frozen);
            Assert.IsFalse(f.Thawing);
            Assert.IsTrue(f.Freeze(1f, 1f));
        }

        [Test]
        public void ASoldierAlwaysThawsAndCanWalkOutOfAVent()
        {
            // the grace is longer than it takes to walk a vent's width, so standing still is the only way to be caught twice
            float walkOut = Creamery.VentHalfWidth * 2f / Tuning.Soldier.RunSpeed;
            Assert.Greater(Creamery.ThawGrace, walkOut);
            Assert.Less(Creamery.SoldierFreeze, Creamery.VentFor + Creamery.ThawGrace + 1f);
            Assert.Greater(Creamery.GoonFreeze, Creamery.SoldierFreeze, "goons stay stuck longer: shoot them while they're frozen");
        }
    }
}
