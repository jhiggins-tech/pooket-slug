using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The coda's watchtower: it stands in the road (its column is a wall) until it's shot down, then topples over to
    /// the right, flattening the goons it lands on, and lies there as scenery.
    /// </summary>
    public sealed class ToppleTower : SetPiece
    {
        const float FallTime = 0.9f, Reach = 5.5f;
        bool m_Landed;

        public ToppleTower(World world, EnemyDef def, Vector2 feet) : base(world, def, feet, Art.Watchtower, SortOrder.Scenery + 1, bars: true) { }

        protected override float Lingers => float.PositiveInfinity; // the wreck lies across the road

        protected override void Die()
        {
            base.Die();
            World.Combat.Explode(Pos + new Vector2(0f, 0.6f), 1.2f, 0, false, 1.4f); // its legs go
            Sfx.Play(Sfx.Thump);
            World.Scoring.Popup("TIMBER!", Pos + Vector2.up * 3f, Palette.Gold);
        }

        protected override void GoesOff() { } // its legs went as it died; it lands after FallTime

        protected override void TickDying(float dt)
        {
            base.TickDying(dt);
            if (m_Landed || DyingTime < FallTime) return;
            m_Landed = true;
            // it lands across the road ahead: goons under it are flattened, and the cabin goes up
            Vector2 cabin = Pos + new Vector2(Reach - 0.6f, 0.5f);
            World.Combat.Explode(cabin, 1.8f, 0, false, 1.8f);
            World.View.Shake(0.45f);
            foreach (Enemy e in World.Enemies)
                if (e != this && e.Shootable && e.Pos.x > Pos.x && e.Pos.x < Pos.x + Reach && e.Pos.y < Pos.y + 2f)
                    World.Combat.DamageEnemy(e, e.Hp);
        }

        public override void Render()
        {
            // not the default topple and blink: it falls like a tree, from its feet, and stays down
            Go.transform.localPosition = new Vector3(Snap(Pos.x), Snap(Pos.y), 0f);
            Body.sprite = Flashed(BodyLook);
            float k = Dying ? Mathf.Clamp01(DyingTime / FallTime) : 0f;
            Visual.localRotation = Quaternion.Euler(0f, 0f, -90f * k * k);
        }
    }
}
