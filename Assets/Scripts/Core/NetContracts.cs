using System;
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    public enum NetState { Offline, Connecting, Online, Failed }

    /// <summary>How many of each thing the shared world replicates at once.</summary>
    public static class NetLimits
    {
        /// <summary>Enemies in the snapshot. Past this, the newest wait until older ones are gone.</summary>
        public const int Enemies = 24;
        /// <summary>Pickups in the snapshot. Past this, the newest wait the same way.</summary>
        public const int Pickups = 16;
    }

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
        /// <summary>Invite code ("ABCD"), or null for quick match.</summary>
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
        /// <summary>The enemy's network id (<see cref="NetIds"/>).</summary>
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
        /// <summary>The pickup's network id (<see cref="NetIds"/>).</summary>
        public readonly int Id;
        /// <summary>What it was, so the winner can apply it even if it never saw the pickup itself.</summary>
        public readonly PickupDef Def;
        /// <summary>True on the winner's client.</summary>
        public readonly bool Mine;

        public PickupGrant(int id, PickupDef def, bool mine)
        {
            Id = id;
            Def = def;
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
        /// <summary>Authority only: a player touched a pickup. The first claim wins.</summary>
        void OnPickupClaimed(int id, int playerId);
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
        /// <summary>Our player's id in the session (-1 before we're in).</summary>
        int LocalPlayerId { get; }
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
        /// <summary>The mission being played (<see cref="MissionDef.NetId"/>), so everyone loads the same map.</summary>
        int Mission { get; set; }
        float CamX { get; set; }
        int Score { get; set; }
        bool Victory { get; set; }
        int NextSpawn { get; set; }
        /// <summary>The next id for something spawned mid-game (see <see cref="NetIds"/>).</summary>
        int NextNetId { get; set; }
        void WriteEnemies(List<NetEnemyState> enemies);
        void ReadEnemies(List<NetEnemyState> into);
        /// <summary>The pickups in the authority's world; when one goes from the list, someone collected it.</summary>
        void WritePickups(List<NetPickupState> pickups);
        /// <summary>False when there is no snapshot to read: an empty list then doesn't mean they were all collected.</summary>
        bool ReadPickups(List<NetPickupState> into);

        void SendEnemyShot(in ShotEvent shot); // authority only
        void ReportHit(in HitReport hit);
        void ClaimPickup(int id);
        void GrantPickup(int id, PickupDef def, int playerId); // authority only
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
        /// <summary>Their player's id in the session.</summary>
        int PlayerId { get; }
        /// <summary>Asks the owning client to apply damage to its soldier.</summary>
        void Hurt(int damage);
    }

    /// <summary>One pickup in the authority's snapshot.</summary>
    public struct NetPickupState
    {
        public int Id;        // its NetIds id: a level spawn index, or a mid-game id
        public PickupDef Def; // null for a pickup this build doesn't know
        public Vector2 Pos;
    }

    /// <summary>One enemy in the authority's snapshot.</summary>
    public struct NetEnemyState
    {
        public const int Dying = 1, FacingRight = 2, Moving = 4, Engaged = 8, Grounded = 16;

        public int Id;       // its NetIds id: a level spawn index, or a mid-game id
        public EnemyDef Def; // null for an enemy this build doesn't know
        public int Flags;
        public int Hp;
        public Vector2 Pos;
    }
}
