using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike (<c>?spike=craters</c>): draws the diggable boss arena (<see cref="CraterGround"/>). The arena's columns
    /// are baked like <see cref="LevelView"/>'s chunks but into a texture we keep, and each blast clears the pixels it
    /// dug and scorches the fresh crater edge.
    /// </summary>
    public sealed class CraterView
    {
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        readonly CraterGround m_Ground;
        readonly Texture2D m_Tex;
        readonly Color32[] m_Px;
        readonly bool[] m_Scorched;

        public CraterView(Level level, CraterGround ground, Transform parent)
        {
            m_Ground = ground;
            var canvas = new PixelCanvas(ground.W, ground.H);
            for (int x = ground.X0; x < ground.X1; x++)
                for (int y = 0; y < level.Height; y++)
                {
                    char t = level.Get(x, y);
                    if (t == Level.Empty) continue;
                    bool exposed = y + 1 >= level.Height || !level.TileSolid(x, y + 1);
                    Art.DrawTile(canvas, (x - ground.X0) * 16, y * 16, t, exposed, x * 31 + y * 17);
                }
            m_Px = canvas.Px;
            m_Scorched = new bool[m_Px.Length];
            m_Tex = new Texture2D(ground.W, ground.H, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            m_Tex.SetPixels32(m_Px);
            m_Tex.Apply(false, false); // kept readable: it's redrawn as it's dug

            Sprite sprite = Sprite.Create(m_Tex, new Rect(0, 0, ground.W, ground.H), Vector2.zero, Art.PPU, 0, SpriteMeshType.FullRect);
            Art.Renderer(parent, "CraterGround", sprite, SortOrder.Tiles).transform.position = new Vector3(ground.X0, 0, 0);
        }

        /// <summary>Redraws whatever's been dug since last time.</summary>
        public void Refresh()
        {
            if (!m_Ground.TakeDirty(out RectInt r)) return;
            int x0 = Mathf.Max(0, r.xMin - 2), x1 = Mathf.Min(m_Ground.W - 1, r.xMax + 1);
            int y0 = Mathf.Max(0, r.yMin - 2), y1 = Mathf.Min(m_Ground.H - 1, r.yMax + 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    int i = y * m_Ground.W + x;
                    if (!m_Ground.SolidPx(x, y)) { m_Px[i] = Clear; continue; }
                    if (m_Scorched[i] || m_Px[i].a == 0) continue;
                    // a burnt rim two pixels deep around the hole
                    bool rim = false;
                    for (int dy = -2; dy <= 2 && !rim; dy++)
                        for (int dx = -2; dx <= 2 && !rim; dx++)
                            if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 2 && !m_Ground.SolidPx(x + dx, y + dy) && y + dy < m_Ground.H && x + dx >= 0 && x + dx < m_Ground.W)
                                rim = true;
                    if (!rim) continue;
                    Color32 c = m_Px[i];
                    m_Px[i] = new Color32((byte)(c.r * 0.55f), (byte)(c.g * 0.5f), (byte)(c.b * 0.48f), c.a);
                    m_Scorched[i] = true;
                }
            m_Tex.SetPixels32(m_Px);
            m_Tex.Apply(false, false);
        }
    }
}
