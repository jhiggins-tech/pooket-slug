using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// One play-through: the level, its entities, and the systems that run them. Destroyed and rebuilt on
    /// restart. Each system owns one concern: <see cref="View"/> the camera, <see cref="Spawner"/> what the
    /// level brings in, <see cref="Combat"/> shots and damage, <see cref="Scoring"/> points and popups, and
    /// <see cref="Sync"/> the co-op session. World holds the entities and decides the order things run in.
    /// </summary>
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

        public readonly ScrollCamera View;
        public readonly Spawner Spawner;
        public readonly Combat Combat;
        public readonly Scoring Scoring = new Scoring();
        /// <summary>Droplets and puddles (spike: tones2's kit).</summary>
        public readonly Liquids Liquids;
        /// <summary>The hit feel spike's sparks, flashes and damage numbers, or null when it's off.</summary>
        public readonly HitFeel HitFeel;
        /// <summary>The co-op session's side of the world; null when playing solo.</summary>
        public readonly NetSync Sync;

        public readonly List<Enemy> Enemies = new List<Enemy>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<Pickup> Pickups = new List<Pickup>();
        public readonly List<Entity> Effects = new List<Entity>();
        public readonly List<RemotePlayer> Remotes = new List<RemotePlayer>();
        public readonly List<Entity> Props = new List<Entity>();

        // Bullets, explosions and debris are reused rather than made and destroyed each time (see EntityPool).
        readonly EntityPool<Projectile> m_ProjectilePool;
        readonly EntityPool<Effect> m_EffectPool;
        readonly EntityPool<Particle> m_ParticlePool;

        /// <summary>The mission being played; its map is <see cref="Level"/>.</summary>
        public readonly MissionDef Mission;
        public float Elapsed;
        public bool Victory, BossDefeatedPending;
        public float VictoryTime;

        public World(Camera camera, MissionDef mission, INetLink net = null)
        {
            Camera = camera;
            Mission = mission;
            Net = net;
            Round = net?.Round ?? 0;
            Root = new GameObject("World").transform;
            m_ProjectilePool = new EntityPool<Projectile>(() => new Projectile(this), 128);
            m_EffectPool = new EntityPool<Effect>(() => new Effect(this), 64);
            m_ParticlePool = new EntityPool<Particle>(() => new Particle(this), 256);
            Level = mission.Load();
            LevelView.Create(Level, Root);
            View = new ScrollCamera(this);
            Spawner = new Spawner(this);
            Combat = new Combat(this);
            Liquids = new Liquids(this);
            if (Spike.Hits && !Coop) HitFeel = new HitFeel(this); // spikes are solo only
            if (net != null) Sync = new NetSync(this);

            Vector2 start = Level.PlayerStart;
            if (Coop)
            {
                // Joining a game in progress: start where the others are, a step apart from the partner.
                float x = Mathf.Max(start.x + net.LocalSkin * 1.25f, View.Left + 2f);
                start = new Vector2(x, Level.TopSurfaceY(x));
            }
            SoldierDef soldier = Spike.Tones2 && !Coop ? SpikeContent.Tones2 : Content.Trooper; // spikes are solo only
            Player = new Player(this, soldier, start, Coop ? net.LocalSkin : 0);
            View.Tick(0f);
            Spawner.SpawnAhead();
        }

        public bool Coop => Net != null;
        /// <summary>Runs enemies, pickups and score: always offline, the master client in co-op.</summary>
        public bool IsAuthority => Net == null || Net.IsAuthority;

        public void Dispose() => Object.Destroy(Root.gameObject);

        /// <summary>Tanks our soldier can still call in this mission (<c>docs/design/tanks.md</c>).</summary>
        public int TanksToCall;
        /// <summary>Our soldier called in a tank, at this spot. Nothing drives one yet, so for now it's a marker.</summary>
        public event System.Action<Vector2> TankCalled;
        float m_HitStop;

        /// <summary>Freezes a solo game for a moment, to give a kill weight. Co-op never stops: the others keep playing.</summary>
        public void HitStop(float seconds)
        {
            if (!Coop) m_HitStop = Mathf.Max(m_HitStop, seconds);
        }

        /// <summary>Our soldier killed an enemy: it charges the super, and lands with a hit-stop.</summary>
        public void OnPlayerKill(Enemy e)
        {
            Player.Kit.AddKill();
            HitFeel?.Kill(e);
            HitStop(e.MaxHp >= Tuning.Impact.BigHp ? Tuning.Impact.BigKillStop : Tuning.Impact.KillStop);
        }

        /// <summary>
        /// Co-op, authority side: who a 1-up claimed by <paramref name="claimant"/> goes to. A partner who's out of
        /// lives comes first (<c>docs/design/player.md</c>, Health and lives).
        /// </summary>
        public int OneUpFor(int claimant)
        {
            if (Player.Lives.OutOfLives && Net.LocalPlayerId >= 0 && Net.LocalPlayerId != claimant) return Net.LocalPlayerId;
            foreach (RemotePlayer r in Remotes)
                if (r.OutOfLives && r.Link.PlayerId >= 0 && r.Link.PlayerId != claimant) return r.Link.PlayerId;
            return claimant;
        }

        /// <summary>Everyone in the game is out of lives and continues: the mission is lost.</summary>
        public bool AllOut
        {
            get
            {
                if (!Player.Lives.IsOut) return false;
                foreach (RemotePlayer r in Remotes)
                    if (!r.Gone) return false; // a partner offered a continue might still take it
                return true;
            }
        }

        /// <summary>Uses up one of <see cref="TanksToCall"/>.</summary>
        public void CallTank(Vector2 at)
        {
            if (TanksToCall <= 0) return;
            TanksToCall--;
            Scoring.Popup("TANK INBOUND!", at + Vector2.up * 2.4f, new Color(1f, 0.85f, 0.3f));
            View.Shake(0.2f);
            TankCalled?.Invoke(at);
        }

        /// <summary>Runs one frame, with the local soldier following <paramref name="input"/>.</summary>
        public void Tick(float dt, in SoldierInput input)
        {
            if (m_HitStop > 0f)
            {
                m_HitStop -= dt;
                return;
            }
            Elapsed += dt;
            if (Victory) VictoryTime += dt;

            Sync?.Pull();

            Player.Input = input;
            Player.Tick(dt);
            for (int i = 0; i < Remotes.Count; i++) Remotes[i].Tick(dt);
            Spawner.SpawnAhead();

            for (int i = 0; i < Enemies.Count; i++) Enemies[i].Tick(dt);
            for (int i = 0; i < Projectiles.Count; i++) Projectiles[i].Tick(dt);
            for (int i = 0; i < Pickups.Count; i++) Pickups[i].Tick(dt);
            for (int i = 0; i < Effects.Count; i++) Effects[i].Tick(dt);

            Combat.Collide();
            Scoring.Tick(dt);

            Sweep(Enemies);
            Sweep(Projectiles);
            Sweep(Pickups);
            Sweep(Effects);

            View.Tick(dt);

            Sync?.Push();
        }

        /// <summary>Pushes positions/sprites to Unity. Called every frame, even while paused.</summary>
        public void Render()
        {
            Player.Render();
            foreach (RemotePlayer r in Remotes) r.Render();
            foreach (Enemy e in Enemies)
            {
                e.Render();
                e.Look?.Render();
            }
            foreach (Projectile p in Projectiles) p.Render();
            foreach (Pickup p in Pickups) p.Render();
            foreach (Entity e in Effects) e.Render();
            foreach (Entity e in Props) e.Render();
            HitFeel?.Render();
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

        public Enemy FindEnemy(int id)
        {
            foreach (Enemy e in Enemies)
                if (e.NetId == id && !e.Removed) return e;
            return null;
        }

        public void OnBossDestroyed()
        {
            if (Victory) return;
            Victory = true;
            Player.InputEnabled = true;
            Sfx.Play(Sfx.Victory);
        }

        // ------------------------------------------------------------------ pooled spawning

        public Projectile SpawnProjectile(ProjectileDef def, Vector2 pos, Vector2 vel, bool hostile, bool cosmetic = false)
        {
            Projectile p = m_ProjectilePool.Get();
            p.Launch(def, pos, vel, hostile, cosmetic);
            Projectiles.Add(p);
            return p;
        }

        public void SpawnEffect(Sprite[] frames, float fps, Vector2 pos, float scale = 1f, Vector2 vel = default)
        {
            Effect e = m_EffectPool.Get();
            e.Play(frames, fps, pos, scale, vel);
            Effects.Add(e);
        }

        public void SpawnParticle(Vector2 pos, Vector2 vel, Color color, float size, float life)
        {
            Particle p = m_ParticlePool.Get();
            p.Emit(pos, vel, color, size, life);
            Effects.Add(p);
        }

        /// <summary>Debris flying out from a point.</summary>
        public void Burst(Vector2 at, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                var vel = new Vector2(Rng.Fx.Range(-5f, 5f), Rng.Fx.Range(2f, 10f));
                SpawnParticle(at, vel, color, Rng.Fx.Range(1.5f, 3f), Rng.Fx.Range(0.5f, 1f));
            }
        }

        // Called from Destroy() once the sweep has dropped them from their list.
        public void Release(Projectile p) => m_ProjectilePool.Release(p);
        public void Release(Effect e) => m_EffectPool.Release(e);
        public void Release(Particle p) => m_ParticlePool.Release(p);
    }
}
