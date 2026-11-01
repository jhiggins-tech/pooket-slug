using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>
    /// kcaj's tabk comes home (Core/Tabks/KcajHomecoming.cs): once, on the first solo bay visit with SOFT SERVE won and
    /// kcaj moved in, in three shots: the drone brings the wreck in from off the screen, the crane lowers the machine on,
    /// the note goes on and the paint runs down, ragged, till it's whole.
    /// </summary>
    public class KcajHomecomingTests
    {
        const float Dt = 1f / 60f;

        static Profile AfterTwo(bool movedIn = true)
        {
            var p = new Profile();
            p.RecordRun(Missions.One, 100, won: true, solo: true);
            p.RecordRun(Missions.Two, 100, won: true, solo: true);
            if (movedIn) p.MarkSeen(DenBeats.MovedIn(Content.Kcaj));
            return p;
        }

        [Test]
        public void ItPlays_OnceKcajsTabkIsWonAndHesMovedIn_SoloOnly_OnceEver()
        {
            var before = new Profile();
            before.RecordRun(Missions.One, 100, won: true, solo: true);
            Assert.IsFalse(KcajHomecoming.Due(before, TabkCards.Won(before), DenMode.Solo, false), "not before Mission two's beaten");
            Profile notHome = AfterTwo(movedIn: false);
            Assert.IsFalse(KcajHomecoming.Due(notHome, TabkCards.Won(notHome), DenMode.Solo, false), "his move-in comes first");
            Profile p = AfterTwo();
            Assert.IsTrue(KcajHomecoming.Due(p, TabkCards.Won(p), DenMode.Solo, false));
            Assert.IsFalse(KcajHomecoming.Due(p, TabkCards.Won(p), DenMode.Coop, false), "never in a co-op room: it waits for a visit on your own");
            Assert.IsFalse(KcajHomecoming.Due(p, TabkCards.Won(p), DenMode.Solo, true), "a ?test= jump never plays it");
            p.MarkSeen(KcajHomecoming.Reveal);
            Assert.IsFalse(KcajHomecoming.Due(p, TabkCards.Won(p), DenMode.Solo, false), "once ever");
            Assert.AreEqual("kcaj-tabk-home", KcajHomecoming.Reveal);
        }

        [Test]
        public void ItsShapeIsTheScripts_ThreeShots_About13Seconds_InTheBay()
        {
            var b = new KcajHomecoming();
            Assert.AreEqual(SkitPlace.Bay, b.Place);
            Assert.AreEqual(13f, b.Length, 0.5f);
            Assert.AreEqual(2, b.Shots.Length);
            CollectionAssert.AreEquivalent(new[] { Content.Tones2, Content.Kcaj }, b.Cast, "kcaj and tones2, whoever's picked");
            foreach (SkitLine l in b.Lines) CollectionAssert.Contains(b.Cast, l.Who, l.Text);
        }

        [Test]
        public void ShotOne_TheDroneBringsTheWreckInFromOffTheScreen_AndSetsItDown()
        {
            var b = new KcajHomecoming();
            Assert.Greater(b.Drone.x, KcajHomecoming.StageWidth, "it starts past the screen's right edge");
            Assert.IsTrue(b.Carrying);
            Assert.Greater(b.Wreck.y, BayMap.StandY, "hanging off its cable");
            Assert.AreEqual(b.Drone.x, b.Wreck.x, 1e-3f, "under the drone");
            Assert.AreEqual(0f, b.Hose, "the hose waits for it");
            Assert.IsFalse(b.KcajIn, "kcaj wanders in after it");
            Step(b, KcajHomecoming.LandAt + 0.01f);
            Assert.IsFalse(b.Carrying);
            Assert.AreEqual((BayMap.StandX, BayMap.StandY), b.Wreck, "on the stands");
            Assert.IsTrue(b.KcajIn);
            Assert.IsTrue(b.Cones, "two cones at once");
            Step(b, KcajHomecoming.Shot2 - 0.05f - b.T);
            Assert.AreEqual(DroneShow.Home.X, b.Drone.x, 1e-2f, "and the drone's home");
            Assert.IsFalse(b.Flying);
            Assert.Greater(b.Hose, 0f, "the hose going in");
            Assert.IsFalse(b.Machine, "half a tabk till the crane");
        }

        [Test]
        public void ShotTwo_TheCraneLowersTheMachine_KcajStepsBack_LooksAtTones2_ThreeLines()
        {
            var b = new KcajHomecoming();
            Step(b, KcajHomecoming.Shot2 + 0.01f);
            Assert.AreEqual(2, b.Shot);
            Assert.IsTrue(b.Machine);
            Assert.Less(b.Lowered, 0.1f);
            Step(b, KcajHomecoming.LowerTo - b.T);
            Assert.AreEqual(1f, b.Lowered, 1e-3f);
            Assert.IsTrue(b.KcajFacesLeft, "looking at it");
            Step(b, KcajHomecoming.TurnAt + 0.01f - b.T);
            Assert.IsFalse(b.KcajFacesLeft, "then at tones2");
            Assert.AreEqual(KcajHomecoming.KcajBack, b.Kcaj.x, 1e-3f, "stepped back");
            Step(b, 5.5f - b.T);
            Assert.AreEqual("THE CREAMERY. THE TROLLOGRAMS. THE MOON.", b.Line.Value.Text);
            Step(b, 7.5f - b.T);
            Assert.AreEqual(Content.Tones2, b.Line.Value.Who);
            Step(b, 8.5f - b.T);
            Assert.AreEqual("IT'S ALL CONNECTED.", b.Line.Value.Text);
            Assert.IsFalse(b.Note);
        }

        [Test]
        public void ShotThree_TheNote_ThePaintRunsDownRagged_TheCard_MineNow()
        {
            var b = new KcajHomecoming();
            Step(b, KcajHomecoming.HopUpTo + 0.01f);
            Assert.AreEqual(3, b.Shot);
            Assert.IsTrue(b.AtMachine);
            Assert.AreEqual(KcajHomecoming.DeckY, b.Kcaj.y, 1e-3f, "up on the hull by the machine");
            Assert.IsFalse(b.Cones, "his hands are busy");
            Step(b, KcajHomecoming.NoteAt + 0.01f - b.T);
            Assert.IsTrue(b.Note, "the note over the sticker");
            Assert.Greater(b.NoteSize, 1f, "slapped on");
            for (int i = 0; i < KcajHomecoming.Drips; i++) Assert.AreEqual(0f, b.Paint(i), "no paint yet");
            Step(b, (KcajHomecoming.PaintFrom + KcajHomecoming.PaintTo) / 2f - b.T);
            Assert.IsTrue(b.Humming);
            float min = 1f, max = 0f;
            for (int i = 0; i < KcajHomecoming.Drips; i++)
            {
                min = System.Math.Min(min, b.Paint(i));
                max = System.Math.Max(max, b.Paint(i));
            }
            Assert.Greater(max, min + 0.1f, "a ragged edge, like a drip");
            Assert.Greater(min, 0f, "running down together");
            Assert.IsFalse(b.Painted);
            Step(b, KcajHomecoming.PaintTo + 0.01f - b.T);
            for (int i = 0; i < KcajHomecoming.Drips; i++) Assert.AreEqual(1f, b.Paint(i), "no olive left");
            Assert.IsTrue(b.Painted);
            Assert.IsFalse(b.Humming);
            Step(b, KcajHomecoming.CardFrom + 0.5f - b.T);
            Assert.AreEqual(1f, b.Card, 1e-3f);
            Assert.AreEqual("MINE NOW.", b.Line.Value.Text);
            Step(b, KcajHomecoming.ToNozzleTo + 0.01f - b.T);
            Assert.IsTrue(b.Polishing);
            Assert.AreEqual((KcajHomecoming.KcajNozzle, BayMap.FloorY(BayDeck.Floor)), b.Kcaj, "leaning on it at the nozzle, where the skit starts him");
            Assert.AreEqual(ConesSkit.KcajX, KcajHomecoming.KcajNozzle);
            Step(b, b.Length - b.T);
            Assert.IsTrue(b.Done);
        }

        [Test]
        public void ATap_SkipsItToItsLastFrame()
        {
            var b = new KcajHomecoming();
            b.Tick(1f);
            b.Skip();
            Assert.IsTrue(b.Done);
            Assert.IsTrue(b.Painted, "SOFT SERVE whole");
            Assert.IsTrue(b.Note, "the note's on: the skit keeps it");
        }

        static void Step(Skit b, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += Dt) b.Tick(System.Math.Min(Dt, seconds - t));
        }
    }
}
