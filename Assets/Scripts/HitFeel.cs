using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Hits that land with weight, on top of the knockback and hit-stop (from the hit-feel spike, on for everyone
    /// since 2026-10-16, co-op too). Sparks fly back from every shot that lands on a goon, more of them for a heavy
    /// round, and from a partner's shots too (without the thump); tones2's jet and spew fizz on what they soak; a
    /// kill throws out a star of them; a big kill or a big blast flashes the screen white; and when you're hurt
    /// (or your tank is) the screen flashes red and the lost health floats off you, counted out of 100 as in Pooket
    /// Tabks (a heart is 20). Flashes are rationed, so a stream of blasts (a tank's cannon, a rocket barrage)
    /// doesn't strobe.
    /// </summary>
    public sealed class HitFeel
    {
        static readonly Color Hot = new Color(1f, 0.95f, 0.6f), Gold = new Color(1f, 0.75f, 0.2f);
        static readonly Color Pain = new Color(1f, 0.15f, 0.1f), Numbers = new Color(1f, 0.3f, 0.25f);

        /// <summary>The faintest a blast's flash gets drawn, the brightest, and the quiet gap after a white flash.</summary>
        const float BlastFlashMin = 0.12f, BlastFlashMax = 0.32f, WhiteGap = 0.3f;
        /// <summary>The least time between fizzes from a soaking stream.</summary>
        const float FizzGap = 0.08f;

        readonly World m_World;
        readonly SpriteRenderer m_Flash;
        float m_FlashLeft, m_FlashTime, m_FlashAlpha, m_NextWhite, m_NextFizz;

        public HitFeel(World world)
        {
            m_World = world;
            var go = new GameObject("Screen flash");
            go.transform.SetParent(world.Root, false);
            m_Flash = go.AddComponent<SpriteRenderer>();
            m_Flash.sprite = Art.Pixel;
            m_Flash.sortingOrder = SortOrder.Effects + 50;
            m_Flash.enabled = false;
        }

        /// <summary>
        /// A shot landed on a goon at <paramref name="at"/>, flying at <paramref name="vel"/>: ours, or a partner's
        /// (<paramref name="ours"/> false: the sparks, without the thump and the shake).
        /// </summary>
        public void Spark(Vector2 at, Vector2 vel, bool heavy, bool ours = true)
        {
            Vector2 back = vel.sqrMagnitude > 0.01f ? -vel.normalized : Vector2.up;
            int count = heavy ? 7 : 3;
            for (int i = 0; i < count; i++)
            {
                float a = Rng.Fx.Range(-55f, 55f) * Mathf.Deg2Rad;
                var dir = new Vector2(back.x * Mathf.Cos(a) - back.y * Mathf.Sin(a), back.x * Mathf.Sin(a) + back.y * Mathf.Cos(a));
                m_World.SpawnParticle(at, dir * Rng.Fx.Range(4f, heavy ? 11f : 8f) + Vector2.up * 2f, i % 2 == 0 ? Hot : Gold,
                    heavy ? 2f : 1.5f, Rng.Fx.Range(0.12f, 0.25f));
            }
            if (heavy && ours)
            {
                Sfx.Play(Sfx.Thump, 0.45f);
                m_World.View.Shake(0.06f);
            }
        }

        /// <summary>A stream's droplet soaked into a goon at <paramref name="at"/> (tones2's jet and spew): a little fizz of hot sparks, rationed.</summary>
        public void Fizz(Vector2 at)
        {
            float now = Time.unscaledTime;
            if (now < m_NextFizz) return;
            m_NextFizz = now + FizzGap;
            for (int i = 0; i < 2; i++)
                m_World.SpawnParticle(at, new Vector2(Rng.Fx.Range(-4f, 4f), Rng.Fx.Range(3f, 7f)), i == 0 ? Hot : Gold, 1.5f, Rng.Fx.Range(0.1f, 0.18f));
        }

        /// <summary>Our soldier killed <paramref name="e"/>: a star of sparks, and for a big one a white flash.</summary>
        public void Kill(Enemy e)
        {
            bool big = e.MaxHp >= Tuning.Impact.BigHp;
            int count = big ? 18 : 10;
            for (int i = 0; i < count; i++)
            {
                float a = (i + Rng.Fx.Range(0f, 0.5f)) / count * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                m_World.SpawnParticle(e.Center, dir * Rng.Fx.Range(6f, big ? 14f : 10f), i % 3 == 0 ? Color.white : Hot, 2f, Rng.Fx.Range(0.15f, 0.3f));
            }
            Sfx.Play(Sfx.Thump, big ? 0.9f : 0.55f);
            if (big) White(0.45f, 0.12f);
        }

        /// <summary>A blast of <paramref name="size"/> went off: a big one flashes the screen.</summary>
        public void Blast(float size)
        {
            if (size >= 1.5f) White(Mathf.Clamp(0.12f * size, BlastFlashMin, BlastFlashMax), 0.08f);
        }

        /// <summary>Our soldier lost <paramref name="damage"/> health (in hearts).</summary>
        public void Hurt(Player p, int damage)
        {
            int out100 = Mathf.RoundToInt(100f * damage / Mathf.Max(1, p.MaxHp));
            m_World.Scoring.Popup("-" + out100, p.Pos + new Vector2(Rng.Fx.Range(-0.3f, 0.3f), p.Height + 0.4f), Numbers, 14);
            Flash(Pain, 0.28f, 0.14f);
        }

        /// <summary>A white flash, unless one went off a moment ago.</summary>
        void White(float alpha, float seconds)
        {
            float now = Time.unscaledTime;
            if (now < m_NextWhite) return;
            m_NextWhite = now + WhiteGap;
            Flash(Color.white, alpha, seconds);
        }

        void Flash(Color color, float alpha, float seconds)
        {
            if (m_FlashLeft > 0f && m_FlashAlpha * (m_FlashLeft / m_FlashTime) > alpha) return; // a stronger flash is already showing
            color.a = alpha;
            m_Flash.color = color;
            m_FlashAlpha = alpha;
            m_FlashLeft = m_FlashTime = seconds;
        }

        /// <summary>Every frame, even through a hit-stop: the flash covers the screen and fades.</summary>
        public void Render()
        {
            m_Flash.enabled = m_FlashLeft > 0f;
            if (!m_Flash.enabled) return;
            m_FlashLeft -= Time.unscaledDeltaTime;
            Rect view = m_World.View.Rect;
            m_Flash.transform.localPosition = new Vector3(view.center.x, view.center.y, 0f);
            m_Flash.transform.localScale = new Vector3((view.width + 4f) * Art.PPU, (view.height + 4f) * Art.PPU, 1f);
            Color c = m_Flash.color;
            c.a = m_FlashAlpha * Mathf.Clamp01(m_FlashLeft / m_FlashTime);
            m_Flash.color = c;
        }
    }
}
