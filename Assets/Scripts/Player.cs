using UnityEngine;

namespace PooketSlug
{
    /// <summary>The locally controlled soldier.</summary>
    public sealed class Player : Actor
    {
        public int Grenades = Tuning.Soldier.StartGrenades;
        public int HeavyAmmo;
        public bool Dead;
        public float DeadTime;
        public bool Crouching;
        public bool InputEnabled = true;

        float m_Coyote, m_JumpBuffer, m_FireCooldown, m_Anim, m_Muzzle, m_Invuln, m_DropThrough;
        Vector2 m_Aim = Vector2.right;
        readonly PlayerVisual m_Visual;

        public Player(World world, Vector2 feet, int skin)
            : base(world, "Player", feet, Tuning.Soldier.HalfWidth, Tuning.Soldier.StandHeight, Tuning.Soldier.MaxHp)
        {
            m_Visual = new PlayerVisual(Visual, Art.PlayerSkins[skin], SortOrder.Player);
            Grounded = true; // spawned standing on the ground (matters for the title-screen pose)
        }

        public bool Invulnerable => m_Invuln > 0f || Dead;
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
                DeadTime += dt;
                Vel.y = Mathf.Max(Vel.y - Tuning.Physics.Gravity * dt, -Tuning.Physics.MaxFall);
                Vel.x = Mathf.MoveTowards(Vel.x, 0f, 6f * dt);
                MoveWithTiles(dt);
                if (World.Coop && DeadTime > Tuning.Coop.RespawnDelay) Respawn();
                return;
            }

            float moveX = InputEnabled ? GameInput.MoveX : 0f;
            bool up = InputEnabled && GameInput.Up, down = InputEnabled && GameInput.Down;

            // --- horizontal ---
            Crouching = Grounded && down;
            Height = Crouching ? Tuning.Soldier.CrouchHeight : Tuning.Soldier.StandHeight;
            Vel.x = moveX * (Crouching ? Tuning.Soldier.CrawlSpeed : Tuning.Soldier.RunSpeed);
            if (moveX != 0f) Facing = moveX > 0f ? 1 : -1;

            // --- jumping (with coyote time + input buffering) ---
            m_Coyote = Grounded ? Tuning.Soldier.CoyoteTime : m_Coyote - dt;
            m_JumpBuffer = InputEnabled && GameInput.JumpPressed ? Tuning.Soldier.JumpBuffer : m_JumpBuffer - dt;
            m_DropThrough -= dt;
            if (m_JumpBuffer > 0f && m_Coyote > 0f)
            {
                if (down && TilePhysics.OnBridge(World.Level, Pos, HalfW))
                {
                    m_DropThrough = Tuning.Soldier.DropThroughTime;
                }
                else
                {
                    Vel.y = Tuning.Soldier.JumpVelocity;
                    Sfx.Play(Sfx.Jump, 0.6f);
                }
                m_JumpBuffer = 0f;
                m_Coyote = 0f;
            }

            float gravity = Tuning.Physics.Gravity;
            if (Vel.y > 0f && !(InputEnabled && GameInput.JumpHeld)) gravity *= Tuning.Soldier.ShortHopGravity; // short hop when released
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
            bool heavy = HeavyAmmo > 0;
            bool wantsFire = InputEnabled && (heavy ? GameInput.FireHeld : (GameInput.FirePressed || GameInput.FireHeld));
            if (wantsFire && m_FireCooldown <= 0f) Fire(heavy ? Content.HeavyMachineGun : Content.Pistol);

            if (InputEnabled && GameInput.GrenadePressed && Grenades > 0)
            {
                Grenades--;
                WeaponDef special = Content.GrenadeThrow;
                World.Combat.FirePlayerShot(special, Pos + new Vector2(Facing * 0.3f, Height * 0.8f), special.LobVelocity(Facing, Vel));
                Sfx.PlayShot(special);
            }

            m_Invuln -= dt;
            m_Muzzle -= dt;
            FlashTimer -= dt;
            m_Anim += dt;
        }

        void Fire(WeaponDef weapon)
        {
            int aimY = m_Aim.y > 0f ? 1 : m_Aim.y < 0f ? -1 : 0;
            Vector2 local = PlayerVisual.MuzzleLocal(aimY, Crouching);
            Vector2 origin = Pos + new Vector2(local.x * Facing, local.y);
            Vector2 dir = m_Aim;
            if (weapon.Spread > 0f)
            {
                float spread = World.Rng.Range(-weapon.Spread, weapon.Spread) * Mathf.Deg2Rad;
                dir = new Vector2(dir.x * Mathf.Cos(spread) - dir.y * Mathf.Sin(spread), dir.x * Mathf.Sin(spread) + dir.y * Mathf.Cos(spread));
            }
            if (weapon.Ammo > 0)
            {
                // a crate's gun, until it runs dry
                HeavyAmmo--;
                if (HeavyAmmo == 0) World.Scoring.Popup("OUT OF AMMO", Pos + Vector2.up * 2f, new Color(1f, 0.6f, 0.4f));
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

        /// <summary>Co-op only: come back at the left edge of the shared screen.</summary>
        void Respawn()
        {
            Dead = false;
            DeadTime = 0f;
            Hp = MaxHp;
            Grenades = Mathf.Max(Grenades, Tuning.Coop.RespawnMinGrenades);
            Vel = Vector2.zero;
            m_Invuln = Tuning.Coop.RespawnInvulnerability;
            float x = World.View.Left + 2f;
            Pos = new Vector2(x, World.Level.TopSurfaceY(x));
            World.Scoring.Popup("BACK IN!", Pos + Vector2.up * 2f, new Color(0.6f, 0.9f, 1f));
        }

        public override void Render()
        {
            base.Render();
            m_Visual.Apply(Pose);
        }
    }
}
