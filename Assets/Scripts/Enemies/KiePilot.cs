using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Trollogram kie, Mission one's pilot (<see cref="Pilot"/>), playing kie's kit by its own rules:
    /// <list type="bullet">
    /// <item>FIRE, Weasel Pop: tumbles out weasels that scurry at you and pop (the tank's weasels, <see cref="Weasel"/>).</item>
    /// <item>SP, Trollogram: drops a hologram of himself (<see cref="PilotDecoy"/>) with a charge; shoot it and it blows
    /// up on you. SP again while it stands swaps him and it.</item>
    /// <item>SU, Steal: the tank's roulette (<see cref="KieRoulette"/>) spins over him and takes your special or your
    /// super's charge until you hit him hard enough to drop it. It charges from his hits on you, not from kills.</item>
    /// </list>
    /// Numbers in <see cref="KiePilotSpec"/>. His roulette rides in the boss's network object's kie fields.
    /// </summary>
    public sealed class KiePilot : Pilot
    {
        static KiePilotSpec Spec => KieTuning.Pilot;
        static SoldierDef Kit => Content.Kie;

        readonly KieRoulette m_Roulette;
        PilotDecoy m_Decoy;
        float m_FireCooldown, m_SinceSwap = 9f;
        ChargeRefill m_Refill = new ChargeRefill(Spec.Specials, Spec.SpecialRefill);
        int m_Specials, m_Super;

        public KiePilot(World world, EnemyDef def, Vector2 feet) : base(world, def, feet, Kit, PilotPlaybook.Kie)
        {
            m_Roulette = new KieRoulette(world, this, Kit.StandHeight + 0.2f);
            m_Specials = Spec.Specials;
        }

        public override string DisplayName => "TROLLOGRAM KIE";
        protected override Vector2 JumpOutVelocity => Spec.JumpOut;
        protected override float JumpOutShield => Spec.JumpOutShield;
        protected override float RunScale => Spec.RunScale;
        protected override float JumpScale => Spec.JumpScale;
        protected override string LastWords => "WAIT, I WAS WINN-";
        protected override AudioClip FireSound => Sfx.WeaselThrow;

        protected override void OnHurt(int damage) => m_Roulette.OnHurt(damage);

        protected override void OnHeartsLost(int hearts) => m_Super = Spec.ChargeSuper(m_Super, hearts);

        protected override void Sees(ref PilotView v)
        {
            v.DecoyUp = m_Decoy != null && m_Decoy.Up;
            v.Decoy = m_Decoy != null ? m_Decoy.Pos : Pos;
            v.Specials = m_Specials;
            v.SuperReady = Spec.SuperReady(m_Super) && !m_Roulette.Busy;
        }

        protected override void Act(float dt, in SoldierInput input)
        {
            m_FireCooldown -= dt;
            m_SinceSwap += dt;
            m_Refill.Tick(ref m_Specials, dt);
            m_Roulette.Fury = 1f + PilotSpec.Rage(Health);
            if ((input.FirePressed || input.Fire) && m_FireCooldown <= 0f) ThrowWeasels();
            if (input.SpecialPressed) Special();
            if (input.SuperPressed && Spec.SuperReady(m_Super) && !m_Roulette.Busy)
            {
                m_Super = 0;
                m_Roulette.Start();
                World.Scoring.Popup("STEAL!", Center + Vector2.up * 1.6f, new Color(1f, 0.6f, 0.9f));
            }
            m_Roulette.Tick(dt);
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
            Fired(FireSound);
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

        protected override void OnBeaten()
        {
            m_Roulette.OnDie();
            if (m_Decoy != null && m_Decoy.Up) m_Decoy.SwitchOff();
        }

        protected override void KitState(ref NetBossState s)
        {
            s.KieState = m_Roulette.NetState(true);
            s.KieProgress = m_Roulette.Progress;
            s.KieDrops = m_Roulette.Drops & 0xff;
        }

        protected override void ApplyKit(in NetBossState s) => m_Roulette.ApplyNet(s.KieState, s.KieProgress, s.KieDrops);

        protected override void TickKitFollower(float dt) => m_Roulette.Tick(dt);

        protected override void RenderKit() => m_Roulette.Render();
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
            m_Visual = new PlayerVisual(Visual, Art.SkinFor(Content.Kie), SortOrder.Enemies - 1);
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
            Walk(dt);
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
