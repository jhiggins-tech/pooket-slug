using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The mission screen (Core/Missions/MissionBoard.cs, docs/design/levels.md, Missions): replays, their records and the next mission.</summary>
    public class MissionBoardTests
    {
        static readonly MissionDef Later = new MissionDef("mission-test", 200, "MISSION X", "A TEST MAP", new[] { "#" });

        [Test]
        public void BeforeMissionOneIsCleared_OnlyItShows()
        {
            List<MissionCard> cards = MissionBoard.Cards(new Profile());
            Assert.AreEqual(1, cards.Count);
            Assert.AreSame(Missions.One, cards[0].Mission);
            Assert.AreEqual(1, cards[0].Number);
            Assert.IsTrue(cards[0].SoloOnly);
            Assert.AreEqual("NOT CLEARED YET", MissionBoard.RecordLine(cards[0]));
        }

        [Test]
        public void OnceMissionOneIsCleared_ItsReplayShows_AndMissionTwoIsReached()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, 12400, won: true, solo: true);
            p.RecordRun(Missions.One, 9000, won: true, solo: true);
            List<MissionCard> cards = MissionBoard.Cards(p);
            Assert.AreEqual(2, cards.Count, "Mission one, then Mission two, not yet cleared");
            Assert.AreEqual(12400, cards[0].Best);
            Assert.AreEqual(2, cards[0].Clears);
            Assert.AreEqual("BEST 12,400 · CLEARED 2 TIMES", MissionBoard.RecordLine(cards[0]));
            Assert.AreSame(Missions.Two, cards[1].Mission);
            Assert.AreEqual(2, cards[1].Number);
            Assert.IsFalse(cards[1].SoloOnly, "Mission two is open to co-op");
            Assert.AreEqual("NOT CLEARED YET", MissionBoard.RecordLine(cards[1]));
        }

        [Test]
        public void OnceEveryMissionIsCleared_TheNextIsComingSoon()
        {
            var p = new Profile();
            foreach (MissionDef m in Missions.All) p.RecordRun(m, 1000, won: true, solo: true);
            List<MissionCard> cards = MissionBoard.Cards(p);
            Assert.AreEqual(Missions.All.Count + 1, cards.Count, "every mission, then the next");
            MissionCard next = cards[cards.Count - 1];
            Assert.IsTrue(next.ComingSoon);
            Assert.AreEqual(Missions.All.Count + 1, next.Number);
            Assert.AreEqual("COMING SOON", MissionBoard.RecordLine(next));
        }

        [Test]
        public void ALostRun_KeepsItsBest_ButIsNoClear()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, 3000, won: false, solo: true);
            MissionCard one = MissionBoard.Cards(p)[0];
            Assert.IsFalse(one.Cleared);
            Assert.AreEqual("BEST 3,000 · NOT CLEARED YET", MissionBoard.RecordLine(one));
            p.RecordRun(Missions.One, 5000, won: true, solo: true);
            Assert.AreEqual("BEST 5,000 · CLEARED ONCE", MissionBoard.RecordLine(MissionBoard.Cards(p)[0]));
        }

        [Test]
        public void AMissionIsCleared_ByAWin()
        {
            var p = new Profile();
            Assert.IsFalse(MissionBoard.Cleared(p, Later));
            p.RecordRun(Later, 100, won: false, solo: false);
            Assert.IsFalse(MissionBoard.Cleared(p, Later), "a lost run isn't a clear");
            p.RecordRun(Later, 900, won: true, solo: false);
            Assert.IsTrue(MissionBoard.Cleared(p, Later));
            Assert.IsFalse(MissionBoard.Cleared(null, Later));
        }

        [Test]
        public void AClearWithNoBest_SaysOnlyItsClears()
        {
            var card = new MissionCard { Mission = Later, Best = 0, Clears = 1 };
            Assert.AreEqual("CLEARED ONCE", MissionBoard.RecordLine(card), "no best of nothing");
        }

        [Test]
        public void EveryMission_HasAPlaceForItsCard()
        {
            foreach (MissionDef m in Missions.All) Assert.IsFalse(string.IsNullOrEmpty(m.Place), m.Id);
        }

        [Test]
        public void TheStartBanner_NamesTheMission_ButNotAStandInInCoop()
        {
            Assert.AreEqual(Later.Name + "  START!", MissionBoard.StartBanner(Later, coop: false));
            Assert.AreEqual(Later.Name + "  START!", MissionBoard.StartBanner(Later, coop: true));
            Assert.AreEqual(Missions.One.Name + "  START!", MissionBoard.StartBanner(Missions.One, coop: false));
            Assert.AreEqual("START!", MissionBoard.StartBanner(Missions.One, coop: true), "co-op plays Mission one only as a stand-in");
        }
    }
}
