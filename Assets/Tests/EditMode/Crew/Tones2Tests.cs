using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>tones2's kit (<see cref="Tones2Tuning"/>), and what a partner's screen needs to copy it.</summary>
    public class Tones2Tests
    {
        [Test]
        public void TheSpewMeltsAGoonInAFewGobbetsAndLastsASecondAndAHalf()
        {
            SpewSpec spew = Tones2Tuning.Spew;
            Assert.AreEqual(1.5f, spew.Duration, 1e-6f);
            float gobbets = Content.Rifleman.Hp / spew.Damage;
            Assert.LessOrEqual(gobbets, 8f, "it melts anything close");
            Assert.Less(gobbets / spew.DropsPerSecond, 0.25f, "a rifleman in the spew is gone in a blink");
            Assert.Greater(spew.SludgeLife, 0f);
        }

        [Test]
        public void TheSpewOnlyReachesClose()
        {
            SpewSpec spew = Tones2Tuning.Spew;
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
            Assert.Greater(Tones2Tuning.Rocket.LaunchVelocity, Content.Tones2.JumpVelocity);
            Assert.AreEqual(3, Content.Tones2.SpecialMax);
            Assert.AreEqual(5f, Content.Tones2.SpecialRefill, 1e-6f, "a rocket jump comes back every 5 seconds");
        }

        [Test]
        public void Tones2sKitIsTheJetAndTheRocketJump()
        {
            Assert.AreEqual(WeaponBehaviour.Jet, Content.Tones2.Primary.Behaviour);
            Assert.AreEqual(WeaponBehaviour.RocketJump, Content.Tones2.Special.Behaviour);
            Assert.AreEqual(WeaponBehaviour.Spew, Content.Tones2.Super.Behaviour);
            Assert.AreEqual(0, Content.Tones2.Primary.Ammo, "tier 1 has unlimited ammo");
        }

        [Test]
        public void EveryoneIsTones2AndHisKitGoesOnTheWire()
        {
            // A partner is sent our soldier's id, so his kit sits in Content's tables (his rocket jumps go as kit moves).
            CollectionAssert.Contains(Content.Soldiers, Content.Tones2);
            CollectionAssert.Contains(Content.Weapons, Content.WaterJet);
            CollectionAssert.Contains(Content.Weapons, Content.RocketJump);
            CollectionAssert.Contains(Content.Weapons, Content.Spew);
            Assert.AreSame(Content.RocketJump, Content.Weapon(Content.RocketJump.Id));
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void APartnersPoseCarriesTheirJetAndSpew(float pressure)
        {
            var pose = new AvatarPose { Facing = -1, AimY = 1, Diagonal = true, Jetting = true, Pressure = pressure, Rooted = true };
            AvatarPose back = AvatarPose.Unpack(pose.Pack(), 0f, 0f);
            Assert.IsTrue(back.Jetting);
            Assert.AreEqual(pressure, back.Pressure, 0.5f / 7f + 1e-4f, "in sevenths");
            Assert.IsTrue(back.Rooted);
            Assert.IsFalse(back.Spewing);
            Assert.AreEqual(-1, back.Facing);
            Assert.IsTrue(back.Diagonal);

            back = AvatarPose.Unpack(new AvatarPose { Spewing = true, Pressure = 1f }.Pack(), 0f, 0f);
            Assert.IsTrue(back.Spewing);
            Assert.IsFalse(back.Jetting);
            Assert.AreEqual(0f, back.Pressure, "no pressure without the jet");
        }

        [TestCase(1, 0, false)]
        [TestCase(-1, 0, false)]
        [TestCase(1, 1, false)]
        [TestCase(-1, 1, true)]
        [TestCase(1, -1, true)]
        [TestCase(-1, -1, false)]
        public void AnAimSurvivesThePose(int facing, int aimY, bool diagonal)
        {
            Vector2 aim = Aiming.FromPose(facing, aimY, diagonal);
            Assert.AreEqual(aimY, Aiming.AimY(aim));
            Assert.AreEqual(diagonal, Aiming.IsDiagonal(aim));
            Assert.AreEqual(1f, aim.magnitude, 1e-5f);
            if (aimY == 0 || diagonal) Assert.AreEqual(facing, Mathf.Sign(aim.x));
        }
    }
}
