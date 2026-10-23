// The bunker's rope: the way in is down it from the hole in the room's roof, and the way out is back up it. A flow, like SwingDoor: it runs itself a frame at a time while the room takes no walking.
using System;

namespace PooketSlug
{
    /// <summary>
    /// The bunker's rope (<c>docs/design/lobby.md</c>, The outer rooms): the hole in the basement's floor leads down into
    /// the bunker, so you climb down its rope hand over hand; the way back is up the rope and out of the hole. A sibling
    /// of <see cref="SwingDoor"/>: while it's going the room takes no walking, and it says once when you've landed or
    /// left.
    /// </summary>
    public sealed class RopeClimb
    {
        /// <summary>Which way you're climbing: down from the hole, up out of it, or none (standing).</summary>
        public enum Way { None, Down, Up }

        Way m_Going;
        float m_Y;

        public RopeClimb()
        {
            Reset();
        }

        /// <summary>Which way you're going, or None when you're not on the rope.</summary>
        public Way Going => m_Going;

        /// <summary>Climbing: the room doesn't take walking.</summary>
        public bool Busy => m_Going != Way.None;

        /// <summary>Where your feet are on the rope, in the room's art pixels (from the floor up to the hole).</summary>
        public float Y => m_Y;

        /// <summary>Standing on the floor, off the rope.</summary>
        public void Reset()
        {
            m_Going = Way.None;
            m_Y = RoomMap.FloorY;
        }

        /// <summary>You've come to the hole: you start at the top of the rope and climb down. False while it's going.</summary>
        public bool GoDown()
        {
            if (Busy) return false;
            m_Going = Way.Down;
            m_Y = RoomMap.RopeTopY;
            return true;
        }

        /// <summary>You've reached the rope from the floor: you climb up and out of the hole. False while it's going.</summary>
        public bool GoUp()
        {
            if (Busy) return false;
            m_Going = Way.Up;
            m_Y = RoomMap.FloorY;
            return true;
        }

        /// <summary>
        /// Moves you on. The way you've just finished, once, on the frame you land on the floor or leave through the
        /// hole; None while you're still climbing.
        /// </summary>
        public Way Tick(float dt)
        {
            if (!Busy) return Way.None;
            float target = m_Going == Way.Down ? RoomMap.FloorY : RoomMap.RopeTopY;
            float step = RoomMap.RopeSpeed * dt;
            m_Y = Math.Abs(target - m_Y) <= step ? target : m_Y + Math.Sign(target - m_Y) * step;
            if (m_Y != target) return Way.None;
            Way done = m_Going;
            m_Going = Way.None;
            return done;
        }
    }
}
