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
        /// <summary>Connected, our soldier and the shared world exist: a co-op world can be built.</summary>
        bool Ready { get; }
        int PlayerCount { get; }
        int PingMs { get; }
        INetLink Link { get; }
        /// <summary>Receives the session's events (shots, hits, pickups, damage); null outside co-op play.</summary>
        INetEvents Events { set; }

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

    // ---------------------------------------------------------------------- what the session sends to the world

    /// <summary>A shot to replay: which weapon, from where, and how fast.</summary>
    public readonly struct ShotEvent
    {
        public readonly WeaponDef Weapon;
        public readonly Vector2 Origin, Velocity;

        public ShotEvent(WeaponDef weapon, Vector2 origin, Vector2 velocity)
        {
            Weapon = weapon;
            Origin = origin;
            Velocity = velocity;
        }
    }

    /// <summary>A follower's shot hit an enemy; the authority applies the damage.</summary>
    public readonly struct HitReport
    {
        /// <summary>The enemy's network id: its index in <see cref="Level.Spawns"/>.</summary>
        public readonly int EnemyId;
        public readonly int Damage;

        public HitReport(int enemyId, int damage)
        {
            EnemyId = enemyId;
            Damage = damage;
        }
    }

    /// <summary>The authority decided who gets a pickup. Everyone removes it; only the winner applies it.</summary>
    public readonly struct PickupGrant
    {
        /// <summary>The pickup's index in <see cref="Level.Spawns"/>.</summary>
        public readonly int SpawnIndex;
        /// <summary>True on the winner's client.</summary>
        public readonly bool Mine;

        public PickupGrant(int spawnIndex, bool mine)
        {
            SpawnIndex = spawnIndex;
            Mine = mine;
        }
    }

    /// <summary>
    /// The world's side of the session's events. Each event is a typed payload, so adding one means a struct,
    /// a method here, and its RPC.
    /// </summary>
    public interface INetEvents
    {
        /// <summary>The round the local world is playing; soldiers in other rounds aren't shown.</summary>
        int Round { get; }
        /// <summary>A partner fired: replay it as a harmless copy.</summary>
        void OnShot(in ShotEvent shot);
        /// <summary>An enemy on the authority fired: replay it. It only hurts our own soldier.</summary>
        void OnEnemyShot(in ShotEvent shot);
        /// <summary>Authority only: a follower hit an enemy.</summary>
        void OnHit(in HitReport hit);
        void OnPickupGranted(in PickupGrant grant);
        /// <summary>The authority's melee or contact damage on our soldier.</summary>
        void OnHurt(int damage);
    }

    // ---------------------------------------------------------------------- what the world sends to the session

    /// <summary>Who we are in the session.</summary>
    public interface ISessionView
    {
        bool IsAuthority { get; }
        int LocalSkin { get; }
        /// <summary>The shared world's round; a restart moves everyone to the next.</summary>
        int Round { get; }
    }

    /// <summary>Our soldier out, the other soldiers in.</summary>
    public interface ISoldierChannel
    {
        IReadOnlyList<IAvatarLink> RemoteAvatars { get; }
        void SendAvatar(SoldierDef soldier, Vector2 pos, Vector2 vel, int poseFlags, int hp);
        void SendShot(in ShotEvent shot);
    }

    /// <summary>The shared world: written by the authority, read by everyone else, plus the events about it.</summary>
    public interface IWorldChannel
    {
        float CamX { get; set; }
        int Score { get; set; }
        bool Victory { get; set; }
        int NextSpawn { get; set; }
        /// <summary>The next id for something spawned mid-game (see <see cref="NetIds"/>).</summary>
        int NextNetId { get; set; }
        bool IsTaken(int spawnIndex);
        void WriteEnemies(List<NetEnemyState> enemies);
        void ReadEnemies(List<NetEnemyState> into);

        void SendEnemyShot(in ShotEvent shot); // authority only
        void ReportHit(in HitReport hit);
        void ClaimPickup(int spawnIndex);
        void RequestRestart();
    }

    /// <summary>
    /// What the game needs from an online co-op session. Implemented with Photon Fusion (Shared Mode) in
    /// <c>Net/NetSession.cs</c>; the gameplay code only talks to these interfaces.
    ///
    /// Authority model: every client simulates its own soldier. The room's master client ("authority")
    /// simulates enemies, pickups, score and the shared camera scroll; the others mirror its snapshot.
    /// Hits on enemies are reported to the authority, damage to a soldier is applied by its owner.
    /// </summary>
    public interface INetLink : ISessionView, ISoldierChannel, IWorldChannel
    {
    }

    /// <summary>A remote player's replicated soldier.</summary>
    public interface IAvatarLink
    {
        /// <summary>Which soldier they play (the Trooper for one this build doesn't know).</summary>
        SoldierDef Soldier { get; }
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

        public int Id;       // spawn index in Level.Spawns
        public EnemyDef Def; // null for an enemy this build doesn't know
        public int Flags;
        public int Hp;
        public Vector2 Pos;
    }
}
