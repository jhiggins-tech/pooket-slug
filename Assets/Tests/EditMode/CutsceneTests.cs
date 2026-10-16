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
            Assert.That(Cutscenes.Opening.Seconds, Is.InRange(30f, 40f));
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
            RunTo(p, 0, 4.35f);
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
            Run(p, 45f, cues);
            Assert.IsTrue(p.Done);
            Assert.IsFalse(p.Skipped);
            int expected = 0;
            foreach (CutsceneShot shot in Cutscenes.Opening.Shots) expected += shot.Cues.Length + (shot.Intro != null ? 2 : 0);
            Assert.AreEqual(expected, cues.Count);
            Assert.AreEqual(1, cues.FindAll(c => c == CutscenePlayer.IntroCue).Count, "tones2's intro, once");
            Assert.Less(cues.IndexOf(CutscenePlayer.BuildUpCue), cues.IndexOf(CutscenePlayer.IntroCue), "its build-up first");
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
            RunTo(p, 0, 6.5f);
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
        public void AnIntroBuildsUpForAFewSecondsWhileTones2WalksOn()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            var cues = new List<string>();
            float at = Cutscenes.Opening.Shots[0].Intro.At;
            Assert.That(at - CutscenePlayer.BuildUp, Is.GreaterThanOrEqualTo(1f), "he's on screen a while before it starts");
            Assert.That(CutscenePlayer.BuildUp, Is.InRange(2f, 3f));
            RunTo(p, 0, at - CutscenePlayer.BuildUp - 0.1f);
            Assert.AreEqual(0f, p.BuildingUp);
            Run(p, 0.2f, cues);
            Assert.Contains(CutscenePlayer.BuildUpCue, cues, "the riser starts");
            RunTo(p, 0, at - 0.1f);
            Assert.That(p.BuildingUp, Is.GreaterThan(0.9f));
            Assert.IsFalse(p.Introducing);
            Run(p, 0.15f);
            Assert.IsTrue(p.Introducing, "and lands at the peak");
            Assert.AreEqual(0f, p.BuildingUp);
        }

        [Test]
        public void ATapBeforeAnIntroJumpsToItRatherThanPastIt()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            Run(p, 0.5f);
            Assert.IsTrue(p.Tap(0));
            Assert.AreEqual(0, p.Shot);
            float at = Cutscenes.Opening.Shots[0].Intro.At;
            Assert.AreEqual(at - CutscenePlayer.BuildUp, p.T, "to the start of its build-up, so that still plays");
            Assert.IsFalse(p.Tap(0), "and no tapping through the build-up");
            Run(p, CutscenePlayer.BuildUp + 0.05f);
            Assert.IsTrue(p.Introducing);
        }
    
        [Test]
        public void TheBeatOnlyGoesUpThroughTheScene()
        {
            var p = new CutscenePlayer(Cutscenes.Opening, seenBefore: false);
            int last = p.Beat, steps = 0;
            Assert.AreEqual(0, last, "before tones2's build-up");
            for (int i = 0; i < 60 * 45 && !p.Done; i++)
            {
                p.Tick(1f / 60f);
                Assert.GreaterOrEqual(p.Beat, last);
                if (p.Beat != last) steps++;
                last = p.Beat;
            }
            Assert.AreEqual(Cutscenes.Opening.Shots.Length * 3, p.Beat, "done");
            Assert.AreEqual(Cutscenes.Opening.Shots.Length + 2, steps, "every shot, and the opening's build-up and intro");
        }

        [Test]
        public void ACoopPartnersTapsTravelAsOneNumber()
        {
            int sent = CutsceneSync.Pack(7, Cutscenes.Opening.NetId, 4, false);
            Assert.AreNotEqual(0, sent);
            Assert.IsTrue(CutsceneSync.Read(sent, 7, Cutscenes.Opening.NetId, out int tapped, out bool skip));
            Assert.AreEqual(4, tapped);
            Assert.IsFalse(skip);
            Assert.IsTrue(CutsceneSync.Read(CutsceneSync.Pack(7, 1, -1, false), 7, 1, out tapped, out _));
            Assert.AreEqual(-1, tapped, "not tapped yet");
            Assert.IsTrue(CutsceneSync.Read(CutsceneSync.Pack(7, 1, 2, true), 7, 1, out _, out skip));
            Assert.IsTrue(skip);
            Assert.IsFalse(CutsceneSync.Read(sent, 8, Cutscenes.Opening.NetId, out _, out _), "last round's taps don't count");
            Assert.IsFalse(CutsceneSync.Read(sent, 7, 2, out _, out _), "nor another scene's");
            Assert.IsFalse(CutsceneSync.Read(0, 7, 1, out _, out _), "nor nothing");
        }

        [Test]
        public void TwoPhonesMoveOnTogetherWhenBothHaveTapped()
        {
            var a = new CutscenePlayer(Cutscenes.Opening, seenBefore: false, players: 2);
            var b = new CutscenePlayer(Cutscenes.Opening, seenBefore: false, players: 2);
            int aTapped = -1, bTapped = -1;
            RunTo(a, 1, 0.5f);
            RunTo(b, 1, 0.5f);

            int beat = a.Beat;
            Assert.IsFalse(a.Tap(0));
            aTapped = beat;
            CutsceneSync.Apply(b, true, CutsceneSync.Pack(3, 1, aTapped, false), 3);
            Assert.IsTrue(b.Waiting, "b's phone shows a has tapped");
            Assert.AreEqual(1, b.Shot);
            Assert.IsTrue(b.Tap(0), "and b's tap moves it on");
            bTapped = beat;
            Assert.AreEqual(2, b.Shot);
            CutsceneSync.Apply(a, true, CutsceneSync.Pack(3, 1, bTapped, false), 3);
            Assert.AreEqual(2, a.Shot, "a's phone follows");
            CutsceneSync.Apply(a, true, CutsceneSync.Pack(3, 1, bTapped, false), 3);
            Assert.AreEqual(2, a.Shot, "b's old tap doesn't count for the next shot");
            Assert.IsFalse(a.HasTapped(1));
        }

        [Test]
        public void ThePhoneThatEndsFirstLetsTheOtherFinish()
        {
            // b tapped the last shot first; a's tap ended it on a's phone in the same frame, so a's tap never went
            // out on its own. a's closing word ("tapped through it all") has to finish it on b's phone.
            var b = new CutscenePlayer(Cutscenes.Opening, seenBefore: false, players: 2);
            int last = Cutscenes.Opening.Shots.Length - 1;
            RunTo(b, last, 0.3f);
            b.Tap(0);
            Assert.IsFalse(b.Done, "b waits on a");
            CutsceneSync.Apply(b, true, CutsceneSync.Pack(3, 1, Cutscenes.Opening.Shots.Length * 3, false), 3);
            Assert.IsTrue(b.Done);
        }

        [Test]
        public void ATapBeforeTheIntroWaitsForThePartnerThenJumpsBothToTheBuildUp()
        {
            var a = new CutscenePlayer(Cutscenes.Opening, seenBefore: false, players: 2);
            Run(a, 0.5f);
            int beat = a.Beat;
            Assert.IsFalse(a.Tap(0));
            Assert.IsTrue(CutsceneSync.Apply(a, true, CutsceneSync.Pack(1, 1, beat, false), 1) == false);
            Assert.AreEqual(Cutscenes.Opening.Shots[0].Intro.At - CutscenePlayer.BuildUp, a.T, "both tapped: on to the build-up");
            Assert.AreEqual(1, a.Beat);
            CutsceneSync.Apply(a, true, CutsceneSync.Pack(1, 1, beat, false), 1);
            Assert.IsFalse(a.HasTapped(1), "the tap before the build-up doesn't count in it");
        }

        [Test]
        public void ASkipNeedsBothAndALeavingPartnerHoldsNobodyUp()
        {
            var a = new CutscenePlayer(Cutscenes.Opening, seenBefore: true, players: 2);
            Assert.IsFalse(CutsceneSync.Apply(a, true, CutsceneSync.Pack(1, 1, -1, false), 1), "they haven't asked to skip");
            Assert.IsTrue(CutsceneSync.Apply(a, true, CutsceneSync.Pack(1, 1, -1, true), 1), "now they have");
            Run(a, 0.5f);
            a.Tap(0);
            Assert.AreEqual(0, a.Shot);
            Assert.IsTrue(CutsceneSync.Apply(a, false, 0, 1), "gone: their say doesn't hold up a skip");
            Assert.IsTrue(a.T >= Cutscenes.Opening.Shots[0].Intro.At - CutscenePlayer.BuildUp, "nor a tap");
        }
    }
}
