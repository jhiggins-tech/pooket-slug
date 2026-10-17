using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class LogTests
    {
        [TearDown]
        public void ResetLevels()
        {
            foreach (Log log in Log.All) log.Level = LogLevel.Info;
        }

        [Test]
        public void ParseSpecReadsChannelsAndLevels()
        {
            var pairs = Log.ParseSpec("net:debug,game:warn");
            Assert.AreEqual(2, pairs.Count);
            Assert.AreEqual("net", pairs[0].channel);
            Assert.AreEqual(LogLevel.Debug, pairs[0].level);
            Assert.AreEqual("game", pairs[1].channel);
            Assert.AreEqual(LogLevel.Warn, pairs[1].level);
        }

        [Test]
        public void ParseSpecTrimsAndIgnoresCase()
        {
            var pairs = Log.ParseSpec(" NET : Debug ");
            Assert.AreEqual(1, pairs.Count);
            Assert.AreEqual("net", pairs[0].channel);
            Assert.AreEqual(LogLevel.Debug, pairs[0].level);
        }

        [TestCase("debug", LogLevel.Debug)]
        [TestCase("info", LogLevel.Info)]
        [TestCase("warn", LogLevel.Warn)]
        [TestCase("warning", LogLevel.Warn)]
        [TestCase("ERROR", LogLevel.Error)]
        [TestCase("off", LogLevel.Off)]
        [TestCase("none", LogLevel.Off)]
        public void ParseSpecKnowsEachLevelName(string text, LogLevel expected)
        {
            var pairs = Log.ParseSpec("net:" + text);
            Assert.AreEqual(1, pairs.Count);
            Assert.AreEqual(expected, pairs[0].level);
        }

        [TestCase((string)null)]
        [TestCase("")]
        [TestCase("net")]
        [TestCase(":debug")]
        [TestCase(" :debug")]
        [TestCase("net:loud")]
        public void ParseSpecSkipsMalformedEntries(string spec)
        {
            Assert.AreEqual(0, Log.ParseSpec(spec).Count);
        }

        [Test]
        public void ParseSpecKeepsTheEntriesAroundABadOne()
        {
            var pairs = Log.ParseSpec("net:debug,oops,game:error");
            Assert.AreEqual(2, pairs.Count);
            Assert.AreEqual("game", pairs[1].channel);
        }

        [Test]
        public void ConfigureSetsTheNamedChannel()
        {
            Assert.AreEqual(1, Log.Configure("net:debug"));
            Assert.AreEqual(LogLevel.Debug, Log.Net.Level);
            Assert.AreEqual(LogLevel.Info, Log.Game.Level);
        }

        [Test]
        public void ConfigureAllSetsEveryChannel()
        {
            Assert.AreEqual(Log.All.Count, Log.Configure("all:warn"));
            foreach (Log log in Log.All) Assert.AreEqual(LogLevel.Warn, log.Level, log.Name);
        }

        [Test]
        public void ConfigureIgnoresUnknownChannels()
        {
            Assert.AreEqual(0, Log.Configure("physics:debug"));
            foreach (Log log in Log.All) Assert.AreEqual(LogLevel.Info, log.Level, log.Name);
        }

        [Test]
        public void LaterEntriesWin()
        {
            Log.Configure("all:error,net:debug");
            Assert.AreEqual(LogLevel.Debug, Log.Net.Level);
            Assert.AreEqual(LogLevel.Error, Log.Game.Level);
        }

        [Test]
        public void AChannelReportsItsLevelAndAbove()
        {
            Log.Net.Level = LogLevel.Warn;
            Assert.IsFalse(Log.Net.IsEnabled(LogLevel.Debug));
            Assert.IsFalse(Log.Net.IsEnabled(LogLevel.Info));
            Assert.IsTrue(Log.Net.IsEnabled(LogLevel.Warn));
            Assert.IsTrue(Log.Net.IsEnabled(LogLevel.Error));
        }

        [Test]
        public void OffSilencesEverything()
        {
            Log.Net.Level = LogLevel.Off;
            Assert.IsFalse(Log.Net.IsEnabled(LogLevel.Error));
            Log.Net.Level = LogLevel.Debug;
            Assert.IsFalse(Log.Net.IsEnabled(LogLevel.Off));
        }
    }
}
