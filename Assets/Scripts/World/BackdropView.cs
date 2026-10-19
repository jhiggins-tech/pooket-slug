using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// The parallax backdrop on screen (<see cref="Backdrop"/>): behind a sky gradient, each layer is the mission's
    /// painted plates where it has them (<c>Resources/Plates/&lt;mission id&gt;/</c>), laid end to end and placed each frame
    /// by <see cref="Backdrop.Cover"/>; a layer without plates is drawn in code (the far mountains and the ruins).
    /// <see cref="ScrollCamera"/> moves it.
    /// </summary>
    public sealed class BackdropView
    {
        sealed class Layer
        {
            public BackdropLayer Def;
            public Sprite[] Plates;
            public float[] Widths;
            public (int plate, float x)[] Cover;
            public SpriteRenderer[] Tiles;
        }

        readonly World m_World;
        readonly Transform m_Sky;
        readonly List<Layer> m_Layers = new List<Layer>();

        static readonly Dictionary<string, Sprite> s_Plates = new Dictionary<string, Sprite>();

        public BackdropView(World world, MissionDef mission)
        {
            m_World = world;
            m_Sky = Art.Renderer(world.Root, "Sky", Art.Sky, SortOrder.Sky).transform;
            int painted = 0;
            IReadOnlyList<BackdropLayer> layers = Backdrop.LayersFor(mission);
            for (int i = 0; i < layers.Count; i++)
            {
                BackdropLayer def = layers[i];
                Sprite[] plates = Load(mission, def);
                if (plates.Length > 0) painted++;
                else plates = Drawn(def); // nothing painted for it: the drawn layer, if there is one
                if (plates.Length == 0) continue;
                var layer = new Layer { Def = def, Plates = plates, Widths = new float[plates.Length] };
                float narrowest = float.MaxValue;
                for (int p = 0; p < plates.Length; p++)
                {
                    layer.Widths[p] = plates[p].rect.width / plates[p].pixelsPerUnit;
                    narrowest = Mathf.Min(narrowest, layer.Widths[p]);
                }
                // enough tiles to cross the widest screen (a co-op view is wider than a phone's) on the narrowest plates
                int tiles = Mathf.CeilToInt(2f * Tuning.Camera.ViewHeight * 1.2f / Mathf.Max(narrowest, 0.5f)) + 2;
                layer.Cover = new (int, float)[tiles];
                layer.Tiles = new SpriteRenderer[tiles];
                int order = SortOrder.Sky + 5 + 10 * i;
                for (int t = 0; t < tiles; t++) layer.Tiles[t] = Art.Renderer(world.Root, def.Name + t, plates[0], order, shown: false);
                m_Layers.Add(layer);
            }
            if (painted > 0) Log.Game.Info("Backdrop: " + painted + " painted layer(s) for " + mission.Id);
        }

        /// <summary>Places the sky and every layer for a camera at (<paramref name="cx"/>, <paramref name="cy"/>) showing <paramref name="halfWidth"/> either side.</summary>
        public void Place(float cx, float cy, float halfWidth)
        {
            m_Sky.position = new Vector3(cx, cy, 0f);
            m_Sky.localScale = new Vector3((halfWidth * 2f + 2f) * Art.PPU, (Tuning.Camera.ViewHeight + 2f) / 2f, 1f);
            foreach (Layer l in m_Layers)
            {
                int n = Backdrop.Cover(cx, halfWidth + 0.5f, l.Def.Factor, l.Widths, l.Cover);
                for (int t = 0; t < l.Tiles.Length; t++)
                {
                    SpriteRenderer sr = l.Tiles[t];
                    bool shown = t < n;
                    if (sr.enabled != shown) sr.enabled = shown;
                    if (!shown) continue;
                    (int plate, float x) = l.Cover[t];
                    if (sr.sprite != l.Plates[plate]) sr.sprite = l.Plates[plate];
                    sr.transform.position = new Vector3(Entity.Snap(x), l.Def.Y, 0f);
                }
            }
        }

        /// <summary>Whether <paramref name="mission"/>'s <paramref name="layer"/> layer is painted (it has plates in the build).</summary>
        public static bool Painted(MissionDef mission, string layer)
        {
            foreach (BackdropLayer l in Backdrop.LayersFor(mission))
                if (l.Name == layer) return Plate(Backdrop.PlatePath(mission, l, 0)) != null;
            return false;
        }

        /// <summary>The layer's painted plates for this mission, in strip order (none when it has none).</summary>
        static Sprite[] Load(MissionDef mission, BackdropLayer layer)
        {
            var plates = new List<Sprite>();
            for (int n = 0; n < Backdrop.MostPlates; n++)
            {
                Sprite s = Plate(Backdrop.PlatePath(mission, layer, n));
                if (s == null) break;
                plates.Add(s);
            }
            return plates.ToArray();
        }

        /// <summary>A plate from Resources (a PNG kept as .bytes, so Unity leaves its pixels alone): game pixels, 16 to a unit, pivot at the bottom left. Null if it isn't there.</summary>
        static Sprite Plate(string path)
        {
            if (s_Plates.TryGetValue(path, out Sprite cached)) return cached;
            var file = Resources.Load<TextAsset>(path);
            Sprite sprite = null;
            if (file != null)
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                if (tex.LoadImage(file.bytes)) sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, Art.PPU, 0, SpriteMeshType.FullRect);
                else Log.Game.Warn("Backdrop: " + path + " isn't an image");
            }
            return s_Plates[path] = sprite;
        }

        /// <summary>The layer drawn in code, for a mission without its plates: the far mountains and the ruins (the gradient behind is the sky).</summary>
        static Sprite[] Drawn(BackdropLayer layer)
        {
            switch (layer.Name)
            {
                case "far": return new[] { Art.Mountains };
                case "mid": return new[] { Art.Ruins };
                default: return new Sprite[0];
            }
        }
    }
}
