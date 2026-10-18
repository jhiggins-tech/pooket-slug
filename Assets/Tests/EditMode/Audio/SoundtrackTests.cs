using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class SoundtrackTests
    {
        [Test]
        public void AContinueTicksDownThenTheMusicComesBack()
        {
            var s = new Soundtrack();
            Assert.AreEqual(MusicCue.None, s.Frame(false, false, false, false));
            Assert.AreEqual(MusicCue.Countdown, s.Frame(true, false, false, false));
            Assert.AreEqual(MusicCue.None, s.Frame(true, false, false, false), "once, not every frame");
            Assert.AreEqual(MusicCue.Resume, s.Frame(false, false, false, false), "the continue's taken");
            Assert.AreEqual(MusicCue.None, s.Frame(false, false, false, false));
        }

        [Test]
        public void AContinueLetGo_LeavesItToTheGameOver()
        {
            var s = new Soundtrack();
            s.Frame(true, false, false, false);
            Assert.AreEqual(MusicCue.None, s.Frame(false, true, false, false));
        }

        [Test]
        public void TheGlitchOutCutsOnce_AndTheCodaCanCutAgain()
        {
            var s = new Soundtrack();
            Assert.AreEqual(MusicCue.Cut, s.Frame(false, false, true, false));
            Assert.AreEqual(MusicCue.None, s.Frame(false, false, true, false));
            Assert.AreEqual(MusicCue.None, s.Frame(false, false, false, false), "the coda opens: the coda's own music starts it");
            Assert.AreEqual(MusicCue.Cut, s.Frame(false, false, true, false));
        }

        [Test]
        public void WinningPlaysTheStingOnce_WhateverElseIsGoingOn()
        {
            var s = new Soundtrack();
            s.Frame(true, false, false, false);
            Assert.AreEqual(MusicCue.Complete, s.Frame(true, false, true, true));
            Assert.AreEqual(MusicCue.None, s.Frame(false, false, false, true));
        }
    }
}
