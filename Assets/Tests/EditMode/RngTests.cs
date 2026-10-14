using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class RngTests
    {
        [Test]
        public void ItIsXorshift32()
        {
            // Replays will depend on this exact sequence.
            var rng = new Rng(1);
            Assert.AreEqual(270369u, rng.NextUInt());
            Assert.AreEqual(67634689u, rng.NextUInt());
            Assert.AreEqual(2647435461u, rng.NextUInt());
        }

        [Test]
        public void TheSameSeedReplaysTheSameSequence()
        {
            var a = new Rng(12345);
            var b = new Rng(12345);
            for (int i = 0; i < 100; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void DifferentSeedsDiffer()
        {
            Assert.AreNotEqual(new Rng(1).NextUInt(), new Rng(2).NextUInt());
        }

        [Test]
        public void ASeedOfZeroStillRuns()
        {
            // xorshift returns zero forever from a zero state, so zero is swapped for a fixed seed.
            Assert.AreEqual(1359758873u, new Rng(0).NextUInt());
        }

        [Test]
        public void NewSeedsAreNeverZero()
        {
            Assert.AreEqual(1u, Rng.NewSeed() & 1u);
        }

        [Test]
        public void ValueIsFromZeroToOne()
        {
            var rng = new Rng(7);
            for (int i = 0; i < 10000; i++)
            {
                float v = rng.Value;
                Assert.GreaterOrEqual(v, 0f);
                Assert.Less(v, 1f);
            }
        }

        [Test]
        public void AFloatRangeStaysInBounds()
        {
            var rng = new Rng(8);
            for (int i = 0; i < 10000; i++)
            {
                float v = rng.Range(-2f, 3f);
                Assert.GreaterOrEqual(v, -2f);
                Assert.Less(v, 3f);
            }
        }

        [Test]
        public void AnIntRangeCoversEveryValueAndNoOthers()
        {
            var rng = new Rng(9);
            var seen = new bool[6];
            for (int i = 0; i < 1000; i++)
            {
                int v = rng.Range(0, 6);
                Assert.That(v, Is.InRange(0, 5));
                seen[v] = true;
            }
            CollectionAssert.DoesNotContain(seen, false);
        }

        [TestCase(5, 5)]
        [TestCase(5, 3)]
        public void AnEmptyIntRangeGivesItsMinimum(int min, int max)
        {
            Assert.AreEqual(min, new Rng(10).Range(min, max));
        }

        [Test]
        public void InsideUnitCircleStaysInside()
        {
            var rng = new Rng(11);
            for (int i = 0; i < 2000; i++)
            {
                Vector2 p = rng.InsideUnitCircle();
                Assert.LessOrEqual(p.x * p.x + p.y * p.y, 1.0001f);
            }
        }
    }
}
