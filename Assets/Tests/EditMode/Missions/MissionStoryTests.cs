using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>Missions' stories (Core/MissionStory.cs): what Mission one's script says, and the once-only beats.</summary>
    public class MissionStoryTests
    {
        [Test]
        public void MissionOneIsASoloPreludeWithItsScenesAndCoda()
        {
            MissionStory s = Missions.One.Story;
            Assert.AreEqual(SceneLibrary.Opening, s.Opening);
            Assert.AreEqual(SceneLibrary.MotorPool, s.BossReveal);
            Assert.AreEqual(SceneLibrary.FreeingKie, s.PilotFreed);
            Assert.IsTrue(s.Coda && s.SoloOnly && s.OpensCoop);
            foreach (string scene in new[] { s.Opening, s.BossReveal, s.PilotFreed })
                CollectionAssert.Contains(SceneLibrary.Names, scene, "every scene a mission plays is one the game has");
        }

        [Test]
        public void AMissionWithoutAStoryHasNone()
        {
            var plain = new MissionDef("mission-test", 200, "MISSION X", "A TEST MAP", new[] { "#" });
            Assert.AreSame(MissionStory.None, plain.Story);
            Assert.IsNull(plain.Story.Opening);
            Assert.IsFalse(plain.Story.SoloOnly || plain.Story.OpensCoop || plain.Story.Coda);
            Assert.IsFalse(MissionBoard.SoloOnly(plain));
            var p = new Profile();
            p.RecordRun(plain, Setting.Normal, 100, won: true, solo: true);
            Assert.IsFalse(p.coop, "only a mission whose story says so opens co-op");
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
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
            Assert.AreEqual("scene-motor-pool", SceneScript.IdOf(SceneLibrary.MotorPool));
        }
    }
}
