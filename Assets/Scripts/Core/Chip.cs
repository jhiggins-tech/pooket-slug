using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A tiny Game Boy-flavoured synth that renders into a buffer: pulse waves (12.5 / 25 / 50% duty) and a
    /// triangle for tones, plus crunchy LFSR noise, with stepped pitch sweeps and simple volume envelopes. It
    /// matches Pooket Tabks' chip synth (src/audio/chip.ts) call for call, so its sound recipes port over as they
    /// are; Tabks plays them live on Web Audio, this renders each one once into a clip at startup.
    /// </summary>
    public sealed class Chip
    {
        public const int Rate = 44100;
        /// <summary>Tabks' master volume, and a little make-up gain for its compressor.</summary>
        const float Master = 0.5f, MakeUp = 2.4f;
        /// <summary>How fast the noise's shift register clocks at rate 1: Tabks holds each bit for 4 samples at 48 kHz.</summary>
        const float NoiseClock = 12000f;

        readonly float[] m_Mix;

        /// <summary>A blank buffer <paramref name="seconds"/> long.</summary>
        public Chip(float seconds) => m_Mix = new float[Mathf.CeilToInt(seconds * Rate)];

        public int Length => m_Mix.Length;

        /// <summary>Frequency of a MIDI note (69 = A4 = 440 Hz).</summary>
        public static float Midi(float n) => 440f * Mathf.Pow(2f, (n - 69f) / 12f);

        /// <summary>
        /// A tone from <paramref name="at"/> for <paramref name="dur"/> seconds, sliding from <paramref name="from"/>
        /// Hz to <paramref name="to"/> (0: no slide), in <paramref name="steps"/> steps like a hardware sweep (0:
        /// smoothly). <paramref name="env"/> is the volume envelope as time (0..1), level (0..1) pairs, from silence;
        /// without one it fades straight out.
        /// </summary>
        public void Tone(float at, float dur, float from, float to = 0f, float duty = 0.5f, bool triangle = false, float vol = 0.2f,
            int steps = 0, float vibratoRate = 0f, float vibratoDepth = 0f, float[] env = null)
        {
            if (to <= 0f) to = from;
            int start = Mathf.RoundToInt(at * Rate), n = Mathf.RoundToInt(dur * Rate);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                int k = start + i;
                if (k < 0) continue;
                if (k >= m_Mix.Length) break;
                float p = i / (float)n;
                float slide = steps > 0 ? Mathf.Floor(p * steps) / steps : p;
                float f = to == from ? from : from * Mathf.Pow(to / from, slide);
                if (vibratoDepth > 0f) f += vibratoDepth * Mathf.Sin(2f * Mathf.PI * vibratoRate * i / Rate);
                phase += Mathf.Max(0f, f) / Rate;
                float frac = phase - Mathf.Floor(phase);
                float wave = triangle ? 1f - 4f * Mathf.Abs(frac - 0.5f) : (frac < duty ? 1f : -1f);
                m_Mix[k] += wave * vol * Envelope(env, p);
            }
        }

        /// <summary>
        /// Noise from <paramref name="at"/> for <paramref name="dur"/> seconds. <paramref name="rate"/> is its
        /// playback rate (lower is crunchier and lower pitched), sliding to <paramref name="to"/> (0: no slide).
        /// </summary>
        public void Noise(float at, float dur, float vol = 0.2f, float rate = 1f, float to = 0f, float[] env = null)
        {
            if (to <= 0f) to = rate;
            int start = Mathf.RoundToInt(at * Rate), n = Mathf.RoundToInt(dur * Rate);
            int lfsr = 0x7fff;
            float clock = 0f;
            for (int i = 0; i < n; i++)
            {
                int k = start + i;
                if (k < 0) continue;
                if (k >= m_Mix.Length) break;
                float p = i / (float)n;
                float r = to == rate ? rate : rate * Mathf.Pow(to / rate, p);
                clock += NoiseClock * r / Rate;
                while (clock >= 1f)
                {
                    clock -= 1f;
                    int bit = (lfsr ^ (lfsr >> 1)) & 1;
                    lfsr = (lfsr >> 1) | (bit << 14);
                }
                m_Mix[k] += ((lfsr & 1) != 0 ? 0.8f : -0.8f) * vol * Envelope(env, p);
            }
        }

        /// <summary>A quick run of notes (MIDI numbers), <paramref name="step"/> seconds apart.</summary>
        public void Arp(float[] notes, float step, float at = 0f, float dur = 0f, float duty = 0.25f, float vol = 0.14f, bool triangle = false)
        {
            for (int i = 0; i < notes.Length; i++)
                Tone(at + i * step, dur > 0f ? dur : step * 0.9f, Midi(notes[i]), duty: duty, vol: vol, triangle: triangle);
        }

        /// <summary>The mixed samples at the master volume, squashed (tanh) so a pile-up can't clip.</summary>
        public float[] Finish()
        {
            var output = new float[m_Mix.Length];
            for (int i = 0; i < output.Length; i++)
            {
                float x = m_Mix[i] * Master * MakeUp;
                output[i] = (float)System.Math.Tanh(x);
            }
            return output;
        }

        /// <summary>The envelope's level at <paramref name="p"/> (0..1 through the sound).</summary>
        static float Envelope(float[] env, float p)
        {
            if (env == null) return 1f - p;
            float t0 = 0f, v0 = 0f;
            for (int i = 0; i + 1 < env.Length; i += 2)
            {
                float t1 = env[i], v1 = Mathf.Max(0f, env[i + 1]);
                if (p <= t1) return t1 <= t0 ? v1 : Mathf.Lerp(v0, v1, (p - t0) / (t1 - t0));
                t0 = t1;
                v0 = v1;
            }
            return v0;
        }
    }
}
