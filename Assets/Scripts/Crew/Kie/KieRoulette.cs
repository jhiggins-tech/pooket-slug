using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kie's roulette on an enemy (his tank, <see cref="KieBoss"/>, and his pilot, <see cref="Pilot"/>): a wheel spins over
    /// it and steals our soldier's special's charges (or the super's charge). Hit its holder hard enough and it drops
    /// what it stole (<see cref="KieLoot"/>) to grab back.
    /// <para>
    /// Co-op: the authority runs it, and it rides in the boss's network object (<see cref="NetBossState.KieState"/>).
    /// Every screen steals from its own soldier and drops its own soldier's loot, so the wheel takes from everyone
    /// at once.
    /// </para>
    /// </summary>
    public sealed class KieRoulette
    {
        static KieTankSpec Spec => Content.KieTank;

        readonly World m_World;
        readonly Enemy m_Host;
        readonly float m_Height;
        readonly SpriteRenderer m_Frame, m_Icon, m_BarBack, m_BarFill;
        float m_Shown;

        // as the authority runs it, and as every screen has seen it
        int m_Phase, m_Damage, m_Drops, m_SeenPhase, m_SeenDrops;
        float m_Spin, m_LocalSpin = -1f, m_Progress, m_Fury = 1f;
        // what this screen's soldier lost
        KieSteal m_Pick, m_Holding;
        int m_Held;

        /// <param name="height">How high over its holder's feet the wheel turns.</param>
        public KieRoulette(World world, Enemy host, float height)
        {
            m_World = world;
            m_Host = host;
            m_Height = height;
            Transform root = host.Go.transform; // not its visual, so a hologram look leaves the wheel alone
            m_Frame = Renderer(root, "Roulette", KieArt.Frame, SortOrder.Effects);
            m_Icon = Renderer(root, "Roulette icon", KieArt.Special, SortOrder.Effects + 1);
            m_BarBack = Renderer(root, "Loot bar", Art.Pixel, SortOrder.Effects);
            m_BarBack.color = new Color(0.11f, 0.08f, 0.12f, 0.85f);
            m_BarFill = Renderer(root, "Loot", Art.Pixel, SortOrder.Effects + 1);
            m_BarFill.color = Palette.Gold;
        }

        static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, int order) => Art.Renderer(parent, name, sprite, order, shown: false);

        /// <summary>Authority: spinning or holding what it stole.</summary>
        public bool Busy => m_Phase != 0;

        /// <summary>How much quicker it spins (its holder's rage).</summary>
        public float Fury
        {
            set => m_Fury = Mathf.Max(0.1f, value);
        }

        /// <summary>Seconds a spin takes now.</summary>
        public float SpinTime => Spec.SpinTime / m_Fury;

        /// <summary>Authority: how close it is to dropping what it stole, 0 to 1.</summary>
        public float Progress => m_Phase == KieTankSpec.RouletteHolding ? Mathf.Clamp01(m_Damage / (float)Spec.DropDamage(m_Host.MaxHp)) : 0f;

        /// <summary>Authority: times it has dropped what it stole.</summary>
        public int Drops => m_Drops;

        /// <summary>Authority: its state for the network object, with whether a landing marker is shown.</summary>
        public int NetState(bool markerShown) => m_Phase | (markerShown ? 0 : KieTankSpec.NoMarker);

        /// <summary>Follower: whether the authority's tank shows its landing marker.</summary>
        public static bool MarkerShown(int netState) => (netState & KieTankSpec.NoMarker) == 0;

        /// <summary>Authority: the wheel starts to spin.</summary>
        public void Start()
        {
            m_Phase = KieTankSpec.RouletteSpinning;
            m_Spin = 0f;
        }

        /// <summary>
        /// Every screen: follow the roulette as the authority runs it. A spin starting picks what to take from our
        /// soldier; holding takes it; a drop gives it back as loot to grab.
        /// </summary>
        public void ApplyNet(int state, float progress, int drops)
        {
            int phase = state & KieTankSpec.RoulettePhase;
            m_Progress = progress;
            if (phase != m_SeenPhase)
            {
                if (phase == KieTankSpec.RouletteSpinning) StartSpin();
                else if (phase == KieTankSpec.RouletteHolding) Steal();
                else m_LocalSpin = -1f;
                m_SeenPhase = phase;
            }
            if (drops != m_SeenDrops)
            {
                m_SeenDrops = drops;
                Drop();
            }
        }

        Vector2 Over => m_Host.Pos + Vector2.up * (m_Height + 0.9f);

        void StartSpin()
        {
            Player p = m_World.Player;
            m_Pick = KieTankSpec.Pick(p.Specials, p.Kit.SuperCharge, Rng.Fx.Value);
            m_LocalSpin = 0f;
            m_World.Scoring.Popup("ROULETTE!", Over, new Color(1f, 0.6f, 0.9f));
        }

        void Steal()
        {
            m_LocalSpin = -1f;
            Player p = m_World.Player;
            if (p.IsDown) return;
            Vector2 over = p.Pos + Vector2.up * 2.6f;
            int amount = m_Pick == KieSteal.Special ? p.Specials : m_Pick == KieSteal.Super ? Mathf.RoundToInt(p.Kit.SuperCharge * p.Kit.SuperKills) : 0;
            if (amount <= 0)
            {
                m_World.Scoring.Popup("EMPTY POCKETS!", over, new Color(0.8f, 0.8f, 0.85f));
                return;
            }
            if (m_Pick == KieSteal.Special) p.Specials = 0;
            else amount = p.Kit.TakeSuper();
            m_Holding = m_Pick;
            m_Held = amount;
            m_World.Scoring.Popup(m_Pick == KieSteal.Special ? p.Def.Special.Hud + " STOLEN!" : "SUPER STOLEN!", over, Palette.Pink);
            Sfx.Play(Sfx.Glitch, 0.7f);
            Sfx.Play(Sfx.Pickup, 0.6f);
            // the loot streams off you into its holder
            Vector2 to = m_Host.Pos + Vector2.up * m_Height;
            for (int i = 0; i < 16; i++)
            {
                Vector2 from = p.Center + new Vector2(Rng.Fx.Range(-0.3f, 0.3f), Rng.Fx.Range(-0.3f, 0.3f));
                float t = Rng.Fx.Range(0.35f, 0.6f);
                var vel = (to - from) / t + Vector2.up * Rng.Fx.Range(-1f, 3f);
                m_World.SpawnParticle(from, vel, i % 2 == 0 ? Palette.Pink : Palette.Gold, Rng.Fx.Range(1.5f, 2.5f), t);
            }
        }

        void Drop()
        {
            if (m_Holding == KieSteal.Nothing) return;
            float side = m_World.Player.Pos.x < m_Host.Pos.x ? -1f : 1f;
            m_World.Props.Add(new KieLoot(m_World, m_Host.Pos + Vector2.up * (m_Height + 0.3f), new Vector2(side * 3.5f, 9f), m_Holding, m_Held));
            m_World.Scoring.Popup("DROPPED IT!", Over, Palette.Gold);
            Sfx.Play(Sfx.Thump, 0.8f);
            m_Holding = KieSteal.Nothing;
            m_Held = 0;
        }

        /// <summary>Each tick of its holder (on a follower too); the authority also runs the wheel.</summary>
        public void Tick(float dt, bool markerShown = true)
        {
            m_Shown += dt;
            if (m_LocalSpin >= 0f) m_LocalSpin += dt;
            if (!m_World.IsAuthority) return;
            if (m_Phase == KieTankSpec.RouletteSpinning)
            {
                m_Spin += dt;
                if (m_Spin >= SpinTime)
                {
                    m_Phase = KieTankSpec.RouletteHolding;
                    m_Damage = 0;
                }
            }
            ApplyNet(NetState(markerShown), Progress, m_Drops); // our own screen follows it like a partner's would
        }

        /// <summary>Authority: its holder took <paramref name="damage"/>; enough and it drops what it stole.</summary>
        public void OnHurt(int damage)
        {
            if (m_Phase != KieTankSpec.RouletteHolding) return;
            m_Damage += damage;
            if (m_Damage < Spec.DropDamage(m_Host.MaxHp)) return;
            m_Phase = 0;
            m_Drops++;
            ApplyNet(NetState(true), 0f, m_Drops);
        }

        /// <summary>Authority: its holder is gone, and anything it stole falls out.</summary>
        public void OnDie()
        {
            if (m_Phase == KieTankSpec.RouletteHolding) m_Drops++;
            m_Phase = 0;
            ApplyNet(0, 0f, m_Drops);
        }

        public void Render()
        {
            bool spinning = m_LocalSpin >= 0f, holding = m_Holding != KieSteal.Nothing;
            bool shown = !m_Host.Dying && (spinning || holding);
            m_Frame.enabled = m_Icon.enabled = shown;
            m_BarBack.enabled = m_BarFill.enabled = shown && holding;
            if (!shown) return;

            float y = m_Height;
            float bob = holding ? Mathf.Round(Mathf.Sin(m_Shown * 4f) * 1.5f) / Art.PPU : 0f;
            m_Frame.transform.localPosition = m_Icon.transform.localPosition = new Vector3(0f, y + bob, 0f);
            KieSteal face;
            if (spinning)
            {
                // flicks between the two, slowing, and settles on what it'll take for the last part of the spin
                float t = m_LocalSpin / SpinTime;
                face = t > 0.8f ? m_Pick : ((int)(Mathf.Sqrt(t) * 14f) % 2 == 0 ? KieSteal.Special : KieSteal.Super);
                if (face == KieSteal.Nothing) face = (int)(m_LocalSpin * 8f) % 2 == 0 ? KieSteal.Special : KieSteal.Super;
                m_Frame.color = t > 0.8f && (int)(m_LocalSpin * 16f) % 2 == 0 ? Color.white : Palette.Pink;
            }
            else
            {
                face = m_Holding;
                m_Frame.color = Palette.Pink;
            }
            m_Icon.sprite = face == KieSteal.Super ? KieArt.Super : KieArt.Special;
            if (!holding) return;

            // how close it is to dropping it
            const float width = 18f;
            float fill = Mathf.Round(m_Progress * (width - 2f));
            float barY = y - 0.75f + bob;
            m_BarBack.transform.localPosition = new Vector3(0f, barY, 0f);
            m_BarBack.transform.localScale = new Vector3(width, 4f, 1f);
            m_BarFill.transform.localPosition = new Vector3((-(width - 2f) * 0.5f + fill * 0.5f) / Art.PPU, barY, 0f);
            m_BarFill.transform.localScale = new Vector3(fill, 2f, 1f);
        }
    }
}
