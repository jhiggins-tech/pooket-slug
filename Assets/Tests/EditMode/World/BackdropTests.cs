using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>A mission's painted backdrop (<see cref="Backdrop"/>): its layers, where its plates live, and which plates cover the screen.</summary>
    public class BackdropTests
    {
        const float Half = 16f; // half a phone screen held sideways

        [Test]
        public void TheLayersGoBackToFront()
        {
            for (int i = 1; i < Backdrop.Layers.Count; i++)
                Assert.Less(Backdrop.Layers[i].Factor, Backdrop.Layers[i - 1].Factor, Backdrop.Layers[i].Name + " is nearer than " + Backdrop.Layers[i - 1].Name);
            foreach (BackdropLayer l in Backdrop.Layers)
            {
                Assert.Greater(l.Factor, 0f, l.Name + " moves slower than the ground");
                Assert.LessOrEqual(l.Factor, 1f, l.Name);
            }
        }

        [Test]
        public void PlatesLiveUnderTheMissionsId()
        {
            Assert.AreEqual("Plates/mission-1/far-0", Backdrop.PlatePath(Missions.One, Backdrop.Layers[1], 0));
            Assert.AreEqual("Plates/mission-1/mid-3", Backdrop.PlatePath(Missions.One, Backdrop.Layers[2], 3));
        }

        [TestCase(0f)]
        [TestCase(16f)]
        [TestCase(137.3f)]
        [TestCase(512f)]
        public void PlatesCoverTheWholeScreenWithNoGaps(float camX)
        {
            var into = new (int plate, float x)[8];
            var widths = new[] { 20f, 12f, 20f };
            int n = Backdrop.Cover(camX, Half, 0.85f, widths, into);
            Assert.Greater(n, 0);
            Assert.LessOrEqual(into[0].x, camX - Half, "the first plate starts at or left of the screen");
            Assert.Greater(into[0].x + widths[into[0].plate], camX - Half, "and reaches onto it");
            for (int i = 1; i < n; i++)
            {
                Assert.AreEqual(into[i - 1].x + widths[into[i - 1].plate], into[i].x, 1e-3f, "each plate starts where the last ends");
                Assert.AreEqual((into[i - 1].plate + 1) % widths.Length, into[i].plate, "in strip order, round and round");
            }
            Assert.GreaterOrEqual(into[n - 1].x + widths[into[n - 1].plate], camX + Half, "the last reaches past the screen's right edge");
        }

        [Test]
        public void AFarLayerMovesSlowerThanTheCamera()
        {
            var into = new (int plate, float x)[4];
            var widths = new[] { 20f };
            Backdrop.Cover(100f, Half, 0.85f, widths, into);
            float before = into[0].x;
            Backdrop.Cover(101f, Half, 0.85f, widths, into);
            // the plate the screen started on moves 0.85 of the camera's step, so on screen it slides back 0.15
            float moved = into[0].x - before;
            if (moved < -10f) moved += 20f; // the next plate along took over as the first
            Assert.AreEqual(0.85f, moved, 1e-3f);
        }

        [Test]
        public void TheStripRunsAlongTheMapThenRepeats()
        {
            // with the layer standing still (a factor of 0), the strip lies along the map from 0
            var into = new (int plate, float x)[8];
            var widths = new[] { 10f, 10f, 10f };
            int n = Backdrop.Cover(25f, 6f, 0f, widths, into);
            Assert.AreEqual(3, n);
            Assert.AreEqual((1, 10f), into[0]);
            Assert.AreEqual((2, 20f), into[1]);
            Assert.AreEqual((0, 30f), into[2], "after the last plate the first comes round again");
        }

        [Test]
        public void NoPlatesCoverNothing()
        {
            Assert.AreEqual(0, Backdrop.Cover(50f, Half, 0.6f, new float[0], new (int, float)[4]));
        }
    }
}
