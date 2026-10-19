using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The debrief screen (<c>docs/design/cutscenes.md</c>, The first debrief): the crew stand to one side and the numbers
    /// count up on the other. The tally, then the XP bar, then the rank badge with its RANK UP! stamp, then the
    /// sign-in offer for players who aren't signed in, then CONTINUE. <see cref="DebriefPlayer"/> keeps the time. The
    /// crew, their bubble and the colours are the stage's (<see cref="StageKit"/>), bigger.
    /// </summary>
    public sealed class DebriefView
    {
        const float Scale = 2.5f, RowGap = 15f;
        static readonly Color Dim = new Color(1f, 1f, 1f, 0.6f), Green = new Color(0.45f, 1f, 0.6f);
        static readonly string[] RowNames = { "SCORE", "BEST", "TIME", "GOONS", "ACCURACY", "LIVES LEFT", "CONTINUES LEFT" };

        readonly RectTransform m_Root, m_Panel, m_XpBlock, m_RankBlock, m_Stamp;
        readonly SpeechBubble m_Bubble;
        readonly Text[] m_Rows = new Text[RowNames.Length], m_Values = new Text[RowNames.Length];
        readonly Text m_NewBest, m_XpTotal, m_XpParts, m_RankName, m_RankLine, m_SignIn, m_Hint, m_StampText;
        readonly Image m_XpFill, m_RankFill, m_BadgeOuter, m_BadgeInner;
        readonly StageFigure m_Tones2, m_Kie;
        readonly Button m_Continue;
        DebriefPlayer m_Player;
        bool m_Tapped, m_Leave, m_RankSounded;
        int m_Blips;

        public DebriefView(Transform parent)
        {
            m_Root = Ui.Canvas(parent, "Debrief", 15);
            m_Root.gameObject.AddComponent<GraphicRaycaster>();
            Image shade = Ui.Cover(m_Root, "Shade", new Color(0.04f, 0.03f, 0.08f, 0.88f));
            shade.raycastTarget = true;
            shade.gameObject.AddComponent<Button>().onClick.AddListener(() => m_Tapped = true); // a tap anywhere

            m_Panel = Ui.Fill(m_Root, "Panel");
            Ui.Label(m_Panel, "Title", Ui.Middle, new Vector2(40f, 145f), 26, TextAnchor.MiddleCenter, Gold).text = "MISSION COMPLETE!";

            // the crew, to one side
            m_Tones2 = new StageFigure(m_Panel, "tones2", Ui.Middle, Scale);
            m_Tones2.Rt.anchoredPosition = new Vector2(-235f, -40f);
            m_Kie = new StageFigure(m_Panel, "kie", Ui.Middle, Scale);
            m_Kie.Rt.anchoredPosition = new Vector2(-170f, -40f);
            m_Kie.Rt.localScale = new Vector3(-1f, 1f, 1f); // facing tones2
            m_Bubble = new SpeechBubble(m_Panel, Ui.Middle, new Vector2(150f, 34f));
            m_Bubble.Place(new Vector2(-170f, 50f), -170f);

            // the tally
            for (int i = 0; i < RowNames.Length; i++)
            {
                float y = 110f - i * RowGap;
                m_Rows[i] = Ui.Label(m_Panel, RowNames[i], Ui.Middle, new Vector2(-30f, y), 11, TextAnchor.MiddleLeft, Color.white);
                m_Rows[i].text = RowNames[i];
                m_Values[i] = Ui.Label(m_Panel, RowNames[i] + " value", Ui.Middle, new Vector2(150f, y), 11, TextAnchor.MiddleRight, Gold);
            }
            m_NewBest = Ui.Label(m_Panel, "New best", Ui.Middle, new Vector2(210f, 110f - RowGap), 11, TextAnchor.MiddleCenter, Green);
            m_NewBest.text = "NEW BEST!";

            // the XP bar
            m_XpBlock = Ui.Rect(m_Panel, "XP", Ui.Middle, Ui.Middle, new Vector2(60f, -18f), new Vector2(260f, 40f));
            Ui.Label(m_XpBlock, "Label", Ui.Middle, new Vector2(-118f, 6f), 14, TextAnchor.MiddleLeft, Gold).text = "XP";
            Bar(m_XpBlock, new Vector2(10f, 6f), new Vector2(200f, 10f), out m_XpFill);
            m_XpTotal = Ui.Label(m_XpBlock, "Total", Ui.Middle, new Vector2(130f, 6f), 11, TextAnchor.MiddleRight, Color.white);
            m_XpParts = Ui.Label(m_XpBlock, "Parts", Ui.Middle, new Vector2(0f, -12f), 9, TextAnchor.MiddleCenter, Dim);
            m_XpParts.rectTransform.sizeDelta = new Vector2(320f, 14f);

            // the rank
            m_RankBlock = Ui.Rect(m_Panel, "Rank", Ui.Middle, Ui.Middle, new Vector2(60f, -72f), new Vector2(260f, 50f));
            m_BadgeOuter = Ui.Picture(m_RankBlock, "Badge", Ui.Disc, Ui.Middle, Ui.Middle, new Vector2(-105f, 0f), new Vector2(36f, 36f), Color.white);
            m_BadgeInner = Ui.Picture(m_RankBlock, "Badge inside", Ui.Disc, Ui.Middle, Ui.Middle, new Vector2(-105f, 0f), new Vector2(24f, 24f), Color.white);
            m_RankName = Ui.Label(m_RankBlock, "Name", Ui.Middle, new Vector2(-80f, 8f), 15, TextAnchor.MiddleLeft, Color.white);
            m_RankName.rectTransform.sizeDelta = new Vector2(220f, 20f);
            m_RankLine = Ui.Label(m_RankBlock, "Line", Ui.Middle, new Vector2(-80f, -8f), 9, TextAnchor.MiddleLeft, Dim);
            m_RankLine.rectTransform.sizeDelta = new Vector2(220f, 14f);
            Bar(m_RankBlock, new Vector2(30f, -20f), new Vector2(160f, 4f), out m_RankFill);
            m_Stamp = Ui.Rect(m_RankBlock, "Stamp", Ui.Middle, Ui.Middle, new Vector2(115f, 10f), new Vector2(110f, 26f));
            var stampBack = m_Stamp.gameObject.AddComponent<Image>();
            stampBack.color = new Color(0.85f, 0.15f, 0.2f, 0.95f);
            m_StampText = Ui.Label(m_Stamp, "Text", Ui.Middle, Vector2.zero, 15, TextAnchor.MiddleCenter, Color.white);
            m_StampText.text = "RANK UP!";

            // signing in, and on
            m_SignIn = Ui.Label(m_Panel, "Sign in", Ui.Middle, new Vector2(60f, -112f), 10, TextAnchor.MiddleCenter, Green);
            m_SignIn.text = "SIGN IN AT THE TOP RIGHT TO KEEP YOUR XP AND RANK";
            m_Continue = Ui.Button(m_Panel, "CONTINUE", new Vector2(60f, -142f), new Vector2(150f, 26f), () => m_Leave = true, out Text key);
            key.text = "ENTER";
            m_Hint = Ui.Label(m_Panel, "Hint", Ui.Middle, new Vector2(60f, -142f), 9, TextAnchor.MiddleCenter, Dim);
            m_Hint.text = "TAP TO GO ON";
            Hide();
        }

        static void Bar(RectTransform parent, Vector2 at, Vector2 size, out Image fill)
        {
            Ui.Picture(parent, "Bar", null, Ui.Middle, Ui.Middle, at, size + new Vector2(2f, 2f), new Color(0f, 0f, 0f, 0.7f));
            fill = Ui.Picture(parent, "Fill", null, Ui.Middle, new Vector2(0f, 0.5f), at - new Vector2(size.x / 2f, 0f), size, Gold);
        }

        public bool Showing => m_Player != null;

        public void Show(DebriefPlayer player)
        {
            m_Player = player;
            m_Tapped = m_Leave = m_RankSounded = false;
            m_Blips = 0;
            Ui.Show(m_Root, true);
            PlayerSkin tones2 = Art.SkinFor(Content.Tones2, 0), kie = Art.SkinFor(Content.Kie, 0);
            m_Tones2.Stand(tones2);
            m_Kie.Stand(kie);
            Refresh();
        }

        public void Hide()
        {
            m_Player = null;
            Ui.Show(m_Root, false);
        }

        /// <summary>A tap on the screen since the last call.</summary>
        public bool TakeTap()
        {
            bool t = m_Tapped;
            m_Tapped = false;
            return t;
        }

        /// <summary>CONTINUE was pressed since the last call.</summary>
        public bool TakeLeave()
        {
            bool l = m_Leave;
            m_Leave = false;
            return l;
        }

        public void Refresh()
        {
            DebriefPlayer p = m_Player;
            if (p == null) return;
            DebriefResult r = p.Result;
            bool tallyDone = p.Reached(DebriefPlayer.Part.Xp);
            float tally = tallyDone ? 1f : p.Progress;

            // the tally: each row counts up in turn, with a blip as it lands
            int rows = RowNames.Length;
            for (int i = 0; i < rows; i++)
            {
                float k = Mathf.Clamp01(tally * rows - i);
                Ui.Show(m_Rows[i], k > 0f);
                Ui.Show(m_Values[i], k > 0f);
                if (k > 0f) m_Values[i].text = Value(i, r, k);
                if (k >= 1f && i >= m_Blips)
                {
                    m_Blips = i + 1;
                    Sfx.Play(Sfx.Pickup, 0.25f);
                }
            }
            Ui.Show(m_NewBest, r.NewBest && tally >= 2f / rows);

            // the XP: the bar slides in, then fills from the run's parts
            bool xp = p.Reached(DebriefPlayer.Part.Xp);
            Ui.Show(m_XpBlock, xp);
            float xpK = !xp ? 0f : p.Now == DebriefPlayer.Part.Xp ? p.Progress : 1f;
            if (xp)
            {
                float slide = p.Now == DebriefPlayer.Part.Xp ? Mathf.Clamp01(p.T / 0.25f) : 1f;
                m_XpBlock.anchoredPosition = new Vector2(60f + (1f - slide) * 400f, -18f);
                int shown = r.XpBefore + Mathf.RoundToInt(r.XpGained * xpK);
                m_XpTotal.text = shown.ToString();
                m_XpFill.rectTransform.localScale = new Vector3(Progression.TokenProgress(shown), 1f, 1f);
                var parts = new System.Text.StringBuilder();
                int upTo = Mathf.CeilToInt(xpK * r.Xp.Count);
                for (int i = 0; i < upTo && i < r.Xp.Count; i++)
                    parts.Append(i == 0 ? "" : "   ").Append(r.Xp[i].Label).Append(" +").Append(r.Xp[i].Amount);
                m_XpParts.text = parts.ToString();
            }

            // the rank: the badge, its name, and the stamp when it goes up a rung
            bool rank = p.Reached(DebriefPlayer.Part.Rank);
            Ui.Show(m_RankBlock, rank);
            if (rank)
            {
                float k = p.Now == DebriefPlayer.Part.Rank ? p.Progress : 1f;
                int climb = r.ClimbBefore + Mathf.RoundToInt((r.ClimbAfter - r.ClimbBefore) * Mathf.Clamp01(k / 0.7f));
                bool up = r.RankedUp && k >= 0.7f;
                Rank shown = Progression.RankAt(up ? r.ClimbAfter : r.ClimbBefore);
                m_BadgeOuter.color = Hex(shown.Dark);
                m_BadgeInner.color = Hex(shown.Light);
                m_RankName.text = shown.Name.ToUpperInvariant();
                m_RankLine.text = shown.Line ?? "";
                m_RankFill.rectTransform.localScale = new Vector3(Progression.RungProgress(climb), 1f, 1f);
                Ui.Show(m_Stamp, up);
                if (up)
                {
                    float since = (k - 0.7f) * DebriefPlayer.RankSeconds;
                    float s = since < 0.15f ? Mathf.Lerp(2.2f, 1f, since / 0.15f) : 1f;
                    m_Stamp.localScale = new Vector3(s, s, 1f);
                    m_Stamp.localRotation = Quaternion.Euler(0f, 0f, 8f);
                    if (!m_RankSounded)
                    {
                        m_RankSounded = true;
                        Sfx.Play(Sfx.Intro, 0.8f); // the stamp's hit
                        if (Music.Muted) Sfx.Play(Sfx.Victory, 0.6f);
                        else MusicFlow.RankUp(); // the chip fanfare
                    }
                }
            }

            // kie's lines, the first time, and the sign-in offer
            m_Bubble.Say(p.KieSays);
            Ui.Show(m_SignIn, p.ShowsSignIn);
            bool end = p.Now == DebriefPlayer.Part.Continue;
            Ui.Show(m_Continue, end);
            Ui.Show(m_Hint, !end && p.Counted);
        }

        static string Value(int row, DebriefResult r, float k)
        {
            int Count(int n) => Mathf.RoundToInt(n * k);
            switch (row)
            {
                case 0: return DropIn.Points(Count(r.Score));
                case 1: return DropIn.Points(Count(r.Best));
                case 2:
                    int s = Count(r.Seconds);
                    return string.Format("{0}:{1:00}", s / 60, s % 60);
                case 3: return Count(r.Kills).ToString();
                case 4: return r.Accuracy < 0f ? "-" : Mathf.RoundToInt(r.Accuracy * 100f * k) + "%";
                case 5: return Count(r.Lives).ToString();
                default: return Count(r.Continues).ToString();
            }
        }

        static Color Hex(uint rgb) => new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);
    }
}
