using UnityEngine;

namespace PooketSlug
{
    /// <summary>The other player's soldier: a puppet that follows its replicated position and pose.</summary>
    public sealed class RemotePlayer : Actor
    {
        public readonly IAvatarLink Link;
        readonly PlayerVisual m_Visual;
        readonly Tones2Copy m_Tones2; // their liquids, as harmless copies
        AvatarPose m_Pose;
        float m_Anim, m_DeadTime;
        bool m_Placed;

        public RemotePlayer(World world, IAvatarLink link)
            : base(world, "RemotePlayer", link.Pos, link.Soldier.HalfWidth, link.Soldier.StandHeight, link.Soldier.MaxHp)
        {
            Link = link;
            m_Visual = new PlayerVisual(Visual, Art.SkinFor(link.Soldier, link.Skin), SortOrder.Player - 5);
            if (link.Soldier.Primary.Behaviour == WeaponBehaviour.Jet) m_Tones2 = new Tones2Copy(this);
        }

        public override bool IsDown => m_Pose.Dead;
        public bool Invulnerable => m_Pose.Blink || m_Pose.Dead;
        /// <summary>Out of lives: a 1-up goes to them first.</summary>
        public bool OutOfLives => m_Pose.Out;
        /// <summary>Out of lives and continues.</summary>
        public bool Gone => m_Pose.Gone;

        public override void Tick(float dt)
        {
            m_Pose = AvatarPose.Unpack(Link.PoseFlags, m_Anim, m_DeadTime);
            m_Anim += dt;
            m_DeadTime = m_Pose.Dead ? m_DeadTime + dt : 0f;
            Facing = m_Pose.Facing;
            Height = m_Pose.Crouching ? Link.Soldier.CrouchHeight : Link.Soldier.StandHeight;
            Hp = Link.Hp;

            // Smooth toward the replicated position, extrapolating a little with its velocity.
            Vector2 target = Link.Pos + Link.Vel * Tuning.Coop.RemoteSoldierLead;
            const float snap = Tuning.Coop.RemoteSoldierSnap;
            if (!m_Placed || (target - Pos).sqrMagnitude > snap * snap)
            {
                Pos = target;
                m_Placed = true;
            }
            else
            {
                Pos = Vector2.Lerp(Pos, target, 1f - Mathf.Exp(-Tuning.Coop.RemoteSoldierSmoothing * dt));
            }
            m_Tones2?.Tick(dt, m_Pose, Link.Vel);
        }

        public override void Render()
        {
            base.Render();
            if (m_Tones2 != null) Go.transform.localPosition += (Vector3)Tones2Copy.ShakeOffset(m_Pose);
            m_Visual.Skin = Art.SkinFor(Link.Soldier, Link.Skin);
            m_Visual.Apply(m_Pose);
        }
    }
}
