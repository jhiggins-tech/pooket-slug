using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// A tiny score notation for the music (<c>docs/design/music.md</c>), the same as Pooket Tabks' (src/audio/score.ts),
    /// read into timed notes. A part is notes separated by spaces, each lasting some steps (a step is a sixteenth note):
    /// <list type="bullet">
    /// <item><c>c5</c>, <c>f#4</c>, <c>bb3</c>: a note (letter, <c>#</c> or <c>b</c>, octave; <c>c4</c> is middle C);</item>
    /// <item><c>c4+e4+g4</c>: a chord;</item>
    /// <item><c>r</c>: a rest;</item>
    /// <item><c>k</c>, <c>s</c>, <c>h</c>, <c>c</c>: drums (kick, snare, hi-hat, crash; <c>k+h</c> together);</item>
    /// <item><c>:3</c> after any of them: how many steps it lasts. It sticks: the next ones last as long until another
    /// <c>:n</c> (a part starts at 1);</item>
    /// <item><c>~</c> after a note: slide from it into the next one.</item>
    /// </list>
    /// </summary>
    public static class Score
    {
        public struct Note
        {
            /// <summary>When it starts and how long it lasts, in steps.</summary>
            public int At, Len;
            /// <summary>MIDI notes (one, or a chord); empty for a rest or drums.</summary>
            public int[] Pitches;
            /// <summary>Drum letters (k, s, h, c); empty for notes.</summary>
            public char[] Drums;
            /// <summary>The MIDI note to slide into over this one, or 0.</summary>
            public int SlideTo;
        }

        static readonly int[] Letters = { 9, 11, 0, 2, 4, 5, 7 }; // a b c d e f g

        /// <summary>A note name (<c>f#4</c>) as MIDI.</summary>
        public static int PitchOf(string name)
        {
            if (name.Length < 2 || name[0] < 'a' || name[0] > 'g') throw new FormatException("not a note: " + name);
            int i = 1, shift = 0;
            if (name[i] == '#') { shift = 1; i++; }
            else if (name[i] == 'b' && name.Length > 2) { shift = -1; i++; }
            if (i != name.Length - 1 || name[i] < '0' || name[i] > '9') throw new FormatException("not a note: " + name);
            return 12 * (name[i] - '0' + 1) + Letters[name[0] - 'a'] + shift;
        }

        /// <summary>Reads a part into its notes, rests included.</summary>
        public static List<Note> Read(string part)
        {
            var notes = new List<Note>();
            int at = 0, len = 1;
            foreach (string raw in part.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            {
                string token = raw;
                int colon = token.IndexOf(':');
                if (colon >= 0)
                {
                    if (!int.TryParse(token.Substring(colon + 1), out len) || len < 1) throw new FormatException("bad length: " + raw);
                    token = token.Substring(0, colon);
                }
                bool slide = token.EndsWith("~");
                if (slide) token = token.Substring(0, token.Length - 1);
                var note = new Note { At = at, Len = len, Pitches = Array.Empty<int>(), Drums = Array.Empty<char>() };
                if (token != "r")
                {
                    string[] bits = token.Split('+');
                    if (bits[0].Length == 1)
                    {
                        note.Drums = new char[bits.Length];
                        for (int i = 0; i < bits.Length; i++)
                        {
                            if (bits[i].Length != 1 || "kshc".IndexOf(bits[i][0]) < 0) throw new FormatException("not a drum: " + raw);
                            note.Drums[i] = bits[i][0];
                        }
                    }
                    else
                    {
                        note.Pitches = new int[bits.Length];
                        for (int i = 0; i < bits.Length; i++) note.Pitches[i] = PitchOf(bits[i]);
                    }
                }
                if (notes.Count > 0 && notes[notes.Count - 1].SlideTo < 0 && note.Pitches.Length > 0)
                {
                    Note prev = notes[notes.Count - 1];
                    prev.SlideTo = note.Pitches[0];
                    notes[notes.Count - 1] = prev;
                }
                if (slide) note.SlideTo = -1; // filled in by the next note
                notes.Add(note);
                at += len;
            }
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i].SlideTo >= 0) continue;
                Note n = notes[i];
                n.SlideTo = 0; // slid into nothing
                notes[i] = n;
            }
            return notes;
        }

        /// <summary>A part's length in steps.</summary>
        public static int Steps(string part)
        {
            List<Note> notes = Read(part);
            return notes.Count == 0 ? 0 : notes[notes.Count - 1].At + notes[notes.Count - 1].Len;
        }
    }
}
