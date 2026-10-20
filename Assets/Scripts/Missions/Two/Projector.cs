using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A Trollogram projector behind frosted glass (Mission two's story beat): real, not a Trollogram, and it just
    /// stands there throwing its cyan light. Shot out, the goons it casts within reach flicker out, and the ones yet to
    /// come near it never appear (<see cref="Creamery.SwitchedOff"/>). Every screen notes where it stood as it dies, so
    /// an authority that takes over carries on without them.
    /// </summary>
    public sealed class Projector : Enemy
    {
        static readonly Color Beam = new Color(0.45f, 1f, 1f, 0.35f), Sparks = new Color(0.55f, 1f, 1f);
        readonly SpriteRenderer m_Body, m_Glass, m_Glow;
        bool m_Out;

        public Projector(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Body = AddRenderer(Visual, "Body", CreameryArt.Projector, SortOrder.Enemies - 1);
            m_Glow = AddRenderer(Visual, "Glow", CreameryArt.ProjectorGlow, SortOrder.Enemies - 2);
            m_Glow.color = Beam;
            m_Glow.transform.localPosition = new Vector3(0f, 14f / Art.PPU, 0f);
            m_Glass = AddRenderer(Go.transform, "Glass", CreameryArt.FrostedGlass, SortOrder.Enemies);
        }

        protected override void TickAlive(float dt)
        {
            Vel = Vector2.zero; // bolted down: belts don't carry it
        }

        protected override void Die()
        {
            base.Die();
            SwitchOff();
        }

        protected override void TickDying(float dt)
        {
            SwitchOff(); // a follower's comes from the snapshot
            if (DyingTime > 0.6f) Remove();
        }

        /// <summary>It's out: the goons near it flicker out (the authority's to decide), and no more come near it.</summary>
        void SwitchOff()
        {
            if (m_Out) return;
            m_Out = true;
            World.DarkProjectors.Add(Pos.x);
            World.Burst(Center, 18, Sparks);
            Sfx.Play(Sfx.ProjectorSputter, 0.8f);
            World.Scoring.Popup("PROJECTOR DOWN!", Pos + Vector2.up * 2.4f, Sparks);
            if (!World.IsAuthority) return;
            foreach (Enemy e in World.Enemies)
                if (!e.Dying && e != this && Creamery.SwitchedOff(e.Def.Brain, e.Pos.x, World.DarkProjectors)) e.Hurt(e.Hp);
        }

        public override void Render()
        {
            base.Render();
            m_Body.sprite = Flashed(CreameryArt.Projector);
            bool lit = !Dying;
            m_Glow.enabled = lit;
            if (lit) m_Glow.color = new Color(Beam.r, Beam.g, Beam.b, Beam.a * (0.75f + 0.25f * Mathf.Sin(Anim * 9f)));
            else m_Body.enabled = Fx.Blink(DyingTime, 20f); // sputters out
        }
    }
}
