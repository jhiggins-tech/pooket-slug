using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class AvatarPoseTests
    {
        [Test]
        public void EveryPoseSurvivesPacking()
        {
            foreach (int facing in new[] { -1, 1 })
                foreach (int aim in new[] { -1, 0, 1 })
                    for (int bits = 0; bits < 128; bits++)
                    {
                        var pose = new AvatarPose
                        {
                            Facing = facing,
                            AimY = aim,
                            Grounded = (bits & 1) != 0,
                            Crouching = (bits & 2) != 0,
                            Running = (bits & 4) != 0,
                            Dead = (bits & 8) != 0,
                            Muzzle = (bits & 16) != 0,
                            Blink = (bits & 32) != 0,
                            Flash = (bits & 64) != 0,
                        };
                        Assert.AreEqual(pose, AvatarPose.Unpack(pose.Pack(), 0f, 0f), $"facing {facing}, aim {aim}, bits {bits}");
                    }
        }

        [Test]
        public void EmptyFlagsAreAStillSoldierFacingLeft()
        {
            AvatarPose pose = AvatarPose.Unpack(0, 0f, 0f);
            Assert.AreEqual(-1, pose.Facing);
            Assert.AreEqual(0, pose.AimY);
            Assert.IsFalse(pose.Grounded || pose.Crouching || pose.Running || pose.Dead || pose.Muzzle || pose.Blink || pose.Flash);
        }

        [Test]
        public void UnpackKeepsTheLocalAnimationClocks()
        {
            AvatarPose pose = AvatarPose.Unpack(new AvatarPose { Facing = 1, Dead = true }.Pack(), 1.5f, 2.5f);
            Assert.AreEqual(1.5f, pose.Anim);
            Assert.AreEqual(2.5f, pose.DeadTime);
        }

        [Test]
        public void ThePackedPoseFitsInTenBits()
        {
            var all = new AvatarPose
            {
                Facing = 1, AimY = 1, Grounded = true, Crouching = true, Running = true,
                Dead = true, Muzzle = true, Blink = true, Flash = true,
            };
            Assert.Less(all.Pack() | new AvatarPose { AimY = -1 }.Pack(), 1 << 10);
        }
    }
}
