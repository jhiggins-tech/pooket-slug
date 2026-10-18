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
            Assert.IsTrue(den.ToggleReady());
            Assert.IsTrue(den.TryPick(Content.Kie));
            Assert.AreEqual(Content.Kie, den.Pick);
            Assert.IsFalse(den.Ready);
            Assert.IsFalse(den.TryPick(Content.Trooper), "nobody without a station");
        }

        [Test]
        public void SoloReadyZoomsIntoTheTvThenGoes()
        {
            var den = new Den(DenMode.Solo, Both, Content.Tones2);
            den.ToggleReady();
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
            Assert.IsFalse(den.ToggleReady());
            Assert.IsTrue(den.TryPick(Content.Kie));
        }

        [Test]
        public void ReadyWithAnEmptySeatSeatsTheFirstFreeCrewmate()
        {
            var den = new Den(DenMode.Coop, Both, null);
            den.Tick(0f, InDen(Content.Tones2, false), false);
            Assert.IsTrue(den.ToggleReady());
            Assert.AreEqual(Content.Kie, den.Pick, "tones2's taken");
            Assert.IsTrue(den.Ready);
        }

        [Test]
        public void ANewPartnersDenWaitsForBothThenCountsDown()
        {
            var den = new Den(DenMode.Coop, Both, Content.Tones2);
            den.ToggleReady();
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
            den.ToggleReady();
            den.Tick(0.01f, new DenPartner { Soldier = Content.Tones2, Playing = true }, false);
            Assert.That(den.Zoom, Is.GreaterThanOrEqualTo(0f), "drops in on a partner already playing");

            var alone = new Den(DenMode.Coop, Both, Content.Tones2);
            alone.ToggleReady();
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
            den.ToggleReady();
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
    }
}
