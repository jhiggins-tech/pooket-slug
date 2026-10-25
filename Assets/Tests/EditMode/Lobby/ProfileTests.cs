using System;
using System.Collections.Generic;
using System.IO;
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

        // The save format. The profile is saved as JsonUtility writes it (its public fields, by name), in this browser
        // and, through pooket-auth.js, in the cloud. A field renamed or retyped here would lose every player's
        // progress, so these pin each name as it is today. Adding a field is fine: add it here too.

        /// <summary>The fields JsonUtility saves of a type: its public instance fields, as name and type.</summary>
        static string[] Saved(Type t) => t.GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(f => f.Name + ":" + f.FieldType.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        [Test]
        public void TheProfilesFieldNames_NeverChange()
        {
            CollectionAssert.AreEqual(new[]
            {
                "coop:Boolean", "first:Int64", "missions:List`1", "rank:Int32", "seen:List`1", "ts:Int64", "version:Int32", "xp:Int32",
            }, Saved(typeof(Profile)));
            Assert.AreEqual(typeof(List<MissionRecord>), typeof(Profile).GetField("missions").FieldType);
            Assert.AreEqual(typeof(List<string>), typeof(Profile).GetField("seen").FieldType);
            Assert.IsTrue(typeof(Profile).IsSerializable, "JsonUtility saves only a [Serializable] type");
        }

        [Test]
        public void AMissionRecordsFieldNames_NeverChange()
        {
            CollectionAssert.AreEqual(new[] { "best:Int32", "clears:Int32", "mission:String", "setting:Int32" }, Saved(typeof(MissionRecord)));
            Assert.IsTrue(typeof(MissionRecord).IsSerializable, "JsonUtility saves only a [Serializable] type");
        }

        /// <summary>A file of the repository's, as text (tests run from the project's root).</summary>
        static string Source(string path) => File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), path)).Replace("\r\n", "\n");

        [Test]
        public void TheBrowsersSaveKeys_NeverChange()
        {
            // PlayerPrefs keys (Save/, which the tests can't reach, so they're read from the source) and the page's
            // own localStorage key. "pooket-slug." keeps them apart from Pooket Tabks, which shares the origin.
            StringAssert.Contains("const string Key = \"pooket-slug.profile\";", Source("Assets/Scripts/Save/LocalProfileStore.cs"));
            StringAssert.Contains("const string OldHiScoreKey = \"pooket-slug.hiscore\";", Source("Assets/Scripts/Save/LocalProfileStore.cs"),
                "the high score from before profiles, read once into the profile");
            StringAssert.Contains("var AUTH_KEY = \"pooket-slug.auth\";", Source("Assets/WebGLTemplates/PooketSlug/pooket-auth.js"));
        }

        [Test]
        public void TheCloudRecordsNames_NeverChange()
        {
            // pooket-auth.js turns the profile into the database's record and back: where it's kept, and each name.
            string js = Source("Assets/WebGLTemplates/PooketSlug/pooket-auth.js");
            StringAssert.Contains("return \"pooketSlug/players/\" + session.uid;", js);
            StringAssert.Contains("var RECORD = \"normal\";", js, "a mission's record, under normal as the rules ask");
            foreach (string field in new[] { "version", "coop", "xp", "rank", "missions", "seen", "first", "ts" })
            {
                StringAssert.Contains("\n      " + field + ": ", js, field + " as the game's profile (toProfile)");
                StringAssert.Contains("rec." + field, js, field + " read from the record (toProfile)");
            }
            StringAssert.Contains("missions.push({ mission: mission, best: r.best | 0, clears: r.clears | 0 });", js);
            StringAssert.Contains("= { best: Math.max(0, r.best | 0), clears: Math.max(0, r.clears | 0) };", js);
            foreach (string field in new[] { "version: Math.max(1, Math.min(1000, p.version", "coop: p.coop", "xp: Math.min(1000000000, Math.max(0, p.xp", "rank: Math.min(1000, Math.max(0, p.rank" })
                StringAssert.Contains(field, js, "written to the record (fromProfile)");
            foreach (string field in new[] { "missions", "seen", "rev", "ts", "tokens", "first", "name" })
                StringAssert.Contains("fields." + field + " = ", js, field + " written to the record");
            StringAssert.Contains("rec.rev", js, "the record's save number");
        }

        const string Page = "https://jhiggins-tech.github.io/pooket-slug/index.html";

        static bool Saves(string query, bool sandbox = false, bool spike = false) =>
            ProfileKeeping.Saves(LaunchFlags.Read(Page + query), sandbox, spike);

        [Test]
        public void APlainVisitAndTestFlagsSaveButASandboxOrASpikeDoesNot()
        {
            Assert.IsTrue(Saves(""), "a plain visit");
            Assert.IsTrue(Saves("?mission=2"), "?mission=");
            Assert.IsTrue(Saves("?test=boss"), "?test= starts save, as they always have");
            Assert.IsTrue(Saves("?test=debrief"), "?test=debrief");
            Assert.IsTrue(Saves("?unlock=crew,coop,tank"), "?unlock= opens things for the visit and saves runs");
            Assert.IsTrue(Saves("?demo=all"), "a demo plays no run, and its store is a plain one");
            Assert.IsFalse(Saves("?sandbox=boss", sandbox: true), "a sandbox keeps nothing");
            Assert.IsFalse(Saves("?spike=craters", spike: true), "a spike keeps nothing");
            Assert.IsFalse(Saves("?spike=craters&test=boss&unlock=crew", spike: true), "a spike keeps nothing, whatever else is on");
        }

        sealed class CountingStore : IProfileStore
        {
            public int Saves;
            public readonly Profile Kept = new Profile();
            public Profile Load() => Kept;
            public void Save(Profile profile) => Saves++;
        }

        [Test]
        public void AVisitThatKeepsNothingLoadsTheSaveAndNeverWrites()
        {
            var store = new CountingStore();
            store.Kept.RecordRun(Missions.One, 900, won: true, solo: true);
            IProfileStore unsaved = ProfileKeeping.For(store, saves: false);
            Profile p = unsaved.Load();
            Assert.AreEqual(1, p.Clears(Missions.One), "it plays from the saved profile");
            p.RecordRun(Missions.One, 1500, won: true, solo: true);
            p.MarkSeen(Reveals.Coop);
            unsaved.Save(p);
            Assert.AreEqual(0, store.Saves);
            Assert.AreSame(store, ProfileKeeping.For(store, saves: true), "a visit that saves uses the store itself");
        }
    }
}
