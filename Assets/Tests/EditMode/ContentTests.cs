using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class ContentTests
    {
        [Test]
        public void WeaponIdsNeverChange()
        {
            // Shots are replicated by weapon id, so changing one breaks co-op between builds.
            Assert.AreEqual(0, Content.Pistol.Id);
            Assert.AreEqual(1, Content.HeavyMachineGun.Id);
            Assert.AreEqual(2, Content.Rifle.Id);
            Assert.AreEqual(3, Content.Cannon.Id);
            Assert.AreEqual(4, Content.GrenadeThrow.Id);
            Assert.AreEqual(5, Content.DroneBomb.Id);
            Assert.AreEqual(6, Content.TankGun.Id);
        }

        [Test]
        public void EnemyIdsNeverChange()
        {
            // The co-op snapshot carries each enemy's id.
            Assert.AreEqual(0, Content.Rifleman.Id);
            Assert.AreEqual(1, Content.Runner.Id);
            Assert.AreEqual(2, Content.Drone.Id);
            Assert.AreEqual(3, Content.Tank.Id);
            Assert.AreEqual(4, Content.Barrel.Id);
        }

        [Test]
        public void EachTableHoldsItsDefinitionsAtTheirIds()
        {
            for (int i = 0; i < Content.Projectiles.Count; i++) Assert.AreEqual(i, Content.Projectiles[i].Id, Content.Projectiles[i].Name);
            for (int i = 0; i < Content.Weapons.Count; i++) Assert.AreEqual(i, Content.Weapons[i].Id, Content.Weapons[i].Name);
            for (int i = 0; i < Content.Enemies.Count; i++) Assert.AreEqual(i, Content.Enemies[i].Id, Content.Enemies[i].Name);
            for (int i = 0; i < Content.Pickups.Count; i++) Assert.AreEqual(i, Content.Pickups[i].Id, Content.Pickups[i].Name);
            for (int i = 0; i < Content.Soldiers.Count; i++) Assert.AreEqual(i, Content.Soldiers[i].Id, Content.Soldiers[i].Name);
        }

        [TestCase(SpawnKind.CrateHeavy, PickupEffect.Gun)]
        [TestCase(SpawnKind.CrateBombs, PickupEffect.Specials)]
        [TestCase(SpawnKind.Medkit, PickupEffect.Heal)]
        [TestCase(SpawnKind.OneUp, PickupEffect.Life)]
        public void EachCrateMarkerPlacesItsPickup(SpawnKind marker, PickupEffect effect)
        {
            Assert.AreEqual(effect, Content.PickupFor(marker).Effect);
        }

        [TestCase(SpawnKind.Soldier)]
        [TestCase(SpawnKind.Tank)]
        [TestCase(SpawnKind.Flag)]
        public void OtherMarkersPlaceNoPickup(SpawnKind marker)
        {
            Assert.IsNull(Content.PickupFor(marker));
        }

        [Test]
        public void EveryPickupDoesSomething()
        {
            foreach (PickupDef p in Content.Pickups)
            {
                if (p.Effect == PickupEffect.Gun)
                {
                    Assert.IsNotNull(p.Gun, p.Name);
                    Assert.Greater(p.Gun.Ammo, 0, p.Name + ": a crate's gun replaces the primary until it runs dry");
                }
                else if (p.Effect != PickupEffect.Specials) Assert.Greater(p.Amount, 0, p.Name); // bombs: the soldier's own amount
                Assert.IsNotEmpty(p.Announce, p.Name);
            }
        }

        [Test]
        public void EverySoldierGetsSomethingFromABombCrate()
        {
            foreach (SoldierDef s in Content.Soldiers) Assert.Greater(s.CrateSpecials, 0, s.Name);
            Assert.Greater(SpikeContent.Tones2.CrateSpecials, 0);
        }

        [Test]
        public void EverySoldiersKitFollowsTheDesign()
        {
            foreach (SoldierDef s in Content.Soldiers)
            {
                Assert.AreEqual(0, s.Primary.Ammo, s.Name + ": tier 1 has unlimited ammo");
                Assert.IsNotNull(s.Special, s.Name);
                Assert.Greater(s.StartSpecials, 0, s.Name + ": the special is fired with charges");
                Assert.Less(s.CrouchHeight, s.StandHeight, s.Name);
                Assert.Greater(s.MaxHp, 0, s.Name);
                Assert.Greater(s.RunSpeed, s.CrawlSpeed, s.Name);
            }
        }

        [TestCase(SpawnKind.Soldier, EnemyBrain.Rifleman)]
        [TestCase(SpawnKind.Runner, EnemyBrain.Runner)]
        [TestCase(SpawnKind.Drone, EnemyBrain.Drone)]
        [TestCase(SpawnKind.Tank, EnemyBrain.Tank)]
        [TestCase(SpawnKind.Barrel, EnemyBrain.Barrel)]
        public void EachEnemyMarkerPlacesItsEnemy(SpawnKind marker, EnemyBrain brain)
        {
            Assert.AreEqual(brain, Content.EnemyFor(marker).Brain);
        }

        [TestCase(SpawnKind.CrateHeavy)]
        [TestCase(SpawnKind.CrateBombs)]
        [TestCase(SpawnKind.Medkit)]
        [TestCase(SpawnKind.OneUp)]
        [TestCase(SpawnKind.Flag)]
        [TestCase(SpawnKind.LiveFence)]
        [TestCase(SpawnKind.SteamVent)]
        [TestCase(SpawnKind.Icicles)]
        [TestCase(SpawnKind.Watchtower)]
        [TestCase(SpawnKind.Lift)]
        [TestCase(SpawnKind.Checkpoint)]
        public void OtherMarkersPlaceNoEnemy(SpawnKind marker)
        {
            Assert.IsNull(Content.EnemyFor(marker));
        }

        [Test]
        public void SoldierIdsNeverChange()
        {
            // Each player's soldier is replicated by id.
            Assert.AreEqual(0, Content.Trooper.Id);
            Assert.AreSame(Content.Trooper, Content.Soldier(0));
            Assert.IsNull(Content.Soldier(-1));
            Assert.IsNull(Content.Soldier(Content.Soldiers.Count));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void AnUnknownEnemyIdFindsNothing(int id)
        {
            Assert.IsNull(Content.Enemy(id));
        }

        [Test]
        public void EveryEnemyHasABodyHealthAndWorth()
        {
            foreach (EnemyDef e in Content.Enemies)
            {
                Assert.Greater(e.HalfWidth, 0f, e.Name);
                Assert.Greater(e.Height, 0f, e.Name);
                Assert.Greater(e.Hp, 0, e.Name);
                Assert.Greater(e.Score, 0, e.Name);
            }
        }

        [TestCase(-1)]
        [TestCase(7)]
        [TestCase(255)]
        public void AnUnknownWeaponIdFindsNothing(int id)
        {
            Assert.IsNull(Content.Weapon(id));
        }

        [Test]
        public void AKnownWeaponIdFindsItsWeapon()
        {
            Assert.AreSame(Content.HeavyMachineGun, Content.Weapon(Content.HeavyMachineGun.Id));
        }

        [Test]
        public void EveryWeaponFiresARegisteredProjectile()
        {
            foreach (WeaponDef w in Content.Weapons)
                Assert.AreSame(w.Projectile, Content.Projectiles[w.Projectile.Id], w.Name);
        }

        [Test]
        public void EveryProjectileCanHurtSomething()
        {
            foreach (ProjectileDef p in Content.Projectiles)
            {
                Assert.IsTrue(p.ContactDamage || p.Explodes, p.Name + " neither hurts on contact nor explodes");
                Assert.Greater(p.Damage, 0, p.Name);
                Assert.Greater(p.Life, 0f, p.Name);
            }
        }

        [Test]
        public void WeaponsThatFireStraightHaveASpeed()
        {
            foreach (WeaponDef w in Content.Weapons)
                if (w.Behaviour == WeaponBehaviour.Bullet) Assert.Greater(w.Speed, 0f, w.Name);
        }

        [Test]
        public void AGrenadeIsThrownForwardAndUp()
        {
            // As the soldier always threw it: (facing × 6.5 + 0.3 of their speed, 9)
            Vector2 v = Content.GrenadeThrow.LobVelocity(-1, new Vector2(4f, 3f));
            Assert.AreEqual(-6.5f + 4f * 0.3f, v.x, 1e-5f);
            Assert.AreEqual(9f, v.y, 1e-5f);
        }

        [Test]
        public void ADroneBombDropsWithHalfTheDronesSpeed()
        {
            Vector2 v = Content.DroneBomb.LobVelocity(1, new Vector2(-2f, 0.5f));
            Assert.AreEqual(-1f, v.x, 1e-5f);
            Assert.AreEqual(0f, v.y, 1e-5f);
        }

        [Test]
        public void OnlyTheCratesGunRunsDry()
        {
            Assert.AreEqual(0, Content.Pistol.Ammo);
            Assert.Greater(Content.HeavyMachineGun.Ammo, 0);
        }
    }
}
