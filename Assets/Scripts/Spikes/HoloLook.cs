using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike: a goon drawn as one of kie's rogue holograms, and its glitch-out death.
    /// <para>
    /// The look: every sprite is swapped for its hologram (<see cref="Art.Holo"/>: greys, scanlines, a bright edge)
    /// and tinted by kind (riflemen cyan, knife runners magenta, drones amber), with a shimmer, and every so often a
    /// glitch: the figure jumps a pixel or two with colour-split ghosts. Getting hit sets one off.
    /// </para>
    /// <para>
    /// The death, instead of toppling over: a white flash, then the figure tears into horizontal bands that jitter
    /// further and further apart in shifting colours, squashes into a bright line like an old TV switching off,
    /// shrinks to a dot and is gone, throwing off a few bits. It costs nothing until a goon dies, and then one
    /// renderer per band for under a second.
    /// </para>
    /// </summary>
    public sealed class HoloLook
    {
        const float FlashEnd = 0.08f, TearEnd = 0.42f, SquashEnd = 0.58f, End = 0.72f;
        const int BandPx = 3;

        static readonly Color Cyan = new Color(0.45f, 1f, 1f), Magenta = new Color(1f, 0.45f, 0.9f), Amber = new Color(1f, 0.82f, 0.35f);
        static readonly Color Poisoned = new Color(0.62f, 1f, 0.2f), Burning = new Color(1f, 0.5f, 0.15f);

        readonly Enemy m_Enemy;
        readonly Color m_Tint;
        readonly float m_Phase;
        SpriteRenderer[] m_Parts, m_Ghosts;
        Sprite[] m_Last; // each part's last hologram sprite, for the death to tear up
        readonly List<SpriteRenderer> m_Bands = new List<SpriteRenderer>();
        float m_GlitchUntil, m_NextGlitch, m_NextJitter; // in world time, so a paused game holds still
        Vector2 m_Jitter;
        bool m_DeathStarted, m_Bits;

        public HoloLook(Enemy enemy)
        {
            m_Enemy = enemy;
            m_Tint = enemy.Def.Brain == EnemyBrain.Runner ? Magenta : enemy.Def.Brain == EnemyBrain.Drone ? Amber : Cyan;
            m_Phase = Rng.Fx.Range(0f, 10f);
            m_NextGlitch = enemy.World.Elapsed + Rng.Fx.Range(0.5f, 2f);
        }

        float Now => m_Enemy.World.Elapsed;

        /// <summary>A hit makes it glitch.</summary>
        public void OnHit() => m_GlitchUntil = Mathf.Max(m_GlitchUntil, Now + 0.12f);

        void Init()
        {
            Transform visual = m_Enemy.VisualRoot;
            m_Parts = visual.GetComponentsInChildren<SpriteRenderer>(true);
            m_Ghosts = new SpriteRenderer[m_Parts.Length];
            m_Last = new Sprite[m_Parts.Length];
            for (int i = 0; i < m_Parts.Length; i++)
            {
                SpriteRenderer part = m_Parts[i];
                var go = new GameObject(part.name + " ghost");
                go.transform.SetParent(part.transform.parent, false);
                SpriteRenderer ghost = go.AddComponent<SpriteRenderer>();
                ghost.sortingOrder = part.sortingOrder - 3;
                ghost.enabled = false;
                m_Ghosts[i] = ghost;
            }
        }

        /// <summary>Runs after the enemy has drawn itself each frame, and redraws it as a hologram.</summary>
        public void Render()
        {
            if (m_Parts == null) Init();
            if (m_Enemy.Dying)
            {
                RenderDeath();
                return;
            }

            float now = Now;
            if (now >= m_NextGlitch)
            {
                m_NextGlitch = now + Rng.Fx.Range(0.8f, 2.6f);
                m_GlitchUntil = now + Rng.Fx.Range(0.06f, 0.16f);
            }
            bool glitching = now < m_GlitchUntil;
            if (glitching && now >= m_NextJitter)
            {
                m_NextJitter = now + 0.04f;
                m_Jitter = new Vector2(Rng.Fx.Range(-2, 3), Rng.Fx.Range(0, 2)) / Art.PPU;
            }
            Transform visual = m_Enemy.VisualRoot;
            visual.localPosition = glitching ? (Vector3)m_Jitter : Vector3.zero;

            Color tint = m_Enemy.BurnTimer > 0f ? Color.Lerp(m_Tint, Burning, 0.75f)
                : m_Enemy.PoisonTimer > 0f ? Color.Lerp(m_Tint, Poisoned, 0.75f) : m_Tint;
            // A steady shimmer, and the odd dropout while glitching.
            tint.a = glitching && Rng.Fx.Value < 0.3f ? 0.35f : 0.8f + 0.12f * Mathf.Sin(now * 23f + m_Phase);
            for (int i = 0; i < m_Parts.Length; i++)
            {
                SpriteRenderer part = m_Parts[i], ghost = m_Ghosts[i];
                Sprite holo = Art.Holo(part.sprite);
                if (holo != part.sprite) m_Last[i] = holo; // a hit's white flash has no hologram: it stays white
                part.sprite = holo;
                part.color = tint;
                ghost.enabled = glitching && part.enabled;
                if (!ghost.enabled) continue;
                ghost.sprite = part.sprite;
                ghost.transform.localPosition = part.transform.localPosition + new Vector3((i % 2 == 0 ? 2f : -2f) / Art.PPU, 0f, 0f);
                ghost.color = i % 2 == 0 ? new Color(1f, 0.2f, 0.6f, 0.5f) : new Color(0.2f, 0.9f, 1f, 0.5f);
            }
        }

        /// <summary>Called each frame of the death with how long it's been. True once it's over.</summary>
        public bool TickDying(float time)
        {
            if (!m_DeathStarted)
            {
                m_DeathStarted = true;
                Sfx.Play(Sfx.Glitch, 0.55f);
            }
            if (time >= SquashEnd && !m_Bits)
            {
                m_Bits = true;
                World w = m_Enemy.World;
                Vector2 at = m_Enemy.Center;
                for (int i = 0; i < 10; i++)
                    w.SpawnParticle(at + new Vector2(Rng.Fx.Range(-0.3f, 0.3f), 0f), new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(2f, 8f)),
                        i % 3 == 0 ? Color.white : m_Tint, Rng.Fx.Range(1f, 2f), Rng.Fx.Range(0.3f, 0.6f));
            }
            return time >= End;
        }

        void BuildBands()
        {
            for (int i = 0; i < m_Parts.Length; i++)
            {
                SpriteRenderer part = m_Parts[i];
                bool shown = part.enabled;
                part.enabled = false;
                m_Ghosts[i].enabled = false;
                Sprite holo = m_Last[i] ?? Art.Holo(part.sprite);
                if (!shown || holo == null) continue;
                foreach (Sprite band in SpikeArt.Bands(holo, BandPx))
                {
                    var go = new GameObject("Band");
                    go.transform.SetParent(part.transform.parent, false);
                    go.transform.localPosition = part.transform.localPosition;
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = band;
                    sr.sortingOrder = part.sortingOrder;
                    m_Bands.Add(sr);
                }
            }
        }

        void RenderDeath()
        {
            if (m_Bands.Count == 0) BuildBands();
            float t = m_Enemy.DyingSeconds;
            Transform visual = m_Enemy.VisualRoot;
            float facing = m_Enemy.Facing, h = m_Enemy.Height;
            bool newJitter = Now >= m_NextJitter;
            if (newJitter) m_NextJitter = Now + 0.035f;

            Vector3 scale = new Vector3(facing, 1f, 1f);
            float lift = 0f;
            if (t >= TearEnd)
            {
                // Squash into a bright line (like an old TV switching off), then shrink to a dot.
                float k = Mathf.Clamp01((t - TearEnd) / (SquashEnd - TearEnd));
                float sy = Mathf.Lerp(1f, 0.06f, k), sx = 1f + 0.8f * k;
                if (t >= SquashEnd) sx *= Mathf.Lerp(1f, 0.05f, Mathf.Clamp01((t - SquashEnd) / (End - SquashEnd)));
                scale = new Vector3(facing * sx, sy, 1f);
                lift = h * 0.5f * (1f - sy); // squash toward the middle, not the feet
            }
            visual.localScale = scale;
            visual.localPosition = new Vector3(0f, lift, 0f);

            float tear = t < FlashEnd ? 0f : Mathf.Clamp01((t - FlashEnd) / (TearEnd - FlashEnd));
            for (int i = 0; i < m_Bands.Count; i++)
            {
                SpriteRenderer band = m_Bands[i];
                if (t < FlashEnd || t >= TearEnd)
                {
                    band.enabled = true;
                    band.color = Color.white;
                    SetBandX(band, 0f);
                    continue;
                }
                if (!newJitter) continue;
                // Bands tear further apart as it goes, flicking between colours, some dropping out.
                float amp = Mathf.Lerp(1f, 5f, tear);
                SetBandX(band, Mathf.Round(Rng.Fx.Range(-amp, amp)) / Art.PPU);
                band.enabled = Rng.Fx.Value > 0.15f + 0.3f * tear;
                float pick = Rng.Fx.Value;
                band.color = pick < 0.5f ? m_Tint : pick < 0.75f ? new Color(1f, 0.25f, 0.7f) : Color.white;
            }
        }

        static void SetBandX(SpriteRenderer band, float x)
        {
            Vector3 p = band.transform.localPosition;
            band.transform.localPosition = new Vector3(x, p.y, p.z);
        }
    }
}
