using UnityEngine;

namespace PooketSlug
{
    /// <summary>The other player's soldier: a puppet that follows its replicated position and pose.</summary>
    public sealed class RemotePlayer : Actor
    {
        public readonly IAvatarLink Link;
        readonly PlayerVisual m_Visual;
        AvatarPose m_Pose;
        float m_Anim, m_DeadTime;
        bool m_Placed;

        public RemotePlayer(World world, IAvatarLink link)
            : base(world, "RemotePlayer", link.Pos, Content.Trooper.HalfWidth, Content.Trooper.StandHeight, Content.Trooper.MaxHp)
        {
            Link = link;
            m_Visual = new PlayerVisual(Visual, Art.PlayerSkins[Mathf.Clamp(link.Skin, 0, 1)], SortOrder.Player - 5);
        }

        public override bool IsDown => m_Pose.Dead;
        public bool Invulnerable => m_Pose.Blink || m_Pose.Dead;

        public override void Tick(float dt)
        {
            m_Pose = AvatarPose.Unpack(Link.PoseFlags, m_Anim, m_DeadTime);
            m_Anim += dt;
            m_DeadTime = m_Pose.Dead ? m_DeadTime + dt : 0f;
            Facing = m_Pose.Facing;
            Height = m_Pose.Crouching ? Content.Trooper.CrouchHeight : Content.Trooper.StandHeight;
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
        }

        public override void Render()
        {
            base.Render();
            m_Visual.Skin = Art.PlayerSkins[Mathf.Clamp(Link.Skin, 0, 1)];
            m_Visual.Apply(m_Pose);
        }
    }
}
