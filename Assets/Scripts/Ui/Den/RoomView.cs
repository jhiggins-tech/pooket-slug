using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// One of the house's outer rooms on screen (<c>docs/design/lobby.md</c>, The outer rooms): the room's painting, you
    /// walking round it, and the way out. Each room is its own screen behind a door, like the tabk bay.
    /// </summary>
    public sealed class RoomView : StageScreen
    {
        readonly StageFigure m_Soldier;
        readonly Text m_Note;
        readonly RectTransform m_Tap;
        readonly Latch<RoomWay> m_TapWay = new Latch<RoomWay>();
        readonly Latch<float> m_TapWalk = new Latch<float>();
        readonly Latch m_Back = new Latch();
        readonly RoomBackDoor m_Door;

        GameObject m_Plate;
        float m_Clock, m_CamX, m_CamY, m_NoteUntil;
        RoomId m_Room = RoomId.None;
        SoldierDef m_SoldierDef;
        bool m_OnRope;
        float m_RopeY;

        public RoomView(Transform parent) : base(parent, "Room", 9) // over the den (8), under the menus (10)
        {
            m_Letterbox.raycastTarget = true; // taps stop here, not on the den under it

            // the whole room takes taps: a way to use, anywhere else to walk to. It's made under the soldier and the
            // painting, and brought on top of them, so a tap lands on it and not on them
            m_Tap = Ui.Rect(m_World, "Tap room", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(RoomMap.Width, RoomMap.Height) * Px);
            Ui.Catcher(m_Tap);
            OnPointer(m_Tap, EventTriggerType.PointerClick, Tapped);

            m_Soldier = new StageFigure(m_World, "You");
            m_Door = new RoomBackDoor(m_World);

            Vector2 half = new Vector2(StageW, StageH) * Px / 2f;
            Ui.Button(m_Stage, "BACK", new Vector2(-half.x + 40f, half.y - 18f), new Vector2(64f, 22f), m_Back.Set, out Text backKey);
            backKey.text = "ESC";
            m_Note = Ui.Label(m_Stage, "Note", Ui.TopCenter, new Vector2(0f, -24f), 11, TextAnchor.UpperCenter, new Color(1f, 0.55f, 0.45f));

            Ui.Show(m_Root, false);
        }

        /// <summary>A tap on the room, in its art pixels: a way to use, or the floor to walk to.</summary>
        void Tapped(PointerEventData e)
        {
            // the tap's in the room's own art pixels: the tap rect's pivot is its bottom left, which is the room's
            // bottom left, and the world moves with the camera, so this is where the tap landed in the room
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Tap, e.position, e.pressEventCamera, out Vector2 local)) return;
            Vector2 p = local / Px;
            RoomWay way = RoomMap.WayAt(m_Room, p.x, p.y);
            if (way != RoomWay.None) m_TapWay.Set(way);
            else m_TapWalk.Set(p.x);
        }

        /// <summary>Opens a room: its painting, and you standing at the way in.</summary>
        public void Show(RoomId room, SoldierDef soldier)
        {
            m_Room = room;
            m_SoldierDef = soldier;
            m_Clock = 0f;
            m_CamX = m_CamY = 0f;

            // the painting, its own screen: it sits at the room's bottom left
            if (m_Plate != null) Object.Destroy(m_Plate);
            Sprite plate = DenRoom.Painting(RoomMap.PaintingOf(room));
            if (plate != null)
            {
                var img = Ui.Picture(m_World, "Room", plate, Vector2.zero, Vector2.zero, Vector2.zero, plate.rect.size * Px, Color.white);
                m_Plate = img.gameObject;
            }
            // the order back to front: the painting, then you, then the taps over them
            if (m_Plate != null) m_Plate.transform.SetAsFirstSibling();
            m_Soldier.Rt.SetAsLastSibling();
            m_Tap.SetAsLastSibling();
            // the taps cover the whole room, which is wider than the screen (or taller, in the wintergarden). They're anchored
            // at the world's bottom left, as the painting is, so the room's own art pixels and theirs line up
            m_Tap.sizeDelta = new Vector2(RoomMap.WidthOf(room), RoomMap.HeightOf(room)) * Px;
            m_Tap.anchoredPosition = Vector2.zero;

            m_TapWay.Clear();
            m_TapWalk.Clear();
            m_Back.Clear();
            m_Note.text = "";
            DoorThrough = 0f;
            m_Reached = RoomWay.None;
            m_Door.Swing = 0f;
            m_OnRope = false;
            Ui.Show(m_Root, true);
        }

        public void Hide() => Ui.Show(m_Root, false);

        /// <summary>How far through the back door you are (<see cref="SwingDoor.Through"/>): 0 in the room, 1 through.</summary>
        public float DoorThrough { get; set; }

        /// <summary>Arms the back door's beat (<see cref="SwingDoor"/>, in Core): the leaf over the painting and you.</summary>
        public void PlayDoor() => m_Door.OnTop();

        public RoomWay TakeWay() => m_TapWay.Take(out RoomWay w) ? w : RoomWay.None;
        public bool TakeWalk(out float x) => m_TapWalk.Take(out x);
        public bool TakeBack() => m_Back.Take();

        /// <summary>The walking that finished this frame, to a way (or None): the room reads it and uses the way.</summary>
        public RoomWay TakeReached()
        {
            RoomWay way = m_Reached;
            m_Reached = RoomWay.None;
            return way;
        }
        RoomWay m_Reached;

        /// <summary>Says something across the top for a few seconds.</summary>
        public void Note(string text, float seconds = 3f)
        {
            m_Note.text = text;
            m_NoteUntil = m_Clock + seconds;
        }

        /// <summary>
        /// The back door's beat, a frame: the leaf at <paramref name="swing"/> (the flow's <see cref="SwingDoor"/>), and
        /// you stepping into its dark as far as <paramref name="through"/>.
        /// </summary>
        public void DoorBeat(float dt, float swing, float through, RoomWalker walker)
        {
            m_Clock += dt;
            m_Door.Swing = swing;
            DoorThrough = through;
            You(walker);
            Camera(dt, walker);
            m_Door.Draw();
        }

        /// <summary>Draws the room: you, the camera following you, and the back door's leaf when the beat leaves it open.</summary>
        public void Refresh(float dt, RoomWalker walker)
        {
            m_Clock += dt;
            m_OnRope = false;
            RoomWay done = walker.Step(dt);
            if (done != RoomWay.None) m_Reached = done;
            You(walker);
            Camera(dt, walker);
            if (m_Door.Swing > 0f) m_Door.Draw();
            if (m_Clock > m_NoteUntil) m_Note.text = "";
        }

        /// <summary>
        /// The bunker's rope, a frame (<see cref="RopeClimb"/>, in Core): you on the rope with your feet at
        /// <paramref name="y"/>, hand over hand, on the way down into the bunker or up and out of it.
        /// </summary>
        public void RopeBeat(float dt, float y, RoomWalker walker)
        {
            m_Clock += dt;
            m_OnRope = true;
            m_RopeY = y;
            You(walker);
            Camera(dt, walker);
            if (m_Door.Swing > 0f) m_Door.Draw();
        }

        void You(RoomWalker w)
        {
            PlayerSkin skin = Art.SkinFor(m_SoldierDef);
            if (m_OnRope)
            {
                // on the rope, facing us: the den's ladder climb's pose, shivering hand over hand
                m_Soldier.Pose(skin, StagePose.Climbing(m_Clock));
                m_Soldier.AtPlate(RoomMap.RopeX + Fx.Shiver(m_Clock, 6f, 0.5f), m_RopeY);
                m_Soldier.Tint(Color.white);
                m_Soldier.Rt.localScale = Vector3.one;
                return;
            }
            // through the back door: into its dark, a little smaller as you go (as the den's crew is in its roller door)
            bool stepping = DoorThrough > 0f;
            bool walking = stepping || !w.Standing;
            m_Soldier.Pose(skin, StagePose.Walking(walking, m_Clock));
            float x = w.X, y = w.Y;
            if (stepping)
            {
                x = RoomBackDoor.Sill.x + Mathf.Lerp(RoomBackDoor.From, RoomBackDoor.To, DoorThrough);
                y = RoomBackDoor.Sill.y;
            }
            m_Soldier.AtPlate(x, y);
            Color shade = Color.Lerp(Color.white, new Color(0.15f, 0.15f, 0.2f), DoorThrough);
            m_Soldier.Tint(shade);
            float size = 1f - 0.2f * DoorThrough;
            bool faceLeft = stepping ? DoorThrough < 0.5f : w.FaceLeft;
            m_Soldier.Rt.localScale = new Vector3((faceLeft ? -1f : 1f) * size, size, 1f);
        }

        /// <summary>
        /// The camera: on the room, following you. A room wider than a screen scrolls along, and the wintergarden rises
        /// with you up its stair, so the rose at the top is out of reach until you've climbed.
        /// </summary>
        void Camera(float dt, RoomWalker w)
        {
            float width = RoomMap.WidthOf(m_Room);
            float height = RoomMap.HeightOf(m_Room);
            float toX = 0f, toY = 0f;
            if (m_Room == RoomId.Wintergarden)
            {
                // up the stair: the camera rises with you, and pans a little to the stair's foot
                float rise = Mathf.Clamp01((w.Y - RoomMap.FloorY) / (RoomMap.LandingY - RoomMap.FloorY));
                toY = rise * (height - StageH);
            }
            // a room wider than a screen scrolls along to keep you in the middle
            if (width > StageW) toX = Mathf.Clamp(w.X - StageW / 2f, 0f, width - StageW);
            m_CamX = Mathf.Lerp(m_CamX, toX, 1f - Mathf.Exp(-8f * dt));
            m_CamY = Mathf.Lerp(m_CamY, toY, 1f - Mathf.Exp(-8f * dt));
            if (Mathf.Abs(m_CamX - toX) < 0.05f) m_CamX = toX;
            if (Mathf.Abs(m_CamY - toY) < 0.05f) m_CamY = toY;
            m_World.anchoredPosition = -new Vector2(Mathf.Round(m_CamX), Mathf.Round(m_CamY)) * Px;
        }
    }
}
