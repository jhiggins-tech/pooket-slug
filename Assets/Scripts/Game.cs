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

    public enum GameState { Title, Settings, Online, CodeEntry, Lobby, Cutscene, Playing, GameOver, Victory, Demo }

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
        bool m_TankUnlocked, m_CrewUnlocked; // ?unlock=crew: the CREW pick is open for this visit, for testing
        float m_OutFor;         // seconds the game has had nobody left in it (out of lives and continues)    // ?unlock=tank: a tank can be called in each mission, for testing
        bool m_TestCrate;       // ?test=crate: a crate just ahead of the start, so the co-op test sees one handed out
        bool m_TestOneUp;       // ?test=1up: a 1-up just ahead of the start
        bool m_TestBoss;        // ?test=boss: a game starts just before the boss's arena (co-op too: the co-op test's boss round)
        bool m_TestPilot;       // ?test=pilot: the same, and the tank's wrecked as it wakes, so the pilot round starts at once
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
        // the boss intro under way (docs/design/tanks.md): seconds since it began, whether it's the full one, and the name card
        float m_IntroT;
        bool m_IntroFull;
        string m_IntroName;
        readonly BossIntroShow.Cue[] m_IntroCues = new BossIntroShow.Cue[4];
        // ?spike=cutscenes: the opening scene before Mission one, and a pretend partner's tap for co-op's rule
        CutsceneView m_SceneView;
        CutscenePlayer m_Scene;
        readonly System.Collections.Generic.List<string> m_SceneCues = new System.Collections.Generic.List<string>();
        float m_PartnerTapIn = -1f;
        bool m_SceneSaidStart;
        // the ⓘ kit screen, over the title (the crew pick) or the pause menu: whose kit, and the move whose demo plays
        bool m_KitOpen, m_KitFromTitle;
        SoldierDef m_KitSoldier;
        int m_KitMove;
        string m_DemoFolder; // where the demo clips are served: demos/ beside the page (null in the editor)
        // ?demo=: moves' demos played in the game, in turn, for CI to record (tools/demos.mjs)
        System.Collections.Generic.IReadOnlyList<MoveGuide> m_Demos;
        int m_DemoIndex, m_DemoFrames;
        float m_DemoT, m_DemoWait;
        bool m_DemoStarted;

        /// <summary>Co-op shows only once Mission one has been beaten solo (progressive disclosure).</summary>
        bool CoopOpen => m_Profile.coop || m_Unlocked;
        /// <summary>The crew opens with co-op, once Mission one is beaten solo (<c>?unlock=crew</c> opens it for testing).</summary>
        bool CrewOpen => CoopOpen || m_CrewUnlocked;
        SoldierDef Soldier => Crew.Pick(m_Options.Crew, CrewOpen);
        bool CoopReady => m_Net != null && CoopOpen;

        void Awake()
        {
            string url = Application.absoluteURL;
            Log.Configure(PageQuery.Get(url, "log")); // ?log=net:debug,game:warn for this visit
            Spike.Configure(url); // ?spike=hits for this visit (before the art and sounds, which make what spikes need)
            Art.Init();
            Sfx.Init(gameObject);
            Music.Init(gameObject);
            if (Spike.Music) Music.Prepare(MusicTracks.All); // rendered at launch, so the mission starts without a stutter
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
            m_CrewUnlocked = Array.IndexOf(unlocks, "crew") >= 0;
            string[] tests = (PageQuery.Get(url, "test") ?? "").ToLowerInvariant().Split(',');
            m_TestCrate = Array.IndexOf(tests, "crate") >= 0;
            m_TestOneUp = Array.IndexOf(tests, "1up") >= 0;
            m_TestPilot = Array.IndexOf(tests, "pilot") >= 0;
            m_TestBoss = m_TestPilot || Array.IndexOf(tests, "boss") >= 0;

            m_World = NewWorld(m_Mission);
            m_Hud = new HudView(transform);
            m_Menu = new MenuView(transform);
            if (Spike.Scenes) m_SceneView = new CutsceneView(transform);
            m_PageUrl = PageUrl();
            string folder = PageQuery.Folder(url);
            m_DemoFolder = folder.Length > 0 ? folder + "demos/" : null;
            SetState(GameState.Title);
            GameInput.TouchActive = Application.isMobilePlatform; // until the player shows otherwise

            if (StartDemos(PageQuery.Get(url, "demo"))) return; // a demo is all this visit plays
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
            var world = new World(m_Camera, mission, net, Soldier) { TanksToCall = m_TankUnlocked || (Spike.Drive && net == null) ? 1 : 0, SkipToPilot = m_TestPilot };
            world.BossIntro += OnBossIntro;
            world.BossFightStarted += OnBossFightStarted;
            world.TankWrecked += OnTankWrecked;
            m_IntroName = null;
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
            if (!m_SceneSaidStart) Sfx.Say("mission-start", 0.3f); // the opening's caption card already called it
            m_SceneSaidStart = false;
            if (Spike.Music)
            {
                Music.Silence();
                Music.Sting(MusicTracks.MissionStart);
                Music.Play(MusicTracks.SteelNight, MusicTracks.MissionStart.Seconds);
            }
        }

        /// <summary>START on the title screen: the opening scene first, with ?spike=cutscenes, then Mission one.</summary>
        void StartFromTitle()
        {
            if (m_SceneView == null)
            {
                StartSolo();
                return;
            }
            m_Scene = new CutscenePlayer(Cutscenes.Opening, m_Profile.HasSeen(Cutscenes.Opening.Id), Spike.ScenesCoop ? 2 : 1);
            m_SceneView.Begin(m_Scene);
            m_PartnerTapIn = -1f;
            m_SceneSaidStart = false;
            SetState(GameState.Cutscene);
        }

        /// <summary>Moves the scene on: taps (yours, and the pretend partner's a moment later), SKIP, and its cues.</summary>
        void TickScene(float dt, MenuInput keys, SoldierInput soldier)
        {
            bool tap = m_SceneView.TakeTap() || keys.Start || soldier.FirePressed || soldier.JumpPressed;
            if ((m_SceneView.TakeSkip() || keys.Back) && m_Scene.Skip()) { EndScene(); return; }
            int shot = m_Scene.Shot;
            if (tap) m_Scene.Tap(0);
            if (m_Scene.Players > 1)
            {
                if (m_Scene.HasTapped(0) && m_PartnerTapIn < 0f) m_PartnerTapIn = 1.2f;
                if (m_PartnerTapIn >= 0f && (m_PartnerTapIn -= dt) < 0f) m_Scene.Tap(1);
            }
            if (m_Scene.Shot != shot || m_Scene.Done || !m_Scene.HasTapped(0)) m_PartnerTapIn = -1f; // moved on, or jumped to an intro
            m_Scene.Tick(dt, m_SceneCues);
            foreach (string cue in m_SceneCues)
            {
                if (cue == "say:mission-start") m_SceneSaidStart = true;
                m_SceneView.Cue(cue);
            }
            if (m_Scene.Done) EndScene();
            else m_SceneView.Refresh();
        }

        void EndScene()
        {
            if (m_Profile.MarkSeen(m_Scene.Scene.Id)) m_Store.Save(m_Profile);
            m_SceneView.Hide();
            m_Scene = null;
            StartSolo();
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
            Music.Silence();
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

        // ------------------------------------------------------------------ the ⓘ kit screen

        /// <summary>Opens the kit screen on <paramref name="soldier"/>'s moves, the first one's demo playing.</summary>
        void OpenKit(SoldierDef soldier, bool fromTitle)
        {
            m_KitOpen = true;
            m_KitFromTitle = fromTitle;
            m_KitSoldier = soldier;
            m_KitMove = 0;
            Sfx.Play(Sfx.Pickup, 0.4f);
        }

        /// <summary>The kit screen's buttons: a move (or its own button, so FIRE shows FIRE's), the crew pick, and back.</summary>
        void TickKit(MenuAction click, MenuInput keys, SoldierInput soldier)
        {
            System.Collections.Generic.IReadOnlyList<MoveGuide> moves = KitGuide.For(m_KitSoldier);
            int pick = -1;
            if (click >= MenuAction.Move1 && click <= MenuAction.Move4) pick = click - MenuAction.Move1;
            else if (soldier.FirePressed) pick = SlotIndex(moves, KitSlot.Fire);
            else if (soldier.SpecialPressed) pick = SlotIndex(moves, KitSlot.Special);
            else if (soldier.SuperPressed) pick = SlotIndex(moves, KitSlot.Super);
            else if (soldier.AbilityPressed) pick = SlotIndex(moves, KitSlot.Ability);
            if (pick >= 0 && pick < moves.Count && pick != m_KitMove)
            {
                m_KitMove = pick;
                Sfx.Play(Sfx.Pickup, 0.3f);
            }
            if (click == MenuAction.Crew && m_KitFromTitle && CrewOpen)
            {
                Options o = m_Options;
                o.Crew = Crew.Next(Soldier.Id).Id;
                ChangeOptions(o);
                m_KitSoldier = Soldier;
                m_KitMove = 0;
                Sfx.Play(Sfx.Pickup, 0.5f);
            }
            if (click == MenuAction.Back || keys.Back || keys.Settings || keys.Start) m_KitOpen = false;
        }

        static int SlotIndex(System.Collections.Generic.IReadOnlyList<MoveGuide> moves, KitSlot slot)
        {
            for (int i = 0; i < moves.Count; i++)
                if (moves[i].Slot == slot) return i;
            return -1;
        }

        // ------------------------------------------------------------------ ?demo: the moves' demos

        /// <summary>
        /// <c>?demo=tones2-fire</c> plays that move's demo over and over; <c>?demo=all</c> plays every move's in turn, as
        /// CI records them (<c>tools/demos.mjs</c>); <c>?demo=list</c> just logs their names. False for no demo.
        /// </summary>
        bool StartDemos(string demo)
        {
            if (string.IsNullOrEmpty(demo)) return false;
            var ids = new System.Collections.Generic.List<string>();
            foreach (MoveGuide m in KitGuide.All) ids.Add(m.DemoId);
            Log.Game.Info("Demos: " + string.Join(",", ids));
            if (string.Equals(demo, "all", StringComparison.OrdinalIgnoreCase)) m_Demos = KitGuide.All;
            else if (KitGuide.Find(demo) is MoveGuide one) m_Demos = new[] { one };
            else
            {
                if (!string.Equals(demo, "list", StringComparison.OrdinalIgnoreCase)) Log.Game.Warn("No demo called " + demo);
                return false;
            }
            BeginDemo(0);
            return true;
        }

        /// <summary>A fresh world for a demo: its little map, its soldier, who nothing hurts, with the super charged if it's needed.</summary>
        void BeginDemo(int index)
        {
            m_DemoIndex = index;
            MoveGuide move = m_Demos[index];
            m_World.Dispose();
            m_World = new World(m_Camera, new MissionDef("demo", byte.MaxValue, "DEMO", move.Demo.Map), null, move.Soldier);
            m_World.Player.NoDamage = true;
            if (move.Demo.SuperReady)
                for (int i = 0; i < Tuning.Kit.SuperKills; i++) m_World.Player.Kit.AddKill();
            m_DemoT = 0f;
            m_DemoFrames = 0;
            m_World.Render();
            SetState(GameState.Demo);
            if (m_DemoStarted) Log.Game.Info("Demo start: " + move.DemoId);
        }

        /// <summary>
        /// One frame of a demo, on a fixed step (so a slow machine records it in slow motion, and the recorder speeds it
        /// back up). The first waits for Enter or a tap (one demo on its own starts by itself after two seconds), so the page
        /// has finished loading.
        /// </summary>
        void TickDemo(MenuInput keys, SoldierInput soldier)
        {
            MoveGuide move = m_Demos[m_DemoIndex];
            if (!m_DemoStarted)
            {
                m_DemoWait += Time.unscaledDeltaTime;
                bool waited = m_Demos.Count == 1 && m_DemoWait > 2f; // ?demo=all (CI's recorder) waits for its Enter
                if (!keys.Start && !soldier.FirePressed && !waited) return;
                m_DemoStarted = true;
                Log.Game.Info("Demo start: " + move.DemoId);
            }
            const float dt = 1f / DemoScript.Fps;
            m_DemoT += dt;
            m_DemoFrames++;
            m_World.Tick(dt, move.Demo.InputAt(m_DemoT, dt));
            if (m_DemoT < move.Demo.Seconds) return;
            Log.Game.Info("Demo end: " + move.DemoId + " (" + m_DemoFrames + " frames, " + (m_DemoFrames / DemoScript.Fps).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s)");
            if (m_DemoIndex == m_Demos.Count - 1 && m_Demos.Count > 1) Log.Game.Info("Demos done");
            BeginDemo((m_DemoIndex + 1) % m_Demos.Count);
        }

        // ------------------------------------------------------------------ the boss intro

        /// <summary>?spike=music, which is solo only.</summary>
        bool MusicOn => Spike.Music && !m_Coop;

        void OnBossIntro(BossEnemy boss, bool firstTime)
        {
            if (MusicOn) Music.Stop(); // the screen locks and the mission's music cuts out (docs/design/tanks.md)
            m_IntroT = -0.001f; // just before 0, so the first cues fire on the next tick
            m_IntroFull = firstTime;
            m_IntroName = boss.DisplayName;
        }

        /// <summary>The tank's first shot: the boss music kicks in.</summary>
        void OnBossFightStarted(BossEnemy boss)
        {
            if (MusicOn && Music.Playing != MusicTracks.TankAlertFinal) Music.Play(MusicTracks.TankAlert);
        }

        /// <summary>
        /// The tank's wrecked: a victory sting, then the boss theme's even faster final section for the pilot round,
        /// which crossfades from the chip to an orchestra playing the same notes.
        /// </summary>
        void OnTankWrecked()
        {
            if (!MusicOn) return;
            Music.Sting(MusicTracks.BossVictory);
            // after one pass in chip, the orchestra takes over (in builds that have it: the vendor repository's CI makes it)
            Music.Play(MusicTracks.TankAlertFinal, MusicTracks.BossVictory.Seconds, Music.Layer("tank-alert-final"), MusicTracks.TankAlertFinal.Seconds);
        }

        /// <summary>Moves the intro on, and plays the siren and the announcer's lines as they come due.</summary>
        void TickIntro(float dt)
        {
            if (m_IntroName == null) return;
            float from = m_IntroT;
            m_IntroT += dt;
            int n = BossIntroShow.Cues(m_IntroFull, from, m_IntroT, m_IntroCues);
            for (int i = 0; i < n; i++)
            {
                switch (m_IntroCues[i])
                {
                    case BossIntroShow.Cue.Siren: Sfx.Play(Sfx.Siren, 0.7f); break;
                    case BossIntroShow.Cue.SayWarning: Sfx.Say("warning"); break;
                    case BossIntroShow.Cue.SayBoss: Sfx.Say("boss-tank"); break;
                }
            }
            if (new BossIntroShow(m_IntroT, m_IntroFull).Over) m_IntroName = null;
        }

        // ------------------------------------------------------------------ settings

        void ApplyOptions()
        {
            Sfx.Muted = !m_Options.Sound;
            Music.Muted = !m_Options.Sound;
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
            Music.Silence(); // spikes are solo only
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
            Music.Tick(dt);
            Music.Paused = m_Paused;

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

            if (m_KitOpen) TickKit(click, keys, soldier);
            else switch (m_State)
            {
                case GameState.Demo:
                    TickDemo(keys, soldier);
                    break;

                case GameState.Title:
                    if (click == MenuAction.Kit || soldier.AbilityPressed) { OpenKit(Soldier, fromTitle: true); break; }
                    if (CoopReady)
                    {
                        // J doubles as fire, so the online keys are checked first
                        if (click == MenuAction.Online) { OpenOnline(); break; }
                        if (keys.Host) { GoOnline(m_Net.Host); break; }
                        if (keys.Quick) { GoOnline(m_Net.QuickMatch); break; }
                        if (keys.Join) { OpenCodeEntry(""); break; }
                    }
                    if (click == MenuAction.Settings || keys.Settings) SetState(GameState.Settings);
                    else if (CrewOpen && (click == MenuAction.Crew || soldier.SpecialPressed))
                    {
                        Options o = m_Options;
                        o.Crew = Crew.Next(Soldier.Id).Id;
                        ChangeOptions(o);
                        Sfx.Play(Sfx.Pickup, 0.5f);
                        m_World.Dispose(); // the title's world again, with the new pick standing in it
                        m_World = NewWorld(m_Mission);
                    }
                    else if (click == MenuAction.Play || keys.Start || soldier.FirePressed || soldier.JumpPressed) StartFromTitle();
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

                case GameState.Cutscene:
                    TickScene(dt, keys, soldier);
                    break;

                case GameState.Playing:
                    if (!m_Paused) TickIntro(dt);
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
                        if (click == MenuAction.Kit || soldier.AbilityPressed) OpenKit(m_World.Player.Def, fromTitle: false);
                        else if (click == MenuAction.Resume || keys.Pause || keys.PadPause || keys.Start) m_Paused = false;
                        else if (click == MenuAction.Restart || keys.Restart) Restart();
                        else if (click == MenuAction.Menu || keys.Back) ToTitle();
                        break;
                    }
                    if (keys.Restart) { Restart(); break; }
                    if (keys.Pause || keys.PadPause || keys.Back || (keys.Start && m_StateTime > 0.2f)) { m_Paused = true; break; }

                    m_World.Tick(dt, soldier);
                    if (MusicOn && m_World.Victory && Music.Playing != null) Music.Stop(fade: true); // the pilot glitched out
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
                Intro = new BossIntroShow(m_IntroT, m_IntroFull),
                IntroName = m_IntroName,
            });
            m_Menu.Refresh(new MenuInfo
            {
                Screen = MenuScreenNow(),
                StateTime = m_StateTime,
                Touch = GameInput.TouchActive,
                Online = m_Net != null,
                OnlineOff = m_Net == null && NetSessions.Factory != null,
                CoopOpen = CoopOpen,
                CrewOpen = CrewOpen,
                Crew = Soldier.Name.ToUpperInvariant(),
                CoopNew = CoopOpen && !m_Profile.HasSeen(Reveals.Coop),
                CoopJustOpened = m_CoopJustOpened,
                InCoop = m_Coop,
                Notice = m_Notice != null && Time.unscaledTime - m_NoticeTime < 8f ? m_Notice : null,
                Status = m_Net?.Status,
                Score = m_World.Scoring.Points,
                Seconds = Mathf.FloorToInt(m_World.Elapsed),
                Account = AccountLine(),
                Options = m_Options,
                Kit = m_KitOpen ? KitGuide.For(m_KitSoldier) : null,
                KitMove = m_KitMove,
                KitSoldier = m_KitOpen ? m_KitSoldier.Name.ToUpperInvariant() : null,
                KitCrew = m_KitFromTitle && CrewOpen,
                DemoFolder = m_DemoFolder,
            });

            // The sign-in button sits over the game, on the title screen and settings only.
            bool account = (m_State == GameState.Title || m_State == GameState.Settings) && !m_KitOpen;
            if (account != m_AccountShown)
            {
                m_AccountShown = account;
                m_Account.Show(account);
            }
        }

        MenuScreen MenuScreenNow()
        {
            if (m_KitOpen) return MenuScreen.Kit;
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
            if (state == GameState.GameOver) Music.Silence();
            bool wasOpen = m_Profile.coop;
            m_Profile.RecordRun(m_World.Mission, Setting.Normal, m_World.Scoring.Points, won: state == GameState.Victory, solo: !m_Coop);
            m_CoopJustOpened = !wasOpen && m_Profile.coop;
            m_Store.Save(m_Profile);
        }
    }
}
