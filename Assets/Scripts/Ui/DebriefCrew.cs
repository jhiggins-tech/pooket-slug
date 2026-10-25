using UnityEngine;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>
    /// The debrief's crew, to one side of its numbers (<see cref="DebriefView"/>): tones2 and kie, and kcaj once he's been
    /// freed, with their bubble; and They're tracking us played on them (<see cref="TrackingUs"/>, its timing), with kcaj's
    /// sticky note slapped over the accuracy number, where it stays for the rest of the debrief.
    /// </summary>
    public sealed class DebriefCrew
    {
        const float Feet = -40f, BubbleY = 50f;
        /// <summary>Where each stands: two of them as ever, or three with kcaj (a little left, to make room).</summary>
        const float Tones2X = -235f, KieX = -170f, Tones2X3 = -255f, KieX3 = -195f, KcajX3 = -135f;
        /// <summary>kcaj's lean at its fullest: his top in towards the numbers and down a pixel, and his feet a step on (art pixels, panel units).</summary>
        const float LeanTop = 2f, LeanStep = 6f;
        /// <summary>The slap: the note lands from this much bigger, over these seconds.</summary>
        const float SlapFrom = 1.8f, SlapSeconds = 0.12f;
        /// <summary>kcaj, still a little frosty from the cold store, at his first debrief.</summary>
        static readonly Color Frosty = new Color(0.8f, 0.92f, 1f);
        /// <summary>The sticky note's yellow (the stage's <c>note</c> prop, <see cref="Staging"/>), and its gummed strip.</summary>
        static readonly Color NoteYellow = new Color(1f, 0.92f, 0.3f), NoteStrip = new Color(0.9f, 0.78f, 0.22f);

        readonly StageFigure m_Tones2, m_Kie, m_Kcaj;
        readonly SpeechBubble m_Bubble;
        readonly RectTransform m_Note;
        PlayerSkin m_Tones2Skin, m_KcajSkin;
        bool m_NoteSounded;

        /// <param name="noteAt">The middle of the accuracy number, where the note goes.</param>
        public DebriefCrew(RectTransform panel, float scale, Vector2 noteAt)
        {
            m_Tones2 = new StageFigure(panel, "tones2", Ui.Middle, scale);
            m_Kie = new StageFigure(panel, "kie", Ui.Middle, scale);
            m_Kie.Rt.localScale = new Vector3(-1f, 1f, 1f); // facing tones2
            m_Kcaj = new StageFigure(panel, "kcaj", Ui.Middle, scale); // facing the numbers
            m_Bubble = new SpeechBubble(panel, Ui.Middle, new Vector2(150f, 34f));

            Image note = Ui.Picture(panel, "Sticky note", null, Ui.Middle, Ui.Middle, noteAt, new Vector2(40f, 16f), NoteYellow);
            m_Note = note.rectTransform;
            Ui.Picture(m_Note, "Strip", null, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(40f, 3f), NoteStrip);
            m_Note.localRotation = Quaternion.Euler(0f, 0f, -7f);
        }

        /// <summary>Stands the crew for <paramref name="r"/>'s debrief: kcaj with them if he's free, and the note off.</summary>
        public void Show(DebriefResult r)
        {
            m_Tones2Skin = Art.SkinFor(Content.Tones2);
            m_KcajSkin = Art.SkinFor(Content.Kcaj);
            m_Tones2.Rt.anchoredPosition = new Vector2(r.Kcaj ? Tones2X3 : Tones2X, Feet);
            m_Kie.Rt.anchoredPosition = new Vector2(r.Kcaj ? KieX3 : KieX, Feet);
            m_Kcaj.Rt.anchoredPosition = new Vector2(KcajX3, Feet);
            Ui.Show(m_Kcaj.Rt, r.Kcaj);
            m_Tones2.Stand(m_Tones2Skin);
            m_Kie.Stand(Art.SkinFor(Content.Kie));
            m_Kcaj.Stand(m_KcajSkin);
            m_Kcaj.Tint(r.TrackingUs ? Frosty : Color.white);
            Ui.Show(m_Note, false);
            m_Note.SetAsLastSibling(); // over the number
            m_NoteSounded = false;
        }

        /// <summary>Poses the crew for <paramref name="p"/>'s moment, and says whatever's said.</summary>
        public void Refresh(DebriefPlayer p)
        {
            string line = p.KieSays;
            StageFigure speaker = m_Kie;
            if (p.Result.TrackingUs) Skit(p, ref line, ref speaker);
            m_Bubble.Say(line);
            if (line != null)
            {
                float x = speaker.Rt.anchoredPosition.x;
                m_Bubble.Place(new Vector2(x, BubbleY), x);
            }
        }

        /// <summary>They're tracking us, <see cref="DebriefPlayer.SkitT"/> seconds in (its end, once the tally's done).</summary>
        void Skit(DebriefPlayer p, ref string line, ref StageFigure speaker)
        {
            float t = p.SkitT;
            if (p.SkitPlaying)
            {
                TrackingUs.Line? said = TrackingUs.Saying(t);
                line = said?.Text;
                speaker = said?.Who == Content.Kcaj ? m_Kcaj : m_Kie;
            }

            // kcaj leans in towards the accuracy, slowly, and stares; tones2 looks at the note, then back out at us
            float lean = TrackingUs.Lean(t);
            m_Kcaj.Pose(m_KcajSkin, new StagePose(StageLegs.Idle, StageTorso.Forward, lean * LeanTop, -lean));
            m_Kcaj.Rt.anchoredPosition = new Vector2(KcajX3 + lean * LeanStep, Feet);
            m_Tones2.Pose(m_Tones2Skin, new StagePose(StageLegs.Idle, StageTorso.Forward, TrackingUs.TonesLooks(t) ? 1f : 0f));

            bool on = TrackingUs.NoteOn(t);
            Ui.Show(m_Note, on);
            if (!on) return;
            float slap = Mathf.Lerp(SlapFrom, 1f, Mathf.Clamp01((t - TrackingUs.NoteAt) / SlapSeconds));
            m_Note.localScale = new Vector3(slap, slap, 1f);
            if (m_NoteSounded) return;
            m_NoteSounded = true;
            Sfx.Play(Sfx.Pickup, 0.6f); // the note: the same sound as the scenes' note cue (Cues.Note)
        }
    }
}
