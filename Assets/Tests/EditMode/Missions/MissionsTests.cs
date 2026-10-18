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
