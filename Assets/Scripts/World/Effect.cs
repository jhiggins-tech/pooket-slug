using UnityEngine;

namespace PooketSlug
{
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
}
