using UnityEngine;

namespace PooketSlug
{
    public enum ProjectileKind { Bullet, Heavy, EnemyBullet, Shell, Grenade, Bomb }

    /// <summary>A bullet, shell, grenade or bomb. Pooled: <see cref="World.SpawnProjectile"/> launches one.</summary>
    public sealed class Projectile : Entity
    {
        public ProjectileKind Kind { get; private set; }
        public bool Hostile { get; private set; }  // true = hurts the player
        public bool Cosmetic { get; private set; } // replay of another player's shot: looks the same, deals no damage
        public Vector2 Vel;
        public float Radius { get; private set; }
        public int Damage { get; private set; }

        readonly SpriteRenderer m_Sr;
        float m_Gravity, m_Life, m_Spin;
        int m_Bounces;

        public Projectile(World world) : base(world, "Projectile", Vector2.zero)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", null, SortOrder.Projectiles);
        }

        public void Launch(ProjectileKind kind, Vector2 pos, Vector2 vel, bool hostile, bool cosmetic)
        {
            Kind = kind;
            Pos = pos;
            Vel = vel;
            Hostile = hostile;
            Cosmetic = cosmetic;
            m_Gravity = 0f;
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
            m_Sr.sprite = sprite;
            m_Life = kind == ProjectileKind.Grenade ? 1.8f : 4f;
            m_Spin = 0f;
            m_Bounces = 0;
        }

        public override void Destroy() => World.Release(this);

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

            Rect view = World.View.Rect;
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
                World.Combat.Explode(Pos, radius, Cosmetic ? 0 : Damage, Hostile, Kind == ProjectileKind.Grenade ? 1f : 0.7f);
            }
            else
            {
                World.SpawnEffect(Art.Spark, 30f, Pos);
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

    /// <summary>Frame-animated sprite that removes itself when the animation ends. Pooled: see <see cref="World.SpawnEffect"/>.</summary>
    public sealed class Effect : Entity
    {
        readonly SpriteRenderer m_Sr;
        Sprite[] m_Frames;
        float m_Fps, m_Time;
        Vector2 m_Vel;

        public Effect(World world) : base(world, "Effect", Vector2.zero)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", null, SortOrder.Effects);
        }

        public void Play(Sprite[] frames, float fps, Vector2 pos, float scale, Vector2 vel)
        {
            m_Frames = frames;
            m_Fps = fps;
            Pos = pos;
            m_Vel = vel;
            m_Time = 0f;
            m_Sr.sprite = frames[0];
            Go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        public override void Destroy() => World.Release(this);

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

    /// <summary>Single tinted pixel-square particle with gravity (debris, blood, sparks). Pooled: see <see cref="World.SpawnParticle"/>.</summary>
    public sealed class Particle : Entity
    {
        readonly SpriteRenderer m_Sr;
        Vector2 m_Vel;
        float m_Life, m_MaxLife;

        public Particle(World world) : base(world, "Particle", Vector2.zero)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", Art.Pixel, SortOrder.Effects - 1);
        }

        public void Emit(Vector2 pos, Vector2 vel, Color color, float size, float life)
        {
            Pos = pos;
            m_Vel = vel;
            m_Life = m_MaxLife = life;
            m_Sr.color = color;
            Go.transform.localScale = new Vector3(size, size, 1f);
        }

        public override void Destroy() => World.Release(this);

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
        public int NetId = -1;      // index in Level.Spawns
        public bool ClaimPending;   // co-op: asked the authority for it, waiting for the answer
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

        /// <summary>Gives the pickup's effect to the local soldier.</summary>
        public void ApplyTo(Player player) => Give(Kind, player);

        public static void Give(PickupKind kind, Player player)
        {
            switch (kind)
            {
                case PickupKind.HeavyMachineGun: player.HeavyAmmo += 200; break;
                case PickupKind.Bombs: player.Grenades += 10; break;
                case PickupKind.Medkit: player.Heal(2); break;
            }
        }

        /// <summary>Removes the pickup with its popup and sound (on every client).</summary>
        public void Collect()
        {
            string text = Kind == PickupKind.HeavyMachineGun ? "HEAVY MACHINE GUN!" : Kind == PickupKind.Bombs ? "BOMBS +10" : "+2 HP";
            Color color = Kind == PickupKind.HeavyMachineGun ? new Color(1f, 0.85f, 0.3f) : Kind == PickupKind.Bombs ? new Color(0.6f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
            World.Scoring.Popup(text, Pos + Vector2.up * 1.6f, color);
            if (World.IsAuthority) World.Scoring.Add(500, Pos + Vector2.up, false);
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
