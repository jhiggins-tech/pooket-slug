using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The one co-op room: its name, the <c>?room=</c> override and the invite link.</summary>
    public class CoopRoomTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("-EU")]
        public void CoOpJoinsTheMatesRoom(string flag)
        {
            Assert.AreEqual(CoopRoom.Mates, CoopRoom.NameFor(flag));
        }

        [TestCase("ABCD", "ABCD")]
        [TestCase("abcd", "ABCD")]
        [TestCase("  WXYZ  ", "WXYZ")]
        [TestCase("ABCD-EU", "ABCD")]
        [TestCase("abcd - us", "ABCD")]
        [TestCase("mates", "MATES")]
        public void ARoomFlagNamesAnotherRoomAndAnOldRegionIsIgnored(string flag, string name)
        {
            Assert.AreEqual(name, CoopRoom.NameFor(flag));
        }

        [Test]
        public void TheInviteLinkIsJustThePageForTheMatesRoom()
        {
            const string page = "https://jhiggins-tech.github.io/pooket-slug/";
            Assert.AreEqual(page, CoopRoom.Link(page, CoopRoom.Mates));
            Assert.AreEqual(page + "?room=ABCD", CoopRoom.Link(page, "ABCD"), "another room's link names it");
            Assert.AreEqual("HCNW", CoopRoom.NameFor(PageQuery.Get(CoopRoom.Link("https://x.test/", "HCNW"), "room")), "opening it reads the room back");
            Assert.IsNull(CoopRoom.Link("", "ABCD"), "no page address (the editor): no link");
        }
    }
}
