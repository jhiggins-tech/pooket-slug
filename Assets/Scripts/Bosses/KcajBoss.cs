using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's tabk, riding on the artillery tabk (<see cref="TankBoss"/>), for Mission two (<c>?test=kcajtank</c> puts
    /// it in Mission one's arena until Mission two is built). Its charged shots go shell, sweep, shell, rain
    /// (<see cref="KcajTankSpec.AttackFor"/>, tuned in <see cref="Content.KcajTank"/>):
    /// <list type="bullet">
    /// <item>Sweep: Hyperfixate turned on the floor. A beam flickers onto the ground in front of it (the tell), then the
    /// turret drags it out across the arena. It takes a heart from a soldier it crosses: jump it.</item>
    /// <item>Rain: Unmedicated from the turret. A handful of big pills fall across the arena, bounce once and pop, a
    /// heart a direct hit.</item>
    /// </list>
    /// It all quickens as the tabk is hurt, with its rage.
    /// <para>
    /// Co-op: the authority runs the attacks; the sweep (its phase, direction and progress) and the rains so far ride
    /// in the tabk's network object, where kie's roulette does (<see cref="NetBossState.KieState"/>). Every screen
    /// draws the beam and the pills and hurts its own soldier with them, and a rain's pills fall the same way on
    /// every screen (their seed comes from the count of rains).
    /// </para>
    /// </summary>
    public sealed class KcajBoss : IBossKit
    {
        static KcajTankSpec Spec => Content.KcajTank;
        const int Tell = 1, Sweeping = 2, PhaseBits = 3, Right = 8;
        static readonly IReadOnlyList<TankBoss> NoCopies = new List<TankBoss>();

        readonly World m_World;
        readonly TankBoss m_Tank;
        readonly HostilePills m_Pills;
        readonly SpriteRenderer m_Beam;
        int m_Turn, m_Phase, m_Dir = -1, m_Rains, m_SeenRains;
        float m_T; // seconds into the tell, or 0 to 1 through the sweep
        bool m_HitThisSweep;

        public KcajBoss(World world, TankBoss tank)
        {
            m_World = world;
            m_Tank = tank;
            m_Pills = new HostilePills(world, Spec.Rain);
            m_Beam = Art.Renderer(world.Root, "Sweep beam", Art.Pixel, SortOrder.Effects + 2, shown: false);
        }

        public string Name => "KCAJ'S TABK";
        public IReadOnlyList<TankBoss> Copies => NoCopies;
        public bool Disguised => false;
        public void SetDisguised(bool on) { }

        bool Raining => m_Pills.Raining;
        KcajAttack Next => KcajTankSpec.AttackFor(m_Turn, Raining);
        public bool NextLands => Next == KcajAttack.Shell;
        public float Wait { get; private set; }

        public bool Fire(Vector2 muzzle, float angle, out bool thrown)
        {
            KcajAttack attack = Next;
            m_Turn++;
            Wait = 0f;
            thrown = attack == KcajAttack.Rain;
            float rage = m_Tank.Fury;
            switch (attack)
            {
                case KcajAttack.Sweep:
                    Actor target = m_World.Combat.NearestAvatar(m_Tank.Pos);
                    m_Dir = target.Pos.x >= m_Tank.Pos.x ? 1 : -1;
                    StartTell();
                    Wait = (Spec.SweepTell + Spec.SweepTime) / rage + 0.2f;
                    return true;
                case KcajAttack.Rain:
                    m_Rains++;
                    StartRain();
                    Wait = 0.4f;
                    return true;
                default:
                    return false;
            }
        }

        void StartTell()
        {
            m_Phase = Tell;
            m_T = 0f;
            m_HitThisSweep = false;
            Sfx.Play(Sfx.Charge, 0.6f);
            m_World.Scoring.Popup("HYPERFIXATE!", m_Tank.Pos + Vector2.up * 4.4f, KcajArt.Beam);
        }

        void StartRain()
        {
            m_SeenRains = m_Rains;
            m_Pills.Start(m_Rains, m_Tank.Muzzle); // out of the barrel
            m_World.Scoring.Popup("UNMEDICATED!", m_Tank.Pos + Vector2.up * 4.4f, KcajArt.Beam);
        }

        /// <summary>Where the beam touches the floor now.</summary>
        Vector2 Floor
        {
            get
            {
                float x = Spec.BeamFloorX(m_Tank.Pos.x, m_Dir, m_Phase == Sweeping ? m_T : 0f);
                return new Vector2(x, m_World.Level.TopSurfaceY(x));
            }
        }

        public void Tick(float dt)
        {
            float rage = m_Tank.Fury;
            if (m_Phase == Tell)
            {
                m_T += dt * rage;
                if (m_World.IsAuthority && m_T >= Spec.SweepTell)
                {
                    m_Phase = Sweeping;
                    m_T = 0f;
                    Sfx.Play(Sfx.BeamLock, 0.8f);
                }
            }
            else if (m_Phase == Sweeping)
            {
                m_T = Mathf.Min(1f, m_T + Spec.SweepProgress(dt, rage));
                if (m_World.IsAuthority && m_T >= 1f) m_Phase = 0;
            }
            if (m_Phase != 0)
            {
                Vector2 floor = Floor, d = floor - m_Tank.TurretPivot;
                m_Tank.AimTurret(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                if (m_Phase == Sweeping) Scorch(floor);
            }

            m_Pills.Tick(dt);
        }

        /// <summary>The beam across our own soldier (each screen hurts its own), once a sweep; and sparks where it touches.</summary>
        void Scorch(Vector2 floor)
        {
            if (Rng.Fx.Value < 0.6f)
                m_World.SpawnParticle(floor, new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(2f, 5f)), Rng.Fx.Value < 0.5f ? KcajArt.Beam : KcajArt.Hot, 1.5f, 0.3f);
            Player p = m_World.Player;
            if (m_HitThisSweep || p.Dead) return;
            if (!Spec.Hits(m_Tank.Muzzle, floor, p.Pos, p.HalfW, p.Height)) return;
            m_HitThisSweep = true;
            p.Hurt(Spec.SweepDamage);
        }

        public void OnHurt(int damage) { }

        public void OnDie()
        {
            m_Phase = 0;
        }

        public void Render()
        {
            bool on = m_Phase != 0 && !m_Tank.Dying;
            // The tell flickers thin; the sweep burns thick.
            m_Beam.enabled = on && (m_Phase == Sweeping || Fx.Blink(m_T, 24f));
            if (m_Beam.enabled)
            {
                // the sweep is kcaj's own beam at full heat
                if (m_Phase == Sweeping) KcajArt.DrawBeam(m_Beam, m_Tank.Muzzle, Floor, 1f);
                else KcajArt.DrawFaintBeam(m_Beam, m_Tank.Muzzle, Floor, new Color(1f, 0.6f, 0.85f, 0.7f));
            }
            m_Pills.Render();
        }

        // ------------------------------------------------------------------ co-op

        public int NetState(bool markerShown) => m_Phase | (m_Dir > 0 ? Right : 0) | (markerShown ? 0 : KieTankSpec.NoMarker);
        public float Progress => m_Phase == Sweeping ? m_T : 0f;
        public int Drops => m_Rains;
        public bool ShowsMarker(int netState) => (netState & KieTankSpec.NoMarker) == 0;

        public void ApplyNet(int state, float progress, int drops)
        {
            if (m_World.IsAuthority) return;
            int phase = state & PhaseBits;
            m_Dir = (state & Right) != 0 ? 1 : -1;
            if (phase != m_Phase)
            {
                if (phase == Tell) StartTell();
                else if (phase == Sweeping) Sfx.Play(Sfx.BeamLock, 0.8f);
                m_Phase = phase;
                m_T = phase == Sweeping ? progress : 0f;
            }
            else if (phase == Sweeping && progress > m_T) m_T = progress; // catch up with the authority, never back
            if (drops != m_SeenRains && drops > 0)
            {
                m_Rains = drops;
                StartRain();
            }
        }
    }
}
