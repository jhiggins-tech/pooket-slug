using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Mission one's hazards (<c>docs/design/levels.md</c>, Building a map): a live fence, steam vents and
    /// icicles (the lift, the checkpoint and the explosive barrel have their own files). They run on every client and hurt only that client's own soldier, the way each client already
    /// owns its soldier's damage, so nothing about them goes over the network. Their timing comes from
    /// <see cref="World.Elapsed"/>, so they pulse together closely enough on each phone.
    /// </summary>
    public abstract class Hazard : Entity
    {
        protected Hazard(World world, string name, Vector2 feet) : base(world, name, feet) { }

        /// <summary>Hurts our soldier if it's inside <paramref name="area"/> (the soldier's own i-frames space hits out).</summary>
        protected void HurtInside(Rect area, int damage)
        {
            Player p = World.Player;
            if (!p.Dead && area.Overlaps(p.Bounds)) p.Hurt(damage);
        }
    }

    /// <summary>A fence that switches on and off, sparking while it's live. Touch it live and it hurts.</summary>
    public sealed class LiveFence : Hazard
    {
        readonly SpriteRenderer m_Sr;

        public LiveFence(World world, Vector2 feet) : base(world, "LiveFence", feet)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", Art.LiveFence[0], SortOrder.Pickups);
        }

        bool Live => BaseHazards.Fence.On(World.Elapsed, Pos.x);

        public override void Tick(float dt)
        {
            if (Live) HurtInside(new Rect(Pos.x - 0.75f, Pos.y, 1.5f, 1.5f), 1);
        }

        public override void Render()
        {
            base.Render();
            m_Sr.sprite = Live ? Art.LiveFence[1 + Fx.Step(World.Elapsed, 12f, 2)] : Art.LiveFence[0];
        }
    }

    /// <summary>A grate in the ground that blasts a column of steam every few seconds.</summary>
    public sealed class SteamVent : Hazard
    {
        public SteamVent(World world, Vector2 feet) : base(world, "SteamVent", feet)
        {
            AddRenderer(Go.transform, "Sprite", Art.SteamVent, SortOrder.Pickups);
        }

        public override void Tick(float dt)
        {
            if (!BaseHazards.Steam.On(World.Elapsed, Pos.x)) return;
            HurtInside(new Rect(Pos.x - 0.4f, Pos.y, 0.8f, 2.6f), 1);
            if (Rng.Fx.Range(0f, 1f) < 0.6f) // the steam itself is cosmetic
            {
                var vel = new Vector2(Rng.Fx.Range(-0.6f, 0.6f), Rng.Fx.Range(5f, 8f));
                World.SpawnParticle(Pos + new Vector2(Rng.Fx.Range(-0.3f, 0.3f), 0.2f), vel, new Color(0.9f, 0.93f, 0.97f, 0.7f),
                    Rng.Fx.Range(3f, 5f), Rng.Fx.Range(0.3f, 0.5f));
            }
        }
    }

    /// <summary>
    /// Icicles hanging under a catwalk. When a soldier walks underneath they shake, drop, and shatter on the
    /// ground; they hurt our soldier on the way down.
    /// </summary>
    public sealed class Icicles : Hazard
    {
        const float Shake = 0.35f, Gravity = 30f;
        float m_Fuse = -1f, m_Vel;
        bool m_Falling;

        public Icicles(World world, Vector2 cell) : base(world, "Icicles", new Vector2(cell.x, cell.y + 1f))
        {
            AddRenderer(Go.transform, "Sprite", Art.Icicles, SortOrder.Pickups);
        }

        public override void Tick(float dt)
        {
            if (!m_Falling)
            {
                if (m_Fuse < 0f && SoldierBelow()) m_Fuse = Shake;
                if (m_Fuse < 0f) return;
                m_Fuse -= dt;
                if (m_Fuse <= 0f) m_Falling = true;
                return;
            }
            m_Vel -= Gravity * dt;
            Pos.y += m_Vel * dt;
            HurtInside(new Rect(Pos.x - 0.55f, Pos.y - 0.85f, 1.1f, 0.85f), 1);
            if (World.Level.IsSolidAt(Pos + Vector2.down * 0.85f) || Pos.y < 0f)
            {
                World.Burst(Pos + Vector2.down * 0.6f, 10, new Color(0.85f, 0.9f, 0.98f));
                Sfx.Play(Sfx.Hit, 0.6f);
                Remove();
            }
        }

        bool SoldierBelow()
        {
            if (Near(World.Player)) return true;
            foreach (RemotePlayer r in World.Remotes)
                if (Near(r)) return true;
            return false;
        }

        bool Near(Actor a) => !a.IsDown && Mathf.Abs(a.Pos.x - Pos.x) < 1.6f && a.Pos.y < Pos.y;

        public override void Render()
        {
            base.Render();
            if (m_Fuse > 0f && !m_Falling)
                Go.transform.localPosition += new Vector3(Fx.Shiver(m_Fuse, 40f, 1f) / Art.PPU, 0f, 0f);
        }
    }
}
