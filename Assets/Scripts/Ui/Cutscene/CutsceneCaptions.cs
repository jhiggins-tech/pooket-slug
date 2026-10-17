using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The cutscenes' words that aren't in a bubble: the caption card (slammed up over the middle of the stage), and the
    /// narrator, read aloud with a subtitle in a band along the bottom.
    /// </summary>
    public sealed class CutsceneCaptions
    {
        readonly RectTransform m_Stage;
        RectTransform m_CardBand, m_SubBand;
        Text m_CardText, m_Subtitle;
        string m_LastSaid;

        public CutsceneCaptions(RectTransform stage)
        {
            m_Stage = stage;
        }

        /// <summary>The caption card (made in drawing order, by <see cref="CutsceneView"/>).</summary>
        public void MakeCard()
        {
            m_CardBand = Ui.Rect(m_Stage, "Caption card", Ui.Middle, Ui.Middle, new Vector2(0f, 10f), new Vector2(StageW * Px + 20f, 76f));
            var band = m_CardBand.gameObject.AddComponent<Image>();
            band.color = Band;
            band.raycastTarget = false; // a tap on the card still moves on
            m_CardText = Ui.Label(m_CardBand, "Text", Ui.Middle, Vector2.zero, 22, TextAnchor.MiddleCenter, Gold);
            m_CardText.lineSpacing = 1.1f;
        }

        /// <summary>The narrator's subtitles, in a band along the bottom (made in drawing order, by <see cref="CutsceneView"/>).</summary>
        public void MakeSubtitles()
        {
            m_SubBand = Ui.Rect(m_Stage, "Subtitles", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(StageW * Px - 40f, 26f));
            var sub = m_SubBand.gameObject.AddComponent<Image>();
            sub.color = new Color(0f, 0f, 0f, 0.62f);
            sub.raycastTarget = false;
            m_Subtitle = Ui.Label(m_SubBand, "Narrator", Ui.Middle, Vector2.zero, 13, TextAnchor.MiddleCenter, Paper);
            m_Subtitle.fontStyle = FontStyle.BoldAndItalic;
        }

        /// <summary>A new scene: nothing read yet.</summary>
        public void Begin() => m_LastSaid = null;

        /// <summary>The caption card says <paramref name="line"/>, slamming up as it comes in (<paramref name="t"/> is the shot's time).</summary>
        public void Card(CutsceneLine line, float t)
        {
            m_CardText.text = line.Text;
            float since = t - line.At;
            float scale = since < 0.12f ? Mathf.Lerp(1.6f, 1f, since / 0.12f) : 1f;
            m_CardBand.localScale = new Vector3(1f, scale, 1f);
            m_CardText.rectTransform.localScale = new Vector3(scale, 1f, 1f);
        }

        /// <summary>Shows the caption card this frame or not, and the narrator's line, if they're reading one.</summary>
        public void Show(bool card, CutscenePlayer p)
        {
            bool narrating = p.Narrating(out CutsceneLine narration);
            Ui.Show(m_SubBand, narrating);
            if (narrating) m_Subtitle.text = narration.Text;
            Voice(narrating ? narration.Voice : null);
            Ui.Show(m_CardBand, card);
        }

        /// <summary>The narrator reads each of their lines as it comes up, with its subtitle.</summary>
        void Voice(string line)
        {
            if (line == m_LastSaid) return;
            m_LastSaid = line;
            if (line != null) Sfx.Narrate(line);
        }
    }
}
