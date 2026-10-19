using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The start flow (Core/Missions/StartFlow.cs): the opening and Mission one, then the den.</summary>
    public class StartFlowTests
    {
        [Test]
        public void StartPlaysMissionOneThenTheDen()
        {
            var p = new Profile();
            Assert.AreSame(Missions.One, StartFlow.Next(p), "a new player: the opening and Mission one");
            Assert.IsFalse(StartFlow.DenOpen(p));
            p.RecordRun(Missions.One, Setting.Normal, 100, won: false, solo: true);
            Assert.AreSame(Missions.One, StartFlow.Next(p), "a lost run doesn't move it on");
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
            Assert.IsNull(StartFlow.Next(p));
            Assert.IsTrue(StartFlow.DenOpen(p), "Mission one's beaten: START and its debrief go to the den, where Mission two and co-op are");
            Assert.IsTrue(p.coop);
        }

        [Test]
        public void MissionTwoOpensWithItsBriefing()
        {
            Assert.AreEqual(SceneLibrary.MissionTwoBriefing, Missions.Two.Story.Opening);
            CollectionAssert.Contains(SceneLibrary.Names, Missions.Two.Story.Opening, "the game has the scene");
            Assert.IsTrue(Missions.One.Story.BeforeDen);
            Assert.IsFalse(Missions.Two.Story.BeforeDen, "Mission two starts from the den, so it can be played in co-op");
        }
    }
}
