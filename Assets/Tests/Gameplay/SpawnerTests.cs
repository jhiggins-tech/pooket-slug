using System;
using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>The spawner's tables (<see cref="Spawner"/>): every brain and map marker places something, and the right look.</summary>
    public class SpawnerTests : GameplayFixture
    {
        [Test]
        public void EveryBrainHasAClassToPlayIt()
        {
            foreach (EnemyBrain brain in Enum.GetValues(typeof(EnemyBrain)))
                CollectionAssert.Contains(Spawner.Brains, brain, brain + " has no row in the spawner");
        }

        [Test]
        public void EveryMapMarkerPlacesSomething()
        {
            foreach (SpawnKind kind in Level.Markers.Values)
                if (Content.EnemyFor(kind) == null && Content.PickupFor(kind) == null)
                    CollectionAssert.Contains(Spawner.Scenery, kind, kind + " is on maps but the spawner places nothing for it");
        }

        [Test]
        public void GoonsAreTrollogramsAndSetPiecesAreReal()
        {
            Make("..P......s.x.c");
            Assert.IsNotNull(First(EnemyBrain.Rifleman).Look, "a goon wears the Trollogram look");
            Assert.IsNull(First(EnemyBrain.Barrel).Look, "a barrel is real");
            Assert.IsNull(First(EnemyBrain.ConeBox).Look, "so is a box of cones");
        }

        [Test]
        public void ALevelSpawnKeepsItsIndexAsItsId()
        {
            Make("..P.......s..x");
            Assert.AreEqual(0, First(EnemyBrain.Rifleman).NetId);
            Assert.AreEqual(1, First(EnemyBrain.Barrel).NetId);
        }

        [Test]
        public void ABossTabkOrPilotIsTheWorldsBoss()
        {
            Make("..P");
            Enemy tank = Spawn(Content.Tank, 8f);
            Assert.AreSame(tank, World.Boss);
            Enemy pilot = Spawn(Content.KcajPilot, 6f);
            Assert.AreSame(pilot, World.Boss);
            Assert.IsInstanceOf<KcajPilot>(pilot);
        }

        [Test]
        public void ThingsSpawnedMidGameGetIdsAfterTheLevels()
        {
            Make("..P.......s");
            Enemy weasel = Spawn(Content.Weasel, 4f);
            Assert.Greater(weasel.NetId, 0);
            Assert.IsNull(weasel.Look, "kie's weasels are real");
        }
    }
}
