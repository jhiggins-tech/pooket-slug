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
    /// This side connects: hosting, joining, quick match, failures and the <see cref="NetworkRunner"/>'s life.
    /// What the session shares while it's up is <see cref="NetReplication"/>.
    /// </summary>
    public sealed class NetSession : MonoBehaviour, INetSession, INetworkRunnerCallbacks
    {
        const int MaxPlayers = Tuning.Coop.MaxPlayers;

        static readonly Rng s_CodeRng = new Rng(Rng.NewSeed());

        public static NetSession Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => NetSessions.Factory = host => host.AddComponent<NetSession>();

        NetworkRunner m_Runner;
        readonly NetReplication m_Replication = new NetReplication();

        public NetState State { get; private set; }
        public string Status { get; private set; } = "";
        public string RoomCode { get; private set; }
        public INetEvents Events { set => m_Replication.Events = value; }
        public INetLink Link => m_Replication;
        public bool Ready => State == NetState.Online && m_Replication.Ready;
        public int PlayerCount => m_Replication.PlayerCount;
        /// <summary>The replicated objects' way in: their spawns, and the RPCs' events.</summary>
        internal NetReplication Replication => m_Replication;

        public int PingMs =>
            m_Runner != null && m_Runner.IsRunning ? Mathf.RoundToInt((float)(m_Runner.GetPlayerRtt(m_Runner.LocalPlayer) * 1000.0)) : 0;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Leave();
        }

        // ------------------------------------------------------------------ lobby

        public void Host() => Connect(InviteCode.NewName(s_CodeRng), false, false);

        /// <summary>Joins by invite code: "ABCD" is session "ABCD".</summary>
        public void Join(string code)
        {
            if (State == NetState.Connecting || State == NetState.Online) return;
            if (!InviteCode.TryParse(code, out string name))
            {
                Fail("TYPE A ROOM CODE LIKE ABCD");
                return;
            }
            Connect(name, false, true);
        }

        /// <summary>Joins any open public game, or opens one.</summary>
        public void QuickMatch() => Connect(null, true, false);

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

        async void Connect(string sessionName, bool visible, bool joining)
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
            m_Replication.Reset(); // before StartGame: a joiner meets the others' objects while it's still starting

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
            settings.FixedRegion = NetConfig.Region;   // every game is in one region, so a code is just a name
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
                Fail(DescribeConnectFailure(reason));
                Dispose(runner);
                return;
            }
            if (joining && runner.IsSharedModeMasterClient)
            {
                // Shared Mode creates a session that doesn't exist yet: being first in means nobody is hosting that code.
                Log.Net.Warn($"No game called {sessionName} in region {runner.SessionInfo.Region}; leaving");
                Leave();
                Fail("NOBODY IS HOSTING " + sessionName);
                return;
            }

            string sessionRegion = runner.SessionInfo.Region;
            RoomCode = visible ? null : runner.SessionInfo.Name;
            State = NetState.Online;
            Status = "";
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
            m_Replication.Tick(m_Runner);
        }

        void Fail(string why) => Clear(NetState.Failed, why);

        void Clear(NetState state, string status)
        {
            m_Runner = null;
            m_Replication.Reset();
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
                case ShutdownReason.InvalidRegion: return "UNKNOWN PHOTON REGION IN THIS BUILD";
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

        /// <summary>
        /// A failure before the session started. The reasons Photon names keep their message; the vague ones (an
        /// error, a timeout, a refusal) almost always mean Photon couldn't be reached at all.
        /// </summary>
        static string DescribeConnectFailure(ShutdownReason reason)
        {
            switch (reason)
            {
                case ShutdownReason.Error:
                case ShutdownReason.PhotonCloudTimeout:
                case ShutdownReason.ConnectionTimeout:
                case ShutdownReason.OperationTimeout:
                case ShutdownReason.ConnectionRefused:
                    return "COULDN'T REACH PHOTON, CHECK YOUR CONNECTION";
                default:
                    return Describe(reason);
            }
        }

        // ------------------------------------------------------------------ INetworkRunnerCallbacks

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (runner != m_Runner) return; // we left on purpose
            Log.Net.Warn($"Session ended: {shutdownReason}");
            Fail(State == NetState.Connecting ? DescribeConnectFailure(shutdownReason) : Describe(shutdownReason));
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
}
#endif
