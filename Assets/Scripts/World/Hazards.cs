using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Mission one's hazards (<c>docs/design/levels.md</c>, Building a map): a live fence, steam vents and
    /// icicles. (The lift, the checkpoint and the explosive barrel are further down.) They run on every client and hurt only that client's own soldier, the way each client already
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
        const float Cycle = 3f, LiveFor = 1.4f;
        readonly SpriteRenderer m_Sr;
        readonly float m_Offset;

        public LiveFence(World world, Vector2 feet) : base(world, "LiveFence", feet)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", Art.LiveFence[0], SortOrder.Pickups);
            m_Offset = feet.x * 0.37f % Cycle;
        }

        bool Live => (World.Elapsed + m_Offset) % Cycle < LiveFor;

        public override void Tick(float dt)
        {
            if (Live) HurtInside(new Rect(Pos.x - 0.75f, Pos.y, 1.5f, 1.5f), 1);
        }

        public override void Render()
        {
            base.Render();
            m_Sr.sprite = Live ? Art.LiveFence[1 + (int)(World.Elapsed * 12f) % 2] : Art.LiveFence[0];
        }
    }

    /// <summary>A grate in the ground that blasts a column of steam every few seconds.</summary>
    public sealed class SteamVent : Hazard
    {
        const float Cycle = 2.6f, VentFor = 1f;
        readonly float m_Offset;

        public SteamVent(World world, Vector2 feet) : base(world, "SteamVent", feet)
        {
            AddRenderer(Go.transform, "Sprite", Art.SteamVent, SortOrder.Pickups);
            m_Offset = feet.x * 0.53f % Cycle;
        }

        public override void Tick(float dt)
        {
            if ((World.Elapsed + m_Offset) % Cycle >= VentFor) return;
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
                Go.transform.localPosition += new Vector3(((int)(m_Fuse * 40f) % 2 == 0 ? 1f : -1f) / Art.PPU, 0f, 0f);
        }
    }

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

    /// <summary>
    /// A lift: a steel platform that rides up and down its rails on a fixed timetable, so it's in the same place
    /// on every client. Our soldier can stand on it (each client carries its own soldier).
    /// </summary>
    public sealed class Lift : Entity
    {
        /// <summary>How far it rises, in tiles.</summary>
        public const int Rise = 5;
        const float Speed = 2f, Pause = 1.2f, HalfWidth = 1f;
        readonly float m_Bottom;

        /// <summary><paramref name="feet"/> is its marker: the left of its two tiles, at the bottom of its run.</summary>
        public Lift(World world, Vector2 feet) : base(world, "Lift", new Vector2(feet.x + 0.5f, feet.y))
        {
            m_Bottom = feet.y;
            Pos.y = HeightAt(world.Elapsed);
            AddRenderer(Go.transform, "Car", Art.LiftCar, SortOrder.Pickups);
        }

        float HeightAt(float time)
        {
            float travel = Rise / Speed, t = time % (2f * (travel + Pause));
            if (t < Pause) return m_Bottom;
            t -= Pause;
            if (t < travel) return m_Bottom + t * Speed;
            t -= travel;
            if (t < Pause) return m_Bottom + Rise;
            t -= Pause;
            return m_Bottom + Rise - t * Speed;
        }

        public override void Tick(float dt)
        {
            // Our soldier has just moved (soldiers tick first). One standing on the car, or landing on it, rides it.
            Player p = World.Player;
            bool riding = !p.Dead && Mathf.Abs(p.Pos.x - Pos.x) < HalfWidth && p.Vel.y <= 0.01f &&
                          p.Pos.y <= Pos.y + 0.05f && p.Pos.y >= Pos.y - 0.45f;
            Pos.y = HeightAt(World.Elapsed);
            if (!riding) return;
            p.Pos.y = Pos.y;
            p.Vel.y = 0f;
            p.Grounded = true;
        }
    }

    /// <summary>
    /// The checkpoint at the boss door: our soldier passing it lights it, and losing after that restarts a solo
    /// game here rather than at the start of the run (<c>docs/design/levels.md</c>, Missions).
    /// </summary>
    public sealed class Checkpoint : Entity
    {
        readonly SpriteRenderer m_Sr;
        bool m_Lit;

        public Checkpoint(World world, Vector2 feet) : base(world, "Checkpoint", feet)
        {
            m_Sr = AddRenderer(Go.transform, "Sprite", Art.Checkpoint[0], SortOrder.Pickups);
        }

        public override void Tick(float dt)
        {
            if (m_Lit) return;
            if (World.CheckpointX >= Pos.x)
            {
                m_Lit = true; // restarted here: already lit
                return;
            }
            Player p = World.Player;
            if (p.Dead || p.Pos.x < Pos.x) return;
            m_Lit = true;
            World.CheckpointX = Pos.x;
            World.Scoring.Popup("CHECKPOINT!", Pos + Vector2.up * 2.8f, new Color(0.45f, 1f, 0.6f));
            Sfx.Play(Sfx.Pickup);
        }

        public override void Render()
        {
            base.Render();
            m_Sr.sprite = Art.Checkpoint[m_Lit ? 1 : 0];
        }
    }
}
