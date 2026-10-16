using UnityEngine;

namespace PooketSlug
{
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
}
