using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PooketSlug
{
    /// <summary>Spawns the game into whatever scene is loaded, so the project needs no scene assets.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindAnyObjectByType<Game>() != null) return;
            var go = new GameObject("Pooket Slug");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Game>();
        }
    }

    public enum GameState { Title, Settings, Online, CodeEntry, Lobby, Playing, GameOver, Victory }

    /// <summary>
    /// Owns the camera, the current <see cref="World"/>, the online session and the player's profile, and runs
    /// the game's flow: title, online menus, play, pause, game over and victory. The HUD and the menus are
    /// views (<see cref="HudView"/>, <see cref="MenuView"/>) that it hands what to show each frame.
    /// </summary>
    public sealed class Game : MonoBehaviour
    {
        Camera m_Camera;
        World m_World;
        GameState m_State;
        float m_StateTime, m_Fps, m_FpsTimer;
        int m_Frames;
        bool m_Paused;

        // online co-op
        INetSession m_Net;      // null when the build has no Photon SDK or AppId
        bool m_Coop;            // m_World is the shared co-op world
        bool m_Unlocked;        // ?unlock=coop: co-op is open for this visit, for testing
        bool m_TankUnlocked;
        float m_OutFor;         // seconds the game has had nobody left in it (out of lives and continues)    // ?unlock=tank: a tank can be called in each mission, for testing
        bool m_TestCrate;       // ?test=crate: a crate just ahead of the start, so the co-op test sees one handed out
        bool m_TestOneUp;       // ?test=1up: a 1-up just ahead of the start
        bool m_TestBoss;        // ?test=boss: a game starts just before the boss's arena (co-op too: the co-op test's boss round)
        string m_Notice, m_StartNote;
        float m_NoticeTime, m_LeaveArmed;

        HudView m_Hud;
        MenuView m_Menu;
        IProfileStore m_Store; // this browser, and the cloud while signed in
        CloudAccount m_Account;
        bool m_AccountShown = true; // the page shows the sign-in button until the game says otherwise
        Options m_Options;
        Profile m_Profile;
        bool m_CoopJustOpened; // this run's win opened co-op
        MissionDef m_Mission = Missions.One; // the mission a solo game plays; a shared game plays the host's
        string m_PageUrl;      // the page's address without its query, for invite links

        /// <summary>Co-op shows only once Mission one has been beaten solo (progressive disclosure).</summary>
        bool CoopOpen => m_Profile.coop || m_Unlocked;
        bool CoopReady => m_Net != null && CoopOpen;

        void Awake()
        {
            string url = Application.absoluteURL;
            Log.Configure(PageQuery.Get(url, "log")); // ?log=net:debug,game:warn for this visit
            Spike.Configure(url); // ?spike=hits for this visit (before the art and sounds, which make what spikes need)
            Art.Init();
            Sfx.Init(gameObject);
            m_Options = OptionsStore.Load();
            ApplyOptions();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(transform, false);
            m_Camera = camGo.AddComponent<Camera>();
            m_Camera.orthographic = true;
            m_Camera.orthographicSize = Tuning.Camera.ViewHeight * 0.5f;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = new Color32(0x6c, 0xa8, 0xe0, 255);
            m_Camera.nearClipPlane = 0.1f;
            m_Camera.farClipPlane = 50f;
            camGo.AddComponent<AudioListener>();

            m_Account = CloudAccount.Create();
            m_Account.CloudProfile += OnCloudProfile;
            m_Store = new CloudProfileStore(new LocalProfileStore(), m_Account);
            m_Profile = m_Store.Load();
            string[] unlocks = (PageQuery.Get(url, "unlock") ?? "").ToLowerInvariant().Split(',');
            m_Unlocked = Array.IndexOf(unlocks, Reveals.Coop) >= 0;
            m_TankUnlocked = Array.IndexOf(unlocks, Reveals.Tank) >= 0;
            string[] tests = (PageQuery.Get(url, "test") ?? "").ToLowerInvariant().Split(',');
            m_TestCrate = Array.IndexOf(tests, "crate") >= 0;
            m_TestOneUp = Array.IndexOf(tests, "1up") >= 0;
            m_TestBoss = Array.IndexOf(tests, "boss") >= 0;

            m_World = NewWorld(m_Mission);
            m_Hud = new HudView(transform);
            m_Menu = new MenuView(transform);
            m_PageUrl = PageUrl();
            SetState(GameState.Title);
            GameInput.TouchActive = Application.isMobilePlatform; // until the player shows otherwise

            if (NetSessions.Factory != null && NetConfig.Configured) m_Net = NetSessions.Factory(gameObject);
            string invite = RoomFromUrl(url);
            if (m_Net != null && !string.IsNullOrEmpty(invite))
            {
                if (CoopOpen)
                {
                    OpenCodeEntry(invite); // the code is filled in: Enter or JOIN
                }
                else
                {
                    // Invite links respect the order: co-op waits until Mission one is beaten, so start that.
                    m_StartNote = "CO-OP OPENS ONCE YOU'VE BEATEN THIS MISSION";
                    StartSolo();
                }
            }
        }

        /// <summary>A world for <paramref name="mission"/>, shared when <paramref name="net"/> is given.</summary>
        World NewWorld(MissionDef mission, INetLink net = null)
        {
            var world = new World(m_Camera, mission, net) { TanksToCall = m_TankUnlocked ? 1 : 0 };
            if (m_TestCrate)
            {
                // Just past where a joining partner appears (1.25 ahead of the start), so whoever runs first takes
                // it before any jump. Only the authority places it; a follower gets it from the snapshot.
                float x = world.Level.PlayerStart.x + 2.75f;
                world.Spawner.SpawnPickup(Content.HeavyMachineGunCrate, new Vector2(x, world.Level.TopSurfaceY(x)));
            }
            if (m_TestOneUp)
            {
                float x = world.Level.PlayerStart.x + 4.5f;
                world.Spawner.SpawnPickup(Content.OneUp, new Vector2(x, world.Level.TopSurfaceY(x)));
            }
            if (m_TestBoss) StartAtBoss(world);
            return world;
        }

        /// <summary>?test=boss: puts the soldier a screen short of the boss, to try boss fights without the run there.</summary>
        static void StartAtBoss(World world)
        {
            foreach (Spawn s in world.Level.Spawns)
            {
                if (Content.EnemyFor(s.Kind)?.Brain != EnemyBrain.Tank) continue;
                StartAt(world, s.Pos.x - 16f);
                return;
            }
        }

        /// <summary>Puts the soldier, and the camera, at <paramref name="x"/>; what's behind is left behind.</summary>
        static void StartAt(World world, float x)
        {
            world.Player.Pos = new Vector2(x, world.Level.TopSurfaceY(x));
            world.View.JumpTo(x + Tuning.Camera.LookAhead);
            world.View.Tick(0f);
            world.Spawner.SpawnAhead();
        }

        void SetState(GameState state)
        {
            m_State = state;
            m_StateTime = 0f;
            m_OutFor = 0f;
        }

        void StartSolo()
        {
            m_Paused = false;
            SetState(GameState.Playing);
            Sfx.Play(Sfx.Start);
            Sfx.Say("mission-start", 0.3f);
        }

        void Restart()
        {
            // Lost after the checkpoint at the boss door: start again there (docs/design/levels.md, Missions).
            float checkpoint = m_State == GameState.GameOver ? m_World.CheckpointX : -1f;
            m_World.Dispose();
            m_World = NewWorld(m_Mission);
            if (checkpoint >= 0f)
            {
                m_World.CheckpointX = checkpoint;
                m_World.QuickBossIntro = true; // the second time: the tank's already in place
                StartAt(m_World, checkpoint);
            }
            m_StartNote = null;
            StartSolo();
        }

        /// <summary>Back to the title screen from a solo game.</summary>
        void ToTitle()
        {
            m_World.Dispose();
            m_World = NewWorld(m_Mission);
            m_Paused = false;
            m_StartNote = null;
            SetState(GameState.Title);
        }

        /// <summary>The cloud's copy, after signing in: take the better of each field and save to both.</summary>
        void OnCloudProfile(Profile cloud)
        {
            m_Profile.Merge(cloud);
            m_Store.Save(m_Profile);
        }

        /// <summary>The title screen's line about the account, or null when the build has no sign-in.</summary>
        string AccountLine()
        {
            if (!m_Account.Available) return null;
            if (m_Account.SignedIn)
                return string.IsNullOrEmpty(m_Account.Name) ? "SIGNED IN · PROGRESS SAVES ONLINE" : "SIGNED IN AS " + m_Account.Name.ToUpperInvariant() + " · PROGRESS SAVES ONLINE";
            return "SIGN IN AT THE TOP RIGHT TO SAVE YOUR PROGRESS ONLINE";
        }

        // ------------------------------------------------------------------ settings

        void ApplyOptions()
        {
            Sfx.Muted = !m_Options.Sound;
            ScrollCamera.Shaking = m_Options.Shake;
        }

        void ChangeOptions(Options options)
        {
            m_Options = options;
            ApplyOptions();
            OptionsStore.Save(options);
        }

        // ------------------------------------------------------------------ online co-op

        void OpenOnline()
        {
            if (m_Profile.MarkSeen(Reveals.Coop)) m_Store.Save(m_Profile);
            SetState(GameState.Online);
        }

        void OpenCodeEntry(string code)
        {
            m_Menu.Code = code;
            SetState(GameState.CodeEntry);
        }

        void JoinWithCode()
        {
            string code = m_Menu.Code.Trim('-', ' ');
            if (code.Length > 0) GoOnline(() => m_Net.Join(code));
        }

        void GoOnline(Action connect)
        {
            m_Notice = null;
            connect();
            SetState(GameState.Lobby);
        }

        /// <summary>Connected: swap the title/solo world for the shared one (also after a co-op restart).</summary>
        void BeginCoop()
        {
            m_World.Dispose();
            m_World = NewWorld(Missions.ByNetId(m_Net.Link.Mission) ?? m_Mission, m_Net.Link);
            m_Net.Events = m_World.Sync;
            m_Coop = true;
            m_Paused = false;
            m_LeaveArmed = 0f;
            m_StartNote = null;
            SetState(GameState.Playing);
            Sfx.Play(Sfx.Start);
            Sfx.Say("mission-start", 0.3f);
        }

        void EndCoop(string notice)
        {
            m_Net.Events = null;
            m_Coop = false;
            m_World.Dispose();
            m_World = NewWorld(m_Mission);
            Notice(notice);
            SetState(GameState.Title);
        }

        /// <summary>Drops back to the title screen if the session is gone. True while co-op can go on.</summary>
        bool CoopAlive()
        {
            if (m_Net.Ready) return true;
            string why = m_Net.State == NetState.Failed && !string.IsNullOrEmpty(m_Net.Status) ? m_Net.Status : "DISCONNECTED";
            m_Net.Leave();
            EndCoop(why);
            return false;
        }

        void Notice(string text)
        {
            m_Notice = text;
            m_NoticeTime = Time.unscaledTime;
        }

        static string RoomFromUrl(string url) => PageQuery.Get(url, "room")?.Trim().ToUpperInvariant();

        static string PageUrl() => PageQuery.Base(Application.absoluteURL);

        void Update()
        {
            UpdatePad();
            GameInput.Poll();
            MenuInput keys = GameInput.Menu;
            SoldierInput soldier = GameInput.Soldier;
            MenuAction click = m_Menu.TakeClick();
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            m_StateTime += dt;

            m_Frames++;
            m_FpsTimer += Time.unscaledDeltaTime;
            if (m_FpsTimer >= 0.5f)
            {
                m_Fps = m_Frames / m_FpsTimer;
                m_Frames = 0;
                m_FpsTimer = 0f;
            }

            if ((keys.Mute && m_State != GameState.CodeEntry) || (click == MenuAction.Sound && m_State == GameState.Settings))
            {
                Options o = m_Options;
                o.Sound = !o.Sound;
                ChangeOptions(o);
            }

            switch (m_State)
            {
                case GameState.Title:
                    if (CoopReady)
                    {
                        // J doubles as fire, so the online keys are checked first
                        if (click == MenuAction.Online) { OpenOnline(); break; }
                        if (keys.Host) { GoOnline(m_Net.Host); break; }
                        if (keys.Quick) { GoOnline(m_Net.QuickMatch); break; }
                        if (keys.Join) { OpenCodeEntry(""); break; }
                    }
                    if (click == MenuAction.Settings || keys.Settings) SetState(GameState.Settings);
                    else if (click == MenuAction.Play || keys.Start || soldier.FirePressed || soldier.JumpPressed) StartSolo();
                    break;

                case GameState.Settings:
                    if (click == MenuAction.Shake || keys.Shake)
                    {
                        Options o = m_Options;
                        o.Shake = !o.Shake;
                        ChangeOptions(o);
                    }
                    else if (click == MenuAction.Back || keys.Back || keys.Settings) SetState(GameState.Title);
                    break;

                case GameState.Online:
                    if (click == MenuAction.Host || keys.Host) GoOnline(m_Net.Host);
                    else if (click == MenuAction.Quick || keys.Quick) GoOnline(m_Net.QuickMatch);
                    else if (click == MenuAction.Join || keys.Join) OpenCodeEntry("");
                    else if (click == MenuAction.Back || keys.Back) SetState(GameState.Title);
                    break;

                case GameState.CodeEntry:
                    if (click == MenuAction.Submit || keys.Start) JoinWithCode();
                    else if (click == MenuAction.Back || keys.Back) SetState(GameState.Online);
                    break;

                case GameState.Lobby:
                    if (click == MenuAction.Back || keys.Back)
                    {
                        m_Net.Leave();
                        SetState(GameState.Title);
                    }
                    else if (m_Net.State == NetState.Failed || m_Net.State == NetState.Offline)
                    {
                        Notice(string.IsNullOrEmpty(m_Net.Status) ? "COULDN'T CONNECT" : m_Net.Status);
                        SetState(GameState.Title);
                    }
                    else if (m_Net.Ready)
                    {
                        BeginCoop();
                    }
                    break;

                case GameState.Playing:
                    if (m_Coop)
                    {
                        if (!CoopAlive()) break;
                        if (m_Net.Link.Round != m_World.Round) { BeginCoop(); break; } // partner restarted the mission
                        m_LeaveArmed -= dt;
                        if (keys.Back || keys.PadPause)
                        {
                            if (m_LeaveArmed > 0f)
                            {
                                m_Net.Leave();
                                EndCoop(null);
                                break;
                            }
                            m_LeaveArmed = 2f;
                        }
                        // no pausing a shared game; it's over when everyone is out of lives and continues
                        m_World.Tick(dt, soldier);
                        m_OutFor = m_World.AllOut ? m_OutFor + dt : 0f;
                        if (m_OutFor > Tuning.Lives.GameOverDelay) EndRun(GameState.GameOver);
                        else if (m_World.Victory && m_World.VictoryTime > 1f) EndRun(GameState.Victory);
                        break;
                    }
                    if (m_Paused)
                    {
                        if (click == MenuAction.Resume || keys.Pause || keys.PadPause || keys.Start) m_Paused = false;
                        else if (click == MenuAction.Restart || keys.Restart) Restart();
                        else if (click == MenuAction.Menu || keys.Back) ToTitle();
                        break;
                    }
                    if (keys.Restart) { Restart(); break; }
                    if (keys.Pause || keys.PadPause || keys.Back || (keys.Start && m_StateTime > 0.2f)) { m_Paused = true; break; }

                    m_World.Tick(dt, soldier);
                    m_OutFor = m_World.Player.Lives.IsOut ? m_OutFor + dt : 0f;
                    if (m_OutFor > Tuning.Lives.GameOverDelay) EndRun(GameState.GameOver);
                    else if (m_World.Victory && m_World.VictoryTime > 1f) EndRun(GameState.Victory);
                    break;

                case GameState.GameOver:
                case GameState.Victory:
                    bool ready = m_StateTime > 1.2f; // so a button mashed at the end doesn't skip the screen
                    if (m_Coop)
                    {
                        if (!CoopAlive()) break;
                        if (m_Net.Link.Round != m_World.Round) { BeginCoop(); break; }
                        if (click == MenuAction.Back || keys.Back)
                        {
                            m_Net.Leave();
                            EndCoop(null);
                            break;
                        }
                    }
                    else if (ready && (click == MenuAction.Menu || keys.Back))
                    {
                        ToTitle();
                        break;
                    }
                    m_World.Tick(dt, soldier);
                    if (ready && (click == MenuAction.Restart || keys.Start || keys.Restart || soldier.FirePressed))
                    {
                        if (m_Coop) m_Net.Link.RequestRestart(); // the authority bumps the round; everyone rebuilds
                        else Restart();
                    }
                    break;
            }

            m_World.Render();
            m_Hud.Refresh(m_World, m_Camera, new HudInfo
            {
                InGame = m_State == GameState.Playing || m_State == GameState.GameOver || m_State == GameState.Victory,
                Playing = m_State == GameState.Playing,
                Paused = m_Paused,
                StateTime = m_StateTime,
                Note = m_StartNote ?? (m_Coop ? null : Spike.Label),
                HiScore = m_Profile.Best(m_World.Mission),
                Fps = Mathf.RoundToInt(m_Fps),
                Muted = Sfx.Muted,
                Net = m_Coop ? m_Net : null,
                PageUrl = m_PageUrl,
                LeaveArmed = m_LeaveArmed > 0f,
            });
            m_Menu.Refresh(new MenuInfo
            {
                Screen = MenuScreenNow(),
                StateTime = m_StateTime,
                Touch = GameInput.TouchActive,
                Online = m_Net != null,
                OnlineOff = m_Net == null && NetSessions.Factory != null,
                CoopOpen = CoopOpen,
                CoopNew = CoopOpen && !m_Profile.HasSeen(Reveals.Coop),
                CoopJustOpened = m_CoopJustOpened,
                InCoop = m_Coop,
                Notice = m_Notice != null && Time.unscaledTime - m_NoticeTime < 8f ? m_Notice : null,
                Status = m_Net?.Status,
                Score = m_World.Scoring.Points,
                Seconds = Mathf.FloorToInt(m_World.Elapsed),
                Account = AccountLine(),
                Options = m_Options,
            });

            // The sign-in button sits over the game, on the title screen and settings only.
            bool account = m_State == GameState.Title || m_State == GameState.Settings;
            if (account != m_AccountShown)
            {
                m_AccountShown = account;
                m_Account.Show(account);
            }
        }

        MenuScreen MenuScreenNow()
        {
            switch (m_State)
            {
                case GameState.Title: return MenuScreen.Title;
                case GameState.Settings: return MenuScreen.Settings;
                case GameState.Online: return MenuScreen.Online;
                case GameState.CodeEntry: return MenuScreen.Join;
                case GameState.Lobby: return MenuScreen.Connecting;
                case GameState.Playing: return m_Paused ? MenuScreen.Paused : MenuScreen.None;
                case GameState.GameOver: return MenuScreen.GameOver;
                case GameState.Victory: return MenuScreen.Victory;
                default: return MenuScreen.None;
            }
        }

        /// <summary>Shows the touch pad while playing with a touch screen, and fits it to the screen.</summary>
        void UpdatePad()
        {
            ArcadePad pad = GameInput.Pad;
            pad.Layout(Screen.width * Tuning.Hud.Height / Mathf.Max(1, Screen.height));
            bool touch = GameInput.TouchActive;
            bool playing = m_State == GameState.Playing;
            pad.Visible = touch && playing && !m_Paused;
            pad.PauseVisible = touch && playing; // solo: pause; co-op: leave (twice)
        }

        void EndRun(GameState state)
        {
            SetState(state);
            bool wasOpen = m_Profile.coop;
            m_Profile.RecordRun(m_World.Mission, Setting.Normal, m_World.Scoring.Points, won: state == GameState.Victory, solo: !m_Coop);
            m_CoopJustOpened = !wasOpen && m_Profile.coop;
            m_Store.Save(m_Profile);
        }
    }
}
