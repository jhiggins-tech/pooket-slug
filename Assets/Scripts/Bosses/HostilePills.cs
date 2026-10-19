using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Unmedicated on the Trollograms' side: kcaj's pills raining on the soldiers, from his tabk (<see cref="KcajBoss"/>)
    /// or his pilot (<see cref="KcajPilot"/>). A rain is laid out from its number (the count of rains so far), so every
    /// screen in co-op rains the same pills from the number alone, and each screen hurts its own soldier with them.
    /// </summary>
    public sealed class HostilePills
    {
        readonly World m_World;
        readonly UnmedicatedSpec m_Spec;
        readonly PillRain m_Rain;
        readonly PillView m_Pills;
        readonly System.Func<float, float, bool> m_OnSoldier;
        IPillGround m_Ground;
        bool m_Started;

        public HostilePills(World world, UnmedicatedSpec spec)
        {
            m_World = world;
            m_Spec = spec;
            m_Rain = new PillRain(spec);
            m_Pills = new PillView(world.Root, m_Rain.Capacity, 1.5f);
            m_OnSoldier = OnSoldier;
        }

        /// <summary>Pills are still falling or bouncing.</summary>
        public bool Raining => m_Started && !m_Rain.Done;

        /// <summary>
        /// Rain number <paramref name="number"/> starts across the screen, with a burst of pills out of
        /// <paramref name="from"/> to say where they came from.
        /// </summary>
        public void Start(int number, Vector2 from)
        {
            Rect view = m_World.View.Rect;
            m_Rain.Start(new Rng(unchecked((uint)number * 2654435761u + 977u) | 1u), view.xMin, view.xMax, view.yMax);
            m_Started = true;
            Sfx.Play(Sfx.PillsRattle, 0.9f);
            for (int i = 0; i < 10; i++)
                m_World.SpawnParticle(from, new Vector2(Rng.Fx.Range(-2f, 2f), Rng.Fx.Range(6f, 12f)),
                    KcajArt.PillColours[i % KcajArt.PillColours.Length], 2f, 0.5f);
        }

        /// <summary>The pills fall, bounce and pop, each pop near our soldier a hurt.</summary>
        public void Tick(float dt)
        {
            if (!Raining) return;
            m_Rain.Step(dt, m_Ground ??= new LevelGround(m_World.Level), m_OnSoldier);
            for (int i = 0; i < m_Rain.PopCount; i++) Pop(m_Rain.Pops[i]);
            if (m_Rain.PopCount > 0) Sfx.Play(Sfx.PillPopAt(), 0.6f);
        }

        bool OnSoldier(float x, float y)
        {
            Player p = m_World.Player;
            return !p.Dead && p.Bounds.Contains(new Vector2(x, y));
        }

        void Pop(Vector2 at)
        {
            KcajArt.Pop(m_World, at, 4);
            Player p = m_World.Player;
            if (p.Dead) return;
            Rect b = p.Bounds;
            float dx = Mathf.Max(Mathf.Max(b.xMin - at.x, at.x - b.xMax), 0f), dy = Mathf.Max(Mathf.Max(b.yMin - at.y, at.y - b.yMax), 0f);
            if (dx * dx + dy * dy <= m_Spec.PopRadius * m_Spec.PopRadius) p.Hurt(Mathf.RoundToInt(m_Spec.PopDamage));
        }

        public void Render() => m_Pills.Render(m_Rain, Raining);

        /// <summary>The level's solid tiles, for the pills.</summary>
        sealed class LevelGround : IPillGround
        {
            readonly Level m_Level;
            public LevelGround(Level level) => m_Level = level;
            public bool Solid(float x, float y) => m_Level.IsSolidAt(new Vector2(x, y));
        }
    }
}
