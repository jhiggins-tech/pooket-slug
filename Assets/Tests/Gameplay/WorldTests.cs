using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>A play-through on its own: our soldier lands and runs, and the level brings its goons in.</summary>
    public class WorldTests : GameplayFixture
    {
        [Test]
        public void OurSoldierLandsOnTheGround()
        {
            Make("..P");
            Run(2f);
            Assert.That(World.Player.Grounded);
            Assert.AreEqual(World.Level.TopSurfaceY(World.Player.Pos.x), World.Player.Pos.y, 0.01f);
            Assert.AreEqual(World.Player.MaxHp, World.Player.Hp);
        }

        [Test]
        public void HoldingRightRunsRight()
        {
            Make("..P");
            Run(0.5f);
            float x = World.Player.Pos.x;
            Run(1f, new SoldierInput { Right = true });
            Assert.Greater(World.Player.Pos.x, x + 2f);
        }

        [Test]
        public void TheLevelBringsInTheGoonsAhead()
        {
            Make("..P.........s");
            Assert.IsNotNull(First(EnemyBrain.Rifleman), "a rifleman on screen is spawned with the world");
        }

        [Test]
        public void ARiflemanInRangeShootsOurSoldier()
        {
            Make("..P......s");
            Run(8f);
            Assert.Less(World.Player.Hp, World.Player.MaxHp);
        }
    }
}
