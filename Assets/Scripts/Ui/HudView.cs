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
        /// <summary>A move's demo is playing (<c>?demo=</c>): the build line and frame rate stay out of the recording.</summary>
        public bool Demo;
        /// <summary>Co-op: the session, or null in a solo game.</summary>
        public INetSession Net;
        public string PageUrl;
        /// <summary>Co-op: the first press of Esc (or EXIT) is waiting for the second.</summary>
        public bool LeaveArmed;
        /// <summary>The boss intro under way, and the boss's name for its card; null name when there's none.</summary>
        public BossIntroShow Intro;
        public string IntroName;
    }

    /// <summary>
    /// The in-game HUD in uGUI: score, hearts, arms, high score, time, the boss's bar, popups, co-op status, the
    /// touch pad and the footer. It only shows things; the menus are drawn by <see cref="Game"/>.
    /// </summary>
    public sealed class HudView
    {
        static readonly Color Gold = new Color(1f, 0.85f, 0.3f), Arms = new Color(1f, 0.9f, 0.5f), Faint = new Color(1f, 1f, 1f, 0.75f);
        static readonly Color EmptyHeart = new Color(0.2f, 0.15f, 0.2f, 0.7f), Hurt = new Color(1f, 0.5f, 0.4f);
        static readonly Color CoopBlue = new Color(0.6f, 0.9f, 1f);
        static readonly Color[] SkinColors = { new Color(1f, 0.5f, 0.4f), new Color(0.45f, 0.72f, 1f) };
        static readonly Color PadFill = new Color(0.08f, 0.06f, 0.1f, 0.35f), PadHeld = new Color(1f, 0.62f, 0.2f, 0.6f);
        static readonly Color PadReady = new Color(1f, 0.85f, 0.3f, 0.45f);

        readonly RectTransform m_Root, m_Top;
        // the boss intro
        static readonly Color Warn = new Color(1f, 0.18f, 0.12f), WarnDim = new Color(0.55f, 0.05f, 0.05f);
        readonly Image m_Edges;
        readonly RawImage m_StripesTop, m_StripesBottom;
        readonly Text m_Warning, m_IntroName;
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
        int m_Points = -1, m_HiScore = -1, m_Seconds = -1, m_FpsShown = -1;
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
            m_SuperBack = Meter(m_Top, "Super meter", new Vector2(24f, -57f), out m_SuperFill, Gold);
            m_RefillFill = Ui.Picture(m_SuperBack.rectTransform, "SP refill", null, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, -3f), new Vector2(0f, 2f), new Color(0.55f, 0.85f, 1f, 0.9f));
            m_AbilityLabel = Ui.Label(m_Top, "Ability", Ui.TopLeft, new Vector2(8f, -67f), 8, TextAnchor.UpperLeft, Arms);
            m_AbilityLabel.text = "AB";
            m_AbilityBack = Meter(m_Top, "Ability meter", new Vector2(24f, -69f), out m_AbilityFill, CoopBlue);
            m_TankLabel = Ui.Label(m_Top, "Tank", Ui.TopLeft, new Vector2(8f, -79f), 8, TextAnchor.UpperLeft, Arms);
            m_TankLabel.text = "TK";
            m_TankBack = Meter(m_Top, "Tank health", new Vector2(24f, -81f), out m_TankFill, new Color(0.55f, 0.9f, 0.45f));
            m_FuelLabel = Ui.Label(m_Top, "Fuel", Ui.TopLeft, new Vector2(8f, -91f), 8, TextAnchor.UpperLeft, Arms);
            m_FuelLabel.text = "FU";
            m_FuelBack = Meter(m_Top, "Tank fuel", new Vector2(24f, -93f), out m_FuelFill, new Color(1f, 0.6f, 0.2f));
            m_CallBack = Ui.Picture(m_Root, "Call-in", null, Ui.BottomLeft, Ui.Middle, Vector2.zero, new Vector2(34f, 6f), new Color(0.1f, 0.05f, 0.08f, 0.85f));
            m_CallFill = Ui.Picture(m_CallBack.rectTransform, "Charge", null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(0f, 4f), new Color(1f, 0.85f, 0.3f));
            m_Banner = Ui.Label(m_Root, "Banner", Ui.Middle, new Vector2(0f, 60f), 26, TextAnchor.MiddleCenter, Gold);
            m_Edges = Ui.Picture(m_Root, "Warning edges", null, Ui.Middle, Ui.Middle, Vector2.zero, Vector2.zero, Color.clear);
            m_Edges.rectTransform.anchorMin = Vector2.zero;
            m_Edges.rectTransform.anchorMax = Vector2.one;
            m_Edges.raycastTarget = false;
            m_StripesTop = Stripes(m_Root, "Stripes top", 1f);
            m_StripesBottom = Stripes(m_Root, "Stripes bottom", 0f);
            m_Warning = Ui.Label(m_Root, "Warning", Ui.Middle, new Vector2(0f, 40f), 44, TextAnchor.MiddleCenter, Warn);
            m_Warning.text = "WARNING!";
            m_IntroName = Ui.Label(m_Root, "Boss name", Ui.Middle, new Vector2(0f, 2f), 18, TextAnchor.MiddleCenter, Color.white);
            m_Note = Ui.Label(m_Root, "Note", Ui.Middle, new Vector2(0f, 32f), 12, TextAnchor.MiddleCenter, CoopBlue);
            m_Coach = Ui.Label(m_Root, "Call-in hint", Ui.Middle, new Vector2(0f, 46f), 14, TextAnchor.MiddleCenter, Gold);
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
            m_TeamTitle = Ui.Label(m_TeamCard, "Title", Ui.Middle, new Vector2(0f, 76f), 30, TextAnchor.MiddleCenter, Gold);
            m_TeamTitle.text = "TEAM SUPER!";
            m_TeamName = Ui.Label(m_TeamCard, "Combo", Ui.Middle, new Vector2(0f, -72f), 22, TextAnchor.MiddleCenter, Color.white);
            m_TeamCard.gameObject.SetActive(false);

            m_Coop = Ui.Fill(m_Top, "Co-op");
            m_Waiting = Ui.Label(m_Coop, "Waiting", Ui.TopCenter, new Vector2(0f, -40f), 12, TextAnchor.UpperCenter, CoopBlue);
            m_Waiting.text = "WAITING FOR A PARTNER";
            m_Room = Ui.Label(m_Coop, "Room", Ui.TopCenter, new Vector2(0f, -56f), 16, TextAnchor.UpperCenter, Color.white);
            m_Link = Ui.Label(m_Coop, "Link", Ui.TopCenter, new Vector2(0f, -76f), 8, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.8f));
            m_Status = Ui.Label(m_Coop, "Status", new Vector2(0.5f, 0f), new Vector2(0f, 28f), 8, TextAnchor.UpperCenter, Faint);
            m_Down = Ui.Label(m_Top, "Down", Ui.Middle, new Vector2(0f, 30f), 18, TextAnchor.MiddleCenter, Hurt);
            m_DownNote = Ui.Label(m_Top, "Down note", Ui.Middle, new Vector2(0f, 10f), 10, TextAnchor.MiddleCenter, Color.white);
            m_Lives = Ui.Label(m_Top, "Lives", Ui.TopLeft, new Vector2(78f, -22f), 10, TextAnchor.UpperLeft, Arms);
            m_Leave = Ui.Label(m_Coop, "Leave", Ui.Middle, new Vector2(0f, -16f), 12, TextAnchor.MiddleCenter, Color.white);

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
        }

        /// <summary>A soldier's colour on the team card.</summary>
        static Color CardColour(SoldierDef s) => s == Content.Tones2 ? new Color(1f, 0.72f, 0.1f, 0.95f)
            : s == Content.Kie ? new Color(0.25f, 0.85f, 0.8f, 0.95f) : new Color(0.6f, 0.6f, 0.65f, 0.95f);

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
                m_TeamLeftPic.sprite = Art.SkinFor(show.Fired, 0).TorsoForward;
                m_TeamRightPic.sprite = Art.SkinFor(show.Partner, 1).TorsoForward;
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
            if (words) m_TeamTitle.color = (int)(t * 12f) % 2 == 0 ? Gold : Color.white;
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
            m_SuperFill.color = kit.SuperReady && (int)(Time.unscaledTime * 6f) % 2 == 0 ? Color.white : Gold;
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
            string down = null, note = null;
            if (p.Dead && !lives.OutOfLives)
            {
                down = "DOWN!  BACK IN " + Mathf.Max(1, Mathf.CeilToInt(Tuning.Lives.RespawnDelay - p.DeadTime));
                if (lives.Lives == 0 && p.DeadTime < Tuning.Lives.RespawnDelay) note = "NO LIVES LEFT AFTER THIS ONE";
            }
            else if (lives.Offering)
            {
                down = "CONTINUE?  " + Mathf.CeilToInt(lives.OfferLeft);
                string press = GameInput.TouchActive ? "TAP FIRE" : "PRESS FIRE";
                note = press + " TO CONTINUE  ·  " + lives.Continues + (lives.Continues == 1 ? " CONTINUE LEFT" : " CONTINUES LEFT");
            }
            else if (lives.IsOut && coop)
            {
                down = "OUT!";
                note = "YOUR PARTNER'S NEXT 1UP BRINGS YOU BACK";
            }
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
                if (points != m_Points) m_Score.text = "SCORE " + (m_Points = points).ToString("000000");
                int hi = Mathf.Max(info.HiScore, points);
                if (hi != m_HiScore) m_Hi.text = "HI " + (m_HiScore = hi).ToString("000000");
                int secs = Mathf.FloorToInt(w.Elapsed);
                if (secs != m_Seconds) m_Time.text = string.Format("TIME {0}:{1:00}", (m_Seconds = secs) / 60, secs % 60);
                int gun = p.GunAmmo > 0 ? p.Gun.Id : -1;
                PlayerTank tank = p.Riding;
                int tankRounds = tank == null ? -1 : tank.Rounds.Main * 100 + tank.Rounds.Specials * 10 + tank.Rounds.Supers;
                if (gun != m_ArmsGun || p.GunAmmo != m_ArmsAmmo || p.Specials != m_ArmsSpecials || tankRounds != m_ArmsTank)
                {
                    m_ArmsGun = gun;
                    m_ArmsAmmo = p.GunAmmo;
                    m_ArmsSpecials = p.Specials;
                    m_ArmsTank = tankRounds;
                    string arms = p.GunAmmo > 0 ? p.Gun.Hud + " " + p.GunAmmo : p.Def.Primary.Hud; // the font has no infinity glyph
                    m_Arms.text = tank != null
                        ? "TONES2'S TABK  WATER " + tank.Rounds.Main + "   MUD " + tank.Rounds.Specials + "   SPEW " + tank.Rounds.Supers
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
                    m_FuelLabel.color = tank.Fuel.Empty && (int)(Time.unscaledTime * 4f) % 2 == 0 ? Hurt : Arms;
                }
                Kit(p);
                Lives(p, info.Net != null);
                HideFrom(m_Hearts, Hearts(m_Hearts, m_NewHeart, 0, p.MaxHp, p.Hp, new Vector2(8f, -24f), 13f, 12f, Color.white));

                BossEnemy boss = w.Boss;
                bool bossBar = boss != null && boss.Engaged && !boss.Removed;
                Ui.Show(m_BossName, bossBar);
                Ui.Show(m_BossBack, bossBar);
                if (bossBar) m_BossName.text = boss.DisplayName;
                if (bossBar) m_BossFill.rectTransform.sizeDelta = new Vector2(180f * Mathf.Clamp01(boss.Hp / (float)boss.MaxHp), 6f);

                Ui.Show(m_Coop, info.Net != null);
                if (info.Net != null) Coop(w, cam, info);
            }

            bool starting = info.StateTime < (info.Note != null ? 4f : 2.2f) && !info.Paused;
            bool banner = info.Playing && (w.Victory || starting);
            Ui.Show(m_Banner, banner);
            if (banner)
            {
                m_Banner.text = w.Victory ? "MISSION COMPLETE!" : w.Mission.Name + "  START!";
                m_Banner.fontSize = w.Victory ? 28 : 26;
            }
            // the call-in prompt, the first time, until you call: blinking so it's seen
            // then the coda's, teaching the tabk one thing at a time
            string coach = w.CallInCoach != null && w.CallInCoach.Showing ? CallInPrompt : w.CodaCoach?.Prompt;
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
                int skin = Mathf.Clamp(r.Link.Skin, 0, 1);
                string name = skin == 0 ? "P1" : "P2";
                float y = 80f + 14f * row; // under the kit's meters
                Text label = Pooled(m_PartnerLabels, row, m_NewPartnerLabel);
                label.rectTransform.anchoredPosition = new Vector2(8f, -y);
                label.text = name;
                label.color = SkinColors[skin];
                hearts = Hearts(m_PartnerHearts, m_NewPartnerHeart, hearts, r.MaxHp, r.Hp, new Vector2(28f, -(y + 2f)), 10f, 9f, SkinColors[skin]);

                Text tag = Pooled(m_Tags, row, m_NewTag);
                tag.rectTransform.anchoredPosition = Ui.FromScreen(cam.WorldToScreenPoint(new Vector3(r.Pos.x, r.Pos.y + 1.9f, 0f)));
                tag.text = name;
                tag.color = SkinColors[skin];
                row++;
            }
            HideFrom(m_PartnerLabels, row);
            HideFrom(m_Tags, row);
            HideFrom(m_PartnerHearts, hearts);

            INetSession net = info.Net;
            string room = net.RoomCode;
            bool alone = w.Remotes.Count == 0 && !w.Victory;
            Ui.Show(m_Waiting, alone && (int)(Time.unscaledTime * 2f) % 2 == 0);
            Ui.Show(m_Room, alone);
            Ui.Show(m_Link, alone && room != null && info.PageUrl.Length > 0);
            if (alone)
            {
                m_Room.text = room != null ? "ROOM CODE  " + room : "QUICK MATCH: THE NEXT PLAYER TO PRESS Q JOINS YOU";
                m_Room.fontSize = room != null ? 16 : 10;
                if (room != null) m_Link.text = "OR SEND THIS LINK:  " + info.PageUrl + "?room=" + room;
            }

            string leave = GameInput.TouchActive ? "EXIT TWICE TO LEAVE" : "ESC ESC TO LEAVE";
            string status = (room != null ? "ROOM " + room : "QUICK MATCH") + "  ·  " + net.PlayerCount + "/2  ·  " + net.PingMs + " MS  ·  " + leave;
            if (status != m_StatusKey) m_Status.text = m_StatusKey = status;

            Ui.Show(m_Leave, info.LeaveArmed);
            if (info.LeaveArmed) m_Leave.text = GameInput.TouchActive ? "TAP EXIT AGAIN TO LEAVE THE GAME" : "PRESS ESC AGAIN TO LEAVE THE GAME";
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
                    : b == ArcadePad.Button.Team ? ((int)(Time.unscaledTime * 5f) % 2 == 0 ? PadReady : PadHeld) : PadFill;
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
