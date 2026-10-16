using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The coda's pieces (<c>docs/design/story.md</c>, The coda): the motor pool's back gate that grinds open once kie's
    /// freed, the watchtower that stands in the road until it's shot down, the base's last gate, and kie asleep
    /// against the wall. Mission one is single player only, so none of them is shared over the network.
    /// </summary>
    static class CodaArt
    {
        static Sprite s_Gate, s_Exit, s_Rubble;

        static readonly Color32 Steel = Art.Hex(0x5a6478), SteelDark = Art.Hex(0x2e3444), SteelLight = Art.Hex(0x8a96aa);
        static readonly Color32 Hazard = Art.Hex(0xf0c030), HazardDark = Art.Hex(0x24242c), Red = Art.Hex(0xd83838);
        static readonly Color32 Snow = Art.Hex(0xf4f8ff), Ink = Art.OutlineColor;

        /// <summary>The back gate: a narrow blast shutter, hazard-striped, 1.5 tiles wide and 4 tall.</summary>
        public static Sprite BackGate => s_Gate != null ? s_Gate : s_Gate = Shutter(24, 64, false);

        /// <summary>The last gate: a heavier shutter with EXIT lamps, 2 tiles wide and 4 tall.</summary>
        public static Sprite ExitGate => s_Exit != null ? s_Exit : s_Exit = Shutter(32, 64, true);

        /// <summary>What's left of a smashed gate: a heap of bent slats.</summary>
        public static Sprite Rubble
        {
            get
            {
                if (s_Rubble != null) return s_Rubble;
                var c = new PixelCanvas(34, 12);
                for (int i = 0; i < 6; i++)
                {
                    int x = 1 + i * 5, y = 1 + (i * 7) % 4;
                    c.Rect(x, y, 9, 3, i % 2 == 0 ? Steel : SteelDark);
                    c.Rect(x, y + 2, 9, 1, SteelLight);
                }
                c.Rect(4, 7, 6, 2, Hazard);
                c.Rect(20, 6, 6, 2, Hazard);
                return s_Rubble = Art.ToSprite(c, new Vector2(0.5f, 0f));
            }
        }

        static Sprite Shutter(int w, int h, bool exit)
        {
            var c = new PixelCanvas(w, h);
            c.Rect(0, 0, w, h, Ink);
            c.Rect(1, 1, w - 2, h - 2, Steel);
            for (int y = 3; y < h - 6; y += 4)
            {
                c.Rect(2, y, w - 4, 1, SteelLight);
                c.Rect(2, y + 2, w - 4, 1, SteelDark);
            }
            for (int x = 1; x < w - 1; x++)
                for (int y = 1; y < 6; y++)
                    c.Set(x, y, ((x + y) / 3) % 2 == 0 ? Hazard : HazardDark); // a hazard-striped kick plate
            c.Rect(1, h - 6, w - 2, 5, SteelDark);
            c.Rect(1, h - 2, w - 2, 1, Snow);
            if (exit)
            {
                c.Rect(4, h - 5, 4, 3, Red);
                c.Rect(w - 8, h - 5, 4, 3, Red);
                c.Rect(w / 2 - 6, h / 2, 12, 5, HazardDark);
                c.Rect(w / 2 - 5, h / 2 + 1, 10, 3, Hazard); // EXIT, at this size a bright plate
            }
            return Art.ToSprite(c, new Vector2(0.5f, 0f), withFlash: true);
        }
    }

    /// <summary>
    /// The motor pool's back gate, at the right of the boss's arena. Shut (a wall, <see cref="Level.ScrollLimit"/>)
    /// until kie's freed; then it grinds up, juddering, and the coda starts.
    /// </summary>
    public sealed class BackGate : Entity
    {
        const float OpenTime = 1.6f;
        readonly SpriteRenderer m_Sr;
        float m_Open = -1f;

        public BackGate(World world, Vector2 feet) : base(world, "Back gate", feet)
        {
            m_Sr = AddRenderer(Go.transform, "Shutter", CodaArt.BackGate, SortOrder.Scenery + 1);
            if (world.CodaOpen) m_Open = OpenTime; // a restart at the coda: it's already up
        }

        public bool Opening => m_Open >= 0f && m_Open < OpenTime;

        public void Open()
        {
            if (m_Open >= 0f) return;
            m_Open = 0f;
            Sfx.Play(Sfx.Thump);
            World.View.Shake(0.2f);
        }

        public override void Tick(float dt)
        {
            if (m_Open < 0f || m_Open >= OpenTime) return;
            m_Open += dt;
            if ((int)(m_Open * 6f) != (int)((m_Open - dt) * 6f)) World.View.Shake(0.06f); // it judders up
        }

        public override void Render()
        {
            base.Render();
            // it rolls up into the wall above: squashed from the bottom up
            float k = m_Open < 0f ? 0f : Mathf.Clamp01(m_Open / OpenTime);
            float judder = k > 0f && k < 1f ? ((int)(m_Open * 14f) % 2) * 0.04f : 0f;
            float left = Mathf.Max(0.04f, 1f - k + judder);
            Transform t = m_Sr.transform;
            t.localScale = new Vector3(1f, left, 1f);
            t.localPosition = new Vector3(0f, 4f * (1f - left), 0f);
        }
    }

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

    /// <summary>kie, asleep against the motor pool's wall with the snacks, for the rest of Mission one.</summary>
    public sealed class SleepingKie : Entity
    {
        readonly PlayerVisual m_Visual;
        readonly Transform m_Body;
        float m_Z;

        public SleepingKie(World world, Vector2 feet) : base(world, "kie, asleep", feet)
        {
            m_Body = new GameObject("Body").transform;
            m_Body.SetParent(Go.transform, false);
            m_Visual = new PlayerVisual(m_Body, Art.SkinFor(Content.Kie, 0), SortOrder.Scenery + 2);
            m_Body.localScale = new Vector3(-1f, 1f, 1f); // facing back the way tones2 came
        }

        public override void Tick(float dt)
        {
            m_Z += dt;
            if (m_Z < 1.4f) return;
            m_Z = 0f;
            World.Scoring.Popup("Z", Pos + new Vector2(-0.3f, 1.4f), new Color(0.8f, 0.9f, 1f));
            if (Pos.x < World.View.Left - 4f) Remove();
        }

        public override void Render()
        {
            base.Render();
            m_Visual.Apply(new AvatarPose { Facing = 1, Grounded = true, Crouching = true });
        }
    }
}
