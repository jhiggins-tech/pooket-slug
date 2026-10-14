using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>Tiny procedural sound bank: every clip is synthesised at startup.</summary>
    public static class Sfx
    {
        const int Rate = 22050;

        public static AudioClip Shot, Heavy, EnemyShot, Cannon, Explosion, BigExplosion, Jump, Hurt, Hit, Pickup, Throw, Start, Victory, GameOver;
        public static bool Muted;

        static AudioSource s_Source;
        static System.Random s_Rng = new System.Random(1234);

        public static void Init(GameObject host)
        {
            try
            {
                s_Source = host.AddComponent<AudioSource>();
                s_Source.playOnAwake = false;
                s_Source.spatialBlend = 0f;

                Shot = Make("shot", 0.09f, (t, p) => Square(Sweep(t, 0.09f, 900, 260)) * 0.35f + Noise() * 0.25f * (1 - p));
                Heavy = Make("heavy", 0.07f, (t, p) => Square(Sweep(t, 0.07f, 600, 180)) * 0.3f + Noise() * 0.35f * (1 - p));
                EnemyShot = Make("enemyShot", 0.12f, (t, p) => Square(Sweep(t, 0.12f, 520, 160)) * 0.25f);
                Cannon = Make("cannon", 0.25f, (t, p) => Noise() * 0.5f * (1 - p) + Sine(Sweep(t, 0.25f, 140, 50)) * 0.5f);
                Explosion = Make("explosion", 0.55f, LowNoise(0.55f, 0.75f));
                BigExplosion = Make("bigExplosion", 1.1f, LowNoise(1.1f, 0.9f));
                Jump = Make("jump", 0.12f, (t, p) => Square(Sweep(t, 0.12f, 260, 620)) * 0.18f);
                Hurt = Make("hurt", 0.25f, (t, p) => Square(Sweep(t, 0.25f, 440, 90)) * 0.35f);
                Hit = Make("hit", 0.04f, (t, p) => Noise() * 0.3f);
                Throw = Make("throw", 0.15f, (t, p) => Noise() * 0.15f * Mathf.Sin(p * Mathf.PI));
                Pickup = Notes("pickup", 0.07f, 660, 880, 1320);
                Start = Notes("start", 0.11f, 392, 523, 659, 784);
                Victory = Notes("victory", 0.13f, 523, 659, 784, 1047, 784, 1047);
                GameOver = Notes("gameOver", 0.2f, 392, 330, 262, 196);
            }
            catch (Exception e)
            {
                Log.Audio.Warn("Sound disabled: " + e.Message);
                s_Source = null;
            }
        }

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (Muted || s_Source == null || clip == null) return;
            s_Source.PlayOneShot(clip, volume);
        }

        // ---- synthesis helpers ----

        static float s_Phase;

        static float Sweep(float t, float len, float f0, float f1)
        {
            float f = Mathf.Lerp(f0, f1, t / len);
            s_Phase += f / Rate;
            return s_Phase;
        }

        static float Square(float phase) => (phase - Mathf.Floor(phase)) < 0.5f ? 1f : -1f;
        static float Sine(float phase) => Mathf.Sin(phase * Mathf.PI * 2f);
        static float Noise() => (float)(s_Rng.NextDouble() * 2.0 - 1.0);

        static Func<float, float, float> LowNoise(float len, float volume)
        {
            float lp = 0f;
            return (t, p) =>
            {
                float cutoff = Mathf.Lerp(0.5f, 0.03f, p);
                lp += (Noise() - lp) * cutoff;
                return lp * volume * 2.2f * (1f - p) * (1f - p);
            };
        }

        static AudioClip Make(string name, float seconds, Func<float, float, float> sample)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            s_Phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, p = i / (float)n;
                float env = Mathf.Min(1f, i / 40f) * (1f - p * 0.6f); // short attack, gentle decay
                data[i] = Mathf.Clamp(sample(t, p) * env, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Notes(string name, float noteLen, params float[] freqs)
        {
            int per = Mathf.CeilToInt(noteLen * Rate);
            var data = new float[per * freqs.Length];
            for (int k = 0; k < freqs.Length; k++)
            {
                float phase = 0f;
                for (int i = 0; i < per; i++)
                {
                    phase += freqs[k] / Rate;
                    float p = i / (float)per;
                    float env = Mathf.Min(1f, i / 30f) * (1f - p);
                    data[k * per + i] = Square(phase) * 0.2f * env;
                }
            }
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
