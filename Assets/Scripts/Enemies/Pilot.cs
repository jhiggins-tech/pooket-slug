using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Hologram kie, the pilot who jumps out of his wrecked tank to fight on foot (<c>docs/design/tanks.md</c>, The
    /// pilot round). He is a soldier on the holograms' side: <see cref="PilotBrain"/>, with kie's playbook, presses his
    /// buttons every frame as a player would, and he moves like a soldier and plays kie's kit by its own rules:
    /// <list type="bullet">
    /// <item>FIRE, Weasel Pop: tumbles out weasels that scurry at you and pop (the tank's weasels, <see cref="Weasel"/>).</item>
    /// <item>SP, Trollogram: drops a hologram of himself (<see cref="PilotDecoy"/>) with a charge; shoot it and it blows
    /// up on you. SP again while it stands swaps him and it.</item>
    /// <item>SU, Steal: the tank's roulette (<see cref="KieRoulette"/>) spins over him and takes your special or your
    /// super's charge until you hit him hard enough to drop it. It charges from his hits on you, not from kills.</item>
    /// </list>
    /// Every big move has its tell: he flashes white and a charge sound plays half a second before it lands. Numbers in
    /// <see cref="KiePilotSpec"/>. Beaten, he glitches out like the goons, and the mission's won.
    /// <para>
    /// Co-op: the authority runs him, with the same health and moves as solo, and he goes for whoever's nearest. His
    /// pose, tell and shots ride in the boss's network object (<see cref="NetBossState"/>, phase
    /// <see cref="NetPhase"/>), so the partner's screen draws him like a remote player.
    /// </para>
    /// </summary>
    public sealed class Pilot : BossEnemy
    {
        /// <summary>His phase in the boss's network object, well clear of the tank's own (which count up from 1).</summary>
        public const int NetPhase = 100;

        static KiePilotSpec Spec => KieTuning.Pilot;
        static SoldierDef Kit => Content.Kie;

        readonly PilotBrain m_Brain;
        readonly PlayerVisual m_Visual;
        readonly KieRoulette m_Roulette;
        PilotDecoy m_Decoy;
        Vector2 m_Aim = Vector2.right;
        float m_Coyote, m_JumpBuffer, m_DropThrough, m_FireCooldown, m_SinceSwap = 9f, m_Shield, m_Muzzle, m_Tell;
        ChargeRefill m_Refill = new ChargeRefill(Spec.Specials, Spec.SpecialRefill);
        int m_Specials, m_Super, m_Shots, m_SeenShots = -1, m_HeartsSeen = -1, m_NetPose;
        bool m_Crouching, m_Telling, m_Leaping;

        public Pilot(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Brain = new PilotBrain(PilotPlaybook.Kie, new PilotSpec(), (uint)(feet.x * 977f) ^ 0x9e3779b9u);
            m_Visual = new PlayerVisual(Visual, Art.SkinFor(Kit, 0), SortOrder.Enemies);
            m_Roulette = new KieRoulette(world, this, Kit.StandHeight + 0.2f);
            m_Specials = Spec.Specials;
            Engaged = true; // the boss bar is his from the moment he's out
        }

        public override string DisplayName => "TROLLOGRAM KIE";

        /// <summary>Authority: out of the wreck, with a leap and a moment no shot can touch him.</summary>
        public void JumpOut(int side)
        {
            Vel = new Vector2(Spec.JumpOut.x * side, Spec.JumpOut.y);
            Facing = side;
            m_Shield = Spec.JumpOutShield;
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
            if (Hp < before) m_Roulette.OnHurt(before - Hp);
        }

        float Health => Hp / (float)MaxHp;

        protected override void TickAlive(float dt)
        {
            m_Shield -= dt;
            m_FireCooldown -= dt;
            m_SinceSwap += dt;
            m_Muzzle -= dt;
            m_Refill.Tick(ref m_Specials, dt);
            ChargeSuper();
            m_Roulette.Fury = 1f + PilotSpec.Rage(Health);

            if (World.WeakPilot && Anim > 3f)
            {
                Hp = 0; // ?test=pilot,weak: he goes down on his own a few seconds in, to see what beating him does
                Die();
                return;
            }
            SoldierInput input = m_Brain.Tick(dt, View());
            if (m_Brain.Telling && !m_Telling) Sfx.Play(Sfx.Charge, 0.6f); // the tell: a flash and a sound
            m_Telling = m_Brain.Telling;
            Move(dt, input);

            if ((input.FirePressed || input.Fire) && m_FireCooldown <= 0f) ThrowWeasels();
            if (input.SpecialPressed) Special();
            if (input.SuperPressed && Spec.SuperReady(m_Super) && !m_Roulette.Busy)
            {
                m_Super = 0;
                m_Roulette.Start();
                World.Scoring.Popup("STEAL!", Center + Vector2.up * 1.6f, new Color(1f, 0.6f, 0.9f));
            }
            m_Roulette.Tick(dt);
            m_Tell = m_Telling ? m_Tell + dt : 0f;
        }

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
                DecoyUp = m_Decoy != null && m_Decoy.Up, Decoy = m_Decoy != null ? m_Decoy.Pos : Pos,
                Specials = m_Specials, SuperReady = Spec.SuperReady(m_Super) && !m_Roulette.Busy,
                Left = w.View.Left, Right = w.View.Right,
            };
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

        /// <summary>Steal charges from what the soldiers lose to him (and his weasels and hologram) while he fights.</summary>
        void ChargeSuper()
        {
            int hearts = World.Player.Dead ? 0 : World.Player.Hp;
            foreach (RemotePlayer r in World.Remotes)
                if (!r.IsDown) hearts += r.Hp;
            if (m_HeartsSeen >= 0 && hearts < m_HeartsSeen) m_Super = Spec.ChargeSuper(m_Super, m_HeartsSeen - hearts);
            m_HeartsSeen = hearts;
        }

        /// <summary>Runs, jumps and drops through bridges as a soldier does, on kie's numbers.</summary>
        void Move(float dt, in SoldierInput input)
        {
            float moveX = input.MoveX;
            bool down = input.Down;
            m_Crouching = Grounded && down && !input.JumpPressed;
            Height = m_Crouching ? Kit.CrouchHeight : Kit.StandHeight;
            if (m_Leaping && Grounded && Vel.y <= 0f) m_Leaping = false;
            if (!m_Leaping) Vel.x = moveX * (m_Crouching ? Kit.CrawlSpeed : Kit.RunSpeed * Spec.RunScale);
            if (moveX != 0f) Facing = moveX > 0f ? 1 : -1;

            m_Coyote = Grounded ? Tuning.Soldier.CoyoteTime : m_Coyote - dt;
            m_JumpBuffer = input.JumpPressed ? Tuning.Soldier.JumpBuffer : m_JumpBuffer - dt;
            m_DropThrough -= dt;
            if (m_JumpBuffer > 0f && m_Coyote > 0f)
            {
                if (down && TilePhysics.OnBridge(World.Level, Pos, HalfW)) m_DropThrough = Tuning.Soldier.DropThroughTime;
                else
                {
                    Vel.y = Kit.JumpVelocity * Spec.JumpScale;
                    Sfx.Play(Sfx.Jump, 0.4f);
                }
                m_JumpBuffer = 0f;
                m_Coyote = 0f;
            }
            float gravity = Gravity;
            if (Vel.y > 0f && !input.Jump && !m_Leaping) gravity *= Tuning.Soldier.ShortHopGravity;
            Vel.y = Mathf.Max(Vel.y - gravity * dt, -MaxFall);
            MoveWithTiles(dt, m_DropThrough > 0f);
            Pos.x = Mathf.Clamp(Pos.x, World.View.Left + HalfW, World.View.Right - HalfW); // never off the screen

            m_Aim = Aiming.Direction(input.Up, down && !Grounded, moveX, Facing, Grounded);
        }

        Vector2 MuzzlePos
        {
            get
            {
                Vector2 local = PlayerVisual.MuzzleLocal(Aiming.AimY(m_Aim), m_Crouching, Aiming.IsDiagonal(m_Aim));
                return Pos + new Vector2(local.x * Facing, local.y);
            }
        }

        /// <summary>Weasel Pop on the holograms' side: weasels that hunt the soldiers, a couple a press.</summary>
        void ThrowWeasels()
        {
            int out_ = 0;
            foreach (Enemy e in World.Enemies)
                if (e is Weasel && !e.Dying && !e.Removed) out_++;
            if (out_ >= Spec.WeaselsOut) return;
            Vector2[] throws = KieTuning.Weasels.Volley(m_Aim, Vel);
            for (int i = 0; i < Spec.WeaselsPerThrow && i < throws.Length && out_ < Spec.WeaselsOut; i++, out_++)
                if (World.Spawner.SpawnEnemy(Content.Weasel, MuzzlePos) is Weasel weasel) weasel.Launch(throws[i], Content.KieTank.Weasel.ScurrySpeed);
            m_FireCooldown = Spec.WeaselCooldown;
            Fired();
        }

        void Fired()
        {
            m_Shots++;
            m_Muzzle = 0.06f;
            Sfx.Play(Sfx.WeaselThrow, 0.6f);
        }

        /// <summary>Trollogram: swap with the hologram while it stands, or drop one with a charge.</summary>
        void Special()
        {
            bool up = m_Decoy != null && m_Decoy.Up;
            switch (KieTuning.Trollogram.OnSpecial(up, m_SinceSwap, m_Specials))
            {
                case TrollAction.Drop:
                    m_Specials--;
                    m_Decoy = World.Spawner.SpawnEnemy(Content.PilotDecoy, Pos) as PilotDecoy;
                    m_Decoy?.Hold(Pose(), Height);
                    World.Scoring.Popup("TROLLOGRAM!", Center + Vector2.up * 1.4f, Palette.HostileRifle);
                    Sfx.Play(Sfx.TrollDrop, 0.8f);
                    break;
                case TrollAction.Swap:
                    Vector2 here = Pos;
                    AvatarPose pose = Pose();
                    World.Burst(Center, 10, Palette.HostileRifle);
                    Pos = m_Decoy.Pos;
                    Vel = Vector2.zero;
                    m_Decoy.Pos = here;
                    m_Decoy.Hold(pose, Height);
                    World.Burst(Center, 10, Palette.HostileRifle);
                    m_SinceSwap = 0f;
                    m_Shield = Mathf.Max(m_Shield, Spec.SwapShield);
                    Sfx.Play(Sfx.TrollSwap, 0.8f);
                    break;
            }
        }

        AvatarPose Pose() => new AvatarPose
        {
            Facing = Facing,
            AimY = Aiming.AimY(m_Aim),
            Diagonal = Aiming.IsDiagonal(m_Aim),
            Grounded = Grounded,
            Crouching = m_Crouching,
            Running = Grounded && Mathf.Abs(Vel.x) > 0.1f,
            Muzzle = m_Muzzle > 0f,
            Blink = m_Shield > 0f,
            Flash = FlashTimer > 0f || (m_Telling && (int)(m_Tell * 14f) % 2 == 0),
            Anim = Anim,
        };

        protected override void Die()
        {
            m_Roulette.OnDie();
            if (m_Decoy != null && m_Decoy.Up) m_Decoy.SwitchOff();
            base.Die();
            if (!Glitches) return;
            // the big glitch-out: frozen mid-taunt, the music cut, then torn apart (story.md, Freeing kie)
            PilotFreeingSpec spec = KieTuning.Freeing;
            Look?.Big(spec.Freeze, spec.Stretch, spec.TearPx);
            World.BossMusicOff = true;
            World.Scoring.Popup("WAIT, I WAS WINN-", Center + Vector2.up * 1.2f, Palette.HostileRifle);
            World.View.Shake(0.15f);
        }

        /// <summary>
        /// The big glitch-out, then the freeing scene: Mission one's, so solo only (Mission one is single player only).
        /// A co-op pilot, in later missions, goes the goons' way and wins the mission.
        /// </summary>
        bool Glitches => !World.Coop;

        /// <summary>Glitched out: the real kie is freed (the freeing scene), or in co-op the mission's won.</summary>
        public override void Destroy()
        {
            if (Dying && Glitches) World.OnPilotFreed();
            else if (Dying) World.OnBossDestroyed();
            base.Destroy();
        }

        // ------------------------------------------------------------------ co-op

        public override NetBossState NetState => new NetBossState
        {
            Phase = NetPhase, Angle = Pose().Pack(), Charge = m_Telling ? 1f : 0f, Shots = m_Shots & 0xff,
            KieState = m_Roulette.NetState(true), KieProgress = m_Roulette.Progress, KieDrops = m_Roulette.Drops & 0xff,
        };

        public override void ApplyNet(in NetBossState s)
        {
            if (s.Phase != NetPhase) return; // still the tank's
            m_Roulette.ApplyNet(s.KieState, s.KieProgress, s.KieDrops);
            if (Dying) return;
            m_NetPose = (int)s.Angle;
            bool telling = s.Charge > 0.5f;
            if (telling && !m_Telling) Sfx.Play(Sfx.Charge, 0.6f);
            m_Telling = telling;
            if (m_SeenShots >= 0 && s.Shots != m_SeenShots) Fired();
            m_SeenShots = s.Shots;
        }

        protected override void TickFollower(float dt)
        {
            m_Roulette.Tick(dt);
            m_Muzzle -= dt;
            m_Tell = m_Telling ? m_Tell + dt : 0f;
        }

        public override void Render()
        {
            base.Render();
            AvatarPose pose;
            if (World.IsAuthority) pose = Pose();
            else
            {
                pose = AvatarPose.Unpack(m_NetPose, Anim, 0f);
                pose.Facing = Facing;
                pose.Muzzle = m_Muzzle > 0f;
                pose.Flash = FlashTimer > 0f || (m_Telling && (int)(m_Tell * 14f) % 2 == 0);
            }
            if (Dying) pose.Blink = pose.Muzzle = false;
            m_Visual.Apply(pose);
            m_Roulette.Render();
        }
    }

    /// <summary>
    /// The pilot's Trollogram: a hologram of him frozen as he stood. It looks just like him. Shoot it and it blows up on
    /// the soldiers near it; left alone it switches itself off. Its own entity (an enemy), so co-op carries it.
    /// </summary>
    public sealed class PilotDecoy : Enemy
    {
        readonly PlayerVisual m_Visual;
        AvatarPose m_Pose;
        float m_Life;
        bool m_Blown;

        public PilotDecoy(World world, EnemyDef def, Vector2 feet) : base(world, def, feet)
        {
            m_Visual = new PlayerVisual(Visual, Art.SkinFor(Content.Kie, 0), SortOrder.Enemies - 1);
            m_Pose = new AvatarPose { Facing = 1, Grounded = true };
        }

        /// <summary>Standing, not yet shot or switched off.</summary>
        public bool Up => !Dying && !Removed;

        /// <summary>Holds the pilot's pose where it stands.</summary>
        public void Hold(AvatarPose pose, float height)
        {
            m_Pose = pose = pose.Still();
            Facing = pose.Facing;
            Height = height;
            Vel = Vector2.zero;
        }

        /// <summary>Switches off without a blast (its time ran out, or the pilot's beaten).</summary>
        public void SwitchOff()
        {
            if (Dying) return;
            Dying = true;
            Sfx.Play(Sfx.Glitch, 0.4f);
        }

        protected override void TickAlive(float dt)
        {
            m_Life += dt;
            if (m_Life >= KieTuning.Trollogram.Life) SwitchOff();
            Vel.x = 0f;
            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveWithTiles(dt);
        }

        /// <summary>Shot: it blows up on the soldiers near it (scoring nothing, like a weasel).</summary>
        protected override void Die()
        {
            Dying = true;
            Blow();
            if (World.IsAuthority) World.Combat.HostileBlast(Center, KieTuning.Pilot.DecoyBlastRadius, KieTuning.Pilot.DecoyDamage, 0f);
        }

        void Blow()
        {
            if (m_Blown) return;
            m_Blown = true;
            World.Combat.Explode(Center, 0f, 0, false, KieTuning.Pilot.DecoyBlastSize);
            World.Scoring.Popup("TROLLED!", Center + Vector2.up * 1.2f, Palette.HostileRifle);
        }

        protected override void OnSnapshot(int flags)
        {
            if ((flags & NetEnemyState.Dying) != 0 && Hp <= 0) Blow(); // shot on the authority's screen
        }

        public override void Render()
        {
            base.Render();
            m_Pose.Facing = Facing;
            m_Pose.Anim = 0f;
            m_Visual.Apply(m_Pose);
        }
    }
}
