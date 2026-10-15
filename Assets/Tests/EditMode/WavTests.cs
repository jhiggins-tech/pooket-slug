using System.IO;
using NUnit.Framework;

namespace PooketSlug.Tests
{
    public class WavTests
    {
        static byte[] Make(short[] samples, int channels, int rate, bool extraChunk = false)
        {
            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            w.Write("RIFF".ToCharArray()); w.Write(0); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)channels);
            w.Write(rate); w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
            if (extraChunk) { w.Write("LIST".ToCharArray()); w.Write(3); w.Write(new byte[] { 1, 2, 3, 0 }); }
            w.Write("data".ToCharArray()); w.Write(samples.Length * 2);
            foreach (short s in samples) w.Write(s);
            return ms.ToArray();
        }

        [Test]
        public void ReadsMonoSamplesAndTheRate()
        {
            float[] s = Wav.Read(Make(new short[] { 0, 16384, -32768 }, 1, 22050), out int rate);
            Assert.AreEqual(22050, rate);
            CollectionAssert.AreEqual(new[] { 0f, 0.5f, -1f }, s);
        }

        [Test]
        public void MixesStereoDownAndSkipsOtherChunks()
        {
            float[] s = Wav.Read(Make(new short[] { 16384, 0, -16384, -16384 }, 2, 44100, extraChunk: true), out int rate);
            Assert.AreEqual(44100, rate);
            CollectionAssert.AreEqual(new[] { 0.25f, -0.5f }, s);
        }

        [Test]
        public void RefusesWhatIsntAWav()
        {
            Assert.IsNull(Wav.Read(new byte[] { 1, 2, 3 }, out _));
            Assert.IsNull(Wav.Read(null, out _));
        }
    }
}
