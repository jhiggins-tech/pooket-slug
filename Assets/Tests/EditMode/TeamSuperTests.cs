using NUnit.Framework;
using UnityEngine;

namespace PooketSlug.Tests
{
    public class TeamSuperTests
    {
        static TeamSuperSpec Spec => TeamSupers.Spec;

        [Test]
        public void Tones2AndKieAreSewerRatsEitherWayRound()
        {
            Assert.AreSame(TeamSupers.SewerRats, TeamSupers.For(Content.Tones2, Content.Kie));
            Assert.AreSame(TeamSupers.SewerRats, TeamSupers.For(Content.Kie, Content.Tones2));
            Assert.AreEqual("SEWER RATS", TeamSupers.SewerRats.Name);
            Assert.AreSame(Content.Kie, TeamSupers.SewerRats.Other(Content.Tones2));
            Assert.AreSame(TeamSupers.SewerRats, TeamSupers.ById(TeamSupers.SewerRats.Id));
        }

        [Test]
        public void NoComboNoTeamSuper()
        {
            Assert.IsNull(TeamSupers.For(Content.Kie, Content.Kie), "the same soldier twice");
            Assert.IsNull(TeamSupers.For(Content.Tones2, Content.Trooper), "a pair that isn't built");
            Assert.IsNull(TeamSupers.For(Content.Tones2, null), "nobody to team with");
            Assert.IsNull(TeamSupers.ById(0));
        }

        [TestCase(true, true, true, false, true)]
        [TestCase(false, true, true, false, false)]
        [TestCase(true, false, true, false, false)]
        [TestCase(true, true, false, false, false)]
        [TestCase(true, true, true, true, false)]
        public void TeamShowsWhenBothSupersAreCharged(bool mine, bool partner, bool combo, bool playing, bool shows)
        {
            Assert.AreEqual(shows, TeamSupers.CanFire(mine, partner, combo ? TeamSupers.SewerRats : null, playing));
        }

        [Test]
        public void ItsShortAndEndsWithEverythingDead()
        {
            Assert.That(Spec.Length, Is.InRange(2f, 3.6f), "two or three seconds, and the card");
            Assert.AreEqual(TeamSuperSpec.Stage.Card, Spec.At(0f));
            Assert.AreEqual(TeamSuperSpec.Stage.Flood, Spec.At(Spec.Card + 0.01f));
            Assert.AreEqual(TeamSuperSpec.Stage.Surf, Spec.At(Spec.KillAt - 0.01f));
            Assert.AreEqual(TeamSuperSpec.Stage.Pop, Spec.At(Spec.KillAt));
            Assert.AreEqual(TeamSuperSpec.Stage.Done, Spec.At(Spec.Length));
        }

        [Test]
        public void TheFloodRisesHoldsAndDrains()
        {
            Assert.AreEqual(0f, Spec.FloodLevel(Spec.Card * 0.5f), "not during the card");
            Assert.AreEqual(1f, Spec.FloodLevel(Spec.KillAt - 0.01f), 1e-4f);
            Assert.AreEqual(0f, Spec.FloodLevel(Spec.Length), 1e-4f);
            Assert.AreEqual(0f, Spec.SurfProgress(Spec.Card));
            Assert.AreEqual(1f, Spec.SurfProgress(Spec.KillAt), 1e-4f, "across by the time everything pops");
        }

        [TestCase(1200, 1200, 360)]
        [TestCase(200, 1200, 199)]
        [TestCase(1, 40, 0)]
        public void ABossTakesABigHitButLives(int hp, int max, int damage)
        {
            Assert.AreEqual(damage, Spec.BossDamage(hp, max));
        }

        [Test]
        public void TheSuperReadyFlagReachesThePartner()
        {
            var pose = new AvatarPose { Facing = 1, SuperReady = true, Grounded = true };
            Assert.IsTrue(AvatarPose.Unpack(pose.Pack(), 0f, 0f).SuperReady);
            pose.SuperReady = false;
            Assert.IsFalse(AvatarPose.Unpack(pose.Pack(), 0f, 0f).SuperReady);
        }

        [Test]
        public void TheTeamButtonOnlyShowsWhenItCanBeFired()
        {
            var pad = new ArcadePad { Visible = true };
            Assert.IsFalse(pad.Shown(ArcadePad.Button.Team));
            pad.TeamVisible = true;
            Assert.IsTrue(pad.Shown(ArcadePad.Button.Team));
            Vector2 at = pad.Center(ArcadePad.Button.Team);
            var input = new SoldierInput();
            pad.Read(new[] { new ArcadePad.Finger(3, at, true) }, ref input, out _);
            Assert.IsTrue(input.TeamPressed && !input.SuperPressed);
        }
    }
}
