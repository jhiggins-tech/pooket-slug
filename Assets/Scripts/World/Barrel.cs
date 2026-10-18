using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// An explosive barrel. It's an enemy as far as the game is concerned (shot, replicated and scored like
    /// one, never a hologram), but it just stands there. Shoot it and it blows up, hurting soldiers and goons
    /// alike and setting off other barrels. Each client's blast hurts its own soldier; the authority's hurts goons.
    /// Mission two's nitrogen tank is one too (<see cref="EnemyBrain.NitrogenTank"/>): a smaller bang, and a wide freeze
    /// over soldiers and goons alike (<see cref="Creamery"/>).
    /// </summary>
    public sealed class Barrel : Enemy
    {
        const float BlastRadius = 1.8f;
        const int BlastDamage = 2, GoonDamage = 6;
        static readonly Color Mist = new Color(0.85f, 0.95f, 1f);
        readonly SpriteRenderer m_Sr;
        readonly Sprite m_Look;
        readonly bool m_Nitrogen;
        bool m_Blown;

        public Barrel(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Nitrogen = def.Brain == EnemyBrain.NitrogenTank;
            m_Look = m_Nitrogen ? CreameryArt.NitrogenTank : Art.Barrel;
            m_Sr = AddRenderer(Visual, "Body", m_Look, SortOrder.Enemies - 1);
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
                if (m_Nitrogen)
                {
                    FreezeBlast(at);
                    return;
                }
                World.Combat.Explode(at, BlastRadius, BlastDamage, true, 1.4f);
                if (World.IsAuthority)
                    foreach (Enemy e in World.Enemies)
                        if (e != this && e.Shootable && Vector2.Distance(e.Center, at) <= BlastRadius + e.HalfW) e.Hurt(GoonDamage);
            }
            Remove();
        }

        /// <summary>The nitrogen tank's blast: everyone near it frozen first (our soldier here, the goons by the authority), then a little hurt.</summary>
        void FreezeBlast(Vector2 at)
        {
            float r = Creamery.TankRadius;
            Player p = World.Player;
            if (!p.Dead && Vector2.Distance(p.Center, at) <= r + p.HalfW) p.Freeze(Creamery.TankSoldierFreeze);
            World.Combat.Explode(at, BlastRadius * 0.7f, Creamery.TankDamage, true, 1.1f);
            World.Burst(at, 26, Mist);
            for (int i = 0; i < 24; i++) // a ring of cold mist
            {
                float a = i * Mathf.PI * 2f / 24f;
                World.SpawnParticle(at, new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * r * 2.4f, Mist, Rng.Fx.Range(4f, 6f), 0.45f);
            }
            Sfx.Play(Sfx.Sizzle, 0.8f);
            if (World.IsAuthority)
                foreach (Enemy e in World.Enemies)
                {
                    if (e == this || !e.Shootable || Vector2.Distance(e.Center, at) > r + e.HalfW) continue;
                    e.Freeze(Creamery.TankGoonFreeze);
                    e.Hurt(Creamery.TankGoonDamage);
                }
            Remove();
        }

        public override void Render()
        {
            base.Render();
            m_Sr.sprite = Flashed(m_Look);
            m_Sr.enabled = !Dying;
        }
    }
}
