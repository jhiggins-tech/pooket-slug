using NUnit.Framework;
using UnityEngine;

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

        [TestCase("?spike=holo", true, false)]
        [TestCase("?spike=tank,holo", true, true)]
        [TestCase("?SPIKE=Holo,TANK", true, true)]
        [TestCase("?spike=tones2", false, false)] // tones2 is everyone's soldier now: there's no spike for him
        [TestCase("?spike=hol", false, false)]
        public void TheAddressTurnsSpikesOn(string query, bool holo, bool tank)
        {
            Spike.Configure(Page + query);
            Assert.AreEqual(holo, Spike.Holo);
            Assert.AreEqual(tank, Spike.Tank);
            Assert.IsFalse(Spike.Hits);
            Assert.AreEqual(holo || tank, Spike.Label != null);
        }

        [Test]
        public void TheBannerNamesEverySpikeThatsOn()
        {
            Spike.Configure(Page + "?spike=holo,hits,tank");
            Assert.IsTrue(Spike.Hits && Spike.Tank);
            Assert.AreEqual("SPIKE TEST · HOLOGRAM GOONS + HIT FEEL + BOSS TANK", Spike.Label);
            Spike.Configure(Page + "?spike=hits");
            Assert.AreEqual("SPIKE TEST · HIT FEEL", Spike.Label);
            Spike.Configure(Page + "?spike=chip");
            Assert.IsTrue(Spike.Chip);
            Assert.AreEqual("SPIKE TEST · 8-BIT SOUND", Spike.Label);
        }

        [TestCase(-8f, 0f, 45f)]
        [TestCase(-5f, -1f, 60f)]
        [TestCase(6f, 2f, 55f)]
        public void TheBossTanksShotLandsWhereItAimed(float dx, float dy, float lob)
        {
            const float g = 18f;
            float v = BossTankSpec.PowerFor(dx, dy, lob, g);
            Assert.IsFalse(float.IsNaN(v));
            // fly it: at the time it crosses dx, it's at dy
            float a = lob * UnityEngine.Mathf.Deg2Rad;
            float t = UnityEngine.Mathf.Abs(dx) / (v * UnityEngine.Mathf.Cos(a));
            float y = v * UnityEngine.Mathf.Sin(a) * t - 0.5f * g * t * t;
            Assert.AreEqual(dy, y, 1e-3f);
        }

        [Test]
        public void ATooFlatLobCantReachAHighTarget()
        {
            Assert.IsTrue(float.IsNaN(BossTankSpec.PowerFor(-4f, 6f, 30f, 18f)), "tan 30° × 4 is under 6: no speed gets there");
        }

        [Test]
        public void TheBossTankGetsQuickerAsItsHurt()
        {
            BossTankSpec spec = SpikeContent.BossTank;
            Assert.AreEqual(1f, spec.Rage(1f), 1e-6f);
            Assert.AreEqual(spec.MaxRage, spec.Rage(0f), 1e-6f);
            Assert.Greater(spec.Rage(0.5f), 1f);
            Assert.Less(spec.Rage(0.5f), spec.MaxRage);
            Assert.Greater(spec.MaxAngle, 90f, "it can lob over its own back");
        }

    
        [Test]
        public void ChipMidiNotesMatchTabks()
        {
            Assert.AreEqual(440f, Chip.Midi(69), 0.01f);
            Assert.AreEqual(880f, Chip.Midi(81), 0.01f);
        }

        /// <summary>Upward zero crossings in a stretch of samples, which counts the cycles of a plain tone.</summary>
        static int Cycles(float[] s, int from, int to)
        {
            int n = 0;
            for (int i = Mathf.Max(1, from); i < to; i++) if (s[i - 1] <= 0f && s[i] > 0f) n++;
            return n;
        }

        [Test]
        public void ChipToneIsInTuneAndStartsWhereAsked()
        {
            var chip = new Chip(1f);
            chip.Tone(0.5f, 0.5f, 441f, vol: 0.2f, env: new[] { 0f, 1f, 1f, 1f });
            float[] s = chip.Finish();
            for (int i = 0; i < Chip.Rate / 2 - 1; i++) Assert.AreEqual(0f, s[i], "silent before it starts");
            Assert.AreEqual(220, Cycles(s, Chip.Rate / 2, Chip.Rate), 1); // half a second of 441 Hz
        }

        [Test]
        public void ChipSlideEndsAtItsTargetPitch()
        {
            var chip = new Chip(1f);
            chip.Tone(0f, 1f, 200f, 800f, steps: 4, env: new[] { 0f, 1f, 1f, 1f });
            float[] s = chip.Finish();
            // stepped: the first quarter is all 200 Hz, the last quarter the 3/4 step, 200 * 4^0.75
            Assert.AreEqual(50, Cycles(s, 0, Chip.Rate / 4), 1);
            Assert.AreEqual(Mathf.RoundToInt(200f * Mathf.Pow(4f, 0.75f) / 4f), Cycles(s, 3 * Chip.Rate / 4, Chip.Rate), 1);
        }

        [Test]
        public void ChipNoiseIsTheSameEveryTimeAndFadesOut()
        {
            var a = new Chip(0.2f);
            var b = new Chip(0.2f);
            a.Noise(0f, 0.2f, rate: 0.5f, to: 0.1f);
            b.Noise(0f, 0.2f, rate: 0.5f, to: 0.1f);
            float[] x = a.Finish(), y = b.Finish();
            CollectionAssert.AreEqual(x, y);
            float early = 0f, late = 0f;
            for (int i = 0; i < 400; i++) { early += Mathf.Abs(x[i]); late += Mathf.Abs(x[x.Length - 1 - i]); }
            Assert.Greater(early, late * 10f);
        }

        [Test]
        public void ChipPileUpsNeverClip()
        {
            var chip = new Chip(0.1f);
            for (int i = 0; i < 20; i++) chip.Tone(0f, 0.1f, 300f + i * 37f, vol: 0.5f);
            foreach (float v in chip.Finish()) Assert.LessOrEqual(Mathf.Abs(v), 1f);
        }

        [Test]
        public void SpikeContentNeverGoesOnTheWire()
        {
            // Not in Content's tables, so a co-op partner can never be sent one.
            CollectionAssert.DoesNotContain(Content.Weapons, SpikeContent.BossCannon);
            CollectionAssert.DoesNotContain(Content.Projectiles, SpikeContent.BigShell);
            Assert.IsNull(Content.Weapon(SpikeContent.BossCannon.Id));
        }
    }
}
