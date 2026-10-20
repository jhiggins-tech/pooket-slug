using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// What one combo shows in a team super (<see cref="TeamSuperShow"/>, <c>docs/design/team-supers.md</c>), between the
    /// split card and the moment everything on screen dies: built as the card leaves, told when each stage begins
    /// (<see cref="TeamSuperSpec.Stage"/>), drawn each frame, and given each enemy's splash as it dies. The show does the
    /// killing; an act only looks and sounds like it did.
    /// </summary>
    public abstract class TeamAct
    {
        protected readonly World World;
        protected readonly Transform Root;
        /// <summary>The way the presser faced: the combo sweeps that way.</summary>
        protected readonly int Dir;

        protected TeamAct(World world, Transform root, int dir)
        {
            World = world;
            Root = root;
            Dir = dir;
        }

        protected static TeamSuperSpec Spec => TeamSupers.Spec;

        public static TeamAct For(TeamCombo combo, World world, Transform root, int dir)
        {
            if (combo == TeamSupers.MentosAndCola) return new MentosAndCola(world, root, dir);
            if (combo == TeamSupers.PillWeasels) return new PillWeasels(world, root, dir);
            return new SewerRats(world, root, dir);
        }

        /// <summary>The colour of a boss's TEAM SUPER! popup.</summary>
        public abstract Color Highlight { get; }

        /// <summary>The card has gone: the act's pieces go up.</summary>
        public abstract void Build();

        /// <summary>A stage begins: its sounds.</summary>
        public virtual void Begin(TeamSuperSpec.Stage stage) { }

        public virtual void Tick(float dt, float t) { }

        public abstract void Render(float t);

        /// <summary>The moment everything dies, before the splashes.</summary>
        public virtual void Kill() { }

        /// <summary>Enemy <paramref name="e"/> (the <paramref name="index"/>th on screen) dies in the act's splash.</summary>
        public abstract void Splash(Enemy e, int index);

        protected SpriteRenderer Renderer(string name, Sprite sprite, int order) => Art.Renderer(Root, name, sprite, order);

        /// <summary>The top of the ground under <paramref name="x"/> (0 over a pit).</summary>
        protected static float FloorAt(World w, float x)
        {
            float y = 0f;
            while (y < Tuning.Camera.ViewHeight * 0.8f && w.Level.IsSolidAt(new Vector2(x, y + 0.05f))) y += 0.25f;
            return y;
        }
    }

    /// <summary>A flood across the whole screen, in columns standing on the floor: tones2's sludge, in Sewer Rats and Mentos and Cola.</summary>
    public sealed class FloodLayer
    {
        public const int ColumnPx = 4;
        const float Height = 1.3f; // world units above the floor at full flood

        public static readonly Color Sludge = new Color(0.61f, 0.89f, 0.18f, 0.9f), SludgeDark = new Color(0.43f, 0.54f, 0.11f, 0.9f);
        public static readonly Color Foam = new Color(0.86f, 0.96f, 0.5f, 1f);

        readonly List<SpriteRenderer> m_Columns = new List<SpriteRenderer>(), m_Foam = new List<SpriteRenderer>();
        readonly float[] m_Floor;
        readonly float m_Left;
        readonly int m_Dir;

        public FloodLayer(World w, Transform root, int dir, System.Func<World, float, float> floorAt)
        {
            m_Dir = dir;
            m_Left = w.View.Rect.xMin;
            int count = Mathf.CeilToInt(w.View.Rect.width / Step) + 1;
            m_Floor = new float[count];
            for (int i = 0; i < count; i++)
            {
                float x = X(i);
                m_Floor[i] = floorAt(w, x);
                SpriteRenderer col = Art.Renderer(root, "Flood", Art.Pixel, SortOrder.Effects - 2);
                col.color = i % 3 == 0 ? SludgeDark : Sludge;
                col.transform.localPosition = new Vector3(x, 0f, 0f);
                m_Columns.Add(col);
                SpriteRenderer foam = Art.Renderer(root, "Foam", Art.Pixel, SortOrder.Effects - 1);
                foam.color = Foam;
                foam.transform.localPosition = new Vector3(x, 0f, 0f);
                m_Foam.Add(foam);
            }
        }

        static float Step => ColumnPx / (float)Art.PPU;

        float X(int i) => m_Left + (i + 0.5f) * Step;

        /// <summary>The column over <paramref name="x"/>.</summary>
        public int ColumnAt(float x) => Mathf.Clamp(Mathf.FloorToInt((x - m_Left) / Step), 0, m_Floor.Length - 1);

        /// <summary>The wave's top at column <paramref name="i"/>, in world units, with the flood at <paramref name="level"/> (0 to 1).</summary>
        public float Crest(int i, float level, float t)
        {
            float wave = Mathf.Sin(i * 0.45f - t * 9f * m_Dir) * 0.12f + Mathf.Sin(i * 0.13f + t * 3f) * 0.08f;
            return m_Floor[i] + level * (Height + wave);
        }

        public void Render(float level, float t)
        {
            bool shown = level > 0.01f;
            for (int i = 0; i < m_Columns.Count; i++)
            {
                SpriteRenderer col = m_Columns[i], foam = m_Foam[i];
                col.enabled = foam.enabled = shown;
                if (!shown) continue;
                float top = Crest(i, level, t), x = X(i);
                float floor = m_Floor[i] - 0.1f; // a little into the ground, so no gap shows
                col.transform.localPosition = new Vector3(x, (floor + top) * 0.5f, 0f);
                col.transform.localScale = new Vector3(ColumnPx, Mathf.Max(1f, (top - floor) * Art.PPU), 1f);
                foam.transform.localPosition = new Vector3(x, Entity.Snap(top), 0f);
                foam.transform.localScale = new Vector3(ColumnPx, 2f, 1f);
            }
        }
    }

    /// <summary>
    /// Sewer Rats (tones2 + kie): tones2's sludge floods the floor, kie's weasels surf the wave from one side of the
    /// screen to the other, and every goon pops in a toxic splash.
    /// </summary>
    public sealed class SewerRats : TeamAct
    {
        readonly List<SpriteRenderer> m_Weasels = new List<SpriteRenderer>();
        FloodLayer m_Flood;

        public SewerRats(World world, Transform root, int dir) : base(world, root, dir) { }

        public override Color Highlight => FloodLayer.Foam;

        public override void Build()
        {
            m_Flood = new FloodLayer(World, Root, Dir, FloorAt);
            for (int i = 0; i < Spec.Weasels; i++) m_Weasels.Add(Renderer("Weasel", KieArt.Weasel[0], SortOrder.Effects));
        }

        public override void Begin(TeamSuperSpec.Stage stage)
        {
            if (stage == TeamSuperSpec.Stage.Flood)
            {
                Sfx.Play(Sfx.Spew, 1f);
                Sfx.Play(Sfx.Splat, 0.8f);
                World.View.Shake(0.15f);
            }
            else if (stage == TeamSuperSpec.Stage.Surf)
            {
                Sfx.Play(Sfx.WeaselTune, 0.9f);
                Sfx.Play(Sfx.WeaselThrow, 0.8f);
            }
        }

        public override void Kill() => Sfx.Play(Sfx.Poison, 0.9f);

        public override void Splash(Enemy e, int index) => World.Burst(e.Center, 14, index % 2 == 0 ? FloodLayer.Sludge : FloodLayer.Foam);

        public override void Render(float t)
        {
            float level = Spec.FloodLevel(t);
            m_Flood.Render(level, t);

            // the weasels surf the crest, a ragged line of them, from the presser's back to the far side
            float surf = Spec.SurfProgress(t);
            Rect view = World.View.Rect;
            for (int i = 0; i < m_Weasels.Count; i++)
            {
                SpriteRenderer weasel = m_Weasels[i];
                float across = Mathf.Clamp01(surf * 1.6f - i * 0.07f);
                bool riding = surf > 0f && across > 0f && across < 1f && level > 0.5f;
                weasel.enabled = riding;
                if (!riding) continue;
                float x = Dir > 0 ? view.xMin + across * view.width : view.xMin + (1f - across) * view.width;
                float y = m_Flood.Crest(m_Flood.ColumnAt(x), level, t) + Mathf.Abs(Mathf.Sin(t * 14f + i)) * 0.18f;
                weasel.transform.localPosition = new Vector3(Entity.Snap(x), Entity.Snap(y), 0f);
                weasel.transform.localScale = new Vector3(Dir, 1f, 1f);
                weasel.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 10f + i * 1.7f) * 12f);
                weasel.sprite = KieArt.Weasel[Fx.Step(t, 16f, 2, i)];
                if (Rng.Fx.Value < 0.15f)
                    World.SpawnParticle(new Vector2(x - Dir * 0.2f, y), new Vector2(-Dir * Rng.Fx.Range(1f, 3f), Rng.Fx.Range(1f, 3f)), FloodLayer.Foam, 1f, 0.3f);
            }
        }
    }

    /// <summary>
    /// Mentos and Cola (tones2 + kcaj): tones2's sludge floods the floor, kcaj's pills rain into it, and every pill that
    /// lands fizzes up into a geyser; at the end a geyser blows every goon on screen away.
    /// </summary>
    public sealed class MentosAndCola : TeamAct
    {
        const int Pills = 28, Geysers = 48;
        const float FallSpeed = 16f, GeyserTime = 0.7f, GeyserWidthPx = 6f;

        static readonly Color Fizz = new Color(0.97f, 0.95f, 0.85f, 0.95f), Cola = new Color(0.45f, 0.25f, 0.12f, 1f);

        readonly SpriteRenderer[] m_Pills = new SpriteRenderer[Pills];
        readonly float[] m_PillX = new float[Pills], m_PillAt = new float[Pills];
        readonly bool[] m_Landed = new bool[Pills];
        readonly SpriteRenderer[] m_Geysers = new SpriteRenderer[Geysers];
        readonly float[] m_GeyserX = new float[Geysers], m_GeyserAt = new float[Geysers], m_GeyserHeight = new float[Geysers];
        FloodLayer m_Flood;
        int m_NextGeyser;
        float m_Now, m_LastFizz;

        public MentosAndCola(World world, Transform root, int dir) : base(world, root, dir) { }

        public override Color Highlight => Fizz;

        public override void Build()
        {
            m_Flood = new FloodLayer(World, Root, Dir, FloorAt);
            Rect view = World.View.Rect;
            float rainFrom = Spec.Card + Spec.Flood, rainFor = Spec.Surf * 0.75f;
            for (int i = 0; i < Pills; i++)
            {
                // spread across the screen, a pill a slot, falling in a sweep the way the presser faced
                float across = (i + Rng.Fx.Range(0.2f, 0.8f)) / Pills;
                m_PillX[i] = Dir > 0 ? view.xMin + across * view.width : view.xMax - across * view.width;
                m_PillAt[i] = rainFrom + rainFor * (i / (float)Pills) + Rng.Fx.Range(0f, 0.08f);
                m_Pills[i] = Renderer("Pill", KcajArt.Pills[i % KcajArt.Pills.Length], SortOrder.Effects);
                m_Pills[i].enabled = false;
            }
            for (int i = 0; i < Geysers; i++)
            {
                m_Geysers[i] = Renderer("Geyser", Art.Pixel, SortOrder.Effects - 1);
                m_Geysers[i].color = Fizz;
                m_Geysers[i].enabled = false;
                m_GeyserAt[i] = -10f;
            }
        }

        public override void Begin(TeamSuperSpec.Stage stage)
        {
            if (stage == TeamSuperSpec.Stage.Flood)
            {
                Sfx.Play(Sfx.Spew, 1f);
                Sfx.Play(Sfx.Splat, 0.8f);
                World.View.Shake(0.15f);
            }
            else if (stage == TeamSuperSpec.Stage.Surf) Sfx.Play(Sfx.Thump, 0.9f);
        }

        void Geyser(float x, float height)
        {
            int g = m_NextGeyser;
            m_NextGeyser = (m_NextGeyser + 1) % Geysers;
            m_GeyserX[g] = x;
            m_GeyserAt[g] = m_Now;
            m_GeyserHeight[g] = height;
        }

        public override void Tick(float dt, float t)
        {
            m_Now = t;
            if (m_Flood == null) return;
            float level = Spec.FloodLevel(t);
            for (int i = 0; i < Pills; i++)
            {
                if (m_Landed[i] || t < m_PillAt[i]) continue;
                float y = World.View.Rect.yMax + 0.5f - FallSpeed * (t - m_PillAt[i]);
                if (y > m_Flood.Crest(m_Flood.ColumnAt(m_PillX[i]), level, t)) continue;
                m_Landed[i] = true;
                Geyser(m_PillX[i], Rng.Fx.Range(2f, 3.5f));
                if (t - m_LastFizz > 0.08f)
                {
                    m_LastFizz = t;
                    Sfx.Play(Sfx.Sizzle, 0.5f);
                }
            }
        }

        public override void Kill() => Sfx.Play(Sfx.Splat, 1f);

        public override void Splash(Enemy e, int index)
        {
            Geyser(e.Center.x, 6f);
            World.Burst(e.Center, 14, index % 2 == 0 ? Fizz : Cola);
        }

        public override void Render(float t)
        {
            float level = Spec.FloodLevel(t);
            m_Flood.Render(level, t);
            float top = World.View.Rect.yMax + 0.5f;
            for (int i = 0; i < Pills; i++)
            {
                SpriteRenderer pill = m_Pills[i];
                bool falling = !m_Landed[i] && t >= m_PillAt[i];
                pill.enabled = falling;
                if (!falling) continue;
                float y = top - FallSpeed * (t - m_PillAt[i]);
                pill.transform.localPosition = new Vector3(Entity.Snap(m_PillX[i]), Entity.Snap(y), 0f);
                pill.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Round((t * 900f + i * 45f) / 45f) * 45f);
            }
            for (int g = 0; g < Geysers; g++)
            {
                SpriteRenderer sr = m_Geysers[g];
                float age = t - m_GeyserAt[g];
                bool up = age >= 0f && age < GeyserTime;
                sr.enabled = up;
                if (!up) continue;
                float x = m_GeyserX[g];
                float bottom = m_Flood.Crest(m_Flood.ColumnAt(x), Mathf.Max(level, 0.3f), t);
                float h = m_GeyserHeight[g] * Mathf.Sin(Mathf.PI * age / GeyserTime);
                sr.transform.localPosition = new Vector3(Entity.Snap(x), bottom + h * 0.5f, 0f);
                sr.transform.localScale = new Vector3(GeyserWidthPx, Mathf.Max(1f, h * Art.PPU), 1f);
                if (Rng.Fx.Value < 0.4f)
                    World.SpawnParticle(new Vector2(x, bottom + h), new Vector2(Rng.Fx.Range(-2f, 2f), Rng.Fx.Range(1f, 4f)),
                        Rng.Fx.Value < 0.5f ? Fizz : FloodLayer.Foam, 1.5f, 0.35f);
            }
        }
    }

    /// <summary>
    /// Pill Weasels (kie + kcaj): a weasel for every goon on screen, each carrying one of kcaj's pills, scurries out
    /// from behind the presser and under its goon; then they all pop, and burst into bouncing pills.
    /// </summary>
    public sealed class PillWeasels : TeamAct
    {
        const int Most = 14, Fewest = 5;

        readonly List<SpriteRenderer> m_Weasels = new List<SpriteRenderer>(), m_Pills = new List<SpriteRenderer>();
        readonly List<Enemy> m_Goons = new List<Enemy>();
        readonly List<float> m_To = new List<float>();
        float m_From;
        bool m_Popped;

        public PillWeasels(World world, Transform root, int dir) : base(world, root, dir) { }

        public override Color Highlight => KcajArt.PillColours[0];

        public override void Build()
        {
            Rect view = World.View.Rect;
            m_From = Dir > 0 ? view.xMin - 0.5f : view.xMax + 0.5f;
            foreach (Enemy e in World.Enemies)
            {
                if (m_Goons.Count >= Most) break;
                if (!e.Shootable || e.Pos.x < view.xMin || e.Pos.x > view.xMax) continue;
                m_Goons.Add(e);
                m_To.Add(e.Pos.x);
            }
            while (m_To.Count < Fewest)
            {
                // a screen without enough goons still gets a pack: the rest run to where goons would be
                m_Goons.Add(null);
                m_To.Add(Mathf.Lerp(view.xMin, view.xMax, Rng.Fx.Range(0.3f, 0.95f)));
            }
            for (int i = 0; i < m_To.Count; i++)
            {
                m_Weasels.Add(Renderer("Weasel", KieArt.Weasel[0], SortOrder.Effects));
                m_Pills.Add(Renderer("Pill", KcajArt.Pills[i % KcajArt.Pills.Length], SortOrder.Effects + 1));
            }
        }

        public override void Begin(TeamSuperSpec.Stage stage)
        {
            if (stage == TeamSuperSpec.Stage.Flood)
            {
                Sfx.Play(Sfx.WeaselTune, 0.9f);
                Sfx.Play(Sfx.WeaselThrow, 0.8f);
            }
        }

        /// <summary>How far weasel <paramref name="i"/> has run to its goon, 0 to 1, <paramref name="t"/> seconds in.</summary>
        float Run(int i, float t)
        {
            float lag = i * 0.06f, length = Spec.Flood + Spec.Surf - 0.35f;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - Spec.Card - lag) / length));
        }

        float Target(int i)
        {
            Enemy e = m_Goons[i];
            if (e != null && !e.Removed) m_To[i] = e.Pos.x;
            return m_To[i];
        }

        public override void Kill()
        {
            m_Popped = true;
            Sfx.Play(Sfx.Splat, 1f);
            for (int i = 0; i < m_Weasels.Count; i++)
            {
                float x = Target(i);
                var at = new Vector2(x, FloorAt(World, x) + 0.3f);
                for (int k = 0; k < 8; k++)
                    World.SpawnParticle(at, new Vector2(Rng.Fx.Range(-4f, 4f), Rng.Fx.Range(3f, 8f)), KcajArt.PillColours[k % KcajArt.PillColours.Length], 2f, 0.6f);
            }
        }

        public override void Splash(Enemy e, int index)
        {
            KcajArt.Pop(World, e.Center, 10);
            World.Burst(e.Center, 10, KcajArt.PillColours[index % KcajArt.PillColours.Length]);
        }

        public override void Render(float t)
        {
            for (int i = 0; i < m_Weasels.Count; i++)
            {
                SpriteRenderer weasel = m_Weasels[i], pill = m_Pills[i];
                float run = Run(i, t);
                bool shown = !m_Popped && run > 0f;
                weasel.enabled = pill.enabled = shown;
                if (!shown) continue;
                float to = Target(i), x = Mathf.Lerp(m_From, to, run);
                float floor = FloorAt(World, x);
                float hop = run < 1f ? Mathf.Abs(Mathf.Sin(t * 18f + i)) * 0.12f : 0f;
                int face = to >= m_From ? 1 : -1;
                weasel.transform.localPosition = new Vector3(Entity.Snap(x), Entity.Snap(floor + hop), 0f);
                weasel.transform.localScale = new Vector3(face, 1f, 1f);
                weasel.sprite = KieArt.Weasel[run < 1f ? Fx.Step(t, 16f, 2, i) : 0];
                // the pill rides on its back, wobbling, and shakes as the weasel waits under its goon
                float shake = run >= 1f ? Mathf.Sin(t * 60f + i) * 0.04f : 0f;
                pill.transform.localPosition = new Vector3(Entity.Snap(x - face * 0.05f + shake), Entity.Snap(floor + hop + 0.55f), 0f);
                pill.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Round(Mathf.Sin(t * 8f + i) * 2f) * 22.5f);
            }
        }
    }
}
