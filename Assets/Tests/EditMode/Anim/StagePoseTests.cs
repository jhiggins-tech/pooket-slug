using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class StagePoseTests
    {
        [Test]
        public void CutscenePosesKeepTheirNames()
        {
            Assert.AreEqual(StageLegs.Walk, StagePose.Cutscene("run", 0f, 10f).Legs);
            Assert.AreEqual(StageLegs.Crouch, StagePose.Cutscene("crouch", 0f, 10f).Legs);
            Assert.AreEqual(StageLegs.Hidden, StagePose.Cutscene("hatch", 0f, 10f).Legs);
            StagePose spray = StagePose.Cutscene("spray", 0f, 10f);
            Assert.AreEqual(StageLegs.Idle, spray.Legs);
            Assert.AreEqual(-1f, spray.TorsoX);
            StagePose stand = StagePose.Cutscene("stand", 0f, 10f);
            Assert.AreEqual(StageLegs.Idle, stand.Legs);
            Assert.AreEqual(StageTorso.Forward, stand.Torso);
            Assert.AreEqual(0f, stand.TorsoX);
        }

        [Test]
        public void AWalkStepsAsTheScreensDid()
        {
            // the den, the bay and the beats: (int)(clock * 10) % frames; a cutscene at its own rate
            for (float t = 0f; t < 3f; t += 0.013f)
            {
                Assert.AreEqual((int)(t * 10f) % 4, StagePose.Walking(true, t).WalkFrame(4));
                Assert.AreEqual((int)(t * 7f) % 6, StagePose.Cutscene("run", t, 7f).WalkFrame(6));
            }
            Assert.AreEqual(StageLegs.Idle, StagePose.Walking(false, 1f).Legs);
            Assert.AreEqual(StageTorso.Unarmed, StagePose.Walking(true, 1f).Torso);
        }

        [Test]
        public void StandingFacesTheCamera()
        {
            Assert.AreEqual(StageLegs.Idle, StagePose.Stand.Legs);
            Assert.AreEqual(StageTorso.Forward, StagePose.Stand.Torso);
        }

        // the bunker's rope climbs in the den's ladder pose: legs bent round it, the top reaching up
        [Test]
        public void Climbing_IsTheDensLadderPose()
        {
            StagePose climb = StagePose.Climbing(1.5f);
            Assert.AreEqual(StageLegs.Jump, climb.Legs);
            Assert.AreEqual(StageTorso.Lounge, climb.Torso);
            Assert.AreEqual(1.5f, climb.WalkT, 0.001f, "the hand-over-hand's clock");
        }
    }
}
