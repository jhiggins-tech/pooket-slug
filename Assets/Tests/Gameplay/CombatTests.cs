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
    }
}
