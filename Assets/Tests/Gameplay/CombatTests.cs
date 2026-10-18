using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>Shots, hits and who gets the kill (<see cref="Combat"/>).</summary>
    public class CombatTests : GameplayFixture
    {
        [Test]
        public void OurShotHurtsAGoon()
        {
            Make("..P.......s");
            Enemy goon = First(EnemyBrain.Rifleman);
            int hp = goon.Hp;
            Vector2 from = new Vector2(goon.Pos.x - 3f, goon.Center.y);
            World.Combat.FirePlayerShot(Content.Pistol, from, Vector2.right * Content.Pistol.Speed);
            Run(0.5f);
            Assert.Less(goon.Hp, hp);
        }

        [Test]
        public void AKillCountsAndScores()
        {
            Make("..P.......s");
            Enemy goon = First(EnemyBrain.Rifleman);
            World.Combat.DamageEnemy(goon, goon.Hp);
            Run(0.1f);
            Assert.AreEqual(1, World.Kills);
            Assert.Greater(World.Scoring.Points, 0);
        }

        [Test]
        public void KcajsFireLobsTwoConesInAnArc()
        {
            Make("..P..........s", soldier: Content.Kcaj);
            World.Tick(Frame, new SoldierInput { Fire = true, FirePressed = true });
            var cones = new System.Collections.Generic.List<Projectile>();
            foreach (Projectile p in World.Projectiles)
                if (!p.Removed && p.Def == Content.Cone) cones.Add(p);
            Assert.AreEqual(2, cones.Count, "Double Park throws two at once");
            foreach (Projectile p in cones)
            {
                Assert.IsFalse(p.Hostile);
                Assert.Greater(p.Vel.y, 0f, "lobbed, not fired flat");
                Assert.Greater(p.Vel.x, 0f, "ahead");
            }
            Assert.AreNotEqual(cones[0].Vel.y, cones[1].Vel.y, "one either side of the aim");
        }
    }
}
