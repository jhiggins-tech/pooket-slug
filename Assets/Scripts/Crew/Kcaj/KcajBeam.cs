// kcaj's special, Hyperfixate: the beam that sticks to one target and burns it, from kcaj or his tabk's nozzle.
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's special, Hyperfixate, on screen. The rules are <see cref="HyperfixateSpec"/> and
    /// <see cref="Hyperfixation"/>; this finds what the beam touches, draws the beam and the dimmed screen edges, burns
    /// the target (through <see cref="Combat.DamageEnemy"/>, so a partner's authority hears of it), and tells the
    /// partner what it's stuck to.
    /// </summary>
    public sealed class KcajBeam
    {
        readonly IFixateHost m_Host;
        readonly HyperfixateSpec m_Spec;
        readonly Hyperfixation m_Fix;
        readonly SpriteRenderer m_Beam;
        readonly SpriteRenderer[] m_Edges = new SpriteRenderer[16]; // four layers on each side, darker towards the edge
        readonly List<Enemy> m_Seen = new List<Enemy>();
        Vector2[] m_Centres = new Vector2[16], m_Halves = new Vector2[16];
        Enemy m_Target;
        Vector2 m_MissEnd;
        float m_Miss;

        public KcajBeam(IFixateHost host, HyperfixateSpec spec)
        {
            m_Host = host;
            m_Spec = spec;
            m_Fix = new Hyperfixation(spec); // its charges are the host's: kcaj's specials (Content.Kcaj), or his tabk's rounds
            Transform root = host.World.Root;
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

        /// <summary>Its renderers go (its tabk's gone).</summary>
        public void Destroy()
        {
            Object.Destroy(m_Beam.gameObject);
            foreach (SpriteRenderer sr in m_Edges) Object.Destroy(sr.gameObject);
        }

        /// <summary>Lets go at once (he's down, or in a tabk).</summary>
        public void Cancel()
        {
            if (m_Fix.Locked)
            {
                m_Fix.Step(0f, false, false, 0f);
                Tell(m_Target, false);
            }
            m_Target = null;
        }

        /// <summary>Tells the partner the beam stuck to <paramref name="e"/>, or let go of it.</summary>
        void Tell(Enemy e, bool holds) =>
            CrewKit.Send(m_Host.World, KitMove.Fixate, e != null ? e.Center : m_Host.Center, new Vector2(e != null ? e.NetId : -1, holds ? 1f : 0f));

        /// <summary>
        /// A frame with SP <paramref name="pressed"/> or <paramref name="held"/>. Returns whether a press locked on (a
        /// charge spent) or fizzled; false when it held on, wasn't pressed or had no charge to lock with.
        /// </summary>
        public bool Tick(float dt, bool pressed, bool held)
        {
            IFixateHost p = m_Host;
            m_Miss -= dt;

            if (m_Fix.Locked)
            {
                Enemy e = m_Target;
                float burn = m_Fix.Step(dt, held, e != null && e.Shootable, e != null ? Vector2.Distance(p.Center, e.Center) : 0f);
                if (burn > 0f) Burn(e, burn);
                if (!m_Fix.Locked)
                {
                    Tell(e, false);
                    m_Target = null;
                    if (m_Fix.Ended == FixateEnd.Spent || m_Fix.Ended == FixateEnd.OutOfReach) Sfx.Play(Sfx.BeamSnap, 0.6f);
                }
                else p.Face(e.Pos);
                return false;
            }

            if (!pressed || !p.HasCharge) return false;
            Vector2 origin = p.BeamFrom, dir = p.BeamAim.normalized;
            float wall = WallDistance(origin, dir);
            Enemy hit = FirstTouched(origin, dir, wall);
            if (hit == null)
            {
                // Nothing to fixate on: the beam flicks out and fizzles, and the charge isn't spent.
                m_MissEnd = origin + dir * Mathf.Min(wall, m_Spec.Range);
                m_Miss = 0.12f;
                Sfx.Play(Sfx.BeamFizzle, 0.5f);
                return true;
            }
            p.SpendCharge();
            m_Target = hit;
            m_Fix.Lock();
            Tell(hit, true);
            Sfx.Play(Sfx.BeamLock, 0.7f);
            return true;
        }

        Enemy FirstTouched(Vector2 origin, Vector2 dir, float wall)
        {
            m_Seen.Clear();
            foreach (Enemy e in m_Host.World.Enemies)
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
            Level level = m_Host.World.Level;
            const float step = 0.12f;
            for (float d = 0f; d <= m_Spec.Range; d += step)
                if (level.IsSolidAt(origin + dir * d)) return d;
            return float.MaxValue;
        }

        void Burn(Enemy e, float burn)
        {
            World w = m_Host.World;
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
            float heat = Mathf.Clamp01(m_Fix.Held / m_Spec.RampTime);
            Sfx.Play(Sfx.BeamBurnAt(heat), 0.4f + 0.3f * heat);
        }

        public void Render()
        {
            IFixateHost p = m_Host;
            bool locked = m_Fix.Locked && m_Target != null && !p.Down;
            bool miss = !locked && m_Miss > 0f;
            m_Beam.enabled = locked || miss;
            if (m_Beam.enabled)
            {
                if (locked) KcajArt.DrawBeam(m_Beam, p.BeamFrom, m_Target.Center, Mathf.Clamp01(m_Fix.Held / m_Spec.RampTime));
                else KcajArt.DrawFaintBeam(m_Beam, p.BeamFrom, m_MissEnd, new Color(1f, 1f, 1f, 0.5f));
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
}
