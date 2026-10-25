using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The baked Photon settings' rules: which region a build is in, and whether it can go online.</summary>
    public class NetConfigTests
    {
        [Test]
        public void TheDefaultRegionIsAustralia() => Assert.AreEqual("au", NetConfig.DefaultRegion);

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\n")]
        public void ABlankRegionIsTheDefault(string named) => Assert.AreEqual(NetConfig.DefaultRegion, NetConfig.Region(named));

        [TestCase("eu", "eu")]
        [TestCase(" EU ", "eu")]
        [TestCase("US", "us")]
        [TestCase("\tasia\n", "asia")]
        [TestCase("Au", "au")]
        public void ANamedRegionIsTrimmedAndLowerCase(string named, string region) => Assert.AreEqual(region, NetConfig.Region(named));

        [Test]
        public void DataReadsItsRegionByTheSameRule()
        {
            Assert.AreEqual("au", new NetConfig.Data().Region);
            Assert.AreEqual("au", new NetConfig.Data { region = " " }.Region);
            Assert.AreEqual("usw", new NetConfig.Data { region = " USW" }.Region);
        }

        [Test]
        public void OnlyAnAppIdMakesItConfigured()
        {
            Assert.IsFalse(new NetConfig.Data().Configured);
            Assert.IsFalse(new NetConfig.Data { appId = "", region = "eu" }.Configured);
            Assert.IsTrue(new NetConfig.Data { appId = "an-app-id" }.Configured);
        }
    }
}
