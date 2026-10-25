using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>
    /// The skits (Core/Lobby/Skits.cs, <c>docs/design/cutscenes/skits.md</c>): which one is due as the den opens (once
    /// ever, solo only, one a visit, never with a beat but the two that follow one), the co-op button that Two of us now
    /// puts up, and each skit's clock: its lines from its cast, a skip to the end.
    /// </summary>
    public class SkitsTests
    {
        const float Dt = 1f / 60f;
        static readonly SoldierDef[] Starters = { Content.Tones2, Content.Kie };
        static readonly SoldierDef[] WithKcaj = { Content.Tones2, Content.Kie, Content.Kcaj };

        static SkitVisit Visit(IReadOnlyList<SoldierDef> crew, Profile p, bool coopOpen = true, MissionDef replayed = null, DenMode mode = DenMode.Solo) =>
            new SkitVisit
            {
                Place = SkitPlace.Den,
                Mode = mode,
                Crew = crew,
                Beats = DenBeats.Due(p, crew, mode),
                CoopOpen = coopOpen,
                Replayed = replayed,
            };

        /// <summary>A profile past Mission one: co-op open, the first visit played.</summary>
        static Profile AfterTheFirstVisit()
        {
            var p = new Profile { coop = true };
            p.MarkSeen(DenBeats.FirstVisit);
            return p;
        }

        [Test]
        public void TwoOfUsNow_FollowsTheFirstVisit_OnTheSameVisit_Once()
        {
            var p = new Profile { coop = true };
            SkitVisit v = Visit(Starters, p);
            Assert.AreEqual(1, v.Beats.Count, "the first visit plays first");
            Skit skit = Skits.Due(p, v);
            Assert.IsInstanceOf<TwoOfUsSkit>(skit, "and the skit straight after it");
            Assert.AreEqual("skit-two-of-us", skit.Key);
            Assert.IsTrue(Skits.Played(p, skit));
            Assert.IsNull(Skits.Due(p, Visit(Starters, p)), "once ever");
        }

        [Test]
        public void TwoOfUsNow_WaitsForCoopToOpen_AndForASoloVisit()
        {
            var p = new Profile();
            Assert.IsNull(Skits.Due(p, Visit(Starters, p, coopOpen: false)), "nothing to put up yet");
            p.coop = true;
            Assert.IsNull(Skits.Due(p, Visit(Starters, p, mode: DenMode.Coop)), "never in a co-op room");
            Assert.IsInstanceOf<TwoOfUsSkit>(Skits.Due(p, Visit(Starters, p)), "the next visit on your own");
        }

        [Test]
        public void APhonePastIt_GetsTwoOfUsNowOnItsNextSoloVisit_UnlessItsPressedTheButton()
        {
            Profile p = AfterTheFirstVisit();
            Assert.IsInstanceOf<TwoOfUsSkit>(Skits.Due(p, Visit(Starters, p)), "co-op open from before the skit was built");
            p.MarkSeen(Reveals.Coop);
            Assert.IsNull(Skits.Due(p, Visit(Starters, p)), "it's found co-op: the button just shows");
        }

        [Test]
        public void TheCoopButton_IsUpOnceTwoOfUsNowHasPutItUp()
        {
            var p = new Profile { coop = true };
            Assert.IsFalse(Skits.CoopButtonUp(p, unlocked: false), "hidden on the first visit till the skit puts it up");
            Assert.IsTrue(Skits.CoopButtonUp(p, unlocked: true), "?unlock=coop and the ?test= starts show it at once");
            var skit = new TwoOfUsSkit();
            Assert.IsFalse(skit.ButtonUp);
            skit.Skip();
            Assert.IsTrue(skit.ButtonUp, "a skip still puts it up");
            Skits.Played(p, skit);
            Assert.IsTrue(Skits.CoopButtonUp(p, unlocked: false), "and from then on it shows");
            var pressed = new Profile { coop = true };
            pressed.MarkSeen(Reveals.Coop);
            Assert.IsTrue(Skits.CoopButtonUp(pressed, unlocked: false), "a player who's pressed it before");
        }

        [Test]
        public void NoSkitsInATestStart_AndUnlockedCoopHasNoSkitForItsButton()
        {
            var p = new Profile { coop = true };
            SkitVisit v = Visit(Starters, p);
            v.Testing = true;
            Assert.IsNull(Skits.Due(p, v));
            v = Visit(Starters, p);
            v.CoopUnlocked = true;
            Assert.IsNull(Skits.Due(p, v));
        }

        [Test]
        public void SpecialDelivery_PlaysTheFirstTimeTheDenOpensAfterAMissionOneReplay()
        {
            Profile p = AfterTheFirstVisit();
            p.MarkSeen(TwoOfUsSkit.Id);
            Assert.IsNull(Skits.Due(p, Visit(Starters, p)), "only from a replay's debrief");
            Assert.IsNull(Skits.Due(p, Visit(Starters, p, replayed: Missions.Two)), "Mission one's");
            Skit skit = Skits.Due(p, Visit(Starters, p, replayed: Missions.One));
            Assert.IsInstanceOf<SpecialDeliverySkit>(skit);
            Skits.Played(p, skit);
            Assert.IsNull(Skits.Due(p, Visit(Starters, p, replayed: Missions.One)), "once ever");
        }

        [Test]
        public void SpecialDelivery_NeverPlaysOnTheSameVisitAsABeat_OrAnotherSkit()
        {
            var p = new Profile { coop = true };
            p.MarkSeen(TwoOfUsSkit.Id);
            Assert.IsNull(Skits.Due(p, Visit(Starters, p, replayed: Missions.One)), "the first visit's due");
            p = AfterTheFirstVisit();
            Assert.IsInstanceOf<TwoOfUsSkit>(Skits.Due(p, Visit(Starters, p, replayed: Missions.One)), "one skit a visit: Two of us now first");
        }

        [Test]
        public void ATrollogramOfAnything_FollowsKcajsMoveIn_OnTheSameVisit()
        {
            Profile p = AfterTheFirstVisit();
            p.MarkSeen(TwoOfUsSkit.Id);
            SkitVisit v = Visit(WithKcaj, p);
            Assert.AreEqual(1, v.Beats.Count, "kcaj moves in first");
            Assert.IsInstanceOf<TrollogramSkit>(Skits.Due(p, v));
            Assert.IsNull(Skits.Due(p, Visit(Starters, p)), "not before he's home");
        }

        [Test]
        public void ATrollogramOfAnything_WaitsOutTheFirstVisit_AndPlaysLaterIfMissed()
        {
            var p = new Profile();
            p.MarkSeen(TwoOfUsSkit.Id);
            Assert.IsNull(Skits.Due(p, Visit(WithKcaj, p)), "the first visit's in the beats too");
            foreach (DenBeat b in DenBeats.Due(p, WithKcaj, DenMode.Solo)) DenBeats.Played(p, b);
            Skit skit = Skits.Due(p, Visit(WithKcaj, p));
            Assert.IsInstanceOf<TrollogramSkit>(skit, "on the next visit, with no beat in its way");
            Skits.Played(p, skit);
            Assert.IsNull(Skits.Due(p, Visit(WithKcaj, p)), "once ever");
            Assert.IsInstanceOf<SpecialDeliverySkit>(Skits.Due(p, Visit(WithKcaj, p, replayed: Missions.One)), "the next skit's its own trigger's");
        }

        [Test]
        public void ASkitAskedFor_PlaysSeenOrNot_ButNeverInACoopRoom()
        {
            var p = new Profile();
            foreach (var skit in Skits.All) p.MarkSeen(skit.key);
            SkitVisit v = Visit(Starters, p);
            v.Asked = "special-delivery";
            Assert.IsInstanceOf<SpecialDeliverySkit>(Skits.Due(p, v), "even with the first visit before it");
            v.Mode = DenMode.Coop;
            Assert.IsNull(Skits.Due(p, v));
        }

        [Test]
        public void EverySkit_IsSpokenByItsCast_InOrder_AndASkipEndsIt()
        {
            foreach (Skit skit in new Skit[] { new TwoOfUsSkit(), new SpecialDeliverySkit(), new TrollogramSkit() })
            {
                Assert.AreEqual(SkitPlace.Den, skit.Place);
                float last = 0f;
                foreach (SkitLine l in skit.Lines)
                {
                    CollectionAssert.Contains(skit.Cast, l.Who, skit.Key + ": " + l.Text);
                    Assert.GreaterOrEqual(l.From, last, skit.Key + ": lines in order, one at a time");
                    Assert.Greater(l.To, l.From);
                    Assert.LessOrEqual(l.To, skit.Length);
                    StringAssert.DoesNotContain("—", l.Text);
                    last = l.To;
                }
                Assert.AreEqual(1, skit.Shot);
                skit.Tick(0.5f);
                Assert.IsFalse(skit.Done);
                skit.Skip();
                Assert.IsTrue(skit.Done);
                Assert.AreEqual(skit.Shots.Length + 1, skit.Shot, "skipped to the last shot");
            }
        }

        [Test]
        public void TwoOfUsNow_TheControllerLeavesHisHand_AndTheButtonPopsUpWhereItLay()
        {
            var s = new TwoOfUsSkit();
            Step(s, TwoOfUsSkit.PickAt + 0.1f);
            Assert.IsTrue(s.Holding);
            Assert.IsFalse(s.ButtonUp);
            Step(s, TwoOfUsSkit.Shot2 + 0.1f);
            Assert.IsTrue(s.HoldingOut, "held out to us");
            Step(s, TwoOfUsSkit.TossAt + 0.1f);
            Assert.IsFalse(s.Holding, "tossed");
            Assert.That(s.Toss, Is.InRange(0f, 1f));
            Step(s, TwoOfUsSkit.ButtonAt + 0.3f);
            Assert.IsTrue(s.ButtonUp);
            Assert.Greater(s.ButtonScale, 1f, "one soft pulse");
            Step(s, TwoOfUsSkit.PulseTo + 0.1f);
            Assert.AreEqual(1f, s.ButtonScale, 1e-4f, "and it settles");
            Assert.AreEqual(0f, s.Glow, 1e-4f);
            Assert.IsTrue(s.Perched);
        }

        [Test]
        public void SpecialDelivery_TheDroneBringsKieInUnderTheDoor_AndTakesItselfBackOut()
        {
            var s = new SpecialDeliverySkit();
            Assert.AreEqual(0f, s.DoorLift);
            Assert.IsFalse(s.DroneIn);
            Assert.IsTrue(s.TvOff);
            Step(s, SpecialDeliverySkit.DoorUp);
            Assert.AreEqual(1f, s.DoorLift, 1e-3f, "the door's up before the drone comes");
            Step(s, SpecialDeliverySkit.FlyFrom + 0.05f);
            Assert.IsTrue(s.DroneIn && s.Hanging);
            Assert.That(s.Drone.x, Is.GreaterThan(DenMap.RollerX), "in under the roller door");
            Step(s, SpecialDeliverySkit.LowerTo);
            Assert.AreEqual(SpecialDeliverySkit.OverX, s.Drone.x, 1e-3f, "over the couch");
            Assert.AreEqual(SpecialDeliverySkit.LowY, s.Drone.y, 1e-3f, "and down");
            Step(s, SpecialDeliverySkit.LetGo + 0.05f);
            Assert.IsFalse(s.Hanging, "the hook lets go");
            Step(s, SpecialDeliverySkit.LeaveTo);
            Assert.IsFalse(s.DroneIn, "back out");
            Step(s, SpecialDeliverySkit.DoorDownTo);
            Assert.AreEqual(0f, s.DoorLift, 1e-3f, "and the door's down");
            Step(s, SpecialDeliverySkit.TvOnAt);
            Assert.IsTrue(s.RolledOver);
            Assert.IsFalse(s.Remote);
            Assert.IsFalse(s.TvOff, "the TV's back on");
        }

        [Test]
        public void ATrollogramOfAnything_HoldsItsSilence_AndKcajPocketsTheNote()
        {
            var s = new TrollogramSkit();
            Assert.AreEqual(TrollogramSkit.Look.Kie, s.Kcaj);
            bool sawProjector = false, sawBoard = false, upAndAcross = false;
            while (s.T < TrollogramSkit.Shot4)
            {
                s.Tick(Dt);
                if (!s.Silence) continue;
                Assert.IsNull(s.Line, "no bubbles in the silence");
                Assert.IsTrue(s.Slurping, "tones2 frozen mid-slurp");
                sawProjector |= s.Kcaj == TrollogramSkit.Look.Projector;
                sawBoard |= s.Kcaj == TrollogramSkit.Look.Board && sawProjector;
                upAndAcross |= s.Tones2LooksUp;
            }
            Assert.IsTrue(sawProjector && sawBoard, "the projector, then the board");
            Assert.AreEqual(TrollogramSkit.Look.Kie, s.Kcaj, "then back at kie");
            Assert.IsTrue(upAndAcross);
            Assert.IsFalse(s.Working, "the screwdriver's down");
            var notes = new List<int>();
            while (!s.Done)
            {
                s.Tick(Dt);
                if (notes.Count == 0 || notes[notes.Count - 1] != s.Note) notes.Add(s.Note);
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0 }, notes, "none, on the board, in his hand, folded, in his pocket");
        }

        static void Step(Skit s, float to)
        {
            while (s.T < to - 1e-5f) s.Tick(Bite(to - s.T));
        }

        static float Bite(float left) => left < Dt ? left : Dt;
    }
}
