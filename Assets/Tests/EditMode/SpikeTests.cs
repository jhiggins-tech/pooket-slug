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

        [TestCase("?spike=craters", true)]
        [TestCase("?SPIKE=Craters,TANK", true)]
        [TestCase("?spike=tones2,holo,chip,tank,hits", false)] // all five are in the game now: no spikes for them
        [TestCase("?spike=crater", false)]
        public void TheAddressTurnsSpikesOn(string query, bool craters)
        {
            Spike.Configure(Page + query);
            Assert.AreEqual(craters, Spike.Craters);
            Assert.AreEqual(craters, Spike.Label != null);
        }

        [Test]
        public void TheBannerNamesEverySpikeThatsOn()
        {
            Spike.Configure(Page + "?spike=craters,holo");
            Assert.IsTrue(Spike.Craters);
            Assert.AreEqual("SPIKE TEST · CRATERS", Spike.Label);
            Spike.Configure(Page + "?spike=hits"); // the hit feel is the game's own now: no longer a spike
            Assert.IsNull(Spike.Label);
            Spike.Configure(Page + "?spike=kie"); // kie's tank is the game's own boss now: no longer a spike
            Assert.IsNull(Spike.Label);
            Spike.Configure(Page + "?spike=kie-weasels,kie-trollogram,kie-steal,steal"); // kie's kit is the game's own now
            Assert.IsNull(Spike.Label);
            Spike.Configure(Page + "?spike=chip"); // the 8-bit sounds are the game's own now: no longer a spike
            Assert.IsNull(Spike.Label);
        }

        [Test]
        public void YourTankIsNoLongerASpike()
        {
            Spike.Configure(Page + "?spike=drive"); // your tank is the game's own now (?test=tank parks one)
            Assert.IsFalse(Spike.Any);
            Assert.IsNull(Spike.Label);
        }

        [Test]
        public void TheCallInSpikeIsItsOwn()
        {
            Spike.Configure(Page + "?spike=callin");
            Assert.IsTrue(Spike.CallIn);
            Assert.AreEqual("SPIKE TEST · TABK CALL-IN", Spike.Label);
            Spike.Configure(Page + "?spike=call");
            Assert.IsFalse(Spike.CallIn);
        }

        [Test]
        public void ThePilotGlitchSpikeIsItsOwn()
        {
            Spike.Configure(Page + "?spike=pilot-glitch");
            Assert.IsTrue(Spike.PilotGlitch);
            Assert.AreEqual("SPIKE TEST · PILOT GLITCH-OUT", Spike.Label);
            Spike.Configure(Page + "?spike=pilot");
            Assert.IsFalse(Spike.PilotGlitch);
        }

        [Test]
        public void TheCageOfLightFlickersOut()
        {
            PilotFreeingSpec spec = KieTuning.Freeing;
            Assert.IsTrue(spec.CageLit(0f), "lit as he appears");
            Assert.IsTrue(spec.CageLit(spec.Cage * 0.2f), "steady at first");
            Assert.IsFalse(spec.CageLit(spec.Cage), "out when he drops");
            int off = 0, steps = 200;
            for (int i = 0; i < steps; i++) if (!spec.CageLit(spec.Cage * (0.3f + 0.7f * i / steps))) off++;
            Assert.That(off, Is.InRange(steps / 5, steps * 3 / 4), "it stutters on and off before it goes");
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
    }
}
