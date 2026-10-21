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
            if (m_View.TakeBack() || keys.Back) m_Game.Rooms.Back(m_Room);
            RoomWay way = m_View.TakeWay();
            if (way != RoomWay.None) Use(way);
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
            RoomWay done = m_Walker.Step(dt);
            if (done != RoomWay.None) Use(done);
            m_View.Refresh(dt, m_Walker);
        }

        /// <summary>Uses a way: the way back to the house, or the way on to the next room.</summary>
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
            m_Game.Rooms.Go(m_Room, to, by);
        }
    }
}
