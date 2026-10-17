using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class ProfileTests
    {
        [Test]
        public void ANewProfileHasNothing()
        {
            var p = new Profile();
            Assert.AreEqual(0, p.Best(Missions.One));
            Assert.AreEqual(0, p.Clears(Missions.One));
            Assert.IsFalse(p.coop);
            Assert.AreEqual(0, p.first);
        }

        [Test]
        public void TheBestScoreOnlyGoesUp()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 1200, won: false, solo: true);
            p.RecordRun(Missions.One, Setting.Normal, 800, won: false, solo: true);
            Assert.AreEqual(1200, p.Best(Missions.One));
        }

        [Test]
        public void ClearsCountWins()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 100, won: false, solo: true);
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: false);
            Assert.AreEqual(2, p.Clears(Missions.One));
        }

        [Test]
        public void SettingsAreKeptApart()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Hard, 5000, won: true, solo: true);
            Assert.AreEqual(0, p.Best(Missions.One, Setting.Normal));
            Assert.AreEqual(5000, p.Best(Missions.One, Setting.Hard));
        }

        [Test]
        public void BeatingMissionOneSoloOpensCoop()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 100, won: false, solo: true);
            Assert.IsFalse(p.coop, "losing doesn't");
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: false);
            Assert.IsFalse(p.coop, "a win in co-op doesn't: the design asks for a solo win");
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
            Assert.IsTrue(p.coop);
        }

        [Test]
        public void EachRevealIsSeenOnce()
        {
            var p = new Profile();
            Assert.IsFalse(p.HasSeen("coop"));
            Assert.IsTrue(p.MarkSeen("coop"));
            Assert.IsFalse(p.MarkSeen("coop"));
            Assert.IsTrue(p.HasSeen("coop"));
        }

        [Test]
        public void MergingTakesTheBetterOfEach()
        {
            var phone = new Profile();
            phone.RecordRun(Missions.One, Setting.Normal, 3000, won: true, solo: true);
            phone.MarkSeen("coop");
            phone.Stamp(2000);
            var laptop = new Profile();
            laptop.RecordRun(Missions.One, Setting.Normal, 4000, won: false, solo: true);
            laptop.RecordRun(Missions.One, Setting.Hard, 100, won: false, solo: true);
            laptop.MarkSeen("tank");
            laptop.Stamp(1000);
            laptop.Stamp(3000);

            laptop.Merge(phone);
            Assert.AreEqual(4000, laptop.Best(Missions.One));
            Assert.AreEqual(1, laptop.Clears(Missions.One));
            Assert.AreEqual(100, laptop.Best(Missions.One, Setting.Hard));
            Assert.IsTrue(laptop.coop);
            Assert.IsTrue(laptop.HasSeen("coop") && laptop.HasSeen("tank"));
            Assert.AreEqual(1000, laptop.first, "the earliest first visit");
            Assert.AreEqual(3000, laptop.ts, "the latest save");
        }

        [Test]
        public void MergingTheSameRecordTwiceChangesNothing()
        {
            var a = new Profile();
            a.RecordRun(Missions.One, Setting.Normal, 900, won: true, solo: true);
            var b = new Profile();
            b.Merge(a);
            b.Merge(a);
            Assert.AreEqual(1, b.Clears(Missions.One), "clears that are on both copies aren't added up");
            Assert.AreEqual(1, b.missions.Count);
        }

        [Test]
        public void TheFirstVisitIsStampedOnce()
        {
            var p = new Profile();
            p.Stamp(100);
            p.Stamp(200);
            Assert.AreEqual(100, p.first);
            Assert.AreEqual(200, p.ts);
        }

        [Test]
        public void ADamagedSaveIsRepaired()
        {
            var p = new Profile { missions = null, seen = null };
            p.Repair();
            Assert.AreEqual(0, p.Best(Missions.One));
            Assert.IsFalse(p.HasSeen("coop"));

            var q = new Profile();
            q.missions.Add(new MissionRecord { mission = null, best = 5 });
            q.seen.Add("");
            q.Repair();
            Assert.AreEqual(0, q.missions.Count);
            Assert.AreEqual(0, q.seen.Count);
        }
    }
}
