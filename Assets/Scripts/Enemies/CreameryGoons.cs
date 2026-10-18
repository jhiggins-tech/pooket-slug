using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Shield soldier (Mission two): plods at the nearest soldier behind a riot shield that stops shots from the front
    /// (<see cref="ShieldSpec.Blocks"/>), and fires past its edge. He's slow to turn, so jump over him or get behind him;
    /// blasts, mud and weasels under the shield get him too.
    /// </summary>
    public sealed class ShieldSoldier : Enemy
    {
        public override bool Freezable => true;
        readonly SpriteRenderer m_Legs, m_Torso, m_Shield, m_Flash;
        float m_Cooldown, m_Turn, m_Muzzle;

        public ShieldSoldier(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Legs = AddRenderer(Visual, "Legs", Art.SoldierLegsIdle, SortOrder.Enemies);
            m_Torso = AddRenderer(Visual, "Torso", Art.SoldierTorso, SortOrder.Enemies + 1);
            m_Shield = AddRenderer(Visual, "Shield", CreameryArt.Shield, SortOrder.Enemies + 2);
            m_Shield.transform.localPosition = new Vector3(6f / Art.PPU, 0f, 0f);
            m_Flash = AddRenderer(Visual, "Muzzle", Art.MuzzleFlash, SortOrder.Enemies + 3);
            m_Flash.transform.localPosition = new Vector3(12f / Art.PPU, 17.5f / Art.PPU, 0f);
            m_Flash.enabled = false;
            m_Cooldown = World.Rng.Range(1f, 2f);
            Facing = -1; // they come at you from the right
        }

        /// <summary>A shot moving at <paramref name="vel"/> that touched him at <paramref name="at"/> stops on his shield.</summary>
        public bool Blocks(Vector2 at, Vector2 vel) => !Dying && ShieldSpec.Blocks(Facing, Pos, at, vel);

        protected override void TickAlive(float dt)
        {
            int want = Target.Pos.x >= Pos.x ? 1 : -1;
            if (want != Facing && !Target.IsDown)
            {
                m_Turn += dt; // the shield's heavy: he's slow to swing round
                if (m_Turn >= ShieldSpec.TurnDelay)
                {
                    Facing = want;
                    m_Turn = 0f;
                }
            }
            else m_Turn = 0f;

            float dist = Mathf.Abs(Target.Pos.x - Pos.x);
            Vel.x = dist > ShieldSpec.StopAt && want == Facing && !Target.IsDown ? Facing * ShieldSpec.WalkSpeed : 0f;
            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveResult r = MoveWithTiles(dt);
            if (r.HitWall && Grounded) Vel.y = 10f; // hops a crate

            m_Cooldown -= dt;
            m_Muzzle -= dt;
            bool onScreen = Pos.x < World.View.Right - 0.5f;
            if (onScreen && dist < 9f && want == Facing && m_Cooldown <= 0f && !Target.IsDown)
            {
                var origin = Pos + new Vector2(Facing * 0.8f, 1.1f);
                World.Combat.FireEnemyShot(Content.Rifle, origin, new Vector2(Facing * Content.Rifle.Speed, 0f));
                m_Muzzle = 0.06f;
                m_Cooldown = ShieldSpec.FireEvery;
            }
        }

        public override void Render()
        {
            base.Render();
            bool walking = Mathf.Abs(Vel.x) > 0.1f && Grounded && !Dying;
            m_Legs.sprite = Flashed(walking ? Art.SoldierLegsRun[(int)(Anim * 6f) % 4] : Art.SoldierLegsIdle);
            m_Torso.sprite = Flashed(Art.SoldierTorso);
            m_Shield.sprite = Flashed(CreameryArt.Shield);
            m_Flash.enabled = m_Muzzle > 0f && !Dying;
        }
    }

    /// <summary>
    /// Grenadier (Mission two): stays put behind his cover and lobs grenades that land where a soldier's standing,
    /// after a wind-up you can see (<see cref="GrenadierSpec"/>). Keep moving.
    /// </summary>
    public sealed class Grenadier : Enemy
    {
        public override bool Freezable => true;
        const int WindingFlag = 32; // a follower's copy winds up too
        readonly SpriteRenderer m_Legs, m_Torso, m_Grenade;
        float m_Cooldown, m_Wind = -1f;
        bool m_NetWinding, m_Pinned;

        public Grenadier(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Legs = AddRenderer(Visual, "Legs", Art.SoldierLegsIdle, SortOrder.Enemies);
            m_Torso = AddRenderer(Visual, "Torso", Art.SoldierTorso, SortOrder.Enemies + 1);
            m_Grenade = AddRenderer(Visual, "Grenade", Art.ProjectileSprite(Content.GoonGrenade), SortOrder.Enemies + 2);
            m_Grenade.transform.localPosition = new Vector3(-3f / Art.PPU, 24f / Art.PPU, 0f);
            m_Cooldown = World.Rng.Range(0.8f, 1.8f);
            Facing = -1;
        }

        bool Winding => World.IsAuthority ? m_Wind >= 0f : m_NetWinding;

        protected override int ExtraFlags => m_Wind >= 0f ? WindingFlag : 0;
        protected override void OnSnapshot(int flags) => m_NetWinding = (flags & WindingFlag) != 0;

        protected override void TickAlive(float dt)
        {
            FaceTarget();
            Vel.x = 0f;
            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveWithTiles(dt);

            float dx = Target.Pos.x - Pos.x;
            bool onScreen = Pos.x < World.View.Right - 0.5f && Pos.x > World.View.Left + 0.5f;
            if (m_Wind >= 0f)
            {
                m_Wind += dt;
                if (m_Wind < GrenadierSpec.WindUp) return;
                m_Wind = -1f;
                if (Target.IsDown) return;
                // it lands where they're standing now: move
                Vector2 from = Pos + new Vector2(Facing * 0.2f, 1.5f);
                Vector2 to = new Vector2(Target.Pos.x, World.Level.TopSurfaceY(Target.Pos.x));
                Vector2 v = GrenadierSpec.LobTo(from, to, Content.GoonGrenade.Gravity, GrenadierSpec.Flight);
                World.Combat.FireEnemyShot(Content.GoonGrenadeThrow, from, v);
                m_Cooldown = GrenadierSpec.ThrowEvery * World.Rng.Range(0.85f, 1.15f);
                return;
            }
            m_Cooldown -= dt;
            if (onScreen && m_Cooldown <= 0f && GrenadierSpec.InRange(dx) && !Target.IsDown) m_Wind = 0f;
        }

        public override void Render()
        {
            base.Render();
            m_Legs.sprite = Flashed(Art.SoldierLegsIdle);
            m_Torso.sprite = Flashed(Art.SoldierTorso);
            bool winding = Winding && !Dying;
            if (winding && !m_Pinned) Sfx.Play(Sfx.GrenadePin, 0.6f); // the pin pulled: the tell's sound
            m_Pinned = winding;
            m_Grenade.enabled = winding; // held up over his head: the tell
            m_Torso.transform.localPosition = winding ? new Vector3(-1f / Art.PPU, 1f / Art.PPU, 0f) : Vector3.zero;
        }
    }
}
