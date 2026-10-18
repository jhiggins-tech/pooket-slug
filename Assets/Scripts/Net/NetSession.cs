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
    /// This side connects: joining the one co-op room (<see cref="CoopRoom"/>) to play or watch, failures and the
    /// <c>NetworkRunner</c>'s life (Fusion's).
    /// What the session shares while it's up is <see cref="NetReplication"/>.
    /// </summary>
    public sealed class NetSession : MonoBehaviour, INetSession, INetworkRunnerCallbacks
    {
        const int MaxPlayers = Tuning.Coop.MaxPlayers;
        /// <summary>Seconds between the master's updates of the room's listing.</summary>
        const float ListingEvery = 1f;

        public static NetSession Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => NetSessions.Factory = host => host.AddComponent<NetSession>();

        NetworkRunner m_Runner;
        readonly NetReplication m_Replication = new NetReplication();
        readonly MasterWatch m_MasterWatch = new MasterWatch();

        // watching, and the room's listing (which a phone arriving reads to know whether it plays or watches)
        bool m_Watching;
        float m_ListingT;
        readonly WatcherCount m_Watchers = new WatcherCount();
        RoomListing m_Listed;
        bool m_ListingSent;

        public NetState State { get; private set; }
        public string Status { get; private set; } = "";
        public string RoomCode { get; private set; }
        public INetEvents Events { set => m_Replication.Events = value; }
        public INetLink Link => m_Replication;
        public bool Ready => State == NetState.Online && m_Replication.Ready;
        public int PlayerCount => m_Replication.PlayerCount;
        public bool Watching => m_Watching && State == NetState.Online;

        public int Watchers => State == NetState.Online ? m_Watchers.Shown : 0;

        int PeopleWithoutSoldiers =>
            m_Runner != null && m_Runner.IsRunning && m_Runner.SessionInfo.IsValid
                ? Mathf.Max(0, m_Runner.SessionInfo.PlayerCount - m_Replication.PlayerCount) : 0;

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

        // ------------------------------------------------------------------ the room

        public void JoinRoom(string room, Func<int, bool> cleared)
        {
            if (State == NetState.Connecting || State == NetState.Online) return;
            string name = CoopRoom.NameFor(room);
            Log.Net.Info($"Joining room {name}"); // the co-op test presses CO-OP until it sees this
            Connect(name, cleared ?? (_ => false));
        }

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

        static FusionAppSettings AppSettings()
        {
            FusionAppSettings settings = PhotonAppSettings.TryGetGlobal(out PhotonAppSettings global) && global.AppSettings != null
                ? global.AppSettings.GetCopy()
                : new FusionAppSettings();
            settings.AppIdFusion = NetConfig.AppId;
            settings.AppVersion = Application.version; // only identical builds meet
            settings.FixedRegion = NetConfig.Region;   // every game is in one region, so a room is just a name
            settings.UseNameServer = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            settings.Protocol = Photon.Client.ConnectionProtocol.WebSocketSecure; // browsers only have secure WebSockets
#endif
            return settings;
        }

        /// <summary>A room's listing as session properties. Every key is given when the room opens: only those can be updated.</summary>
        static Dictionary<string, SessionProperty> Properties(in RoomListing r) => new Dictionary<string, SessionProperty>
        {
            [RoomBrowser.MissionKey] = r.Mission,
            [RoomBrowser.SoldiersKey] = r.Soldiers,
            [RoomBrowser.WatchingKey] = r.Watching,
            [RoomBrowser.UnderWayKey] = r.UnderWay ? 1 : 0,
            [RoomBrowser.HostKey] = r.Host,
        };

        static int Prop(SessionInfo info, string key, int fallback) =>
            info.Properties != null && info.Properties.TryGetValue(key, out SessionProperty p) && p.IsInt ? (int)p : fallback;

        static RoomListing ListingOf(SessionInfo info) => new RoomListing
        {
            Name = info.Name,
            Mission = Prop(info, RoomBrowser.MissionKey, 0),
            Soldiers = Prop(info, RoomBrowser.SoldiersKey, 0),
            Watching = Prop(info, RoomBrowser.WatchingKey, 0),
            UnderWay = Prop(info, RoomBrowser.UnderWayKey, 0) != 0,
            Host = Prop(info, RoomBrowser.HostKey, -1),
        };

        async void Connect(string sessionName, Func<int, bool> cleared)
        {
            if (State == NetState.Online || State == NetState.Connecting) return;
            if (!NetConfig.Configured)
            {
                Fail("ONLINE PLAY IS NOT SET UP IN THIS BUILD");
                return;
            }
            NetPrefabs.Register();

            State = NetState.Connecting;
            Status = "JOINING THE ROOM...";
            RoomCode = null;
            m_Watching = false;
            m_ListingSent = false;
            m_Replication.Reset(); // before StartGame: a joiner meets the others' objects while it's still starting

            var go = new GameObject("Network Runner");
            var runner = go.AddComponent<NetworkRunner>();
            runner.ProvideInput = false;
            runner.AddCallbacks(this);
            m_Runner = runner;

            FusionAppSettings settings = AppSettings();

            bool ok = false;
            ShutdownReason reason = ShutdownReason.Error;
            try
            {
                StartGameResult result = await runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = sessionName,
                    PlayerCount = RoomBrowser.MaxPeople, // the players, and room for people watching
                    IsVisible = false, // nobody browses for it: everyone joins it by name
                    SessionProperties = Properties(new RoomListing { Host = -1 }),
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
            // Both seats taken: watch, if this phone has cleared the mission (no spoilers), or say it's full.
            RoomListing there = ListingOf(runner.SessionInfo);
            switch (RoomBrowser.ActionFor(there, cleared))
            {
                case RoomAction.Watch:
                    m_Watching = true;
                    m_Replication.Watching = true; // before anything spawns: a watcher never spawns the world's objects
                    break;
                case RoomAction.Locked:
                    Log.Net.Warn($"{sessionName} has {there.Soldiers} players, on a mission not cleared here; leaving");
                    Leave();
                    Fail("BOTH SEATS ARE TAKEN. CLEAR THEIR MISSION TO WATCH");
                    return;
                case RoomAction.Full:
                    Log.Net.Warn($"{sessionName} has {there.Soldiers} players and {there.Watching} watching; leaving");
                    Leave();
                    Fail("THE ROOM IS FULL");
                    return;
            }

            string sessionRegion = runner.SessionInfo.Region;
            RoomCode = runner.SessionInfo.Name;
            State = NetState.Online;
            Status = "";
            Log.Net.Info($"Online in region {sessionRegion}: room {RoomCode}{(m_Watching ? ", watching" : "")}, " +
                      $"{there.Soldiers}/{MaxPlayers} players, we are {runner.LocalPlayer}{(runner.IsSharedModeMasterClient ? " (master client)" : "")}");
            if (!m_Watching) runner.Spawn(NetPrefabs.Player, inputAuthority: runner.LocalPlayer);
        }

        void Update()
        {
            if (State != NetState.Online)
            {
                m_Watchers.Reset();
                return;
            }
            if (m_Runner == null || m_Runner.IsShutdown)
            {
                Fail("DISCONNECTED");
                return;
            }
            if (!m_Runner.IsRunning) return;
            m_Replication.Tick(m_Runner);
            m_Watchers.Tick(Time.unscaledDeltaTime, PeopleWithoutSoldiers);
            if (m_Watching) KeepOutOfCharge();
            else WatchMaster();
            if (m_Runner != null && m_Runner.IsSharedModeMasterClient) KeepListing();
        }

        /// <summary>
        /// Someone watching never runs the shared world (they have no soldier to play it with): if Photon makes us the
        /// master (the host left), hand it to a player, or leave when nobody's playing any more.
        /// </summary>
        void KeepOutOfCharge()
        {
            if (!m_Runner.IsSharedModeMasterClient) return;
            if (m_Replication.FirstSoldier(out PlayerRef soldier))
            {
                Log.Net.Info($"Watching, and made the master client: handing it to {soldier}");
                m_Runner.SetMasterClient(soldier);
                return;
            }
            Log.Net.Info("Nobody is playing any more; leaving");
            Leave();
            Fail("THE GAME ENDED");
        }

        /// <summary>The master keeps the room's listing up to date: a phone arriving reads it to play or watch.</summary>
        void KeepListing()
        {
            m_ListingT -= Time.unscaledDeltaTime;
            if (m_ListingT > 0f) return;
            m_ListingT = ListingEvery;
            if (!m_Runner.SessionInfo.IsValid) return;
            RoomListing now = m_Replication.Listing(m_Runner);
            now.Name = m_Runner.SessionInfo.Name;
            now.Watching = Watchers;
            if (m_ListingSent && now.Mission == m_Listed.Mission && now.Soldiers == m_Listed.Soldiers && now.Watching == m_Listed.Watching &&
                now.UnderWay == m_Listed.UnderWay && now.Host == m_Listed.Host) return;
            if (m_ListingSent && now.Watching != m_Listed.Watching) Log.Net.Info($"{now.Watching} watching room {now.Name}");
            m_Listed = now;
            m_ListingSent = true;
            m_Runner.SessionInfo.UpdateCustomProperties(Properties(now));
            Log.Net.Debug($"Listing: {now.Name} mission {now.Mission}, {now.Soldiers} playing, {now.Watching} watching{(now.UnderWay ? ", under way" : "")}");
        }

        /// <summary>
        /// A host whose phone went to the background stops the shared world for everyone until Photon times them out:
        /// once their beat has stopped, ask to run it ourselves (<see cref="MasterWatch"/>).
        /// </summary>
        void WatchMaster()
        {
            PlayerRef master = m_Runner.GetMasterClient();
            bool canTakeOver = m_Replication.Ready && !m_Runner.IsSharedModeMasterClient;
            float wasQuiet = m_MasterWatch.Quiet;
            bool ask = m_MasterWatch.Tick(Time.unscaledDeltaTime, master.PlayerId, m_Replication.BeatOf(master), canTakeOver);
            if (canTakeOver && wasQuiet < 1f && m_MasterWatch.Quiet >= 1f) Log.Net.Info($"{master} has gone quiet (its beat stopped a second ago)");
            if (!ask) return;
            Log.Net.Info($"{master} has been quiet for {m_MasterWatch.Quiet:0.0} s: asking to run the shared world");
            m_Runner.RequestMasterClientReassign();
        }

        void Fail(string why) => Clear(NetState.Failed, why);

        void Clear(NetState state, string status)
        {
            m_Runner = null;
            m_Watching = false;
            m_Replication.Reset();
            m_Replication.Watching = false;
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
