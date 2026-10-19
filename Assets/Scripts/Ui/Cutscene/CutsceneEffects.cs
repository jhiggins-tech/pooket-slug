using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.CutsceneBackdrops;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' named effects (<see cref="Staging.Things"/>), whose looks and little animations live here: snow,
    /// the searchlights, spilt chips, tones2's water jet, the keycard's close-up, kcaj's string board close up, the motor pool's shutter with its cold
    /// light and work lights, the row of parked tabks and the Trollograms' junk, steam, the cage of light, dazed stars,
    /// z Z, and the alarm's red wash. Each is shown and animated as the shot's script has it (<see cref="ShotStage"/>).
    /// <para>
    /// Most sit in the world, among the cast; the close-up and the alarm sit on the stage, over it. Their pieces are made
    /// by <see cref="CutsceneView"/>, in drawing order among the cast's; see there.
    /// </para>
    /// </summary>
    public sealed class CutsceneEffects
    {
        // the other crew's paint on the parked tabks (tones2's pink is his own thing's colour)
        static readonly Color[] RowPaint = { new Color(1f, 0.62f, 0.8f), new Color(0.6f, 0.78f, 1f), new Color(1f, 0.9f, 0.5f) };

        readonly RectTransform m_World, m_Stage;
        readonly Image[] m_Snow = new Image[36], m_Chips = new Image[10], m_Jet = new Image[8], m_Beams = new Image[2];
        readonly Image[] m_Row = new Image[2], m_Junk = new Image[9], m_Steam = new Image[4], m_Lamps = new Image[3];
        readonly Image[] m_Cage = new Image[7], m_Stars = new Image[3], m_Frost = new Image[10];
        RectTransform m_ShutterMask, m_BigCard, m_Board;
        Image m_Shutter, m_ColdLight, m_Dim, m_Alarm;
        Text m_Zz;

        public CutsceneEffects(RectTransform world, RectTransform stage)
        {
            m_World = world;
            m_Stage = stage;
        }

        Image Picture(string name, Sprite sprite, float x, float y, float w, float h, Color color) => StageKit.Picture(m_World, name, sprite, x, y, w, h, color);

        // ------------------------------------------------------------------ made in drawing order (CutsceneView)

        /// <summary>The base's searchlights, past the hangout's window.</summary>
        public void MakeSearchlights()
        {
            for (int i = 0; i < m_Beams.Length; i++)
            {
                m_Beams[i] = Picture("Searchlight", null, 267f + i * 10f, 111f, 3f, 36f, new Color(1f, 1f, 0.8f, 0.35f));
                m_Beams[i].rectTransform.pivot = new Vector2(0.5f, 0f);
            }
        }

        /// <summary>The motor pool's work lights, over the row.</summary>
        public void MakeWorkLights()
        {
            for (int i = 0; i < m_Lamps.Length; i++)
                m_Lamps[i] = Picture("Work light", null, MotorRow[i] + 6f, 0f, 46f, 74f, new Color(1f, 0.95f, 0.75f, 0.12f));
        }

        /// <summary>The row of the crew's parked tabks, the Trollograms' junk over it, and the steam.</summary>
        public void MakeRow()
        {
            for (int i = 0; i < m_Row.Length; i++) m_Row[i] = Picture("Tabk", Art.Tank, 0f, 0f, Art.Tank.rect.width, Art.Tank.rect.height, RowPaint[i + 1]);
            for (int i = 0; i < m_Junk.Length; i++) m_Junk[i] = Picture("Trollogram junk", null, 0f, 0f, 3f, 2f, Cyan);
            for (int i = 0; i < m_Steam.Length; i++) m_Steam[i] = Picture("Steam", null, 0f, 0f, 5f, 5f, new Color(0.85f, 0.88f, 0.95f, 0.5f));
        }

        /// <summary>The cage of light, and the dazed stars.</summary>
        public void MakeCageAndStars()
        {
            for (int i = 0; i < m_Cage.Length; i++) m_Cage[i] = Picture("Cage of light", null, 0f, 0f, 1f, 1f, Cyan);
            for (int i = 0; i < m_Stars.Length; i++) m_Stars[i] = Picture("Dazed", null, 0f, 0f, 2f, 2f, Gold);
        }

        /// <summary>The cold store's frost, rolling out over the floor in front of the cast's feet.</summary>
        public void MakeFrost()
        {
            for (int i = 0; i < m_Frost.Length; i++) m_Frost[i] = Picture("Frost", null, 0f, 0f, 14f, 5f, new Color(0.85f, 0.95f, 1f, 0.4f));
        }

        /// <summary>z Z, the shutter's cold light, and the shutter (its slats in a mask the size of the opening).</summary>
        public void MakeSleepAndShutter()
        {
            m_Zz = Ui.Label(m_World, "Zz", Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleCenter, Paper);
            m_Zz.text = "z Z";
            m_ColdLight = Picture("Cold light", null, MotorOpenX + MotorOpenW / 2f, 0f, MotorOpenW + 30f, 22f, new Color(0.7f, 0.95f, 1f, 0f));
            m_ShutterMask = Ui.Rect(m_World, "Shutter", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(MotorOpenW, MotorOpenH) * Px);
            m_ShutterMask.pivot = Vector2.zero;
            m_ShutterMask.gameObject.AddComponent<RectMask2D>();
            m_Shutter = Ui.Picture(m_ShutterMask, "Slats", White, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(MotorOpenW, MotorOpenH) * Px, Color.white);
            m_Shutter.rectTransform.pivot = Vector2.zero;
        }

        /// <summary>The chips.</summary>
        public void MakeChips()
        {
            for (int i = 0; i < m_Chips.Length; i++) m_Chips[i] = Picture("Chip", null, 0f, 0f, 3f, 2f, new Color(1f, 0.82f, 0.3f));
        }

        /// <summary>tones2's water jet, and the snow, in front of everything.</summary>
        public void MakeWaterAndSnow()
        {
            for (int i = 0; i < m_Jet.Length; i++) m_Jet[i] = Picture("Water", null, 0f, 0f, 4f, 3f, new Color(0.45f, 0.75f, 1f));
            for (int i = 0; i < m_Snow.Length; i++) m_Snow[i] = Picture("Snow", null, 0f, 0f, 1.5f, 1.5f, new Color(1f, 1f, 1f, 0.85f));
        }

        /// <summary>On the stage: the keycard's close-up, the room dimmed and the card big enough to read.</summary>
        public void MakeCloseup()
        {
            m_Dim = Ui.Cover(m_Stage, "Dim", new Color(0.03f, 0.02f, 0.05f, 0.7f));
            m_BigCard = Ui.Rect(m_Stage, "Keycard close-up", Ui.Middle, Ui.Middle, new Vector2(0f, 26f), new Vector2(124f, 78f) * Px);
            m_BigCard.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.85f, 0.92f);
            m_BigCard.gameObject.AddComponent<Outline>().effectColor = Ink;
            Ui.Picture(m_BigCard, "Stripe", White, Ui.TopCenter, Ui.TopCenter, new Vector2(0f, -10f), new Vector2(124f, 18f) * Px, new Color(0.08f, 0.2f, 0.26f));
            Ui.Picture(m_BigCard, "Photo", White, Ui.BottomLeft, Ui.BottomLeft, new Vector2(10f, 12f) * Px, new Vector2(26f, 30f) * Px, new Color(0.12f, 0.3f, 0.36f));
            Ui.Picture(m_BigCard, "Chip", White, Ui.BottomRight, Ui.BottomRight, new Vector2(-10f, 14f) * Px, new Vector2(16f, 12f) * Px, Gold);
            Ui.Label(m_BigCard, "Title", Ui.TopCenter, new Vector2(0f, -18f), 22, TextAnchor.UpperCenter, Color.white).text = "MILITARY BASE";
            Ui.Label(m_BigCard, "Gate", Vector2.zero, new Vector2(84f, 66f), 18, TextAnchor.MiddleLeft, Ink).text = "NORTH GATE";
            Ui.Label(m_BigCard, "Access", Vector2.zero, new Vector2(84f, 40f), 11, TextAnchor.MiddleLeft, new Color(0.08f, 0.2f, 0.26f)).text = "ACCESS: ALL AREAS";
            foreach (Text t in m_BigCard.GetComponentsInChildren<Text>()) t.GetComponent<Shadow>().enabled = false;
        }

        /// <summary>
        /// On the stage: kcaj's string board, close up (docs/design/cutscenes.md, The Mission two briefing): cork, sticky
        /// notes tied together with red string, and one string off to a photo of the creamery, its giant soft-serve sign
        /// on the roof, with THEY NEED IT COLD scrawled under it.
        /// </summary>
        public void MakeBoard()
        {
            Color cork = new Color(0.6f, 0.4f, 0.24f), red = new Color(0.9f, 0.12f, 0.1f);
            m_Board = Ui.Rect(m_Stage, "String board close-up", Ui.Middle, Ui.Middle, new Vector2(0f, 10f), new Vector2(200f, 120f) * Px);
            m_Board.gameObject.AddComponent<Image>().color = cork;
            m_Board.gameObject.AddComponent<Outline>().effectColor = new Color(0.28f, 0.17f, 0.1f);
            void Bit(string name, float x, float y, float w, float h, float turn, Color c)
            {
                Image img = Ui.Picture(m_Board, name, White, Ui.Middle, Ui.Middle, new Vector2(x, y) * Px, new Vector2(w, h) * Px, c);
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, turn);
            }
            void Tie(float x0, float y0, float x1, float y1)
            {
                Vector2 a = new Vector2(x0, y0), b = new Vector2(x1, y1);
                Bit("String", (x0 + x1) / 2f, (y0 + y1) / 2f, Vector2.Distance(a, b), 1.2f, Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg, red);
            }
            // the notes, and the strings between them
            Vector2[] notes = { new Vector2(-80f, 38f), new Vector2(-50f, 8f), new Vector2(-82f, -28f), new Vector2(-30f, 40f), new Vector2(-20f, -36f), new Vector2(8f, 14f) };
            for (int i = 0; i < notes.Length - 1; i++) Tie(notes[i].x, notes[i].y, notes[i + 1].x, notes[i + 1].y);
            Tie(notes[0].x, notes[0].y, notes[2].x, notes[2].y);
            Color[] paper = { new Color(1f, 0.92f, 0.4f), new Color(1f, 0.62f, 0.75f), new Color(0.6f, 0.9f, 1f) };
            for (int i = 0; i < notes.Length; i++) Bit("Note", notes[i].x, notes[i].y, 15f, 14f, (i * 37 % 11) - 5f, paper[i % paper.Length]);
            // the one that matters: off the last note, to the creamery's photo
            Tie(notes[notes.Length - 1].x, notes[notes.Length - 1].y, 58f, 14f);
            Bit("Photo", 58f, 16f, 64f, 46f, 3f, Paper);
            Bit("Sky", 58f, 20f, 56f, 34f, 3f, new Color(0.55f, 0.75f, 0.9f));
            Bit("Factory", 56f, 9f, 46f, 14f, 3f, new Color(0.45f, 0.45f, 0.5f));
            Bit("Cone", 60f, 22f, 6f, 10f, 3f, new Color(0.85f, 0.62f, 0.3f));
            Bit("Scoop", 60f, 29f, 10f, 7f, 3f, new Color(1f, 0.75f, 0.85f));
            Bit("Pin", 58f, 37f, 4f, 4f, 0f, red);
            foreach (Vector2 n in notes) Bit("Pin", n.x, n.y + 5f, 3f, 3f, 0f, red);
            Text scrawl = Ui.Label(m_Board, "Scrawl", Ui.Middle, new Vector2(56f, -22f) * Px, 13, TextAnchor.MiddleCenter, Ink);
            scrawl.text = "THEY NEED IT COLD";
            scrawl.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            scrawl.GetComponent<Shadow>().enabled = false;
        }

        /// <summary>On the stage: the alarm's red wash.</summary>
        public void MakeAlarm()
        {
            m_Alarm = Ui.Cover(m_Stage, "Alarm", new Color(1f, 0.1f, 0.05f, 0f));
        }

        /// <summary>Gives the shutter its slats, once the backdrops are painted.</summary>
        public void Paint() => m_Shutter.sprite = Slats;

        // ------------------------------------------------------------------ each frame

        /// <summary>Shows and animates each effect as the shot's script has it at <paramref name="t"/> (<paramref name="now"/> is the scene's clock).</summary>
        public void Draw(ShotStage stage, float t, float now, float ground)
        {
            for (int i = 0; i < m_Snow.Length; i++) Snow(i, now, stage.Shown("snow", t));
            Searchlights(stage, t, now);
            Chips(stage, t, ground);
            Jet(stage, t, ground);
            Closeup(stage, t, now);
            Board(stage, t);
            Shutter(stage, t, now, ground);
            Row(stage, t, now, ground);
            Steam(stage, t, now, ground);
            Frost(stage, t, now, ground);
            Cage(stage, t, ground);
            Stars(stage, t, now, ground);
            Zz(stage, t, now, ground);
            Alarm(stage, t);
        }

        void Snow(int i, float now, bool on)
        {
            Image s = m_Snow[i];
            Ui.Show(s, on);
            if (!on) return;
            float speed = 10f + (i * 7 % 9);
            float y = StageH - ((now * speed + i * 53f) % (StageH + 4f));
            float x = (i * 97f % StageW) + Mathf.Sin(now * 0.9f + i) * 4f;
            s.rectTransform.anchoredPosition = new Vector2(x, y) * Px;
        }

        /// <summary>The base's searchlights sweeping past the window.</summary>
        void Searchlights(ShotStage stage, float t, float now)
        {
            bool on = stage.Shown("searchlights", t);
            for (int i = 0; i < m_Beams.Length; i++)
            {
                Ui.Show(m_Beams[i], on);
                if (on) m_Beams[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(now * 1.3f + i * 2f) * 20f);
            }
        }

        /// <summary>The chips: bursting out of the dropped bag (<c>burst</c>), or already all over the floor (<c>spill</c>).</summary>
        void Chips(ShotStage stage, float t, float ground)
        {
            bool on = stage.Shown("chips", t);
            float burst = stage.Since("chips", "burst", t);
            float[] where = stage.Numbers("chips", "burst", t) ?? stage.Numbers("chips", "spill", t);
            if (stage.Latest("chips", t, "burst", "spill")?.Verb == "spill") burst = 9f;
            on &= where != null && burst >= 0f;
            for (int i = 0; i < m_Chips.Length; i++)
            {
                Ui.Show(m_Chips[i], on);
                if (!on) continue;
                float dir = (i - m_Chips.Length / 2f) * 2.4f;
                float k = Mathf.Clamp01(burst / 0.35f);
                float hop = Mathf.Sin(k * Mathf.PI) * (4f + i % 3 * 3f);
                m_Chips[i].rectTransform.anchoredPosition = new Vector2(where[0] + dir * k * 1.6f, ground + hop) * Px;
            }
        }

        /// <summary>tones2's water jet, arcing from one place to another while it sprays.</summary>
        void Jet(ShotStage stage, float t, float ground)
        {
            StageStep s = stage.Latest("jet", t, "spray");
            bool on = s != null && t < s.To && stage.Shown("jet", t);
            for (int i = 0; i < m_Jet.Length; i++)
            {
                Ui.Show(m_Jet[i], on);
                if (!on) continue;
                float k = ((t - s.From) * 3f + i / (float)m_Jet.Length) % 1f;
                float x = Mathf.Lerp(s.N[0], s.N[1], k);
                float y = ground + 17f + Mathf.Sin(k * Mathf.PI) * 6f;
                m_Jet[i].rectTransform.anchoredPosition = new Vector2(x, y) * Px;
            }
        }

        /// <summary>The keycard, close up: the room dimmed, and the card sliding up into view, flickering like the Trollograms.</summary>
        void Closeup(ShotStage stage, float t, float now)
        {
            bool on = stage.Shown("closeup", t);
            Ui.Show(m_Dim, on);
            Ui.Show(m_BigCard, on);
            if (!on) return;
            float up = Mathf.Clamp01((t - stage.ShownSince("closeup", t)) / 0.35f);
            m_BigCard.anchoredPosition = new Vector2((int)(now * 10f) % 9 == 0 ? 2f : 0f, Mathf.Lerp(-140f, 26f, 1f - (1f - up) * (1f - up)));
            m_BigCard.localRotation = Quaternion.Euler(0f, 0f, (1f - up) * -8f);
        }

        /// <summary>kcaj's string board, close up: the room dimmed, and the camera creeping in on the board.</summary>
        void Board(ShotStage stage, float t)
        {
            bool on = stage.Shown("board", t);
            Ui.Show(m_Board, on);
            if (!on)
            {
                if (!stage.Shown("closeup", t)) Ui.Show(m_Dim, false);
                return;
            }
            Ui.Show(m_Dim, true);
            float k = 1f + Mathf.Clamp01((t - stage.ShownSince("board", t)) / 6f) * 0.08f;
            m_Board.localScale = new Vector3(k, k, 1f);
        }

        /// <summary>
        /// The motor pool's blast shutter, down until it grinds up (<c>open</c>, juddering as it goes) or is already up;
        /// the cold light it lets out, and the work lights over the row, steady or stuttering.
        /// </summary>
        void Shutter(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("shutter", t);
            Ui.Show(m_ShutterMask, on);
            Ui.Show(m_ColdLight, on);
            StageStep s = stage.Latest("shutter", t, "open", "up");
            float up = s == null ? 0f : s.Verb == "up" ? 1f : s.Progress(t);
            if (on)
            {
                float grind = up > 0f && up < 1f ? (int)(now * 16f) % 2 : 0f;
                m_ShutterMask.anchoredPosition = new Vector2(MotorOpenX, ground) * Px;
                m_Shutter.rectTransform.anchoredPosition = new Vector2(0f, up * MotorOpenH + grind) * Px;
                m_ColdLight.rectTransform.anchoredPosition = new Vector2(MotorOpenX + MotorOpenW / 2f, 0f) * Px;
                m_ColdLight.rectTransform.sizeDelta = new Vector2(MotorOpenW + 30f, ground + 4f) * Px;
                m_ColdLight.color = new Color(0.7f, 0.95f, 1f, 0.22f * up);
            }
            bool lamps = stage.Shown("lamps", t);
            bool stutter = stage.Doing("lamps", "stutter", t);
            if (!on) up = 1f; // lamps without a shutter are just on
            for (int i = 0; i < m_Lamps.Length; i++)
            {
                Ui.Show(m_Lamps[i], lamps);
                if (!lamps) continue;
                bool flick = stutter && (int)(now * 11f + i * 5) % 17 == 0; // a work light stutters now and then
                m_Lamps[i].rectTransform.anchoredPosition = new Vector2(MotorRow[i] + 6f, ground + 6f) * Px;
                m_Lamps[i].color = new Color(1f, 0.95f, 0.75f, up * (flick ? 0.05f : 0.14f));
            }
        }

        /// <summary>The rest of the crew's tabks parked in a row under the work lights, and the Trollograms' junk all over the row.</summary>
        void Row(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("row", t);
            for (int i = 0; i < m_Row.Length; i++)
            {
                Ui.Show(m_Row[i], on);
                if (on) m_Row[i].rectTransform.anchoredPosition = new Vector2(MotorRow[i + 1], ground + 2f) * Px;
            }
            bool junk = stage.Shown("junk", t);
            for (int i = 0; i < m_Junk.Length; i++)
            {
                Ui.Show(m_Junk[i], junk);
                if (!junk) continue;
                float x = MotorRow[i % MotorRow.Length] - 14f + (i * 11 % 26);
                float y = ground + 8f + (i * 7 % 13);
                bool lit = (int)(now * 8f + i * 3) % 6 != 0;
                m_Junk[i].color = new Color(0.45f, 1f, 1f, lit ? 0.9f : 0.25f);
                m_Junk[i].rectTransform.anchoredPosition = new Vector2(x, y) * Px;
            }
        }

        /// <summary>
        /// Frost rolling out of a door along the floor: low puffs that spread right from where it's placed, faster and
        /// further as they go, and thin out; it rolls in from nothing as it comes on.
        /// </summary>
        void Frost(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("frost", t);
            Vector2 at = stage.Where("frost", t, now);
            Color c = stage.Colour("frost", t, now);
            float since = t - stage.ShownSince("frost", t);
            for (int i = 0; i < m_Frost.Length; i++)
            {
                Image f = m_Frost[i];
                Ui.Show(f, on);
                if (!on) continue;
                float k = (now * 0.35f + i / (float)m_Frost.Length) % 1f;
                float reach = Mathf.Clamp01(since / 1.2f) * 150f;
                float x = at.x + k * reach + Mathf.Sin(now * 1.3f + i * 2f) * 3f;
                f.rectTransform.anchoredPosition = new Vector2(x, ground + at.y + 1f + (i % 3) * 2f + k * 3f) * Px;
                f.rectTransform.localScale = new Vector3(1f + k * 1.5f, 1f + k * 0.4f, 1f);
                f.color = new Color(c.r, c.g, c.b, c.a * 0.5f * (1f - k) * Mathf.Clamp01(since * 3f));
            }
        }

        /// <summary>Steam (or smoke, coloured so) rising off a parking space.</summary>
        void Steam(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("steam", t);
            Vector2 at = stage.Where("steam", t, now);
            Color c = stage.Colour("steam", t, now);
            for (int i = 0; i < m_Steam.Length; i++)
            {
                Ui.Show(m_Steam[i], on);
                if (!on) continue;
                float k = (now * 0.5f + i / (float)m_Steam.Length) % 1f;
                m_Steam[i].color = new Color(c.r, c.g, c.b, 0.45f * (1f - k));
                m_Steam[i].rectTransform.anchoredPosition = new Vector2(at.x - 12f + i * 8f + Mathf.Sin(now * 2f + i) * 2f, ground + at.y + k * 22f) * Px;
            }
        }

        /// <summary>The cage of light round the real kie: bars and two rails, lit until it flickers out (<c>flicker-out</c>).</summary>
        void Cage(ShotStage stage, float t, float ground)
        {
            const float W = 18f, H = 30f;
            StageStep s = stage.Latest("cage", t, "flicker-out");
            bool lit = stage.Shown("cage", t) && (s == null || PilotFreeingSpec.CageLitFor(t - s.From, s.To - s.From));
            float x = stage.Where("cage", t, 0f).x;
            for (int i = 0; i < m_Cage.Length; i++)
            {
                Ui.Show(m_Cage[i], lit);
                if (!lit) continue;
                bool rail = i >= 5;
                RectTransform rt = m_Cage[i].rectTransform;
                rt.sizeDelta = (rail ? new Vector2(W + 2f, 1f) : new Vector2(1f, H)) * Px;
                rt.anchoredPosition = new Vector2(rail ? x : x - W / 2f + i * (W / 4f), ground + (rail && i == 6 ? H : 0f)) * Px;
            }
        }

        /// <summary>Dazed stars, circling over whoever they're on.</summary>
        void Stars(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("stars", t);
            Vector2 at = stage.Where("stars", t, now);
            for (int i = 0; i < m_Stars.Length; i++)
            {
                Ui.Show(m_Stars[i], on);
                if (!on) continue;
                float a = now * 4f + i * Mathf.PI * 2f / m_Stars.Length;
                m_Stars[i].rectTransform.anchoredPosition = new Vector2(at.x + Mathf.Cos(a) * 7f, ground + at.y + Mathf.Sin(a) * 2f) * Px;
            }
        }

        /// <summary>z Z, drifting up off a sleeper, over and over.</summary>
        void Zz(ShotStage stage, float t, float now, float ground)
        {
            bool on = stage.Shown("zz", t);
            Ui.Show(m_Zz, on);
            if (!on) return;
            Vector2 at = stage.Where("zz", t, now);
            float k = Mathf.Repeat(t - stage.ShownSince("zz", t), 1.6f) / 1.6f;
            m_Zz.rectTransform.anchoredPosition = new Vector2(at.x + k * 8f, ground + at.y + k * 10f) * Px;
            m_Zz.color = new Color(Paper.r, Paper.g, Paper.b, 1f - k);
        }

        /// <summary>The alarm's slow red wash over everything: once a second, never a strobe.</summary>
        void Alarm(ShotStage stage, float t)
        {
            bool on = stage.Shown("alarm", t);
            Ui.Show(m_Alarm, on);
            if (!on) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f);
            m_Alarm.color = new Color(1f, 0.1f, 0.05f, 0.1f * pulse);
        }
    }
}
