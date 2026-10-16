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
            if (level.Craters != null && level.Craters.Covers(feet.x)) return MoveOnCraters(level, level.Craters, ref feet, halfW, height, ref vel, dt);
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

        /// <summary>
        /// Movement over dug ground (spike: <see cref="CraterGround"/>). The feet ride the pixel surface under the
        /// actor's middle and both its edges. Walking climbs a slope up to <see cref="CraterGround.MaxClimb"/> (and a
        /// one-pixel lip); anything steeper is a wall, so getting out of a deep crater takes a jump. There are no
        /// overhangs or bridges in an arena, so a ceiling is just solid ground over the head.
        /// </summary>
        static MoveResult MoveOnCraters(Level level, CraterGround g, ref Vector2 feet, float halfW, float height, ref Vector2 vel, float dt)
        {
            var result = new MoveResult();

            // --- X ---
            float nx = Mathf.Clamp(feet.x + vel.x * dt, halfW, level.Width - halfW);
            if (nx != feet.x)
            {
                float rise = Surface(level, g, nx, halfW, feet.y + height - Eps) - feet.y;
                if (rise > Mathf.Abs(nx - feet.x) * CraterGround.MaxClimb + 1f / CraterGround.Px + Eps)
                {
                    nx = feet.x;
                    vel.x = 0f;
                    result.HitWall = true;
                }
            }
            feet.x = nx;

            // --- Y ---
            float ground = Surface(level, g, feet.x, halfW, feet.y + height - Eps);
            float ny = feet.y + vel.y * dt;
            if (vel.y <= 0f)
            {
                // on the ground (following it down a slope), or landing this step
                if (ny <= ground || (feet.y - ground < 0.12f))
                {
                    ny = ground;
                    vel.y = 0f;
                    result.Grounded = true;
                }
            }
            else if (g.IsSolidAt(new Vector2(feet.x, ny + height)))
            {
                ny = feet.y;
                vel.y = 0f;
                result.HitCeiling = true;
            }
            feet.y = ny;
            return result;
        }

        /// <summary>The highest ground at or below <paramref name="fromY"/> under an actor's middle and edges.</summary>
        static float Surface(Level level, CraterGround g, float x, float halfW, float fromY)
        {
            float best = 0f;
            for (int i = -1; i <= 1; i++)
            {
                float sx = x + i * (halfW - Eps);
                float s = g.Covers(sx) ? g.SurfaceBelow(sx, fromY) : TileSurfaceBelow(level, sx, fromY);
                best = Mathf.Max(best, s);
            }
            return best;
        }

        static float TileSurfaceBelow(Level level, float x, float fromY)
        {
            int c = Mathf.FloorToInt(x);
            for (int y = Mathf.Min(Mathf.FloorToInt(fromY), level.Height - 1); y >= 0; y--)
                if (level.TileSolid(c, y)) return y + 1;
            return 0f;
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
