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

    public enum GameState { Title, Settings, Online, CodeEntry, Lobby, Den, Cutscene, Playing, GameOver, Victory, Demo }

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
        bool m_TestTank;        // ?test=tank: a tank of your own parked just ahead of the start, and one call-in
        bool m_TestOneUp;       // ?test=1up: a 1-up just ahead of the start
        bool m_TestCrew;        // ?test=crew: two of the crew plays on the title screen (once a visit), as if Mission one were just beaten
        CrewReveal m_Reveal;
        CrewRevealView m_RevealView;
        bool m_TestDebrief;     // ?test=debrief: the mission's won as it starts, so the debrief shows (as if it's the first)
        bool m_TestCoda;        // ?test=coda: a game starts at the coda, the tabk run after the boss, with its prompts
        bool m_TestBoss;        // ?test=boss: a game starts just before the boss's arena (co-op too: the co-op test's boss round)
        bool m_TestPilot;       // ?test=pilot: the same, and the tank's wrecked as it wakes, so the pilot round starts at once
        bool m_TestTeam;        // ?test=team: solo, a stand-in partner and both supers charged, to try TEAM
        bool m_TestScene;       // ?test=scene: in co-op, starting together from the den plays a stand-in scene on both phones
        bool m_TestReveal;      // ?test=reveal: the motor pool reveal plays even if it's been seen (with boss: ?test=boss,reveal)
        bool m_TestWeakPilot;   // ?test=pilot,weak: and the pilot goes down a few seconds in (or to one shot), to see what beating him does
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
        DebriefPlayer m_Debrief; // a won solo run's debrief, while it's up
        DebriefView m_DebriefView;
        MissionDef m_Mission = Missions.One; // the mission a solo game plays; a shared game plays the host's
        string m_PageUrl;      // the page's address without its query, for invite links
        // the boss intro under way (docs/design/tanks.md): seconds since it began, whether it's the full one, and the name card
        float m_IntroT;
        bool m_IntroFull;
        string m_IntroName;
        readonly BossIntroShow.Cue[] m_IntroCues = new BossIntroShow.Cue[4];
        // the cutscene playing (docs/design/story.md, Cutscenes), what comes after it, and in co-op our taps to send
        CutsceneView m_SceneView;
        CutscenePlayer m_Scene;
        readonly System.Collections.Generic.List<string> m_SceneCues = new System.Collections.Generic.List<string>();
        Action m_AfterScene;
        bool m_SceneCoop, m_SceneSkip, m_SceneSaidStart;
        int m_SceneRound, m_SceneTapped;
        // the lobby, the crew's gaming den (docs/design/lobby.md): once the crew's open, START, CREW and co-op go through it
        DenView m_DenView;
        Den m_Den;
        // the ⓘ kit screen, over the den (the couch pick) or the pause menu: whose kit, and the move whose demo plays
        bool m_KitOpen;
        SoldierDef m_KitSoldier;
        int m_KitMove;
        string m_DemoFolder; // where the demo clips are served: demos/ beside the page (null in the editor)
        // ?demo=: moves' demos played in the game, in turn, for CI to record (tools/demos.mjs)
        System.Collections.Generic.IReadOnlyList<MoveGuide> m_Demos;
        int m_DemoIndex, m_DemoFrames;
        float m_DemoT, m_DemoWait;
        bool m_DemoStarted, m_DemoRecording; // the first time through ?demo=all, the page records each demo for CI

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
            m_TestTank = Array.IndexOf(tests, "tank") >= 0;
            m_TestOneUp = Array.IndexOf(tests, "1up") >= 0;
            m_TestPilot = Array.IndexOf(tests, "pilot") >= 0;
            m_TestWeakPilot = m_TestPilot && Array.IndexOf(tests, "weak") >= 0;
            m_TestTeam = Array.IndexOf(tests, "team") >= 0;
            m_TestBoss = m_TestPilot || Array.IndexOf(tests, "boss") >= 0;
            m_TestReveal = Array.IndexOf(tests, "reveal") >= 0;
            m_TestCoda = Array.IndexOf(tests, "coda") >= 0;
            m_TestDebrief = Array.IndexOf(tests, "debrief") >= 0;
            m_TestCrew = Array.IndexOf(tests, "crew") >= 0;
            m_Unlocked |= m_TestCrew;
            m_TestScene = Array.IndexOf(tests, "scene") >= 0;

            m_World = NewWorld(m_Mission);
            m_Hud = new HudView(transform);
            m_Menu = new MenuView(transform);
            m_SceneView = new CutsceneView(transform);
            m_DebriefView = new DebriefView(transform);
            m_RevealView = new CrewRevealView(transform);
            m_DenView = new DenView(transform);
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
                    StartFromTitle();
                }
            }
        }

        /// <summary>A world for <paramref name="mission"/>, shared when <paramref name="net"/> is given.</summary>
        World NewWorld(MissionDef mission, INetLink net = null)
        {
            var world = new World(m_Camera, mission, net, Soldier) { TanksToCall = m_TankUnlocked || m_TestTank || (Spike.CallIn && net == null) ? 1 : 0, SkipToPilot = m_TestPilot, WeakPilot = m_TestWeakPilot, TestTeam = m_TestTeam };
            world.BossIntro += OnBossIntro;
            world.BossFightStarted += OnBossFightStarted;
            world.PilotFreed += OnPilotFreed;
            world.TankWrecked += OnTankWrecked;
            world.CodaCleared += OnCodaCleared;
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
            if (m_TestCoda && net == null) StartAtCoda(world, true);
            if (Spike.CallIn && net == null) world.CallInCoach = new CallInCoach(false); // every visit, so it can be tried again
            if (m_TestTank) world.ParkTank(world.Player.Pos.x + Content.PlayerTank.HalfWidth + 4f); // each player's own, a few steps ahead
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

        /// <summary>Profile key: the coda's been cleared once, so its prompts don't show again.</summary>
        const string CodaSeen = "coda";

        /// <summary>
        /// Starts at the coda: the back gate's open, the boss is left out, and there's a tabk to call
        /// (a restart at its checkpoint, or <c>?test=coda</c>).
        /// </summary>
        static void StartAtCoda(World world, bool prompts)
        {
            float x = world.Level.CodaX;
            if (x < 0f) return;
            world.Spawner.SkipBefore = x;
            world.OpenCoda(prompts, restart: true);
            StartAt(world, x + 2f);
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
            if (state != GameState.Title) m_RevealView?.End(); // kie leaves with the title screen
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
            if (m_TestDebrief) m_World.OnBossDestroyed();
            if (Spike.Music)
            {
                Music.Silence();
                Music.Sting(MusicTracks.MissionStart);
                Music.Play(MusicTracks.SteelNight, MusicTracks.MissionStart.Seconds);
            }
        }

        /// <summary>START on the title screen, or a solo START in the den: Mission one plays its opening scene first.</summary>
        void StartFromTitle()
        {
            // ?test=boss, pilot and team are for trying something at once: they start without the opening
            if (m_Mission == Missions.One && !m_TestBoss && !m_TestTeam && !m_TestCoda && !m_TestDebrief) PlayScene(Cutscenes.Opening, false, StartSolo);
            else StartSolo();
        }

        /// <summary>
        /// Plays <paramref name="scene"/> (<c>docs/design/story.md</c>, Cutscenes), then <paramref name="after"/>. In co-op
        /// both phones play it, and a shot moves on early once both have tapped (<see cref="CutsceneSync"/>).
        /// </summary>
        void PlayScene(CutsceneDef scene, bool coop, Action after)
        {
            m_Scene = new CutscenePlayer(scene, m_Profile.HasSeen(scene.Id), coop ? 2 : 1);
            m_SceneCoop = coop;
            m_SceneRound = coop ? m_Net.Link.Round : 0;
            m_SceneTapped = -1;
            m_SceneSkip = false;
            m_SceneSaidStart = false;
            m_AfterScene = after;
            m_SceneView.Begin(m_Scene);
            SetState(GameState.Cutscene);
            Log.Game.Info("Cutscene: " + scene.Id + (coop ? ", with a partner" : "") + (m_Scene.CanSkip ? ", seen before" : ""));
        }

        /// <summary>Moves the scene on: taps (and in co-op the partner's), SKIP, and its cues.</summary>
        void TickScene(float dt, MenuInput keys, SoldierInput soldier)
        {
            if (m_SceneCoop && !CoopAlive()) return;
            bool tap = m_SceneView.TakeTap() || keys.Start || soldier.FirePressed || soldier.JumpPressed;
            bool skip = m_SceneView.TakeSkip() || keys.Back;
            if (skip && !m_SceneCoop && m_Scene.Skip())
            {
                EndScene();
                return;
            }
            if (skip && m_SceneCoop && m_Scene.CanSkip) m_SceneSkip = true; // it skips once the partner asks too
            int beat = m_Scene.Beat, shot = m_Scene.Shot;
            float left = m_Scene.Scene.Shots[shot].Seconds - m_Scene.T; // a shot that moves on with time left was tapped on
            if (tap && (m_Scene.Tap(0) || m_Scene.HasTapped(0))) m_SceneTapped = Math.Max(m_SceneTapped, beat);
            if (m_SceneCoop)
            {
                System.Collections.Generic.IReadOnlyList<IAvatarLink> partners = m_Net.Link.Partners;
                bool here = partners.Count > 0;
                bool partnerSkips = CutsceneSync.Apply(m_Scene, here, here ? partners[0].SceneTaps : 0, m_SceneRound);
                if (m_SceneSkip && partnerSkips && m_Scene.Skip())
                {
                    EndScene();
                    return;
                }
                m_Net.Link.SendScene(CutsceneSync.Pack(m_SceneRound, m_Scene.Scene.NetId, m_SceneTapped, m_SceneSkip));
                m_SceneView.Waiting(m_SceneSkip ? "SKIPPING ONCE YOUR PARTNER DOES" : null);
            }
            m_Scene.Tick(dt, m_SceneCues, Sfx.NarratorTalking);
            foreach (string cue in m_SceneCues)
            {
                if (cue == Cues.SayMissionStart) m_SceneSaidStart = true;
                m_SceneView.Cue(cue);
            }
            if (m_Scene.Shot != shot && !m_Scene.Done)
                Log.Game.Info("Cutscene shot " + (m_Scene.Shot + 1) + " of " + m_Scene.Scene.Shots.Length + (left > 0.5f ? ", tapped on" : ""));
            if (m_Scene.Done) EndScene();
            else m_SceneView.Refresh();
        }

        void EndScene()
        {
            if (m_Profile.MarkSeen(m_Scene.Scene.Id)) m_Store.Save(m_Profile);
            Log.Game.Info("Cutscene over: " + m_Scene.Scene.Id + (m_Scene.Skipped ? ", skipped" : ""));
            // Leave the last word standing as "tapped through it all": the tap that ended it here went out with no
            // packet of its own, so the partner may still be waiting on it. The round and scene key keep it from
            // counting anywhere else.
            if (m_SceneCoop)
                m_Net.Link.SendScene(CutsceneSync.Pack(m_SceneRound, m_Scene.Scene.NetId, m_Scene.Scene.Shots.Length * 3, m_SceneSkip || m_Scene.Skipped));
            DropScene();
            Action after = m_AfterScene;
            m_AfterScene = null;
            after?.Invoke();
        }

        void DropScene()
        {
            if (m_Scene == null) return;
            m_SceneView.Hide();
            m_Scene = null;
            m_SceneCoop = false;
        }

        void Restart()
        {
            // Lost after the checkpoint at the boss door: start again there (docs/design/levels.md, Missions).
            float checkpoint = m_State == GameState.GameOver ? m_World.CheckpointX : -1f;
            m_World.Dispose();
            m_World = NewWorld(m_Mission);
            if (checkpoint >= 0f && m_World.Level.CodaX >= 0f && checkpoint >= m_World.Level.CodaX)
            {
                // lost in the coda: start again at its start, not at the boss (docs/design/story.md, The coda)
                StartAtCoda(m_World, !m_Profile.HasSeen(CodaSeen));
                m_World.CheckpointX = checkpoint;
            }
            else if (checkpoint >= 0f)
            {
                m_World.CheckpointX = checkpoint;
                m_World.QuickBossIntro = true; // the second time: the tank's already in place
                StartAt(m_World, checkpoint);
            }
            m_StartNote = null;
            StartSolo();
        }

        // ------------------------------------------------------------------ two of the crew

        /// <summary>Two of the crew plays once, ever, the first time the title loads after Mission one's beaten solo.</summary>
        bool CrewRevealDue => !m_Coop && !m_RevealView.Showing &&
                              (m_TestCrew || m_Profile.coop && !m_Profile.HasSeen(Reveals.TwoOfTheCrew));

        void BeginCrewReveal()
        {
            m_Reveal = new CrewReveal();
            m_RevealView.Begin(m_World);
        }

        void TickCrewReveal(float dt, MenuInput keys, SoldierInput soldier)
        {
            if (m_RevealView.TakeTap() || keys.Start || soldier.FirePressed || soldier.JumpPressed) m_Reveal.Tap();
            CrewReveal.Shot was = m_Reveal.Now;
            bool wasCracked = !m_Reveal.OldStart;
            if (m_Reveal.Tick(dt)) Sfx.Play(Sfx.Thump, 0.9f); // each new button's clunk
            if (!wasCracked && !m_Reveal.OldStart) Sfx.Play(Sfx.Hit, 0.8f); // START splits
            if (m_Reveal.Now != was && m_Reveal.Now == CrewReveal.Shot.KiesLine) Sfx.Play(Sfx.Pickup, 0.5f);
            if (!m_Reveal.Done)
            {
                m_RevealView.Refresh(m_Reveal, m_Camera, dt);
                return;
            }
            m_RevealView.Finish(); // kie stays, stood by tones2, until the title's left
            m_Reveal = null;
            m_TestCrew = false; // once a visit
            if (m_Profile.MarkSeen(Reveals.TwoOfTheCrew)) m_Store.Save(m_Profile);
            Log.Game.Info("Two of the crew: the menu's grown");
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

        // ------------------------------------------------------------------ the den

        /// <summary>Opens the den: to start a solo mission, to look round the crew, or a co-op room's lobby.</summary>
        void OpenDen(DenMode mode)
        {
            m_Den = new Den(mode, Crew.Freed, Soldier);
            m_DenView.Begin(m_Den, DenInfoNow());
            SetState(GameState.Den);
            Log.Game.Info("Den open: " + mode + ", on the couch: " + (m_Den.Pick?.Name ?? "nobody"));
        }

        DenInfo DenInfoNow()
        {
            bool coop = m_Den.Mode == DenMode.Coop;
            MissionDef mission = coop ? Missions.ByNetId(m_Net.Link.Mission) ?? m_Mission : m_Mission;
            return new DenInfo
            {
                // Mission one is solo only (docs/design/lobby.md, Co-op): until Mission two is built, co-op plays it as a stand-in
                Mission = m_Den.Mode == DenMode.Crew ? null : coop && mission == Missions.One ? "TEST MISSION" : mission.Name.ToUpperInvariant(),
                Room = coop ? m_Net.RoomCode : null,
                CanHost = CoopReady && m_Den.Mode == DenMode.Solo,
                Skin = coop ? m_Net.Link.LocalSkin : 0,
            };
        }

        /// <summary>The den's buttons and keys: a crewmate tapped (or Special for the next), READY (Enter, fire), the kit, hosting, BACK.</summary>
        void TickDen(MenuInput keys, SoldierInput soldier)
        {
            bool coop = m_Den.Mode == DenMode.Coop;
            if (coop && !CoopAlive()) return;
            if (m_DenView.TakeBack() || keys.Back)
            {
                Log.Game.Info("Den left");
                if (coop) m_Net.Leave();
                m_World.Dispose(); // the title's world again, with whoever's on the couch standing in it
                m_World = NewWorld(m_Mission);
                SetState(GameState.Title);
                return;
            }
            if (m_DenView.TakeKit() || soldier.AbilityPressed)
            {
                OpenKit(m_Den.Pick ?? Soldier);
                return;
            }
            if ((m_DenView.TakeHost() || keys.Host) && CoopReady && m_Den.Mode == DenMode.Solo)
            {
                Log.Game.Info("Den: hosting from the couch");
                GoOnline(m_Net.Host);
                return;
            }
            SoldierDef tapped = m_DenView.TakePick();
            if (tapped == null && soldier.SpecialPressed) tapped = m_Den.NextFree();
            if (tapped != null && tapped != m_Den.Pick)
            {
                if (m_Den.TryPick(tapped)) Log.Game.Info("Den pick: " + tapped.Name);
                else Sfx.Play(Sfx.Thump, 0.3f); // taken, or too late
            }
            bool keyReady = m_StateTime > 0.3f && (keys.Start || soldier.FirePressed || soldier.JumpPressed); // not the START that opened the den
            if (m_DenView.TakeReady() || keyReady)
            {
                if (m_Den.ToggleReady())
                {
                    Sfx.Play(Sfx.Pickup, m_Den.Ready ? 0.7f : 0.3f);
                    Log.Game.Info("Den " + (m_Den.Ready ? "ready: " : "unready: ") + m_Den.Pick.Name);
                }
            }
            if (m_Den.Pick != null && m_Den.Pick != Soldier)
            {
                Options o = m_Options; // the title and the next den remember who's on the couch
                o.Crew = m_Den.Pick.Id;
                ChangeOptions(o);
            }
        }

        /// <summary>Moves the den on each frame: the partner's seat and flags in, ours out, the countdown, the zoom, and the mission once it's done.</summary>
        void MoveDen(float dt)
        {
            bool coop = m_Den.Mode == DenMode.Coop && m_Net != null && m_Net.Ready;
            DenPartner partner = default;
            bool authority = true;
            if (coop)
            {
                authority = m_Net.Link.IsAuthority;
                System.Collections.Generic.IReadOnlyList<IAvatarLink> partners = m_Net.Link.Partners;
                if (partners.Count > 0)
                {
                    IAvatarLink p = partners[0];
                    partner = new DenPartner { Flags = p.DenFlags, Playing = p.Playing };
                    if (partner.Seated || partner.Playing) partner.Soldier = p.Soldier; // otherwise it's whoever they played last
                }
            }
            bool zooming = m_Den.Zoom >= 0f;
            m_Den.Tick(dt, partner, authority);
            if (m_Den.TakeUndone())
            {
                m_DenView.Note("YOUR MATE GOT THERE FIRST: PICK SOMEONE ELSE");
                Sfx.Play(Sfx.Glitch, 0.4f);
                Log.Game.Info("Den pick undone: the partner has it");
            }
            if (!zooming && m_Den.Zoom >= 0f)
            {
                Sfx.Play(Sfx.Start, 0.6f);
                Log.Game.Info("Den START: " + m_Den.Pick.Name + (coop && partner.Present ? ", partner " + (partner.Playing ? "playing" : "in the den") : ""));
            }
            if (coop) m_Net.Link.SendDen(m_Den.Pick, m_Den.Flags);
            m_DenView.Refresh(dt, m_Den, DenInfoNow());
            if (!m_Den.Go || m_KitOpen) return;
            m_DenView.Hide();
            if (m_Den.Mode == DenMode.Coop)
            {
                // Mission one is single player only, so its scenes never play in a co-op room; ?test=scene plays a
                // stand-in on both phones when starting together, to check the two-phone sync
                if (m_TestScene && partner.InDenNow) PlayScene(Cutscenes.CoopTest, true, BeginCoop);
                else BeginCoop();
            }
            else
            {
                m_World.Dispose(); // the soldier on the couch plays
                m_World = NewWorld(m_Mission);
                StartFromTitle();
            }
        }

        // ------------------------------------------------------------------ the ⓘ kit screen

        /// <summary>Opens the kit screen on <paramref name="soldier"/>'s moves, the first one's demo playing.</summary>
        void OpenKit(SoldierDef soldier)
        {
            m_KitOpen = true;
            m_KitSoldier = soldier;
            m_KitMove = 0;
            Sfx.Play(Sfx.Pickup, 0.4f);
        }

        /// <summary>The kit screen's buttons: a move (or its own button, so FIRE shows FIRE's), and back.</summary>
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
            if (m_DemoStarted) StartedDemo(move);
        }

        void StartedDemo(MoveGuide move)
        {
            Log.Game.Info("Demo start: " + move.DemoId);
            if (m_DemoRecording) DemoRecording.Begin(move.DemoId);
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
                m_DemoRecording = m_Demos.Count > 1;
                StartedDemo(move);
            }
            const float dt = 1f / DemoScript.Fps;
            m_DemoT += dt;
            m_DemoFrames++;
            m_World.Tick(dt, move.Demo.InputAt(m_DemoFrames));
            if (m_DemoT < move.Demo.Seconds) return;
            Log.Game.Info("Demo end: " + move.DemoId + " (" + m_DemoFrames + " frames, " + (m_DemoFrames / DemoScript.Fps).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s)");
            if (m_DemoRecording) DemoRecording.End();
            if (m_DemoIndex == m_Demos.Count - 1 && m_Demos.Count > 1)
            {
                m_DemoRecording = false;
                Log.Game.Info("Demos done");
            }
            BeginDemo((m_DemoIndex + 1) % m_Demos.Count);
        }

        // ------------------------------------------------------------------ the boss intro

        /// <summary>?spike=music, which is solo only.</summary>
        bool MusicOn => Spike.Music && !m_Coop;

        void OnBossIntro(BossEnemy boss, bool firstTime)
        {
            if (MusicOn) Music.Stop(); // the screen locks and the mission's music cuts out (docs/design/tanks.md)
            if (firstTime && boss is TankBoss tank && PlaysMotorPool)
            {
                // The first time: the motor pool reveal is the tabk's entrance, so after it the tabk's in place and the
                // intro goes straight to the banner, the name card and the music (docs/design/story.md).
                float playedFor = m_StateTime; // the mission's clock (and its start banner) carry on from where they were
                PlayScene(Cutscenes.MotorPool, false, () =>
                {
                    tank.SkipEntrance();
                    m_Paused = false;
                    SetState(GameState.Playing);
                    m_StateTime = playedFor;
                    StartBossIntro(boss, true);
                });
                return;
            }
            StartBossIntro(boss, firstTime);
        }

        /// <summary>
        /// The motor pool reveal plays once, ever: the first time tones2 reaches Mission one's motor pool, solo (co-op
        /// opens only once Mission one's beaten, so it's been seen by then). Test starts skip it, unless ?test=reveal.
        /// </summary>
        bool PlaysMotorPool => !m_Coop && m_Mission == Missions.One &&
            (m_TestReveal || !m_Profile.HasSeen(Cutscenes.MotorPool.Id) && !m_TestBoss && !m_TestTeam);

        /// <summary>
        /// Trollogram kie glitched out, solo: the freeing scene, then the mission's won. (The coda, when it's built,
        /// comes between them.)
        /// </summary>
        void OnPilotFreed()
        {
            if (m_Coop)
            {
                m_World.OnBossDestroyed();
                return;
            }
            float playedFor = m_StateTime;
            PlayScene(Cutscenes.FreeingKie, false, () =>
            {
                m_Paused = false;
                SetState(GameState.Playing);
                m_StateTime = playedFor;
                // on into the coda: the back gate opens, and the run out of the base with tones2's tabk
                m_World.OpenCoda(!m_Profile.HasSeen(CodaSeen));
                if (MusicOn) Music.Play(MusicTracks.TankAlert);
            });
        }

        /// <summary>The coda's last gate is down: its prompts have been seen.</summary>
        void OnCodaCleared()
        {
            if (m_Profile.MarkSeen(CodaSeen)) m_Store.Save(m_Profile);
        }

        void StartBossIntro(BossEnemy boss, bool firstTime)
        {
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
            if (!m_SceneSaidStart) Sfx.Say("mission-start", 0.3f); // the opening's caption card already called it
            m_SceneSaidStart = false;
        }

        void EndCoop(string notice)
        {
            DropScene();
            m_AfterScene = null;
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
                    if (m_Reveal == null && CrewRevealDue) BeginCrewReveal();
                    if (m_Reveal != null)
                    {
                        TickCrewReveal(dt, keys, soldier);
                        break;
                    }
                    if (CoopReady)
                    {
                        // J doubles as fire, so the online keys are checked first
                        if (click == MenuAction.Online) { OpenOnline(); break; }
                        if (keys.Host) { GoOnline(m_Net.Host); break; }
                        if (keys.Quick) { GoOnline(m_Net.QuickMatch); break; }
                        if (keys.Join) { OpenCodeEntry(""); break; }
                    }
                    if (click == MenuAction.Settings || keys.Settings) SetState(GameState.Settings);
                    else if (CrewOpen && (click == MenuAction.Crew || soldier.SpecialPressed)) OpenDen(DenMode.Crew);
                    else if (click == MenuAction.Play || keys.Start || soldier.FirePressed || soldier.JumpPressed)
                    {
                        if (CrewOpen) OpenDen(DenMode.Solo);
                        else StartFromTitle(); // before the crew's freed, tones2 is all there is: straight in
                    }
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
                        OpenDen(DenMode.Coop);
                    }
                    break;

                case GameState.Den:
                    TickDen(keys, soldier);
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
                        if (click == MenuAction.Kit || soldier.AbilityPressed) OpenKit(m_World.Player.Def);
                        else if (click == MenuAction.Resume || keys.Pause || keys.PadPause || keys.Start) m_Paused = false;
                        else if (click == MenuAction.Restart || keys.Restart) Restart();
                        else if (click == MenuAction.Menu || keys.Back) ToTitle();
                        break;
                    }
                    if (keys.Restart) { Restart(); break; }
                    if (keys.Pause || keys.PadPause || keys.Back || (keys.Start && m_StateTime > 0.2f)) { m_Paused = true; break; }

                    m_World.Tick(dt, soldier);
                    if (MusicOn && (m_World.Victory || m_World.BossMusicOff) && Music.Playing != null) Music.Stop(fade: !m_World.BossMusicOff); // the pilot glitched out (cut dead by his big glitch-out)
                    m_OutFor = m_World.Player.Lives.IsOut ? m_OutFor + dt : 0f;
                    if (m_OutFor > Tuning.Lives.GameOverDelay) EndRun(GameState.GameOver);
                    else if (m_World.Victory && m_World.VictoryTime > 1f) EndRun(GameState.Victory);
                    break;

                case GameState.Victory when m_Debrief != null: // the world holds still behind it, so nothing can hurt tones2
                    TickDebrief(dt, keys, soldier);
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

            if (m_State == GameState.Den) MoveDen(dt); // over the kit screen too
            else if (m_DenView.Showing) m_DenView.Hide();
            m_World.Render();
            m_Hud.Refresh(m_World, m_Camera, new HudInfo
            {
                InGame = m_State == GameState.Playing || m_State == GameState.GameOver || m_State == GameState.Victory,
                Playing = m_State == GameState.Playing,
                Paused = m_Paused,
                Demo = m_State == GameState.Demo,
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
                Reveal = m_Reveal,
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
                DemoFolder = m_DemoFolder,
            });

            // The sign-in button sits over the game, on the title screen and settings only.
            bool account = (m_State == GameState.Title || m_State == GameState.Settings) && !m_KitOpen ||
                           m_Debrief != null && (m_Debrief.Now == DebriefPlayer.Part.SignIn || m_Debrief.Now == DebriefPlayer.Part.Continue) && m_Debrief.Result.OfferSignIn;
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
                case GameState.Victory: return m_Debrief != null ? MenuScreen.None : MenuScreen.Victory;
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
            bool wasOpen = m_Profile.coop, won = state == GameState.Victory;
            int best = m_Profile.Best(m_World.Mission), clears = m_Profile.Clears(m_World.Mission);
            m_Profile.RecordRun(m_World.Mission, Setting.Normal, m_World.Scoring.Points, won, solo: !m_Coop);
            m_CoopJustOpened = !wasOpen && m_Profile.coop;
            if (won && !m_Coop) Debrief(best, firstClear: clears == 0 || m_TestDebrief);
            m_Store.Save(m_Profile);
        }

        /// <summary>Profile key: the first debrief's been shown (kie's lines are for that one).</summary>
        const string DebriefSeen = "debrief";

        /// <summary>
        /// A won solo run: its XP and rank go on the profile, and the debrief shows them (docs/design/story.md, The
        /// first debrief). Co-op wins keep the plain victory screen for now.
        /// </summary>
        void Debrief(int bestBefore, bool firstClear)
        {
            World w = m_World;
            int score = w.Scoring.Points;
            var result = new DebriefResult
            {
                Score = score, Best = Math.Max(bestBefore, score), NewBest = score > bestBefore && bestBefore > 0,
                Seconds = Mathf.FloorToInt(w.Elapsed), Kills = w.Kills,
                Accuracy = w.ShotsFired > 0 ? Mathf.Clamp01(w.ShotsHit / (float)w.ShotsFired) : -1f,
                Lives = w.Player.Lives.Lives, Continues = w.Player.Lives.Continues,
                Xp = Progression.XpFor(true, w.Mission == Missions.One ? Content.Kie.Name : null, w.Kills),
                XpBefore = m_Profile.xp, ClimbBefore = m_Profile.rank,
                First = !m_Profile.HasSeen(DebriefSeen) || m_TestDebrief,
                OfferSignIn = m_Account.Available && !m_Account.SignedIn,
            };
            int climb = Progression.ClimbFor(true, firstClear);
            result.ClimbAfter = result.ClimbBefore + climb;
            m_Profile.Award(result.XpGained, climb);
            m_Profile.MarkSeen(DebriefSeen);
            Log.Game.Info("Debrief: " + result.XpGained + " XP, " + climb + " stages up the ladder" + (result.RankedUp ? ", a rank up" : ""));
            m_Debrief = new DebriefPlayer(result);
            m_DebriefView.Show(m_Debrief);
        }

        /// <summary>The debrief: taps fill each part and move on; CONTINUE goes to the title screen.</summary>
        void TickDebrief(float dt, MenuInput keys, SoldierInput soldier)
        {
            bool tap = m_DebriefView.TakeTap() || keys.Start || soldier.FirePressed || soldier.JumpPressed;
            bool leave = m_DebriefView.TakeLeave() || (m_Debrief.Now == DebriefPlayer.Part.Continue && m_StateTime > 1.2f && (tap || keys.Back));
            if (leave)
            {
                CloseDebrief();
                ToTitle();
                return;
            }
            m_Debrief.Tick(dt);
            if (tap) m_Debrief.Tap();
            m_DebriefView.Refresh();
        }

        void CloseDebrief()
        {
            m_Debrief = null;
            m_DebriefView.Hide();
        }
    }
}
