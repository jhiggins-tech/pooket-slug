using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>
    /// The skits that play in a mission or at its debrief (Core/Missions/MissionSkits.cs; docs/design/cutscenes/skits.md):
    /// when Keep it down and They're tracking us are due, their lines as the scripts have them, and their timing.
    /// </summary>
    public class MissionSkitsTests
    {
        static readonly StoryRun Solo = new StoryRun(coop: false);

        static Profile Replaying()
        {
            var p = new Profile();
            p.MarkSeen(StoryRecord.Coda);
            return p;
        }

        [Test]
        public void KeepItDownPlaysOnAReplayOfMissionOnesCodaOnce()
        {
            Assert.IsFalse(KeepItDown.Due(new Profile(), Missions.One, Solo, false), "the first play of the coda stays clean");
            Profile p = Replaying();
            Assert.IsTrue(KeepItDown.Due(p, Missions.One, Solo, false), "a replay: the coda's been cleared before");
            Assert.IsTrue(KeepItDown.Spends(Solo));
            p.MarkSeen(KeepItDown.Key);
            Assert.IsFalse(KeepItDown.Due(p, Missions.One, Solo, false), "once ever");
        }

        [Test]
        public void KeepItDownIsSoloAndMissionOnesOnly()
        {
            Profile p = Replaying();
            Assert.IsFalse(KeepItDown.Due(p, Missions.Two, Solo, false), "only Mission one has kie asleep by the wall");
            Assert.IsFalse(KeepItDown.Due(p, Missions.One, new StoryRun(coop: true), false), "never in co-op");
            Assert.IsFalse(KeepItDown.Due(p, Missions.One, new StoryRun(false, sandbox: true), true), "never in a sandbox");
        }

        [Test]
        public void TestCodaShowsKeepItDownWithoutSpendingIt()
        {
            var testing = new StoryRun(false, testing: true);
            Assert.IsTrue(KeepItDown.Due(new Profile(), Missions.One, testing, testCoda: true), "?test=coda shows it, cleared or not");
            Assert.IsFalse(KeepItDown.Spends(testing), "and it's still to come on a real replay");
            Assert.IsFalse(KeepItDown.Due(Replaying(), Missions.One, testing, testCoda: false), "another test start doesn't play it");
        }

        [Test]
        public void KeepItDownIsKiesLineWhileYouKeepPlaying()
        {
            Assert.AreEqual("...KEEP IT DOWN.", KeepItDown.Line);
            Assert.Greater(KeepItDown.LineAt, 0f, "the engine turns over first");
            Assert.Less(KeepItDown.LineAt, KeepItDown.StirFor);
            Assert.Greater(KeepItDown.ZsBack, KeepItDown.LineAt, "his Zs stop while he talks");
            Assert.LessOrEqual(KeepItDown.ZsBack, KeepItDown.Length, "he settles back within the skit's 3 s");
        }

        [Test]
        public void TrackingUsPlaysOnMissionTwosFirstClearOnce()
        {
            var p = new Profile();
            Assert.IsTrue(TrackingUs.Due(p, Missions.Two, firstClear: true, testDebrief: false));
            Assert.IsFalse(TrackingUs.Due(p, Missions.Two, firstClear: false, testDebrief: false), "a replay's debrief");
            Assert.IsFalse(TrackingUs.Due(p, Missions.One, firstClear: true, testDebrief: false), "Mission one's debrief has no kcaj");
            p.MarkSeen(TrackingUs.Key);
            Assert.IsFalse(TrackingUs.Due(p, Missions.Two, firstClear: true, testDebrief: false), "once ever");
            Assert.IsTrue(TrackingUs.Due(p, Missions.Two, firstClear: true, testDebrief: true), "?test=debrief on Mission two shows it every time");
            Assert.IsFalse(TrackingUs.Due(p, Missions.One, firstClear: true, testDebrief: true));
            Assert.IsFalse(TrackingUs.Spends(testDebrief: true));
            Assert.IsTrue(TrackingUs.Spends(testDebrief: false));
        }

        [Test]
        public void TrackingUsLinesAreTheScriptsInItsThreeShots()
        {
            Assert.AreEqual(3, TrackingUs.Lines.Count);
            Assert.AreSame(Content.Kcaj, TrackingUs.Lines[0].Who);
            Assert.AreEqual("ACCURACY? THEY'RE TRACKING US.", TrackingUs.Lines[0].Text);
            Assert.AreSame(Content.Kie, TrackingUs.Lines[1].Who);
            Assert.AreEqual("IT'S THE SCORE.", TrackingUs.Lines[1].Text);
            Assert.AreSame(Content.Kcaj, TrackingUs.Lines[2].Who);
            Assert.AreEqual("THAT'S WHAT THEY WANT YOU TO THINK.", TrackingUs.Lines[2].Text);
            float[] shots = { 0f, TrackingUs.Shot2, TrackingUs.Shot3, TrackingUs.Length };
            for (int i = 0; i < 3; i++)
            {
                TrackingUs.Line l = TrackingUs.Lines[i];
                Assert.GreaterOrEqual(l.From, shots[i], l.Text + " is in its shot");
                Assert.LessOrEqual(l.To, shots[i + 1] + 0.5f, l.Text);
                Assert.Greater(l.To - l.From, 1.5f, l.Text + " is up long enough to read");
                Assert.AreEqual(l.Text, TrackingUs.Saying((l.From + l.To) / 2f)?.Text);
                StringAssert.DoesNotContain("—", l.Text);
            }
            Assert.IsNull(TrackingUs.Saying(TrackingUs.Length), "nothing's said once it's over");
        }

        [Test]
        public void KcajStaresAsTheAccuracyCountsAndTheNoteGoesOverIt()
        {
            const int rows = DebriefPlayer.TallyRows, acc = DebriefPlayer.AccuracyRow;
            float startsAt = TrackingUs.AccuracyFrom;
            Assert.AreEqual(acc / (float)rows, TrackingUs.Tally(startsAt, rows, acc), 1e-4, "the rows above count first");
            Assert.LessOrEqual(startsAt, TrackingUs.Lines[0].From, "the accuracy's counting as kcaj asks about it");
            Assert.Greater(TrackingUs.Lean(TrackingUs.Lines[0].From), 0f, "leaning in, slowly, as he asks");
            Assert.AreEqual(1f, TrackingUs.Lean(TrackingUs.Lines[0].To), 1e-4, "and right in by the end of it");
            float under = TrackingUs.Tally(TrackingUs.NoteAt, rows, acc) * rows - acc;
            Assert.Greater(under, 0f);
            Assert.Less(under, 1f, "the counting carries on under the note");
            Assert.IsFalse(TrackingUs.NoteOn(TrackingUs.Shot3), "the note's in the third shot, after his line");
            Assert.IsTrue(TrackingUs.NoteOn(TrackingUs.NoteAt) && TrackingUs.NoteOn(TrackingUs.Length));
            Assert.Greater(TrackingUs.LookFrom, TrackingUs.NoteAt, "tones2 looks at the note once it's on");
            Assert.Less(TrackingUs.LookTo, TrackingUs.Length, "and then at the player");
            Assert.AreEqual(0f, TrackingUs.Lean(TrackingUs.Length), "kcaj's back upright at the end");
            float last = 0f;
            for (float t = 0f; t <= TrackingUs.Length; t += 0.05f)
            {
                float k = TrackingUs.Tally(t, rows, acc);
                Assert.GreaterOrEqual(k, last, "the tally only counts up");
                last = k;
            }
            Assert.AreEqual(1f, TrackingUs.Tally(TrackingUs.Length, rows, acc));
        }

        static DebriefResult Result(bool skit) => new DebriefResult { Score = 100, Best = 100, TrackingUs = skit, Kcaj = skit };

        [Test]
        public void TheTallyWaitsForTheSkitAndATapSkipsItFillingTheNumbers()
        {
            var p = new DebriefPlayer(Result(true));
            Assert.IsTrue(p.SkitPlaying);
            p.Tick(DebriefPlayer.TallySeconds + 0.1f);
            Assert.IsFalse(p.Counted, "the tally waits for the skit");
            Assert.IsTrue(p.SkitPlaying);
            Assert.Less(p.Progress, 1f);
            p.Tap();
            Assert.AreEqual(DebriefPlayer.Part.Tally, p.Now, "the tap skips the skit and fills the numbers");
            Assert.IsTrue(p.Counted);
            Assert.AreEqual(1f, p.Progress);
            Assert.IsFalse(p.SkitPlaying);
            Assert.IsTrue(TrackingUs.NoteOn(p.SkitT), "skipped, it ends as it would have: the note on");
            p.Tick(1f);
            p.Tap();
            Assert.AreEqual(DebriefPlayer.Part.Xp, p.Now);
            Assert.IsTrue(TrackingUs.NoteOn(p.SkitT), "the note stays for the rest of the debrief");
        }

        [Test]
        public void PlayedThroughTheTallyEndsWithTheSkit()
        {
            var p = new DebriefPlayer(Result(true));
            p.Tick(TrackingUs.Length - 0.1f);
            Assert.IsFalse(p.Counted);
            p.Tick(0.2f);
            Assert.IsTrue(p.Counted);
            Assert.IsFalse(p.SkitPlaying);
        }

        [Test]
        public void WithoutTheSkitTheTallyIsAsEver()
        {
            var p = new DebriefPlayer(Result(false));
            Assert.IsFalse(p.SkitPlaying);
            p.Tick(DebriefPlayer.TallySeconds / 2f);
            Assert.AreEqual(0.5f, p.Progress, 1e-4);
            p.Tick(DebriefPlayer.TallySeconds);
            Assert.IsTrue(p.Counted);
        }
    }
}
