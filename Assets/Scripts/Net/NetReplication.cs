#if FUSION2
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The replicated side of a session: the <see cref="PlayerNet"/> and <see cref="WorldNet"/> objects, and the
    /// game's <see cref="INetLink"/> onto them. <see cref="NetSession"/> connects and runs the session's life;
    /// this is what the session shares while it's up. The objects' RPCs arrive here and go on to <see cref="Events"/>.
    /// </summary>
    public sealed class NetReplication : INetLink
    {
        WorldNet m_Shared;
        PlayerNet m_Local;
        readonly List<PlayerNet> m_Players = new List<PlayerNet>();
        readonly List<IAvatarLink> m_Remotes = new List<IAvatarLink>();
        bool m_SpawnedWorld, m_WasAuthority;

        /// <summary>Receives the session's events; null outside co-op play.</summary>
        public INetEvents Events { get; set; }

        /// <summary>The shared world and our own soldier both exist.</summary>
        public bool Ready => SharedValid && m_Local != null;
        public int PlayerCount => m_Players.Count;

        bool SharedValid => m_Shared != null && m_Shared.Object != null && m_Shared.Object.IsValid;

        /// <summary>Forgets every replicated object: the session ended, or a new one is starting.</summary>
        public void Reset()
        {
            m_Shared = null;
            m_Local = null;
            m_Players.Clear();
            m_Remotes.Clear();
            m_SpawnedWorld = false;
            m_WasAuthority = false;
        }

        /// <summary>Every frame while the session is online.</summary>
        public void Tick(NetworkRunner runner)
        {
            // The master client opens the shared world (once; if it leaves, the object migrates to the next master).
            if (m_Shared == null && !m_SpawnedWorld && runner.IsSharedModeMasterClient)
            {
                m_SpawnedWorld = true;
                runner.Spawn(NetPrefabs.World);
            }

            if (IsAuthority != m_WasAuthority)
            {
                m_WasAuthority = IsAuthority;
                Log.Net.Info(m_WasAuthority ? "This client now runs the shared world" : "Another client runs the shared world");
            }

            // Two soldiers in the same colours (both picked "red" while joining together): the higher id switches.
            if (m_Local != null)
            {
                foreach (PlayerNet p in m_Players)
                    if (p != m_Local && p.Skin == m_Local.NetSkin && m_Local.PlayerId > p.PlayerId) m_Local.NetSkin = 1 - p.Skin;
            }
        }

        // ------------------------------------------------------------------ the replicated objects check in

        internal void OnPlayerSpawned(PlayerNet p)
        {
            if (!m_Players.Contains(p)) m_Players.Add(p);
            if (!p.HasStateAuthority) return;
            m_Local = p;
            bool redTaken = false;
            foreach (PlayerNet other in m_Players) redTaken |= other != p && other.Skin == 0;
            p.NetSkin = redTaken ? 1 : 0;
        }

        internal void OnPlayerDespawned(PlayerNet p)
        {
            m_Players.Remove(p);
            if (m_Local == p) m_Local = null;
        }

        internal void OnWorldSpawned(WorldNet w) => m_Shared = w;

        internal void OnWorldDespawned(WorldNet w)
        {
            if (m_Shared == w) m_Shared = null;
        }

        // ------------------------------------------------------------------ INetLink

        public bool IsAuthority => SharedValid && m_Shared.HasStateAuthority;
        public int LocalSkin => m_Local != null ? m_Local.Skin : 0;
        public int Round => SharedValid ? m_Shared.Round : 0;

        /// <summary>The other soldiers already playing the same round as us.</summary>
        public IReadOnlyList<IAvatarLink> RemoteAvatars
        {
            get
            {
                m_Remotes.Clear();
                int round = Events != null ? Events.Round : Round;
                foreach (PlayerNet p in m_Players)
                    if (p != m_Local && p.PlayingRound == round) m_Remotes.Add(p);
                return m_Remotes;
            }
        }

        public float CamX
        {
            get => SharedValid ? m_Shared.CamX : 0f;
            set { if (IsAuthority) m_Shared.CamX = value; }
        }

        public int Score
        {
            get => SharedValid ? m_Shared.Score : 0;
            set { if (IsAuthority) m_Shared.Score = value; }
        }

        public bool Victory
        {
            get => SharedValid && m_Shared.Victory;
            set { if (IsAuthority) m_Shared.Victory = value; }
        }

        public int NextSpawn
        {
            get => SharedValid ? m_Shared.NextSpawn : 0;
            set { if (IsAuthority) m_Shared.NextSpawn = value; }
        }

        public int NextNetId
        {
            get => SharedValid ? m_Shared.NextNetId : 0;
            set { if (IsAuthority) m_Shared.NextNetId = value; }
        }

        public bool IsTaken(int spawnIndex) =>
            SharedValid && spawnIndex >= 0 && spawnIndex < 64 && (m_Shared.Taken & (1L << spawnIndex)) != 0;

        public void WriteEnemies(List<NetEnemyState> enemies)
        {
            if (!IsAuthority) return;
            NetworkArray<NetEnemy> slots = m_Shared.Enemies;
            int n = 0;
            foreach (NetEnemyState e in enemies)
            {
                if (n == slots.Length) break; // more than fit: the rest show up once earlier ones are gone
                slots.Set(n++, new NetEnemy
                {
                    Ref = (short)(e.Id + 1),
                    Kind = e.Def.Id,
                    Flags = (byte)e.Flags,
                    Hp = (short)e.Hp,
                    Pos = e.Pos,
                });
            }
            for (; n < slots.Length; n++) slots.Set(n, default);
        }

        public void ReadEnemies(List<NetEnemyState> into)
        {
            into.Clear();
            if (!SharedValid) return;
            NetworkArray<NetEnemy> slots = m_Shared.Enemies;
            for (int i = 0; i < slots.Length; i++)
            {
                NetEnemy e = slots.Get(i);
                if (e.Ref > 0) into.Add(new NetEnemyState { Id = e.Ref - 1, Def = Content.Enemy(e.Kind), Flags = e.Flags, Hp = e.Hp, Pos = e.Pos });
            }
        }

        public void SendAvatar(Vector2 pos, Vector2 vel, int poseFlags, int hp)
        {
            if (m_Local == null || !m_Local.Object.IsValid) return;
            m_Local.NetPos = pos;
            m_Local.NetVel = vel;
            m_Local.NetPose = poseFlags;
            m_Local.NetHp = hp;
            m_Local.NetRound = (Events != null ? Events.Round : Round) + 1;
        }

        public void SendShot(in ShotEvent shot)
        {
            if (m_Local != null && m_Local.Object.IsValid) m_Local.RPC_Shot(shot.Weapon.Id, shot.Origin, shot.Velocity);
        }

        public void SendEnemyShot(in ShotEvent shot)
        {
            if (IsAuthority) m_Shared.RPC_EnemyShot(shot.Weapon.Id, shot.Origin, shot.Velocity);
        }

        public void ReportHit(in HitReport hit)
        {
            if (SharedValid) m_Shared.RPC_Hit((short)hit.EnemyId, (byte)Mathf.Clamp(hit.Damage, 0, 255));
        }

        public void ClaimPickup(int spawnIndex)
        {
            if (SharedValid) m_Shared.RPC_Claim((short)spawnIndex);
        }

        public void RequestRestart()
        {
            if (SharedValid) m_Shared.RPC_RequestRestart();
        }
    }
}
#endif
