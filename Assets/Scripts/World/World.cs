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
        /// <summary>Our soldier's tank (called in, or <c>?test=tank</c>): parked, driven or rolling away; null when there's none.</summary>
        public PlayerTank Tank;
        /// <summary>Our tanks that have blown up, and where the last one did: a partner's screen shows the blast.</summary>
        public int TanksBlown;
        public Vector2 LastTankPos;
        public BossEnemy Boss;
        /// <summary>kie's hologram on the players' side (Trollogram), or null: the goons shoot it instead while it's up.</summary>
        /// <summary>kie's Trollograms (players' side) standing in the world: every goon shoots one while it's on screen.</summary>
        public readonly List<TrollDecoy> Decoys = new List<TrollDecoy>();

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
        /// <summary>Where the checkpoint our soldier has passed stands, or -1 (see <see cref="Checkpoint"/>).</summary>
        public float CheckpointX = -1f;
        public bool Victory, BossDefeatedPending;
        /// <summary>The boss music's cut before the win (the pilot's big glitch-out, <c>?spike=pilot-glitch</c>).</summary>
        public bool BossMusicOff;
        public float VictoryTime;

        /// <param name="soldier">Who our soldier is (the title's CREW pick); tones2 when not given.</param>
        public World(Camera camera, MissionDef mission, INetLink net = null, SoldierDef soldier = null)
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
            if (Spike.Craters && net == null) // spikes are solo only
            {
                // the boss arena, from the last checkpoint to the end of the map, is diggable ground
                int arena = Level.Width;
                foreach (Spawn s in Level.Spawns)
                    if (s.Kind == SpawnKind.Checkpoint) arena = Mathf.FloorToInt(s.Pos.x);
                if (arena < Level.Width) Level.Craters = new CraterGround(Level, arena, Level.Width);
            }
            LevelView.Create(Level, Root, Level.Craters?.X0 ?? int.MaxValue);
            if (Level.Craters != null) CraterView = new CraterView(Level, Level.Craters, Root);
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
            Player = new Player(this, soldier ?? Content.Tones2, start, Coop ? net.LocalSkin : 0); // tones2 until the crew opens (Crew)
            View.Tick(0f);
            Spawner.SpawnAhead();
        }

        /// <summary>Spike: draws the arena's dug ground (null unless <c>?spike=craters</c>).</summary>
        public CraterView CraterView;

        public bool Coop => Net != null;
        /// <summary>Runs enemies, pickups and score: always offline, the master client in co-op.</summary>
        public bool IsAuthority => Net == null || Net.IsAuthority;

        public void Dispose() => Object.Destroy(Root.gameObject);

        /// <summary>Tanks our soldier can still call in this mission (<c>docs/design/tanks.md</c>).</summary>
        public int TanksToCall;
        /// <summary>Our soldier called in a tank, at this spot: it drops in (<see cref="PlayerTank"/>). In co-op each player calls their own.</summary>
        public event System.Action<Vector2> TankCalled;

        /// <summary>
        /// The boss intro (<c>docs/design/tanks.md</c>): the boss woke as the soldiers stepped into its arena. True the
        /// first time; false for the quick intro, with the tank already in place. Raised on every screen in co-op (a
        /// follower raises it when the authority's tank wakes).
        /// </summary>
        public event System.Action<BossEnemy, bool> BossIntro;
        /// <summary>The boss's first shot: its entrance is over and the fight is on.</summary>
        public event System.Action<BossEnemy> BossFightStarted;
        /// <summary>A boss tank was wrecked, and its pilot round starts.</summary>
        public event System.Action TankWrecked;
        /// <summary>
        /// Set before the boss spawns for the quick intro: the tank starts in place and skips its entrance (a restart at
        /// the boss-door checkpoint; replays when there are any).
        /// </summary>
        public bool QuickBossIntro;
        /// <summary>?test=pilot: a boss tank is wrecked the moment it's ready to fight, so the pilot round starts at once.</summary>
        public bool SkipToPilot;
        /// <summary>Testing (<c>?test=pilot,weak</c>): the pilot jumps out on one point of health.</summary>
        public bool WeakPilot;

        public void OnBossIntro(BossEnemy boss, bool firstTime)
        {
            Log.Game.Info(firstTime ? "Boss intro: " + boss.DisplayName : "Boss intro (quick): " + boss.DisplayName);
            BossIntro?.Invoke(boss, firstTime);
        }

        public void OnBossFightStarted(BossEnemy boss)
        {
            Log.Game.Info("Boss fight on: " + boss.DisplayName);
            BossFightStarted?.Invoke(boss);
        }
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
            // it drops in just ahead of the soldier, on screen; the tank left over, if any, is gone
            int facing = Player.Facing;
            float x = Mathf.Clamp(at.x + facing * 2.6f, View.Left + Content.PlayerTank.HalfWidth + 0.5f, View.Right - Content.PlayerTank.HalfWidth - 0.5f);
            SetTank(PlayerTank.Drop(this, x, facing));
            TankCalled?.Invoke(at);
        }

        /// <summary>?test=tank: an empty tank parked at <paramref name="x"/>, to climb into.</summary>
        public void ParkTank(float x) => SetTank(PlayerTank.Park(this, x));

        void SetTank(PlayerTank tank)
        {
            if (Tank != null)
            {
                if (Player.Riding == Tank) Player.LeaveTank(Tank.Pos + Vector2.up * Tank.Height, Vector2.up * 8f, Tuning.Soldier.HurtInvulnerability);
                Tank.Blow();
                Tank.Destroy();
            }
            Tank = tank;
        }

        /// <summary>Our tank blew up at <paramref name="at"/> (its blast is already on our screen; partners are told by the count).</summary>
        public void TankBlew(Vector2 at)
        {
            TanksBlown++;
            LastTankPos = at;
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
            if (Tank != null)
            {
                if (!Tank.Removed) Tank.Tick(dt); // before the soldier: a driver rides where it goes
                if (Tank.Removed && Player.Riding != Tank)
                {
                    Tank.Destroy();
                    Tank = null;
                }
            }
            Player.Tick(dt);
            for (int i = 0; i < Remotes.Count; i++) Remotes[i].Tick(dt);
            Spawner.SpawnAhead();

            for (int i = 0; i < Enemies.Count; i++) Enemies[i].Tick(dt);
            for (int i = 0; i < Projectiles.Count; i++) Projectiles[i].Tick(dt);
            for (int i = 0; i < Pickups.Count; i++) Pickups[i].Tick(dt);
            for (int i = 0; i < Effects.Count; i++) Effects[i].Tick(dt);
            for (int i = 0; i < Props.Count; i++) Props[i].Tick(dt); // scenery does nothing; hazards hurt

            Combat.Collide();
            Scoring.Tick(dt);

            Sweep(Enemies);
            Sweep(Projectiles);
            Sweep(Pickups);
            Sweep(Effects);
            Sweep(Props);

            View.Tick(dt);

            Sync?.Push();
        }

        /// <summary>Pushes positions/sprites to Unity. Called every frame, even while paused.</summary>
        public void Render()
        {
            Player.Render();
            Tank?.Render();
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

        /// <summary>
        /// A boss tank is wrecked: its hologram pilot jumps out to fight on foot (<c>docs/design/tanks.md</c>, The pilot
        /// round). The authority (or a solo game) puts him in; a co-op follower gets him in the snapshot.
        /// </summary>
        public void OnTankWrecked(TankBoss tank)
        {
            Log.Game.Info("Boss tank wrecked: the pilot jumps out");
            Scoring.Popup("TANK WRECKED!", tank.Center + Vector2.up * 1.6f, new Color(1f, 0.85f, 0.3f));
            TankWrecked?.Invoke();
            if (!IsAuthority) return;
            if (Spawner.SpawnEnemy(Content.Pilot, tank.Pos + Vector2.up * 0.6f) is Pilot pilot)
            {
                pilot.JumpOut(Player.Pos.x < tank.Pos.x ? -1 : 1);
                if (WeakPilot) pilot.Hp = 1;
            }
        }

        /// <summary>The mission's last boss is beaten (the pilot glitched out): the mission's won.</summary>
        public void OnBossDestroyed()
        {
            if (Victory) return;
            Victory = true;
            Player.InputEnabled = true;
            Sfx.Play(Sfx.Victory);
            Sfx.Say("mission-complete", 0.2f);
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
