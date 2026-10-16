using System;
using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// Rooms: what a co-op room tells the room browser about itself. The room's master client keeps these up to date in
    /// the session's properties, which Photon shows to everyone browsing.
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
    /// The room browser's rules (docs/design/levels.md, Online): a room with a free seat can be joined, and a full one
    /// watched, but only by someone who has cleared its mission, so nothing ahead of them is spoiled.
    /// </summary>
    public static class RoomBrowser
    {
        /// <summary>People who can watch one room at once (Photon's free plan has 20 connections in all).</summary>
        public const int MaxWatchers = 4;
        /// <summary>Everyone a room holds: the players and the watchers.</summary>
        public const int MaxPeople = Tuning.Coop.MaxPlayers + MaxWatchers;
        /// <summary>Rooms the browser shows at once.</summary>
        public const int MaxRows = 4;

        /// <summary>
        /// Whether a hosted room starts listed, and whether its host can switch that in the den. Jack's call (pending):
        /// the host chooses, starting invite only.
        /// </summary>
        public const bool HostListed = false, HostChooses = true;

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

        /// <summary>Whether a quick match may put a new player in this room: it's open, with a free seat.</summary>
        public static bool OpenFor(int soldiers, bool soloOnly) => !soloOnly && soldiers < Tuning.Coop.MaxPlayers;
    }
}
