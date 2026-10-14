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

        /// <summary>True for a soldier that is dead (or respawning), so enemies ignore it.</summary>
        public virtual bool IsDown => false;

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
}
