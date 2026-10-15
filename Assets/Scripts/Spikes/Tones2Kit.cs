using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike: tones2's ten-1, the water jet. Hold FIRE and the pressure builds from a wide spray that falls short to
    /// a clean, long arc in about a second (<see cref="JetSpec"/>); let go and it resets. A gauge over his head
    /// shows the pressure, and the hiss rises with it. The droplets are <see cref="Liquids"/>.
    /// </summary>
    public sealed class WaterJet
    {
        readonly Player m_Player;
        readonly JetSpec m_Spec;
        readonly SpriteRenderer m_GaugeBack, m_GaugeFill;
        AudioSource m_Hiss;
        float m_Held, m_Due;

        public WaterJet(Player player, JetSpec spec)
        {
            m_Player = player;
            m_Spec = spec;
            m_GaugeBack = Gauge("Gauge", SortOrder.Effects, new Color(0.11f, 0.08f, 0.12f, 0.85f));
            m_GaugeFill = Gauge("Pressure", SortOrder.Effects + 1, SpikeArt.Water);
        }

        /// <summary>0 to 1: how built up the jet is.</summary>
        public float Pressure { get; private set; }

        SpriteRenderer Gauge(string name, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(m_Player.Go.transform, false); // not the visual: the gauge doesn't flip with facing
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Art.Pixel;
            sr.sortingOrder = order;
            sr.color = color;
            sr.enabled = false;
            return sr;
        }

        public void Tick(float dt, bool firing)
        {
            if (!firing)
            {
                m_Held = 0f;
                m_Due = 0f;
                Pressure = 0f;
                if (m_Hiss != null && m_Hiss.isPlaying) m_Hiss.Stop();
                return;
            }

            m_Held += dt;
            Pressure = m_Spec.Pressure(m_Held);
            float p = Pressure;
            m_Due += dt * m_Spec.DropsPerSecond(p);

            World w = m_Player.World;
            Vector2 aim = m_Player.Aim, origin = m_Player.MuzzlePos;
            float spread = m_Spec.Spread(p), speed = m_Spec.Speed(p), gravity = m_Spec.Gravity(p);
            while (m_Due >= 1f)
            {
                m_Due -= 1f;
                float a = Rng.Fx.Range(-spread, spread) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(aim.x * Mathf.Cos(a) - aim.y * Mathf.Sin(a), aim.x * Mathf.Sin(a) + aim.y * Mathf.Cos(a));
                Vector2 vel = dir * (speed * Rng.Fx.Range(0.9f, 1.05f)) + new Vector2(m_Player.Vel.x * 0.4f, 0f);
                Vector2 at = origin + dir * Rng.Fx.Range(0f, 0.25f);
                w.Liquids.Spray(at, vel, gravity, m_Spec.Damage(p), m_Spec.Push(p));
            }

            if (m_Hiss == null) m_Hiss = Sfx.LoopSource(m_Player.Go, Sfx.JetLoop);
            if (m_Hiss != null)
            {
                if (!m_Hiss.isPlaying) m_Hiss.Play();
                m_Hiss.volume = Sfx.Muted ? 0f : 0.12f + 0.22f * p;
                m_Hiss.pitch = 0.7f + 0.7f * p;
            }
        }

        /// <summary>The pressure gauge over his head, while he's firing.</summary>
        public void Render()
        {
            bool show = Pressure > 0f && !m_Player.Dead;
            m_GaugeBack.enabled = m_GaugeFill.enabled = show;
            if (!show) return;
            const float width = 18f, y = 1.7f; // px wide, units above his feet
            float fill = Mathf.Round(Pressure * (width - 2f));
            m_GaugeBack.transform.localPosition = new Vector3(0f, y, 0f);
            m_GaugeBack.transform.localScale = new Vector3(width, 4f, 1f);
            m_GaugeFill.transform.localPosition = new Vector3((-(width - 2f) * 0.5f + fill * 0.5f) / Art.PPU, y, 0f);
            m_GaugeFill.transform.localScale = new Vector3(fill, 2f, 1f);
            // A flicker of white once the jet is at full pressure.
            m_GaugeFill.color = Pressure >= 0.999f && (int)(Time.unscaledTime * 12f) % 2 == 0 ? Color.white : SpikeArt.Water;
        }
    }

    /// <summary>
    /// Spike: tones2's ten-2, the rocket jump. Press SP on the ground: he shakes on the spot for half a second, then
    /// blasts straight up, about twice as high as a jump, and steers in the air like a normal jump. The launch
    /// carpets the floor in toxic mud that poisons goons who walk through it. Three charges, which come back
    /// one at a time (<see cref="RocketSpec"/>).
    /// </summary>
    public sealed class RocketJump
    {
        readonly Player m_Player;
        readonly RocketSpec m_Spec;
        float m_Shake = -1f, m_Recharge, m_Puff;

        public RocketJump(Player player, RocketSpec spec)
        {
            m_Player = player;
            m_Spec = spec;
            player.Specials = spec.Charges;
        }

        /// <summary>Shaking before the launch: he can't move, jump or fire.</summary>
        public bool Rooted => m_Shake >= 0f;
        /// <summary>Climbing from a launch, so letting go of JUMP doesn't cut it short.</summary>
        public bool Rising { get; private set; }

        /// <summary>How far the shaking moves him this frame.</summary>
        public Vector2 ShakeOffset => Rooted
            ? new Vector2(Mathf.Round(Rng.Fx.Range(-1f, 1f) * (1f + 2f * m_Shake / m_Spec.ShakeTime)) / Art.PPU, 0f)
            : Vector2.zero;

        public void Tick(float dt, bool pressed)
        {
            Player p = m_Player;
            World w = p.World;
            if (p.Specials < m_Spec.Charges)
            {
                m_Recharge += dt;
                if (m_Recharge >= m_Spec.RechargeTime)
                {
                    m_Recharge = 0f;
                    p.Specials++;
                }
            }
            else m_Recharge = 0f;
            if (Rising && p.Vel.y <= 0f) Rising = false;

            if (Rooted)
            {
                m_Shake += dt;
                m_Puff -= dt;
                if (m_Puff <= 0f)
                {
                    m_Puff = 0.05f;
                    w.SpawnParticle(p.Pos + new Vector2(Rng.Fx.Range(-0.3f, 0.3f), 0.1f), new Vector2(Rng.Fx.Range(-2f, 2f), Rng.Fx.Range(1f, 3f)),
                        SpikeArt.Mud, 2f, 0.4f);
                }
                if (m_Shake >= m_Spec.ShakeTime) Launch();
                return;
            }

            if (pressed && p.Specials > 0 && p.Grounded)
            {
                p.Specials--;
                m_Shake = 0f;
                m_Puff = 0f;
                Sfx.Play(Sfx.RocketRumble, 0.8f);
            }
        }

        void Launch()
        {
            Player p = m_Player;
            World w = p.World;
            m_Shake = -1f;
            p.Vel.y = m_Spec.LaunchVelocity;
            Rising = true;
            w.Liquids.SpillMud(p.Pos, m_Spec);
            // The exhaust: a blast of mud, flecked with poison, thrown out along the floor.
            for (int i = 0; i < 18; i++)
            {
                var vel = new Vector2(Rng.Fx.Range(-7f, 7f), Rng.Fx.Range(0.5f, 4f));
                w.SpawnParticle(p.Pos + Vector2.up * 0.1f, vel, i % 4 == 0 ? SpikeArt.Toxic : SpikeArt.Mud, Rng.Fx.Range(1.5f, 3f), Rng.Fx.Range(0.4f, 0.8f));
            }
            Sfx.Play(Sfx.RocketLaunch, 0.9f);
            w.View.Shake(0.12f);
        }
    }
}
