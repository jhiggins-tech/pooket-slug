using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's tabk's guns, SOFT SERVE's (<c>docs/design/tanks.md</c>, kcaj's tabk), numbers in <see cref="KcajTabkSpec"/>,
    /// all from the soft-serve machine's nozzle. FIRE is Double Park: the swirl wobbles, then the nozzle pipes out three
    /// pairs of his cones. SP is Hyperfixate: his beam from the nozzle (<see cref="KcajBeam"/>), and the tabk stands still
    /// while it holds. SU is Unmedicated: his pills rain across the screen (<see cref="PillShower"/>). The cones go to a
    /// partner as shots, the beam and the pills as his kit moves do, played there by the partner's copy of the tabk
    /// (<see cref="KcajCopy"/>).
    /// </summary>
    public sealed class KcajTabkGuns : TabkGuns
    {
        static KcajTabkSpec Guns => KcajTuning.Tabk;

        KcajBeam m_Beam;
        Nozzle m_Nozzle;
        // seconds into a volley (-1 when none)
        float m_Volley = -1f;

        public override bool Rooted => m_Beam != null && m_Beam.Locked;
        public override bool Spinning => KcajTabkSpec.SwirlWobbles(m_Volley >= 0f, m_Beam != null && m_Beam.Locked);

        public override void Tick(PlayerTank tank, float dt, in SoldierInput input)
        {
            World w = tank.World;
            if (m_Beam == null)
            {
                m_Nozzle = new Nozzle(tank);
                m_Beam = new KcajBeam(m_Nozzle, Guns.Beam);
            }
            if (tank.Fire(TabkGun.Main, input.FirePressed && m_Volley < 0f && !m_Beam.Locked)) m_Volley = 0f;
            Piping(tank, dt);
            // a beam that touches nothing fizzles and keeps its round (KcajBeam spends it only on a lock)
            if (input.SpecialPressed && m_Volley < 0f && !m_Beam.Locked && tank.Rounds.Specials <= 0) tank.Click();
            m_Beam.Tick(dt, input.SpecialPressed && m_Volley < 0f, input.Special);
            if (tank.Fire(TabkGun.Super, input.SuperPressed))
            {
                w.Props.Add(new PillShower(w, Guns.Pills));
                w.Scoring.Popup("UNMEDICATED!", tank.Pos + Vector2.up * 2.6f, KcajArt.Beam);
                tank.FlashMuzzle();
            }
        }

        /// <summary>Double Park: the swirl wobbles, then the pairs go one after another along the aim.</summary>
        void Piping(PlayerTank tank, float dt)
        {
            if (m_Volley < 0f) return;
            float before = m_Volley;
            m_Volley += dt;
            int due = Guns.PairsDue(before, m_Volley);
            WeaponDef cone = Content.DoublePark;
            for (int pair = 0; pair < due; pair++)
            {
                Vector2 muzzle = tank.MuzzleAlong(tank.Aim);
                for (int i = 0; i < Guns.Cones.Count; i++)
                    tank.World.Combat.FirePlayerShot(cone, muzzle, Guns.Cones.Velocity(i, tank.Aim, tank.Facing, tank.Vel));
                Sfx.PlayShot(cone);
                tank.FlashMuzzle();
            }
            if (m_Volley >= Guns.VolleyTime) m_Volley = -1f;
        }

        public override void Render(PlayerTank tank) => m_Beam?.Render();

        public override void Stop()
        {
            m_Volley = -1f;
            m_Beam?.Cancel();
        }

        public override void Destroy() => m_Beam?.Destroy();

        /// <summary>The beam's host: the nozzle, along the tabk's aim, on its rounds; the tabk doesn't turn to its target.</summary>
        sealed class Nozzle : IFixateHost
        {
            readonly PlayerTank m_Tank;
            public Nozzle(PlayerTank tank) => m_Tank = tank;
            public World World => m_Tank.World;
            public Vector2 BeamFrom => m_Tank.MuzzleAlong(m_Tank.Aim);
            public Vector2 BeamAim => m_Tank.Aim;
            public Vector2 Center => m_Tank.Center;
            public bool Down => !m_Tank.Occupied || m_Tank.Removed;
            public bool HasCharge => m_Tank.Rounds.Specials > 0;
            public void SpendCharge() => m_Tank.Rounds.UseSpecial();
            public void Face(Vector2 at) { }
        }
    }

    /// <summary>
    /// Unmedicated out of kcaj's tabk: one rain of his pills across the screen (<see cref="KcajPills"/>), the world's
    /// own while it falls, so it rains on whether the tabk's driven, ejected or gone. It's gone once the last pill's popped.
    /// </summary>
    public sealed class PillShower : Entity
    {
        readonly KcajPills m_Pills;

        public PillShower(World world, UnmedicatedSpec spec) : base(world, "Unmedicated", Vector2.zero)
        {
            m_Pills = new KcajPills(world, spec, Go.transform);
            m_Pills.Start();
        }

        public override void Tick(float dt)
        {
            m_Pills.Tick(dt);
            if (!m_Pills.Raining) Remove();
        }

        public override void Render() => m_Pills.Render();
    }
}
