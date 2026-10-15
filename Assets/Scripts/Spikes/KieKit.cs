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
            if (Pos.y < -2f || !WeaselPopSpec.OnScreen(Pos.x, World.View.Left, World.View.Right))
            {
                // off the screen: gone, without a pop
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
                if (!WeaselPopSpec.OnScreen(e.Pos.x, World.View.Left, World.View.Right)) continue; // nothing ahead of the screen
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
    /// Spike (<c>?spike=kie-trollogram</c>): kie's special for a player. A tap of SP drops a hologram of him where he
    /// stands (a charge, up to three at once); the goons shoot them instead of him while they're on screen. Holding SP
    /// swaps him with the one furthest away (free). The first thing that hits one blows it up on every goon nearby
    /// (<see cref="TrollogramSpec"/>).
    /// </summary>
    public sealed class Trollogram
    {
        readonly Player m_Player;
        float m_SinceSwap = 99f, m_Held = -1f;

        public Trollogram(Player player) => m_Player = player;

        /// <param name="pressed">SP was pressed this frame (as a special, not a tank call-in).</param>
        /// <param name="held">SP is down.</param>
        public void Tick(float dt, bool pressed, bool held)
        {
            m_SinceSwap += dt;
            TrollogramSpec spec = KieTuning.Trollogram;
            Player p = m_Player;
            int up = Up(p.World);
            if (pressed)
            {
                TrollAction a = spec.OnPress(up, p.Specials);
                if (a == TrollAction.Wait) m_Held = 0f;
                else Do(a);
                return;
            }
            if (m_Held < 0f) return;
            if (!held)
            {
                m_Held = -1f;
                Do(spec.OnTap(up, p.Specials, m_SinceSwap));
                return;
            }
            m_Held += dt;
            if (spec.HoldSwaps(m_Held))
            {
                m_Held = -1f;
                if (up > 0) Do(TrollAction.Swap);
            }
        }

        static int Up(World w)
        {
            int n = 0;
            foreach (TrollDecoy d in w.Decoys)
                if (d.Up) n++;
            return n;
        }

        void Do(TrollAction action)
        {
            Player p = m_Player;
            World w = p.World;
            switch (action)
            {
                case TrollAction.Drop:
                    p.Specials--;
                    var decoy = new TrollDecoy(w, p.Pos, p.Pose, p.Def);
                    w.Decoys.Add(decoy);
                    w.Props.Add(decoy);
                    Sfx.Play(Sfx.TrollDrop, 0.8f);
                    w.Scoring.Popup("TROLLOGRAM!", p.Pos + Vector2.up * 2.2f, TrollDecoy.Tint);
                    break;
                case TrollAction.Swap:
                    TrollDecoy far = null;
                    foreach (TrollDecoy d in w.Decoys)
                        if (d.Up && (far == null || Mathf.Abs(d.Pos.x - p.Pos.x) > Mathf.Abs(far.Pos.x - p.Pos.x))) far = d;
                    if (far == null) return;
                    m_SinceSwap = 0f;
                    Vector2 was = p.Pos;
                    p.Pos = far.Pos;
                    p.Vel = Vector2.zero;
                    p.Shield(KieTuning.Trollogram.SwapShield);
                    far.SwapTo(was, p.Pose);
                    w.Burst(p.Center, 10, TrollDecoy.Tint);
                    w.Burst(far.Center, 10, TrollDecoy.Tint);
                    Sfx.Play(Sfx.TrollSwap, 0.8f);
                    break;
            }
        }
    }

    /// <summary>
    /// A hologram of kie on the players' side (Trollogram): it stands in his pose, shimmering, and draws the goons'
    /// fire while it's on screen (several can be up) (<see cref="Combat.NearestAvatar"/>). The first hit blows it up on every goon nearby;
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
            World.Decoys.Remove(this);
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

namespace PooketSlug
{
    /// <summary>
    /// Spike (<c>?spike=kie-steal</c>): kie's super for a player. SU spins a roulette over the toughest enemy on screen,
    /// and kie lifts a weapon off it: a burst of a goon's own weapon, or a boss tank's whole cannon (<see cref="StealSpec"/>).
    /// The loot streams off it into kie and takes his primary's place until its ammo runs out.
    /// </summary>
    public sealed class StealRoulette : Entity
    {
        static readonly Color Pink = new Color(1f, 0.45f, 0.8f), Gold = new Color(1f, 0.85f, 0.3f);
        static readonly StealPrize[] Faces = { StealPrize.Rifle, StealPrize.Knives, StealPrize.Bombs, StealPrize.Cannon };

        readonly Player m_Player;
        readonly Enemy m_Mark;
        readonly StealPrize m_Prize;
        readonly SpriteRenderer m_Frame, m_Icon;
        float m_Time;

        StealRoulette(World world, Player player, Enemy mark) : base(world, "Steal", mark.Center)
        {
            m_Player = player;
            m_Mark = mark;
            m_Prize = StealSpec.PrizeFor(mark.Def.Brain);
            m_Frame = AddRenderer(Go.transform, "Roulette", KieArt.Frame, SortOrder.Effects);
            m_Icon = AddRenderer(Go.transform, "Prize", Icon(Faces[0]), SortOrder.Effects + 1);
        }

        /// <summary>kie pressed SU: a roulette over the toughest enemy on screen, or false with nobody there to rob.</summary>
        public static bool Start(World w, Player p)
        {
            var marks = new System.Collections.Generic.List<Enemy>();
            foreach (Enemy e in w.Enemies)
            {
                if (!e.Shootable || !StealSpec.Robbable(e.Def.Brain)) continue;
                if (e is TankBoss t && (t.Decoy || t.Entering)) continue;
                if (e.Pos.x < w.View.Left || e.Pos.x > w.View.Right) continue;
                marks.Add(e);
            }
            var hp = new int[marks.Count];
            var dist = new float[marks.Count];
            for (int i = 0; i < marks.Count; i++)
            {
                hp[i] = marks[i].MaxHp;
                dist[i] = Mathf.Abs(marks[i].Pos.x - p.Pos.x);
            }
            int pick = StealSpec.Toughest(hp, dist);
            if (pick < 0)
            {
                w.Scoring.Popup("NOBODY TO ROB!", p.Pos + Vector2.up * 2.2f, new Color(0.8f, 0.8f, 0.85f));
                return false;
            }
            w.Props.Add(new StealRoulette(w, p, marks[pick]));
            w.Scoring.Popup("STEAL!", p.Pos + Vector2.up * 2.2f, Pink);
            Sfx.Play(Sfx.Servo, 0.6f);
            return true;
        }

        static Sprite Icon(StealPrize prize)
        {
            switch (prize)
            {
                case StealPrize.Knives: return Art.ProjectileSprite(Content.Knife);
                case StealPrize.Bombs: return Art.Grenade;
                case StealPrize.Cannon: return Art.Shell;
                default: return Art.EnemyBullet;
            }
        }

        public override void Tick(float dt)
        {
            float spin = KieTuning.Steal.SpinTime;
            float before = m_Time;
            m_Time += dt;
            if (!m_Mark.Removed) Pos = new Vector2(m_Mark.Pos.x, m_Mark.Pos.y + m_Mark.Height + 1.2f);
            // ticks as it spins, slowing down
            if ((int)(Mathf.Sqrt(before / spin) * 12f) != (int)(Mathf.Sqrt(m_Time / spin) * 12f)) Sfx.Play(Sfx.Servo, 0.5f);
            if (m_Time >= spin) Lift();
        }

        void Lift()
        {
            Remove();
            Player p = m_Player;
            WeaponDef loot = Content.StolenWeapon(m_Prize);
            if (loot == null || p.Dead) return;
            p.GiveGun(loot);
            World.Scoring.Popup(loot.Hud + " STOLEN!", p.Pos + Vector2.up * 2.4f, Gold);
            Sfx.Play(Sfx.Pickup, 0.8f);
            Sfx.Play(Sfx.Glitch, 0.4f);
            // the loot streams off the mark into kie
            Vector2 to = p.Center;
            for (int i = 0; i < 16; i++)
            {
                Vector2 from = Pos + new Vector2(Rng.Fx.Range(-0.3f, 0.3f), Rng.Fx.Range(-0.3f, 0.3f));
                float t = Rng.Fx.Range(0.3f, 0.5f);
                var vel = (to - from) / t + Vector2.up * Rng.Fx.Range(-1f, 3f);
                World.SpawnParticle(from, vel, i % 2 == 0 ? Pink : Gold, Rng.Fx.Range(1.5f, 2.5f), t);
            }
        }

        public override void Render()
        {
            base.Render();
            float t = m_Time / KieTuning.Steal.SpinTime;
            // flicks through the prizes, slowing, and settles on the real one for the last part
            StealPrize face = t > 0.75f ? m_Prize : Faces[(int)(Mathf.Sqrt(t) * 12f) % Faces.Length];
            m_Icon.sprite = Icon(face);
            m_Frame.color = t > 0.75f && (int)(m_Time * 16f) % 2 == 0 ? Color.white : Pink;
        }
    }
}
