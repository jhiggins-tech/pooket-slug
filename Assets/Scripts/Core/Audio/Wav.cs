using System;

namespace PooketSlug
{
    /// <summary>
    /// Reads a plain WAV file (16-bit PCM, any rate, mono or stereo) into samples. The recordings (the announcer's and
    /// the narrator's lines, the title and den themes, the boss's orchestra layer) arrive as raw bytes and are made into
    /// clips in code, like the chip sounds, so no browser has to decode a compressed format (WebGL builds use AAC, which
    /// some Chromium builds can't play).
    /// </summary>
    public static class Wav
    {
        /// <summary>The samples, mono, -1..1, and their rate; or null if it isn't a 16-bit PCM WAV.</summary>
        public static float[] Read(byte[] data, out int rate)
        {
            rate = 0;
            if (data == null || data.Length < 12 || Tag(data, 0) != "RIFF" || Tag(data, 8) != "WAVE") return null;
            int channels = 0, bits = 0, pos = 12;
            while (pos + 8 <= data.Length)
            {
                string id = Tag(data, pos);
                int size = BitConverter.ToInt32(data, pos + 4);
                int body = pos + 8;
                if (size < 0 || body + size > data.Length) size = data.Length - body;
                if (id == "fmt " && size >= 16)
                {
                    if (BitConverter.ToInt16(data, body) != 1) return null; // PCM only
                    channels = BitConverter.ToInt16(data, body + 2);
                    rate = BitConverter.ToInt32(data, body + 4);
                    bits = BitConverter.ToInt16(data, body + 14);
                }
                else if (id == "data")
                {
                    if (bits != 16 || channels < 1 || rate <= 0) return null;
                    int frames = size / (2 * channels);
                    var samples = new float[frames];
                    for (int i = 0; i < frames; i++)
                    {
                        float sum = 0f;
                        for (int c = 0; c < channels; c++) sum += BitConverter.ToInt16(data, body + (i * channels + c) * 2) / 32768f;
                        samples[i] = sum / channels;
                    }
                    return samples;
                }
                pos = body + size + (size & 1);
            }
            return null;
        }

        static string Tag(byte[] d, int at) => at + 4 <= d.Length ? new string(new[] { (char)d[at], (char)d[at + 1], (char)d[at + 2], (char)d[at + 3] }) : "";
    }
}
