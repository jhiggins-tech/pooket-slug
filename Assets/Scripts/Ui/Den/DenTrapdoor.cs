using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The trapdoor down to the basement, in the den's ground floor where the mission board used to hang
    /// (<c>docs/design/lobby.md</c>, The outer rooms). The den's painting has no trapdoor in it, so it's drawn here over
    /// the floor: a hatch in the boards, with the way down beyond it.
    /// </summary>
    sealed class DenTrapdoor
    {
        /// <summary>The hatch's opening in the den (its left edge, bottom, width and height, in art pixels).</summary>
        public const float X = 224f, Y = 0f, W = 22f, H = 15f;

        /// <summary>Where the hatch's middle is: where you stand to go down it.</summary>
        public static Vector2 Middle => new Vector2(X + W / 2f, Y + H / 2f);

        readonly RectTransform m_Hatch;

        public DenTrapdoor(RectTransform parent)
        {
            m_Hatch = Ui.Rect(parent, "Trapdoor", Vector2.zero, Vector2.zero, new Vector2(X, Y) * Px, new Vector2(W, H) * Px);
            // the boards of the hatch, and a dark hole under them
            Ui.Picture(m_Hatch, "Hole", null, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(W, H) * Px, new Color(0.06f, 0.05f, 0.08f));
            Ui.Picture(m_Hatch, "Hatch", null, Vector2.zero, Vector2.zero, new Vector2(0f, H - 3f) * Px, new Vector2(W, 3f) * Px, new Color(0.34f, 0.24f, 0.16f));
            Ui.Picture(m_Hatch, "Lip", null, Vector2.zero, Vector2.zero, new Vector2(0f, 0f) * Px, new Vector2(W, 1f) * Px, new Color(0.2f, 0.14f, 0.1f));
        }

        /// <summary>Keeps it over the paintings, which are remade when the crew changes.</summary>
        public void OnTop() => m_Hatch.SetAsLastSibling();
    }
}
