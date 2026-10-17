using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The base's last gate, at the end of the coda: a wall until it's shot down or rammed by the tabk. Through it is
    /// the road out, and the mission's won (<see cref="World.OnCodaCleared"/>).
    /// </summary>
    public sealed class ExitGate : Enemy
    {
        const float RamTime = 0.35f;
        readonly SpriteRenderer m_Sr;
        readonly int m_Column;
        float m_Ram;

        public ExitGate(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Sr = AddRenderer(Visual, "Gate", CodaArt.ExitGate, SortOrder.Scenery + 1);
            m_Column = Mathf.FloorToInt(feet.x);
            world.Level.Bar(m_Column, true);
        }

        protected override void TickAlive(float dt)
        {
            Vel = Vector2.zero;
            // the tabk driven into it, still pushing, smashes straight through
            PlayerTank tank = World.Tank;
            bool pushing = tank != null && tank.Occupied && World.Player.Input.MoveX > 0.3f &&
                           tank.Pos.x + tank.HalfW >= Pos.x - HalfW - 0.15f && tank.Pos.x < Pos.x;
            m_Ram = pushing ? m_Ram + dt : 0f;
            if (m_Ram >= RamTime)
            {
                World.Scoring.Popup("RAMMED!", Center + Vector2.up * 1.6f, new Color(1f, 0.85f, 0.3f));
                World.Combat.DamageEnemy(this, Hp);
            }
        }

        protected override void Die()
        {
            base.Die();
            World.Level.Bar(m_Column, false);
            for (int i = 0; i < 3; i++)
                World.Combat.Explode(Pos + new Vector2(Rng.Fx.Range(-0.8f, 0.8f), 0.8f + i * 1.1f), 1.2f, 0, false, 1.3f + 0.2f * i);
            World.View.Shake(0.5f);
            World.OnCodaCleared();
        }

        protected override void TickDying(float dt) { }

        public override void Render()
        {
            Go.transform.localPosition = new Vector3(Snap(Pos.x), Snap(Pos.y), 0f);
            m_Sr.sprite = Dying ? CodaArt.Rubble : Flashed(CodaArt.ExitGate);
        }
    }
}
