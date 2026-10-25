using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// tones2's tabk's guns as numbers (<c>docs/design/tabks/tones2.md</c>): tabk-sized versions of his kit,
    /// 5, 3 and 1 rounds as every tabk has (<see cref="PlayerTankSpec"/>). The tabk itself, its health, fuel and drive,
    /// is <see cref="PlayerTankSpec"/>; these are only what it fires.
    /// </summary>
    public sealed class Tones2TabkSpec
    {
        /// <summary>
        /// ten-1, the water cannon (FIRE): one round is one spray cycle, as in Pooket Tabks. The pressure builds in
        /// spurts, holds, then eases off; the droplets are his jet's, bigger and harder.
        /// </summary>
        public float RampUp = 1f, Hold = 0.8f, RampDown = 1f;
        /// <summary>How many spurts the pressure builds in.</summary>
        public int Spurts = 4;
        public readonly JetSpec Water = new JetSpec
        {
            MinSpeed = 7f, MaxSpeed = 20f, MaxSpread = 26f, MinSpread = 2f, MaxGravity = 28f, MinGravity = 7f,
            MinDropsPerSecond = 40f, MaxDropsPerSecond = 90f, MinDamage = 0.06f, MaxDamage = 0.22f,
            MinPush = 0.5f, MaxPush = 1.8f, MaxShove = 9f, DropLife = 1.5f,
        };

        /// <summary>Seconds a spray cycle lasts.</summary>
        public float CycleLength => RampUp + Hold + RampDown;

        /// <summary>The water's pressure, 0 to 1, <paramref name="t"/> seconds into a cycle: 0 once it's over.</summary>
        public float CyclePressure(float t)
        {
            if (t < 0f || t >= CycleLength) return 0f;
            if (t < RampUp)
            {
                // in spurts: each one surges up a step, then levels off before the next
                float u = t / RampUp * Spurts, done = Mathf.Floor(u);
                float p = (done + Mathf.Sqrt(u - done)) / Spurts;
                return Mathf.Clamp01(Mathf.Max(0.08f, p));
            }
            if (t < RampUp + Hold) return 1f;
            float d = (t - RampUp - Hold) / RampDown;
            return 1f - d * d * (3f - 2f * d);
        }

        /// <summary>
        /// ten-2, the mud jump (SP), as in Pooket Tabks: it shakes while it builds up, then blasts off along the aim like
        /// a jetpack and comes down somewhere new, the exhaust carpeting the launch spot in toxic mud.
        /// </summary>
        public float BuildTime = 1f;
        /// <summary>Launch speed along the aim. A tabk's hop is 12; straight up this goes about three times as high.</summary>
        public float JumpSpeed = 21f;
        /// <summary>Its mud: wider than the soldier's rocket jump, and it stays longer.</summary>
        public readonly RocketSpec Mud = new RocketSpec { MudHalfWidth = 3.2f, MudLife = 9f, PoisonInterval = 0.4f };

        /// <summary>
        /// Which way the jump goes for an aim: the upper five ways as they are, except that a flat aim launches long
        /// and low, and a downward one straight up.
        /// </summary>
        public static Vector2 JumpDirection(Vector2 aim, int facing)
        {
            if (aim.y < -0.01f) return Vector2.up;
            if (aim.y < 0.01f) return new Vector2(Mathf.Sign(aim.x == 0f ? facing : aim.x), 0.5f).normalized;
            return aim.normalized;
        }

        /// <summary>ten-3, the spew (SU): twice as long as on foot (<see cref="SpewSpec"/>), and he can drive while it pours.</summary>
        public readonly SpewSpec Spew = new SpewSpec
        {
            Duration = 2f * new SpewSpec().Duration, DropsPerSecond = 110f, Speed = 12f, Spread = 20f, Gravity = 18f,
            Damage = 0.6f, Push = 0.6f,
        };
    }
}
