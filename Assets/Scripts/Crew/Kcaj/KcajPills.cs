// kcaj's super, Unmedicated: a rain of pills laid out from a seed, and the pool of sprites that draws a rain.
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kcaj's super, Unmedicated, on screen. The pills are a <see cref="PillRain"/>: numbers in arrays, drawn here with
    /// a pool of sprites, so a phone keeps up with 120 of them. A rain is laid out from a seed, which is all the partner
    /// is sent (<see cref="KitMove.Pills"/>).
    /// </summary>
    public sealed class KcajPills
    {
        /// <summary>What the pills land on and pop against: the level's ground, and the goons.</summary>
        public sealed class Ground : IPillGround
        {
            readonly World m_World;
            public readonly System.Func<float, float, bool> OnGoon;

            public Ground(World world)
            {
                m_World = world;
                OnGoon = Touches;
            }

            public bool Solid(float x, float y) => m_World.Level.IsSolidAt(new Vector2(x, y));

            bool Touches(float x, float y)
            {
                var at = new Vector2(x, y);
                foreach (Enemy e in m_World.Enemies)
                    if (e.Shootable && e.Bounds.Contains(at)) return true;
                return false;
            }
        }

        readonly World m_World;
        readonly UnmedicatedSpec m_Spec;
        readonly PillRain m_Rain;
        readonly PillView m_View;
        readonly Ground m_Ground;

        /// <param name="parent">What the pills are drawn under: the world's root, unless something that comes and goes owns them.</param>
        public KcajPills(World world, UnmedicatedSpec spec, Transform parent = null)
        {
            m_World = world;
            m_Spec = spec;
            m_Rain = new PillRain(spec);
            m_Ground = new Ground(world);
            m_View = new PillView(parent != null ? parent : world.Root, m_Rain.Capacity, 1f);
        }

        /// <summary>Pills are still falling or bouncing.</summary>
        public bool Raining { get; private set; }

        public void Start()
        {
            World w = m_World;
            Rect view = w.View.Rect;
            uint seed = PillRain.Seed(w.Rng);
            m_Rain.Start(seed, view.xMin, view.xMax, view.yMax);
            CrewKit.Send(w, KitMove.Pills, new Vector2(view.xMin, view.yMax), new Vector2(view.xMax, seed));
            Raining = true;
            Sfx.Play(Sfx.PillsRattle, 0.8f);
        }

        public void Tick(float dt)
        {
            if (!Raining) return;
            m_Rain.Step(dt, m_Ground, m_Ground.OnGoon);
            World w = m_World;
            for (int p = 0; p < m_Rain.PopCount; p++)
            {
                Vector2 at = m_Rain.Pops[p];
                Pop(w, at);
            }
            if (m_Rain.PopCount > 0) Sfx.Play(Sfx.PillPopAt(), Mathf.Min(0.7f, 0.3f + 0.08f * m_Rain.PopCount));
            if (m_Rain.Done) Raining = false;
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
                m_Sprites[i] = KcajArt.Pill(root, i, SortOrder.Projectiles, scale);
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
                KcajArt.Place(sr, rain.Position(i), t * m_Spin[i]); // tumbling
            }
        }
    }
}
