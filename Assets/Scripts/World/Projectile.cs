using UnityEngine;

namespace PooketSlug
{
    /// <summary>A bullet, shell, grenade or bomb. Pooled: <see cref="World.SpawnProjectile"/> launches one.</summary>
    public sealed class Projectile : Entity
    {
        public ProjectileDef Def { get; private set; }
        public bool Hostile { get; private set; }  // true = hurts the player
        public bool Cosmetic { get; private set; } // replay of another player's shot: looks the same, deals no damage
        public Vector2 Vel;

        readonly SpriteRenderer m_Sr;
        float m_Life, m_Spin;
        int m_Bounces;

        public Projectile(World world) : base(world, "Projectile", Vector2.zero)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", null, SortOrder.Projectiles);
        }

        public void Launch(ProjectileDef def, Vector2 pos, Vector2 vel, bool hostile, bool cosmetic)
        {
            Def = def;
            Pos = pos;
            Vel = vel;
            Hostile = hostile;
            Cosmetic = cosmetic;
            m_Sr.sprite = Art.ProjectileSprite(def);
            m_Life = def.Life;
            m_Spin = 0f;
            m_Bounces = 0;
        }

        public float Radius => Def.Radius;
        public int Damage => Def.Damage;

        public override void Destroy() => World.Release(this);

        public override void Tick(float dt)
        {
            m_Life -= dt;
            Vel.y -= Def.Gravity * dt;
            Vector2 next = Pos + Vel * dt;

            if (World.Level.IsSolidAt(next))
            {
                if (m_Bounces < Def.Bounces && Vel.y < 0f && World.Level.IsSolidAt(new Vector2(Pos.x, next.y)))
                {
                    // bounce off the floor
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

        /// <summary>Called by <see cref="Combat"/> when this projectile touches a valid target.</summary>
        public void Detonate()
        {
            if (Removed) return;
            Remove();
            if (Def.Explodes) World.Combat.Explode(Pos, Def.BlastRadius, Cosmetic ? 0 : Def.Damage, Hostile, Def.BlastSize);
            else World.SpawnEffect(Art.Spark, 30f, Pos);
        }

        public override void Render()
        {
            base.Render();
            float angle;
            switch (Def.Spin)
            {
                case ProjectileSpin.Tumble: angle = -m_Spin * 720f * Mathf.Sign(Vel.x == 0f ? 1f : Vel.x); break;
                case ProjectileSpin.None: angle = 0f; break;
                default: angle = Mathf.Atan2(Vel.y, Vel.x) * Mathf.Rad2Deg; break;
            }
            m_Sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
