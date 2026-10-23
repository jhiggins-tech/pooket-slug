using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The bunker's rope, drawn over its painting (<see cref="RoomMap.ByRope"/>, <see cref="RoomMap.RopeX"/>): the plate
    /// has the vault door at the way's end but no rope, and this is what the crew climb in and out by. It hangs from the
    /// hole in the roof down to the floor, with knots down it and a coil at its foot, and it's hidden in every other room.
    /// </summary>
    sealed class RoomRope
    {
        /// <summary>The rope's width, in art pixels.</summary>
        const float W = 1.5f;
        /// <summary>Tarred hemp, and a knot's darker twist (the bunker's lamp is warm, so the rope reads against the dark).</summary>
        static readonly Color Hemp = new Color(0.76f, 0.65f, 0.44f), Knot = new Color(0.56f, 0.46f, 0.29f);
        /// <summary>Where the knots are up the rope, in art pixels from the floor.</summary>
        static readonly float[] Knots = { 38f, 74f, 110f, 146f };

        /// <summary>The rope's parts, shown and hidden together.</summary>
        public readonly RectTransform Rt;

        public RoomRope(RectTransform world, RoomId room)
        {
            // a container at the room's bottom left: its parts are placed in the room's own art pixels
            Rt = Ui.Rect(world, "Rope", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            float top = RoomMap.RopeTop(room);
            Picture(Rt, "Line", RoomMap.RopeX, RoomMap.FloorY + 1f, W, top - RoomMap.FloorY - 1f, Hemp);
            foreach (float y in Knots) Picture(Rt, "Knot", RoomMap.RopeX, y, W + 1.5f, 2f, Knot);
            Picture(Rt, "Coil", RoomMap.RopeX, RoomMap.FloorY + 0.5f, 3.5f, 1.5f, Hemp); // the loop you step into
            Ui.Show(Rt, false);
        }

        /// <summary>Shows it, so it's over the painting and under the soldier.</summary>
        public void Draw()
        {
            Ui.Show(Rt, true);
            Rt.SetSiblingIndex(1); // the painting's first, then the rope, then you
        }

        public void Hide() => Ui.Show(Rt, false);
    }
}
