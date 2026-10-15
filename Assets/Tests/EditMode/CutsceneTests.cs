using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class CutsceneTests
    {
        static void Run(CutscenePlayer p, float seconds, List<string> cues = null)
        {
            var due = new List<string>();
            for (float t = 0f; t < seconds && !p.Done; t += 1f / 60f)
            {
                p.Tick(1f / 60f, due);
                cues?.AddRange(due);
            }
        }

        [Test]
        public void TheOpeningIsFiveShotsOfAboutFifteenSeconds()
        {
            Assert.AreEqual(5, Cutscenes.Opening.Shots.Length);
            Assert.AreEqual(15f, Cutscenes.Opening.Seconds, 0.01f);
            foreach (CutsceneShot shot in Cutscenes.Opening.Shots)
                Assert.That(shot.Seconds, Is.InRange(2f, 4f), shot.Name);
        }

        [Test]
        public void EachShotMovesOnByItselfAndEveryCueFiresOnce()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            var cues = new List<string>();
            Run(p, 20f, cues);
            Assert.IsTrue(p.Done);
            Assert.IsFalse(p.Skipped);
            int expected = 0;
            foreach (CutsceneShot shot in Cutscenes.Opening.Shots) expected += shot.Cues.Length;
            Assert.AreEqual(expected, cues.Count);
            Assert.AreEqual(1, cues.FindAll(c => c == "say:mission-start").Count);
        }

        [Test]
        public void ATapMovesToTheNextShotButNotTooSoon()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            Assert.IsFalse(p.Tap(0), "a tap the moment a shot starts is ignored");
            Run(p, CutscenePlayer.TapGuard + 0.05f);
            Assert.IsTrue(p.Tap(0));
            Assert.AreEqual(1, p.Shot);
            Assert.AreEqual(0f, p.T);
            Assert.IsFalse(p.Tap(0), "mashing doesn't skip a second shot");
        }

        [Test]
        public void OnlyAScenesSeenBeforeCanBeSkipped()
        {
            var first = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            Assert.IsFalse(first.CanSkip);
            Assert.IsFalse(first.Skip());
            Assert.IsFalse(first.Done);

            var again = new CutscenePlayer(Cutscenes.Opening, seenBefore: true);
            Assert.IsTrue(again.Skip());
            Assert.IsTrue(again.Done);
            Assert.IsTrue(again.Skipped);
        }

        [Test]
        public void InCoopAShotWaitsUntilBothHaveTapped()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false, players: 2);
            Run(p, 0.5f);
            Assert.IsFalse(p.Tap(0));
            Assert.IsTrue(p.Waiting, "one has tapped, the other hasn't");
            Assert.IsTrue(p.HasTapped(0));
            Assert.AreEqual(0, p.Shot);
            Assert.IsTrue(p.Tap(1));
            Assert.AreEqual(1, p.Shot);
            Assert.IsFalse(p.Waiting);
            Assert.IsFalse(p.HasTapped(0), "taps are per shot");
        }

        [Test]
        public void InCoopTheTimerStillMovesItOnForBoth()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false, players: 2);
            Run(p, 0.5f);
            p.Tap(0);
            Run(p, Cutscenes.Opening.Shots[0].Seconds);
            Assert.AreEqual(1, p.Shot);
            Assert.IsFalse(p.Waiting);
        }

        [Test]
        public void BubblesAppearOnTheirCueAndLastTheShot()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            var lines = new List<CutsceneLine>();
            Assert.AreEqual(0, p.Showing(lines));
            Run(p, 1f);
            Assert.AreEqual(1, p.Showing(lines));
            Assert.AreEqual("SNACKS ARE HERE LADS", lines[0].Text);
            while (p.Shot < 3) Run(p, 0.1f);
            Run(p, 3f);
            Assert.AreEqual(2, p.Showing(lines), "the goon's cut-off line, then tones2's");
            Assert.AreEqual(Speaker.Goon, lines[0].Who);
        }
    }
}
