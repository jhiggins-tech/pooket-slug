using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class Tones2TabkTests
    {
        static readonly Tones2TabkSpec Spec = new Tones2TabkSpec();

        [Test]
        public void ASprayCycleBuildsUpHoldsThenEasesOff()
        {
            Assert.Greater(Spec.CyclePressure(0f), 0f, "a little water at once");
            Assert.Less(Spec.CyclePressure(0.1f), 0.5f);
            float last = 0f;
            for (float t = 0f; t < Spec.RampUp; t += 0.02f)
            {
                float p = Spec.CyclePressure(t);
                Assert.GreaterOrEqual(p, last - 1e-5f, "it never drops while building");
                last = p;
            }
            Assert.AreEqual(1f, Spec.CyclePressure(Spec.RampUp + Spec.Hold * 0.5f));
            float easing = Spec.CyclePressure(Spec.RampUp + Spec.Hold + Spec.RampDown * 0.5f);
            Assert.That(easing, Is.InRange(0.2f, 0.8f));
            Assert.Less(Spec.CyclePressure(Spec.CycleLength - 0.01f), 0.01f);
        }

        [Test]
        public void ACycleIsOverAfterItsLength()
        {
            Assert.AreEqual(0f, Spec.CyclePressure(-0.1f));
            Assert.AreEqual(0f, Spec.CyclePressure(Spec.CycleLength));
            Assert.AreEqual(0f, Spec.CyclePressure(Spec.CycleLength + 5f));
        }

        [Test]
        public void ItBuildsInSpurts()
        {
            // each spurt surges, then levels off before the next one
            float step = Spec.RampUp / Spec.Spurts;
            float surge = Spec.CyclePressure(step * 1.1f) - Spec.CyclePressure(step * 0.99f);
            float level = Spec.CyclePressure(step * 1.99f) - Spec.CyclePressure(step * 1.88f);
            Assert.Greater(surge, level * 2f);
        }

        [Test]
        public void TheMudJumpGoesAlongTheUpperAimsStraightUpForDownAndLongAndLowForFlat()
        {
            float d = Mathf.Sqrt(0.5f);
            Assert.AreEqual(Vector2.up, Tones2TabkSpec.JumpDirection(Vector2.up, 1));
            Assert.AreEqual(new Vector2(d, d).x, Tones2TabkSpec.JumpDirection(new Vector2(d, d), 1).x, 1e-4f);
            Assert.AreEqual(Vector2.up, Tones2TabkSpec.JumpDirection(Vector2.down, 1));
            Assert.AreEqual(Vector2.up, Tones2TabkSpec.JumpDirection(new Vector2(-d, -d), 1));
            Vector2 flat = Tones2TabkSpec.JumpDirection(Vector2.left, 1);
            Assert.Less(flat.x, 0f, "it goes the way it aims, not the way it faces");
            Assert.Greater(flat.y, 0f);
            Assert.Greater(Mathf.Abs(flat.x), flat.y, "long and low");
            Assert.AreEqual(1f, flat.magnitude, 1e-4f);
        }

        [Test]
        public void ItsSpewLastsTwiceAsLongAsOnFoot()
        {
            Assert.AreEqual(2f * Tones2Tuning.Spew.Duration, Spec.Spew.Duration, 1e-4f);
            Assert.AreEqual(3f, Spec.Spew.Duration, 1e-4f);
        }

        [Test]
        public void ItHasEveryTabksRounds()
        {
            var rounds = new TankRounds(new PlayerTankSpec());
            Assert.AreEqual(5, rounds.Main);
            Assert.AreEqual(3, rounds.Specials);
            Assert.AreEqual(1, rounds.Supers);
        }
    }
}
