using UnityEngine;

namespace PooketSlug
{
    /// <summary>Draws a <see cref="Level"/>: the tile map is baked into a few wide chunk textures.</summary>
    public static class LevelView
    {
        const int Chunk = 32;

        /// <param name="skipFrom">Columns from here on are drawn by someone else (the craters spike's arena).</param>
        public static GameObject Create(Level level, Transform parent, int skipFrom = int.MaxValue)
        {
            var root = new GameObject("LevelView");
            root.transform.SetParent(parent, false);
            for (int cx = 0; cx < level.Width; cx += Chunk)
            {
                int w = Mathf.Min(Chunk, level.Width - cx);
                var canvas = new PixelCanvas(w * 16, level.Height * 16);
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < level.Height; y++)
                    {
                        char t = level.Get(cx + x, y);
                        if (t == Level.Empty || cx + x >= skipFrom) continue;
                        bool exposed = y + 1 >= level.Height || !level.TileSolid(cx + x, y + 1);
                        if (level.Belt(cx + x, y) != 0) CreameryArt.DrawBelt(canvas, x * 16, y * 16); // its tread moves: BeltTread
                        else Art.DrawTile(canvas, x * 16, y * 16, t, exposed, (cx + x) * 31 + y * 17);
                    }
                var go = new GameObject("Chunk" + cx / Chunk);
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(cx, 0, 0);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Art.ToSprite(canvas, Vector2.zero);
                sr.sortingOrder = SortOrder.Tiles;
            }
            return root;
        }
    }
}
