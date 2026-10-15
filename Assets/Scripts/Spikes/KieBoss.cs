using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike (<c>?spike=kie</c>, solo only): kie's tank boss, riding on the artillery tank (<see cref="TankBoss"/>). Each
    /// charged shot is the next attack of three (<see cref="KieTankSpec.AttackFor"/>):
    /// <list type="bullet">
    /// <item>Weasels: three lobbed at you, which land, scurry over and pop (<see cref="Weasel"/>). Shoot them first.</item>
    /// <item>Holograms: two copies of the tank glitch in, and all three shuffle; every one flickers like a hologram
    /// (<see cref="HoloLook"/>) and fires. Only the real one can be hurt, and the first hit on it ends the trick.</item>
    /// <item>Roulette: a wheel spins over the tank and steals your special's charges (or your super's charge). Hit
    /// the tank hard enough and it drops what it stole (<see cref="KieLoot"/>) for you to grab back.</item>
    /// </list>
    /// A turn whose attack is still going is a plain shell. It all quickens as the tank is hurt, with its rage.
    /// </summary>
    public sealed class KieBoss
    {
        public static readonly KieTankSpec Spec = new KieTankSpec();

        readonly World m_World;
        readonly TankBoss m_Tank;
        readonly List<TankBoss> m_Decoys = new List<TankBoss>();
        readonly SpriteRenderer m_Frame, m_Icon, m_BarBack, m_BarFill;
        float m_HomeX, m_DecoyTime, m_Spin = -1f, m_Shown;
        int m_Turn, m_Held, m_Damage;
        KieSteal m_Pick, m_Holding;

        public KieBoss(World world, TankBoss tank)
        {
            m_World = world;
            m_Tank = tank;
            m_HomeX = tank.HomeX;
            Transform root = tank.Go.transform; // not the tank's visual, so its hologram look leaves the wheel alone
            m_Frame = Renderer(root, "Roulette", KieArt.Frame, SortOrder.Effects);
            m_Icon = Renderer(root, "Roulette icon", KieArt.Special, SortOrder.Effects + 1);
            m_BarBack = Renderer(root, "Loot bar", Art.Pixel, SortOrder.Effects);
            m_BarBack.color = new Color(0.11f, 0.08f, 0.12f, 0.85f);
            m_BarFill = Renderer(root, "Loot", Art.Pixel, SortOrder.Effects + 1);
            m_BarFill.color = new Color(1f, 0.85f, 0.3f);
        }

        static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.enabled = false;
            return sr;
        }

        bool DecoysUp => m_Decoys.Count > 0;
        bool Holding => m_Holding != KieSteal.Nothing || m_Spin >= 0f;
        KieAttack Next => KieTankSpec.AttackFor(m_Turn, DecoysUp, Holding);

        /// <summary>The next charged shot comes down somewhere, so the landing marker means something.</summary>
        public bool NextLands => Next == KieAttack.Shell || Next == KieAttack.Weasels;

        /// <summary>Extra seconds the tank rests after the attack just made (the wheel spinning).</summary>
        public float Wait { get; private set; }

        /// <summary>The tank's charged shot: kie's attack in its place, or false for a plain shell.</summary>
        public bool Fire(Vector2 muzzle, float angle)
        {
            KieAttack attack = Next;
            m_Turn++;
            Wait = 0f;
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
                    Player p = m_World.Player;
                    m_Pick = KieTankSpec.Pick(p.Specials, p.Kit.SuperCharge, m_World.Rng.Value);
                    m_Spin = 0f;
                    Wait = Spec.SpinTime / m_Tank.Fury + 0.3f;
                    m_World.Scoring.Popup("ROULETTE!", m_Tank.Pos + Vector2.up * 4.4f, new Color(1f, 0.6f, 0.9f));
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
                m_World.Enemies.Add(new Weasel(m_World, muzzle, dir * (power * k), speed));
            }
            Sfx.Play(Sfx.Throw, 0.9f);
            Sfx.Play(Sfx.Cannon, 0.5f);
            m_World.Scoring.Popup("WEASELS!", m_Tank.Pos + Vector2.up * 4.4f, new Color(1f, 0.75f, 0.4f));
        }

        // ------------------------------------------------------------------ holograms

        void Holograms()
        {
            float[] slots = Spec.Slots(m_HomeX);
            int real = m_World.Rng.Range(0, slots.Length);
            float y = m_Tank.Pos.y;
            m_World.Burst(m_Tank.Center, 14, new Color(0.45f, 1f, 1f));
            for (int i = 0; i < slots.Length; i++)
            {
                var at = new Vector2(Standable(slots[i], y), y);
                if (i == real)
                {
                    m_Tank.Pos = at;
                    m_Tank.HomeX = at.x;
                }
                else
                {
                    var copy = new TankBoss(m_World, m_Tank.Def, at, decoy: true) { Hp = m_Tank.Hp, MaxHp = m_Tank.MaxHp, Facing = m_Tank.Facing };
                    copy.Look = new HoloLook(copy);
                    m_World.Enemies.Add(copy);
                    m_Decoys.Add(copy);
                }
                m_World.Burst(at + Vector2.up, 10, new Color(0.45f, 1f, 1f));
            }
            m_Tank.Look = new HoloLook(m_Tank);
            m_DecoyTime = Spec.DecoyTime;
            Sfx.Play(Sfx.Glitch, 0.9f);
            m_World.Scoring.Popup("WHICH ONE?", m_Tank.Pos + Vector2.up * 4.4f, new Color(0.45f, 1f, 1f));
        }

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

        void EndHolograms(bool found)
        {
            if (!DecoysUp) return;
            foreach (TankBoss copy in m_Decoys)
                if (!copy.Removed) copy.Dying = true; // glitches out
            m_Decoys.Clear();
            m_Tank.Look?.Detach();
            m_Tank.Look = null;
            m_Tank.HomeX = m_HomeX;
            if (found) m_World.Scoring.Popup("FOUND ME!", m_Tank.Pos + Vector2.up * 4.4f, Color.white);
        }

        // ------------------------------------------------------------------ the roulette

        void Steal()
        {
            m_Spin = -1f;
            Player p = m_World.Player;
            Vector2 over = p.Pos + Vector2.up * 2.6f;
            int amount = m_Pick == KieSteal.Special ? p.Specials : m_Pick == KieSteal.Super ? Mathf.RoundToInt(p.Kit.SuperCharge * p.Kit.SuperKills) : 0;
            if (amount <= 0)
            {
                m_World.Scoring.Popup("EMPTY POCKETS!", over, new Color(0.8f, 0.8f, 0.85f));
                return;
            }
            if (m_Pick == KieSteal.Special) p.Specials = 0;
            else amount = p.Kit.TakeSuper();
            m_Holding = m_Pick;
            m_Held = amount;
            m_Damage = 0;
            m_World.Scoring.Popup(m_Pick == KieSteal.Special ? p.Def.Special.Hud + " STOLEN!" : "SUPER STOLEN!", over, new Color(1f, 0.45f, 0.8f));
            Sfx.Play(Sfx.Glitch, 0.7f);
            Sfx.Play(Sfx.Pickup, 0.6f);
            // the loot streams off you into the tank
            Vector2 to = m_Tank.Center + Vector2.up * 1.5f;
            for (int i = 0; i < 16; i++)
            {
                Vector2 from = p.Center + new Vector2(Rng.Fx.Range(-0.3f, 0.3f), Rng.Fx.Range(-0.3f, 0.3f));
                float t = Rng.Fx.Range(0.35f, 0.6f);
                var vel = (to - from) / t + Vector2.up * Rng.Fx.Range(-1f, 3f);
                m_World.SpawnParticle(from, vel, i % 2 == 0 ? new Color(1f, 0.45f, 0.8f) : new Color(1f, 0.85f, 0.3f), Rng.Fx.Range(1.5f, 2.5f), t);
            }
        }

        void Drop()
        {
            if (m_Holding == KieSteal.Nothing) return;
            float side = m_World.Player.Pos.x < m_Tank.Pos.x ? -1f : 1f;
            m_World.Props.Add(new KieLoot(m_World, m_Tank.Center + Vector2.up * 1.6f, new Vector2(side * 3.5f, 9f), m_Holding, m_Held));
            m_World.Scoring.Popup("DROPPED IT!", m_Tank.Pos + Vector2.up * 4.4f, new Color(1f, 0.85f, 0.3f));
            Sfx.Play(Sfx.Thump, 0.8f);
            m_Holding = KieSteal.Nothing;
            m_Held = 0;
        }

        // ------------------------------------------------------------------ the tank's side

        /// <summary>Each tick of the tank, once it has woken.</summary>
        public void Tick(float dt)
        {
            m_Shown += dt;
            if (DecoysUp)
            {
                m_DecoyTime -= dt;
                if (m_DecoyTime <= 0f) EndHolograms(false);
            }
            if (m_Spin >= 0f)
            {
                m_Spin += dt;
                if (m_Spin >= Spec.SpinTime / m_Tank.Fury) Steal();
            }
        }

        /// <summary>The real tank took <paramref name="damage"/>.</summary>
        public void OnHurt(int damage)
        {
            EndHolograms(true);
            if (m_Holding == KieSteal.Nothing) return;
            m_Damage += damage;
            if (m_Damage >= Spec.DropDamage(m_Tank.MaxHp)) Drop();
        }

        /// <summary>The tank is destroyed: the trick ends and anything it stole falls out.</summary>
        public void OnDie()
        {
            EndHolograms(false);
            m_Spin = -1f;
            Drop();
            m_Frame.enabled = m_Icon.enabled = m_BarBack.enabled = m_BarFill.enabled = false;
        }

        public void Render()
        {
            bool spinning = m_Spin >= 0f, holding = m_Holding != KieSteal.Nothing;
            bool shown = !m_Tank.Dying && (spinning || holding);
            m_Frame.enabled = m_Icon.enabled = shown;
            m_BarBack.enabled = m_BarFill.enabled = shown && holding;
            if (!shown) return;

            const float y = 3.5f;
            float bob = holding ? Mathf.Round(Mathf.Sin(m_Shown * 4f) * 1.5f) / Art.PPU : 0f;
            m_Frame.transform.localPosition = m_Icon.transform.localPosition = new Vector3(0f, y + bob, 0f);
            KieSteal face;
            if (spinning)
            {
                // flicks between the two, slowing, and settles on what it'll take for the last part of the spin
                float t = m_Spin / (Spec.SpinTime / m_Tank.Fury);
                face = t > 0.8f ? m_Pick : ((int)(Mathf.Sqrt(t) * 14f) % 2 == 0 ? KieSteal.Special : KieSteal.Super);
                if (face == KieSteal.Nothing) face = (int)(m_Spin * 8f) % 2 == 0 ? KieSteal.Special : KieSteal.Super;
                m_Frame.color = t > 0.8f && (int)(m_Spin * 16f) % 2 == 0 ? Color.white : new Color(1f, 0.45f, 0.8f);
            }
            else
            {
                face = m_Holding;
                m_Frame.color = new Color(1f, 0.45f, 0.8f);
            }
            m_Icon.sprite = face == KieSteal.Super ? KieArt.Super : KieArt.Special;
            if (!holding) return;

            // how close it is to dropping it
            const float width = 18f;
            float fill = Mathf.Round(Mathf.Clamp01(m_Damage / (float)Spec.DropDamage(m_Tank.MaxHp)) * (width - 2f));
            float barY = y - 0.75f + bob;
            m_BarBack.transform.localPosition = new Vector3(0f, barY, 0f);
            m_BarBack.transform.localScale = new Vector3(width, 4f, 1f);
            m_BarFill.transform.localPosition = new Vector3((-(width - 2f) * 0.5f + fill * 0.5f) / Art.PPU, barY, 0f);
            m_BarFill.transform.localScale = new Vector3(fill, 2f, 1f);
        }
    }

    /// <summary>Spike: one of kie's weasels. Thrown from the tank, it lands, scurries at you and pops; shoot it first.</summary>
    public sealed class Weasel : Enemy
    {
        readonly SpriteRenderer m_Sr;
        readonly float m_Speed;
        float m_Fuse;
        bool m_Landed;

        public Weasel(World world, Vector2 at, Vector2 vel, float speed) : base(world, KieTankSpec.Weasel, at)
        {
            Vel = vel;
            m_Speed = speed;
            Facing = vel.x >= 0f ? 1 : -1;
            m_Sr = AddRenderer(Visual, "Body", KieArt.Weasel[0], SortOrder.Enemies + 1);
        }

        protected override void TickAlive(float dt)
        {
            KieTankSpec spec = KieBoss.Spec;
            if (!m_Landed)
            {
                Vel.y = Mathf.Max(Vel.y - spec.WeaselGravity * dt, -MaxFall);
                MoveWithTiles(dt);
                if (Grounded) m_Landed = true;
                return;
            }

            m_Fuse += dt;
            Vel.x = 0f;
            if (!Target.IsDown)
            {
                FaceTarget();
                Vel.x = Facing * m_Speed;
            }
            Vel.y = Mathf.Max(Vel.y - Gravity * dt, -MaxFall);
            MoveResult r = MoveWithTiles(dt);
            if (r.HitWall && Grounded) Vel.y = 10f;
            bool close = !Target.IsDown && (Target.Center - Center).sqrMagnitude < spec.WeaselReach * spec.WeaselReach;
            if (close || m_Fuse >= spec.WeaselFuse) Pop();
        }

        void Pop()
        {
            KieTankSpec spec = KieBoss.Spec;
            World.Combat.Explode(Center, spec.WeaselBlast, spec.WeaselDamage, true, 0.6f);
            Remove();
        }

        protected override void Die()
        {
            base.Die();
            World.Combat.Explode(Center, 0.4f, 0, false, 0.35f); // shot: a harmless pop
            Remove();
        }

        public override void Render()
        {
            base.Render();
            bool running = m_Landed && Grounded && Mathf.Abs(Vel.x) > 0.1f;
            m_Sr.sprite = Flashed(KieArt.Weasel[running && (int)(Anim * 16f) % 2 == 1 ? 1 : 0]);
            // a weasel in the air tumbles
            Visual.localRotation = m_Landed ? Quaternion.identity : Quaternion.Euler(0f, 0f, Anim * -720f * Facing);
        }
    }

    /// <summary>Spike: what kie's tank stole, knocked loose. Grab it to get it back.</summary>
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

    /// <summary>Spike art for kie's tank: the weasel, and the roulette's frame and icons.</summary>
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
