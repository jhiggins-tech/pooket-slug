using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>What the HUD shows besides the world itself, gathered by <see cref="Game"/> each frame.</summary>
    public struct HudInfo
    {
        /// <summary>Playing, or on the game-over or victory screen.</summary>
        public bool InGame;
        public bool Playing, Paused;
        public float StateTime;
        /// <summary>A line under the start banner, or null.</summary>
        public string Note;
        public int HiScore, Fps;
        public bool Muted;
        /// <summary>A move's demo is playing (<c>?demo=</c>), or a sandbox's scripted take: the build line, frame rate and start banner stay out of the recording.</summary>
        public bool Demo;
        /// <summary>Co-op: the session, or null in a solo game.</summary>
        public INetSession Net;
        /// <summary>Rooms: we're watching, with no soldier of our own (the HUD shows the players, not us).</summary>
        public bool Watching;
        public string PageUrl;
        /// <summary>Co-op: the first press of Esc (or EXIT) is waiting for the second.</summary>
        public bool LeaveArmed;
        /// <summary>The boss intro under way, and the boss's name for its card; null name when there's none.</summary>
        public BossIntroShow Intro;
        public string IntroName;
        /// <summary>The pilot's part of it: their name for the intro band, and their line (null with no pilot's part).</summary>
        public string PilotName, PilotLine;
        /// <summary>The super's been met (it's charged once): its meter and SU button show. Hidden before that.</summary>
        public bool SuperShown;
        /// <summary>The one-off prompt for the super's first charge while it shows, or null.</summary>
        public string SuperPrompt;
    }

    /// <summary>
    /// The in-game HUD in uGUI: score, hearts, arms, high score, time, the boss's bar, popups, co-op status, the
    /// touch pad and the footer. It only shows things; the menus are drawn by <see cref="MenuView"/>.
    /// </summary>
    public sealed class HudView
    {
        static readonly Color Arms = new Color(1f, 0.9f, 0.5f), Faint = new Color(1f, 1f, 1f, 0.75f);
        static readonly Color EmptyHeart = new Color(0.2f, 0.15f, 0.2f, 0.7f), Hurt = new Color(1f, 0.5f, 0.4f);
        static readonly Color PadFill = new Color(0.08f, 0.06f, 0.1f, 0.35f), PadHeld = new Color(1f, 0.62f, 0.2f, 0.6f);
        static readonly Color PadReady = new Color(1f, 0.85f, 0.3f, 0.45f);

        readonly RectTransform m_Root, m_Top;
        // the boss intro
        static readonly Color Warn = new Color(1f, 0.18f, 0.12f), WarnDim = new Color(0.55f, 0.05f, 0.05f);
        readonly Image m_Edges;
        readonly RawImage m_StripesTop, m_StripesBottom;
        readonly Text m_Warning, m_IntroName;
        // the pilot's intro: a slanted gold-edged band with their name, a white flash as it slams in, and their line
        readonly RectTransform m_PilotBand;
        readonly Text m_PilotName, m_PilotLine;
        readonly Image m_PilotFlash;
        // a team super's split card: both soldiers slam in from the sides, the combo's name between them
        readonly RectTransform m_TeamCard;
        readonly Image m_TeamBand, m_TeamLeft, m_TeamRight, m_TeamLeftPic, m_TeamRightPic;
        readonly Text m_TeamTitle, m_TeamName, m_TeamLeftName, m_TeamRightName;
        SoldierDef m_TeamShownLeft;
        const string CallInPrompt = "HOLD DOWN + SP TO CALL YOUR TABK";
        readonly Text m_Score, m_Arms, m_Hi, m_Time, m_BossName, m_Build, m_Fps, m_Banner, m_Note, m_Coach;
        readonly Image m_BossBack, m_BossFill;
        // the kit's meters: the super (SU), the special's refill (SP) and the ability (AB); and a tank call-in's charge
        readonly Text m_SuperLabel, m_AbilityLabel;
        readonly Image m_SuperBack, m_SuperFill, m_RefillFill, m_AbilityBack, m_AbilityFill, m_CallBack, m_CallFill;
        // your tank's health while you're in it
        readonly Text m_TankLabel, m_FuelLabel;
        readonly Image m_TankBack, m_TankFill, m_FuelBack, m_FuelFill;
        const float MeterWidth = 60f;
        const int PopupSize = 9;
        readonly List<Image> m_Hearts = new List<Image>();
        readonly List<Text> m_Popups = new List<Text>();
        int m_Points = -1, m_OwnShown = -1, m_HiScore = -1, m_Seconds = -1, m_FpsShown = -1;
        int m_ArmsGun = -1, m_ArmsAmmo = -1, m_ArmsSpecials = -1, m_ArmsTank = -1;
        bool m_MutedShown;
        readonly System.Func<Text> m_NewPopup, m_NewPartnerLabel, m_NewTag;
        readonly System.Func<Image> m_NewHeart, m_NewPartnerHeart; // made once: a lambda per frame would be garbage

        // co-op
        readonly RectTransform m_Coop;
        readonly Text m_Waiting, m_Room, m_Link, m_Status, m_Down, m_DownNote, m_Lives, m_Leave;
        int m_LivesShown = -1;
        readonly List<Text> m_PartnerLabels = new List<Text>(), m_Tags = new List<Text>();
        readonly List<Image> m_PartnerHearts = new List<Image>();
        string m_StatusKey;

        // the partner beacon (PartnerBeacon, in every co-op game): an arrow to a partner who's off screen, and their warnings
        static readonly Color Low = new Color(1f, 0.3f, 0.25f), AwayGrey = new Color(0.75f, 0.75f, 0.8f);
        readonly RectTransform m_Beacons;
        readonly List<Image> m_Arrows = new List<Image>();
        readonly List<Text> m_ArrowLabels = new List<Text>(), m_Warnings = new List<Text>();
        readonly System.Func<Image> m_NewArrow;
        readonly System.Func<Text> m_NewArrowLabel, m_NewWarning;

        // touch pad
        readonly RectTransform m_Pad;
        readonly Image m_StickBase, m_StickKnob;
        readonly Image[] m_Buttons = new Image[7];
        readonly Text[] m_ButtonLabels = new Text[7];
        int m_SpecialsShown = -1;

        public HudView(Transform parent)
        {
            m_Root = Ui.Canvas(parent, "HUD", 0);
            m_NewPopup = () => Ui.Label(m_Root, "Popup", Ui.BottomLeft, Vector2.zero, PopupSize, TextAnchor.MiddleCenter, Color.white);
            m_NewPartnerLabel = () => Ui.Label(m_Coop, "Partner", Ui.TopLeft, Vector2.zero, 10, TextAnchor.UpperLeft, Color.white);
            m_NewTag = () => Ui.Label(m_Coop, "Name tag", Ui.BottomLeft, Vector2.zero, 8, TextAnchor.MiddleCenter, Color.white);
            m_NewHeart = () => Ui.Picture(m_Top, "Heart", Ui.Heart, Ui.TopLeft, Ui.TopLeft, Vector2.zero, Vector2.one, Color.white);
            m_NewPartnerHeart = () => Ui.Picture(m_Coop, "Heart", Ui.Heart, Ui.TopLeft, Ui.TopLeft, Vector2.zero, Vector2.one, Color.white);

            m_Top = Ui.Fill(m_Root, "In game");
            m_Score = Ui.Label(m_Top, "Score", Ui.TopLeft, new Vector2(8f, -5f), 14, TextAnchor.UpperLeft, Color.white);
            m_Arms = Ui.Label(m_Top, "Arms", Ui.TopLeft, new Vector2(8f, -40f), 11, TextAnchor.UpperLeft, Arms);
            m_Hi = Ui.Label(m_Top, "High score", Ui.TopRight, new Vector2(-8f, -5f), 14, TextAnchor.UpperRight, Color.white);
            m_Time = Ui.Label(m_Top, "Time", Ui.TopRight, new Vector2(-8f, -24f), 11, TextAnchor.UpperRight, Color.white);
            m_BossName = Ui.Label(m_Top, "Boss", Ui.TopCenter, new Vector2(0f, -6f), 10, TextAnchor.UpperCenter, new Color(1f, 0.6f, 0.4f));
            m_BossName.text = "REBEL TABK";
            m_BossBack = Ui.Picture(m_Top, "Boss bar", null, Ui.TopCenter, Ui.TopCenter, new Vector2(0f, -21f), new Vector2(182f, 8f), new Color(0.1f, 0.05f, 0.08f, 0.85f));
            m_BossFill = Ui.Picture(m_BossBack.rectTransform, "Health", null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(180f, 6f), new Color(0.9f, 0.25f, 0.2f));
            m_SuperLabel = Ui.Label(m_Top, "Super", Ui.TopLeft, new Vector2(8f, -55f), 8, TextAnchor.UpperLeft, Arms);
            m_SuperLabel.text = "SU";
            m_SuperBack = Meter(m_Top, "Super meter", new Vector2(24f, -57f), out m_SuperFill, Palette.Gold);
            m_RefillFill = Ui.Picture(m_SuperBack.rectTransform, "SP refill", null, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, -3f), new Vector2(0f, 2f), new Color(0.55f, 0.85f, 1f, 0.9f));
            m_AbilityLabel = Ui.Label(m_Top, "Ability", Ui.TopLeft, new Vector2(8f, -67f), 8, TextAnchor.UpperLeft, Arms);
            m_AbilityLabel.text = "AB";
            m_AbilityBack = Meter(m_Top, "Ability meter", new Vector2(24f, -69f), out m_AbilityFill, Palette.CoopBlue);
            m_TankLabel = Ui.Label(m_Top, "Tank", Ui.TopLeft, new Vector2(8f, -79f), 8, TextAnchor.UpperLeft, Arms);
            m_TankLabel.text = "TK";
            m_TankBack = Meter(m_Top, "Tank health", new Vector2(24f, -81f), out m_TankFill, new Color(0.55f, 0.9f, 0.45f));
            m_FuelLabel = Ui.Label(m_Top, "Fuel", Ui.TopLeft, new Vector2(8f, -91f), 8, TextAnchor.UpperLeft, Arms);
            m_FuelLabel.text = "FU";
            m_FuelBack = Meter(m_Top, "Tank fuel", new Vector2(24f, -93f), out m_FuelFill, new Color(1f, 0.6f, 0.2f));
            m_CallBack = Ui.Picture(m_Root, "Call-in", null, Ui.BottomLeft, Ui.Middle, Vector2.zero, new Vector2(34f, 6f), new Color(0.1f, 0.05f, 0.08f, 0.85f));
            m_CallFill = Ui.Picture(m_CallBack.rectTransform, "Charge", null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(0f, 4f), Palette.Gold);
            m_Banner = Ui.Label(m_Root, "Banner", Ui.Middle, new Vector2(0f, 60f), 26, TextAnchor.MiddleCenter, Palette.Gold);
            m_Edges = Ui.Cover(m_Root, "Warning edges", Color.clear);
            m_Edges.raycastTarget = false;
            m_StripesTop = Stripes(m_Root, "Stripes top", 1f);
            m_StripesBottom = Stripes(m_Root, "Stripes bottom", 0f);
            m_Warning = Ui.Label(m_Root, "Warning", Ui.Middle, new Vector2(0f, 40f), 44, TextAnchor.MiddleCenter, Warn);
            m_Warning.text = "WARNING!";
            m_IntroName = Ui.Label(m_Root, "Boss name", Ui.Middle, new Vector2(0f, 2f), 18, TextAnchor.MiddleCenter, Color.white);
            m_PilotFlash = Ui.Cover(m_Root, "Pilot flash", Color.clear);
            m_PilotFlash.raycastTarget = false;
            m_PilotBand = Ui.Rect(m_Root, "Pilot intro", Ui.Middle, Ui.Middle, new Vector2(60f, 20f), new Vector2(340f, 64f));
            var band = m_PilotBand.gameObject.AddComponent<Image>();
            band.color = new Color(0.06f, 0.04f, 0.08f, 0.88f);
            band.raycastTarget = false;
            m_PilotBand.localRotation = Quaternion.Euler(0f, 0f, 6f);
            Ui.Picture(m_PilotBand, "Edge", null, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(340f, 4f), Palette.Gold);
            Ui.Picture(m_PilotBand, "Edge", null, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(340f, 4f), Palette.Gold);
            m_PilotName = Ui.Label(m_PilotBand, "Name", Ui.Middle, Vector2.zero, 40, TextAnchor.MiddleCenter, Palette.Gold);
            m_PilotLine = Ui.Label(m_Root, "Pilot line", Ui.Middle, new Vector2(60f, -30f), 14, TextAnchor.MiddleCenter, Palette.Cyan);
            m_Note = Ui.Label(m_Root, "Note", Ui.Middle, new Vector2(0f, 32f), 12, TextAnchor.MiddleCenter, Palette.CoopBlue);
            m_Coach = Ui.Label(m_Root, "Call-in hint", Ui.Middle, new Vector2(0f, 46f), 14, TextAnchor.MiddleCenter, Palette.Gold);
            m_Coach.text = CallInPrompt;
            m_TeamCard = Ui.Fill(m_Root, "Team card");
            m_TeamBand = Ui.Picture(m_TeamCard, "Band", null, Ui.Middle, Ui.Middle, Vector2.zero, new Vector2(2000f, 110f), new Color(0.06f, 0.04f, 0.08f, 0.85f));
            m_TeamLeft = Ui.Picture(m_TeamCard, "Left", null, Ui.Middle, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(220f, 100f), Color.white);
            m_TeamRight = Ui.Picture(m_TeamCard, "Right", null, Ui.Middle, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(220f, 100f), Color.white);
            m_TeamLeftPic = Ui.Picture(m_TeamLeft.rectTransform, "Portrait", null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 4f), new Vector2(64f, 64f), Color.white);
            m_TeamRightPic = Ui.Picture(m_TeamRight.rectTransform, "Portrait", null, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-10f, 4f), new Vector2(64f, 64f), Color.white); // flipped about its left edge, so it stays inside
            m_TeamLeftPic.preserveAspect = m_TeamRightPic.preserveAspect = true;
            m_TeamLeftName = Ui.Label(m_TeamLeft.rectTransform, "Name", new Vector2(0f, 0f), new Vector2(12f, 10f), 12, TextAnchor.LowerLeft, Color.white);
            m_TeamRightName = Ui.Label(m_TeamRight.rectTransform, "Name", new Vector2(1f, 0f), new Vector2(-12f, 10f), 12, TextAnchor.LowerRight, Color.white);
            m_TeamTitle = Ui.Label(m_TeamCard, "Title", Ui.Middle, new Vector2(0f, 76f), 30, TextAnchor.MiddleCenter, Palette.Gold);
            m_TeamTitle.text = "TEAM SUPER!";
            m_TeamName = Ui.Label(m_TeamCard, "Combo", Ui.Middle, new Vector2(0f, -72f), 22, TextAnchor.MiddleCenter, Color.white);
            m_TeamCard.gameObject.SetActive(false);

            m_Coop = Ui.Fill(m_Top, "Co-op");
            m_Waiting = Ui.Label(m_Coop, "Waiting", Ui.TopCenter, new Vector2(0f, -40f), 12, TextAnchor.UpperCenter, Palette.CoopBlue);
            m_Waiting.text = "WAITING FOR A PARTNER";
            m_Room = Ui.Label(m_Coop, "Room", Ui.TopCenter, new Vector2(0f, -56f), 16, TextAnchor.UpperCenter, Color.white);
            m_Link = Ui.Label(m_Coop, "Link", Ui.TopCenter, new Vector2(0f, -76f), 8, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.8f));
            m_Status = Ui.Label(m_Coop, "Status", new Vector2(0.5f, 0f), new Vector2(0f, 28f), 8, TextAnchor.UpperCenter, Faint);
            m_Down = Ui.Label(m_Top, "Down", Ui.Middle, new Vector2(0f, 30f), 18, TextAnchor.MiddleCenter, Hurt);
            m_DownNote = Ui.Label(m_Top, "Down note", Ui.Middle, new Vector2(0f, 10f), 10, TextAnchor.MiddleCenter, Color.white);
            m_Lives = Ui.Label(m_Top, "Lives", Ui.TopLeft, new Vector2(78f, -22f), 10, TextAnchor.UpperLeft, Arms);
            m_Leave = Ui.Label(m_Coop, "Leave", Ui.Middle, new Vector2(0f, -16f), 12, TextAnchor.MiddleCenter, Color.white);

            m_Beacons = Ui.Fill(m_Top, "Partner beacon");
            m_NewArrow = () => Ui.Picture(m_Beacons, "Arrow", Ui.Arrow, Ui.BottomLeft, Ui.Middle, Vector2.zero, new Vector2(22f, 22f), Color.white);
            m_NewArrowLabel = () => Ui.Label(m_Beacons, "Arrow label", Ui.BottomLeft, Vector2.zero, 8, TextAnchor.MiddleCenter, Color.white);
            m_NewWarning = () => Ui.Label(m_Beacons, "Warning", Ui.BottomLeft, Vector2.zero, 8, TextAnchor.MiddleCenter, Low);

            m_Build = Ui.Label(m_Root, "Build", Ui.BottomLeft, new Vector2(6f, 14f), 8, TextAnchor.UpperLeft, Faint);
            m_Build.text = "v" + Application.version + " · Unity " + Application.unityVersion + " · built with GameCI";
            m_Fps = Ui.Label(m_Root, "FPS", Ui.BottomRight, new Vector2(-6f, 14f), 8, TextAnchor.UpperRight, Faint);

            m_Pad = Ui.Fill(m_Root, "Touch pad");
            m_StickBase = Ui.Picture(m_Pad, "Stick", Ui.Disc, Ui.TopLeft, Ui.Middle, Vector2.zero, Vector2.one * (ArcadePad.StickRadius + 12f) * 2f, PadFill);
            m_StickKnob = Ui.Picture(m_Pad, "Knob", Ui.Disc, Ui.TopLeft, Ui.Middle, Vector2.zero, Vector2.one * 36f, Color.white);
            string[] names = { "FIRE", "JUMP", "SP", "SU", "AB", "II", "TEAM" };
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                m_Buttons[i] = Ui.Picture(m_Pad, names[i], Ui.Disc, Ui.TopLeft, Ui.Middle, Vector2.zero, Vector2.one, PadFill);
                m_ButtonLabels[i] = Ui.Label(m_Buttons[i].rectTransform, "Label", Ui.Middle, Vector2.zero, i == (int)ArcadePad.Button.Pause ? 8 : 10, TextAnchor.MiddleCenter, Color.white);
                m_ButtonLabels[i].text = names[i];
            }
        }

        /// <summary>A band of yellow and black hazard stripes across the screen, at the top (1) or bottom (0).</summary>
        static RawImage Stripes(RectTransform parent, string name, float edge)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, edge);
            rt.anchorMax = new Vector2(1f, edge);
            rt.pivot = new Vector2(0.5f, edge);
            rt.sizeDelta = new Vector2(0f, 16f);
            var img = go.GetComponent<RawImage>();
            img.texture = HazardTexture();
            img.raycastTarget = false;
            go.SetActive(false);
            return img;
        }

        static Texture2D s_Hazard;

        static Texture2D HazardTexture()
        {
            if (s_Hazard != null) return s_Hazard;
            const int size = 16;
            s_Hazard = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var yellow = new Color32(255, 200, 40, 255);
            var black = new Color32(20, 16, 20, 255);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = ((x + y) / (size / 2)) % 2 == 0 ? yellow : black;
            s_Hazard.SetPixels32(px);
            s_Hazard.Apply();
            return s_Hazard;
        }

        /// <summary>The boss intro: WARNING! flashing between hazard stripes, the screen's edges going red, then the name card.</summary>
        void Intro(in HudInfo info)
        {
            BossIntroShow show = info.Intro;
            bool on = info.IntroName != null && info.Playing;
            float stripes = on ? show.Stripes : 0f;
            m_StripesTop.gameObject.SetActive(stripes > 0f);
            m_StripesBottom.gameObject.SetActive(stripes > 0f);
            if (stripes > 0f)
            {
                // they sweep in from the sides, and crawl while they're up
                float tiles = m_Root.rect.width / 16f, scroll = show.T * 2f;
                m_StripesTop.uvRect = new Rect(scroll, 0f, tiles, 1f);
                m_StripesBottom.uvRect = new Rect(-scroll, 0f, tiles, 1f);
                float h = 16f * stripes;
                m_StripesTop.rectTransform.sizeDelta = new Vector2(0f, h);
                m_StripesBottom.rectTransform.sizeDelta = new Vector2(0f, h);
            }
            Ui.Show(m_Warning, on && show.Warning);
            if (on && show.Warning) m_Warning.color = show.WarningLit ? Warn : WarnDim;
            float edges = on ? show.Edges : 0f;
            m_Edges.color = new Color(1f, 0f, 0f, 0.22f * edges);
            Ui.Show(m_Edges, edges > 0f);
            Ui.Show(m_IntroName, on && show.NameCard);
            if (on && show.NameCard && m_IntroName.text != info.IntroName) m_IntroName.text = info.IntroName;
            PilotIntro(show, on && info.PilotName != null, info);
        }

        /// <summary>The pilot's intro (Trollogram kcaj up in his tabk's hatch): the band slams in with a flash, then the line, glitchy.</summary>
        void PilotIntro(in BossIntroShow show, bool on, in HudInfo info)
        {
            bool band = on && show.Band;
            Ui.Show(m_PilotBand, band);
            if (band)
            {
                if (m_PilotName.text != info.PilotName) m_PilotName.text = info.PilotName;
                float k = show.BandIn, size = Mathf.Lerp(1.6f, 1f, k);
                m_PilotBand.localScale = new Vector3(size, size, 1f);
            }
            float flash = band ? 1f - show.BandIn : 0f;
            m_PilotFlash.color = new Color(1f, 1f, 1f, 0.6f * flash);
            Ui.Show(m_PilotFlash, flash > 0f);
            bool line = on && show.Line;
            Ui.Show(m_PilotLine, line);
            if (line)
            {
                if (m_PilotLine.text != info.PilotLine) m_PilotLine.text = info.PilotLine;
                bool flick = Fx.TrolloFlicker.On(show.T); // the Trollograms' flicker
                m_PilotLine.color = new Color(Palette.Cyan.r, Palette.Cyan.g, Palette.Cyan.b, flick ? 0.4f : 1f);
            }
        }

        /// <summary>A soldier's colour on the team card.</summary>
        static Color CardColour(SoldierDef s) => Crew.Of(s)?.Card ?? new Color(0.6f, 0.6f, 0.65f, 0.95f);

        /// <summary>
        /// A team super's split card, while the action's frozen: the presser slams in from the left, the partner from
        /// the right, "TEAM SUPER!" over them and the combo's name under; then they're swept away.
        /// </summary>
        void TeamCard(World w, in HudInfo info)
        {
            TeamSuperShow show = w.Team;
            bool on = show != null && show.Freezing && info.Playing;
            m_TeamCard.gameObject.SetActive(on);
            if (!on) return;
            if (m_TeamShownLeft != show.Fired)
            {
                m_TeamShownLeft = show.Fired;
                m_TeamLeft.color = CardColour(show.Fired);
                m_TeamRight.color = CardColour(show.Partner);
                m_TeamLeftPic.sprite = Art.SkinFor(show.Fired).Portrait;
                m_TeamRightPic.sprite = Art.SkinFor(show.Partner).Portrait;
                m_TeamRightPic.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // facing the presser
                m_TeamLeftName.text = show.Fired.Name.ToUpperInvariant();
                m_TeamRightName.text = show.Partner.Name.ToUpperInvariant();
                m_TeamName.text = show.Combo.Name;
            }
            float t = show.CardTime, card = TeamSupers.Spec.Card;
            float slam = Mathf.Clamp01(t / 0.14f), leave = Mathf.Clamp01((t - (card - 0.12f)) / 0.12f);
            float off = (1f - slam) * 420f + leave * 420f;
            float shake = slam >= 1f && t < 0.24f ? Mathf.Sin(t * 120f) * 3f : 0f; // the slam lands with a judder
            m_TeamLeft.rectTransform.anchoredPosition = new Vector2(-2f - off + shake, 0f);
            m_TeamRight.rectTransform.anchoredPosition = new Vector2(2f + off - shake, 0f);
            m_TeamBand.color = new Color(0.06f, 0.04f, 0.08f, 0.85f * slam * (1f - leave));
            bool words = slam >= 1f && leave <= 0f;
            Ui.Show(m_TeamTitle, words);
            Ui.Show(m_TeamName, words);
            if (words) m_TeamTitle.color = Fx.TeamTitle.On(t) ? Palette.Gold : Color.white;
        }

        static Image Meter(RectTransform parent, string name, Vector2 at, out Image fill, Color color)
        {
            Image back = Ui.Picture(parent, name, null, Ui.TopLeft, Ui.TopLeft, at, new Vector2(MeterWidth + 2f, 6f), new Color(0.1f, 0.05f, 0.08f, 0.85f));
            fill = Ui.Picture(back.rectTransform, "Fill", null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(0f, 4f), color);
            return back;
        }

        /// <summary>The super meter (flashing when ready), the special's refill under it, and the ability's cooldown.</summary>
        void Kit(Player p)
        {
            KitMeters kit = p.Kit;
            m_SuperFill.rectTransform.sizeDelta = new Vector2(MeterWidth * kit.SuperCharge, 4f);
            m_SuperFill.color = kit.SuperReady && Fx.SuperReady.On(Time.unscaledTime) ? Color.white : Palette.Gold;
            m_RefillFill.rectTransform.sizeDelta = new Vector2(MeterWidth * kit.SpecialRefillProgress, 2f);
            Ui.Show(m_AbilityLabel, kit.HasAbility);
            Ui.Show(m_AbilityBack, kit.HasAbility);
            if (kit.HasAbility) m_AbilityFill.rectTransform.sizeDelta = new Vector2(MeterWidth * kit.AbilityProgress, 4f);
        }

        /// <summary>Spare lives by the hearts; while down, when they're back, the continue on offer, or that they're out.</summary>
        void Lives(Player p, bool coop)
        {
            LifeBank lives = p.Lives;
            if (lives.Lives != m_LivesShown) m_Lives.text = "LIVES " + (m_LivesShown = lives.Lives);
            DownLine line = DownLine.For(lives, p.Dead, p.DeadTime, coop, GameInput.TouchActive);
            string down = line.Down, note = line.Note;
            Ui.Show(m_Down, down != null);
            Ui.Show(m_DownNote, note != null);
            if (down != null && down != m_Down.text) m_Down.text = down;
            if (note != null && note != m_DownNote.text) m_DownNote.text = note;
        }

        /// <summary>A tank call-in's charge, over the soldier's head while SP is held.</summary>
        void CallIn(Player p, Camera cam)
        {
            float charge = p.CallInCharge;
            bool on = charge > 0f && !p.Dead;
            Ui.Show(m_CallBack, on);
            if (!on) return;
            m_CallBack.rectTransform.anchoredPosition = Ui.FromScreen(cam.WorldToScreenPoint(new Vector3(p.Pos.x, p.Pos.y + p.Height + 0.6f, 0f)));
            m_CallFill.rectTransform.sizeDelta = new Vector2(32f * charge, 4f);
        }

        public void Refresh(World w, Camera cam, in HudInfo info)
        {
            Player p = w.Player;
            Ui.Show(m_Build, !info.Demo);
            Ui.Show(m_Fps, !info.Demo);
            Ui.Show(m_Top, info.InGame);
            if (info.InGame)
            {
                int points = w.Scoring.Points;
                bool own = info.Net != null; // in co-op, our own points beside the shared score
                int ownPoints = own ? w.OwnPoints : -1;
                if (points != m_Points || ownPoints != m_OwnShown)
                {
                    m_Points = points;
                    m_OwnShown = ownPoints;
                    m_Score.text = own ? "SCORE " + DropIn.Points(points) + "  YOU " + DropIn.Points(w.OwnPoints) : "SCORE " + DropIn.Points(points);
                }
                int hi = Mathf.Max(info.HiScore, points);
                if (hi != m_HiScore) m_Hi.text = "HI " + DropIn.Points(m_HiScore = hi);
                int secs = Mathf.FloorToInt(w.Elapsed);
                if (secs != m_Seconds) m_Time.text = string.Format("TIME {0}:{1:00}", (m_Seconds = secs) / 60, secs % 60);
                int gun = p.GunAmmo > 0 ? p.Gun.Id : -1;
                PlayerTank tank = p.Riding;
                int tankRounds = tank == null ? -1 : tank.Rounds.Main * 100 + tank.Rounds.Specials * 10 + tank.Rounds.Supers;
                if (info.Watching)
                {
                    string watching = "WATCHING ROOM " + info.Net.RoomCode + (w.Remotes.Count == 0 ? "   THE PLAYERS ARE IN THE DEN" : "");
                    if (m_Arms.text != watching) m_Arms.text = watching;
                    m_ArmsGun = int.MinValue; // the soldier's line again once we're playing
                    tank = null;
                }
                else if (gun != m_ArmsGun || p.GunAmmo != m_ArmsAmmo || p.Specials != m_ArmsSpecials || tankRounds != m_ArmsTank)
                {
                    m_ArmsGun = gun;
                    m_ArmsAmmo = p.GunAmmo;
                    m_ArmsSpecials = p.Specials;
                    m_ArmsTank = tankRounds;
                    string arms = p.GunAmmo > 0 ? p.Gun.Hud + " " + p.GunAmmo : p.Def.Primary.Hud; // the font has no infinity glyph
                    m_Arms.text = tank != null
                        ? tank.Kit.Card.HudLine(tank.Rounds.Main, tank.Rounds.Specials, tank.Rounds.Supers, p.GunAmmo > 0 ? p.Gun.Hud : null, p.GunAmmo)
                        : "ARMS " + arms + "   " + p.Def.Special.Hud + " " + p.Specials;
                }
                Ui.Show(m_TankLabel, tank != null);
                Ui.Show(m_TankBack, tank != null);
                Ui.Show(m_FuelLabel, tank != null);
                Ui.Show(m_FuelBack, tank != null);
                if (tank != null)
                {
                    m_TankFill.rectTransform.sizeDelta = new Vector2(MeterWidth * Mathf.Clamp01(tank.Hp / (float)tank.MaxHp), 4f);
                    m_FuelFill.rectTransform.sizeDelta = new Vector2(MeterWidth * tank.Fuel.Fraction, 4f);
                    // a dry tank's label blinks
                    m_FuelLabel.color = tank.Fuel.Empty && Fx.Warning.On(Time.unscaledTime) ? Hurt : Arms;
                }
                Kit(p);
                Lives(p, info.Net != null);
                HideFrom(m_Hearts, info.Watching ? 0 : Hearts(m_Hearts, m_NewHeart, 0, p.MaxHp, p.Hp, new Vector2(8f, -24f), 13f, 12f, Color.white));
                // watching: no soldier of our own, so none of its meters, lives or warnings
                Ui.Show(m_Lives, !info.Watching);
                Ui.Show(m_SuperLabel, !info.Watching && info.SuperShown);
                Ui.Show(m_SuperBack, !info.Watching && info.SuperShown);
                if (info.Watching)
                {
                    Ui.Show(m_AbilityLabel, false);
                    Ui.Show(m_AbilityBack, false);
                    Ui.Show(m_Down, false);
                    Ui.Show(m_DownNote, false);
                }

                BossEnemy boss = w.Boss;
                bool bossBar = boss != null && boss.Engaged && !boss.Removed;
                Ui.Show(m_BossName, bossBar);
                Ui.Show(m_BossBack, bossBar);
                if (bossBar) m_BossName.text = boss.DisplayName;
                if (bossBar) m_BossFill.rectTransform.sizeDelta = new Vector2(180f * Mathf.Clamp01(boss.Hp / (float)boss.MaxHp), 6f);

                Ui.Show(m_Coop, info.Net != null);
                if (info.Net != null) Coop(w, cam, info);
                Ui.Show(m_Beacons, info.Net != null);
                if (info.Net != null) Beacons(w, cam);
            }

            bool starting = info.StateTime < (info.Note != null ? 4f : 2.2f) && !info.Paused;
            bool banner = info.Playing && (w.Victory || starting) && (!info.Demo || info.Note != null); // a clean scripted take (?sandbox) is filmed bare
            Ui.Show(m_Banner, banner);
            if (banner)
            {
                m_Banner.text = w.Victory ? "MISSION COMPLETE!" : MissionBoard.StartBanner(w.Mission, info.Net != null);
                m_Banner.fontSize = w.Victory ? 28 : 26;
            }
            // the call-in prompt, the first time, until you call: blinking so it's seen
            // then the coda's, teaching the tabk one thing at a time
            string coach = w.CallInCoach != null && w.CallInCoach.Showing ? CallInPrompt : w.CodaCoach?.Prompt;
            coach ??= info.SuperPrompt;
            if (coach != null && m_Coach.text != coach) m_Coach.text = coach;
            Ui.Show(m_Coach, info.Playing && !banner && coach != null && Time.unscaledTime % 0.8f < 0.55f);
            bool note = banner && !w.Victory && info.Note != null;
            Ui.Show(m_Note, note);
            if (note) m_Note.text = info.Note;

            Intro(info);
            TeamCard(w, info);
            Popups(w, cam);
            if (info.Fps != m_FpsShown || info.Muted != m_MutedShown)
                m_Fps.text = (m_FpsShown = info.Fps) + " FPS" + ((m_MutedShown = info.Muted) ? " · MUTED" : "");
            GameInput.Pad.TeamVisible = info.Playing && w.TeamReady != null;
            Pad(GameInput.Pad, p, info.Net != null);
            CallIn(p, cam);
        }

        void Coop(World w, Camera cam, in HudInfo info)
        {
            Player p = w.Player;
            int row = 0, hearts = 0;
            foreach (RemotePlayer r in w.Remotes)
            {
                int seat = Mathf.Clamp(r.Link.Seat, 0, 1);
                string name = seat == 0 ? "P1" : "P2";
                float y = 80f + 14f * row; // under the kit's meters
                Text label = Pooled(m_PartnerLabels, row, m_NewPartnerLabel);
                label.rectTransform.anchoredPosition = new Vector2(8f, -y);
                label.text = name;
                label.color = Palette.Players[seat];
                hearts = Hearts(m_PartnerHearts, m_NewPartnerHeart, hearts, r.MaxHp, r.Hp, new Vector2(28f, -(y + 2f)), 10f, 9f, Palette.Players[seat]);

                Text tag = Pooled(m_Tags, row, m_NewTag);
                tag.rectTransform.anchoredPosition = Ui.FromScreen(cam.WorldToScreenPoint(new Vector3(r.Pos.x, r.Pos.y + 1.9f, 0f)));
                tag.text = name + "  " + DropIn.Points(r.Link.Score);
                tag.color = Palette.Players[seat];
                row++;
            }
            HideFrom(m_PartnerLabels, row);
            HideFrom(m_Tags, row);
            HideFrom(m_PartnerHearts, hearts);

            INetSession net = info.Net;
            string room = net.RoomCode;
            bool alone = w.Remotes.Count == 0 && !w.Victory && !info.Watching;
            Ui.Show(m_Waiting, alone && Fx.Blink(Time.unscaledTime, 2f));
            Ui.Show(m_Room, alone);
            Ui.Show(m_Link, alone && room != null && info.PageUrl.Length > 0);
            if (alone)
            {
                m_Room.text = ScreenWords.CoopWaiting;
                m_Room.fontSize = 12;
                if (room != null) m_Link.text = "OR SEND THIS LINK:  " + CoopRoom.Link(info.PageUrl, room);
            }

            string leave = GameInput.TouchActive ? "EXIT TWICE TO LEAVE" : "ESC ESC TO LEAVE";
            string status = (info.Watching ? "WATCHING  ·  " : "") + "ROOM " + room + "  ·  " + net.PlayerCount + "/2" +
                            (net.Watchers > 0 && !info.Watching ? "  ·  " + net.Watchers + " WATCHING" : "") + "  ·  " + net.PingMs + " MS  ·  " + leave;
            if (status != m_StatusKey) m_Status.text = m_StatusKey = status;

            Ui.Show(m_Leave, info.LeaveArmed);
            if (info.LeaveArmed) m_Leave.text = GameInput.TouchActive ? "TAP EXIT AGAIN TO LEAVE THE GAME" : "PRESS ESC AGAIN TO LEAVE THE GAME";
        }

        /// <summary>
        /// Each partner off screen gets an arrow at the edge pointing at them, with how far off they are; one on screen gets
        /// their warnings under their tag.
        /// </summary>
        void Beacons(World w, Camera cam)
        {
            float halfH = Tuning.Camera.ViewHeight * 0.5f, halfW = w.View.HalfWidth;
            Vector3 c = cam.transform.position;
            var view = new Rect(c.x - halfW, c.y - halfH, 2f * halfW, 2f * halfH);
            int n = 0;
            foreach (RemotePlayer r in w.Remotes)
            {
                int seat = Mathf.Clamp(r.Link.Seat, 0, 1);
                Beacon(cam, view, n++, r.Pos, r.Height, seat == 0 ? "P1" : "P2", Palette.Players[seat], r.LowHealth, r.Away);
            }
            HideFrom(m_Arrows, n);
            HideFrom(m_ArrowLabels, n);
            HideFrom(m_Warnings, n);
        }

        /// <summary>One partner's beacon, slot <paramref name="i"/>. Returns true when they're on screen.</summary>
        bool Beacon(Camera cam, Rect view, int i, Vector2 feet, float height, string name, Color colour, bool low, bool away)
        {
            bool on = PartnerBeacon.OnScreen(view, feet, height);
            bool blink = Fx.Warning.On(Time.unscaledTime);
            Image arrow = Pooled(m_Arrows, i, m_NewArrow);
            Text label = Pooled(m_ArrowLabels, i, m_NewArrowLabel);
            Text warning = Pooled(m_Warnings, i, m_NewWarning);
            Vector2 at; // where the warning goes, on the HUD
            if (on)
            {
                Ui.Show(arrow, false);
                Ui.Show(label, false); // their own tag is already over them
                at = Ui.FromScreen(cam.WorldToScreenPoint(new Vector3(feet.x, feet.y + height + 1.0f, 0f)));
            }
            else
            {
                Vector2 edge = PartnerBeacon.EdgePoint(view, feet + Vector2.up * (height * 0.5f), out float angle);
                Vector2 hud = Ui.FromScreen(cam.WorldToScreenPoint(new Vector3(edge.x, edge.y, 0f)));
                arrow.rectTransform.anchoredPosition = hud;
                arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
                arrow.color = low && blink ? Low : away ? AwayGrey : colour;
                // the label sits just inside the arrow, toward the middle of the screen
                Vector2 inward = -new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                Vector2 spot = hud + inward * 40f; // far enough in that the label clears the arrow either side
                label.rectTransform.anchoredPosition = spot;
                label.text = name + "  " + PartnerBeacon.Metres(view, feet) + "M";
                label.color = colour;
                at = spot + new Vector2(0f, -10f);
            }
            string words = away && low ? "AWAY · LOW HEALTH" : away ? "AWAY" : low ? "LOW HEALTH" : null;
            Ui.Show(warning, words != null && (!low || blink || away));
            if (words != null)
            {
                if (warning.text != words) warning.text = words;
                warning.color = low && blink ? Low : away ? AwayGrey : Low;
                warning.rectTransform.anchoredPosition = at;
            }
            return on;
        }

        /// <summary>Score and pickup popups, floating where they happened in the world.</summary>
        void Popups(World w, Camera cam)
        {
            List<PopupText> popups = w.Scoring.Popups;
            for (int i = 0; i < popups.Count; i++)
            {
                PopupText pop = popups[i];
                Text t = Pooled(m_Popups, i, m_NewPopup);
                t.rectTransform.anchoredPosition = Ui.FromScreen(cam.WorldToScreenPoint(new Vector3(pop.Pos.x, pop.Pos.y, 0f)));
                if (!ReferenceEquals(t.text, pop.Text)) t.text = pop.Text;
                int size = pop.Size > 0 ? pop.Size : PopupSize;
                if (t.fontSize != size) t.fontSize = size;
                Color c = pop.Color;
                c.a = Mathf.Clamp01(Tuning.Hud.PopupLife - pop.Age);
                t.color = c;
            }
            HideFrom(m_Popups, popups.Count);
        }

        void Pad(ArcadePad pad, Player p, bool coop)
        {
            Ui.Show(m_StickBase, pad.Visible);
            Ui.Show(m_StickKnob, pad.Visible);
            if (pad.Visible)
            {
                m_StickBase.rectTransform.anchoredPosition = Down(pad.StickOrigin);
                m_StickKnob.rectTransform.anchoredPosition = Down(pad.StickKnob);
                m_StickKnob.color = new Color(1f, 1f, 1f, pad.StickActive ? 0.7f : 0.35f);
                int specials = p.Riding != null ? p.Riding.Rounds.Specials : p.Specials;
                if (specials != m_SpecialsShown)
                    m_ButtonLabels[(int)ArcadePad.Button.Special].text = "SP " + (m_SpecialsShown = specials);
                pad.AbilityVisible = p.Kit.HasAbility;
            }
            m_ButtonLabels[(int)ArcadePad.Button.Pause].text = coop ? "EXIT" : "II";
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                var b = (ArcadePad.Button)i;
                Image image = m_Buttons[i];
                Ui.Show(image, pad.Shown(b));
                if (!pad.Shown(b)) continue;
                bool held = pad.Held(b);
                image.rectTransform.anchoredPosition = Down(pad.Center(b));
                image.rectTransform.sizeDelta = Vector2.one * pad.Radius(b) * 2f;
                image.color = held ? PadHeld : b == ArcadePad.Button.Super && p.Kit.SuperReady ? PadReady
                    : b == ArcadePad.Button.Team ? (Fx.Blink(Time.unscaledTime, 5f) ? PadReady : PadHeld) : PadFill;
                m_ButtonLabels[i].color = held ? Color.white : new Color(1f, 1f, 1f, 0.85f);
            }
        }

        /// <summary>A row of hearts, taken from a pool starting at <paramref name="first"/>. Returns the next free index.</summary>
        static int Hearts(List<Image> pool, System.Func<Image> make, int first, int max, int hp, Vector2 at, float step, float size, Color full)
        {
            for (int i = 0; i < max; i++)
            {
                Image heart = Pooled(pool, first + i, make);
                heart.rectTransform.anchoredPosition = at + new Vector2(i * step, 0f);
                heart.rectTransform.sizeDelta = new Vector2(size, size);
                heart.color = i < hp ? full : EmptyHeart;
            }
            return first + max;
        }

        /// <summary>A HUD position (y down from the top) as an offset from the top-left corner.</summary>
        static Vector2 Down(Vector2 hud) => new Vector2(hud.x, -hud.y);

        static T Pooled<T>(List<T> pool, int i, System.Func<T> make) where T : Component
        {
            while (pool.Count <= i) pool.Add(make());
            Ui.Show(pool[i], true);
            return pool[i];
        }

        static void HideFrom<T>(List<T> pool, int count) where T : Component
        {
            for (int i = count; i < pool.Count; i++) Ui.Show(pool[i], false);
        }
    }
}
