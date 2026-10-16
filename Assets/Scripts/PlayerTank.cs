using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Your tank, the one you drive or call in (<c>docs/design/tanks.md</c>, Driving a tank). It's its own entity, apart
    /// from the boss tank (<see cref="TankBoss"/>), and shares only its shape (<see cref="TankLook"/>). It's tones2's tabk
    /// (tanks.md, tones2's tabk): his paint (<see cref="Tones2Art.TabkHull"/>) and his guns, tabk-sized. Numbers are in
    /// <see cref="PlayerTankSpec"/>, its guns' in <see cref="Tones2TabkSpec"/>.
    ///
    /// Jump into an empty tank (or press JUMP touching it) to climb in. Inside, the stick drives it and points the gun
    /// the eight ways (<see cref="TankAim"/>). FIRE is ten-1, the water cannon: a round is one spray cycle, its pressure
    /// building in spurts, holding, then easing off. SP is ten-2, the mud jump: it shakes for a second, then blasts off
    /// along the aim like a jetpack (no fuel), carpeting the launch spot in toxic mud and squashing what it lands on.
    /// SU is ten-3, three seconds of spew while you drive. Its rounds never refill (<see cref="TankRounds"/>). JUMP hops it; down and JUMP ejects. Driving and hopping burn
    /// fuel (<see cref="TankFuel"/>), which never refills: a dry tank stays put but still aims and fires, until you
    /// eject. Driving over a goon squashes it, the payoff for the fuel (<see cref="PlayerTankSpec.Crushes"/>). Ejected,
    /// the empty tank rolls on and blows up on the first goon or wall; wrecked, it throws its driver clear and blows up.
    /// Either way it's gone.
    ///
    /// While someone's inside, the soldier rides along hidden (<see cref="Player.Riding"/>), with the tank's size,
    /// so every shot, blast and touch that would hurt them lands on the tank instead.
    ///
    /// Each player's tank is their own and runs on their own screen, like their soldier. In co-op it travels with the
    /// soldier's replicated state (<see cref="Pose"/>), and a partner's screen draws it (<see cref="RemotePlayer"/>);
    /// its water, mud and spew do their harm on the driver's screen, and squashed goons go as their hits. (Mission one, where
    /// it's won, is single player.)
    /// </summary>
    public sealed class PlayerTank : Actor
    {
        public enum State { Dropping, Parked, Driven, Runaway }

        static PlayerTankSpec Spec => Content.PlayerTank;
        static Tones2TabkSpec Guns => Tones2Tuning.Tabk;

        public State Mode { get; private set; }
        public readonly TankRounds Rounds = new TankRounds(Content.PlayerTank);
        public readonly TankFuel Fuel = new TankFuel(Content.PlayerTank);

        readonly TankLook m_Look;
        /// <summary>Where a called-in tank will land, blinking on the ground while it drops.</summary>
        readonly SpriteRenderer m_Marker;
        readonly float m_LandY;
        /// <summary>The cargo drone, in his orange and black, that lowers a called-in tank on a cable, then lets go and flies off.</summary>
        readonly SpriteRenderer m_Drone, m_Cable;
        float m_DroneAway;
        const float DroneUp = 1.1f; // its middle, above the hull's top, while it carries the tank
        Vector2 m_Aim;
        float m_BarrelAngle, m_Grace, m_Runaway, m_Muzzle, m_Hint;
        bool m_SaidEmpty, m_SaidDry;
        // the guns: seconds into a spray cycle and building up a mud jump (-1 when not), the jump in the air, spew left
        float m_Cycle = -1f, m_Build = -1f, m_JetDue, m_Puff, m_SpewLeft, m_SpewDue;
        bool m_Flying;
        AudioSource m_Hiss;

        PlayerTank(World world, Vector2 feet, int facing, State mode)
            : base(world, "Player tank", feet, Content.PlayerTank.HalfWidth, Content.PlayerTank.Height, Content.PlayerTank.Hp)
        {
            Facing = facing;
            Mode = mode;
            m_Aim = new Vector2(facing, 0f);
            m_BarrelAngle = TankAim.Angle(m_Aim);
            m_Look = new TankLook(Go.transform, SortOrder.Player + 1, Tones2Art.TabkHull, Tones2Art.TabkBarrel);
            if (mode != State.Dropping) return;
            Vel = new Vector2(0f, -Spec.DropSpeed);
            m_LandY = world.Level.TopSurfaceY(feet.x);
            var marker = new GameObject("Landing marker");
            marker.transform.SetParent(Go.transform, false);
            m_Marker = marker.AddComponent<SpriteRenderer>();
            m_Marker.sprite = Art.LandingMarker;
            m_Marker.color = new Color(1f, 0.85f, 0.3f);
            m_Marker.sortingOrder = SortOrder.Effects;
            m_Drone = Child("Cargo drone", Tones2Art.TabkDrone[0], SortOrder.Player + 4);
            m_Drone.transform.localScale = new Vector3(2f, 2f, 1f);
            m_Cable = Child("Cable", Art.Pixel, SortOrder.Player + 3);
            m_Cable.color = new Color(0.15f, 0.15f, 0.2f);
        }

        SpriteRenderer Child(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Go.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
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

        /// <summary>The tank as a partner's screen draws it.</summary>
        public TankPose Pose => new TankPose
        {
            State = Mode == State.Dropping ? TankPose.Mode.Dropping : Mode == State.Parked ? TankPose.Mode.Parked
                : Mode == State.Driven ? TankPose.Mode.Driven : TankPose.Mode.Runaway,
            Facing = Facing,
            AimOctant = TankPose.Octant(m_Aim),
            Hp = Hp,
            Flash = FlashTimer > 0f,
            Muzzle = m_Muzzle > 0f,
        };

        /// <summary>The soldier climbs in: they ride along hidden, and the tank takes their orders.</summary>
        public void Board()
        {
            Mode = State.Driven;
            Sfx.Play(Sfx.Thump, 0.8f);
            Sfx.Play(Sfx.RocketRumble, 0.4f);
            World.Scoring.Popup("TONES2'S TABK!", Pos + Vector2.up * 2.4f, new Color(1f, 0.85f, 0.3f));
        }

        public override void Tick(float dt)
        {
            m_Grace -= dt;
            m_Muzzle -= dt;
            FlashTimer -= dt;
            if (Mode != State.Dropping) m_DroneAway += dt;
            if (Mode != State.Driven) StopGuns();
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

            // fuel: driving burns it; dry, the tank stays put (it still aims and fires)
            // building up a mud jump roots it; in the air from one, it flies where it was thrown
            float moveX = m_Build >= 0f || m_Flying ? 0f : input.MoveX;
            bool drives = moveX != 0f && Fuel.Burn(dt);
            if (!m_Flying) Vel.x = drives ? moveX * Spec.DriveSpeed : 0f;
            if (drives) Facing = moveX > 0f ? 1 : -1;
            if (input.JumpPressed && Grounded && m_Build < 0f && !m_Flying && Fuel.Burn(Spec.HopFuel))
            {
                Vel.y = Spec.HopVelocity;
                Sfx.Play(Sfx.Jump, 0.7f);
            }
            if (Fuel.Empty && !m_SaidDry)
            {
                m_SaidDry = true;
                World.Scoring.Popup("OUT OF FUEL!", Pos + Vector2.up * 2.4f, new Color(1f, 0.6f, 0.4f));
                Sfx.Play(Sfx.Servo, 0.7f);
            }
            Gravity(dt);
            MoveWithTiles(dt);
            // Metal Slug style: the camera never scrolls back, and you can't leave it (co-op: either side)
            float minX = World.View.Left + HalfW;
            if (Pos.x < minX) Pos.x = minX;
            if (World.Coop) Pos.x = Mathf.Min(Pos.x, World.View.Right - HalfW);
            if (drives || Vel.y < -2f) Crush(); // rolling or landing on goons squashes them
            if (m_Flying && Grounded) Land();

            m_Aim = TankAim.Next(m_Aim, input.Left, input.Right, input.Up, input.Down);
            m_BarrelAngle = Mathf.MoveTowardsAngle(m_BarrelAngle, TankAim.Angle(m_Aim), Spec.BarrelTurn * dt);

            if (input.FirePressed && m_Cycle < 0f)
            {
                if (Rounds.UseMain()) StartCycle();
                else Click();
            }
            if (input.SpecialPressed && m_Build < 0f && !m_Flying && Grounded)
            {
                if (Rounds.UseSpecial()) StartBuild();
                else Click();
            }
            if (input.SuperPressed && m_SpewLeft <= 0f)
            {
                if (Rounds.UseSuper()) StartSpew();
                else Click();
            }
            Spray(dt);
            BuildUp(dt);
            Pour(dt);
            if (Rounds.Empty && !m_SaidEmpty)
            {
                m_SaidEmpty = true;
                World.Scoring.Popup("EMPTY! DOWN + JUMP TO EJECT", Pos + Vector2.up * 2.6f, new Color(1f, 0.6f, 0.4f));
            }
        }

        /// <summary>Goons under the tracks are squashed: a killing hit (reported to the authority in co-op).</summary>
        void Crush()
        {
            Rect under = Bounds;
            foreach (Enemy e in World.Enemies)
            {
                if (!e.Shootable || e.KillCredited || !Spec.Crushes(e.Def) || !under.Overlaps(e.Bounds)) continue;
                World.Combat.DamageEnemy(e, Mathf.Max(1, e.Hp));
                World.Scoring.Popup("SQUASH!", e.Center + Vector2.up * 0.8f, new Color(1f, 0.85f, 0.3f));
                World.Burst(e.Pos + Vector2.up * 0.2f, 8, new Color(0.45f, 0.4f, 0.35f));
                World.View.Shake(0.12f);
                Sfx.Play(Sfx.Splat, 0.9f);
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

        Vector2 MuzzleAlong(Vector2 dir) => Pos + TankLook.PivotLocal(Facing) + dir * Spec.BarrelLength;

        /// <summary>ten-1: one round, one spray cycle.</summary>
        void StartCycle()
        {
            m_Cycle = 0f;
            m_JetDue = 0f;
            Sfx.Play(Sfx.Servo, 0.4f);
        }

        void Spray(float dt)
        {
            if (m_Cycle < 0f) return;
            m_Cycle += dt;
            if (m_Cycle >= Guns.CycleLength)
            {
                m_Cycle = -1f;
                if (m_Hiss != null) m_Hiss.Stop();
                return;
            }
            float p = Guns.CyclePressure(m_Cycle);
            m_JetDue += dt * Guns.Water.DropsPerSecond(p);
            int drops = (int)m_JetDue;
            m_JetDue -= drops;
            Tones2Sprays.Jet(World, Guns.Water, MuzzleAlong(m_Aim), m_Aim, p, Vel.x, drops, false);
            if (p >= 0.999f) m_Muzzle = 0.03f;
            if (m_Hiss == null) m_Hiss = Sfx.LoopSource(Go, Sfx.JetLoop);
            if (m_Hiss == null) return;
            if (!m_Hiss.isPlaying) m_Hiss.Play();
            m_Hiss.volume = Sfx.Muted ? 0f : (0.15f + 0.25f * p) * Sfx.EffectsVolume;
            m_Hiss.pitch = 0.55f + 0.6f * p; // deeper than his jet: it's a cannon
        }

        /// <summary>ten-2: it roots and shakes while the mud builds up (<see cref="Tones2TabkSpec.BuildTime"/>).</summary>
        void StartBuild()
        {
            m_Build = 0f;
            m_Puff = 0f;
            Sfx.Play(Sfx.RocketRumble, 0.9f);
            World.Scoring.Popup("MUD JUMP!", Pos + Vector2.up * 2.6f, Tones2Art.Mud);
        }

        void BuildUp(float dt)
        {
            if (m_Build < 0f) return;
            m_Build += dt;
            m_Puff -= dt;
            if (m_Puff <= 0f)
            {
                m_Puff = 0.04f;
                World.SpawnParticle(Pos + new Vector2(Rng.Fx.Range(-HalfW, HalfW), 0.1f), new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(1f, 4f)),
                    Rng.Fx.Range(0f, 1f) < 0.25f ? Tones2Art.Toxic : Tones2Art.Mud, 2.5f, 0.45f);
            }
            if (m_Build >= Guns.BuildTime) Launch();
        }

        /// <summary>Off it goes along the aim, leaving the launch spot carpeted in mud.</summary>
        void Launch()
        {
            m_Build = -1f;
            m_Flying = true;
            Vector2 dir = Tones2TabkSpec.JumpDirection(m_Aim, Facing);
            Vel = dir * Guns.JumpSpeed;
            if (dir.x != 0f) Facing = dir.x > 0f ? 1 : -1;
            World.Liquids.SpillMud(Pos, Guns.Mud);
            Tones2Sprays.Exhaust(World, Pos);
            Tones2Sprays.Exhaust(World, Pos);
            Sfx.Play(Sfx.RocketLaunch);
            World.View.Shake(0.25f);
            World.Net?.SendShot(new ShotEvent(Content.RocketJump, Pos, Vector2.zero)); // partners lay mud too, harmless, on their screens
        }

        void Land()
        {
            m_Flying = false;
            Vel.x = 0f;
            World.View.Shake(0.3f);
            Sfx.Play(Sfx.Thump);
            World.Burst(Pos, 12, Tones2Art.Mud);
        }

        /// <summary>ten-3: three seconds of spew along the aim, while it drives.</summary>
        void StartSpew()
        {
            m_SpewLeft = Guns.Spew.Duration;
            m_SpewDue = 0f;
            Sfx.Play(Sfx.Spew);
            World.Scoring.Popup("SPEW!", Pos + Vector2.up * 2.6f, Tones2Art.Spew);
        }

        void Pour(float dt)
        {
            if (m_SpewLeft <= 0f) return;
            m_SpewLeft -= dt;
            m_SpewDue += dt * Guns.Spew.DropsPerSecond;
            int drops = (int)m_SpewDue;
            m_SpewDue -= drops;
            Tones2Sprays.Spew(World, Guns.Spew, MuzzleAlong(m_Aim), m_Aim, Vel.x, drops, false);
            World.View.Shake(0.05f);
            m_Muzzle = 0.03f;
        }

        void StopGuns()
        {
            m_Cycle = m_Build = -1f;
            m_SpewLeft = 0f;
            m_Flying = false;
            if (m_Hiss != null && m_Hiss.isPlaying) m_Hiss.Stop();
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
                World.Scoring.Popup("TABK WRECKED!", Pos + Vector2.up * 2.4f, new Color(1f, 0.5f, 0.4f));
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
            World.TankBlew(Center);
            World.Combat.Explode(Center, Spec.RunawayBlast, Spec.RunawayDamage, false, 2f);
            Blast(World, Center);
        }

        /// <summary>The look and sound of a tank blowing up (a partner's, on our screen, is only this).</summary>
        public static void Blast(World world, Vector2 at)
        {
            world.Burst(at, 24, new Color(0.3f, 0.32f, 0.2f));
            Sfx.Play(Sfx.BigExplosion);
            world.View.Shake(0.45f);
        }

        public override void Render()
        {
            base.Render();
            bool blink = Mode == State.Runaway && (int)(m_Runaway * 12f) % 2 == 0;
            // shaking while a mud jump builds, harder as it goes
            Vector2 shake = m_Build >= 0f ? new Vector2(Mathf.Round(Rng.Fx.Range(-1f, 1f) * (1f + 2f * m_Build / Guns.BuildTime)) / Art.PPU, 0f) : Vector2.zero;
            m_Look.Apply(shake, Facing, m_BarrelAngle, FlashTimer > 0f, m_Muzzle > 0f, blink);
            if (m_Marker != null)
            {
                bool shown = Mode == State.Dropping && (int)(Time.time * 8f) % 2 == 0;
                if (m_Marker.enabled != shown) m_Marker.enabled = shown;
                m_Marker.transform.localPosition = new Vector3(0f, m_LandY - Pos.y + 0.05f, 0f);
            }
            if (m_Drone != null)
            {
                // it carries the tank down, lets go as it lands, and climbs away off the top of the screen
                bool carrying = Mode == State.Dropping, shown = carrying || m_DroneAway < 1.5f;
                if (m_Drone.enabled != shown) m_Drone.enabled = shown;
                if (m_Cable.enabled != carrying) m_Cable.enabled = carrying;
                if (shown)
                {
                    float t = carrying ? 0f : m_DroneAway, y = Height + DroneUp + t * (3f + 6f * t);
                    m_Drone.sprite = Tones2Art.TabkDrone[(int)(Time.time * 20f) % 2];
                    m_Drone.transform.localPosition = new Vector3(-Facing * t * 1.5f, y, 0f);
                }
                if (carrying)
                {
                    float bottom = Height - 0.05f, top = Height + DroneUp - 0.3f;
                    m_Cable.transform.localPosition = new Vector3(0f, (bottom + top) * 0.5f, 0f);
                    m_Cable.transform.localScale = new Vector3(1f, (top - bottom) * Art.PPU, 1f);
                }
            }
        }
    }
}
