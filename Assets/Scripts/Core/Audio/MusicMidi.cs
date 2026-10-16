using System.Collections.Generic;
using System.IO;

namespace PooketSlug
{
    /// <summary>
    /// A <see cref="MusicTrack"/> as a General MIDI file, scored for an orchestra, so the same notes can be played by
    /// real instruments (<c>?spike=music</c>: the vendor repository's CI renders it with a General MIDI soundfont into
    /// the orchestral rendition the boss fight crossfades to). The lead goes to strings, doubled by horns an octave
    /// down; the harmony to pizzicato strings; the bass to contrabasses and tuba; the drums to concert bass drum,
    /// snare and cymbals (the hi-hats are left out). Same tempo and length as the chip render, so the two line up.
    /// </summary>
    public static class MusicMidi
    {
        const int TicksPerQuarter = 480, TicksPerStep = TicksPerQuarter / 4;

        struct Event
        {
            public int Tick, Order;
            public byte[] Bytes;
        }

        public static byte[] Write(MusicTrack track)
        {
            var tracks = new List<List<Event>> { TempoTrack(track) };
            tracks.Add(Part(Score.Read(track.Lead), 0, 48, 0, 100, 0.95f));    // string ensemble
            tracks.Add(Part(Score.Read(track.Lead), 1, 60, -12, 80, 0.95f));   // french horns
            tracks.Add(Part(Score.Read(track.Harmony), 2, 45, 0, 70, 0.6f));   // pizzicato strings
            tracks.Add(Part(Score.Read(track.Bass), 3, 43, 0, 95, 0.85f));     // contrabass
            tracks.Add(Part(Score.Read(track.Bass), 4, 58, 0, 70, 0.6f));      // tuba
            tracks.Add(Percussion(Score.Read(track.Drums)));

            using (var ms = new MemoryStream())
            {
                Ascii(ms, "MThd");
                Int(ms, 6, 4);
                Int(ms, 1, 2); // format 1
                Int(ms, tracks.Count, 2);
                Int(ms, TicksPerQuarter, 2);
                foreach (List<Event> t in tracks) Chunk(ms, t);
                return ms.ToArray();
            }
        }

        static List<Event> TempoTrack(MusicTrack track)
        {
            int usPerQuarter = (int)System.Math.Round(60000000.0 / track.Bpm);
            return new List<Event>
            {
                new Event { Tick = 0, Bytes = new byte[] { 0xFF, 0x51, 0x03, (byte)(usPerQuarter >> 16), (byte)(usPerQuarter >> 8), (byte)usPerQuarter } },
                new Event { Tick = track.Steps * TicksPerStep, Bytes = new byte[] { 0xFF, 0x01, 0x00 } }, // marks the end of the loop
            };
        }

        static List<Event> Part(List<Score.Note> notes, int channel, int program, int shift, int velocity, float gate)
        {
            var events = new List<Event> { new Event { Tick = 0, Bytes = new byte[] { (byte)(0xC0 | channel), (byte)program } } };
            foreach (Score.Note n in notes)
            {
                int on = n.At * TicksPerStep, off = on + System.Math.Max(1, (int)(n.Len * TicksPerStep * gate));
                foreach (int p in n.Pitches)
                {
                    int key = System.Math.Max(0, System.Math.Min(127, p + shift));
                    events.Add(new Event { Tick = on, Order = 1, Bytes = new byte[] { (byte)(0x90 | channel), (byte)key, (byte)velocity } });
                    events.Add(new Event { Tick = off, Order = 0, Bytes = new byte[] { (byte)(0x80 | channel), (byte)key, 0 } });
                }
            }
            return events;
        }

        static List<Event> Percussion(List<Score.Note> notes)
        {
            var events = new List<Event>();
            foreach (Score.Note n in notes)
            {
                int on = n.At * TicksPerStep;
                foreach (char d in n.Drums)
                {
                    int key = d == 'k' ? 35 : d == 's' ? 38 : d == 'c' ? 49 : -1;
                    if (key < 0) continue;
                    int velocity = d == 's' ? 70 : 95;
                    events.Add(new Event { Tick = on, Order = 1, Bytes = new byte[] { 0x99, (byte)key, (byte)velocity } });
                    events.Add(new Event { Tick = on + TicksPerStep / 2, Order = 0, Bytes = new byte[] { 0x89, (byte)key, 0 } });
                }
            }
            return events;
        }

        static void Chunk(Stream s, List<Event> events)
        {
            // note-offs before note-ons at the same tick, so a repeated note restarts
            events.Sort((a, b) => a.Tick != b.Tick ? a.Tick.CompareTo(b.Tick) : a.Order.CompareTo(b.Order));
            using (var body = new MemoryStream())
            {
                int last = 0;
                foreach (Event e in events)
                {
                    VarInt(body, e.Tick - last);
                    body.Write(e.Bytes, 0, e.Bytes.Length);
                    last = e.Tick;
                }
                VarInt(body, 0);
                body.Write(new byte[] { 0xFF, 0x2F, 0x00 }, 0, 3);
                Ascii(s, "MTrk");
                Int(s, (int)body.Length, 4);
                body.WriteTo(s);
            }
        }

        static void Ascii(Stream s, string text)
        {
            foreach (char c in text) s.WriteByte((byte)c);
        }

        static void Int(Stream s, int value, int bytes)
        {
            for (int i = bytes - 1; i >= 0; i--) s.WriteByte((byte)(value >> (8 * i)));
        }

        static void VarInt(Stream s, int value)
        {
            var stack = new Stack<byte>();
            stack.Push((byte)(value & 0x7F));
            while ((value >>= 7) > 0) stack.Push((byte)((value & 0x7F) | 0x80));
            while (stack.Count > 0) s.WriteByte(stack.Pop());
        }
    }
}
