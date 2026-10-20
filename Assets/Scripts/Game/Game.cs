using System;
using System.Collections.Generic;
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
            // The test runner's own scene (PlayMode tests build the worlds they need): no game behind them.
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.StartsWith("InitTestScene")) return;
            var go = new GameObject("Pooket Slug");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Game>();
        }
    }

    public enum GameState { Title, Settings, Lobby, Den, Cutscene, Playing, GameOver, Victory, Demo }

    /// <summary>
    /// Owns the camera, the current <see cref="World"/>, the online session and the player's profile, and runs the
    /// game's state machine: title, settings, online menus, play, pause, game over and victory, and Mission one's
    /// story beats. The rest of the game's flow is in flows it owns, one per part: the cutscenes (<see cref="SceneFlow"/>),
    /// the den (<see cref="DenFlow"/>), the kit screen (<see cref="KitFlow"/>), the mission screen (<see cref="BoardFlow"/>),
    /// the demos (<see cref="DemoFlow"/>), the sandbox (<see cref="SandboxFlow"/>), the boss intro
    /// (<see cref="BossIntroFlow"/>), the music (<see cref="MusicFlow"/>) and the
    /// debrief (<see cref="DebriefFlow"/>). The HUD and the menus are views
    /// (<see cref="HudView"/>, <see cref="MenuView"/>) that it hands what to show each frame.
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
        float m_OutFor;         // seconds the game has had nobody left in it (out of lives and continues)
        LaunchFlags m_Flags;    // ?unlock=, ?test= and the rest, read once in Awake
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
        string m_PageUrl;      // the page's address without its query, for invite links
        string m_DemoFolder;   // where the demo clips are served: demos/ beside the page (null in the editor)

        // ------------------------------------------------------------------ what the flows share

        internal Camera Camera => m_Camera;
        internal World World => m_World;
        internal float StateTime => m_StateTime;
        internal INetSession Net => m_Net;
        internal bool Coop => m_Coop;
        internal LaunchFlags Flags => m_Flags;
        internal IProfileStore Store => m_Store;
        internal CloudAccount Account => m_Account;
        internal Options Options => m_Options;
        internal Profile Profile => m_Profile;
        /// <summary>The profile's once-only story beats (the coda's prompts, the first debrief, a boss reveal).</summary>
        internal StoryRecord Story => new StoryRecord(m_Profile);
        internal string PageUrl => m_PageUrl;
        /// <summary>The mission a solo game plays; a shared game plays the host's.</summary>
        internal MissionDef Mission { get; set; } = Missions.One;
        /// <summary>The setting a solo game plays (hard is only ever open in co-op).</summary>
        internal Setting Setting { get; set; } = Setting.Normal;

        internal SceneFlow Scenes { get; private set; }
        internal DenFlow Den { get; private set; }
        internal KitFlow Kit { get; private set; }
        internal BoardFlow Board { get; private set; }
        internal BayFlow Bay { get; private set; }
        internal DemoFlow Demos { get; private set; }
        internal SandboxFlow Sandbox { get; private set; }
        internal BossIntroFlow Intro { get; private set; }
        internal MusicFlow MissionMusic { get; private set; }
        internal DebriefFlow Debrief { get; private set; }

        /// <summary>Co-op shows only once Mission one has been beaten solo (progressive disclosure).</summary>
        bool CoopOpen => m_Profile.coop || m_Flags.UnlockCoop;
        /// <summary>
        /// The den (and the crew and co-op in it) opens once the story's run before it is beaten (<see cref="StartFlow"/>);
        /// <c>?unlock=crew</c> or <c>coop</c> opens it for testing.
        /// </summary>
        bool CrewOpen => StartFlow.DenOpen(m_Profile) || m_Flags.UnlockCoop || m_Flags.UnlockCrew;
        /// <summary>Who can be picked: the crewmates freed so far (any <c>?unlock=</c> frees them all, for testing).</summary>
        internal IReadOnlyList<SoldierDef> Freed => Crew.Freed(m_Profile, m_Flags.UnlockCoop || m_Flags.UnlockCrew);
        internal SoldierDef Soldier => Crew.Pick(m_Options.Crew, Freed);
        /// <summary>The tabks won so far (<c>?unlock=crew</c> or <c>tank</c> gives them all, for testing), and the one loaded on the bay's stands.</summary>
        internal IReadOnlyList<TabkCard> TabksWon => TabkCards.Won(m_Profile, m_Flags.UnlockCrew || m_Flags.UnlockTank);
        internal TabkCard LoadedTabk => TabkCards.Loaded(m_Options.Tabk, TabksWon);
        internal bool CoopReady => m_Net != null && CoopOpen;

        void Awake()
        {
            string url = Application.absoluteURL;
            m_Flags = LaunchFlags.Read(url);
            if (m_Flags.Mission != null) Mission = m_Flags.Mission; // ?mission=2
            Log.Configure(m_Flags.LogLevels); // ?log=net:debug,game:warn for this visit
            Spike.Configure(url); // ?spike=craters,partner for this visit (before the art and sounds, which make what spikes need)
            Art.Init();
            Sfx.Init(gameObject);
            Music.Init(gameObject);
            m_Options = OptionsStore.Load();
            ApplyOptions();
            MusicFlow.Prepare(); // after the options: with the music off, nothing's rendered up front

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

            Intro = new BossIntroFlow();
            MissionMusic = new MusicFlow(this);
            Sandbox = new SandboxFlow(this);
            m_World = NewWorld(Mission);
            m_Hud = new HudView(transform);
            m_Menu = new MenuView(transform);
            Scenes = new SceneFlow(this, transform);
            Debrief = new DebriefFlow(this, transform);
            Den = new DenFlow(this, transform);
            Bay = new BayFlow(this, transform);
            Kit = new KitFlow();
            Board = new BoardFlow(this);
            Demos = new DemoFlow(this);
            m_PageUrl = PageQuery.Base(url);
            string folder = PageQuery.Folder(url);
            m_DemoFolder = folder.Length > 0 ? folder + "demos/" : null;
            SetState(GameState.Title);
            GameInput.TouchActive = Application.isMobilePlatform; // until the player shows otherwise

            Scenes.Fetch(url);
            if (Demos.Start(m_Flags.Demo)) return; // a demo is all this visit plays
            if (Scenes.StartDraft(PageQuery.Get(url, "scene"), url)) return; // and so is a draft scene
            if (Sandbox.Start(PageQuery.Get(url, "sandbox"), url)) return; // and so is a sandbox: solo, and never recorded
            if (NetSessions.Factory != null && NetConfig.Configured) m_Net = NetSessions.Factory(gameObject);
            // An invite link with a room in it (?room=, see CoopRoom) waits for CO-OP like any visit; co-op still waits
            // until Mission one is beaten, so before then the link starts that.
            if (m_Net != null && !string.IsNullOrEmpty(m_Flags.Room) && !CoopOpen)
            {
                m_StartNote = "CO-OP OPENS ONCE YOU'VE BEATEN THIS MISSION";
                StartFromTitle();
            }
        }

        // ------------------------------------------------------------------ worlds

        /// <summary>A world for <paramref name="mission"/>, shared when <paramref name="net"/> is given.</summary>
        World NewWorld(MissionDef mission, INetLink net = null)
        {
            if (Sandbox.Active) return Sandbox.MakeWorld(m_Camera);
            var world = new World(m_Camera, mission, net, Soldier) { TanksToCall = m_Flags.UnlockTank || m_Flags.Tank ? 1 : mission.Story.CallIns, SkipToPilot = m_Flags.Pilot, WeakPilot = m_Flags.WeakPilot, TestTeam = m_Flags.Team,
                BossTabk = m_Flags.KcajTank ? BossTabk.Kcaj : mission.Boss,
                Tabk = TabkKit.Of(m_Flags.KieTabk ? TabkCards.Kie : m_Flags.SoftServe ? TabkCards.Kcaj : LoadedTabk) };
            // a replay meets the boss a second time: the quick intro (?test=reveal plays the first meeting whatever's saved)
            world.QuickBossIntro = !m_Flags.Reveal && Story.QuickBossIntro(mission, lostAtBoss: false);
            Wire(world, coda: true);
            if (m_Flags.Crate)
            {
                // Just past where a joining partner appears (1.25 ahead of the start), so whoever runs first takes
                // it before any jump. Only the authority places it; a follower gets it from the snapshot.
                float x = world.Level.PlayerStart.x + 2.75f;
                world.Spawner.SpawnPickup(Content.BombCrate, new Vector2(x, world.Level.TopSurfaceY(x)));
            }
            if (m_Flags.OneUp)
            {
                float x = world.Level.PlayerStart.x + 4.5f;
                world.Spawner.SpawnPickup(Content.OneUp, new Vector2(x, world.Level.TopSurfaceY(x)));
            }
            if (m_Flags.Boss) StartAtBoss(world);
            if (m_Flags.Coda && net == null) StartAtCoda(world, true);
            if (m_Flags.Tank) world.ParkTank(world.Player.Pos.x + Content.PlayerTank.HalfWidth + 4f); // each player's own, a few steps ahead
            return world;
        }

        /// <summary>Hooks a world's story events up to the game, and clears any boss intro left from the last one.</summary>
        internal void Wire(World world, bool coda)
        {
            world.BossIntro += OnBossIntro;
            world.BossFightStarted += _ => MissionMusic.BossFightStarted();
            world.PilotFreed += OnPilotFreed;
            world.TankWrecked += MissionMusic.TankWrecked;
            if (coda) world.CodaCleared += OnCodaCleared;
            Intro.Clear();
        }

        /// <summary>The solo world again, for the mission picked: the title's, the den's, a new solo game's.</summary>
        internal void NewTitleWorld()
        {
            m_World.Dispose();
            m_World = NewWorld(Mission);
        }

        /// <summary>A bare world in place of this one (a demo's): no launch flags, nothing wired, never shared.</summary>
        internal World ReplaceWorld(MissionDef mission, SoldierDef soldier)
        {
            m_World.Dispose();
            m_World = new World(m_Camera, mission, null, soldier);
            return m_World;
        }

        /// <summary>?test=boss: puts the soldier a screen short of the boss, to try boss fights without the run there.</summary>
        internal static void StartAtBoss(World world)
        {
            foreach (Spawn s in world.Level.Spawns)
            {
                if (Content.EnemyFor(s.Kind)?.Brain != EnemyBrain.Tank) continue;
                StartAt(world, s.Pos.x - 16f);
                return;
            }
        }

        /// <summary>
        /// Starts at the coda: the back gate's open, the boss is left out, and there's a tabk to call
        /// (a restart at its checkpoint, or <c>?test=coda</c>).
        /// </summary>
        internal static void StartAtCoda(World world, bool prompts)
        {
            float x = world.Level.CodaX;
            if (x < 0f) return;
            world.Spawner.SkipBefore = x;
            world.OpenCoda(prompts, restart: true);
            StartAt(world, x - 1f); // in the open gateway, just past kie asleep by the wall

        }

        /// <summary>Puts the soldier, and the camera, at <paramref name="x"/>; what's behind is left behind.</summary>
        internal static void StartAt(World world, float x)
        {
            world.Player.Pos = new Vector2(x, world.Level.TopSurfaceY(x));
            world.View.JumpTo(x + Tuning.Camera.LookAhead);
            world.View.Tick(0f);
            world.Spawner.SpawnAhead();
        }

        // ------------------------------------------------------------------ the state machine

        internal void SetState(GameState state)
        {
            m_State = state;
            m_StateTime = 0f;
            m_OutFor = 0f;
        }

        internal void StartSolo()
        {
            m_Paused = false;
            SetState(GameState.Playing);
            Log.Game.Info("Mission start: " + Mission.Name); // tools/touch.mjs waits for it
            Sfx.Play(Sfx.Start);
            if (!Scenes.TakeSaidStart()) Sfx.Say(Cues.StartLine(Mission), 0.3f); // the opening's caption card already called it
            if (m_Flags.Debrief) m_World.OnBossDestroyed();
            MissionMusic.MissionStart();
        }

        /// <summary>START on the title screen, or a solo START in the den: the mission's opening scene first, if it has one.</summary>
        internal void StartFromTitle()
        {
            // ?test=boss, pilot and team are for trying something at once: they start without the opening
            string opening = Mission.Story.Opening;
            if (opening != null && !Sandbox.Active && !m_Flags.Boss && !m_Flags.Team && !m_Flags.Coda && !m_Flags.Debrief) Scenes.Play(Scenes.Library.Get(opening), false, StartSolo);
            else StartSolo();
        }

        internal void Restart()
        {
            // Lost after the checkpoint at the boss door: start again there (docs/design/levels.md, Missions).
            float checkpoint = m_State == GameState.GameOver ? m_World.CheckpointX : -1f;
            NewTitleWorld();
            if (checkpoint >= 0f && m_World.Level.CodaX >= 0f && checkpoint >= m_World.Level.CodaX)
            {
                // lost in the coda: start again at its start, not at the boss (docs/design/story.md, The coda)
                StartAtCoda(m_World, Story.CodaPrompts);
                m_World.CheckpointX = checkpoint;
            }
            else if (checkpoint >= 0f)
            {
                m_World.CheckpointX = checkpoint;
                m_World.QuickBossIntro = Story.QuickBossIntro(Mission, lostAtBoss: true); // the second time: the tank's already in place
                StartAt(m_World, checkpoint);
            }
            m_StartNote = null;
            StartSolo();
        }

        /// <summary>Sets up <paramref name="mission"/> to play next, on its own world (null: as it is).</summary>
        void PlayNext(MissionDef mission)
        {
            if (mission == null || mission == Mission) return;
            Mission = mission;
            Setting = Setting.Normal;
            NewTitleWorld();
        }

        /// <summary>
        /// After a won solo run's debrief (<see cref="StartFlow"/>): straight on into the story's next mission, with its
        /// opening scene, until the den opens; then the den.
        /// </summary>
        internal void AfterDebrief()
        {
            if (m_Coop)
            {
                BackToCoopDen();
                return;
            }
            MissionDef next = StartFlow.Next(m_Profile);
            ToTitle();
            if (Sandbox.Active) return;
            if (CrewOpen) Den.Show(DenMode.Solo);
            else if (next != null)
            {
                PlayNext(next);
                StartFromTitle();
            }
        }

        /// <summary>Back to the title screen from a solo game.</summary>
        internal void ToTitle()
        {
            if (Sandbox.Active)
            {
                // a sandbox visit plays only the sandbox (its tuning stays turned): MENU starts it again
                m_Paused = false;
                Restart();
                return;
            }
            NewTitleWorld();
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
            return "SIGN IN AT THE TOP LEFT TO SAVE YOUR PROGRESS ONLINE";
        }

        // ------------------------------------------------------------------ the mission's story beats (its MissionStory)

        void OnBossIntro(BossEnemy boss, bool firstTime)
        {
            MissionMusic.BossIntro();
            if (boss is TankBoss tank && PlaysBossReveal(firstTime))
            {
                // The reveal (Mission one's motor pool, Mission two's cold store) is the tabk's entrance, so after it the
                // tabk's in place and the intro goes straight to the banner, the name card and the music, its pilot met in
                // the scene (docs/design/cutscenes/mission-one.md, The boss intro). In co-op both phones play it together.
                m_BossRevealed = m_World;
                float playedFor = m_StateTime; // the mission's clock (and its start banner) carry on from where they were
                bool together = m_Coop && Net.Link.Partners.Count > 0;
                CutsceneDef scene = together ? Scenes.Library.ForCoop(Mission.Story.BossReveal, Scenes.PartnerVersions()) : Scenes.Library.Get(Mission.Story.BossReveal);
                Scenes.Play(scene, together, () =>
                {
                    tank.SkipEntrance();
                    m_Paused = false;
                    SetState(GameState.Playing);
                    m_StateTime = playedFor;
                    Intro.Start(boss, true, Mission.Story, revealed: true);
                });
                return;
            }
            Intro.Start(boss, firstTime, Mission.Story);
        }

        /// <summary>
        /// The super shows on the HUD, the touch pad and the controls line: it's charged once (or co-op is open). Until
        /// then it's hidden, so a new player meets it when it's ready (docs/design/player.md).
        /// </summary>
        internal bool SuperShown => StoryRecord.ShowsSuper(m_Profile);

        /// <summary>Seconds left on the one-off prompt for the super's first charge (it blinks on the HUD's coach line).</summary>
        float m_SuperPrompt;
        const float SuperPromptTime = 6f;

        /// <summary>The first time the soldier's super is charged, remember it (it shows from then on) and prompt it once.</summary>
        void NoteSuper(float dt)
        {
            // the prompt shows until the super's fired, or for a few seconds
            m_SuperPrompt = m_World.Player.Kit.SuperReady ? Mathf.Max(0f, m_SuperPrompt - dt) : 0f;
            if (Sandbox.Active || !m_World.Player.Kit.SuperReady || SuperShown) return;
            if (Story.Mark(StoryRecord.Super)) m_Store.Save(m_Profile);
            Log.Game.Info("The super's charged for the first time");
            m_SuperPrompt = SuperPromptTime;
        }

        /// <summary>The mission's boss reveal plays as its boss wakes (<see cref="MissionStory.PlaysBossReveal"/>), once per world.</summary>
        bool PlaysBossReveal(bool firstTime)
        {
            MissionStory story = Mission.Story;
            return m_BossRevealed != m_World && story.PlaysBossReveal(Run, firstTime, story.BossReveal != null && Story.Due(story.BossReveal));
        }

        /// <summary>How this mission's being played, for its story's rules.</summary>
        StoryRun Run => new StoryRun(m_Coop, Sandbox.Active, m_Flags.Testing, m_Flags.Reveal);

        /// <summary>The world whose boss reveal has played, so its boss waking again doesn't play it twice.</summary>
        World m_BossRevealed;

        /// <summary>
        /// The boss's pilot is freed (its Trollogram glitched out): the mission's scene for it, solo or (where the story
        /// says so, Mission two's) on both phones in co-op, then its coda, or the win.
        /// </summary>
        void OnPilotFreed()
        {
            MissionStory story = Mission.Story;
            if (!story.PlaysPilotFreed(Run))
            {
                m_World.OnBossDestroyed();
                return;
            }
            float playedFor = m_StateTime;
            CutsceneDef scene = m_Coop ? Scenes.Library.ForCoop(story.PilotFreed, Scenes.PartnerVersions()) : Scenes.Library.Get(story.PilotFreed);
            Scenes.Play(scene, m_Coop, () =>
            {
                m_Paused = false;
                SetState(GameState.Playing);
                m_StateTime = playedFor;
                if (!story.Coda)
                {
                    m_World.OnBossDestroyed();
                    return;
                }
                // on into the coda: the back gate opens, and the run out of the base with tones2's tabk
                m_World.OpenCoda(Story.CodaPrompts);
                MissionMusic.Coda();
            });
        }

        /// <summary>The coda's last gate is down: its prompts have been seen.</summary>
        void OnCodaCleared()
        {
            if (Story.Mark(StoryRecord.Coda)) m_Store.Save(m_Profile);
        }

        // ------------------------------------------------------------------ settings

        void ApplyOptions()
        {
            Sfx.Muted = !m_Options.Sound;
            Music.Muted = !m_Options.Sound || m_Options.Music == Volume.Off;
            Music.Gain = Options.Gain(m_Options.Music);
            Sfx.EffectsVolume = Options.Gain(m_Options.Effects);
            Sfx.VoicesVolume = Options.Gain(m_Options.Voices);
            ScrollCamera.Shaking = m_Options.Shake;
        }

        internal void ChangeOptions(Options options)
        {
            m_Options = options;
            ApplyOptions();
            OptionsStore.Save(options);
        }

        // ------------------------------------------------------------------ online co-op

        /// <summary>CO-OP: into the one room (<see cref="CoopRoom"/>), or the link's room.</summary>
        internal void JoinCoop()
        {
            if (m_Profile.MarkSeen(Reveals.Coop)) m_Store.Save(m_Profile);
            GoOnline(() => m_Net.JoinRoom(m_Flags.Room, Cleared));
        }

        /// <summary>A mission's been cleared on this phone: its games can be watched without spoilers.</summary>
        bool Cleared(int netId)
        {
            MissionDef m = Missions.ByNetId(netId);
            return m != null && (MissionBoard.Cleared(m_Profile, m) || m_Flags.UnlockCoop);
        }

        internal void GoOnline(Action connect)
        {
            m_Notice = null;
            connect();
            SetState(GameState.Lobby);
        }

        /// <summary>Connected: swap the title/solo world for the shared one (also after a co-op restart).</summary>
        internal void BeginCoop()
        {
            m_World.Dispose();
            m_World = NewWorld(Missions.ForCoop(Missions.ByNetId(m_Net.Link.Mission) ?? Mission), m_Net.Link); // Mission one is solo only
            if (m_Net.Watching) m_World.Spectate();
            m_Net.Events = m_World.Sync;
            if (m_Net.Watching) Log.Net.Info($"Watching room {m_Net.RoomCode}, {m_Net.Link.CamX:0} m in");
            else if (DropIn.UnderWay(m_Net.Link.IsAuthority, m_Net.Link.CamX)) // the co-op test's drop-in round looks for it
                Log.Net.Info($"Dropped in to a mission under way, {m_Net.Link.CamX:0} m in, with {m_World.Player.Lives.Lives} lives and our own score at 0");
            m_Coop = true;
            if (!m_Net.Watching) Log.Game.Info($"Co-op mission start: {m_World.Mission.Name}, round {m_World.Round}"); // the co-op test's debrief round looks for it
            // ?test=debrief in co-op: the room's first round is won at once (the shared world's runner wins it; the
            // partner's world follows), for the co-op test's debrief round
            if (m_Flags.Debrief && m_Net.Link.IsAuthority && m_World.Round == 0) m_World.OnBossDestroyed();
            MissionMusic.MissionStart(); // each phone plays the mission's music, following its own screen
            m_Paused = false;
            m_LeaveArmed = 0f;
            m_StartNote = null;
            SetState(GameState.Playing);
            Sfx.Play(Sfx.Start);
            if (!Scenes.TakeSaidStart()) Sfx.Say(Cues.StartLine(Mission), 0.3f); // the opening's caption card already called it
        }

        void EndCoop(string notice)
        {
            Scenes.Abandon();
            m_Net.Events = null;
            m_Coop = false;
            NewTitleWorld();
            Notice(notice);
            SetState(GameState.Title);
        }

        /// <summary>
        /// After a co-op win's debrief: back to the room's den, still together, for the next mission. The first phone
        /// out of its debrief resets the shared world for the next round; the den waits for the other (it shows them in
        /// the den, not seated, until their own debrief's done). With the room gone, the den on our own.
        /// </summary>
        void BackToCoopDen()
        {
            Scenes.Abandon();
            m_Net.Events = null;
            m_Coop = false;
            NewTitleWorld();
            m_Paused = false;
            Music.Silence();
            if (!m_Net.Ready)
            {
                m_Net.Leave();
                Notice("DISCONNECTED");
                SetState(GameState.Title);
                if (CrewOpen) Den.Show(DenMode.Solo);
                return;
            }
            m_Net.Link.RequestRestart(); // the shared world's next round (only the first ask counts)
            Log.Game.Info("Debrief done: back to the co-op den");
            Den.Show(DenMode.Coop);
        }

        /// <summary>Drops back to the title screen if the session is gone. True while co-op can go on.</summary>
        internal bool CoopAlive()
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

        // ------------------------------------------------------------------ each frame

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
            // the title's music starts with the logo's shot (title-logo-animation, S3)
            MissionMusic.Menus((m_State == GameState.Title && m_Menu.TitleMusicDue) || m_State == GameState.Settings,
                m_State == GameState.Den || m_State == GameState.Lobby, Kit.Open || Board.Open, m_State == GameState.Den && Den.BeatPlaying);
            Music.Paused = m_Paused;
            CountFrame();

            if (keys.Mute || (click == MenuAction.Sound && m_State == GameState.Settings))
            {
                Options o = m_Options;
                o.Sound = !o.Sound;
                ChangeOptions(o);
            }

            if (Sandbox.Loading) { } // the title shows behind until the sandbox's file is in
            else if (Kit.Open) Kit.Tick(click, keys, soldier);
            else if (Board.Open) Board.Tick(click, keys, soldier);
            else if (Bay.Open) Bay.Tick(keys, soldier);
            else switch (m_State)
            {
                case GameState.Demo: Demos.Tick(keys, soldier); break;
                case GameState.Title: TickTitle(click, keys, soldier); break;
                case GameState.Settings: TickSettings(click, keys); break;
                case GameState.Lobby: TickLobby(click, keys); break;
                case GameState.Den: Den.Tick(keys, soldier); break;
                case GameState.Cutscene: Scenes.Tick(dt, keys, soldier); break;
                case GameState.Playing:
                    if (!m_Paused) Intro.Tick(dt);
                    if (m_Coop) TickCoopPlay(dt, keys, soldier);
                    else TickSoloPlay(dt, click, keys, soldier);
                    break;
                case GameState.Victory when Debrief.Now != null: // the world holds still behind it, so nothing can hurt tones2
                    if (m_Coop && m_Net.Ready) m_Net.Link.SendDen(null, PooketSlug.Den.InDen); // a partner already in the den waits for us
                    Debrief.Tick(dt, keys, soldier);
                    break;
                case GameState.GameOver:
                case GameState.Victory:
                    TickEnd(dt, click, keys, soldier);
                    break;
            }

            if (m_State == GameState.Den) Den.Move(dt); // over the kit screen and the tabk bay too
            else if (Den.Showing) Den.Hide();
            if (Bay.Open) Bay.Move(dt);
            m_World.Render();
            RefreshScreens();
        }

        void CountFrame()
        {
            m_Frames++;
            m_FpsTimer += Time.unscaledDeltaTime;
            if (m_FpsTimer < 0.5f) return;
            m_Fps = m_Frames / m_FpsTimer;
            m_Frames = 0;
            m_FpsTimer = 0f;
        }

        /// <summary>
        /// The splash: START and SETTINGS (Start flow 5). Until the den opens, START plays the story's next mission
        /// (StartFlow); after, it goes to the den, where the couch (who you play), the mission board and co-op are.
        /// </summary>
        void TickTitle(MenuAction click, MenuInput keys, SoldierInput soldier)
        {
            if (click == MenuAction.Settings || keys.Settings) SetState(GameState.Settings);
            else if (click == MenuAction.Play || keys.Start || soldier.FirePressed || soldier.JumpPressed)
            {
                if (CrewOpen) Den.Show(DenMode.Solo);
                else
                {
                    if (m_Flags.Mission == null) PlayNext(StartFlow.Next(m_Profile)); // ?mission=2 plays that one
                    StartFromTitle();
                }
            }
        }

        /// <summary>SETTINGS: each button steps its option on (SOUND is everywhere's, above).</summary>
        void TickSettings(MenuAction click, MenuInput keys)
        {
            Options o = m_Options;
            if (click == MenuAction.Shake || keys.Shake) o.Shake = !o.Shake;
            else if (click == MenuAction.Effects) o.Effects = Options.Next(o.Effects);
            else if (click == MenuAction.Voices) o.Voices = Options.Next(o.Voices);
            else if (click == MenuAction.Music) o.Music = Options.Next(o.Music);
            else if (click == MenuAction.LeftHanded) o.LeftHanded = !o.LeftHanded;
            else
            {
                if (click == MenuAction.Back || keys.Back || keys.Settings) SetState(GameState.Title);
                return;
            }
            ChangeOptions(o);
            if (click == MenuAction.Effects) Sfx.Play(Sfx.Pickup, 0.8f); // a taste of the new level
            else if (click == MenuAction.Voices) Sfx.Say("mission-start");
        }

        /// <summary>Joining the co-op room: into the den when it's ready (or straight to the game, watching), back on a failure.</summary>
        void TickLobby(MenuAction click, MenuInput keys)
        {
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
                if (m_Net.Watching) BeginCoop(); // watching skips the den: straight to the game as the players see it
                else Den.Show(DenMode.Coop);
            }
        }

        /// <summary>A shared game: no pausing; BACK twice leaves; it's over when everyone is out of lives and continues.</summary>
        void TickCoopPlay(float dt, MenuInput keys, SoldierInput soldier)
        {
            if (!CoopAlive()) return;
            if (m_Net.Link.Round != m_World.Round) { BeginCoop(); return; } // partner restarted the mission
            m_LeaveArmed -= dt;
            if (keys.Back || keys.PadPause)
            {
                if (m_LeaveArmed > 0f)
                {
                    m_Net.Leave();
                    EndCoop(null);
                    return;
                }
                m_LeaveArmed = 2f;
            }
            m_World.Tick(dt, soldier);
            NoteSuper(dt);
            MissionMusic.Tick(m_World);
            m_OutFor = m_World.AllOut ? m_OutFor + dt : 0f;
            EndIfOver();
        }

        /// <summary>Playing solo: the pause menu, a restart, a sandbox's own tick, or the world's.</summary>
        void TickSoloPlay(float dt, MenuAction click, MenuInput keys, SoldierInput soldier)
        {
            if (m_Paused)
            {
                if (click == MenuAction.Kit || soldier.AbilityPressed) Kit.Show(m_World.Player.Def);
                else if (click == MenuAction.Resume || keys.Pause || keys.PadPause || keys.Start) m_Paused = false;
                else if (click == MenuAction.Restart || keys.Restart) Restart();
                else if (click == MenuAction.Menu || keys.Back) ToTitle();
                return;
            }
            if (keys.Restart) { Restart(); return; }
            if (keys.Pause || keys.PadPause || keys.Back || (keys.Start && m_StateTime > 0.2f)) { m_Paused = true; return; }

            if (Sandbox.Active)
            {
                if (Sandbox.Tick(dt, soldier)) return;
            }
            else if (!Intro.Holding) m_World.Tick(dt, soldier); // the pilot's intro holds the action (solo only)
            NoteSuper(dt);
            MissionMusic.Tick(m_World);
            m_OutFor = m_World.Player.Lives.IsOut && !m_World.Spectating ? m_OutFor + dt : 0f;
            EndIfOver();
        }

        /// <summary>Everyone out for long enough is game over; the win, a moment after it, is victory.</summary>
        void EndIfOver()
        {
            if (m_OutFor > Tuning.Lives.GameOverDelay) EndRun(GameState.GameOver);
            else if (m_World.Victory && m_World.VictoryTime > 1f) EndRun(GameState.Victory);
        }

        /// <summary>GAME OVER or the victory screen: the world plays on behind it; restart, or back (co-op: leave the room).</summary>
        void TickEnd(float dt, MenuAction click, MenuInput keys, SoldierInput soldier)
        {
            bool ready = m_StateTime > 1.2f; // so a button mashed at the end doesn't skip the screen
            if (m_Coop)
            {
                if (!CoopAlive()) return;
                if (m_Net.Link.Round != m_World.Round) { BeginCoop(); return; }
                if (click == MenuAction.Back || keys.Back)
                {
                    m_Net.Leave();
                    EndCoop(null);
                    return;
                }
            }
            else if (ready && (click == MenuAction.Menu || keys.Back))
            {
                ToTitle();
                return;
            }
            m_World.Tick(dt, soldier);
            if (ready && (click == MenuAction.Restart || keys.Start || keys.Restart || soldier.FirePressed))
            {
                if (m_Coop) { if (!m_World.Spectating) m_Net.Link.RequestRestart(); } // the authority bumps the round; everyone rebuilds
                else Restart();
            }
        }

        /// <summary>The HUD, the menus and the sign-in button, as the frame left things.</summary>
        void RefreshScreens()
        {
            m_Hud.Refresh(m_World, m_Camera, new HudInfo
            {
                InGame = m_State == GameState.Playing || m_State == GameState.GameOver || m_State == GameState.Victory,
                Playing = m_State == GameState.Playing,
                Paused = m_Paused,
                Demo = m_State == GameState.Demo || Sandbox.Setup?.Script != null, // a scripted take is recorded the same way
                StateTime = m_StateTime,
                Note = m_StartNote ?? (m_Coop ? null : Sandbox.Active ? Sandbox.Note : Spike.Label),
                HiScore = m_Profile.Best(m_World.Mission),
                Fps = Mathf.RoundToInt(m_Fps),
                Muted = Sfx.Muted,
                Net = m_Coop ? m_Net : null,
                Watching = m_Coop && m_World.Spectating,
                PageUrl = m_PageUrl,
                LeaveArmed = m_LeaveArmed > 0f,
                Intro = Intro.Show,
                IntroName = Intro.Name,
                PilotName = Intro.PilotName,
                PilotLine = Intro.PilotLine,
                SuperShown = SuperShown || m_State == GameState.Demo, // a move's demo shows the whole kit
                SuperPrompt = m_SuperPrompt > 0f ? (GameInput.TouchActive ? "SUPER READY! TAP SU" : "SUPER READY! PRESS V") : null,
            });
            m_Menu.Refresh(new MenuInfo
            {
                Screen = MenuScreenNow(),
                StateTime = m_StateTime,
                SuperShown = SuperShown,
                Touch = GameInput.TouchActive,
                CoopJustOpened = m_CoopJustOpened,
                InCoop = m_Coop,
                Notice = m_Notice != null && Time.unscaledTime - m_NoticeTime < 8f ? m_Notice : null,
                Status = m_Net?.Status,
                Score = m_World.Scoring.Points,
                Seconds = Mathf.FloorToInt(m_World.Elapsed),
                Account = AccountLine(),
                Options = m_Options,
                Kit = Kit.Open ? KitGuide.For(Kit.Soldier) : null,
                KitMove = Kit.Move,
                KitSoldier = Kit.Open ? Kit.Soldier.Name.ToUpperInvariant() : null,
                DemoFolder = m_DemoFolder,
                Missions = Board.Open ? Board.Cards : null,
                MissionPick = Board.Pick,
                MissionHard = Board.Hard,
                MissionFromDen = Board.FromDen,
                MissionTabk = Board.Open && Board.FromDen ? LoadedTabk.Name : null,
                MissionTabkPaint = Art.Hex(LoadedTabk.Paint),
            });

            // The sign-in button sits over the game, on the title screen and settings only.
            bool account = (m_State == GameState.Title || m_State == GameState.Settings) && !Kit.Open && !Board.Open || Debrief.OffersSignIn;
            if (account != m_AccountShown)
            {
                m_AccountShown = account;
                m_Account.Show(account);
            }
        }

        MenuScreen MenuScreenNow()
        {
            if (Kit.Open) return MenuScreen.Kit;
            if (Board.Open) return MenuScreen.Missions;
            switch (m_State)
            {
                case GameState.Title: return MenuScreen.Title;
                case GameState.Settings: return MenuScreen.Settings;
                case GameState.Lobby: return MenuScreen.Connecting;
                case GameState.Playing: return m_Paused ? MenuScreen.Paused : MenuScreen.None;
                case GameState.GameOver: return MenuScreen.GameOver;
                case GameState.Victory: return Debrief.Now != null ? MenuScreen.None : MenuScreen.Victory;
                default: return MenuScreen.None;
            }
        }

        /// <summary>Shows the touch pad while playing with a touch screen, and fits it to the screen.</summary>
        void UpdatePad()
        {
            ArcadePad pad = GameInput.Pad;
            pad.Layout(Screen.width * Tuning.Hud.Height / Mathf.Max(1, Screen.height), m_Options.LeftHanded);
            bool touch = GameInput.TouchActive;
            bool playing = m_State == GameState.Playing;
            pad.Visible = touch && playing && !m_Paused;
            pad.SuperVisible = SuperShown;
            pad.PauseVisible = touch && playing; // solo: pause; co-op: leave (twice)
        }

        void EndRun(GameState state)
        {
            SetState(state);
            if (m_World.Spectating) return; // watching a room isn't a run of our own
            if (state == GameState.GameOver) MissionMusic.GameOver();
            if (Sandbox.Active) return; // nothing a sandbox does goes on the profile
            bool wasOpen = m_Profile.coop, won = state == GameState.Victory;
            int best = m_Profile.Best(m_World.Mission), clears = m_Profile.Clears(m_World.Mission);
            m_Profile.RecordRun(m_World.Mission, m_Coop ? Setting.Normal : Setting, m_World.Scoring.Points, won, solo: !m_Coop);
            m_CoopJustOpened = !wasOpen && m_Profile.coop;
            if (won) // solo or co-op: each phone's debrief, its own player's XP and rank
            {
                Debrief.Show(best, firstClear: clears == 0 || m_Flags.Debrief);
                MissionMusic.Debrief(m_World.VictoryTime);
            }
            m_Store.Save(m_Profile);
        }
    }
}
