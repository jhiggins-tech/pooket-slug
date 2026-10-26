using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The shell's words that name a button elsewhere name the button as it reads.</summary>
    public class ScreenWordsTests
    {
        [Test]
        public void CoopLinesNameTheDensMultiPlayerButton()
        {
            Assert.AreEqual("MULTI-PLAYER", ScreenWords.CoopButton);
            StringAssert.Contains(ScreenWords.CoopButton, ScreenWords.CoopWaiting);
            StringAssert.Contains(ScreenWords.CoopButton, ScreenWords.CoopOpened);
            StringAssert.Contains("DEN", ScreenWords.CoopOpened);
        }

        [Test]
        public void TheButtonsFace_IsMultiOverPlayer_TheWordsItsNamedBy()
        {
            Assert.AreEqual(new[] { "MULTI", "PLAYER" }, ScreenWords.CoopButtonFace.Split('\n'));
            Assert.AreEqual(ScreenWords.CoopButton, ScreenWords.CoopButtonFace.Replace('\n', '-'));
        }

        [Test]
        public void NoLineSaysTheOldMateButton()
        {
            StringAssert.DoesNotContain("MATE", ScreenWords.CoopWaiting);
            StringAssert.DoesNotContain("MATE", ScreenWords.CoopOpened);
        }

        [Test]
        public void NoLineSaysTheOldCoopButtonOrTheMenu()
        {
            StringAssert.DoesNotContain("PRESS CO-OP", ScreenWords.CoopWaiting);
            StringAssert.DoesNotContain("MENU", ScreenWords.CoopOpened);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TheTouchHintNamesTheGoButtonItShows(bool fromDen)
        {
            StringAssert.EndsWith(ScreenWords.MissionGo(fromDen), ScreenWords.MissionHint(touch: true, fromDen));
        }

        [Test]
        public void FromTheDenTheGoButtonIsPick()
        {
            Assert.AreEqual("PICK", ScreenWords.MissionGo(fromDen: true));
            Assert.AreEqual("TAP A MISSION, THEN PICK", ScreenWords.MissionHint(touch: true, fromDen: true));
            Assert.AreEqual("LEFT / RIGHT PICKS A MISSION", ScreenWords.MissionHint(touch: false, fromDen: true));
        }

        [Test]
        public void TheKitKeyIsTheAbilityKey()
        {
            Assert.AreEqual("B", ScreenWords.KitKey);
        }
    }
}
