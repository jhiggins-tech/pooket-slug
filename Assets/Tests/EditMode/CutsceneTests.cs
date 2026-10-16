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

        static void RunTo(CutscenePlayer p, int shot, float t = 0f)
        {
            for (int i = 0; i < 6000 && !p.Done && (p.Shot < shot || p.Shot == shot && p.T < t); i++) p.Tick(1f / 60f);
        }

        [Test]
        public void TheOpeningIsSixUnhurriedShotsEachWithANarratorLine()
        {
            Assert.AreEqual(6, Cutscenes.Opening.Shots.Length);
            Assert.That(Cutscenes.Opening.Seconds, Is.InRange(30f, 36f));
            foreach (CutsceneShot shot in Cutscenes.Opening.Shots)
            {
                Assert.That(shot.Seconds, Is.InRange(4f, 8f), shot.Name);
                Assert.IsTrue(System.Array.Exists(shot.Lines, l => l.Who == Speaker.Narrator), shot.Name + " has narration");
            }
        }

        [Test]
        public void EveryLineHasTimeToBeRead()
        {
            // about three words a second, counted from when a line appears to when it goes
            foreach (CutsceneShot shot in Cutscenes.Opening.Shots)
                for (int i = 0; i < shot.Lines.Length; i++)
                {
                    CutsceneLine line = shot.Lines[i];
                    if (line.Who == Speaker.Caption) continue;
                    float until = shot.Seconds;
                    if (line.Who == Speaker.Narrator)
                        for (int j = i + 1; j < shot.Lines.Length; j++)
                            if (shot.Lines[j].Who == Speaker.Narrator) { until = shot.Lines[j].At; break; }
                    int words = line.Text.Split(' ').Length;
                    Assert.That(until - line.At, Is.GreaterThanOrEqualTo(words / 3f), line.Text);
                }
        }

        [Test]
        public void TheNarratorSpeaksInSubtitlesNotBubbles()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            var lines = new List<CutsceneLine>();
            Assert.IsNull(p.Narration);
            RunTo(p, 0, 1.35f);
            Assert.AreEqual("Friday night. tones2's back from the snack run.", p.Narration);
            Assert.AreEqual(0, p.Showing(lines), "no bubble for the narrator");
            while (p.Shot < 3) Run(p, 0.1f);
            Run(p, 5.5f);
            Assert.AreEqual("It glitches out, and drops its keycard.", p.Narration, "the latest line replaces the last");
            Assert.IsTrue(p.Narrating(out CutsceneLine line));
            Assert.AreEqual("opening-4b", line.Voice, "the recording the narrator voice spike plays");
        }

        [Test]
        public void EveryNarratorLineHasItsOwnRecording()
        {
            var ids = new HashSet<string>();
            foreach (CutsceneShot shot in Cutscenes.Opening.Shots)
                foreach (CutsceneLine line in shot.Lines)
                    if (line.Who == Speaker.Narrator)
                        Assert.IsTrue(ids.Add(line.Voice ?? ""), line.Text);
            Assert.AreEqual(7, ids.Count);
        }

        [Test]
        public void EachShotMovesOnByItselfAndEveryCueFiresOnce()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            var cues = new List<string>();
            Run(p, 40f, cues);
            Assert.IsTrue(p.Done);
            Assert.IsFalse(p.Skipped);
            int expected = 0;
            foreach (CutsceneShot shot in Cutscenes.Opening.Shots) expected += shot.Cues.Length + (shot.Intro != null ? 1 : 0);
            Assert.AreEqual(expected, cues.Count);
            Assert.AreEqual(1, cues.FindAll(c => c == CutscenePlayer.IntroCue).Count, "tones2's intro, once");
            Assert.AreEqual(1, cues.FindAll(c => c == "say:mission-start").Count);
        }

        [Test]
        public void ATapMovesToTheNextShotButNotTooSoon()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            RunTo(p, 1);
            Assert.IsFalse(p.Tap(0), "a tap the moment a shot starts is ignored");
            Run(p, CutscenePlayer.TapGuard + 0.05f);
            Assert.IsTrue(p.Tap(0));
            Assert.AreEqual(2, p.Shot);
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
            RunTo(p, 1, 0.5f);
            Assert.IsFalse(p.Tap(0));
            Assert.IsTrue(p.Waiting, "one has tapped, the other hasn't");
            Assert.IsTrue(p.HasTapped(0));
            Assert.AreEqual(1, p.Shot);
            Assert.IsTrue(p.Tap(1));
            Assert.AreEqual(2, p.Shot);
            Assert.IsFalse(p.Waiting);
            Assert.IsFalse(p.HasTapped(0), "taps are per shot");
        }

        [Test]
        public void InCoopTheTimerStillMovesItOnForBoth()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false, players: 2);
            RunTo(p, 1, 0.5f);
            p.Tap(0);
            Run(p, Cutscenes.Opening.Shots[1].Seconds);
            Assert.AreEqual(2, p.Shot);
            Assert.IsFalse(p.Waiting);
        }

        [Test]
        public void BubblesAppearOnTheirCueAndLastTheShot()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            var lines = new List<CutsceneLine>();
            Assert.AreEqual(0, p.Showing(lines));
            Run(p, 1f);
            Assert.AreEqual(0, p.Showing(lines));
            RunTo(p, 0, 3.3f);
            Assert.AreEqual(1, p.Showing(lines));
            Assert.AreEqual("SNACKS ARE HERE LADS", lines[0].Text);
            while (p.Shot < 4) Run(p, 0.1f);
            Run(p, 3f);
            Assert.AreEqual(1, p.Showing(lines), "tones2 says where the card sends him");
            Assert.AreEqual(Speaker.Tones2, lines[0].Who);
        }
    
        [Test]
        public void Tones2GetsHisIntroTheFirstTimeHesOnScreen()
        {
            CutsceneIntro intro = Cutscenes.Opening.Shots[0].Intro;
            Assert.IsNotNull(intro, "the opening's first shot is tones2's first time on screen");
            Assert.AreEqual(Speaker.Tones2, intro.Who);
            Assert.AreEqual("TONES2", intro.Name);
            var who = new HashSet<Speaker>();
            foreach (CutsceneShot shot in Cutscenes.Opening.Shots)
                if (shot.Intro != null) Assert.IsTrue(who.Add(shot.Intro.Who), "one intro per character");
        }

        [Test]
        public void AnIntroGoesSlowMoAndATapCantCutItShort()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            var cues = new List<string>();
            float at = Cutscenes.Opening.Shots[0].Intro.At;
            Run(p, at + 0.05f, cues);
            Assert.IsTrue(p.Introducing);
            Assert.Contains(CutscenePlayer.IntroCue, cues, "the musical hit");
            Assert.IsFalse(p.Tap(0), "no tapping past an intro");
            float t = p.T;
            Run(p, 1f);
            Assert.That(p.T - t, Is.EqualTo(CutscenePlayer.SlowMo).Within(0.02f), "a second of slow-mo moves the shot on 0.15 s");
            Assert.IsTrue(p.Introducing);
            Run(p, CutscenePlayer.IntroSeconds);
            Assert.IsFalse(p.Introducing, "and then it's over");
            Assert.AreEqual(0, p.Shot);
            t = p.T;
            Run(p, 0.5f);
            Assert.That(p.T - t, Is.EqualTo(0.5f).Within(0.02f), "back to full speed");
        }

        [Test]
        public void ATapBeforeAnIntroJumpsToItRatherThanPastIt()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            Run(p, 0.5f);
            Assert.IsTrue(p.Tap(0));
            Assert.AreEqual(0, p.Shot);
            Assert.AreEqual(Cutscenes.Opening.Shots[0].Intro.At, p.T);
            Run(p, 0.05f);
            Assert.IsTrue(p.Introducing);
        }
    }
}
