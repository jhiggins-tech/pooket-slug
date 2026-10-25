using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>The wire meter (it began as spike coop-weapons, in the game now): what goes over Photon, and one message for a burst instead of a shot per particle.</summary>
    public class WireMeterTests
    {
        sealed class Flat : IPillGround
        {
            public bool Solid(float x, float y) => y < 0f;
        }

        [Test]
        public void ABurstRainsTheSamePillsOnBothScreens()
        {
            var burst = new BurstEvent(1, 0xC0FFEEu, 30f, Tuning.Camera.ViewHeight, 20f);
            var mine = new PillRain(KcajTuning.Pills);
            var theirs = new PillRain(KcajTuning.Pills);
            burst.Start(mine);
            burst.Start(theirs);
            var ground = new Flat();
            int pops = 0;
            for (int frame = 0; frame < 60 * 6 && !mine.Done; frame++)
            {
                mine.Step(1f / 60f, ground, null);
                theirs.Step(1f / 60f, ground, null);
                Assert.AreEqual(mine.PopCount, theirs.PopCount, $"frame {frame}");
                for (int i = 0; i < mine.PopCount; i++) Assert.AreEqual(mine.Pops[i], theirs.Pops[i], $"frame {frame}, pop {i}");
                pops += mine.PopCount;
            }
            Assert.IsTrue(mine.Done && theirs.Done);
            Assert.AreEqual(KcajTuning.Pills.Count, pops, "every pill popped, in the same places");
        }

        [Test]
        public void AnotherSeedRainsDifferently()
        {
            var a = new PillRain(KcajTuning.Pills);
            var b = new PillRain(KcajTuning.Pills);
            new BurstEvent(1, 1u, 0f, 11f, 20f).Start(a);
            new BurstEvent(1, 2u, 0f, 11f, 20f).Start(b);
            Assert.AreNotEqual(a.Position(0), b.Position(0));
        }

        [Test]
        public void OneBurstCostsFarLessThanAShotPerPill()
        {
            int asShots = KcajTuning.Pills.Count * WireMeter.Bytes(WireKind.Shot);
            int asBurst = WireMeter.Bytes(WireKind.Burst);
            Assert.Less(asBurst * 50, asShots, $"{asBurst} bytes against {asShots}");
        }

        [Test]
        public void TheMeterSumsUpEachStretch()
        {
            var meter = new WireMeter();
            Assert.IsNull(meter.Tick(WireMeter.Window), "nothing sent: nothing to say");
            for (int i = 0; i < 30; i++) meter.Count(WireKind.Shot);
            meter.Count(WireKind.Burst);
            Assert.AreEqual(30 * WireMeter.Bytes(WireKind.Shot) + WireMeter.Bytes(WireKind.Burst), meter.TotalBytes);
            Assert.IsNull(meter.Tick(1f));
            string line = meter.Tick(WireMeter.Window);
            StringAssert.Contains("30 shot, 1 burst", line);
            int rate = Mathf.RoundToInt((30 * WireMeter.Bytes(WireKind.Shot) + WireMeter.Bytes(WireKind.Burst)) / (WireMeter.Window + 1f));
            StringAssert.Contains("about " + rate + " bytes a second", line);
            Assert.AreEqual(0, meter.TotalBytes, "and starts again");
        }
    }
}
