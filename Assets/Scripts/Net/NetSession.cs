#if FUSION2
using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Online co-op with Photon Fusion 2 in Shared Mode (the topology Fusion supports for WebGL): no host
    /// process, every client simulates its own soldier, and the room's master client runs the shared world.
    /// Bridges the replicated <see cref="PlayerNet"/> / <see cref="WorldNet"/> objects to the game's
    /// <see cref="INetLink"/>, and owns the <see cref="NetworkRunner"/> lifecycle.
    /// </summary>
    public sealed class NetSession : MonoBehaviour, INetSession, INetLink, INetworkRunnerCallbacks
    {
        const int MaxPlayers = Tuning.Coop.MaxPlayers;

        static readonly Rng s_CodeRng = new Rng(Rng.NewSeed());

        public static NetSession Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => NetSessions.Factory = host => host.AddComponent<NetSession>();

        NetworkRunner m_Runner;
        WorldNet m_Shared;
        PlayerNet m_Local;
        readonly List<PlayerNet> m_Players = new List<PlayerNet>();
        readonly List<IAvatarLink> m_Remotes = new List<IAvatarLink>();
        bool m_SpawnedWorld, m_WasAuthority;

        public NetState State { get; private set; }
        public string Status { get; private set; } = "";
        public string RoomCode { get; private set; }
        public World World { get; set; }
        public INetLink Link => this;
        public bool Ready => State == NetState.Online && SharedValid && m_Local != null;
        public int PlayerCount => m_Players.Count;

        public int PingMs =>
            m_Runner != null && m_Runner.IsRunning ? Mathf.RoundToInt((float)(m_Runner.GetPlayerRtt(m_Runner.LocalPlayer) * 1000.0)) : 0;

        bool SharedValid => m_Shared != null && m_Shared.Object != null && m_Shared.Object.IsValid;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Leave();
        }

        // ------------------------------------------------------------------ lobby

        public void Host() => Connect(InviteCode.NewName(s_CodeRng), null, false, false);

        /// <summary>Joins by invite code: "ABCD-EU" is session "ABCD" in Photon region "eu".</summary>
        public void Join(string code)
        {
            if (State == NetState.Connecting || State == NetState.Online) return;
            if (!InviteCode.TryParse(code, out string name, out string region))
            {
                Fail("TYPE A ROOM CODE LIKE ABCD-EU");
                return;
            }
            Connect(name, region, false, true);
        }

        /// <summary>Joins any open public game (or opens one) in the configured region, or the best by ping.</summary>
        public void QuickMatch() => Connect(null, string.IsNullOrEmpty(NetConfig.Region) ? null : NetConfig.Region, true, false);

        public void Leave()
        {
            NetworkRunner runner = m_Runner;
            Clear(NetState.Offline, "");
            Dispose(runner);
        }

        static void Dispose(NetworkRunner runner)
        {
            if (runner == null) return; // also true once Unity has destroyed it
            if (!runner.IsShutdown) runner.Shutdown(); // destroys its GameObject when done
            else Destroy(runner.gameObject);
        }

        async void Connect(string sessionName, string region, bool visible, bool joining)
        {
            if (State == NetState.Connecting || State == NetState.Online) return;
            if (!NetConfig.Configured)
            {
                Fail("ONLINE PLAY IS NOT SET UP IN THIS BUILD");
                return;
            }
            NetPrefabs.Register();

            State = NetState.Connecting;
            Status = sessionName == null ? "LOOKING FOR A GAME..." : joining ? "JOINING " + sessionName + "..." : "OPENING A ROOM...";
            RoomCode = null;
            m_SpawnedWorld = false;

            var go = new GameObject("Network Runner");
            var runner = go.AddComponent<NetworkRunner>();
            runner.ProvideInput = false;
            runner.AddCallbacks(this);
            m_Runner = runner;

            FusionAppSettings settings = PhotonAppSettings.TryGetGlobal(out PhotonAppSettings global) && global.AppSettings != null
                ? global.AppSettings.GetCopy()
                : new FusionAppSettings();
            settings.AppIdFusion = NetConfig.AppId;
            settings.AppVersion = Application.version; // only identical builds meet
            settings.FixedRegion = region ?? "";       // empty = best region by ping
            settings.UseNameServer = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            settings.Protocol = Photon.Client.ConnectionProtocol.WebSocketSecure; // browsers only have secure WebSockets
#endif

            bool ok = false;
            ShutdownReason reason = ShutdownReason.Error;
            try
            {
                StartGameResult result = await runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = sessionName,
                    PlayerCount = MaxPlayers,
                    IsVisible = visible,
                    SceneManager = go.AddComponent<NetworkSceneManagerDefault>(),
                    ObjectProvider = go.AddComponent<NetworkObjectProviderDefault>(),
                    CustomPhotonAppSettings = settings,
                });
                ok = result.Ok;
                reason = result.ShutdownReason;
            }
            catch (Exception e)
            {
                Log.Net.Exception(e);
            }

            if (runner != m_Runner) return; // cancelled while connecting
            if (!ok)
            {
                Log.Net.Warn($"Couldn't go online: {reason}");
                Fail(Describe(reason));
                Dispose(runner);
                return;
            }
            if (joining && runner.IsSharedModeMasterClient)
            {
                // Shared Mode creates a session that doesn't exist yet: being first in means nobody is hosting that code.
                Log.Net.Warn($"No game called {sessionName} in region {runner.SessionInfo.Region}; leaving");
                Leave();
                Fail("NOBODY IS HOSTING " + InviteCode.Format(sessionName, region));
                return;
            }

            string sessionRegion = runner.SessionInfo.Region;
            RoomCode = visible ? null : InviteCode.Format(runner.SessionInfo.Name, sessionRegion);
            State = NetState.Online;
            Status = "";
            m_WasAuthority = false;
            Log.Net.Info($"Online in region {sessionRegion}: {(RoomCode != null ? "room " + RoomCode : "quick match " + runner.SessionInfo.Name)}, " +
                      $"{runner.SessionInfo.PlayerCount}/{MaxPlayers} players, we are {runner.LocalPlayer}{(runner.IsSharedModeMasterClient ? " (master client)" : "")}");
            runner.Spawn(NetPrefabs.Player, inputAuthority: runner.LocalPlayer);
        }

        void Update()
        {
            if (State != NetState.Online) return;
            if (m_Runner == null || m_Runner.IsShutdown)
            {
                Fail("DISCONNECTED");
                return;
            }
            if (!m_Runner.IsRunning) return;

            // The master client opens the shared world (once; if it leaves, the object migrates to the next master).
            if (m_Shared == null && !m_SpawnedWorld && m_Runner.IsSharedModeMasterClient)
            {
                m_SpawnedWorld = true;
                m_Runner.Spawn(NetPrefabs.World);
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

        void Fail(string why) => Clear(NetState.Failed, why);

        void Clear(NetState state, string status)
        {
            m_Runner = null;
            m_Shared = null;
            m_Local = null;
            m_Players.Clear();
            m_Remotes.Clear();
            State = state;
            Status = status;
            RoomCode = null;
        }

        static string Describe(ShutdownReason reason)
        {
            switch (reason)
            {
                case ShutdownReason.GameNotFound: return "ROOM NOT FOUND";
                case ShutdownReason.GameIsFull: return "THAT GAME IS FULL";
                case ShutdownReason.GameClosed: return "THAT GAME IS CLOSED";
                case ShutdownReason.MaxCcuReached: return "PHOTON IS FULL RIGHT NOW, TRY LATER";
                case ShutdownReason.InvalidRegion: return "UNKNOWN REGION IN THAT CODE";
                case ShutdownReason.InvalidAuthentication:
                case ShutdownReason.CustomAuthenticationFailed: return "PHOTON REJECTED THIS BUILD'S APP ID";
                case ShutdownReason.PhotonCloudTimeout:
                case ShutdownReason.ConnectionTimeout:
                case ShutdownReason.OperationTimeout: return "CONNECTION TIMED OUT";
                case ShutdownReason.ConnectionRefused: return "CONNECTION REFUSED";
                case ShutdownReason.DisconnectedByPluginLogic: return "DISCONNECTED BY THE SERVER";
                default: return "DISCONNECTED (" + reason.ToString().ToUpperInvariant() + ")";
            }
        }

        // ------------------------------------------------------------------ replicated objects

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
                int round = World != null ? World.Round : Round;
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

        public bool IsTaken(int spawnIndex) =>
            SharedValid && spawnIndex >= 0 && spawnIndex < 64 && (m_Shared.Taken & (1L << spawnIndex)) != 0;

        public void WriteEnemies(List<Enemy> enemies)
        {
            if (!IsAuthority) return;
            NetworkArray<NetEnemy> slots = m_Shared.Enemies;
            int n = 0;
            foreach (Enemy e in enemies)
            {
                if (n == slots.Length) break; // more than fit: the rest show up once earlier ones are gone
                if (e.Removed || e.NetId < 0) continue;
                slots.Set(n++, new NetEnemy
                {
                    Ref = (short)(e.NetId + 1),
                    Kind = e.Def.Id,
                    Flags = (byte)e.SnapshotFlags,
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
            m_Local.NetRound = (World != null ? World.Round : Round) + 1;
        }

        public void SendShot(WeaponDef weapon, Vector2 origin, Vector2 vel)
        {
            if (m_Local != null && m_Local.Object.IsValid) m_Local.RPC_Shot(weapon.Id, origin, vel);
        }

        public void SendEnemyShot(WeaponDef weapon, Vector2 origin, Vector2 vel)
        {
            if (IsAuthority) m_Shared.RPC_EnemyShot(weapon.Id, origin, vel);
        }

        public void ReportHit(int enemyId, int damage)
        {
            if (SharedValid) m_Shared.RPC_Hit((short)enemyId, (byte)Mathf.Clamp(damage, 0, 255));
        }

        public void ClaimPickup(int spawnIndex)
        {
            if (SharedValid) m_Shared.RPC_Claim((short)spawnIndex);
        }

        public void RequestRestart()
        {
            if (SharedValid) m_Shared.RPC_RequestRestart();
        }

        // ------------------------------------------------------------------ INetworkRunnerCallbacks

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (runner != m_Runner) return; // we left on purpose
            Log.Net.Warn($"Session ended: {shutdownReason}");
            Fail(Describe(shutdownReason));
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (runner == m_Runner) Log.Net.Warn($"Disconnected from Photon: {reason}");
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (player != runner.LocalPlayer) Log.Net.Info($"{player} joined");
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) => Log.Net.Info($"{player} left");

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
#pragma warning disable CS0618 // still part of the interface, though Fusion marks the type obsolete
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
#pragma warning restore CS0618
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }

    /// <summary>
    /// The project has no prefab assets, so the two network prefabs are built in code too: a template is
    /// assembled under an inactive root (so it never runs itself), baked, and registered in Fusion's prefab
    /// table under a fixed GUID. Every client registers them in the same order, so the ids line up.
    /// </summary>
    static class NetPrefabs
    {
        public static NetworkPrefabId Player, World;
        static GameObject s_Root;

        public static void Register()
        {
            if (s_Root != null) return;
            s_Root = new GameObject("[Network Templates]");
            s_Root.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(s_Root);
            Player = Add<PlayerNet>("PlayerNet", new Guid("7c0f3d1e-52a8-4f0b-9a61-3b1e6d2c0a01"), NetworkObjectFlags.DestroyWhenStateAuthorityLeaves);
            World = Add<WorldNet>("WorldNet", new Guid("7c0f3d1e-52a8-4f0b-9a61-3b1e6d2c0a02"), NetworkObjectFlags.MasterClientObject);
        }

        static NetworkPrefabId Add<T>(string name, Guid guid, NetworkObjectFlags flags) where T : NetworkBehaviour
        {
            var go = new GameObject(name);
            go.transform.SetParent(s_Root.transform, false);
            NetworkObject obj = go.AddComponent<NetworkObject>();
            go.AddComponent<T>();
            obj.Flags |= flags;
            new TemplateBaker().Bake(go);

            NetworkObjectGuid id = guid;
            NetworkPrefabTable table = NetworkProjectConfig.Global.PrefabTable;
            var source = new NetworkPrefabSourceStatic { Object = obj, AssetGuid = id, AssetPath = name };
            return table.TryAddSource(source, out NetworkPrefabId prefabId) ? prefabId : table.GetId(id);
        }

        sealed class TemplateBaker : NetworkObjectBaker
        {
            // The baker wants an execution order for objects that start inactive; ours never run as templates.
            protected override bool TryGetExecutionOrder(MonoBehaviour obj, out int order)
            {
                order = obj is NetworkObject ? 1 : 0;
                return true;
            }
        }
    }
}
#endif
