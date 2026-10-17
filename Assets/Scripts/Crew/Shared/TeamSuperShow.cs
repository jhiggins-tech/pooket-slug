using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A team super playing out (<c>docs/design/team-supers.md</c>; the numbers are <see cref="TeamSuperSpec"/>). The
    /// action freezes while the split card slams in (drawn by the HUD from <see cref="CardTime"/>), then the combo plays
    /// across the whole screen and every enemy on it dies; a boss takes a big hit instead. Every screen in co-op plays
    /// the show; only the world's authority deals the damage, and the snapshot carries the deaths to the partner.
    /// <para>
    /// Sewer Rats (tones2 + kie), the only combo so far: tones2's sludge floods the floor, kie's weasels surf the wave
    /// from one side of the screen to the other, and every goon pops in a toxic splash.
    /// </para>
    /// </summary>
    public sealed class TeamSuperShow
    {
        const int ColumnPx = 4;
        const float FloodHeight = 1.3f; // world units above the floor at full flood

        static readonly Color Sludge = new Color(0.61f, 0.89f, 0.18f, 0.9f), SludgeDark = new Color(0.43f, 0.54f, 0.11f, 0.9f);
        static readonly Color Foam = new Color(0.86f, 0.96f, 0.5f, 1f);

        public readonly TeamCombo Combo;
        /// <summary>The soldier who pressed TEAM (on the card's left) and their partner.</summary>
        public readonly SoldierDef Fired, Partner;

        readonly World m_World;
        readonly Transform m_Root;
        readonly List<SpriteRenderer> m_Columns = new List<SpriteRenderer>(), m_Foam = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> m_Weasels = new List<SpriteRenderer>();
        float[] m_Floor;
        float m_Time;
        int m_Dir;
        TeamSuperSpec.Stage m_Stage = TeamSuperSpec.Stage.Card;

        static TeamSuperSpec Spec => TeamSupers.Spec;

        public TeamSuperShow(World world, TeamCombo combo, SoldierDef fired, SoldierDef partner, int facing)
        {
            m_World = world;
            Combo = combo;
            Fired = fired;
            Partner = partner;
            m_Dir = facing >= 0 ? 1 : -1;
            m_Root = new GameObject("Team super").transform;
            m_Root.SetParent(world.Root, false);
            Sfx.Play(Sfx.Charge, 1f);
            Sfx.Say("team-super", 0.1f);
            world.View.Shake(0.25f);
            Log.Game.Info("Team super: " + combo.Name);
        }

        /// <summary>The action is frozen for the split card.</summary>
        public bool Freezing => m_Time < Spec.Card;
        /// <summary>Seconds since TEAM was pressed: the HUD draws the card from it.</summary>
        public float CardTime => m_Time;
        public bool Over => m_Time >= Spec.Length;

        public void Tick(float dt)
        {
            m_Time += dt;
            TeamSuperSpec.Stage stage = Spec.At(m_Time);
            if (stage != m_Stage)
            {
                m_Stage = stage;
                if (stage == TeamSuperSpec.Stage.Flood)
                {
                    Sfx.Play(Sfx.Spew, 1f);
                    Sfx.Play(Sfx.Splat, 0.8f);
                    m_World.View.Shake(0.15f);
                }
                else if (stage == TeamSuperSpec.Stage.Surf)
                {
                    Sfx.Play(Sfx.WeaselTune, 0.9f);
                    Sfx.Play(Sfx.WeaselThrow, 0.8f);
                }
            }
            if (stage != TeamSuperSpec.Stage.Card && m_Floor == null) Build();
            if (m_Time >= Spec.KillAt) Kill();
        }

        bool m_Killed;

        /// <summary>Every enemy on screen dies (bosses take a big hit), with a toxic splash under each.</summary>
        void Kill()
        {
            if (m_Killed) return;
            m_Killed = true;
            World w = m_World;
            float left = w.View.Left - 0.5f, right = w.View.Right + 0.5f;
            Sfx.Play(Sfx.BigExplosion, 1f);
            Sfx.Play(Sfx.Poison, 0.9f);
            w.View.Shake(0.5f);
            for (int i = 0; i < w.Enemies.Count; i++)
            {
                Enemy e = w.Enemies[i];
                if (!e.Shootable || e.Pos.x < left || e.Pos.x > right) continue;
                w.Burst(e.Center, 14, i % 2 == 0 ? Sludge : Foam);
                w.SpawnEffect(Art.Explosion, 30f, e.Center, 0.8f);
                if (!w.IsAuthority) continue; // the authority's damage comes back in the snapshot
                if (e is BossEnemy)
                {
                    int damage = Spec.BossDamage(e.Hp, e.MaxHp);
                    if (damage > 0) e.Hurt(damage);
                    w.Scoring.Popup("TEAM SUPER!", e.Center + Vector2.up * 1.6f, Foam);
                }
                else e.Hurt(e.Hp);
            }
            // the goons' shots in the air go too
            foreach (Projectile p in w.Projectiles)
                if (p.Hostile && !p.Removed && p.Pos.x >= left && p.Pos.x <= right) p.Remove();
        }

        /// <summary>The flood's columns across the screen, each standing on the floor below it.</summary>
        void Build()
        {
            World w = m_World;
            float left = w.View.Rect.xMin, width = w.View.Rect.width;
            float step = ColumnPx / (float)Art.PPU;
            int count = Mathf.CeilToInt(width / step) + 1;
            m_Floor = new float[count];
            for (int i = 0; i < count; i++)
            {
                float x = left + (i + 0.5f) * step;
                float y = 0f;
                while (y < Tuning.Camera.ViewHeight * 0.8f && w.Level.IsSolidAt(new Vector2(x, y + 0.05f))) y += 0.25f;
                m_Floor[i] = y;
                SpriteRenderer col = Renderer("Flood", Art.Pixel, SortOrder.Effects - 2);
                col.color = i % 3 == 0 ? SludgeDark : Sludge;
                col.transform.localPosition = new Vector3(x, 0f, 0f);
                m_Columns.Add(col);
                SpriteRenderer foam = Renderer("Foam", Art.Pixel, SortOrder.Effects - 1);
                foam.color = Foam;
                foam.transform.localPosition = new Vector3(x, 0f, 0f);
                m_Foam.Add(foam);
            }
            for (int i = 0; i < Spec.Weasels; i++) m_Weasels.Add(Renderer("Weasel", KieArt.Weasel[0], SortOrder.Effects));
        }

        SpriteRenderer Renderer(string name, Sprite sprite, int order) => Art.Renderer(m_Root, name, sprite, order);

        /// <summary>The wave's top at column <paramref name="i"/>, in world units.</summary>
        float Crest(int i, float level)
        {
            float wave = Mathf.Sin(i * 0.45f - m_Time * 9f * m_Dir) * 0.12f + Mathf.Sin(i * 0.13f + m_Time * 3f) * 0.08f;
            return m_Floor[i] + level * (FloodHeight + wave);
        }

        public void Render()
        {
            if (m_Floor == null) return;
            float level = Spec.FloodLevel(m_Time);
            float step = ColumnPx / (float)Art.PPU;
            float left = m_World.View.Rect.xMin;
            for (int i = 0; i < m_Columns.Count; i++)
            {
                float top = Crest(i, level);
                bool shown = level > 0.01f;
                SpriteRenderer col = m_Columns[i], foam = m_Foam[i];
                col.enabled = foam.enabled = shown;
                if (!shown) continue;
                float x = left + (i + 0.5f) * step;
                float floor = m_Floor[i] - 0.1f; // a little into the ground, so no gap shows
                col.transform.localPosition = new Vector3(x, (floor + top) * 0.5f, 0f);
                col.transform.localScale = new Vector3(ColumnPx, Mathf.Max(1f, (top - floor) * Art.PPU), 1f);
                foam.transform.localPosition = new Vector3(x, Entity.Snap(top), 0f);
                foam.transform.localScale = new Vector3(ColumnPx, 2f, 1f);
            }

            // the weasels surf the crest, a ragged line of them, from the presser's back to the far side
            float surf = Spec.SurfProgress(m_Time);
            float width = m_World.View.Rect.width;
            for (int i = 0; i < m_Weasels.Count; i++)
            {
                SpriteRenderer weasel = m_Weasels[i];
                float lag = i * 0.07f;
                float across = Mathf.Clamp01(surf * 1.6f - lag);
                bool riding = surf > 0f && across > 0f && across < 1f && level > 0.5f;
                weasel.enabled = riding;
                if (!riding) continue;
                float x = m_Dir > 0 ? left + across * width : left + (1f - across) * width;
                int col = Mathf.Clamp(Mathf.FloorToInt((x - left) / step), 0, m_Floor.Length - 1);
                float y = Crest(col, level) + Mathf.Abs(Mathf.Sin(m_Time * 14f + i)) * 0.18f;
                weasel.transform.localPosition = new Vector3(Entity.Snap(x), Entity.Snap(y), 0f);
                weasel.transform.localScale = new Vector3(m_Dir, 1f, 1f);
                weasel.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(m_Time * 10f + i * 1.7f) * 12f);
                weasel.sprite = KieArt.Weasel[(int)(m_Time * 16f + i) % 2];
                if (Rng.Fx.Value < 0.15f)
                    m_World.SpawnParticle(new Vector2(x - m_Dir * 0.2f, y), new Vector2(-m_Dir * Rng.Fx.Range(1f, 3f), Rng.Fx.Range(1f, 3f)), Foam, 1f, 0.3f);
            }
        }

        public void Destroy() => Object.Destroy(m_Root.gameObject);
    }
}
