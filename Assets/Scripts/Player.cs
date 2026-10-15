using UnityEngine;

namespace PooketSlug
{
    /// <summary>The locally controlled soldier. How they move and their kit come from their <see cref="SoldierDef"/>.</summary>
    public sealed class Player : Actor
    {
        public readonly SoldierDef Def;
        /// <summary>Charges left for the special (<see cref="SoldierDef.Special"/>).</summary>
        public int Specials;
        /// <summary>A crate's gun, which replaces the primary while <see cref="GunAmmo"/> lasts.</summary>
        public WeaponDef Gun;
        public int GunAmmo;
        public bool Dead;
        public float DeadTime;
        public bool Crouching;
        public bool InputEnabled = true;
        /// <summary>This frame's orders, from <see cref="GameInput"/>; the world sets them before <see cref="Tick"/>.</summary>
        public SoldierInput Input;

        /// <summary>Seconds left slowed down by splashback from his own jet (tones2).</summary>
        public float SlowTimer;

        float m_Coyote, m_JumpBuffer, m_FireCooldown, m_Anim, m_Muzzle, m_Invuln, m_DropThrough;
        Vector2 m_Aim = Vector2.right;
        readonly PlayerVisual m_Visual;
        // spike kits (tones2): a primary or special that isn't a plain shot
        readonly WaterJet m_Jet;
        readonly RocketJump m_Rocket;

        public Player(World world, SoldierDef def, Vector2 feet, int skin)
            : base(world, "Player", feet, def.HalfWidth, def.StandHeight, def.MaxHp)
        {
            Def = def;
            Specials = def.StartSpecials;
            m_Visual = new PlayerVisual(Visual, def == SpikeContent.Tones2 ? Art.Tones2Skin : Art.PlayerSkins[skin], SortOrder.Player);
            if (def.Primary.Behaviour == WeaponBehaviour.Jet) m_Jet = new WaterJet(this, SpikeContent.Jet);
            if (def.Special.Behaviour == WeaponBehaviour.RocketJump) m_Rocket = new RocketJump(this, SpikeContent.Rocket);
            Grounded = true; // spawned standing on the ground (matters for the title-screen pose)
        }

        public bool Invulnerable => m_Invuln > 0f || Dead;
        /// <summary>Where the soldier is aiming: forward, up, or down in the air.</summary>
        public Vector2 Aim => m_Aim;
        public override bool IsDown => Dead;
        public int Skin { set => m_Visual.Skin = Art.PlayerSkins[value]; }

        public AvatarPose Pose => new AvatarPose
        {
            Facing = Facing,
            AimY = m_Aim.y > 0f ? 1 : m_Aim.y < 0f ? -1 : 0,
            Grounded = Grounded,
            Crouching = Crouching,
            Running = Grounded && Mathf.Abs(Vel.x) > 0.1f,
            Dead = Dead,
            Muzzle = m_Muzzle > 0f,
            Blink = m_Invuln > 0f && !Dead,
            Flash = FlashTimer > 0f,
            Anim = m_Anim,
            DeadTime = DeadTime,
        };

        public override void Tick(float dt)
        {
            if (Dead)
            {
                m_Jet?.Tick(dt, false);
                DeadTime += dt;
                Vel.y = Mathf.Max(Vel.y - Tuning.Physics.Gravity * dt, -Tuning.Physics.MaxFall);
                Vel.x = Mathf.MoveTowards(Vel.x, 0f, 6f * dt);
                MoveWithTiles(dt);
                if (World.Coop && DeadTime > Tuning.Coop.RespawnDelay) Respawn();
                return;
            }

            SoldierInput input = InputEnabled ? Input : default;
            float moveX = input.MoveX;
            bool up = input.Up, down = input.Down;

            // --- horizontal ---
            bool rooted = m_Rocket != null && m_Rocket.Rooted; // shaking before the rocket jump
            Crouching = Grounded && down && !rooted;
            Height = Crouching ? Def.CrouchHeight : Def.StandHeight;
            float speed = Crouching ? Def.CrawlSpeed : Def.RunSpeed;
            if (SlowTimer > 0f) speed *= SpikeContent.Jet.SplashSlow;
            Vel.x = rooted ? 0f : moveX * speed;
            if (moveX != 0f && !rooted) Facing = moveX > 0f ? 1 : -1;
            SlowTimer -= dt;

            // --- jumping (with coyote time + input buffering) ---
            m_Coyote = Grounded ? Tuning.Soldier.CoyoteTime : m_Coyote - dt;
            m_JumpBuffer = input.JumpPressed ? Tuning.Soldier.JumpBuffer : m_JumpBuffer - dt;
            m_DropThrough -= dt;
            if (m_JumpBuffer > 0f && m_Coyote > 0f && !rooted)
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
            bool rocketing = m_Rocket != null && m_Rocket.Rising; // a rocket jump isn't cut short by letting go of JUMP
            if (Vel.y > 0f && !input.Jump && !rocketing) gravity *= Tuning.Soldier.ShortHopGravity; // short hop when released
            Vel.y = Mathf.Max(Vel.y - gravity * dt, -Tuning.Physics.MaxFall);
            MoveWithTiles(dt, m_DropThrough > 0f);

            // Metal Slug style: the camera never scrolls back, and you can't leave it (co-op: either side).
            float minX = World.View.Left + HalfW;
            if (Pos.x < minX) Pos.x = minX;
            if (World.Coop) Pos.x = Mathf.Min(Pos.x, World.View.Right - HalfW);

            // --- aiming & shooting ---
            if (up) m_Aim = Vector2.up;
            else if (down && !Grounded) m_Aim = Vector2.down;
            else m_Aim = new Vector2(Facing, 0f);

            m_FireCooldown -= dt;
            bool gun = GunAmmo > 0;
            m_Jet?.Tick(dt, input.Fire && !gun && !rooted);
            bool wantsFire = gun ? input.Fire : (input.FirePressed || input.Fire);
            if (wantsFire && m_FireCooldown <= 0f && (gun || m_Jet == null)) Fire(gun ? Gun : Def.Primary);

            if (m_Rocket != null)
            {
                m_Rocket.Tick(dt, input.SpecialPressed);
            }
            else if (input.SpecialPressed && Specials > 0)
            {
                Specials--;
                WeaponDef special = Def.Special;
                World.Combat.FirePlayerShot(special, Pos + new Vector2(Facing * 0.3f, Height * 0.8f), special.LobVelocity(Facing, Vel));
                Sfx.PlayShot(special);
            }

            m_Invuln -= dt;
            m_Muzzle -= dt;
            FlashTimer -= dt;
            m_Anim += dt;
        }

        /// <summary>Where shots leave the gun, in the world.</summary>
        public Vector2 MuzzlePos
        {
            get
            {
                int aimY = m_Aim.y > 0f ? 1 : m_Aim.y < 0f ? -1 : 0;
                Vector2 local = PlayerVisual.MuzzleLocal(aimY, Crouching);
                return Pos + new Vector2(local.x * Facing, local.y);
            }
        }

        void Fire(WeaponDef weapon)
        {
            Vector2 origin = MuzzlePos;
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
            World.Combat.FirePlayerShot(weapon, origin, dir * weapon.Speed);
            m_FireCooldown = weapon.Cooldown;
            m_Muzzle = 0.05f;
            Sfx.PlayShot(weapon);
        }

        public void Hurt(int damage)
        {
            if (Invulnerable) return;
            Hp -= damage;
            FlashTimer = 0.08f;
            World.View.Shake(0.15f);
            if (Hp <= 0)
            {
                Hp = 0;
                Dead = true;
                DeadTime = 0f;
                Vel = new Vector2(-Facing * 3f, 9f);
                Sfx.Play(World.Coop ? Sfx.Hurt : Sfx.GameOver);
                World.Burst(Center, 14, new Color(0.85f, 0.2f, 0.15f));
            }
            else
            {
                m_Invuln = Tuning.Soldier.HurtInvulnerability;
                Sfx.Play(Sfx.Hurt);
            }
        }

        public void Heal(int amount) => Hp = Mathf.Min(MaxHp, Hp + amount);

        /// <summary>A crate's gun: it replaces the primary until its ammo runs out. The same gun again adds its ammo.</summary>
        public void GiveGun(WeaponDef gun)
        {
            if (gun != Gun) GunAmmo = 0;
            Gun = gun;
            GunAmmo += gun.Ammo;
        }

        /// <summary>Co-op only: come back at the left edge of the shared screen.</summary>
        void Respawn()
        {
            Dead = false;
            DeadTime = 0f;
            Hp = MaxHp;
            Specials = Mathf.Max(Specials, Tuning.Coop.RespawnMinSpecials);
            Vel = Vector2.zero;
            m_Invuln = Tuning.Coop.RespawnInvulnerability;
            float x = World.View.Left + 2f;
            Pos = new Vector2(x, World.Level.TopSurfaceY(x));
            World.Scoring.Popup("BACK IN!", Pos + Vector2.up * 2f, new Color(0.6f, 0.9f, 1f));
        }

        public override void Render()
        {
            base.Render();
            if (m_Rocket != null) Go.transform.localPosition += (Vector3)m_Rocket.ShakeOffset;
            m_Visual.Apply(Pose);
            m_Jet?.Render();
        }
    }
}
