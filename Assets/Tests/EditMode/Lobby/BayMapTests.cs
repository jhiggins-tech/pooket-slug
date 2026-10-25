using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The tabk bay's map and walking round it: along the floor, up to the top deck on the lift, and looking a tabk over.</summary>
    public class BayMapTests
    {
        static BayErrand Run(BayWalker w, float seconds)
        {
            BayErrand done = BayErrand.None;
            for (float t = 0f; t < seconds; t += 1f / 60f)
            {
                BayErrand e = w.Step(1f / 60f);
                if (e.Any) done = e;
            }
            return done;
        }

        [Test]
        public void TheSpots_AreTheStandsAndTwoDecksOfSix()
        {
            Assert.AreEqual(BayMap.StandX, BayMap.SpotX(BayMap.Stands));
            Assert.AreEqual(BayDeck.Floor, BayMap.DeckOf(BayMap.Stands));
            for (int s = 0; s < BayMap.Spaces; s++)
            {
                Assert.AreEqual(s < 6 ? BayDeck.Floor : BayDeck.Top, BayMap.DeckOf(s));
                Assert.AreEqual(BayMap.SpaceX[s % 6], BayMap.SpotX(s));
                Assert.AreEqual(s, BayMap.SpotAt(BayMap.SpotX(s), BayMap.SpotY(s) + 10f), "a tap on its tabk");
                if (s >= 6) Assert.That(BayMap.SpotX(s) - BayMap.TabkW / 2f, Is.GreaterThan(BayMap.DeckLeft), "on the deck");
            }
            Assert.AreEqual(BayMap.Stands, BayMap.SpotAt(BayMap.StandX, 30f));
            Assert.IsNull(BayMap.SpotAt(500f, 150f), "the ceiling");
            // the spaces don't overlap, and the stands are clear of the den's door
            for (int i = 1; i < 6; i++) Assert.That(BayMap.SpaceX[i] - BayMap.SpaceX[i - 1], Is.GreaterThanOrEqualTo(BayMap.TabkW - 4f));
            Assert.That(BayMap.StandX - BayMap.TabkW / 2f, Is.GreaterThan(BayMap.XOf(BayPlace.Door) + 10f));
        }

        [Test]
        public void ATapPicksTheDeck_OnlyWhereTheTopDeckIs()
        {
            Assert.AreEqual(BayDeck.Top, BayMap.DeckAt(400f, 90f));
            Assert.AreEqual(BayDeck.Floor, BayMap.DeckAt(400f, 30f));
            Assert.AreEqual(BayDeck.Floor, BayMap.DeckAt(92f, 90f), "over the stands there's no deck");
            Assert.AreEqual(BayDeck.Floor, BayMap.DeckAt(870f, 90f), "nor over the wash");
        }

        [Test]
        public void ThePlaces_AndWhatTheySay()
        {
            Assert.AreEqual(BayPlace.Door, BayMap.PlaceAt(8f, 40f));
            Assert.AreEqual(BayPlace.Lift, BayMap.PlaceAt(BayMap.LiftX, 40f));
            Assert.AreEqual(BayPlace.Wash, BayMap.PlaceAt(868f, 40f));
            Assert.AreEqual(BayPlace.Street, BayMap.PlaceAt(970f, 40f));
            Assert.IsNull(BayMap.Note(BayPlace.Door));
            Assert.IsNull(BayMap.Note(BayPlace.Lift));
            StringAssert.Contains("WASH", BayMap.Note(BayPlace.Wash));
            StringAssert.Contains("SHUT", BayMap.Note(BayPlace.Street));
            foreach (BayPlace p in new[] { BayPlace.Wash, BayPlace.Street })
            {
                StringAssert.DoesNotContain("TANK", BayMap.Note(p), "players read tabk");
                StringAssert.DoesNotContain("HOLOGRAM", BayMap.Note(p));
            }
        }

        [Test]
        public void WalkingAlongTheFloor_StopsWhereItWasSent()
        {
            var w = new BayWalker(BayDeck.Floor, 20f);
            w.WalkTo(BayDeck.Floor, 300f);
            Run(w, 5f);
            Assert.AreEqual(300f, w.X, 0.01f);
            Assert.IsTrue(w.Standing);
            w.WalkTo(BayDeck.Floor, 2000f);
            Run(w, 15f);
            Assert.AreEqual(BayMap.MaxX(BayDeck.Floor), w.X, 0.01f, "short of the street door");
        }

        [Test]
        public void TheLift_CarriesYouUpToTheTopDeck_AndBackDown()
        {
            var w = new BayWalker(BayDeck.Floor, 300f);
            w.Visit(BayMap.PerDeck + 1);
            bool rode = false;
            BayErrand done = BayErrand.None;
            for (float t = 0f; t < 20f; t += 1f / 60f)
            {
                BayErrand e = w.Step(1f / 60f);
                if (e.Any) done = e;
                if (w.Riding)
                {
                    rode = true;
                    Assert.AreEqual(BayMap.LiftX, w.X, 0.01f, "on the lift");
                    Assert.AreEqual(w.Y, w.LiftY, 0.01f, "on its platform");
                }
            }
            Assert.IsTrue(rode, "took the lift");
            Assert.AreEqual(BayDeck.Top, w.Deck);
            Assert.AreEqual(BayMap.StandBy(BayMap.PerDeck + 1), w.X, 0.01f);
            Assert.AreEqual(BayMap.PerDeck + 1, done.Spot, "and looked the tabk over");
            Assert.AreEqual(BayMap.FloorY(BayDeck.Top), w.LiftY, 0.01f, "the lift stays up");

            w.Go(BayPlace.Lift);
            Run(w, 10f);
            Assert.AreEqual(BayDeck.Floor, w.Deck);
            Assert.AreEqual(BayMap.FloorY(BayDeck.Floor), w.Y, 0.01f);
        }

        [Test]
        public void TheLift_ComesToYouFirst_WhenItsAtTheOtherLevel()
        {
            var w = new BayWalker(BayDeck.Top, BayMap.LiftX - 12f);
            Assert.AreEqual(BayMap.FloorY(BayDeck.Floor), w.LiftY, "it starts down");
            w.Ride();
            Run(w, 0.5f);
            Assert.IsFalse(w.Riding, "waiting for it");
            Run(w, 3f);
            Assert.IsTrue(w.Riding || w.Deck == BayDeck.Floor, "on it");
            Run(w, 5f);
            Assert.AreEqual(BayDeck.Floor, w.Deck);
        }

        [Test]
        public void WalkingByHand_StaysOnTheDeck()
        {
            var w = new BayWalker(BayDeck.Top, 400f);
            w.Walk(-1);
            Run(w, 10f);
            Assert.AreEqual(BayMap.MinX(BayDeck.Top), w.X, 0.01f);
            Assert.AreEqual(BayDeck.Top, w.Deck, "doesn't fall off the end");
            Assert.IsTrue(w.FaceLeft);
        }

        [Test]
        public void WalkingIntoTheDensDoor_GoesThroughIt()
        {
            var w = new BayWalker(BayDeck.Floor, BayMap.XOf(BayPlace.Door) + 14f);
            w.Walk(-1);
            BayErrand done = Run(w, 1f);
            Assert.AreEqual(BayPlace.Door, done.Place, "as if you'd tapped it");

            w = new BayWalker(BayDeck.Floor, 300f);
            w.Walk(-1);
            for (int f = 0; f < 30; f++) { w.Walk(-1); w.Step(1f / 60f); }
            w.Walk(0);
            Assert.IsFalse(Run(w, 5f).Any, "let go before the door: you stay");

            w = new BayWalker(BayDeck.Floor, 300f);
            w.Walk(1);
            Assert.IsFalse(Run(w, 15f).Any, "the other way: the street door stays shut");
            w = new BayWalker(BayDeck.Top, 400f);
            w.Walk(-1);
            Assert.IsFalse(Run(w, 10f).Any, "the top deck's end is no door");
        }

        [Test]
        public void JumpUsesTheDoor_OnlyRightByIt()
        {
            Assert.AreEqual(BayPlace.Door, new BayWalker(BayDeck.Floor, BayMap.XOf(BayPlace.Door) + 14f).PlaceHere, "where you come in");
            Assert.AreEqual(BayPlace.None, new BayWalker(BayDeck.Floor, BayMap.StandBy(BayMap.Stands)).PlaceHere, "by the stands");
            Assert.AreEqual(BayPlace.None, new BayWalker(BayDeck.Top, BayMap.DeckLeft).PlaceHere);
        }

        [Test]
        public void KiesReelsBlinkOnTheirOldBeats_ThroughFx()
        {
            // BayHomecomingView's reels used (int)(clock * rate) % 2 == 0, the rate rising with Reels
            for (float reels = 0.05f; reels <= 1f; reels += 0.05f)
            {
                float rate = 6f + 18f * reels;
                for (float t = 0f; t < 20f; t += 1f / 60f)
                    Assert.AreEqual((int)(t * rate) % 2 == 0, Fx.Blink(t, rate), "reels " + reels + " at " + t);
            }
        }

        [Test]
        public void TheTabkSizes_AreTheSameNumbersAsBefore()
        {
            Assert.AreEqual(1.5f, BayMap.Scale);
            Assert.AreEqual(78f, BayMap.TabkW);
            Assert.AreEqual(33f, BayMap.TabkH);
            Assert.AreEqual(42f, BayMap.TallTabkH);
        }

        // kie's and kcaj's tabks are 28 rows to tones2's 22, so a tap box of tones2's height missed their top 9 px
        [Test]
        public void EachTabk_HasItsOwnHeightInTheBay()
        {
            Assert.AreEqual(22f * 1.5f, BayMap.TabkHOf(TabkCards.Tones2.Id), 0.01f, "tones2's: 22 rows, 1.5 times");
            Assert.AreEqual(28f * 1.5f, BayMap.TabkHOf(TabkCards.Kie.Id), 0.01f, "kie's projector: 28 rows");
            Assert.AreEqual(28f * 1.5f, BayMap.TabkHOf(TabkCards.Kcaj.Id), 0.01f, "kcaj's soft-serve machine: 28 rows");
        }

        [Test]
        public void ATapOnATallTabksTop_FindsIt()
        {
            int spot = TabkCards.Kie.Home;
            float top = BayMap.SpotY(spot) + BayMap.TabkHOf(TabkCards.Kie.Id) - 2f;
            Assert.AreEqual(spot, BayMap.SpotAt(BayMap.SpotX(spot), top, s => s == spot ? BayMap.TabkHOf(TabkCards.Kie.Id) : BayMap.TabkH),
                "kie's projector, near its top");
            Assert.IsNull(BayMap.SpotAt(BayMap.SpotX(spot), top), "tones2's height there: above his roof");
            foreach (TabkCard c in TabkCards.All)
                for (int s = BayMap.Stands; s < BayMap.PerDeck; s++)
                {
                    // the tallest tabk's box on the floor stays under the top deck's
                    float boxTop = BayMap.SpotY(s) + BayMap.TabkHOf(c.Id) + 4f;
                    Assert.That(boxTop, Is.LessThan(BayMap.FloorY(BayDeck.Top) - 4f), c.Name + " in spot " + s);
                }
        }
    }
}
