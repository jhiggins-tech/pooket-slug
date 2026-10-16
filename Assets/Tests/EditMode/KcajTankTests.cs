using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>kcaj's boss tabk (<see cref="KcajTankSpec"/>): its attack cycle, the floor sweep and the pill rain.</summary>
    public class KcajTankTests
    {
        static readonly Vector2 Muzzle = new Vector2(0f, 1.6f);
        const float SoldierHalf = 0.3f, SoldierHeight = 1.35f;

        [Test]
        public void ItSweepsThenRains()
        {
            Assert.AreEqual(KcajAttack.Shell, KcajTankSpec.AttackFor(0, false));
            Assert.AreEqual(KcajAttack.Sweep, KcajTankSpec.AttackFor(1, false));
            Assert.AreEqual(KcajAttack.Shell, KcajTankSpec.AttackFor(2, false));
            Assert.AreEqual(KcajAttack.Rain, KcajTankSpec.AttackFor(3, false));
            Assert.AreEqual(KcajAttack.Sweep, KcajTankSpec.AttackFor(5, false), "round again");
        }

        [Test]
        public void ItDoesntRainWhileItsPillsAreStillFalling()
        {
            Assert.AreEqual(KcajAttack.Shell, KcajTankSpec.AttackFor(3, raining: true));
        }

        [Test]
        public void TheSweepDragsTheBeamOutAcrossTheFloor()
        {
            var spec = new KcajTankSpec();
            Assert.AreEqual(spec.SweepNear, spec.BeamFloorX(0f, 1, 0f), 1e-5f);
            Assert.AreEqual(spec.SweepFar, spec.BeamFloorX(0f, 1, 1f), 1e-5f);
            Assert.AreEqual(-spec.SweepFar, spec.BeamFloorX(0f, -1, 1f), 1e-5f, "it sweeps the way it faces");
            Assert.AreEqual(1f, spec.SweepProgress(spec.SweepTime, 1f), 1e-5f);
            Assert.AreEqual(1f, spec.SweepProgress(spec.SweepTime / 1.5f, 1.5f), 1e-5f, "an angry tabk sweeps faster");
            Assert.Greater(spec.SweepFar, 11f, "it reaches across most of a screen");
        }

        [Test]
        public void AStandingSoldierIsCaughtAsTheBeamPasses()
        {
            var spec = new KcajTankSpec();
            var feet = new Vector2(8f, 0f);
            bool caught = false;
            for (float p = 0f; p <= 1f; p += 0.01f)
                caught |= spec.Hits(Muzzle, new Vector2(spec.BeamFloorX(0f, 1, p), 0f), feet, SoldierHalf, SoldierHeight);
            Assert.IsTrue(caught);
        }

        [Test]
        public void JumpingAsItPassesClearsIt()
        {
            // A jump keeps the soldier's feet above about a tile for most of a second: long enough to let it pass beneath.
            var spec = new KcajTankSpec();
            float x = 8f;
            for (float p = 0f; p <= 1f; p += 0.01f)
            {
                float floor = spec.BeamFloorX(0f, 1, p);
                if (Mathf.Abs(floor - x) > 1.5f) continue; // the jump's timed for when it's close
                Assert.IsFalse(spec.Hits(Muzzle, new Vector2(floor, 0f), new Vector2(x, 1.2f), SoldierHalf, SoldierHeight), $"floor at {floor}");
            }
        }

        [Test]
        public void TheBeamDoesntReachBehindItsEnd()
        {
            var spec = new KcajTankSpec();
            Assert.IsFalse(spec.Hits(Muzzle, new Vector2(5f, 0f), new Vector2(9f, 0f), SoldierHalf, SoldierHeight));
            Assert.IsFalse(spec.Hits(Muzzle, new Vector2(5f, 0f), new Vector2(-4f, 0f), SoldierHalf, SoldierHeight), "or behind the tabk");
        }

        [Test]
        public void TheTabksPillsHurtSoldiersAndAreFewEnoughToDodge()
        {
            UnmedicatedSpec rain = new KcajTankSpec().Rain;
            Assert.IsTrue(rain.HurtsSoldiers);
            Assert.AreEqual(1f, rain.PopDamage, "a heart a pill");
            Assert.LessOrEqual(rain.Count, 20);
            Assert.Less(rain.Landings, KcajTuning.Pills.Landings, "they pop sooner than kcaj's own");
            Assert.Greater(rain.Radius, KcajTuning.Pills.Radius, "and are bigger, so you can see them coming");
        }

        [Test]
        public void TheTabksRainEndsToo()
        {
            var spec = new KcajTankSpec();
            var rain = new PillRain(spec.Rain);
            rain.Start(new Rng(3), 0f, 20f, Tuning.Camera.ViewHeight);
            var ground = new FlatGround();
            for (float t = 0f; t < 8f && !rain.Done; t += 1f / 60f) rain.Step(1f / 60f, ground, null);
            Assert.IsTrue(rain.Done);
        }

        sealed class FlatGround : IPillGround
        {
            public bool Solid(float x, float y) => y < 0f;
        }
    }
}
