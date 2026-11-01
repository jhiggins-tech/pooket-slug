using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    /// <summary>
    /// The beats drawn outside scene files (kie's tabk coming home in the bay, the den's first visit and a crewmate moving
    /// in, the skits) hold their lines to the cutscenes' reading rule (<c>docs/design/cutscenes/README.md</c>, How every cutscene
    /// plays): about three words a second, <see cref="SceneCheck.WordsPerSecond"/>, as <see cref="SceneCheck.Timing"/>
    /// holds a scene file's. Each line's time is measured by playing the beat, from when it shows to when it goes.
    /// </summary>
    public class BeatReadingTests
    {
        const float Dt = 1f / 60f;

        static int Words(string text) => text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

        static void AssertReadable(string text, float seconds, string where)
        {
            float needs = Words(text) / SceneCheck.WordsPerSecond;
            Assert.GreaterOrEqual(seconds + Dt, needs, where + ": \"" + text + "\" is up for " + seconds.ToString("0.00") +
                " s, too short to read " + Words(text) + " words (" + needs.ToString("0.00") + " s)");
        }

        [Test]
        public void KiesTabkComingHome_GivesEveryLineItsReadingTime()
        {
            var b = new KieHomecoming();
            var shown = new Dictionary<string, float>();
            while (!b.Done)
            {
                b.Tick(Dt);
                var line = b.Line;
                if (line == null) continue;
                string text = line.Value.text;
                shown[text] = (shown.TryGetValue(text, out float s) ? s : 0f) + Dt;
            }
            Assert.AreEqual(KieHomecoming.Lines.Count, shown.Count, "every line shows");
            foreach (var l in KieHomecoming.Lines) AssertReadable(l.text, shown[l.text], "kie's tabk comes home");
        }

        [Test]
        public void TheDensBeats_GiveTheirLinesTheirReadingTime()
        {
            foreach (DenBeat beat in new[] { DenBeat.FirstVisit(), DenBeat.MoveIn(Content.Kcaj), DenBeat.MoveIn(Content.Tones2) })
            {
                float up = 0f;
                while (!beat.Done)
                {
                    beat.Tick(Dt);
                    if (beat.Speaking) up += Dt;
                }
                Assert.Greater(up, 0f, beat.Kind + ": the line shows");
                AssertReadable(beat.Line, up, "the den's " + beat.Kind);
            }
        }

        [Test]
        public void TheSkits_GiveEveryLineItsReadingTime()
        {
            foreach (Skit skit in new Skit[] { new TwoOfUsSkit(), new SpecialDeliverySkit(), new TrollogramSkit() })
            {
                var shown = new Dictionary<string, float>();
                while (!skit.Done)
                {
                    skit.Tick(Dt);
                    SkitLine? line = skit.Line;
                    if (line == null) continue;
                    string text = line.Value.Text;
                    shown[text] = (shown.TryGetValue(text, out float s) ? s : 0f) + Dt;
                }
                Assert.AreEqual(skit.Lines.Count, shown.Count, skit.Key + ": every line shows");
                foreach (SkitLine l in skit.Lines) AssertReadable(l.Text, shown[l.Text], skit.Key);
            }
        }
    }
}
