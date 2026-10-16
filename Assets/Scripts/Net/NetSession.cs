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
        /// <summary>Seconds a quick match looks through the listed rooms before opening one of its own.</summary>
        const float QuickLook = 4f;
        /// <summary>Seconds between the master's updates of the room's listing.</summary>
        const float ListingEvery = 1f;

        static readonly Rng s_CodeRng = new Rng(Rng.NewSeed());

        public static NetSession Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => NetSessions.Factory = host => host.AddComponent<NetSession>();

        NetworkRunner m_Runner;
        readonly NetReplication m_Replication = new NetReplication();
        readonly MasterWatch m_MasterWatch = new MasterWatch();

        // rooms: the browser's lobby connection, quick match's look through it, watching, and our room's listing
        NetworkRunner m_Lobby;
        readonly List<RoomListing> m_Rooms = new List<RoomListing>();
        bool m_ListArrived, m_Quick, m_Watching;
        float m_QuickT, m_ListingT;
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
        public bool Browsing => m_Lobby != null && !m_Quick;
        public IReadOnlyList<RoomListing> Rooms => m_Rooms;

        public int Watchers =>
            State == NetState.Online && m_Runner != null && m_Runner.IsRunning && m_Runner.SessionInfo.IsValid
                ? Mathf.Max(0, m_Runner.SessionInfo.PlayerCount - m_Replication.PlayerCount) : 0;

        public bool Listed
        {
            get => State == NetState.Online && m_Runner != null && m_Runner.IsRunning && m_Runner.SessionInfo.IsValid && m_Runner.SessionInfo.IsVisible;
            set
            {
                if (State != NetState.Online || m_Runner == null || !m_Runner.IsRunning || !m_Runner.IsSharedModeMasterClient) return;
                if (m_Runner.SessionInfo.IsVisible == value) return;
                m_Runner.SessionInfo.IsVisible = value;
                Log.Net.Info(value ? "Room listed: it shows in the room browser" : "Room unlisted: invite only");
            }
        }
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

        public void Host(bool listed) => Connect(InviteCode.NewName(s_CodeRng), listed, false);

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

        /// <summary>
        /// Joins a listed room with a free seat, or opens a listed one for the next player to find. It looks through the
        /// room browser's list first, so it never lands a third soldier in a room that only has space for watchers.
        /// </summary>
        public void QuickMatch()
        {
            if (State == NetState.Connecting || State == NetState.Online) return;
            if (!NetConfig.Configured)
            {
                Fail("ONLINE PLAY IS NOT SET UP IN THIS BUILD");
                return;
            }
            State = NetState.Connecting;
            Status = "LOOKING FOR A GAME...";
            m_Quick = true;
            m_QuickT = 0f;
            StartLobby();
        }

        /// <summary>Watches a listed room: no soldier, the shared world as its players see it.</summary>
        public void Watch(string code)
        {
            if (State == NetState.Connecting || State == NetState.Online) return;
            if (!InviteCode.TryParse(code, out string name)) return;
            StopLobby();
            Connect(name, false, true, watching: true);
        }

        public void Browse()
        {
            if (State == NetState.Connecting || State == NetState.Online || m_Lobby != null) return;
            if (!NetConfig.Configured)
            {
                Fail("ONLINE PLAY IS NOT SET UP IN THIS BUILD");
                return;
            }
            Status = "LOOKING FOR ROOMS...";
            StartLobby();
        }

        public void StopBrowsing()
        {
            if (!m_Quick) StopLobby();
        }

        public void Leave()
        {
            NetworkRunner runner = m_Runner;
            StopLobby();
            m_Quick = false;
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
            settings.FixedRegion = NetConfig.Region;   // every game is in one region, so a code is just a name
            settings.UseNameServer = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            settings.Protocol = Photon.Client.ConnectionProtocol.WebSocketSecure; // browsers only have secure WebSockets
#endif
            return settings;
        }

        /// <summary>A room's listing as session properties. Every key is given when a room opens: only those Photon shows to browsers.</summary>
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

        async void Connect(string sessionName, bool visible, bool joining, bool watching = false)
        {
            if (State == NetState.Online) return;
            if (State == NetState.Connecting && !m_Quick) return; // a quick match connects once it has looked
            if (!NetConfig.Configured)
            {
                Fail("ONLINE PLAY IS NOT SET UP IN THIS BUILD");
                return;
            }
            NetPrefabs.Register();

            m_Quick = false;
            State = NetState.Connecting;
            Status = watching ? "WATCHING " + sessionName + "..." : joining ? "JOINING " + sessionName + "..." : "OPENING A ROOM...";
            RoomCode = null;
            m_Watching = watching;
            m_ListingSent = false;
            m_Replication.Reset(); // before StartGame: a joiner meets the others' objects while it's still starting
            m_Replication.Watching = watching;

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
                    IsVisible = visible,
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
            if (joining && runner.IsSharedModeMasterClient)
            {
                // Shared Mode creates a session that doesn't exist yet: being first in means nobody is hosting that code.
                Log.Net.Warn($"No game called {sessionName} in region {runner.SessionInfo.Region}; leaving");
                Leave();
                Fail("NOBODY IS HOSTING " + sessionName);
                return;
            }

            RoomListing there = ListingOf(runner.SessionInfo);
            if (joining && !watching && there.Soldiers >= MaxPlayers)
            {
                Log.Net.Warn($"{sessionName} already has {there.Soldiers} players; leaving");
                Leave();
                Fail("THAT GAME IS FULL");
                return;
            }
            if (watching && there.Soldiers == 0)
            {
                Log.Net.Warn($"Nobody is playing in {sessionName}; leaving");
                Leave();
                Fail("NOBODY IS PLAYING IN " + sessionName);
                return;
            }

            string sessionRegion = runner.SessionInfo.Region;
            RoomCode = runner.SessionInfo.Name;
            State = NetState.Online;
            Status = "";
            Log.Net.Info($"Online in region {sessionRegion}: room {RoomCode}{(runner.SessionInfo.IsVisible ? " (listed)" : "")}{(watching ? ", watching" : "")}, " +
                      $"{there.Soldiers}/{MaxPlayers} players, we are {runner.LocalPlayer}{(runner.IsSharedModeMasterClient ? " (master client)" : "")}");
            if (!watching) runner.Spawn(NetPrefabs.Player, inputAuthority: runner.LocalPlayer);
        }

        // ------------------------------------------------------------------ the room browser's lobby

        async void StartLobby()
        {
            if (m_Lobby != null) return;
            m_Rooms.Clear();
            m_ListArrived = false;
            var go = new GameObject("Lobby Runner");
            var runner = go.AddComponent<NetworkRunner>();
            runner.ProvideInput = false;
            runner.AddCallbacks(this);
            m_Lobby = runner;
            bool ok = false;
            ShutdownReason reason = ShutdownReason.Error;
            try
            {
                StartGameResult result = await runner.JoinSessionLobby(SessionLobby.Shared, customAppSettings: AppSettings());
                ok = result.Ok;
                reason = result.ShutdownReason;
            }
            catch (Exception e)
            {
                Log.Net.Exception(e);
            }
            if (runner != m_Lobby) return; // stopped while it was connecting
            if (ok)
            {
                Log.Net.Info("Looking through the listed rooms");
                return;
            }
            Log.Net.Warn($"Couldn't list the rooms: {reason}");
            StopLobby();
            if (m_Quick)
            {
                m_Quick = false;
                Fail(DescribeConnectFailure(reason));
            }
            else Status = DescribeConnectFailure(reason);
        }

        void StopLobby()
        {
            NetworkRunner lobby = m_Lobby;
            m_Lobby = null;
            m_Rooms.Clear();
            m_ListArrived = false;
            Dispose(lobby);
        }

        /// <summary>Quick match: once the list is in (or it's taken too long), a room with a free seat, or a new listed one.</summary>
        void TickQuick()
        {
            m_QuickT += Time.unscaledDeltaTime;
            if (!m_ListArrived && m_QuickT < QuickLook) return;
            string seat = null;
            foreach (RoomRow row in RoomBrowser.Rows(m_Rooms, _ => true))
                if (row.Action == RoomAction.Join) { seat = row.Room.Name; break; }
            Log.Net.Info(seat != null ? $"Quick match: {seat} has a free seat" : "Quick match: no free seats, opening a listed room");
            StopLobby();
            if (seat != null) Connect(seat, false, true);
            else Connect(InviteCode.NewName(s_CodeRng), true, false);
        }

        void Update()
        {
            if (m_Quick && State == NetState.Connecting) TickQuick();
            if (State != NetState.Online) return;
            if (m_Runner == null || m_Runner.IsShutdown)
            {
                Fail("DISCONNECTED");
                return;
            }
            if (!m_Runner.IsRunning) return;
            m_Replication.Tick(m_Runner);
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

        /// <summary>The master keeps the room's listing (what the room browser shows) up to date.</summary>
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
            m_Listed = now;
            m_ListingSent = true;
            m_Runner.SessionInfo.UpdateCustomProperties(Properties(now));
            Log.Net.Debug("Listing: " + RoomBrowser.Line(now));
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
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            if (runner != m_Lobby) return;
            m_Rooms.Clear();
            foreach (SessionInfo info in sessionList)
                if (info.IsValid && info.IsVisible && info.IsOpen) m_Rooms.Add(ListingOf(info));
            if (!m_ListArrived) Log.Net.Info($"Listed rooms: {m_Rooms.Count}");
            else Log.Net.Debug($"Listed rooms: {m_Rooms.Count}");
            m_ListArrived = true;
            if (!m_Quick) Status = m_Rooms.Count == 0 ? "NO ROOMS RIGHT NOW" : "";
        }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
    }
}
#endif
