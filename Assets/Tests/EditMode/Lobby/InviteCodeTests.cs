using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class InviteCodeTests
    {
        [Test]
        public void ANewNameIsFourAllowedLetters()
        {
            var rng = new Rng(3);
            for (int i = 0; i < 200; i++)
            {
                string name = InviteCode.NewName(rng);
                Assert.AreEqual(InviteCode.NameLength, name.Length);
                foreach (char c in name) StringAssert.Contains(c.ToString(), InviteCode.Letters);
            }
        }

        [Test]
        public void NamesNeverUseIOrO()
        {
            StringAssert.DoesNotContain("I", InviteCode.Letters);
            StringAssert.DoesNotContain("O", InviteCode.Letters);
        }

        [Test]
        public void ANewNameFollowsTheSeed()
        {
            Assert.AreEqual("RHVX", InviteCode.NewName(new Rng(7)));
        }

        [TestCase("ABCD", "ABCD")]
        [TestCase("abcd", "ABCD")]
        [TestCase("  WXYZ  ", "WXYZ")]
        [TestCase("ABCD-EU", "ABCD")]
        [TestCase("abcd - us", "ABCD")]
        [TestCase("ABCD-", "ABCD")]
        public void TryParseReadsTheNameAndIgnoresAnOldRegion(string code, string name)
        {
            Assert.IsTrue(InviteCode.TryParse(code, out string parsed));
            Assert.AreEqual(name, parsed);
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("-EU")]
        public void TryParseNeedsAName(string code)
        {
            Assert.IsFalse(InviteCode.TryParse(code, out _));
        }

        [Test]
        public void ANewNameIsItsOwnCode()
        {
            string name = InviteCode.NewName(new Rng(42));
            Assert.AreEqual("AEDJ", name);
            Assert.IsTrue(InviteCode.TryParse(name, out string parsed));
            Assert.AreEqual(name, parsed);
        }

        [Test]
        public void TheInviteLinkIsThePageWithTheRoom()
        {
            Assert.AreEqual("https://jhiggins-tech.github.io/pooket-slug/?room=ABCD",
                InviteCode.Link("https://jhiggins-tech.github.io/pooket-slug/", "ABCD"));
            Assert.IsTrue(InviteCode.TryParse(PageQuery.Get(InviteCode.Link("https://x.test/", "HCNW"), "room"), out string name));
            Assert.AreEqual("HCNW", name, "opening the link reads the code back");
            Assert.IsNull(InviteCode.Link("", "ABCD"), "no page address (the editor): no link");
            Assert.IsNull(InviteCode.Link("https://x.test/", null));
        }
    }
}
