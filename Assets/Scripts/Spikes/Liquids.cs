using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike: tones2's liquids. Droplets from his water jet, which soak damage into goons and push them back, and
    /// splash back onto him off walls and big things up close; and the toxic mud his rocket jump leaves. Droplets
    /// are pooled and run in the world's effects list, so they tick and sweep with everything else.
    /// </summary>
    public sealed class Liquids
    {
        readonly World m_World;
        EntityPool<Droplet> m_Drops;
        float m_SplatQuiet;

        public Liquids(World world) => m_World = world;

        /// <summary>A droplet from the jet (or, with <paramref name="splash"/>, one splashing back at tones2).</summary>
        public void Spray(Vector2 pos, Vector2 vel, float gravity, float damage, float push, bool splash = false)
        {
            if (m_Drops == null) m_Drops = new EntityPool<Droplet>(() => new Droplet(m_World), 128);
            Droplet d = m_Drops.Get();
            d.Launch(pos, vel, gravity, damage, push, splash);
            m_World.Effects.Add(d);
        }

        /// <summary>Carpets the floor around <paramref name="feet"/> in toxic mud.</summary>
        public void SpillMud(Vector2 feet, RocketSpec spec)
        {
            Level level = m_World.Level;
            int c0 = Mathf.FloorToInt(feet.x - spec.MudHalfWidth), c1 = Mathf.FloorToInt(feet.x + spec.MudHalfWidth);
            int top = Mathf.FloorToInt(feet.y + 0.5f);
            for (int c = c0; c <= c1; c++)
            {
                // The first floor at or a little below his feet: mud doesn't climb walls or float over pits.
                for (int y = top; y >= top - 2; y--)
                {
                    if (level.Get(c, y - 1) == Level.Empty || level.Get(c, y) != Level.Empty) continue;
                    m_World.Effects.Add(new MudPuddle(m_World, new Vector2(c, y), spec, c));
                    break;
                }
            }
        }

        public void Release(Droplet d) => m_Drops.Release(d);

        /// <summary>A wet slap, but not for every droplet.</summary>
        public void Splat(float volume)
        {
            if (m_World.Elapsed < m_SplatQuiet) return;
            m_SplatQuiet = m_World.Elapsed + 0.09f;
            Sfx.Play(Sfx.Splat, volume);
        }
    }

    /// <summary>One droplet of tones2's jet. Pooled: <see cref="Liquids.Spray"/> launches one.</summary>
    public sealed class Droplet : Entity
    {
        readonly SpriteRenderer m_Sr;
        Vector2 m_Vel;
        float m_Gravity, m_Damage, m_Push, m_Life;
        bool m_Splash;

        public Droplet(World world) : base(world, "Droplet", Vector2.zero)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", null, SortOrder.Projectiles);
        }

        public void Launch(Vector2 pos, Vector2 vel, float gravity, float damage, float push, bool splash)
        {
            Pos = pos;
            m_Vel = vel;
            m_Gravity = gravity;
            m_Damage = damage;
            m_Push = push;
            m_Splash = splash;
            m_Life = SpikeContent.Jet.DropLife;
            m_Sr.sprite = SpikeArt.Drops[!splash && vel.sqrMagnitude > 14f * 14f ? 1 : 0];
        }

        public override void Destroy() => World.Liquids.Release(this);

        public override void Tick(float dt)
        {
            m_Life -= dt;
            m_Vel.y -= m_Gravity * dt;
            Vector2 next = Pos + m_Vel * dt;

            if (m_Splash)
            {
                Player p = World.Player;
                if (!p.Dead && p.Bounds.Contains(next))
                {
                    // Splashback doesn't hurt: it just slows him down for a moment.
                    p.SlowTimer = SpikeContent.Jet.SplashSlowTime;
                    World.SpawnParticle(next, new Vector2(Rng.Fx.Range(-1f, 1f), -1f), SpikeArt.Water, 1.5f, 0.4f);
                    Remove();
                    return;
                }
            }
            else
            {
                foreach (Enemy e in World.Enemies)
                {
                    if (!e.Shootable || !e.Bounds.Contains(next)) continue;
                    Soak(e, next);
                    return;
                }
            }

            if (World.Level.IsSolidAt(next))
            {
                if (!m_Splash) SplashOff(next);
                Remove();
                return;
            }
            Pos = next;

            Rect view = World.View.Rect;
            if (m_Life <= 0f || Pos.x < view.xMin - 1f || Pos.x > view.xMax + 1f || Pos.y < -1f) Remove();
        }

        /// <summary>The droplet hits a goon: some damage soaks in, and it's pushed back.</summary>
        void Soak(Enemy e, Vector2 at)
        {
            Remove();
            e.Soak += m_Damage;
            while (e.Soak >= 1f && e.Shootable)
            {
                e.Soak -= 1f;
                World.Combat.DamageEnemy(e, 1);
            }
            JetSpec spec = SpikeContent.Jet;
            Vector2 dir = m_Vel.normalized;
            e.Shove = Vector2.ClampMagnitude(e.Shove + dir * m_Push, spec.MaxShove);
            if (Rng.Fx.Value < 0.4f) World.SpawnParticle(at, new Vector2(-dir.x * Rng.Fx.Range(1f, 3f), Rng.Fx.Range(1f, 4f)), SpikeArt.Water, 1.5f, 0.35f);
            World.Liquids.Splat(0.25f);
            if (e.HalfW >= 1f) SplashOff(at); // something big up close splashes back too
        }

        /// <summary>Off a wall or something big: up close, some of it arcs back onto tones2.</summary>
        void SplashOff(Vector2 at)
        {
            for (int i = 0; i < 2; i++)
                World.SpawnParticle(at, new Vector2(-m_Vel.x * Rng.Fx.Range(0.05f, 0.2f), Rng.Fx.Range(1f, 5f)), SpikeArt.Water, 1.5f, 0.4f);

            Player p = World.Player;
            JetSpec spec = SpikeContent.Jet;
            Vector2 to = p.Center - at;
            if (p.Dead || to.sqrMagnitude > spec.SplashRange * spec.SplashRange || Rng.Fx.Value > 0.5f) return;
            // A short lob that lands on him: flight time t, under the splash's own gravity.
            const float t = 0.3f, g = 24f;
            var vel = new Vector2(to.x / t + Rng.Fx.Range(-1f, 1f), (to.y + 0.5f * g * t * t) / t);
            Vector2 start = at - m_Vel.normalized * 0.1f;
            World.Liquids.Spray(start, vel, g, 0f, 0f, splash: true);
        }

        public override void Render()
        {
            base.Render();
            float speed = m_Vel.magnitude;
            m_Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(m_Vel.y, m_Vel.x) * Mathf.Rad2Deg);
            // Fast droplets stretch along their flight, so the full jet reads as a stream.
            m_Sr.transform.localScale = new Vector3(1f + speed / 9f, 1f, 1f);
        }
    }

    /// <summary>A tile-wide strip of toxic mud from tones2's rocket jump. Goons standing in it are poisoned.</summary>
    public sealed class MudPuddle : Entity
    {
        readonly SpriteRenderer m_Sr;
        readonly RocketSpec m_Spec;
        readonly Dictionary<Enemy, float> m_NextDose = new Dictionary<Enemy, float>();
        float m_Age, m_BubbleTimer;

        public MudPuddle(World world, Vector2 bottomLeft, RocketSpec spec, int variant) : base(world, "Mud", bottomLeft)
        {
            m_Spec = spec;
            Sprite[] strips = SpikeArt.MudStrips;
            m_Sr = AddRenderer(Go.transform, "Sprite", strips[(variant % strips.Length + strips.Length) % strips.Length], SortOrder.Pickups);
            m_BubbleTimer = Rng.Fx.Range(0.1f, 0.6f);
        }

        /// <summary>Where it poisons: the tile it covers, a little way up.</summary>
        Rect Area => new Rect(Pos.x, Pos.y, 1f, 0.45f);

        public override void Tick(float dt)
        {
            m_Age += dt;
            if (m_Age >= m_Spec.MudLife)
            {
                Remove();
                return;
            }

            Rect area = Area;
            foreach (Enemy e in World.Enemies)
            {
                if (!e.Shootable || !e.Bounds.Overlaps(area)) continue;
                e.PoisonTimer = 0.4f;
                if (m_NextDose.TryGetValue(e, out float at) && World.Elapsed < at) continue;
                m_NextDose[e] = World.Elapsed + m_Spec.PoisonInterval;
                World.Combat.DamageEnemy(e, 1);
                Sfx.Play(Sfx.Poison, 0.4f);
                World.SpawnParticle(e.Center, new Vector2(Rng.Fx.Range(-1f, 1f), 3f), SpikeArt.Toxic, 1.5f, 0.6f);
            }

            m_BubbleTimer -= dt;
            if (m_BubbleTimer <= 0f)
            {
                m_BubbleTimer = Rng.Fx.Range(0.4f, 1.2f);
                World.SpawnParticle(Pos + new Vector2(Rng.Fx.Range(0.1f, 0.9f), 0.3f), new Vector2(0f, 2.5f), SpikeArt.Toxic, 1.5f, 0.35f);
            }
        }

        public override void Render()
        {
            base.Render();
            // Oozes in, then dries up over its last second.
            float grow = Mathf.Clamp01(m_Age / 0.2f), fade = Mathf.Clamp01(m_Spec.MudLife - m_Age);
            m_Sr.transform.localScale = new Vector3(1f, grow, 1f);
            Color c = Color.white;
            c.a = fade;
            m_Sr.color = c;
        }
    }
}
