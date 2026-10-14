using System;
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    public enum NetState { Offline, Connecting, Online, Failed }

    /// <summary>
    /// Lobby-side control of an online co-op session. The Photon Fusion implementation lives in
    /// <c>Net/NetSession.cs</c> and only compiles when the (privately fetched) SDK is in the project, so
    /// the rest of the game never references Fusion types and still builds solo without it.
    /// </summary>
    public interface INetSession
    {
        NetState State { get; }
        /// <summary>What is happening, or why it failed, in HUD-ready words.</summary>
        string Status { get; }
        /// <summary>Invite code ("ABCD-EU"), or null for quick match.</summary>
        string RoomCode { get; }
        /// <summary>Connected, our soldier and the shared world exist: a co-op <see cref="World"/> can be built.</summary>
        bool Ready { get; }
        int PlayerCount { get; }
        int PingMs { get; }
        INetLink Link { get; }
        /// <summary>Receives the session's events (remote shots, hits, pickups); null outside co-op play.</summary>
        World World { set; }

        void Host();
        void Join(string code);
        void QuickMatch();
        void Leave();
    }

    public static class NetSessions
    {
        /// <summary>Set by the Fusion implementation when it is compiled in; null means no online play.</summary>
        public static Func<GameObject, INetSession> Factory;
    }

    /// <summary>
    /// What the game needs from an online co-op session. Implemented with Photon Fusion (Shared Mode) in
    /// <c>Net/NetSession.cs</c>; the gameplay code only talks to this interface.
    ///
    /// Authority model: every client simulates its own soldier. The room's master client ("authority")
    /// simulates enemies, pickups, score and the shared camera scroll; the others mirror its snapshot.
    /// Hits on enemies are reported to the authority, damage to a soldier is applied by its owner.
    /// </summary>
    public interface INetLink
    {
        bool IsAuthority { get; }
        int LocalSkin { get; }
        int Round { get; }
        IReadOnlyList<IAvatarLink> RemoteAvatars { get; }

        // Shared world state: written by the authority, read by everyone else.
        float CamX { get; set; }
        int Score { get; set; }
        bool Victory { get; set; }
        int NextSpawn { get; set; }
        bool IsTaken(int spawnIndex);
        void WriteEnemies(List<Enemy> enemies);
        void ReadEnemies(List<NetEnemyState> into);

        // Local soldier -> network.
        void SendAvatar(Vector2 pos, Vector2 vel, int poseFlags, int hp);
        void SendShot(WeaponDef weapon, Vector2 origin, Vector2 vel);

        // Events.
        void SendEnemyShot(WeaponDef weapon, Vector2 origin, Vector2 vel); // authority only
        void ReportHit(int enemyId, int damage);
        void ClaimPickup(int spawnIndex);
        void RequestRestart();
    }

    /// <summary>A remote player's replicated soldier.</summary>
    public interface IAvatarLink
    {
        Vector2 Pos { get; }
        Vector2 Vel { get; }
        int PoseFlags { get; }
        int Hp { get; }
        int Skin { get; }
        /// <summary>Asks the owning client to apply damage to its soldier.</summary>
        void Hurt(int damage);
    }

    /// <summary>One enemy in the authority's snapshot.</summary>
    public struct NetEnemyState
    {
        public const int Dying = 1, FacingRight = 2, Moving = 4, Engaged = 8, Grounded = 16;

        public int Id;      // spawn index in Level.Spawns
        public EnemyDef Def; // null for an enemy this build doesn't know
        public int Flags;
        public int Hp;
        public Vector2 Pos;
    }
}
