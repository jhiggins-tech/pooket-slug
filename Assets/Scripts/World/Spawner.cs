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

        public static bool IsEnemy(SpawnKind kind) =>
            kind == SpawnKind.Soldier || kind == SpawnKind.Runner || kind == SpawnKind.Drone || kind == SpawnKind.Tank;

        public static PickupKind ToPickup(SpawnKind kind) =>
            kind == SpawnKind.CrateHeavy ? PickupKind.HeavyMachineGun : kind == SpawnKind.CrateBombs ? PickupKind.Bombs : PickupKind.Medkit;

        /// <summary>Makes an enemy (not yet in the world's list). Null for a kind that isn't an enemy.</summary>
        public Enemy CreateEnemy(SpawnKind kind, Vector2 pos, int id)
        {
            World w = m_World;
            Enemy e;
            switch (kind)
            {
                case SpawnKind.Soldier: e = new Soldier(w, pos); break;
                case SpawnKind.Runner: e = new Runner(w, pos); break;
                case SpawnKind.Drone: e = new Drone(w, pos); break;
                case SpawnKind.Tank: e = w.Boss = new TankBoss(w, pos); break;
                default: return null;
            }
            e.NetId = id;
            e.Kind = kind;
            return e;
        }

        void SpawnStatic(Spawn s, int index)
        {
            World w = m_World;
            switch (s.Kind)
            {
                case SpawnKind.CrateHeavy:
                case SpawnKind.CrateBombs:
                case SpawnKind.Medkit:
                    if (w.Coop && w.Net.IsTaken(index)) return;
                    w.Pickups.Add(new Pickup(w, ToPickup(s.Kind), s.Pos) { NetId = index });
                    break;
                case SpawnKind.Flag:
                    w.Props.Add(new Prop(w, Art.Flag, s.Pos, SortOrder.Pickups));
                    break;
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
                    if (!IsEnemy(level.Spawns[i].Kind)) SpawnStatic(level.Spawns[i], i);
                }
                return;
            }

            if (w.Coop) m_Next = Mathf.Max(m_Next, Mathf.Max(m_NextStatic, w.Net.NextSpawn)); // took over from another authority
            while (m_Next < level.Spawns.Count && level.Spawns[m_Next].Pos.x < w.View.Right + Tuning.Camera.SpawnAhead)
            {
                int index = m_Next++;
                Spawn s = level.Spawns[index];
                if (IsEnemy(s.Kind)) w.Enemies.Add(CreateEnemy(s.Kind, s.Pos, index));
                else SpawnStatic(s, index);
            }
            m_NextStatic = m_Next;
        }
    }
}
