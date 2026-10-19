using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The start flow (Core/Missions/StartFlow.cs): the story runs straight through to Mission two, then the den.</summary>
    public class StartFlowTests
    {
        [Test]
        public void StartPlaysMissionOneThenMissionTwoThenTheDen()
        {
            var p = new Profile();
            Assert.AreSame(Missions.One, StartFlow.Next(p), "a new player: the opening and Mission one");
            Assert.IsFalse(StartFlow.DenOpen(p));
            p.RecordRun(Missions.One, Setting.Normal, 100, won: false, solo: true);
            Assert.AreSame(Missions.One, StartFlow.Next(p), "a lost run doesn't move it on");
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
            Assert.AreSame(Missions.Two, StartFlow.Next(p), "Mission one's beaten: its debrief goes on into Mission two");
            Assert.IsFalse(StartFlow.DenOpen(p), "the den waits for Mission two");
            p.RecordRun(Missions.Two, Setting.Normal, 100, won: true, solo: true);
            Assert.IsNull(StartFlow.Next(p));
            Assert.IsTrue(StartFlow.DenOpen(p), "both beaten: START and the debrief go to the den");
        }

        [Test]
        public void MissionTwoOpensWithItsBriefing()
        {
            Assert.AreEqual(SceneLibrary.MissionTwoBriefing, Missions.Two.Story.Opening);
            CollectionAssert.Contains(SceneLibrary.Names, Missions.Two.Story.Opening, "the game has the scene");
            Assert.IsTrue(Missions.One.Story.BeforeDen && Missions.Two.Story.BeforeDen);
        }

        [Test]
        public void ASaveFromBeforeKeepsItsPlaceInTheRun()
        {
            // co-op used to open with Mission one: such a save still has Mission two to play before the den
            var p = new Profile { coop = true };
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
            Assert.AreSame(Missions.Two, StartFlow.Next(p));
        }
    }
}
