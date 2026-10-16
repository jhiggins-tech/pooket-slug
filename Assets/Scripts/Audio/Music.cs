using System;
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The music player (<c>docs/design/music.md</c>): one looping source for the tracks and one for the stings, each
    /// track rendered on the chip synth the first time it's wanted (<see cref="MusicRender"/>). It sits under the sound
    /// effects, and goes quiet while paused. A track can have a layer, a recording of the same notes in step with it
    /// (the boss's final section played by an orchestra, from the vendor repository's CI into <c>Resources/Music</c>),
    /// which it crossfades to after a while. First cut: only <c>?spike=music</c> plays anything.
    /// </summary>
    public static class Music
    {
        /// <summary>The music's volume: about half the sound effects'.</summary>
        const float Volume = 0.45f, PausedVolume = 0.15f, FadeTime = 0.35f, CrossTime = 4f;

        static AudioSource s_Loop, s_Sting, s_Layer;
        static readonly Dictionary<MusicTrack, AudioClip> s_Clips = new Dictionary<MusicTrack, AudioClip>();
        static readonly Dictionary<string, AudioClip> s_Layers = new Dictionary<string, AudioClip>();
        static MusicTrack s_Playing;
        static float s_Fade = -1f;
        /// <summary>Seconds until the crossfade to the layer starts (below 0: none coming), and how far it's got (0..1).</summary>
        static float s_CrossIn = -1f, s_Mix;
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
                if (s_Layer != null) s_Layer.mute = value;
            }
        }

        public static bool Paused
        {
            get => s_Paused;
            set
            {
                s_Paused = value;
                ApplyVolume();
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
                s_Layer = host.AddComponent<AudioSource>();
                s_Layer.playOnAwake = false;
                s_Layer.spatialBlend = 0f;
                s_Layer.loop = true;
                s_Layer.volume = 0f;
            }
            catch (Exception e)
            {
                Log.Audio.Warn("Music disabled: " + e.Message);
                s_Loop = null;
                s_Sting = null;
                s_Layer = null;
            }
        }

        /// <summary>Renders <paramref name="tracks"/> now, so playing them later doesn't stutter.</summary>
        public static void Prepare(params MusicTrack[] tracks)
        {
            foreach (MusicTrack t in tracks) ClipOf(t);
        }

        /// <summary>
        /// A recorded layer for a track, <c>Resources/Music/&lt;id&gt;.bytes</c> (a WAV kept as raw bytes), or null when this
        /// build hasn't got it.
        /// </summary>
        public static AudioClip Layer(string id)
        {
            if (s_Layers.TryGetValue(id, out AudioClip clip)) return clip;
            TextAsset raw = Resources.Load<TextAsset>("Music/" + id);
            int rate = 0;
            float[] samples = raw != null ? Wav.Read(raw.bytes, out rate) : null;
            clip = samples != null ? Sfx.Clip("music-layer-" + id, samples, rate) : null;
            Log.Audio.Info(clip != null ? $"Music layer {id}: {clip.length:0.0} s" : "No music layer " + id + " in this build");
            s_Layers[id] = clip;
            return clip;
        }

        /// <summary>
        /// Loops <paramref name="track"/> from the start, after <paramref name="delay"/> seconds (a sting first). With a
        /// <paramref name="layer"/>, it plays alongside in step, silent, and the track crossfades to it
        /// <paramref name="layerAfter"/> seconds in.
        /// </summary>
        public static void Play(MusicTrack track, float delay = 0f, AudioClip layer = null, float layerAfter = 0f)
        {
            if (s_Loop == null || track == null) return;
            if (s_Playing == track && s_Loop.isPlaying && s_Fade < 0f) return;
            AudioClip clip = ClipOf(track);
            if (clip == null) return;
            s_Playing = track;
            s_Fade = -1f;
            s_Loop.Stop();
            s_Loop.clip = clip;
            s_Mix = 0f;
            s_CrossIn = -1f;
            if (s_Layer != null)
            {
                s_Layer.Stop();
                if (layer != null)
                {
                    s_Layer.clip = layer;
                    s_Layer.PlayDelayed(delay);
                    s_CrossIn = delay + layerAfter;
                }
            }
            ApplyVolume();
            s_Loop.PlayDelayed(delay);
            Log.Audio.Info("Music: " + track.Name + (layer != null ? " (with a layer to crossfade to)" : ""));
        }

        /// <summary>Stops the track: at once (the boss intro cuts it), or with a quick fade.</summary>
        public static void Stop(bool fade = false)
        {
            if (s_Loop == null || s_Playing == null) return;
            Log.Audio.Info("Music stopped: " + s_Playing.Name);
            s_Playing = null;
            s_CrossIn = -1f;
            if (fade && s_Loop.isPlaying) s_Fade = FadeTime;
            else
            {
                s_Fade = -1f;
                s_Loop.Stop();
                if (s_Layer != null) s_Layer.Stop();
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
            if (s_Loop == null) return;
            if (s_CrossIn >= 0f)
            {
                float was = s_CrossIn;
                s_CrossIn -= dt;
                if (was > 0f && s_CrossIn <= 0f) Log.Audio.Info("Music: crossfading to the layer");
                if (s_CrossIn <= 0f)
                {
                    s_Mix = Mathf.Min(1f, -s_CrossIn / CrossTime);
                    if (s_Mix >= 1f) s_CrossIn = -1f;
                }
            }
            if (s_Fade >= 0f)
            {
                s_Fade -= dt;
                if (s_Fade <= 0f)
                {
                    s_Fade = -1f;
                    s_Loop.Stop();
                    if (s_Layer != null) s_Layer.Stop();
                }
            }
            ApplyVolume();
        }

        /// <summary>The track's and its layer's volumes, from pause, any fade out and the crossfade.</summary>
        static void ApplyVolume()
        {
            if (s_Loop == null) return;
            float v = (s_Paused ? PausedVolume : Volume) * (s_Fade >= 0f ? s_Fade / FadeTime : 1f);
            s_Loop.volume = v * (1f - s_Mix);
            if (s_Layer != null) s_Layer.volume = v * s_Mix;
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
