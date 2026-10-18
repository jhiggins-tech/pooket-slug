using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>What every crewmate's kit shares: the drip that spreads a flow over frames, and the kit moves sent to a partner.</summary>
    public class CrewKitTests
    {
        [Test]
        public void ADripHandsOutWholeDropsAndCarriesTheRestOver()
        {
            var drip = new Drip();
            int drops = 0;
            for (int frame = 0; frame < 60; frame++) drops += drip.Take(1f / 60f, 90f);
            Assert.That(drops, Is.InRange(89, 90), "90 a second comes out as 90 in a second, whatever the frames");

            Assert.AreEqual(0, drip.Take(0.004f, 100f), "less than a drop: none yet");
            Assert.AreEqual(1, drip.Take(0.006f, 100f), "the rest carried over makes one");
        }

        [Test]
        public void ADripStartsAfreshOnceReset()
        {
            var drip = new Drip();
            drip.Take(0.009f, 100f);
            drip.Reset();
            Assert.AreEqual(0, drip.Take(0.002f, 100f), "nothing carried over from before");
        }

        [Test]
        public void KitMovesKeepTheirNumbersOnTheWire()
        {
            // Phones on the same build talk in these numbers: never change or reuse one.
            Assert.AreEqual(0, (int)KitMove.None);
            Assert.AreEqual(1, (int)KitMove.Weasel);
            Assert.AreEqual(2, (int)KitMove.Trollogram);
            Assert.AreEqual(3, (int)KitMove.Mud);
        }

        [Test]
        public void AKitMoveCostsWhatAShotDoes()
        {
            Assert.AreEqual(WireMeter.Bytes(WireKind.Shot), WireMeter.Bytes(WireKind.Kit), "a byte for the move, where, and its data");
        }
    }
}
