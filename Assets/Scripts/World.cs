using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    public struct PopupText
    {
        public string Text;
        public Vector2 Pos;
        public Color Color;
        public float Age;
    }

    /// <summary>One play-through: level, entities, camera and scoring. Destroyed and rebuilt on restart.</summary>
    public sealed class World
    {
        public const float ViewHeight = 11.25f; // 180 px at 16 px per unit

        public readonly Transform Root;
        public readonly Level Level;
        public readonly Camera Camera;
        public Player Player;
        public TankBoss Boss;

        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<Pickup> Pickups = new List<Pickup>();
        public readonly List<Entity> Effects = new List<Entity>();
        public readonly List<PopupText> Popups = new List<PopupText>();
        readonly List<Entity> m_Props = new List<Entity>();

        public int Score;
        public float Elapsed;
        public bool Victory, BossDefeatedPending;
        public float VictoryTime;

        float m_CamX, m_Shake;
        int m_NextSpawn;
        readonly Transform[] m_Mountains = new Transform[3], m_Ruins = new Transform[3];
        Transform m_Sky;

        public World(Camera camera)
        {
            Camera = camera;
            Root = new GameObject("World").transform;
            Level = new Level();
            Level.CreateView(Root);
            BuildBackground();

            Player = new Player(this, Level.PlayerStart);
            m_CamX = HalfWidth;
            UpdateCamera(0f);
            SpawnAhead();
        }

        public float HalfWidth => ViewHeight * 0.5f * Camera.aspect;
        public float CameraLeft => m_CamX - HalfWidth;
        public float CameraRight => m_CamX + HalfWidth;
        public Rect ViewRect => new Rect(CameraLeft, 0f, HalfWidth * 2f, ViewHeight);

        public void Dispose() => Object.Destroy(Root.gameObject);

        public void Tick(float dt)
        {
            Elapsed += dt;
            if (Victory) VictoryTime += dt;

            Player.Tick(dt);
            SpawnAhead();

            for (int i = 0; i < Enemies.Count; i++) Enemies[i].Tick(dt);
            for (int i = 0; i < Projectiles.Count; i++) Projectiles[i].Tick(dt);
            for (int i = 0; i < Pickups.Count; i++) Pickups[i].Tick(dt);
            for (int i = 0; i < Effects.Count; i++) Effects[i].Tick(dt);

            Collide();

            for (int i = 0; i < Popups.Count; i++)
            {
                PopupText p = Popups[i];
                p.Age += dt;
                p.Pos.y += dt * 0.8f;
                Popups[i] = p;
            }
            Popups.RemoveAll(p => p.Age > 1.4f);

            Sweep(Enemies);
            Sweep(Projectiles);
            Sweep(Pickups);
            Sweep(Effects);

            UpdateCamera(dt);
        }

        /// <summary>Pushes positions/sprites to Unity. Called every frame, even while paused.</summary>
        public void Render()
        {
            Player.Render();
            foreach (Enemy e in Enemies) e.Render();
            foreach (Projectile p in Projectiles) p.Render();
            foreach (Pickup p in Pickups) p.Render();
            foreach (Entity e in Effects) e.Render();
            foreach (Entity e in m_Props) e.Render();
        }

        static void Sweep<T>(List<T> list) where T : Entity
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i].Removed) continue;
                list[i].Destroy();
                list.RemoveAt(i);
            }
        }

        void Collide()
        {
            foreach (Projectile p in Projectiles)
            {
                if (p.Removed) continue;
                if (p.Hostile)
                {
                    if (!Player.Invulnerable && Overlaps(Player.Bounds, p.Pos, p.Radius))
                    {
                        if (p.Kind == ProjectileKind.EnemyBullet) Player.Hurt(p.Damage);
                        p.Detonate();
                    }
                    continue;
                }
                foreach (Enemy e in Enemies)
                {
                    if (!e.Shootable || !Overlaps(e.Bounds, p.Pos, p.Radius)) continue;
                    if (p.Kind == ProjectileKind.Bullet || p.Kind == ProjectileKind.Heavy) e.Hurt(p.Damage);
                    p.Detonate();
                    break;
                }
            }

            if (!Player.Dead)
            {
                foreach (Pickup pk in Pickups)
                    if (!pk.Removed && pk.Bounds.Overlaps(Player.Bounds)) pk.Apply(Player);
            }
        }

        static bool Overlaps(Rect r, Vector2 c, float radius)
        {
            float cx = Mathf.Clamp(c.x, r.xMin, r.xMax), cy = Mathf.Clamp(c.y, r.yMin, r.yMax);
            float dx = c.x - cx, dy = c.y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        public void Explode(Vector2 at, float radius, int damage, bool hostile, float size)
        {
            Effects.Add(new Effect(this, Art.Explosion, 18f, at, size));
            Burst(at, Mathf.RoundToInt(6 * size), new Color(0.35f, 0.3f, 0.28f));
            Sfx.Play(Sfx.Explosion, Mathf.Clamp01(0.4f + size * 0.4f));
            Shake(0.1f * size);
            if (damage <= 0) return;

            if (hostile)
            {
                if (!Player.Invulnerable && Overlaps(Player.Bounds, at, radius)) Player.Hurt(damage);
                return;
            }
            foreach (Enemy e in Enemies)
                if (e.Shootable && Overlaps(e.Bounds, at, radius)) e.Hurt(damage);
        }

        public void Burst(Vector2 at, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                var vel = new Vector2(Random.Range(-5f, 5f), Random.Range(2f, 10f));
                Effects.Add(new Particle(this, at, vel, color, Random.Range(1.5f, 3f), Random.Range(0.5f, 1f)));
            }
        }

        public void Shake(float amount) => m_Shake = Mathf.Max(m_Shake, amount);

        public void AddScore(int points, Vector2 at, bool popup)
        {
            Score += points;
            if (popup) Popup(points.ToString(), at, Color.white);
        }

        public void Popup(string text, Vector2 at, Color color) =>
            Popups.Add(new PopupText { Text = text, Pos = at, Color = color });

        public void OnBossDestroyed()
        {
            Victory = true;
            Player.InputEnabled = true;
            Sfx.Play(Sfx.Victory);
        }

        void SpawnAhead()
        {
            while (m_NextSpawn < Level.Spawns.Count && Level.Spawns[m_NextSpawn].Pos.x < CameraRight + 1.5f)
            {
                Spawn s = Level.Spawns[m_NextSpawn++];
                switch (s.Kind)
                {
                    case SpawnKind.Soldier: Enemies.Add(new Soldier(this, s.Pos)); break;
                    case SpawnKind.Runner: Enemies.Add(new Runner(this, s.Pos)); break;
                    case SpawnKind.Drone: Enemies.Add(new Drone(this, s.Pos)); break;
                    case SpawnKind.Tank:
                        Boss = new TankBoss(this, s.Pos);
                        Enemies.Add(Boss);
                        break;
                    case SpawnKind.CrateHeavy: Pickups.Add(new Pickup(this, PickupKind.HeavyMachineGun, s.Pos)); break;
                    case SpawnKind.CrateBombs: Pickups.Add(new Pickup(this, PickupKind.Bombs, s.Pos)); break;
                    case SpawnKind.Medkit: Pickups.Add(new Pickup(this, PickupKind.Medkit, s.Pos)); break;
                    case SpawnKind.Flag: m_Props.Add(new Prop(this, Art.Flag, s.Pos, SortOrder.Pickups)); break;
                }
            }
        }

        void UpdateCamera(float dt)
        {
            float maxX = Level.Width - HalfWidth;
            // Scroll forward only (Metal Slug style), with a little look-ahead.
            float desired = Player.Pos.x + 2f;
            if (desired > m_CamX) m_CamX = Mathf.Min(desired, m_CamX + 12f * Mathf.Max(dt, 0.0001f));
            m_CamX = Mathf.Clamp(m_CamX, HalfWidth, Mathf.Max(HalfWidth, maxX));

            Vector2 shake = Vector2.zero;
            if (m_Shake > 0f)
            {
                shake = Random.insideUnitCircle * m_Shake;
                m_Shake = Mathf.Max(0f, m_Shake - dt * 0.6f);
            }
            float cx = Entity.Snap(m_CamX + shake.x), cy = Entity.Snap(ViewHeight * 0.5f + shake.y);
            Camera.transform.position = new Vector3(cx, cy, -10f);

            // Parallax: layer origin moves with the camera by `factor` (1 = infinitely far away).
            m_Sky.position = new Vector3(cx, cy, 0f);
            m_Sky.localScale = new Vector3((HalfWidth * 2f + 2f) * Art.PPU, (ViewHeight + 2f) / 2f, 1f);
            PlaceLayer(m_Mountains, Art.Mountains, 0.85f, 1.2f, cx);
            PlaceLayer(m_Ruins, Art.Ruins, 0.6f, 1.0f, cx);
        }

        void PlaceLayer(Transform[] tiles, Sprite sprite, float factor, float y, float camX)
        {
            float width = sprite.rect.width / Art.PPU;
            float origin = camX * factor;
            float left = camX - HalfWidth;
            float first = origin + Mathf.Floor((left - origin) / width) * width;
            for (int i = 0; i < tiles.Length; i++)
                tiles[i].position = new Vector3(Entity.Snap(first + i * width), y, 0f);
        }

        void BuildBackground()
        {
            m_Sky = MakeBg("Sky", Art.Sky, SortOrder.Sky);
            for (int i = 0; i < 3; i++)
            {
                m_Mountains[i] = MakeBg("Mountains" + i, Art.Mountains, SortOrder.Mountains);
                m_Ruins[i] = MakeBg("Ruins" + i, Art.Ruins, SortOrder.Ruins);
            }
        }

        Transform MakeBg(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return go.transform;
        }
    }
}
