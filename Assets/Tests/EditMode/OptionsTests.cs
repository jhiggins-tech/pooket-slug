using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class OptionsTests
    {
        [Test]
        public void EverythingIsOnAtFirst()
        {
            Options o = Options.Default;
            Assert.IsTrue(o.Sound && o.Shake);
            Assert.AreEqual(0, o.Bits, "nothing stored reads as the defaults");
            Assert.AreEqual(Options.Default, Options.FromBits(0));
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void SettingsSurviveBeingStored(bool sound, bool shake)
        {
            var o = new Options { Sound = sound, Shake = shake };
            Assert.AreEqual(o, Options.FromBits(o.Bits));
        }

        [Test]
        public void ButtonsSayWhetherASettingIsOn()
        {
            Assert.AreEqual("SOUND  ON", Options.Label("SOUND", true));
            Assert.AreEqual("SCREEN SHAKE  OFF", Options.Label("SCREEN SHAKE", false));
        }
    }
}
