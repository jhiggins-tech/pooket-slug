using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's kit (<c>docs/design/kits.md</c>, numbers in <see cref="KcajTuning"/>): FIRE is Double Park (two cones lobbed
    /// either side of the aim, <see cref="DoubleParkSpec"/>), SP is Hyperfixate (<see cref="KcajBeam"/>) and SU is
    /// Unmedicated (<see cref="KcajPills"/>). The cones are shots, so they reach a partner as shots; the beam and the
    /// pills go as kit moves (<see cref="KitMove.Fixate"/>, <see cref="KitMove.Pills"/>) and <see cref="KcajCopy"/>
    /// plays them there.
    /// </summary>
    public sealed class KcajKit : CrewKit
    {
        readonly KcajBeam m_Beam;
        readonly KcajPills m_Pills;

        public KcajKit(Player player) : base(player)
        {
            m_Beam = new KcajBeam(new Soldier(player), KcajTuning.Beam);
            m_Pills = new KcajPills(player.World, KcajTuning.Pills);
        }

        public override bool Fixated => m_Beam.Locked;
        public override float SpeedScale => m_Beam.Locked ? KcajTuning.Beam.WalkScale : 1f;

        public override void Always(float dt) => m_Pills.Tick(dt);

        public override void LetGo() => m_Beam.Cancel();

        public override bool OwnPrimary => true;

        /// <summary>Double Park: both cones at once, from the hand, along the aim.</summary>
        public override bool FirePrimary(Vector2 muzzle)
        {
            DoubleParkSpec cones = KcajTuning.Cones;
            WeaponDef w = Content.DoublePark;
            for (int i = 0; i < cones.Count; i++)
                World.Combat.FirePlayerShot(w, muzzle, cones.Velocity(i, Player.Aim, Player.Facing, Player.Vel));
            Sfx.PlayShot(w);
            return true;
        }

        public override void Special(float dt, bool pressed, bool held) => m_Beam.Tick(dt, pressed, held);

        public override void Super()
        {
            if (m_Pills.Raining || !Player.Kit.UseSuper()) return;
            World.Scoring.Popup("UNMEDICATED!", Player.Pos + Vector2.up * 2.2f, KcajArt.Beam);
            m_Pills.Start();
        }

        public override void Render()
        {
            m_Pills.Render();
            m_Beam.Render();
        }

        /// <summary>His beam on foot: from his gun, along his aim, from his specials, and he turns to what it's stuck to.</summary>
        sealed class Soldier : IFixateHost
        {
            readonly Player m_Player;
            public Soldier(Player player) => m_Player = player;
            public World World => m_Player.World;
            public Vector2 BeamFrom => m_Player.MuzzlePos;
            public Vector2 BeamAim => m_Player.Aim;
            public Vector2 Center => m_Player.Center;
            public bool Down => m_Player.Dead;
            public bool HasCharge => m_Player.Specials > 0;
            public void SpendCharge() => m_Player.Kit.UseSpecial();
            public void Face(Vector2 at) => m_Player.Facing = at.x >= m_Player.Pos.x ? 1 : -1;
        }
    }

    /// <summary>Whoever shines Hyperfixate (<see cref="KcajBeam"/>): kcaj on foot, or his tabk's nozzle (<see cref="KcajTabkGuns"/>).</summary>
    public interface IFixateHost
    {
        World World { get; }
        /// <summary>Where the beam leaves from, and which way.</summary>
        Vector2 BeamFrom { get; }
        Vector2 BeamAim { get; }
        /// <summary>Where it's measured from, for how far the target's got.</summary>
        Vector2 Center { get; }
        /// <summary>Down, so the beam isn't drawn.</summary>
        bool Down { get; }
        /// <summary>A charge (a special, a tabk's round) is left to lock with, and spending it.</summary>
        bool HasCharge { get; }
        void SpendCharge();
        /// <summary>Held on something at <paramref name="at"/>: turn to it, if it turns.</summary>
        void Face(Vector2 at);
    }
}
