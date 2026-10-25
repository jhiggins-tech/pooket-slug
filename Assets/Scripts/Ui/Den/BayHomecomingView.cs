using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// kie's tabk coming home, drawn in the bay (<see cref="KieHomecoming"/>, in Core, has the timing): his tabk on the
    /// repair stands, the bare hull till the crane lowers the frosted projector on; kie's legs out from under it, then kie
    /// beside it and climbing in; the weasel with its spanner and the cone handed back; the sparks, the steam, the lens's
    /// gold glow, the little Trollogram of the tabk waving from the wall; the lines in bubbles; and the name card.
    /// <see cref="BayView"/> makes it and calls it each frame while it plays.
    /// </summary>
    sealed class BayHomecomingView
    {
        /// <summary>Tabks in the bay are 1.5 times the game's sprite (<see cref="BayView"/>).</summary>
        const float Scale = 1.5f;
        /// <summary>kie beside his tabk (behind it: it faces left, toward the door), and where the weasel sits.</summary>
        const float KieX = BayMap.StandX + 52f, WeaselX = BayMap.StandX + 64f;
        /// <summary>How high the crane holds the projector before it lowers it.</summary>
        const float CraneLift = 70f;
        static readonly Color Legs = new Color(0.35f, 0.3f, 0.25f), Boots = new Color(0.15f, 0.12f, 0.12f), Fur = new Color(0.72f, 0.5f, 0.3f);
        static readonly Color Frost = new Color(0.82f, 0.94f, 1f), Steam = new Color(1f, 1f, 1f, 0.6f);

        readonly RectTransform m_Card;
        readonly Image m_Turret, m_Cable, m_LegL, m_LegR, m_Weasel, m_Spanner, m_Cone, m_Spark, m_Glow, m_Holo;
        readonly Image[] m_Steam = new Image[3];
        readonly StageFigure m_Kie;
        readonly SpeechBubble m_Bubble;
        readonly Text m_CardName, m_CardTitle;

        /// <param name="world">The bay's world, made after its tabks and before you, so the cast stands with you.</param>
        /// <param name="stage">The stage over it, for the name card.</param>
        public BayHomecomingView(RectTransform world, RectTransform stage)
        {
            Sprite turret = KieTabkArt.Turret;
            m_Cable = Picture(world, "Crane cable", null, 0f, 0f, 1f, 1f, new Color(0.1f, 0.1f, 0.12f));
            m_Turret = Ui.Picture(world, "Projector", turret, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, turret.rect.size * Scale * Px, Color.white);
            for (int i = 0; i < m_Steam.Length; i++) m_Steam[i] = Picture(world, "Steam", null, 0f, 0f, 3f, 3f, Steam);
            m_Glow = Picture(world, "Lens glow", null, 0f, 0f, 7f, 7f, new Color(1f, 0.85f, 0.35f, 0.6f));
            m_LegL = Picture(world, "kie's leg", null, 0f, 0f, 12f, 3f, Legs);
            m_LegR = Picture(world, "kie's leg", null, 0f, 0f, 12f, 3f, Legs);
            m_Kie = new StageFigure(world, "kie");
            m_Weasel = Picture(world, "Weasel", null, 0f, 0f, 5f, 4f, Fur);
            m_Spanner = Picture(world, "Spanner", null, 0f, 0f, 1f, 6f, new Color(0.62f, 0.66f, 0.72f));
            Sprite cone = KcajArt.Cone;
            m_Cone = Ui.Picture(world, "Cone", cone, Vector2.zero, Ui.Middle, Vector2.zero, cone.rect.size * Px, Color.white);
            m_Cone.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f); // held up, the empty end on top
            m_Spark = Picture(world, "Welding", Art.Spark[0], 0f, 0f, 10f, 10f, Color.white);
            Sprite tabk = KieTabkArt.Parked;
            m_Holo = Ui.Picture(world, "Little Trollogram", tabk, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, tabk.rect.size * 0.6f * Px, Cyan);
            m_Bubble = new SpeechBubble(world, Vector2.zero);

            m_Card = Ui.Rect(stage, "Name card", Ui.TopCenter, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(300f, 40f));
            var band = m_Card.gameObject.AddComponent<Image>();
            band.color = Band;
            band.raycastTarget = false;
            m_CardName = Ui.Label(m_Card, "Name", Ui.Middle, new Vector2(0f, 7f), 16, TextAnchor.MiddleCenter, Gold);
            m_CardName.text = KieHomecoming.CardName;
            m_CardTitle = Ui.Label(m_Card, "Title", Ui.Middle, new Vector2(0f, -10f), 10, TextAnchor.MiddleCenter, Color.white);
            m_CardTitle.text = KieHomecoming.CardTitle;
            Hide();
        }

        public void Hide()
        {
            foreach (Graphic g in new Graphic[] { m_Turret, m_Cable, m_LegL, m_LegR, m_Weasel, m_Spanner, m_Cone, m_Spark, m_Glow, m_Holo })
                Ui.Show(g, false);
            foreach (Image s in m_Steam) Ui.Show(s, false);
            Ui.Show(m_Kie.Rt, false);
            m_Bubble.Hide();
            Ui.Show(m_Card, false);
        }

        /// <summary>
        /// Draws the beat at its time, with kie's tabk on the stands (<paramref name="body"/>, its sprite swapped for the
        /// bare hull, then spinning reels) and the player at <paramref name="playerHead"/>.
        /// </summary>
        public void Draw(KieHomecoming beat, Image body, Vector2 playerHead, float clock)
        {
            float ground = BayMap.StandY;
            Vector2 feet = new Vector2(BayMap.StandX, ground);

            // the tabk: the bare hull till the projector's on, then whole, its reels turning as they spin up
            bool spinning = beat.Reels > 0f && Fx.Blink(clock, 6f + 18f * beat.Reels);
            bool lowering = beat.Projector && beat.Lowered < 1f;
            body.sprite = !beat.Projector || lowering ? KieTabkArt.Bare : spinning ? KieTabkArt.ParkedSpun : KieTabkArt.Parked;
            Ui.Show(m_Turret, lowering);
            Ui.Show(m_Cable, lowering);
            if (lowering)
            {
                float lift = (1f - beat.Lowered) * CraneLift;
                m_Turret.rectTransform.anchoredPosition = (feet + new Vector2(0f, lift)) * Px;
                m_Turret.color = Frost;
                float top = ground + lift + 27f * Scale, crane = 168f;
                m_Cable.rectTransform.anchoredPosition = new Vector2(feet.x, top) * Px;
                m_Cable.rectTransform.sizeDelta = new Vector2(1f, crane - top) * Px;
            }
            if (!lowering && beat.Shot < 3) body.color = Color.Lerp(Frost, Color.white, Mathf.Clamp01(beat.T - KieHomecoming.LowerTo));
            else body.color = Color.white;

            // steam curling off the copper pipes, once the projector's on
            bool steam = beat.Projector && beat.Lowered >= 1f;
            for (int i = 0; i < m_Steam.Length; i++)
            {
                Ui.Show(m_Steam[i], steam);
                if (!steam) continue;
                float k = (clock * 0.7f + i / 3f) % 1f;
                m_Steam[i].rectTransform.anchoredPosition = new Vector2(feet.x + 14f + i * 5f + Mathf.Sin(k * 6f + i) * 2f, ground + 34f + k * 18f) * Px;
                m_Steam[i].color = new Color(1f, 1f, 1f, 0.6f * (1f - k));
            }

            // the welding under the crane, on and off
            Ui.Show(m_Spark, beat.Sparking);
            if (beat.Sparking)
            {
                m_Spark.sprite = Art.Spark[Fx.Spark.Step(clock)];
                m_Spark.rectTransform.anchoredPosition = new Vector2(feet.x + 24f, ground + 4f) * Px;
            }

            // kie: legs out from under the tabk's back, then out and standing, then up and in at the hatch
            float k1 = beat.Kie;
            bool under = k1 < 0.5f;
            float slide = Mathf.Clamp01(k1 * 2f) * 8f;
            Ui.Show(m_LegL, under);
            Ui.Show(m_LegR, under);
            if (under)
            {
                float x0 = feet.x + BayMap.TabkW / 2f - 6f + slide;
                m_LegL.rectTransform.anchoredPosition = new Vector2(x0, ground) * Px;
                m_LegR.rectTransform.anchoredPosition = new Vector2(x0 + 1f, ground + 3f) * Px;
                m_LegL.color = m_LegR.color = Fx.Blink(clock, 3f) ? Legs : Boots; // working away under there
            }
            bool standing = !under && k1 < 2f;
            Ui.Show(m_Kie.Rt, standing);
            Vector2 kieAt = new Vector2(KieX, ground);
            if (standing)
            {
                PlayerSkin skin = Art.SkinFor(Content.Kie);
                float climb = Mathf.Clamp01(k1 - 1f);
                kieAt = Vector2.Lerp(new Vector2(KieX, ground), new Vector2(feet.x + 8f, ground + 30f), climb);
                m_Kie.Pose(skin, climb > 0f ? new StagePose(StageLegs.Jump, StageTorso.Lounge) : new StagePose(StageLegs.Idle, StageTorso.Unarmed));
                m_Kie.At(kieAt.x, kieAt.y);
                m_Kie.Rt.localScale = new Vector3(-1f, 1f, 1f) * (1f - 0.3f * climb); // facing his tabk and the door
            }

            // the weasel: the spanner up, the cone back, then a hop in after him
            float hop = beat.Hop;
            Ui.Show(m_Weasel, hop < 1f);
            Vector2 weasel = Vector2.Lerp(new Vector2(WeaselX, ground), new Vector2(feet.x + 10f, ground + 32f), hop);
            weasel.y += Mathf.Sin(hop * Mathf.PI) * 16f;
            m_Weasel.rectTransform.anchoredPosition = weasel * Px;
            Ui.Show(m_Spanner, beat.Spanner);
            if (beat.Spanner) m_Spanner.rectTransform.anchoredPosition = (weasel + new Vector2(0f, 4f)) * Px;
            Ui.Show(m_Cone, beat.Cone);
            if (beat.Cone) m_Cone.rectTransform.anchoredPosition = (weasel + new Vector2(0f, 9f)) * Px;

            // the lens glowing gold, and the little Trollogram of the tabk on the wall, waving
            Ui.Show(m_Glow, beat.Glow);
            if (beat.Glow)
            {
                Vector2 lens = feet + new Vector2(KieTabkArt.LensPivot.x - KieTabkArt.LensLength, KieTabkArt.LensPivot.y) * Scale;
                m_Glow.rectTransform.anchoredPosition = (lens - new Vector2(0f, 3.5f)) * Px;
                m_Glow.color = new Color(1f, 0.85f, 0.35f, 0.45f + 0.2f * Mathf.Sin(clock * 6f));
            }
            Ui.Show(m_Holo, beat.Holo);
            if (beat.Holo)
            {
                m_Holo.rectTransform.anchoredPosition = new Vector2(feet.x + 6f, 112f + Mathf.Round(Mathf.Sin(clock * 3f))) * Px;
                m_Holo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(clock * 5f) * 8f); // waving
                m_Holo.color = new Color(Cyan.r, Cyan.g, Cyan.b, Fx.Step(clock, 20f, 4) == 0 ? 0.3f : 0.6f);
            }

            // the line, from kie (or where he climbed in) or the player
            var line = beat.Line;
            if (line == null) m_Bubble.Hide();
            else
            {
                Vector2 head = line.Value.kie ? (kieAt + new Vector2(0f, 30f)) * Px : playerHead;
                m_Bubble.Rt.SetAsLastSibling();
                float w = m_Bubble.Say(line.Value.text);
                Vector2 at = head + new Vector2(0f, 16f);
                at.x = Mathf.Max(at.x, w / 2f + 4f);
                m_Bubble.Place(at, head.x);
            }

            // the name card, dropping in from the top
            Ui.Show(m_Card, beat.Card > 0f);
            float drop = beat.Card * beat.Card * (3f - 2f * beat.Card);
            m_Card.anchoredPosition = new Vector2(0f, Mathf.Lerp(44f, -40f, drop));
        }
    }
}
