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
