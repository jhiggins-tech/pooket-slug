using System;

namespace PooketSlug
{
    /// <summary>
    /// Network identities for the things in the shared world. A level spawn's id is its index in
    /// <see cref="Level.Spawns"/>, so every client knows it without a message. Things that appear mid-game
    /// (drops, reinforcements, a called-in tank) get the ids after those, from a counter the authority keeps
    /// in the shared world, so a new authority carries on where the old one stopped.
    /// </summary>
    public sealed class NetIds
    {
        /// <summary>The largest id: the co-op snapshot stores id + 1 in a 16-bit field.</summary>
        public const int MaxId = short.MaxValue - 1;

        readonly int m_LevelSpawns;
        int m_Next;

        public NetIds(int levelSpawns)
        {
            m_LevelSpawns = levelSpawns;
            m_Next = levelSpawns;
        }

        /// <summary>The next id it will give out.</summary>
        public int Next => m_Next;

        public bool IsLevelSpawn(int id) => id >= 0 && id < m_LevelSpawns;

        /// <summary>A new id for something spawned mid-game, or -1 when they've run out (it then stays local).</summary>
        public int Allocate() => m_Next <= MaxId ? m_Next++ : -1;

        /// <summary>Adopts the shared counter when taking over as the authority. It never goes backwards.</summary>
        public void CatchUp(int sharedNext) => m_Next = Math.Max(m_Next, sharedNext);
    }
}
