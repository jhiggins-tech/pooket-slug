// The frame contract for what the world draws by id: every projectile and pickup in Content has a sprite of its own
// (not the shell or crate it would borrow), and every crewmate's skin, and the shared trooper's, has a sprite for every
// CrewBody and CrewLegs value. A PlayMode test because the sprites are made by Art.Init, which needs Unity.
using System;
using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>Nothing the world draws by id or by frame comes out blank (<see cref="Art"/>, <see cref="CrewArt"/>).</summary>
    public class ArtTests
    {
        [SetUp]
        public void MakeTheArt() => Art.Init();

        [Test]
        public void EveryProjectileHasASpriteOfItsOwn()
        {
            foreach (ProjectileDef def in Content.Projectiles)
            {
                Assert.That(Art.PaintedProjectile(def), $"projectile {def.Id} ({def.Name}) has no sprite in Art's table");
                Assert.That(Art.ProjectileSprite(def), Is.Not.Null, $"projectile {def.Id} ({def.Name}) draws nothing");
            }
        }

        [Test]
        public void EveryPickupHasASpriteOfItsOwn()
        {
            foreach (PickupDef def in Content.Pickups)
            {
                Assert.That(Art.PaintedPickup(def), $"pickup {def.Id} ({def.Name}) has no sprite in Art's table");
                Assert.That(Art.PickupSprite(def), Is.Not.Null, $"pickup {def.Id} ({def.Name}) draws nothing");
            }
        }

        [Test]
        public void TheTabksShotsDrawTheShell()
        {
            Assert.That(Art.ProjectileSprite(Content.PlayerTankShellShot), Is.SameAs(Art.Shell));
            Assert.That(Art.ProjectileSprite(Content.PlayerTankMortarShot), Is.SameAs(Art.Shell));
        }

        [Test]
        public void EveryBodyAndLegsFrameHasArtForEveryone()
        {
            foreach (CrewBody body in Enum.GetValues(typeof(CrewBody)))
                Assert.That(CrewArt.SharedBody(body), Is.Not.Null.And.Not.Empty, $"the shared body has no rows for {body}");
            foreach (CrewLegs legs in Enum.GetValues(typeof(CrewLegs)))
                Assert.That(CrewArt.SharedLegs(legs), Is.Not.Null.And.Not.Empty, $"the shared legs have no rows for {legs}");

            CheckSkin("the trooper", Art.SkinFor(null));
            foreach (CrewMember m in Crew.All) CheckSkin(m.Soldier.Name, Art.SkinFor(m.Soldier));
        }

        static void CheckSkin(string who, PlayerSkin skin)
        {
            Assert.That(skin, Is.Not.Null, $"{who} has no skin");
            foreach (CrewBody body in Enum.GetValues(typeof(CrewBody)))
                Assert.That(skin.Body(body), Is.Not.Null, $"{who} has no sprite for the {body} body");
            foreach (CrewLegs legs in Enum.GetValues(typeof(CrewLegs)))
                Assert.That(skin.Legs(legs), Is.Not.Null, $"{who} has no sprite for the {legs} legs");
        }
    }
}
