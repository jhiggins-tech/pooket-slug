using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class KieTabkTests
    {
        static readonly KieTabkSpec Spec = new KieTabkSpec();

        [Test]
        public void ItsWeaselsAreHisTabkSized()
        {
            WeaselPopSpec own = new WeaselPopSpec(), tabk = Spec.Weasels;
            Assert.Greater(tabk.Count, own.Count, "a bigger volley");
            Assert.Greater(tabk.Damage, own.Damage);
            Assert.Greater(tabk.BlastRadius, own.BlastRadius);
            Assert.GreaterOrEqual(tabk.MaxOut, tabk.Count * 3, "three volleys can be out at once");
            Assert.Greater(Spec.Charge, 0f, "the reels spin up first");
        }

        [Test]
        public void AVolleyLobsAlongTheAimAndFansOut()
        {
            Vector2[] v = Spec.Weasels.Volley(Vector2.right, Vector2.zero);
            Assert.AreEqual(Spec.Weasels.Count, v.Length);
            foreach (Vector2 w in v)
            {
                Assert.Greater(w.x, 0f);
                Assert.Greater(w.y, 0f, "lobbed");
            }
            Assert.AreNotEqual(v[0].normalized, v[v.Length - 1].normalized);
        }

        [Test]
        public void ItsTrollogramBlowsUpBiggerThanHis()
        {
            TrollogramSpec own = new TrollogramSpec(), tabk = Spec.Trollogram;
            Assert.Greater(tabk.Damage, own.Damage);
            Assert.Greater(tabk.BlastRadius, own.BlastRadius);
            Assert.Greater(tabk.Life, 0f);
        }

        [Test]
        public void TheTrollogramStandsAheadOfTheTabkOnScreen()
        {
            Assert.AreEqual(10f + Spec.BeamAhead, Spec.DecoyX(10f, 1, 0f, 30f, 1.35f), 1e-4f);
            Assert.AreEqual(10f - Spec.BeamAhead, Spec.DecoyX(10f, -1, 0f, 30f, 1.35f), 1e-4f);
            Assert.AreEqual(30f - 1.35f, Spec.DecoyX(29f, 1, 0f, 30f, 1.35f), 1e-4f, "never off the right edge");
            Assert.AreEqual(1.35f, Spec.DecoyX(1f, -1, 0f, 30f, 1.35f), 1e-4f, "or the left");
        }

        [Test]
        public void StolenLootFiresFromTheLensWhileItLasts()
        {
            Assert.IsTrue(KieTabkSpec.FiresLoot(3));
            Assert.IsFalse(KieTabkSpec.FiresLoot(0));
        }

        [Test]
        public void TheReelsSpinWhileAShotCharges()
        {
            Assert.IsFalse(KieTabkSpec.ReelsSpin(false, false, false));
            Assert.IsTrue(KieTabkSpec.ReelsSpin(true, false, false));
            Assert.IsTrue(KieTabkSpec.ReelsSpin(false, true, false));
            Assert.IsTrue(KieTabkSpec.ReelsSpin(false, false, true));
        }

        [Test]
        public void ItsTrollogramTellsThePartnerItsATabk()
        {
            Vector2 v = TrollNet.Pack(TrollEvent.Drop, 255, -1, false, tabk: true);
            Assert.AreEqual(TrollEvent.Drop, TrollNet.Unpack(v, out int id, out int facing, out bool crouching, out bool tabk));
            Assert.AreEqual(255, id);
            Assert.AreEqual(-1, facing);
            Assert.IsFalse(crouching);
            Assert.IsTrue(tabk);
            TrollNet.Unpack(TrollNet.Pack(TrollEvent.Drop, 4, 1, true), out _, out _, out _, out tabk);
            Assert.IsFalse(tabk, "his own Trollogram is of him");
        }
    }
}
