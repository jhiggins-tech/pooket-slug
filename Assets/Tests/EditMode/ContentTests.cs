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
        public void EachTableHoldsItsDefinitionsAtTheirIds()
        {
            for (int i = 0; i < Content.Projectiles.Count; i++) Assert.AreEqual(i, Content.Projectiles[i].Id, Content.Projectiles[i].Name);
            for (int i = 0; i < Content.Weapons.Count; i++) Assert.AreEqual(i, Content.Weapons[i].Id, Content.Weapons[i].Name);
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
