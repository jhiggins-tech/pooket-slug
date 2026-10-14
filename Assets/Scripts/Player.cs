using UnityEngine;

namespace PooketSlug
{
    public sealed class Player : Actor
    {
        const float RunSpeed = 5.5f, CrawlSpeed = 2f, JumpVelocity = 17f, Gravity = 40f, MaxFall = 18f;
        const float StandHeight = 1.35f, CrouchHeight = 0.8f;
        const float PistolCooldown = 0.18f, HeavyCooldown = 0.075f, InvulnTime = 1.4f;

        public int Grenades = 10;
        public int HeavyAmmo;
        public bool Dead;
        public float DeadTime;
        public bool Crouching;
        public bool InputEnabled = true;

        float m_Coyote, m_JumpBuffer, m_FireCooldown, m_Anim, m_Muzzle, m_Invuln, m_DropThrough;
        Vector2 m_Aim = Vector2.right;
        readonly SpriteRenderer m_Legs, m_Torso, m_Flash;

        public Player(World world, Vector2 feet) : base(world, "Player", feet, 0.3f, StandHeight, 5)
        {
            m_Legs = AddRenderer(Visual, "Legs", Art.PlayerLegsIdle, SortOrder.Player);
            m_Torso = AddRenderer(Visual, "Torso", Art.PlayerTorsoForward, SortOrder.Player + 1);
            m_Flash = AddRenderer(Visual, "Muzzle", Art.MuzzleFlash, SortOrder.Player + 2);
            m_Flash.enabled = false;
            Grounded = true; // spawned standing on the ground (matters for the title-screen pose)
        }

        public bool Invulnerable => m_Invuln > 0f || Dead;

        public override void Tick(float dt)
        {
            if (Dead)
            {
                DeadTime += dt;
                Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
                Vel.x = Mathf.MoveTowards(Vel.x, 0f, 6f * dt);
                MoveWithTiles(dt);
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

            // Metal Slug style: the camera never scrolls back, and you can't leave it.
            float minX = World.CameraLeft + HalfW;
            if (Pos.x < minX) Pos.x = minX;

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
                World.Projectiles.Add(new Projectile(World, ProjectileKind.Grenade, Pos + new Vector2(Facing * 0.3f, Height * 0.8f), throwVel, false));
                Sfx.Play(Sfx.Throw);
            }

            m_Invuln -= dt;
            m_Muzzle -= dt;
            FlashTimer -= dt;
            m_Anim += dt;
        }

        void Fire(bool heavy)
        {
            Vector2 local = MuzzleLocal();
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
            var kind = heavy ? ProjectileKind.Heavy : ProjectileKind.Bullet;
            World.Projectiles.Add(new Projectile(World, kind, origin, dir * speed, false));
            m_FireCooldown = heavy ? HeavyCooldown : PistolCooldown;
            m_Muzzle = 0.05f;
            Sfx.Play(heavy ? Sfx.Heavy : Sfx.Shot, heavy ? 0.5f : 0.6f);
        }

        /// <summary>Gun tip position relative to the feet, for a right-facing player (px / 16).</summary>
        Vector2 MuzzleLocal()
        {
            if (m_Aim.y > 0f) return new Vector2(3.5f, 30.5f) / Art.PPU;
            if (m_Aim.y < 0f) return new Vector2(2.5f, 3.5f) / Art.PPU;
            return new Vector2(11.5f, Crouching ? 8.5f : 14.5f) / Art.PPU;
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
                Vel = new Vector2(-Facing * 3f, 9f);
                Sfx.Play(Sfx.GameOver);
                World.Burst(Center, 14, new Color(0.85f, 0.2f, 0.15f));
            }
            else
            {
                m_Invuln = InvulnTime;
                Sfx.Play(Sfx.Hurt);
            }
        }

        public void Heal(int amount) => Hp = Mathf.Min(MaxHp, Hp + amount);

        public override void Render()
        {
            base.Render();

            bool running = Grounded && Mathf.Abs(Vel.x) > 0.1f;
            int runFrame = (int)(m_Anim * 12f) % 4;
            Sprite legs;
            if (Dead || !Grounded) legs = Art.PlayerLegsJump;
            else if (Crouching) legs = Art.PlayerLegsCrouch;
            else if (running) legs = Art.PlayerLegsRun[runFrame];
            else legs = Art.PlayerLegsIdle;

            Sprite torso = m_Aim.y > 0f ? Art.PlayerTorsoUp : m_Aim.y < 0f ? Art.PlayerTorsoDown : Art.PlayerTorsoForward;
            m_Legs.sprite = Flashed(legs);
            m_Torso.sprite = Flashed(torso);

            float bob = running && (runFrame == 0 || runFrame == 2) ? -1f : 0f;
            float drop = Crouching ? -ArtData.CrouchDrop : bob;
            m_Torso.transform.localPosition = new Vector3(0f, drop / Art.PPU, 0f);

            m_Flash.enabled = m_Muzzle > 0f && !Dead;
            if (m_Flash.enabled)
            {
                // Visual is mirrored by Facing, so work in right-facing local space.
                Vector2 localAim = new Vector2(Mathf.Abs(m_Aim.x), m_Aim.y);
                Vector2 tip = MuzzleLocal() + localAim * (3f / Art.PPU);
                m_Flash.transform.localPosition = new Vector3(tip.x, tip.y, 0f);
            }

            // blink while invulnerable, spin when dead
            Visual.gameObject.SetActive(!(m_Invuln > 0f && ((int)(m_Invuln * 20f) % 2 == 0)) && !(Dead && DeadTime > 1.2f && ((int)(DeadTime * 12f) % 2 == 0)));
            Visual.localRotation = Dead ? Quaternion.Euler(0f, 0f, Mathf.Min(90f, DeadTime * 300f) * Facing) : Quaternion.identity;
        }
    }
}
