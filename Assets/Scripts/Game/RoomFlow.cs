using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// One of the house's outer rooms, open on screen (<c>docs/design/lobby.md</c>, The outer rooms): you walk round it,
    /// and the way in or the way on takes you somewhere else. Each room is its own screen, like the tabk bay.
    /// </summary>
    public sealed class RoomFlow
    {
        readonly Game m_Game;
        readonly RoomView m_View;
        RoomWalker m_Walker;
        RoomId m_Room = RoomId.None;
        int m_HeldDir;
        readonly SwingDoor m_Door = new SwingDoor();
        RoomId m_DoorTo = RoomId.None;

        public RoomFlow(Game game, Transform parent)
        {
            m_Game = game;
            m_View = new RoomView(parent);
        }

        public bool Open => m_Room != RoomId.None;

        /// <summary>The room you're in, or None.</summary>
        public RoomId Room => m_Room;

        /// <summary>Opens a room, with you at the way in.</summary>
        public void Show(RoomId room, RoomWay wayIn)
        {
            m_Room = room;
            m_HeldDir = 0;
            m_Door.Reset();
            m_DoorTo = RoomId.None;
            m_Walker = new RoomWalker(room, RoomMap.WayX(room, wayIn));
            m_View.Show(room, m_Game.Soldier);
            Log.Game.Info("Room open: " + room);
        }

        public void Hide()
        {
            m_Room = RoomId.None;
            m_View.Hide();
        }

        /// <summary>
        /// The room's ticks: a tap on a way, a tap on the floor, BACK, or the soldier walking by hand.
        /// </summary>
        public void Tick(MenuInput keys, SoldierInput soldier)
        {
            if (m_Room == RoomId.None) return;
            if (m_Door.Busy)
            {
                // through the back door: nothing else till it's done (and nothing tapped meanwhile kept)
                m_View.TakeWay();
                m_View.TakeWalk(out _);
                return;
            }
            if (m_View.TakeBack() || keys.Back) m_Game.Rooms.Back(m_Room);
            RoomWay way = m_View.TakeWay();
            if (way != RoomWay.None) m_Walker.Go(way);   // walk to it, and use it on arriving
            if (m_View.TakeWalk(out float x)) m_Walker.WalkTo(x);

            // by hand: jump uses the way you stand at, up and down work on the wintergarden's stair
            if (soldier.JumpPressed && m_Walker.Standing) Use(m_Walker.WayHere);
            int dir = soldier.Left == soldier.Right ? 0 : soldier.Left ? -1 : 1;
            int climb = soldier.Up == soldier.Down ? 0 : soldier.Up ? 1 : -1;
            if (climb != 0 && m_Walker.Climb(climb)) m_HeldDir = 0;
            else if (dir != 0) m_Walker.Walk(dir);
            else if (m_HeldDir != 0) m_Walker.Walk(0); // let go of the key: stop
            if (climb == 0) m_HeldDir = dir;
        }

        /// <summary>Moves you on, a frame.</summary>
        public void Move(float dt)
        {
            if (m_Room == RoomId.None) return;
            if (m_Door.Busy)
            {
                // the back door: it creaks open, you step into its dark, it swings shut behind you, and the next room
                // takes over on the frame it lands
                m_Door.Tick(dt);
                m_View.DoorThrough = m_Door.Through;
                if (m_Door.TakeSwung()) Sfx.Play(Sfx.CableClunk, 0.5f); // its creak
                if (m_Door.TakeLanded()) Sfx.Play(Sfx.Thump, 0.5f); // it lands shut
                if (!m_Door.Busy)
                {
                    Log.Game.Info("Room: through the back door, to " + m_DoorTo);
                    m_Game.Rooms.Go(m_Room, m_DoorTo, RoomWay.Back);
                    return;
                }
                m_Walker.Place(m_Room, RoomBackDoor.Sill.x); // the view draws the step through, from where you stood
                m_View.DoorBeat(dt, m_Door.Swing, m_Door.Through, m_Walker);
                return;
            }
            m_View.Refresh(dt, m_Walker);
            Use(m_View.TakeReached());
        }

        /// <summary>Uses a way: the way back to the house, the way on to the next room through the back door, or a note.</summary>
        void Use(RoomWay way)
        {
            if (way == RoomWay.None) return;
            RoomMap.Leads(m_Room, way, out RoomId to, out RoomWay by);
            if (to == RoomId.None && by == RoomWay.None)
            {
                // it doesn't lead anywhere yet: it says so
                string note = RoomMap.Note(way);
                if (note != null) m_View.Note(note);
                return;
            }
            if (to != RoomId.None)
            {
                // the way on: the door creaks open, and you step into its dark
                m_DoorTo = to;
                if (m_Door.GoIn()) m_View.PlayDoor();
                return;
            }
            m_Game.Rooms.Go(m_Room, to, by);
        }
    }
}
