using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The base's last gate, at the end of the coda: a wall until it's shot down or rammed by the tabk. Through it is
    /// the road out, and the mission's won (<see cref="World.OnCodaCleared"/>).
    /// </summary>
    public sealed class ExitGate : SetPiece
    {
        const float RamTime = 0.35f;
        float m_Ram;

        public ExitGate(World world, EnemyDef def, Vector2 feet) : base(world, def, feet, CodaArt.ExitGate, SortOrder.Scenery + 1, bars: true) { }

        protected override float Lingers => float.PositiveInfinity; // rubble

        protected override void TickAlive(float dt)
        {
            base.TickAlive(dt);
            // the tabk driven into it, still pushing, smashes straight through
            PlayerTank tank = World.Tank;
            bool pushing = tank != null && tank.Occupied && World.Player.Input.MoveX > 0.3f &&
                           tank.Pos.x + tank.HalfW >= Pos.x - HalfW - 0.15f && tank.Pos.x < Pos.x;
            m_Ram = pushing ? m_Ram + dt : 0f;
            if (m_Ram >= RamTime)
            {
                World.Scoring.Popup("RAMMED!", Center + Vector2.up * 1.6f, Palette.Gold);
                World.Combat.DamageEnemy(this, Hp);
            }
        }

        protected override void Die()
        {
            base.Die();
            for (int i = 0; i < 3; i++)
                World.Combat.Explode(Pos + new Vector2(Rng.Fx.Range(-0.8f, 0.8f), 0.8f + i * 1.1f), 1.2f, 0, false, 1.3f + 0.2f * i);
            World.View.Shake(0.5f);
            World.OnCodaCleared();
        }

        protected override void GoesOff() { } // it blew as it died, on every screen

        public override void Render()
        {
            // not the default topple and blink: it stands as rubble
            Go.transform.localPosition = new Vector3(Snap(Pos.x), Snap(Pos.y), 0f);
            Body.sprite = Dying ? CodaArt.Rubble : Flashed(BodyLook);
        }
    }
}
