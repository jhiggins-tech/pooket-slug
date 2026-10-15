using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike (<c>?spike=kie-weasels</c>, solo only): kie's kit for a player. So far Weasel Pop: each press of FIRE
    /// tumbles out three weasels (<see cref="WeaselPopSpec"/>) that hunt goons along the ground and pop under them.
    /// </summary>
    public static class KieKit
    {
        /// <summary>kie fires Weasel Pop from <paramref name="muzzle"/>: false when too many weasels are already out.</summary>
        public static bool ThrowWeasels(World w, Vector2 muzzle, Vector2 aim, Vector2 carry)
        {
            WeaselPopSpec spec = KieTuning.Weasels;
            if (PopWeasel.Out(w) + spec.Count > spec.MaxOut) return false;
            foreach (Vector2 v in spec.Volley(aim, carry)) w.Props.Add(new PopWeasel(w, muzzle, v));
            Sfx.Play(Sfx.WeaselThrow, 0.7f);
            return true;
        }
    }

    /// <summary>
    /// One of kie's weasels, on the players' side. It tumbles where it's thrown, then scurries under the nearest goon
    /// on the ground in reach and pops; with nothing to hunt it runs on and pops when its fuse runs out. It touches a
    /// goon on the way down: it pops there. Goons can't shoot it. The first pop in a while plays Pop Goes the Weasel.
    /// </summary>
    public sealed class PopWeasel : Actor
    {
        static float s_TuneAt = -99f;
        static World s_TuneWorld;

        readonly SpriteRenderer m_Sr;
        float m_Time, m_Fuse;
        bool m_Landed;

        public PopWeasel(World world, Vector2 at, Vector2 vel) : base(world, "Weasel", at, 0.3f, 0.5f, 1)
        {
            Vel = vel;
            Facing = vel.x >= 0f ? 1 : -1;
            m_Sr = AddRenderer(Visual, "Body", KieArt.Weasel[0], SortOrder.Player - 1);
        }

        /// <summary>How many weasels kie has out in <paramref name="w"/>.</summary>
        public static int Out(World w)
        {
            int n = 0;
            foreach (Entity e in w.Props)
                if (e is PopWeasel && !e.Removed) n++;
            return n;
        }

        public override void Tick(float dt)
        {
            WeaselPopSpec spec = KieTuning.Weasels;
            m_Time += dt;
            if (Pos.y < -2f || Pos.x < World.View.Left - 4f)
            {
                Remove();
                return;
            }

            Enemy prey = Prey(spec);
            if (!m_Landed)
            {
                Vel.y = Mathf.Max(Vel.y - spec.Gravity * dt, -Tuning.Physics.MaxFall);
                MoveResult r = MoveWithTiles(dt);
                if (r.HitWall) Vel.x = 0f;
                if (prey != null && prey.Bounds.Overlaps(Bounds))
                {
                    Pop();
                    return;
                }
                if (Grounded) m_Landed = true;
                else if (m_Time >= spec.AirTime) Pop();
                return;
            }

            m_Fuse += dt;
            if (prey != null)
            {
                float dx = prey.Pos.x - Pos.x;
                if (Mathf.Abs(dx) > 0.05f) Facing = dx > 0f ? 1 : -1;
                if (spec.Under(Pos.x, prey.Pos.x, prey.HalfW) && Mathf.Abs(prey.Pos.y - Pos.y) < 1.2f)
                {
                    Pop();
                    return;
                }
            }
            Vel.x = Facing * spec.ScurrySpeed;
            Vel.y = Mathf.Max(Vel.y - Tuning.Physics.Gravity * dt, -Tuning.Physics.MaxFall);
            MoveResult m = MoveWithTiles(dt);
            if (m.HitWall && Grounded) Vel.y = 10f; // hops up steps
            if (m_Fuse >= spec.Fuse) Pop();
        }

        /// <summary>The nearest goon it will hunt: on the ground, in reach (<see cref="WeaselPopSpec.CanHunt"/>).</summary>
        Enemy Prey(WeaselPopSpec spec)
        {
            Enemy best = null;
            float bestDx = float.MaxValue;
            foreach (Enemy e in World.Enemies)
            {
                if (!e.Shootable || e.Def.Brain == EnemyBrain.Barrel) continue;
                if (!spec.CanHunt(Pos, e.Pos, e.Def.Brain == EnemyBrain.Drone)) continue;
                float dx = Mathf.Abs(e.Pos.x - Pos.x);
                if (dx < bestDx)
                {
                    best = e;
                    bestDx = dx;
                }
            }
            return best;
        }

        void Pop()
        {
            WeaselPopSpec spec = KieTuning.Weasels;
            World.Combat.Explode(Center, spec.BlastRadius, spec.Damage, false, spec.BlastSize);
            World.Burst(Center, 6, new Color(0.69f, 0.44f, 0.22f)); // a puff of fur
            if (s_TuneWorld != World || World.Elapsed - s_TuneAt >= spec.TuneGap)
            {
                s_TuneWorld = World;
                s_TuneAt = World.Elapsed;
                Sfx.Play(Sfx.WeaselTune, 0.8f);
                World.Scoring.Popup("POP!", Center + Vector2.up * 1.2f, new Color(1f, 0.75f, 0.4f));
            }
            Remove();
        }

        public override void Render()
        {
            base.Render();
            bool running = m_Landed && Grounded && Mathf.Abs(Vel.x) > 0.1f;
            m_Sr.sprite = KieArt.Weasel[running && (int)(m_Time * 16f) % 2 == 1 ? 1 : 0];
            // tumbles in the air; blinks as its fuse runs out
            Visual.localRotation = m_Landed ? Quaternion.identity : Quaternion.Euler(0f, 0f, m_Time * -720f * Facing);
            Blink(m_Sr, m_Landed ? KieTuning.Weasels.Fuse - m_Fuse : 1f);
        }

        static void Blink(SpriteRenderer sr, float left) =>
            sr.color = left < 0.6f && (int)(left * 20f) % 2 == 0 ? new Color(1f, 0.6f, 0.6f) : Color.white;
    }
}

namespace PooketSlug
{
    /// <summary>
    /// Spike (<c>?spike=kie-trollogram</c>): kie's special for a player. SP drops a hologram of him where he stands
    /// (a charge); the goons shoot it instead of him while it's on screen, and SP again swaps him and it (free). The
    /// first thing that hits it blows it up on every goon nearby (<see cref="TrollogramSpec"/>).
    /// </summary>
    public sealed class Trollogram
    {
        readonly Player m_Player;
        float m_SinceSwap = 99f;

        public Trollogram(Player player) => m_Player = player;

        public void Tick(float dt, bool pressed)
        {
            m_SinceSwap += dt;
            if (!pressed) return;
            Player p = m_Player;
            World w = p.World;
            TrollDecoy decoy = w.Decoy != null && w.Decoy.Up ? w.Decoy : null;
            switch (KieTuning.Trollogram.OnSpecial(decoy != null, m_SinceSwap, p.Specials))
            {
                case TrollAction.Drop:
                    p.Specials--;
                    w.Decoy = new TrollDecoy(w, p.Pos, p.Pose, p.Def);
                    w.Props.Add(w.Decoy);
                    Sfx.Play(Sfx.TrollDrop, 0.8f);
                    w.Scoring.Popup("TROLLOGRAM!", p.Pos + Vector2.up * 2.2f, TrollDecoy.Tint);
                    break;
                case TrollAction.Swap:
                    m_SinceSwap = 0f;
                    Vector2 was = p.Pos;
                    p.Pos = decoy.Pos;
                    p.Vel = Vector2.zero;
                    p.Shield(KieTuning.Trollogram.SwapShield);
                    decoy.SwapTo(was, p.Pose);
                    w.Burst(p.Center, 10, TrollDecoy.Tint);
                    w.Burst(decoy.Center, 10, TrollDecoy.Tint);
                    Sfx.Play(Sfx.TrollSwap, 0.8f);
                    break;
            }
        }
    }

    /// <summary>
    /// A hologram of kie on the players' side (Trollogram): it stands in his pose, shimmering, and draws the goons'
    /// fire while it's on screen (<see cref="Combat.NearestAvatar"/>). The first hit blows it up on every goon nearby;
    /// left alone, it switches itself off. It glitches out either way.
    /// </summary>
    public sealed class TrollDecoy : Actor
    {
        public static readonly Color Tint = new Color(0.49f, 0.97f, 0.83f); // Tabks' Trollogram colour

        readonly PlayerVisual m_Visual;
        readonly SpriteRenderer[] m_Parts;
        AvatarPose m_Pose;
        float m_Time, m_Gone = -1f;
        Vector2 m_Jitter;

        public TrollDecoy(World world, Vector2 feet, AvatarPose pose, SoldierDef def) : base(world, "Trollogram", feet, def.HalfWidth, def.StandHeight, 1)
        {
            m_Pose = Still(pose);
            Facing = pose.Facing;
            Height = pose.Crouching ? def.CrouchHeight : def.StandHeight;
            m_Visual = new PlayerVisual(Visual, Art.SkinFor(def, 0), SortOrder.Player - 2);
            m_Parts = Visual.GetComponentsInChildren<SpriteRenderer>(true);
        }

        /// <summary>Standing, not yet blown up or switched off.</summary>
        public bool Up => m_Gone < 0f && !Removed;

        /// <summary>Is the pose it holds: frozen mid-whatever, never firing or blinking.</summary>
        static AvatarPose Still(AvatarPose p)
        {
            p.Muzzle = false;
            p.Blink = false;
            p.Flash = false;
            p.Running = false;
            p.Dead = false;
            return p;
        }

        /// <summary>kie swapped with it: it stands where he stood, as he stood.</summary>
        public void SwapTo(Vector2 feet, AvatarPose pose)
        {
            Pos = feet;
            Vel = Vector2.zero;
            m_Pose = Still(pose);
            Facing = pose.Facing;
        }

        /// <summary>Something hostile hit it: it blows up on every goon nearby.</summary>
        public void Blow()
        {
            if (!Up) return;
            TrollogramSpec spec = KieTuning.Trollogram;
            World.Combat.Explode(Center, spec.BlastRadius, spec.Damage, false, spec.BlastSize);
            World.Scoring.Popup("TROLLED!", Center + Vector2.up * 1.4f, Tint);
            GoOut();
        }

        void GoOut()
        {
            m_Gone = 0f;
            Sfx.Play(Sfx.Glitch, 0.6f);
            World.Burst(Center, 12, Tint);
        }

        public override void Tick(float dt)
        {
            m_Time += dt;
            if (m_Gone >= 0f)
            {
                m_Gone += dt;
                if (m_Gone >= 0.45f) Remove();
                return;
            }
            if (m_Time >= KieTuning.Trollogram.Life) GoOut();
            // it stands where it was dropped, falling if the floor wasn't under it (dropped mid-jump)
            Vel.x = 0f;
            Vel.y = Mathf.Max(Vel.y - Tuning.Physics.Gravity * dt, -Tuning.Physics.MaxFall);
            MoveWithTiles(dt);
            if (Pos.y < -2f) Remove();
            if (Rng.Fx.Value < dt * 1.5f) m_Jitter = new Vector2(Rng.Fx.Range(-2, 3), 0f) / Art.PPU; // a glitch now and then
            else if (Rng.Fx.Value < dt * 10f) m_Jitter = Vector2.zero;
        }

        public override void Destroy()
        {
            if (World.Decoy == this) World.Decoy = null;
            base.Destroy();
        }

        public override void Render()
        {
            base.Render();
            m_Pose.Anim = m_Time;
            m_Pose.Grounded = Grounded || m_Pose.Grounded;
            m_Visual.Apply(m_Pose);
            Visual.localPosition = m_Jitter;
            float shimmer = 0.7f + 0.2f * Mathf.Sin(m_Time * 9f) + (Rng.Fx.Value < 0.05f ? -0.3f : 0f);
            Color c = new Color(Tint.r, Tint.g, Tint.b, shimmer);
            if (m_Gone >= 0f)
            {
                // switches off like an old TV: a flash, squashed to a line, then a dot
                float t = m_Gone / 0.45f;
                c = Color.Lerp(Color.white, Tint, t);
                Visual.localScale = new Vector3(Facing * Mathf.Lerp(1f, t < 0.6f ? 1.6f : 0.1f, t), Mathf.Max(0.05f, 1f - t * 1.6f), 1f);
            }
            foreach (SpriteRenderer sr in m_Parts)
            {
                sr.sprite = Art.Holo(sr.sprite);
                sr.color = c;
            }
        }
    }
}
