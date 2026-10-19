using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The den's beats: the first visit once ever, a move-in for each crewmate freed after it, none in co-op.</summary>
    public class DenBeatsTests
    {
        static readonly SoldierDef[] Starters = { Content.Tones2, Content.Kie };
        static readonly SoldierDef[] WithKcaj = { Content.Tones2, Content.Kie, Content.Kcaj };

        [Test]
        public void TheFirstVisit_PlaysOnce_AndTheStartersNeedNoMoveIn()
        {
            var p = new Profile();
            List<DenBeat> due = DenBeats.Due(p, Starters, DenMode.Solo);
            Assert.AreEqual(1, due.Count);
            Assert.AreEqual(DenBeatKind.FirstVisit, due[0].Kind);
            Assert.AreEqual(Content.Kie, due[0].Speaker);
            Assert.AreEqual("HOME SWEET HOME.", due[0].Line);
            Assert.IsTrue(DenBeats.Played(p, due[0]));
            Assert.IsEmpty(DenBeats.Due(p, Starters, DenMode.Solo), "once ever");
            Assert.IsEmpty(DenBeats.Due(p, Starters, DenMode.Crew));
        }

        [Test]
        public void ACrewmateFreedLater_MovesIn_Once()
        {
            var p = new Profile();
            DenBeats.Played(p, DenBeat.FirstVisit());
            List<DenBeat> due = DenBeats.Due(p, WithKcaj, DenMode.Solo);
            Assert.AreEqual(1, due.Count);
            Assert.AreEqual(DenBeatKind.MoveIn, due[0].Kind);
            Assert.AreEqual(Content.Kcaj, due[0].Newcomer);
            Assert.AreEqual(Content.Tones2, due[0].Speaker, "tones2 says hi from by the couch");
            DenBeats.Played(p, due[0]);
            Assert.IsEmpty(DenBeats.Due(p, WithKcaj, DenMode.Solo));
        }

        [Test]
        public void BothCanBeDue_FirstVisitFirst_AndNoneInCoop()
        {
            var p = new Profile();
            List<DenBeat> due = DenBeats.Due(p, WithKcaj, DenMode.Solo);
            Assert.AreEqual(2, due.Count);
            Assert.AreEqual(DenBeatKind.FirstVisit, due[0].Kind);
            Assert.AreEqual(DenBeatKind.MoveIn, due[1].Kind);
            Assert.IsEmpty(DenBeats.Due(p, WithKcaj, DenMode.Coop), "the two phones would play them apart");
        }

        [Test]
        public void TheFirstVisit_WarmsTheRoomAfterTheCouchAndTheTv_ThenKieSpeaks()
        {
            DenBeat b = DenBeat.FirstVisit();
            Assert.AreEqual(DenBeat.FirstVisitSeconds, b.Length);
            Assert.AreEqual(0f, b.Warm);
            Step(b, DenBeat.PlugTo);
            Assert.AreEqual(1f, b.Heave);
            Assert.AreEqual(1f, b.Plug, 1e-4f);
            Assert.That(b.Warm, Is.LessThan(0.5f), "still mostly dark");
            Assert.IsFalse(b.Speaking);
            Step(b, DenBeat.WarmTo - DenBeat.PlugTo);
            Assert.AreEqual(1f, b.Warm, 1e-3f);
            Assert.IsTrue(b.Speaking);
            Step(b, 2f);
            Assert.IsTrue(b.Done);
        }

        [Test]
        public void MovingIn_TheDoorGoesUp_TheyComeIn_TheCornerLights_ThenTheDoorComesDown()
        {
            DenBeat b = DenBeat.MoveIn(Content.Kcaj);
            Assert.AreEqual(DenBeat.MoveInSeconds, b.Length);
            Assert.AreEqual(0f, b.DoorLift);
            Assert.AreEqual(1f, b.Through, "behind the door");
            Step(b, DenBeat.UpTo);
            Assert.AreEqual(1f, b.DoorLift, 1e-3f);
            Step(b, DenBeat.StepTo - DenBeat.UpTo);
            Assert.AreEqual(0f, b.Through, 1e-3f);
            Assert.AreEqual(0f, b.Lit);
            Step(b, DenBeat.LitTo - DenBeat.StepTo);
            Assert.AreEqual(1f, b.Lit, 1e-3f);
            Assert.IsTrue(b.Speaking);
            Assert.AreEqual(1f, b.DoorLift, "still up as they walk in");
            Step(b, DenBeat.MoveInSeconds - DenBeat.LitTo);
            Assert.AreEqual(0f, b.DoorLift, 1e-3f);
            Assert.AreEqual(1f, b.Walked);
            Assert.IsTrue(b.Done);
            Assert.AreEqual(1f, b.Warm, "the room's already warm");
        }

        [Test]
        public void Skipping_EndsIt()
        {
            DenBeat b = DenBeat.MoveIn(Content.Kcaj);
            b.Tick(0.5f);
            b.Skip();
            Assert.IsTrue(b.Done);
        }

        static void Step(DenBeat b, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += 1f / 60f) b.Tick(System.Math.Min(1f / 60f, seconds - t));
        }
    }
}
