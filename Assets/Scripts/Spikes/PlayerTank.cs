using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike (<c>?spike=drive</c>, solo): your tank, the one you drive or call in (<c>docs/design/tanks.md</c>, Driving
    /// a tank). It's its own entity, apart from the boss tank (<see cref="TankBoss"/>), and shares only its look.
    ///
    /// Jump into an empty tank (or press JUMP touching it) to climb in. Inside, the stick drives it and points the gun
    /// the eight ways (<see cref="TankAim"/>), FIRE shoots a shell at once, SP lobs a mortar and SU fans a barrage;
    /// its rounds never refill (<see cref="TankRounds"/>). JUMP hops it; down and JUMP ejects. Ejected, the empty
    /// tank rolls on and blows up on the first goon or wall; wrecked, it throws its driver clear and blows up. Either
    /// way it's gone.
    ///
    /// While someone's inside, the soldier rides along hidden (<see cref="Player.Riding"/>), with the tank's size,
    /// so every shot, blast and touch that would hurt them lands on the tank instead.
    /// </summary>
    public sealed class PlayerTank : Actor
    {
        public enum State { Dropping, Parked, Driven, Runaway }

        // Spike ids from 250 are kept off the wire (spikes are solo only).
        static readonly ProjectileDef ShellShot = new ProjectileDef(250, "Tank shell", radius: 0.22f, damage: Spec.ShellDamage, life: 3f,
            gravity: 2f, blastRadius: Spec.ShellBlast, blastSize: 1f);
        static readonly ProjectileDef MortarShot = new ProjectileDef(251, "Tank mortar", radius: 0.25f, damage: Spec.MortarDamage, life: 4f,
            gravity: 18f, blastRadius: Spec.MortarBlast, blastSize: 1.3f, spin: ProjectileSpin.Tumble);
        static readonly WeaponDef Shell = new WeaponDef(250, "Tank cannon", WeaponBehaviour.Bullet, ShellShot, speed: Spec.ShellSpeed,
            cooldown: Spec.ShellCooldown, muzzleBlast: 0.45f, kick: 0.12f, hud: "SHELL");
        static readonly WeaponDef Mortar = new WeaponDef(251, "Tank mortar", WeaponBehaviour.Lob, MortarShot, throwVelocity: Spec.MortarThrow,
            carry: 0.4f, hud: "MORTAR");

        static PlayerTankSpec Spec => Content.PlayerTank;

        public State Mode { get; private set; }
        public readonly TankRounds Rounds = new TankRounds(Content.PlayerTank);

        readonly SpriteRenderer m_Hull, m_Barrel;
        readonly Transform m_BarrelPivot;
        Vector2 m_Aim;
        float m_BarrelAngle, m_FireCooldown, m_Grace, m_Runaway, m_Muzzle, m_Hint;
        bool m_SaidEmpty;

        PlayerTank(World world, Vector2 feet, int facing, State mode)
            : base(world, "Player tank", feet, Content.PlayerTank.HalfWidth, Content.PlayerTank.Height, Content.PlayerTank.Hp)
        {
            Facing = facing;
            Mode = mode;
            m_Aim = new Vector2(facing, 0f);
            m_BarrelAngle = TankAim.Angle(m_Aim);
            m_Hull = AddRenderer(Visual, "Hull", Art.TankHull, SortOrder.Player + 2);
            m_BarrelPivot = new GameObject("Turret").transform;
            m_BarrelPivot.SetParent(Go.transform, false);
            m_Barrel = AddRenderer(m_BarrelPivot, "Barrel", Art.TankBarrel, SortOrder.Player + 1);
            if (mode == State.Dropping) Vel = new Vector2(0f, -Spec.DropSpeed);
        }

        /// <summary>An empty tank, parked on the ground at <paramref name="x"/>, facing right.</summary>
        public static PlayerTank Park(World world, float x) => new PlayerTank(world, new Vector2(x, world.Level.TopSurfaceY(x)), 1, State.Parked);

        /// <summary>A called-in tank: it drops from above the screen at <paramref name="x"/> and lands on whatever's under it.</summary>
        public static PlayerTank Drop(World world, float x, int facing) =>
            new PlayerTank(world, new Vector2(x, Tuning.Camera.ViewHeight + 2f), facing, State.Dropping);

        /// <summary>Empty and on the ground: a soldier can climb in.</summary>
        public bool CanBoard => Mode == State.Parked;
        public bool Occupied => Mode == State.Driven;
        public bool Invulnerable => m_Grace > 0f;
        /// <summary>Where the gun points.</summary>
        public Vector2 Aim => m_Aim;

        /// <summary>The soldier climbs in: they ride along hidden, and the tank takes their orders.</summary>
        public void Board()
        {
            Mode = State.Driven;
            Sfx.Play(Sfx.Thump, 0.8f);
            Sfx.Play(Sfx.RocketRumble, 0.4f);
            World.Scoring.Popup("TANK!", Pos + Vector2.up * 2.4f, new Color(1f, 0.85f, 0.3f));
        }

        public override void Tick(float dt)
        {
            m_Grace -= dt;
            m_Muzzle -= dt;
            FlashTimer -= dt;
            switch (Mode)
            {
                case State.Dropping: Fall(dt); break;
                case State.Parked: Idle(dt); break;
                case State.Driven: Drive(dt); break;
                default: Roll(dt); break;
            }
            if (Pos.y < -3f) Remove(); // into a pit
        }

        void Gravity(float dt) => Vel.y = Mathf.Max(Vel.y - Tuning.Physics.Gravity * dt, -Tuning.Physics.MaxFall);

        void Fall(float dt)
        {
            Vel.x = 0f;
            Vel.y = Mathf.Min(Vel.y, -Spec.DropSpeed);
            MoveWithTiles(dt);
            if (!Grounded) return;
            // the landing: a thump that blasts any goon it lands on
            Mode = State.Parked;
            World.View.Shake(0.35f);
            Sfx.Play(Sfx.Thump);
            World.Burst(Pos, 16, new Color(0.45f, 0.4f, 0.35f));
            World.Combat.Explode(Pos + Vector2.up * 0.4f, Spec.LandingBlast, Spec.LandingDamage, false, 0.9f);
        }

        void Idle(float dt)
        {
            Vel.x = 0f;
            Gravity(dt);
            MoveWithTiles(dt);
            // a nudge to hop in, while the soldier is close by
            Player p = World.Player;
            m_Hint -= dt;
            if (m_Hint <= 0f && !p.Dead && Mathf.Abs(p.Pos.x - Pos.x) < 4f)
            {
                m_Hint = 2.5f;
                World.Scoring.Popup("JUMP IN!", Pos + Vector2.up * 2.2f, new Color(0.6f, 0.9f, 1f));
            }
        }

        void Drive(float dt)
        {
            Player driver = World.Player;
            SoldierInput input = driver.InputEnabled ? driver.Input : default;

            if (input.JumpPressed && input.Down)
            {
                Eject(false);
                return;
            }

            float moveX = input.MoveX;
            Vel.x = moveX * Spec.DriveSpeed;
            if (moveX != 0f) Facing = moveX > 0f ? 1 : -1;
            if (input.JumpPressed && Grounded)
            {
                Vel.y = Spec.HopVelocity;
                Sfx.Play(Sfx.Jump, 0.7f);
            }
            Gravity(dt);
            MoveWithTiles(dt);
            // Metal Slug style: the camera never scrolls back, and you can't leave it
            float minX = World.View.Left + HalfW;
            if (Pos.x < minX) Pos.x = minX;

            m_Aim = TankAim.Next(m_Aim, input.Left, input.Right, input.Up, input.Down);
            m_BarrelAngle = Mathf.MoveTowardsAngle(m_BarrelAngle, TankAim.Angle(m_Aim), Spec.BarrelTurn * dt);

            m_FireCooldown -= dt;
            if (input.FirePressed && m_FireCooldown <= 0f)
            {
                if (Rounds.UseMain()) FireShell(m_Aim);
                else Click();
            }
            if (input.SpecialPressed)
            {
                if (Rounds.UseSpecial()) FireMortar();
                else Click();
            }
            if (input.SuperPressed)
            {
                if (Rounds.UseSuper()) FireBarrage();
                else Click();
            }
            if (Rounds.Empty && !m_SaidEmpty)
            {
                m_SaidEmpty = true;
                World.Scoring.Popup("EMPTY! DOWN + JUMP TO EJECT", Pos + Vector2.up * 2.6f, new Color(1f, 0.6f, 0.4f));
            }
        }

        void Roll(float dt)
        {
            m_Runaway += dt;
            Vel.x = Facing * Spec.RunawaySpeed;
            Gravity(dt);
            MoveResult r = MoveWithTiles(dt);
            bool hit = r.HitWall || m_Runaway >= Spec.RunawayTime;
            foreach (Enemy e in World.Enemies)
                if (!hit && e.Shootable && e.Bounds.Overlaps(Bounds)) hit = true;
            if (hit) Blow();
        }

        /// <summary>Where the barrel turns: the middle of the turret (the art faces left, so it's mirrored by facing).</summary>
        Vector2 PivotLocal => new Vector2(-Facing * 4.5f / Art.PPU, 17.5f / Art.PPU);

        Vector2 MuzzleAlong(Vector2 dir) => Pos + PivotLocal + dir * Spec.BarrelLength;

        void FireShell(Vector2 dir)
        {
            Vector2 at = MuzzleAlong(dir);
            World.Combat.FirePlayerShot(Shell, at, dir * Shell.Speed);
            World.SpawnEffect(Art.Explosion, 30f, at, Shell.MuzzleBlast);
            World.View.Shake(Shell.Kick);
            Sfx.Play(Sfx.Cannon, 0.85f);
            m_FireCooldown = Shell.Cooldown;
            m_Muzzle = 0.08f;
            m_BarrelAngle = TankAim.Angle(dir); // shots go at once, the barrel with them
        }

        void FireMortar()
        {
            Vector2 at = Pos + PivotLocal + Vector2.up * 0.6f;
            World.Combat.FirePlayerShot(Mortar, at, Mortar.LobVelocity(Facing, Vel));
            Sfx.Play(Sfx.Throw, 0.9f);
            Sfx.Play(Sfx.Cannon, 0.5f);
            World.View.Shake(0.1f);
        }

        void FireBarrage()
        {
            World.Scoring.Popup("BARRAGE!", Pos + Vector2.up * 2.6f, new Color(1f, 0.85f, 0.3f));
            World.View.Shake(0.3f);
            int n = Mathf.Max(1, Spec.BarrageShells);
            for (int i = 0; i < n; i++)
            {
                float a = (n == 1 ? 0f : -Spec.BarrageFan + 2f * Spec.BarrageFan * i / (n - 1)) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(m_Aim.x * Mathf.Cos(a) - m_Aim.y * Mathf.Sin(a), m_Aim.x * Mathf.Sin(a) + m_Aim.y * Mathf.Cos(a));
                World.Combat.FirePlayerShot(Shell, MuzzleAlong(m_Aim), dir * Shell.Speed);
            }
            World.SpawnEffect(Art.Explosion, 30f, MuzzleAlong(m_Aim), 0.8f);
            Sfx.Play(Sfx.BigExplosion, 0.7f);
            m_Muzzle = 0.12f;
        }

        void Click() => Sfx.Play(Sfx.Servo, 0.5f);

        /// <summary>Hit while someone's inside (every hurt meant for the driver comes here).</summary>
        public void Hurt(int damage)
        {
            if (Mode != State.Driven || Invulnerable || damage <= 0) return;
            Hp -= damage;
            m_Grace = Spec.HurtGrace;
            FlashTimer = 0.08f;
            World.View.Shake(0.15f);
            Sfx.Play(Sfx.Hit, 0.8f);
            World.HitFeel?.Hurt(World.Player, damage);
            if (Hp <= 0)
            {
                Hp = 0;
                World.Scoring.Popup("TANK WRECKED!", Pos + Vector2.up * 2.4f, new Color(1f, 0.5f, 0.4f));
                Eject(true);
            }
        }

        /// <summary>The driver pops out of the hatch. A wrecked tank blows up at once; an ejected one rolls on first.</summary>
        void Eject(bool wrecked)
        {
            World.Player.LeaveTank(Pos + new Vector2(0f, Height), new Vector2(-Facing * Mathf.Abs(Spec.EjectVelocity.x), Spec.EjectVelocity.y), Spec.EjectShield);
            Sfx.Play(Sfx.Jump);
            if (wrecked)
            {
                Blow();
                return;
            }
            Mode = State.Runaway;
            m_Runaway = 0f;
            World.Scoring.Popup("EJECT!", Pos + Vector2.up * 2.4f, new Color(1f, 0.85f, 0.3f));
        }

        /// <summary>It blows up, hurting goons (never the soldiers), and it's gone.</summary>
        public void Blow()
        {
            if (Removed) return;
            if (Mode == State.Driven) World.Player.LeaveTank(Pos + new Vector2(0f, Height), new Vector2(0f, Spec.EjectVelocity.y), Spec.EjectShield);
            Remove();
            World.Combat.Explode(Center, Spec.RunawayBlast, Spec.RunawayDamage, false, 2f);
            World.Burst(Center, 24, new Color(0.3f, 0.32f, 0.2f));
            Sfx.Play(Sfx.BigExplosion);
            World.View.Shake(0.45f);
        }

        public override void Render()
        {
            base.Render();
            Visual.localScale = new Vector3(-Facing, 1f, 1f); // the art faces left
            m_Hull.sprite = Flashed(Art.TankHull);
            m_Hull.color = Mode == State.Runaway && (int)(m_Runaway * 12f) % 2 == 0 ? new Color(1f, 0.6f, 0.5f) : Color.white;
            m_BarrelPivot.localPosition = PivotLocal;
            m_BarrelPivot.localRotation = Quaternion.Euler(0f, 0f, m_BarrelAngle - 180f); // the barrel's art points left
            m_Barrel.color = m_Muzzle > 0f ? new Color(1f, 0.9f, 0.6f) : Color.white;
        }
    }
}
