using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The kitchen's back door, drawn over the kitchen's painting as it swings to the yard (<see cref="SwingDoor"/>, in
    /// Core). The painting has a door frame here, showing the night beyond, so this draws only the leaf: a code-drawn
    /// panel hinged on the frame's outer edge, swinging out of the frame. The soldier stepping in is tinted and shrunk
    /// by the view (<see cref="RoomView.DoorBeat"/>), as the den's crew is in its roller door. Shut, it's the painting
    /// as it is.
    /// </summary>
    sealed class RoomBackDoor
    {
        /// <summary>The door's opening in the kitchen (its left edge, bottom, width and height, in art pixels): the
        /// frame the painting has at its right end, over the 22 px soldier, showing the night beyond it.</summary>
        public const float X = 389.5f, Y = 16f, W = 25f, H = 50f;

        /// <summary>Where the opening's middle is: where you stand to step through it.</summary>
        public static Vector2 Sill => new Vector2(X + W / 2f, RoomMap.FloorY);

        /// <summary>Where the step in starts, and where it ends: a soldier's width before the frame, and one past its far edge (a shoulder's either side of the threshold, like the den's roller door).</summary>
        public const float From = W / 2f - 4f, To = -W / 2f - 4f;

        /// <summary>The yard's night beyond the door, matching the plate's.</summary>
        static readonly Color Night = new Color(0.04f, 0.07f, 0.12f);

        public readonly RectTransform Opening, Leaf;

        /// <summary>How far it's swung open: 0 shut, 1 all the way (the flow's <see cref="SwingDoor"/> sets it).</summary>
        public float Swing { get; set; }

        public RoomBackDoor(RectTransform parent)
        {
            Opening = Ui.Rect(parent, "Back door", Vector2.zero, Vector2.zero, new Vector2(X, Y) * Px, new Vector2(W, H) * Px);
            Opening.gameObject.AddComponent<RectMask2D>();
            // beyond it: the yard's night, matching the plate's
            Ui.Picture(Opening, "Beyond", null, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(W, H) * Px, Night).raycastTarget = false;
            // the leaf, hinged on the frame's outer (right) edge: its pivot is the hinge, and it swings out of the frame
            Leaf = Ui.Rect(Opening, "Leaf", Vector2.zero, new Vector2(1f, 0f), new Vector2(W, 0f) * Px, new Vector2(W, H) * Px);
            Panel(Leaf, new Vector2(0f, 0f), new Vector2(W, 3f), new Color(0.32f, 0.21f, 0.12f)); // the top rail
            Panel(Leaf, new Vector2(0f, H - 3f), new Vector2(W, 3f), new Color(0.32f, 0.21f, 0.12f)); // the bottom rail
            Panel(Leaf, new Vector2(0f, 3f), new Vector2(W, H - 6f), new Color(0.42f, 0.28f, 0.16f)); // the panel
            Panel(Leaf, new Vector2(2f, H * 0.42f), new Vector2(W - 4f, 8f), new Color(0.3f, 0.19f, 0.11f)); // the middle rail
            Panel(Leaf, new Vector2(1.5f, H * 0.42f + 3f), new Vector2(3f, 3f), Palette.Gold); // the knob
            Ui.Show(Opening, false);
        }

        static Image Panel(RectTransform parent, Vector2 at, Vector2 size, Color color) =>
            Ui.Picture(parent, "Panel", null, Vector2.zero, Vector2.zero, at * Px, size * Px, color);

        /// <summary>Keeps it over the painting and the soldier, which are remade when the room opens.</summary>
        public void OnTop() => Opening.SetAsLastSibling();

        /// <summary>Draws it. The swing is <see cref="Swing"/>, set by the view each frame before this.</summary>
        public void Draw()
        {
            float swing = Swing;
            Ui.Show(Opening, swing > 0f);
            float eased = swing * swing * (3f - 2f * swing); // the leaf swings out of the frame, flat to us at 1
            Leaf.localScale = new Vector3(Mathf.Max(0.05f, 1f - eased), 1f, 1f);
        }
    }
}
