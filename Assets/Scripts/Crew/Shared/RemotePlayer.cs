using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The other player's soldier: a puppet that follows its replicated position and pose. Their tank, if they have one
    /// out, comes with it (<see cref="TankPose"/>): drawn where it is, and while they drive it the soldier is hidden
    /// inside and takes the tank's size, so the goons' shots stop on it.
    /// </summary>
    public sealed class RemotePlayer : Actor
    {
        public readonly IAvatarLink Link;
        readonly PlayerVisual m_Visual;
        readonly Tones2Copy m_Tones2; // their liquids, as harmless copies
        AvatarPose m_Pose;
        readonly Presence m_Presence = new Presence();
        float m_Anim, m_DeadTime;
        bool m_Placed;
        readonly TankLook m_Tank;
        TankPose m_TankPose;
        Vector2 m_TankPos;
        float m_TankAngle, m_TankClock;
        int m_TanksBlown = -1;

        public RemotePlayer(World world, IAvatarLink link)
            : base(world, "RemotePlayer", link.Pos, link.Soldier.HalfWidth, link.Soldier.StandHeight, link.Soldier.MaxHp)
        {
            Link = link;
            m_Visual = new PlayerVisual(Visual, Art.SkinFor(link.Soldier, link.Skin), SortOrder.Player - 5);
            if (link.Soldier.Primary.Behaviour == WeaponBehaviour.Jet) m_Tones2 = new Tones2Copy(this);
            TabkKit tabk = TabkKit.For(link.Soldier); // the tabk their crewmate calls in
            m_Tank = new TankLook(Go.transform, SortOrder.Player - 4, tabk.Hull, tabk.Barrel);
            m_Tank.Shown = false;
        }

        /// <summary>Driving their tank: the soldier is inside it.</summary>
        public bool Driving => m_TankPose.State == TankPose.Mode.Driven;

        public override bool IsDown => m_Pose.Dead;
        public bool Invulnerable => m_Pose.Blink || m_Pose.Dead;
        /// <summary>Out of lives: a 1-up goes to them first.</summary>
        public bool OutOfLives => m_Pose.Out;
        /// <summary>Out of lives and continues.</summary>
        public bool Gone => m_Pose.Gone;
        /// <summary>Their super is charged (for a team super).</summary>
        public bool SuperReady => m_Pose.SuperReady && !m_Pose.Dead;
        /// <summary>They've switched away from the game, or it has stopped running on their phone (<see cref="Presence"/>).</summary>
        public bool Away => m_Presence.Away;
        /// <summary>Their health is low (<see cref="PartnerBeacon.IsLow"/>).</summary>
        public bool LowHealth => !m_Pose.Dead && PartnerBeacon.IsLow(Hp, MaxHp);

        public override void Tick(float dt)
        {
            m_Pose = AvatarPose.Unpack(Link.PoseFlags, m_Anim, m_DeadTime);
            m_Anim += dt;
            m_Presence.Tick(dt, m_Pose);
            m_DeadTime = m_Pose.Dead ? m_DeadTime + dt : 0f;
            Facing = m_Pose.Facing;
            Height = m_Pose.Crouching ? Link.Soldier.CrouchHeight : Link.Soldier.StandHeight;
            HalfW = Link.Soldier.HalfWidth;
            Hp = Link.Hp;
            TickTank(dt);

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

        /// <summary>Their tank: where it is, which way its gun points, and a blast when one of theirs blows up.</summary>
        void TickTank(float dt)
        {
            m_TankPose = TankPose.Unpack(Link.TankFlags);
            if (m_TanksBlown >= 0 && m_TankPose.Blown != m_TanksBlown)
            {
                // harmless here: the blast hurt the goons on their screen
                World.Combat.Explode(Link.TankPos + Vector2.up * Content.PlayerTank.Height * 0.5f, Content.PlayerTank.RunawayBlast, 0, false, 2f);
                PlayerTank.Blast(World, Link.TankPos);
            }
            m_TanksBlown = m_TankPose.Blown;
            if (m_TankPose.State == TankPose.Mode.None) return;
            float want = TankAim.Angle(TankPose.AimOf(m_TankPose.AimOctant));
            m_TankAngle = Mathf.MoveTowardsAngle(m_TankAngle, want, Content.PlayerTank.BarrelTurn * dt);
            m_TankClock += dt;
            m_TankPos = Link.TankPos;
            if (Driving)
            {
                HalfW = Content.PlayerTank.HalfWidth;
                Height = Content.PlayerTank.Height;
            }
        }

        public override void Render()
        {
            base.Render();
            bool tank = m_TankPose.State != TankPose.Mode.None;
            m_Tank.Shown = tank;
            if (Visual.gameObject.activeSelf == Driving) Visual.gameObject.SetActive(!Driving); // inside it, the soldier's hidden
            if (tank)
            {
                // driving, it goes where the soldier does (smoothed); otherwise where it was last sent
                Vector2 at = Driving ? Vector2.zero : m_TankPos - Pos;
                bool blink = m_TankPose.State == TankPose.Mode.Runaway && (int)(m_TankClock * 12f) % 2 == 0;
                m_Tank.Apply(at, m_TankPose.Facing, m_TankAngle, m_TankPose.Flash, m_TankPose.Muzzle, blink);
            }
            if (m_Tones2 != null) Go.transform.localPosition += (Vector3)Tones2Copy.ShakeOffset(m_Pose);
            m_Visual.Skin = Art.SkinFor(Link.Soldier, Link.Skin);
            m_Visual.Apply(m_Pose);
        }
    }
}
