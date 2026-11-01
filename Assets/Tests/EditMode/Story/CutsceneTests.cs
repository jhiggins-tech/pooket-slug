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
        public void TheOpeningIsEightUnhurriedShotsEachWithANarratorLine()
        {
            Assert.AreEqual(8, TestScenes.Opening.Shots.Length);
            Assert.That(TestScenes.Opening.Seconds, Is.InRange(45f, 52f));
            foreach (CutsceneShot shot in TestScenes.Opening.Shots)
            {
                Assert.That(shot.Seconds, Is.InRange(4f, 9f), shot.Name);
                Assert.IsTrue(System.Array.Exists(shot.Lines, l => l.Who == Speaker.Narrator), shot.Name + " has narration");
            }
        }

        [Test]
        public void ItDipsToBlackOnlyIntoANewScene()
        {
            var a = new CutsceneShot("Outside", 3f, new CutsceneLine[0], set: "street");
            var b = new CutsceneShot("Still outside", 3f, new CutsceneLine[0], set: "street");
            var c = new CutsceneShot("Inside", 3f, new CutsceneLine[0], set: "den");
            var scene = new CutsceneDef("dips", 99, a, b, c);
            Assert.IsTrue(scene.NewScene(0), "into the first shot");
            Assert.IsFalse(scene.NewScene(1), "the same set: a cut, no dip between lines");
            Assert.IsTrue(scene.NewScene(2), "a new set");
            for (int i = 1; i < TestScenes.MotorPool.Shots.Length; i++)
                Assert.IsFalse(TestScenes.MotorPool.NewScene(i), "the motor pool's all one scene: shot " + i);
        }

        [Test]
        public void EveryLineHasTimeToBeRead()
        {
            // about three words a second, counted from when a line appears to when it goes (SceneCheck.Timing)
            foreach (SceneScript scene in TestScenes.All.Values)
                Assert.IsEmpty(SceneCheck.Timing(scene.Def), scene.Name + ": " + string.Join("; ", SceneCheck.Timing(scene.Def)));
        }

        [Test]
        public void TheNarratorSpeaksInSubtitlesNotBubbles()
        {
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false);
            var lines = new List<CutsceneLine>();
            Assert.IsNull(p.Narration);
            RunTo(p, 0, 4.35f);
            Assert.AreEqual("Friday night. tones2's back from the snack run.", p.Narration);
            Assert.AreEqual(0, p.Showing(lines), "no bubble for the narrator");
            while (p.Shot < 5) Run(p, 0.1f);
            Run(p, 5.9f);
            Assert.AreEqual("It glitches out, and drops its keycard.", p.Narration, "the latest line replaces the last");
            Assert.IsTrue(p.Narrating(out CutsceneLine line));
            Assert.AreEqual("opening-4b", line.Voice, "the recording the narrator voice spike plays");
        }

        [Test]
        public void EveryNarratorLineHasItsOwnRecording()
        {
            var ids = new HashSet<string>();
            foreach (CutsceneShot shot in TestScenes.Opening.Shots)
                foreach (CutsceneLine line in shot.Lines)
                    if (line.Who == Speaker.Narrator)
                        Assert.IsTrue(ids.Add(line.Voice ?? ""), line.Text);
            Assert.AreEqual(9, ids.Count);
        }

        [Test]
        public void EachShotMovesOnByItselfAndEveryCueFiresOnce()
        {
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false);
            var cues = new List<string>();
            Run(p, 55f, cues);
            Assert.IsTrue(p.Done);
            Assert.IsFalse(p.Skipped);
            int expected = 0;
            foreach (CutsceneShot shot in TestScenes.Opening.Shots) expected += shot.Cues.Length + (shot.Intro != null ? 2 : 0);
            Assert.AreEqual(expected, cues.Count);
            Assert.AreEqual(1, cues.FindAll(c => c == CutscenePlayer.IntroCue).Count, "tones2's intro, once");
            Assert.Less(cues.IndexOf(CutscenePlayer.BuildUpCue), cues.IndexOf(CutscenePlayer.IntroCue), "its build-up first");
            Assert.AreEqual(1, cues.FindAll(c => c == "say:mission-start").Count);
        }

        [Test]
        public void ATapMovesToTheNextShotButNotTooSoon()
        {
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false);
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
            var first = new CutscenePlayer(TestScenes.Opening, seenBefore: false);
            Assert.IsFalse(first.CanSkip);
            Assert.IsFalse(first.Skip());
            Assert.IsFalse(first.Done);

            var again = new CutscenePlayer(TestScenes.Opening, seenBefore: true);
            Assert.IsTrue(again.Skip());
            Assert.IsTrue(again.Done);
            Assert.IsTrue(again.Skipped);
        }

        [Test]
        public void InCoopAShotWaitsUntilBothHaveTapped()
        {
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false, players: 2);
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
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false, players: 2);
            RunTo(p, 1, 0.5f);
            p.Tap(0);
            Run(p, TestScenes.Opening.Shots[1].Seconds);
            Assert.AreEqual(2, p.Shot);
            Assert.IsFalse(p.Waiting);
        }

        [Test]
        public void BubblesAppearOnTheirCueAndLastTheShot()
        {
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false);
            var lines = new List<CutsceneLine>();
            Assert.AreEqual(0, p.Showing(lines));
            Run(p, 1f);
            Assert.AreEqual(0, p.Showing(lines));
            RunTo(p, 0, 6.5f);
            Assert.AreEqual(1, p.Showing(lines));
            Assert.AreEqual("SNACKS ARE HERE LADS", lines[0].Text);
            while (p.Shot < 6) Run(p, 0.1f);
            Run(p, 3f);
            Assert.AreEqual(0, p.Showing(lines), "the keycard's close-up plays on its own");
            Run(p, 1f);
            Assert.AreEqual(1, p.Showing(lines), "then, back in the den, tones2 says where the card sends him");
            Assert.AreEqual(Speaker.Tones2, lines[0].Who);
        }
    
        [Test]
        public void Tones2GetsHisIntroTheFirstTimeHesOnScreen()
        {
            CutsceneIntro intro = TestScenes.Opening.Shots[0].Intro;
            Assert.IsNotNull(intro, "the opening's first shot is tones2's first time on screen");
            Assert.AreEqual("tones2", intro.Figure);
            Assert.AreEqual("TONES2", intro.Name);
            var who = new HashSet<string>();
            foreach (CutsceneShot shot in TestScenes.Opening.Shots)
                if (shot.Intro != null) Assert.IsTrue(who.Add(shot.Intro.Figure), "one intro per character");
        }

        [Test]
        public void AnIntroGoesSlowMoAndATapCantCutItShort()
        {
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false);
            var cues = new List<string>();
            float at = TestScenes.Opening.Shots[0].Intro.At;
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
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false);
            var cues = new List<string>();
            float at = TestScenes.Opening.Shots[0].Intro.At;
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
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false);
            Run(p, 0.5f);
            Assert.IsTrue(p.Tap(0));
            Assert.AreEqual(0, p.Shot);
            float at = TestScenes.Opening.Shots[0].Intro.At;
            Assert.AreEqual(at - CutscenePlayer.BuildUp, p.T, "to the start of its build-up, so that still plays");
            Assert.IsFalse(p.Tap(0), "and no tapping through the build-up");
            Run(p, CutscenePlayer.BuildUp + 0.05f);
            Assert.IsTrue(p.Introducing);
        }
    
        [Test]
        public void TheBeatOnlyGoesUpThroughTheScene()
        {
            var p = new CutscenePlayer(TestScenes.Opening, seenBefore: false);
            int last = p.Beat, steps = 0;
            Assert.AreEqual(0, last, "before tones2's build-up");
            for (int i = 0; i < 60 * 55 && !p.Done; i++)
            {
                p.Tick(1f / 60f);
                Assert.GreaterOrEqual(p.Beat, last);
                if (p.Beat != last) steps++;
                last = p.Beat;
            }
            Assert.AreEqual(TestScenes.Opening.Shots.Length * 3, p.Beat, "done");
            Assert.AreEqual(TestScenes.Opening.Shots.Length + 2, steps, "every shot, and the opening's build-up and intro");
        }

        [Test]
        public void ACoopPartnersTapsTravelAsOneNumber()
        {
            int sent = CutsceneSync.Pack(7, TestScenes.Opening.NetId, 4, false);
            Assert.AreNotEqual(0, sent);
            Assert.IsTrue(CutsceneSync.Read(sent, 7, TestScenes.Opening.NetId, out int tapped, out bool skip));
            Assert.AreEqual(4, tapped);
            Assert.IsFalse(skip);
            Assert.IsTrue(CutsceneSync.Read(CutsceneSync.Pack(7, 1, -1, false), 7, 1, out tapped, out _));
            Assert.AreEqual(-1, tapped, "not tapped yet");
            Assert.IsTrue(CutsceneSync.Read(CutsceneSync.Pack(7, 1, 2, true), 7, 1, out _, out skip));
            Assert.IsTrue(skip);
            Assert.IsFalse(CutsceneSync.Read(sent, 8, TestScenes.Opening.NetId, out _, out _), "last round's taps don't count");
            Assert.IsFalse(CutsceneSync.Read(sent, 7, 2, out _, out _), "nor another scene's");
            Assert.IsFalse(CutsceneSync.Read(0, 7, 1, out _, out _), "nor nothing");
        }

        [Test]
        public void TwoPhonesMoveOnTogetherWhenBothHaveTapped()
        {
            var a = new CutscenePlayer(TestScenes.Opening, seenBefore: false, players: 2);
            var b = new CutscenePlayer(TestScenes.Opening, seenBefore: false, players: 2);
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
        public void TheMotorPoolRevealIsFourShotsWithTrolloKiesIntroInTheThird()
        {
            CutsceneDef m = TestScenes.MotorPool;
            Assert.AreEqual("scene-motor-pool", m.Id);
            Assert.AreNotEqual(TestScenes.Opening.NetId, m.NetId, "each scene has its own number for a partner's phone");
            Assert.AreEqual(4, m.Shots.Length);
            Assert.That(m.Seconds, Is.InRange(15f, 25f));
            for (int i = 0; i < m.Shots.Length; i++)
            {
                CutsceneShot shot = m.Shots[i];
                Assert.AreEqual(i == 2, shot.Intro != null, shot.Name);
                bool narrated = false;
                foreach (CutsceneLine line in shot.Lines)
                {
                    if (line.Who != Speaker.Narrator) continue;
                    narrated = true;
                    StringAssert.StartsWith("motor-pool-", line.Voice, "every narrator line is recorded");
                }
                Assert.IsTrue(narrated, shot.Name + " has the narrator");
            }
            CutsceneIntro intro = m.Shots[2].Intro;
            Assert.AreEqual("kie", intro.Figure, "Trollogram kie, in his hatch");
            Assert.AreEqual("TROLLO-KIE", intro.Name);
            Assert.That(intro.At, Is.GreaterThanOrEqualTo(CutscenePlayer.BuildUp), "he's on screen through the build-up");
            foreach (CutsceneLine line in m.Shots[2].Lines) Assert.Greater(line.At, intro.At, "his lines come after his intro");
        }

        [Test]
        public void TheCoopStandInIsItsOwnSceneWithNothingToRecord()
        {
            CutsceneDef c = TestScenes.CoopTest;
            Assert.AreEqual(6, c.Shots.Length, "CI's two-phone check counts the moves between them");
            Assert.AreNotEqual(TestScenes.Opening.NetId, c.NetId);
            Assert.AreNotEqual(TestScenes.MotorPool.NetId, c.NetId);
            foreach (CutsceneShot shot in c.Shots)
            {
                Assert.IsNull(shot.Intro);
                foreach (CutsceneLine line in shot.Lines) Assert.IsNull(line.Voice);
            }
        }

        [Test]
        public void FreeingKiePicksUpAtTheCageWithKiesIntroFirst()
        {
            CutsceneDef f = TestScenes.FreeingKie;
            Assert.AreEqual("scene-freeing-kie", f.Id);
            CollectionAssert.AllItemsAreUnique(new[] { TestScenes.Opening.NetId, TestScenes.MotorPool.NetId, f.NetId, TestScenes.CoopTest.NetId });
            Assert.AreEqual(4, f.Shots.Length, "the glitch-out, the design's shot 1, plays in the game itself");
            CutsceneIntro intro = f.Shots[0].Intro;
            Assert.AreEqual("kie", intro.Figure);
            Assert.AreEqual("KIE", intro.Name);
            Assert.That(intro.At, Is.GreaterThanOrEqualTo(CutscenePlayer.BuildUp));
            Assert.That(intro.At, Is.GreaterThan(KieTuning.Freeing.Cage), "he's out of the cage when it lands");
            for (int i = 1; i < f.Shots.Length; i++) Assert.IsNull(f.Shots[i].Intro);
            foreach (CutsceneShot shot in f.Shots)
                foreach (CutsceneLine line in shot.Lines)
                    if (line.Who == Speaker.Narrator) StringAssert.StartsWith("freeing-", line.Voice);
            Assert.AreEqual("I'M GONNA... SIT THIS ONE OUT.", f.Shots[3].Lines[0].Text);
        }

        [Test]
        public void TheMotorPoolCantBeSkippedTheFirstTime()
        {
            var first = new CutscenePlayer(TestScenes.MotorPool, seenBefore: false, players: 1);
            Assert.IsFalse(first.CanSkip);
            Assert.IsFalse(first.Skip());
            var again = new CutscenePlayer(TestScenes.MotorPool, seenBefore: true, players: 1);
            Assert.IsTrue(again.Skip());
            Assert.IsTrue(again.Done);
        }

        [Test]
        public void ThePhoneThatEndsFirstLetsTheOtherFinish()
        {
            // b tapped the last shot first; a's tap ended it on a's phone in the same frame, so a's tap never went
            // out on its own. a's closing word ("tapped through it all") has to finish it on b's phone.
            var b = new CutscenePlayer(TestScenes.Opening, seenBefore: false, players: 2);
            int last = TestScenes.Opening.Shots.Length - 1;
            RunTo(b, last, 0.3f);
            b.Tap(0);
            Assert.IsFalse(b.Done, "b waits on a");
            CutsceneSync.Apply(b, true, CutsceneSync.Pack(3, 1, TestScenes.Opening.Shots.Length * 3, false), 3);
            Assert.IsTrue(b.Done);
        }

        [Test]
        public void ATapBeforeTheIntroWaitsForThePartnerThenJumpsBothToTheBuildUp()
        {
            var a = new CutscenePlayer(TestScenes.Opening, seenBefore: false, players: 2);
            Run(a, 0.5f);
            int beat = a.Beat;
            Assert.IsFalse(a.Tap(0));
            Assert.IsTrue(CutsceneSync.Apply(a, true, CutsceneSync.Pack(1, 1, beat, false), 1) == false);
            Assert.AreEqual(TestScenes.Opening.Shots[0].Intro.At - CutscenePlayer.BuildUp, a.T, "both tapped: on to the build-up");
            Assert.AreEqual(1, a.Beat);
            CutsceneSync.Apply(a, true, CutsceneSync.Pack(1, 1, beat, false), 1);
            Assert.IsFalse(a.HasTapped(1), "the tap before the build-up doesn't count in it");
        }

        [Test]
        public void ASkipNeedsBothAndALeavingPartnerHoldsNobodyUp()
        {
            var a = new CutscenePlayer(TestScenes.Opening, seenBefore: true, players: 2);
            Assert.IsFalse(CutsceneSync.Apply(a, true, CutsceneSync.Pack(1, 1, -1, false), 1), "they haven't asked to skip");
            Assert.IsTrue(CutsceneSync.Apply(a, true, CutsceneSync.Pack(1, 1, -1, true), 1), "now they have");
            Run(a, 0.5f);
            a.Tap(0);
            Assert.AreEqual(0, a.Shot);
            Assert.IsTrue(CutsceneSync.Apply(a, false, 0, 1), "gone: their say doesn't hold up a skip");
            Assert.IsTrue(a.T >= TestScenes.Opening.Shots[0].Intro.At - CutscenePlayer.BuildUp, "nor a tap");
        }
    
        [Test]
        public void ANarratorLineNeedsItsRecordingAndTimeToPlayIt()
        {
            // the scene deploy reads each recording's length from the clip itself (tools/scenecheck); here, made-up ones
            CutsceneDef opening = TestScenes.Opening;
            var clips = new Dictionary<string, float>();
            foreach (CutsceneShot shot in opening.Shots)
                foreach (CutsceneLine line in shot.Lines)
                    if (line.Voice != null) clips[line.Voice] = 1f;
            Assert.IsEmpty(SceneCheck.Timing(opening, clips));
            clips["opening-2"] = 1.3f; // a 4.5 s shot, the line at 0.9 s: room for 3.2 s of clip and a breath
            Assert.IsEmpty(SceneCheck.Timing(opening, clips));
            clips["opening-2"] = 3.4f;
            List<string> problems = SceneCheck.Timing(opening, clips);
            Assert.AreEqual(1, problems.Count);
            StringAssert.Contains("The door", problems[0]);
            clips.Remove("opening-2");
            StringAssert.Contains("no recording called 'opening-2'", SceneCheck.Timing(opening, clips)[0]);
        }

        [Test]
        public void TheShotWaitsForTheNarratorToFinish()
        {
            var p = new CutscenePlayer(TestScenes.MotorPool, false);
            float end = TestScenes.MotorPool.Shots[0].Seconds;
            while (p.T < end - 0.1f) p.Tick(0.05f);
            for (int i = 0; i < 20; i++) p.Tick(0.05f, null, true);
            Assert.AreEqual(0, p.Shot, "held while they talk");
            p.Tick(0.05f);
            Assert.AreEqual(1, p.Shot, "then on");

            p = new CutscenePlayer(TestScenes.MotorPool, false);
            while (p.T < end - 0.1f) p.Tick(0.05f);
            for (int i = 0; i < 100; i++) p.Tick(0.05f, null, true);
            Assert.AreEqual(1, p.Shot, "but never for longer than " + CutscenePlayer.MaxHold + " s");
        }
    }
}
