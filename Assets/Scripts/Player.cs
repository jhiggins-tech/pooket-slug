using UnityEngine;

namespace PooketSlug
{
    /// <summary>The locally controlled soldier.</summary>
    public sealed class Player : Actor
    {
        const float RunSpeed = 5.5f, CrawlSpeed = 2f, JumpVelocity = 17f, Gravity = 40f, MaxFall = 18f;
        const float StandHeight = 1.35f, CrouchHeight = 0.8f;
        const float PistolCooldown = 0.18f, HeavyCooldown = 0.075f, InvulnTime = 1.4f;
        public const float RespawnDelay = 2.5f; // co-op

        public int Grenades = 10;
        public int HeavyAmmo;
        public bool Dead;
        public float DeadTime;
        public bool Crouching;
        public bool InputEnabled = true;

        float m_Coyote, m_JumpBuffer, m_FireCooldown, m_Anim, m_Muzzle, m_Invuln, m_DropThrough;
        Vector2 m_Aim = Vector2.right;
        readonly PlayerVisual m_Visual;

        public Player(World world, Vector2 feet, int skin) : base(world, "Player", feet, 0.3f, StandHeight, 5)
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
                Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
                Vel.x = Mathf.MoveTowards(Vel.x, 0f, 6f * dt);
                MoveWithTiles(dt);
                if (World.Coop && DeadTime > RespawnDelay) Respawn();
                return;
            }

            float moveX = InputEnabled ? GameInput.MoveX : 0f;
            bool up = InputEnabled && GameInput.Up, down = InputEnabled && GameInput.Down;

            // --- horizontal ---
            Crouching = Grounded && down;
            Height = Crouching ? CrouchHeight : StandHeight;
            Vel.x = moveX * (Crouching ? CrawlSpeed : RunSpeed);
            if (moveX != 0f) Facing = moveX > 0f ? 1 : -1;

            // --- jumping (with coyote time + input buffering) ---
            m_Coyote = Grounded ? 0.1f : m_Coyote - dt;
            m_JumpBuffer = InputEnabled && GameInput.JumpPressed ? 0.12f : m_JumpBuffer - dt;
            m_DropThrough -= dt;
            if (m_JumpBuffer > 0f && m_Coyote > 0f)
            {
                if (down && TilePhysics.OnBridge(World.Level, Pos, HalfW))
                {
                    m_DropThrough = 0.25f;
                }
                else
                {
                    Vel.y = JumpVelocity;
                    Sfx.Play(Sfx.Jump, 0.6f);
                }
                m_JumpBuffer = 0f;
                m_Coyote = 0f;
            }

            float gravity = Gravity;
            if (Vel.y > 0f && !(InputEnabled && GameInput.JumpHeld)) gravity *= 2.2f; // short hop when released
            Vel.y = Mathf.Max(Vel.y - gravity * dt, -MaxFall);
            MoveWithTiles(dt, m_DropThrough > 0f);

            // Metal Slug style: the camera never scrolls back, and you can't leave it (co-op: either side).
            float minX = World.CameraLeft + HalfW;
            if (Pos.x < minX) Pos.x = minX;
            if (World.Coop) Pos.x = Mathf.Min(Pos.x, World.CameraRight - HalfW);

            // --- aiming & shooting ---
            if (up) m_Aim = Vector2.up;
            else if (down && !Grounded) m_Aim = Vector2.down;
            else m_Aim = new Vector2(Facing, 0f);

            m_FireCooldown -= dt;
            bool heavy = HeavyAmmo > 0;
            bool wantsFire = InputEnabled && (heavy ? GameInput.FireHeld : (GameInput.FirePressed || GameInput.FireHeld));
            if (wantsFire && m_FireCooldown <= 0f) Fire(heavy);

            if (InputEnabled && GameInput.GrenadePressed && Grenades > 0)
            {
                Grenades--;
                var throwVel = new Vector2(Facing * 6.5f + Vel.x * 0.3f, 9f);
                World.FirePlayerProjectile(ProjectileKind.Grenade, Pos + new Vector2(Facing * 0.3f, Height * 0.8f), throwVel);
                Sfx.Play(Sfx.Throw);
            }

            m_Invuln -= dt;
            m_Muzzle -= dt;
            FlashTimer -= dt;
            m_Anim += dt;
        }

        void Fire(bool heavy)
        {
            int aimY = m_Aim.y > 0f ? 1 : m_Aim.y < 0f ? -1 : 0;
            Vector2 local = PlayerVisual.MuzzleLocal(aimY, Crouching);
            Vector2 origin = Pos + new Vector2(local.x * Facing, local.y);
            Vector2 dir = m_Aim;
            float speed = heavy ? 20f : 16f;
            if (heavy)
            {
                // heavy machine gun: slight spread
                float spread = Random.Range(-4f, 4f) * Mathf.Deg2Rad;
                dir = new Vector2(dir.x * Mathf.Cos(spread) - dir.y * Mathf.Sin(spread), dir.x * Mathf.Sin(spread) + dir.y * Mathf.Cos(spread));
                HeavyAmmo--;
                if (HeavyAmmo == 0) World.Popup("OUT OF AMMO", Pos + Vector2.up * 2f, new Color(1f, 0.6f, 0.4f));
            }
            World.FirePlayerProjectile(heavy ? ProjectileKind.Heavy : ProjectileKind.Bullet, origin, dir * speed);
            m_FireCooldown = heavy ? HeavyCooldown : PistolCooldown;
            m_Muzzle = 0.05f;
            Sfx.Play(heavy ? Sfx.Heavy : Sfx.Shot, heavy ? 0.5f : 0.6f);
        }

        public void Hurt(int damage)
        {
            if (Invulnerable) return;
            Hp -= damage;
            FlashTimer = 0.08f;
            World.Shake(0.15f);
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
                m_Invuln = InvulnTime;
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
            Grenades = Mathf.Max(Grenades, 5);
            Vel = Vector2.zero;
            m_Invuln = 2.5f;
            float x = World.CameraLeft + 2f;
            Pos = new Vector2(x, World.Level.TopSurfaceY(x));
            World.Popup("BACK IN!", Pos + Vector2.up * 2f, new Color(0.6f, 0.9f, 1f));
        }

        public override void Render()
        {
            base.Render();
            m_Visual.Apply(Pose);
        }
    }
}
