using System;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>The sound bank: every clip is an 8-bit recipe (<see cref="ChipSounds"/>), rendered at startup.</summary>
    public static class Sfx
    {
        public static AudioClip Shot, Heavy, EnemyShot, Cannon, Explosion, BigExplosion, Jump, Hurt, Hit, Pickup, Throw, Start, Victory, GameOver;
        // tones2's kit, and the extras (the hologram goons, the boss tank)
        public static AudioClip JetLoop, Splat, RocketRumble, RocketLaunch, Glitch, Poison, Spew, Sizzle, Thump, Charge, Servo;
        // kie's kit
        public static AudioClip WeaselThrow, WeaselTune;
        public static bool Muted;

        static AudioSource s_Source, s_Voice;
        static readonly System.Collections.Generic.Dictionary<string, AudioClip> s_Lines = new System.Collections.Generic.Dictionary<string, AudioClip>();

        public static void Init(GameObject host)
        {
            try
            {
                s_Source = host.AddComponent<AudioSource>();
                s_Source.playOnAwake = false;
                s_Source.spatialBlend = 0f;
                s_Voice = host.AddComponent<AudioSource>();
                s_Voice.playOnAwake = false;
                s_Voice.spatialBlend = 0f;

                ChipSounds.Make();
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

        /// <summary>
        /// The arcade announcer says line <paramref name="id"/> (<c>mission-start</c>, <c>mission-complete</c>,
        /// <c>warning</c>, <c>boss-tank</c>) after <paramref name="delay"/> seconds, cutting off any line still
        /// playing. The lines are made by the vendor repository's CI and fetched into <c>Resources/Voice</c> by the
        /// game's (as <c>&lt;id&gt;.bytes</c>, so Unity leaves them as plain WAV bytes); a build without them stays quiet here.
        /// </summary>
        public static void Say(string id, float delay = 0f)
        {
            if (Muted || s_Voice == null) return;
            if (!s_Lines.TryGetValue(id, out AudioClip clip))
            {
                TextAsset raw = Resources.Load<TextAsset>("Voice/" + id); // <id>.bytes: a WAV, made into a clip here
                int rate = 0;
                float[] samples = raw != null ? Wav.Read(raw.bytes, out rate) : null;
                clip = samples != null ? Clip("voice-" + id, samples, rate) : null;
                s_Lines[id] = clip;
                if (clip == null) Log.Audio.Info("No announcer line " + id + " in this build");
            }
            if (clip == null) return;
            s_Voice.Stop();
            s_Voice.clip = clip;
            s_Voice.volume = 0.9f;
            s_Voice.PlayDelayed(delay);
        }

        /// <summary>A clip of ready-made samples (<see cref="ChipSounds"/>' renders).</summary>
        public static AudioClip Clip(string name, float[] data, int rate)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
