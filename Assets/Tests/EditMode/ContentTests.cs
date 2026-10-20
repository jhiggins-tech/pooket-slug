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
            Assert.AreEqual(7, Content.WaterJet.Id);
            Assert.AreEqual(8, Content.RocketJump.Id);
            Assert.AreEqual(9, Content.Spew.Id);
            Assert.AreEqual(11, Content.WeaselPop.Id);
            Assert.AreEqual(12, Content.Trollogram.Id);
            Assert.AreEqual(13, Content.Steal.Id);
            Assert.AreEqual(17, Content.StolenCannon.Id);
            Assert.AreEqual(21, Content.GoonGrenadeThrow.Id);
            Assert.AreEqual(22, Content.ScoopSlam.Id);
            Assert.AreEqual(23, Content.ScoopHurl.Id);
            Assert.AreEqual(24, Content.DoublePark.Id);
            Assert.AreEqual(25, Content.Hyperfixate.Id);
            Assert.AreEqual(26, Content.Unmedicated.Id);
            Assert.AreEqual(14, Content.Cone.Id, "Double Park's cone rides the network by its id too");
            Assert.AreEqual(27, Content.TrolloCones.Id);
            Assert.AreEqual(15, Content.TrolloCone.Id);
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
            Assert.AreEqual(5, Content.Weasel.Id);
            Assert.AreEqual(6, Content.TankCopy.Id);
            Assert.AreEqual(11, Content.ConeBox.Id);
            Assert.AreEqual(12, Content.NitrogenTank.Id);
            Assert.AreEqual(13, Content.ShieldSoldier.Id);
            Assert.AreEqual(14, Content.Grenadier.Id);
            Assert.AreEqual(15, Content.BigScoop.Id);
            Assert.AreEqual(16, Content.Projector.Id);
            Assert.AreEqual(17, Content.KcajPilot.Id);
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

        [TestCase(SpawnKind.Rifleman)]
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
            Assert.AreEqual(3, Content.Tones2.CrateSpecials, "the rocket jump is a strong special: a bomb crate gives 3");
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

        [TestCase(SpawnKind.Rifleman, EnemyBrain.Rifleman)]
        [TestCase(SpawnKind.Runner, EnemyBrain.Runner)]
        [TestCase(SpawnKind.Drone, EnemyBrain.Drone)]
        [TestCase(SpawnKind.Tank, EnemyBrain.Tank)]
        [TestCase(SpawnKind.Barrel, EnemyBrain.Barrel)]
        [TestCase(SpawnKind.ConeBox, EnemyBrain.ConeBox)]
        [TestCase(SpawnKind.NitrogenTank, EnemyBrain.NitrogenTank)]
        [TestCase(SpawnKind.ShieldSoldier, EnemyBrain.Shield)]
        [TestCase(SpawnKind.Grenadier, EnemyBrain.Grenadier)]
        [TestCase(SpawnKind.BigScoop, EnemyBrain.BigScoop)]
        [TestCase(SpawnKind.Projector, EnemyBrain.Projector)]
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
        [TestCase(SpawnKind.TowerScenery)]
        [TestCase(SpawnKind.Lift)]
        [TestCase(SpawnKind.Checkpoint)]
        [TestCase(SpawnKind.ScoopDropper)]
        [TestCase(SpawnKind.NitrogenVent)]
        [TestCase(SpawnKind.ColdStore)]
        public void OtherMarkersPlaceNoEnemy(SpawnKind marker)
        {
            Assert.IsNull(Content.EnemyFor(marker));
        }

        [Test]
        public void OnlyTheseAreRealEverythingElseIsATrollogram()
        {
            // Real: the set pieces, the boss tabk and kie's weasels. A goon added with real left out is a Trollogram.
            var real = new System.Collections.Generic.List<EnemyDef>();
            foreach (EnemyDef e in Content.Enemies)
                if (e.Real) real.Add(e);
            CollectionAssert.AreEquivalent(new[] { Content.Tank, Content.Barrel, Content.Weasel, Content.ToppleTower, Content.ExitGate,
                Content.ConeBox, Content.NitrogenTank, Content.Projector }, real);
        }

        [Test]
        public void EveryMapCharacterPlacesOneThing()
        {
            // the characters each enemy and pickup is placed by, and the scenery's, are one table (Level throws on a repeat)
            Assert.AreEqual('s', Content.Rifleman.Map);
            Assert.AreEqual(SpawnKind.Rifleman, Level.Markers['s']);
            Assert.AreEqual(SpawnKind.CrateHeavy, Level.Markers['H']);
            Assert.AreEqual(SpawnKind.Fog, Level.Markers['f']);
            foreach (EnemyDef e in Content.Enemies)
            {
                if (e.Marker == null) continue;
                Assert.AreNotEqual('\0', e.Map, e.Name + " has a marker but no map character");
                Assert.AreEqual(e.Marker, Level.Markers[e.Map], e.Name);
                Assert.AreSame(e, Content.EnemyFor(e.Marker.Value), e.Name);
            }
            foreach (PickupDef p in Content.Pickups)
                Assert.AreSame(p, Content.PickupFor(Level.Markers[p.Map]), p.Name);
            Assert.IsFalse(Level.Markers.ContainsKey('P'), "the soldier's start isn't a spawn");
            Assert.IsFalse(Level.Markers.ContainsKey(Level.Ground) || Level.Markers.ContainsKey(Level.Block) || Level.Markers.ContainsKey(Level.Empty));
        }

        [Test]
        public void EnemiesMadeOnlyMidGameHaveNoMarker()
        {
            foreach (EnemyDef e in new[] { Content.Weasel, Content.TankCopy, Content.Pilot, Content.PilotDecoy, Content.KcajPilot })
                Assert.IsNull(e.Marker, e.Name);
        }

        [Test]
        public void SoldierIdsNeverChange()
        {
            // Each player's soldier is replicated by id.
            Assert.AreEqual(0, Content.Trooper.Id);
            Assert.AreSame(Content.Trooper, Content.Soldier(0));
            Assert.AreEqual(1, Content.Tones2.Id);
            Assert.AreSame(Content.Tones2, Content.Soldier(1));
            Assert.AreEqual(2, Content.Kie.Id);
            Assert.AreSame(Content.Kie, Content.Soldier(2));
            Assert.AreEqual(3, Content.Kcaj.Id);
            Assert.AreSame(Content.Kcaj, Content.Soldier(3));
            Assert.IsNull(Content.Soldier(-1));
            Assert.IsNull(Content.Soldier(Content.Soldiers.Count));
        }

        [TestCase(-1)]
        [TestCase(18)]
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
        [TestCase(255)]
        public void AnUnknownWeaponIdFindsNothing(int id)
        {
            Assert.IsNull(Content.Weapon(id));
            Assert.IsNull(Content.Weapon(Content.Weapons.Count), "one past the last weapon");
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
            {
                bool pours = w.Behaviour == WeaponBehaviour.Jet || w.Behaviour == WeaponBehaviour.RocketJump || w.Behaviour == WeaponBehaviour.Spew;
                if (pours) Assert.IsNull(w.Projectile, w.Name + ": tones2's kit fires no projectile");
                else if (w.Behaviour == WeaponBehaviour.Weasels || w.Behaviour == WeaponBehaviour.Trollogram || w.Behaviour == WeaponBehaviour.Steal
                    || w.Behaviour == WeaponBehaviour.TeamSuper || w.Behaviour == WeaponBehaviour.Hyperfixate || w.Behaviour == WeaponBehaviour.Unmedicated)
                    Assert.IsNull(w.Projectile, w.Name + ": weasels, holograms, the beam and the pills aren't projectiles");
                else Assert.AreSame(w.Projectile, Content.Projectiles[w.Projectile.Id], w.Name);
            }
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

        [Test]
        public void EverySoldierHasTheSharedBodyUnlessTheirsSaysOtherwise()
        {
            // The body and moves are in Tuning.Soldier once; no soldier overrides them yet.
            foreach (var s in Content.Soldiers)
            {
                Assert.AreEqual(Tuning.Soldier.RunSpeed, s.RunSpeed, s.Name);
                Assert.AreEqual(Tuning.Soldier.CrawlSpeed, s.CrawlSpeed, s.Name);
                Assert.AreEqual(Tuning.Soldier.JumpVelocity, s.JumpVelocity, s.Name);
                Assert.AreEqual(Tuning.Soldier.HalfWidth, s.HalfWidth, s.Name);
                Assert.AreEqual(Tuning.Soldier.StandHeight, s.StandHeight, s.Name);
                Assert.AreEqual(Tuning.Soldier.CrouchHeight, s.CrouchHeight, s.Name);
                Assert.AreEqual(Tuning.Soldier.MaxHp, s.MaxHp, s.Name);
            }
        }

        [Test]
        public void PixelsPerUnitIsOneNumber()
        {
            Assert.AreEqual(Tuning.Pixels.PerUnit, CraterGround.Px);
            Assert.AreEqual(Tuning.Hud.Height * 16f / 9f, Tuning.Hud.Width, 1e-4f);
        }
    }
}
