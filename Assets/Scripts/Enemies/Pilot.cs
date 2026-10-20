using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// A boss tabk's Trollogram pilot, who jumps out of the wreck to fight on foot (<c>docs/design/tanks.md</c>, The
    /// pilot round). He is a soldier on the Trollograms' side: <see cref="PilotBrain"/>, with his kit's playbook, presses
    /// his buttons every frame as a player would, and he moves like a soldier on his crewmate's numbers. What his
    /// buttons do is his kit's: kie's (<see cref="KiePilot"/>, Mission one) and kcaj's (<see cref="KcajPilot"/>, Mission
    /// two). Every big move has its tell: he flashes white and a charge sound plays half a second before it lands.
    /// Beaten, he freezes mid-taunt and glitches out, and the freeing scene plays (or, where the mission has none, the
    /// mission's won).
    /// <para>
    /// Co-op: the authority runs him, with the same health and moves as solo, and he goes for whoever's nearest. His
    /// pose, tell and shots ride in the boss's network object (<see cref="NetBossState"/>, phase
    /// <see cref="NetPhase"/>), with his kit's own state in its kit fields, so the partner's screen draws him like a
    /// remote player.
    /// </para>
    /// </summary>
    public abstract class Pilot : BossEnemy
    {
        /// <summary>His phase in the boss's network object, well clear of the tank's own (which count up from 1).</summary>
        public const int NetPhase = 100;

        readonly PilotBrain m_Brain;
        readonly PlayerVisual m_Visual;
        readonly SoldierDef m_Kit;
        protected Vector2 m_Aim = Vector2.right;
        SoldierJump m_Jump;
        float m_Tell;
        protected float m_Shield, m_Muzzle;
        int m_Shots, m_SeenShots = -1, m_HeartsSeen = -1, m_NetPose;
        protected bool m_Crouching;
        bool m_Telling, m_Leaping;

        protected Pilot(World world, EnemyDef def, Vector2 feet, SoldierDef kit, PilotPlaybook book) : base(world, def, feet)
        {
            m_Kit = kit;
            m_Brain = new PilotBrain(book, new PilotSpec(), (uint)(feet.x * 977f) ^ 0x9e3779b9u);
            m_Visual = new PlayerVisual(Visual, Art.SkinFor(kit, 0), SortOrder.Enemies);
            Engaged = true; // the boss bar is his from the moment he's out
        }

        /// <summary>How hard he leaps out of the wreck, and how long no shot can touch him while he does.</summary>
        protected abstract Vector2 JumpOutVelocity { get; }
        protected abstract float JumpOutShield { get; }
        /// <summary>His run and jump next to his crewmate's own.</summary>
        protected abstract float RunScale { get; }
        protected abstract float JumpScale { get; }
        /// <summary>His last words as he freezes mid-taunt (<c>docs/design/cutscenes/README.md</c>).</summary>
        protected abstract string LastWords { get; }

        /// <summary>Authority: out of the wreck, with a leap and a moment no shot can touch him.</summary>
        public void JumpOut(int side)
        {
            Vector2 v = JumpOutVelocity;
            Vel = new Vector2(v.x * side, v.y);
            Facing = side;
            m_Shield = JumpOutShield;
            Grounded = false;
            m_Leaping = true;
            World.Scoring.Popup("THE PILOT!", Center + Vector2.up * 1.4f, Palette.HostileRifle);
            Sfx.Play(Sfx.Glitch, 0.8f);
            Sfx.Play(Sfx.Jump, 0.8f);
        }

        public override void Hurt(int damage)
        {
            if (m_Shield > 0f && World.IsAuthority) return; // leaping out, or glitching into place after a swap
            int before = Hp;
            base.Hurt(damage);
            if (Hp < before) OnHurt(before - Hp);
        }

        /// <summary>Authority: he lost <paramref name="damage"/> health.</summary>
        protected virtual void OnHurt(int damage) { }

        protected float Health => Hp / (float)MaxHp;

        protected override void TickAlive(float dt)
        {
            m_Shield -= dt;
            m_Muzzle -= dt;
            ChargeSuper();

            if (World.WeakPilot && Anim > 3f)
            {
                Hp = 0; // ?test=pilot,weak: he goes down on his own a few seconds in, to see what beating him does
                Die();
                return;
            }
            SoldierInput input = m_Brain.Tick(dt, View());
            if (m_Brain.Telling && !m_Telling) Sfx.Play(Sfx.Charge, 0.6f); // the tell: a flash and a sound
            m_Telling = m_Brain.Telling;
            Move(dt, input, MoveScale);
            Act(dt, input);
            m_Tell = m_Telling ? m_Tell + dt : 0f;
        }

        /// <summary>Authority: his kit's moves this frame, from the buttons the brain pressed.</summary>
        protected abstract void Act(float dt, in SoldierInput input);

        /// <summary>How much of his run he has now (kcaj stands still while his beam's stuck).</summary>
        protected virtual float MoveScale => 1f;

        /// <summary>What he can see: himself, his target if it's on screen, shots coming at him, his meters.</summary>
        PilotView View()
        {
            World w = World;
            Actor target = Target;
            bool has = target != null && !target.IsDown && target.Pos.x >= w.View.Left - 0.5f && target.Pos.x <= w.View.Right + 0.5f;
            var v = new PilotView
            {
                Pos = Pos, Vel = Vel, Grounded = Grounded, OnBridge = TilePhysics.OnBridge(w.Level, Pos, HalfW), Facing = Facing,
                Health = Health, HasTarget = has, Target = has ? target.Pos : Pos,
                Left = w.View.Left, Right = w.View.Right,
            };
            Sees(ref v);
            // shots and weasels on their way at him
            foreach (Projectile p in w.Projectiles)
            {
                if (p.Removed || p.Hostile) continue;
                float dx = Pos.x - p.Pos.x, dy = Center.y - p.Pos.y;
                if (Mathf.Abs(dx) > 4.5f || Mathf.Abs(dy) > 1.6f || (p.Vel.x != 0f && Mathf.Sign(p.Vel.x) != Mathf.Sign(dx))) continue;
                v.Threat = true;
                v.ThreatSide = dx > 0f ? -1 : 1;
                break;
            }
            if (!v.Threat)
            {
                foreach (Entity e in w.Props)
                {
                    if (!(e is PopWeasel) || e.Removed || (e.Pos - Pos).sqrMagnitude > 2.5f * 2.5f) continue;
                    v.Threat = true;
                    v.ThreatSide = e.Pos.x < Pos.x ? -1 : 1;
                    break;
                }
            }
            return v;
        }

        /// <summary>His kit's part of what he sees: his meters, a hologram of his.</summary>
        protected abstract void Sees(ref PilotView v);

        /// <summary>His super charges from what the soldiers lose to him while he fights.</summary>
        void ChargeSuper()
        {
            int hearts = World.Player.Dead ? 0 : World.Player.Hp;
            foreach (RemotePlayer r in World.Remotes)
                if (!r.IsDown) hearts += r.Hp;
            if (m_HeartsSeen >= 0 && hearts < m_HeartsSeen) OnHeartsLost(m_HeartsSeen - hearts);
            m_HeartsSeen = hearts;
        }

        /// <summary>The soldiers lost <paramref name="hearts"/> while he fights: his super's meter.</summary>
        protected abstract void OnHeartsLost(int hearts);

        /// <summary>Runs, jumps and drops through bridges as a soldier does, on his crewmate's numbers.</summary>
        void Move(float dt, in SoldierInput input, float scale)
        {
            float moveX = input.MoveX * scale;
            bool down = input.Down;
            bool jump = input.JumpPressed && scale > 0f;
            m_Crouching = Grounded && down && !jump;
            Height = m_Crouching ? m_Kit.CrouchHeight : m_Kit.StandHeight;
            if (m_Leaping && Grounded && Vel.y <= 0f) m_Leaping = false;
            if (!m_Leaping) Vel.x = moveX * (m_Crouching ? m_Kit.CrawlSpeed : m_Kit.RunSpeed * RunScale);
            if (input.MoveX != 0f) Facing = input.MoveX > 0f ? 1 : -1;

            if (m_Jump.Tick(dt, Grounded, jump))
            {
                if (down && TilePhysics.OnBridge(World.Level, Pos, HalfW)) m_Jump.Drop();
                else
                {
                    Vel.y = m_Kit.JumpVelocity * JumpScale;
                    Sfx.Play(Sfx.Jump, 0.4f);
                }
            }
            Fall(dt, SoldierJump.Gravity(Vel.y, input.Jump, m_Leaping));
            MoveWithTiles(dt, m_Jump.DroppingThrough);
            Pos.x = Mathf.Clamp(Pos.x, World.View.Left + HalfW, World.View.Right - HalfW); // never off the screen

            m_Aim = Aiming.Direction(input.Up, down && !Grounded, input.MoveX, Facing, Grounded);
        }

        protected Vector2 MuzzlePos
        {
            get
            {
                Vector2 local = PlayerVisual.MuzzleLocal(Aiming.AimY(m_Aim), m_Crouching, Aiming.IsDiagonal(m_Aim));
                return Pos + new Vector2(local.x * Facing, local.y);
            }
        }

        /// <summary>He fired: the muzzle flashes, and a follower's screen is told by the count.</summary>
        protected void Fired(AudioClip sound)
        {
            m_Shots++;
            m_Muzzle = 0.06f;
            if (sound != null) Sfx.Play(sound, 0.6f);
        }

        /// <summary>The sound his kit's primary makes, for a follower's screen to play when the authority's fires.</summary>
        protected abstract AudioClip FireSound { get; }

        protected AvatarPose Pose() => new AvatarPose
        {
            Facing = Facing,
            AimY = Aiming.AimY(m_Aim),
            Diagonal = Aiming.IsDiagonal(m_Aim),
            Grounded = Grounded,
            Crouching = m_Crouching,
            Running = Grounded && Mathf.Abs(Vel.x) > 0.1f,
            Muzzle = m_Muzzle > 0f,
            Blink = m_Shield > 0f,
            Flash = FlashTimer > 0f || (m_Telling && Fx.TellFlash.On(m_Tell)),
            Anim = Anim,
        };

        protected override void Die()
        {
            OnBeaten();
            base.Die();
            GlitchOut();
        }

        /// <summary>Follower: beaten on the authority's screen, so the same big glitch-out here.</summary>
        protected override void OnSnapshot(int flags)
        {
            if ((flags & NetEnemyState.Dying) == 0 || Dying) return;
            OnBeaten();
            GlitchOut();
        }

        void GlitchOut()
        {
            if (!Glitches) return;
            // the big glitch-out: frozen mid-taunt, the music cut, then torn apart (cutscenes/mission-one.md, Freeing kie and kcaj)
            PilotFreeingSpec spec = KieTuning.Freeing;
            Look?.Big(spec.Freeze, spec.Stretch, spec.TearPx);
            World.BossMusicOff = true;
            World.Scoring.Popup(LastWords, Center + Vector2.up * 1.2f, Palette.HostileRifle);
            World.View.Shake(0.15f);
        }

        /// <summary>He's beaten: his kit's moves stop.</summary>
        protected abstract void OnBeaten();

        /// <summary>
        /// The big glitch-out, then the freeing scene: solo, or on both phones in co-op where the mission's story says
        /// so (Mission two's). Otherwise a co-op pilot goes the goons' way and wins the mission.
        /// </summary>
        bool Glitches => World.Mission.Story.PilotGlitches(World.Coop);

        /// <summary>Glitched out: the real crewmate is freed (the freeing scene), or the mission's won.</summary>
        public override void Destroy()
        {
            if (Dying && Glitches) World.OnPilotFreed();
            else if (Dying) World.OnBossDestroyed();
            base.Destroy();
        }

        // ------------------------------------------------------------------ co-op

        public override NetBossState NetState
        {
            get
            {
                var s = new NetBossState { Phase = NetPhase, Angle = Pose().Pack(), Charge = m_Telling ? 1f : 0f, Shots = m_Shots & 0xff };
                KitState(ref s);
                return s;
            }
        }

        /// <summary>Authority: his kit's state, in the kit fields of the boss's network object.</summary>
        protected abstract void KitState(ref NetBossState s);

        public override void ApplyNet(in NetBossState s)
        {
            if (s.Phase != NetPhase) return; // still the tank's
            ApplyKit(s);
            if (Dying) return;
            m_NetPose = (int)s.Angle;
            bool telling = s.Charge > 0.5f;
            if (telling && !m_Telling) Sfx.Play(Sfx.Charge, 0.6f);
            m_Telling = telling;
            if (m_SeenShots >= 0 && s.Shots != m_SeenShots) Fired(FireSound);
            m_SeenShots = s.Shots;
        }

        /// <summary>Follower: take up his kit's state from the authority's.</summary>
        protected abstract void ApplyKit(in NetBossState s);

        protected override void TickFollower(float dt)
        {
            m_Muzzle -= dt;
            m_Tell = m_Telling ? m_Tell + dt : 0f;
            TickKitFollower(dt);
        }

        /// <summary>Follower: his kit's own moving parts, each frame.</summary>
        protected abstract void TickKitFollower(float dt);

        /// <summary>His pose as this screen draws it: his own (authority), or the authority's.</summary>
        protected AvatarPose ShownPose
        {
            get
            {
                AvatarPose pose;
                if (World.IsAuthority) pose = Pose();
                else
                {
                    pose = AvatarPose.Unpack(m_NetPose, Anim, 0f);
                    pose.Facing = Facing;
                    pose.Muzzle = m_Muzzle > 0f;
                    pose.Flash = FlashTimer > 0f || (m_Telling && Fx.TellFlash.On(m_Tell));
                }
                if (Dying) pose.Blink = pose.Muzzle = false;
                return pose;
            }
        }

        public override void Render()
        {
            base.Render();
            m_Visual.Apply(ShownPose);
            RenderKit();
        }

        /// <summary>His kit's drawing: kie's roulette, kcaj's beam and pills.</summary>
        protected abstract void RenderKit();
    }
}
