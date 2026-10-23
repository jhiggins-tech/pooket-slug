using System;

namespace PooketSlug
{
    /// <summary>
    /// The bunker's rope (<see cref="RoomMap.ByRope"/>, <c>docs/design/lobby.md</c>, The outer rooms): you're at the hole
    /// in its roof as it opens and climb down it, and you climb back up it to leave, hand over hand. While it's going the
    /// room doesn't take walking, and the frame you land or climb out of the hole it says so once. A sibling of
    /// <see cref="SwingDoor"/> and <see cref="RollerDoor"/>, which are doors rather than a climb.
    /// </summary>
    public sealed class RopeClimb
    {
        enum Mode { Still, Down, Up }

        readonly float m_Top;
        Mode m_Mode;
        bool m_Landed, m_Left;

        /// <param name="room">The room it hangs in: its ceiling is the rope's top, and its floor the bottom.</param>
        public RopeClimb(RoomId room)
        {
            m_Top = RoomMap.RopeTop(room);
            Y = RoomMap.FloorY;
        }

        /// <summary>Where your feet are on it, in the room's art pixels: the ceiling at the top, the floor at the bottom.</summary>
        public float Y { get; private set; }

        /// <summary>Climbing: the room doesn't take walking.</summary>
        public bool Busy => m_Mode != Mode.Still;

        /// <summary>Climbing up, out of the hole (rather than down into the room).</summary>
        public bool GoingUp => m_Mode == Mode.Up;

        /// <summary>On the floor, off the rope (the room opening).</summary>
        public void Reset()
        {
            m_Mode = Mode.Still;
            Y = RoomMap.FloorY;
            m_Landed = m_Left = false;
        }

        /// <summary>At the hole, coming down it: only from off the rope.</summary>
        public bool GoDown()
        {
            if (m_Mode != Mode.Still) return false;
            m_Mode = Mode.Down;
            Y = m_Top;
            return true;
        }

        /// <summary>On the floor, going up it: only from off the rope.</summary>
        public bool GoUp()
        {
            if (m_Mode != Mode.Still) return false;
            m_Mode = Mode.Up;
            Y = RoomMap.FloorY;
            return true;
        }

        /// <summary>True once, the frame your feet reach the floor.</summary>
        public bool TakeLanded()
        {
            bool l = m_Landed;
            m_Landed = false;
            return l;
        }

        /// <summary>True once, the frame you climb out of the hole.</summary>
        public bool TakeLeft()
        {
            bool l = m_Left;
            m_Left = false;
            return l;
        }

        /// <summary>Moves you on, a frame.</summary>
        public void Tick(float dt)
        {
            if (m_Mode == Mode.Still) return;
            float target = m_Mode == Mode.Down ? RoomMap.FloorY : m_Top;
            float step = RoomMap.RopeSpeed * dt;
            Y = Math.Abs(target - Y) <= step ? target : Y + Math.Sign(target - Y) * step;
            if (Y != target) return;
            if (m_Mode == Mode.Down) m_Landed = true;
            else m_Left = true;
            m_Mode = Mode.Still;
        }
    }
}
