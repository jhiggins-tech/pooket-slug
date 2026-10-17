using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.DenRoom;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>A crewmate's figure (<see cref="StageFigure"/>), with a controller for the couch and tones2's slushie; sitting, the torso drops and the legs fold.</summary>
    sealed class DenFigure
    {
        public readonly StageFigure Body;
        public readonly Image Controller, Cup;
        public RectTransform Rt => Body.Rt;
        public Image Legs => Body.Legs;
        public Image Torso => Body.Torso;

        public DenFigure(RectTransform world, string name)
        {
            Body = new StageFigure(world, name);
            Controller = Ui.Picture(Rt, "Controller", null, Ui.Middle, Ui.Middle, new Vector2(5f, -6f) * Px, new Vector2(5f, 3f) * Px, Pad);
            Cup = Ui.Picture(Rt, "Slushie", null, Ui.Middle, Ui.Middle, new Vector2(6f, 0f) * Px, new Vector2(3f, 5f) * Px, new Color(0.55f, 0.35f, 0.95f));
        }
    }

    enum DenDoing { Station, Walking, Seated, Bit, Away }

    /// <summary>A crewmate in the den, and what they're up to.</summary>
    sealed class DenActor
    {
        public SoldierDef Soldier;
        public Station Home;
        public DenFigure Fig;
        public float X, Y, TargetX, TargetY;
        public bool FaceLeft, Jumping;
        public DenDoing Doing;
        public int Seat = -1, Skin;
        public int Cycle;
        public float CycleT, CycleLen;
    }

    /// <summary>
    /// The den's crew, frame by frame (<c>docs/design/lobby.md</c>): each freed crewmate at their station running little
    /// idle loops, walking to the couch when picked (loft crew vault the railing) and home again, away while a co-op
    /// partner plays them in the mission, and a partner whose soldier isn't freed here walking in as a visitor; and the
    /// station props that move with them (tones2's spray, kie's tiny Trollogram, the weasel, the bits' slushie).
    /// </summary>
    sealed class DenCrew
    {
        const float WalkSpeed = 46f;

        readonly RectTransform m_World;
        readonly System.Random m_Rng;
        readonly List<DenActor> m_Actors = new List<DenActor>();
        readonly Image[] m_Drops = new Image[6];
        Image m_Holo, m_Weasel, m_Splash;
        DenFigure m_Visitor;
        DenActor m_VisitorActor;
        float m_Clock;

        /// <summary>The bits the crew do (set once both are made): picking someone ends theirs, and the slushie bit has props.</summary>
        public DenBitPlayer Bits;

        public DenCrew(RectTransform world, System.Random rng)
        {
            m_World = world;
            m_Rng = rng;
        }

        public IReadOnlyList<DenActor> Actors => m_Actors;

        Image Picture(string name, float x, float y, float w, float h, Color color) => StageKit.Picture(m_World, name, null, x, y, w, h, color);

        // ------------------------------------------------------------------ made in drawing order (DenView)

        /// <summary>The station props that move: tones2's spray, kie's tiny Trollogram and the weasel, and the bits' slushie.</summary>
        public void MakeProps()
        {
            for (int i = 0; i < m_Drops.Length; i++) m_Drops[i] = Picture("Water", 0f, 0f, 3f, 2f, new Color(0.45f, 0.75f, 1f));
            m_Holo = Picture("Tiny Trollogram", 22f, Floor + 26f, 6f, 9f, new Color(0.45f, 1f, 1f, 0.7f));
            m_Weasel = Picture("Weasel", 14f, Floor + 6f, 5f, 4f, new Color(0.72f, 0.5f, 0.3f));
            m_Splash = Picture("Splash", 0f, Floor, 10f, 2f, new Color(0.55f, 0.35f, 0.95f));
        }

        /// <summary>The visitor's figure, in front of the stations.</summary>
        public void MakeVisitor() => m_Visitor = new DenFigure(m_World, "Visitor");

        // ------------------------------------------------------------------ opening the den

        /// <summary>Each freed crewmate at their station, or already on the couch; drawn just in front of <paramref name="back"/>.</summary>
        public void Begin(Den den, DenInfo info, Transform back)
        {
            foreach (DenActor a in m_Actors) Object.Destroy(a.Fig.Rt.gameObject);
            m_Actors.Clear();
            foreach (SoldierDef s in den.Crew)
            {
                Station home = null;
                foreach (Station st in Stations)
                    if (st.Soldier == s) home = st;
                if (home == null) continue;
                var a = new DenActor { Soldier = s, Home = home, Fig = new DenFigure(m_World, s.Name), X = home.Spot, Y = home.Upstairs ? Loft : Floor };
                a.TargetX = a.X;
                a.TargetY = a.Y;
                a.FaceLeft = a.X > 160f;
                a.Fig.Rt.SetSiblingIndex(back.GetSiblingIndex() + 1);
                m_Actors.Add(a);
                if (den.Pick == s) Sit(a, den.Seat, info.Skin, instant: true);
            }
            m_VisitorActor = null;
            Ui.Show(m_Visitor.Rt, false);
        }

        // ------------------------------------------------------------------ each frame

        /// <summary>Who goes where: our pick to our seat, the partner's to theirs (a partner in the mission isn't in the den).</summary>
        public void Arrange(Den den, DenInfo info)
        {
            DenPartner partner = den.Partner;
            int partnerSeat = 1 - den.Seat;
            foreach (DenActor a in m_Actors)
            {
                if (a.Soldier == den.Pick) Sit(a, den.Seat, info.Skin, instant: false);
                else if (den.Mode == DenMode.Coop && partner.Soldier == a.Soldier && partner.InDenNow && partner.Seated) Sit(a, partnerSeat, 1 - info.Skin, instant: false);
                else if (den.Mode == DenMode.Coop && partner.Soldier == a.Soldier && partner.Playing) a.Doing = DenDoing.Away;
                else if (a.Seat >= 0 || a.Doing == DenDoing.Away) GoHome(a);
            }
            Visitor(den, partner, partnerSeat, 1 - info.Skin);
        }

        /// <summary>Moves everyone on and draws them, and the props (<paramref name="clock"/> is the den's).</summary>
        public void Draw(float dt, float clock, Den den)
        {
            m_Clock = clock;
            foreach (DenActor a in m_Actors) Move(a, dt);
            foreach (DenActor a in m_Actors) Draw(a, den);
            if (m_VisitorActor != null)
            {
                Move(m_VisitorActor, dt);
                Draw(m_VisitorActor, den);
            }
            Props();
        }

        /// <summary>The visitor's sitting in seat <paramref name="seat"/>.</summary>
        public bool VisitorOn(int seat) => m_VisitorActor != null && m_VisitorActor.Seat == seat;

        /// <summary>A partner whose soldier isn't freed on this phone walks in by the roller door and sits down as a visitor.</summary>
        void Visitor(Den den, DenPartner partner, int seat, int skin)
        {
            bool needed = den.Mode == DenMode.Coop && partner.InDenNow && partner.Seated && partner.Soldier != null && !Has(den.Crew, partner.Soldier);
            if (!needed)
            {
                m_VisitorActor = null;
                Ui.Show(m_Visitor.Rt, false);
                return;
            }
            if (m_VisitorActor == null || m_VisitorActor.Soldier != partner.Soldier)
            {
                m_VisitorActor = new DenActor { Soldier = partner.Soldier, Fig = m_Visitor, X = 298f, Y = Floor, FaceLeft = true };
                Ui.Show(m_Visitor.Rt, true);
            }
            Sit(m_VisitorActor, seat, skin, instant: false);
        }

        void Sit(DenActor a, int seat, int skin, bool instant)
        {
            a.Skin = skin;
            if (a.Seat == seat && (a.Doing == DenDoing.Seated || a.Doing == DenDoing.Walking)) return;
            Bits.EndIfIn(a);
            a.Seat = seat;
            a.TargetX = SeatX[seat];
            a.TargetY = Floor;
            if (instant)
            {
                a.X = a.TargetX;
                a.Y = a.TargetY;
                a.Doing = DenDoing.Seated;
            }
            else
            {
                a.Doing = DenDoing.Walking;
                a.Jumping = a.Y > Floor + 1f; // loft crew vault the railing and drop down
                Sfx.Play(Sfx.Pickup, 0.4f);
            }
        }

        void GoHome(DenActor a)
        {
            if (a.Doing == DenDoing.Bit) return;
            a.Seat = -1;
            a.Skin = 0;
            a.TargetX = a.Home.Spot;
            a.TargetY = a.Home.Upstairs ? Loft : Floor;
            if (a.Doing == DenDoing.Away)
            {
                a.X = a.TargetX; // back from the mission: they're just there
                a.Y = a.TargetY;
                a.Doing = DenDoing.Station;
                return;
            }
            if (a.Doing == DenDoing.Seated) Sfx.Play(Sfx.Thump, 0.3f); // handed the controller over with a shrug
            a.Doing = DenDoing.Walking;
        }

        void Move(DenActor a, float dt)
        {
            if (a.Doing != DenDoing.Walking && a.Doing != DenDoing.Bit) return;
            float dx = a.TargetX - a.X;
            if (Mathf.Abs(dx) > 0.5f)
            {
                a.FaceLeft = dx < 0f;
                a.X += Mathf.Clamp(dx, -WalkSpeed * dt, WalkSpeed * dt);
            }
            else a.X = a.TargetX;
            if (a.Jumping)
            {
                // a drop from the loft: a short hop off the railing, then down
                a.Y = Mathf.MoveTowards(a.Y, a.TargetY, 140f * dt);
                if (Mathf.Abs(a.Y - a.TargetY) < 0.5f)
                {
                    a.Jumping = false;
                    Sfx.Play(Sfx.Thump, 0.6f);
                }
            }
            else a.Y = Mathf.MoveTowards(a.Y, a.TargetY, 140f * dt);
            if (a.Doing == DenDoing.Walking && Mathf.Abs(a.X - a.TargetX) < 0.5f && Mathf.Abs(a.Y - a.TargetY) < 0.5f)
            {
                a.Doing = a.Seat >= 0 ? DenDoing.Seated : DenDoing.Station;
                if (a.Seat >= 0) Sfx.Play(Sfx.Thump, 0.5f); // flops onto the couch
                a.FaceLeft = a.Seat < 0 && a.X > 160f;
                a.CycleT = a.CycleLen = 0f;
            }
        }

        void Draw(DenActor a, Den den)
        {
            DenFigure f = a.Fig;
            bool away = a.Doing == DenDoing.Away;
            Ui.Show(f.Rt, !away);
            if (away) return;
            PlayerSkin skin = Art.SkinFor(a.Soldier, a.Skin);
            bool seated = a.Doing == DenDoing.Seated;
            bool walking = (a.Doing == DenDoing.Walking || a.Doing == DenDoing.Bit) && Mathf.Abs(a.X - a.TargetX) > 0.5f;
            f.Legs.sprite = seated ? skin.LegsCrouch : a.Jumping ? skin.LegsJump : walking ? skin.LegsRun[(int)(m_Clock * 10f) % skin.LegsRun.Length] : skin.LegsIdle;
            f.Torso.sprite = skin.TorsoForward;
            float bob = 0f, shiver = 0f;
            if (a.Doing == DenDoing.Station) Idle(a, out bob, out shiver);
            f.Torso.rectTransform.anchoredPosition = new Vector2(0f, (seated ? -6f : bob) * Px);
            float y = seated ? SeatY - 5f : a.Y;
            f.Rt.anchoredPosition = new Vector2(a.X + shiver, y) * Px;
            f.Rt.localScale = new Vector3(a.FaceLeft && !seated ? -1f : 1f, 1f, 1f);
            Ui.Show(f.Controller, seated);
            if (seated) f.Controller.rectTransform.anchoredPosition = new Vector2(5f, -7f + (den.Ready && a.Seat == den.Seat ? 0f : Mathf.Sin(m_Clock * 6f + a.X) * 0.5f)) * Px;
            bool cup = a.Soldier == Content.Tones2 && (a.Doing == DenDoing.Station && a.Cycle == 0 || a.Doing == DenDoing.Bit && Bits.Slushie && Bits.T < 3.4f);
            Ui.Show(f.Cup, cup);
            if (cup) f.Cup.rectTransform.anchoredPosition = new Vector2(5f, a.Doing == DenDoing.Station && a.CycleT > 0.6f && a.CycleT < 2.2f ? 8f : 1f) * Px;
        }

        /// <summary>
        /// A crewmate at their station runs short idle loops, then breathes for a bit (docs/design/lobby.md, Who lives where):
        /// tones2 slurps the slushie and gets brain freeze, or sprays the machine clean; kie tinkers with the projector, which
        /// flickers a tiny Trollogram of him, or a weasel pops out of the hutch.
        /// </summary>
        void Idle(DenActor a, out float bob, out float shiver)
        {
            bob = (int)(m_Clock * 1.6f + a.X) % 2 == 0 ? 0f : -0.5f;
            shiver = 0f;
            a.CycleT += Time.unscaledDeltaTime;
            if (a.CycleT >= a.CycleLen)
            {
                a.Cycle = m_Rng.Next(3); // 2: just breathing
                a.CycleLen = a.Cycle == 2 ? 2.5f + (float)m_Rng.NextDouble() * 2f : 4.5f;
                a.CycleT = 0f;
                if (a.Soldier == Content.Kie && a.Cycle == 0) Sfx.Play(Sfx.Glitch, 0.25f);
            }
            if (a.Soldier == Content.Tones2)
            {
                a.FaceLeft = a.Cycle == 1; // sprays the slushie machine, to his left
                if (a.Cycle == 0 && a.CycleT > 2.2f && a.CycleT < 3.4f) shiver = (int)(m_Clock * 30f) % 2 == 0 ? 0.6f : -0.6f; // brain freeze
            }
            else if (a.Soldier == Content.Kie) a.FaceLeft = a.Cycle == 0; // the projector's on the bench behind him
        }

        /// <summary>The station props that move: tones2's spray, kie's tiny Trollogram and the weasel, and the bits' slushie.</summary>
        void Props()
        {
            DenActor tones2 = Find(Content.Tones2), kie = Find(Content.Kie);
            bool spraying = tones2 != null && tones2.Doing == DenDoing.Station && tones2.Cycle == 1 && tones2.CycleT > 0.5f && tones2.CycleT < 3.5f;
            for (int i = 0; i < m_Drops.Length; i++)
            {
                Ui.Show(m_Drops[i], spraying);
                if (!spraying) continue;
                float k = (m_Clock * 2.2f + i / (float)m_Drops.Length) % 1f;
                m_Drops[i].rectTransform.anchoredPosition = new Vector2(tones2.X - 4f - k * 14f, Floor + 15f + Mathf.Sin(k * Mathf.PI) * 4f) * Px;
            }
            bool tinkering = kie != null && kie.Doing == DenDoing.Station && kie.Cycle == 0 && kie.CycleT > 0.8f && kie.CycleT < 3.6f;
            DenActor first = Bits.First;
            bool holoBit = Bits.Slushie && Bits.T > 2.6f && Bits.T < 3.6f;
            Ui.Show(m_Holo, tinkering || holoBit);
            if (tinkering) m_Holo.rectTransform.anchoredPosition = new Vector2(22f + ((int)(m_Clock * 14f) % 5 == 0 ? 1f : 0f), Floor + 26f) * Px;
            if (holoBit && first != null) m_Holo.rectTransform.anchoredPosition = new Vector2(first.X + 10f, Floor + 14f) * Px;
            m_Holo.color = new Color(0.45f, 1f, 1f, (int)(m_Clock * 20f) % 3 == 0 ? 0.35f : 0.75f);
            bool weasel = kie != null && kie.Doing == DenDoing.Station && kie.Cycle == 1;
            Ui.Show(m_Weasel, weasel);
            if (weasel) m_Weasel.rectTransform.anchoredPosition = new Vector2(14f, Floor + 5f + Mathf.Max(0f, Mathf.Sin(kie.CycleT * 2.4f)) * 7f) * Px;
            bool splash = Bits.Slushie && Bits.T > 3.5f;
            Ui.Show(m_Splash, splash);
            if (splash && first != null) m_Splash.rectTransform.anchoredPosition = new Vector2(first.X + 9f, Floor) * Px;
        }

        /// <summary>The crewmate playing <paramref name="s"/>, or null.</summary>
        public DenActor Find(SoldierDef s)
        {
            foreach (DenActor a in m_Actors)
                if (a.Soldier == s) return a;
            return null;
        }
    }
}
