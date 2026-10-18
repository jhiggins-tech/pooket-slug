using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// tones2's tabk's guns (<c>docs/design/tanks.md</c>, tones2's tabk), numbers in <see cref="Tones2TabkSpec"/>. FIRE
    /// is ten-1, the water cannon: a round is one spray cycle, its pressure building in spurts, holding, then easing
    /// off. SP is ten-2, the mud jump: it shakes for a second, then blasts off along the aim like a jetpack (no fuel),
    /// carpeting the launch spot in toxic mud and squashing what it lands on. SU is ten-3, three seconds of spew while
    /// you drive. Their water, mud and spew do their harm on the driver's screen.
    /// </summary>
    public sealed class Tones2TabkGuns : TabkGuns
    {
        static Tones2TabkSpec Guns => Tones2Tuning.Tabk;

        // seconds into a spray cycle and building up a mud jump (-1 when not), the jump in the air, spew left
        float m_Cycle = -1f, m_Build = -1f, m_Puff, m_SpewLeft;
        Drip m_Jet, m_Spew;
        bool m_Flying;
        AudioSource m_Hiss;

        public override bool Rooted => m_Build >= 0f;
        public override bool Flying => m_Flying;
        /// <summary>Shaking while a mud jump builds, harder as it goes.</summary>
        public override float Shake => m_Build >= 0f ? 1f + 2f * m_Build / Guns.BuildTime : 0f;

        public override void Tick(PlayerTank tank, float dt, in SoldierInput input)
        {
            if (input.FirePressed && m_Cycle < 0f)
            {
                if (tank.Rounds.UseMain()) StartCycle();
                else tank.Click();
            }
            if (input.SpecialPressed && m_Build < 0f && !m_Flying && tank.Grounded)
            {
                if (tank.Rounds.UseSpecial()) StartBuild(tank);
                else tank.Click();
            }
            if (input.SuperPressed && m_SpewLeft <= 0f)
            {
                if (tank.Rounds.UseSuper()) StartSpew(tank);
                else tank.Click();
            }
            Spray(tank, dt);
            BuildUp(tank, dt);
            Pour(tank, dt);
        }

        /// <summary>ten-1: one round, one spray cycle.</summary>
        void StartCycle()
        {
            m_Cycle = 0f;
            m_Jet.Reset();
            Sfx.Play(Sfx.Servo, 0.4f);
        }

        void Spray(PlayerTank tank, float dt)
        {
            if (m_Cycle < 0f) return;
            m_Cycle += dt;
            if (m_Cycle >= Guns.CycleLength)
            {
                m_Cycle = -1f;
                if (m_Hiss != null) m_Hiss.Stop();
                return;
            }
            float p = Guns.CyclePressure(m_Cycle);
            int drops = m_Jet.Take(dt, Guns.Water.DropsPerSecond(p));
            Tones2Sprays.Jet(tank.World, Guns.Water, tank.MuzzleAlong(tank.Aim), tank.Aim, p, tank.Vel.x, drops, false);
            if (p >= 0.999f) tank.FlashMuzzle();
            if (m_Hiss == null) m_Hiss = Sfx.LoopSource(tank.Go, Sfx.JetLoop);
            if (m_Hiss == null) return;
            if (!m_Hiss.isPlaying) m_Hiss.Play();
            m_Hiss.volume = Sfx.Muted ? 0f : (0.15f + 0.25f * p) * Sfx.EffectsVolume;
            m_Hiss.pitch = 0.55f + 0.6f * p; // deeper than his jet: it's a cannon
        }

        /// <summary>ten-2: it roots and shakes while the mud builds up (<see cref="Tones2TabkSpec.BuildTime"/>).</summary>
        void StartBuild(PlayerTank tank)
        {
            m_Build = 0f;
            m_Puff = 0f;
            Sfx.Play(Sfx.RocketRumble, 0.9f);
            tank.World.Scoring.Popup("MUD JUMP!", tank.Pos + Vector2.up * 2.6f, Tones2Art.Mud);
        }

        void BuildUp(PlayerTank tank, float dt)
        {
            if (m_Build < 0f) return;
            m_Build += dt;
            m_Puff -= dt;
            if (m_Puff <= 0f)
            {
                m_Puff = 0.04f;
                tank.World.SpawnParticle(tank.Pos + new Vector2(Rng.Fx.Range(-tank.HalfW, tank.HalfW), 0.1f), new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(1f, 4f)),
                    Rng.Fx.Range(0f, 1f) < 0.25f ? Tones2Art.Toxic : Tones2Art.Mud, 2.5f, 0.45f);
            }
            if (m_Build >= Guns.BuildTime) Launch(tank);
        }

        /// <summary>Off it goes along the aim, leaving the launch spot carpeted in mud.</summary>
        void Launch(PlayerTank tank)
        {
            World world = tank.World;
            m_Build = -1f;
            m_Flying = true;
            Vector2 dir = Tones2TabkSpec.JumpDirection(tank.Aim, tank.Facing);
            tank.Vel = dir * Guns.JumpSpeed;
            if (dir.x != 0f) tank.Facing = dir.x > 0f ? 1 : -1;
            world.Liquids.SpillMud(tank.Pos, Guns.Mud);
            Tones2Sprays.Exhaust(world, tank.Pos);
            Sfx.Play(Sfx.RocketLaunch);
            world.View.Shake(0.25f);
            CrewKit.Send(world, KitMove.Mud, tank.Pos, Vector2.zero); // partners lay mud too, harmless, on their screens
        }

        public override void Land(PlayerTank tank)
        {
            m_Flying = false;
            tank.Vel.x = 0f;
            tank.World.View.Shake(0.3f);
            Sfx.Play(Sfx.Thump);
            tank.World.Burst(tank.Pos, 12, Tones2Art.Mud);
        }

        /// <summary>ten-3: three seconds of spew along the aim, while it drives.</summary>
        void StartSpew(PlayerTank tank)
        {
            m_SpewLeft = Guns.Spew.Duration;
            m_Spew.Reset();
            Sfx.Play(Sfx.Spew);
            tank.World.Scoring.Popup("SPEW!", tank.Pos + Vector2.up * 2.6f, Tones2Art.Spew);
        }

        void Pour(PlayerTank tank, float dt)
        {
            if (m_SpewLeft <= 0f) return;
            m_SpewLeft -= dt;
            int drops = m_Spew.Take(dt, Guns.Spew.DropsPerSecond);
            Tones2Sprays.Spew(tank.World, Guns.Spew, tank.MuzzleAlong(tank.Aim), tank.Aim, tank.Vel.x, drops, false);
            tank.World.View.Shake(0.05f);
            tank.FlashMuzzle();
        }

        public override void Stop()
        {
            m_Cycle = m_Build = -1f;
            m_SpewLeft = 0f;
            m_Flying = false;
            if (m_Hiss != null && m_Hiss.isPlaying) m_Hiss.Stop();
        }
    }
}
