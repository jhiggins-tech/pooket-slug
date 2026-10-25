using System.Collections.Generic;

namespace PooketSlug
{
    /// <summary>
    /// The mission screen's flow (<c>docs/design/levels.md</c>, Missions), over the den (its mission board): its cards,
    /// and the one picked. A pick sets the game's mission. The title is a splash with
    /// no MISSIONS button, so only the den opens it today; the path for opening it from elsewhere is still here.
    /// </summary>
    public sealed class BoardFlow
    {
        readonly Game m_Game;
        bool m_Held;

        public bool Open { get; private set; }
        public bool FromDen { get; private set; }
        public List<MissionCard> Cards { get; private set; }
        public int Pick { get; private set; }

        public BoardFlow(Game game) => m_Game = game;

        /// <summary>
        /// Opens the mission screen on the mission picked last (else the newest reached): from the den's mission board,
        /// PICK goes back to it with that mission on the board; opened from anywhere else, PLAY goes on to the den.
        /// </summary>
        public void Show(bool fromDen)
        {
            Cards = MissionBoard.Cards(m_Game.Profile);
            Pick = 0;
            for (int i = 0; i < Cards.Count; i++)
                if (!Cards[i].ComingSoon) Pick = i;
            for (int i = 0; i < Cards.Count; i++)
                if (Cards[i].Mission == m_Game.Mission) Pick = i;
            FromDen = fromDen;
            Open = true;
            Sfx.Play(Sfx.Pickup, 0.4f);
            Log.Game.Info("Mission screen open" + (fromDen ? " (the den's board)" : "") + ": " + Cards.Count + " cards");
        }

        /// <summary>The mission screen's buttons: a card (or left and right), PLAY (Enter, fire) and back.</summary>
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
                Sfx.Play(Sfx.Pickup, 0.3f);
            }
            MissionCard card = Cards[Pick];
            if (click == MenuAction.Play || keys.Start || soldier.FirePressed || soldier.JumpPressed)
            {
                if (card.ComingSoon) return;
                m_Game.Mission = card.Mission;
                Open = false;
                Log.Game.Info("Mission picked: " + m_Game.Mission.Name);
                if (!FromDen) m_Game.Den.Show(DenMode.Solo); // from the den, its board shows the pick from the next frame
            }
        }
    }
}
