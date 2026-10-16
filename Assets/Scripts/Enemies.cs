using UnityEngine;

namespace PooketSlug
{
    /// <summary>An enemy: the numbers come from its <see cref="EnemyDef"/>, the behaviour from the subclass (its brain).</summary>
    public abstract class Enemy : Actor
    {
        public readonly EnemyDef Def;
        public int NetId = -1;   // its NetIds id: the enemy's identity in co-op; -1 stays local
        public bool Dying;
        protected float DyingTime, Anim;
        /// <summary>Damage soaked in from droplets that hasn't added up to a whole point yet (tones2's jet).</summary>
        public float Soak;
        /// <summary>Seconds left showing the poison of toxic mud.</summary>
        public float PoisonTimer;
        /// <summary>Seconds left showing the burn of tones2's sludge.</summary>
        public float BurnTimer;
        /// <summary>This enemy's hologram look (every goon has one), or null for the plain one.</summary>
        public HoloLook Look;
        /// <summary>Our soldier has had the kill (a co-op follower sees the death only when the authority reports it).</summary>
        public bool KillCredited;
        protected static float Gravity => Tuning.Physics.Gravity;
        protected static float MaxFall => Tuning.Physics.MaxFall;
        Vector2 m_NetPos;
        bool m_NetPlaced;

        protected Enemy(World world, EnemyDef def, Vector2 feet)
            : base(world, def.Name, feet, def.HalfWidth, def.Height, def.Hp)
        {
            Def = def;
        }

        public int Score => Def.Score;
        /// <summary>Seconds since it started dying.</summary>
        public float DyingSeconds => DyingTime;

        public bool Shootable => !Dying && !Removed;
        /// <summary>Nearest soldier (local or remote), refreshed every authoritative tick.</summary>
        protected Actor Target { get; private set; }

        public virtual void Hurt(int damage)
        {
            if (!Shootable) return;
            if (!World.IsAuthority)
            {
                FlashHit(); // co-op follower: the authority applies the damage
                return;
            }
            Hp -= damage;
            FlashHit();
            if (Hp <= 0) Die();
        }

        public void FlashHit()
        {
            Look?.OnHit();
            FlashTimer = 0.07f;
            Sfx.Play(Sfx.Hit, 0.5f);
        }

        protected virtual void Die()
        {
            Dying = true;
            World.Scoring.Add(Score, Center + Vector2.up * 0.6f, true);
        }

        public override void Tick(float dt)
        {
            FlashTimer -= dt;
            Anim += dt;
            PoisonTimer -= dt;
            BurnTimer -= dt;
            if (Dying)
            {
                DyingTime += dt;
                if (Look != null)
                {
                    if (Look.TickDying(DyingTime)) Remove();
                    return;
                }
                TickDying(dt);
                return;
            }
            if (!World.IsAuthority)
            {
                TickPuppet(dt);
                TickFollower(dt);
                return;
            }
            Target = World.Combat.NearestAvatar(Pos);
            TickAlive(dt);
            Shove = Vector2.MoveTowards(Shove, Vector2.zero, 10f * dt);
            if (Pos.x < World.View.Left - 4f) Remove(); // left behind
        }

        protected abstract void TickAlive(float dt);

        /// <summary>Co-op follower: anything beyond gliding to the snapshot, each tick while it's alive.</summary>
        protected virtual void TickFollower(float dt) { }

        /// <summary>Co-op follower: glide toward the authority's latest snapshot.</summary>
        void TickPuppet(float dt)
        {
            if (!m_NetPlaced) return;
            const float snap = Tuning.Coop.RemoteEnemySnap;
            if ((m_NetPos - Pos).sqrMagnitude > snap * snap) Pos = m_NetPos;
            else Pos = Vector2.Lerp(Pos, m_NetPos, 1f - Mathf.Exp(-Tuning.Coop.RemoteEnemySmoothing * dt));
        }

        /// <summary>State the authority replicates for this enemy.</summary>
        public int SnapshotFlags =>
            (Dying ? NetEnemyState.Dying : 0) | (Facing > 0 ? NetEnemyState.FacingRight : 0) |
            (Mathf.Abs(Vel.x) > 0.05f ? NetEnemyState.Moving : 0) | (Grounded ? NetEnemyState.Grounded : 0) | ExtraFlags;

        protected virtual int ExtraFlags => 0;
        protected virtual void OnSnapshot(int flags) { }

        /// <summary>Co-op follower: adopt the authority's view of this enemy.</summary>
        public void ApplySnapshot(in NetEnemyState s)
        {
            m_NetPos = s.Pos;
            if (!m_NetPlaced)
            {
                Pos = s.Pos;
                m_NetPlaced = true;
            }
            if (Dying) return;
            Facing = (s.Flags & NetEnemyState.FacingRight) != 0 ? 1 : -1;
            Vel = new Vector2((s.Flags & NetEnemyState.Moving) != 0 ? Facing : 0f, 0f);
            Grounded = (s.Flags & NetEnemyState.Grounded) != 0;
            Hp = s.Hp;
            OnSnapshot(s.Flags);
            if ((s.Flags & NetEnemyState.Dying) != 0)
            {
                Dying = true;
                DyingTime = 0f;
                // shot down; a weasel that popped on someone or one of kie's copies glitching out scores nothing
                if (Hp <= 0) World.Scoring.Popup(Score.ToString(), Center + Vector2.up * 0.6f, Color.white);
            }
        }

        /// <summary>Default death: knocked back, topples over, blinks out.</summary>
        protected virtual void TickDying(float dt)
        {
            if (DyingTime < dt * 1.5f) Vel = new Vector2(-Facing * 2.5f, 6f);
            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            Vel.x = Mathf.MoveTowards(Vel.x, 0f, 4f * dt);
            MoveWithTiles(dt);
            if (DyingTime > 1.1f) Remove();
        }

        public override void Render()
        {
            base.Render();
            if (Dying && Look == null)
            {
                Visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Min(90f, DyingTime * 400f) * Facing);
                Visual.gameObject.SetActive(DyingTime < 0.6f || ((int)(DyingTime * 16f) % 2 == 0));
            }
        }

        protected void FaceTarget()
        {
            float dx = Target.Pos.x - Pos.x;
            if (Mathf.Abs(dx) > 0.2f) Facing = dx > 0f ? 1 : -1;
        }
    }

    /// <summary>Rifleman: advances, stops at range and fires level shots (crouch to dodge).</summary>
    public sealed class Soldier : Enemy
    {
        readonly SpriteRenderer m_Legs, m_Torso, m_Flash;
        float m_Cooldown, m_Muzzle;

        public Soldier(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Legs = AddRenderer(Visual, "Legs", Art.SoldierLegsIdle, SortOrder.Enemies);
            m_Torso = AddRenderer(Visual, "Torso", Art.SoldierTorso, SortOrder.Enemies + 1);
            m_Flash = AddRenderer(Visual, "Muzzle", Art.MuzzleFlash, SortOrder.Enemies + 2);
            m_Flash.transform.localPosition = new Vector3(14f / Art.PPU, 14.5f / Art.PPU, 0f);
            m_Flash.enabled = false;
            m_Cooldown = World.Rng.Range(0.6f, 1.4f);
        }

        protected override void TickAlive(float dt)
        {
            FaceTarget();
            float dist = Mathf.Abs(Target.Pos.x - Pos.x);
            bool onScreen = Pos.x < World.View.Right - 0.5f;

            Vel.x = 0f;
            if (dist > 7f && !Target.IsDown) Vel.x = Facing * 1.8f;

            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveResult r = MoveWithTiles(dt);
            if (r.HitWall && Grounded) Vel.y = 11f; // hop small obstacles

            m_Cooldown -= dt;
            m_Muzzle -= dt;
            if (onScreen && dist < 9f && m_Cooldown <= 0f && !Target.IsDown)
            {
                var origin = Pos + new Vector2(Facing * 0.8f, 0.95f);
                World.Combat.FireEnemyShot(Content.Rifle, origin, new Vector2(Facing * Content.Rifle.Speed, 0f));
                m_Muzzle = 0.06f;
                m_Cooldown = World.Rng.Range(1.5f, 2.6f);
            }
        }

        public override void Render()
        {
            base.Render();
            bool walking = Mathf.Abs(Vel.x) > 0.1f && Grounded && !Dying;
            m_Legs.sprite = Flashed(walking ? Art.SoldierLegsRun[(int)(Anim * 8f) % 4] : Art.SoldierLegsIdle);
            m_Torso.sprite = Flashed(Art.SoldierTorso);
            m_Flash.enabled = m_Muzzle > 0f && !Dying;
        }
    }

    /// <summary>Knife runner: sprints at the player and slashes up close.</summary>
    public sealed class Runner : Enemy
    {
        readonly SpriteRenderer m_Legs, m_Torso;
        float m_AttackCooldown;

        public Runner(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Legs = AddRenderer(Visual, "Legs", Art.RunnerLegsRun[0], SortOrder.Enemies);
            m_Torso = AddRenderer(Visual, "Torso", Art.RunnerTorso, SortOrder.Enemies + 1);
        }

        protected override void TickAlive(float dt)
        {
            bool active = Pos.x < World.View.Right + 0.5f;
            m_AttackCooldown -= dt;

            Vel.x = 0f;
            if (active && !Target.IsDown)
            {
                FaceTarget();
                float dx = Target.Pos.x - Pos.x;
                if (Mathf.Abs(dx) > 0.6f && m_AttackCooldown < 0.5f) Vel.x = Facing * 4.2f;
            }

            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveResult r = MoveWithTiles(dt);
            if (r.HitWall && Grounded) Vel.y = 13f;

            if (!Target.IsDown && m_AttackCooldown <= 0f && Bounds.Overlaps(Target.Bounds))
            {
                World.Combat.DamageAvatar(Target, 1);
                m_AttackCooldown = 1.0f;
                Sfx.Play(Sfx.Throw, 0.8f);
            }
        }

        public override void Render()
        {
            base.Render();
            Sprite legs = !Grounded ? Art.RunnerLegsJump : Mathf.Abs(Vel.x) > 0.1f ? Art.RunnerLegsRun[(int)(Anim * 14f) % 4] : Art.RunnerLegsRun[0];
            m_Legs.sprite = Flashed(legs);
            m_Torso.sprite = Flashed(Art.RunnerTorso);
            // slash lunge
            m_Torso.transform.localPosition = new Vector3(m_AttackCooldown > 0.75f ? 2f / Art.PPU : 0f, 0f, 0f);
        }
    }

    /// <summary>Hover drone: shadows the player from above and drops bombs.</summary>
    public sealed class Drone : Enemy
    {
        readonly SpriteRenderer m_Sr;
        readonly float m_Phase;
        float m_BombCooldown = 1.2f;

        public Drone(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Sr = AddRenderer(Visual, "Body", Art.Drone[0], SortOrder.Enemies + 2);
            m_Sr.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            m_Phase = World.Rng.Range(0f, 6f);
        }

        protected override void TickAlive(float dt)
        {
            if (Pos.x > World.View.Right + 1f) return; // wait off-screen until the camera arrives
            float tx = Target.Pos.x + Mathf.Sin(Anim * 1.3f + m_Phase) * 2.5f;
            float ty = Mathf.Clamp(Target.Pos.y + 5.2f, 6f, 9.5f) + Mathf.Sin(Anim * 3f) * 0.3f;
            Vector2 desired = new Vector2(tx - Pos.x, ty - Pos.y) * 1.5f;
            Vel = Vector2.MoveTowards(Vel, Vector2.ClampMagnitude(desired, 3.2f), 8f * dt);
            Pos += (Vel + Shove) * dt;
            Facing = Vel.x >= 0f ? 1 : -1;

            m_BombCooldown -= dt;
            if (m_BombCooldown <= 0f && Mathf.Abs(Target.Pos.x - Pos.x) < 1f && !Target.IsDown)
            {
                World.Combat.FireEnemyShot(Content.DroneBomb, Pos + new Vector2(0f, -0.1f), Content.DroneBomb.LobVelocity(Facing, Vel));
                m_BombCooldown = 1.8f;
            }
        }

        protected override void TickDying(float dt)
        {
            Vel.y -= Gravity * 0.5f * dt;
            Pos += Vel * dt;
            if (World.Level.IsSolidAt(Pos) || DyingTime > 2f)
            {
                World.Combat.Explode(Pos + Vector2.up * 0.3f, 0.8f, 0, false, 0.6f);
                Remove();
            }
        }

        public override void Render()
        {
            base.Render();
            m_Sr.sprite = Flashed(Art.Drone[(int)(Anim * 20f) % 2]);
            if (Dying && Look == null)
            {
                Visual.gameObject.SetActive(true);
                Visual.localRotation = Quaternion.Euler(0f, 0f, DyingTime * 540f);
            }
        }
    }

    /// <summary>An end-of-level boss: it wakes when the camera reaches it, and the HUD shows its health bar while it fights.</summary>
    public abstract class BossEnemy : Enemy
    {
        public bool Engaged;

        protected BossEnemy(World world, EnemyDef def, Vector2 feet) : base(world, def, feet) { }

        /// <summary>What the HUD and the intro call it.</summary>
        public virtual string DisplayName => Def.Name.ToUpperInvariant();

        /// <summary>Authority: what the boss's network object carries (phase 0: nothing yet).</summary>
        public virtual NetBossState NetState => default;

        /// <summary>Follower: take up the authority's boss state (each boss ignores another's phase).</summary>
        public virtual void ApplyNet(in NetBossState s) { }
    }
}
