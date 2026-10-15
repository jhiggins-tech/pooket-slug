using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Mission one's boss: a tank that fights as artillery, its own entity apart from the tank you drive (<c>docs/design/tanks.md</c>,
    /// Two kinds of tank). It fights like a Pocket Tanks tank: it drives a short way along its arena, stops, swings
    /// its turret round to the angle it wants, then charges the shot while a meter fills over it and a marker
    /// shows where the shell will land, and lobs it. Every third shot is a spread of three. The turret can swing
    /// right over its back, so getting behind it doesn't make you safe. It gets quicker as it's hurt
    /// (<see cref="BossTankSpec"/>, <see cref="Content.BossTank"/>).
    ///
    /// In co-op the authority runs it. Its shells go out as enemy shots, and its turret, charge and shots go in its
    /// network object (<see cref="NetBossState"/>), so a follower's tank swings, charges, shows the marker and kicks
    /// along with it.
    ///
    /// It's kie's tank: its charged shots cycle through weasels, hologram copies and a roulette that steals
    /// (<see cref="KieBoss"/>). The copies are tanks of this kind too (<see cref="Content.TankCopy"/>, made with
    /// <c>decoy</c>): they stand where kie's trick put them and copy the real one's every move, turret, charge and shot.
    /// </summary>
    public sealed class TankBoss : BossEnemy
    {
        enum State { Drive, Aim, Charge, Recoil, Entrance }

        static readonly Color MeterBack = new Color(0.11f, 0.08f, 0.12f, 0.85f), MeterFill = new Color(1f, 0.55f, 0.15f);

        readonly BossTankSpec m_Spec;
        readonly SpriteRenderer m_Hull, m_Barrel, m_MeterBack, m_MeterFill, m_Marker;
        readonly Transform m_BarrelPivot;
        readonly KieBoss m_Kie;
        float m_HomeX;
        State m_State = State.Recoil;
        float m_StateTime, m_Timer = 1.2f, m_DriveTo, m_Angle = 150f, m_WantAngle = 150f, m_Power, m_Charge, m_Kick, m_ServoTimer, m_ExplodeTimer;
        int m_Shots, m_SeenShots = -1;
        Vector2 m_Landing;
        bool m_LandingSeen, m_SeenFire, m_FightStarted;

        /// <summary>A hologram copy of kie's tank: it moves and fires with the real one, but can't be hurt.</summary>
        public readonly bool Decoy;

        public TankBoss(World world, EnemyDef def, Vector2 feet, bool decoy = false) : base(world, def, feet)
        {
            Decoy = decoy;
            m_Spec = Content.BossTank;
            m_HomeX = feet.x;
            Facing = -1; // the art faces left already; keep Visual unflipped
            m_Hull = AddRenderer(Visual, "Hull", Art.TankHull, SortOrder.Boss);
            var pivot = new GameObject("Turret");
            m_BarrelPivot = pivot.transform;
            m_BarrelPivot.SetParent(Visual, false);
            m_BarrelPivot.localPosition = Art.TankBarrelPivot;
            m_Barrel = AddRenderer(m_BarrelPivot, "Barrel", Art.TankBarrel, SortOrder.Boss - 1);
            m_MeterBack = Bar("Meter", SortOrder.Effects, MeterBack);
            m_MeterFill = Bar("Power", SortOrder.Effects + 1, MeterFill);
            var marker = new GameObject("Landing marker");
            marker.transform.SetParent(Go.transform, false); // goes with the tank; placed in the world each frame
            m_Marker = marker.AddComponent<SpriteRenderer>();
            m_Marker.sprite = Art.LandingMarker;
            m_Marker.sortingOrder = SortOrder.Effects;
            m_Marker.enabled = false;
            if (decoy) Engaged = true;
            else m_Kie = new KieBoss(world, this); // on every screen in co-op: the roulette and the disguise are drawn on each
            if (!decoy && !world.QuickBossIntro && ShutterX(world, feet.x, out float shutter))
            {
                // Waits out of sight in the motor pool, to roll out of its shutter when it wakes.
                Pos = new Vector2(shutter, feet.y);
                m_State = State.Entrance;
            }
        }

        /// <summary>Rage, for kie's attacks: how much quicker it is, hurt.</summary>
        public float Fury => Rage;

        /// <summary>The middle of its patrol. Kie's hologram trick moves it.</summary>
        public float HomeX
        {
            get => m_HomeX;
            set => m_HomeX = value;
        }

        /// <summary>The motor pool gate just short of its patrol, where it makes its entrance from.</summary>
        static bool ShutterX(World world, float homeX, out float x)
        {
            x = 0f;
            bool found = false;
            foreach (Spawn s in world.Level.Spawns)
                if (s.Kind == SpawnKind.MotorPool && s.Pos.x < homeX && homeX - s.Pos.x < 16f && (!found || s.Pos.x > x))
                {
                    x = s.Pos.x;
                    found = true;
                }
            return found;
        }

        public override string DisplayName => m_Spec.Name;

        /// <summary>Still rolling out: it can't be hurt and doesn't fire.</summary>
        public bool Entering => m_State == State.Entrance;

        public override void Hurt(int damage)
        {
            if (Entering) return;
            if (Decoy)
            {
                if (Shootable) FlashHit(); // a copy only glitches
                return;
            }
            int before = Hp;
            base.Hurt(damage);
            if (Hp < before) m_Kie?.OnHurt(before - Hp);
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
                return Pos + Art.TankBarrelPivot + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (20f / Art.PPU);
            }
        }

        void Enter(State s)
        {
            m_State = s;
            m_StateTime = 0f;
        }

        protected override void TickAlive(float dt)
        {
            if (Decoy)
            {
                Mirror(true);
                return;
            }
            if (!Engaged)
            {
                if (World.View.Right < Pos.x + HalfW + 0.5f) return;
                Engaged = true;
                if (!Decoy) World.OnBossIntro(this, Entering);
            }

            float rage = Rage;
            m_StateTime += dt;
            m_Kick = Mathf.MoveTowards(m_Kick, 0f, dt * 6f);
            m_Charge = 0f;
            Vel.x = 0f;
            switch (m_State)
            {
                case State.Entrance:
                    Vel.x = Mathf.Sign(m_HomeX - Pos.x) * m_Spec.EntranceSpeed;
                    if (Mathf.Abs(m_HomeX - Pos.x) < 0.1f || m_StateTime >= m_Spec.EntranceTime)
                    {
                        m_Timer = 0.3f;
                        Enter(State.Recoil);
                    }
                    break;
                case State.Drive:
                    Vel.x = Mathf.Clamp(m_DriveTo - Pos.x, -1f, 1f) * m_Spec.DriveSpeed * rage;
                    if (Mathf.Abs(m_DriveTo - Pos.x) < 0.1f || m_StateTime >= m_Spec.DriveTime) Aim();
                    break;
                case State.Aim:
                    Swing(dt);
                    if (Mathf.Approximately(m_Angle, m_WantAngle)) StartCharge();
                    break;
                case State.Charge:
                    m_Charge = Mathf.Clamp01(m_StateTime * rage / m_Spec.ChargeTime);
                    if (m_StateTime >= m_Spec.ChargeTime / rage) Fire();
                    break;
                case State.Recoil:
                    if (World.SkipToPilot)
                    {
                        World.SkipToPilot = false; // ?test=pilot
                        Hurt(Hp);
                        return;
                    }
                    m_Timer -= dt;
                    if (m_Timer <= 0f && !Target.IsDown) Drive();
                    break;
            }

            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveWithTiles(dt);
            World.Combat.DamageAvatarsTouching(Bounds, 1);
            m_Kie.Tick(dt);
        }

        /// <summary>Turns the turret toward the angle it wants, whirring.</summary>
        void Swing(float dt)
        {
            if (Mathf.Approximately(m_Angle, m_WantAngle)) return;
            m_Angle = Mathf.MoveTowards(m_Angle, m_WantAngle, m_Spec.TurnRate * Rage * dt);
            m_ServoTimer -= dt;
            if (m_ServoTimer <= 0f)
            {
                m_ServoTimer = 0.11f;
                Sfx.Play(Sfx.Servo, 0.25f);
            }
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
            Vector2 from = Pos + Art.TankBarrelPivot, to = Target.Pos;
            float dx = to.x - from.x, dy = to.y - from.y;
            float g = Content.BigShell.Gravity;
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
            m_LandingSeen = m_Kie == null || m_Kie.NextLands; // kie's copies and roulette land nowhere
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
            float g = Content.BigShell.Gravity;
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
            StartFight();
            if (m_Kie.Fire(Muzzle, m_Angle, out bool thrown))
            {
                if (thrown)
                {
                    m_Shots++; // a follower's tank kicks with it
                    Recoil();
                }
                m_LandingSeen = false;
                m_Timer = m_Spec.RecoilTime / Rage + m_Kie.Wait;
                Enter(State.Recoil);
                return;
            }
            m_Shots++;
            bool spread = m_Spec.SpreadEvery > 0 && m_Shots % m_Spec.SpreadEvery == 0;
            Shoot(Muzzle, spread);
            foreach (TankBoss copy in m_Kie.Copies) Shoot(copy.Muzzle, spread); // kie's copies fire with it
            Recoil();
            m_Timer = m_Spec.RecoilTime / Rage;
            Enter(State.Recoil);
        }

        void Shoot(Vector2 muzzle, bool spread)
        {
            WeaponDef cannon = Content.BossCannon;
            if (spread)
            {
                for (int i = -1; i <= 1; i++)
                    World.Combat.FireEnemyShot(cannon, muzzle, Velocity(m_Angle, m_Power * (1f + i * m_Spec.SpreadPower)));
            }
            else World.Combat.FireEnemyShot(cannon, muzzle, Velocity(m_Angle, m_Power));
        }

        /// <summary>
        /// A copy: take up the real tank's turret, charge, kick and landing marker (moved over by the gap between
        /// them), and on the authority its steps along the patrol too, so nothing tells them apart.
        /// </summary>
        void Mirror(bool move)
        {
            if (!(World.Boss is TankBoss real) || real == this || real.Dying) return;
            m_State = real.m_State;
            m_Charge = real.m_Charge;
            m_Power = real.m_Power;
            m_Angle = real.m_Angle;
            m_WantAngle = real.m_WantAngle;
            m_Kick = real.m_Kick;
            m_LandingSeen = real.m_LandingSeen;
            m_Landing = real.m_Landing + new Vector2(Pos.x - real.Pos.x, 0f);
            Vel.x = real.Vel.x;
            if (move) Pos.x = m_HomeX + (real.Pos.x - real.m_HomeX);
        }

        /// <summary>The bang and the barrel kicking back, as it fires (here, or on the authority's screen).</summary>
        void Recoil()
        {
            Sfx.Play(Sfx.Cannon, 0.9f);
            m_Kick = 1f;
            m_LandingSeen = false;
        }

        // ------------------------------------------------------------------ co-op

        /// <summary>Authority: what its network object carries.</summary>
        public override NetBossState NetState => new NetBossState
        {
            Phase = (int)m_State + 1, Angle = m_Angle, Charge = m_Charge, Power = m_Power, Landing = m_Landing, Shots = m_Shots & 0xff,
            KieState = m_Kie.NetState(m_LandingSeen), KieProgress = m_Kie.Progress, KieDrops = m_Kie.Drops & 0xff,
        };

        /// <summary>Follower: take up the authority's turret, charge and shots. The shells come as enemy shots.</summary>
        public override void ApplyNet(in NetBossState s)
        {
            if (s.Phase == 0 || s.Phase == Pilot.NetPhase) return; // the pilot's, once he's out
            m_Kie.ApplyNet(s.KieState, s.KieProgress, s.KieDrops); // before the death check: it drops what it stole as it dies
            if (Dying) return;
            var phase = (State)(s.Phase - 1);
            if (phase == State.Charge && m_State != State.Charge) Sfx.Play(Sfx.Charge, 0.5f);
            m_State = phase;
            m_WantAngle = s.Angle;
            m_Charge = s.Charge;
            m_Power = s.Power;
            m_Landing = s.Landing;
            m_LandingSeen = phase == State.Charge && KieBoss.MarkerShown(s.KieState);
            if (m_SeenShots >= 0 && s.Shots != m_SeenShots)
            {
                if (!m_SeenFire) Log.Net.Info("The boss tank fired on the authority's screen");
                m_SeenFire = true;
                StartFight();
                Recoil();
            }
            m_SeenShots = s.Shots;
        }

        protected override void TickFollower(float dt)
        {
            if (Decoy)
            {
                Mirror(false);
                return;
            }
            m_Kie.Tick(dt);
            m_Kick = Mathf.MoveTowards(m_Kick, 0f, dt * 6f);
            Swing(dt * 1.5f); // a touch quicker, so it never lags far behind the authority's
        }

        protected override int ExtraFlags => (Engaged ? NetEnemyState.Engaged : 0) | (m_Kie != null && m_Kie.Disguised ? NetEnemyState.Disguised : 0);
        protected override void OnSnapshot(int flags)
        {
            m_Kie?.SetDisguised((flags & NetEnemyState.Disguised) != 0); // the authority's hologram trick
            bool woke = !Engaged && (flags & NetEnemyState.Engaged) != 0;
            Engaged = (flags & NetEnemyState.Engaged) != 0;
            if (woke && !Decoy) World.OnBossIntro(this, Entering); // the authority's tank woke
        }

        /// <summary>Its first shot: the fight is on.</summary>
        void StartFight()
        {
            if (m_FightStarted || Decoy) return;
            m_FightStarted = true;
            World.OnBossFightStarted(this);
        }

        protected override void Die()
        {
            m_Kie?.OnDie();
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
                World.OnTankWrecked(this); // and out jumps its pilot
                Remove();
            }
        }

        public override void Render()
        {
            Go.transform.localPosition = new Vector3(Snap(Pos.x), Snap(Pos.y), 0f);
            Visual.localScale = Vector3.one;
            Visual.gameObject.SetActive(Engaged || !Entering); // in the motor pool until it wakes
            float rumble = !Dying && Mathf.Abs(Vel.x) > 0.05f && ((int)(Anim * 16f) % 2 == 0) ? 1f / Art.PPU : 0f;
            m_Hull.transform.localPosition = new Vector3(0f, rumble, 0f);
            m_Hull.sprite = Flashed(Art.TankHull);
            m_Barrel.sprite = Flashed(Art.TankBarrel);

            // The barrel sprite points left; turn it to the angle, and kick it back along itself when it fires.
            m_BarrelPivot.localRotation = Quaternion.Euler(0f, 0f, m_Angle - 180f);
            m_BarrelPivot.localPosition = (Vector3)Art.TankBarrelPivot + new Vector3(0f, rumble, 0f);
            m_Barrel.transform.localPosition = new Vector3(m_Kick * 4f / Art.PPU, 0f, 0f);

            bool charging = m_State == State.Charge && !Dying;
            float charge = charging ? m_Charge : 0f;
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
            m_Kie?.Render();
        }
    }
}
