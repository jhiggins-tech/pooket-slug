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
        public readonly Transform Root;
        public readonly Level Level;
        public readonly Camera Camera;
        public readonly INetLink Net; // null when playing solo
        public readonly int Round;
        /// <summary>Gameplay randomness for this play-through; cosmetic effects use <see cref="Rng.Fx"/>.</summary>
        public readonly Rng Rng = new Rng(Rng.NewSeed());
        public Player Player;
        public TankBoss Boss;

        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<Pickup> Pickups = new List<Pickup>();
        public readonly List<Entity> Effects = new List<Entity>();
        public readonly List<RemotePlayer> Remotes = new List<RemotePlayer>();
        public readonly List<PopupText> Popups = new List<PopupText>();
        readonly List<Entity> m_Props = new List<Entity>();

        // co-op bookkeeping
        readonly List<NetEnemyState> m_Snapshot = new List<NetEnemyState>();
        readonly HashSet<int> m_SeenIds = new HashSet<int>();
        readonly HashSet<int> m_DeadIds = new HashSet<int>();
        readonly Dictionary<IAvatarLink, float> m_HurtCooldown = new Dictionary<IAvatarLink, float>();

        public int Score;
        public float Elapsed;
        public bool Victory, BossDefeatedPending;
        public float VictoryTime;

        float m_CamX, m_Shake;
        int m_NextSpawn, m_NextStatic;
        readonly Transform[] m_Mountains = new Transform[3], m_Ruins = new Transform[3];
        Transform m_Sky;

        public World(Camera camera, INetLink net = null)
        {
            Camera = camera;
            Net = net;
            Round = net?.Round ?? 0;
            Root = new GameObject("World").transform;
            Level = new Level();
            Level.CreateView(Root);
            BuildBackground();

            m_CamX = GameHalfWidth;
            Vector2 start = Level.PlayerStart;
            if (Coop)
            {
                // Joining a game in progress: start where the others are, a step apart from the partner.
                if (!net.IsAuthority) m_CamX = Mathf.Max(m_CamX, net.CamX);
                float x = Mathf.Max(start.x + net.LocalSkin * 1.25f, CameraLeft + 2f);
                start = new Vector2(x, Level.TopSurfaceY(x));
            }
            Player = new Player(this, start, Coop ? net.LocalSkin : 0);
            UpdateCamera(0f);
            SpawnAhead();
        }

        public bool Coop => Net != null;
        /// <summary>Runs enemies, pickups and score: always offline, the master client in co-op.</summary>
        public bool IsAuthority => Net == null || Net.IsAuthority;
        public float HalfWidth => Tuning.Camera.ViewHeight * 0.5f * Camera.aspect;
        float GameHalfWidth => Coop ? Tuning.Coop.HalfWidth : HalfWidth;
        public float CameraLeft => m_CamX - GameHalfWidth;
        public float CameraRight => m_CamX + GameHalfWidth;
        public Rect ViewRect => new Rect(m_CamX - Mathf.Max(HalfWidth, GameHalfWidth), 0f, 2f * Mathf.Max(HalfWidth, GameHalfWidth), Tuning.Camera.ViewHeight);

        public void Dispose() => Object.Destroy(Root.gameObject);

        public void Tick(float dt)
        {
            Elapsed += dt;
            if (Victory) VictoryTime += dt;

            if (Coop) PullNet();

            Player.Tick(dt);
            for (int i = 0; i < Remotes.Count; i++) Remotes[i].Tick(dt);
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
                p.Pos.y += dt * Tuning.Hud.PopupRise;
                Popups[i] = p;
            }
            Popups.RemoveAll(p => p.Age > Tuning.Hud.PopupLife);

            Sweep(Enemies);
            Sweep(Projectiles);
            Sweep(Pickups);
            Sweep(Effects);

            UpdateCamera(dt);

            if (Coop) PushNet();
        }

        /// <summary>Pushes positions/sprites to Unity. Called every frame, even while paused.</summary>
        public void Render()
        {
            Player.Render();
            foreach (RemotePlayer r in Remotes) r.Render();
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

        // ------------------------------------------------------------------ co-op sync

        /// <summary>Applies what arrived from the network: remote soldiers and (followers) the authority's world.</summary>
        void PullNet()
        {
            IReadOnlyList<IAvatarLink> links = Net.RemoteAvatars;
            for (int i = Remotes.Count - 1; i >= 0; i--)
            {
                bool present = false;
                for (int j = 0; j < links.Count; j++) present |= ReferenceEquals(links[j], Remotes[i].Link);
                if (present) continue;
                Remotes[i].Destroy();
                Remotes.RemoveAt(i);
            }
            for (int j = 0; j < links.Count; j++)
            {
                bool known = false;
                foreach (RemotePlayer r in Remotes) known |= ReferenceEquals(r.Link, links[j]);
                if (!known) Remotes.Add(new RemotePlayer(this, links[j]));
            }
            Player.Skin = Net.LocalSkin;

            if (IsAuthority) return;

            Score = Net.Score;
            if (Net.Victory) OnBossDestroyed();
            foreach (Pickup pk in Pickups)
                if (!pk.Removed && pk.NetId >= 0 && Net.IsTaken(pk.NetId)) pk.Collect(); // state beat the grant message

            Net.ReadEnemies(m_Snapshot);
            m_SeenIds.Clear();
            foreach (NetEnemyState s in m_Snapshot)
            {
                m_SeenIds.Add(s.Id);
                Enemy e = FindEnemy(s.Id);
                if (e == null)
                {
                    if ((s.Flags & NetEnemyState.Dying) != 0 || m_DeadIds.Contains(s.Id)) continue;
                    e = CreateEnemy(s.Kind, s.Pos, s.Id);
                    if (e == null) continue;
                    Enemies.Add(e);
                }
                e.ApplySnapshot(s);
                if (e.Dying) m_DeadIds.Add(s.Id);
            }
            // gone from the authority's world (left behind or finished dying)
            foreach (Enemy e in Enemies)
                if (!e.Dying && !m_SeenIds.Contains(e.NetId)) e.Remove();
        }

        /// <summary>Publishes our soldier, and (authority) the shared world state.</summary>
        void PushNet()
        {
            Net.SendAvatar(Player.Pos, Player.Vel, Player.Pose.Pack(), Player.Hp);
            if (!IsAuthority) return;
            Net.CamX = m_CamX;
            Net.Score = Score;
            Net.Victory = Victory;
            Net.NextSpawn = m_NextSpawn;
            Net.WriteEnemies(Enemies);
        }

        // network events (delivered by the session)

        public void OnRemoteShot(ProjectileKind kind, Vector2 origin, Vector2 vel)
        {
            Projectiles.Add(new Projectile(this, kind, origin, vel, false, cosmetic: true));
            Sfx.Play(kind == ProjectileKind.Heavy ? Sfx.Heavy : kind == ProjectileKind.Grenade ? Sfx.Throw : Sfx.Shot, 0.35f);
        }

        public void OnEnemyShot(ProjectileKind kind, Vector2 origin, Vector2 vel) => SpawnEnemyProjectile(kind, origin, vel);

        public void OnHitReported(int enemyId, int damage)
        {
            if (!IsAuthority) return;
            Enemy e = FindEnemy(enemyId);
            if (e != null && e.Shootable) e.Hurt(damage);
        }

        public void OnPickupGranted(int spawnIndex, bool mine)
        {
            Pickup pk = null;
            foreach (Pickup p in Pickups)
                if (p.NetId == spawnIndex && !p.Removed) pk = p;
            if (mine)
            {
                if (pk != null) pk.ApplyTo(Player);
                else if (spawnIndex >= 0 && spawnIndex < Level.Spawns.Count) Pickup.Give(ToPickup(Level.Spawns[spawnIndex].Kind), Player);
            }
            pk?.Collect();
        }

        public void OnLocalHurt(int damage) => Player.Hurt(damage);

        // ------------------------------------------------------------------ combat routing

        public void FirePlayerProjectile(ProjectileKind kind, Vector2 origin, Vector2 vel)
        {
            Projectiles.Add(new Projectile(this, kind, origin, vel, false));
            Net?.SendShot(kind, origin, vel);
        }

        /// <summary>Authority side: an enemy fires (replayed on the other clients).</summary>
        public void FireEnemyProjectile(ProjectileKind kind, Vector2 origin, Vector2 vel)
        {
            SpawnEnemyProjectile(kind, origin, vel);
            if (Coop) Net.SendEnemyShot(kind, origin, vel);
        }

        void SpawnEnemyProjectile(ProjectileKind kind, Vector2 origin, Vector2 vel)
        {
            Projectiles.Add(new Projectile(this, kind, origin, vel, true));
            if (kind == ProjectileKind.EnemyBullet) Sfx.Play(Sfx.EnemyShot, 0.35f);
            if (kind == ProjectileKind.Shell)
            {
                Effects.Add(new Effect(this, Art.Explosion, 30f, origin, 0.4f));
                Sfx.Play(Sfx.Cannon, 0.8f);
                Shake(0.08f);
            }
        }

        /// <summary>Our soldier hit an enemy: apply it (authority) or report it to the authority.</summary>
        public void DamageEnemy(Enemy e, int damage)
        {
            if (IsAuthority)
            {
                e.Hurt(damage);
                return;
            }
            e.FlashHit();
            Net.ReportHit(e.NetId, damage);
        }

        /// <summary>Nearest soldier still in the fight (the local one if everybody is down).</summary>
        public Actor NearestAvatar(Vector2 from)
        {
            Actor best = Player;
            float bestDist = Player.IsDown ? float.MaxValue : (Player.Pos - from).sqrMagnitude;
            foreach (RemotePlayer r in Remotes)
            {
                if (r.IsDown) continue;
                float d = (r.Pos - from).sqrMagnitude;
                if (d < bestDist)
                {
                    best = r;
                    bestDist = d;
                }
            }
            return best;
        }

        /// <summary>Authority side: melee/contact damage. A partner's soldier is hurt by its own client.</summary>
        public void DamageAvatar(Actor target, int damage)
        {
            if (target == Player)
            {
                Player.Hurt(damage);
                return;
            }
            if (target is RemotePlayer rp && !rp.Invulnerable)
            {
                if (m_HurtCooldown.TryGetValue(rp.Link, out float until) && Elapsed < until) return;
                m_HurtCooldown[rp.Link] = Elapsed + Tuning.Coop.RemoteHurtCooldown;
                rp.Link.Hurt(damage);
            }
        }

        public void DamageAvatarsTouching(Rect area, int damage)
        {
            if (area.Overlaps(Player.Bounds)) Player.Hurt(damage);
            foreach (RemotePlayer r in Remotes)
                if (!r.IsDown && area.Overlaps(r.Bounds)) DamageAvatar(r, damage);
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
                        continue;
                    }
                    // the partner's client applies the damage; here the shot just stops on them
                    foreach (RemotePlayer r in Remotes)
                    {
                        if (r.IsDown || r.Invulnerable || !Overlaps(r.Bounds, p.Pos, p.Radius)) continue;
                        p.Detonate();
                        break;
                    }
                    continue;
                }
                foreach (Enemy e in Enemies)
                {
                    if (!e.Shootable || !Overlaps(e.Bounds, p.Pos, p.Radius)) continue;
                    if (!p.Cosmetic && (p.Kind == ProjectileKind.Bullet || p.Kind == ProjectileKind.Heavy)) DamageEnemy(e, p.Damage);
                    p.Detonate();
                    break;
                }
            }

            if (!Player.Dead)
            {
                foreach (Pickup pk in Pickups)
                {
                    if (pk.Removed || pk.ClaimPending || !pk.Bounds.Overlaps(Player.Bounds)) continue;
                    if (Coop)
                    {
                        pk.ClaimPending = true; // the authority decides who got it
                        Net.ClaimPickup(pk.NetId);
                    }
                    else
                    {
                        pk.ApplyTo(Player);
                        pk.Collect();
                    }
                }
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
                if (e.Shootable && Overlaps(e.Bounds, at, radius)) DamageEnemy(e, damage);
        }

        public void Burst(Vector2 at, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                var vel = new Vector2(Rng.Fx.Range(-5f, 5f), Rng.Fx.Range(2f, 10f));
                Effects.Add(new Particle(this, at, vel, color, Rng.Fx.Range(1.5f, 3f), Rng.Fx.Range(0.5f, 1f)));
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
            if (Victory) return;
            Victory = true;
            Player.InputEnabled = true;
            Sfx.Play(Sfx.Victory);
        }

        // ------------------------------------------------------------------ spawning

        static bool IsEnemy(SpawnKind kind) =>
            kind == SpawnKind.Soldier || kind == SpawnKind.Runner || kind == SpawnKind.Drone || kind == SpawnKind.Tank;

        static PickupKind ToPickup(SpawnKind kind) =>
            kind == SpawnKind.CrateHeavy ? PickupKind.HeavyMachineGun : kind == SpawnKind.CrateBombs ? PickupKind.Bombs : PickupKind.Medkit;

        Enemy FindEnemy(int id)
        {
            foreach (Enemy e in Enemies)
                if (e.NetId == id && !e.Removed) return e;
            return null;
        }

        Enemy CreateEnemy(SpawnKind kind, Vector2 pos, int id)
        {
            Enemy e;
            switch (kind)
            {
                case SpawnKind.Soldier: e = new Soldier(this, pos); break;
                case SpawnKind.Runner: e = new Runner(this, pos); break;
                case SpawnKind.Drone: e = new Drone(this, pos); break;
                case SpawnKind.Tank: e = Boss = new TankBoss(this, pos); break;
                default: return null;
            }
            e.NetId = id;
            e.Kind = kind;
            return e;
        }

        void SpawnStatic(Spawn s, int index)
        {
            switch (s.Kind)
            {
                case SpawnKind.CrateHeavy:
                case SpawnKind.CrateBombs:
                case SpawnKind.Medkit:
                    if (Coop && Net.IsTaken(index)) return;
                    Pickups.Add(new Pickup(this, ToPickup(s.Kind), s.Pos) { NetId = index });
                    break;
                case SpawnKind.Flag:
                    m_Props.Add(new Prop(this, Art.Flag, s.Pos, SortOrder.Pickups));
                    break;
            }
        }

        void SpawnAhead()
        {
            if (Coop && !IsAuthority)
            {
                // Followers only place static things; enemies arrive with the authority's snapshot.
                int limit = Mathf.Min(Net.NextSpawn, Level.Spawns.Count);
                while (m_NextStatic < limit)
                {
                    int i = m_NextStatic++;
                    if (!IsEnemy(Level.Spawns[i].Kind)) SpawnStatic(Level.Spawns[i], i);
                }
                return;
            }

            if (Coop) m_NextSpawn = Mathf.Max(m_NextSpawn, Mathf.Max(m_NextStatic, Net.NextSpawn)); // took over from another authority
            while (m_NextSpawn < Level.Spawns.Count && Level.Spawns[m_NextSpawn].Pos.x < CameraRight + Tuning.Camera.SpawnAhead)
            {
                int index = m_NextSpawn++;
                Spawn s = Level.Spawns[index];
                if (IsEnemy(s.Kind)) Enemies.Add(CreateEnemy(s.Kind, s.Pos, index));
                else SpawnStatic(s, index);
            }
            m_NextStatic = m_NextSpawn;
        }

        // ------------------------------------------------------------------ camera

        void UpdateCamera(float dt)
        {
            float h = GameHalfWidth;
            float step = Tuning.Camera.ScrollSpeed * Mathf.Max(dt, 0.0001f);
            if (!Coop)
            {
                // Scroll forward only (Metal Slug style), with a little look-ahead.
                float desired = Player.Pos.x + Tuning.Camera.LookAhead;
                if (desired > m_CamX) m_CamX = Mathf.Min(desired, m_CamX + step);
            }
            else if (IsAuthority)
            {
                // Follow the leading soldier, but never push the trailing one off the left edge.
                float lead = float.MinValue, trail = float.MaxValue;
                if (!Player.IsDown)
                {
                    lead = trail = Player.Pos.x;
                }
                foreach (RemotePlayer r in Remotes)
                {
                    if (r.IsDown) continue;
                    lead = Mathf.Max(lead, r.Pos.x);
                    trail = Mathf.Min(trail, r.Pos.x);
                }
                if (lead > float.MinValue)
                {
                    float desired = Mathf.Min(lead + Tuning.Camera.LookAhead, trail - Tuning.Coop.TrailMargin + h);
                    if (desired > m_CamX) m_CamX = Mathf.Min(desired, m_CamX + step);
                }
            }
            else if (Net.CamX > m_CamX)
            {
                m_CamX = Mathf.Min(Net.CamX, m_CamX + Tuning.Coop.FollowerCatchUp * step);
            }
            m_CamX = Mathf.Clamp(m_CamX, h, Mathf.Max(h, Level.Width - h));

            Vector2 shake = Vector2.zero;
            if (m_Shake > 0f)
            {
                shake = Rng.Fx.InsideUnitCircle() * m_Shake;
                m_Shake = Mathf.Max(0f, m_Shake - dt * Tuning.Camera.ShakeDecay);
            }
            // A view narrower than the shared co-op width (odd fullscreen aspect) keeps our own soldier in frame.
            float view = m_CamX;
            if (HalfWidth < h) view = Mathf.Clamp(Player.Pos.x, m_CamX - (h - HalfWidth), m_CamX + (h - HalfWidth));
            view = Mathf.Clamp(view, HalfWidth, Mathf.Max(HalfWidth, Level.Width - HalfWidth));
            float cx = Entity.Snap(view + shake.x), cy = Entity.Snap(Tuning.Camera.ViewHeight * 0.5f + shake.y);
            Camera.transform.position = new Vector3(cx, cy, -10f);

            // Parallax: layer origin moves with the camera by `factor` (1 = infinitely far away).
            m_Sky.position = new Vector3(cx, cy, 0f);
            m_Sky.localScale = new Vector3((HalfWidth * 2f + 2f) * Art.PPU, (Tuning.Camera.ViewHeight + 2f) / 2f, 1f);
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
