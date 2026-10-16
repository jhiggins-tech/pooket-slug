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
    }
}
