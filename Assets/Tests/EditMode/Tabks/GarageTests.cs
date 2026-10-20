using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The bay's tabks: who's won which, which is loaded, and the drone's swaps and shuffles.</summary>
    public class GarageTests
    {
        static readonly DroneShow.Point Home = DroneShow.Home;

        static Profile BeatenTwo()
        {
            var p = new Profile();
            p.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
            p.RecordRun(Missions.Two, Setting.Normal, 100, won: true, solo: false);
            return p;
        }

        [Test]
        public void TheCards_SayWhoseTabkItIs_AndItsGuns()
        {
            CollectionAssert.AreEqual(new[] { "WATER", "MUD", "SPEW" }, Names(TabkCards.Tones2));
            CollectionAssert.AreEqual(new[] { "WEASEL", "TROLLOGRAM", "STEAL" }, Names(TabkCards.Kie));
            CollectionAssert.AreEqual(new[] { "CONES", "FIXATE", "PILLS" }, Names(TabkCards.Kcaj));
            foreach (TabkCard c in TabkCards.All)
            {
                CollectionAssert.AreEqual(new[] { 5, 3, 1 }, new[] { c.Guns[0].rounds, c.Guns[1].rounds, c.Guns[2].rounds }, c.Name);
                StringAssert.EndsWith("'S TABK", c.Name);
                Assert.AreSame(c, TabkCards.ById(c.Id));
                Assert.AreSame(c.Owner, Crew.Of(c.Owner).Soldier, "a crewmate's");
            }
            Assert.AreEqual(BayDeck.Floor, BayMap.DeckOf(TabkCards.Kie.Home), "kie's parks downstairs");
            Assert.AreEqual(BayDeck.Top, BayMap.DeckOf(TabkCards.Kcaj.Home), "kcaj's on the top deck");
            Assert.IsNull(TabkCards.ById(Content.Trooper.Id));
        }

        [Test]
        public void TheHud_NamesTheTabkOverItsRounds()
        {
            Assert.AreEqual("KIE'S TABK  WEASEL 5   TROLLOGRAM 3   STEAL 1", TabkCards.Kie.HudLine(5, 3, 1));
            Assert.AreEqual("TONES2'S TABK  WATER 2   MUD 0   SPEW 1", TabkCards.Tones2.HudLine(2, 0, 1));
            Assert.AreEqual("KIE'S TABK  RIFLE 12   TROLLOGRAM 3   STEAL 0", TabkCards.Kie.HudLine(5, 3, 0, "RIFLE", 12), "stolen loot is what FIRE shoots");
            Assert.AreEqual("KIE'S TABK  WEASEL 5   TROLLOGRAM 3   STEAL 0", TabkCards.Kie.HudLine(5, 3, 0, "RIFLE", 0), "once it's spent, the weasels again");
        }

        static string[] Names(TabkCard c) => new[] { c.Guns[0].name, c.Guns[1].name, c.Guns[2].name };

        [Test]
        public void TonesTabkIsWonFirst_TheOthersOnceMissionTwoIsBeaten()
        {
            var fresh = new Profile();
            fresh.RecordRun(Missions.One, Setting.Normal, 100, won: true, solo: true);
            CollectionAssert.AreEqual(new[] { TabkCards.Tones2 }, TabkCards.Won(fresh));
            CollectionAssert.AreEqual(TabkCards.All, TabkCards.Won(BeatenTwo()));
            CollectionAssert.AreEqual(TabkCards.All, TabkCards.Won(new Profile(), everything: true));
        }

        [Test]
        public void TheLoadedTabk_IsThePick_IfItsBeenWon()
        {
            IReadOnlyList<TabkCard> one = new[] { TabkCards.Tones2 };
            Assert.AreSame(TabkCards.Tones2, TabkCards.Loaded(0, TabkCards.All), "never picked");
            Assert.AreSame(TabkCards.Kie, TabkCards.Loaded(Content.Kie.Id, TabkCards.All));
            Assert.AreSame(TabkCards.Tones2, TabkCards.Loaded(Content.Kie.Id, one), "not won (yet)");
        }

        [Test]
        public void TheBayStarts_WithTheLoadedTabkOnTheStands_AndTheRestAtHome()
        {
            var g = new Garage(TabkCards.All, TabkCards.Kie);
            Assert.AreSame(TabkCards.Kie, g.Loaded);
            Assert.AreEqual(BayMap.Stands, g.SpotOf(TabkCards.Kie));
            Assert.AreEqual(TabkCards.Tones2.Home, g.SpotOf(TabkCards.Tones2));
            Assert.AreEqual(TabkCards.Kcaj.Home, g.SpotOf(TabkCards.Kcaj));
            Assert.IsNull(g.At(TabkCards.Kie.Home), "its space is empty");
            Assert.IsTrue(g.Holds(TabkCards.All));
            Assert.IsFalse(g.Holds(new[] { TabkCards.Tones2 }));
        }

        [Test]
        public void ASwap_PutsTheOldPickAway_AndTheNewOneOnTheStands()
        {
            var g = new Garage(TabkCards.All, TabkCards.Tones2);
            DroneShow show = g.Swap(TabkCards.Kie, Home);
            Assert.IsNotNull(show);
            Assert.AreSame(TabkCards.Kie, g.Loaded);
            Assert.AreEqual(TabkCards.Tones2.Home, g.SpotOf(TabkCards.Tones2));
            Assert.AreEqual(BayMap.Stands, show.EndSpot(TabkCards.Kie));
            Assert.AreEqual(TabkCards.Tones2.Home, show.EndSpot(TabkCards.Tones2));
            Assert.IsNull(show.EndSpot(TabkCards.Kcaj), "it isn't touched");
            Assert.IsNull(g.Swap(TabkCards.Kie, Home), "already loaded");
        }

        [Test]
        public void ASwap_TakesAboutTenSeconds_AndTheHoseComesOutAndGoesBackIn()
        {
            foreach (TabkCard pick in new[] { TabkCards.Kie, TabkCards.Kcaj })
            {
                var g = new Garage(TabkCards.All, TabkCards.Tones2);
                DroneShow show = g.Swap(pick, Home);
                Assert.That(show.Length, Is.InRange(6f, 13f), pick.Name);
                Assert.That(show.Total, Is.GreaterThanOrEqualTo(show.Length), "then home");
                Assert.AreEqual(1f, show.Hose, "in, to start");
                bool hoseOut = false;
                TabkCard firstCarried = null, lastCarried = null;
                while (!show.Done)
                {
                    show.Tick(1f / 30f);
                    if (show.Hose == 0f) hoseOut = true;
                    TabkCard c = show.Carried;
                    if (c != null)
                    {
                        firstCarried ??= c;
                        lastCarried = c;
                        Assert.AreEqual(0f, show.Hose, "never carried with the hose in");
                        Assert.IsTrue(show.Where(c, out DroneShow.Point feet));
                        Assert.AreEqual(show.Drone.X, feet.X, 0.01f, "under the drone");
                        Assert.That(feet.Y, Is.LessThan(show.Drone.Y));
                    }
                }
                Assert.IsTrue(hoseOut);
                Assert.AreEqual(1f, show.Hose, "back in");
                Assert.AreSame(TabkCards.Tones2, firstCarried, "the old one off first");
                Assert.AreSame(pick, lastCarried, "then the new one on");
                Assert.IsTrue(show.Where(pick, out DroneShow.Point at));
                Assert.AreEqual(BayMap.StandX, at.X, 0.01f);
                Assert.AreEqual(BayMap.StandY, at.Y, 0.01f);
                Assert.AreEqual(DroneShow.Home.X, show.Drone.X, 0.01f, "the drone's home");
            }
        }

        [Test]
        public void ASwap_ChangesLevel_OnlyPastTheTopDecksEnd()
        {
            var g = new Garage(TabkCards.All, TabkCards.Tones2);
            DroneShow show = g.Swap(TabkCards.Kcaj, Home);
            float lastY = show.Drone.Y, lastX = show.Drone.X;
            while (!show.Done)
            {
                show.Tick(1f / 60f);
                DroneShow.Point d = show.Drone;
                bool climbing = System.Math.Abs(d.Y - lastY) > 2f && show.Carried != null;
                if (climbing && System.Math.Abs(d.X - lastX) < 0.01f && d.Y > 70f && lastY < 70f)
                    Assert.That(d.X, Is.LessThanOrEqualTo(DroneShow.EdgeX + 0.01f), "up past the deck's end, never through it");
                lastY = d.Y;
                lastX = d.X;
            }
        }

        [Test]
        public void SkippingIt_GoesStraightToTheEnd()
        {
            var g = new Garage(TabkCards.All, TabkCards.Tones2);
            DroneShow show = g.Swap(TabkCards.Kie, Home);
            show.Tick(1f);
            show.Skip();
            Assert.IsTrue(show.Done && show.Landed);
            Assert.AreEqual(1f, show.Hose);
            Assert.IsNull(show.Carried);
        }

        [Test]
        public void TheIdle_MovesAParkedTabkToAFreeSpaceOnItsDeck()
        {
            var g = new Garage(TabkCards.All, TabkCards.Tones2);
            var rng = new System.Random(3);
            for (int i = 0; i < 40; i++)
            {
                var spots = new Dictionary<TabkCard, int>();
                foreach (TabkCard c in TabkCards.All) spots[c] = g.SpotOf(c);
                DroneShow show = g.Shuffle(rng.Next, DroneShow.Home);
                Assert.IsNotNull(show);
                Assert.AreSame(TabkCards.Tones2, g.Loaded, "the stands are left alone");
                Assert.AreEqual(1f, show.Hose, "and the hose");
                int moved = 0;
                foreach (TabkCard c in TabkCards.All)
                {
                    if (g.SpotOf(c) == spots[c]) continue;
                    moved++;
                    Assert.AreEqual(BayMap.DeckOf(spots[c]), BayMap.DeckOf(g.SpotOf(c)), "on its deck");
                }
                Assert.AreEqual(1, moved);
                var taken = new HashSet<int>();
                foreach (TabkCard c in TabkCards.All) Assert.IsTrue(taken.Add(g.SpotOf(c)), "one tabk a space");
            }
            Assert.IsNull(new Garage(new[] { TabkCards.Tones2 }, TabkCards.Tones2).Shuffle(rng.Next, Home), "nothing parked");
        }

        [Test]
        public void AfterShuffles_ASwapStillParksTheOldPickInAFreeSpace()
        {
            var g = new Garage(TabkCards.All, TabkCards.Tones2);
            g.Swap(TabkCards.Kie, Home); // tones2 home, kie on the stands
            // kcaj's moved into kie's old space would be fine; move tones2 out of his and put kie back
            var rng = new System.Random(1);
            for (int i = 0; i < 10; i++) g.Shuffle(rng.Next, Home);
            g.Swap(TabkCards.Tones2, Home);
            var taken = new HashSet<int>();
            foreach (TabkCard c in TabkCards.All) Assert.IsTrue(taken.Add(g.SpotOf(c)), "one tabk a space");
            Assert.AreSame(TabkCards.Tones2, g.Loaded);
        }
    }
}
