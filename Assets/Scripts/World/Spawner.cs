using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Brings the level's spawns into the world as the camera reaches them, and anything spawned mid-game.
    /// The authority (and a solo game) places everything. Co-op followers place only props, and get enemies
    /// and pickups from the snapshot. Each enemy's and pickup's network identity comes from <see cref="Ids"/>.
    /// </summary>
    public sealed class Spawner
    {
        readonly World m_World;
        int m_Next, m_NextStatic;

        /// <summary>Level spawns keep their index as their id; things spawned mid-game get the ids after.</summary>
        public readonly NetIds Ids;

        public Spawner(World world)
        {
            m_World = world;
            Ids = new NetIds(world.Level.Spawns.Count);
        }

        /// <summary>How far through the level's spawns the authority has got; the next index to place.</summary>
        public int Next => m_Next;

        /// <summary>Makes an enemy (not yet in the world's list), driven by its definition's brain.</summary>
        public Enemy CreateEnemy(EnemyDef def, Vector2 pos, int id)
        {
            World w = m_World;
            Enemy e;
            switch (def.Brain)
            {
                case EnemyBrain.Rifleman: e = new Soldier(w, def, pos); break;
                case EnemyBrain.Runner: e = new Runner(w, def, pos); break;
                case EnemyBrain.Drone: e = new Drone(w, def, pos); break;
                case EnemyBrain.Tank:
                    e = w.Boss = Spike.Tank && !w.Coop ? new ArtilleryTank(w, def, pos) : (BossEnemy)new TankBoss(w, def, pos); // spikes are solo only
                    break;
                default: return null;
            }
            e.NetId = id;
            if (Spike.Holo && !w.Coop && def.Brain != EnemyBrain.Tank) e.Look = new HoloLook(e); // the goons are holograms; the tank is real
            return e;
        }

        /// <summary>
        /// Authority only: an enemy that appears mid-game (reinforcements, a called-in tank). It gets a fresh
        /// network id, so the snapshot carries it to the other clients. Null on a follower.
        /// </summary>
        public Enemy SpawnEnemy(EnemyDef def, Vector2 pos)
        {
            World w = m_World;
            if (!w.IsAuthority) return null;
            Enemy e = CreateEnemy(def, pos, Ids.Allocate());
            if (e != null) w.Enemies.Add(e);
            return e;
        }

        /// <summary>
        /// Authority only: a pickup that appears mid-game (a drop). It gets a fresh network id, so the snapshot
        /// carries it to the other clients. Null on a follower.
        /// </summary>
        public Pickup SpawnPickup(PickupDef def, Vector2 pos)
        {
            World w = m_World;
            if (!w.IsAuthority) return null;
            var pk = new Pickup(w, def, pos) { NetId = Ids.Allocate() };
            w.Pickups.Add(pk);
            return pk;
        }

        void SpawnStatic(Spawn s, int index)
        {
            World w = m_World;
            PickupDef pickup = Content.PickupFor(s.Kind);
            if (pickup != null) w.Pickups.Add(new Pickup(w, pickup, s.Pos) { NetId = index });
            else if (s.Kind == SpawnKind.Flag) w.Props.Add(new Prop(w, Art.Flag, s.Pos, SortOrder.Pickups));
        }

        public void SpawnAhead()
        {
            World w = m_World;
            Level level = w.Level;
            if (w.Coop && !w.IsAuthority)
            {
                // Followers only place props; enemies and pickups arrive with the authority's snapshot.
                int limit = Mathf.Min(w.Net.NextSpawn, level.Spawns.Count);
                while (m_NextStatic < limit)
                {
                    int i = m_NextStatic++;
                    SpawnKind kind = level.Spawns[i].Kind;
                    if (Content.EnemyFor(kind) == null && Content.PickupFor(kind) == null) SpawnStatic(level.Spawns[i], i);
                }
                return;
            }

            if (w.Coop)
            {
                // took over from another authority: carry on where it stopped
                m_Next = Mathf.Max(m_Next, Mathf.Max(m_NextStatic, w.Net.NextSpawn));
                Ids.CatchUp(w.Net.NextNetId);
            }
            while (m_Next < level.Spawns.Count && level.Spawns[m_Next].Pos.x < w.View.Right + Tuning.Camera.SpawnAhead)
            {
                int index = m_Next++;
                Spawn s = level.Spawns[index];
                EnemyDef enemy = Content.EnemyFor(s.Kind);
                if (enemy != null) w.Enemies.Add(CreateEnemy(enemy, s.Pos, index));
                else SpawnStatic(s, index);
            }
            m_NextStatic = m_Next;
        }
    }
}
