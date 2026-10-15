using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class SpikeTests
    {
        const string Page = "https://jhiggins-tech.github.io/pooket-slug/index.html";

        [TearDown]
        public void Off() => Spike.Configure(Page);

        [Test]
        public void NoSpikeIsOnByDefault()
        {
            Spike.Configure(Page);
            Assert.IsFalse(Spike.Any);
            Assert.IsNull(Spike.Label);
        }

        [TestCase("?spike=tones2", true, false)]
        [TestCase("?spike=holo", false, true)]
        [TestCase("?spike=tones2,holo", true, true)]
        [TestCase("?SPIKE=Holo,TONES2", true, true)]
        [TestCase("?spike=tones", false, false)]
        public void TheAddressTurnsSpikesOn(string query, bool tones2, bool holo)
        {
            Spike.Configure(Page + query);
            Assert.AreEqual(tones2, Spike.Tones2);
            Assert.AreEqual(holo, Spike.Holo);
            Assert.IsFalse(Spike.Hits);
            Assert.AreEqual(tones2 || holo, Spike.Label != null);
        }

        [Test]
        public void TheBannerNamesEverySpikeThatsOn()
        {
            Spike.Configure(Page + "?spike=tones2,holo,hits");
            Assert.IsTrue(Spike.Hits);
            Assert.AreEqual("SPIKE TEST · TONES2 + HOLOGRAM GOONS + HIT FEEL", Spike.Label);
            Spike.Configure(Page + "?spike=hits");
            Assert.AreEqual("SPIKE TEST · HIT FEEL", Spike.Label);
        }

        [Test]
        public void TheSpewMeltsAGoonInAFewGobbetsAndLastsASecondAndAHalf()
        {
            SpewSpec spew = SpikeContent.Spew;
            Assert.AreEqual(1.5f, spew.Duration, 1e-6f);
            float gobbets = Content.Rifleman.Hp / spew.Damage;
            Assert.LessOrEqual(gobbets, 8f, "it melts anything close");
            Assert.Less(gobbets / spew.DropsPerSecond, 0.25f, "a rifleman in the spew is gone in a blink");
            Assert.Greater(spew.SludgeLife, 0f);
        }

        [Test]
        public void TheSpewOnlyReachesClose()
        {
            SpewSpec spew = SpikeContent.Spew;
            var jet = new JetSpec();
            // Thrown level, how far a gobbet gets before it drops a tile and a half: much less than the full jet.
            float t = UnityEngine.Mathf.Sqrt(2f * 1.5f / spew.Gravity);
            float reach = spew.Speed * 1.1f * t;
            float jetT = UnityEngine.Mathf.Sqrt(2f * 1.5f / jet.Gravity(1f));
            Assert.Less(reach, 4f);
            Assert.Less(reach, jet.Speed(1f) * jetT / 2f);
        }

        [Test]
        public void TheJetBuildsToFullPressureThenHolds()
        {
            var jet = new JetSpec();
            Assert.AreEqual(0f, jet.Pressure(0f), 1e-6f);
            Assert.AreEqual(1f, jet.Pressure(jet.RampTime), 1e-6f);
            Assert.AreEqual(1f, jet.Pressure(jet.RampTime * 3f), 1e-6f);
            float last = 0f;
            for (int i = 1; i <= 20; i++)
            {
                float p = jet.Pressure(jet.RampTime * i / 20f);
                Assert.GreaterOrEqual(p, last, "the pressure only builds while FIRE is held");
                last = p;
            }
        }

        [Test]
        public void FullPressureIsAFasterNarrowerLongerJet()
        {
            var jet = new JetSpec();
            Assert.Greater(jet.Speed(1f), jet.Speed(0f));
            Assert.Less(jet.Spread(1f), jet.Spread(0f), "a spray at first, a clean jet at full");
            Assert.Less(jet.Gravity(1f), jet.Gravity(0f), "a weak spray drops short");
            Assert.Greater(jet.Damage(1f), jet.Damage(0f), "the full jet does the real damage");
            Assert.Greater(jet.Push(1f), jet.Push(0f));
        }

        [Test]
        public void EachDropletOnlyTricklesDamage()
        {
            var jet = new JetSpec();
            Assert.Less(jet.Damage(1f), 1f, "it takes several droplets to make a point of damage");
            Assert.Greater(jet.Damage(0f), 0f);
        }

        [Test]
        public void SplashbackSlowsButNeverStops()
        {
            var jet = new JetSpec();
            Assert.Greater(jet.SplashSlow, 0f);
            Assert.Less(jet.SplashSlow, 1f);
        }

        [Test]
        public void TheRocketJumpGoesHigherThanAJump()
        {
            Assert.Greater(SpikeContent.Rocket.LaunchVelocity, SpikeContent.Tones2.JumpVelocity);
            Assert.AreEqual(SpikeContent.Rocket.Charges, SpikeContent.Tones2.StartSpecials, "the special starts with all its charges");
        }

        [Test]
        public void Tones2sKitIsTheJetAndTheRocketJump()
        {
            Assert.AreEqual(WeaponBehaviour.Jet, SpikeContent.Tones2.Primary.Behaviour);
            Assert.AreEqual(WeaponBehaviour.RocketJump, SpikeContent.Tones2.Special.Behaviour);
            Assert.AreEqual(WeaponBehaviour.Spew, SpikeContent.Tones2.Super.Behaviour);
            Assert.AreEqual(0, SpikeContent.Tones2.Primary.Ammo, "tier 1 has unlimited ammo");
        }

        [Test]
        public void SpikeContentNeverGoesOnTheWire()
        {
            // Not in Content's tables, so a co-op partner can never be sent one.
            CollectionAssert.DoesNotContain(Content.Weapons, SpikeContent.WaterJet);
            CollectionAssert.DoesNotContain(Content.Weapons, SpikeContent.RocketJump);
            CollectionAssert.DoesNotContain(Content.Weapons, SpikeContent.SpewWeapon);
            CollectionAssert.DoesNotContain(Content.Soldiers, SpikeContent.Tones2);
            Assert.IsNull(Content.Weapon(SpikeContent.WaterJet.Id));
            Assert.IsNull(Content.Soldier(SpikeContent.Tones2.Id));
        }
    }
}
