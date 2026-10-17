using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class NetIdsTests
    {
        [Test]
        public void LevelSpawnsKeepTheirIndex()
        {
            var ids = new NetIds(33);
            Assert.IsTrue(ids.IsLevelSpawn(0));
            Assert.IsTrue(ids.IsLevelSpawn(32));
            Assert.IsFalse(ids.IsLevelSpawn(33));
            Assert.IsFalse(ids.IsLevelSpawn(-1));
        }

        [Test]
        public void MidGameIdsComeAfterTheLevelsAndCountUp()
        {
            var ids = new NetIds(33);
            Assert.AreEqual(33, ids.Allocate());
            Assert.AreEqual(34, ids.Allocate());
            Assert.AreEqual(35, ids.Next);
        }

        [Test]
        public void ANewAuthorityCarriesOnFromTheSharedCounter()
        {
            var ids = new NetIds(33);
            ids.CatchUp(40);
            Assert.AreEqual(40, ids.Allocate());
        }

        [Test]
        public void CatchingUpNeverGoesBackwards()
        {
            var ids = new NetIds(33);
            ids.Allocate();
            ids.Allocate();
            ids.CatchUp(0); // a restarted round's shared counter
            Assert.AreEqual(35, ids.Allocate());
        }

        [Test]
        public void WhenIdsRunOutTheThingStaysLocal()
        {
            var ids = new NetIds(0);
            ids.CatchUp(NetIds.MaxId);
            Assert.AreEqual(NetIds.MaxId, ids.Allocate());
            Assert.AreEqual(-1, ids.Allocate());
        }

        [Test]
        public void EveryIdFitsTheSnapshotsSixteenBits()
        {
            Assert.LessOrEqual(NetIds.MaxId + 1, short.MaxValue);
        }
    }
}
