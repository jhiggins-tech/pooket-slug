using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// tones2's liquids. Droplets from his water jet, which soak damage into goons and push them back, and
    /// splash back onto him off walls and big things up close; the toxic mud his rocket jump leaves; and the
    /// gobbets of his spew, which leave burning sludge where they land. Droplets are pooled and run in the
    /// world's effects list, so they tick and sweep with everything else. In co-op a partner's liquids are
    /// cosmetic copies here, as their shots are: they splash and lie about, but only the partner's own do harm.
    /// </summary>
    public sealed class Liquids
    {
        readonly World m_World;
        EntityPool<Droplet> m_Drops;
        // by column: one strip each, our own and a partner's (cosmetic) kept apart
        readonly Dictionary<int, MudPuddle> m_Sludge = new Dictionary<int, MudPuddle>(), m_CopySludge = new Dictionary<int, MudPuddle>();
        float m_SplatQuiet;

        public Liquids(World world) => m_World = world;

        /// <summary>A droplet: from the jet, splashing back at tones2, or a gobbet of spew; a harmless copy if <paramref name="cosmetic"/>.</summary>
        public void Spray(Vector2 pos, Vector2 vel, float gravity, float damage, float push, DropKind kind = DropKind.Jet, bool cosmetic = false)
        {
            if (m_Drops == null) m_Drops = new EntityPool<Droplet>(() => new Droplet(m_World), 128);
            Droplet d = m_Drops.Get();
            d.Launch(pos, vel, gravity, damage, push, kind, cosmetic);
            m_World.Effects.Add(d);
        }

        /// <summary>A gobbet of spew landed at <paramref name="at"/>: sludge on the floor there, or fresh sludge if there's some already.</summary>
        public void SpillSludge(Vector2 at, bool cosmetic = false)
        {
            Level level = m_World.Level;
            Dictionary<int, MudPuddle> sludge = cosmetic ? m_CopySludge : m_Sludge;
            int c = Mathf.FloorToInt(at.x);
            int top = Mathf.FloorToInt(at.y + 0.5f);
            for (int y = top; y >= top - 2; y--)
            {
                if (level.Get(c, y - 1) == Level.Empty || level.Get(c, y) != Level.Empty) continue;
                if (sludge.TryGetValue(c, out MudPuddle old) && !old.Removed && Mathf.Approximately(old.Pos.y, y))
                {
                    old.Renew();
                    return;
                }
                sludge[c] = new MudPuddle(m_World, new Vector2(c, y), MudPuddle.Kind.Sludge, c, cosmetic);
                m_World.Effects.Add(sludge[c]);
                return;
            }
        }

        /// <summary>Carpets the floor around <paramref name="feet"/> in toxic mud (harmless if <paramref name="cosmetic"/>).</summary>
        public void SpillMud(Vector2 feet, RocketSpec spec, bool cosmetic = false)
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
                    m_World.Effects.Add(new MudPuddle(m_World, new Vector2(c, y), MudPuddle.Kind.Mud, c, cosmetic));
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

    /// <summary>What a droplet is: from the jet, splashing back at tones2, or a gobbet of his spew.</summary>
    public enum DropKind { Jet, Splash, Spew }

    /// <summary>One droplet of tones2's jet or spew. Pooled: <see cref="Liquids.Spray"/> launches one.</summary>
    public sealed class Droplet : Entity
    {
        readonly SpriteRenderer m_Sr;
        Vector2 m_Vel;
        float m_Gravity, m_Damage, m_Push, m_Life;
        DropKind m_Kind;
        bool m_Cosmetic;

        public Droplet(World world) : base(world, "Droplet", Vector2.zero)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", null, SortOrder.Projectiles);
        }

        public void Launch(Vector2 pos, Vector2 vel, float gravity, float damage, float push, DropKind kind, bool cosmetic)
        {
            m_Cosmetic = cosmetic;
            Pos = pos;
            m_Vel = vel;
            m_Gravity = gravity;
            m_Damage = damage;
            m_Push = push;
            m_Kind = kind;
            m_Life = Tones2Tuning.Jet.DropLife;
            m_Sr.sprite = kind == DropKind.Spew ? Tones2Art.Gobs[Rng.Fx.Range(0, Tones2Art.Gobs.Length)]
                : Tones2Art.Drops[kind == DropKind.Jet && vel.sqrMagnitude > 14f * 14f ? 1 : 0];
        }

        public override void Destroy() => World.Liquids.Release(this);

        public override void Tick(float dt)
        {
            m_Life -= dt;
            m_Vel.y -= m_Gravity * dt;
            Vector2 next = Pos + m_Vel * dt;

            if (m_Kind == DropKind.Splash)
            {
                Player p = World.Player;
                if (!p.Dead && p.Bounds.Contains(next))
                {
                    // Splashback doesn't hurt: it just slows him down for a moment.
                    p.SlowTimer = Tones2Tuning.Jet.SplashSlowTime;
                    World.SpawnParticle(next, new Vector2(Rng.Fx.Range(-1f, 1f), -1f), Tones2Art.Water, 1.5f, 0.4f);
                    Remove();
                    return;
                }
            }
            else
            {
                foreach (Enemy e in World.Enemies)
                {
                    if (!e.Shootable || !e.Bounds.Contains(next)) continue;
                    if (m_Cosmetic) Remove(); // a partner's copy: their own droplet does the damage
                    else Soak(e, next);
                    return;
                }
            }

            if (World.Level.IsSolidAt(next))
            {
                if (m_Kind == DropKind.Jet) SplashOff(next);
                else if (m_Kind == DropKind.Spew) World.Liquids.SpillSludge(Pos, m_Cosmetic);
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
            JetSpec spec = Tones2Tuning.Jet;
            Vector2 dir = m_Vel.normalized;
            e.Shove = Vector2.ClampMagnitude(e.Shove + dir * m_Push, spec.MaxShove);
            Color fleck = m_Kind == DropKind.Spew ? Tones2Art.Spew : Tones2Art.Water;
            if (Rng.Fx.Value < 0.4f) World.SpawnParticle(at, new Vector2(-dir.x * Rng.Fx.Range(1f, 3f), Rng.Fx.Range(1f, 4f)), fleck, 1.5f, 0.35f);
            World.HitFeel.Fizz(at);
            World.Liquids.Splat(0.25f);
            if (m_Kind == DropKind.Jet && e.HalfW >= 1f) SplashOff(at); // something big up close splashes back too
        }

        /// <summary>Off a wall or something big: up close, some of it arcs back onto tones2.</summary>
        void SplashOff(Vector2 at)
        {
            for (int i = 0; i < 2; i++)
                World.SpawnParticle(at, new Vector2(-m_Vel.x * Rng.Fx.Range(0.05f, 0.2f), Rng.Fx.Range(1f, 5f)), Tones2Art.Water, 1.5f, 0.4f);

            if (m_Cosmetic) return; // a partner's splashback lands on them, on their screen
            Player p = World.Player;
            JetSpec spec = Tones2Tuning.Jet;
            Vector2 to = p.Center - at;
            if (p.Dead || to.sqrMagnitude > spec.SplashRange * spec.SplashRange || Rng.Fx.Value > 0.5f) return;
            // A short lob that lands on him: flight time t, under the splash's own gravity.
            const float t = 0.3f, g = 24f;
            var vel = new Vector2(to.x / t + Rng.Fx.Range(-1f, 1f), (to.y + 0.5f * g * t * t) / t);
            Vector2 start = at - m_Vel.normalized * 0.1f;
            World.Liquids.Spray(start, vel, g, 0f, 0f, DropKind.Splash);
        }

        public override void Render()
        {
            base.Render();
            float speed = m_Vel.magnitude;
            m_Sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(m_Vel.y, m_Vel.x) * Mathf.Rad2Deg);
            // Fast droplets stretch along their flight, so the full jet reads as a stream. Gobbets stay lumpy.
            m_Sr.transform.localScale = m_Kind == DropKind.Spew ? new Vector3(1.5f, 1.5f, 1f) : new Vector3(1f + speed / 9f, 1f, 1f);
        }
    }

    /// <summary>
    /// A tile-wide strip on the floor that hurts goons standing in it: toxic mud from tones2's rocket jump
    /// (poison) or sludge from his spew (it burns). A partner's (cosmetic) strip only lies there and bubbles.
    /// </summary>
    public sealed class MudPuddle : Entity
    {
        public enum Kind { Mud, Sludge }

        readonly SpriteRenderer m_Sr;
        readonly Kind m_Kind;
        readonly float m_Life, m_Interval;
        readonly bool m_Cosmetic;
        readonly Dictionary<Enemy, float> m_NextDose = new Dictionary<Enemy, float>();
        float m_Age, m_BubbleTimer;

        public MudPuddle(World world, Vector2 bottomLeft, Kind kind, int variant, bool cosmetic = false)
            : base(world, kind == Kind.Mud ? "Mud" : "Sludge", bottomLeft)
        {
            m_Kind = kind;
            m_Cosmetic = cosmetic;
            m_Life = kind == Kind.Mud ? Tones2Tuning.Rocket.MudLife : Tones2Tuning.Spew.SludgeLife;
            m_Interval = kind == Kind.Mud ? Tones2Tuning.Rocket.PoisonInterval : Tones2Tuning.Spew.BurnInterval;
            Sprite[] strips = kind == Kind.Mud ? Tones2Art.MudStrips : Tones2Art.SludgeStrips;
            m_Sr = AddRenderer(Go.transform, "Sprite", strips[(variant % strips.Length + strips.Length) % strips.Length], SortOrder.Pickups);
            m_BubbleTimer = Rng.Fx.Range(0.1f, 0.6f);
        }

        /// <summary>Where it hurts: the tile it covers, a little way up.</summary>
        Rect Area => new Rect(Pos.x, Pos.y, 1f, 0.45f);

        /// <summary>More landed on it: it starts its time over (without oozing in again).</summary>
        public void Renew() => m_Age = Mathf.Min(m_Age, 0.2f);

        public override void Tick(float dt)
        {
            m_Age += dt;
            if (m_Age >= m_Life)
            {
                Remove();
                return;
            }

            bool mud = m_Kind == Kind.Mud;
            Color fleck = mud ? Tones2Art.Toxic : Tones2Art.Ember;
            Rect area = Area;
            foreach (Enemy e in World.Enemies)
            {
                if (m_Cosmetic) break;
                if (!e.Shootable || !e.Bounds.Overlaps(area)) continue;
                if (mud) e.PoisonTimer = 0.4f;
                else e.BurnTimer = 0.4f;
                if (m_NextDose.TryGetValue(e, out float at) && World.Elapsed < at) continue;
                m_NextDose[e] = World.Elapsed + m_Interval;
                World.Combat.DamageEnemy(e, 1);
                Sfx.Play(mud ? Sfx.Poison : Sfx.Sizzle, 0.4f);
                World.SpawnParticle(e.Center, new Vector2(Rng.Fx.Range(-1f, 1f), 3f), fleck, 1.5f, 0.6f);
            }

            m_BubbleTimer -= dt;
            if (m_BubbleTimer <= 0f)
            {
                m_BubbleTimer = mud ? Rng.Fx.Range(0.4f, 1.2f) : Rng.Fx.Range(0.15f, 0.5f); // sludge smokes and spits more
                World.SpawnParticle(Pos + new Vector2(Rng.Fx.Range(0.1f, 0.9f), 0.3f), new Vector2(0f, 2.5f), fleck, 1.5f, 0.35f);
            }
        }

        public override void Render()
        {
            base.Render();
            // Oozes in, then dries up over its last second.
            float grow = Mathf.Clamp01(m_Age / 0.2f), fade = Mathf.Clamp01(m_Life - m_Age);
            m_Sr.transform.localScale = new Vector3(1f, grow, 1f);
            Color c = Color.white;
            if (m_Kind == Kind.Sludge) c = Color.Lerp(Color.white, new Color(1f, 0.75f, 0.55f), 0.5f + 0.5f * Mathf.Sin(m_Age * 9f + Pos.x)); // a hot glow
            c.a = fade;
            m_Sr.color = c;
        }
    }
}
