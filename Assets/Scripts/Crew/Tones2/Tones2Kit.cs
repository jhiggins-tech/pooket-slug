using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// tones2's kit for a player (<c>docs/design/kits.md</c>, numbers in <see cref="Tones2Tuning"/>): the water jet
    /// (<see cref="WaterJet"/>), the rocket jump (<see cref="RocketJump"/>) and the spew (<see cref="Spew"/>). A partner
    /// copies the jet and the spew from his pose (<see cref="Tones2Copy"/>) and the rocket jump's mud from a kit move.
    /// </summary>
    public sealed class Tones2Kit : CrewKit
    {
        readonly WaterJet m_Jet;
        readonly RocketJump m_Rocket;
        readonly Spew m_Spew;

        public Tones2Kit(Player player) : base(player)
        {
            m_Jet = new WaterJet(player, Tones2Tuning.Jet);
            m_Rocket = new RocketJump(player, Tones2Tuning.Rocket);
            m_Spew = new Spew(player, Tones2Tuning.Spew);
        }

        public override bool Rooted => m_Rocket.Rooted;
        public override bool Rising => m_Rocket.Rising;
        public override bool Busy => m_Spew.Active;
        public override bool Streams => true;

        public override void HoldPrimary(float dt, bool firing) => m_Jet.Tick(dt, firing);
        public override void LetGo() => m_Jet.Tick(0f, false);
        public override void Tick(float dt) => m_Spew.Tick(dt);
        public override void Special(float dt, bool pressed, bool held) => m_Rocket.Tick(dt, pressed && !m_Spew.Active);

        public override void Super()
        {
            if (!Player.Kit.UseSuper()) return;
            Player.SuperFanfare();
            m_Spew.Start();
        }

        public override void Render() => m_Jet.Render();
        public override Vector2 ShakeOffset => m_Rocket.ShakeOffset;

        public override void Pose(ref AvatarPose pose)
        {
            pose.Jetting = m_Jet.Pressure > 0f;
            pose.Pressure = m_Jet.Pressure;
            pose.Spewing = m_Spew.Active;
            pose.Rooted = m_Rocket.Rooted;
        }

        /// <summary>A partner's rocket jump (or their tabk's mud gun): the same mud here, harmless (it poisons goons on theirs).</summary>
        public static void CopyMud(World w, Vector2 at)
        {
            w.Liquids.SpillMud(at, Tones2Tuning.Rocket, cosmetic: true);
            Tones2Sprays.Exhaust(w, at);
            Sfx.Play(Sfx.RocketLaunch, PartnerVolume);
        }
    }

    /// <summary>
    /// tones2's ten-1, the water jet. Hold FIRE and the pressure builds from a wide spray that falls short to
    /// a clean, long arc in about a second (<see cref="JetSpec"/>); let go and it resets. A gauge over his head
    /// shows the pressure, and the hiss rises with it. The droplets are <see cref="Liquids"/>.
    /// </summary>
    public sealed class WaterJet
    {
        readonly Player m_Player;
        readonly JetSpec m_Spec;
        readonly SpriteRenderer m_GaugeBack, m_GaugeFill;
        AudioSource m_Hiss;
        float m_Held;
        Drip m_Drip;

        public WaterJet(Player player, JetSpec spec)
        {
            m_Player = player;
            m_Spec = spec;
            m_GaugeBack = Gauge("Gauge", SortOrder.Effects, new Color(0.11f, 0.08f, 0.12f, 0.85f));
            m_GaugeFill = Gauge("Pressure", SortOrder.Effects + 1, Tones2Art.Water);
        }

        /// <summary>0 to 1: how built up the jet is.</summary>
        public float Pressure { get; private set; }

        SpriteRenderer Gauge(string name, int order, Color color)
        {
            // on the soldier, not the visual: the gauge doesn't flip with facing
            SpriteRenderer sr = Art.Renderer(m_Player.Go.transform, name, Art.Pixel, order, shown: false);
            sr.color = color;
            return sr;
        }

        public void Tick(float dt, bool firing)
        {
            if (!firing)
            {
                m_Held = 0f;
                m_Drip.Reset();
                Pressure = 0f;
                if (m_Hiss != null && m_Hiss.isPlaying) m_Hiss.Stop();
                return;
            }

            m_Held += dt;
            Pressure = m_Spec.Pressure(m_Held);
            float p = Pressure;
            int drops = m_Drip.Take(dt, m_Spec.DropsPerSecond(p));
            Tones2Sprays.Jet(m_Player.World, m_Spec, m_Player.MuzzlePos, m_Player.Aim, p, m_Player.Vel.x, drops, false);

            if (m_Hiss == null) m_Hiss = Sfx.LoopSource(m_Player.Go, Sfx.JetLoop);
            if (m_Hiss != null)
            {
                if (!m_Hiss.isPlaying) m_Hiss.Play();
                m_Hiss.volume = Sfx.Muted ? 0f : (0.12f + 0.22f * p) * Sfx.EffectsVolume;
                m_Hiss.pitch = 0.7f + 0.7f * p;
            }
        }

        /// <summary>The pressure gauge over his head, while he's firing.</summary>
        public void Render()
        {
            bool show = Pressure > 0f && !m_Player.Dead;
            m_GaugeBack.enabled = m_GaugeFill.enabled = show;
            if (!show) return;
            const float width = 18f, y = 1.7f; // px wide, units above his feet
            float fill = Mathf.Round(Pressure * (width - 2f));
            m_GaugeBack.transform.localPosition = new Vector3(0f, y, 0f);
            m_GaugeBack.transform.localScale = new Vector3(width, 4f, 1f);
            m_GaugeFill.transform.localPosition = new Vector3((-(width - 2f) * 0.5f + fill * 0.5f) / Art.PPU, y, 0f);
            m_GaugeFill.transform.localScale = new Vector3(fill, 2f, 1f);
            // A flicker of white once the jet is at full pressure.
            m_GaugeFill.color = Pressure >= 0.999f && Fx.Blink(Time.unscaledTime, 12f) ? Color.white : Tones2Art.Water;
        }
    }

    /// <summary>
    /// tones2's ten-2, the rocket jump. Press SP on the ground: he shakes on the spot for half a second, then
    /// blasts straight up, about twice as high as a jump, and steers in the air like a normal jump. The launch
    /// carpets the floor in toxic mud that poisons goons who walk through it (<see cref="RocketSpec"/>). Its charges
    /// and their refill are on his soldier card, as everyone's are.
    /// </summary>
    public sealed class RocketJump
    {
        readonly Player m_Player;
        readonly RocketSpec m_Spec;
        float m_Shake = -1f, m_Puff;

        public RocketJump(Player player, RocketSpec spec)
        {
            m_Player = player;
            m_Spec = spec;
        }

        /// <summary>Shaking before the launch: he can't move, jump or fire.</summary>
        public bool Rooted => m_Shake >= 0f;
        /// <summary>Climbing from a launch, so letting go of JUMP doesn't cut it short.</summary>
        public bool Rising { get; private set; }

        /// <summary>How far the shaking moves him this frame.</summary>
        public Vector2 ShakeOffset => Rooted
            ? new Vector2(Mathf.Round(Rng.Fx.Range(-1f, 1f) * (1f + 2f * m_Shake / m_Spec.ShakeTime)) / Art.PPU, 0f)
            : Vector2.zero;

        public void Tick(float dt, bool pressed)
        {
            Player p = m_Player;
            World w = p.World;
            if (Rising && p.Vel.y <= 0f) Rising = false;

            if (Rooted)
            {
                m_Shake += dt;
                m_Puff -= dt;
                if (m_Puff <= 0f)
                {
                    m_Puff = 0.05f;
                    w.SpawnParticle(p.Pos + new Vector2(Rng.Fx.Range(-0.3f, 0.3f), 0.1f), new Vector2(Rng.Fx.Range(-2f, 2f), Rng.Fx.Range(1f, 3f)),
                        Tones2Art.Mud, 2f, 0.4f);
                }
                if (m_Shake >= m_Spec.ShakeTime) Launch();
                return;
            }

            if (pressed && p.Grounded && p.Kit.UseSpecial())
            {
                m_Shake = 0f;
                m_Puff = 0f;
                Sfx.Play(Sfx.RocketRumble, 0.8f);
            }
        }

        void Launch()
        {
            Player p = m_Player;
            World w = p.World;
            m_Shake = -1f;
            p.Vel.y = m_Spec.LaunchVelocity;
            Rising = true;
            w.Liquids.SpillMud(p.Pos, m_Spec);
            Tones2Sprays.Exhaust(w, p.Pos);
            Sfx.Play(Sfx.RocketLaunch, 0.9f);
            w.View.Shake(0.12f);
            CrewKit.Send(w, KitMove.Mud, p.Pos, Vector2.zero); // partners lay the same mud, harmless, on their screens
        }
    }

    /// <summary>
    /// tones2's ten-3, the super. A second and a half of spew along his aim that melts anything close and
    /// leaves sludge on the floor that burns goons who walk through it (<see cref="SpewSpec"/>). He can walk and
    /// turn while it pours out, and nothing can hurt him until it's done.
    /// </summary>
    public sealed class Spew
    {
        readonly Player m_Player;
        readonly SpewSpec m_Spec;
        float m_Left;
        Drip m_Drip;

        public Spew(Player player, SpewSpec spec)
        {
            m_Player = player;
            m_Spec = spec;
        }

        /// <summary>Pouring out: he can't fire anything else, and can't be hurt.</summary>
        public bool Active => m_Left > 0f;

        public void Start()
        {
            m_Left = m_Spec.Duration;
            m_Drip.Reset();
            Sfx.Play(Sfx.Spew, 0.8f);
        }

        public void Tick(float dt)
        {
            if (!Active) return;
            m_Left -= dt;
            int drops = m_Drip.Take(dt, m_Spec.DropsPerSecond);
            Tones2Sprays.Spew(m_Player.World, m_Spec, m_Player.MuzzlePos, m_Player.Aim, m_Player.Vel.x, drops, false);
            m_Player.World.View.Shake(0.05f);
        }
    }

    /// <summary>The liquids themselves, poured out by tones2 here and by a partner's tones2 as harmless copies.</summary>
    public static class Tones2Sprays
    {
        /// <summary><paramref name="count"/> droplets of the jet at <paramref name="pressure"/>; walking at <paramref name="carryX"/> carries them along.</summary>
        public static void Jet(World w, JetSpec spec, Vector2 origin, Vector2 aim, float pressure, float carryX, int count, bool cosmetic)
        {
            float spread = spec.Spread(pressure), speed = spec.Speed(pressure), gravity = spec.Gravity(pressure);
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Turn(aim, Rng.Fx.Range(-spread, spread));
                Vector2 vel = dir * (speed * Rng.Fx.Range(0.9f, 1.05f)) + new Vector2(carryX * 0.4f, 0f);
                Vector2 at = origin + dir * Rng.Fx.Range(0f, 0.25f);
                w.Liquids.Spray(at, vel, gravity, spec.Damage(pressure), spec.Push(pressure), DropKind.Jet, cosmetic);
            }
        }

        /// <summary><paramref name="count"/> gobbets of spew.</summary>
        public static void Spew(World w, SpewSpec spec, Vector2 origin, Vector2 aim, float carryX, int count, bool cosmetic)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Turn(aim, Rng.Fx.Range(-spec.Spread, spec.Spread));
                Vector2 vel = dir * (spec.Speed * Rng.Fx.Range(0.75f, 1.1f)) + new Vector2(carryX * 0.4f, 0f); // walking carries it along
                w.Liquids.Spray(origin, vel, spec.Gravity, spec.Damage, spec.Push, DropKind.Spew, cosmetic);
            }
        }

        /// <summary>A rocket jump's exhaust: a blast of mud, flecked with poison, thrown out along the floor.</summary>
        public static void Exhaust(World w, Vector2 feet)
        {
            for (int i = 0; i < 18; i++)
            {
                var vel = new Vector2(Rng.Fx.Range(-7f, 7f), Rng.Fx.Range(0.5f, 4f));
                w.SpawnParticle(feet + Vector2.up * 0.1f, vel, i % 4 == 0 ? Tones2Art.Toxic : Tones2Art.Mud, Rng.Fx.Range(1.5f, 3f), Rng.Fx.Range(0.4f, 0.8f));
            }
        }

        static Vector2 Turn(Vector2 v, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }

    /// <summary>
    /// A co-op partner's tones2, on our screen: it pours out harmless copies of their jet and spew from what their
    /// pose says (whether they're firing, how hard, and where they aim). Their rocket jump's mud arrives as a shot
    /// kit move (<see cref="Tones2Kit.CopyMud"/>). The damage is all done on their own screen, as with their shots.
    /// </summary>
    public sealed class Tones2Copy : KitCopy
    {
        Drip m_Jet, m_Spew;
        bool m_WasSpewing;

        public Tones2Copy(RemotePlayer remote) : base(remote) { }

        public override void Tick(float dt, in AvatarPose pose, Vector2 vel)
        {
            bool spewing = pose.Spewing && !pose.Dead, jetting = pose.Jetting && !pose.Dead && !spewing;
            if (spewing && !m_WasSpewing) Sfx.Play(Sfx.Spew, CrewKit.PartnerVolume);
            m_WasSpewing = spewing;
            if (!jetting) m_Jet.Reset();
            if (!spewing) m_Spew.Reset();
            if (!jetting && !spewing) return;

            World w = Remote.World;
            Vector2 aim = Aiming.FromPose(pose.Facing, pose.AimY, pose.Diagonal);
            Vector2 local = PlayerVisual.MuzzleLocal(pose.AimY, pose.Crouching, pose.Diagonal);
            Vector2 origin = Remote.Pos + new Vector2(local.x * pose.Facing, local.y);
            if (jetting)
            {
                JetSpec spec = Tones2Tuning.Jet;
                int drops = m_Jet.Take(dt, spec.DropsPerSecond(pose.Pressure));
                Tones2Sprays.Jet(w, spec, origin, aim, pose.Pressure, vel.x, drops, true);
            }
            else
            {
                SpewSpec spec = Tones2Tuning.Spew;
                int drops = m_Spew.Take(dt, spec.DropsPerSecond);
                Tones2Sprays.Spew(w, spec, origin, aim, vel.x, drops, true);
            }
        }

        /// <summary>How far shaking before a rocket jump moves them this frame.</summary>
        public override Vector2 ShakeOffset(in AvatarPose pose) =>
            pose.Rooted && !pose.Dead ? new Vector2(Mathf.Round(Rng.Fx.Range(-2f, 2f)) / Art.PPU, 0f) : Vector2.zero;
    }
}
