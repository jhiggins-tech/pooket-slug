using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.DenRoom;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The lobby: the crew's gaming den, drawn in uGUI (<c>docs/design/lobby.md</c>). The hangout from the opening, side-on in
    /// two floors, with a station for each crewmate: freed ones live at theirs and run little idle loops, and the rest stay
    /// wrecked and dark from the raid. Tapping a crewmate sits them on the couch with a controller (<see cref="Den"/>);
    /// READY presses START, and the camera zooms into the TV. Every so often two of the crew do a bit with speech bubbles
    /// (<see cref="DenBits"/>). The stage is <see cref="StageKit"/>'s, shared with the cutscenes: 320 x 180 art pixels at 2x, letterboxed on other shapes.
    /// <para>
    /// Its parts: <see cref="DenRoom"/> the room's layout and painting, <see cref="DenCrew"/> the crew frame by frame,
    /// <see cref="DenBitPlayer"/> the bits, <see cref="DenConsole"/> the TV and buttons. This view is the screen around
    /// them: the taps (the stations, the board, the TV) and what's been pressed, and the zoom into the TV.
    /// </para>
    /// </summary>
    public sealed class DenView
    {
        readonly RectTransform m_Root, m_Stage, m_World;
        readonly Image m_Back, m_Fade;
        readonly List<Button> m_StationTaps = new List<Button>();
        readonly RectTransform m_InviteTap;
        readonly DenCrew m_Crew;
        readonly DenBitPlayer m_Bits;
        readonly DenConsole m_Console;
        Sprite m_Backdrop;
        string m_BackdropKey;
        float m_Clock;

        SoldierDef m_Picked;
        bool m_ReadyPressed, m_BackPressed, m_KitPressed, m_HostPressed, m_BoardPressed, m_InviteDown, m_ListPressed;

        public DenView(Transform parent)
        {
            m_Root = Ui.Canvas(parent, "Den", 8); // under the menus (10), so the ⓘ kit screen and notices show over it
            m_Root.gameObject.AddComponent<GraphicRaycaster>();
            Build(m_Root, out m_Stage, out m_World);
            var rng = new System.Random();
            m_Crew = new DenCrew(m_World, rng);
            m_Bits = new DenBitPlayer(m_World, m_Crew, rng);
            m_Crew.Bits = m_Bits;
            m_Console = new DenConsole(m_World, m_Stage, m_Crew);

            // made in drawing order, back to front (the crew go in just in front of the room, as the den opens)
            m_Back = BackdropImage(m_World);
            // the TV: the console's start screen, the room, both controllers' lights, the countdown
            m_Console.MakeTv();
            m_Crew.MakeProps();

            // the stations: a tap on a freed crewmate's corner sits them on the couch
            foreach (Station st in Stations)
            {
                if (st.Soldier == null) continue;
                Station s = st;
                float y0 = s.Upstairs ? Loft : Floor;
                RectTransform rt = Ui.Rect(m_World, "Tap " + s.Crewmate, Vector2.zero, Vector2.zero, new Vector2(s.X0, y0) * Px, new Vector2(s.X1 - s.X0, 46f) * Px);
                m_StationTaps.Add(Ui.Tap(rt, () => m_Picked = s.Soldier));
            }

            // the mission board, over the stations: a tap opens the mission screen
            RectTransform board = Ui.Rect(m_World, "Tap mission board", Vector2.zero, Vector2.zero, new Vector2(234f, 54f) * Px, new Vector2(32f, 30f) * Px);
            Ui.Tap(board, () => m_BoardPressed = true);

            // the TV, in a co-op room: a tap shares the invite link. It's armed as the finger goes down (Invite)
            m_InviteTap = Ui.Rect(m_World, "Tap TV", Vector2.zero, Vector2.zero, new Vector2(86f, 54f) * Px, new Vector2(62f, 34f) * Px);
            Ui.Catcher(m_InviteTap);
            var down = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
            down.callback.AddListener(_ => m_InviteDown = true);
            m_InviteTap.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>().triggers.Add(down);

            m_Crew.MakeVisitor();
            m_Fade = Ui.Cover(m_Stage, "Fade", new Color(0f, 0f, 0f, 0f));

            m_Console.MakeButtons(() => m_BackPressed = true, () => m_ListPressed = true, () => m_ReadyPressed = true, () => m_KitPressed = true, () => m_HostPressed = true);

            Ui.Show(m_Root, false);
        }

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
            m_Crew.Begin(den, info, m_Back.transform);
            m_Bits.Begin();
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
            m_Bits.Stop();
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
        public void TvSay(string text) => m_Console.TvSay(text, m_Clock);

        static bool Take(ref bool flag)
        {
            bool f = flag;
            flag = false;
            return f;
        }

        /// <summary>Says something across the top for a few seconds (a pick undone, say).</summary>
        public void Note(string text, float seconds = 3f) => m_Console.Note(text, seconds, m_Clock);

        // ------------------------------------------------------------------ each frame

        public void Refresh(float dt, Den den, DenInfo info)
        {
            m_Clock += dt;
            m_Crew.Arrange(den, info);
            m_Bits.Tick(dt, den);
            m_Crew.Draw(dt, m_Clock, den);
            bool zooming = den.Zoom >= 0f;
            Ui.Show(m_InviteTap, info.InviteLink != null && !zooming);
            foreach (Button b in m_StationTaps) b.interactable = !zooming;
            m_Console.Refresh(den, info, m_Clock);
            Zoom(den);
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
    }
}
