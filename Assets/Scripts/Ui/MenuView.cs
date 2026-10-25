using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>Which menu is on screen.</summary>
    public enum MenuScreen { None, Title, Settings, Connecting, Paused, GameOver, Victory, Kit, Missions }

    /// <summary>What a menu button (or its key) asks the game to do.</summary>
    public enum MenuAction { None, Play, Settings, Sound, Shake, Back, Resume, Restart, Menu, Kit, Move1, Move2, Move3, Move4, Card1, Card2, Card3, Card4, Hard, Effects, Voices, LeftHanded, Music }

    /// <summary>What the menus show, gathered by <see cref="Game"/> each frame.</summary>
    public struct MenuInfo
    {
        public MenuScreen Screen;
        public float StateTime;
        /// <summary>The super's been met (it's charged once), so the controls name it.</summary>
        public bool SuperShown;
        /// <summary>Show touch hints instead of keys.</summary>
        public bool Touch;
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
        /// <summary>The tabk loaded in the bay, as the mission screen names it, and its paint; null before there's one.</summary>
        public string MissionTabk;
        public Color MissionTabkPaint;
        /// <summary>Where the demo clips are (<c>&lt;id&gt;.mp4</c>), or null when this build can't fetch them.</summary>
        public string DemoFolder;
    }

    /// <summary>
    /// The menus in uGUI: the title (<see cref="TitleView"/>, the splash), settings, connecting to co-op, pause, game over, victory, the ⓘ kit screen
    /// (from the den's couch pick, and the pause menu), which plays a demo of each move, and the mission screen
    /// (<see cref="MissionBoard"/>, from the den's mission board). Buttons work by
    /// click and tap; each also has a key, which <see cref="Game"/> reads, shown under it on a keyboard.
    /// </summary>
    public sealed class MenuView
    {
        static readonly Color Orange = new Color(1f, 0.62f, 0.2f), Keys = new Color(1f, 0.95f, 0.8f);
        static readonly Vector2 Wide = new Vector2(150f, 26f), Narrow = new Vector2(110f, 26f), Setting = new Vector2(190f, 26f);
        static readonly Vector2 CardSize = new Vector2(150f, 112f);

        readonly RectTransform m_Root;
        readonly Image m_Dim;
        readonly TitleView m_Title;
        readonly RectTransform m_Settings, m_Connecting, m_Paused, m_GameOver, m_Victory;
        readonly Text m_EffectsLabel, m_VoicesLabel, m_LeftyLabel, m_MusicLabel, m_EffectsKey, m_VoicesKey, m_LeftyKey, m_MusicKey;
        readonly Text m_Controls1, m_Controls2, m_Controls3, m_SoundLabel, m_ShakeLabel, m_Notice, m_Status, m_Score, m_Result, m_Opened;
        readonly Button m_Retry, m_GameOverMenu, m_Again, m_SecondVictory;
        readonly Text m_SoundKey, m_ShakeKey, m_SettingsBackKey, m_CancelKey;
        readonly Text m_ResumeKey, m_RestartKey, m_MenuKey, m_RetryKey, m_GameOverMenuKey, m_AgainKey, m_SecondKey, m_SecondLabel;
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
        readonly Text m_HardLabel, m_MissionPlayLabel, m_MissionPlayKey, m_HardKey, m_MissionsBackKey, m_MissionsHint, m_MissionTabk;
        MenuScreen m_Shown;
        readonly Latch<MenuAction> m_Clicked = new Latch<MenuAction>();
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

            m_Title = new TitleView(m_Root, Click); // under the dim, so settings dims the splash behind it
            m_Dim = Ui.Cover(m_Root, "Dim", Color.clear);
            m_Notice = Line(m_Root, -100f, 12, new Color(1f, 0.5f, 0.4f));

            // settings
            m_Settings = Ui.Fill(m_Root, "Settings");
            // sound on the left, the screen, the pad and the music on the right, the controls under them
            Line(m_Settings, 128f, 22, Orange).text = "SETTINGS";
            m_SoundLabel = Label(Ui.Button(m_Settings, "SOUND", new Vector2(-105f, 90f), Setting, () => Click(MenuAction.Sound), out m_SoundKey));
            m_EffectsLabel = Label(Ui.Button(m_Settings, "EFFECTS", new Vector2(-105f, 56f), Setting, () => Click(MenuAction.Effects), out m_EffectsKey));
            m_VoicesLabel = Label(Ui.Button(m_Settings, "VOICES", new Vector2(-105f, 22f), Setting, () => Click(MenuAction.Voices), out m_VoicesKey));
            m_ShakeLabel = Label(Ui.Button(m_Settings, "SCREEN SHAKE", new Vector2(105f, 90f), Setting, () => Click(MenuAction.Shake), out m_ShakeKey));
            m_LeftyLabel = Label(Ui.Button(m_Settings, "LEFT-HANDED", new Vector2(105f, 56f), Setting, () => Click(MenuAction.LeftHanded), out m_LeftyKey));
            m_MusicLabel = Label(Ui.Button(m_Settings, "MUSIC", new Vector2(105f, 22f), Setting, () => Click(MenuAction.Music), out m_MusicKey));
            Line(m_Settings, -14f, 12, Orange).text = "CONTROLS";
            m_Controls1 = Line(m_Settings, -32f, 10, Keys);
            m_Controls2 = Line(m_Settings, -48f, 10, Keys);
            m_Controls3 = Line(m_Settings, -64f, 10, Keys);
            Ui.Button(m_Settings, "BACK", new Vector2(0f, -104f), Narrow, () => Click(MenuAction.Back), out m_SettingsBackKey);

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
            Line(m_Victory, 60f, 32, Palette.Gold).text = "MISSION COMPLETE!";
            m_Result = Line(m_Victory, 20f, 14, Color.white);
            m_Opened = Line(m_Victory, -2f, 12, Palette.CoopBlue);
            m_Opened.text = ScreenWords.CoopOpened;
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
            Image blocker = Ui.Cover(m_Missions, "Blocker", Color.clear);
            blocker.raycastTarget = true; // taps on the screen stop here, not on the den under it
            Line(m_Missions, 140f, 22, Orange).text = "MISSIONS";
            m_MissionsHint = Line(m_Missions, 116f, 9, Keys);
            m_MissionTabk = Line(m_Missions, -77f, 9, Color.white); // the tabk loaded in the bay (docs/design/lobby.md, The tabk bay)
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
                m_CardNames[i] = Ui.Label(card, "Name", Ui.TopCenter, new Vector2(0f, -14f), 16, TextAnchor.UpperCenter, Palette.Gold);
                m_CardPlaces[i] = Ui.Label(card, "Place", Ui.TopCenter, new Vector2(0f, -38f), 10, TextAnchor.UpperCenter, Color.white);
                m_CardRecords[i] = Ui.Label(card, "Record", Ui.Middle, new Vector2(0f, -14f), 9, TextAnchor.MiddleCenter, Keys);
                m_CardTags[i] = Ui.Label(card, "Tag", new Vector2(0.5f, 0f), new Vector2(0f, 12f), 9, TextAnchor.LowerCenter, Palette.CoopBlue);
            }
            m_HardButton = Ui.Button(m_Missions, "NORMAL", new Vector2(0f, -54f), Setting, () => Click(MenuAction.Hard), out m_HardKey);
            m_HardLabel = Label(m_HardButton);
            m_MissionPlay = Ui.Button(m_Missions, "PLAY", new Vector2(80f, -100f), Narrow, () => Click(MenuAction.Play), out m_MissionPlayKey);
            m_MissionPlayLabel = Label(m_MissionPlay);
            Ui.Button(m_Missions, "BACK", new Vector2(-80f, -100f), Narrow, () => Click(MenuAction.Back), out m_MissionsBackKey);

            Show(MenuScreen.None);
        }

        /// <summary>The title music can start: the splash's logo has made its entrance (<see cref="TitleView.MusicDue"/>).</summary>
        public bool TitleMusicDue => m_Title.MusicDue;

        /// <summary>The button clicked or tapped since the last call, or None.</summary>
        public MenuAction TakeClick() => m_Clicked.Take(out MenuAction a) ? a : MenuAction.None;

        public void Refresh(in MenuInfo info)
        {
            if (info.Screen != m_Shown) Show(info.Screen);
            bool keys = !info.Touch;
            switch (info.Screen)
            {
                case MenuScreen.Title:
                    Dim(Color.clear);
                    m_Title.Refresh(info);
                    break;
                case MenuScreen.Settings:
                    Dim(new Color(0f, 0f, 0f, 0.6f));
                    m_SoundLabel.text = Options.Label("SOUND", info.Options.Sound);
                    m_ShakeLabel.text = Options.Label("SCREEN SHAKE", info.Options.Shake);
                    m_EffectsLabel.text = Options.Label("EFFECTS", info.Options.Effects);
                    m_VoicesLabel.text = Options.Label("VOICES", info.Options.Voices);
                    m_LeftyLabel.text = Options.Label("LEFT-HANDED", info.Options.LeftHanded);
                    m_MusicLabel.text = Options.Label("MUSIC", info.Options.Music);
                    if (info.Touch)
                    {
                        bool lefty = info.Options.LeftHanded;
                        m_Controls1.text = (lefty ? "RIGHT" : "LEFT") + " THUMB  RUN, AIM UP, CROUCH (AIM DOWN IN THE AIR)";
                        m_Controls2.text = (lefty ? "LEFT" : "RIGHT") + " THUMB  FIRE (HOLD IT), JUMP, SP" + (info.SuperShown ? ", SU (SUPER, FROM KILLS)" : "") + "      DROP  DOWN + JUMP";
                        m_Controls3.text = "II  PAUSE";
                    }
                    else
                    {
                        m_Controls1.text = "MOVE  Arrows / WASD      JUMP  Z / Space      FIRE  X / J";
                        m_Controls2.text = "SP  C / L      " + (info.SuperShown ? "SUPER  V / U      " : "") + "AIM  Up / Down (in air), + run for 45°      DROP  Down + Jump";
                        m_Controls3.text = "PAUSE  P / Enter      RESTART  R      MUTE  M      Gamepad supported";
                    }
                    Key(m_SoundKey, keys, "M");
                    Key(m_ShakeKey, keys, "K");
                    Key(m_EffectsKey, false, null);
                    Key(m_VoicesKey, false, null);
                    Key(m_LeftyKey, false, null);
                    Key(m_MusicKey, false, null);
                    Key(m_SettingsBackKey, keys, "ESC");
                    break;
                case MenuScreen.Connecting:
                    Dim(new Color(0f, 0f, 0f, 0.6f));
                    string dots = new string('.', 1 + Fx.Step(Time.unscaledTime, 3f, 3));
                    m_Status.text = (string.IsNullOrEmpty(info.Status) ? "CONNECTING" : info.Status.TrimEnd('.')) + dots;
                    Key(m_CancelKey, keys, "ESC");
                    break;
                case MenuScreen.Paused:
                    Dim(new Color(0f, 0f, 0f, 0.4f));
                    Key(m_ResumeKey, keys, "P");
                    Key(m_RestartKey, keys, "R");
                    Key(m_MenuKey, keys, "ESC");
                    Key(m_PauseKitKey, keys, ScreenWords.KitKey);
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
            m_Title.Show(screen);
            Ui.Show(m_Settings, screen == MenuScreen.Settings);
            Ui.Show(m_Connecting, screen == MenuScreen.Connecting);
            Ui.Show(m_Paused, screen == MenuScreen.Paused);
            Ui.Show(m_GameOver, screen == MenuScreen.GameOver);
            Ui.Show(m_Victory, screen == MenuScreen.Victory);
            Ui.Show(m_Kit, screen == MenuScreen.Kit);
            Ui.Show(m_Missions, screen == MenuScreen.Missions);
            if (screen != MenuScreen.Kit) m_Clip.Stop();
            m_Clicked.Clear(); // a click meant for the screen that just went
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
                m_MoveLabels[i].color = i == pick ? Palette.Gold : Color.white;
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
                m_CardEdges[i].effectColor = picked ? Palette.Gold : card.ComingSoon ? new Color(0.4f, 0.4f, 0.45f) : Orange;
                m_CardEdges[i].effectDistance = picked ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
                Set(m_CardNames[i], "MISSION " + card.Number);
                m_CardNames[i].color = card.ComingSoon ? new Color(1f, 1f, 1f, 0.45f) : Palette.Gold;
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
            Set(m_MissionPlayLabel, ScreenWords.MissionGo(info.MissionFromDen));
            Set(m_MissionsHint, ScreenWords.MissionHint(info.Touch, info.MissionFromDen));
            Ui.Show(m_MissionTabk, info.MissionTabk != null);
            if (info.MissionTabk != null)
            {
                Set(m_MissionTabk, "LOADED: " + info.MissionTabk);
                m_MissionTabk.color = info.MissionTabkPaint;
            }
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
                default: return ScreenWords.KitKey; // the ability's key, which also opens the kit
            }
        }

        void Click(MenuAction action) => m_Clicked.Set(action);

        void Dim(Color color) => m_Dim.color = color;

        static Text Label(Button button) => button.transform.Find("Label").GetComponent<Text>();

        static void Key(Text hint, bool show, string key)
        {
            Ui.Show(hint, show);
            if (show && hint.text != key) hint.text = key;
        }

        static Text Line(RectTransform parent, float y, int size, Color color) =>
            Ui.Label(parent, "Line", Ui.Middle, new Vector2(0f, y), size, TextAnchor.MiddleCenter, color);
    }
}
