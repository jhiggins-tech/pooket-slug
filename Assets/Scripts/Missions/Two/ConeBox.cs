using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A box of cones (Mission two's breakable): an enemy as far as the game is concerned, shot and replicated like one
    /// and never a Trollogram, that just stands there. Broken, it bursts into wafer and drops an item
    /// (<see cref="Creamery.ConeBoxDrop"/>), placed by the authority so every screen gets the same one.
    /// </summary>
    public sealed class ConeBox : Enemy
    {
        static readonly Color Wafer = new Color(0.85f, 0.62f, 0.35f);
        readonly SpriteRenderer m_Sr;
        bool m_Broken;

        public ConeBox(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Sr = AddRenderer(Visual, "Body", CreameryArt.ConeBox, SortOrder.Enemies - 1);
        }

        protected override void TickAlive(float dt)
        {
            Vel.x = 0f;
            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveWithTiles(dt);
        }

        protected override void TickDying(float dt)
        {
            if (!m_Broken)
            {
                m_Broken = true;
                World.Burst(Center, 14, Wafer);
                Sfx.Play(Sfx.Thump, 0.6f);
                World.Spawner.SpawnPickup(Creamery.ConeBoxDrop(World.Rng), Pos); // null on a follower: it comes in the snapshot
            }
            Remove();
        }

        public override void Render()
        {
            base.Render();
            m_Sr.sprite = Flashed(CreameryArt.ConeBox);
            m_Sr.enabled = !Dying;
        }
    }
}
