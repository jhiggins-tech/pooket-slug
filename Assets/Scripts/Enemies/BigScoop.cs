using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The Big Scoop, the sign yard's miniboss (<see cref="BigScoopSpec"/>, his brain <see cref="BigScoopBrain"/>): an
    /// oversized Trollogram heavy who drops into the yard as the camera reaches him and holds it there until he's down.
    /// He slams (a shockwave each way along the floor: jump it), hurls a scoop where you stand (move), and charges
    /// across the yard (jump over him), each after a wind-up. A nitrogen tank's freeze stops him, and frozen he takes
    /// double: the damage window.
    /// <para>
    /// Co-op: the authority runs him; his shockwaves and scoops are enemy shots, and whether he's awake and which move
    /// he's making ride in his snapshot flags, so every screen holds the yard and poses his scoop.
    /// </para>
    /// </summary>
    public sealed class BigScoop : Enemy
    {
        const int AwakeFlag = NetEnemyState.Engaged, MoveA = 32, MoveB = 128; // the move: A slam, B hurl, A|B charge
        const float DropHeight = 7f;
        static readonly Color BarBack = new Color(0.11f, 0.08f, 0.12f, 0.85f), BarFill = new Color(1f, 0.45f, 0.75f);

        static BigScoopSpec Spec => Content.BigScoopMoves;
        readonly BigScoopBrain m_Brain = new BigScoopBrain(Content.BigScoopMoves);
        readonly SpriteRenderer m_Body, m_Scoop, m_BarBack, m_BarFill;
        readonly Transform m_Arm;
        bool m_Awake, m_Landed, m_HitThisCharge, m_WasFrozen;
        float m_Drop;
        ScoopMove m_NetMove;

        public BigScoop(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Body = AddRenderer(Visual, "Body", CreameryArt.Heavy, SortOrder.Enemies);
            var arm = new GameObject("Arm");
            m_Arm = arm.transform;
            m_Arm.SetParent(Visual, false);
            m_Arm.localPosition = new Vector3(8f / Art.PPU, 24f / Art.PPU, 0f);
            m_Scoop = AddRenderer(m_Arm, "Scoop", CreameryArt.BigScoop, SortOrder.Enemies + 1);
            m_BarBack = Bar("Health", SortOrder.Effects, BarBack);
            m_BarFill = Bar("Health fill", SortOrder.Effects + 1, BarFill);
            Facing = -1;
        }

        public override bool Freezable => true;
        bool Awake => World.IsAuthority ? m_Awake : (m_Awake = m_Awake || m_NetAwake);
        bool m_NetAwake;
        ScoopMove Move => World.IsAuthority ? m_Brain.Move : m_NetMove;

        SpriteRenderer Bar(string name, int order, Color color)
        {
            SpriteRenderer sr = AddRenderer(Go.transform, name, Art.Pixel, order);
            sr.color = color;
            sr.enabled = false;
            return sr;
        }

        /// <summary>Frozen, every hit counts double.</summary>
        public override void Hurt(int damage)
        {
            if (!Awake) return;
            base.Hurt(Frost.Frozen ? damage * Spec.FrozenDamage : damage);
        }

        protected override void Die()
        {
            base.Die();
            Look?.Big(0.5f, 1.6f, 7f);
            World.ReleaseArena();
            World.HitStop(0.12f);
            Sfx.Play(Sfx.BigExplosion, 0.7f);
        }

        protected override int ExtraFlags =>
            (m_Awake ? AwakeFlag : 0) | (m_Brain.Move == ScoopMove.Slam || m_Brain.Move == ScoopMove.Charge ? MoveA : 0) |
            (m_Brain.Move == ScoopMove.Hurl || m_Brain.Move == ScoopMove.Charge ? MoveB : 0);

        protected override void OnSnapshot(int flags)
        {
            m_NetAwake = (flags & AwakeFlag) != 0;
            bool a = (flags & MoveA) != 0, b = (flags & MoveB) != 0;
            m_NetMove = a && b ? ScoopMove.Charge : a ? ScoopMove.Slam : b ? ScoopMove.Hurl : ScoopMove.None;
        }

        protected override void TickFollower(float dt)
        {
            if (Awake) Wake(false);
        }

        /// <summary>He's in the yard: it's held until he's down (on every screen: a follower's from his snapshot).</summary>
        void Wake(bool first)
        {
            if (World.ArenaHeld) return;
            World.HoldArena(Pos.x + 9f);
            if (!first) return;
            World.Scoring.Popup("THE BIG SCOOP!", Pos + Vector2.up * 4f, BarFill);
            Sfx.Play(Sfx.Siren, 0.5f);
        }

        protected override void TickAlive(float dt)
        {
            if (!m_Awake)
            {
                if (World.View.Right < Pos.x + HalfW + 1f) return; // waits up out of sight
                if (Pos.x < World.View.Left) return; // passed by (a restart further on, ?test=boss): he's left behind
                m_Awake = true;
                m_Drop = DropHeight;
                Wake(true);
            }
            if (!m_Landed)
            {
                // drops into the yard from above
                m_Drop = Mathf.Max(0f, m_Drop - 16f * dt);
                if (m_Drop > 0f) return;
                m_Landed = true;
                World.View.Shake(0.35f);
                World.Burst(Pos + Vector2.up * 0.2f, 20, new Color(0.85f, 0.62f, 0.35f));
                Sfx.Play(Sfx.Thump, 1f);
            }

            float dx = Target.Pos.x - Pos.x;
            bool acting = m_Brain.Phase == ScoopPhase.Act;
            if (!(acting && m_Brain.Move == ScoopMove.Charge)) FaceTarget();
            bool went = m_Brain.Tick(dt, Target.IsDown ? 99f : dx, Hp / (float)MaxHp, World.Rng);
            if (went) Act();

            Vel.x = 0f;
            if (m_Brain.Phase == ScoopPhase.Rest && Mathf.Abs(dx) > 2f && !Target.IsDown) Vel.x = Facing * Spec.WalkSpeed;
            if (m_Brain.Phase == ScoopPhase.Act && m_Brain.Move == ScoopMove.Charge) Vel.x = Facing * Spec.ChargeSpeed;
            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveResult r = MoveWithTiles(dt);
            if (m_Brain.Phase == ScoopPhase.Act && m_Brain.Move == ScoopMove.Charge)
            {
                bool edge = Pos.x - HalfW <= World.View.Left + 0.3f || Pos.x + HalfW >= World.View.Right - 0.3f;
                if (r.HitWall || edge)
                {
                    m_Brain.StopCharge();
                    World.View.Shake(0.25f);
                    Sfx.Play(Sfx.Thump, 0.8f);
                }
                if (!m_HitThisCharge && HitsSoldier()) m_HitThisCharge = true;
            }
            Pos.x = Mathf.Clamp(Pos.x, World.View.Left + HalfW, World.View.Right - HalfW); // he stays in the yard
        }

        bool HitsSoldier()
        {
            Rect body = Bounds;
            if (!World.Player.IsDown && body.Overlaps(World.Player.Bounds))
            {
                World.Combat.DamageAvatar(World.Player, 1);
                return true;
            }
            foreach (RemotePlayer rp in World.Remotes)
                if (!rp.IsDown && body.Overlaps(rp.Bounds))
                {
                    World.Combat.DamageAvatar(rp, 1);
                    return true;
                }
            return false;
        }

        void Act()
        {
            switch (m_Brain.Move)
            {
                case ScoopMove.Slam:
                    float floor = Pos.y + Content.ScoopWave.Radius;
                    float speed = Content.ScoopSlam.Speed;
                    World.Combat.FireEnemyShot(Content.ScoopSlam, new Vector2(Pos.x + 1.1f, floor), new Vector2(speed, 0f));
                    World.Combat.FireEnemyShot(Content.ScoopSlam, new Vector2(Pos.x - 1.1f, floor), new Vector2(-speed, 0f));
                    World.Burst(Pos + new Vector2(Facing * 1.2f, 0.2f), 16, new Color(1f, 0.75f, 0.85f));
                    break;
                case ScoopMove.Hurl:
                    Vector2 from = Pos + new Vector2(Facing * 0.6f, 2.8f);
                    Vector2 to = new Vector2(Target.Pos.x, World.Level.TopSurfaceY(Target.Pos.x));
                    World.Combat.FireEnemyShot(Content.ScoopHurl, from, GrenadierSpec.LobTo(from, to, Content.HurledScoop.Gravity, Spec.HurlFlight));
                    break;
                case ScoopMove.Charge:
                    m_HitThisCharge = false;
                    Sfx.Play(Sfx.RocketRumble, 0.6f);
                    break;
            }
        }

        public override void Render()
        {
            base.Render();
            bool awake = Awake;
            Visual.gameObject.SetActive(awake || Dying);
            Go.transform.localPosition += new Vector3(0f, m_Drop, 0f); // (the Trollogram look moves Visual itself)
            m_Body.sprite = Flashed(CreameryArt.Heavy);
            m_Scoop.sprite = Flashed(CreameryArt.BigScoop);

            // the scoop tells the move: raised for a slam, back for a hurl, levelled for a charge
            ScoopMove move = Move;
            bool telling = World.IsAuthority ? m_Brain.Phase == ScoopPhase.Tell : move != ScoopMove.None;
            float angle = 25f + Mathf.Sin(Anim * 3f) * 6f;
            if (move == ScoopMove.Slam) angle = telling ? 115f : -35f;
            else if (move == ScoopMove.Hurl) angle = telling ? 160f : 40f;
            else if (move == ScoopMove.Charge) angle = telling ? 10f + Mathf.Sin(Anim * 40f) * 8f : 0f;
            m_Arm.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (telling && (int)(Anim * 20f) % 2 == 0) Go.transform.localPosition += new Vector3(1f / Art.PPU, 0f, 0f);

            bool frozen = LooksFrozen;
            if (frozen && !m_WasFrozen) World.Scoring.Popup("FROZEN! HIT HIM!", Pos + Vector2.up * 3.2f, new Color(0.7f, 0.9f, 1f));
            m_WasFrozen = frozen;

            // his health, over his head while he fights
            bool bar = awake && !Dying;
            m_BarBack.enabled = m_BarFill.enabled = bar;
            if (!bar) return;
            const float w = 2.4f, y = 3f;
            float f = Mathf.Clamp01(Hp / (float)MaxHp);
            m_BarBack.transform.localPosition = new Vector3(0f, y, 0f);
            m_BarBack.transform.localScale = new Vector3(w * Art.PPU + 2f, 4f, 1f);
            m_BarFill.transform.localPosition = new Vector3(-w * 0.5f * (1f - f), y, 0f);
            m_BarFill.transform.localScale = new Vector3(w * f * Art.PPU, 2f, 1f);
        }
    }
}
