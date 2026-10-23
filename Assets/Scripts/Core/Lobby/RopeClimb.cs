using System;

namespace PooketSlug
{
    /// <summary>
    /// The bunker's rope (<c>docs/design/lobby.md</c>, The outer rooms): the way in is climbing down it from the hole in
    /// B1's floor, and the way out is climbing up it and out of the hole. It takes no walking while it runs, and goes one
    /// way at a time. A sibling of <see cref="SwingDoor"/>, which swings: this one is climbed.
    /// </summary>
    public sealed class RopeClimb
    {
        /// <summary>The way a climb ended: down onto the bunker's floor, or up and out of the hole.</summary>
        public enum Way { None, Down, Up }

        bool m_Going, m_Up;

        /// <summary>How far up the rope your feet are, in the room's art pixels (<see cref="RoomMap.FloorY"/> is the floor).</summary>
        public float Y { get; private set; }

        /// <summary>Climbing, up or down the rope: the room doesn't take walking.</summary>
        public bool Busy => m_Going;

        /// <summary>Climbing up, on the way out. False while climbing down, and standing.</summary>
        public bool GoingUp => m_Going && m_Up;

        /// <summary>Not climbing: the rope is still, as the room opens.</summary>
        public void Reset()
        {
            m_Going = false;
            m_Up = false;
            Y = RoomMap.FloorY;
        }

        /// <summary>You step into the hole: you start at the top and climb down. Only from standing.</summary>
        public bool GoDown()
        {
            if (m_Going) return false;
            m_Going = true;
            m_Up = false;
            Y = RoomMap.RopeTopY;
            return true;
        }

        /// <summary>You take the rope from the floor: you climb up and out of the hole. Only from standing.</summary>
        public bool GoUp()
        {
            if (m_Going) return false;
            m_Going = true;
            m_Up = true;
            Y = RoomMap.FloorY;
            return true;
        }

        /// <summary>
        /// Moves you on a frame. The way you finished it, once, on the frame you land on the floor or leave the hole;
        /// None otherwise.
        /// </summary>
        public Way Tick(float dt)
        {
            if (!m_Going) return Way.None;
            float to = m_Up ? RoomMap.RopeTopY : RoomMap.FloorY;
            float step = RoomMap.RopeSpeed * dt;
            Y = Math.Abs(to - Y) <= step ? to : Y + Math.Sign(to - Y) * step;
            if (Y != to) return Way.None;
            m_Going = false;
            return m_Up ? Way.Up : Way.Down;
        }
    }
}
