using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The motor pool's back gate, at the right of the boss's arena. Shut (a wall, <see cref="Level.ScrollLimit"/>)
    /// until kie's freed; then it grinds up, juddering, and the coda starts.
    /// </summary>
    public sealed class BackGate : Entity
    {
        const float OpenTime = 1.6f;
        readonly SpriteRenderer m_Sr;
        float m_Open = -1f;

        public BackGate(World world, Vector2 feet) : base(world, "Back gate", feet)
        {
            m_Sr = AddRenderer(Go.transform, "Shutter", CodaArt.BackGate, SortOrder.Scenery + 1);
            if (world.CodaOpen) m_Open = OpenTime; // a restart at the coda: it's already up
        }

        public bool Opening => m_Open >= 0f && m_Open < OpenTime;

        public void Open()
        {
            if (m_Open >= 0f) return;
            m_Open = 0f;
            Sfx.Play(Sfx.Thump);
            World.View.Shake(0.2f);
        }

        public override void Tick(float dt)
        {
            if (m_Open < 0f || m_Open >= OpenTime) return;
            m_Open += dt;
            if ((int)(m_Open * 6f) != (int)((m_Open - dt) * 6f)) World.View.Shake(0.06f); // it judders up
        }

        public override void Render()
        {
            base.Render();
            // it rolls up into the wall above: squashed from the bottom up
            float k = m_Open < 0f ? 0f : Mathf.Clamp01(m_Open / OpenTime);
            float judder = k > 0f && k < 1f ? Fx.Step(m_Open, 14f, 2) * 0.04f : 0f;
            float left = Mathf.Max(0.04f, 1f - k + judder);
            Transform t = m_Sr.transform;
            t.localScale = new Vector3(1f, left, 1f);
            t.localPosition = new Vector3(0f, 4f * (1f - left), 0f);
        }
    }
}
