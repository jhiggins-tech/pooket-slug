using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>The skits' snack shows (Core/Story/SnackShow.cs): the packet turned round, the label's close-up, the soft-serve splat; and the scene lines that stage them.</summary>
    public class SnackShowTests
    {
        static SceneScript Read(string text) => SceneScript.Read("test", text);

        [Test]
        public void ThePacketTurnsRoundThroughItsEdgeToItsLabel()
        {
            Assert.AreEqual(1f, SnackShow.BagWidth(0f), 1e-5f, "its front, full width");
            Assert.AreEqual(SnackShow.EdgeOn, SnackShow.BagWidth(0.5f), 1e-5f, "edge on, but never gone");
            Assert.AreEqual(1f, SnackShow.BagWidth(1f), 1e-5f, "its back, full width");
            Assert.AreEqual(1f, SnackShow.BagWidth(3f), 1e-5f, "and it stays round");
            for (float k = 0f; k < 0.5f; k += 0.05f)
            {
                Assert.Greater(SnackShow.BagWidth(k), SnackShow.BagWidth(k + 0.05f) - 1e-5f, "narrowing at " + k);
                Assert.IsFalse(SnackShow.LabelOut(k), "front out at " + k);
            }
            for (float k = 0.5f; k <= 1f; k += 0.05f) Assert.IsTrue(SnackShow.LabelOut(k), "label out at " + k);
        }

        [Test]
        public void TheLabelComesUpAndSettles()
        {
            Assert.AreEqual(0f, SnackShow.LabelUp(0f));
            Assert.AreEqual(0.75f, SnackShow.LabelUp(SnackShow.LabelRise / 2f), 1e-5f, "slowing as it arrives: three quarters up at halfway");
            Assert.AreEqual(1f, SnackShow.LabelUp(SnackShow.LabelRise));
            Assert.AreEqual(1f, SnackShow.LabelUp(5f));
        }

        [Test]
        public void TheSplatBurstsOnceThenDripsForAsLongAsItsShown()
        {
            Assert.AreEqual(7, SnackShow.SplatFrames, "three of burst, four of drips: the art's count");
            var seen = new List<int>();
            for (float t = 0f; t < 6f; t += 0.01f)
            {
                int f = SnackShow.SplatFrame(t);
                if (seen.Count == 0 || seen[seen.Count - 1] != f) seen.Add(f);
                Assert.That(f, Is.InRange(0, SnackShow.SplatFrames - 1));
                if (t >= SnackShow.SplatBurst.Length) Assert.GreaterOrEqual(f, SnackShow.SplatBurst.Frames, "never bursts again, at " + t);
            }
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 3 }, seen.GetRange(0, 8), "the burst, then the drips round and round");
            Assert.AreEqual(SnackShow.SplatBurst.Frames + SnackShow.SplatDrip.Frame(1f), SnackShow.SplatFrame(SnackShow.SplatBurst.Length + 1f));
        }

        [Test]
        public void ScenesTurnTheBagRoundAndSquashTheCones()
        {
            SceneScript s = Read("net 9\nshot: A\nlength 8\nset bay\n" +
                                 "kie holds bag\n1-1.6 bag turn-round\n" +
                                 "tones2 holds softserve softserve2\n2 softserve squash\n2 softserve2 squash\n2 splat on tones2 0 0\n3 label show\n");
            Assert.IsEmpty(s.Problems, string.Join("; ", s.Problems));
            var stage = new ShotStage(s.Def.Shots[0]);
            Assert.AreEqual(0f, stage.Progress("bag", "turn-round", 0.5f), "not yet");
            Assert.IsTrue(SnackShow.LabelOut(stage.Progress("bag", "turn-round", 1.4f)), "past halfway");
            Assert.AreEqual(1f, stage.Progress("bag", "turn-round", 5f), "and round it stays");
            Assert.Less(stage.Since("softserve", "squash", 1.9f), 0f, "whole until it's hit");
            Assert.GreaterOrEqual(stage.Since("softserve2", "squash", 2.1f), 0f);
            UnityEngine.Vector2 feet = stage.Where("tones2", 3f, 0f);
            Assert.AreEqual(feet, stage.Where("splat", 3f, 0f), "on him, at his feet");
            Assert.AreEqual(2f, stage.ShownSince("splat", 3f), 1e-5f, "the burst starts as it's put on him");
            Assert.IsTrue(stage.Shown("label", 3.1f));
            Assert.Greater(Staging.Hand("softserve").x, 0f, "one in the right hand");
            Assert.Less(Staging.Hand("softserve2").x, 0f, "and one in the left");
        }

        [Test]
        public void TheirMistakesAreProblems()
        {
            foreach (string line in new[] { "2 bag turn-round", "1-2 softserve squash 3", "1-2 bag turn-round 4", "2 cup turn-round" })
                Assert.IsNotEmpty(Read("net 9\nshot: A\nlength 8\nset den\n" + line + "\n").Problems, line);
        }
    }
}
