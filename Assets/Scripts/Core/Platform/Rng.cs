using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A small seeded random-number source (xorshift32). Each system owns one, so the same seed replays the
    /// same choices: gameplay rolls use the world's own Rng, and cosmetic effects use <see cref="Fx"/>.
    /// Replays and generated levels will build on this.
    /// </summary>
    public sealed class Rng
    {
        /// <summary>Cosmetic randomness (particles, screen shake) that never changes how the game plays.</summary>
        public static readonly Rng Fx = new Rng(NewSeed());

        uint m_State;

        public Rng(uint seed)
        {
            m_State = seed != 0 ? seed : 0x9E3779B9u; // xorshift is stuck at zero
        }

        /// <summary>A seed from the clock, for runs nobody needs to reproduce.</summary>
        public static uint NewSeed() => unchecked((uint)System.DateTime.UtcNow.Ticks * 2654435761u) | 1u;

        public uint NextUInt()
        {
            uint x = m_State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return m_State = x;
        }

        /// <summary>A float in [0, 1).</summary>
        public float Value => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>A float in [min, max).</summary>
        public float Range(float min, float max) => min + (max - min) * Value;

        /// <summary>An int in [min, max), or min when the range is empty.</summary>
        public int Range(int min, int max) => max <= min ? min : min + (int)(NextUInt() % (uint)(max - min));

        /// <summary>A point inside the unit circle.</summary>
        public Vector2 InsideUnitCircle()
        {
            float angle = Value * Mathf.PI * 2f, radius = Mathf.Sqrt(Value);
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }
    }
}
