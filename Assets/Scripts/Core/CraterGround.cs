using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike (<c>?spike=craters</c>, solo): ground that blasts can dig into, like Pooket Tabks' terrain, for a boss
    /// arena only. It's a solid/air mask at the art's resolution (16 px a unit) over a run of the level's columns,
    /// made from their tiles. Inside it, the <see cref="Level"/> answers solidity from the mask, and
    /// <see cref="TilePhysics"/> walks actors over its pixel surface: up gentle slopes, stopped by steep crater
    /// walls (jump out). Everywhere else stays tiles. A thin floor at the bottom never digs away.
    /// </summary>
    public sealed class CraterGround
    {
        public const int Px = Tuning.Pixels.PerUnit;
        /// <summary>Pixels at the bottom of the level that never dig away, so nothing falls out of the world.</summary>
        public const int Bedrock = 4;
        /// <summary>The steepest slope an actor walks up (rise over run): steeper is a wall, and takes a jump.</summary>
        public const float MaxClimb = 1.2f;

        /// <summary>The first column it covers, and the one after its last.</summary>
        public readonly int X0, X1;
        /// <summary>Its size in pixels.</summary>
        public readonly int W, H;
        readonly bool[] m_Solid;

        /// <summary>Pixels dug since the last <see cref="TakeDirty"/>, in mask pixels (empty when xMax &lt; xMin).</summary>
        int m_DirtyX0 = int.MaxValue, m_DirtyY0 = int.MaxValue, m_DirtyX1 = -1, m_DirtyY1 = -1;

        public CraterGround(Level level, int x0, int x1)
        {
            X0 = Mathf.Clamp(x0, 0, level.Width);
            X1 = Mathf.Clamp(x1, X0, level.Width);
            W = (X1 - X0) * Px;
            H = level.Height * Px;
            m_Solid = new bool[W * H];
            for (int tx = X0; tx < X1; tx++)
                for (int ty = 0; ty < level.Height; ty++)
                {
                    if (!level.TileSolid(tx, ty)) continue;
                    for (int j = 0; j < Px; j++)
                        for (int i = 0; i < Px; i++)
                            m_Solid[(ty * Px + j) * W + (tx - X0) * Px + i] = true;
                }
        }

        public bool Covers(float x) => x >= X0 && x < X1;
        public bool CoversColumn(int x) => x >= X0 && x < X1;

        /// <summary>Solid at mask pixel (px, py)? Off the sides it's the tiles' business; below it's solid.</summary>
        public bool SolidPx(int px, int py)
        {
            if (py < 0) return true;
            if (px < 0 || px >= W || py >= H) return false;
            return m_Solid[py * W + px];
        }

        public bool IsSolidAt(Vector2 p) => SolidPx(Mathf.FloorToInt((p.x - X0) * Px), Mathf.FloorToInt(p.y * Px));

        /// <summary>A whole tile counts as solid when its middle is (for the tile-based checks: ledges, walls, ceilings).</summary>
        public bool TileSolid(int x, int y) => SolidPx((x - X0) * Px + Px / 2, y * Px + Px / 2);

        /// <summary>
        /// The top of the highest ground at <paramref name="x"/> that's at or below <paramref name="fromY"/>, in units;
        /// 0 if there's none (the bottom of the world).
        /// </summary>
        public float SurfaceBelow(float x, float fromY)
        {
            int px = Mathf.FloorToInt((x - X0) * Px);
            int py = Mathf.Min(Mathf.FloorToInt(fromY * Px), H - 1);
            for (; py >= 0; py--)
                if (SolidPx(px, py)) return (py + 1) / (float)Px;
            return 0f;
        }

        /// <summary>Digs a round crater at <paramref name="at"/>, <paramref name="radius"/> units across. True if any ground went.</summary>
        public bool Dig(Vector2 at, float radius)
        {
            float cx = (at.x - X0) * Px, cy = at.y * Px, r = radius * Px;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + r));
            int y0 = Mathf.Max(Bedrock, Mathf.FloorToInt(cy - r)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + r));
            bool any = false;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    int i = y * W + x;
                    if (!m_Solid[i] || dx * dx + dy * dy > r * r) continue;
                    m_Solid[i] = false;
                    any = true;
                    m_DirtyX0 = Mathf.Min(m_DirtyX0, x);
                    m_DirtyY0 = Mathf.Min(m_DirtyY0, y);
                    m_DirtyX1 = Mathf.Max(m_DirtyX1, x);
                    m_DirtyY1 = Mathf.Max(m_DirtyY1, y);
                }
            return any;
        }

        /// <summary>The mask pixels dug since last asked (for the view to redraw), as a rect; false if none.</summary>
        public bool TakeDirty(out RectInt rect)
        {
            rect = default;
            if (m_DirtyX1 < m_DirtyX0) return false;
            rect = new RectInt(m_DirtyX0, m_DirtyY0, m_DirtyX1 - m_DirtyX0 + 1, m_DirtyY1 - m_DirtyY0 + 1);
            m_DirtyX0 = m_DirtyY0 = int.MaxValue;
            m_DirtyX1 = m_DirtyY1 = -1;
            return true;
        }
    }
}
