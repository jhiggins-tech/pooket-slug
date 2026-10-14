using UnityEngine;

namespace PooketSlug
{
    public enum ProjectileKind { Bullet, Heavy, EnemyBullet, Shell, Grenade, Bomb }

    public sealed class Projectile : Entity
    {
        public readonly ProjectileKind Kind;
        public readonly bool Hostile; // true = hurts the player
        public Vector2 Vel;
        public readonly float Radius;
        public readonly int Damage;

        readonly SpriteRenderer m_Sr;
        readonly float m_Gravity;
        float m_Life, m_Spin;
        int m_Bounces;

        public Projectile(World world, ProjectileKind kind, Vector2 pos, Vector2 vel, bool hostile) : base(world, kind.ToString(), pos)
        {
            Kind = kind;
            Hostile = hostile;
            Vel = vel;
            Sprite sprite;
            switch (kind)
            {
                case ProjectileKind.Heavy: sprite = Art.HeavyBullet; Radius = 0.12f; Damage = 1; break;
                case ProjectileKind.EnemyBullet: sprite = Art.EnemyBullet; Radius = 0.14f; Damage = 1; break;
                case ProjectileKind.Shell: sprite = Art.Shell; Radius = 0.2f; Damage = 1; m_Gravity = 18f; break;
                case ProjectileKind.Grenade: sprite = Art.Grenade; Radius = 0.18f; Damage = 6; m_Gravity = 30f; break;
                case ProjectileKind.Bomb: sprite = Art.Bomb; Radius = 0.18f; Damage = 1; m_Gravity = 22f; break;
                default: sprite = Art.Bullet; Radius = 0.12f; Damage = 1; break;
            }
            m_Sr = AddRenderer(Go.transform, "Sprite", sprite, SortOrder.Projectiles);
            m_Life = kind == ProjectileKind.Grenade ? 1.8f : 4f;
        }

        bool Explosive => Kind == ProjectileKind.Shell || Kind == ProjectileKind.Grenade || Kind == ProjectileKind.Bomb;

        public override void Tick(float dt)
        {
            m_Life -= dt;
            Vel.y -= m_Gravity * dt;
            Vector2 next = Pos + Vel * dt;

            if (World.Level.IsSolidAt(next))
            {
                if (Kind == ProjectileKind.Grenade && m_Bounces < 1 && Vel.y < 0f && World.Level.IsSolidAt(new Vector2(Pos.x, next.y)))
                {
                    // bounce once off the floor
                    m_Bounces++;
                    Vel = new Vector2(Vel.x * 0.55f, -Vel.y * 0.45f);
                    return;
                }
                Pos = next;
                Detonate();
                return;
            }
            Pos = next;
            m_Spin += dt;

            if (m_Life <= 0f) { Detonate(); return; }

            Rect view = World.ViewRect;
            if (Pos.x < view.xMin - 2f || Pos.x > view.xMax + 2f || Pos.y < -2f || Pos.y > view.yMax + 4f) Remove();
        }

        /// <summary>Called by <see cref="World"/> when this projectile touches a valid target.</summary>
        public void Detonate()
        {
            if (Removed) return;
            Remove();
            if (Explosive)
            {
                float radius = Kind == ProjectileKind.Grenade ? 1.7f : 1.0f;
                World.Explode(Pos, radius, Damage, Hostile, Kind == ProjectileKind.Grenade ? 1f : 0.7f);
            }
            else
            {
                World.Effects.Add(new Effect(World, Art.Spark, 30f, Pos));
            }
        }

        public override void Render()
        {
            base.Render();
            float angle;
            if (Kind == ProjectileKind.Grenade) angle = -m_Spin * 720f * Mathf.Sign(Vel.x == 0f ? 1f : Vel.x);
            else if (Kind == ProjectileKind.Bomb || Kind == ProjectileKind.EnemyBullet) angle = 0f;
            else angle = Mathf.Atan2(Vel.y, Vel.x) * Mathf.Rad2Deg;
            m_Sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    /// <summary>Frame-animated sprite that removes itself when the animation ends.</summary>
    public sealed class Effect : Entity
    {
        readonly Sprite[] m_Frames;
        readonly float m_Fps;
        readonly SpriteRenderer m_Sr;
        readonly Vector2 m_Vel;
        float m_Time;

        public Effect(World world, Sprite[] frames, float fps, Vector2 pos, float scale = 1f, Vector2 vel = default) : base(world, "Effect", pos)
        {
            m_Frames = frames;
            m_Fps = fps;
            m_Vel = vel;
            m_Sr = AddRenderer(Go.transform, "Sprite", frames[0], SortOrder.Effects);
            Go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        public override void Tick(float dt)
        {
            m_Time += dt;
            Pos += m_Vel * dt;
            if (m_Time * m_Fps >= m_Frames.Length) Remove();
        }

        public override void Render()
        {
            base.Render();
            m_Sr.sprite = m_Frames[Mathf.Min(m_Frames.Length - 1, (int)(m_Time * m_Fps))];
        }
    }

    /// <summary>Single tinted pixel-square particle with gravity (debris, blood, sparks).</summary>
    public sealed class Particle : Entity
    {
        readonly SpriteRenderer m_Sr;
        Vector2 m_Vel;
        float m_Life;
        readonly float m_MaxLife;

        public Particle(World world, Vector2 pos, Vector2 vel, Color color, float size, float life) : base(world, "Particle", pos)
        {
            m_Vel = vel;
            m_Life = m_MaxLife = life;
            m_Sr = AddRenderer(Go.transform, "Sprite", Art.Pixel, SortOrder.Effects - 1);
            m_Sr.color = color;
            Go.transform.localScale = new Vector3(size, size, 1f);
        }

        public override void Tick(float dt)
        {
            m_Life -= dt;
            m_Vel.y -= 30f * dt;
            Pos += m_Vel * dt;
            if (World.Level.IsSolidAt(Pos))
            {
                Pos.y = Mathf.Ceil(Pos.y);
                m_Vel = new Vector2(m_Vel.x * 0.5f, Mathf.Abs(m_Vel.y) * 0.3f);
            }
            if (m_Life <= 0f) Remove();
        }

        public override void Render()
        {
            base.Render();
            Color c = m_Sr.color;
            c.a = Mathf.Clamp01(m_Life / (m_MaxLife * 0.4f));
            m_Sr.color = c;
        }
    }

    public enum PickupKind { HeavyMachineGun, Bombs, Medkit }

    public sealed class Pickup : Entity
    {
        public readonly PickupKind Kind;
        readonly SpriteRenderer m_Sr;
        float m_Time;

        public Pickup(World world, PickupKind kind, Vector2 feet) : base(world, "Pickup", feet)
        {
            Kind = kind;
            Sprite sprite = kind == PickupKind.HeavyMachineGun ? Art.CrateHeavy : kind == PickupKind.Bombs ? Art.CrateBombs : Art.Medkit;
            m_Sr = AddRenderer(Go.transform, "Sprite", sprite, SortOrder.Pickups);
        }

        public Rect Bounds => new Rect(Pos.x - 0.45f, Pos.y, 0.9f, 0.8f);

        public override void Tick(float dt) => m_Time += dt;

        public override void Render()
        {
            base.Render();
            // gentle hop so pickups read as interactive
            float hop = Mathf.Abs(Mathf.Sin(m_Time * 4f)) * 2f;
            m_Sr.transform.localPosition = new Vector3(0f, Mathf.Round(hop) / Art.PPU, 0f);
        }

        public void Apply(Player player)
        {
            switch (Kind)
            {
                case PickupKind.HeavyMachineGun:
                    player.HeavyAmmo += 200;
                    World.Popup("HEAVY MACHINE GUN!", Pos + Vector2.up * 1.6f, new Color(1f, 0.85f, 0.3f));
                    break;
                case PickupKind.Bombs:
                    player.Grenades += 10;
                    World.Popup("BOMBS +10", Pos + Vector2.up * 1.6f, new Color(0.6f, 1f, 0.5f));
                    break;
                case PickupKind.Medkit:
                    player.Heal(2);
                    World.Popup("+2 HP", Pos + Vector2.up * 1.6f, new Color(1f, 0.5f, 0.5f));
                    break;
            }
            World.AddScore(500, Pos + Vector2.up, false);
            Sfx.Play(Sfx.Pickup);
            Remove();
        }
    }

    /// <summary>Static decoration (end-of-level flag).</summary>
    public sealed class Prop : Entity
    {
        public Prop(World world, Sprite sprite, Vector2 feet, int order) : base(world, "Prop", feet)
        {
            AddRenderer(Go.transform, "Sprite", sprite, order);
        }

        public override void Tick(float dt) { }
    }
}
