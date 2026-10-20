using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>kcaj's kit as rules (<see cref="KcajTuning"/>): Double Park's lobbed cones, Hyperfixate's lock and burn, and Unmedicated's pill rain.</summary>
    public class KcajKitTests
    {
        const float Frame = 1f / 60f;
        const float ScreenWidth = 20f; // Tuning.Camera.ViewHeight on a 16:9 phone held sideways

        // ------------------------------------------------------------------ Double Park

        [Test]
        public void KcajsKitIsHisOwn()
        {
            Assert.AreSame(Content.DoublePark, Content.Kcaj.Primary);
            Assert.AreSame(Content.Hyperfixate, Content.Kcaj.Special);
            Assert.AreSame(Content.Unmedicated, Content.Kcaj.Super);
            Assert.AreEqual(WeaponBehaviour.Lob, Content.DoublePark.Behaviour, "the cones are lobbed");
            Assert.AreSame(Content.Cone, Content.DoublePark.Projectile);
            Assert.AreEqual(KcajTuning.Cones.Gravity, Content.Cone.Gravity, "the cone falls as the rule says");
            Assert.IsTrue(Content.Cone.Explodes, "a cone splats on whatever it lands on");
            Assert.AreEqual(KcajTuning.Beam.Charges, Content.Kcaj.StartSpecials);
            Assert.AreEqual(KcajTuning.Beam.Charges, Content.Kcaj.SpecialMax);
            Assert.AreEqual(KcajTuning.Beam.RechargeTime, Content.Kcaj.SpecialRefill);
        }

        [Test]
        public void AimingAheadLobsBothConesInAnArc()
        {
            var cones = new DoubleParkSpec();
            for (int i = 0; i < cones.Count; i++)
            {
                Vector2 v = cones.Velocity(i, Vector2.right, 1, Vector2.zero);
                Assert.Greater(v.x, 0f, "ahead");
                Assert.Greater(v.y, 0f, "and up: it's lobbed, not fired flat");
                Assert.AreEqual(cones.Speed, v.magnitude, 1e-4f);
            }
            Assert.AreEqual(cones.Lift, cones.Throw(Vector2.right, 1), 1e-4f);
        }

        [Test]
        public void TheTwoConesLandEitherSideOfTheAim()
        {
            var cones = new DoubleParkSpec();
            Vector2 low = cones.Velocity(0, Vector2.right, 1, Vector2.zero), high = cones.Velocity(1, Vector2.right, 1, Vector2.zero);
            float lowAngle = Mathf.Atan2(low.y, low.x) * Mathf.Rad2Deg, highAngle = Mathf.Atan2(high.y, high.x) * Mathf.Rad2Deg;
            Assert.AreEqual(cones.Lift - cones.Spread, lowAngle, 1e-3f);
            Assert.AreEqual(cones.Lift + cones.Spread, highAngle, 1e-3f);
            Assert.Greater(Mathf.Abs(cones.Reach(lowAngle) - cones.Reach(highAngle)), 0.3f, "one lands short and one long");
            Assert.Greater(cones.Reach(lowAngle), 4f, "a lob lands well ahead");
            Assert.Less(cones.Reach(highAngle), ScreenWidth * 0.5f, "inside the screen ahead of him");
        }

        [Test]
        public void FacingLeftMirrorsTheThrow()
        {
            var cones = new DoubleParkSpec();
            Vector2 right = cones.Velocity(0, Vector2.right, 1, Vector2.zero), left = cones.Velocity(0, Vector2.left, -1, Vector2.zero);
            Assert.AreEqual(-right.x, left.x, 1e-4f);
            Assert.AreEqual(right.y, left.y, 1e-4f);
        }

        [Test]
        public void AimingUpOrDownBendsTheThrow()
        {
            var cones = new DoubleParkSpec();
            Assert.AreEqual(cones.Highest, cones.Throw(Vector2.up, 1), 1e-4f, "straight up is as steep as it goes, still a little ahead");
            Assert.Less(cones.Throw(Vector2.up, 1), 90f);
            float down = cones.Throw(Vector2.down, 1);
            Assert.AreEqual(Mathf.Max(cones.Lowest, -90f + cones.Lift), down, 1e-4f);
            Assert.Less(cones.Velocity(1, Vector2.down, 1, Vector2.zero).y, 0f, "aiming down in the air throws them down");
            Assert.Greater(cones.Throw(new Vector2(1f, 1f), 1), cones.Throw(Vector2.right, 1), "aiming up the diagonal throws higher");
        }

        [Test]
        public void ARunningThrowCarriesSomeOfHisSpeed()
        {
            var cones = new DoubleParkSpec();
            Vector2 still = cones.Velocity(0, Vector2.right, 1, Vector2.zero), running = cones.Velocity(0, Vector2.right, 1, new Vector2(6f, 0f));
            Assert.AreEqual(still.x + 6f * cones.Carry, running.x, 1e-4f);
        }

        [Test]
        public void ARainFromTheSameSeedFallsTheSameOnBothScreens()
        {
            var spec = new UnmedicatedSpec();
            uint seed = PillRain.Seed(new Rng(42));
            Assert.Greater(seed, 0u);
            Assert.Less(seed, 1u << 24, "it crosses the network in a float, exactly");
            Assert.AreEqual(seed, (uint)(float)seed);
            var mine = new PillRain(spec);
            var theirs = new PillRain(spec);
            mine.Start(seed, 0f, ScreenWidth, 11f);
            theirs.Start((uint)(float)seed, 0f, ScreenWidth, 11f);
            var flat = new Flat();
            for (int f = 0; f < 60; f++)
            {
                mine.Step(Frame, flat, null);
                theirs.Step(Frame, flat, null);
            }
            for (int i = 0; i < mine.Capacity; i++) Assert.AreEqual(mine.Position(i), theirs.Position(i), "pill " + i);
        }

        // ------------------------------------------------------------------ Hyperfixate

        [Test]
        public void TheBeamSticksToTheFirstThingItTouches()
        {
            var beam = new HyperfixateSpec();
            var centres = new[] { new Vector2(8f, 0f), new Vector2(4f, 0f), new Vector2(4f, 3f) };
            var halves = new[] { new Vector2(0.3f, 0.7f), new Vector2(0.3f, 0.7f), new Vector2(0.3f, 0.7f) };
            Assert.AreEqual(1, beam.FirstTouched(Vector2.zero, Vector2.right, centres, halves, 3, float.MaxValue), "the nearer goon on the line");
            Assert.AreEqual(-1, beam.FirstTouched(Vector2.zero, Vector2.left, centres, halves, 3, float.MaxValue), "nothing behind him");
            Assert.AreEqual(2, beam.FirstTouched(Vector2.zero, new Vector2(4f, 3f).normalized, centres, halves, 3, float.MaxValue), "it follows the aim");
        }

        [Test]
        public void AWallStopsTheBeamFromFindingAnything()
        {
            var beam = new HyperfixateSpec();
            var centres = new[] { new Vector2(6f, 0f) };
            var halves = new[] { new Vector2(0.3f, 0.7f) };
            Assert.AreEqual(-1, beam.FirstTouched(Vector2.zero, Vector2.right, centres, halves, 1, wall: 3f));
            Assert.AreEqual(0, beam.FirstTouched(Vector2.zero, Vector2.right, centres, halves, 1, wall: 7f));
        }

        [Test]
        public void TheBeamReachesMostOfAScreenButNotPastIt()
        {
            var beam = new HyperfixateSpec();
            var halves = new[] { new Vector2(0.3f, 0.7f) };
            Assert.AreEqual(0, beam.FirstTouched(Vector2.zero, Vector2.right, new[] { new Vector2(beam.Range - 0.5f, 0f) }, halves, 1, float.MaxValue));
            Assert.AreEqual(-1, beam.FirstTouched(Vector2.zero, Vector2.right, new[] { new Vector2(beam.Range + 1f, 0f) }, halves, 1, float.MaxValue));
            Assert.Greater(beam.Range, ScreenWidth / 2f, "it reaches past the middle of the screen");
            Assert.Less(beam.Range, ScreenWidth);
            Assert.Greater(beam.BreakRange, beam.Range, "a target that steps back stays stuck");
        }

        [Test]
        public void TheBurnGrowsTheLongerItStaysOn()
        {
            var beam = new HyperfixateSpec();
            float last = 0f;
            for (float t = beam.TickInterval; t <= beam.MaxHold; t += beam.TickInterval)
            {
                Assert.GreaterOrEqual(beam.TickDamage(t), last, "longer fixate, higher damage ticks (owner's note)");
                last = beam.TickDamage(t);
            }
            Assert.Greater(beam.TickDamage(beam.RampTime), beam.TickDamage(beam.TickInterval) * 4f, "it gets much hotter, not a little");
            Assert.AreEqual(beam.HottestTick, beam.TickDamage(beam.MaxHold), 1e-5f);
        }

        [Test]
        public void HoldingOnIsWorthItAgainstSomethingBig()
        {
            var beam = new HyperfixateSpec();
            float pistolPerSecond = Content.Bullet.Damage / Content.Pistol.Cooldown;
            Assert.Less(beam.TotalDamage(1f), pistolPerSecond * 1f, "a quick flick is weaker than just shooting");
            Assert.Greater(beam.TotalDamage(beam.MaxHold), pistolPerSecond * beam.MaxHold, "the whole hold beats shooting for as long");
            float tabk = Content.Tank.Hp;
            Assert.Greater(beam.TotalDamage(beam.MaxHold), tabk * 0.4f, "a full charge takes a big bite out of a tabk");
            Assert.Less(beam.TotalDamage(beam.MaxHold), tabk, "but no charge kills one alone");
        }

        [Test]
        public void ARiflemanBurnsInAboutTwoSeconds()
        {
            var beam = new HyperfixateSpec();
            float t = 0f, burnt = 0f;
            while (burnt < Content.Rifleman.Hp && t < beam.MaxHold) { t += beam.TickInterval; burnt += beam.TickDamage(t); }
            Assert.Less(t, 2.5f);
            Assert.Greater(t, 1f, "it isn't for small fry");
        }

        [Test]
        public void TheFixationBurnsInTicksWhileHeld()
        {
            var beam = new HyperfixateSpec();
            var fix = new Hyperfixation(beam);
            fix.Lock();
            float burnt = 0f;
            for (float t = 0f; t < 2f - 1e-4f; t += Frame) burnt += fix.Step(Frame, held: true, alive: true, distance: 5f);
            Assert.IsTrue(fix.Locked);
            Assert.AreEqual(beam.TotalDamage(2f), burnt, 0.01f, "the same burn whatever the frame rate");
            Assert.AreEqual(2f, fix.Held, 1e-3f);
        }

        [Test]
        public void TheBurnDoesntDependOnTheFrameRate()
        {
            var beam = new HyperfixateSpec();
            float a = 0f, b = 0f;
            var fast = new Hyperfixation(beam);
            var slow = new Hyperfixation(beam);
            fast.Lock();
            slow.Lock();
            for (int i = 0; i < 300; i++) a += fast.Step(1f / 120f, true, true, 3f);
            for (int i = 0; i < 50; i++) b += slow.Step(1f / 20f, true, true, 3f);
            Assert.AreEqual(a, b, 0.01f);
        }

        [Test]
        public void LettingGoEndsIt()
        {
            var fix = new Hyperfixation(new HyperfixateSpec());
            fix.Lock();
            fix.Step(0.5f, true, true, 3f);
            Assert.AreEqual(0f, fix.Step(Frame, held: false, alive: true, distance: 3f));
            Assert.IsFalse(fix.Locked);
            Assert.AreEqual(FixateEnd.LetGo, fix.Ended);
            Assert.AreEqual(0f, fix.Step(1f, true, true, 3f), "nothing burns until it sticks again");
        }

        [Test]
        public void ItLetsGoOfTheDeadAndTheFarAway()
        {
            var beam = new HyperfixateSpec();
            var fix = new Hyperfixation(beam);
            fix.Lock();
            fix.Step(Frame, true, alive: false, distance: 3f);
            Assert.AreEqual(FixateEnd.TargetGone, fix.Ended);
            fix.Lock();
            fix.Step(Frame, true, true, distance: beam.BreakRange + 0.1f);
            Assert.AreEqual(FixateEnd.OutOfReach, fix.Ended);
        }

        [Test]
        public void OneChargeHoldsOnForAFewSecondsAtMost()
        {
            var beam = new HyperfixateSpec();
            var fix = new Hyperfixation(beam);
            fix.Lock();
            float burnt = 0f;
            for (int i = 0; i < 600; i++) burnt += fix.Step(Frame, true, true, 3f);
            Assert.IsFalse(fix.Locked);
            Assert.AreEqual(FixateEnd.Spent, fix.Ended);
            Assert.AreEqual(beam.TotalDamage(beam.MaxHold), burnt, 0.01f);
        }

        [Test]
        public void TheScreenEdgesDimWhileFixated()
        {
            var beam = new HyperfixateSpec();
            var fix = new Hyperfixation(beam);
            Assert.AreEqual(0f, fix.Dim);
            fix.Lock();
            fix.Step(0.1f, true, true, 3f);
            float early = fix.Dim;
            fix.Step(beam.DimTime, true, true, 3f);
            Assert.Greater(early, 0f);
            Assert.Greater(fix.Dim, early, "they close in");
            Assert.AreEqual(beam.MaxDim, fix.Dim, 1e-4f);
            Assert.Less(beam.MaxDim, 1f, "you can still see what's shooting you");
            fix.Step(Frame, false, true, 3f);
            Assert.AreEqual(0f, fix.Dim, "the screen clears when it ends");
        }

        [Test]
        public void FixatedHeOnlyShuffles()
        {
            Assert.Greater(KcajTuning.Beam.WalkScale, 0f);
            Assert.Less(KcajTuning.Beam.WalkScale, 0.5f);
        }

        // ------------------------------------------------------------------ Unmedicated

        sealed class Flat : IPillGround
        {
            public bool Solid(float x, float y) => y < 0f;
        }

        sealed class Pit : IPillGround
        {
            public bool Solid(float x, float y) => y < 0f && (x < 9f || x > 11f);
        }

        static PillRain Rain(UnmedicatedSpec spec, uint seed = 7)
        {
            var rain = new PillRain(spec);
            rain.Start(new Rng(seed), 0f, ScreenWidth, Tuning.Camera.ViewHeight);
            return rain;
        }

        [Test]
        public void ItRainsAHundredAndTwentyPills()
        {
            Assert.AreEqual(120, KcajTuning.Pills.Count);
            Assert.AreEqual(120, Rain(KcajTuning.Pills).Capacity);
        }

        [Test]
        public void EveryPillBouncesTwiceThenPops()
        {
            var spec = new UnmedicatedSpec();
            var rain = Rain(spec);
            var ground = new Flat();
            int pops = 0, frames = 0;
            while (!rain.Done && frames++ < 60 * 10)
            {
                rain.Step(Frame, ground, null);
                pops += rain.PopCount;
            }
            Assert.IsTrue(rain.Done, "the rain is over in a few seconds");
            Assert.AreEqual(spec.Count, pops, "every pill pops once");
            for (int i = 0; i < spec.Count; i++) Assert.AreEqual(spec.Landings, rain.Landed(i), "two bounces, then it pops on the third landing");
        }

        [Test]
        public void TheRainIsOverInAFewSeconds()
        {
            var spec = new UnmedicatedSpec();
            var rain = Rain(spec);
            var ground = new Flat();
            float t = 0f;
            while (!rain.Done && t < 10f) { rain.Step(Frame, ground, null); t += Frame; }
            Assert.Less(t, spec.MaxLife + spec.RainTime);
            Assert.Greater(t, 2f, "long enough to watch it");
        }

        [Test]
        public void ItRainsAcrossTheWholeScreen()
        {
            var spec = new UnmedicatedSpec();
            var rain = Rain(spec);
            var ground = new Flat();
            var quarters = new int[4];
            while (!rain.Done)
            {
                rain.Step(Frame, ground, null);
                for (int p = 0; p < rain.PopCount; p++)
                    quarters[Mathf.Clamp((int)(rain.Pops[p].x / (ScreenWidth / 4f)), 0, 3)]++;
            }
            foreach (int q in quarters) Assert.Greater(q, spec.Count / 8, "no quarter of the screen is left dry");
        }

        [Test]
        public void ThePillsDontAllComeDownAtOnce()
        {
            var spec = new UnmedicatedSpec();
            var rain = Rain(spec);
            var ground = new Flat();
            rain.Step(Frame, ground, null);
            Assert.Less(rain.Live, spec.Count / 4, "it starts as a sprinkle");
            for (float t = Frame; t < spec.RainTime; t += Frame) rain.Step(Frame, ground, null);
            Assert.Greater(rain.Live, spec.Count / 2, "and becomes a downpour");
        }

        [Test]
        public void APillThatFallsIntoAPitStillPops()
        {
            var spec = new UnmedicatedSpec();
            var rain = Rain(spec);
            var ground = new Pit();
            int pops = 0;
            for (float t = 0f; t < spec.MaxLife + spec.RainTime + 0.5f; t += Frame) { rain.Step(Frame, ground, null); pops += rain.PopCount; }
            Assert.IsTrue(rain.Done);
            Assert.AreEqual(spec.Count, pops);
        }

        [Test]
        public void APillPopsOnAGoon()
        {
            var spec = new UnmedicatedSpec();
            var rain = Rain(spec);
            var ground = new Flat();
            // A goon under the middle of the screen, a rifleman's size.
            System.Func<float, float, bool> goon = (px, py) => Mathf.Abs(px - 10f) < 0.3f && py < 1.35f;
            int onGoon = 0;
            while (!rain.Done)
            {
                rain.Step(Frame, ground, goon);
                for (int p = 0; p < rain.PopCount; p++)
                    if (Mathf.Abs(rain.Pops[p].x - 10f) < 0.31f && rain.Pops[p].y < 1.36f && rain.Pops[p].y > 0.2f) onGoon++;
            }
            Assert.Greater(onGoon, 0, "pills land on goons' heads and pop there");
        }

        [Test]
        public void TheSameSeedRainsTheSame()
        {
            var spec = new UnmedicatedSpec();
            var a = Rain(spec, 42);
            var b = Rain(spec, 42);
            var ground = new Flat();
            for (int f = 0; f < 120; f++) { a.Step(Frame, ground, null); b.Step(Frame, ground, null); }
            for (int i = 0; i < spec.Count; i++) Assert.AreEqual(a.Position(i), b.Position(i));
        }

        [Test]
        public void ItHurtsButIsntAScreenClearer()
        {
            // A screen of riflemen standing a couple of steps apart: some go down, some are left standing. Clearing the
            // screen is what team supers are for (docs/design/team-supers.md).
            var spec = new UnmedicatedSpec();
            int goons = 8;
            int killed = 0, hurt = 0;
            for (uint seed = 1; seed <= 20; seed++)
            {
                var hp = new float[goons];
                for (int g = 0; g < goons; g++) hp[g] = Content.Rifleman.Hp;
                var rain = Rain(spec, seed);
                var ground = new Flat();
                System.Func<float, float, bool> goon = (px, py) =>
                {
                    for (int g = 0; g < goons; g++)
                        if (hp[g] > 0f && Mathf.Abs(px - GoonX(g, goons)) < 0.3f && py < 1.35f) return true;
                    return false;
                };
                while (!rain.Done)
                {
                    rain.Step(Frame, ground, goon);
                    for (int p = 0; p < rain.PopCount; p++)
                        for (int g = 0; g < goons; g++)
                            if (Vector2.Distance(rain.Pops[p], new Vector2(GoonX(g, goons), 0.7f)) < spec.PopRadius + 0.3f) hp[g] -= spec.PopDamage;
                }
                for (int g = 0; g < goons; g++)
                {
                    if (hp[g] <= 0f) killed++;
                    if (hp[g] < Content.Rifleman.Hp) hurt++;
                }
            }
            float total = goons * 20f;
            Assert.Greater(hurt / total, 0.9f, "almost everything on screen gets hit");
            Assert.Greater(killed / total, 0.2f, "it kills some");
            Assert.Less(killed / total, 0.7f, "but leaves a good few standing");
        }

        static float GoonX(int g, int goons) => (g + 0.5f) * ScreenWidth / goons;

        [Test]
        public void ThePopsNeverHurtTheCrew()
        {
            Assert.IsFalse(KcajTuning.Pills.HurtsSoldiers, "they never hurt kcaj or a co-op partner (kits.md)");
        }
    }
}
