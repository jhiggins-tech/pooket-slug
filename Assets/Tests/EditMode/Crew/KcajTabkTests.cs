using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class KcajTabkTests
    {
        static readonly KcajTabkSpec Spec = new KcajTabkSpec();

        [Test]
        public void AVolleyIsThreeTwinConesAfterTheSwirlWobbles()
        {
            Assert.AreEqual(2, Spec.Cones.Count, "twin cones, as on foot");
            Assert.AreEqual(0, Spec.PairsDue(-1f, Spec.Charge - 0.01f), "nothing until it's charged");
            Assert.AreEqual(1, Spec.PairsDue(0f, Spec.Charge), "the first pair as the charge ends");
            Assert.AreEqual(Spec.Pairs, Spec.PairsDue(-1f, Spec.VolleyTime));
            int seen = 0;
            for (float t = 0f; t < Spec.VolleyTime + 0.5f; t += 1f / 60f) seen += Spec.PairsDue(t - 1f / 60f, t);
            Assert.AreEqual(Spec.Pairs, seen, "each pair goes once, frame by frame");
        }

        [Test]
        public void ItsConesLobFurtherThanHis()
        {
            var own = new DoubleParkSpec();
            Assert.Greater(Spec.Cones.Reach(40f), own.Reach(40f));
            Vector2 low = Spec.Cones.Velocity(0, Vector2.right, 1, Vector2.zero), high = Spec.Cones.Velocity(1, Vector2.right, 1, Vector2.zero);
            Assert.Greater(low.x, 0f);
            Assert.Greater(high.y, low.y, "either side of the aim");
        }

        [Test]
        public void ItsBeamReachesFurtherAndBurnsHotter()
        {
            var own = new HyperfixateSpec();
            Assert.Greater(Spec.Beam.Range, own.Range);
            Assert.Greater(Spec.Beam.BreakRange, Spec.Beam.Range, "it holds a little past where it can lock");
            Assert.Greater(Spec.Beam.TotalDamage(Spec.Beam.MaxHold), own.TotalDamage(own.MaxHold));
        }

        [Test]
        public void ItsPillsPopHarderAndNeverTouchTheCrew()
        {
            var own = new UnmedicatedSpec();
            Assert.Greater(Spec.Pills.PopDamage, own.PopDamage);
            Assert.AreEqual(own.Count, Spec.Pills.Count, "the same rain, so a partner's screen copies it from the seed");
            Assert.IsFalse(Spec.Pills.HurtsSoldiers);
        }

        [Test]
        public void TheSwirlWobblesWhileAShotCharges()
        {
            Assert.IsTrue(KcajTabkSpec.SwirlWobbles(true, false));
            Assert.IsTrue(KcajTabkSpec.SwirlWobbles(false, true));
            Assert.IsFalse(KcajTabkSpec.SwirlWobbles(false, false));
        }
    }
}
