using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class LivesTests
    {
        static LifeBank Bank(int spare = 2, int continues = 2) => new LifeBank(spare, continues, 9, 9f);

        [Test]
        public void AFallSpendsASpareLife()
        {
            LifeBank b = Bank();
            Assert.AreEqual(LifeBank.Fall.Respawn, b.Fallen());
            Assert.AreEqual(1, b.Lives);
            Assert.AreEqual(LifeBank.Fall.Respawn, b.Fallen());
            Assert.AreEqual(0, b.Lives);
            Assert.IsFalse(b.OutOfLives);
        }

        [Test]
        public void OutOfLivesAContinueIsOffered()
        {
            LifeBank b = Bank(spare: 0);
            Assert.AreEqual(LifeBank.Fall.OfferContinue, b.Fallen());
            Assert.IsTrue(b.Offering && b.OutOfLives);
            Assert.IsFalse(b.IsOut);
            Assert.AreEqual(9f, b.OfferLeft);
        }

        [Test]
        public void TakingAContinueRefillsTheLives()
        {
            LifeBank b = Bank(spare: 2, continues: 2);
            b.Fallen(); b.Fallen(); b.Fallen();
            Assert.IsTrue(b.TakeContinue());
            Assert.AreEqual(2, b.Lives);
            Assert.AreEqual(1, b.Continues);
            Assert.IsFalse(b.OutOfLives);
            Assert.IsFalse(b.TakeContinue(), "only while offered");
        }

        [Test]
        public void TheOfferRunsOut()
        {
            LifeBank b = Bank(spare: 0);
            b.Fallen();
            Assert.IsFalse(b.Tick(8f));
            Assert.IsTrue(b.Tick(1.5f), "true on the frame it runs out");
            Assert.IsTrue(b.IsOut);
            Assert.IsFalse(b.Tick(1f));
            Assert.IsFalse(b.TakeContinue());
        }

        [Test]
        public void DecliningIsOut()
        {
            LifeBank b = Bank(spare: 0);
            b.Fallen();
            b.Decline();
            Assert.IsTrue(b.IsOut);
        }

        [Test]
        public void NoContinuesLeftIsOut()
        {
            LifeBank b = Bank(spare: 0, continues: 0);
            Assert.AreEqual(LifeBank.Fall.Out, b.Fallen());
            Assert.IsTrue(b.IsOut);
        }

        [Test]
        public void AOneUpAddsASpareLifeUpToTheCap()
        {
            LifeBank b = new LifeBank(8, 2, 9, 9f);
            Assert.IsFalse(b.AddLife());
            Assert.AreEqual(9, b.Lives);
            b.AddLife();
            Assert.AreEqual(9, b.Lives);
        }

        [Test]
        public void AOneUpBringsAnOutPlayerBack()
        {
            LifeBank b = Bank(spare: 0, continues: 0);
            b.Fallen();
            Assert.IsTrue(b.AddLife());
            Assert.IsFalse(b.OutOfLives);
            Assert.AreEqual(0, b.Lives, "back on this life, with no spare");
        }

        [Test]
        public void AOneUpDuringTheOfferSavesTheContinue()
        {
            LifeBank b = Bank(spare: 0, continues: 2);
            b.Fallen();
            Assert.IsTrue(b.AddLife());
            Assert.AreEqual(2, b.Continues);
            Assert.IsFalse(b.Offering);
        }

        [Test]
        public void TheStandardBankMatchesTuning()
        {
            LifeBank b = LifeBank.Standard();
            Assert.AreEqual(Tuning.Lives.Spare, b.Lives);
            Assert.AreEqual(Tuning.Lives.Continues, b.Continues);
        }
    }
}
