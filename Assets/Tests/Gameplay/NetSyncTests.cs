using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>The world's side of co-op (<see cref="NetSync"/>), against a stand-in session.</summary>
    public class NetSyncTests : GameplayFixture
    {
        [Test]
        public void TheAuthorityPublishesItsGoons()
        {
            var net = new FakeNet { Authority = true };
            Make("..P.......s", net);
            Run(0.1f);
            Assert.AreEqual(1, net.Written.Count);
            Assert.AreEqual(Content.Rifleman, net.Written[0].Def);
        }

        [Test]
        public void AFollowerMakesTheAuthoritysGoonsAndReportsItsHits()
        {
            var net = new FakeNet { Authority = false };
            Make("..P", net);
            net.Incoming.Add(new NetEnemyState { Id = 7, Def = Content.Rifleman, Hp = Content.Rifleman.Hp, Pos = World.Player.Pos + Vector2.right * 6f });
            Run(0.1f);
            Enemy goon = World.FindEnemy(7);
            Assert.IsNotNull(goon, "the follower makes the goon the authority listed");

            World.Combat.DamageEnemy(goon, 1);
            Assert.AreEqual(1, net.Hits.Count);
            Assert.AreEqual(7, net.Hits[0].EnemyId);
            Assert.AreEqual(Content.Rifleman.Hp, goon.Hp, "only the authority takes the damage off");
        }
    }
}
