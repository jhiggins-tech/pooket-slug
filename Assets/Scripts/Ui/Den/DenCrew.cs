using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.DenRoom;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>A crewmate's figure (<see cref="StageFigure"/>), with tones2's slushie; sitting, the torso drops and the legs fold (the off-duty poses: <see cref="CrewBody.Pad"/>).</summary>
    sealed class DenFigure
    {
        public readonly StageFigure Body;
        public readonly Image Cup;
        public RectTransform Rt => Body.Rt;
        public Image Legs => Body.Legs;
        public Image Torso => Body.Torso;

        public DenFigure(RectTransform world, string name)
        {
            Body = new StageFigure(world, name);
            Cup = Ui.Picture(Rt, "Slushie", CutsceneSnacks.Tiny, Ui.Middle, Ui.Middle, new Vector2(6f, 0f) * Px, new Vector2(3f, 5f) * Px, Color.white);
        }
    }

    /// <summary>What a crewmate in the den is up to. <see cref="Yours"/> is the soldier you're playing, walked by you.</summary>
    enum DenDoing { Station, Walking, Seated, Bit, Away, Yours }

    /// <summary>A crewmate in the den, and what they're up to.</summary>
    sealed class DenActor
    {
        public SoldierDef Soldier;
        public Station Home;
        public DenFigure Fig;
        public DenWalker Walk;
        public DenDoing Doing;
        public int Seat = -1;
        public int Cycle;
        public float CycleT, CycleLen;
        public float X => Walk.X;
        public float Y => Walk.Y;
        /// <summary>Sitting on the couch: the partner's pick, or yours while you're ready.</summary>
        public bool OnCouch => Doing == DenDoing.Seated || Doing == DenDoing.Yours && Seat >= 0;
    }

    /// <summary>
    /// The den's crew, frame by frame (<c>docs/design/lobby.md</c>, Den 2.0): the soldier you're playing, walked round the
    /// den and loft by you (<see cref="Mine"/>), and each other freed crewmate at their station running little idle loops,
    /// walking home when swapped out (by the ladder, as everyone goes between floors), away while a co-op partner plays
    /// them in the mission, or on the couch as the partner's pick; a partner whose soldier isn't freed here walking in as a
    /// visitor; and the station props that move with them (tones2's spray, kie's tiny Trollogram, the weasel, the bits' slushie).
    /// </summary>
    sealed class DenCrew
    {
        readonly RectTransform m_World;
        readonly System.Random m_Rng;
        readonly List<DenActor> m_Actors = new List<DenActor>();
        readonly Image[] m_Drops = new Image[6];
        Image m_Holo, m_Weasel, m_Splash;
        DenFigure m_Visitor;
        DenActor m_VisitorActor;
        float m_Clock;

        /// <summary>How far through the roller door to the tabk bay your soldier is (<see cref="RollerDoor.Through"/>).</summary>
        public float Through;

        /// <summary>The den's beat playing (<see cref="DenBeatView"/> draws whoever's in it, so they're hidden here), or null.</summary>
        public DenBeat Beat;

        /// <summary>The bits the crew do (set once both are made): swapping someone in ends theirs, and the slushie bit has props.</summary>
        public DenBitPlayer Bits;

        public DenCrew(RectTransform world, System.Random rng)
        {
            m_World = world;
            m_Rng = rng;
        }

        public IReadOnlyList<DenActor> Actors => m_Actors;

        /// <summary>The soldier you're playing, or null.</summary>
        public DenActor Mine { get; private set; }

        Image Picture(string name, float x, float y, float w, float h, Color color) => StageKit.Picture(m_World, name, null, x, y, w, h, color);

        // ------------------------------------------------------------------ made in drawing order (DenView)

        /// <summary>The station props that move: tones2's spray, kie's tiny Trollogram and the weasel, and the bits' slushie.</summary>
        public void MakeProps()
        {
            for (int i = 0; i < m_Drops.Length; i++) m_Drops[i] = Picture("Water", 0f, 0f, 3f, 2f, new Color(0.45f, 0.75f, 1f));
            m_Holo = Picture("Tiny Trollogram", Holo.x, Holo.y, 6f, 9f, new Color(0.45f, 1f, 1f, 0.7f));
            m_Weasel = Picture("Weasel", WeaselHole.x, WeaselHole.y, 5f, 4f, new Color(0.72f, 0.5f, 0.3f));
            m_Splash = Picture("Splash", 0f, DenMap.FloorY(DenFloor.Ground), 10f, 2f, Art.Hex(0x40a8ff)); // blue raspberry
        }

        /// <summary>The visitor's figure, in front of the stations.</summary>
        public void MakeVisitor() => m_Visitor = new DenFigure(m_World, "Visitor");

        // ------------------------------------------------------------------ opening the den

        /// <summary>
        /// Each freed crewmate at their station, and the one you're playing standing at the couch; drawn just in front
        /// of <paramref name="back"/>.
        /// </summary>
        public void Begin(Den den, DenInfo info, Transform back)
        {
            foreach (DenActor a in m_Actors) Object.Destroy(a.Fig.Rt.gameObject);
            m_Actors.Clear();
            Mine = null;
            foreach (SoldierDef s in den.Crew)
            {
                Station home = null;
                foreach (Station st in Stations)
                    if (st.Soldier == s) home = st;
                if (home == null) continue;
                var a = new DenActor { Soldier = s, Home = home, Fig = new DenFigure(m_World, s.Name), Walk = new DenWalker(home.Floor, home.Spot) };
                a.Walk.Face(home.Spot > 160f);
                a.Fig.Rt.SetSiblingIndex(back.GetSiblingIndex() + 1);
                m_Actors.Add(a);
                if (den.Pick == s)
                {
                    a.Walk.Place(DenFloor.Ground, DenMap.XOf(DenPlace.Couch));
                    TakeOver(a);
                }
            }
            m_VisitorActor = null;
            Ui.Show(m_Visitor.Rt, false);
        }

        // ------------------------------------------------------------------ each frame

        /// <summary>
        /// Who goes where: the soldier you're playing is yours to walk (on the couch while you're ready), the partner's
        /// pick to their seat (a partner in the mission isn't in the den), and everyone else home.
        /// </summary>
        public void Arrange(Den den, DenInfo info)
        {
            DenPartner partner = den.Partner;
            int partnerSeat = 1 - den.Seat;
            foreach (DenActor a in m_Actors)
            {
                if (a.Soldier == den.Pick)
                {
                    if (a != Mine) TakeOver(a);
                    a.Seat = den.Ready ? den.Seat : -1;
                }
                else if (den.Mode == DenMode.Coop && partner.Soldier == a.Soldier && partner.InDenNow && partner.Seated) Sit(a, partnerSeat);
                else if (den.Mode == DenMode.Coop && partner.Soldier == a.Soldier && partner.Playing) a.Doing = DenDoing.Away;
                else if (a.Doing == DenDoing.Yours || a.Seat >= 0 || a.Doing == DenDoing.Away) GoHome(a);
            }
            if (Mine != null && Mine.Soldier != den.Pick) Mine = null;
            Visitor(den, partner, partnerSeat);
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
        void Visitor(Den den, DenPartner partner, int seat)
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
                m_VisitorActor = new DenActor { Soldier = partner.Soldier, Fig = m_Visitor, Walk = new DenWalker(DenFloor.Ground, DenMap.XOf(DenPlace.RollerDoor)) };
                m_VisitorActor.Walk.Face(true);
                Ui.Show(m_Visitor.Rt, true);
            }
            Sit(m_VisitorActor, seat);
        }

        /// <summary>You're playing <paramref name="a"/> now: they stop what they're doing and are yours to walk.</summary>
        void TakeOver(DenActor a)
        {
            Bits.EndIfIn(a);
            Mine = a;
            a.Doing = DenDoing.Yours;
            a.Seat = -1;
            a.Walk.WalkTo(a.Walk.Floor, a.Walk.X); // stop where they stand
            Sfx.Play(Sfx.Pickup, 0.4f);
        }

        /// <summary>Off to seat <paramref name="seat"/> (the partner's pick, or the visitor).</summary>
        void Sit(DenActor a, int seat)
        {
            if (a.Seat == seat && (a.Doing == DenDoing.Seated || a.Doing == DenDoing.Walking)) return;
            if (a.Home != null) Bits.EndIfIn(a);
            a.Seat = seat;
            a.Walk.WalkTo(DenFloor.Ground, SeatX[seat]);
            a.Doing = DenDoing.Walking;
        }

        void GoHome(DenActor a)
        {
            if (a.Doing == DenDoing.Bit) return;
            a.Seat = -1;
            if (a.Doing == DenDoing.Away)
            {
                a.Walk.Place(a.Home.Floor, a.Home.Spot); // back from the mission: they're just there
                a.Doing = DenDoing.Station;
                return;
            }
            if (a.Doing == DenDoing.Yours || a.Doing == DenDoing.Seated) Sfx.Play(Sfx.Thump, 0.3f); // handed over with a shrug
            a.Walk.WalkTo(a.Home.Floor, a.Home.Spot);
            a.Doing = DenDoing.Walking;
        }

        /// <summary>Sends a crewmate somewhere on their own floor (a bit's walk over), or home.</summary>
        public void Send(DenActor a, float x) => a.Walk.WalkTo(a.Walk.Floor, x);

        void Move(DenActor a, float dt)
        {
            if (a.Doing == DenDoing.Station || a.Doing == DenDoing.Away || a.Doing == DenDoing.Seated) return;
            if (a.Doing == DenDoing.Yours && a.Seat >= 0) return; // sitting on the couch, ready
            DenPlace reached = a.Walk.Step(dt);
            if (a == Mine && reached != DenPlace.None) m_Reached = reached;
            if (a.Doing == DenDoing.Walking && a.Walk.Standing)
            {
                a.Doing = a.Seat >= 0 ? DenDoing.Seated : DenDoing.Station;
                if (a.Seat >= 0) Sfx.Play(Sfx.Thump, 0.5f); // flops onto the couch
                else if (a.Home != null) a.Walk.Face(a.Home.Spot > 160f);
                a.CycleT = a.CycleLen = 0f;
            }
        }

        DenPlace m_Reached;

        /// <summary>The place the soldier you're playing has just walked up to, to use it; once.</summary>
        public DenPlace TakeReached()
        {
            DenPlace p = m_Reached;
            m_Reached = DenPlace.None;
            return p;
        }

        void Draw(DenActor a, Den den)
        {
            DenFigure f = a.Fig;
            bool away = a.Doing == DenDoing.Away || InBeat(a);
            Ui.Show(f.Rt, !away);
            if (away) return;
            PlayerSkin skin = Art.SkinFor(a.Soldier);
            bool seated = a.OnCouch;
            bool climbing = a.Walk.Climbing;
            bool walking = !seated && !a.Walk.Standing && !climbing;
            // off duty, no guns: gaming on the couch, lounging at their station between loops (and reaching up the ladder)
            bool lounging = climbing || a.Doing == DenDoing.Station && a.Cycle == DenIdle.Breath;
            float bob = 0f, shiver = 0f;
            if (a.Doing == DenDoing.Station) Idle(a, out bob, out shiver);
            else if (climbing) shiver = Fx.Shiver(m_Clock, 6f, 0.5f); // hand over hand
            StageLegs legs = seated ? StageLegs.Sit : climbing ? StageLegs.Jump : walking ? StageLegs.Walk : StageLegs.Idle;
            StageTorso top = seated ? StageTorso.Pad : lounging ? StageTorso.Lounge : StageTorso.Unarmed;
            float x = seated ? SeatX[a.Seat] : a.X, y = seated ? SitY : a.Y;
            Vector2 sill = DenRollerDoor.Sill; // the step through the roller door walks the soldier through the door
            float through = a == Mine ? Through : 0f;
            if (through > 0f)
            {
                // in under it, into the dark of the bay: the step starts a soldier's width before the door and ends one
                // past its far edge (a shoulder's either side of the threshold), and they walk their run cycle as they go
                x = sill.x + Mathf.Lerp(DenRollerDoor.From, DenRollerDoor.To, through);
                y = DenRollerDoor.Sill.y;
                walking = true;
                legs = StageLegs.Walk;
            }
            f.Body.Pose(skin, new StagePose(legs, top, 0f, seated ? -CrewFigure.CrouchDrop : bob) { WalkT = m_Clock });
            Ui.Show(f.Rt, through < 1f);
            Color shade = Color.Lerp(Color.white, new Color(0.15f, 0.15f, 0.2f), through);
            f.Body.Tint(shade);
            float size = 1f - 0.2f * through;
            f.Rt.anchoredPosition = new Vector2(x + shiver, y) * Px;
            // through the door they face the way they're going: in is left (the bay's inside), out is right
            bool faceLeft = through > 0f ? through < 0.5f : a.Walk.FaceLeft && !seated && !climbing;
            f.Rt.localScale = new Vector3((faceLeft ? -1f : 1f) * size, size, 1f);
            bool cup = a.Soldier == Content.Tones2 && (a.Doing == DenDoing.Station && a.Cycle == 0 || a.Doing == DenDoing.Bit && Bits.Slushie && Bits.T < 3.4f);
            Ui.Show(f.Cup, cup);
            if (cup) f.Cup.rectTransform.anchoredPosition = new Vector2(5f, a.Doing == DenDoing.Station && a.CycleT > 0.6f && a.CycleT < 2.2f ? 8f : 1f) * Px;
        }

        /// <summary>
        /// A crewmate at their station runs short idle loops, then breathes for a bit (docs/design/lobby.md, Who lives where):
        /// tones2 slurps the slushie and gets brain freeze, or sprays the machine clean; kie tinkers with the projector, which
        /// flickers a tiny Trollogram of him, or a weasel pops out of the birdhouse.
        /// </summary>
        void Idle(DenActor a, out float bob, out float shiver)
        {
            bob = Fx.Step(m_Clock, 1.6f, 2, a.X) == 0 ? 0f : -0.5f;
            shiver = 0f;
            a.CycleT += Time.unscaledDeltaTime;
            if (a.CycleT >= a.CycleLen)
            {
                a.Cycle = DenIdle.Next(a.Cycle, m_Rng); // a breath between every two loops
                a.CycleLen = DenIdle.SecondsOf(a.Cycle, m_Rng);
                a.CycleT = 0f;
                if (a.Soldier == Content.Kie && a.Cycle == 0) Sfx.Play(Sfx.Glitch, 0.25f);
            }
            if (a.Soldier == Content.Tones2)
            {
                a.Walk.Face(a.Cycle == 1); // sprays the slushie machine, to his left
                if (a.Cycle == 0 && a.CycleT > 2.2f && a.CycleT < 3.4f) shiver = Fx.Shiver(m_Clock, 30f, 0.6f); // brain freeze
            }
            else if (a.Soldier == Content.Kie) a.Walk.Face(a.Cycle == 0); // the projector's on the bench behind him
        }

        /// <summary>The station props that move: tones2's spray, kie's tiny Trollogram and the weasel, and the bits' slushie.</summary>
        void Props()
        {
            DenActor tones2 = Find(Content.Tones2), kie = Find(Content.Kie);
            float ground = DenMap.FloorY(DenFloor.Ground);
            bool spraying = tones2 != null && tones2.Doing == DenDoing.Station && tones2.Cycle == 1 && tones2.CycleT > 0.5f && tones2.CycleT < 3.5f;
            for (int i = 0; i < m_Drops.Length; i++)
            {
                Ui.Show(m_Drops[i], spraying);
                if (!spraying) continue;
                float k = (m_Clock * 2.2f + i / (float)m_Drops.Length) % 1f;
                m_Drops[i].rectTransform.anchoredPosition = new Vector2(tones2.X - 4f - k * 14f, ground + 15f + Mathf.Sin(k * Mathf.PI) * 4f) * Px;
            }
            bool tinkering = kie != null && kie.Doing == DenDoing.Station && kie.Cycle == 0 && kie.CycleT > 0.8f && kie.CycleT < 3.6f;
            DenActor first = Bits.First;
            bool holoBit = Bits.Slushie && Bits.T > 2.6f && Bits.T < 3.6f;
            Ui.Show(m_Holo, tinkering || holoBit);
            if (tinkering) m_Holo.rectTransform.anchoredPosition = new Vector2(Holo.x + (Fx.Step(m_Clock, 14f, 5) == 0 ? 1f : 0f), Holo.y) * Px;
            if (holoBit && first != null) m_Holo.rectTransform.anchoredPosition = new Vector2(first.X + 10f, first.Y + 14f) * Px;
            m_Holo.color = new Color(0.45f, 1f, 1f, Fx.Step(m_Clock, 20f, 3) == 0 ? 0.35f : 0.75f);
            bool weasel = kie != null && kie.Doing == DenDoing.Station && kie.Cycle == 1;
            Ui.Show(m_Weasel, weasel);
            if (weasel) m_Weasel.rectTransform.anchoredPosition = new Vector2(WeaselHole.x, WeaselHole.y + Mathf.Max(0f, Mathf.Sin(kie.CycleT * 2.4f)) * 4f) * Px;
            bool splash = Bits.Slushie && Bits.T > 3.5f;
            Ui.Show(m_Splash, splash);
            if (splash && first != null) m_Splash.rectTransform.anchoredPosition = new Vector2(first.X + 9f, first.Y) * Px;
        }

        /// <summary>In the beat playing: everyone on the first visit (the room's still dark), the newcomer moving in.</summary>
        bool InBeat(DenActor a) => Beat != null && (Beat.Kind == DenBeatKind.FirstVisit || a.Soldier == Beat.Newcomer);

        /// <summary>
        /// <paramref name="beat"/>'s over: whoever was in it is where it left them (kie by the couch, tones2 by the TV, the
        /// newcomer just in from the door), and off home, unless they're yours.
        /// </summary>
        public void EndBeat(DenBeat beat)
        {
            Beat = null;
            if (beat.Kind == DenBeatKind.FirstVisit)
            {
                From(Find(Content.Kie), DenBeatView.HeaveX);
                From(Find(Content.Tones2), DenBeatView.PlugX);
            }
            else From(Find(beat.Newcomer), DenBeatView.NewcomerX(beat));
        }

        void From(DenActor a, float x)
        {
            if (a == null || a.Doing == DenDoing.Away || a.OnCouch) return;
            a.Walk.Place(DenFloor.Ground, x);
            if (a == Mine) return;
            a.Doing = DenDoing.Walking;
            a.Walk.WalkTo(a.Home.Floor, a.Home.Spot);
        }

        /// <summary>The crewmate playing <paramref name="s"/>, or null.</summary>
        public DenActor Find(SoldierDef s)
        {
            foreach (DenActor a in m_Actors)
                if (a.Soldier == s) return a;
            return null;
        }

        /// <summary>The freed crewmate whose figure is at <paramref name="p"/> (den art pixels), other than yours, or null.</summary>
        public DenActor At(Vector2 p)
        {
            foreach (DenActor a in m_Actors)
            {
                if (a == Mine || a.Doing == DenDoing.Away) continue;
                float x = a.OnCouch ? SeatX[a.Seat] : a.X, y = a.OnCouch ? SitY : a.Y;
                if (Mathf.Abs(p.x - x) <= 9f && p.y >= y - 2f && p.y <= y + 30f) return a;
            }
            return null;
        }
    }
}
