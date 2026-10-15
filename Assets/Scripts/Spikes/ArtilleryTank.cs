using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike: the boss tank as artillery, its own entity apart from the tank you drive (<c>docs/design/tanks.md</c>,
    /// Two kinds of tank). It fights like a Pocket Tanks tank: it drives a short way along its arena, stops, swings
    /// its turret round to the angle it wants, then charges the shot while a meter fills over it and a marker
    /// shows where the shell will land, and lobs it. Every third shot is a spread of three. The turret can swing
    /// right over its back, so getting behind it doesn't make you safe. It gets quicker as it's hurt
    /// (<see cref="BossTankSpec"/>). Solo only, like every spike: co-op still gets the old tank.
    /// </summary>
    public sealed class ArtilleryTank : BossEnemy
    {
        enum State { Drive, Aim, Charge, Recoil }

        static readonly Color MeterBack = new Color(0.11f, 0.08f, 0.12f, 0.85f), MeterFill = new Color(1f, 0.55f, 0.15f);

        readonly BossTankSpec m_Spec;
        readonly SpriteRenderer m_Hull, m_Barrel, m_MeterBack, m_MeterFill, m_Marker;
        readonly Transform m_BarrelPivot;
        readonly float m_HomeX;
        State m_State = State.Recoil;
        float m_StateTime, m_Timer = 1.2f, m_DriveTo, m_Angle = 150f, m_WantAngle = 150f, m_Power, m_Kick, m_ServoTimer, m_ExplodeTimer;
        int m_Shots;
        Vector2 m_Landing;
        bool m_LandingSeen;

        public ArtilleryTank(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Spec = SpikeContent.BossTank;
            m_HomeX = feet.x;
            Facing = -1; // the art faces left already; keep Visual unflipped
            m_Hull = AddRenderer(Visual, "Hull", SpikeArt.TankHull, SortOrder.Boss);
            var pivot = new GameObject("Turret");
            m_BarrelPivot = pivot.transform;
            m_BarrelPivot.SetParent(Visual, false);
            m_BarrelPivot.localPosition = SpikeArt.BarrelPivot;
            m_Barrel = AddRenderer(m_BarrelPivot, "Barrel", SpikeArt.TankBarrel, SortOrder.Boss - 1);
            m_MeterBack = Bar("Meter", SortOrder.Effects, MeterBack);
            m_MeterFill = Bar("Power", SortOrder.Effects + 1, MeterFill);
            var marker = new GameObject("Landing marker");
            marker.transform.SetParent(Go.transform, false); // goes with the tank; placed in the world each frame
            m_Marker = marker.AddComponent<SpriteRenderer>();
            m_Marker.sprite = SpikeArt.LandingMarker;
            m_Marker.sortingOrder = SortOrder.Effects;
            m_Marker.enabled = false;
        }

        SpriteRenderer Bar(string name, int order, Color color)
        {
            SpriteRenderer sr = AddRenderer(Go.transform, name, Art.Pixel, order);
            sr.color = color;
            sr.enabled = false;
            return sr;
        }

        float Rage => m_Spec.Rage(Hp / (float)MaxHp);

        /// <summary>Where the barrel's tip is, in the world, at its current angle.</summary>
        Vector2 Muzzle
        {
            get
            {
                float a = m_Angle * Mathf.Deg2Rad;
                return Pos + SpikeArt.BarrelPivot + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (20f / Art.PPU);
            }
        }

        void Enter(State s)
        {
            m_State = s;
            m_StateTime = 0f;
        }

        protected override void TickAlive(float dt)
        {
            if (!Engaged)
            {
                if (World.View.Right >= Pos.x + HalfW + 0.5f) Engaged = true;
                else return;
            }

            float rage = Rage;
            m_StateTime += dt;
            m_Kick = Mathf.MoveTowards(m_Kick, 0f, dt * 6f);
            Vel.x = 0f;
            switch (m_State)
            {
                case State.Drive:
                    Vel.x = Mathf.Clamp(m_DriveTo - Pos.x, -1f, 1f) * m_Spec.DriveSpeed * rage;
                    if (Mathf.Abs(m_DriveTo - Pos.x) < 0.1f || m_StateTime >= m_Spec.DriveTime) Aim();
                    break;
                case State.Aim:
                    m_Angle = Mathf.MoveTowards(m_Angle, m_WantAngle, m_Spec.TurnRate * rage * dt);
                    m_ServoTimer -= dt;
                    if (m_ServoTimer <= 0f)
                    {
                        m_ServoTimer = 0.11f;
                        Sfx.Play(Sfx.Servo, 0.25f);
                    }
                    if (Mathf.Approximately(m_Angle, m_WantAngle)) StartCharge();
                    break;
                case State.Charge:
                    if (m_StateTime >= m_Spec.ChargeTime / rage) Fire();
                    break;
                case State.Recoil:
                    m_Timer -= dt;
                    if (m_Timer <= 0f && !Target.IsDown) Drive();
                    break;
            }

            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveWithTiles(dt);
            World.Combat.DamageAvatarsTouching(Bounds, 1);
        }

        void Drive()
        {
            float half = m_Spec.PatrolHalfWidth;
            m_DriveTo = m_HomeX + World.Rng.Range(-half, half);
            Enter(State.Drive);
        }

        /// <summary>Picks the shot: a lob that lands on the target, and the turret angle that throws it.</summary>
        void Aim()
        {
            Vector2 from = Pos + SpikeArt.BarrelPivot, to = Target.Pos;
            float dx = to.x - from.x, dy = to.y - from.y;
            float g = SpikeContent.BigShell.Gravity;
            float lob = World.Rng.Range(m_Spec.LowLob, m_Spec.HighLob);
            float power = BossTankSpec.PowerFor(dx, dy, lob, g);
            if (float.IsNaN(power) || power > m_Spec.MaxPower) power = m_Spec.MaxPower; // out of reach: it falls short
            m_Power = power * (1f + World.Rng.Range(-m_Spec.PowerError, m_Spec.PowerError));
            float angle = dx < 0f ? 180f - lob : lob; // over its back if you're behind it
            m_WantAngle = Mathf.Clamp(angle, m_Spec.MinAngle, m_Spec.MaxAngle);
            m_ServoTimer = 0f;
            Enter(State.Aim);
        }

        void StartCharge()
        {
            m_Landing = Predict(Muzzle, Velocity(m_Angle, m_Power));
            m_LandingSeen = true;
            Sfx.Play(Sfx.Charge, 0.5f);
            Enter(State.Charge);
        }

        static Vector2 Velocity(float angle, float power)
        {
            float a = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * power;
        }

        /// <summary>Where a shell thrown from <paramref name="at"/> at <paramref name="vel"/> comes down.</summary>
        Vector2 Predict(Vector2 at, Vector2 vel)
        {
            const float step = 1f / 60f;
            float g = SpikeContent.BigShell.Gravity;
            for (int i = 0; i < 600; i++)
            {
                vel.y -= g * step;
                Vector2 next = at + vel * step;
                if (World.Level.IsSolidAt(next) || next.y < 0f) return new Vector2(next.x, Mathf.Ceil(next.y - 0.01f));
                at = next;
            }
            return at;
        }

        void Fire()
        {
            m_Shots++;
            bool spread = m_Spec.SpreadEvery > 0 && m_Shots % m_Spec.SpreadEvery == 0;
            Vector2 muzzle = Muzzle;
            WeaponDef cannon = SpikeContent.BossCannon;
            if (spread)
            {
                for (int i = -1; i <= 1; i++)
                    World.Combat.SpawnEnemyShot(cannon, muzzle, Velocity(m_Angle, m_Power * (1f + i * m_Spec.SpreadPower)));
            }
            else World.Combat.SpawnEnemyShot(cannon, muzzle, Velocity(m_Angle, m_Power));
            Sfx.Play(Sfx.Cannon, 0.9f);
            m_Kick = 1f;
            m_LandingSeen = false;
            m_Timer = m_Spec.RecoilTime / Rage;
            Enter(State.Recoil);
        }

        protected override void Die()
        {
            base.Die();
            m_LandingSeen = false;
            World.BossDefeatedPending = true;
        }

        protected override void TickDying(float dt)
        {
            m_ExplodeTimer -= dt;
            if (m_ExplodeTimer <= 0f && DyingTime < 1.6f)
            {
                m_ExplodeTimer = 0.18f;
                var p = Pos + new Vector2(Rng.Fx.Range(-1.4f, 1.4f), Rng.Fx.Range(0.2f, 1.6f));
                World.Combat.Explode(p, 0.5f, 0, false, 0.7f);
            }
            if (DyingTime >= 1.6f)
            {
                World.Combat.Explode(Center, 1.5f, 0, false, 1.8f);
                World.Burst(Center, 30, new Color(0.35f, 0.4f, 0.2f));
                Sfx.Play(Sfx.BigExplosion);
                World.OnBossDestroyed();
                Remove();
            }
        }

        public override void Render()
        {
            Go.transform.localPosition = new Vector3(Snap(Pos.x), Snap(Pos.y), 0f);
            Visual.localScale = Vector3.one;
            float rumble = !Dying && Mathf.Abs(Vel.x) > 0.05f && ((int)(Anim * 16f) % 2 == 0) ? 1f / Art.PPU : 0f;
            m_Hull.transform.localPosition = new Vector3(0f, rumble, 0f);
            m_Hull.sprite = Flashed(SpikeArt.TankHull);
            m_Barrel.sprite = Flashed(SpikeArt.TankBarrel);

            // The barrel sprite points left; turn it to the angle, and kick it back along itself when it fires.
            m_BarrelPivot.localRotation = Quaternion.Euler(0f, 0f, m_Angle - 180f);
            m_BarrelPivot.localPosition = (Vector3)SpikeArt.BarrelPivot + new Vector3(0f, rumble, 0f);
            m_Barrel.transform.localPosition = new Vector3(m_Kick * 4f / Art.PPU, 0f, 0f);

            bool charging = m_State == State.Charge && !Dying;
            float charge = charging ? Mathf.Clamp01(m_StateTime * Rage / m_Spec.ChargeTime) : 0f;
            // The barrel glows hotter as the shot charges.
            m_Barrel.color = charging ? Color.Lerp(Color.white, new Color(1f, 0.55f, 0.3f), charge * (0.6f + 0.4f * Mathf.Sin(Anim * 40f))) : Color.white;

            m_MeterBack.enabled = m_MeterFill.enabled = charging;
            if (charging)
            {
                const float width = 30f, y = 2.4f; // px wide, units above its feet
                float full = Mathf.Clamp01(m_Power / m_Spec.MaxPower) * (width - 2f);
                float fill = Mathf.Round(full * charge);
                m_MeterBack.transform.localPosition = new Vector3(0f, y, 0f);
                m_MeterBack.transform.localScale = new Vector3(width, 5f, 1f);
                m_MeterFill.transform.localPosition = new Vector3((-(width - 2f) * 0.5f + fill * 0.5f) / Art.PPU, y, 0f);
                m_MeterFill.transform.localScale = new Vector3(fill, 3f, 1f);
            }

            if (Dying) m_Hull.color = m_Barrel.color = Color.Lerp(Color.white, new Color(0.35f, 0.3f, 0.3f), DyingTime / 1.5f);

            // The landing marker blinks faster as the shot gets close.
            m_Marker.enabled = m_LandingSeen && charging && ((int)(Anim * Mathf.Lerp(6f, 20f, charge)) % 2 == 0);
            if (m_Marker.enabled) m_Marker.transform.localPosition = new Vector3(Snap(m_Landing.x) - Snap(Pos.x), Snap(m_Landing.y) - Snap(Pos.y), 0f);
        }
    }
}
