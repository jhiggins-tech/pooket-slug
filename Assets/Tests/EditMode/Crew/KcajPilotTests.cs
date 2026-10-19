using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>Trollogram kcaj's rules (<see cref="KcajPilotSpec"/>): his beam on a soldier, what breaks it, his super.</summary>
    public class KcajPilotTests
    {
        static KcajPilotSpec Spec => KcajTuning.Pilot;

        [Test]
        public void HeHasKiesPilotsHealth_ABossDuel()
        {
            Assert.AreEqual(KieTuning.Pilot.Hp, Spec.Hp);
            Assert.AreEqual(Spec.Hp, Content.KcajPilot.Hp);
            Assert.AreEqual(EnemyBrain.KcajPilot, Content.KcajPilot.Brain);
        }

        [Test]
        public void HisBeamHeldToTheEnd_BurnsAboutTwoHearts_InWholeHearts()
        {
            var fix = new Hyperfixation(Spec.Beam);
            fix.Lock();
            float carried = 0f;
            int hearts = 0;
            for (int i = 0; i < 600 && fix.Locked; i++) hearts += KcajPilotSpec.Hearts(ref carried, fix.Step(1f / 60f, true, true, 5f));
            Assert.AreEqual(FixateEnd.Spent, fix.Ended);
            Assert.AreEqual(Spec.Beam.MaxHold, fix.Held, 1e-3f);
            Assert.AreEqual(2, hearts, "a soldier's 5 hearts: the beam is a threat, not a kill");
            Assert.Less(carried, 1f);
        }

        [Test]
        public void ItBurnsNothingForTheFirstHalfSecond_SoYouCanGetOut()
        {
            var fix = new Hyperfixation(Spec.Beam);
            fix.Lock();
            float carried = 0f;
            int hearts = 0;
            for (int i = 0; i < 29; i++) hearts += KcajPilotSpec.Hearts(ref carried, fix.Step(1f / 60f, true, true, 5f));
            Assert.AreEqual(0, hearts);
        }

        [Test]
        public void GettingFarEnoughAway_BreaksIt()
        {
            var fix = new Hyperfixation(Spec.Beam);
            fix.Lock();
            fix.Step(0.1f, true, true, Spec.Beam.BreakRange + 0.5f);
            Assert.IsFalse(fix.Locked);
            Assert.AreEqual(FixateEnd.OutOfReach, fix.Ended);
        }

        [Test]
        public void HurtingHimEnough_BreaksIt()
        {
            Assert.IsFalse(Spec.Breaks(0));
            Assert.IsFalse(Spec.Breaks(Spec.BreakOnHurt - 1));
            Assert.IsTrue(Spec.Breaks(Spec.BreakOnHurt));
            Assert.Less(Spec.BreakOnHurt, Spec.Hp / 4, "a few seconds of shooting him, not a quarter of his bar");
        }

        [Test]
        public void HeartsCarryOverBetweenBurns()
        {
            float carried = 0f;
            Assert.AreEqual(0, KcajPilotSpec.Hearts(ref carried, 0.6f));
            Assert.AreEqual(1, KcajPilotSpec.Hearts(ref carried, 0.6f));
            Assert.AreEqual(0.2f, carried, 1e-4f);
            Assert.AreEqual(0, KcajPilotSpec.Hearts(ref carried, -1f), "never a negative burn");
        }

        [Test]
        public void HisPillsHurtTheSoldiers_HisSuperChargesFromHisHits()
        {
            Assert.IsTrue(Spec.Rain.HurtsSoldiers);
            Assert.Less(Spec.Rain.Count, KcajTuning.Pills.Count, "fewer, bigger pills than kcaj's own");
            int meter = 0;
            meter = Spec.ChargeSuper(meter, 2);
            Assert.IsFalse(Spec.SuperReady(meter));
            meter = Spec.ChargeSuper(meter, 5);
            Assert.IsTrue(Spec.SuperReady(meter));
            Assert.AreEqual(Spec.SuperHits, meter);
        }

        [Test]
        public void HisConesAreKcajsThrow_AHeartASplat()
        {
            Assert.AreEqual(WeaponBehaviour.Lob, Content.TrolloCones.Behaviour);
            Assert.AreSame(Content.TrolloCone, Content.TrolloCones.Projectile);
            Assert.AreEqual(1, Content.TrolloCone.Damage);
            Assert.AreEqual(KcajTuning.Cones.Gravity, Content.TrolloCone.Gravity);
            Assert.Greater(Spec.ConeCooldown, KcajTuning.Cones.Cooldown, "slower than kcaj's own");
        }
    }
}
