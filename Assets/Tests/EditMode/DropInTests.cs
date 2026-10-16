using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>Spike drop-in: joining a mission under way, and each player's own score.</summary>
    public class DropInTests
    {
        [Test]
        public void OnlyAJoinerCanDropIn()
        {
            Assert.IsFalse(DropIn.UnderWay(true, 80f), "the authority runs the world: it never joins one");
            Assert.IsFalse(DropIn.UnderWay(false, Tuning.Coop.HalfWidth), "still at the start");
            Assert.IsTrue(DropIn.UnderWay(false, 80f));
        }

        [TestCase(0, "000000")]
        [TestCase(1250, "001250")]
        [TestCase(-5, "000000")]
        [TestCase(12345678, "999999")]
        public void ScoresFitTheHud(int points, string shown) => Assert.AreEqual(shown, DropIn.Points(points));

        [Test]
        public void TheOwnScoresSpikeIsItsOwn()
        {
            Spike.Configure("https://x/?spike=dropin");
            try
            {
                Assert.IsTrue(Spike.DropIn);
                Assert.IsFalse(Spike.Partner);
                Assert.AreEqual("SPIKE TEST · OWN SCORES", Spike.Label);
            }
            finally
            {
                Spike.Configure("https://x/");
            }
        }
    }
}
