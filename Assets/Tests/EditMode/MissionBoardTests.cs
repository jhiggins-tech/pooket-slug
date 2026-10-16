using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The mission screen (Core/MissionBoard.cs, docs/design/levels.md, Missions): replays and the hard setting.</summary>
    public class MissionBoardTests
    {
        static readonly MissionDef Later = new MissionDef("mission-test", 200, "MISSION X", "A TEST MAP", new[] { "#" });

        [Test]
        public void BeforeMissionOneIsCleared_OnlyItShows()
        {
            List<MissionCard> cards = MissionBoard.Cards(new Profile(), coop: false);
            Assert.AreEqual(1, cards.Count);
            Assert.AreSame(Missions.One, cards[0].Mission);
            Assert.AreEqual(1, cards[0].Number);
            Assert.IsTrue(cards[0].SoloOnly);
            Assert.AreEqual("NOT CLEARED YET", MissionBoard.RecordLine(cards[0], Setting.Normal));
        }

        [Test]
        public void OnceMissionOneIsCleared_ItsReplayShows_AndTheNextMissionIsComingSoon()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 12400, won: true, solo: true);
            p.RecordRun(Missions.One, Setting.Normal, 9000, won: true, solo: true);
            List<MissionCard> cards = MissionBoard.Cards(p, coop: false);
            Assert.AreEqual(Missions.All.Count + 1, cards.Count, "every mission, then the next");
            Assert.AreEqual(12400, cards[0].Best);
            Assert.AreEqual(2, cards[0].Clears);
            Assert.AreEqual("BEST 12,400 · CLEARED 2 TIMES", MissionBoard.RecordLine(cards[0], Setting.Normal));
            MissionCard next = cards[cards.Count - 1];
            Assert.IsTrue(next.ComingSoon);
            Assert.AreEqual(Missions.All.Count + 1, next.Number);
            Assert.AreEqual("COMING SOON", MissionBoard.RecordLine(next, Setting.Normal));
        }

        [Test]
        public void ALostRun_KeepsItsBest_ButIsNoClear()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 3000, won: false, solo: true);
            MissionCard one = MissionBoard.Cards(p, coop: false)[0];
            Assert.IsFalse(one.Cleared);
            Assert.AreEqual("BEST 3,000 · NOT CLEARED YET", MissionBoard.RecordLine(one, Setting.Normal));
            p.RecordRun(Missions.One, Setting.Normal, 5000, won: true, solo: true);
            Assert.AreEqual("BEST 5,000 · CLEARED ONCE", MissionBoard.RecordLine(MissionBoard.Cards(p, coop: false)[0], Setting.Normal));
        }

        [Test]
        public void MissionOne_NeverHasAHardSetting()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
            Assert.IsFalse(MissionBoard.HardOpen(p, Missions.One, coop: true));
            Assert.IsFalse(MissionBoard.Cards(p, coop: true)[0].HardOpen);
        }

        [Test]
        public void HardOpens_ForALaterMission_OnceCleared_InCoopOnly()
        {
            var p = new Profile();
            Assert.IsFalse(MissionBoard.HardOpen(p, Later, coop: true), "not cleared yet");
            p.RecordRun(Later, Setting.Normal, 100, won: true, solo: false);
            Assert.IsTrue(MissionBoard.HardOpen(p, Later, coop: true));
            Assert.IsFalse(MissionBoard.HardOpen(p, Later, coop: false), "hard is tuned for two");
        }

        [Test]
        public void HardIsPlayed_OnlyWhereItsOpen()
        {
            var open = new MissionCard { Mission = Later, Number = 2, HardOpen = true };
            var shut = new MissionCard { Mission = Missions.One, Number = 1 };
            Assert.AreEqual(Setting.Hard, MissionBoard.SettingFor(open, wantHard: true));
            Assert.AreEqual(Setting.Normal, MissionBoard.SettingFor(open, wantHard: false));
            Assert.AreEqual(Setting.Normal, MissionBoard.SettingFor(shut, wantHard: true));
        }

        [Test]
        public void HardRecords_AreKeptApart()
        {
            var p = new Profile();
            p.RecordRun(Later, Setting.Normal, 100, won: true, solo: false);
            p.RecordRun(Later, Setting.Hard, 900, won: true, solo: false);
            Assert.AreEqual(100, p.Best(Later, Setting.Normal));
            Assert.AreEqual(900, p.Best(Later, Setting.Hard));
            var card = new MissionCard { Mission = Later, Best = 100, Clears = 1, HardBest = 900, HardClears = 1 };
            Assert.AreEqual("BEST 900 · CLEARED ONCE", MissionBoard.RecordLine(card, Setting.Hard));
        }

        [Test]
        public void EveryMission_HasAPlaceForItsCard()
        {
            foreach (MissionDef m in Missions.All) Assert.IsFalse(string.IsNullOrEmpty(m.Place), m.Id);
        }
    }
}
