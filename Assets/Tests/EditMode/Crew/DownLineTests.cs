using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    /// <summary>What the HUD says while a player's down (docs/design/player.md, Health and lives).</summary>
    public class DownLineTests
    {
        static LifeBank Bank(int spare = 2, int continues = 2) => new LifeBank(spare, continues, 9, 9f);

        [Test]
        public void PlayingThereIsNothingToSay()
        {
            DownLine line = DownLine.For(Bank(), false, 0f, coop: false, touch: false);
            Assert.IsNull(line.Down);
            Assert.IsNull(line.Note);
        }

        [Test]
        public void DownItCountsToTheRespawn()
        {
            LifeBank b = Bank();
            b.Fallen();
            float delay = Tuning.Lives.RespawnDelay;
            Assert.AreEqual("DOWN!  BACK IN " + Mathf.CeilToInt(delay), DownLine.For(b, true, 0f, false, false).Down);
            Assert.AreEqual("DOWN!  BACK IN 1", DownLine.For(b, true, delay - 0.5f, false, false).Down);
            Assert.AreEqual("DOWN!  BACK IN 1", DownLine.For(b, true, delay + 1f, false, false).Down, "never 0");
            Assert.IsNull(DownLine.For(b, true, 0f, false, false).Note, "a life left after this one");
        }

        [Test]
        public void TheLastLifeSaysSo()
        {
            LifeBank b = Bank(spare: 1);
            b.Fallen();
            Assert.AreEqual(0, b.Lives);
            Assert.AreEqual("NO LIVES LEFT AFTER THIS ONE", DownLine.For(b, true, 0f, false, false).Note);
            Assert.IsNull(DownLine.For(b, true, Tuning.Lives.RespawnDelay, false, false).Note, "gone once they're back");
        }

        [Test]
        public void TheContinueOnOfferCountsDownAndSaysHowToTakeIt()
        {
            LifeBank b = Bank(spare: 0, continues: 2);
            b.Fallen();
            b.Tick(0.5f);
            DownLine line = DownLine.For(b, true, 1f, false, touch: false);
            Assert.AreEqual("CONTINUE?  " + Mathf.CeilToInt(b.OfferLeft), line.Down);
            Assert.AreEqual("PRESS FIRE TO CONTINUE  ·  2 CONTINUES LEFT", line.Note);
            Assert.AreEqual("TAP FIRE TO CONTINUE  ·  2 CONTINUES LEFT", DownLine.For(b, true, 1f, false, touch: true).Note);
            Assert.AreEqual("PRESS FIRE TO CONTINUE  ·  1 CONTINUE LEFT", DownLine.For(Offering(1), true, 1f, false, false).Note);
        }

        static LifeBank Offering(int continues)
        {
            LifeBank b = Bank(spare: 0, continues: continues);
            b.Fallen();
            return b;
        }

        [Test]
        public void OutInCoopAPartnersOneUpBringsThemBack()
        {
            LifeBank b = Bank(spare: 0, continues: 0);
            b.Fallen();
            Assert.IsTrue(b.IsOut);
            DownLine coop = DownLine.For(b, true, 5f, coop: true, touch: false);
            Assert.AreEqual("OUT!", coop.Down);
            Assert.AreEqual("YOUR PARTNER'S NEXT 1UP BRINGS YOU BACK", coop.Note);
            Assert.IsNull(DownLine.For(b, true, 5f, coop: false, touch: false).Down, "solo, the game over screen says it");
        }
    }
}
