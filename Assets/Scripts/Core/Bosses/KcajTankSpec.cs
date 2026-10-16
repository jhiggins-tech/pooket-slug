using UnityEngine;

namespace PooketSlug
{
    /// <summary>What kcaj's boss tabk does with a charged shot (<see cref="KcajTankSpec.AttackFor"/>).</summary>
    public enum KcajAttack { Shell, Sweep, Rain }

    /// <summary>
    /// kcaj's boss tabk, Mission two's (<c>docs/design/tanks.md</c>, Tabk bosses: "It sweeps a beam, then rains pills
    /// across the screen"): the artillery tabk with kcaj's kit in place of some of its shells. Its charged shots go
    /// shell, sweep, shell, rain, round again. The sweep is Hyperfixate turned on the floor: the turret locks a beam
    /// onto the ground in front of it and drags it out across the arena, and anyone it crosses takes a heart, so you
    /// jump it. The rain is Unmedicated from the turret: a handful of pills lobbed high over the arena that bounce
    /// once and pop, a heart each on a direct hit. It still gets quicker as it's hurt (<see cref="BossTankSpec.Rage"/>).
    /// The numbers and rules, kept apart from the tabk so the tests can reach them.
    /// </summary>
    public sealed class KcajTankSpec
    {
        /// <summary>Seconds the beam flickers on the floor where the sweep starts (the tell), then sweeps.</summary>
        public float SweepTell = 0.7f, SweepTime = 1.4f;
        /// <summary>Where the sweep starts and stops, in front of the tabk, and the beam's thickness.</summary>
        public float SweepNear = 2.5f, SweepFar = 14f, BeamHalfWidth = 0.12f;
        /// <summary>Hearts the beam takes from a soldier it crosses (once a sweep).</summary>
        public int SweepDamage = 1;

        /// <summary>The tabk's rain: fewer, bigger pills than kcaj's own, that hurt soldiers and pop on the second landing.</summary>
        public readonly UnmedicatedSpec Rain = new UnmedicatedSpec
        {
            Count = 16, RainTime = 1.6f, MinFall = 1f, MaxFall = 3f, MaxDrift = 1f, Landings = 2,
            Radius = 0.2f, PopRadius = 0.5f, PopDamage = 1f, HurtsSoldiers = true,
        };

        /// <summary>
        /// The attack for its <paramref name="turn"/>th charged shot (from 0): shell, sweep, shell, rain, round again.
        /// While its pills are still falling, a rain turn is a plain shell.
        /// </summary>
        public static KcajAttack AttackFor(int turn, bool raining)
        {
            switch (((turn % 4) + 4) % 4)
            {
                case 1: return KcajAttack.Sweep;
                case 3: return raining ? KcajAttack.Shell : KcajAttack.Rain;
                default: return KcajAttack.Shell;
            }
        }

        /// <summary>How far along the sweep is (0 to 1), <paramref name="t"/> seconds after the tell ended, at <paramref name="rage"/>.</summary>
        public float SweepProgress(float t, float rage) => Mathf.Clamp01(t * rage / SweepTime);

        /// <summary>Where the beam touches the floor, <paramref name="progress"/> through the sweep, for a tabk at <paramref name="tankX"/> facing <paramref name="facing"/>.</summary>
        public float BeamFloorX(float tankX, int facing, float progress) => tankX + facing * Mathf.Lerp(SweepNear, SweepFar, progress);

        /// <summary>
        /// Does the beam from <paramref name="muzzle"/> to <paramref name="floor"/> cross the box at
        /// <paramref name="feet"/> (<paramref name="halfWidth"/> either side, <paramref name="height"/> tall)?
        /// </summary>
        public bool Hits(Vector2 muzzle, Vector2 floor, Vector2 feet, float halfWidth, float height)
        {
            Vector2 d = floor - muzzle;
            float len = d.magnitude;
            if (len < 1e-4f) return false;
            Vector2 dir = d / len;
            var centre = new Vector2(feet.x, feet.y + height * 0.5f);
            var half = new Vector2(halfWidth + BeamHalfWidth, height * 0.5f + BeamHalfWidth);
            float at = HyperfixateSpec.RayBox(muzzle, dir, centre, half);
            return at >= 0f && at <= len;
        }
    }
}
