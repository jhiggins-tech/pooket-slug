using System;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class CrewFigureTests
    {
        [Test]
        public void EveryMoveHasItsClipAndEveryBodyFramePlacesTheHead()
        {
            foreach (CrewMove move in Enum.GetValues(typeof(CrewMove)))
                Assert.AreEqual(move, CrewFigure.Of(move).Move);
            foreach (CrewBody body in Enum.GetValues(typeof(CrewBody)))
                Assert.DoesNotThrow(() => CrewFigure.HeadOn(body), body.ToString());
            Assert.AreEqual(Enum.GetValues(typeof(CrewMove)).Length, CrewFigure.All.Length);
        }

        [Test]
        public void APosePlaysTheMoveItAlwaysShowed()
        {
            Assert.AreEqual(CrewMove.Down, CrewFigure.MoveOf(new AvatarPose { Dead = true, Grounded = true }));
            Assert.AreEqual(CrewMove.Jump, CrewFigure.MoveOf(new AvatarPose { Running = true }));
            Assert.AreEqual(CrewMove.Crouch, CrewFigure.MoveOf(new AvatarPose { Grounded = true, Crouching = true, Running = true }));
            Assert.AreEqual(CrewMove.Run, CrewFigure.MoveOf(new AvatarPose { Grounded = true, Running = true }));
            Assert.AreEqual(CrewMove.Idle, CrewFigure.MoveOf(new AvatarPose { Grounded = true }));
        }

        [Test]
        public void TheFigureDrawsEveryPoseAsTheSoldierDid()
        {
            // PlayerVisual's sums before the shared figure: legs, top, the bob and the crouch's drop, the hurt blink, the
            // flicker out and the topple
            var legsRun = new[] { CrewLegs.Run1, CrewLegs.Run2, CrewLegs.Run3, CrewLegs.Run4 };
            for (int bits = 0; bits < 1 << 9; bits++)
                for (int aimY = -1; aimY <= 1; aimY++)
                    for (float t = 0f; t < 2f; t += 0.07f)
                    {
                        var p = new AvatarPose
                        {
                            Facing = (bits & 1) != 0 ? 1 : -1, Grounded = (bits & 2) != 0, Crouching = (bits & 4) != 0,
                            Running = (bits & 8) != 0, Dead = (bits & 16) != 0, Muzzle = (bits & 32) != 0,
                            Blink = (bits & 64) != 0, Gone = (bits & 128) != 0, Diagonal = (bits & 256) != 0 && aimY != 0,
                            Flash = (bits & 64) != 0, AimY = aimY, Anim = t, DeadTime = t,
                        };
                        int runFrame = (int)(t * 12f) % 4;
                        CrewLegs legs = p.Dead || !p.Grounded ? CrewLegs.Jump : p.Crouching ? CrewLegs.Crouch
                            : p.Running ? legsRun[runFrame] : CrewLegs.Idle;
                        CrewBody body = p.AimY > 0 ? (p.Diagonal ? CrewBody.UpDiag : CrewBody.Up)
                            : p.AimY < 0 ? (p.Diagonal ? CrewBody.DownDiag : CrewBody.Down) : CrewBody.Forward;
                        float bob = p.Running && (runFrame == 0 || runFrame == 2) ? -1f : 0f;
                        float drop = p.Crouching ? -6f : bob;
                        float now = t * 1.3f;
                        bool hidden = (p.Blink && (int)(now * 20f) % 2 == 0) ||
                                      (p.Dead && p.DeadTime > 1.2f && (int)(p.DeadTime * 12f) % 2 == 0) || (p.Dead && p.Gone);
                        float tilt = p.Dead ? Math.Min(90f, p.DeadTime * 300f) * p.Facing : 0f;

                        CrewFrame f = CrewFigure.Frame(p, now);
                        string at = $"bits {bits} aim {aimY} t {t}";
                        Assert.AreEqual(legs, f.Legs, at);
                        Assert.AreEqual(body, f.Body, at);
                        Assert.AreEqual(drop, (float)f.Lift, at);
                        Assert.AreEqual(p.Muzzle && !p.Dead, f.Muzzle, at);
                        Assert.AreEqual(p.Flash, f.Flash, at);
                        Assert.AreEqual(hidden, f.Hidden, at);
                        Assert.AreEqual(tilt, f.Tilt, 1e-4f, at);
                    }
        }

        [Test]
        public void RepairingWorksAWrenchCrouchedWithTheHeadBowed()
        {
            CrewClip repair = CrewFigure.Repair;
            Assert.IsFalse(repair.Aims);
            Assert.AreEqual(4, repair.Clip.Frames);
            Assert.IsTrue(repair.Clip.Loop);
            Assert.AreEqual(-CrewFigure.CrouchDrop, repair.Lift(0.3f));
            for (float t = 0f; t < 2f; t += 0.05f)
            {
                Assert.AreEqual(CrewLegs.Crouch, repair.Legs(t));
                CrewBody body = repair.Body(t, CrewBody.Forward);
                Assert.AreEqual(CrewBody.Repair1 + (int)(t * 8f) % 4, body);
                Assert.AreEqual(CrewHead.Down, CrewFigure.HeadOn(body).Head);
            }
            Assert.AreEqual(CrewHead.Front, CrewFigure.HeadOn(CrewBody.Forward).Head);
        }

        [Test]
        public void AStageFigureCanRepair()
        {
            StagePose p = StagePose.Cutscene("repair", 0.2f, 10f);
            Assert.AreEqual(StageLegs.Crouch, p.Legs);
            Assert.AreEqual(StageTorso.Repair, p.Torso);
            Assert.AreEqual(-CrewFigure.CrouchDrop, p.TorsoY);
            Assert.AreEqual(CrewBody.Repair2, p.Body);
            Assert.AreEqual(CrewBody.Pad, new StagePose(StageLegs.Sit, StageTorso.Pad).Body);
            Assert.AreEqual(CrewBody.Forward, StagePose.Stand.Body);
            CollectionAssert.Contains(Staging.Poses, "repair");
        }

        // Does it do cones?: kie's grin falls, slowly, all the way down, and stays down
        [Test]
        public void AFaceFallsFromTheGrinToASlumpAndStaysThere()
        {
            CrewClip fall = CrewFigure.FaceFall;
            Assert.IsFalse(fall.Aims);
            Assert.IsFalse(fall.Clip.Loop);
            var seen = new System.Collections.Generic.List<CrewBody>();
            for (float t = 0f; t < 6f; t += 0.02f)
            {
                Assert.AreEqual(CrewLegs.Idle, fall.Legs(t));
                CrewBody body = fall.Body(t, CrewBody.Forward);
                if (seen.Count == 0 || seen[seen.Count - 1] != body) seen.Add(body);
            }
            CollectionAssert.AreEqual(new[] { CrewBody.Grin, CrewBody.Blank, CrewBody.Glum, CrewBody.Slump }, seen, "in order, once, held on the slump");
            Assert.Greater(fall.Clip.Length, 2f, "slowly");
            Assert.AreEqual(CrewHead.Grin, CrewFigure.HeadOn(CrewBody.Grin).Head);
            Assert.AreEqual(CrewHead.Blank, CrewFigure.HeadOn(CrewBody.Blank).Head);
            Assert.AreEqual(CrewHead.Glum, CrewFigure.HeadOn(CrewBody.Glum).Head);
            HeadPlace slump = CrewFigure.HeadOn(CrewBody.Slump);
            Assert.AreEqual(CrewHead.Glum, slump.Head);
            Assert.AreEqual(1, slump.Y, "a row lower, on the lower shoulders");
        }

        [Test]
        public void AStageFigureGrinsFallsAndSlumps()
        {
            Assert.AreEqual(CrewBody.Grin, StagePose.Cutscene("grin", 3f, 10f).Body);
            Assert.AreEqual(CrewBody.Grin, StagePose.Cutscene("fall", 3f, 10f, since: 0f).Body, "the fall starts from the grin, whenever in the shot it's struck");
            Assert.AreEqual(CrewBody.Slump, StagePose.Cutscene("fall", 0.1f, 10f, since: 9f).Body, "and ends slumped");
            Assert.AreEqual(CrewBody.Slump, StagePose.Cutscene("glum", 0f, 10f).Body);
            StagePose away = StagePose.Glum(true, 0.3f, 6f);
            Assert.AreEqual(StageLegs.Walk, away.Legs, "walking off glum");
            Assert.AreEqual(CrewBody.Slump, away.Body);
            Assert.AreEqual(StageLegs.Walk, StagePose.Grinning(true, 0.3f).Legs, "running in grinning");
            foreach (string pose in new[] { "grin", "fall", "glum" })
            {
                CollectionAssert.Contains(Staging.Poses, pose);
                Assert.AreEqual(StageLegs.Idle, StagePose.Cutscene(pose, 1f, 10f).Legs, pose + " stands");
            }
        }

        [Test]
        public void TheLastInlineBlinksKeepTheirBeat()
        {
            for (float t = 0f; t < 4f; t += 0.011f)
            {
                Assert.AreEqual((int)(t * BossIntroShow.FlashRate * 2f) % 2 == 0, Fx.Blink(t, BossIntroShow.FlashRate * 2f));
                Assert.AreEqual((int)(t * 5f) % 3 != 2, Fx.Step(t, 5f, 3) != 2);
                Assert.AreEqual((int)(t * 20f) % 2 == 0, Fx.SceneShake.On(t));
            }
        }
    }
}
