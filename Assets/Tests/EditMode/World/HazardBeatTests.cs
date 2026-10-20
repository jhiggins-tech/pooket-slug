using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class HazardBeatTests
    {
        [Test]
        public void ItsOnForItsShareOfTheCycle_WithItsWarningBefore()
        {
            var beat = new HazardBeat(3f, 1f, 0.5f);
            int on = 0, soon = 0, steps = 3000;
            for (int i = 0; i < steps; i++)
            {
                float t = i * 3f / steps;
                bool o = beat.On(t, 7f), s = beat.Soon(t, 7f);
                Assert.IsFalse(o && s, "it warns before, not while");
                if (o) on++;
                if (s) soon++;
            }
            Assert.AreEqual(1f / 3f, on / (float)steps, 0.01f);
            Assert.AreEqual(0.5f / 3f, soon / (float)steps, 0.01f);
        }

        [Test]
        public void WithoutAWarningItsNeverSoon()
        {
            for (float t = 0f; t < 3f; t += 0.01f) Assert.IsFalse(BaseHazards.Fence.Soon(t, 4f));
        }

        [Test]
        public void ItKeepsTheBeatTheFenceAndVentAlwaysHad()
        {
            // what the fence and vent did before: (elapsed + x * spread % cycle) % cycle < on
            foreach (float x in new[] { 0f, 3f, 17.5f, 120f })
                for (float t = 0f; t < 20f; t += 0.0173f)
                {
                    Assert.AreEqual((t + x * 0.37f % 3f) % 3f < 1.4f, BaseHazards.Fence.On(t, x), $"fence at {x}, {t}");
                    Assert.AreEqual((t + x * 0.53f % 2.6f) % 2.6f < 1f, BaseHazards.Steam.On(t, x), $"vent at {x}, {t}");
                }
        }

        [Test]
        public void ItGoesOnceACycle()
        {
            var beat = new HazardBeat(2f, 0f, 0.5f);
            int went = 0;
            const float dt = 1f / 60f;
            for (float t = dt; t <= 10f; t += dt) if (beat.Went(t, dt, 5f)) went++;
            Assert.AreEqual(5, went, 1);
            Assert.IsFalse(beat.Went(1f, 0f, 5f));
        }

        [Test]
        public void NeighboursKeepTheirOwnBeat()
        {
            int differ = 0;
            for (float t = 0f; t < 3f; t += 0.05f) if (BaseHazards.Fence.On(t, 10f) != BaseHazards.Fence.On(t, 12f)) differ++;
            Assert.Greater(differ, 0);
        }
    }
}
