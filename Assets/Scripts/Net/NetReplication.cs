#if FUSION2
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The replicated side of a session: the <see cref="PlayerNet"/>, <see cref="WorldNet"/> and <see cref="BossNet"/> objects, and the
    /// game's <see cref="INetLink"/> onto them. <see cref="NetSession"/> connects and runs the session's life;
    /// this is what the session shares while it's up. The objects' RPCs arrive here and go on to <see cref="Events"/>.
    /// </summary>
    public sealed class NetReplication : INetLink
    {
        WorldNet m_Shared;
        BossNet m_Boss;
        PlayerNet m_Local;
        readonly List<PlayerNet> m_Players = new List<PlayerNet>();
        readonly List<IAvatarLink> m_Remotes = new List<IAvatarLink>(), m_Partners = new List<IAvatarLink>();
        bool m_SpawnedWorld, m_SpawnedBoss, m_WasAuthority;

        /// <summary>Receives the session's events; null outside co-op play.</summary>
        public INetEvents Events { get; set; }

        /// <summary>The shared world and our own soldier both exist.</summary>
        public bool Ready => SharedValid && m_Local != null;
        public int PlayerCount => m_Players.Count;

        bool SharedValid => m_Shared != null && m_Shared.Object != null && m_Shared.Object.IsValid;
        bool BossValid => m_Boss != null && m_Boss.Object != null && m_Boss.Object.IsValid;

        /// <summary>Forgets every replicated object: the session ended, or a new one is starting.</summary>
        public void Reset()
        {
            m_Shared = null;
            m_Boss = null;
            m_Local = null;
            m_Players.Clear();
            m_Remotes.Clear();
            m_SpawnedWorld = false;
            m_SpawnedBoss = false;
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
            // And the boss's object beside it (once; it migrates the same way).
            if (m_Boss == null && !m_SpawnedBoss && runner.IsSharedModeMasterClient)
            {
                m_SpawnedBoss = true;
                runner.Spawn(NetPrefabs.Boss);
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

        internal void OnBossSpawned(BossNet b) => m_Boss = b;

        internal void OnBossDespawned(BossNet b)
        {
            if (m_Boss == b) m_Boss = null;
        }

        // ------------------------------------------------------------------ INetLink

        public bool IsAuthority => SharedValid && m_Shared.HasStateAuthority;
        public int LocalSkin => m_Local != null ? m_Local.Skin : 0;
        public int LocalPlayerId => m_Local != null ? m_Local.PlayerId : -1;
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

        public IReadOnlyList<IAvatarLink> Partners
        {
            get
            {
                m_Partners.Clear();
                foreach (PlayerNet p in m_Players)
                    if (p != m_Local && p.Object != null && p.Object.IsValid) m_Partners.Add(p);
                return m_Partners;
            }
        }

        public int Mission
        {
            get => SharedValid ? m_Shared.Mission : 0;
            set { if (IsAuthority) m_Shared.Mission = (byte)value; }
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

        public void WritePickups(List<NetPickupState> pickups)
        {
            if (!IsAuthority) return;
            NetworkArray<NetPickup> slots = m_Shared.Pickups;
            int n = 0;
            foreach (NetPickupState p in pickups)
            {
                if (n == slots.Length) break; // more than fit: the rest show up once earlier ones are collected
                slots.Set(n++, new NetPickup { Ref = (short)(p.Id + 1), Kind = p.Def.Id, Pos = p.Pos });
            }
            for (; n < slots.Length; n++) slots.Set(n, default);
        }

        public bool ReadPickups(List<NetPickupState> into)
        {
            into.Clear();
            if (!SharedValid) return false;
            NetworkArray<NetPickup> slots = m_Shared.Pickups;
            for (int i = 0; i < slots.Length; i++)
            {
                NetPickup p = slots.Get(i);
                if (p.Ref > 0) into.Add(new NetPickupState { Id = p.Ref - 1, Def = Content.Pickup(p.Kind), Pos = p.Pos });
            }
            return true;
        }

        public void WriteBoss(in NetBossState boss)
        {
            if (!IsAuthority || !BossValid || !m_Boss.HasStateAuthority) return;
            m_Boss.State = new NetBoss
            {
                Phase = (byte)boss.Phase,
                Angle = boss.Angle,
                Charge = boss.Charge,
                Power = boss.Power,
                Landing = boss.Landing,
                Shots = (byte)boss.Shots,
                KieState = (byte)boss.KieState,
                KieProgress = boss.KieProgress,
                KieDrops = (byte)boss.KieDrops,
            };
        }

        public bool ReadBoss(out NetBossState boss)
        {
            boss = default;
            if (!BossValid) return false;
            NetBoss b = m_Boss.State;
            if (b.Phase == 0) return false;
            boss = new NetBossState { Phase = b.Phase, Angle = b.Angle, Charge = b.Charge, Power = b.Power, Landing = b.Landing, Shots = b.Shots,
                KieState = b.KieState, KieProgress = b.KieProgress, KieDrops = b.KieDrops };
            return true;
        }

        public void SendAvatar(SoldierDef soldier, Vector2 pos, Vector2 vel, int poseFlags, int hp, int tankFlags, Vector2 tankPos)
        {
            if (m_Local == null || !m_Local.Object.IsValid) return;
            m_Local.NetSoldier = soldier.Id;
            m_Local.NetPos = pos;
            m_Local.NetVel = vel;
            m_Local.NetPose = poseFlags;
            m_Local.NetHp = hp;
            m_Local.NetTank = tankFlags;
            m_Local.NetTankPos = tankPos;
            m_Local.NetRound = (Events != null ? Events.Round : Round) + 1;
            m_Local.NetDen = 0;
        }

        public void SendDen(SoldierDef soldier, int flags)
        {
            if (m_Local == null || !m_Local.Object.IsValid) return;
            if (soldier != null) m_Local.NetSoldier = soldier.Id;
            m_Local.NetDen = (byte)flags;
            m_Local.NetRound = 0; // not playing: partners in a mission don't draw us
        }

        public void SendScene(int taps)
        {
            if (m_Local != null && m_Local.Object.IsValid) m_Local.NetScene = taps;
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

        public void ClaimPickup(int id)
        {
            if (SharedValid) m_Shared.RPC_Claim((short)id);
        }

        public void GrantPickup(int id, PickupDef def, int playerId)
        {
            if (IsAuthority) m_Shared.RPC_Granted((short)id, def.Id, playerId);
        }

        public void RequestRestart()
        {
            if (SharedValid) m_Shared.RPC_RequestRestart();
        }
    }
}
#endif
