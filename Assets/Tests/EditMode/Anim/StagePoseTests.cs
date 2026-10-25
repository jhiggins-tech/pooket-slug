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
            Assert.AreEqual(0f, stand.Tilt, "upright");
        }

        // freeing kie: he drops in a heap. The crew's down frame (legs kicked up, the body), toppled flat, no art of its own
        [Test]
        public void Down_IsTheCrewsDownFrame_ToppledFlat()
        {
            StagePose down = StagePose.Cutscene("down", 0.4f, 10f);
            Assert.AreEqual(CrewLegs.Jump, CrewFigure.Down.Legs(0f), "the game's down legs, drawn by StageLegs.Jump");
            Assert.AreEqual(StageLegs.Jump, down.Legs);
            Assert.AreEqual(CrewBody.Forward, down.Body);
            Assert.AreEqual(90f, down.Tilt, "flat on his back");
            Assert.AreEqual(CrewFigure.DownTilt(10f), down.Tilt, "as far as the game's down topples");
            foreach (string pose in new[] { "idle", "run", "crouch", "spray", "hatch", "repair" })
                Assert.AreEqual(0f, StagePose.Cutscene(pose, 0.4f, 10f).Tilt, pose + " stands up");
        }

        // stage-no-gun: a cutscene soldier with their hands full, or by their sides, has no gun; the shared body, so anyone
        [Test]
        public void Arms_TakeTheGunOutOfTheirHands_WhateverThePose()
        {
            Assert.AreEqual(StageTorso.Forward, StagePose.Arms("gun"));
            Assert.AreEqual(StageTorso.Unarmed, StagePose.Arms("down"), "by their sides: the den's off-duty top");
            Assert.AreEqual(StageTorso.Carry, StagePose.Arms("out"));
            Assert.AreEqual(CrewBody.Carry, new StagePose(StageLegs.Idle, StageTorso.Carry).Body);
            Assert.AreEqual(CrewBody.Forward, StagePose.Cutscene("idle", 0f, 10f).Body, "the gun unless the scene says");
            foreach (string pose in new[] { "idle", "run", "crouch", "hatch", "down" })
            {
                Assert.AreEqual(CrewBody.Carry, StagePose.Cutscene(pose, 0.4f, 10f, "out").Body, pose);
                Assert.AreEqual(CrewBody.Unarmed, StagePose.Cutscene(pose, 0.4f, 10f, "down").Body, pose);
                Assert.AreEqual(StagePose.Cutscene(pose, 0.4f, 10f).Legs, StagePose.Cutscene(pose, 0.4f, 10f, "out").Legs, pose + "'s legs");
            }
            Assert.AreEqual(90f, StagePose.Cutscene("down", 0.4f, 10f, "down").Tilt, "still in a heap");
            Assert.AreEqual(CrewBody.Forward, StagePose.Cutscene("spray", 0f, 10f, "out").Body, "the spray is the gun's");
            Assert.AreEqual(StageTorso.Repair, StagePose.Cutscene("repair", 0f, 10f, "down").Torso, "a repair keeps its wrench");
            Assert.AreEqual(CrewHead.Front, CrewFigure.HeadOn(CrewBody.Carry).Head);
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
