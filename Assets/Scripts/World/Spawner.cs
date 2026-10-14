using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Brings the level's spawns into the world as the camera reaches them. The authority (and a solo game)
    /// places everything. Co-op followers place only pickups and props, and get enemies from the snapshot.
    /// A spawn's index in <see cref="Level.Spawns"/> is its network identity.
    /// </summary>
    public sealed class Spawner
    {
        readonly World m_World;
        int m_Next, m_NextStatic;

        public Spawner(World world) => m_World = world;

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
                case EnemyBrain.Tank: e = w.Boss = new TankBoss(w, def, pos); break;
                default: return null;
            }
            e.NetId = id;
            return e;
        }

        void SpawnStatic(Spawn s, int index)
        {
            World w = m_World;
            PickupDef pickup = Content.PickupFor(s.Kind);
            if (pickup != null)
            {
                if (w.Coop && w.Net.IsTaken(index)) return;
                w.Pickups.Add(new Pickup(w, pickup, s.Pos) { NetId = index });
            }
            else if (s.Kind == SpawnKind.Flag)
            {
                w.Props.Add(new Prop(w, Art.Flag, s.Pos, SortOrder.Pickups));
            }
        }

        public void SpawnAhead()
        {
            World w = m_World;
            Level level = w.Level;
            if (w.Coop && !w.IsAuthority)
            {
                // Followers only place static things; enemies arrive with the authority's snapshot.
                int limit = Mathf.Min(w.Net.NextSpawn, level.Spawns.Count);
                while (m_NextStatic < limit)
                {
                    int i = m_NextStatic++;
                    if (Content.EnemyFor(level.Spawns[i].Kind) == null) SpawnStatic(level.Spawns[i], i);
                }
                return;
            }

            if (w.Coop) m_Next = Mathf.Max(m_Next, Mathf.Max(m_NextStatic, w.Net.NextSpawn)); // took over from another authority
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
