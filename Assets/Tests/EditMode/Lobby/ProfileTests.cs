using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
            p.RecordRun(Missions.One, 1200, won: false, solo: true);
            p.RecordRun(Missions.One, 800, won: false, solo: true);
            Assert.AreEqual(1200, p.Best(Missions.One));
        }

        [Test]
        public void ClearsCountWins()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, 100, won: false, solo: true);
            p.RecordRun(Missions.One, 100, won: true, solo: true);
            p.RecordRun(Missions.One, 100, won: true, solo: false);
            Assert.AreEqual(2, p.Clears(Missions.One));
        }

        /// <summary>
        /// A save from when there was a hard setting, as this browser kept it (JsonUtility writes the setting as its
        /// number: 0 normal, 1 hard). It's read here the way JsonUtility reads it, field by field, since JsonUtility
        /// itself only runs inside Unity.
        /// </summary>
        const string SaveWithHard =
            "{\"version\":1,\"coop\":true,\"missions\":[" +
            "{\"mission\":\"mission-1\",\"setting\":0,\"best\":4000,\"clears\":2}," +
            "{\"mission\":\"mission-2\",\"setting\":0,\"best\":2500,\"clears\":1}," +
            "{\"mission\":\"mission-2\",\"setting\":1,\"best\":9000,\"clears\":3}]," +
            "\"seen\":[\"coop\"],\"xp\":700,\"rank\":4,\"first\":1000,\"ts\":2000}";

        static Profile ReadOldSave(string json)
        {
            var p = new Profile
            {
                version = (int)Number(json, "version"),
                coop = json.Contains("\"coop\":true"),
                xp = (int)Number(json, "xp"),
                rank = (int)Number(json, "rank"),
                first = Number(json, "first"),
                ts = Number(json, "ts"),
            };
            foreach (Match m in Regex.Matches(json, "\\{\"mission\":\"([^\"]*)\",\"setting\":(\\d+),\"best\":(\\d+),\"clears\":(\\d+)\\}"))
                p.missions.Add(new MissionRecord
                {
                    mission = m.Groups[1].Value,
                    setting = int.Parse(m.Groups[2].Value),
                    best = int.Parse(m.Groups[3].Value),
                    clears = int.Parse(m.Groups[4].Value),
                });
            foreach (Match m in Regex.Matches(Regex.Match(json, "\"seen\":\\[([^\\]]*)\\]").Groups[1].Value, "\"([^\"]*)\""))
                p.seen.Add(m.Groups[1].Value);
            return p;
        }

        static long Number(string json, string field) => long.Parse(Regex.Match(json, "\"" + field + "\":(\\d+)").Groups[1].Value);

        [Test]
        public void AnOldSaveWithHardRecords_LoadsWithOnlyTheHardPartsGone()
        {
            Profile p = ReadOldSave(SaveWithHard).Repair();
            Assert.AreEqual(2, p.missions.Count, "the hard record is dropped");
            Assert.AreEqual(4000, p.Best(Missions.One));
            Assert.AreEqual(2, p.Clears(Missions.One));
            Assert.AreEqual(2500, p.Best(Missions.Two), "Mission two's own record, not its hard one");
            Assert.AreEqual(1, p.Clears(Missions.Two));
            Assert.IsTrue(p.coop);
            Assert.IsTrue(p.HasSeen("coop"));
            Assert.AreEqual(700, p.xp);
            Assert.AreEqual(4, p.rank);
            Assert.AreEqual(1000, p.first);
            Assert.AreEqual(2000, p.ts);
        }

        [Test]
        public void AHardOnlyClear_IsNoClear()
        {
            var p = new Profile();
            p.missions.Add(new MissionRecord { mission = Missions.Two.Id, setting = 1, best = 900, clears = 1 });
            Assert.AreEqual(0, p.Clears(Missions.Two), "even before it's repaired");
            p.Repair();
            Assert.AreEqual(0, p.missions.Count);
        }

        [Test]
        public void MergingDropsTheOtherCopysHardRecords()
        {
            var cloud = new Profile();
            cloud.missions.Add(new MissionRecord { mission = Missions.One.Id, setting = 0, best = 300, clears = 1 });
            cloud.missions.Add(new MissionRecord { mission = Missions.One.Id, setting = 1, best = 900, clears = 2 });
            var phone = new Profile();
            phone.RecordRun(Missions.One, 500, won: false, solo: true);
            phone.Merge(cloud);
            Assert.AreEqual(1, phone.missions.Count);
            Assert.AreEqual(500, phone.Best(Missions.One));
            Assert.AreEqual(1, phone.Clears(Missions.One));
        }

        [Test]
        public void TheSavedFieldsKeepTheirNames()
        {
            CollectionAssert.AreEquivalent(new[] { "version", "coop", "missions", "seen", "xp", "rank", "first", "ts" },
                typeof(Profile).GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name));
            CollectionAssert.AreEquivalent(new[] { "mission", "setting", "best", "clears" },
                typeof(MissionRecord).GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name));
            Assert.AreEqual(typeof(int), typeof(MissionRecord).GetField("setting").FieldType, "an old save holds it as a number");
        }

        [Test]
        public void BeatingMissionOneSoloOpensCoop()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, 100, won: false, solo: true);
            Assert.IsFalse(p.coop, "losing doesn't");
            p.RecordRun(Missions.One, 100, won: true, solo: false);
            Assert.IsFalse(p.coop, "a win in co-op doesn't: the design asks for a solo win");
            p.RecordRun(Missions.One, 100, won: true, solo: true);
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
            phone.RecordRun(Missions.One, 3000, won: true, solo: true);
            phone.MarkSeen("coop");
            phone.Stamp(2000);
            var laptop = new Profile();
            laptop.RecordRun(Missions.One, 4000, won: false, solo: true);
            laptop.MarkSeen("tank");
            laptop.Stamp(1000);
            laptop.Stamp(3000);

            laptop.Merge(phone);
            Assert.AreEqual(4000, laptop.Best(Missions.One));
            Assert.AreEqual(1, laptop.Clears(Missions.One));
            Assert.IsTrue(laptop.coop);
            Assert.IsTrue(laptop.HasSeen("coop") && laptop.HasSeen("tank"));
            Assert.AreEqual(1000, laptop.first, "the earliest first visit");
            Assert.AreEqual(3000, laptop.ts, "the latest save");
        }

        [Test]
        public void MergingTheSameRecordTwiceChangesNothing()
        {
            var a = new Profile();
            a.RecordRun(Missions.One, 900, won: true, solo: true);
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
