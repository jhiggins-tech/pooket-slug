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

    public enum GameState { Title, CodeEntry, Lobby, Playing, GameOver, Victory }

    /// <summary>Owns the camera, the current <see cref="World"/>, the state machine and the IMGUI HUD.</summary>
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
        string m_Code = "";     // join box contents
        int m_CodeOpenedFrame;
        string m_Notice;
        float m_NoticeTime, m_LeaveArmed;

        HudView m_Hud;
        readonly IProfileStore m_Store = new LocalProfileStore();
        Profile m_Profile;
        string m_PageUrl; // the page's address without its query, for invite links
        GUIStyle m_Style, m_FieldStyle; // the menus, still IMGUI
        float m_Scale;

        void Awake()
        {
            Log.Configure(PageQuery.Get(Application.absoluteURL, "log")); // ?log=net:debug,game:warn for this visit
            Art.Init();
            Sfx.Init(gameObject);

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

            m_Profile = m_Store.Load();

            m_World = new World(m_Camera);
            m_Hud = new HudView(transform);
            m_PageUrl = PageUrl();
            SetState(GameState.Title);
            GameInput.TouchActive = Application.isMobilePlatform; // until the player shows otherwise

            if (NetSessions.Factory != null && NetConfig.Configured) m_Net = NetSessions.Factory(gameObject);
            // invite link (?room=ABCD-EU): open the join box with the code filled in
            string invite = RoomFromUrl(Application.absoluteURL);
            if (m_Net != null && !string.IsNullOrEmpty(invite)) OpenCodeEntry(invite);
        }

        void SetState(GameState state)
        {
            m_State = state;
            m_StateTime = 0f;
        }

        void Restart()
        {
            m_World.Dispose();
            m_World = new World(m_Camera);
            m_Paused = false;
            SetState(GameState.Playing);
            Sfx.Play(Sfx.Start);
        }

        // ------------------------------------------------------------------ online co-op

        void OpenCodeEntry(string code)
        {
            m_Code = code ?? "";
            m_CodeOpenedFrame = Time.frameCount; // don't type the J that opened the box
            SetState(GameState.CodeEntry);
        }

        void JoinWithCode()
        {
            string code = m_Code.Trim('-', ' ');
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
            m_World = new World(m_Camera, m_Net.Link);
            m_Net.Events = m_World.Sync;
            m_Coop = true;
            m_Paused = false;
            m_LeaveArmed = 0f;
            SetState(GameState.Playing);
            Sfx.Play(Sfx.Start);
        }

        void EndCoop(string notice)
        {
            m_Net.Events = null;
            m_Coop = false;
            m_World.Dispose();
            m_World = new World(m_Camera);
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
            MenuInput menu = GameInput.Menu;
            SoldierInput soldier = GameInput.Soldier;
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

            if (menu.Mute && m_State != GameState.CodeEntry) Sfx.Muted = !Sfx.Muted;

            switch (m_State)
            {
                case GameState.Title:
                    if (m_Net != null)
                    {
                        // J doubles as fire, so the online keys are checked first
                        if (menu.Host) { GoOnline(m_Net.Host); break; }
                        if (menu.Quick) { GoOnline(m_Net.QuickMatch); break; }
                        if (menu.Join) { OpenCodeEntry(""); break; }
                    }
                    if (menu.Start || menu.Tap || soldier.FirePressed || soldier.JumpPressed)
                    {
                        SetState(GameState.Playing);
                        Sfx.Play(Sfx.Start);
                    }
                    break;

                case GameState.CodeEntry:
                    if (menu.Tap) JoinWithCode(); // on a phone, an invite link fills in the code and a tap joins
                    break; // the IMGUI text field handles typing, Enter and Esc (see OnGUI)

                case GameState.Lobby:
                    if (menu.Back)
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
                        if (menu.Back || menu.PadPause)
                        {
                            if (m_LeaveArmed > 0f)
                            {
                                m_Net.Leave();
                                EndCoop(null);
                                break;
                            }
                            m_LeaveArmed = 2f;
                        }
                        // no pausing a shared game, and no game over: fallen soldiers come back
                        m_World.Tick(dt, soldier);
                        if (m_World.Victory && m_World.VictoryTime > 1f) EndRun(GameState.Victory);
                        break;
                    }
                    if (menu.Restart) { Restart(); break; }
                    if (menu.Pause || menu.PadPause || (menu.Start && m_StateTime > 0.2f)) m_Paused = !m_Paused;
                    if (m_Paused) break;

                    m_World.Tick(dt, soldier);
                    if (m_World.Player.Dead && m_World.Player.DeadTime > 1.6f) EndRun(GameState.GameOver);
                    else if (m_World.Victory && m_World.VictoryTime > 1f) EndRun(GameState.Victory);
                    break;

                case GameState.GameOver:
                case GameState.Victory:
                    if (m_Coop)
                    {
                        if (!CoopAlive()) break;
                        if (m_Net.Link.Round != m_World.Round) { BeginCoop(); break; }
                        if (menu.Back || menu.PadPause)
                        {
                            m_Net.Leave();
                            EndCoop(null);
                            break;
                        }
                    }
                    m_World.Tick(dt, soldier);
                    if (m_StateTime > 1.2f && (menu.Start || menu.Restart || menu.Tap || soldier.FirePressed))
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
                HiScore = m_Profile.Best(Missions.One),
                Fps = Mathf.RoundToInt(m_Fps),
                Muted = Sfx.Muted,
                Net = m_Coop ? m_Net : null,
                PageUrl = m_PageUrl,
                LeaveArmed = m_LeaveArmed > 0f,
            });
        }

        /// <summary>Shows the touch pad while playing with a touch screen, and fits it to the screen.</summary>
        void UpdatePad()
        {
            ArcadePad pad = GameInput.Pad;
            pad.Layout(Screen.width * Tuning.Hud.Height / Mathf.Max(1, Screen.height));
            bool touch = GameInput.TouchActive;
            bool playing = m_State == GameState.Playing;
            pad.Visible = touch && playing && !m_Paused;
            // solo: pause; co-op: leave (twice), which is also how a finished shared game is left
            pad.PauseVisible = touch && (playing || (m_Coop && m_State == GameState.Victory));
        }

        void EndRun(GameState state)
        {
            SetState(state);
            m_Profile.RecordRun(Missions.One, Setting.Normal, m_World.Scoring.Points, won: state == GameState.Victory, solo: !m_Coop);
            m_Store.Save(m_Profile);
        }

        // ------------------------------------------------------------------ HUD (IMGUI)

        void OnGUI()
        {
            if (m_World == null) return;
            if (m_Style == null)
            {
                m_Style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false, clipping = TextClipping.Overflow };
            }
            m_Scale = Screen.height / 360f;
            float vw = Screen.width / m_Scale;

            World w = m_World;

            bool blink = (int)(Time.unscaledTime * 2.5f) % 2 == 0;
            bool touch = GameInput.TouchActive;
            switch (m_State)
            {
                case GameState.Title:
                    Box(0, 0, vw, 360, new Color(0f, 0f, 0f, 0.35f));
                    Text(vw * 0.5f, 70, "POOKET SLUG", 44, new Color(1f, 0.62f, 0.2f), TextAnchor.MiddleCenter);
                    Text(vw * 0.5f, 106, "built by GameCI · hosted on GitHub Pages", 12, Color.white, TextAnchor.MiddleCenter);
                    Color keys = new Color(1f, 0.95f, 0.8f);
                    if (touch)
                    {
                        Text(vw * 0.5f, 150, "LEFT THUMB  RUN, AIM UP, CROUCH (AIM DOWN IN THE AIR)", 10, keys, TextAnchor.MiddleCenter);
                        Text(vw * 0.5f, 166, "RIGHT THUMB  FIRE (HOLD IT), JUMP, SP      DROP  DOWN + JUMP", 10, keys, TextAnchor.MiddleCenter);
                        Text(vw * 0.5f, 182, "II  PAUSE", 10, keys, TextAnchor.MiddleCenter);
                    }
                    else
                    {
                        Text(vw * 0.5f, 150, "MOVE  Arrows / WASD      JUMP  Z / Space      FIRE  X / J", 10, keys, TextAnchor.MiddleCenter);
                        Text(vw * 0.5f, 166, "GRENADE  C / L      AIM  Up / Down (in air)      DROP  Down + Jump", 10, keys, TextAnchor.MiddleCenter);
                        Text(vw * 0.5f, 182, "PAUSE  P / Enter      RESTART  R      MUTE  M      Gamepad supported", 10, keys, TextAnchor.MiddleCenter);
                    }
                    string go = touch ? "TAP" : "PRESS ENTER OR X";
                    if (blink) Text(vw * 0.5f, 222, go + (m_Net != null ? " TO PLAY SOLO" : " TO START"), 16, Color.white, TextAnchor.MiddleCenter);
                    if (m_Net != null && touch)
                        Text(vw * 0.5f, 252, "ONLINE CO-OP: OPEN A FRIEND'S INVITE LINK", 11, CoopBlue, TextAnchor.MiddleCenter);
                    else if (m_Net != null)
                        Text(vw * 0.5f, 252, "ONLINE CO-OP      H  HOST A GAME      J  JOIN WITH A CODE      Q  QUICK MATCH", 11, CoopBlue, TextAnchor.MiddleCenter);
                    else if (NetSessions.Factory != null)
                        Text(vw * 0.5f, 252, "ONLINE CO-OP IS OFF IN THIS BUILD (NO PHOTON APP ID)", 9, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter);
                    if (!string.IsNullOrEmpty(m_Notice) && Time.unscaledTime - m_NoticeTime < 8f)
                        Text(vw * 0.5f, 276, m_Notice, 12, new Color(1f, 0.5f, 0.4f), TextAnchor.MiddleCenter);
                    break;

                case GameState.CodeEntry:
                    Box(0, 0, vw, 360, new Color(0f, 0f, 0f, 0.6f));
                    Text(vw * 0.5f, 110, "JOIN A FRIEND'S GAME", 22, new Color(1f, 0.62f, 0.2f), TextAnchor.MiddleCenter);
                    Text(vw * 0.5f, 138, "TYPE THE ROOM CODE FROM THEIR SCREEN, E.G. ABCD-EU", 10, Color.white, TextAnchor.MiddleCenter);
                    CodeField(vw * 0.5f - 90f, 156f, 180f, 30f);
                    Text(vw * 0.5f, 206, touch ? "TAP TO JOIN" : "ENTER  JOIN       ESC  BACK", 11, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
                    break;

                case GameState.Lobby:
                    Box(0, 0, vw, 360, new Color(0f, 0f, 0f, 0.6f));
                    Text(vw * 0.5f, 130, "ONLINE CO-OP", 22, new Color(1f, 0.62f, 0.2f), TextAnchor.MiddleCenter);
                    string dots = new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
                    Text(vw * 0.5f, 166, (string.IsNullOrEmpty(m_Net.Status) ? "CONNECTING" : m_Net.Status.TrimEnd('.')) + dots, 13, Color.white, TextAnchor.MiddleCenter);
                    Text(vw * 0.5f, 204, "ESC  CANCEL", 11, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
                    break;

                case GameState.Playing:
                    if (m_Paused)
                    {
                        Box(0, 0, vw, 360, new Color(0f, 0f, 0f, 0.4f));
                        Text(vw * 0.5f, 160, "PAUSED", 28, Color.white, TextAnchor.MiddleCenter);
                        if (touch) Text(vw * 0.5f, 190, "TAP II TO GO ON", 11, Color.white, TextAnchor.MiddleCenter);
                    }
                    break;

                case GameState.GameOver:
                    Box(0, 0, vw, 360, new Color(0.2f, 0f, 0f, Mathf.Min(0.45f, m_StateTime)));
                    Text(vw * 0.5f, 130, "GAME OVER", 36, new Color(1f, 0.35f, 0.3f), TextAnchor.MiddleCenter);
                    Text(vw * 0.5f, 168, "SCORE " + w.Scoring.Points, 14, Color.white, TextAnchor.MiddleCenter);
                    if (blink && m_StateTime > 1.2f) Text(vw * 0.5f, 210, touch ? "TAP TO RETRY" : "PRESS ENTER TO RETRY", 14, Color.white, TextAnchor.MiddleCenter);
                    break;

                case GameState.Victory:
                    Box(0, 0, vw, 360, new Color(0f, 0f, 0f, Mathf.Min(0.35f, m_StateTime)));
                    Text(vw * 0.5f, 120, "MISSION COMPLETE!", 32, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
                    int t = Mathf.FloorToInt(w.Elapsed);
                    Text(vw * 0.5f, 160, string.Format("SCORE {0}     TIME {1}:{2:00}", w.Scoring.Points, t / 60, t % 60), 14, Color.white, TextAnchor.MiddleCenter);
                    if (blink && m_StateTime > 1.2f) Text(vw * 0.5f, 204, touch ? "TAP TO PLAY AGAIN" : "PRESS ENTER TO PLAY AGAIN", 14, Color.white, TextAnchor.MiddleCenter);
                    if (m_Coop) Text(vw * 0.5f, 228, touch ? "EXIT  LEAVE THE GAME" : "ESC  LEAVE THE GAME", 10, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
                    break;
            }

        }

        static readonly Color CoopBlue = new Color(0.6f, 0.9f, 1f);

        /// <summary>IMGUI text box for the room code; Enter joins, Esc goes back.</summary>
        void CodeField(float x, float y, float w, float h)
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                e.Use();
                JoinWithCode();
                return;
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                SetState(GameState.Title);
                return;
            }

            Box(x - 1, y - 1, w + 2, h + 2, new Color(1f, 0.62f, 0.2f));
            Box(x, y, w, h, new Color(0.13f, 0.1f, 0.15f));
            if (Time.frameCount == m_CodeOpenedFrame) return; // the J that opened the box is still in this frame's events
            if (m_FieldStyle == null)
            {
                m_FieldStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                m_FieldStyle.normal.textColor = Color.white;
            }
            m_FieldStyle.fontSize = Mathf.RoundToInt(18 * m_Scale);
            GUI.SetNextControlName("room-code");
            string typed = GUI.TextField(R(x, y, w, h), m_Code, 12, m_FieldStyle);
            GUI.FocusControl("room-code");

            var clean = new System.Text.StringBuilder(typed.Length);
            foreach (char c in typed.ToUpperInvariant())
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-') clean.Append(c);
            m_Code = clean.ToString();
        }

        Rect R(float x, float y, float w, float h) => new Rect(x * m_Scale, y * m_Scale, w * m_Scale, h * m_Scale);

        void Box(float x, float y, float w, float h, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(R(x, y, w, h), Art.WhiteTex);
            GUI.color = Color.white;
        }

        /// <summary>Draws shadowed text anchored at a point in 360-px-tall virtual coordinates.</summary>
        void Text(float x, float y, string text, int size, Color color, TextAnchor anchor)
        {
            m_Style.fontSize = Mathf.Max(8, Mathf.RoundToInt(size * m_Scale));
            m_Style.alignment = anchor;
            float px = x * m_Scale, py = y * m_Scale;
            const float W = 2000f, H = 400f;
            Rect rect;
            switch (anchor)
            {
                case TextAnchor.UpperRight: rect = new Rect(px - W, py, W, H); break;
                case TextAnchor.UpperCenter: rect = new Rect(px - W * 0.5f, py, W, H); break;
                case TextAnchor.MiddleCenter: rect = new Rect(px - W * 0.5f, py - H * 0.5f, W, H); break;
                default: rect = new Rect(px, py, W, H); break;
            }
            float o = Mathf.Max(1f, Mathf.Round(m_Scale * 0.75f));
            m_Style.normal.textColor = new Color(0.08f, 0.05f, 0.08f, color.a * 0.9f);
            GUI.Label(new Rect(rect.x + o, rect.y + o, rect.width, rect.height), text, m_Style);
            m_Style.normal.textColor = color;
            GUI.Label(rect, text, m_Style);
        }
    }
}
