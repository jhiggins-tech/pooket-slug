using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The den's roller door into the tabk bay, drawn over the den's painting as it rolls (<see cref="RollerDoor"/>, in
    /// Core): its slats, cut from the painting, slide up into the frame and show the bay's far end behind them, cut from
    /// the bay's painting. Shut, it's the painting as it is.
    /// </summary>
    sealed class DenRollerDoor
    {
        /// <summary>The door's opening in the den's painting (its left edge, bottom, width and height, in art pixels).</summary>
        public const float X = 261f, Y = 29f, W = 36f, H = 49f;

        /// <summary>Where the opening's bottom middle is: where you stand to step through it.</summary>
        public static Vector2 Sill => new Vector2(X + W / 2f, Y);

        readonly RectTransform m_Mask;
        readonly Image m_Slats;

        public DenRollerDoor(RectTransform parent)
        {
            m_Mask = Ui.Rect(parent, "Roller door", Vector2.zero, Vector2.zero, new Vector2(X, Y) * Px, new Vector2(W, H) * Px);
            m_Mask.gameObject.AddComponent<RectMask2D>();
            Sprite den = DenRoom.Painting("den"), bay = DenRoom.Painting("bay");
            // beyond it, the bay's far end by the stands, a little darker than the bay itself
            Ui.Picture(m_Mask, "Beyond", Cut(bay, 24f, 8f), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(W, H) * Px, new Color(0.75f, 0.75f, 0.8f)).raycastTarget = false;
            m_Slats = Ui.Picture(m_Mask, "Slats", Cut(den, X, Y), Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(W, H) * Px, Color.white);
            m_Slats.raycastTarget = false;
            Ui.Show(m_Mask, false);
        }

        /// <summary>The door's size cut from <paramref name="painting"/> at <paramref name="x"/>, <paramref name="y"/> (or nothing, a plain colour, if it's missing).</summary>
        static Sprite Cut(Sprite painting, float x, float y) =>
            painting == null ? null : Sprite.Create(painting.texture, new Rect(x, y, W, H), Vector2.zero, 1f);

        /// <summary>Keeps it over the paintings, which are remade when the crew changes.</summary>
        public void OnTop() => m_Mask.SetAsLastSibling();

        /// <summary>Rolled up <paramref name="lift"/> of the way (0 shut: the painting's own door shows).</summary>
        public void Draw(float lift)
        {
            Ui.Show(m_Mask, lift > 0f);
            float eased = lift * lift * (3f - 2f * lift);
            m_Slats.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Round(eased * (H - 3f))) * Px; // a lip stays in the frame
        }
    }
}
