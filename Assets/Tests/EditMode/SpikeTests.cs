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

        [TestCase("?spike=hits", true)]
        [TestCase("?SPIKE=Hits,TANK", true)]
        [TestCase("?spike=tones2,holo,chip,tank", false)] // all four are in the game now: no spikes for them
        [TestCase("?spike=hit", false)]
        public void TheAddressTurnsSpikesOn(string query, bool hits)
        {
            Spike.Configure(Page + query);
            Assert.AreEqual(hits, Spike.Hits);
            Assert.AreEqual(hits, Spike.Label != null);
        }

        [Test]
        public void TheBannerNamesEverySpikeThatsOn()
        {
            Spike.Configure(Page + "?spike=hits,holo");
            Assert.IsTrue(Spike.Hits);
            Assert.AreEqual("SPIKE TEST · HIT FEEL", Spike.Label);
            Spike.Configure(Page + "?spike=hits");
            Assert.AreEqual("SPIKE TEST · HIT FEEL", Spike.Label);
            Spike.Configure(Page + "?spike=kie,hits");
            Assert.IsTrue(Spike.Kie);
            Assert.AreEqual("SPIKE TEST · HIT FEEL + KIE'S TANK", Spike.Label);
            Spike.Configure(Page + "?spike=kie-weasels");
            Assert.IsTrue(Spike.Weasels);
            Assert.IsFalse(Spike.Kie, "kie's kit is not kie's tank");
            Assert.AreEqual("SPIKE TEST · WEASEL POP", Spike.Label);
            Spike.Configure(Page + "?spike=chip"); // the 8-bit sounds are the game's own now: no longer a spike
            Assert.IsNull(Spike.Label);
        }

        [Test]
        public void TheWeaselSpikeHasYouPlayKie()
        {
            Spike.Configure(Page);
            Assert.IsNull(Spike.Soldier);
            Spike.Configure(Page + "?spike=kie-weasels");
            Assert.AreSame(Content.Kie, Spike.Soldier);
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
