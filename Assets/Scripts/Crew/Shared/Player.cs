using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The locally controlled soldier. How they move comes from their <see cref="SoldierDef"/>, and what their FIRE, SP
    /// and SU do from their crewmate's kit (<see cref="CrewKit"/>), which the soldier calls without knowing whose it is.
    /// </summary>
    public sealed class Player : Actor
    {
        public readonly SoldierDef Def;
        /// <summary>The special's charges, the super and the ability (<c>docs/design/player.md</c>, The kit).</summary>
        public readonly KitMeters Kit;
        /// <summary>Lives and continues, this player's own.</summary>
        public readonly LifeBank Lives = LifeBank.Standard();
        /// <summary>Charges left for the special (<see cref="SoldierDef.Special"/>).</summary>
        public int Specials { get => Kit.Specials; set => Kit.Specials = value; }
        /// <summary>A crate's gun, which replaces the primary while <see cref="GunAmmo"/> lasts.</summary>
        public WeaponDef Gun;
        public int GunAmmo;
        public bool Dead;
        public float DeadTime;
        public bool Crouching;
        public bool InputEnabled = true;
        /// <summary>Nothing hurts them: a move's demo (<see cref="DemoScript"/>), where the goons' shots would only get in the way.</summary>
        public bool NoDamage;
        /// <summary>This frame's orders, from <see cref="GameInput"/>; the world sets them before <see cref="Tick"/>.</summary>
        public SoldierInput Input;

        /// <summary>Seconds left slowed down by splashback from his own jet (tones2).</summary>
        public float SlowTimer;

        /// <summary>
        /// The tank this soldier is driving, or null on foot. Inside, they ride along hidden
        /// with the tank's size, so whatever would hurt them hits the tank (<see cref="PlayerTank"/>).
        /// </summary>
        public PlayerTank Riding { get; private set; }

        float m_Coyote, m_JumpBuffer, m_FireCooldown, m_Anim, m_Muzzle, m_Invuln, m_DropThrough, m_Knock;
        bool m_Fell; // this death's fall has been counted against the lives
        bool m_LeftTank; // just popped out of a tank's hatch this frame
        readonly CallInGesture m_CallIn = new CallInGesture(Tuning.Kit.CallInTapTime, Tuning.Kit.CallInChargeTime);
        Vector2 m_Aim = Vector2.right;
        readonly PlayerVisual m_Visual;
        readonly CrewKit m_Kit;
        FrostLook m_Ice;

        public Player(World world, SoldierDef def, Vector2 feet, int skin)
            : base(world, "Player", feet, def.HalfWidth, def.StandHeight, def.MaxHp)
        {
            Def = def;
            Kit = new KitMeters(def.StartSpecials, def.SpecialMax, def.SpecialRefill, Tuning.Kit.SuperKills, def.Ability != null ? def.AbilityCooldown : 0f);
            m_Visual = new PlayerVisual(Visual, Art.SkinFor(def, skin), SortOrder.Player);
            m_Kit = CrewKit.For(this);
            Grounded = true; // spawned standing on the ground (matters for the title-screen pose)
        }

        public bool Invulnerable => Riding != null ? Riding.Invulnerable : m_Invuln > 0f || Dead || m_Kit.Busy;
        /// <summary>How charged a tank call-in is, 0 to 1, while SP is held with the stick down.</summary>
        public float CallInCharge => m_CallIn.Charge;
        /// <summary>Where the soldier is aiming: forward, up, down in the air, or a diagonal (<see cref="Aiming"/>).</summary>
        public Vector2 Aim => m_Aim;
        public override bool IsDown => Dead;
        public int Skin { set => m_Visual.Skin = Art.SkinFor(Def, value); }

        public AvatarPose Pose
        {
            get
            {
                var pose = new AvatarPose
                {
                    Facing = Facing,
                    AimY = Aiming.AimY(m_Aim),
                    Diagonal = Aiming.IsDiagonal(m_Aim),
                    Grounded = Grounded,
                    Crouching = Crouching,
                    Running = Grounded && Mathf.Abs(Vel.x) > 0.1f,
                    Dead = Dead,
                    Out = Lives.OutOfLives,
                    Gone = Lives.IsOut,
                    Muzzle = m_Muzzle > 0f,
                    Blink = m_Invuln > 0f && !Dead,
                    Flash = FlashTimer > 0f,
                    Anim = m_Anim,
                    DeadTime = DeadTime,
                    SuperReady = Kit.SuperReady,
                    Frozen = Frost.Frozen,
                };
                m_Kit.Pose(ref pose);
                return pose;
            }
        }

        /// <summary>Mission two's nitrogen got our soldier, on foot: frozen for <paramref name="seconds"/>, unless just thawed.</summary>
        public bool Freeze(float seconds)
        {
            if (Dead || Riding != null || NoDamage || !Frost.Freeze(seconds, Creamery.ThawGrace)) return false;
            Sfx.Play(Sfx.Sizzle, 0.6f);
            return true;
        }

        public override void Tick(float dt)
        {
            m_Kit.Always(dt);
            Frost.Tick(dt);
            if (Dead)
            {
                Frost.Clear();
                m_Kit.LetGo();
                m_CallIn.Reset();
                DeadTime += dt;
                Vel.y = Mathf.Max(Vel.y - Tuning.Physics.Gravity * dt, -Tuning.Physics.MaxFall);
                Vel.x = Mathf.MoveTowards(Vel.x, 0f, 6f * dt);
                MoveWithTiles(dt);
                Down(dt);
                return;
            }

            if (Riding != null)
            {
                Ride(dt);
                return;
            }

            SoldierInput input = InputEnabled && !m_LeftTank && !Frost.Frozen ? Input : default; // the press that ejected doesn't jump too; frozen, nothing
            m_LeftTank = false;
            float moveX = input.MoveX;
            bool up = input.Up, down = input.Down;

            // --- horizontal ---
            bool rooted = m_Kit.Rooted, fixated = m_Kit.Fixated, busy = m_Kit.Busy;
            Crouching = Grounded && down && !rooted;
            Height = Crouching ? Def.CrouchHeight : Def.StandHeight;
            float speed = Crouching ? Def.CrawlSpeed : Def.RunSpeed;
            if (SlowTimer > 0f) speed *= Tones2Tuning.Jet.SplashSlow;
            speed *= m_Kit.SpeedScale;
            if (m_Knock > 0f) m_Knock -= dt; // knocked back: the push carries on for a moment
            else Vel.x = rooted ? 0f : moveX * speed;
            if (moveX != 0f && !rooted && !fixated && m_Knock <= 0f) Facing = moveX > 0f ? 1 : -1;
            SlowTimer -= dt;

            // --- jumping (with coyote time + input buffering) ---
            m_Coyote = Grounded ? Tuning.Soldier.CoyoteTime : m_Coyote - dt;
            m_JumpBuffer = input.JumpPressed ? Tuning.Soldier.JumpBuffer : m_JumpBuffer - dt;
            m_DropThrough -= dt;
            if (m_JumpBuffer > 0f && m_Coyote > 0f && !rooted && !fixated)
            {
                if (down && TilePhysics.OnBridge(World.Level, Pos, HalfW))
                {
                    m_DropThrough = Tuning.Soldier.DropThroughTime;
                }
                else
                {
                    Vel.y = Def.JumpVelocity;
                    Sfx.Play(Sfx.Jump, 0.6f);
                }
                m_JumpBuffer = 0f;
                m_Coyote = 0f;
            }

            float gravity = Tuning.Physics.Gravity;
            if (Vel.y > 0f && !input.Jump && !m_Kit.Rising) gravity *= Tuning.Soldier.ShortHopGravity; // short hop when released
            Vel.y = Mathf.Max(Vel.y - gravity * dt, -Tuning.Physics.MaxFall);
            MoveWithTiles(dt, m_DropThrough > 0f);

            // Metal Slug style: the camera never scrolls back, and you can't leave it (co-op: either side).
            float minX = World.View.Left + HalfW;
            if (Pos.x < minX) Pos.x = minX;
            if (World.Coop) Pos.x = Mathf.Min(Pos.x, World.View.Right - HalfW);

            // --- aiming & shooting ---
            m_Aim = Aiming.Direction(up, down, moveX, Facing, Grounded);
            m_Kit.Tick(dt);

            m_FireCooldown -= dt;
            bool gun = GunAmmo > 0;
            if (m_Kit.Streams) m_Kit.HoldPrimary(dt, input.Fire && !gun && !rooted && !busy && !fixated);
            bool wantsFire = gun ? input.Fire : (input.FirePressed || input.Fire);
            if (wantsFire && m_FireCooldown <= 0f && (gun || !m_Kit.Streams) && !rooted && !busy && !fixated) Fire(gun ? Gun : Def.Primary);

            // SP: the special, or with the stick down and a tank to call, a held call-in
            Kit.Tick(dt);
            CallInGesture.Result sp = m_CallIn.Tick(dt, down, input.Special, input.SpecialPressed, World.TanksToCall > 0);
            bool special = sp == CallInGesture.Result.FireSpecial;
            if (sp == CallInGesture.Result.Called) World.CallTank(Pos);
            else if (sp == CallInGesture.Result.Cancelled) Sfx.Play(Sfx.Hurt, 0.25f);
            m_Kit.Special(dt, special, input.Special);
            if (input.SuperPressed && Kit.SuperReady) m_Kit.Super();
            if (input.AbilityPressed && Kit.UseAbility()) FireOnce(Def.Ability);
            if (input.TeamPressed) World.FireTeam(); // only when TEAM shows

            // jump into an empty tank, or press JUMP touching one, to climb in
            PlayerTank tank = World.Tank;
            if (tank != null && tank.CanBoard && !tank.Removed && Bounds.Overlaps(tank.Bounds) && (input.JumpPressed || (!Grounded && Vel.y < 0f)))
                Board(tank);

            m_Invuln -= dt;
            m_Muzzle -= dt;
            FlashTimer -= dt;
            m_Anim += dt;
        }

        void Board(PlayerTank tank)
        {
            m_Kit.LetGo();
            Riding = tank;
            m_CallIn.Reset();
            Crouching = false;
            tank.Board();
            Ride(0f);
        }

        /// <summary>Inside a tank: the soldier goes where it goes, with its size; the tank does the driving.</summary>
        void Ride(float dt)
        {
            PlayerTank tank = Riding;
            if (tank.Removed)
            {
                LeaveTank(tank.Pos + Vector2.up * tank.Height, new Vector2(0f, Def.JumpVelocity * 0.6f), Tuning.Soldier.HurtInvulnerability);
                return;
            }
            Pos = tank.Pos;
            Vel = tank.Vel;
            Facing = tank.Facing;
            HalfW = tank.HalfW;
            Height = tank.Height;
            Grounded = tank.Grounded;
            Kit.Tick(dt);
            m_Invuln -= dt;
            m_Anim += dt;
        }

        /// <summary>Out of the tank's hatch (ejecting, or thrown clear of a wreck), flying up and away, briefly safe.</summary>
        public void LeaveTank(Vector2 at, Vector2 vel, float shield)
        {
            if (Riding == null) return;
            Riding = null;
            HalfW = Def.HalfWidth;
            Height = Def.StandHeight;
            Pos = at;
            Vel = vel;
            m_Knock = 0.35f; // the pop carries them clear before the stick takes over
            m_LeftTank = true;
            m_Invuln = Mathf.Max(m_Invuln, shield);
        }

        /// <summary>Where shots leave the gun, in the world.</summary>
        public Vector2 MuzzlePos
        {
            get
            {
                Vector2 local = PlayerVisual.MuzzleLocal(Aiming.AimY(m_Aim), Crouching, Aiming.IsDiagonal(m_Aim));
                return Pos + new Vector2(local.x * Facing, local.y);
            }
        }

        void Fire(WeaponDef weapon)
        {
            Vector2 origin = MuzzlePos;
            if (weapon == Def.Primary && m_Kit.OwnPrimary)
            {
                // the kit's own (kie's weasels: critters, not shots); nothing went, nothing cools down
                if (!m_Kit.FirePrimary(origin)) return;
                m_FireCooldown = weapon.Cooldown;
                m_Muzzle = 0.05f;
                return;
            }
            Vector2 dir = m_Aim;
            if (weapon.Spread > 0f)
            {
                float spread = World.Rng.Range(-weapon.Spread, weapon.Spread) * Mathf.Deg2Rad;
                dir = new Vector2(dir.x * Mathf.Cos(spread) - dir.y * Mathf.Sin(spread), dir.x * Mathf.Sin(spread) + dir.y * Mathf.Cos(spread));
            }
            if (weapon == Gun)
            {
                GunAmmo--;
                if (GunAmmo == 0) World.Scoring.Popup("OUT OF AMMO", Pos + Vector2.up * 2f, new Color(1f, 0.6f, 0.4f));
            }
            Vector2 vel = weapon.Behaviour == WeaponBehaviour.Lob ? weapon.LobVelocity(Facing, Vel) : dir * weapon.Speed;
            World.Combat.FirePlayerShot(weapon, origin, vel);
            m_FireCooldown = weapon.Cooldown;
            m_Muzzle = 0.05f;
            Sfx.PlayShot(weapon);
        }

        /// <summary>The super's fanfare: its popup and a shake.</summary>
        public void SuperFanfare()
        {
            World.Scoring.Popup("SUPER!", Pos + Vector2.up * 2.2f, Palette.Gold);
            World.View.Shake(0.25f);
        }

        /// <summary>
        /// A plain super (<see cref="CrewKit.Super"/>): one shot of <see cref="SoldierDef.Super"/>, or, for a soldier
        /// without one, a fan of their primary's shots.
        /// </summary>
        public void FireSuper()
        {
            SuperFanfare();
            if (Def.Super != null)
            {
                FireOnce(Def.Super);
                return;
            }
            WeaponDef w = Def.Primary.Projectile != null ? Def.Primary : Content.Pistol; // a jet has no shot to fan
            const int shots = 9;
            const float fan = 40f;
            for (int i = 0; i < shots; i++)
            {
                float a = (-fan + 2f * fan * i / (shots - 1)) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(m_Aim.x * Mathf.Cos(a) - m_Aim.y * Mathf.Sin(a), m_Aim.x * Mathf.Sin(a) + m_Aim.y * Mathf.Cos(a));
                World.Combat.FirePlayerShot(w, MuzzlePos, dir * w.Speed);
            }
            m_Muzzle = 0.1f;
            Sfx.PlayShot(w);
        }

        /// <summary>One shot of a super or ability weapon, along the aim.</summary>
        void FireOnce(WeaponDef weapon)
        {
            if (weapon == null) return;
            World.Combat.FirePlayerShot(weapon, MuzzlePos, m_Aim * weapon.Speed);
            m_Muzzle = 0.05f;
            Sfx.PlayShot(weapon);
        }

        public void Hurt(int damage)
        {
            if (Riding != null)
            {
                Riding.Hurt(damage);
                return;
            }
            if (Invulnerable || NoDamage) return;
            Hp -= damage;
            World.HitFeel?.Hurt(this, damage);
            FlashTimer = Tuning.Impact.HitFlash;
            World.View.Shake(0.15f);
            if (Hp <= 0)
            {
                Hp = 0;
                Dead = true;
                DeadTime = 0f;
                Vel = new Vector2(-Facing * 3f, 9f);
                Sfx.Play(Sfx.Hurt);
                World.Burst(Center, 14, new Color(0.85f, 0.2f, 0.15f));
            }
            else
            {
                m_Invuln = Tuning.Soldier.HurtInvulnerability;
                Sfx.Play(Sfx.Hurt);
                // knocked back, away from where they face (the hit nearly always comes from ahead)
                Vel = new Vector2(-Facing * Tuning.Impact.KnockSpeed, Mathf.Max(Vel.y, Tuning.Impact.KnockHop));
                m_Knock = Tuning.Impact.KnockTime;
            }
        }

        /// <summary>Can't be hurt for <paramref name="seconds"/> (blinking), as after a hit.</summary>
        public void Shield(float seconds) => m_Invuln = Mathf.Max(m_Invuln, seconds);

        public void Heal(int amount) => Hp = Mathf.Min(MaxHp, Hp + amount);

        /// <summary>A crate's gun: it replaces the primary until its ammo runs out. The same gun again adds its ammo.</summary>
        public void GiveGun(WeaponDef gun)
        {
            if (gun != Gun) GunAmmo = 0;
            Gun = gun;
            GunAmmo += gun.Ammo;
        }

        /// <summary>
        /// While down: after a moment the fall costs a life and the soldier comes back. Out of lives, a continue is
        /// offered (FIRE or JUMP takes it); let it go and they're out, until a partner's 1-up brings them back.
        /// </summary>
        void Down(float dt)
        {
            if (!m_Fell)
            {
                if (DeadTime < Tuning.Lives.RespawnDelay) return;
                m_Fell = true;
                LifeBank.Fall fall = Lives.Fallen();
                if (fall == LifeBank.Fall.Respawn) Respawn();
                else if (fall == LifeBank.Fall.Out) Sfx.Play(Sfx.GameOver);
                return;
            }
            if (!Lives.Offering) return;
            SoldierInput input = InputEnabled ? Input : default;
            if (input.FirePressed || input.JumpPressed)
            {
                Lives.TakeContinue();
                Respawn();
            }
            else if (Lives.Tick(dt)) Sfx.Play(Sfx.GameOver);
        }

        /// <summary>
        /// Rooms: someone watching has no soldier in play. Theirs is out for good, unseen and untouchable, and takes no
        /// input; the world still has one, so everything that looks for the player finds a soldier who's down.
        /// </summary>
        public void Spectate()
        {
            Lives.Retire();
            Dead = true;
            DeadTime = Tuning.Lives.RespawnDelay;
            m_Fell = true;
            InputEnabled = false;
        }

        /// <summary>A 1-up: a spare life, or back in for a player who was out of lives.</summary>
        public void GiveLife()
        {
            if (Lives.AddLife() && Dead) Respawn();
        }

        /// <summary>Comes back at the left edge of the screen.</summary>
        void Respawn()
        {
            Dead = false;
            DeadTime = 0f;
            m_Fell = false;
            Hp = MaxHp;
            Specials = Mathf.Max(Specials, Tuning.Lives.RespawnMinSpecials);
            Vel = Vector2.zero;
            m_Invuln = Tuning.Lives.RespawnInvulnerability;
            float x = World.View.Left + 2f;
            Pos = new Vector2(x, World.Level.TopSurfaceY(x));
            World.Scoring.Popup("BACK IN!", Pos + Vector2.up * 2f, Palette.CoopBlue);
        }

        public override void Render()
        {
            Go.SetActive(Riding == null);
            m_Kit.Render();
            if (Riding != null) return;
            base.Render();
            Go.transform.localPosition += (Vector3)m_Kit.ShakeOffset;
            m_Visual.Apply(Pose);
            if (Frost.Frozen && m_Ice == null) m_Ice = new FrostLook(Go.transform);
            m_Ice?.Render(this, Frost.Frozen);
        }
    }
}
