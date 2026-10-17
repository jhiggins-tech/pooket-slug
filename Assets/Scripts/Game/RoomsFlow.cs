using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The room browser's flow (<see cref="RoomBrowser"/>): the listed rooms, refreshed twice a second, and JOIN or WATCH
    /// on one. A game can be watched only once its mission's been cleared on this phone, so it spoils nothing.
    /// </summary>
    public sealed class RoomsFlow
    {
        readonly Game m_Game;
        float m_T;

        /// <summary>The rows on the screen.</summary>
        public List<RoomRow> Rows { get; private set; } = new List<RoomRow>();

        public RoomsFlow(Game game) => m_Game = game;

        public void Show()
        {
            m_Game.Net.Browse();
            Rows.Clear();
            m_T = 0f;
            m_Game.SetState(GameState.Rooms);
            Log.Game.Info("Room browser open");
        }

        /// <summary>A mission's been cleared on this phone: its games can be watched without spoilers.</summary>
        bool Cleared(int netId)
        {
            MissionDef m = Missions.ByNetId(netId);
            Profile profile = m_Game.Profile;
            return m != null && (profile.Clears(m) > 0 || profile.Clears(m, Setting.Hard) > 0 || m_Game.Flags.UnlockCoop);
        }

        public void Tick(MenuAction click, MenuInput keys)
        {
            INetSession net = m_Game.Net;
            m_T -= Time.unscaledDeltaTime;
            if (m_T <= 0f)
            {
                m_T = 0.5f;
                Rows = RoomBrowser.Rows(net.Rooms, Cleared);
            }
            if (click == MenuAction.Back || keys.Back)
            {
                net.StopBrowsing();
                m_Game.SetState(GameState.Online);
                return;
            }
            int pick = click >= MenuAction.Room1 && click <= MenuAction.Room4 ? click - MenuAction.Room1 : keys.Start ? 0 : -1;
            if (pick < 0 || pick >= Rows.Count) return;
            RoomRow row = Rows[pick];
            string code = row.Room.Name;
            Log.Game.Info($"Room browser: {RoomBrowser.Button(row.Action)} {code}");
            if (row.Action == RoomAction.Join)
            {
                net.StopBrowsing();
                m_Game.GoOnline(() => net.Join(code));
            }
            else if (row.Action == RoomAction.Watch)
            {
                net.StopBrowsing();
                m_Game.GoOnline(() => net.Watch(code));
            }
            else Sfx.Play(Sfx.Thump, 0.3f);
        }
    }
}
