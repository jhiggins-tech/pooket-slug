using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>Set pieces (<see cref="SetPiece"/>): each does its thing once as it's destroyed, then goes.</summary>
    public class SetPieceTests : GameplayFixture
    {
        void Destroy(Enemy e)
        {
            World.Combat.DamageEnemy(e, e.Hp);
            Run(0.2f);
        }

        [Test]
        public void ABarrelBlowsUpTheGoonsNextToIt()
        {
            Make("..P......sx");
            Enemy goon = First(EnemyBrain.Rifleman), barrel = First(EnemyBrain.Barrel);
            Destroy(barrel);
            Assert.That(barrel.Removed, "a barrel goes at once");
            Assert.That(goon.Dying || goon.Removed, "and takes the goon with it");
        }

        [Test]
        public void ABoxOfConesDropsAnItem()
        {
            Make("..P......c");
            int before = World.Pickups.Count;
            Destroy(First(EnemyBrain.ConeBox));
            Assert.AreEqual(before + 1, World.Pickups.Count);
        }

        [Test]
        public void ANitrogenTankFreezesTheGoonsNearIt()
        {
            Make("..P......hn");
            Enemy shield = First(EnemyBrain.Shield);
            Destroy(First(EnemyBrain.NitrogenTank));
            Assert.That(shield.Frost.Frozen, "frozen by the blast");
            Assert.Less(shield.Hp, Content.ShieldSoldier.Hp, "and a little hurt");
        }

        [Test]
        public void AProjectorShotOutSwitchesOffItsGoons()
        {
            Make("..P......J..s");
            Enemy goon = First(EnemyBrain.Rifleman), projector = First(EnemyBrain.Projector);
            Destroy(projector);
            Assert.That(goon.Dying || goon.Removed, "its goon flickers out");
            CollectionAssert.IsNotEmpty(World.DarkProjectors);
            Run(1f);
            Assert.That(projector.Removed, "it sputters out, then goes");
        }

        [Test]
        public void AProjectorIsBoltedDown()
        {
            Make("..P......J");
            Enemy projector = First(EnemyBrain.Projector);
            Vector2 at = projector.Pos;
            Run(1f);
            Assert.AreEqual(at, projector.Pos);
        }

        [Test]
        public void TheWatchtowerBarsTheRoadUntilItsShotDown()
        {
            Make("..P......Y");
            Enemy tower = First(EnemyBrain.ToppleTower);
            int column = Mathf.FloorToInt(tower.Pos.x);
            Assert.That(World.Level.Barred(column));
            Destroy(tower);
            Assert.IsFalse(World.Level.Barred(column));
            Run(2f);
            Assert.IsFalse(tower.Removed, "the wreck lies across the road");
        }
    }
}
