using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>A jingle's sound effect or its music sting, never both (<see cref="Jingles"/>).</summary>
    public class JinglesTests
    {
        [TestCase(Jingle.MissionStart)]
        [TestCase(Jingle.Victory)]
        [TestCase(Jingle.GameOver)]
        public void WithTheMusicOff_TheEffectPlays(Jingle jingle)
        {
            Assert.IsTrue(Jingles.EffectPlays(jingle, musicOn: false));
            Assert.IsTrue(Jingles.EffectPlays(jingle, musicOn: false, runEnds: false));
        }

        [TestCase(Jingle.MissionStart)]
        [TestCase(Jingle.Victory)]
        [TestCase(Jingle.GameOver)]
        public void WithTheMusicOn_TheStingPlaysInstead(Jingle jingle)
        {
            Assert.IsFalse(Jingles.EffectPlays(jingle, musicOn: true));
        }

        [Test]
        public void ACoopSoldierOutWhileThePartnerPlaysOn_StillGetsTheTrombone()
        {
            Assert.IsTrue(Jingles.EffectPlays(Jingle.GameOver, musicOn: true, runEnds: false), "no game over sting follows");
            Assert.IsFalse(Jingles.EffectPlays(Jingle.GameOver, musicOn: true, runEnds: true), "the last one out: the sting");
        }

        [TestCase(Jingle.MissionStart)]
        [TestCase(Jingle.Victory)]
        public void TheStartAndTheWin_AlwaysHaveTheirSting(Jingle jingle)
        {
            Assert.IsFalse(Jingles.EffectPlays(jingle, musicOn: true, runEnds: false), "runEnds is the game over's only");
        }
    }
}
