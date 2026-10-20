using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's tabk's guns as numbers (<c>docs/design/tanks.md</c>, kcaj's tabk): his kit, tabk-sized, 5, 3 and 1 rounds
    /// as every tabk has (<see cref="PlayerTankSpec"/>), all from the soft-serve machine's nozzle. The swirl on top
    /// wobbles while a shot charges.
    /// </summary>
    public sealed class KcajTabkSpec
    {
        /// <summary>
        /// Double Park (FIRE): a round wobbles the swirl for <see cref="Charge"/> seconds, then the nozzle pipes out
        /// <see cref="Pairs"/> twin cones, <see cref="Gap"/> seconds apart, lobbed either side of the aim and further than
        /// his own. Each cone is his cone, splat and all (<see cref="Content.DoublePark"/>).
        /// </summary>
        public float Charge = 0.2f, Gap = 0.12f;
        public int Pairs = 3;
        public readonly DoubleParkSpec Cones = new DoubleParkSpec { Speed = 14f, Lift = 28f, Spread = 7f, Carry = 0.3f };

        /// <summary>
        /// Hyperfixate (SP): the beam shines from the nozzle and sticks to the first enemy it touches, as on foot
        /// (<see cref="Hyperfixation"/>), further and hotter. While it holds, the tabk stands still. A beam that touches
        /// nothing fizzles and the round isn't spent.
        /// </summary>
        public readonly HyperfixateSpec Beam = new HyperfixateSpec { Range = 14f, BreakRange = 17f, FirstTick = 0.3f, HottestTick = 3f, MaxHold = 5f };

        /// <summary>
        /// Unmedicated (SU): the serving hatch flies open and pills rain across the screen, as on foot
        /// (<see cref="PillRain"/>), the same rain popping harder and wider.
        /// </summary>
        public readonly UnmedicatedSpec Pills = new UnmedicatedSpec { PopDamage = 0.4f, PopRadius = 1f };

        /// <summary>How many pairs of a volley go between <paramref name="before"/> and <paramref name="after"/> seconds into it.</summary>
        public int PairsDue(float before, float after)
        {
            int due = 0;
            for (int i = 0; i < Pairs; i++)
            {
                float at = Charge + i * Gap;
                if (at > before && at <= after) due++;
            }
            return due;
        }

        /// <summary>When a volley's done: its last pair's gone.</summary>
        public float VolleyTime => Charge + (Pairs - 1) * Gap;

        /// <summary>The swirl wobbles while a volley charges and pipes, and while the beam holds.</summary>
        public static bool SwirlWobbles(bool volley, bool fixated) => volley || fixated;
    }
}
