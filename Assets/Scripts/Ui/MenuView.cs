using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>Which menu is on screen.</summary>
    public enum MenuScreen { None, Title, Online, Join, Connecting, Paused, GameOver, Victory }

    /// <summary>What a menu button (or its key) asks the game to do.</summary>
    public enum MenuAction { None, Play, Online, Host, Join, Quick, Submit, Back, Resume, Restart, Menu }

    /// <summary>What the menus show, gathered by <see cref="Game"/> each frame.</summary>
    public struct MenuInfo
    {
        public MenuScreen Screen;
        public float StateTime;
        /// <summary>Show touch hints instead of keys.</summary>
        public bool Touch;
        /// <summary>Online co-op is in this build (the Photon SDK and an AppId).</summary>
        public bool Online;
        /// <summary>The Photon SDK is in, but the build has no AppId.</summary>
        public bool OnlineOff;
        /// <summary>Co-op is open to this player (progressive disclosure).</summary>
        public bool CoopOpen;
        /// <summary>Co-op has opened, and its menu hasn't been visited yet.</summary>
        public bool CoopNew;
        /// <summary>This win opened co-op.</summary>
        public bool CoopJustOpened;
        /// <summary>A shared game is on (the victory screen's second button leaves it).</summary>
        public bool InCoop;
        public string Notice, Status;
        public int Score, Seconds;
        /// <summary>The title screen's line about sign-in; null hides it.</summary>
        public string Account;
    }

    /// <summary>
    /// The menus in uGUI: title, online co-op, join, connecting, pause, game over and victory. Buttons work by
    /// click and tap; each also has a key, which <see cref="Game"/> reads, shown under it on a keyboard. Only
    /// what the player has met is shown: co-op appears once Mission one is beaten solo.
    /// </summary>
    public sealed class MenuView
    {
        static readonly Color Orange = new Color(1f, 0.62f, 0.2f), Keys = new Color(1f, 0.95f, 0.8f);
        static readonly Color CoopBlue = new Color(0.6f, 0.9f, 1f), Gold = new Color(1f, 0.85f, 0.3f);
        static readonly Vector2 Wide = new Vector2(150f, 26f), Narrow = new Vector2(110f, 26f);

        readonly RectTransform m_Root;
        readonly Image m_Dim;
        readonly RectTransform m_Title, m_Online, m_Join, m_Connecting, m_Paused, m_GameOver, m_Victory;
        readonly Text m_Controls1, m_Controls2, m_Controls3, m_OnlineOff, m_Account, m_Notice, m_NewBadge, m_Status, m_Score, m_Result, m_Opened;
        readonly Button m_Play, m_Coop, m_Retry, m_GameOverMenu, m_Again, m_SecondVictory;
        readonly Text m_PlayKey, m_CoopKey, m_HostKey, m_JoinKey, m_QuickKey, m_OnlineBackKey, m_SubmitKey, m_JoinBackKey, m_CancelKey;
        readonly Text m_ResumeKey, m_RestartKey, m_MenuKey, m_RetryKey, m_GameOverMenuKey, m_AgainKey, m_SecondKey, m_SecondLabel;
        readonly InputField m_Code;
        MenuScreen m_Shown;
        int m_FocusFrame = -1;
        MenuAction m_Clicked;
        bool m_SecondIsLeave; // the victory screen's second button leaves a shared game instead of going to the menu

        public MenuView(Transform parent)
        {
            m_Root = Ui.Canvas(parent, "Menus", 10);
            m_Root.gameObject.AddComponent<GraphicRaycaster>();
            var events = new GameObject("Event system", typeof(EventSystem));
            events.transform.SetParent(parent, false);
            events.GetComponent<EventSystem>().sendNavigationEvents = false; // keys belong to the game (see Ui.Button)
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); // assigns its default actions
#else
            events.AddComponent<StandaloneInputModule>();
#endif

            m_Dim = Ui.Picture(m_Root, "Dim", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, Color.clear);
            Stretch(m_Dim.rectTransform);

            // title
            m_Title = Ui.Fill(m_Root, "Title");
            Line(m_Title, 110f, 44, Orange).text = "POOKET SLUG";
            Line(m_Title, 74f, 12, Color.white).text = "built by GameCI · hosted on GitHub Pages";
            m_Controls1 = Line(m_Title, 30f, 10, Keys);
            m_Controls2 = Line(m_Title, 14f, 10, Keys);
            m_Controls3 = Line(m_Title, -2f, 10, Keys);
            m_Play = Ui.Button(m_Title, "PLAY", new Vector2(0f, -46f), Wide, () => Click(MenuAction.Play), out m_PlayKey);
            m_Coop = Ui.Button(m_Title, "CO-OP", new Vector2(85f, -46f), Narrow, () => Click(MenuAction.Online), out m_CoopKey);
            m_NewBadge = Ui.Label(m_Coop.GetComponent<RectTransform>(), "New", Ui.TopRight, new Vector2(6f, 7f), 9, TextAnchor.MiddleCenter, Gold);
            m_NewBadge.text = "NEW!";
            m_OnlineOff = Line(m_Title, -84f, 9, new Color(1f, 1f, 1f, 0.6f));
            m_OnlineOff.text = "ONLINE CO-OP IS OFF IN THIS BUILD (NO PHOTON APP ID)";
            m_Account = Line(m_Title, -125f, 9, new Color(1f, 1f, 1f, 0.75f));
            m_Notice = Line(m_Root, -100f, 12, new Color(1f, 0.5f, 0.4f));

            // online co-op
            m_Online = Ui.Fill(m_Root, "Online");
            Line(m_Online, 74f, 22, Orange).text = "ONLINE CO-OP";
            Line(m_Online, 50f, 10, CoopBlue).text = "PLAY THE MISSION WITH A FRIEND";
            Ui.Button(m_Online, "HOST A GAME", new Vector2(0f, 16f), Wide, () => Click(MenuAction.Host), out m_HostKey);
            Ui.Button(m_Online, "JOIN WITH A CODE", new Vector2(0f, -20f), Wide, () => Click(MenuAction.Join), out m_JoinKey);
            Ui.Button(m_Online, "QUICK MATCH", new Vector2(0f, -56f), Wide, () => Click(MenuAction.Quick), out m_QuickKey);
            Ui.Button(m_Online, "BACK", new Vector2(0f, -100f), Narrow, () => Click(MenuAction.Back), out m_OnlineBackKey);

            // join with a code
            m_Join = Ui.Fill(m_Root, "Join");
            Line(m_Join, 70f, 22, Orange).text = "JOIN A FRIEND'S GAME";
            Line(m_Join, 42f, 10, Color.white).text = "TYPE THE ROOM CODE FROM THEIR SCREEN, E.G. ABCD";
            m_Code = Ui.TextBox(m_Join, new Vector2(0f, 9f), new Vector2(180f, 30f), 12, CodeChar, "ABCD");
            Ui.Button(m_Join, "JOIN", new Vector2(-62f, -38f), Narrow, () => Click(MenuAction.Submit), out m_SubmitKey);
            Ui.Button(m_Join, "BACK", new Vector2(62f, -38f), Narrow, () => Click(MenuAction.Back), out m_JoinBackKey);

            // connecting
            m_Connecting = Ui.Fill(m_Root, "Connecting");
            Line(m_Connecting, 50f, 22, Orange).text = "ONLINE CO-OP";
            m_Status = Line(m_Connecting, 14f, 13, Color.white);
            Ui.Button(m_Connecting, "CANCEL", new Vector2(0f, -30f), Narrow, () => Click(MenuAction.Back), out m_CancelKey);

            // paused
            m_Paused = Ui.Fill(m_Root, "Paused");
            Line(m_Paused, 30f, 28, Color.white).text = "PAUSED";
            Ui.Button(m_Paused, "RESUME", new Vector2(-120f, -20f), Narrow, () => Click(MenuAction.Resume), out m_ResumeKey);
            Ui.Button(m_Paused, "RESTART", new Vector2(0f, -20f), Narrow, () => Click(MenuAction.Restart), out m_RestartKey);
            Ui.Button(m_Paused, "MENU", new Vector2(120f, -20f), Narrow, () => Click(MenuAction.Menu), out m_MenuKey);

            // game over
            m_GameOver = Ui.Fill(m_Root, "Game over");
            Line(m_GameOver, 50f, 36, new Color(1f, 0.35f, 0.3f)).text = "GAME OVER";
            m_Score = Line(m_GameOver, 12f, 14, Color.white);
            m_Retry = Ui.Button(m_GameOver, "RETRY", new Vector2(-62f, -36f), Narrow, () => Click(MenuAction.Restart), out m_RetryKey);
            m_GameOverMenu = Ui.Button(m_GameOver, "MENU", new Vector2(62f, -36f), Narrow, () => Click(MenuAction.Menu), out m_GameOverMenuKey);

            // victory
            m_Victory = Ui.Fill(m_Root, "Victory");
            Line(m_Victory, 60f, 32, Gold).text = "MISSION COMPLETE!";
            m_Result = Line(m_Victory, 20f, 14, Color.white);
            m_Opened = Line(m_Victory, -2f, 12, CoopBlue);
            m_Opened.text = "CO-OP IS OPEN: PLAY ONLINE WITH A FRIEND FROM THE MENU";
            m_Again = Ui.Button(m_Victory, "PLAY AGAIN", new Vector2(-70f, -40f), Wide, () => Click(MenuAction.Restart), out m_AgainKey);
            m_SecondVictory = Ui.Button(m_Victory, "MENU", new Vector2(70f, -40f), Wide, () => Click(m_SecondIsLeave ? MenuAction.Back : MenuAction.Menu), out m_SecondKey);
            m_SecondLabel = m_SecondVictory.transform.Find("Label").GetComponent<Text>();

            Show(MenuScreen.None);
        }

        /// <summary>The button clicked or tapped since the last call, or None.</summary>
        public MenuAction TakeClick()
        {
            MenuAction a = m_Clicked;
            m_Clicked = MenuAction.None;
            return a;
        }

        /// <summary>The join screen's room code.</summary>
        public string Code
        {
            get => m_Code.text;
            set => m_Code.text = value ?? "";
        }

        public void Refresh(in MenuInfo info)
        {
            if (info.Screen != m_Shown) Show(info.Screen);
            bool keys = !info.Touch;
            switch (info.Screen)
            {
                case MenuScreen.Title:
                    Dim(new Color(0f, 0f, 0f, 0.35f));
                    if (info.Touch)
                    {
                        m_Controls1.text = "LEFT THUMB  RUN, AIM UP, CROUCH (AIM DOWN IN THE AIR)";
                        m_Controls2.text = "RIGHT THUMB  FIRE (HOLD IT), JUMP, SP, SU (SUPER, FROM KILLS)      DROP  DOWN + JUMP";
                        m_Controls3.text = "II  PAUSE";
                    }
                    else
                    {
                        m_Controls1.text = "MOVE  Arrows / WASD      JUMP  Z / Space      FIRE  X / J";
                        m_Controls2.text = "GRENADE  C / L      SUPER  V / U      AIM  Up / Down (in air), + run for 45°      DROP  Down + Jump";
                        m_Controls3.text = "PAUSE  P / Enter      RESTART  R      MUTE  M      Gamepad supported";
                    }
                    bool coop = info.Online && info.CoopOpen;
                    Ui.Show(m_Coop, coop);
                    m_Play.GetComponent<RectTransform>().anchoredPosition = new Vector2(coop ? -85f : 0f, -46f);
                    Ui.Show(m_NewBadge, info.CoopNew && (int)(Time.unscaledTime * 2.5f) % 2 == 0);
                    Key(m_PlayKey, keys, "ENTER");
                    Key(m_CoopKey, keys, "H HOST · J JOIN · Q QUICK");
                    Ui.Show(m_OnlineOff, info.OnlineOff && info.CoopOpen);
                    Ui.Show(m_Account, info.Account != null);
                    if (info.Account != null && m_Account.text != info.Account) m_Account.text = info.Account;
                    break;
                case MenuScreen.Online:
                    Dim(new Color(0f, 0f, 0f, 0.6f));
                    Key(m_HostKey, keys, "H");
                    Key(m_JoinKey, keys, "J");
                    Key(m_QuickKey, keys, "Q");
                    Key(m_OnlineBackKey, keys, "ESC");
                    break;
                case MenuScreen.Join:
                    Dim(new Color(0f, 0f, 0f, 0.6f));
                    Key(m_SubmitKey, keys, "ENTER");
                    Key(m_JoinBackKey, keys, "ESC");
                    // One frame late, so the J that opened this screen isn't typed into the box.
                    if (Time.frameCount == m_FocusFrame) m_Code.ActivateInputField();
                    break;
                case MenuScreen.Connecting:
                    Dim(new Color(0f, 0f, 0f, 0.6f));
                    string dots = new string('.', 1 + (int)(Time.unscaledTime * 3f) % 3);
                    m_Status.text = (string.IsNullOrEmpty(info.Status) ? "CONNECTING" : info.Status.TrimEnd('.')) + dots;
                    Key(m_CancelKey, keys, "ESC");
                    break;
                case MenuScreen.Paused:
                    Dim(new Color(0f, 0f, 0f, 0.4f));
                    Key(m_ResumeKey, keys, "P");
                    Key(m_RestartKey, keys, "R");
                    Key(m_MenuKey, keys, "ESC");
                    break;
                case MenuScreen.GameOver:
                    Dim(new Color(0.2f, 0f, 0f, Mathf.Min(0.45f, info.StateTime)));
                    m_Score.text = "SCORE " + info.Score;
                    Ui.Show(m_Retry, info.StateTime > 1.2f);
                    Ui.Show(m_GameOverMenu, info.StateTime > 1.2f);
                    Key(m_RetryKey, keys, "ENTER");
                    Key(m_GameOverMenuKey, keys, "ESC");
                    break;
                case MenuScreen.Victory:
                    Dim(new Color(0f, 0f, 0f, Mathf.Min(0.35f, info.StateTime)));
                    m_Result.text = string.Format("SCORE {0}     TIME {1}:{2:00}", info.Score, info.Seconds / 60, info.Seconds % 60);
                    Ui.Show(m_Opened, info.CoopJustOpened);
                    Ui.Show(m_Again, info.StateTime > 1.2f);
                    Ui.Show(m_SecondVictory, info.StateTime > 1.2f);
                    m_SecondIsLeave = info.InCoop;
                    m_SecondLabel.text = info.InCoop ? "LEAVE" : "MENU";
                    Key(m_AgainKey, keys, "ENTER");
                    Key(m_SecondKey, keys, "ESC");
                    break;
            }
            bool notice = !string.IsNullOrEmpty(info.Notice) && info.Screen == MenuScreen.Title;
            Ui.Show(m_Notice, notice);
            if (notice) m_Notice.text = info.Notice;
        }

        void Show(MenuScreen screen)
        {
            m_Shown = screen;
            Ui.Show(m_Dim, screen != MenuScreen.None);
            Ui.Show(m_Title, screen == MenuScreen.Title);
            Ui.Show(m_Online, screen == MenuScreen.Online);
            Ui.Show(m_Join, screen == MenuScreen.Join);
            Ui.Show(m_Connecting, screen == MenuScreen.Connecting);
            Ui.Show(m_Paused, screen == MenuScreen.Paused);
            Ui.Show(m_GameOver, screen == MenuScreen.GameOver);
            Ui.Show(m_Victory, screen == MenuScreen.Victory);
            if (screen == MenuScreen.Join) m_FocusFrame = Time.frameCount + 1;
            else if (m_Code.isFocused) m_Code.DeactivateInputField();
            m_Clicked = MenuAction.None; // a click meant for the screen that just went
        }

        void Click(MenuAction action) => m_Clicked = action;

        void Dim(Color color) => m_Dim.color = color;

        static void Key(Text hint, bool show, string key)
        {
            Ui.Show(hint, show);
            if (show && hint.text != key) hint.text = key;
        }

        static Text Line(RectTransform parent, float y, int size, Color color) =>
            Ui.Label(parent, "Line", Ui.Middle, new Vector2(0f, y), size, TextAnchor.MiddleCenter, color);

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        /// <summary>Room codes are letters, digits and a dash, typed in capitals.</summary>
        static char CodeChar(string text, int index, char c)
        {
            c = char.ToUpperInvariant(c);
            return (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' ? c : '\0';
        }
    }
}
