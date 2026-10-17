using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// The mission screen's flow (<c>docs/design/levels.md</c>, Missions), over the title (MISSIONS) or the den (its
    /// mission board): its cards, the one picked, and whether hard is picked. A pick sets the game's mission and setting.
    /// </summary>
    public sealed class BoardFlow
    {
        readonly Game m_Game;
        bool m_Held;

        public bool Open { get; private set; }
        public bool FromDen { get; private set; }
        public bool Hard { get; private set; }
        public List<MissionCard> Cards { get; private set; }
        public int Pick { get; private set; }

        public BoardFlow(Game game) => m_Game = game;

        /// <summary>
        /// Opens the mission screen on the mission picked last (else the newest reached): from MISSIONS, PLAY goes on
        /// to the den; from the den's mission board, PICK goes back to it with that mission on the board.
        /// </summary>
        public void Show(bool fromDen)
        {
            Cards = MissionBoard.Cards(m_Game.Profile, coop: false);
            Pick = 0;
            for (int i = 0; i < Cards.Count; i++)
                if (!Cards[i].ComingSoon) Pick = i;
            for (int i = 0; i < Cards.Count; i++)
                if (Cards[i].Mission == m_Game.Mission) Pick = i;
            Hard = m_Game.Setting == Setting.Hard && Cards[Pick].HardOpen;
            FromDen = fromDen;
            Open = true;
            Sfx.Play(Sfx.Pickup, 0.4f);
            Log.Game.Info("Mission screen open" + (fromDen ? " (the den's board)" : "") + ": " + Cards.Count + " cards");
        }

        /// <summary>The mission screen's buttons: a card (or left and right), the setting, PLAY (Enter, fire) and back.</summary>
        public void Tick(MenuAction click, MenuInput keys, SoldierInput soldier)
        {
            if (click == MenuAction.Back || keys.Back || keys.Settings)
            {
                Open = false;
                return;
            }
            int pick = Pick;
            if (click >= MenuAction.Card1 && click <= MenuAction.Card4) pick = click - MenuAction.Card1;
            else if (m_Game.StateTime > 0.2f && (soldier.Left || soldier.Right) && !m_Held) pick += soldier.Right ? 1 : -1;
            m_Held = soldier.Left || soldier.Right;
            if (pick != Pick && pick >= 0 && pick < Cards.Count && !Cards[pick].ComingSoon)
            {
                Pick = pick;
                Hard = false;
                Sfx.Play(Sfx.Pickup, 0.3f);
            }
            MissionCard card = Cards[Pick];
            if ((click == MenuAction.Hard || keys.Host) && card.HardOpen)
            {
                Hard = !Hard;
                Sfx.Play(Sfx.Pickup, 0.5f);
            }
            if (click == MenuAction.Play || keys.Start || soldier.FirePressed || soldier.JumpPressed)
            {
                if (card.ComingSoon) return;
                m_Game.Mission = card.Mission;
                m_Game.Setting = MissionBoard.SettingFor(card, Hard);
                Open = false;
                Log.Game.Info("Mission picked: " + m_Game.Mission.Name + " (" + m_Game.Setting + ")");
                if (!FromDen) m_Game.Den.Show(DenMode.Solo); // from the den, its board shows the pick from the next frame
            }
        }
    }
}
