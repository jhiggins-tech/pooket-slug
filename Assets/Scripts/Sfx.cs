using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>Tiny procedural sound bank: every clip is synthesised at startup.</summary>
    public static class Sfx
    {
        const int Rate = 22050;

        public static AudioClip Shot, Heavy, EnemyShot, Cannon, Explosion, BigExplosion, Jump, Hurt, Hit, Pickup, Throw, Start, Victory, GameOver;
        // spike sounds: tones2's kit and the hologram goons
        public static AudioClip JetLoop, Splat, RocketRumble, RocketLaunch, Glitch, Poison, Spew, Sizzle, Thump, Charge, Servo;
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
                if (Spike.Any) MakeSpikeSounds();
            }
            catch (Exception e)
            {
                Log.Audio.Warn("Sound disabled: " + e.Message);
                s_Source = null;
            }
        }

        /// <summary>
        /// Plays a weapon's shot, at the volume for whoever fired it, or at <paramref name="volume"/> (a partner's
        /// shots are quieter). The content is data in Core; how it sounds lives here.
        /// </summary>
        public static void PlayShot(WeaponDef weapon, float volume = -1f)
        {
            AudioClip clip;
            float own;
            if (weapon == Content.Pistol) { clip = Shot; own = 0.6f; }
            else if (weapon == Content.HeavyMachineGun) { clip = Heavy; own = 0.5f; }
            else if (weapon == Content.GrenadeThrow) { clip = Throw; own = 1f; }
            else if (weapon == Content.Cannon) { clip = Cannon; own = 0.8f; }
            else if (weapon == Content.Rifle || weapon == Content.TankGun) { clip = EnemyShot; own = 0.35f; }
            else return; // dropped bombs are silent
            Play(clip, volume >= 0f ? volume : own);
        }

        static void MakeSpikeSounds()
        {
            // A seamless hiss for the jet (looped; its pitch and volume follow the pressure).
            float lp = 0f;
            JetLoop = Make("jetLoop", 0.5f, (t, p) =>
            {
                lp += (Noise() - lp) * 0.35f;
                return lp * 0.8f + Noise() * 0.12f;
            }, loop: true);
            Splat = Make("splat", 0.06f, (t, p) => Noise() * 0.35f * (1 - p) + Sine(Sweep(t, 0.06f, 900, 300)) * 0.15f);
            RocketRumble = Make("rocketRumble", 0.5f, LowNoise(0.5f, 0.5f));
            RocketLaunch = Make("rocketLaunch", 0.45f, (t, p) => Noise() * 0.45f * (1 - p) + Square(Sweep(t, 0.45f, 90, 420)) * 0.2f * (1 - p));
            // The hologram glitching out: a bit-crushed chirp that stutters down.
            Glitch = Make("glitch", 0.32f, (t, p) =>
            {
                int step = (int)(t * 60f);
                float f = 1400f - step * 110f + (step % 3) * 300f;
                return Square(Sweep(t, 0.32f, f, f * 0.7f)) * 0.22f * ((step % 4) == 3 ? 0.2f : 1f) + Noise() * 0.06f;
            });
            Poison = Make("poison", 0.12f, (t, p) => Sine(Sweep(t, 0.12f, 300, 180)) * 0.25f + Noise() * 0.08f);
            // tones2's spew: a gurgling retch that wobbles down over a second and a half.
            Spew = Make("spew", 1.5f, (t, p) =>
            {
                float wobble = 1f + 0.25f * Mathf.Sin(t * 38f);
                return Square(Sweep(t, 1.5f, 170f * wobble, 70f * wobble)) * 0.22f * (1f - p * 0.6f) + Noise() * 0.18f * (0.6f + 0.4f * Mathf.Sin(t * 23f));
            });
            Sizzle = Make("sizzle", 0.14f, (t, p) => Noise() * 0.3f * (1 - p) * (0.5f + 0.5f * Mathf.Sin(t * 900f)));
            // A heavy hit landing: a low thud with a click on top.
            // The boss tank charging a shot: a whine that climbs; and its turret's servo, a little ratchet.
            Charge = Make("charge", 0.9f, (t, p) => Square(Sweep(t, 0.9f, 180, 900)) * 0.16f * (0.4f + 0.6f * p) + Noise() * 0.05f);
            Servo = Make("servo", 0.06f, (t, p) => Square(Sweep(t, 0.06f, 320, 260)) * 0.14f * (1 - p));
            Thump = Make("thump", 0.12f, (t, p) => Sine(Sweep(t, 0.12f, 160, 50)) * 0.5f * (1 - p) + Noise() * 0.2f * Mathf.Max(0f, 1f - p * 6f));
        }

        /// <summary>A looping source for a continuous sound (the jet), or null without sound.</summary>
        public static AudioSource LoopSource(GameObject host, AudioClip clip)
        {
            if (s_Source == null || clip == null) return null;
            var src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = true;
            src.clip = clip;
            src.volume = 0f;
            return src;
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

        static AudioClip Make(string name, float seconds, Func<float, float, float> sample, bool loop = false)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            s_Phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, p = i / (float)n;
                float env = loop ? 1f : Mathf.Min(1f, i / 40f) * (1f - p * 0.6f); // short attack, gentle decay
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
