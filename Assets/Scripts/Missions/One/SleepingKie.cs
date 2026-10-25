using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kie, asleep against the motor pool's wall with the snacks, for the rest of Mission one; on a replay he stirs as the
    /// coda gets under way and says Keep it down's line in his sleep (<see cref="KeepItDown"/>, its timing), then settles back.
    /// </summary>
    public sealed class SleepingKie : Entity
    {
        /// <summary>How fast he shifts as he stirs, a pixel each way.</summary>
        const float StirHz = 8f;

        readonly PlayerVisual m_Visual;
        readonly Transform m_Body;
        float m_Z;
        /// <summary>Seconds since he was stirred (Keep it down), or below zero while he sleeps on.</summary>
        float m_Stirred = -1f;
        bool m_Said;

        public SleepingKie(World world, Vector2 feet) : base(world, "kie, asleep", feet)
        {
            m_Body = new GameObject("Body").transform;
            m_Body.SetParent(Go.transform, false);
            m_Visual = new PlayerVisual(m_Body, Art.SkinFor(Content.Kie), SortOrder.Scenery + 2);
            m_Body.localScale = new Vector3(-1f, 1f, 1f); // facing back the way tones2 came
        }

        /// <summary>Keep it down: the tabk's engine turns over beside him (<paramref name="engine"/>: tones2's climbed in), and he stirs.</summary>
        public void Stir(bool engine)
        {
            if (m_Stirred >= 0f) return;
            m_Stirred = 0f;
            if (engine) Sfx.Play(Sfx.EngineRev, 0.5f);
        }

        public override void Tick(float dt)
        {
            if (m_Stirred >= 0f)
            {
                m_Stirred += dt;
                if (!m_Said && m_Stirred >= KeepItDown.LineAt)
                {
                    m_Said = true;
                    World.Scoring.Popup(KeepItDown.Line, Pos + new Vector2(0f, 1.9f), Crew.Kie.Card); // in kie's colours
                }
            }
            bool talking = m_Stirred >= 0f && m_Stirred < KeepItDown.ZsBack; // no Zs until he settles back
            m_Z += dt;
            if (m_Z < 1.4f) return;
            m_Z = 0f;
            if (!talking) World.Scoring.Popup("Z", Pos + new Vector2(-0.3f, 1.4f), new Color(0.8f, 0.9f, 1f));
            if (Pos.x < World.View.Left - 4f) Remove();
        }

        public override void Render()
        {
            base.Render();
            m_Visual.Apply(new AvatarPose { Facing = 1, Grounded = true, Crouching = true });
            bool stirring = m_Stirred >= 0f && m_Stirred < KeepItDown.StirFor;
            m_Body.localPosition = new Vector3(stirring ? Fx.Shiver(m_Stirred, StirHz, 1f / Art.PPU) : 0f, 0f, 0f);
        }
    }
}
