using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PooketSlug.DenRoom;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The lobby: the crew's den and the loft above it (with the bunks in its roof), painted (<c>docs/design/lobby.md</c>, Den 2.0). You walk the
    /// soldier you're playing round it, and walking somewhere is the menu: the couch gets you ready (and the camera zooms
    /// into the TV once everyone is), the mission board opens the mission screen, the gym up the ladder the kit screen.
    /// Freed crewmates live at their stations and run little idle loops; tapping one swaps you to them. Every so often two
    /// of the crew do a bit with speech bubbles (<see cref="DenBits"/>). The stage is <see cref="StageKit"/>'s, shared with
    /// the cutscenes: 320 x 180 art pixels at 2x, and the camera follows you up the ladder and along the loft.
    /// <para>
    /// Its parts: <see cref="DenRoom"/> the paintings and where things sit on them, <see cref="DenMap"/> (Core) the floors,
    /// ladder and places, <see cref="DenCrew"/> the crew frame by frame, <see cref="DenBitPlayer"/> the bits,
    /// <see cref="DenConsole"/> the TV and buttons, <see cref="DenRollerDoor"/> the roller door to the tabk bay. This view is the screen around them: the camera, the taps and what's
    /// been pressed, and the zoom into the TV.
    /// </para>
    /// </summary>
    public sealed class DenView
    {
        readonly RectTransform m_Root, m_Stage, m_World, m_Paintings, m_InviteTap, m_RoomTap;
        readonly Image m_Fade;
        readonly DenCrew m_Crew;
        readonly DenBitPlayer m_Bits;
        readonly DenConsole m_Console;
        readonly DenRollerDoor m_Door;
        List<GameObject> m_Painted = new List<GameObject>();
        string m_PaintedKey;
        float m_Clock, m_CamX;

        SoldierDef m_Picked;
        DenPlace m_Place;
        bool m_Walk;
        DenFloor m_WalkFloor;
        float m_WalkX;
        bool m_ReadyPressed, m_BackPressed, m_KitPressed, m_HostPressed, m_InviteDown;

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

            // made in drawing order, back to front: the paintings (filled as the den opens), the crew just in front of them
            m_Paintings = Ui.Rect(m_World, "Paintings", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(StageW, StageH) * Px);
            m_Door = new DenRollerDoor(m_Paintings);
            m_Console.MakeTv();
            m_Crew.MakeProps();

            // the whole den and loft: a tap on someone swaps to them, on a place walks there to use it, anywhere else walks there
            m_RoomTap = Ui.Rect(m_World, "Tap room", Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(DenMap.LoftRight, DenMap.LoftY + DenMap.LoftH) * Px);
            Ui.Catcher(m_RoomTap);
            OnPointer(m_RoomTap, EventTriggerType.PointerClick, Tapped);

            // the TV, in a co-op room: a tap shares the invite link. It's armed as the finger goes down (Invite)
            m_InviteTap = Ui.Rect(m_World, "Tap TV", Vector2.zero, Vector2.zero, (TvCentre - new Vector2(30f, 17f)) * Px, new Vector2(60f, 34f) * Px);
            Ui.Catcher(m_InviteTap);
            OnPointer(m_InviteTap, EventTriggerType.PointerDown, _ => m_InviteDown = true);

            m_Crew.MakeVisitor();
            m_Fade = Ui.Cover(m_Stage, "Fade", new Color(0f, 0f, 0f, 0f));

            m_Console.MakeButtons(() => m_BackPressed = true, () => m_ReadyPressed = true, () => m_KitPressed = true, () => m_HostPressed = true);

            Ui.Show(m_Root, false);
        }

        static void OnPointer(RectTransform rt, EventTriggerType type, System.Action<PointerEventData> act)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(e => act((PointerEventData)e));
            rt.gameObject.AddComponent<EventTrigger>().triggers.Add(entry);
        }

        /// <summary>A tap on the room, in the den's art pixels: someone to swap to, a place to use, or the floor to walk on.</summary>
        void Tapped(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_World, e.position, e.pressEventCamera, out Vector2 local)) return;
            Vector2 p = local / Px + new Vector2(StageW, StageH) / 2f; // the world's pivot is its middle
            DenActor who = m_Crew.At(p);
            if (who != null && who.Doing != DenDoing.Seated)
            {
                m_Picked = who.Soldier;
                return;
            }
            DenPlace place = DenMap.PlaceAt(p.x, p.y);
            if (place != DenPlace.None)
            {
                m_Place = place;
                return;
            }
            m_Walk = true;
            m_WalkFloor = DenMap.WalkTarget(p.x, p.y, Walker?.Floor ?? DenFloor.Ground, out m_WalkX);
        }

        public bool Showing => m_Root.gameObject.activeSelf;

        /// <summary>Opens the den on <paramref name="den"/>: each freed crewmate at their station, and yours by the couch.</summary>
        public void Begin(Den den, DenInfo info)
        {
            string key = string.Join(",", System.Array.ConvertAll(ToArray(den.Crew), s => s.Name));
            if (m_PaintedKey != key)
            {
                m_PaintedKey = key;
                foreach (GameObject g in m_Painted) Object.Destroy(g);
                m_Painted = Paint(m_Paintings, den.Crew);
                m_Door.OnTop();
            }
            m_Crew.Begin(den, info, m_Paintings);
            m_Bits.Begin();
            m_Clock = 0f;
            m_CamX = 0f;
            m_Picked = null;
            m_Place = DenPlace.None;
            m_Walk = m_ReadyPressed = m_BackPressed = m_KitPressed = m_HostPressed = m_InviteDown = false;
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

        /// <summary>The place tapped since the last call (to walk there and use it), or None.</summary>
        public DenPlace TakePlace()
        {
            DenPlace p = m_Place;
            m_Place = DenPlace.None;
            return p;
        }

        /// <summary>The floor was tapped since the last call: where to walk.</summary>
        public bool TakeWalk(out DenFloor floor, out float x)
        {
            floor = m_WalkFloor;
            x = m_WalkX;
            return Take(ref m_Walk);
        }

        /// <summary>The soldier you're playing, walking round (null before there's a pick).</summary>
        public DenWalker Walker => m_Crew.Mine?.Walk;

        /// <summary>The place the soldier you're playing has just walked up to, to use it; once.</summary>
        public DenPlace TakeReached() => m_Crew.TakeReached();

        public bool TakeReady() => Take(ref m_ReadyPressed);
        public bool TakeBack() => Take(ref m_BackPressed);
        public bool TakeKit() => Take(ref m_KitPressed);
        public bool TakeHost() => Take(ref m_HostPressed);
        /// <summary>A finger went down on the TV in a co-op room: arm the invite (<see cref="Invite"/>).</summary>
        public bool TakeInvite() => Take(ref m_InviteDown);

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
            m_Door.Draw(info.DoorLift);
            m_Crew.Through = info.DoorThrough;
            m_Crew.Draw(dt, m_Clock, den);
            bool zooming = den.Zoom >= 0f;
            Ui.Show(m_InviteTap, info.InviteLink != null && !zooming);
            Ui.Show(m_RoomTap, !zooming);
            m_Console.Refresh(den, info, m_Clock);
            Camera(dt, den);
        }

        /// <summary>
        /// The camera: on the den screen until you climb above its upper floor, then up to the loft, following you along
        /// it; once everyone's ready, it zooms into the TV and everything goes dark, into the mission.
        /// </summary>
        void Camera(float dt, Den den)
        {
            DenWalker w = Walker;
            // up the den's ladder the camera rises to the loft (its top at the painting's roof, the den's ceiling showing
            // under its floor), then pans the loft's little extra width; the bunks in its roof are in the same view
            float upper = DenMap.FloorY(DenFloor.Upper), loft = DenMap.FloorY(DenFloor.Loft);
            float rise = w == null ? 0f : Mathf.Clamp01((w.Y - upper) / (loft - upper));
            float camY = rise * (DenMap.LoftY + DenMap.LoftH - StageH);
            float toX = w != null && rise >= 1f ? Mathf.Clamp(w.X - StageW / 2f, DenMap.LoftLeft, DenMap.LoftRight - StageW) : 0f;
            m_CamX = Mathf.Lerp(m_CamX, toX, 1f - Mathf.Exp(-8f * dt));
            if (Mathf.Abs(m_CamX - toX) < 0.05f) m_CamX = toX;
            Vector2 cam = -new Vector2(Mathf.Round(m_CamX), Mathf.Round(camY)) * Px;
            if (den.Zoom < 0f)
            {
                m_World.localScale = Vector3.one;
                m_World.anchoredPosition = cam;
                m_Fade.color = new Color(0f, 0f, 0f, 0f);
                return;
            }
            float k = Mathf.Clamp01(den.Zoom / Den.ZoomSeconds);
            float ease = k * k * (3f - 2f * k);
            float scale = Mathf.Lerp(1f, 6f, ease);
            Vector2 off = (TvCentre - new Vector2(StageW, StageH) / 2f) * Px;
            m_World.localScale = new Vector3(scale, scale, 1f);
            m_World.anchoredPosition = Vector2.Lerp(cam, -off * scale, ease);
            m_Fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01((k - 0.55f) / 0.4f));
        }
    }
}
