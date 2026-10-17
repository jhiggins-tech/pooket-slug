using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>What the den shows besides the <see cref="Den"/> itself.</summary>
    public struct DenInfo
    {
        /// <summary>The mission on the TV and the board (null in CREW, which has none).</summary>
        public string Mission;
        /// <summary>The co-op room's code for the TV, or null.</summary>
        public string Room;
        /// <summary>The room's invite link (tapping the TV shares it), or null.</summary>
        public string InviteLink;
        /// <summary>The host can list the room in the room browser or keep it invite only, and which it is.</summary>
        public bool CanList, Listed;
        /// <summary>People watching the room (the TV says so).</summary>
        public int Watchers;
        /// <summary>Tapping the empty second controller hosts a room (solo, once co-op's open).</summary>
        public bool CanHost;
        /// <summary>Our colours (<see cref="Art.SkinFor"/>): 0 player one's, 1 player two's.</summary>
        public int Skin;
    }

    /// <summary>
    /// The lobby: the crew's gaming den, drawn in uGUI (<c>docs/design/lobby.md</c>). The hangout from the opening, side-on in
    /// two floors, with a station for each crewmate: freed ones live at theirs and run little idle loops, and the rest stay
    /// wrecked and dark from the raid. Tapping a crewmate sits them on the couch with a controller (<see cref="Den"/>);
    /// READY presses START, and the camera zooms into the TV. Every so often two of the crew do a bit with speech bubbles
    /// (<see cref="DenBits"/>). The stage is <see cref="StageKit"/>'s, shared with the cutscenes: 320 x 180 art pixels at 2x, letterboxed on other shapes.
    /// </summary>
    public sealed class DenView
    {
        const int Loft = 100;
        const float SeatY = Floor + 12f, WalkSpeed = 46f;
        static readonly float[] SeatX = { 101f, 131f };
        static readonly Vector2 TvCentre = new Vector2(116f, 69f);

        static readonly Color Pad = new Color(0.42f, 0.31f, 0.69f);
        static readonly Color[] PlayerColours = { new Color(1f, 0.35f, 0.37f), new Color(0.31f, 0.66f, 1f) };

        /// <summary>A station: whose it is, which floor, where it spans, and where they stand at it.</summary>
        sealed class Station
        {
            public readonly string Crewmate;
            public readonly SoldierDef Soldier; // null for a crewmate the game doesn't have yet
            public readonly bool Upstairs;
            public readonly int X0, X1;
            public readonly float Spot;

            public Station(string crewmate, SoldierDef soldier, bool upstairs, int x0, int x1, float spot)
            {
                Crewmate = crewmate;
                Soldier = soldier;
                Upstairs = upstairs;
                X0 = x0;
                X1 = x1;
                Spot = spot;
            }
        }

        // docs/design/lobby.md, Who lives where (tones2's corner is the slushie machine and a beanbag: the couch is the players')
        static readonly Station[] Stations =
        {
            new Station("kie", Content.Kie, false, 4, 58, 40f),
            new Station("tones2", Content.Tones2, false, 156, 194, 184f),
            new Station("kiwicore", null, false, 198, 236, 216f),
            new Station("garyoldmancorp", null, false, 238, 262, 250f),
            new Station("kcaj", null, true, 4, 62, 34f),
            new Station("torikloud", null, true, 68, 126, 96f),
            new Station("ciarra", null, true, 132, 190, 160f),
            new Station("larinovsky", null, true, 196, 256, 226f),
        };

        /// <summary>A crewmate's figure (<see cref="StageFigure"/>), with a controller for the couch and tones2's slushie; sitting, the torso drops and the legs fold.</summary>
        sealed class Figure
        {
            public readonly StageFigure Body;
            public readonly Image Controller, Cup;
            public RectTransform Rt => Body.Rt;
            public Image Legs => Body.Legs;
            public Image Torso => Body.Torso;

            public Figure(RectTransform world, string name)
            {
                Body = new StageFigure(world, name);
                Controller = Ui.Picture(Rt, "Controller", null, Ui.Middle, Ui.Middle, new Vector2(5f, -6f) * Px, new Vector2(5f, 3f) * Px, Pad);
                Cup = Ui.Picture(Rt, "Slushie", null, Ui.Middle, Ui.Middle, new Vector2(6f, 0f) * Px, new Vector2(3f, 5f) * Px, new Color(0.55f, 0.35f, 0.95f));
            }

        }

        enum Doing { Station, Walking, Seated, Bit, Away }

        /// <summary>A crewmate in the den, and what they're up to.</summary>
        sealed class Actor
        {
            public SoldierDef Soldier;
            public Station Home;
            public Figure Fig;
            public float X, Y, TargetX, TargetY;
            public bool FaceLeft, Jumping;
            public Doing Doing;
            public int Seat = -1, Skin;
            public int Cycle;
            public float CycleT, CycleLen;
        }

        readonly RectTransform m_Root, m_Stage, m_World;
        readonly Image m_Back, m_Fade, m_Dim, m_Holo, m_Weasel, m_Splash;
        readonly Image[] m_Lights = new Image[2], m_LoosePads = new Image[2], m_Drops = new Image[6];
        readonly Text m_TvTitle, m_TvRoom, m_TvLights, m_TvCount, m_Board, m_Hint, m_Note;
        readonly Text[] m_Tags = new Text[2];
        readonly Button m_Ready, m_Back2, m_HostPad, m_KitButton, m_ListButton;
        readonly Text m_ListLabel;
        readonly Text m_ReadyLabel, m_CardName;
        readonly RectTransform m_Card;
        readonly List<Button> m_StationTaps = new List<Button>();
        readonly List<SpeechBubble> m_Bubbles = new List<SpeechBubble>();
        readonly List<Actor> m_Actors = new List<Actor>();
        readonly Figure m_Visitor;
        Actor m_VisitorActor;
        Sprite m_Backdrop;
        string m_BackdropKey;

        readonly System.Random m_Rng = new System.Random();
        DenBit m_Bit, m_LastBit;
        Actor m_BitA, m_BitB;
        float m_BitT, m_NextBit, m_Clock, m_NoteUntil;
        bool m_BitSaidHolo;

        SoldierDef m_Picked;
        bool m_ReadyPressed, m_BackPressed, m_KitPressed, m_HostPressed, m_BoardPressed, m_InviteDown, m_ListPressed;
        RectTransform m_InviteTap;
        string m_TvSaid;
        float m_TvSaidUntil;

        public DenView(Transform parent)
        {
            m_Root = Ui.Canvas(parent, "Den", 8); // under the menus (10), so the ⓘ kit screen and notices show over it
            m_Root.gameObject.AddComponent<GraphicRaycaster>();
            Build(m_Root, out m_Stage, out m_World);
            m_Back = BackdropImage(m_World);

            // the TV: the console's start screen, the room, both controllers' lights, the countdown
            m_TvTitle = WorldLabel("TV title", 89f, 82f, 10, TextAnchor.UpperLeft, Gold);
            m_TvRoom = WorldLabel("TV room", 89f, 74f, 7, TextAnchor.UpperLeft, new Color(0.67f, 0.86f, 1f));
            m_TvLights = WorldLabel("TV lights", 89f, 60f, 8, TextAnchor.LowerLeft, Color.white);
            m_TvCount = WorldLabel("TV countdown", TvCentre.x, TvCentre.y, 30, TextAnchor.MiddleCenter, Gold);
            m_Board = WorldLabel("Mission board", 250f, 70f, 6, TextAnchor.MiddleCenter, Ink);
            m_Board.GetComponent<Shadow>().enabled = false;
            WorldLabel("POOKET sign", 299f, 97f, 9, TextAnchor.MiddleCenter, Gold).text = "POOKET";
            for (int i = 0; i < 2; i++)
            {
                m_Lights[i] = Picture("Port light", 66f + i * 8f, Floor + 12f, 2f, 2f, Color.black);
                m_LoosePads[i] = Picture("Controller", SeatX[i], SeatY, 6f, 3f, Pad);
                m_Tags[i] = WorldLabel("Tag", SeatX[i], Floor + 3f, 9, TextAnchor.LowerCenter, PlayerColours[i]);
                m_Tags[i].text = "P" + (i + 1);
            }
            for (int i = 0; i < m_Drops.Length; i++) m_Drops[i] = Picture("Water", 0f, 0f, 3f, 2f, new Color(0.45f, 0.75f, 1f));
            m_Holo = Picture("Tiny Trollogram", 22f, Floor + 26f, 6f, 9f, new Color(0.45f, 1f, 1f, 0.7f));
            m_Weasel = Picture("Weasel", 14f, Floor + 6f, 5f, 4f, new Color(0.72f, 0.5f, 0.3f));
            m_Splash = Picture("Splash", 0f, Floor, 10f, 2f, new Color(0.55f, 0.35f, 0.95f));

            // the stations: a tap on a freed crewmate's corner sits them on the couch
            foreach (Station st in Stations)
            {
                if (st.Soldier == null) continue;
                Station s = st;
                float y0 = s.Upstairs ? Loft : Floor;
                RectTransform rt = Ui.Rect(m_World, "Tap " + s.Crewmate, Vector2.zero, Vector2.zero, new Vector2(s.X0, y0) * Px, new Vector2(s.X1 - s.X0, 46f) * Px);
                var img = rt.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 1f, 1f, 0f);
                var b = rt.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.navigation = new Navigation { mode = Navigation.Mode.None };
                b.onClick.AddListener(() => m_Picked = s.Soldier);
                m_StationTaps.Add(b);
            }

            // the mission board, over the stations: a tap opens the mission screen
            RectTransform board = Ui.Rect(m_World, "Tap mission board", Vector2.zero, Vector2.zero, new Vector2(234f, 54f) * Px, new Vector2(32f, 30f) * Px);
            board.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var boardTap = board.gameObject.AddComponent<Button>();
            boardTap.transition = Selectable.Transition.None;
            boardTap.navigation = new Navigation { mode = Navigation.Mode.None };
            boardTap.onClick.AddListener(() => m_BoardPressed = true);

            // the TV, in a co-op room: a tap shares the invite link. It's armed as the finger goes down (Invite)
            m_InviteTap = Ui.Rect(m_World, "Tap TV", Vector2.zero, Vector2.zero, new Vector2(86f, 54f) * Px, new Vector2(62f, 34f) * Px);
            m_InviteTap.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            var down = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
            down.callback.AddListener(_ => m_InviteDown = true);
            m_InviteTap.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>().triggers.Add(down);

            m_Visitor = new Figure(m_World, "Visitor");
            m_Dim = Ui.Picture(m_Stage, "Fade", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            Stretch(m_Dim.rectTransform);
            m_Fade = m_Dim;

            // over the room: BACK top left, READY bottom right, the name card bottom left, the empty seat's invite
            Vector2 half = new Vector2(StageW, StageH) * Px / 2f;
            m_Back2 = Ui.Button(m_Stage, "BACK", new Vector2(-half.x + 40f, half.y - 18f), new Vector2(64f, 22f), () => m_BackPressed = true, out Text backKey);
            backKey.text = "ESC";
            // the host's switch: the room in the room browser for anyone, or invite only
            m_ListButton = Ui.Button(m_Stage, "INVITE ONLY", new Vector2(-half.x + 146f, half.y - 18f), new Vector2(132f, 22f), () => m_ListPressed = true, out Text listKey);
            m_ListLabel = m_ListButton.GetComponentInChildren<Text>();
            m_ListLabel.fontSize = 10;
            listKey.text = "";
            m_Ready = Ui.Button(m_Stage, "READY", new Vector2(half.x - 56f, -half.y + 32f), new Vector2(96f, 30f), () => m_ReadyPressed = true, out Text readyKey);
            readyKey.text = "ENTER";
            m_ReadyLabel = m_Ready.GetComponentInChildren<Text>();
            m_ReadyLabel.fontSize = 16;
            m_Card = Ui.Rect(m_Stage, "Name card", Ui.Middle, Ui.Middle, new Vector2(-half.x + 70f, -half.y + 30f), new Vector2(124f, 28f));
            var card = m_Card.gameObject.AddComponent<Image>();
            card.color = Band;
            card.raycastTarget = false;
            m_CardName = Ui.Label(m_Card, "Name", new Vector2(0f, 0.5f), new Vector2(10f, 0f), 14, TextAnchor.MiddleLeft, Gold);
            m_KitButton = Ui.Button(m_Card, "i", new Vector2(46f, 0f), new Vector2(22f, 22f), () => m_KitPressed = true, out Text kitKey);
            m_KitButton.GetComponent<Image>().sprite = Ui.Disc;
            m_KitButton.GetComponentInChildren<Text>().fontStyle = FontStyle.BoldAndItalic;
            kitKey.text = "B";
            Vector2 p2 = new Vector2(SeatX[1] - StageW / 2f, SeatY + 9f - StageH / 2f) * Px;
            m_HostPad = Ui.Button(m_Stage, "+ MATE", p2, new Vector2(52f, 16f), () => m_HostPressed = true, out Text hostKey);
            m_HostPad.GetComponentInChildren<Text>().fontSize = 9;
            hostKey.text = "H";
            m_Hint = Ui.Label(m_Stage, "Hint", Ui.TopCenter, new Vector2(0f, -8f), 10, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.75f));
            m_Note = Ui.Label(m_Stage, "Note", Ui.TopCenter, new Vector2(0f, -24f), 11, TextAnchor.UpperCenter, new Color(1f, 0.55f, 0.45f));

            Ui.Show(m_Root, false);
        }

        Image Picture(string name, float x, float y, float w, float h, Color color) => StageKit.Picture(m_World, name, null, x, y, w, h, color);

        Text WorldLabel(string name, float x, float y, int size, TextAnchor align, Color color) => Label(m_World, name, x, y, size, align, color);

        public bool Showing => m_Root.gameObject.activeSelf;

        /// <summary>Opens the den on <paramref name="den"/>: each freed crewmate at their station, or already on the couch.</summary>
        public void Begin(Den den, DenInfo info)
        {
            string key = string.Join(",", System.Array.ConvertAll(ToArray(den.Crew), s => s.Name));
            if (m_BackdropKey != key)
            {
                m_BackdropKey = key;
                m_Backdrop = Backdrop(c => Draw(c, den.Crew));
                m_Back.sprite = m_Backdrop;
            }
            foreach (Actor a in m_Actors) Object.Destroy(a.Fig.Rt.gameObject);
            m_Actors.Clear();
            foreach (SoldierDef s in den.Crew)
            {
                Station home = null;
                foreach (Station st in Stations)
                    if (st.Soldier == s) home = st;
                if (home == null) continue;
                var a = new Actor { Soldier = s, Home = home, Fig = new Figure(m_World, s.Name), X = home.Spot, Y = home.Upstairs ? Loft : Floor };
                a.TargetX = a.X;
                a.TargetY = a.Y;
                a.FaceLeft = a.X > 160f;
                a.Fig.Rt.SetSiblingIndex(m_Back.transform.GetSiblingIndex() + 1);
                m_Actors.Add(a);
                if (den.Pick == s) Sit(a, den.Seat, info.Skin, instant: true);
            }
            m_VisitorActor = null;
            Ui.Show(m_Visitor.Rt, false);
            m_Bit = null;
            m_NextBit = Mathf.Lerp(DenBits.MinGap, DenBits.MaxGap, (float)m_Rng.NextDouble()) * 0.5f; // the first comes sooner
            m_Clock = 0f;
            m_Picked = null;
            m_ReadyPressed = m_BackPressed = m_KitPressed = m_HostPressed = m_BoardPressed = m_InviteDown = m_ListPressed = false;
            m_World.localScale = Vector3.one;
            m_World.anchoredPosition = Vector2.zero;
            Ui.Show(m_Root, true);
        }

        static SoldierDef[] ToArray(IReadOnlyList<SoldierDef> list)
        {
            var a = new SoldierDef[list.Count];
            for (int i = 0; i < a.Length; i++) a[i] = list[i];
            return a;
        }

        public void Hide()
        {
            m_Bit = null;
            Ui.Show(m_Root, false);
        }

        /// <summary>The crewmate tapped since the last call, or null.</summary>
        public SoldierDef TakePick()
        {
            SoldierDef s = m_Picked;
            m_Picked = null;
            return s;
        }

        public bool TakeReady() => Take(ref m_ReadyPressed);
        public bool TakeBack() => Take(ref m_BackPressed);
        public bool TakeKit() => Take(ref m_KitPressed);
        public bool TakeHost() => Take(ref m_HostPressed);
        /// <summary>The mission board was tapped: it opens the mission screen.</summary>
        public bool TakeBoard() => Take(ref m_BoardPressed);
        /// <summary>A finger went down on the TV in a co-op room: arm the invite (<see cref="Invite"/>).</summary>
        public bool TakeInvite() => Take(ref m_InviteDown);
        /// <summary>The host tapped the listing switch.</summary>
        public bool TakeList() => Take(ref m_ListPressed);

        /// <summary>The TV says this under the room code for a few seconds ("LINK COPIED").</summary>
        public void TvSay(string text)
        {
            m_TvSaid = text;
            m_TvSaidUntil = m_Clock + 2.5f;
        }

        static bool Take(ref bool flag)
        {
            bool f = flag;
            flag = false;
            return f;
        }

        /// <summary>Says something across the top for a few seconds (a pick undone, say).</summary>
        public void Note(string text, float seconds = 3f)
        {
            m_Note.text = text;
            m_NoteUntil = m_Clock + seconds;
        }

        // ------------------------------------------------------------------ each frame

        public void Refresh(float dt, Den den, DenInfo info)
        {
            m_Clock += dt;
            DenPartner partner = den.Partner;
            int partnerSeat = 1 - den.Seat;

            // who goes where: our pick to our seat, the partner's to theirs (a partner in the mission isn't in the den)
            foreach (Actor a in m_Actors)
            {
                if (a.Soldier == den.Pick) Sit(a, den.Seat, info.Skin, instant: false);
                else if (den.Mode == DenMode.Coop && partner.Soldier == a.Soldier && partner.InDenNow && partner.Seated) Sit(a, partnerSeat, 1 - info.Skin, instant: false);
                else if (den.Mode == DenMode.Coop && partner.Soldier == a.Soldier && partner.Playing) a.Doing = Doing.Away;
                else if (a.Seat >= 0 || a.Doing == Doing.Away) GoHome(a);
            }
            Visitor(den, partner, partnerSeat, 1 - info.Skin);

            Bits(dt, den);
            foreach (Actor a in m_Actors) Move(a, dt);
            foreach (Actor a in m_Actors) Draw(a, den, info);
            if (m_VisitorActor != null)
            {
                Move(m_VisitorActor, dt);
                Draw(m_VisitorActor, den, info);
            }
            Props();
            Tv(den, info);
            Buttons(den, info);
            Zoom(den);
        }

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
                m_VisitorActor = new Actor { Soldier = partner.Soldier, Fig = m_Visitor, X = 298f, Y = Floor, FaceLeft = true };
                Ui.Show(m_Visitor.Rt, true);
            }
            Sit(m_VisitorActor, seat, skin, instant: false);
        }

        static bool Has(IReadOnlyList<SoldierDef> crew, SoldierDef s)
        {
            foreach (SoldierDef x in crew)
                if (x == s) return true;
            return false;
        }

        void Sit(Actor a, int seat, int skin, bool instant)
        {
            a.Skin = skin;
            if (a.Seat == seat && (a.Doing == Doing.Seated || a.Doing == Doing.Walking)) return;
            if (m_Bit != null && (a == m_BitA || a == m_BitB)) EndBit();
            a.Seat = seat;
            a.TargetX = SeatX[seat];
            a.TargetY = Floor;
            if (instant)
            {
                a.X = a.TargetX;
                a.Y = a.TargetY;
                a.Doing = Doing.Seated;
            }
            else
            {
                a.Doing = Doing.Walking;
                a.Jumping = a.Y > Floor + 1f; // loft crew vault the railing and drop down
                Sfx.Play(Sfx.Pickup, 0.4f);
            }
        }

        void GoHome(Actor a)
        {
            if (a.Doing == Doing.Bit) return;
            a.Seat = -1;
            a.Skin = 0;
            a.TargetX = a.Home.Spot;
            a.TargetY = a.Home.Upstairs ? Loft : Floor;
            if (a.Doing == Doing.Away)
            {
                a.X = a.TargetX; // back from the mission: they're just there
                a.Y = a.TargetY;
                a.Doing = Doing.Station;
                return;
            }
            if (a.Doing == Doing.Seated) Sfx.Play(Sfx.Thump, 0.3f); // handed the controller over with a shrug
            a.Doing = Doing.Walking;
        }

        void Move(Actor a, float dt)
        {
            if (a.Doing != Doing.Walking && a.Doing != Doing.Bit) return;
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
            if (a.Doing == Doing.Walking && Mathf.Abs(a.X - a.TargetX) < 0.5f && Mathf.Abs(a.Y - a.TargetY) < 0.5f)
            {
                a.Doing = a.Seat >= 0 ? Doing.Seated : Doing.Station;
                if (a.Seat >= 0) Sfx.Play(Sfx.Thump, 0.5f); // flops onto the couch
                a.FaceLeft = a.Seat < 0 && a.X > 160f;
                a.CycleT = a.CycleLen = 0f;
            }
        }

        void Draw(Actor a, Den den, DenInfo info)
        {
            Figure f = a.Fig;
            bool away = a.Doing == Doing.Away;
            Ui.Show(f.Rt, !away);
            if (away) return;
            PlayerSkin skin = Art.SkinFor(a.Soldier, a.Skin);
            bool seated = a.Doing == Doing.Seated;
            bool walking = (a.Doing == Doing.Walking || a.Doing == Doing.Bit) && Mathf.Abs(a.X - a.TargetX) > 0.5f;
            f.Legs.sprite = seated ? skin.LegsCrouch : a.Jumping ? skin.LegsJump : walking ? skin.LegsRun[(int)(m_Clock * 10f) % skin.LegsRun.Length] : skin.LegsIdle;
            f.Torso.sprite = skin.TorsoForward;
            float bob = 0f, shiver = 0f;
            if (a.Doing == Doing.Station) Idle(a, out bob, out shiver);
            f.Torso.rectTransform.anchoredPosition = new Vector2(0f, (seated ? -6f : bob) * Px);
            float y = seated ? SeatY - 5f : a.Y;
            f.Rt.anchoredPosition = new Vector2(a.X + shiver, y) * Px;
            f.Rt.localScale = new Vector3(a.FaceLeft && !seated ? -1f : 1f, 1f, 1f);
            Ui.Show(f.Controller, seated);
            if (seated) f.Controller.rectTransform.anchoredPosition = new Vector2(5f, -7f + (den.Ready && a.Seat == den.Seat ? 0f : Mathf.Sin(m_Clock * 6f + a.X) * 0.5f)) * Px;
            bool cup = a.Soldier == Content.Tones2 && (a.Doing == Doing.Station && a.Cycle == 0 || a.Doing == Doing.Bit && m_Bit != null && m_Bit.Id == "slushie" && m_BitT < 3.4f);
            Ui.Show(f.Cup, cup);
            if (cup) f.Cup.rectTransform.anchoredPosition = new Vector2(5f, a.Doing == Doing.Station && a.CycleT > 0.6f && a.CycleT < 2.2f ? 8f : 1f) * Px;
        }

        /// <summary>
        /// A crewmate at their station runs short idle loops, then breathes for a bit (docs/design/lobby.md, Who lives where):
        /// tones2 slurps the slushie and gets brain freeze, or sprays the machine clean; kie tinkers with the projector, which
        /// flickers a tiny Trollogram of him, or a weasel pops out of the hutch.
        /// </summary>
        void Idle(Actor a, out float bob, out float shiver)
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
            Actor tones2 = Find(Content.Tones2), kie = Find(Content.Kie);
            bool spraying = tones2 != null && tones2.Doing == Doing.Station && tones2.Cycle == 1 && tones2.CycleT > 0.5f && tones2.CycleT < 3.5f;
            for (int i = 0; i < m_Drops.Length; i++)
            {
                Ui.Show(m_Drops[i], spraying);
                if (!spraying) continue;
                float k = (m_Clock * 2.2f + i / (float)m_Drops.Length) % 1f;
                m_Drops[i].rectTransform.anchoredPosition = new Vector2(tones2.X - 4f - k * 14f, Floor + 15f + Mathf.Sin(k * Mathf.PI) * 4f) * Px;
            }
            bool tinkering = kie != null && kie.Doing == Doing.Station && kie.Cycle == 0 && kie.CycleT > 0.8f && kie.CycleT < 3.6f;
            bool holoBit = m_Bit != null && m_Bit.Id == "slushie" && m_BitT > 2.6f && m_BitT < 3.6f;
            Ui.Show(m_Holo, tinkering || holoBit);
            if (tinkering) m_Holo.rectTransform.anchoredPosition = new Vector2(22f + ((int)(m_Clock * 14f) % 5 == 0 ? 1f : 0f), Floor + 26f) * Px;
            if (holoBit && m_BitA != null) m_Holo.rectTransform.anchoredPosition = new Vector2(m_BitA.X + 10f, Floor + 14f) * Px;
            m_Holo.color = new Color(0.45f, 1f, 1f, (int)(m_Clock * 20f) % 3 == 0 ? 0.35f : 0.75f);
            bool weasel = kie != null && kie.Doing == Doing.Station && kie.Cycle == 1;
            Ui.Show(m_Weasel, weasel);
            if (weasel) m_Weasel.rectTransform.anchoredPosition = new Vector2(14f, Floor + 5f + Mathf.Max(0f, Mathf.Sin(kie.CycleT * 2.4f)) * 7f) * Px;
            bool splash = m_Bit != null && m_Bit.Id == "slushie" && m_BitT > 3.5f;
            Ui.Show(m_Splash, splash);
            if (splash && m_BitA != null) m_Splash.rectTransform.anchoredPosition = new Vector2(m_BitA.X + 9f, Floor) * Px;
        }

        Actor Find(SoldierDef s)
        {
            foreach (Actor a in m_Actors)
                if (a.Soldier == s) return a;
            return null;
        }

        // ------------------------------------------------------------------ the bits

        void Bits(float dt, Den den)
        {
            if (m_Bit == null)
            {
                HideBubbles(0);
                if (den.Zoom >= 0f) return;
                m_NextBit -= dt;
                if (m_NextBit > 0f) return;
                m_NextBit = Mathf.Lerp(DenBits.MinGap, DenBits.MaxGap, (float)m_Rng.NextDouble());
                var idle = new List<SoldierDef>();
                var seated = new List<SoldierDef>();
                foreach (Actor a in m_Actors)
                {
                    if (a.Doing == Doing.Station) idle.Add(a.Soldier);
                    else if (a.Doing == Doing.Seated) seated.Add(a.Soldier);
                }
                DenBit bit = DenBits.Choose(idle, seated, m_LastBit, m_Rng);
                if (bit == null) return;
                m_Bit = m_LastBit = bit;
                m_BitA = Find(bit.First);
                m_BitB = Find(bit.Second);
                m_BitT = 0f;
                m_BitSaidHolo = false;
                m_BitA.Doing = Doing.Bit;
                // walk over to the other one: beside them at their station, or to the couch's arm
                float to = bit.OnCouch ? (m_BitB.Seat == 0 ? 78f : 154f) : m_BitB.X + (m_BitB.X > m_BitA.X ? -16f : 16f);
                m_BitA.TargetX = to;
                m_BitA.TargetY = Floor;
                return;
            }
            m_BitT += dt;
            if (m_Bit.Id == "slushie" && !m_BitSaidHolo && m_BitT > 2.6f)
            {
                m_BitSaidHolo = true;
                Sfx.Play(Sfx.Glitch, 0.4f);
            }
            if (m_Bit.Id == "slushie" && m_BitT > 3.5f && m_BitT - dt <= 3.5f) Sfx.Play(Sfx.Splat, 0.5f);
            // the walk there doesn't count against the lines: they start once they've arrived
            if (Mathf.Abs(m_BitA.X - m_BitA.TargetX) > 0.5f && m_BitT < 1f) m_BitT = Mathf.Min(m_BitT, 0.5f);
            else if (m_BitB.X != 0f) m_BitA.FaceLeft = m_BitB.X < m_BitA.X;
            int shown = 0;
            foreach (DenLine line in m_Bit.Lines)
            {
                if (m_BitT < line.At || m_BitT > line.At + 2.2f) continue;
                Bubble(shown++, line.Text, line.Who == 0 ? m_BitA : m_BitB);
            }
            HideBubbles(shown);
            bool partnerGone = m_BitB.Doing == Doing.Away || (!m_Bit.OnCouch && m_BitB.Doing != Doing.Station) || (m_Bit.OnCouch && m_BitB.Doing != Doing.Seated);
            if (m_BitT >= m_Bit.Seconds || partnerGone || den.Zoom >= 0f) EndBit();
        }

        void EndBit()
        {
            Actor a = m_BitA;
            m_Bit = null;
            m_BitA = m_BitB = null;
            HideBubbles(0);
            if (a != null && a.Doing == Doing.Bit)
            {
                a.Doing = Doing.Walking;
                a.TargetX = a.Home.Spot;
                a.TargetY = a.Home.Upstairs ? Loft : Floor;
            }
        }

        void Bubble(int i, string text, Actor who)
        {
            while (m_Bubbles.Count <= i) m_Bubbles.Add(new SpeechBubble(m_World, Vector2.zero));
            SpeechBubble b = m_Bubbles[i];
            b.Rt.SetAsLastSibling();
            float w = b.Say(text);
            Vector2 head = new Vector2(who.X, (who.Doing == Doing.Seated ? SeatY - 5f : who.Y) + 30f) * Px;
            Vector2 at = head + new Vector2(0f, 16f);
            float half = w / 2f + 4f;
            at.x = Mathf.Clamp(at.x, half, StageW * Px - half);
            b.Place(at, head.x);
        }

        void HideBubbles(int from)
        {
            for (int i = from; i < m_Bubbles.Count; i++) m_Bubbles[i].Hide();
        }

        // ------------------------------------------------------------------ the TV, the console and the buttons

        void Tv(Den den, DenInfo info)
        {
            bool counting = den.Countdown > 0f;
            m_TvTitle.text = info.Mission ?? "CREW";
            string invite = m_Clock < m_TvSaidUntil ? m_TvSaid : "TAP TO INVITE";
            m_TvRoom.text = info.Room != null ? "ROOM " + info.Room + (info.InviteLink != null ? "  " + invite : "")
                : den.Mode == DenMode.Crew ? "PICK WHO YOU'LL PLAY" : "SOLO";
            Ui.Show(m_InviteTap, info.InviteLink != null && den.Zoom < 0f);
            Ui.Show(m_TvCount, counting);
            if (counting) m_TvCount.text = Mathf.CeilToInt(den.Countdown).ToString();
            m_Board.text = info.Mission ?? "";
            bool partnerSeat = den.Mode == DenMode.Coop && den.Partner.InDenNow && den.Partner.Seated;
            for (int seat = 0; seat < 2; seat++)
            {
                bool mine = seat == den.Seat, filled = mine ? den.Pick != null : partnerSeat || VisitorOn(seat);
                bool ready = mine ? den.Ready : partnerSeat && den.Partner.Ready;
                m_Lights[seat].color = ready ? new Color(0.3f, 1f, 0.45f) : filled ? new Color(1f, 0.8f, 0.3f) : new Color(0.2f, 0.18f, 0.25f);
                m_LoosePads[seat].enabled = !filled;
                Ui.Show(m_Tags[seat], filled && (den.Mode == DenMode.Coop || seat == 0));
                m_Tags[seat].color = PlayerColours[mine ? info.Skin : 1 - info.Skin];
            }
            string l1 = den.Ready ? "READY" : den.Pick != null ? "IN" : "--"; // the console font has no dots
            string l2 = partnerSeat ? (den.Partner.Ready ? "READY" : "IN") : "--";
            string mineTag = "P" + (den.Seat + 1), theirTag = "P" + (2 - den.Seat);
            string start = StartLine(den) + (info.Watchers > 0 ? "  " + info.Watchers + " WATCHING" : "");
            m_TvLights.text = den.Mode == DenMode.Crew ? "" : den.Seat == 0 ? $"{mineTag} {l1}   {theirTag} {l2}\n{start}" : $"{theirTag} {l2}   {mineTag} {l1}\n{start}";
        }

        bool VisitorOn(int seat) => m_VisitorActor != null && m_VisitorActor.Seat == seat;

        static string StartLine(Den den)
        {
            if (den.Zoom >= 0f) return "GO!";
            if (den.Countdown > 0f) return "GET READY";
            if (den.Ready) return den.Mode == DenMode.Coop && den.Partner.InDenNow ? "WAITING FOR P" + (2 - den.Seat) : "GO!";
            return "PRESS START";
        }

        void Buttons(Den den, DenInfo info)
        {
            bool zooming = den.Zoom >= 0f;
            Ui.Show(m_Ready, den.Mode != DenMode.Crew && !zooming);
            m_ReadyLabel.text = den.Ready ? "UNREADY" : "READY";
            Ui.Show(m_Back2, !zooming);
            Ui.Show(m_Card, den.Pick != null && !zooming);
            if (den.Pick != null) m_CardName.text = den.Pick.Name.ToUpperInvariant();
            Ui.Show(m_HostPad, info.CanHost && den.Mode != DenMode.Coop && !zooming);
            Ui.Show(m_ListButton, info.CanList && !zooming);
            if (info.CanList) m_ListLabel.text = info.Listed ? "PUBLIC ROOM" : "INVITE ONLY";
            foreach (Button b in m_StationTaps) b.interactable = !zooming;
            Ui.Show(m_Hint, !zooming);
            m_Hint.text = den.Pick == null ? "TAP SOMEONE TO SIT THEM ON THE COUCH"
                : den.Mode == DenMode.Crew ? "TAP SOMEONE ELSE TO SWAP"
                : den.Ready ? "" : "TAP SOMEONE ELSE TO SWAP · READY TO PRESS START";
            Ui.Show(m_Note, m_Clock < m_NoteUntil);
        }

        /// <summary>The camera zooms into the TV and everything goes dark, into the mission.</summary>
        void Zoom(Den den)
        {
            if (den.Zoom < 0f)
            {
                m_World.localScale = Vector3.one;
                m_World.anchoredPosition = Vector2.zero;
                m_Fade.color = new Color(0f, 0f, 0f, 0f);
                return;
            }
            float k = Mathf.Clamp01(den.Zoom / Den.ZoomSeconds);
            float ease = k * k * (3f - 2f * k);
            float scale = Mathf.Lerp(1f, 6f, ease);
            Vector2 off = (TvCentre - new Vector2(StageW, StageH) / 2f) * Px;
            m_World.localScale = new Vector3(scale, scale, 1f);
            m_World.anchoredPosition = -off * scale;
            m_Fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01((k - 0.55f) / 0.4f));
        }

        // ------------------------------------------------------------------ the painted room

        /// <summary>The den, side-on: the loft up a ladder, the couch, TV and console in the middle, the roller door on the right.</summary>
        static void Draw(PixelCanvas c, IReadOnlyList<SoldierDef> freed)
        {
            // warm plank walls, with the raid's cyan scorch marks painted over badly
            for (int y = 0; y < StageH; y++) c.Rect(0, y, StageW, 1, (y / 6) % 2 == 0 ? Art.Hex(0x4a3330) : Art.Hex(0x3e2a28));
            for (int x = 0; x < StageW; x += 23) c.Rect(x, 0, 1, StageH, Art.Hex(0x33221f));
            var rng = new System.Random(5);
            for (int i = 0; i < 4; i++)
            {
                int sx = 30 + rng.Next(200), sy = 40 + rng.Next(110);
                for (int k = 0; k < 18; k++) c.Set(sx + rng.Next(-7, 8), sy + rng.Next(-4, 5), Art.Hex(0x3a5a60, 150));
                for (int k = 0; k < 30; k++) c.Set(sx + rng.Next(-6, 7), sy + rng.Next(-3, 4), Art.Hex(0x5a4a46));
            }
            // string lights under the loft and along the ceiling
            for (int row = 0; row < 2; row++)
            {
                int y0 = row == 0 ? StageH - 6 : Loft - 9;
                for (int x = 4; x < 262; x += 2) c.Set(x, y0 - (int)(2f * Mathf.Abs((x % 40) - 20) / 20f), Art.Hex(0x2a1c1a));
                uint[] bulbs = { 0xffd166, 0xff7eb6, 0x7cf7d4, 0xff8c42 };
                for (int i = 0, x = 8; x < 260; x += 14, i++) c.Circle(x, y0 - 1 - (int)(2f * Mathf.Abs((x % 40) - 20) / 20f), 1.3f, Art.Hex(bulbs[i % 4]));
            }

            // the floor
            c.Rect(0, 0, StageW, Floor, Art.Hex(0x3a3436));
            c.Rect(0, Floor - 1, StageW, 1, Art.Hex(0x5a5256));
            for (int x = 0; x < StageW; x += 16) c.Rect(x, 0, 1, Floor - 1, Art.Hex(0x302a2c));
            // the loft: its floor, railing, and the ladder up
            c.Rect(0, Loft - 6, 264, 6, Art.Hex(0x6b4a38));
            c.Rect(0, Loft - 1, 264, 1, Art.Hex(0x8a6448));
            c.Rect(0, Loft - 8, 264, 2, Art.Hex(0x2a1c1a));
            for (int x = 6; x < 264; x += 40) c.Rect(x, Loft - 14, 3, 6, Art.Hex(0x2a1c1a));
            c.Rect(0, Loft + 13, 264, 1, Art.Hex(0x6b4a38));
            for (int x = 0; x < 264; x += 12) c.Rect(x, Loft, 1, 14, Art.Hex(0x6b4a38));
            c.Rect(266, Floor, 2, Loft - Floor + 2, Art.Hex(0x8a6448));
            c.Rect(274, Floor, 2, Loft - Floor + 2, Art.Hex(0x8a6448));
            for (int y = Floor + 6; y < Loft; y += 8) c.Rect(266, y, 10, 1, Art.Hex(0xa07a58));

            // the roller door, the POOKET sign and a window on the night
            c.Rect(278, Floor, 42, 64, Art.Hex(0x1c1a22));
            for (int y = Floor + 2; y < Floor + 62; y += 4)
            {
                c.Rect(280, y, 38, 1, Art.Hex(0x5e6474));
                c.Rect(280, y + 1, 38, 1, Art.Hex(0x7a8090));
            }
            c.Rect(276, Floor + 62, 44, 4, Art.Hex(0x2a2a30));
            c.Rect(280, 90, 38, 14, Art.Hex(0x2a1c1a));
            c.Rect(281, 91, 36, 12, Art.Hex(0xc0503c));
            c.Rect(284, 112, 32, 36, Art.Hex(0x2a1c1a));
            c.Rect(286, 114, 28, 32, Art.Hex(0x0e1630));
            for (int i = 0; i < 7; i++) c.Set(288 + i * 4, 120 + (i * 7) % 22, Art.Hex(0xffffff));
            for (int y = 114; y < 146; y++) c.Set(292 + (y - 114) / 4, y, Art.Hex(0x9ab0ff, 90));
            c.Rect(299, 114, 1, 32, Art.Hex(0x2a1c1a));
            c.Rect(286, 129, 28, 1, Art.Hex(0x2a1c1a));

            // the mission board, a corkboard with the mission's card (its words are drawn over it)
            c.Rect(238, 58, 24, 22, Art.Hex(0x2a1c1a));
            c.Rect(239, 59, 22, 20, Art.Hex(0xb08850));
            c.Rect(241, 62, 18, 14, Art.Hex(0xe8e0d0));
            c.Circle(250, 76, 1.2f, Art.Hex(0xd04030));

            // the big TV, the cube console on its side table, and the controllers' cables to the couch
            c.Rect(84, 52, 64, 36, Art.Hex(0x1c1a22));
            c.Rect(86, 54, 60, 32, Art.Hex(0x1a3a5a));
            c.Rect(86, 54, 60, 10, Art.Hex(0x2a2050));
            c.Rect(114, 44, 4, 8, Art.Hex(0x2a2a30));
            c.Rect(60, Floor, 18, 10, Art.Hex(0x5a3e34));
            c.Rect(60, Floor + 9, 18, 1, Art.Hex(0x7a5a44));
            c.Rect(62, Floor + 10, 14, 12, Art.Hex(0x4a3a8a));
            c.Rect(62, Floor + 21, 14, 1, Art.Hex(0x6a5ab0));
            c.Rect(65, Floor + 22, 8, 3, Art.Hex(0x2a2050));
            c.Rect(66, Floor + 23, 6, 1, Art.Hex(0x4a3a8a)); // the handle
            c.Circle(69, Floor + 17, 1.8f, Art.Hex(0x8f74d8));
            Cable(c, 66, Floor + 11, (int)SeatX[0], (int)SeatY);
            Cable(c, 74, Floor + 11, (int)SeatX[1], (int)SeatY);
            // the couch, facing us: two seats
            c.Rect(84, Floor, 64, 12, Art.Hex(0x8a3a5a));
            c.Rect(84, Floor + 11, 64, 1, Art.Hex(0xa8507a));
            c.Rect(86, Floor + 12, 60, 12, Art.Hex(0x7a3050));
            c.Rect(86, Floor + 23, 60, 1, Art.Hex(0x9a4a6e));
            c.Rect(115, Floor + 12, 2, 11, Art.Hex(0x6a2846));
            c.Rect(80, Floor, 6, 18, Art.Hex(0x8a3a5a));
            c.Rect(80, Floor + 17, 6, 1, Art.Hex(0xa8507a));
            c.Rect(146, Floor, 6, 18, Art.Hex(0x8a3a5a));
            c.Rect(146, Floor + 17, 6, 1, Art.Hex(0xa8507a));

            // the stations: a freed crewmate's is lit and lived in; the rest stay wrecked from the raid, dark, with no name
            foreach (Station s in Stations)
            {
                bool lit = s.Soldier != null && Has(freed, s.Soldier);
                if (lit) DrawStation(c, s);
                else Wrecked(c, s);
            }
        }

        static void Cable(PixelCanvas c, int x0, int y0, int x1, int y1)
        {
            for (int i = 0; i <= 40; i++)
            {
                float t = i / 40f;
                c.Set((int)Mathf.Lerp(x0, x1, t), (int)(Mathf.Lerp(y0, y1, t) - 5f * Mathf.Sin(t * Mathf.PI)), Art.Hex(0x2a2050));
            }
        }

        static void DrawStation(PixelCanvas c, Station s)
        {
            switch (s.Crewmate)
            {
                case "kie": // a workbench with the Trollogram projector half taken apart, and a weasel hutch
                    c.Rect(8, Floor + 16, 48, 3, Art.Hex(0x8a6448));
                    c.Rect(10, Floor, 3, 16, Art.Hex(0x5a3e34));
                    c.Rect(51, Floor, 3, 16, Art.Hex(0x5a3e34));
                    c.Rect(16, Floor + 19, 12, 8, Art.Hex(0x454b5c));
                    c.Rect(16, Floor + 26, 12, 1, Art.Hex(0x707888));
                    c.Circle(30, Floor + 23, 2.5f, Art.Hex(0x7cf7d4));
                    for (int i = 0; i < 4; i++) c.Set(34 + i * 3, Floor + 19, Art.Hex(0xc0c4cc));
                    c.Rect(4, Floor, 20, 11, Art.Hex(0xa07a58));
                    c.Rect(6, Floor + 2, 16, 7, Art.Hex(0x2a1c1a));
                    for (int x = 7; x < 22; x += 3) c.Rect(x, Floor + 2, 1, 7, Art.Hex(0x7a5a44));
                    break;
                case "tones2": // a slushie machine and a beanbag beside the couch
                    c.Rect(158, Floor, 13, 30, Art.Hex(0xd0d4dc));
                    c.Rect(158, Floor + 29, 13, 1, Art.Hex(0xffffff));
                    c.Rect(160, Floor + 16, 9, 11, Art.Hex(0xff5a5f));
                    c.Rect(160, Floor + 10, 9, 5, Art.Hex(0x3ec0ff));
                    c.Rect(163, Floor + 5, 3, 3, Art.Hex(0x454b5c));
                    c.Circle(184, Floor + 4, 7f, Art.Hex(0xff8c42));
                    c.Rect(176, Floor, 17, 4, Art.Hex(0xc06428));
                    break;
            }
        }

        static void Wrecked(PixelCanvas c, Station s)
        {
            int y0 = s.Upstairs ? Loft : Floor, w = s.X1 - s.X0;
            // dark: no lights reach it
            for (int y = y0; y < y0 + 44; y++)
                for (int x = s.X0; x < s.X1; x++)
                {
                    Color32 p = c.Get(x, y);
                    c.Set(x, y, new Color32((byte)(p.r * 0.45f), (byte)(p.g * 0.42f), (byte)(p.b * 0.5f), 255));
                }
            // a tarp over whatever was there, and the mess
            int peak = s.X0 + w / 2 + (s.X0 % 7) - 3;
            for (int y = 0; y < 22; y++)
            {
                int half = (int)((22 - y) * w * 0.42f / 22f);
                c.Rect(peak - half, y0 + y, half * 2, 1, y % 5 == 0 ? Art.Hex(0x2c3a2c) : Art.Hex(0x324232));
            }
            c.Rect(s.X0 + 3, y0, 6, 2, Art.Hex(0x3a2a20));
            c.Rect(s.X1 - 10, y0 + 1, 4, 1, Art.Hex(0x4a4a4a));
            for (int k = 0; k < 10; k++) c.Set(s.X0 + (k * 13 + s.X0) % Mathf.Max(1, w), y0 + 26 + (k * 7) % 14, Art.Hex(0x2a5a66, 150));
        }
    }
}
