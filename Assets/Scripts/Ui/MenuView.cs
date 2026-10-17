using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>Which menu is on screen.</summary>
    public enum MenuScreen { None, Title, Settings, Online, Join, Connecting, Paused, GameOver, Victory, Kit, Missions, Rooms }

    /// <summary>What a menu button (or its key) asks the game to do.</summary>
    public enum MenuAction { None, Play, Settings, Crew, Sound, Shake, Online, Host, Join, Quick, Submit, Back, Resume, Restart, Menu, Kit, Move1, Move2, Move3, Move4, Card1, Card2, Card3, Card4, Hard, Effects, Voices, LeftHanded, Browse, Room1, Room2, Room3, Room4 }

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
        /// <summary>The crew is open (the CREW pick shows), and who's picked.</summary>
        public bool CrewOpen;
        public string Crew;
        /// <summary>Co-op has opened, and its menu hasn't been visited yet.</summary>
        public bool CoopNew;
        /// <summary>Two of the crew is playing on the title screen: the menu grows as it says (null when not).</summary>
        public CrewReveal Reveal;
        /// <summary>This win opened co-op.</summary>
        public bool CoopJustOpened;
        /// <summary>A shared game is on (the victory screen's second button leaves it).</summary>
        public bool InCoop;
        public string Notice, Status;
        public int Score, Seconds;
        /// <summary>The title screen's line about sign-in; null hides it.</summary>
        public string Account;
        /// <summary>The settings, as the settings screen shows them.</summary>
        public Options Options;
        /// <summary>The kit screen: whose kit (their name), their moves, and the one whose demo plays.</summary>
        public string KitSoldier;
        public System.Collections.Generic.IReadOnlyList<MoveGuide> Kit;
        public int KitMove;
        /// <summary>The mission screen: its cards, the one picked, whether hard is picked, and whether it was opened from the den.</summary>
        public System.Collections.Generic.IReadOnlyList<MissionCard> Missions;
        public int MissionPick;
        public bool MissionHard, MissionFromDen;
        /// <summary>The room browser: its lines (<see cref="RoomBrowser.Rows"/>), and what it's doing or why it can't.</summary>
        public System.Collections.Generic.IReadOnlyList<RoomRow> Rooms;
        public string RoomsStatus;
        /// <summary>Where the demo clips are (<c>&lt;id&gt;.mp4</c>), or null when this build can't fetch them.</summary>
        public string DemoFolder;
    }

    /// <summary>
    /// The menus in uGUI: title, settings, online co-op, join, connecting, pause, game over, victory, the ⓘ kit screen
    /// (from the den's couch pick, and the pause menu), which plays a demo of each move, and the mission screen
    /// (<see cref="MissionBoard"/>, from MISSIONS and the den's mission board). Buttons work by
    /// click and tap; each also has a key, which <see cref="Game"/> reads, shown under it on a keyboard. Only
    /// what the player has met is shown: co-op appears once Mission one is beaten solo.
    /// </summary>
    public sealed class MenuView
    {
        static readonly Color Orange = new Color(1f, 0.62f, 0.2f), Keys = new Color(1f, 0.95f, 0.8f);
        static readonly Color CoopBlue = new Color(0.6f, 0.9f, 1f), Gold = new Color(1f, 0.85f, 0.3f);
        static readonly Vector2 Wide = new Vector2(150f, 26f), Narrow = new Vector2(110f, 26f), Setting = new Vector2(190f, 26f);
        static readonly Vector2 CardSize = new Vector2(150f, 112f);

        readonly RectTransform m_Root;
        readonly Image m_Dim;
        readonly RectTransform m_Title, m_Settings, m_Online, m_Join, m_Connecting, m_Paused, m_GameOver, m_Victory;
        readonly Text m_EffectsLabel, m_VoicesLabel, m_LeftyLabel, m_EffectsKey, m_VoicesKey, m_LeftyKey;
        readonly Text m_Hint, m_Controls1, m_Controls2, m_Controls3, m_OnlineOff, m_SoundLabel, m_ShakeLabel, m_Account, m_Notice, m_NewBadge, m_Status, m_Score, m_Result, m_Opened;
        readonly Button m_SettingsButton, m_Crew;
        readonly Text m_CrewKey, m_CrewLabel;
        readonly Button m_Play, m_Coop, m_Retry, m_GameOverMenu, m_Again, m_SecondVictory;
        readonly Text m_PlayKey, m_SettingsKey, m_SoundKey, m_ShakeKey, m_SettingsBackKey, m_CoopKey, m_HostKey, m_JoinKey, m_QuickKey, m_OnlineBackKey, m_SubmitKey, m_JoinBackKey, m_CancelKey;
        readonly Text m_ResumeKey, m_RestartKey, m_MenuKey, m_RetryKey, m_GameOverMenuKey, m_AgainKey, m_SecondKey, m_SecondLabel;
        readonly InputField m_Code;
        // the kit screen (from the den's couch pick, and the pause menu)
        readonly RectTransform m_Kit;
        readonly Text m_PauseKitKey, m_KitTitle, m_KitText, m_KitBackKey;
        readonly Button[] m_Moves = new Button[4];
        readonly Text[] m_MoveLabels = new Text[4], m_MoveKeys = new Text[4];
        readonly DemoClip m_Clip;
        // the mission screen
        public const int MaxCards = 4;
        readonly RectTransform m_Missions;
        readonly Button[] m_Cards = new Button[MaxCards];
        readonly Outline[] m_CardEdges = new Outline[MaxCards];
        readonly Text[] m_CardNames = new Text[MaxCards], m_CardPlaces = new Text[MaxCards], m_CardRecords = new Text[MaxCards], m_CardTags = new Text[MaxCards];
        readonly Button m_HardButton, m_MissionPlay;
        readonly Text m_HardLabel, m_MissionPlayLabel, m_MissionPlayKey, m_HardKey, m_MissionsBackKey, m_MissionsHint;
        // the room browser
        readonly RectTransform m_Rooms;
        readonly Text m_RoomsStatus, m_RoomsBackKey, m_BrowseKey;
        readonly Text[] m_RoomLines = new Text[RoomBrowser.MaxRows], m_RoomLabels = new Text[RoomBrowser.MaxRows], m_RoomKeys = new Text[RoomBrowser.MaxRows];
        readonly Button[] m_RoomButtons = new Button[RoomBrowser.MaxRows];
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
            m_Hint = Line(m_Title, 34f, 10, Keys);
            m_Play = Ui.Button(m_Title, "START", new Vector2(0f, -4f), Wide, () => Click(MenuAction.Play), out m_PlayKey);
            m_Coop = Ui.Button(m_Title, "CO-OP", new Vector2(85f, -4f), Narrow, () => Click(MenuAction.Online), out m_CoopKey);
            m_SettingsButton = Ui.Button(m_Title, "SETTINGS", new Vector2(0f, -48f), Narrow, () => Click(MenuAction.Settings), out m_SettingsKey);
            m_Crew = Ui.Button(m_Title, "CREW", new Vector2(-85f, -48f), Narrow, () => Click(MenuAction.Crew), out m_CrewKey);
            m_CrewLabel = Label(m_Crew);
            m_NewBadge = Ui.Label(m_Coop.GetComponent<RectTransform>(), "New", Ui.TopRight, new Vector2(6f, 7f), 9, TextAnchor.MiddleCenter, Gold);
            m_NewBadge.text = "NEW!";
            m_OnlineOff = Line(m_Title, -90f, 9, new Color(1f, 1f, 1f, 0.6f));
            m_OnlineOff.text = "ONLINE CO-OP IS OFF IN THIS BUILD (NO PHOTON APP ID)";
            m_Account = Line(m_Title, -125f, 9, new Color(1f, 1f, 1f, 0.75f));
            m_Notice = Line(m_Root, -100f, 12, new Color(1f, 0.5f, 0.4f));

            // settings
            m_Settings = Ui.Fill(m_Root, "Settings");
            // sound on the left, the screen and the pad on the right, the controls under them
            Line(m_Settings, 128f, 22, Orange).text = "SETTINGS";
            m_SoundLabel = Label(Ui.Button(m_Settings, "SOUND", new Vector2(-105f, 90f), Setting, () => Click(MenuAction.Sound), out m_SoundKey));
            m_EffectsLabel = Label(Ui.Button(m_Settings, "EFFECTS", new Vector2(-105f, 56f), Setting, () => Click(MenuAction.Effects), out m_EffectsKey));
            m_VoicesLabel = Label(Ui.Button(m_Settings, "VOICES", new Vector2(-105f, 22f), Setting, () => Click(MenuAction.Voices), out m_VoicesKey));
            m_ShakeLabel = Label(Ui.Button(m_Settings, "SCREEN SHAKE", new Vector2(105f, 90f), Setting, () => Click(MenuAction.Shake), out m_ShakeKey));
            m_LeftyLabel = Label(Ui.Button(m_Settings, "LEFT-HANDED", new Vector2(105f, 56f), Setting, () => Click(MenuAction.LeftHanded), out m_LeftyKey));
            Line(m_Settings, -14f, 12, Orange).text = "CONTROLS";
            m_Controls1 = Line(m_Settings, -32f, 10, Keys);
            m_Controls2 = Line(m_Settings, -48f, 10, Keys);
            m_Controls3 = Line(m_Settings, -64f, 10, Keys);
            Ui.Button(m_Settings, "BACK", new Vector2(0f, -104f), Narrow, () => Click(MenuAction.Back), out m_SettingsBackKey);

            // online co-op
            m_Online = Ui.Fill(m_Root, "Online");
            Line(m_Online, 74f, 22, Orange).text = "ONLINE CO-OP";
            Line(m_Online, 50f, 10, CoopBlue).text = "PLAY THE MISSION WITH A FRIEND";
            Ui.Button(m_Online, "HOST A GAME", new Vector2(-82f, 16f), Wide, () => Click(MenuAction.Host), out m_HostKey);
            Ui.Button(m_Online, "BROWSE ROOMS", new Vector2(82f, 16f), Wide, () => Click(MenuAction.Browse), out m_BrowseKey);
            Ui.Button(m_Online, "JOIN WITH A CODE", new Vector2(-82f, -20f), Wide, () => Click(MenuAction.Join), out m_JoinKey);
            Ui.Button(m_Online, "QUICK MATCH", new Vector2(82f, -20f), Wide, () => Click(MenuAction.Quick), out m_QuickKey);
            Ui.Button(m_Online, "BACK", new Vector2(0f, -70f), Narrow, () => Click(MenuAction.Back), out m_OnlineBackKey);

            // the room browser: listed rooms to join, or to watch once you've cleared their mission
            m_Rooms = Ui.Fill(m_Root, "Rooms");
            Line(m_Rooms, 112f, 22, Orange).text = "ROOMS";
            m_RoomsStatus = Line(m_Rooms, 84f, 10, CoopBlue);
            for (int i = 0; i < RoomBrowser.MaxRows; i++)
            {
                float y = 50f - 32f * i;
                m_RoomLines[i] = Ui.Label(m_Rooms, "Room", Ui.Middle, new Vector2(-270f, y), 11, TextAnchor.MiddleLeft, Color.white);
                MenuAction pick = MenuAction.Room1 + i;
                m_RoomButtons[i] = Ui.Button(m_Rooms, "JOIN", new Vector2(220f, y), new Vector2(100f, 24f), () => Click(pick), out m_RoomKeys[i]);
                m_RoomLabels[i] = m_RoomButtons[i].GetComponentInChildren<Text>();
            }
            Ui.Button(m_Rooms, "BACK", new Vector2(0f, -110f), Narrow, () => Click(MenuAction.Back), out m_RoomsBackKey);

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
            Ui.Button(m_Paused, "YOUR KIT", new Vector2(0f, -64f), Narrow, () => Click(MenuAction.Kit), out m_PauseKitKey);

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
            m_SecondLabel = Label(m_SecondVictory);

            // the kit screen: the moves down the left, the picked one's demo and what it does on the right
            m_Kit = Ui.Fill(m_Root, "Kit");
            m_KitTitle = Line(m_Kit, 140f, 20, Orange);
            for (int i = 0; i < m_Moves.Length; i++)
            {
                MenuAction pick = MenuAction.Move1 + i;
                m_Moves[i] = Ui.Button(m_Kit, "Move", new Vector2(-185f, 70f - 40f * i), Setting, () => Click(pick), out m_MoveKeys[i]);
                m_MoveLabels[i] = Label(m_Moves[i]);
            }
            Image frame = Ui.Picture(m_Kit, "Demo frame", null, Ui.Middle, Ui.Middle, new Vector2(70f, 40f), new Vector2(260f, 148f), new Color(0.08f, 0.06f, 0.09f, 0.95f));
            Outline edge = frame.gameObject.AddComponent<Outline>();
            edge.effectColor = Orange;
            edge.effectDistance = new Vector2(1f, -1f);
            RectTransform screen = Ui.Rect(m_Kit, "Demo", Ui.Middle, Ui.Middle, new Vector2(70f, 40f), new Vector2(256f, 144f));
            var picture = screen.gameObject.AddComponent<RawImage>();
            picture.raycastTarget = false;
            Text note = Ui.Label(m_Kit, "Demo note", Ui.Middle, new Vector2(70f, 40f), 10, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f));
            m_Clip = new DemoClip(screen.gameObject, picture, note);
            m_KitText = Ui.Label(m_Kit, "Move text", Ui.Middle, new Vector2(70f, -40f), 10, TextAnchor.UpperCenter, Color.white);
            m_KitText.horizontalOverflow = HorizontalWrapMode.Wrap;
            m_KitText.rectTransform.sizeDelta = new Vector2(300f, 60f);
            Ui.Button(m_Kit, "BACK", new Vector2(0f, -122f), Narrow, () => Click(MenuAction.Back), out m_KitBackKey);

            // the mission screen: a card per mission reached, then the next one's
            m_Missions = Ui.Fill(m_Root, "Missions");
            Image blocker = Ui.Picture(m_Missions, "Blocker", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, Color.clear);
            Stretch(blocker.rectTransform);
            blocker.raycastTarget = true; // taps on the screen stop here, not on the den under it
            Line(m_Missions, 140f, 22, Orange).text = "MISSIONS";
            m_MissionsHint = Line(m_Missions, 116f, 9, Keys);
            for (int i = 0; i < MaxCards; i++)
            {
                MenuAction pick = MenuAction.Card1 + i;
                m_Cards[i] = Ui.Button(m_Missions, "", new Vector2(0f, 30f), CardSize, () => Click(pick), out Text cardKey);
                Ui.Show(cardKey, false);
                m_CardEdges[i] = m_Cards[i].GetComponent<Outline>();
                ColorBlock colors = m_Cards[i].colors;
                colors.disabledColor = new Color(0.07f, 0.06f, 0.08f, 0.85f); // the coming-soon card, dark like a locked one
                m_Cards[i].colors = colors;
                var card = m_Cards[i].GetComponent<RectTransform>();
                m_CardNames[i] = Ui.Label(card, "Name", Ui.TopCenter, new Vector2(0f, -14f), 16, TextAnchor.UpperCenter, Gold);
                m_CardPlaces[i] = Ui.Label(card, "Place", Ui.TopCenter, new Vector2(0f, -38f), 10, TextAnchor.UpperCenter, Color.white);
                m_CardRecords[i] = Ui.Label(card, "Record", Ui.Middle, new Vector2(0f, -14f), 9, TextAnchor.MiddleCenter, Keys);
                m_CardTags[i] = Ui.Label(card, "Tag", new Vector2(0.5f, 0f), new Vector2(0f, 12f), 9, TextAnchor.LowerCenter, CoopBlue);
            }
            m_HardButton = Ui.Button(m_Missions, "NORMAL", new Vector2(0f, -54f), Setting, () => Click(MenuAction.Hard), out m_HardKey);
            m_HardLabel = Label(m_HardButton);
            m_MissionPlay = Ui.Button(m_Missions, "PLAY", new Vector2(80f, -100f), Narrow, () => Click(MenuAction.Play), out m_MissionPlayKey);
            m_MissionPlayLabel = Label(m_MissionPlay);
            Ui.Button(m_Missions, "BACK", new Vector2(-80f, -100f), Narrow, () => Click(MenuAction.Back), out m_MissionsBackKey);

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
                    m_Hint.text = info.Touch ? (info.Options.LeftHanded ? "RIGHT THUMB RUNS · LEFT THUMB FIRES AND JUMPS · MORE IN SETTINGS" : "LEFT THUMB RUNS · RIGHT THUMB FIRES AND JUMPS · MORE IN SETTINGS")
                        : "MOVE  Arrows      JUMP  Z      FIRE  X      MORE IN SETTINGS";
                    LayOutTitle(info);
                    Key(m_PlayKey, keys, "ENTER");
                    Key(m_CoopKey, keys, "H HOST · J JOIN · Q QUICK · R ROOMS");
                    Key(m_SettingsKey, keys, "O");
                    if (info.CrewOpen) m_CrewLabel.text = "CREW: " + info.Crew;
                    Key(m_CrewKey, keys, "C");
                    Ui.Show(m_OnlineOff, info.OnlineOff && info.CoopOpen);
                    Ui.Show(m_Account, info.Account != null);
                    if (info.Account != null && m_Account.text != info.Account) m_Account.text = info.Account;
                    break;
                case MenuScreen.Settings:
                    Dim(new Color(0f, 0f, 0f, 0.6f));
                    m_SoundLabel.text = Options.Label("SOUND", info.Options.Sound);
                    m_ShakeLabel.text = Options.Label("SCREEN SHAKE", info.Options.Shake);
                    m_EffectsLabel.text = Options.Label("EFFECTS", info.Options.Effects);
                    m_VoicesLabel.text = Options.Label("VOICES", info.Options.Voices);
                    m_LeftyLabel.text = Options.Label("LEFT-HANDED", info.Options.LeftHanded);
                    if (info.Touch)
                    {
                        bool lefty = info.Options.LeftHanded;
                        m_Controls1.text = (lefty ? "RIGHT" : "LEFT") + " THUMB  RUN, AIM UP, CROUCH (AIM DOWN IN THE AIR)";
                        m_Controls2.text = (lefty ? "LEFT" : "RIGHT") + " THUMB  FIRE (HOLD IT), JUMP, SP, SU (SUPER, FROM KILLS)      DROP  DOWN + JUMP";
                        m_Controls3.text = "II  PAUSE";
                    }
                    else
                    {
                        m_Controls1.text = "MOVE  Arrows / WASD      JUMP  Z / Space      FIRE  X / J";
                        m_Controls2.text = "GRENADE  C / L      SUPER  V / U      AIM  Up / Down (in air), + run for 45°      DROP  Down + Jump";
                        m_Controls3.text = "PAUSE  P / Enter      RESTART  R      MUTE  M      Gamepad supported";
                    }
                    Key(m_SoundKey, keys, "M");
                    Key(m_ShakeKey, keys, "K");
                    Key(m_EffectsKey, false, null);
                    Key(m_VoicesKey, false, null);
                    Key(m_LeftyKey, false, null);
                    Key(m_SettingsBackKey, keys, "ESC");
                    break;
                case MenuScreen.Online:
                    Dim(new Color(0f, 0f, 0f, 0.6f));
                    Key(m_HostKey, keys, "H");
                    Key(m_BrowseKey, keys, "R");
                    Key(m_JoinKey, keys, "J");
                    Key(m_QuickKey, keys, "Q");
                    Key(m_OnlineBackKey, keys, "ESC");
                    break;
                case MenuScreen.Rooms:
                    Dim(new Color(0f, 0f, 0f, 0.7f));
                    RefreshRooms(info, keys);
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
                    Key(m_PauseKitKey, keys, "I");
                    break;
                case MenuScreen.Kit:
                    Dim(new Color(0f, 0f, 0f, 0.8f));
                    RefreshKit(info, keys);
                    break;
                case MenuScreen.Missions:
                    Dim(new Color(0f, 0f, 0f, 0.8f));
                    RefreshMissions(info, keys);
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
            Ui.Show(m_Settings, screen == MenuScreen.Settings);
            Ui.Show(m_Online, screen == MenuScreen.Online);
            Ui.Show(m_Join, screen == MenuScreen.Join);
            Ui.Show(m_Connecting, screen == MenuScreen.Connecting);
            Ui.Show(m_Paused, screen == MenuScreen.Paused);
            Ui.Show(m_GameOver, screen == MenuScreen.GameOver);
            Ui.Show(m_Victory, screen == MenuScreen.Victory);
            Ui.Show(m_Kit, screen == MenuScreen.Kit);
            Ui.Show(m_Missions, screen == MenuScreen.Missions);
            Ui.Show(m_Rooms, screen == MenuScreen.Rooms);
            if (screen != MenuScreen.Kit) m_Clip.Stop();
            if (screen == MenuScreen.Join) m_FocusFrame = Time.frameCount + 1;
            else if (m_Code.isFocused) m_Code.DeactivateInputField();
            m_Clicked = MenuAction.None; // a click meant for the screen that just went
        }

        /// <summary>The room browser: a line for each listed room, with JOIN or WATCH (or why not), and the first one on ENTER.</summary>
        void RefreshRooms(in MenuInfo info, bool keys)
        {
            int count = info.Rooms?.Count ?? 0;
            string status = !string.IsNullOrEmpty(info.RoomsStatus) ? info.RoomsStatus
                : count == 0 ? "NO ROOMS RIGHT NOW: HOST ONE AND LIST IT, OR TRY QUICK MATCH" : "WATCH ONLY MISSIONS YOU'VE CLEARED: NO SPOILERS";
            if (m_RoomsStatus.text != status) m_RoomsStatus.text = status;
            for (int i = 0; i < m_RoomButtons.Length; i++)
            {
                bool shown = i < count;
                Ui.Show(m_RoomLines[i], shown);
                Ui.Show(m_RoomButtons[i], shown);
                if (!shown) continue;
                RoomRow row = info.Rooms[i];
                string line = RoomBrowser.Line(row.Room);
                if (m_RoomLines[i].text != line) m_RoomLines[i].text = line;
                m_RoomLines[i].color = row.Action == RoomAction.Join || row.Action == RoomAction.Watch ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                m_RoomLabels[i].text = RoomBrowser.Button(row.Action);
                m_RoomButtons[i].interactable = row.Action == RoomAction.Join || row.Action == RoomAction.Watch;
                Key(m_RoomKeys[i], keys && i == 0, "ENTER");
            }
            Key(m_RoomsBackKey, keys, "ESC");
        }

        void RefreshKit(in MenuInfo info, bool keys)
        {
            string title = info.KitSoldier + "'S KIT";
            if (m_KitTitle.text != title) m_KitTitle.text = title;
            int count = info.Kit?.Count ?? 0;
            int pick = Mathf.Clamp(info.KitMove, 0, Mathf.Max(0, count - 1));
            for (int i = 0; i < m_Moves.Length; i++)
            {
                bool shown = i < count;
                Ui.Show(m_Moves[i], shown);
                if (!shown) continue;
                MoveGuide move = info.Kit[i];
                string label = move.Button + " · " + move.Name;
                if (m_MoveLabels[i].text != label) m_MoveLabels[i].text = label;
                m_MoveLabels[i].color = i == pick ? Gold : Color.white;
                Key(m_MoveKeys[i], keys, KeyOf(move.Slot));
            }
            MoveGuide shownMove = count > 0 ? info.Kit[pick] : null;
            string text = shownMove?.Text ?? "";
            if (m_KitText.text != text) m_KitText.text = text;
            m_Clip.Play(shownMove == null || info.DemoFolder == null ? null : info.DemoFolder + shownMove.DemoId + ".mp4");
            Key(m_KitBackKey, keys, "ESC");
        }

        /// <summary>
        /// The mission screen's cards in a row, the picked one edged in gold; a coming-soon card is dimmed and can't be
        /// picked. The setting button shows only where hard is open, and PLAY says PICK when the den opened the screen.
        /// </summary>
        void RefreshMissions(in MenuInfo info, bool keys)
        {
            const float Gap = 162f;
            int count = Mathf.Min(MaxCards, info.Missions?.Count ?? 0);
            int pick = Mathf.Clamp(info.MissionPick, 0, Mathf.Max(0, count - 1));
            for (int i = 0; i < MaxCards; i++)
            {
                bool shown = i < count;
                Ui.Show(m_Cards[i], shown);
                if (!shown) continue;
                MissionCard card = info.Missions[i];
                bool picked = i == pick && !card.ComingSoon;
                m_Cards[i].GetComponent<RectTransform>().anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * Gap, 30f);
                m_Cards[i].interactable = !card.ComingSoon;
                m_CardEdges[i].effectColor = picked ? Gold : card.ComingSoon ? new Color(0.4f, 0.4f, 0.45f) : Orange;
                m_CardEdges[i].effectDistance = picked ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
                Set(m_CardNames[i], "MISSION " + card.Number);
                m_CardNames[i].color = card.ComingSoon ? new Color(1f, 1f, 1f, 0.45f) : Gold;
                Set(m_CardPlaces[i], card.ComingSoon ? "???" : card.Mission.Place);
                PooketSlug.Setting setting = picked && info.MissionHard ? PooketSlug.Setting.Hard : PooketSlug.Setting.Normal;
                Set(m_CardRecords[i], MissionBoard.RecordLine(card, setting));
                Set(m_CardTags[i], card.SoloOnly ? "SOLO ONLY" : card.HardOpen ? "HARD OPEN" : "");
            }
            MissionCard chosen = count > 0 ? info.Missions[pick] : null;
            bool hard = chosen != null && chosen.HardOpen;
            Ui.Show(m_HardButton, hard);
            if (hard) Set(m_HardLabel, info.MissionHard ? "SETTING: HARD (FOR TWO)" : "SETTING: NORMAL");
            Ui.Show(m_MissionPlay, chosen != null && !chosen.ComingSoon);
            Set(m_MissionPlayLabel, info.MissionFromDen ? "PICK" : "PLAY");
            Set(m_MissionsHint, info.Touch ? "TAP A MISSION, THEN PLAY" : "LEFT / RIGHT PICKS A MISSION");
            Key(m_MissionPlayKey, keys, "ENTER");
            Key(m_HardKey, keys, "H");
            Key(m_MissionsBackKey, keys, "ESC");
        }

        static void Set(Text text, string value)
        {
            if (text.text != value) text.text = value;
        }

        /// <summary>The keyboard key for a kit button, as the settings list them.</summary>
        static string KeyOf(KitSlot slot)
        {
            switch (slot)
            {
                case KitSlot.Fire: return "X";
                case KitSlot.Special: return "C";
                case KitSlot.Super: return "V";
                default: return "B";
            }
        }

        void Click(MenuAction action) => m_Clicked = action;

        void Dim(Color color) => m_Dim.color = color;

        /// <summary>
        /// The title's buttons. Before Mission one's beaten: START, with SETTINGS under it. Once the crew's open, the
        /// menu has grown (<c>docs/design/story.md</c>, Two of the crew): MISSIONS, CO-OP and CREW in a row, SETTINGS
        /// still under. While that scene plays, START cracks and the new buttons drop in one at a time.
        /// </summary>
        void LayOutTitle(in MenuInfo info)
        {
            const float RowY = -4f, Gap = 125f, DropFrom = 70f;
            CrewReveal reveal = info.Reveal;
            bool grown = info.CrewOpen && (reveal == null || !reveal.OldStart);
            var play = m_Play.GetComponent<RectTransform>();
            Label(m_Play).text = grown ? "MISSIONS" : "START";
            play.sizeDelta = grown ? Narrow : Wide;
            m_SettingsButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -48f);
            Ui.Show(m_NewBadge, reveal == null && info.CoopNew && (int)(Time.unscaledTime * 2.5f) % 2 == 0);
            if (!grown)
            {
                // the old title (and, as the menu starts to grow, START shaking as it cracks)
                float shake = reveal != null && reveal.Now == CrewReveal.Shot.MenuGrows ? Mathf.Sin(reveal.T * 70f) * 2.5f : 0f;
                play.anchoredPosition = new Vector2(shake, RowY);
                play.localRotation = Quaternion.Euler(0f, 0f, shake);
                Ui.Show(m_Play, true);
                Ui.Show(m_Coop, false);
                Ui.Show(m_Crew, false);
                return;
            }
            play.localRotation = Quaternion.identity;
            bool coop = info.Online && info.CoopOpen;
            Button[] row = { m_Play, coop ? m_Coop : null, m_Crew };
            if (!coop) Ui.Show(m_Coop, false);
            int count = coop ? 3 : 2, slot = 0;
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] == null) continue;
                float drop = reveal != null ? reveal.Drop(i) : 1f;
                Ui.Show(row[i], drop > 0f);
                var rt = row[i].GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2((slot - (count - 1) * 0.5f) * Gap, RowY + (1f - drop) * (1f - drop) * DropFrom);
                float pulse = reveal != null && i == 1 && reveal.Now == CrewReveal.Shot.KiesLine && reveal.T < 0.5f ? Mathf.Sin(reveal.T / 0.5f * Mathf.PI) * 0.15f : 0f;
                rt.localScale = new Vector3(1f + pulse, 1f + pulse, 1f);
                slot++;
            }
        }

        static Text Label(Button button) => button.transform.Find("Label").GetComponent<Text>();

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
