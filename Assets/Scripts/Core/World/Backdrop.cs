using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// One layer of a mission's parallax backdrop: its name (and so its plates' files), how far away it is and where it
    /// stands. <see cref="Factor"/> is how much of the camera's movement the layer keeps (1 is infinitely far away: it
    /// never moves on screen; 0 moves with the ground).
    /// </summary>
    public sealed class BackdropLayer
    {
        public readonly string Name;
        public readonly float Factor;
        /// <summary>Where its bottom edge stands, in world units above the bottom of the screen.</summary>
        public readonly float Y;

        public BackdropLayer(string name, float factor, float y)
        {
            Name = name;
            Factor = factor;
            Y = y;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// A mission's backdrop, painted (<c>docs/design/art.md</c>, Background plates): its parallax layers, back to front,
    /// each a strip of plates laid end to end and repeated, so plates can change along the map. A plate is a PNG at
    /// 16 pixels a unit, kept in the build as <c>Resources/Plates/&lt;mission id&gt;/&lt;layer&gt;-&lt;n&gt;.bytes</c>
    /// (<see cref="PlatePath"/>), numbered from 0 along the strip. A layer with no plates is drawn in code, as before.
    /// The depths are the gen-plates spike's (17 Oct).
    /// </summary>
    public static class Backdrop
    {
        /// <summary>The sky, the far skyline and the base's middle distance, back to front.</summary>
        public static readonly IReadOnlyList<BackdropLayer> Layers = new[]
        {
            new BackdropLayer("sky", 0.95f, 0f),
            new BackdropLayer("far", 0.85f, 1.2f),
            new BackdropLayer("mid", 0.6f, 1.0f),
        };

        /// <summary>
        /// <paramref name="mission"/>'s layers: the shared ones, or, for a mission whose plates are cut at its floor's
        /// top (<see cref="MissionDef.PlatesStand"/>), the same depths with the far and middle plates standing there.
        /// </summary>
        public static IReadOnlyList<BackdropLayer> LayersFor(MissionDef mission)
        {
            if (mission == null || mission.PlatesStand <= 0f) return Layers;
            var layers = new BackdropLayer[Layers.Count];
            for (int i = 0; i < layers.Length; i++)
            {
                BackdropLayer l = Layers[i];
                layers[i] = l.Name == "sky" ? l : new BackdropLayer(l.Name, l.Factor, mission.PlatesStand);
            }
            return layers;
        }

        /// <summary>The most plates a layer's strip can have.</summary>
        public const int MostPlates = 32;

        /// <summary>Plate <paramref name="n"/> of <paramref name="layer"/> in <paramref name="mission"/>'s backdrop, as a path under Resources.</summary>
        public static string PlatePath(MissionDef mission, BackdropLayer layer, int n) => "Plates/" + mission.Id + "/" + layer.Name + "-" + n;

        /// <summary>
        /// The plates that cover the screen: for a camera at <paramref name="camX"/> showing <paramref name="halfWidth"/>
        /// either side, a layer at <paramref name="factor"/> whose strip is plates of <paramref name="widths"/> (world
        /// units) laid end to end from the layer's origin and repeated both ways. Fills <paramref name="into"/> left to
        /// right with each plate's index in the strip and its left edge, and returns how many it filled (no more than
        /// <paramref name="into"/> holds).
        /// </summary>
        public static int Cover(float camX, float halfWidth, float factor, IReadOnlyList<float> widths, (int plate, float x)[] into)
        {
            int n = widths.Count;
            if (n == 0 || into.Length == 0) return 0;
            float strip = 0f;
            for (int i = 0; i < n; i++) strip += Mathf.Max(widths[i], 0.01f);
            float origin = camX * factor, left = camX - halfWidth, right = camX + halfWidth;
            float x = origin + Mathf.Floor((left - origin) / strip) * strip;
            int plate = 0, count = 0;
            while (x + Mathf.Max(widths[plate], 0.01f) <= left) // to the plate the screen's left edge is on
            {
                x += Mathf.Max(widths[plate], 0.01f);
                plate = (plate + 1) % n;
            }
            while (x < right && count < into.Length)
            {
                into[count++] = (plate, x);
                x += Mathf.Max(widths[plate], 0.01f);
                plate = (plate + 1) % n;
            }
            return count;
        }
    }
}
