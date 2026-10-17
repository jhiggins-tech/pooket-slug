using UnityEngine;

namespace PooketSlug
{
    /// <summary>The cutscenes' cue sounds (<see cref="CueSound"/>): which of the game's clips each one is, and playing it.</summary>
    public static class CueSounds
    {
        public static void Play(CueSound sound, float volume)
        {
            AudioClip clip = ClipOf(sound);
            if (clip != null) Sfx.Play(clip, volume);
        }

        static AudioClip ClipOf(CueSound sound)
        {
            switch (sound)
            {
                case CueSound.Thump: return Sfx.Thump;
                case CueSound.Glitch: return Sfx.Glitch;
                case CueSound.Splat: return Sfx.Splat;
                case CueSound.Pickup: return Sfx.Pickup;
                case CueSound.Hit: return Sfx.Hit;
                case CueSound.Servo: return Sfx.Servo;
                case CueSound.RocketRumble: return Sfx.RocketRumble;
                case CueSound.Charge: return Sfx.Charge;
                case CueSound.Siren: return Sfx.Siren;
                case CueSound.IntroRise: return Sfx.IntroRise;
                case CueSound.Intro: return Sfx.Intro;
                default: return null;
            }
        }
    }
}
