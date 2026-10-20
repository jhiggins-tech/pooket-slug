// A timed hazard's beat: on for part of every cycle, with an optional warning before it goes, each one a little out of
// step with its neighbours by where it stands, so a row of them doesn't pulse as one. It runs off the world's clock,
// so every phone sees it on the same beat without anything going over the network.
using UnityEngine;

namespace PooketSlug
{
    /// <summary>On for <see cref="OnFor"/> of every <see cref="Cycle"/> seconds, warning for <see cref="Tell"/> first.</summary>
    public readonly struct HazardBeat
    {
        public readonly float Cycle, OnFor, Tell;
        /// <summary>How far out of step it's put by where it stands: a second of the beat for every so many tiles.</summary>
        public readonly float Spread;

        public HazardBeat(float cycle, float onFor, float tell = 0f, float spread = 0.53f)
        {
            Cycle = cycle;
            OnFor = onFor;
            Tell = tell;
            Spread = spread;
        }

        /// <summary>How far into its cycle the one at <paramref name="x"/> is.</summary>
        public float Phase(float elapsed, float x) => Mathf.Repeat(elapsed + Offset(x), Cycle);

        /// <summary>It's on (live, blasting).</summary>
        public bool On(float elapsed, float x) => Phase(elapsed, x) < OnFor;

        /// <summary>It's about to go: the warning.</summary>
        public bool Soon(float elapsed, float x) => Tell > 0f && Phase(elapsed, x) >= Cycle - Tell;

        /// <summary>A new cycle began in the <paramref name="dt"/> seconds up to <paramref name="elapsed"/> (a dropper lets one go).</summary>
        public bool Went(float elapsed, float dt, float x)
        {
            if (dt <= 0f) return false;
            float o = Offset(x);
            return Mathf.FloorToInt((elapsed + o) / Cycle) != Mathf.FloorToInt((elapsed - dt + o) / Cycle);
        }

        float Offset(float x) => Mathf.Repeat(x * Spread, Cycle);
    }

    /// <summary>Mission one's timed hazards (Mission two's are in <see cref="Creamery"/>).</summary>
    public static class BaseHazards
    {
        /// <summary>The live fence: live 1.4 s of every 3.</summary>
        public static readonly HazardBeat Fence = new HazardBeat(3f, 1.4f, spread: 0.37f);
        /// <summary>A steam vent: blasts 1 s of every 2.6.</summary>
        public static readonly HazardBeat Steam = new HazardBeat(2.6f, 1f);
    }
}
