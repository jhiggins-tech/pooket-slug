using UnityEngine;

namespace PooketSlug
{
    public static class SortOrder
    {
        public const int Sky = -100, Mountains = -90, Ruins = -80, Tiles = 0, Pickups = 5, Boss = 8;
        public const int Enemies = 10, Player = 20, Projectiles = 30, Effects = 40;
    }

    /// <summary>Plain C# game object driven by <see cref="World"/>; owns one Unity GameObject for rendering.</summary>
    public abstract class Entity
    {
        public World World;
        public GameObject Go;
        public Vector2 Pos;
        public bool Removed;

        protected Entity(World world, string name, Vector2 pos)
        {
            World = world;
            Pos = pos;
            Go = new GameObject(name);
            Go.transform.SetParent(world.Root, false);
        }

        public abstract void Tick(float dt);

        public virtual void Render()
        {
            Go.transform.localPosition = new Vector3(Snap(Pos.x), Snap(Pos.y), 0f);
        }

        public void Remove() => Removed = true;

        public virtual void Destroy() => Object.Destroy(Go);

        public static float Snap(float v) => Mathf.Round(v * Art.PPU) / Art.PPU;

        protected SpriteRenderer AddRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }
    }

    /// <summary>Something with a feet-anchored hitbox, health and tile collision.</summary>
    public abstract class Actor : Entity
    {
        public float HalfW, Height;
        public Vector2 Vel;
        public int Hp, MaxHp;
        public int Facing = 1;
        public bool Grounded;
        protected float FlashTimer;
        protected readonly Transform Visual; // flipped with Facing

        protected Actor(World world, string name, Vector2 feet, float halfW, float height, int hp) : base(world, name, feet)
        {
            HalfW = halfW;
            Height = height;
            Hp = MaxHp = hp;
            Visual = new GameObject("Visual").transform;
            Visual.SetParent(Go.transform, false);
        }

        public Rect Bounds => new Rect(Pos.x - HalfW, Pos.y, HalfW * 2f, Height);
        public Vector2 Center => new Vector2(Pos.x, Pos.y + Height * 0.5f);

        public override void Render()
        {
            base.Render();
            Visual.localScale = new Vector3(Facing, 1f, 1f);
        }

        protected Sprite Flashed(Sprite s) => FlashTimer > 0f ? Art.Flash(s) : s;

        protected MoveResult MoveWithTiles(float dt, bool dropThrough = false)
        {
            MoveResult r = TilePhysics.Move(World.Level, ref Pos, HalfW, Height, ref Vel, dt, dropThrough);
            Grounded = r.Grounded;
            return r;
        }
    }

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
