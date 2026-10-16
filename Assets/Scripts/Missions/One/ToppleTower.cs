using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The coda's watchtower: it stands in the road (its column is a wall) until it's shot down, then topples over to
    /// the right, flattening the goons it lands on, and lies there as scenery.
    /// </summary>
    public sealed class ToppleTower : Enemy
    {
        const float FallTime = 0.9f, Reach = 5.5f;
        const int LandDamage = 12;
        readonly SpriteRenderer m_Sr;
        readonly int m_Column;
        bool m_Landed;

        public ToppleTower(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Sr = AddRenderer(Visual, "Tower", Art.Watchtower, SortOrder.Scenery + 1);
            m_Column = Mathf.FloorToInt(feet.x);
            world.Level.Bar(m_Column, true);
        }

        protected override void TickAlive(float dt) => Vel = Vector2.zero;

        protected override void Die()
        {
            base.Die();
            World.Level.Bar(m_Column, false);
            World.Combat.Explode(Pos + new Vector2(0f, 0.6f), 1.2f, 0, false, 1.4f); // its legs go
            Sfx.Play(Sfx.Thump);
            World.Scoring.Popup("TIMBER!", Pos + Vector2.up * 3f, new Color(1f, 0.85f, 0.3f));
        }

        protected override void TickDying(float dt)
        {
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

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (Dying && !Removed && Pos.x < World.View.Left - 6f) Remove(); // the wreck's left behind
        }

        public override void Render()
        {
            // not the default topple and blink: it falls like a tree, from its feet, and stays down
            Go.transform.localPosition = new Vector3(Snap(Pos.x), Snap(Pos.y), 0f);
            m_Sr.sprite = Flashed(Art.Watchtower);
            float k = Dying ? Mathf.Clamp01(DyingTime / FallTime) : 0f;
            Visual.localRotation = Quaternion.Euler(0f, 0f, -90f * k * k);
        }
    }
}
