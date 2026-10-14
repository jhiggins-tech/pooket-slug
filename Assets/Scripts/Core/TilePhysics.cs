using UnityEngine;

namespace PooketSlug
{
    public struct MoveResult
    {
        public bool Grounded, HitWall, HitCeiling;
    }

    /// <summary>Axis-separated AABB vs tile-grid movement with one-way bridges.</summary>
    public static class TilePhysics
    {
        const float Eps = 0.001f;

        public static MoveResult Move(Level level, ref Vector2 feet, float halfW, float height, ref Vector2 vel, float dt, bool dropThrough)
        {
            var result = new MoveResult();

            // --- X ---
            float nx = feet.x + vel.x * dt;
            int y0 = Mathf.FloorToInt(feet.y + Eps), y1 = Mathf.FloorToInt(feet.y + height - Eps);
            if (vel.x > 0f)
            {
                int col = Mathf.FloorToInt(nx + halfW - Eps);
                for (int y = y0; y <= y1; y++)
                    if (level.IsSolid(col, y))
                    {
                        nx = col - halfW;
                        result.HitWall = true;
                        break;
                    }
            }
            else if (vel.x < 0f)
            {
                int col = Mathf.FloorToInt(nx - halfW + Eps);
                for (int y = y0; y <= y1; y++)
                    if (level.IsSolid(col, y))
                    {
                        nx = col + 1 + halfW;
                        result.HitWall = true;
                        break;
                    }
            }
            if (result.HitWall) vel.x = 0f;
            feet.x = nx;

            // --- Y ---
            float ny = feet.y + vel.y * dt;
            int c0 = Mathf.FloorToInt(feet.x - halfW + Eps), c1 = Mathf.FloorToInt(feet.x + halfW - Eps);
            if (vel.y <= 0f)
            {
                int row = Mathf.FloorToInt(ny);
                for (int c = c0; c <= c1; c++)
                {
                    char t = level.Get(c, row);
                    bool solid = t == Level.Ground || t == Level.Block;
                    bool bridge = t == Level.Bridge && !dropThrough && feet.y >= row + 1 - 0.05f;
                    if (solid || bridge)
                    {
                        ny = row + 1;
                        result.Grounded = true;
                        break;
                    }
                }
                if (result.Grounded) vel.y = 0f;
            }
            else
            {
                int row = Mathf.FloorToInt(ny + height - Eps);
                for (int c = c0; c <= c1; c++)
                    if (level.IsSolid(c, row))
                    {
                        ny = row - height;
                        vel.y = 0f;
                        result.HitCeiling = true;
                        break;
                    }
            }
            feet.y = ny;
            return result;
        }

        /// <summary>True if the actor stands on a one-way bridge (so it may drop through).</summary>
        public static bool OnBridge(Level level, Vector2 feet, float halfW)
        {
            int row = Mathf.FloorToInt(feet.y - 0.05f);
            int c0 = Mathf.FloorToInt(feet.x - halfW + Eps), c1 = Mathf.FloorToInt(feet.x + halfW - Eps);
            bool any = false;
            for (int c = c0; c <= c1; c++)
            {
                char t = level.Get(c, row);
                if (t == Level.Ground || t == Level.Block) return false;
                any |= t == Level.Bridge;
            }
            return any;
        }
    }
}
