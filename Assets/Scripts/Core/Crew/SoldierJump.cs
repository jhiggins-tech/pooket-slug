// A soldier's jump: coyote time (a jump just after running off a ledge still counts), a buffered press (a jump pressed
// just before landing still counts), dropping down through a bridge, and the short hop when the button's let go. Your
// soldier and a boss's Trollogram pilot jump by the same rules.
using UnityEngine;

namespace PooketSlug
{
    /// <summary>A soldier's jump timers, ticked once a frame.</summary>
    public struct SoldierJump
    {
        float m_Coyote, m_Buffer, m_DropThrough;

        /// <summary>Dropping down through a bridge (the tiles let them through).</summary>
        public bool DroppingThrough => m_DropThrough > 0f;

        /// <summary>
        /// One frame: true when a jump goes now (pressed lately, and on the ground lately), which spends both. The caller
        /// either jumps or, holding down on a bridge, calls <see cref="Drop"/>. <paramref name="can"/> false holds it (rooted).
        /// </summary>
        public bool Tick(float dt, bool grounded, bool pressed, bool can = true)
        {
            m_Coyote = grounded ? Tuning.Soldier.CoyoteTime : m_Coyote - dt;
            m_Buffer = pressed ? Tuning.Soldier.JumpBuffer : m_Buffer - dt;
            m_DropThrough -= dt;
            if (m_Buffer <= 0f || m_Coyote <= 0f || !can) return false;
            m_Buffer = m_Coyote = 0f;
            return true;
        }

        /// <summary>Down through the bridge underfoot instead of jumping.</summary>
        public void Drop() => m_DropThrough = Tuning.Soldier.DropThroughTime;

        /// <summary>The gravity on a soldier rising at <paramref name="vy"/>: heavier when the jump's let go early, for a short hop (not while a move carries them up).</summary>
        public static float Gravity(float vy, bool held, bool carried = false)
            => vy > 0f && !held && !carried ? Tuning.Physics.Gravity * Tuning.Soldier.ShortHopGravity : Tuning.Physics.Gravity;
    }
}
