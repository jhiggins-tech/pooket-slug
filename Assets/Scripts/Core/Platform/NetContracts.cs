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

        /// <summary>Opens a room; a <paramref name="listed"/> one shows in the room browser for anyone to join or watch.</summary>
        void Host(bool listed);
        void Join(string code);
        /// <summary>Joins a listed room with a free seat, or opens a listed one for the next player to find.</summary>
        void QuickMatch();
        void Leave();

        // ------------------------------------------------------------------ rooms: the browser and watching

        /// <summary>We're watching this session, with no soldier of our own.</summary>
        bool Watching { get; }
        /// <summary>People watching our room.</summary>
        int Watchers { get; }
        /// <summary>Our room shows in the room browser. The host can change it.</summary>
        bool Listed { get; set; }
        /// <summary>Listening for listed rooms (the <c>Rooms</c> list below), with no room of our own.</summary>
        bool Browsing { get; }
        /// <summary>The listed rooms, while browsing (empty until Photon's first list).</summary>
        IReadOnlyList<RoomListing> Rooms { get; }
        void Browse();
        void StopBrowsing();
        /// <summary>Watches a listed room: no soldier, the shared world as the players see it.</summary>
        void Watch(string code);
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
        /// <summary>Every other player in the session, playing or in the den (the lobby).</summary>
        IReadOnlyList<IAvatarLink> Partners { get; }
        /// <summary>Our soldier, and our tank: its flags (<see cref="TankPose.Pack"/>, 0 for none) and where it is. It takes us out of the den.</summary>
        void SendAvatar(SoldierDef soldier, Vector2 pos, Vector2 vel, int poseFlags, int hp, int tankFlags, Vector2 tankPos);
        /// <summary>Where we are in the den: who's on our seat of the couch (null for nobody) and <see cref="Den.Flags"/>.</summary>
        void SendDen(SoldierDef soldier, int flags);
        /// <summary>Our taps in a co-op cutscene (<see cref="CutsceneSync.Pack"/>); 0 when we're in none.</summary>
        void SendScene(int taps);
        /// <summary>Which copies of the scenes we play (<see cref="SceneLibrary.Versions"/>), so both phones play the same.</summary>
        void SendSceneVersions(int versions);
        void SendShot(in ShotEvent shot);
        /// <summary>Our own points: the kills our soldier made (the shared score is everyone's, <see cref="IWorldChannel.Score"/>).</summary>
        void SendScore(int points);
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
        /// <summary>The boss's own state beyond its enemy snapshot: where its turret points and its shot.</summary>
        void WriteBoss(in NetBossState boss);
        /// <summary>False until the authority has written one.</summary>
        bool ReadBoss(out NetBossState boss);

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
        /// <summary>Their own points (<see cref="ISoldierChannel.SendScore"/>).</summary>
        int Score { get; }
        int Skin { get; }
        /// <summary>Their tank (<see cref="TankPose"/>, packed; 0 for none) and where it is.</summary>
        int TankFlags { get; }
        Vector2 TankPos { get; }
        /// <summary>Their player's id in the session.</summary>
        int PlayerId { get; }
        /// <summary>Where they are in the den (<see cref="Den.Flags"/>); 0 once they're playing.</summary>
        int DenFlags { get; }
        /// <summary>Their taps in a co-op cutscene (<see cref="CutsceneSync"/>), 0 when they're in none.</summary>
        int SceneTaps { get; }
        /// <summary>Which copies of the scenes they play (<see cref="SceneLibrary.Versions"/>).</summary>
        int SceneVersions { get; }
        /// <summary>They're in a mission (any round), not in the den.</summary>
        bool Playing { get; }
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
        /// <summary>kie's tank is wearing its hologram look, among its copies.</summary>
        public const int Disguised = 32;

        public int Id;       // its NetIds id: a level spawn index, or a mid-game id
        public EnemyDef Def; // null for an enemy this build doesn't know
        public int Flags;
        public int Hp;
        public Vector2 Pos;
    }

    /// <summary>
    /// The boss tank's own replicated state, in its network object: what the enemy snapshot doesn't carry. A
    /// follower's tank swings, charges and kicks with it; the shells themselves come as enemy shots.
    /// </summary>
    public struct NetBossState
    {
        /// <summary>What it's doing (the tank's own phases, from 1, or the pilot's <c>Pilot.NetPhase</c>; 0 is no boss state yet).</summary>
        public int Phase;
        /// <summary>The turret's angle, degrees up from facing right.</summary>
        public float Angle;
        /// <summary>How far the shot has charged, 0 to 1, and how hard it'll be thrown.</summary>
        public float Charge, Power;
        /// <summary>Where the charging shot will come down.</summary>
        public Vector2 Landing;
        /// <summary>Shots fired so far (it wraps): a change means it just fired.</summary>
        public int Shots;
        /// <summary>kie's roulette (<see cref="KieBoss"/>): spinning or holding what it stole, and whether the shot charging lands anywhere.</summary>
        public int KieState;
        /// <summary>How close kie's tank is to dropping what it stole, 0 to 1.</summary>
        public float KieProgress;
        /// <summary>Times it has dropped what it stole (it wraps): a change means each screen's soldier gets theirs back.</summary>
        public int KieDrops;
    }
}
