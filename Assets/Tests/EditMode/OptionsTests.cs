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

        [Test]
        public void TheCrewPickIsKeptWithTheSettings()
        {
            var o = Options.Default;
            Assert.AreSame(Content.Tones2, Crew.Pick(o.Crew, true), "never picked: tones2");
            o.Crew = Content.Kie.Id;
            Options back = Options.FromBits(o.Bits);
            Assert.AreEqual(Content.Kie.Id, back.Crew);
            Assert.IsTrue(back.Sound && back.Shake);
            Assert.AreSame(Content.Kie, Crew.Pick(back.Crew, true));
            Assert.AreSame(Content.Tones2, Crew.Pick(back.Crew, false), "the crew isn't open yet: tones2");
        }

        [Test]
        public void CrewCyclesThroughTheFreed()
        {
            Assert.AreSame(Content.Kie, Crew.Next(Content.Tones2.Id));
            Assert.AreSame(Content.Tones2, Crew.Next(Content.Kie.Id));
            Assert.AreSame(Content.Tones2, Crew.Pick(Content.Trooper.Id, true), "the trooper isn't one of the crew");
        }
    }
}
