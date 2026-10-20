using System;
using System.Collections.Generic;
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

        /// <summary>
        /// The class that plays each brain (<see cref="EnemyDef.Brain"/>): one row per kind of enemy. The boss tabk and the
        /// pilots are the world's boss as they're made.
        /// </summary>
        static readonly Dictionary<EnemyBrain, Func<World, EnemyDef, Vector2, Enemy>> s_Brains = new Dictionary<EnemyBrain, Func<World, EnemyDef, Vector2, Enemy>>
        {
            [EnemyBrain.Rifleman] = (w, d, p) => new Rifleman(w, d, p),
            [EnemyBrain.Runner] = (w, d, p) => new Runner(w, d, p),
            [EnemyBrain.Drone] = (w, d, p) => new Drone(w, d, p),
            [EnemyBrain.Tank] = (w, d, p) => w.Boss = new TankBoss(w, d, p),
            [EnemyBrain.TankCopy] = (w, d, p) => new TankBoss(w, d, p, decoy: true),
            [EnemyBrain.Barrel] = (w, d, p) => new Barrel(w, d, p),
            [EnemyBrain.NitrogenTank] = (w, d, p) => new Barrel(w, d, p),
            [EnemyBrain.Weasel] = (w, d, p) => new Weasel(w, d, p),
            [EnemyBrain.Pilot] = (w, d, p) => w.Boss = new KiePilot(w, d, p),
            [EnemyBrain.KcajPilot] = (w, d, p) => w.Boss = new KcajPilot(w, d, p),
            [EnemyBrain.PilotDecoy] = (w, d, p) => new PilotDecoy(w, d, p),
            [EnemyBrain.ToppleTower] = (w, d, p) => new ToppleTower(w, d, p),
            [EnemyBrain.ExitGate] = (w, d, p) => new ExitGate(w, d, p),
            [EnemyBrain.ConeBox] = (w, d, p) => new ConeBox(w, d, p),
            [EnemyBrain.Shield] = (w, d, p) => new ShieldSoldier(w, d, p),
            [EnemyBrain.Grenadier] = (w, d, p) => new Grenadier(w, d, p),
            [EnemyBrain.BigScoop] = (w, d, p) => new BigScoop(w, d, p),
            [EnemyBrain.Projector] = (w, d, p) => new Projector(w, d, p),
        };

        /// <summary>What each map marker that isn't an enemy or a pickup puts in the world: one row per kind of scenery or hazard.</summary>
        static readonly Dictionary<SpawnKind, Action<World, Vector2>> s_Scenery = new Dictionary<SpawnKind, Action<World, Vector2>>
        {
            [SpawnKind.Flag] = (w, p) => w.Props.Add(new Prop(w, Art.Flag, p, SortOrder.Pickups)),
            [SpawnKind.TowerScenery] = (w, p) => w.Props.Add(new Prop(w, Art.Watchtower, p, SortOrder.Scenery)),
            [SpawnKind.MotorPool] = (w, p) => w.Props.Add(new Prop(w, Art.MotorPoolGate, p, SortOrder.Scenery)),
            [SpawnKind.LiveFence] = (w, p) => w.Props.Add(new LiveFence(w, p)),
            [SpawnKind.SteamVent] = (w, p) => w.Props.Add(new SteamVent(w, p)),
            [SpawnKind.Icicles] = (w, p) => w.Props.Add(new Icicles(w, p)),
            [SpawnKind.Lift] = (w, p) =>
            {
                w.Props.Add(new Prop(w, Art.LiftRails, p + new Vector2(0.5f, 0f), SortOrder.Scenery));
                w.Props.Add(new Lift(w, p));
            },
            [SpawnKind.Checkpoint] = (w, p) => w.Props.Add(new Checkpoint(w, p)),
            [SpawnKind.BackGate] = (w, p) => w.Props.Add(w.BackGate = new BackGate(w, p)),
            [SpawnKind.NitrogenVent] = (w, p) => w.Props.Add(new NitrogenVent(w, p)),
            [SpawnKind.ScoopDropper] = (w, p) => w.Props.Add(new ScoopDropper(w, p)),
            [SpawnKind.ColdStore] = (w, p) => w.Props.Add(new ColdStoreDoor(w, p)),
            [SpawnKind.Fog] = (w, p) =>
            {
                float end = w.Level.FogEnd(p.x);
                if (end > 0f) w.Props.Add(new FogBank(w, p, end));
            },
        };

        /// <summary>The brains the spawner can make, for the tests: every <see cref="EnemyBrain"/> has a row.</summary>
        public static IEnumerable<EnemyBrain> Brains => s_Brains.Keys;
        /// <summary>The scenery and hazards the spawner can place, for the tests.</summary>
        public static IEnumerable<SpawnKind> Scenery => s_Scenery.Keys;

        /// <summary>
        /// Makes an enemy (not yet in the world's list), driven by its definition's brain. Unless it's real
        /// (<see cref="EnemyDef.Real"/>), it wears the Trollogram look, on every screen in co-op.
        /// </summary>
        public Enemy CreateEnemy(EnemyDef def, Vector2 pos, int id)
        {
            if (!s_Brains.TryGetValue(def.Brain, out Func<World, EnemyDef, Vector2, Enemy> make)) return null;
            Enemy e = make(m_World, def, pos);
            e.NetId = id;
            if (!def.Real) e.Look = new HoloLook(e);
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
            else if (s_Scenery.TryGetValue(s.Kind, out Action<World, Vector2> place)) place(w, s.Pos);
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
