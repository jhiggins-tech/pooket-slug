using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class MissionsTests
    {
        [Test]
        public void MissionOnesIdsNeverChange()
        {
            // in players' saves, and on the wire
            Assert.AreEqual("mission-1", Missions.One.Id);
            Assert.AreEqual(0, Missions.One.NetId);
            Assert.AreSame(Missions.One, Missions.All[0]);
        }

        [Test]
        public void MissionTwosIdsNeverChange()
        {
            Assert.AreEqual("mission-2", Missions.Two.Id);
            Assert.AreEqual(1, Missions.Two.NetId);
            Assert.AreSame(Missions.Two, Missions.All[1]);
        }

        [Test]
        public void MissionOnesBossIsKiesTabkWithHisPilot_MissionTwosIsKcajsWithHis()
        {
            Assert.AreEqual(BossTabk.Kie, Missions.One.Boss);
            Assert.AreSame(Content.Pilot, Missions.One.Pilot);
            Assert.AreEqual(BossTabk.Kcaj, Missions.Two.Boss);
            Assert.AreSame(Content.KcajPilot, Missions.Two.Pilot);
        }

        [Test]
        public void FreeingKcajPlaysOnBothPhones_FreeingKieIsSoloOnly()
        {
            Assert.AreEqual(SceneLibrary.FreeingKcaj, Missions.Two.Story.PilotFreed);
            Assert.IsTrue(Missions.Two.Story.PilotFreedCoop, "every Mission two scene plays on both phones in co-op");
            Assert.IsFalse(Missions.Two.Story.Coda);
            Assert.AreEqual(SceneLibrary.FreeingKie, Missions.One.Story.PilotFreed);
            Assert.IsFalse(Missions.One.Story.PilotFreedCoop);
        }

        [Test]
        public void BothBossTabksArriveInARevealScene_MissionTwosOnBothPhones()
        {
            Assert.AreEqual(SceneLibrary.MotorPool, Missions.One.Story.BossReveal);
            Assert.AreEqual(SceneLibrary.KcajTabk, Missions.Two.Story.BossReveal);
            CollectionAssert.Contains(SceneLibrary.Names, SceneLibrary.KcajTabk);
            Assert.IsTrue(Missions.Two.Story.BossRevealCoop, "Mission two is played in co-op");
            Assert.IsFalse(Missions.One.Story.BossRevealCoop, "Mission one is solo only");
        }

        [Test]
        public void TrolloKcajsBandIsForWhenTheRevealDoesntPlay_TrolloKieIsOnlyInTheReveal()
        {
            Assert.AreEqual("TROLLO-KCAJ", Missions.Two.Story.PilotIntro);
            Assert.AreEqual("YOU'RE NOT IN MY NOTES.", Missions.Two.Story.PilotIntroLine);
            Assert.IsNull(Missions.One.Story.PilotIntro, "Trollogram kie's intro is in the motor pool reveal");
        }

        [Test]
        public void ACoopRoomPlaysTheFirstMissionOpenToCoop()
        {
            Assert.IsTrue(Missions.One.Story.SoloOnly);
            Assert.AreSame(Missions.Two, Missions.ForCoop(Missions.One), "Mission one is solo only");
            Assert.AreSame(Missions.Two, Missions.ForCoop(Missions.Two));
            Assert.AreSame(Missions.Two, Missions.ForCoop(null));
        }

        [Test]
        public void EveryMissionHasItsOwnIds()
        {
            var ids = new HashSet<string>();
            var netIds = new HashSet<int>();
            foreach (MissionDef m in Missions.All)
            {
                Assert.IsTrue(ids.Add(m.Id), m.Id);
                Assert.IsTrue(netIds.Add(m.NetId), m.Id);
                Assert.IsFalse(string.IsNullOrEmpty(m.Name), m.Id);
            }
        }

        [Test]
        public void LookupsFindEachMissionAndNothingElse()
        {
            foreach (MissionDef m in Missions.All)
            {
                Assert.AreSame(m, Missions.ById(m.Id));
                Assert.AreSame(m, Missions.ByNetId(m.NetId));
            }
            Assert.IsNull(Missions.ById("mission-0"));
            Assert.IsNull(Missions.ByNetId(255));
        }

        [Test]
        public void EveryMissionStartsOnTheGroundAndEndsAtTheFlag()
        {
            foreach (MissionDef m in Missions.All)
            {
                Level level = m.Load();
                Assert.AreEqual(level.TopSurfaceY(level.PlayerStart.x), level.PlayerStart.y, m.Id);
                Assert.AreEqual(SpawnKind.Flag, level.Spawns[level.Spawns.Count - 1].Kind, m.Id);
            }
        }

        [Test]
        public void EveryMissionHasOneBoss()
        {
            // beating the boss wins the mission
            foreach (MissionDef m in Missions.All)
            {
                int bosses = 0;
                foreach (Spawn s in m.Load().Spawns)
                    if (s.Kind == SpawnKind.Tank) bosses++;
                Assert.AreEqual(1, bosses, m.Id);
            }
        }

        [Test]
        public void EveryMissionsPickupsFitTheSharedPickupTable()
        {
            // pickups never despawn, so in the worst case they're all in the table at once
            foreach (MissionDef m in Missions.All)
            {
                int pickups = 0;
                foreach (Spawn s in m.Load().Spawns)
                    if (Content.PickupFor(s.Kind) != null) pickups++;
                Assert.LessOrEqual(pickups, NetLimits.Pickups, m.Id);
            }
        }

        [Test]
        public void NoMissionHandsOutAClassicGun()
        {
            // player.md: no classic guns; Mission one's heavy machine gun crate became a bomb crate (design audit, 18 Oct)
            foreach (MissionDef m in Missions.All)
                foreach (Spawn s in m.Load().Spawns)
                    Assert.AreNotEqual(PickupEffect.Gun, Content.PickupFor(s.Kind)?.Effect, m.Id + " at " + s.Pos);
        }
    }
}
