using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// A room's door, drawn over the room's painting as it swings (<see cref="SwingDoor"/>, in Core): the dark beyond
    /// it, and the leaf, hinged on the frame's outer edge. The soldier stepping through goes *into* the opening: they
    /// are drawn inside the frame, which is masked, so the frame's edge cuts them off as they go through instead of
    /// them wandering off to the side of the screen.
    /// </summary>
    sealed class RoomDoor
    {
        /// <summary>The doorway's opening in the room (its left edge, bottom, width and height, in art pixels).</summary>
        public readonly Opening Opening;
        /// <summary>The frame: the doorway itself. It cuts off whoever is inside it.</summary>
        public readonly RectTransform Frame;
        readonly RectTransform m_Leaf;

        /// <summary>The yard's night beyond the door, matching the plate's.</summary>
        static readonly Color Night = new Color(0.04f, 0.07f, 0.12f);

        /// <summary>How far it's swung open: 0 shut, 1 all the way (the flow's <see cref="SwingDoor"/> sets it).</summary>
        public float Swing { get; set; }

        public RoomDoor(RectTransform parent, Opening opening)
        {
            Opening = opening;
            Frame = Ui.Rect(parent, "Door", Vector2.zero, Vector2.zero, new Vector2(opening.X, opening.Y) * Px, new Vector2(opening.W, opening.H) * Px);
            Frame.gameObject.AddComponent<RectMask2D>();
            // beyond it: the night, matching the plate's
            Ui.Picture(Frame, "Beyond", null, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(opening.W, opening.H) * Px, Night).raycastTarget = false;
            // the leaf, hinged on the frame's outer edge: its pivot is the hinge, and it swings out of the frame
            Vector2 pivot = opening.ToRight ? new Vector2(1f, 0f) : Vector2.zero;
            Vector2 at = opening.ToRight ? new Vector2(opening.W, 0f) : Vector2.zero;
            m_Leaf = Ui.Rect(Frame, "Leaf", Vector2.zero, pivot, at * Px, new Vector2(opening.W, opening.H) * Px);
            Panel(m_Leaf, new Vector2(0f, 0f), new Vector2(opening.W, 3f), new Color(0.32f, 0.21f, 0.12f)); // the top rail
            Panel(m_Leaf, new Vector2(0f, opening.H - 3f), new Vector2(opening.W, 3f), new Color(0.32f, 0.21f, 0.12f)); // the bottom rail
            Panel(m_Leaf, new Vector2(0f, 3f), new Vector2(opening.W, opening.H - 6f), new Color(0.42f, 0.28f, 0.16f)); // the panel
            Panel(m_Leaf, new Vector2(2f, opening.H * 0.42f), new Vector2(opening.W - 4f, 8f), new Color(0.3f, 0.19f, 0.11f)); // the middle rail
            Panel(m_Leaf, new Vector2(1.5f, opening.H * 0.42f + 3f), new Vector2(3f, 3f), Palette.Gold); // the knob
            Ui.Show(Frame, false);
        }

        static Image Panel(RectTransform parent, Vector2 at, Vector2 size, Color color) =>
            Ui.Picture(parent, "Panel", null, Vector2.zero, Vector2.zero, at * Px, size * Px, color);

        /// <summary>Keeps it over the painting, which is remade when the room opens.</summary>
        public void OnTop() => Frame.SetAsLastSibling();

        /// <summary>Draws it. The swing is <see cref="Swing"/>, set by the view each frame before this.</summary>
        public void Draw()
        {
            float swing = Swing;
            Ui.Show(Frame, swing > 0f);
            float eased = swing * swing * (3f - 2f * swing); // the leaf swings out of the frame, flat to us at 1
            m_Leaf.localScale = new Vector3(Mathf.Max(0.05f, 1f - eased), 1f, 1f);
        }

        /// <summary>
        /// Puts the soldier in the doorway: inside the frame, which is masked, so the frame's edge cuts them off as
        /// they go through. The leaf is over them, so it swings shut in front of them.
        /// </summary>
        public void Step(RectTransform figure, float x, float y)
        {
            figure.SetParent(Frame, false);
            figure.anchoredPosition = new Vector2(x - Opening.X, y - Opening.Y) * Px;
            figure.SetSiblingIndex(1); // over the dark beyond, under the leaf
        }
    }
}
