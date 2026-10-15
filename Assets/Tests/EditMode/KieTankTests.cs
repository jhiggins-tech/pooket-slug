using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class KieTankTests
    {
        [Test]
        public void ItsChargedShotsCycleThroughTheThreeAttacks()
        {
            Assert.AreEqual(KieAttack.Weasels, KieTankSpec.AttackFor(0, false, false));
            Assert.AreEqual(KieAttack.Holograms, KieTankSpec.AttackFor(1, false, false));
            Assert.AreEqual(KieAttack.Roulette, KieTankSpec.AttackFor(2, false, false));
            Assert.AreEqual(KieAttack.Weasels, KieTankSpec.AttackFor(3, false, false));
        }

        [Test]
        public void AnAttackStillGoingIsAPlainShell()
        {
            Assert.AreEqual(KieAttack.Shell, KieTankSpec.AttackFor(1, true, false), "copies still up");
            Assert.AreEqual(KieAttack.Shell, KieTankSpec.AttackFor(2, false, true), "still holding what it stole");
            Assert.AreEqual(KieAttack.Weasels, KieTankSpec.AttackFor(0, true, true), "weasels come whatever else is going");
        }

        [TestCase(3, 0.5f, 0.2f, KieSteal.Special)]
        [TestCase(3, 0.5f, 0.8f, KieSteal.Super)]
        [TestCase(3, 0f, 0.8f, KieSteal.Special)]
        [TestCase(0, 0.25f, 0.2f, KieSteal.Super)]
        [TestCase(0, 0f, 0.2f, KieSteal.Nothing)]
        public void TheRouletteTakesWhatYouHave(int specials, float super, float roll, KieSteal expected)
        {
            Assert.AreEqual(expected, KieTankSpec.Pick(specials, super, roll));
        }

        [Test]
        public void ItDropsWhatItStoleAfterAProperHit()
        {
            var spec = new KieTankSpec { DropFraction = 0.1f };
            Assert.AreEqual(12, spec.DropDamage(120));
            Assert.AreEqual(1, spec.DropDamage(3));
        }

        [Test]
        public void TheCopiesStandEitherSideOfHome()
        {
            var spec = new KieTankSpec { DecoyGap = 5f };
            CollectionAssert.AreEqual(new[] { 5f, 10f, 15f }, spec.Slots(10f));
        }

        [Test]
        public void TheWeaselNeverGoesOnTheWire()
        {
            Assert.IsNull(Content.Enemy(KieTankSpec.Weasel.Id), "spike content stays out of the tables co-op reads");
        }

        [Test]
        public void AStolenSuperComesBack()
        {
            var kit = new KitMeters(1, 3, 0f, 4, 0f);
            kit.AddKill();
            kit.AddKill();
            Assert.AreEqual(2, kit.TakeSuper());
            Assert.AreEqual(0f, kit.SuperCharge);
            kit.AddKill();
            kit.ReturnSuper(5);
            Assert.IsTrue(kit.SuperReady, "it comes back on top of kills since, up to full");
            Assert.AreEqual(1f, kit.SuperCharge);
        }
    }
}
