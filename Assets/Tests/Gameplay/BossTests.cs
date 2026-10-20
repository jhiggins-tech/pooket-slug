using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>Mission bosses in a real world: the boss tabk (<see cref="TankBoss"/>), its pilots and the Big Scoop.</summary>
    public class BossTests : GameplayFixture
    {
        TankBoss WakeTank(BossTabk boss, EnemyDef pilot)
        {
            Make("..P", boss: boss, pilot: pilot);
            var tank = (TankBoss)Spawn(Content.Tank, 9f);
            Run(0.5f);
            Assert.That(tank.Engaged, "it wakes once it's on screen");
            return tank;
        }

        void Wreck(TankBoss tank)
        {
            World.Combat.DamageEnemy(tank, tank.Hp);
            Run(2.5f);
            Assert.That(tank.Removed, "it blows up and goes");
        }

        [Test]
        public void KiesTabkIsWreckedAndHisPilotJumpsOut()
        {
            Wreck(WakeTank(BossTabk.Kie, Content.Pilot));
            Assert.IsInstanceOf<KiePilot>(World.Boss);
            Assert.That(((BossEnemy)World.Boss).Engaged);
        }

        [Test]
        public void KcajsTabkIsWreckedAndHisPilotJumpsOut()
        {
            Wreck(WakeTank(BossTabk.Kcaj, Content.KcajPilot));
            Assert.IsInstanceOf<KcajPilot>(World.Boss);
        }

        [Test]
        public void ATabkWithNoPilotWinsTheFightWhenItsWrecked()
        {
            Wreck(WakeTank(BossTabk.Kie, null));
            Assert.That(World.Victory, "no pilot round: the wreck wins it");
        }

        [Test]
        public void ThePilotIsDownWhenItsShotDown()
        {
            Make("..P", pilot: Content.Pilot);
            Enemy pilot = Spawn(Content.Pilot, 6f);
            World.Combat.DamageEnemy(pilot, pilot.Hp);
            Run(0.2f);
            Assert.That(pilot.Dying || pilot.Removed);
        }

        [Test]
        public void TheBigScoopTakesMoreWhileFrozen()
        {
            Make("..P");
            Enemy scoop = Spawn(Content.BigScoop, 8f);
            Run(2f); // he wakes on screen and drops into the yard
            int hp = scoop.Hp;
            scoop.Hurt(1);
            int warm = hp - scoop.Hp;
            scoop.Freeze(3f);
            Assert.That(scoop.Frost.Frozen);
            int before = scoop.Hp;
            scoop.Hurt(1);
            Assert.Greater(before - scoop.Hp, warm);
        }

        [Test]
        public void TheBigScoopComesForOurSoldier()
        {
            Make("..P");
            Enemy scoop = Spawn(Content.BigScoop, 8f);
            float x = scoop.Pos.x;
            Run(6f);
            Assert.That(scoop.Pos.x < x - 0.5f || World.Player.Hp < World.Player.MaxHp || World.Player.Dead,
                "it charges, slams or hurls at him");
        }
    }
}
