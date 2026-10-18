using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>Mission two's goons and miniboss (Core/Enemies): the shield, the grenadier's lob, the Big Scoop's moves.</summary>
    public class GoonTests
    {
        static readonly Vector2 Feet = new Vector2(10f, 2f);

        [Test]
        public void AShieldStopsShotsFromTheFront()
        {
            // facing left (at a soldier on his left), a bullet flying in from the left at chest height
            Assert.IsTrue(ShieldSpec.Blocks(-1, Feet, new Vector2(9.7f, 3f), new Vector2(16f, 0f)));
            Assert.IsTrue(ShieldSpec.Blocks(1, Feet, new Vector2(10.3f, 3f), new Vector2(-16f, 0f)), "facing right, from the right");
        }

        [Test]
        public void AShotFromBehindGetsHim()
        {
            Assert.IsFalse(ShieldSpec.Blocks(-1, Feet, new Vector2(10.3f, 3f), new Vector2(-16f, 0f)), "facing left, from the right");
            Assert.IsFalse(ShieldSpec.Blocks(1, Feet, new Vector2(9.7f, 3f), new Vector2(16f, 0f)), "facing right, from the left");
        }

        [Test]
        public void ShotsOverTheTopOrDroppedOnHimGetThrough()
        {
            Assert.IsFalse(ShieldSpec.Blocks(-1, Feet, new Vector2(9.8f, Feet.y + ShieldSpec.ShieldTop + 0.1f), new Vector2(16f, 0f)), "over the shield");
            Assert.IsFalse(ShieldSpec.Blocks(-1, Feet, new Vector2(9.8f, 3f), new Vector2(2f, -14f)), "a bomb falling on him");
            Assert.IsTrue(ShieldSpec.Blocks(-1, Feet, new Vector2(9.8f, 3f), new Vector2(12f, -6f)), "a shot aimed down at him from the front");
        }

        [Test]
        public void HesSlowToTurnSoYouCanGetBehindHim()
        {
            float overHim = 2f * 0.35f / Tuning.Soldier.RunSpeed; // a soldier running past him
            Assert.Greater(ShieldSpec.TurnDelay, overHim * 3f);
            Assert.Less(ShieldSpec.WalkSpeed, Tuning.Soldier.RunSpeed * 0.5f);
        }

        [Test]
        public void AGrenadeLandsWhereItsThrown()
        {
            Vector2 from = new Vector2(30f, 3.5f), to = new Vector2(22f, 2f);
            float g = Content.GoonGrenade.Gravity, t = GrenadierSpec.Flight;
            Vector2 v = GrenadierSpec.LobTo(from, to, g, t);
            Vector2 p = from;
            const float dt = 1f / 240f;
            for (float s = 0f; s < t - 1e-4f; s += dt)
            {
                v.y -= g * dt;
                p += v * dt;
            }
            Assert.AreEqual(to.x, p.x, 0.15f);
            Assert.AreEqual(to.y, p.y, 0.15f);
        }

        [Test]
        public void AGrenadierOnlyThrowsInRange()
        {
            Assert.IsFalse(GrenadierSpec.InRange(1f), "too close: he'd blow himself up");
            Assert.IsTrue(GrenadierSpec.InRange(-6f));
            Assert.IsFalse(GrenadierSpec.InRange(20f));
            Assert.Greater(GrenadierSpec.Flight, 0.8f, "long enough in the air to step away");
        }

        [Test]
        public void TheBigScoopWindsUpBeforeEveryMove()
        {
            var spec = new BigScoopSpec();
            var brain = new BigScoopBrain(spec);
            var rng = new Rng(7);
            const float dt = 1f / 60f;
            int moves = 0;
            float tellSeen = 0f;
            for (int i = 0; i < 60 * 30; i++)
            {
                bool went = brain.Tick(dt, 3f, 1f, rng);
                if (brain.Phase == ScoopPhase.Tell) tellSeen = brain.T;
                if (!went) continue;
                moves++;
                Assert.AreEqual(ScoopPhase.Act, brain.Phase);
                Assert.GreaterOrEqual(tellSeen + dt * 1.5f, spec.TellFor(brain.Move), "the whole tell played first");
                tellSeen = 0f;
            }
            Assert.Greater(moves, 5, "he keeps at it");
        }

        [Test]
        public void TheBigScoopSlamsUpCloseAndChargesFromAfar_NeverTheSameThreeTimes()
        {
            var spec = new BigScoopSpec();
            var rng = new Rng(99);
            int slams = 0, charges = 0;
            ScoopMove last = ScoopMove.None, before = ScoopMove.None;
            for (int i = 0; i < 400; i++)
            {
                ScoopMove near = spec.Pick(2f, ScoopMove.None, ScoopMove.None, rng);
                Assert.AreNotEqual(ScoopMove.Charge, near, "no charging at someone right by him");
                if (near == ScoopMove.Slam) slams++;
                ScoopMove far = spec.Pick(9f, ScoopMove.None, ScoopMove.None, rng);
                Assert.AreNotEqual(ScoopMove.Slam, far, "no slamming at someone across the yard");
                if (far == ScoopMove.Charge) charges++;
                ScoopMove m = spec.Pick(i % 2 == 0 ? 2f : 9f, last, before, rng);
                Assert.IsFalse(m == last && m == before, "never three of a kind");
                before = last;
                last = m;
            }
            Assert.Greater(slams, 200);
            Assert.Greater(charges, 100);
        }

        [Test]
        public void AChargeStopsAtAWall()
        {
            var spec = new BigScoopSpec();
            var brain = new BigScoopBrain(spec);
            var rng = new Rng(3);
            const float dt = 1f / 60f;
            for (int i = 0; i < 60 * 60 && !(brain.Phase == ScoopPhase.Act && brain.Move == ScoopMove.Charge); i++) brain.Tick(dt, 12f, 1f, rng);
            Assert.AreEqual(ScoopMove.Charge, brain.Move);
            brain.StopCharge();
            brain.Tick(dt, 12f, 1f, rng);
            Assert.AreEqual(ScoopPhase.Rest, brain.Phase);
        }

        [Test]
        public void FrozenHeTakesDouble_AndAngryHeRestsLess()
        {
            var spec = new BigScoopSpec();
            Assert.AreEqual(2, spec.FrozenDamage);
            Assert.Less(spec.RestFor(0.3f), spec.RestFor(0.9f));
            Assert.AreEqual(Content.BigScoop.Hp, 40);
            Assert.Greater(Creamery.TankGoonFreeze, 2f, "a tank's freeze is a real window");
        }

        [Test]
        public void TheMinibossShockwaveRunsAlongTheFloor()
        {
            Assert.AreEqual(0f, Content.ScoopWave.Gravity);
            Assert.IsTrue(Content.ScoopWave.ContactDamage);
            Assert.Greater(Content.ScoopSlam.Speed * Content.ScoopWave.Life, 8f, "it crosses most of the yard");
            Assert.Less(Content.ScoopWave.Radius * 2f, Tuning.Soldier.StandHeight, "low enough to jump");
        }

        [Test]
        public void MissionTwoHasItsNewGoonsAndOneBigScoopInTheSignYard()
        {
            int shields = 0, grenadiers = 0, scoops = 0;
            foreach (Spawn s in Missions.Two.Load().Spawns)
            {
                if (s.Kind == SpawnKind.ShieldSoldier) shields++;
                if (s.Kind == SpawnKind.Grenadier) grenadiers++;
                if (s.Kind != SpawnKind.BigScoop) continue;
                scoops++;
                Assert.That(s.Pos.x, Is.InRange(85f, 125f), "he's in the sign yard");
            }
            Assert.Greater(shields, 2);
            Assert.Greater(grenadiers, 2);
            Assert.AreEqual(1, scoops);
        }
    }
}
