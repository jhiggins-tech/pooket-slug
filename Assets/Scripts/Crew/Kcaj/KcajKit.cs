using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's special, Hyperfixate, on screen (<c>?spike=kcaj</c>, solo only: the soldier's SP becomes it). The rules
    /// are <see cref="HyperfixateSpec"/> and <see cref="Hyperfixation"/>; this finds what the beam touches, draws the
    /// beam and the dimmed screen edges, and burns the target.
    /// </summary>
    public sealed class KcajBeam
    {
        readonly Player m_Player;
        readonly HyperfixateSpec m_Spec;
        readonly Hyperfixation m_Fix;
        readonly SpriteRenderer m_Beam;
        readonly SpriteRenderer[] m_Edges = new SpriteRenderer[16]; // four layers on each side, darker towards the edge
        readonly List<Enemy> m_Seen = new List<Enemy>();
        Vector2[] m_Centres = new Vector2[16], m_Halves = new Vector2[16];
        Enemy m_Target;
        Vector2 m_MissEnd;
        float m_Miss;

        public KcajBeam(Player player, HyperfixateSpec spec)
        {
            m_Player = player;
            m_Spec = spec;
            m_Fix = new Hyperfixation(spec);
            player.Specials = spec.Charges;
            player.Kit.Recharge(spec.Charges, spec.RechargeTime);
            Transform root = player.World.Root;
            m_Beam = MakeRenderer(root, "Hyperfixate", SortOrder.Effects + 2);
            for (int i = 0; i < m_Edges.Length; i++)
            {
                m_Edges[i] = MakeRenderer(root, "Dim", SortOrder.Effects + 20);
                m_Edges[i].color = new Color(0.03f, 0.01f, 0.06f, 0f);
            }
        }

        static SpriteRenderer MakeRenderer(Transform parent, string name, int order) => Art.Renderer(parent, name, Art.Pixel, order, shown: false);

        /// <summary>Stuck on something: kcaj only shuffles, and can't jump or fire anything else.</summary>
        public bool Locked => m_Fix.Locked;
        public Enemy Target => Locked ? m_Target : null;

        /// <summary>Lets go at once (he's down, or in a tabk).</summary>
        public void Cancel()
        {
            if (m_Fix.Locked) m_Fix.Step(0f, false, false, 0f);
            m_Target = null;
        }

        public void Tick(float dt, bool pressed, bool held)
        {
            Player p = m_Player;
            m_Miss -= dt;

            if (m_Fix.Locked)
            {
                Enemy e = m_Target;
                float burn = m_Fix.Step(dt, held, e != null && e.Shootable, e != null ? Vector2.Distance(p.Center, e.Center) : 0f);
                if (burn > 0f) Burn(e, burn);
                if (!m_Fix.Locked)
                {
                    m_Target = null;
                    if (m_Fix.Ended == FixateEnd.Spent || m_Fix.Ended == FixateEnd.OutOfReach) Sfx.Play(Sfx.Glitch, 0.4f);
                }
                else p.Facing = e.Pos.x >= p.Pos.x ? 1 : -1;
                return;
            }

            if (!pressed || p.Specials <= 0) return;
            Vector2 origin = p.MuzzlePos, dir = p.Aim.normalized;
            float wall = WallDistance(origin, dir);
            Enemy hit = FirstTouched(origin, dir, wall);
            if (hit == null)
            {
                // Nothing to fixate on: the beam flicks out and fizzles, and the charge isn't spent.
                m_MissEnd = origin + dir * Mathf.Min(wall, m_Spec.Range);
                m_Miss = 0.12f;
                Sfx.Play(Sfx.Glitch, 0.25f);
                return;
            }
            p.Specials--;
            m_Target = hit;
            m_Fix.Lock();
            Sfx.Play(Sfx.Charge, 0.5f);
        }

        Enemy FirstTouched(Vector2 origin, Vector2 dir, float wall)
        {
            m_Seen.Clear();
            foreach (Enemy e in m_Player.World.Enemies)
                if (e.Shootable) m_Seen.Add(e);
            if (m_Centres.Length < m_Seen.Count)
            {
                m_Centres = new Vector2[m_Seen.Count * 2];
                m_Halves = new Vector2[m_Seen.Count * 2];
            }
            for (int i = 0; i < m_Seen.Count; i++)
            {
                Enemy e = m_Seen[i];
                m_Centres[i] = e.Center;
                m_Halves[i] = new Vector2(e.HalfW, e.Height * 0.5f);
            }
            int index = m_Spec.FirstTouched(origin, dir, m_Centres, m_Halves, m_Seen.Count, wall);
            return index >= 0 ? m_Seen[index] : null;
        }

        float WallDistance(Vector2 origin, Vector2 dir)
        {
            Level level = m_Player.World.Level;
            const float step = 0.12f;
            for (float d = 0f; d <= m_Spec.Range; d += step)
                if (level.IsSolidAt(origin + dir * d)) return d;
            return float.MaxValue;
        }

        void Burn(Enemy e, float burn)
        {
            World w = m_Player.World;
            w.ShotsHit++;
            e.Soak += burn;
            while (e.Soak >= 1f && e.Shootable)
            {
                e.Soak -= 1f;
                w.Combat.DamageEnemy(e, 1);
            }
            Vector2 at = e.Center + new Vector2(Rng.Fx.Range(-0.2f, 0.2f), Rng.Fx.Range(-0.3f, 0.3f));
            w.HitFeel.Fizz(at);
            int sparks = 1 + (int)(3f * m_Fix.Held / m_Spec.RampTime);
            for (int i = 0; i < sparks; i++)
                w.SpawnParticle(at, new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(1f, 5f)), i % 2 == 0 ? KcajArt.Beam : KcajArt.Hot, 1.5f, 0.3f);
            Sfx.Play(Sfx.Sizzle, 0.15f + 0.25f * Mathf.Clamp01(m_Fix.Held / m_Spec.RampTime));
        }

        public void Render()
        {
            Player p = m_Player;
            bool locked = m_Fix.Locked && m_Target != null && !p.Dead;
            bool miss = !locked && m_Miss > 0f;
            m_Beam.enabled = locked || miss;
            if (m_Beam.enabled)
            {
                Vector2 from = p.MuzzlePos, to = locked ? m_Target.Center : m_MissEnd;
                Vector2 d = to - from;
                float heat = locked ? Mathf.Clamp01(m_Fix.Held / m_Spec.RampTime) : 0f;
                float thick = locked ? 1f + Mathf.Round(heat * 2f) + ((int)(Time.time * 30f) % 2) : 1f;
                Transform t = m_Beam.transform;
                t.position = (Vector3)((from + to) * 0.5f);
                t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                t.localScale = new Vector3(d.magnitude * Art.PPU, thick, 1f);
                m_Beam.color = miss ? new Color(1f, 1f, 1f, 0.5f) : Color.Lerp(KcajArt.Beam, KcajArt.Hot, heat * (0.6f + 0.4f * Mathf.Sin(Time.time * 40f)));
            }

            // The screen edges close in while he's fixated.
            float dim = locked ? m_Fix.Dim : 0f;
            Rect view = p.World.View.Rect;
            for (int side = 0; side < 4; side++)
            {
                for (int layer = 0; layer < 4; layer++)
                {
                    SpriteRenderer sr = m_Edges[side * 4 + layer];
                    sr.enabled = dim > 0.01f;
                    if (!sr.enabled) continue;
                    bool across = side < 2; // left and right bars span the height
                    float depth = dim * (layer + 1) * (across ? view.width : view.height) * 0.07f;
                    Vector2 centre = side switch
                    {
                        0 => new Vector2(view.xMin + depth * 0.5f, view.center.y),
                        1 => new Vector2(view.xMax - depth * 0.5f, view.center.y),
                        2 => new Vector2(view.center.x, view.yMin + depth * 0.5f),
                        _ => new Vector2(view.center.x, view.yMax - depth * 0.5f),
                    };
                    sr.transform.position = new Vector3(centre.x, centre.y, 0f);
                    sr.transform.rotation = Quaternion.identity;
                    sr.transform.localScale = across
                        ? new Vector3(depth * Art.PPU, view.height * Art.PPU, 1f)
                        : new Vector3(view.width * Art.PPU, depth * Art.PPU, 1f);
                    Color c = sr.color;
                    c.a = dim * 0.5f;
                    sr.color = c;
                }
            }
        }
    }

    /// <summary>
    /// kcaj's super, Unmedicated, on screen (<c>?spike=kcaj</c>: the soldier's SU becomes it, charged again after each
    /// rain). The pills are a <see cref="PillRain"/>: numbers in arrays, drawn here with a pool of sprites, so the
    /// spike answers whether a phone keeps up with 120 of them.
    /// </summary>
    public sealed class KcajPills : IPillGround
    {
        readonly Player m_Player;
        readonly UnmedicatedSpec m_Spec;
        readonly PillRain m_Rain;
        readonly PillView m_View;
        readonly System.Func<float, float, bool> m_OnGoon;

        public KcajPills(Player player, UnmedicatedSpec spec)
        {
            m_Player = player;
            m_Spec = spec;
            m_Rain = new PillRain(spec);
            m_OnGoon = OnGoon;
            m_View = new PillView(player.World.Root, m_Rain.Capacity, 1f);
        }

        /// <summary>Pills are still falling or bouncing.</summary>
        public bool Raining { get; private set; }

        public bool Solid(float x, float y) => m_Player.World.Level.IsSolidAt(new Vector2(x, y));

        bool OnGoon(float x, float y)
        {
            var at = new Vector2(x, y);
            foreach (Enemy e in m_Player.World.Enemies)
                if (e.Shootable && e.Bounds.Contains(at)) return true;
            return false;
        }

        public void Start()
        {
            Rect view = m_Player.World.View.Rect;
            m_Rain.Start(m_Player.World.Rng, view.xMin, view.xMax, view.yMax);
            Raining = true;
            Sfx.Play(Sfx.Thump, 0.8f);
        }

        public void Tick(float dt)
        {
            if (!Raining) return;
            m_Rain.Step(dt, this, m_OnGoon);
            World w = m_Player.World;
            for (int p = 0; p < m_Rain.PopCount; p++)
            {
                Vector2 at = m_Rain.Pops[p];
                Pop(w, at);
            }
            if (m_Rain.PopCount > 0) Sfx.Play(Sfx.Splat, Mathf.Min(0.6f, 0.15f + 0.05f * m_Rain.PopCount));
            if (m_Rain.Done)
            {
                Raining = false;
                // A spike: the super is charged again for the next go.
                while (!m_Player.Kit.SuperReady) m_Player.Kit.AddKill();
            }
        }

        void Pop(World w, Vector2 at)
        {
            float reach = m_Spec.PopRadius;
            foreach (Enemy e in w.Enemies)
            {
                if (!e.Shootable) continue;
                Rect b = e.Bounds;
                float dx = Mathf.Max(Mathf.Max(b.xMin - at.x, at.x - b.xMax), 0f), dy = Mathf.Max(Mathf.Max(b.yMin - at.y, at.y - b.yMax), 0f);
                if (dx * dx + dy * dy > reach * reach) continue;
                e.Soak += m_Spec.PopDamage;
                while (e.Soak >= 1f && e.Shootable)
                {
                    e.Soak -= 1f;
                    w.Combat.DamageEnemy(e, 1);
                }
                w.HitFeel.Fizz(at);
            }
            // m_Spec.HurtsSoldiers is false: the crew are never touched.
            KcajArt.Pop(w, at, 3);
        }

        public void Render() => m_View.Render(m_Rain, Raining);
    }

    /// <summary>A rain's pills on screen: a pool of sprites, one a pill, placed each frame from the <see cref="PillRain"/>.</summary>
    public sealed class PillView
    {
        readonly SpriteRenderer[] m_Sprites;
        readonly float[] m_Spin;

        public PillView(Transform parent, int count, float scale)
        {
            m_Sprites = new SpriteRenderer[count];
            m_Spin = new float[count];
            var root = new GameObject("Pills").transform;
            root.SetParent(parent, false);
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Pill");
                go.transform.SetParent(root, false);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = KcajArt.Pills[i % KcajArt.Pills.Length];
                sr.sortingOrder = SortOrder.Projectiles;
                sr.enabled = false;
                m_Sprites[i] = sr;
                m_Spin[i] = Rng.Fx.Range(-720f, 720f);
            }
        }

        public void Render(PillRain rain, bool raining)
        {
            float t = Time.time;
            for (int i = 0; i < m_Sprites.Length; i++)
            {
                SpriteRenderer sr = m_Sprites[i];
                bool show = raining && rain.IsLive(i);
                if (sr.enabled != show) sr.enabled = show;
                if (!show) continue;
                Vector2 at = rain.Position(i);
                Transform tr = sr.transform;
                tr.position = new Vector3(Entity.Snap(at.x), Entity.Snap(at.y), 0f);
                tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Round(t * m_Spin[i] / 45f) * 45f); // tumbling, in eighths so the pixels stay crisp
            }
        }
    }

    /// <summary>kcaj's colours, and the pills' sprites.</summary>
    public static class KcajArt
    {
        public static readonly Color Beam = new Color(1f, 0.35f, 0.75f), Hot = new Color(1f, 0.95f, 0.85f);
        public static readonly Color[] PillColours =
        {
            new Color(1f, 0.4f, 0.55f), new Color(0.45f, 0.75f, 1f), Palette.Gold, new Color(0.55f, 0.95f, 0.55f),
        };

        static Sprite[] s_Pills;

        /// <summary>A pill popping: a puff in its colours.</summary>
        public static void Pop(World w, Vector2 at, int flecks)
        {
            for (int i = 0; i < flecks; i++)
                w.SpawnParticle(at, new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(1f, 4f)), PillColours[i % PillColours.Length], 1.5f, 0.3f);
        }

        /// <summary>Capsules, half a colour and half white, one for each colour.</summary>
        public static Sprite[] Pills
        {
            get
            {
                if (s_Pills != null) return s_Pills;
                s_Pills = new Sprite[PillColours.Length];
                var white = new Color32(250, 248, 240, 255);
                var outline = new Color32(30, 20, 40, 255);
                for (int i = 0; i < PillColours.Length; i++)
                {
                    var c = new PixelCanvas(7, 4);
                    Color32 colour = PillColours[i];
                    c.Rect(1, 0, 5, 4, outline);
                    c.Rect(0, 1, 7, 2, outline);
                    c.Rect(1, 1, 2, 2, colour);
                    c.Rect(3, 1, 3, 2, white);
                    s_Pills[i] = Art.ToSprite(c, new Vector2(0.5f, 0.5f));
                }
                return s_Pills;
            }
        }
    }
}
