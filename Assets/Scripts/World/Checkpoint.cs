using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The checkpoint at the boss door: our soldier passing it lights it, and losing after that restarts a solo
    /// game here rather than at the start of the run (<c>docs/design/levels.md</c>, Missions).
    /// </summary>
    public sealed class Checkpoint : Entity
    {
        readonly SpriteRenderer m_Sr;
        bool m_Lit;

        public Checkpoint(World world, Vector2 feet) : base(world, "Checkpoint", feet)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", Art.Checkpoint[0], SortOrder.Pickups);
        }

        public override void Tick(float dt)
        {
            if (m_Lit) return;
            if (World.CheckpointX >= Pos.x)
            {
                m_Lit = true; // restarted here: already lit
                return;
            }
            Player p = World.Player;
            if (p.Dead || p.Pos.x < Pos.x) return;
            m_Lit = true;
            World.CheckpointX = Pos.x;
            World.Scoring.Popup("CHECKPOINT!", Pos + Vector2.up * 2.8f, new Color(0.45f, 1f, 0.6f));
            Sfx.Play(Sfx.Pickup);
        }

        public override void Render()
        {
            base.Render();
            m_Sr.sprite = Art.Checkpoint[m_Lit ? 1 : 0];
        }
    }
}
