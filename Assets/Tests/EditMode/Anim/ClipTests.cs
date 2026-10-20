using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class ClipTests
    {
        [Test]
        public void ALoopingClipGoesRound()
        {
            var run = new Clip("run", 4, 12f);
            Assert.AreEqual(0, run.Frame(0f));
            Assert.AreEqual(1, run.Frame(1f / 12f + 1e-4f));
            Assert.AreEqual(3, run.Frame(3.5f / 12f));
            Assert.AreEqual(0, run.Frame(4.5f / 12f));
            Assert.IsFalse(run.Done(100f));
        }

        [Test]
        public void AOneShotClipHoldsItsLastFrame()
        {
            var pop = new Clip("pop", 3, 10f, loop: false);
            Assert.AreEqual(2, pop.Frame(0.25f));
            Assert.AreEqual(2, pop.Frame(9f));
            Assert.IsFalse(pop.Done(0.25f));
            Assert.IsTrue(pop.Done(0.31f));
            Assert.AreEqual(0.3f, pop.Length, 1e-5f);
        }

        [Test]
        public void ItPlaysTheSameFramesAsTheSumsItReplaced()
        {
            // what the sites did before the clips: (int)(t * fps) % frames
            foreach (Clip c in new[] { Clips.SoldierRun, Clips.RiflemanRun, Clips.RunnerRun, Clips.CreameryRun, Clips.StageWalk })
                for (float t = 0f; t < 5f; t += 0.0137f)
                    Assert.AreEqual((int)(t * c.Fps) % 4, c.Frame(t), $"{c.Name} at {c.Fps} fps, {t}s");
        }

        [Test]
        public void ItPicksFromAnArtsOwnFrames()
        {
            var walk = new Clip("walk", 4, 10f);
            string[] six = { "a", "b", "c", "d", "e", "f" };
            Assert.AreEqual("f", walk.Pick(six, 0.55f));
            Assert.AreEqual("a", walk.Pick(six, 0.65f));
        }

        [Test]
        public void EventsFireOnceAsTheirFrameIsReached()
        {
            var step = new Clip("run", 4, 10f, events: new Dictionary<int, string> { [0] = "left foot", [2] = "right foot" });
            CollectionAssert.AreEqual(new[] { "left foot" }, step.Between(-1f, 0f).ToArray());
            CollectionAssert.AreEqual(new string[0], step.Between(0f, 0.15f).ToArray());
            CollectionAssert.AreEqual(new[] { "right foot" }, step.Between(0.15f, 0.25f).ToArray());
            CollectionAssert.AreEqual(new[] { "left foot", "right foot" }, step.Between(0.25f, 0.65f).ToArray());
        }

        [Test]
        public void AOneShotsEventsStopAtItsEnd()
        {
            var bang = new Clip("bang", 2, 10f, loop: false, events: new Dictionary<int, string> { [1] = "boom" });
            CollectionAssert.AreEqual(new[] { "boom" }, bang.Between(0f, 5f).ToArray());
            CollectionAssert.AreEqual(new string[0], bang.Between(0.15f, 5f).ToArray());
        }
    }

    public class FxTests
    {
        [Test]
        public void APulseStepsAsTheInlineSumsDid()
        {
            foreach (Pulse p in new[] { Fx.TrolloFlicker, Fx.TrolloJitter, Fx.HurtBlink, Fx.Rotor, Fx.Warning, new Pulse(11f, 17) })
                for (float t = 0f; t < 4f; t += 0.0071f)
                {
                    Assert.AreEqual((int)(t * p.Hz) % p.Of, p.Step(t));
                    Assert.AreEqual((int)(t * p.Hz + 5) % p.Of, p.Step(t, 5));
                    Assert.AreEqual((int)(t * p.Hz) % p.Of == 0, p.On(t));
                }
        }

        [Test]
        public void ABlinkIsOnHalfTheTime()
        {
            int on = 0;
            for (int i = 0; i < 1000; i++) if (Fx.Blink(i / 1000f, 10f)) on++;
            Assert.AreEqual(500, on, 10);
            Assert.IsTrue(Fx.Blink(0f, 10f));
            Assert.IsFalse(Fx.Blink(0.15f, 10f));
        }

        [Test]
        public void AShiverGoesOneWayThenTheOther()
        {
            Assert.AreEqual(0.5f, Fx.Shiver(0f, 6f, 0.5f));
            Assert.AreEqual(-0.5f, Fx.Shiver(0.2f, 6f, 0.5f));
            Assert.AreEqual(0.6f, new Pulse(30f).Shiver(0f, 0.6f));
        }

        [Test]
        public void TrollogramsFlickerOneStepInNine()
        {
            Assert.AreEqual(30f, Fx.TrolloFlicker.Hz);
            Assert.AreEqual(9, Fx.TrolloFlicker.Of);
            int on = 0;
            for (int i = 0; i < 900; i++) if (Fx.TrolloFlicker.On(i / 30f + 0.001f)) on++;
            Assert.AreEqual(100, on);
        }
    }
}
