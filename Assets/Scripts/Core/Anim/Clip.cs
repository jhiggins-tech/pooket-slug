// A clip: an animation's frames played at a rate, looped or held on its last, with named events on frames (a foot
// down, a muzzle flash). Plain C#: what a sprite shows at a time, not the sprites themselves.
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>Frames at a rate, looping or holding the last, with events on frames.</summary>
    public sealed class Clip
    {
        public readonly string Name;
        /// <summary>How many frames it has (the sprites' count, where it's drawn from an array).</summary>
        public readonly int Frames;
        /// <summary>Frames a second.</summary>
        public readonly float Fps;
        /// <summary>Round again after the last frame, or stay on it.</summary>
        public readonly bool Loop;
        /// <summary>Events by frame: what happens as the clip reaches that frame.</summary>
        public readonly IReadOnlyDictionary<int, string> Events;

        public Clip(string name, int frames, float fps, bool loop = true, IReadOnlyDictionary<int, string> events = null)
        {
            Name = name;
            Frames = frames < 1 ? 1 : frames;
            Fps = fps;
            Loop = loop;
            Events = events ?? new Dictionary<int, string>();
        }

        /// <summary>How long one pass takes, in seconds.</summary>
        public float Length => Fps > 0f ? Frames / Fps : 0f;

        /// <summary>The frame it's on <paramref name="t"/> seconds in.</summary>
        public int Frame(float t) => Frame(t, Frames);

        /// <summary>The frame it's on, of <paramref name="frames"/> (an art's own count, where that's what it draws from).</summary>
        public int Frame(float t, int frames)
        {
            if (frames < 1) return 0;
            int f = (int)(t * Fps);
            if (f < 0) return 0;
            return Loop ? f % frames : f < frames ? f : frames - 1;
        }

        /// <summary>The sprite (or anything) it's on, from an array of frames.</summary>
        public T Pick<T>(T[] frames, float t) => frames[Frame(t, frames.Length)];

        /// <summary>Whether it's played to its end (a clip that loops never does).</summary>
        public bool Done(float t) => !Loop && t * Fps >= Frames;

        /// <summary>The events of the frames reached after <paramref name="from"/>, up to and at <paramref name="to"/> (seconds in), in order.</summary>
        public IEnumerable<string> Between(float from, float to)
        {
            int a = (int)(from * Fps), b = (int)(to * Fps);
            if (from < 0f) a = -1;
            if (!Loop) b = b < Frames ? b : Frames - 1;
            for (int f = a + 1; f <= b; f++)
                if (Events.TryGetValue(Loop ? f % Frames : f, out string e)) yield return e;
        }
    }

    /// <summary>The clips that are shared: who runs at what rate (each one's own, as they always were).</summary>
    public static class Clips
    {
        /// <summary>Your soldier's legs, running (and a partner's, and a pilot's).</summary>
        public static readonly Clip SoldierRun = new Clip("run", 4, 12f);
        /// <summary>Mission one's riflemen.</summary>
        public static readonly Clip RiflemanRun = new Clip("run", 4, 8f);
        /// <summary>Runners: quick little legs.</summary>
        public static readonly Clip RunnerRun = new Clip("run", 4, 14f);
        /// <summary>The creamery's shield soldiers and grenadiers, weighed down.</summary>
        public static readonly Clip CreameryRun = new Clip("run", 4, 6f);
        /// <summary>The crew walking round the den and the bay, and in its beats.</summary>
        public static readonly Clip StageWalk = new Clip("walk", 4, 10f);
    }
}
