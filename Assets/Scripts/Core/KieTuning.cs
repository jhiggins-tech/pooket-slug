using UnityEngine;

namespace PooketSlug
{
    /// <summary>kie's kit as numbers (<c>docs/design/kits.md</c>): Weasel Pop and Trollogram so far.</summary>
    public static class KieTuning
    {
        public static readonly WeaselPopSpec Weasels = new WeaselPopSpec();
        public static readonly TrollogramSpec Trollogram = new TrollogramSpec();
    }

    /// <summary>
    /// kie's special, Trollogram, as numbers and rules. SP drops a hologram of kie where he stands (a charge), and the
    /// goons shoot it instead of him while it's on screen. SP again, while it stands, swaps him and it (free). The
    /// first thing that hits it blows it up on every goon nearby. These are the ones that went rogue.
    /// </summary>
    public sealed class TrollogramSpec
    {
        /// <summary>Seconds a hologram stands before it switches itself off.</summary>
        public float Life = 12f;
        /// <summary>Seconds between swaps, so a held or doubled tap doesn't swap straight back.</summary>
        public float SwapCooldown = 0.35f;
        /// <summary>Seconds kie can't be hurt after a swap, while he glitches into place.</summary>
        public float SwapShield = 0.3f;
        /// <summary>The blast when it's shot: its reach, damage and size on screen.</summary>
        public float BlastRadius = 2.2f, BlastSize = 1.2f;
        public int Damage = 4;

        /// <summary>What SP does: swap with a hologram that's up (once the cooldown allows), or drop one with a charge.</summary>
        public TrollAction OnSpecial(bool decoyUp, float sinceSwap, int charges)
        {
            if (decoyUp) return sinceSwap >= SwapCooldown ? TrollAction.Swap : TrollAction.Nothing;
            return charges > 0 ? TrollAction.Drop : TrollAction.Nothing;
        }

        /// <summary>Whether goons take the hologram for kie: it's up, and on screen (between <paramref name="left"/> and <paramref name="right"/>).</summary>
        public static bool Draws(bool up, float x, float left, float right) => up && x >= left && x <= right;
    }

    /// <summary>What a press of SP does with Trollogram (<see cref="TrollogramSpec.OnSpecial"/>).</summary>
    public enum TrollAction { Nothing, Drop, Swap }

    /// <summary>
    /// kie's primary, Weasel Pop, as numbers and rules. Each press tumbles out three weasels, lobbed along the aim
    /// (so they reach over cover). Where each lands it scurries under the nearest goon on the ground within reach
    /// and pops, to Pop Goes the Weasel; one with nothing to hunt pops when its fuse runs out. Flyers are out of
    /// reach: the weasels only hunt along the ground, and the pop is small.
    /// </summary>
    public sealed class WeaselPopSpec
    {
        /// <summary>Weasels a press tumbles out, and degrees between neighbours (Tabks' ±4°, opened up a little).</summary>
        public int Count = 3;
        public float SpreadDegrees = 7f;
        /// <summary>The throw: speed along the aim, plus a lift upward so a level aim lobs, and how much of kie's own speed it keeps.</summary>
        public float ThrowSpeed = 8.5f, Lift = 6.5f, Carry = 0.3f;
        /// <summary>Each weasel after the first is thrown this much softer, so a volley lands spread out.</summary>
        public float Soften = 0.12f;
        /// <summary>The weasels' own gravity while tumbling, floatier than a soldier's.</summary>
        public float Gravity = 26f;
        /// <summary>Seconds between presses, and the most weasels kie can have out at once.</summary>
        public float Cooldown = 0.5f;
        public int MaxOut = 9;
        /// <summary>How fast a landed weasel scurries, and how far ahead and up or down it will hunt a goon.</summary>
        public float ScurrySpeed = 6.5f, HuntRange = 9f, HuntHeight = 2.5f;
        /// <summary>Seconds after landing before a weasel pops anyway, and in the air before it gives up tumbling.</summary>
        public float Fuse = 2.5f, AirTime = 3f;
        /// <summary>How close under a goon (from its middle) a weasel gets before it pops.</summary>
        public float Reach = 0.35f;
        /// <summary>The pop: its reach, damage, and size on screen.</summary>
        public float BlastRadius = 1.1f, BlastSize = 0.55f;
        public int Damage = 2;
        /// <summary>Seconds that the tune, once played, waits before another pop plays it again (plain pops in between).</summary>
        public float TuneGap = 1.6f;

        /// <summary>
        /// The throws of one press, from <paramref name="aim"/> (a unit vector) by a thrower moving at
        /// <paramref name="carry"/>: each weasel turned a little off the aim, softer one by one, plus a lift.
        /// </summary>
        public Vector2[] Volley(Vector2 aim, Vector2 carry)
        {
            var throws = new Vector2[Count];
            for (int i = 0; i < Count; i++)
            {
                float turn = (i - (Count - 1) * 0.5f) * SpreadDegrees * Mathf.Deg2Rad;
                float cos = Mathf.Cos(turn), sin = Mathf.Sin(turn);
                var dir = new Vector2(aim.x * cos - aim.y * sin, aim.x * sin + aim.y * cos);
                float speed = ThrowSpeed * (1f - Soften * i);
                throws[i] = dir * speed + new Vector2(carry.x * Carry, Lift);
            }
            return throws;
        }

        /// <summary>
        /// Whether a weasel at <paramref name="weasel"/> (its feet) will hunt a goon whose feet are at
        /// <paramref name="goon"/>: on the ground, in reach along it, and not much higher or lower.
        /// </summary>
        public bool CanHunt(Vector2 weasel, Vector2 goon, bool flies) =>
            !flies && Mathf.Abs(goon.x - weasel.x) <= HuntRange && Mathf.Abs(goon.y - weasel.y) <= HuntHeight;

        /// <summary>Close enough under the goon (its middle at <paramref name="goonX"/>) to pop.</summary>
        public bool Under(float weaselX, float goonX, float goonHalfWidth) => Mathf.Abs(goonX - weaselX) <= Mathf.Max(Reach, goonHalfWidth * 0.6f);
    }
}
