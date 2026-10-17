using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// An explosive barrel. It's an enemy as far as the game is concerned (shot, replicated and scored like
    /// one, never a hologram), but it just stands there. Shoot it and it blows up, hurting soldiers and goons
    /// alike and setting off other barrels. Each client's blast hurts its own soldier; the authority's hurts goons.
    /// </summary>
    public sealed class Barrel : Enemy
    {
        const float BlastRadius = 1.8f;
        const int BlastDamage = 2, GoonDamage = 6;
        readonly SpriteRenderer m_Sr;
        bool m_Blown;

        public Barrel(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Sr = AddRenderer(Visual, "Body", Art.Barrel, SortOrder.Enemies - 1);
        }

        protected override void TickAlive(float dt)
        {
            Vel.x = 0f;
            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveWithTiles(dt);
        }

        protected override void TickDying(float dt)
        {
            if (!m_Blown)
            {
                m_Blown = true;
                Vector2 at = Center;
                World.Combat.Explode(at, BlastRadius, BlastDamage, true, 1.4f);
                if (World.IsAuthority)
                    foreach (Enemy e in World.Enemies)
                        if (e != this && e.Shootable && Vector2.Distance(e.Center, at) <= BlastRadius + e.HalfW) e.Hurt(GoonDamage);
            }
            Remove();
        }

        public override void Render()
        {
            base.Render();
            m_Sr.sprite = Flashed(Art.Barrel);
            m_Sr.enabled = !Dying;
        }
    }
}
