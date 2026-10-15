using System;
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The music player (<c>docs/design/music.md</c>): one looping source for the tracks and one for the stings, each
    /// track rendered on the chip synth the first time it's wanted (<see cref="MusicRender"/>). It sits under the sound
    /// effects, and goes quiet while paused. First cut: only <c>?spike=music</c> plays anything.
    /// </summary>
    public static class Music
    {
        /// <summary>The music's volume: about half the sound effects'.</summary>
        const float Volume = 0.45f, PausedVolume = 0.15f, FadeTime = 0.35f;

        static AudioSource s_Loop, s_Sting;
        static readonly Dictionary<MusicTrack, AudioClip> s_Clips = new Dictionary<MusicTrack, AudioClip>();
        static MusicTrack s_Playing;
        static float s_Fade = -1f;
        static bool s_Muted, s_Paused;

        /// <summary>The track playing, or null.</summary>
        public static MusicTrack Playing => s_Playing;

        public static bool Muted
        {
            get => s_Muted;
            set
            {
                s_Muted = value;
                if (s_Loop != null) s_Loop.mute = value;
                if (s_Sting != null) s_Sting.mute = value;
            }
        }

        public static bool Paused
        {
            get => s_Paused;
            set
            {
                s_Paused = value;
                if (s_Loop != null && s_Fade < 0f) s_Loop.volume = value ? PausedVolume : Volume;
            }
        }

        public static void Init(GameObject host)
        {
            try
            {
                s_Loop = host.AddComponent<AudioSource>();
                s_Loop.playOnAwake = false;
                s_Loop.spatialBlend = 0f;
                s_Loop.loop = true;
                s_Loop.volume = Volume;
                s_Sting = host.AddComponent<AudioSource>();
                s_Sting.playOnAwake = false;
                s_Sting.spatialBlend = 0f;
                s_Sting.volume = Volume;
            }
            catch (Exception e)
            {
                Log.Audio.Warn("Music disabled: " + e.Message);
                s_Loop = null;
                s_Sting = null;
            }
        }

        /// <summary>Renders <paramref name="tracks"/> now, so playing them later doesn't stutter.</summary>
        public static void Prepare(params MusicTrack[] tracks)
        {
            foreach (MusicTrack t in tracks) ClipOf(t);
        }

        /// <summary>Loops <paramref name="track"/> from the start, after <paramref name="delay"/> seconds (a sting first).</summary>
        public static void Play(MusicTrack track, float delay = 0f)
        {
            if (s_Loop == null || track == null) return;
            if (s_Playing == track && s_Loop.isPlaying && s_Fade < 0f) return;
            AudioClip clip = ClipOf(track);
            if (clip == null) return;
            s_Playing = track;
            s_Fade = -1f;
            s_Loop.Stop();
            s_Loop.clip = clip;
            s_Loop.volume = s_Paused ? PausedVolume : Volume;
            s_Loop.PlayDelayed(delay);
            Log.Audio.Info("Music: " + track.Name);
        }

        /// <summary>Stops the track: at once (the boss intro cuts it), or with a quick fade.</summary>
        public static void Stop(bool fade = false)
        {
            if (s_Loop == null || s_Playing == null) return;
            Log.Audio.Info("Music stopped: " + s_Playing.Name);
            s_Playing = null;
            if (fade && s_Loop.isPlaying) s_Fade = FadeTime;
            else
            {
                s_Fade = -1f;
                s_Loop.Stop();
            }
        }

        /// <summary>Plays a sting once, over whatever's playing.</summary>
        public static void Sting(MusicTrack sting)
        {
            if (s_Sting == null || sting == null) return;
            AudioClip clip = ClipOf(sting);
            if (clip == null) return;
            s_Sting.Stop();
            s_Sting.clip = clip;
            s_Sting.Play();
            Log.Audio.Info("Music sting: " + sting.Name);
        }

        /// <summary>Stops everything: the track and any sting.</summary>
        public static void Silence()
        {
            Stop();
            if (s_Sting != null) s_Sting.Stop();
        }

        /// <summary>Moves a fade on; call every frame.</summary>
        public static void Tick(float dt)
        {
            if (s_Fade < 0f || s_Loop == null) return;
            s_Fade -= dt;
            if (s_Fade <= 0f)
            {
                s_Fade = -1f;
                s_Loop.Stop();
                return;
            }
            s_Loop.volume = (s_Paused ? PausedVolume : Volume) * s_Fade / FadeTime;
        }

        static AudioClip ClipOf(MusicTrack track)
        {
            if (s_Clips.TryGetValue(track, out AudioClip clip)) return clip;
            try
            {
                float started = Time.realtimeSinceStartup;
                clip = Sfx.Clip("music-" + track.Name, MusicRender.Render(track), Chip.Rate);
                Log.Audio.Info($"Rendered {track.Name}: {track.Seconds:0.0} s in {(Time.realtimeSinceStartup - started) * 1000f:0} ms");
            }
            catch (Exception e)
            {
                Log.Audio.Warn("Couldn't render " + track.Name + ": " + e.Message);
                clip = null;
            }
            s_Clips[track] = clip;
            return clip;
        }
    }
}
