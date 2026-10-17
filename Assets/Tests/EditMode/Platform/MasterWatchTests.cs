using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>Rooms: taking the shared world over from a host whose game has stopped (switched away).</summary>
    public class MasterWatchTests
    {
        const float Frame = 1f / 30f;

        /// <summary>Runs the watch for <paramref name="seconds"/> with the master's beat held; returns how many times it asked.</summary>
        static int Run(MasterWatch w, float seconds, int master, int beat, bool canTakeOver = true)
        {
            int asks = 0;
            for (float t = 0f; t < seconds; t += Frame)
                if (w.Tick(Frame, master, beat, canTakeOver)) asks++;
            return asks;
        }

        [Test]
        public void AMasterStillBeatingIsLeftAlone()
        {
            var w = new MasterWatch();
            int asks = 0;
            for (float t = 0f; t < 20f; t += Frame)
                if (w.Tick(Frame, 1, PartnerBeacon.BeatAt(t), true)) asks++;
            Assert.AreEqual(0, asks);
        }

        [Test]
        public void AQuietMasterIsAskedForAfterPhotonsThreshold()
        {
            var w = new MasterWatch();
            Assert.AreEqual(0, Run(w, MasterWatch.QuietAfter - 0.1f, 1, 2), "not before the threshold");
            Assert.AreEqual(1, Run(w, 0.2f, 1, 2), "once it's passed");
        }

        [Test]
        public void ItAsksAgainWhileTheMasterStaysQuiet()
        {
            var w = new MasterWatch();
            int asks = Run(w, MasterWatch.QuietAfter + 3 * MasterWatch.RetryEvery + 0.3f, 1, 2);
            Assert.AreEqual(4, asks, "the first ask, then one every RetryEvery");
        }

        [Test]
        public void ANewMasterOrABeatStartsTheWaitAgain()
        {
            var w = new MasterWatch();
            Run(w, MasterWatch.QuietAfter - 0.5f, 1, 2);
            Assert.AreEqual(0, Run(w, MasterWatch.QuietAfter - 0.1f, 1, 3), "their beat moved on");
            Assert.AreEqual(0, Run(w, MasterWatch.QuietAfter - 0.1f, 2, 3), "someone else is the master now");
        }

        [Test]
        public void OnlyAPlayerWhoIsntTheMasterAsks()
        {
            var w = new MasterWatch();
            Assert.AreEqual(0, Run(w, 10f, 1, 2, canTakeOver: false));
            Assert.AreEqual(1, Run(w, Frame * 1.5f, 1, 2), "asks at once when it may, the master being long quiet");
        }

        [Test]
        public void OurOwnStalledFrameDoesntCountAsTheMasterGoingQuiet()
        {
            var w = new MasterWatch();
            w.Tick(Frame, 1, 2, true);
            Assert.IsFalse(w.Tick(5f, 1, 2, true), "a five-second frame of ours (loading) counts as half a second");
            Assert.AreEqual(MasterWatch.LongestFrame, w.Quiet, 1e-4f);
        }
    }
}
