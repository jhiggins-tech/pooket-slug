using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>kie's tabk comes home: once, on the first bay visit with it won, in three shots.</summary>
    public class KieHomecomingTests
    {
        static Profile AfterOne()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
            return p;
        }

        [Test]
        public void ItPlays_OnceKiesTabkIsWon_OnceEver()
        {
            Assert.IsFalse(KieHomecoming.Due(new Profile(), TabkCards.Won(new Profile()), false), "not before Mission one's beaten");
            Profile p = AfterOne();
            Assert.IsTrue(KieHomecoming.Due(p, TabkCards.Won(p), false));
            Assert.IsFalse(KieHomecoming.Due(p, TabkCards.Won(p), true), "a ?test= jump skips it");
            p.MarkSeen(KieHomecoming.Reveal);
            Assert.IsFalse(KieHomecoming.Due(p, TabkCards.Won(p), false));
        }

        [Test]
        public void ShotOne_TheSpannerThenTheCone_UnderTheSparks()
        {
            var b = new KieHomecoming();
            Assert.AreEqual(1, b.Shot);
            Assert.IsFalse(b.Projector, "half a tabk");
            Step(b, 1f);
            Assert.IsTrue(b.Spanner);
            Assert.IsFalse(b.Cone);
            Step(b, 1.2f);
            Assert.IsFalse(b.Spanner);
            Assert.IsTrue(b.Cone);
            Assert.AreEqual(0f, b.Kie, "still under it");
        }

        [Test]
        public void ShotTwo_TheProjectorGoesOn_KieComesOut_ThreeLines()
        {
            var b = new KieHomecoming();
            Step(b, KieHomecoming.Shot2 + 0.001f);
            Assert.AreEqual(2, b.Shot);
            Assert.IsTrue(b.Projector);
            Assert.AreEqual(0f, b.Lowered, 1e-3f);
            Step(b, KieHomecoming.LowerTo - KieHomecoming.Shot2);
            Assert.AreEqual(1f, b.Lowered, 1e-3f);
            Step(b, 0.8f);
            Assert.AreEqual(1f, b.Kie, 1e-3f, "out and standing");
            Assert.AreEqual("IT USED TO PROJECT ME.", b.Line.Value.text);
            Step(b, 1.1f);
            Assert.IsFalse(b.Line.Value.kie, "the player asks");
        }

        [Test]
        public void ShotThree_InHeGoes_TheReelsSpin_TheCardDrops_ThenItsDone()
        {
            var b = new KieHomecoming();
            Step(b, KieHomecoming.Shot3 + 0.01f);
            Assert.AreEqual(3, b.Shot);
            Assert.Greater(b.Kie, 1f, "climbing in");
            Step(b, 2f);
            Assert.IsTrue(b.Glow);
            Assert.IsTrue(b.Holo);
            Assert.AreEqual(1f, b.Card, 1e-3f);
            Assert.AreEqual("GOOD AS NEW. BETTER, ACTUALLY.", b.Line.Value.text);
            Step(b, 2f);
            Assert.IsTrue(b.Done);
            Assert.IsNull(b.Line);
        }

        [Test]
        public void ATap_SkipsIt()
        {
            var b = new KieHomecoming();
            b.Tick(1f);
            b.Skip();
            Assert.IsTrue(b.Done);
        }

        static void Step(KieHomecoming b, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) b.Tick(System.Math.Min(1f / 60f, seconds - t));
        }
    }
}
