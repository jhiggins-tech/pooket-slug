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

        [TestCase("ABCD-EU", "ABCD", "eu")]
        [TestCase("abcd-eu", "ABCD", "eu")]
        [TestCase("  WXYZ-AU  ", "WXYZ", "au")]
        [TestCase("ABCD - US", "ABCD", "us")]
        [TestCase("ABCD", "ABCD", null)]
        [TestCase("ABCD-", "ABCD", null)]
        public void TryParseSplitsTheNameAndRegion(string code, string name, string region)
        {
            Assert.IsTrue(InviteCode.TryParse(code, out string parsedName, out string parsedRegion));
            Assert.AreEqual(name, parsedName);
            Assert.AreEqual(region, parsedRegion);
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("-EU")]
        public void TryParseNeedsAName(string code)
        {
            Assert.IsFalse(InviteCode.TryParse(code, out _, out _));
        }

        [TestCase("ABCD", "eu", "ABCD-EU")]
        [TestCase("ABCD", null, "ABCD")]
        [TestCase("ABCD", "", "ABCD")]
        public void FormatJoinsTheNameAndRegion(string name, string region, string expected)
        {
            Assert.AreEqual(expected, InviteCode.Format(name, region));
        }

        [Test]
        public void AFormattedCodeParsesBack()
        {
            string code = InviteCode.Format(InviteCode.NewName(new Rng(42)), "au");
            Assert.AreEqual("AEDJ-AU", code);
            Assert.IsTrue(InviteCode.TryParse(code, out string name, out string region));
            Assert.AreEqual(code, InviteCode.Format(name, region));
        }
    }
}
