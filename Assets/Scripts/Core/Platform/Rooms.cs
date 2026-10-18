using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// Rooms: what the co-op room says about itself (its mission, who's playing, who's watching). The room's master
    /// client keeps these up to date in the session's properties, and a phone arriving reads them to know whether
    /// it plays or watches (<see cref="RoomBrowser.ActionFor"/>). There's one room now (<see cref="CoopRoom"/>).
    /// </summary>
    public struct RoomListing
    {
        /// <summary>The session's name: its room code.</summary>
        public string Name;
        /// <summary>The mission on its TV, or being played (<see cref="MissionDef.NetId"/>).</summary>
        public int Mission;
        /// <summary>Players with a soldier, and people watching.</summary>
        public int Soldiers, Watching;
        /// <summary>A mission is being played (not everyone's in the den).</summary>
        public bool UnderWay;
        /// <summary>The host's soldier (<see cref="SoldierDef"/> id), or -1.</summary>
        public int Host;
    }

    /// <summary>What the browser offers for a room.</summary>
    public enum RoomAction { Join, Watch, Locked, Full }

    /// <summary>A line of the room browser.</summary>
    public struct RoomRow
    {
        public RoomListing Room;
        public RoomAction Action;
    }

    /// <summary>
    /// The room's rules (docs/design/levels.md, Online): with a free seat you play, and with both taken you watch, but
    /// only once you've cleared its mission, so nothing ahead is spoiled. <see cref="Rows"/>, <see cref="Line"/> and
    /// <see cref="Button"/> are the old room browser screen's, which Start flow 2 takes out.
    /// </summary>
    public static class RoomBrowser
    {
        /// <summary>People who can watch one room at once (Photon's free plan has 20 connections in all).</summary>
        public const int MaxWatchers = 4;
        /// <summary>Everyone a room holds: the players and the watchers.</summary>
        public const int MaxPeople = Tuning.Coop.MaxPlayers + MaxWatchers;
        /// <summary>Rooms the browser shows at once.</summary>
        public const int MaxRows = 4;

        /// <summary>The session property keys (short: they go to everyone browsing).</summary>
        public const string MissionKey = "m", SoldiersKey = "n", WatchingKey = "w", UnderWayKey = "g", HostKey = "h", OpenKey = "o";

        public static RoomAction ActionFor(in RoomListing room, Func<int, bool> cleared)
        {
            if (room.Soldiers < Tuning.Coop.MaxPlayers) return RoomAction.Join;
            if (room.Watching >= MaxWatchers) return RoomAction.Full;
            return cleared(room.Mission) ? RoomAction.Watch : RoomAction.Locked;
        }

        /// <summary>
        /// The browser's lines: rooms to join first, then games to watch, then the rest, each in code order, at most
        /// <see cref="MaxRows"/>.
        /// </summary>
        public static List<RoomRow> Rows(IEnumerable<RoomListing> rooms, Func<int, bool> cleared)
        {
            var rows = new List<RoomRow>();
            foreach (RoomListing r in rooms)
                if (!string.IsNullOrEmpty(r.Name)) rows.Add(new RoomRow { Room = r, Action = ActionFor(r, cleared) });
            rows.Sort((a, b) => a.Action != b.Action ? a.Action.CompareTo(b.Action) : string.CompareOrdinal(a.Room.Name, b.Room.Name));
            if (rows.Count > MaxRows) rows.RemoveRange(MaxRows, rows.Count - MaxRows);
            return rows;
        }

        /// <summary>A line for a room: its code, who's hosting, the mission, its seats and watchers.</summary>
        public static string Line(in RoomListing room)
        {
            SoldierDef host = room.Host >= 0 ? Content.Soldier(room.Host) : null;
            MissionDef mission = Missions.ByNetId(room.Mission);
            string what = mission == null ? "A NEWER MISSION" : mission == Missions.One ? "TEST MISSION" : mission.Name; // Mission one is solo only: co-op plays it as a stand-in
            string line = room.Name + "  " + (host != null ? host.Name.ToUpperInvariant() + "  " : "") + what + "  " +
                          Math.Min(room.Soldiers, Tuning.Coop.MaxPlayers) + "/" + Tuning.Coop.MaxPlayers + (room.UnderWay ? " PLAYING" : " IN THE DEN");
            return room.Watching > 0 ? line + "  " + room.Watching + " WATCHING" : line;
        }

        /// <summary>The button for a room's line.</summary>
        public static string Button(RoomAction action)
        {
            switch (action)
            {
                case RoomAction.Join: return "JOIN";
                case RoomAction.Watch: return "WATCH";
                case RoomAction.Full: return "FULL";
                default: return "LOCKED";
            }
        }
    }

    /// <summary>
    /// How many are watching a room: the people in it with no soldier. A player who's just joined has no soldier for a
    /// moment either, so a new count only shows once it has held for <see cref="Hold"/> seconds.
    /// </summary>
    public sealed class WatcherCount
    {
        public const float Hold = 2f;
        int m_Pending, m_Shown;
        float m_Held;

        public int Shown => m_Shown;

        /// <summary>A frame of <paramref name="dt"/> seconds in which <paramref name="raw"/> people had no soldier.</summary>
        public int Tick(float dt, int raw)
        {
            if (raw != m_Pending)
            {
                m_Pending = raw;
                m_Held = 0f;
            }
            else m_Held += dt;
            if (m_Held >= Hold) m_Shown = m_Pending;
            return m_Shown;
        }

        public void Reset() { m_Pending = m_Shown = 0; m_Held = 0f; }
    }
}
