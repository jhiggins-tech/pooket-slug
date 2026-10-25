using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class DenTests
    {
        static readonly SoldierDef[] Both = { Content.Tones2, Content.Kie };

        static DenPartner InDen(SoldierDef s, bool ready) =>
            new DenPartner { Soldier = s, Flags = Den.InDen | (s != null ? Den.Seated : 0) | (ready ? Den.ReadyFlag : 0) };

        static void Run(Den den, float seconds, DenPartner partner = default, bool authority = true)
        {
            for (float t = 0f; t < seconds; t += 1f / 60f) den.Tick(1f / 60f, partner, authority);
        }

        [Test]
        public void SittingOnTheCouch_IsReady_AndGettingUpIsNot()
        {
            var den = new Den(DenMode.Solo, Both, Content.Tones2);
            Assert.IsTrue(den.SetReady(true));
            Assert.IsTrue(den.Ready);
            Assert.IsTrue(den.SetReady(false));
            Assert.IsFalse(den.Ready);
            Assert.IsFalse(new Den(DenMode.Crew, Both, Content.Tones2).SetReady(true), "CREW has no mission");
            Assert.IsFalse(new Den(DenMode.Solo, Both, null).SetReady(true), "nobody to sit");
        }

        [Test]
        public void TheSoldierPlayedLastIsAlreadyOnTheCouch()
        {
            Assert.AreEqual(Content.Kie, new Den(DenMode.Solo, Both, Content.Kie).Pick);
            Assert.IsNull(new Den(DenMode.Solo, new[] { Content.Tones2 }, Content.Kie).Pick, "only a crewmate with a station");
        }

        [Test]
        public void PickingSomeoneElseSwapsTheSeatAndUnreadies()
        {
            var den = new Den(DenMode.Solo, Both, Content.Tones2);
            Assert.IsTrue(den.SetReady(true));
            Assert.IsTrue(den.TryPick(Content.Kie));
            Assert.AreEqual(Content.Kie, den.Pick);
            Assert.IsFalse(den.Ready);
            Assert.IsFalse(den.TryPick(Content.Trooper), "nobody without a station");
        }

        [Test]
        public void SoloReadyZoomsIntoTheTvThenGoes()
        {
            var den = new Den(DenMode.Solo, Both, Content.Tones2);
            den.SetReady(true);
            den.Tick(0.01f, default, true);
            Assert.That(den.Zoom, Is.GreaterThanOrEqualTo(0f));
            Assert.IsFalse(den.TryPick(Content.Kie), "no changing once the zoom's begun");
            Run(den, Den.ZoomSeconds + 0.1f);
            Assert.IsTrue(den.Go);
        }

        [Test]
        public void CrewModeHasNoMission()
        {
            var den = new Den(DenMode.Crew, Both, Content.Tones2);
            Assert.IsFalse(den.SetReady(true));
            Assert.IsTrue(den.TryPick(Content.Kie));
        }

        [Test]
        public void ANewPartnersDenWaitsForBothThenCountsDown()
        {
            var den = new Den(DenMode.Coop, Both, Content.Tones2);
            den.SetReady(true);
            Run(den, 5f, InDen(Content.Kie, false));
            Assert.That(den.Zoom, Is.LessThan(0f), "waits for the partner");
            Assert.That(den.Countdown, Is.LessThan(0f));
            Run(den, 1f, InDen(Content.Kie, true));
            Assert.That(den.Countdown, Is.InRange(1.5f, 2.1f));
            Run(den, 0.5f, InDen(Content.Kie, false));
            Assert.That(den.Countdown, Is.LessThan(0f), "an UNREADY stops it");
            Run(den, Den.CountdownSeconds + 0.1f, InDen(Content.Kie, true));
            Assert.That(den.Zoom, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void AlonePlayingPartnerOrEmptyRoomDoesntWait()
        {
            var den = new Den(DenMode.Coop, Both, Content.Kie);
            den.SetReady(true);
            den.Tick(0.01f, new DenPartner { Soldier = Content.Tones2, Playing = true }, false);
            Assert.That(den.Zoom, Is.GreaterThanOrEqualTo(0f), "drops in on a partner already playing");

            var alone = new Den(DenMode.Coop, Both, Content.Tones2);
            alone.SetReady(true);
            alone.Tick(0.01f, default, true);
            Assert.That(alone.Zoom, Is.GreaterThanOrEqualTo(0f), "a host on their own starts, and a partner can drop in");
        }

        [Test]
        public void TwoPlayersCantBeTheSameSoldierAndTheHostWins()
        {
            var host = new Den(DenMode.Coop, Both, Content.Tones2);
            var guest = new Den(DenMode.Coop, Both, Content.Tones2);
            host.Tick(0f, InDen(Content.Tones2, false), true);
            guest.Tick(0f, InDen(Content.Tones2, false), false);
            Assert.AreEqual(Content.Tones2, host.Pick);
            Assert.IsNull(guest.Pick);
            Assert.IsTrue(guest.TakeUndone());
            Assert.IsFalse(guest.TakeUndone(), "said once");
            Assert.IsTrue(guest.Taken(Content.Tones2));
            Assert.IsFalse(guest.TryPick(Content.Tones2));
            Assert.AreEqual(Content.Kie, guest.NextFree());
            Assert.AreEqual(1, guest.Seat);
            Assert.AreEqual(0, host.Seat);
        }

        [Test]
        public void APartnerInTheMissionKeepsTheirSoldier()
        {
            var host = new Den(DenMode.Coop, Both, Content.Kie);
            host.Tick(0f, new DenPartner { Soldier = Content.Kie, Playing = true }, true);
            Assert.IsNull(host.Pick, "even the host gives way to someone already playing");
        }

        [Test]
        public void FlagsSayWhereWeAre()
        {
            var den = new Den(DenMode.Coop, Both, null);
            Assert.AreEqual(Den.InDen, den.Flags);
            den.TryPick(Content.Tones2);
            den.SetReady(true);
            Assert.AreEqual(Den.InDen | Den.Seated | Den.ReadyFlag, den.Flags);
        }

        [Test]
        public void BitsNeedTheirCrewIdleOrOnTheCouchAndDontRepeat()
        {
            var rng = new System.Random(1);
            var none = new List<SoldierDef>();
            Assert.IsNull(DenBits.Choose(new[] { Content.Tones2 }, none, null, rng), "a bit needs two");
            DenBit slushie = DenBits.Choose(Both, none, null, rng);
            Assert.AreEqual("slushie", slushie.Id, "both idle: the only idle pair's bit");
            DenBit lean = DenBits.Choose(new[] { Content.Kie }, new[] { Content.Tones2 }, null, rng);
            Assert.AreEqual("im-next", lean.Id);
            Assert.IsTrue(lean.OnCouch);
            foreach (DenBit b in DenBits.All)
            {
                Assert.That(b.Seconds, Is.InRange(4f, 8f), b.Id);
                foreach (DenLine l in b.Lines)
                {
                    Assert.That(l.At, Is.LessThan(b.Seconds - 1.5f), b.Id + " has time to read");
                    Assert.AreEqual(l.Text.ToUpperInvariant(), l.Text, "bubbles are in capitals");
                }
            }
        }

        // docs/design/lobby.md, Who lives where: a loop, a breath, another loop. Two loops never run back to back.
        [Test]
        public void IdleLoops_AlwaysHaveABreathBetweenThem()
        {
            var rng = new System.Random(7);
            bool[] seen = new bool[DenIdle.Loops];
            foreach (int start in new[] { 0, 1, DenIdle.Breath })
            {
                int cycle = start;
                for (int i = 0; i < 400; i++)
                {
                    int next = DenIdle.Next(cycle, rng);
                    if (cycle != DenIdle.Breath) Assert.AreEqual(DenIdle.Breath, next, "a breath after loop " + cycle);
                    else
                    {
                        Assert.That(next, Is.InRange(0, DenIdle.Loops - 1), "a loop after a breath");
                        seen[next] = true;
                    }
                    cycle = next;
                }
            }
            for (int l = 0; l < DenIdle.Loops; l++) Assert.IsTrue(seen[l], "loop " + l + " comes up");
        }

        [Test]
        public void IdleLoops_RunTheirLength_AndABreathAFewSeconds()
        {
            var rng = new System.Random(3);
            for (int i = 0; i < 200; i++)
            {
                Assert.AreEqual(DenIdle.LoopSeconds, DenIdle.SecondsOf(i % DenIdle.Loops, rng), 0.0001f);
                Assert.That(DenIdle.SecondsOf(DenIdle.Breath, rng), Is.InRange(DenIdle.BreathMin, DenIdle.BreathMax));
            }
            // lobby.md: loops of 3 to 6 seconds
            Assert.That(DenIdle.LoopSeconds, Is.InRange(3f, 6f));
        }
    
        // docs/design/lobby.md, Crew interactions: a pair with no bit just waves
        [Test]
        public void APairWithNoBit_JustWaves_AndAPairWithOneNever()
        {
            var none = new List<SoldierDef>();
            Assert.IsTrue(DenBits.HaveABit(Content.Tones2, Content.Kie));
            Assert.IsTrue(DenBits.HaveABit(Content.Kie, Content.Tones2), "either way round");
            Assert.IsFalse(DenBits.HaveABit(Content.Kcaj, Content.Tones2));
            Assert.IsFalse(DenBits.HaveABit(Content.Kcaj, Content.Kie));

            var rng = new System.Random(5);
            for (int i = 0; i < 200; i++)
            {
                Assert.IsFalse(DenBits.Choose(Both, none, null, rng).Wave, "tones2 and kie have a bit, so they never just wave");
                DenBit w = DenBits.Choose(new[] { Content.Kcaj, Content.Tones2 }, none, null, rng);
                Assert.IsTrue(w.Wave, "kcaj and tones2 have no bit: a wave");
                Assert.IsFalse(w.OnCouch);
                Assert.AreEqual(0, w.Lines.Length, "a wave says nothing");
                Assert.That(new[] { w.First, w.Second }, Is.EquivalentTo(new[] { Content.Kcaj, Content.Tones2 }));
            }
            Assert.IsNull(DenBits.Choose(new[] { Content.Kcaj }, new[] { Content.Tones2 }, null, rng), "no wave to the couch");
        }

        [Test]
        public void EitherOfAPair_CanWaveFirst_AndAWaveDoesntPlayTwiceInARow()
        {
            var none = new List<SoldierDef>();
            var rng = new System.Random(9);
            var all = new[] { Content.Tones2, Content.Kie, Content.Kcaj };
            bool kcajFirst = false, kcajSecond = false, slushie = false, waveKie = false, waveTones2 = false;
            DenBit last = null;
            for (int i = 0; i < 400; i++)
            {
                DenBit b = DenBits.Choose(all, none, last, rng);
                Assert.IsFalse(b.Same(last), "never the same twice in a row while another fits");
                if (b.Wave)
                {
                    if (b.First == Content.Kcaj) kcajFirst = true;
                    if (b.Second == Content.Kcaj) kcajSecond = true;
                    SoldierDef other = b.First == Content.Kcaj ? b.Second : b.First;
                    Assert.AreNotEqual(Content.Kcaj, other);
                    if (other == Content.Kie) waveKie = true;
                    if (other == Content.Tones2) waveTones2 = true;
                }
                else if (b.Id == "slushie") slushie = true;
                last = b;
            }
            Assert.IsTrue(kcajFirst && kcajSecond, "either one starts the wave");
            Assert.IsTrue(slushie && waveKie && waveTones2, "all three idle: the slushie, and kcaj waving with each of them");
            Assert.IsTrue(DenWave.Of(Content.Kcaj, Content.Kie).Same(DenWave.Of(Content.Kie, Content.Kcaj)), "a wave's the same either way round");
        }

        [Test]
        public void AWave_TheFirstWaves_ThenTheSecondWavesBack()
        {
            Assert.That(DenWave.Seconds, Is.LessThanOrEqualTo(DenBits.MinGap), "a wave's over well before the next bit");
            Assert.IsFalse(DenWave.Waving(0, 0f) || DenWave.Waving(1, 0f), "they turn to each other first");
            Assert.IsTrue(DenWave.Waving(0, 0.5f) && !DenWave.Waving(1, 0.5f), "the first waves");
            Assert.IsTrue(DenWave.Waving(0, 1.5f) && DenWave.Waving(1, 1.5f), "the second waves back as the first finishes");
            Assert.IsTrue(!DenWave.Waving(0, 2.2f) && DenWave.Waving(1, 2.2f));
            Assert.IsFalse(DenWave.Waving(0, DenWave.Seconds) || DenWave.Waving(1, DenWave.Seconds), "both done when it ends");
            for (int who = 0; who < 2; who++)
            {
                bool up = false, down = false;
                for (float t = 0f; t < DenWave.Seconds; t += 1f / 60f)
                {
                    if (!DenWave.Waving(who, t)) Assert.IsFalse(DenWave.HandUp(who, t), "the hand's down when not waving");
                    else if (DenWave.HandUp(who, t)) up = true;
                    else down = true;
                }
                Assert.IsTrue(up && down, "the hand goes up and down: a wave, not a raised hand");
            }
        }

        // docs/design/lobby.md, Who lives where: kcaj eats two cones at once, or adds a note to the string board and the edges dim
        [Test]
        public void KcajsLoops_FitInALoop_AndTheDimComesAndGoes()
        {
            Assert.AreEqual(DenIdle.Loops, 2, "two loops, like the others");
            int bites = 0;
            bool was = false;
            for (float t = 0f; t < DenIdle.LoopSeconds; t += 1f / 60f)
            {
                bool now = KcajIdle.Biting(t);
                if (now && !was) bites++;
                was = now;
            }
            Assert.AreEqual(3, bites, "three bites");
            Assert.IsFalse(KcajIdle.Biting(DenIdle.LoopSeconds - 0.01f), "the cones are down by the loop's end");

            Assert.AreEqual(0, KcajIdle.NoteAt(0f));
            Assert.AreEqual(1, KcajIdle.NoteAt((KcajIdle.NoteInHand + KcajIdle.NoteUp) / 2f), "in his hand");
            Assert.AreEqual(2, KcajIdle.NoteAt(DenIdle.LoopSeconds - 0.01f), "stuck up by the end");
            Assert.That(KcajIdle.NoteUp, Is.LessThan(KcajIdle.DimFrom), "the note's up before the edges dim");

            Assert.AreEqual(0f, KcajIdle.Dim(0f));
            Assert.AreEqual(0f, KcajIdle.Dim(DenIdle.LoopSeconds), "gone by the loop's end, so the breath isn't dimmed");
            Assert.That(KcajIdle.DimTo, Is.LessThanOrEqualTo(DenIdle.LoopSeconds));
            Assert.AreEqual(1f, KcajIdle.Dim((KcajIdle.DimFull + KcajIdle.DimHold) / 2f), 0.0001f, "for a moment, fully");
            float prev = 0f;
            for (float t = 0f; t <= DenIdle.LoopSeconds; t += 1f / 60f)
            {
                float d = KcajIdle.Dim(t);
                Assert.That(d, Is.InRange(0f, 1f));
                Assert.That(System.Math.Abs(d - prev), Is.LessThan(0.1f), "it fades, never snaps");
                prev = d;
            }
        }
    }
}
