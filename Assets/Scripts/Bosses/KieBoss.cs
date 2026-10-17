using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// kie's tank, riding on Mission one's artillery tank (<see cref="TankBoss"/>). Each charged shot is the next attack of
    /// three (<see cref="KieTankSpec.AttackFor"/>, tuned in <see cref="Content.KieTank"/>):
    /// <list type="bullet">
    /// <item>Weasels: three lobbed at you, which land, scurry over and pop (<see cref="Weasel"/>). Shoot them first.</item>
    /// <item>Holograms: two copies of the tank glitch in (<see cref="Content.TankCopy"/>, spawned as decoy
    /// <see cref="TankBoss"/>es) and all three shuffle. Every one
    /// flickers like a hologram and moves and fires as one. Only the real one can be hurt, and the first hit on it
    /// ends the trick.</item>
    /// <item>Roulette: a wheel spins over the tank and steals your special's charges (or your super's charge). Hit
    /// the tank hard enough and it drops what it stole (<see cref="KieLoot"/>) for you to grab back.</item>
    /// </list>
    /// A turn whose attack is still going is a plain shell. It all quickens as the tank is hurt, with its rage.
    /// <para>
    /// Co-op: the authority runs the attacks. Weasels and copies are enemies, so the snapshot carries them; the
    /// disguise is a flag on the tank's snapshot (<see cref="NetEnemyState.Disguised"/>); and the roulette rides in
    /// the tank's network object (<see cref="NetBossState.KieState"/>). Every screen steals from its own soldier and
    /// drops its own soldier's loot, so the wheel takes from everyone at once.
    /// </para>
    /// </summary>
    public sealed class KieBoss : IBossKit
    {
        static KieTankSpec Spec => Content.KieTank;

        readonly World m_World;
        readonly TankBoss m_Tank;
        readonly List<TankBoss> m_Copies = new List<TankBoss>();
        readonly KieRoulette m_Roulette;
        float m_HomeX;
        int m_Turn;
        bool m_Disguised;


        public KieBoss(World world, TankBoss tank)
        {
            m_World = world;
            m_Tank = tank;
            m_HomeX = tank.HomeX;
            m_Roulette = new KieRoulette(world, tank, 3.5f);
        }

        /// <summary>Authority: the copies standing with it, which fire its shells with it.</summary>
        public IReadOnlyList<TankBoss> Copies => m_Copies;

        /// <summary>It's wearing its hologram look among its copies.</summary>
        public bool Disguised => m_Disguised;

        KieAttack Next => KieTankSpec.AttackFor(m_Turn, m_Copies.Count > 0, m_Roulette.Busy);

        /// <summary>Authority: the next charged shot comes down somewhere, so the landing marker means something.</summary>
        public bool NextLands => Next == KieAttack.Shell || Next == KieAttack.Weasels;

        /// <summary>Extra seconds the tank rests after the attack just made (the wheel spinning).</summary>
        public float Wait { get; private set; }

        /// <summary>Authority: how close it is to dropping what it stole, 0 to 1.</summary>
        public float Progress => m_Roulette.Progress;

        /// <summary>Authority: times it has dropped what it stole.</summary>
        public int Drops => m_Roulette.Drops;

        /// <summary>Authority: the roulette for the network object, with whether the landing marker is shown.</summary>
        public int NetState(bool markerShown) => m_Roulette.NetState(markerShown);

        /// <summary>Follower: whether the authority's tank shows its landing marker.</summary>
        public static bool MarkerShown(int netState) => KieRoulette.MarkerShown(netState);
        public bool ShowsMarker(int netState) => MarkerShown(netState);

        public string Name => Content.BossTank.Name;

        /// <summary>Every screen: follow the roulette as the authority runs it (<see cref="KieRoulette.ApplyNet"/>).</summary>
        public void ApplyNet(int state, float progress, int drops) => m_Roulette.ApplyNet(state, progress, drops);

        /// <summary>
        /// Authority: the tank's charged shot, kie's attack in its place, or false for a plain shell.
        /// <paramref name="thrown"/>: it came out of the barrel (the weasels), so the tank kicks as if it fired.
        /// </summary>
        public bool Fire(Vector2 muzzle, float angle, out bool thrown)
        {
            KieAttack attack = Next;
            m_Turn++;
            Wait = 0f;
            thrown = attack == KieAttack.Weasels;
            switch (attack)
            {
                case KieAttack.Weasels:
                    LobWeasels(muzzle, angle);
                    return true;
                case KieAttack.Holograms:
                    Holograms();
                    Wait = 0.5f;
                    return true;
                case KieAttack.Roulette:
                    m_Roulette.Fury = m_Tank.Fury;
                    m_Roulette.Start();
                    Wait = m_Roulette.SpinTime + 0.3f;
                    return true;
                default:
                    return false;
            }
        }

        // ------------------------------------------------------------------ weasels

        void LobWeasels(Vector2 muzzle, float angle)
        {
            Actor target = m_World.Combat.NearestAvatar(muzzle);
            float dx = target.Pos.x - muzzle.x, dy = target.Pos.y - muzzle.y;
            float lob = Mathf.Clamp(angle > 90f ? 180f - angle : angle, 30f, 70f);
            float power = BossTankSpec.PowerFor(dx, dy, lob, Spec.WeaselGravity);
            if (float.IsNaN(power) || power > Spec.WeaselMaxPower) power = Spec.WeaselMaxPower;
            float a = lob * Mathf.Deg2Rad, side = dx < 0f ? -1f : 1f;
            var dir = new Vector2(Mathf.Cos(a) * side, Mathf.Sin(a));
            float speed = Spec.WeaselSpeed * m_Tank.Fury;
            for (int i = 0; i < Spec.WeaselCount; i++)
            {
                float k = 1f + (i - (Spec.WeaselCount - 1) * 0.5f) * Spec.WeaselSpread;
                if (m_World.Spawner.SpawnEnemy(Content.Weasel, muzzle) is Weasel weasel) weasel.Launch(dir * (power * k), speed);
            }
            Sfx.Play(Sfx.Throw, 0.9f);
            m_World.Scoring.Popup("WEASELS!", m_Tank.Pos + Vector2.up * 4.4f, new Color(1f, 0.75f, 0.4f));
        }

        // ------------------------------------------------------------------ holograms

        void Holograms()
        {
            float[] slots = Spec.Slots(m_HomeX);
            int real = m_World.Rng.Range(0, slots.Length);
            float y = m_Tank.Pos.y;
            for (int i = 0; i < slots.Length; i++)
            {
                var at = new Vector2(Standable(slots[i], y), y);
                if (i == real)
                {
                    m_Tank.Pos = at;
                    m_Tank.HomeX = at.x;
                }
                else if (m_World.Spawner.SpawnEnemy(Content.TankCopy, at) is TankBoss copy)
                {
                    copy.Hp = m_Tank.Hp;
                    copy.MaxHp = m_Tank.MaxHp;
                    m_Copies.Add(copy);
                }
            }
            m_DecoyTime = Spec.DecoyTime;
            SetDisguised(true);
            m_World.Scoring.Popup("WHICH ONE?", m_Tank.Pos + Vector2.up * 4.4f, new Color(0.45f, 1f, 1f));
        }

        float m_DecoyTime;

        /// <summary>The nearest spot to <paramref name="x"/>, going back toward home, with floor under it and no wall in it.</summary>
        float Standable(float x, float y)
        {
            Level level = m_World.Level;
            float step = x > m_HomeX ? -0.5f : 0.5f;
            for (int i = 0; i < 16 && Mathf.Abs(x - m_HomeX) > 0.25f; i++, x += step)
            {
                bool floor = level.IsSolidAt(new Vector2(x, y - 0.5f)), clear = !level.IsSolidAt(new Vector2(x, y + 0.5f)) && !level.IsSolidAt(new Vector2(x, y + 1.5f));
                if (floor && clear) return x;
            }
            return m_HomeX;
        }

        /// <summary>Authority: the copies glitch out and the real one drops its disguise.</summary>
        void EndHolograms(bool found)
        {
            if (m_Copies.Count == 0) return;
            foreach (TankBoss copy in m_Copies)
                if (!copy.Removed) copy.Dying = true; // glitches out (the snapshot carries it)
            m_Copies.Clear();
            m_Tank.HomeX = m_HomeX;
            SetDisguised(false);
            if (found) m_World.Scoring.Popup("FOUND ME!", m_Tank.Pos + Vector2.up * 4.4f, Color.white);
        }

        /// <summary>Puts on or takes off the real tank's hologram look (on a follower, as the authority's snapshot says).</summary>
        public void SetDisguised(bool on)
        {
            if (on == m_Disguised) return;
            m_Disguised = on;
            m_World.Burst(m_Tank.Center, 14, new Color(0.45f, 1f, 1f));
            Sfx.Play(Sfx.Glitch, 0.8f);
            if (on) m_Tank.Look = new HoloLook(m_Tank);
            else
            {
                m_Tank.Look?.Detach();
                m_Tank.Look = null;
            }
        }

        // ------------------------------------------------------------------ the tank's side

        /// <summary>Each tick of the tank once it's awake (on a follower, each tick it's alive).</summary>
        public void Tick(float dt)
        {
            m_Roulette.Fury = m_Tank.Fury;
            m_Roulette.Tick(dt);
            if (!m_World.IsAuthority) return;
            if (m_Copies.Count > 0)
            {
                m_DecoyTime -= dt;
                if (m_DecoyTime <= 0f) EndHolograms(false);
            }
        }

        /// <summary>Authority: the real tank took <paramref name="damage"/>.</summary>
        public void OnHurt(int damage)
        {
            EndHolograms(true);
            m_Roulette.OnHurt(damage);
        }

        /// <summary>Authority: the tank is destroyed. The trick ends and anything it stole falls out.</summary>
        public void OnDie()
        {
            EndHolograms(false);
            m_Roulette.OnDie();
        }

        public void Render() => m_Roulette.Render();
    }

    /// <summary>
    /// One of kie's weasels. Thrown from the tank, it lands, scurries at the nearest soldier and pops; shoot it first.
    /// It moves and looks like kie's own (<see cref="WeaselBody"/>).
    /// In co-op the authority runs it; its pop hurts every soldier in reach, and a follower plays the pop when the
    /// snapshot says it's gone (a shot one, with no health left, makes a small harmless one).
    /// </summary>
    public sealed class Weasel : Enemy
    {
        const float PopTime = 0.15f; // dying this long before it's gone, so the snapshot carries the pop
        readonly SpriteRenderer m_Sr;
        float m_Speed, m_Fuse;
        bool m_Landed, m_Boomed;

        public Weasel(World world, EnemyDef def, Vector2 at) : base(world, def, at)
        {
            m_Sr = AddRenderer(Visual, "Body", KieArt.Weasel[0], SortOrder.Enemies + 1);
        }

        /// <summary>Authority: thrown from the tank at <paramref name="vel"/>, to run at <paramref name="speed"/> once down.</summary>
        public void Launch(Vector2 vel, float speed)
        {
            Vel = vel;
            m_Speed = speed;
            Facing = vel.x >= 0f ? 1 : -1;
        }

        protected override void TickAlive(float dt)
        {
            KieTankSpec spec = Content.KieTank;
            if (!m_Landed)
            {
                m_Landed = WeaselBody.Tumble(this, dt, spec.WeaselGravity);
                return;
            }

            m_Fuse += dt;
            if (!Target.IsDown) FaceTarget();
            WeaselBody.Scurry(this, dt, Target.IsDown ? 0f : m_Speed);
            bool close = !Target.IsDown && (Target.Center - Center).sqrMagnitude < spec.WeaselReach * spec.WeaselReach;
            if (close || m_Fuse >= spec.WeaselFuse) Pop();
        }

        void Pop()
        {
            KieTankSpec spec = Content.KieTank;
            World.Combat.HostileBlast(Center, spec.WeaselBlast, spec.WeaselDamage, 0.6f);
            m_Boomed = true;
            Dying = true; // no score: nobody shot it
        }

        protected override void TickDying(float dt)
        {
            if (!m_Boomed)
            {
                m_Boomed = true;
                // shot (no health left): a small harmless pop; on a follower, a popped one goes off at full size
                bool popped = Hp > 0;
                World.Combat.Explode(Center, 0.4f, 0, false, popped ? 0.6f : 0.35f);
            }
            if (DyingTime >= PopTime) Remove();
        }

        public override void Render()
        {
            base.Render();
            Visual.gameObject.SetActive(!Dying);
            if (!World.IsAuthority && Grounded) m_Landed = true; // a follower sees it land in the snapshot
            m_Sr.sprite = Flashed(WeaselBody.Pose(this, m_Landed, Anim));
        }
    }

    /// <summary>What kie's tank stole from our soldier, knocked loose. Grab it to get it back (each screen drops its own soldier's).</summary>
    public sealed class KieLoot : Entity
    {
        const float Life = 14f;
        readonly SpriteRenderer m_Sr;
        readonly KieSteal m_What;
        readonly int m_Amount;
        Vector2 m_Vel;
        float m_Time;
        bool m_Landed;

        public KieLoot(World world, Vector2 at, Vector2 vel, KieSteal what, int amount) : base(world, "Kie loot", at)
        {
            m_Vel = vel;
            m_What = what;
            m_Amount = amount;
            m_Sr = AddRenderer(Go.transform, "Sprite", what == KieSteal.Super ? KieArt.Super : KieArt.Special, SortOrder.Pickups);
        }

        public override void Tick(float dt)
        {
            m_Time += dt;
            if (!m_Landed)
            {
                m_Vel.y = Mathf.Max(m_Vel.y - 22f * dt, -20f);
                Vector2 next = Pos + m_Vel * dt;
                if (World.Level.IsSolidAt(new Vector2(next.x, Pos.y + 0.2f))) next.x = Pos.x; // a wall stops it
                if (m_Vel.y < 0f && World.Level.IsSolidAt(next))
                {
                    next.y = Mathf.Ceil(next.y);
                    m_Landed = true;
                }
                Pos = next;
            }
            Player p = World.Player;
            if (!p.IsDown && p.Bounds.Overlaps(new Rect(Pos.x - 0.5f, Pos.y, 1f, 0.9f))) GiveBack(p);
            else if (m_Time >= Life || Pos.y < -2f) Remove();
        }

        void GiveBack(Player p)
        {
            if (m_What == KieSteal.Super) p.Kit.ReturnSuper(m_Amount);
            else p.Specials += m_Amount;
            string what = m_What == KieSteal.Super ? "SUPER" : p.Def.Special.Hud;
            World.Scoring.Popup(what + " BACK!", Pos + Vector2.up * 1.6f, new Color(1f, 0.85f, 0.3f));
            Sfx.Play(Sfx.Pickup);
            Remove();
        }

        public override void Render()
        {
            base.Render();
            float hop = m_Landed ? Mathf.Abs(Mathf.Sin(m_Time * 4f)) * 2f : 0f;
            m_Sr.transform.localPosition = new Vector3(0f, Mathf.Round(hop) / Art.PPU, 0f);
            m_Sr.enabled = m_Time < Life - 3f || (int)(m_Time * 10f) % 2 == 0; // blinks before it's gone
        }
    }

    /// <summary>The art for kie's tank: the weasel, and the roulette's frame and icons.</summary>
    public static class KieArt
    {
        static Sprite[] s_Weasel;
        static Sprite s_Frame, s_Special, s_Super;

        static readonly Color32 Fur = Art.Hex(0xb07038), FurDark = Art.Hex(0x7a4a24), Belly = Art.Hex(0xf0e0c0), Eye = Art.Hex(0x201820),
            Nose = Art.Hex(0xe06070);

        /// <summary>Two running frames, facing right, feet at the bottom middle.</summary>
        public static Sprite[] Weasel
        {
            get
            {
                if (s_Weasel != null) return s_Weasel;
                s_Weasel = new Sprite[2];
                for (int f = 0; f < 2; f++)
                {
                    var c = new PixelCanvas(16, 9);
                    c.Rect(3, 2, 9, 3, Fur);      // body
                    c.Rect(4, 2, 7, 1, Belly);    // belly
                    c.Rect(11, 3, 3, 3, Fur);     // head
                    c.Set(14, 4, Fur);
                    c.Set(15, 4, Nose);
                    c.Set(13, 5, Eye);
                    c.Set(12, 6, FurDark);        // ear
                    c.Rect(0, 4, 3, 1, FurDark);  // tail
                    c.Set(0, 5, FurDark);
                    if (f == 0)
                    {
                        c.Rect(4, 1, 1, 1, FurDark);
                        c.Rect(10, 1, 1, 1, FurDark);
                    }
                    else
                    {
                        c.Rect(5, 1, 1, 1, FurDark);
                        c.Rect(9, 1, 1, 1, FurDark);
                    }
                    c.Outline(Art.OutlineColor);
                    s_Weasel[f] = Art.ToSprite(c, new Vector2(0.5f, 1f / 9f), withFlash: true);
                }
                return s_Weasel;
            }
        }

        /// <summary>The roulette's box, white so the renderer can tint it.</summary>
        public static Sprite Frame
        {
            get
            {
                if (s_Frame != null) return s_Frame;
                var c = new PixelCanvas(16, 16);
                c.Rect(0, 0, 16, 16, Color.white);
                c.Rect(2, 2, 12, 12, new Color32(0, 0, 0, 0));
                return s_Frame = Art.ToSprite(c, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>The special: a round bomb with a lit fuse.</summary>
        public static Sprite Special
        {
            get
            {
                if (s_Special != null) return s_Special;
                var c = new PixelCanvas(12, 12);
                c.Circle(5.5f, 4.5f, 4f, Art.Hex(0x3a3a48));
                c.Rect(3, 5, 2, 2, Art.Hex(0x8890a8));
                c.Rect(5, 9, 2, 1, Art.Hex(0x707888));
                c.Set(7, 10, Art.Hex(0xf08830));
                c.Set(8, 11, Art.Hex(0xffe860));
                c.Outline(Art.OutlineColor);
                return s_Special = Art.ToSprite(c, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>The super: a gold star.</summary>
        public static Sprite Super
        {
            get
            {
                if (s_Super != null) return s_Super;
                Color32 gold = Art.Hex(0xf0c840), light = Art.Hex(0xffe860);
                var c = new PixelCanvas(12, 12);
                c.Rect(5, 1, 2, 10, gold);
                c.Rect(1, 6, 10, 2, gold);
                c.Rect(3, 3, 6, 5, gold);
                c.Rect(2, 2, 2, 2, gold);
                c.Rect(8, 2, 2, 2, gold);
                c.Rect(5, 5, 2, 3, light);
                c.Outline(Art.OutlineColor);
                return s_Super = Art.ToSprite(c, new Vector2(0.5f, 0.5f));
            }
        }
    }
}
