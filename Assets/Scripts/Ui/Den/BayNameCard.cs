using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// A tabk's name card dropping in from the top of the bay's screen as it comes home (its crewmate's tabk in gold, its
    /// own name under it): kie's (<see cref="BayHomecomingView"/>) and kcaj's (<see cref="BayKcajHomeView"/>).
    /// </summary>
    sealed class BayNameCard
    {
        readonly RectTransform m_Card;

        /// <param name="stage">The stage over the bay's world (the card doesn't scroll with it).</param>
        public BayNameCard(RectTransform stage, string name, string title)
        {
            m_Card = Ui.Rect(stage, "Name card", Ui.TopCenter, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(300f, 40f));
            var band = m_Card.gameObject.AddComponent<Image>();
            band.color = Band;
            band.raycastTarget = false;
            Ui.Label(m_Card, "Name", Ui.Middle, new Vector2(0f, 7f), 16, TextAnchor.MiddleCenter, Gold).text = name;
            Ui.Label(m_Card, "Title", Ui.Middle, new Vector2(0f, -10f), 10, TextAnchor.MiddleCenter, Color.white).text = title;
            Show(0f);
        }

        /// <summary>The card <paramref name="drop"/> of the way in (0 above the screen and hidden, 1 in place), eased.</summary>
        public void Show(float drop)
        {
            Ui.Show(m_Card, drop > 0f);
            float k = drop * drop * (3f - 2f * drop);
            m_Card.anchoredPosition = new Vector2(0f, Mathf.Lerp(44f, -40f, k));
        }
    }
}
