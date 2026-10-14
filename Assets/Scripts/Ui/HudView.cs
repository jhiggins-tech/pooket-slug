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
        /// <summary>Co-op: the session, or null in a solo game.</summary>
        public INetSession Net;
        public string PageUrl;
        /// <summary>Co-op: the first press of Esc (or EXIT) is waiting for the second.</summary>
        public bool LeaveArmed;
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

        readonly RectTransform m_Root, m_Top;
        readonly Text m_Score, m_Arms, m_Hi, m_Time, m_BossName, m_Build, m_Fps, m_Banner, m_Note;
        readonly Image m_BossBack, m_BossFill;
        readonly List<Image> m_Hearts = new List<Image>();
        readonly List<Text> m_Popups = new List<Text>();
        int m_Points = -1, m_HiScore = -1, m_Seconds = -1, m_FpsShown = -1;
        int m_ArmsGun = -1, m_ArmsAmmo = -1, m_ArmsSpecials = -1;
        bool m_MutedShown;
        readonly System.Func<Text> m_NewPopup, m_NewPartnerLabel, m_NewTag;
        readonly System.Func<Image> m_NewHeart, m_NewPartnerHeart; // made once: a lambda per frame would be garbage

        // co-op
        readonly RectTransform m_Coop;
        readonly Text m_Waiting, m_Room, m_Link, m_Status, m_Down, m_Leave;
        readonly List<Text> m_PartnerLabels = new List<Text>(), m_Tags = new List<Text>();
        readonly List<Image> m_PartnerHearts = new List<Image>();
        string m_StatusKey;

        // touch pad
        readonly RectTransform m_Pad;
        readonly Image m_StickBase, m_StickKnob;
        readonly Image[] m_Buttons = new Image[4];
        readonly Text[] m_ButtonLabels = new Text[4];
        int m_SpecialsShown = -1;

        public HudView(Transform parent)
        {
            m_Root = Ui.Canvas(parent, "HUD", 0);
            m_NewPopup = () => Ui.Label(m_Root, "Popup", Ui.BottomLeft, Vector2.zero, 9, TextAnchor.MiddleCenter, Color.white);
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
            m_BossName.text = "REBEL TANK";
            m_BossBack = Ui.Picture(m_Top, "Boss bar", null, Ui.TopCenter, Ui.TopCenter, new Vector2(0f, -21f), new Vector2(182f, 8f), new Color(0.1f, 0.05f, 0.08f, 0.85f));
            m_BossFill = Ui.Picture(m_BossBack.rectTransform, "Health", null, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(180f, 6f), new Color(0.9f, 0.25f, 0.2f));
            m_Banner = Ui.Label(m_Root, "Banner", Ui.Middle, new Vector2(0f, 60f), 26, TextAnchor.MiddleCenter, Gold);
            m_Note = Ui.Label(m_Root, "Note", Ui.Middle, new Vector2(0f, 32f), 12, TextAnchor.MiddleCenter, CoopBlue);

            m_Coop = Ui.Fill(m_Top, "Co-op");
            m_Waiting = Ui.Label(m_Coop, "Waiting", Ui.TopCenter, new Vector2(0f, -40f), 12, TextAnchor.UpperCenter, CoopBlue);
            m_Waiting.text = "WAITING FOR A PARTNER";
            m_Room = Ui.Label(m_Coop, "Room", Ui.TopCenter, new Vector2(0f, -56f), 16, TextAnchor.UpperCenter, Color.white);
            m_Link = Ui.Label(m_Coop, "Link", Ui.TopCenter, new Vector2(0f, -76f), 8, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.8f));
            m_Status = Ui.Label(m_Coop, "Status", new Vector2(0.5f, 0f), new Vector2(0f, 28f), 8, TextAnchor.UpperCenter, Faint);
            m_Down = Ui.Label(m_Coop, "Down", Ui.Middle, new Vector2(0f, 30f), 18, TextAnchor.MiddleCenter, Hurt);
            m_Leave = Ui.Label(m_Coop, "Leave", Ui.Middle, new Vector2(0f, -16f), 12, TextAnchor.MiddleCenter, Color.white);

            m_Build = Ui.Label(m_Root, "Build", Ui.BottomLeft, new Vector2(6f, 14f), 8, TextAnchor.UpperLeft, Faint);
            m_Build.text = "v" + Application.version + " · Unity " + Application.unityVersion + " · built with GameCI";
            m_Fps = Ui.Label(m_Root, "FPS", Ui.BottomRight, new Vector2(-6f, 14f), 8, TextAnchor.UpperRight, Faint);

            m_Pad = Ui.Fill(m_Root, "Touch pad");
            m_StickBase = Ui.Picture(m_Pad, "Stick", Ui.Disc, Ui.TopLeft, Ui.Middle, Vector2.zero, Vector2.one * (ArcadePad.StickRadius + 12f) * 2f, PadFill);
            m_StickKnob = Ui.Picture(m_Pad, "Knob", Ui.Disc, Ui.TopLeft, Ui.Middle, Vector2.zero, Vector2.one * 36f, Color.white);
            string[] names = { "FIRE", "JUMP", "SP", "II" };
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                m_Buttons[i] = Ui.Picture(m_Pad, names[i], Ui.Disc, Ui.TopLeft, Ui.Middle, Vector2.zero, Vector2.one, PadFill);
                m_ButtonLabels[i] = Ui.Label(m_Buttons[i].rectTransform, "Label", Ui.Middle, Vector2.zero, i == (int)ArcadePad.Button.Pause ? 8 : 10, TextAnchor.MiddleCenter, Color.white);
                m_ButtonLabels[i].text = names[i];
            }
        }

        public void Refresh(World w, Camera cam, in HudInfo info)
        {
            Player p = w.Player;
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
                if (gun != m_ArmsGun || p.GunAmmo != m_ArmsAmmo || p.Specials != m_ArmsSpecials)
                {
                    m_ArmsGun = gun;
                    m_ArmsAmmo = p.GunAmmo;
                    m_ArmsSpecials = p.Specials;
                    string arms = p.GunAmmo > 0 ? p.Gun.Hud + " " + p.GunAmmo : p.Def.Primary.Hud; // the font has no infinity glyph
                    m_Arms.text = "ARMS " + arms + "   " + p.Def.Special.Hud + " " + p.Specials;
                }
                HideFrom(m_Hearts, Hearts(m_Hearts, m_NewHeart, 0, p.MaxHp, p.Hp, new Vector2(8f, -24f), 13f, 12f, Color.white));

                TankBoss boss = w.Boss;
                bool bossBar = boss != null && boss.Engaged && !boss.Removed;
                Ui.Show(m_BossName, bossBar);
                Ui.Show(m_BossBack, bossBar);
                if (bossBar) m_BossFill.rectTransform.sizeDelta = new Vector2(180f * Mathf.Clamp01(boss.Hp / (float)boss.MaxHp), 6f);

                Ui.Show(m_Coop, info.Net != null);
                if (info.Net != null) Coop(w, cam, info);
            }

            bool starting = info.StateTime < (info.Note != null ? 4f : 2.2f) && !info.Paused;
            bool banner = info.Playing && (w.Victory || starting);
            Ui.Show(m_Banner, banner);
            if (banner)
            {
                m_Banner.text = w.Victory ? "MISSION COMPLETE!" : "MISSION 1  START!";
                m_Banner.fontSize = w.Victory ? 28 : 26;
            }
            bool note = banner && !w.Victory && info.Note != null;
            Ui.Show(m_Note, note);
            if (note) m_Note.text = info.Note;

            Popups(w, cam);
            if (info.Fps != m_FpsShown || info.Muted != m_MutedShown)
                m_Fps.text = (m_FpsShown = info.Fps) + " FPS" + ((m_MutedShown = info.Muted) ? " · MUTED" : "");
            Pad(GameInput.Pad, p, info.Net != null);
        }

        void Coop(World w, Camera cam, in HudInfo info)
        {
            Player p = w.Player;
            int row = 0, hearts = 0;
            foreach (RemotePlayer r in w.Remotes)
            {
                int skin = Mathf.Clamp(r.Link.Skin, 0, 1);
                string name = skin == 0 ? "P1" : "P2";
                float y = 56f + 14f * row;
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

            Ui.Show(m_Down, p.Dead);
            if (p.Dead) m_Down.text = "DOWN!  BACK IN " + Mathf.Max(1, Mathf.CeilToInt(Tuning.Coop.RespawnDelay - p.DeadTime));
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
                if (p.Specials != m_SpecialsShown)
                    m_ButtonLabels[(int)ArcadePad.Button.Special].text = "SP " + (m_SpecialsShown = p.Specials);
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
                image.color = held ? PadHeld : PadFill;
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
