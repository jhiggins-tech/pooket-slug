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

        /// <summary>
        /// Enemies and pickups placed before this x are left out: a restart at the coda's checkpoint doesn't bring the
        /// boss back (<c>docs/design/story.md</c>, The coda).
        /// </summary>
        public float SkipBefore = -1f;

        /// <summary>How far through the level's spawns the authority has got; the next index to place.</summary>
        public int Next => m_Next;

        /// <summary>Makes an enemy (not yet in the world's list), driven by its definition's brain.</summary>
        public Enemy CreateEnemy(EnemyDef def, Vector2 pos, int id)
        {
            World w = m_World;
            Enemy e;
            switch (def.Brain)
            {
                case EnemyBrain.Rifleman: e = new Rifleman(w, def, pos); break;
                case EnemyBrain.Runner: e = new Runner(w, def, pos); break;
                case EnemyBrain.Drone: e = new Drone(w, def, pos); break;
                case EnemyBrain.Tank:
                    e = w.Boss = new TankBoss(w, def, pos);
                    break;
                case EnemyBrain.Barrel: e = new Barrel(w, def, pos); break;
                case EnemyBrain.Weasel: e = new Weasel(w, def, pos); break;
                case EnemyBrain.TankCopy: e = new TankBoss(w, def, pos, decoy: true); break;
                case EnemyBrain.Pilot:
                    e = w.Boss = new KiePilot(w, def, pos);
                    break;
                case EnemyBrain.KcajPilot:
                    e = w.Boss = new KcajPilot(w, def, pos);
                    break;
                case EnemyBrain.PilotDecoy: e = new PilotDecoy(w, def, pos); break;
                case EnemyBrain.ToppleTower: e = new ToppleTower(w, def, pos); break;
                case EnemyBrain.ExitGate: e = new ExitGate(w, def, pos); break;
                case EnemyBrain.ConeBox: e = new ConeBox(w, def, pos); break;
                case EnemyBrain.NitrogenTank: e = new Barrel(w, def, pos); break;
                case EnemyBrain.Shield: e = new ShieldSoldier(w, def, pos); break;
                case EnemyBrain.Grenadier: e = new Grenadier(w, def, pos); break;
                case EnemyBrain.BigScoop: e = new BigScoop(w, def, pos); break;
                case EnemyBrain.Projector: e = new Projector(w, def, pos); break;
                default: return null;
            }
            e.NetId = id;
            if (def.Brain != EnemyBrain.Tank && def.Brain != EnemyBrain.Barrel && def.Brain != EnemyBrain.Weasel &&
                def.Brain != EnemyBrain.ToppleTower && def.Brain != EnemyBrain.ExitGate && def.Brain != EnemyBrain.ConeBox &&
                def.Brain != EnemyBrain.NitrogenTank && def.Brain != EnemyBrain.Projector)
                e.Look = new HoloLook(e); // the goons, kie's tank copies and its pilot are holograms (on every screen in co-op); the tank, barrels, weasels, the watchtower, the last gate and the creamery's boxes, tanks and projectors are real
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
            else if (s.Kind == SpawnKind.TowerScenery) w.Props.Add(new Prop(w, Art.Watchtower, s.Pos, SortOrder.Scenery));
            else if (s.Kind == SpawnKind.MotorPool) w.Props.Add(new Prop(w, Art.MotorPoolGate, s.Pos, SortOrder.Scenery));
            else if (s.Kind == SpawnKind.LiveFence) w.Props.Add(new LiveFence(w, s.Pos));
            else if (s.Kind == SpawnKind.SteamVent) w.Props.Add(new SteamVent(w, s.Pos));
            else if (s.Kind == SpawnKind.Icicles) w.Props.Add(new Icicles(w, s.Pos));
            else if (s.Kind == SpawnKind.Lift)
            {
                w.Props.Add(new Prop(w, Art.LiftRails, s.Pos + new Vector2(0.5f, 0f), SortOrder.Scenery));
                w.Props.Add(new Lift(w, s.Pos));
            }
            else if (s.Kind == SpawnKind.Checkpoint) w.Props.Add(new Checkpoint(w, s.Pos));
            else if (s.Kind == SpawnKind.BackGate) w.Props.Add(w.BackGate = new BackGate(w, s.Pos));
            else if (s.Kind == SpawnKind.NitrogenVent) w.Props.Add(new NitrogenVent(w, s.Pos));
            else if (s.Kind == SpawnKind.ScoopDropper) w.Props.Add(new ScoopDropper(w, s.Pos));
            else if (s.Kind == SpawnKind.ColdStore) w.Props.Add(new ColdStoreDoor(w, s.Pos));
            else if (s.Kind == SpawnKind.Fog)
            {
                float end = w.Level.FogEnd(s.Pos.x);
                if (end > 0f) w.Props.Add(new FogBank(w, s.Pos, end));
            }
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
            // nothing past the back gate comes in until the coda opens (the gate itself, just past it, does)
            // and once it's open, its goons wait till tones2 is in the tabk, so calling it in isn't a fight
            float ahead = Mathf.Min(w.View.Right + Tuning.Camera.SpawnAhead, level.ScrollLimit + 1f);
            if (w.CodaOpen && !w.CodaUnderway) ahead = Mathf.Min(ahead, level.CodaX + 4f);
            while (m_Next < level.Spawns.Count && level.Spawns[m_Next].Pos.x < ahead)
            {
                int index = m_Next++;
                Spawn s = level.Spawns[index];
                EnemyDef enemy = Content.EnemyFor(s.Kind);
                if (s.Pos.x < SkipBefore && (enemy != null || Content.PickupFor(s.Kind) != null)) continue;
                if (enemy != null && Creamery.SwitchedOff(enemy.Brain, s.Pos.x, w.DarkProjectors)) continue; // its projector's out
                if (enemy != null) w.Enemies.Add(CreateEnemy(enemy, s.Pos, index));
                else SpawnStatic(s, index);
            }
            m_NextStatic = m_Next;
        }
    }
}
