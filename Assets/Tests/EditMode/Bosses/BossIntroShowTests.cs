using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class BossIntroShowTests
    {
        static BossIntroShow.Cue[] All(bool full, float step)
        {
            var list = new System.Collections.Generic.List<BossIntroShow.Cue>();
            var due = new BossIntroShow.Cue[4];
            for (float t = -step; t < 4f; t += step)
            {
                int n = BossIntroShow.Cues(full, t, t + step, due);
                for (int i = 0; i < n; i++) list.Add(due[i]);
            }
            return list.ToArray();
        }

        [TestCase(1f / 60f)]
        [TestCase(1f / 20f)]
        public void TheFullIntroSoundsTwoSirensAndBothLinesOnce(float step)
        {
            CollectionAssert.AreEqual(new[] { BossIntroShow.Cue.Siren, BossIntroShow.Cue.SayWarning, BossIntroShow.Cue.Siren, BossIntroShow.Cue.SayBoss }, All(true, step));
        }

        [Test]
        public void TheQuickIntroIsOneSirenAndTheWarning()
        {
            CollectionAssert.AreEqual(new[] { BossIntroShow.Cue.Siren, BossIntroShow.Cue.SayWarning }, All(false, 1f / 60f));
            var quick = new BossIntroShow(1f, false);
            Assert.IsTrue(quick.Warning);
            Assert.IsFalse(quick.NameCard, "no name card the second time");
            Assert.AreEqual(0f, quick.Edges);
            Assert.IsTrue(new BossIntroShow(BossIntroShow.QuickBanner, false).Over);
        }

        [Test]
        public void TheBannerFlashesBetweenStripesThatSweepInAndOut()
        {
            Assert.AreEqual(0.5f, new BossIntroShow(BossIntroShow.StripesIn / 2f, true).Stripes, 1e-4f);
            Assert.AreEqual(1f, new BossIntroShow(1.5f, true).Stripes);
            Assert.Less(new BossIntroShow(BossIntroShow.FullBanner - 0.1f, true).Stripes, 1f);
            Assert.AreEqual(0f, new BossIntroShow(BossIntroShow.FullBanner, true).Stripes);
            Assert.AreNotEqual(new BossIntroShow(0.05f, true).WarningLit, new BossIntroShow(0.25f, true).WarningLit);
        }

        [Test]
        public void TheNameCardFollowsTheWarning()
        {
            Assert.IsFalse(new BossIntroShow(1f, true).NameCard);
            Assert.IsTrue(new BossIntroShow(2f, true).NameCard);
            Assert.IsFalse(new BossIntroShow(2f, true).Over);
            Assert.IsTrue(new BossIntroShow(BossIntroShow.NameUntil, true).Over);
        }

        [Test]
        public void ThePilotsPartComesAfterTheNameCard_TheHatchThenTheBandThenTheLine()
        {
            var due = new BossIntroShow.Cue[6];
            var list = new System.Collections.Generic.List<BossIntroShow.Cue>();
            for (float t = -1f / 60f; t < BossIntroShow.PilotUntil; t += 1f / 60f)
            {
                int n = BossIntroShow.Cues(true, true, t, t + 1f / 60f, due);
                for (int i = 0; i < n; i++) list.Add(due[i]);
            }
            CollectionAssert.AreEqual(new[]
            {
                BossIntroShow.Cue.Siren, BossIntroShow.Cue.SayWarning, BossIntroShow.Cue.Siren, BossIntroShow.Cue.SayBoss,
                BossIntroShow.Cue.Hatch, BossIntroShow.Cue.Riser, BossIntroShow.Cue.Hit,
            }, list);

            Assert.IsFalse(new BossIntroShow(BossIntroShow.NameUntil, true, true).Over, "the pilot's part follows the name card");
            Assert.IsFalse(new BossIntroShow(2f, true, true).Holding, "the banner and name card play over the action");
            Assert.IsTrue(new BossIntroShow(BossIntroShow.PilotFrom + 0.1f, true, true).Holding, "the pilot's part holds it");
            Assert.AreEqual(0.5f, new BossIntroShow(BossIntroShow.PilotFrom + BossIntroShow.HatchFor / 2f, true, true).Hatch, 1e-4f);
            Assert.IsFalse(new BossIntroShow(BossIntroShow.BandAt - 0.01f, true, true).Band);
            Assert.IsTrue(new BossIntroShow(BossIntroShow.BandAt + 0.1f, true, true).Band);
            Assert.IsTrue(new BossIntroShow(BossIntroShow.LineAt + 0.1f, true, true).Line);
            Assert.IsTrue(new BossIntroShow(BossIntroShow.PilotUntil, true, true).Over);
            Assert.IsFalse(new BossIntroShow(BossIntroShow.PilotUntil, true, true).Holding, "the action carries on after it");
        }

        [Test]
        public void TheQuickIntroHasNoPilotsPart()
        {
            var quick = new BossIntroShow(BossIntroShow.PilotFrom + 0.1f, false, true);
            Assert.IsFalse(quick.Pilot);
            Assert.IsFalse(quick.Holding);
            Assert.AreEqual(0f, quick.Hatch);
            var due = new BossIntroShow.Cue[6];
            Assert.AreEqual(0, BossIntroShow.Cues(false, true, 1f, 10f, due));
        }
    }
}
