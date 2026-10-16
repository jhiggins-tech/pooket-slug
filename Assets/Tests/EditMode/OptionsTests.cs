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

        [Test]
        public void VolumesAndTheLeftHandedPad_StartAtTheirDefaults_InAnOldSave()
        {
            Options old = Options.FromBits(Options.FromBits(0).Bits | 2); // shake off, as an older build stored it
            Assert.AreEqual(Volume.High, old.Effects);
            Assert.AreEqual(Volume.High, old.Voices);
            Assert.IsFalse(old.LeftHanded);
            Assert.IsFalse(old.Shake);
        }

        [Test]
        public void VolumesAndTheLeftHandedPad_SurviveBeingStored()
        {
            foreach (Volume fx in new[] { Volume.High, Volume.Mid, Volume.Low, Volume.Off })
                foreach (Volume voices in new[] { Volume.High, Volume.Off })
                {
                    var o = new Options { Sound = true, Shake = false, Effects = fx, Voices = voices, LeftHanded = fx == Volume.Low, Crew = Content.Kie.Id };
                    Assert.AreEqual(o, Options.FromBits(o.Bits), fx + " / " + voices);
                }
        }

        [Test]
        public void AVolumeButton_StepsQuieterThenWrapsRound()
        {
            Assert.AreEqual(Volume.Mid, Options.Next(Volume.High));
            Assert.AreEqual(Volume.Low, Options.Next(Volume.Mid));
            Assert.AreEqual(Volume.Off, Options.Next(Volume.Low));
            Assert.AreEqual(Volume.High, Options.Next(Volume.Off));
            Assert.AreEqual("EFFECTS  MID", Options.Label("EFFECTS", Volume.Mid));
            Assert.AreEqual(1f, Options.Gain(Volume.High));
            Assert.AreEqual(0f, Options.Gain(Volume.Off));
            Assert.Greater(Options.Gain(Volume.Mid), Options.Gain(Volume.Low));
        }
    }
}
