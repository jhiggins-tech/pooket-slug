using UnityEngine;

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

    public enum GameState { Title, Playing, GameOver, Victory }

    /// <summary>Owns the camera, the current <see cref="World"/>, the state machine and the IMGUI HUD.</summary>
    public sealed class Game : MonoBehaviour
    {
        const string HiScoreKey = "pooket-slug.hiscore";

        Camera m_Camera;
        World m_World;
        GameState m_State;
        float m_StateTime, m_Fps, m_FpsTimer;
        int m_Frames, m_HiScore;
        bool m_Paused;

        GUIStyle m_Style;
        float m_Scale;

        void Awake()
        {
            Art.Init();
            Sfx.Init(gameObject);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(transform, false);
            m_Camera = camGo.AddComponent<Camera>();
            m_Camera.orthographic = true;
            m_Camera.orthographicSize = World.ViewHeight * 0.5f;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = new Color32(0x6c, 0xa8, 0xe0, 255);
            m_Camera.nearClipPlane = 0.1f;
            m_Camera.farClipPlane = 50f;
            camGo.AddComponent<AudioListener>();

            try { m_HiScore = PlayerPrefs.GetInt(HiScoreKey, 0); }
            catch { m_HiScore = 0; }

            m_World = new World(m_Camera);
            SetState(GameState.Title);
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

        void Update()
        {
            GameInput.Poll();
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

            if (GameInput.MutePressed) Sfx.Muted = !Sfx.Muted;

            switch (m_State)
            {
                case GameState.Title:
                    if (GameInput.StartPressed || GameInput.FirePressed || GameInput.JumpPressed)
                    {
                        SetState(GameState.Playing);
                        Sfx.Play(Sfx.Start);
                    }
                    break;

                case GameState.Playing:
                    if (GameInput.RestartPressed) { Restart(); break; }
                    if (GameInput.PausePressed || (GameInput.StartPressed && m_StateTime > 0.2f)) m_Paused = !m_Paused;
                    if (m_Paused) break;

                    m_World.Tick(dt);
                    if (m_World.Player.Dead && m_World.Player.DeadTime > 1.6f) EndRun(GameState.GameOver);
                    else if (m_World.Victory && m_World.VictoryTime > 1f) EndRun(GameState.Victory);
                    break;

                case GameState.GameOver:
                case GameState.Victory:
                    m_World.Tick(dt);
                    if (m_StateTime > 1.2f && (GameInput.StartPressed || GameInput.RestartPressed || GameInput.FirePressed)) Restart();
                    break;
            }

            m_World.Render();
        }

        void EndRun(GameState state)
        {
            SetState(state);
            if (m_World.Score > m_HiScore)
            {
                m_HiScore = m_World.Score;
                try
                {
                    PlayerPrefs.SetInt(HiScoreKey, m_HiScore);
                    PlayerPrefs.Save();
                }
                catch { /* storage may be unavailable in private browsing */ }
            }
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
            Player p = w.Player;

            // world-space popups (score, pickups)
            foreach (PopupText pop in w.Popups)
            {
                Vector3 sp = m_Camera.WorldToScreenPoint(new Vector3(pop.Pos.x, pop.Pos.y, 0f));
                Color c = pop.Color;
                c.a = Mathf.Clamp01(1.4f - pop.Age);
                Text(sp.x / m_Scale, (Screen.height - sp.y) / m_Scale, pop.Text, 9, c, TextAnchor.MiddleCenter);
            }

            if (m_State != GameState.Title)
            {
                Text(8, 5, "SCORE " + w.Score.ToString("000000"), 14, Color.white, TextAnchor.UpperLeft);
                for (int i = 0; i < p.MaxHp; i++)
                {
                    GUI.color = i < p.Hp ? Color.white : new Color(0.2f, 0.15f, 0.2f, 0.7f);
                    GUI.DrawTexture(R(8 + i * 13, 24, 12, 12), Art.HeartTex);
                }
                GUI.color = Color.white;
                string arms = p.HeavyAmmo > 0 ? "HMG " + p.HeavyAmmo : "PISTOL"; // IMGUI's default font has no infinity glyph
                Text(8, 40, "ARMS " + arms + "   BOMB " + p.Grenades, 11, new Color(1f, 0.9f, 0.5f), TextAnchor.UpperLeft);

                Text(vw - 8, 5, "HI " + Mathf.Max(m_HiScore, w.Score).ToString("000000"), 14, Color.white, TextAnchor.UpperRight);
                int secs = Mathf.FloorToInt(w.Elapsed);
                Text(vw - 8, 24, string.Format("TIME {0}:{1:00}", secs / 60, secs % 60), 11, Color.white, TextAnchor.UpperRight);

                TankBoss boss = w.Boss;
                if (boss != null && boss.Engaged && !boss.Removed)
                {
                    float bw = 180f, bx = vw * 0.5f - bw * 0.5f;
                    Text(vw * 0.5f, 6, "REBEL TANK", 10, new Color(1f, 0.6f, 0.4f), TextAnchor.UpperCenter);
                    Box(bx - 1, 21, bw + 2, 8, new Color(0.1f, 0.05f, 0.08f, 0.85f));
                    Box(bx, 22, bw * Mathf.Clamp01(boss.Hp / (float)boss.MaxHp), 6, new Color(0.9f, 0.25f, 0.2f));
                }
            }

            string build = "v" + Application.version + " · Unity " + Application.unityVersion + " · built with GameCI";
            Text(6, 360 - 14, build, 8, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperLeft);
            Text(vw - 6, 360 - 14, Mathf.RoundToInt(m_Fps) + " FPS" + (Sfx.Muted ? " · MUTED" : ""), 8, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperRight);

            bool blink = (int)(Time.unscaledTime * 2.5f) % 2 == 0;
            switch (m_State)
            {
                case GameState.Title:
                    Box(0, 0, vw, 360, new Color(0f, 0f, 0f, 0.35f));
                    Text(vw * 0.5f, 70, "POOKET SLUG", 44, new Color(1f, 0.62f, 0.2f), TextAnchor.MiddleCenter);
                    Text(vw * 0.5f, 106, "built by GameCI · hosted on GitHub Pages", 12, Color.white, TextAnchor.MiddleCenter);
                    Text(vw * 0.5f, 150, "MOVE  Arrows / WASD      JUMP  Z / Space      FIRE  X / J", 10, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
                    Text(vw * 0.5f, 166, "GRENADE  C / L      AIM  Up / Down (in air)      DROP  Down + Jump", 10, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
                    Text(vw * 0.5f, 182, "PAUSE  P / Enter      RESTART  R      MUTE  M      Gamepad supported", 10, new Color(1f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
                    if (blink) Text(vw * 0.5f, 226, "PRESS ENTER OR X TO START", 16, Color.white, TextAnchor.MiddleCenter);
                    break;

                case GameState.Playing:
                    if (m_StateTime < 2.2f && !m_Paused) Text(vw * 0.5f, 120, "MISSION 1  START!", 26, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
                    if (w.Victory) Text(vw * 0.5f, 120, "MISSION COMPLETE!", 28, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
                    if (m_Paused)
                    {
                        Box(0, 0, vw, 360, new Color(0f, 0f, 0f, 0.4f));
                        Text(vw * 0.5f, 160, "PAUSED", 28, Color.white, TextAnchor.MiddleCenter);
                    }
                    break;

                case GameState.GameOver:
                    Box(0, 0, vw, 360, new Color(0.2f, 0f, 0f, Mathf.Min(0.45f, m_StateTime)));
                    Text(vw * 0.5f, 130, "GAME OVER", 36, new Color(1f, 0.35f, 0.3f), TextAnchor.MiddleCenter);
                    Text(vw * 0.5f, 168, "SCORE " + w.Score, 14, Color.white, TextAnchor.MiddleCenter);
                    if (blink && m_StateTime > 1.2f) Text(vw * 0.5f, 210, "PRESS ENTER TO RETRY", 14, Color.white, TextAnchor.MiddleCenter);
                    break;

                case GameState.Victory:
                    Box(0, 0, vw, 360, new Color(0f, 0f, 0f, Mathf.Min(0.35f, m_StateTime)));
                    Text(vw * 0.5f, 120, "MISSION COMPLETE!", 32, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
                    int t = Mathf.FloorToInt(w.Elapsed);
                    Text(vw * 0.5f, 160, string.Format("SCORE {0}     TIME {1}:{2:00}", w.Score, t / 60, t % 60), 14, Color.white, TextAnchor.MiddleCenter);
                    if (blink && m_StateTime > 1.2f) Text(vw * 0.5f, 204, "PRESS ENTER TO PLAY AGAIN", 14, Color.white, TextAnchor.MiddleCenter);
                    break;
            }
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
