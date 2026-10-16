using UnityEngine;

namespace PooketSlug
{
    /// <summary>kie, asleep against the motor pool's wall with the snacks, for the rest of Mission one.</summary>
    public sealed class SleepingKie : Entity
    {
        readonly PlayerVisual m_Visual;
        readonly Transform m_Body;
        float m_Z;

        public SleepingKie(World world, Vector2 feet) : base(world, "kie, asleep", feet)
        {
            m_Body = new GameObject("Body").transform;
            m_Body.SetParent(Go.transform, false);
            m_Visual = new PlayerVisual(m_Body, Art.SkinFor(Content.Kie, 0), SortOrder.Scenery + 2);
            m_Body.localScale = new Vector3(-1f, 1f, 1f); // facing back the way tones2 came
        }

        public override void Tick(float dt)
        {
            m_Z += dt;
            if (m_Z < 1.4f) return;
            m_Z = 0f;
            World.Scoring.Popup("Z", Pos + new Vector2(-0.3f, 1.4f), new Color(0.8f, 0.9f, 1f));
            if (Pos.x < World.View.Left - 4f) Remove();
        }

        public override void Render()
        {
            base.Render();
            m_Visual.Apply(new AvatarPose { Facing = 1, Grounded = true, Crouching = true });
        }
    }
}
