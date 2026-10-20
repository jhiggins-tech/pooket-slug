using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The debrief, XP and rank (docs/design/cutscenes/mission-one.md, The first debrief; docs/design/player.md).</summary>
    public class DebriefTests
    {
        [Test]
        public void TheLadderIsTabksThirtyThreeRanks()
        {
            Assert.AreEqual(33, Ranks.All.Length);
            var ids = new HashSet<string>();
            foreach (Rank r in Ranks.All)
            {
                Assert.IsTrue(ids.Add(r.Id), "ids are never reused");
                Assert.IsFalse(string.IsNullOrEmpty(r.Name));
            }
            int bronze = System.Array.FindIndex(Ranks.All, r => r.Name == "Bronze"), silver = System.Array.FindIndex(Ranks.All, r => r.Name == "Silver");
            Assert.Greater(bronze, 0, "Bronze isn't where you start");
            Assert.AreEqual(bronze + 1, silver);
        }

        [Test]
        public void EveryoneStartsOnTheLowestRankAndTheFirstClearIsAWholeRung()
        {
            Assert.AreSame(Ranks.All[0], Progression.RankAt(0));
            int climb = Progression.ClimbFor(true, firstClear: true);
            Assert.AreSame(Ranks.All[1], Progression.RankAt(climb), "the first debrief ends in a rank-up");
            Assert.Less(Progression.ClimbFor(true, firstClear: false), Progression.Rung, "a replay moves you a little way");
            Assert.AreEqual(0, Progression.ClimbFor(false, false));
            Assert.AreSame(Ranks.All[Ranks.All.Length - 1], Progression.RankAt(100000), "the top's the top");
        }

        [Test]
        public void XpComesFromTheClearTheFreedCrewmateAndTheKills()
        {
            List<XpPart> parts = Progression.XpFor(true, "kie", 23);
            Assert.AreEqual(3, parts.Count);
            int sum = 0;
            foreach (XpPart p in parts) sum += p.Amount;
            Assert.AreEqual(Progression.ClearXp + Progression.FreedXp + 23 * Progression.KillXp, sum);
            Assert.AreEqual("KIE FREED", parts[1].Label);
        }

        [Test]
        public void XpAndRankOnlyGoUpAndMergeByTheBetter()
        {
            var a = new Profile();
            a.Award(520, 4);
            a.Award(-50, -1);
            Assert.AreEqual(520, a.xp);
            Assert.AreEqual(4, a.rank);
            var b = new Profile();
            b.Award(700, 2);
            a.Merge(b);
            Assert.AreEqual(700, a.xp, "the same runs may be on both: the better, not the sum");
            Assert.AreEqual(4, a.rank);
        }

        static DebriefResult Result(bool signIn) => new DebriefResult
        {
            Score = 5000, Best = 5000, Kills = 23, Xp = Progression.XpFor(true, "kie", 23), ClimbBefore = 0, ClimbAfter = Progression.Rung,
            First = true, OfferSignIn = signIn,
        };

        [Test]
        public void ItsPartsGoTallyXpRankSignInContinue()
        {
            var p = new DebriefPlayer(Result(true));
            CollectionAssert.AreEqual(new[] { DebriefPlayer.Part.Tally, DebriefPlayer.Part.Xp, DebriefPlayer.Part.Rank, DebriefPlayer.Part.SignIn, DebriefPlayer.Part.Continue }, p.Parts);
            Assert.IsTrue(p.Result.RankedUp);
            CollectionAssert.DoesNotContain(new DebriefPlayer(Result(false)).Parts, DebriefPlayer.Part.SignIn, "signed in: no offer");
        }

        [Test]
        public void ATapFillsThePartThenAnotherMovesOn()
        {
            var p = new DebriefPlayer(Result(false));
            p.Tick(0.1f);
            p.Tap();
            Assert.AreEqual(DebriefPlayer.Part.Tally, p.Now, "too soon: ignored");
            p.Tick(0.3f);
            p.Tap();
            Assert.IsTrue(p.Counted, "filled at once");
            Assert.AreEqual(DebriefPlayer.Part.Tally, p.Now);
            p.Tap();
            Assert.AreEqual(DebriefPlayer.Part.Xp, p.Now);
            p.Tick(DebriefPlayer.XpSeconds);
            Assert.IsTrue(p.Counted, "or it counts up by itself");
            p.Tick(0.3f);
            p.Tap();
            Assert.AreEqual(DebriefPlayer.Part.Rank, p.Now);
            p.Tick(5f);
            p.Tap();
            Assert.AreEqual(DebriefPlayer.Part.Continue, p.Now);
            Assert.IsFalse(p.Tap(), "at CONTINUE a tap means leave");
        }

        [Test]
        public void ALooksTokenEverySixHundredXp()
        {
            Assert.AreEqual(600, Progression.LooksTokenXp);
            Assert.AreEqual(0f, Progression.TokenProgress(0));
            Assert.AreEqual(0.5f, Progression.TokenProgress(300));
            Assert.AreEqual(0f, Progression.TokenProgress(1200), "a token earned: the bar starts again");
            Assert.AreEqual(0.25f, Progression.TokenProgress(750));
        }

        static void To(DebriefPlayer p, DebriefPlayer.Part part)
        {
            while (p.Now != part)
            {
                p.Tick(10f);
                p.Tap();
                p.Tap();
            }
        }

        [Test]
        public void KieExplainsTheXpTheFirstTimeAndAsksForTheSignIn()
        {
            var first = new DebriefPlayer(Result(true));
            Assert.IsNull(first.KieSays, "the tally");
            To(first, DebriefPlayer.Part.Xp);
            Assert.AreEqual("YOU GET XP FOR THAT.", first.KieSays);
            To(first, DebriefPlayer.Part.Rank);
            Assert.IsNull(first.KieSays);
            To(first, DebriefPlayer.Part.SignIn);
            Assert.AreEqual("SIGN IN SO THIS DOESN'T VANISH.", first.KieSays);

            DebriefResult later = Result(false);
            later.First = false;
            var p = new DebriefPlayer(later);
            To(p, DebriefPlayer.Part.Xp);
            Assert.IsNull(p.KieSays, "after the first debrief kie's quiet");
        }

        [Test]
        public void TheSignInButtonShowsInItsPartAndStaysAtContinue()
        {
            var p = new DebriefPlayer(Result(true));
            Assert.IsFalse(p.ShowsSignIn);
            To(p, DebriefPlayer.Part.SignIn);
            Assert.IsTrue(p.ShowsSignIn);
            To(p, DebriefPlayer.Part.Continue);
            Assert.IsTrue(p.ShowsSignIn, "still on offer at CONTINUE");

            var signedIn = new DebriefPlayer(Result(false));
            To(signedIn, DebriefPlayer.Part.Continue);
            Assert.IsFalse(signedIn.ShowsSignIn, "signed in: never");
        }
    }
}
