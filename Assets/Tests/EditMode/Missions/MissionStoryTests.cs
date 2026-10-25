using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>
    /// Missions' stories (Core/Missions/MissionStory.cs): what Mission one's script says, a mission with none, the once-only
    /// beats, the quick boss intro, the super, and when the boss reveal and the pilot's freeing play.
    /// </summary>
    public class MissionStoryTests
    {
        [Test]
        public void MissionOneIsASoloPreludeWithItsScenesAndCoda()
        {
            MissionStory s = Missions.One.Story;
            Assert.AreEqual(SceneLibrary.Opening, s.Opening);
            Assert.AreEqual(SceneLibrary.MotorPool, s.BossReveal);
            Assert.AreEqual(SceneLibrary.FreeingKie, s.PilotFreed);
            Assert.IsTrue(s.Coda && s.SoloOnly && s.OpensCoop && s.BeforeDen);
            foreach (string scene in new[] { s.Opening, s.BossReveal, s.PilotFreed })
                CollectionAssert.Contains(SceneLibrary.Names, scene, "every scene a mission plays is one the game has");
        }

        [Test]
        public void EveryMissionsScenesAreOnesTheGameHas()
        {
            foreach (MissionDef mission in Missions.All)
            {
                MissionStory s = mission.Story;
                foreach (string scene in new[] { s.Opening, s.BossReveal, s.PilotFreed })
                    if (scene != null)
                        CollectionAssert.Contains(SceneLibrary.Names, scene, mission.Id + " names a scene the game doesn't have");
            }
        }

        [Test]
        public void AMissionWithoutAStoryHasNone()
        {
            var plain = new MissionDef("mission-test", 200, "MISSION X", "A TEST MAP", new[] { "#" });
            Assert.AreSame(MissionStory.None, plain.Story);
            Assert.IsNull(plain.Story.Opening);
            Assert.IsFalse(plain.Story.SoloOnly || plain.Story.OpensCoop || plain.Story.Coda || plain.Story.BeforeDen);
            Assert.IsFalse(MissionBoard.SoloOnly(plain));
            var p = new Profile();
            p.RecordRun(plain, 100, won: true, solo: true);
            Assert.IsFalse(p.coop, "only a mission whose story says so opens co-op");
            p.RecordRun(Missions.One, 100, won: true, solo: true);
            Assert.IsTrue(p.coop);
        }

        [Test]
        public void OnceOnlyBeatsStayMarked()
        {
            var p = new Profile();
            var record = new StoryRecord(p);
            Assert.IsTrue(record.Due(SceneLibrary.MotorPool));
            Assert.IsFalse(record.Due(null));
            Assert.IsTrue(record.CodaPrompts);
            Assert.IsTrue(record.FirstDebrief);
            Assert.IsTrue(p.MarkSeen(SceneScript.IdOf(SceneLibrary.MotorPool)), "a scene played to the end is marked by its id");
            Assert.IsFalse(record.Due(SceneLibrary.MotorPool));
            Assert.IsTrue(record.Mark(StoryRecord.Coda));
            Assert.IsFalse(record.Mark(StoryRecord.Coda), "already marked");
            Assert.IsFalse(record.CodaPrompts);
            record.Mark(StoryRecord.Debrief);
            Assert.IsFalse(record.FirstDebrief);
            // the keys are in players' saves
            Assert.AreEqual("coda", StoryRecord.Coda);
            Assert.AreEqual("debrief", StoryRecord.Debrief);
            Assert.AreEqual("super", StoryRecord.Super);
            Assert.AreEqual("scene-motor-pool", SceneScript.IdOf(SceneLibrary.MotorPool));
        }

        [Test]
        public void TheBossIntroIsQuickOnAReplayOrARetryAtTheBoss()
        {
            var p = new Profile();
            var story = new StoryRecord(p);
            Assert.IsFalse(story.QuickBossIntro(Missions.One, lostAtBoss: false), "the first run meets the tabk properly");
            Assert.IsTrue(story.QuickBossIntro(Missions.One, lostAtBoss: true), "a retry at the boss door");
            p.RecordRun(Missions.One, 100, won: false, solo: true);
            Assert.IsFalse(story.QuickBossIntro(Missions.One, lostAtBoss: false), "a lost run isn't a replay");
            p.RecordRun(Missions.One, 100, won: true, solo: true);
            Assert.IsTrue(story.QuickBossIntro(Missions.One, lostAtBoss: false), "cleared: a replay");
        }

        [Test]
        public void TheSuperShowsOnceItsChargedOrCoopIsOpen()
        {
            var p = new Profile();
            var story = new StoryRecord(p);
            Assert.IsFalse(story.SuperShown, "hidden until it first charges");
            Assert.IsTrue(story.Mark(StoryRecord.Super));
            Assert.IsTrue(story.SuperShown);
            Assert.IsFalse(story.Mark(StoryRecord.Super), "once");

            var veteran = new Profile();
            veteran.RecordRun(Missions.One, 100, won: true, solo: true);
            Assert.IsTrue(StoryRecord.ShowsSuper(veteran), "a save past Mission one has seen it");
        }

        [Test]
        public void TheBossRevealPlaysOnceSolo_EveryCoopRunWhereTheStorySays_AndTestsSkipIt()
        {
            MissionStory one = Missions.One.Story, two = Missions.Two.Story;
            var solo = new StoryRun(coop: false);
            Assert.IsTrue(one.PlaysBossReveal(solo, firstWake: true, due: true));
            Assert.IsFalse(one.PlaysBossReveal(solo, firstWake: true, due: false), "seen once already");
            Assert.IsFalse(one.PlaysBossReveal(solo, firstWake: false, due: true), "a retry at the boss");
            Assert.IsFalse(one.PlaysBossReveal(new StoryRun(coop: true), true, true), "Mission one's isn't for co-op");
            Assert.IsTrue(two.PlaysBossReveal(new StoryRun(coop: true), firstWake: false, due: false), "Mission two's plays in every co-op run, whatever the saves say");
            Assert.IsFalse(one.PlaysBossReveal(new StoryRun(false, testing: true), true, true), "a test start skips it");
            Assert.IsTrue(one.PlaysBossReveal(new StoryRun(false, testing: true, testReveal: true), false, false), "?test=reveal plays it");
            Assert.IsFalse(two.PlaysBossReveal(new StoryRun(true, sandbox: true, testReveal: true), true, true), "never in a sandbox");
            Assert.IsFalse(MissionStory.None.PlaysBossReveal(solo, true, true), "no reveal, nothing to play");
        }

        [Test]
        public void FreeingThePilotPlaysItsScene_SoloOrWhereTheStorySaysInCoop_NeverInASandbox()
        {
            MissionStory one = Missions.One.Story, two = Missions.Two.Story;
            Assert.IsTrue(one.PlaysPilotFreed(new StoryRun(coop: false)));
            Assert.IsFalse(one.PlaysPilotFreed(new StoryRun(coop: true)), "Mission one's pilot goes the goons' way in co-op");
            Assert.IsFalse(one.PilotGlitches(coop: true));
            Assert.IsTrue(two.PlaysPilotFreed(new StoryRun(coop: true)), "freeing kcaj plays on both phones");
            Assert.IsTrue(two.PilotGlitches(coop: true));
            Assert.IsFalse(two.PlaysPilotFreed(new StoryRun(false, sandbox: true)), "a sandbox skips it");
            Assert.IsFalse(MissionStory.None.PlaysPilotFreed(new StoryRun(false)), "no scene: won at once");
        }
    }
}
