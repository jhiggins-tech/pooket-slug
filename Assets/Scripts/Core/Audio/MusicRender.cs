using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Plays a <see cref="MusicTrack"/> into samples on the <see cref="Chip"/> synth (<c>docs/design/music.md</c>, How
    /// the tracks get made): the lead and harmony as pulse tones, the bass on the triangle, the drums as noise. A loop's
    /// tail (notes ringing past its end) is folded back onto its start, so it repeats with no seam.
    /// </summary>
    public static class MusicRender
    {
        /// <summary>How long notes may ring past a loop's end, folded back onto its start.</summary>
        const float Tail = 0.6f;

        /// <summary>A held note: up fast, a little sag, then out.</summary>
        static readonly float[] Held = { 0.03f, 1f, 0.6f, 0.75f, 1f, 0f };

        public static float[] Render(MusicTrack track)
        {
            float step = track.Step, length = track.Seconds;
            var chip = new Chip(length + Tail);
            Voice(chip, Score.Read(track.Lead), step, track.LeadDuty, false, 0.11f, 0.9f);
            Voice(chip, Score.Read(track.Harmony), step, 0.5f, false, 0.05f, 0.7f);
            Voice(chip, Score.Read(track.Bass), step, 0.5f, true, 0.2f, 0.8f);
            Drums(chip, Score.Read(track.Drums), step);
            float[] all = chip.Finish();
            if (!track.Loop) return all;

            int n = Mathf.RoundToInt(length * Chip.Rate);
            var loop = new float[n];
            System.Array.Copy(all, loop, n);
            for (int i = n; i < all.Length && i - n < n; i++) loop[i - n] = Mathf.Clamp(loop[i - n] + all[i], -1f, 1f);
            return loop;
        }

        static void Voice(Chip chip, List<Score.Note> notes, float step, float duty, bool triangle, float vol, float gate)
        {
            foreach (Score.Note note in notes)
            {
                if (note.Pitches.Length == 0) continue;
                float at = note.At * step, dur = note.Len * step * gate;
                float each = vol / Mathf.Sqrt(note.Pitches.Length);
                foreach (int p in note.Pitches)
                {
                    float to = note.SlideTo > 0 ? Chip.Midi(note.SlideTo) : 0f;
                    chip.Tone(at, dur, Chip.Midi(p), to, duty: duty, triangle: triangle, vol: each, env: Held);
                }
            }
        }

        static void Drums(Chip chip, List<Score.Note> notes, float step)
        {
            foreach (Score.Note note in notes)
            {
                float at = note.At * step;
                foreach (char d in note.Drums)
                {
                    switch (d)
                    {
                        case 'k': // a low thump: a falling triangle and crunchy noise
                            chip.Tone(at, 0.09f, 160f, 50f, triangle: true, vol: 0.22f);
                            chip.Noise(at, 0.05f, vol: 0.08f, rate: 0.3f);
                            break;
                        case 's':
                            chip.Noise(at, 0.08f, vol: 0.12f, rate: 0.9f, to: 0.6f);
                            break;
                        case 'h':
                            chip.Noise(at, 0.025f, vol: 0.045f, rate: 3f);
                            break;
                        case 'c':
                            chip.Noise(at, 0.45f, vol: 0.08f, rate: 1.6f, to: 1.1f);
                            break;
                    }
                }
            }
        }
    }
}
