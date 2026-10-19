using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Trollogram kcaj, Mission two's pilot (<see cref="Pilot"/>), playing kcaj's kit against the soldiers:
    /// <list type="bullet">
    /// <item>FIRE, Double Park: two cones lobbed either side of his aim, a heart a splat.</item>
    /// <item>SP, Hyperfixate: his beam sticks to the first soldier along his aim (crouch under it) and burns while he
    /// stands still, hotter the longer it holds. Distance doesn't save you for long; hurting him does: enough damage
    /// while it's stuck breaks it (<see cref="KcajPilotSpec.BreakOnHurt"/>).</item>
    /// <item>SU, Unmedicated: pills rain on the soldiers (<see cref="HostilePills"/>). It charges from his hits on you.</item>
    /// </list>
    /// Numbers in <see cref="KcajPilotSpec"/>. Co-op: his cones are enemy shots, replayed on every screen; his beam
    /// (stuck or not, on which soldier, how long) and his rains so far ride in the boss's network object's kit fields.
    /// The authority burns whoever the beam is on; every screen draws it, and rains the same pills on its own soldier.
    /// </summary>
    public sealed class KcajPilot : Pilot
    {
        static KcajPilotSpec Spec => KcajTuning.Pilot;
        static SoldierDef Kit => Content.Kcaj;
        /// <summary>His beam's state in <see cref="NetBossState.KieState"/>: stuck, flicking out at nothing, and whether it's on the authority's own soldier.</summary>
        const int Stuck = 1, Fizzle = 2, OnOwn = 4;

        readonly Hyperfixation m_Fix;
        readonly HostilePills m_Pills;
        readonly SpriteRenderer m_Beam;
        readonly List<Actor> m_Seen = new List<Actor>();
        ChargeRefill m_Refill = new ChargeRefill(Spec.Specials, Spec.SpecialRefill);
        Actor m_Target;
        Vector2 m_MissEnd;
        float m_FireCooldown, m_Miss, m_Burnt, m_Held;
        int m_Specials, m_Super, m_HurtSinceLock, m_Rains, m_SeenRains, m_NetBeam;

        public KcajPilot(World world, EnemyDef def, Vector2 feet) : base(world, def, feet, Kit, PilotPlaybook.Kcaj)
        {
            m_Fix = new Hyperfixation(Spec.Beam);
            m_Pills = new HostilePills(world, Spec.Rain);
            m_Beam = Art.Renderer(world.Root, "Trollogram beam", Art.Pixel, SortOrder.Effects + 2, shown: false);
            m_Specials = Spec.Specials;
        }

        public override string DisplayName => "TROLLOGRAM KCAJ";
        protected override Vector2 JumpOutVelocity => Spec.JumpOut;
        protected override float JumpOutShield => Spec.JumpOutShield;
        protected override float RunScale => Spec.RunScale;
        protected override float JumpScale => Spec.JumpScale;
        protected override string LastWords => "IT'S ALL CONNECT-";
        protected override AudioClip FireSound => null; // his cones are enemy shots: each screen sounds them as they're thrown
        protected override float MoveScale => m_Fix.Locked ? Spec.Beam.WalkScale : 1f;

        protected override void OnHurt(int damage)
        {
            if (m_Fix.Locked) m_HurtSinceLock += damage;
        }

        protected override void OnHeartsLost(int hearts) => m_Super = Spec.ChargeSuper(m_Super, hearts);

        protected override void Sees(ref PilotView v)
        {
            v.Specials = m_Specials;
            v.SpecialBusy = m_Fix.Locked;
            v.SuperReady = Spec.SuperReady(m_Super) && !m_Pills.Raining;
        }

        protected override void Act(float dt, in SoldierInput input)
        {
            m_FireCooldown -= dt;
            m_Miss -= dt;
            m_Refill.Tick(ref m_Specials, dt);
            if (m_Fix.Locked) Burn(dt);
            else
            {
                if ((input.FirePressed || input.Fire) && m_FireCooldown <= 0f) ThrowCones();
                if (input.SpecialPressed && m_Specials > 0) Fixate();
            }
            if (input.SuperPressed && Spec.SuperReady(m_Super) && !m_Pills.Raining)
            {
                m_Super = 0;
                m_Rains++;
                StartRain();
            }
            m_Pills.Tick(dt);
        }

        /// <summary>Double Park on the Trollograms' side: two cones, one short and one long, at the soldiers.</summary>
        void ThrowCones()
        {
            for (int i = 0; i < KcajTuning.Cones.Count; i++)
                World.Combat.FireEnemyShot(Content.TrolloCones, MuzzlePos, KcajTuning.Cones.Velocity(i, m_Aim, Facing, Vel));
            m_FireCooldown = Spec.ConeCooldown;
            Fired(null);
        }

        /// <summary>Hyperfixate: the beam sticks to the first soldier along his aim, or flicks out and fizzles (the charge kept).</summary>
        void Fixate()
        {
            Vector2 origin = MuzzlePos, dir = m_Aim.normalized;
            Actor hit = FirstTouched(origin, dir);
            if (hit == null)
            {
                m_MissEnd = origin + dir * Spec.Beam.Range;
                m_Miss = 0.15f;
                Sfx.Play(Sfx.BeamFizzle, 0.5f);
                return;
            }
            m_Specials--;
            m_Target = hit;
            m_Fix.Lock();
            m_Burnt = 0f;
            m_HurtSinceLock = 0;
            Sfx.Play(Sfx.BeamLock, 0.7f);
            World.Scoring.Popup("HYPERFIXATE!", Center + Vector2.up * 1.6f, KcajArt.Beam);
        }

        /// <summary>The soldiers (and a kie's hologram) the beam could stick to, nearest along the ray first.</summary>
        Actor FirstTouched(Vector2 origin, Vector2 dir)
        {
            m_Seen.Clear();
            World w = World;
            if (!w.Player.IsDown) m_Seen.Add(w.Player);
            foreach (RemotePlayer r in w.Remotes)
                if (!r.IsDown && !r.Invulnerable) m_Seen.Add(r);
            foreach (TrollDecoy d in w.Decoys)
                if (d.Up) m_Seen.Add(d);
            Actor best = null;
            float bestD = float.MaxValue;
            foreach (Actor a in m_Seen)
            {
                float d = HyperfixateSpec.RayBox(origin, dir, a.Center, new Vector2(a.HalfW, a.Height * 0.5f));
                if (d >= 0f && d <= Spec.Beam.Range && d < bestD)
                {
                    best = a;
                    bestD = d;
                }
            }
            return best;
        }

        /// <summary>Authority: the stuck beam burns its soldier, and breaks if they get away, go down, or he's hurt enough.</summary>
        void Burn(float dt)
        {
            Actor t = m_Target;
            bool alive = t != null && !t.Removed && !t.IsDown && !(t is TrollDecoy d && !d.Up);
            if (Spec.Breaks(m_HurtSinceLock))
            {
                m_Fix.Step(0f, false, alive, 0f);
                World.Scoring.Popup("SNAPPED OUT OF IT!", Center + Vector2.up * 1.6f, Palette.Gold);
            }
            else
            {
                float burn = m_Fix.Step(dt, true, alive, alive ? Vector2.Distance(Center, t.Center) : 0f);
                m_Held = m_Fix.Held;
                int hearts = KcajPilotSpec.Hearts(ref m_Burnt, burn);
                if (burn > 0f && alive)
                {
                    Sparks(t.Center);
                    if (hearts > 0) World.Combat.DamageAvatar(t, hearts);
                }
                if (alive && m_Fix.Locked) Facing = t.Pos.x >= Pos.x ? 1 : -1;
            }
            if (!m_Fix.Locked)
            {
                m_Target = null;
                m_Held = 0f;
                Sfx.Play(Sfx.BeamSnap, 0.6f);
            }
        }

        void Sparks(Vector2 at)
        {
            World.HitFeel.Fizz(at);
            for (int i = 0; i < 3; i++)
                World.SpawnParticle(at, new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(1f, 5f)), i % 2 == 0 ? KcajArt.Beam : KcajArt.Hot, 1.5f, 0.3f);
            float heat = Mathf.Clamp01(m_Held / Spec.Beam.MaxHold);
            Sfx.Play(Sfx.BeamBurnAt(heat), 0.4f + 0.3f * heat);
        }

        void StartRain()
        {
            m_SeenRains = m_Rains;
            m_Pills.Start(m_Rains, Center + Vector2.up * 0.6f);
            World.Scoring.Popup("UNMEDICATED!", Center + Vector2.up * 1.6f, KcajArt.Beam);
        }

        protected override void OnBeaten()
        {
            if (m_Fix.Locked) m_Fix.Step(0f, false, false, 0f);
            m_Target = null;
            m_NetBeam = 0;
        }

        // ------------------------------------------------------------------ co-op

        protected override void KitState(ref NetBossState s)
        {
            int beam = m_Fix.Locked && m_Target != null ? Stuck | (m_Target == World.Player ? OnOwn : 0) : m_Miss > 0f ? Fizzle : 0;
            s.KieState = beam;
            s.KieProgress = m_Fix.Locked ? m_Fix.Held : 0f;
            s.KieDrops = m_Rains & 0xff;
        }

        protected override void ApplyKit(in NetBossState s)
        {
            if (World.IsAuthority) return;
            int was = m_NetBeam;
            m_NetBeam = Dying ? 0 : s.KieState;
            m_Held = s.KieProgress;
            if ((m_NetBeam & Stuck) != 0 && (was & Stuck) == 0) Sfx.Play(Sfx.BeamLock, 0.7f);
            else if ((m_NetBeam & Stuck) == 0 && (was & Stuck) != 0) Sfx.Play(Sfx.BeamSnap, 0.6f);
            if ((m_NetBeam & Fizzle) != 0 && (was & Fizzle) == 0) Sfx.Play(Sfx.BeamFizzle, 0.5f);
            if (s.KieDrops != m_SeenRains && s.KieDrops > 0)
            {
                m_Rains = s.KieDrops;
                StartRain();
            }
        }

        protected override void TickKitFollower(float dt)
        {
            m_Pills.Tick(dt);
            Actor t = BeamTarget;
            if (t != null && Rng.Fx.Value < 0.3f) World.HitFeel.Fizz(t.Center);
        }

        /// <summary>Who the beam is on, as this screen sees it: on a follower, the authority's soldier or our own.</summary>
        Actor BeamTarget
        {
            get
            {
                if (World.IsAuthority) return m_Fix.Locked ? m_Target : null;
                if ((m_NetBeam & Stuck) == 0) return null;
                if ((m_NetBeam & OnOwn) == 0) return World.Player;
                foreach (RemotePlayer r in World.Remotes) return r; // the authority's soldier: co-op is two players
                return null;
            }
        }

        protected override void RenderKit()
        {
            Actor t = Dying ? null : BeamTarget;
            AvatarPose pose = ShownPose;
            Vector2 local = PlayerVisual.MuzzleLocal(pose.AimY, pose.Crouching, pose.Diagonal);
            Vector2 muzzle = Pos + new Vector2(local.x * Facing, local.y);
            bool miss = t == null && !Dying && (World.IsAuthority ? m_Miss > 0f : (m_NetBeam & Fizzle) != 0);
            m_Beam.enabled = t != null || miss;
            if (t != null) KcajArt.DrawBeam(m_Beam, muzzle, t.Center, Mathf.Clamp01(m_Held / Spec.Beam.MaxHold));
            else if (miss)
            {
                Vector2 end = World.IsAuthority ? m_MissEnd : muzzle + new Vector2(Facing * Spec.Beam.Range, 0f);
                KcajArt.DrawFaintBeam(m_Beam, muzzle, end, new Color(1f, 0.6f, 0.85f, 0.7f));
            }
            m_Pills.Render();
        }
    }
}
