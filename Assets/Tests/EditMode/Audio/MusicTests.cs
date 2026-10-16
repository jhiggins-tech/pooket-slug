using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class MusicTests
    {
        [Test]
        public void NotesReadAsMidi()
        {
            Assert.AreEqual(60, Notation.PitchOf("c4"));
            Assert.AreEqual(69, Notation.PitchOf("a4"));
            Assert.AreEqual(66, Notation.PitchOf("f#4"));
            Assert.AreEqual(58, Notation.PitchOf("bb3"));
            Assert.AreEqual(71, Notation.PitchOf("b4"));
        }

        [Test]
        public void LengthsStickUntilChanged()
        {
            var notes = Notation.Read("c5:2 e5 r:4 g5");
            Assert.AreEqual(4, notes.Count);
            Assert.AreEqual(new[] { 0, 2, 4, 8 }, new[] { notes[0].At, notes[1].At, notes[2].At, notes[3].At });
            Assert.AreEqual(2, notes[1].Len);
            Assert.AreEqual(0, notes[2].Pitches.Length);
            Assert.AreEqual(12, Notation.Steps("c5:2 e5 r:4 g5"));
        }

        [Test]
        public void ChordsDrumsAndSlides()
        {
            var notes = Notation.Read("c4+e4+g4:4 k+h:1 c c5~ g5 a5");
            Assert.AreEqual(3, notes[0].Pitches.Length);
            Assert.AreEqual(new[] { 'k', 'h' }, notes[1].Drums);
            Assert.AreEqual(new[] { 'c' }, notes[2].Drums, "a lone c is the crash");
            Assert.AreEqual(Notation.PitchOf("g5"), notes[3].SlideTo);
            Assert.AreEqual(0, notes[4].SlideTo);
        }

        [Test]
        public void BadNotesAreRefused()
        {
            Assert.Throws<System.FormatException>(() => Notation.Read("x5"));
            Assert.Throws<System.FormatException>(() => Notation.Read("c5:0"));
            Assert.Throws<System.FormatException>(() => Notation.Read("k+c5"));
        }

        [Test]
        public void EveryLoopIsWholeBarsInEveryPart()
        {
            foreach (MusicTrack t in MusicTracks.All)
            {
                if (!t.Loop) continue;
                int steps = t.Steps;
                Assert.AreEqual(0, steps % MusicTracks.Bar, t.Name);
                Assert.AreEqual(steps, Notation.Steps(t.Lead), t.Name + " lead");
                Assert.AreEqual(steps, Notation.Steps(t.Harmony), t.Name + " harmony");
                Assert.AreEqual(steps, Notation.Steps(t.Bass), t.Name + " bass");
                Assert.AreEqual(steps, Notation.Steps(t.Drums), t.Name + " drums");
            }
        }

        [Test]
        public void TheTracksAreAsDesigned()
        {
            // docs/design/music.md: Mission one's theme about 50 s, the boss theme faster and about 25 s
            Assert.AreEqual(32 * MusicTracks.Bar, MusicTracks.SteelNight.Steps);
            Assert.AreEqual(16 * MusicTracks.Bar, MusicTracks.TankAlert.Steps);
            Assert.Greater(MusicTracks.TankAlert.Bpm, MusicTracks.SteelNight.Bpm);
            Assert.Greater(MusicTracks.TankAlertFinal.Bpm, MusicTracks.TankAlert.Bpm, "the second stage is faster still");
            Assert.That(MusicTracks.SteelNight.Seconds, Is.InRange(45f, 55f));
            Assert.That(MusicTracks.TankAlert.Seconds, Is.InRange(20f, 30f));
            Assert.Less(MusicTracks.MissionStart.Seconds, 3f);
            Assert.Less(MusicTracks.BossVictory.Seconds, 3f);
        }

        [Test]
        public void ALoopRendersToExactlyItsLength()
        {
            MusicTrack t = MusicTracks.TankAlert;
            float[] samples = MusicRender.Render(t);
            Assert.AreEqual(UnityEngine.Mathf.RoundToInt(t.Seconds * Chip.Rate), samples.Length);
            float peak = 0f;
            foreach (float s in samples) peak = System.Math.Max(peak, System.Math.Abs(s));
            Assert.That(peak, Is.InRange(0.05f, 1f));
        }
    
        [Test]
        public void TheFinalSectionWritesAsAnOrchestralMidiFile()
        {
            byte[] midi = MusicMidi.Write(MusicTracks.TankAlertFinal);
            Assert.AreEqual("MThd", System.Text.Encoding.ASCII.GetString(midi, 0, 4));
            Assert.AreEqual(7, midi[11], "a tempo track and six parts");
            Assert.AreEqual(480, (midi[12] << 8) | midi[13]);
            int tracks = 0;
            for (int i = 0; i + 4 <= midi.Length; i++)
                if (midi[i] == 'M' && midi[i + 1] == 'T' && midi[i + 2] == 'r' && midi[i + 3] == 'k') tracks++;
            Assert.AreEqual(7, tracks);
        }
    }
}
