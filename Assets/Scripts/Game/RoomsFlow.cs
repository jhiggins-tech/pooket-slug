using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The house's outer rooms (<c>docs/design/lobby.md</c>, The outer rooms): each its own screen behind a door. The den
    /// opens one, and a way in a room takes you on to the next, or back to the house.
    /// </summary>
    public sealed class RoomsFlow
    {
        readonly Game m_Game;
        readonly RoomFlow m_Room;

        public RoomsFlow(Game game, Transform parent)
        {
            m_Game = game;
            m_Room = new RoomFlow(game, parent);
        }

        public bool Open => m_Room.Open;

        /// <summary>Opens a room from the house: the den's side door, the trapdoor, or one of the loft's doors.</summary>
        public void Open_(RoomId room, RoomWay way)
        {
            m_Game.Den.Hide(); // the room is over the den
            m_Room.Show(room, way);
        }

        /// <summary>You used a way: it leads to a room, or back to the house (when <paramref name="to"/> is None).</summary>
        public void Go(RoomId from, RoomId to, RoomWay by)
        {
            m_Room.Hide();
            if (to == RoomId.None)
            {
                // back to the house: the den takes over again
                m_Game.Den.RoomClosed();
                return;
            }
            m_Room.Show(to, by);
        }

        public void Tick(MenuInput keys, SoldierInput soldier) => m_Room.Tick(keys, soldier);
        public void Move(float dt) => m_Room.Move(dt);
    }
}
