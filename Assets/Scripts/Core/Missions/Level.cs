using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    public enum SpawnKind { Rifleman, Runner, Drone, Tank, CrateHeavy, CrateBombs, Medkit, Flag, OneUp, TowerScenery, MotorPool, Barrel, LiveFence, SteamVent, Icicles, Lift, Checkpoint, BackGate, ToppleTower, ExitGate,
        ScoopDropper, NitrogenVent, ConeBox, NitrogenTank, ColdStore, ShieldSoldier, Grenadier, BigScoop,
        Fog, Projector }

    public struct Spawn
    {
        public SpawnKind Kind;
        public Vector2 Pos; // feet position (x centre, y bottom) in world units
    }

    /// <summary>
    /// A mission's map, parsed (the maps themselves are in <see cref="Missions"/>). 1 tile = 1 world unit = 16 px.
    /// Row 0 of the map is the top of the level. '#' ground, '=' sandbags (solid), '-' one-way bridge; letters are
    /// spawn markers ('W' and 'M' are scenery: a watchtower, the motor pool's gate; 'x' an explosive barrel; 'Z', 'V' and
    /// 'I' hazards: a live fence, a steam vent, icicles hanging from the tile above; 'L' a lift's left tile at the
    /// bottom of its run; 'K' a checkpoint; 'G' the motor pool's back gate, where the coda starts; 'Y' a watchtower that
    /// topples when it's shot down; 'E' the base's last gate). Mission two's creamery adds '&gt;' and '&lt;', conveyor
    /// belts (ground that carries what stands on it that way, <see cref="BeltSpeedUnder"/>), and markers: 'Q' a scoop
    /// dropper overhead, 'N' a nitrogen vent, 'c' a box of cones, 'n' a nitrogen tank, 'C' the cold store's doors, where
    /// the boss tabk rolls out from; and its goons: 'h' a shield soldier, 'g' a grenadier, 'S' the Big Scoop; 'J' a
    /// Trollogram projector; 'f' fog along the floor, from one 'f' to the next (<see cref="FogEnd"/>). LevelView draws it.
    /// <para>
    /// The coda (<c>docs/design/story.md</c>, The coda) is the stretch past the back gate. Until it opens, the gate's
    /// column is a wall and nothing past it scrolls into view or spawns (<see cref="ScrollLimit"/>). Things that stand
    /// in the way until they're destroyed, like the gates and the watchtower, bar their column (<see cref="Bar"/>).
    /// </para>
    /// </summary>
    public sealed class Level
    {
        public const char Empty = '.', Ground = '#', Block = '=', Bridge = '-';
        /// <summary>Conveyor belts: ground, carrying right or left.</summary>
        public const char BeltRight = '>', BeltLeft = '<';

        public readonly int Width, Height;
        public readonly List<Spawn> Spawns = new List<Spawn>();
        public Vector2 PlayerStart;
        readonly char[,] m_Tiles; // [x, y], y up
        readonly sbyte[,] m_Belts; // [x, y]: which way a belt tile carries, 1 right, -1 left, 0 none
        /// <summary>Spike: ground that blasts dig into, over the boss arena (null for plain tiles everywhere).</summary>
        public CraterGround Craters;
        /// <summary>Where the coda's back gate stands ('G'), or -1 for a map without one.</summary>
        public readonly float CodaX = -1f;
        /// <summary>How far right the camera may go and things may spawn: the back gate until the coda opens.</summary>
        public float ScrollLimit;
        readonly bool[] m_Barred;

        public Level(string[] map)
        {
            Height = map.Length;
            foreach (string row in map) Width = Mathf.Max(Width, row.Length);
            m_Tiles = new char[Width, Height];
            m_Belts = new sbyte[Width, Height];

            for (int r = 0; r < Height; r++)
            {
                int y = Height - 1 - r;
                string row = map[r];
                for (int x = 0; x < Width; x++)
                {
                    char ch = x < row.Length ? row[x] : Empty;
                    m_Tiles[x, y] = ch == Ground || ch == Block || ch == Bridge ? ch : ch == BeltRight || ch == BeltLeft ? Ground : Empty;
                    if (ch == BeltRight || ch == BeltLeft) m_Belts[x, y] = (sbyte)(ch == BeltRight ? 1 : -1);
                    var feet = new Vector2(x + 0.5f, y);
                    switch (ch)
                    {
                        case 'P': PlayerStart = feet; break;
                        case 's': Spawns.Add(new Spawn { Kind = SpawnKind.Rifleman, Pos = feet }); break;
                        case 'r': Spawns.Add(new Spawn { Kind = SpawnKind.Runner, Pos = feet }); break;
                        case 'd': Spawns.Add(new Spawn { Kind = SpawnKind.Drone, Pos = feet }); break;
                        case 'T': Spawns.Add(new Spawn { Kind = SpawnKind.Tank, Pos = feet }); break;
                        case 'H': Spawns.Add(new Spawn { Kind = SpawnKind.CrateHeavy, Pos = feet }); break;
                        case 'B': Spawns.Add(new Spawn { Kind = SpawnKind.CrateBombs, Pos = feet }); break;
                        case '+': Spawns.Add(new Spawn { Kind = SpawnKind.Medkit, Pos = feet }); break;
                        case 'U': Spawns.Add(new Spawn { Kind = SpawnKind.OneUp, Pos = feet }); break;
                        case 'F': Spawns.Add(new Spawn { Kind = SpawnKind.Flag, Pos = feet }); break;
                        case 'W': Spawns.Add(new Spawn { Kind = SpawnKind.TowerScenery, Pos = feet }); break;
                        case 'M': Spawns.Add(new Spawn { Kind = SpawnKind.MotorPool, Pos = feet }); break;
                        case 'x': Spawns.Add(new Spawn { Kind = SpawnKind.Barrel, Pos = feet }); break;
                        case 'Z': Spawns.Add(new Spawn { Kind = SpawnKind.LiveFence, Pos = feet }); break;
                        case 'V': Spawns.Add(new Spawn { Kind = SpawnKind.SteamVent, Pos = feet }); break;
                        case 'I': Spawns.Add(new Spawn { Kind = SpawnKind.Icicles, Pos = feet }); break;
                        case 'L': Spawns.Add(new Spawn { Kind = SpawnKind.Lift, Pos = feet }); break;
                        case 'K': Spawns.Add(new Spawn { Kind = SpawnKind.Checkpoint, Pos = feet }); break;
                        case 'G':
                            Spawns.Add(new Spawn { Kind = SpawnKind.BackGate, Pos = feet });
                            CodaX = x;
                            break;
                        case 'Y': Spawns.Add(new Spawn { Kind = SpawnKind.ToppleTower, Pos = feet }); break;
                        case 'E': Spawns.Add(new Spawn { Kind = SpawnKind.ExitGate, Pos = feet }); break;
                        case 'Q': Spawns.Add(new Spawn { Kind = SpawnKind.ScoopDropper, Pos = feet }); break;
                        case 'N': Spawns.Add(new Spawn { Kind = SpawnKind.NitrogenVent, Pos = feet }); break;
                        case 'c': Spawns.Add(new Spawn { Kind = SpawnKind.ConeBox, Pos = feet }); break;
                        case 'n': Spawns.Add(new Spawn { Kind = SpawnKind.NitrogenTank, Pos = feet }); break;
                        case 'C': Spawns.Add(new Spawn { Kind = SpawnKind.ColdStore, Pos = feet }); break;
                        case 'h': Spawns.Add(new Spawn { Kind = SpawnKind.ShieldSoldier, Pos = feet }); break;
                        case 'g': Spawns.Add(new Spawn { Kind = SpawnKind.Grenadier, Pos = feet }); break;
                        case 'S': Spawns.Add(new Spawn { Kind = SpawnKind.BigScoop, Pos = feet }); break;
                        case 'f': Spawns.Add(new Spawn { Kind = SpawnKind.Fog, Pos = feet }); break;
                        case 'J': Spawns.Add(new Spawn { Kind = SpawnKind.Projector, Pos = feet }); break;
                    }
                }
            }
            Spawns.Sort((a, b) => a.Pos.x.CompareTo(b.Pos.x));
            m_Barred = new bool[Width];
            ScrollLimit = Width;
            if (CodaX >= 0f)
            {
                ScrollLimit = CodaX;
                Bar((int)CodaX, true);
            }
        }

        /// <summary>The coda's open: the back gate's no longer a wall, and the rest of the map scrolls in.</summary>
        public void OpenCoda()
        {
            if (CodaX < 0f) return;
            Bar((int)CodaX, false);
            ScrollLimit = Width;
        }

        /// <summary>Makes column <paramref name="x"/> a wall, all the way up (or not), for something standing in the way.</summary>
        public void Bar(int x, bool on)
        {
            if (x >= 0 && x < Width) m_Barred[x] = on;
        }

        public bool Barred(int x) => x >= 0 && x < Width && m_Barred[x];

        public char Get(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0) return Ground; // walls left/right, solid floor below
            if (y >= Height) return Empty;
            return m_Tiles[x, y];
        }

        public bool IsSolid(int x, int y)
        {
            if (Barred(x) && y >= 0) return true;
            if (Craters != null && Craters.CoversColumn(x) && y >= 0 && y < Height) return Craters.TileSolid(x, y);
            return TileSolid(x, y);
        }

        /// <summary>The map's own tile, whatever's been dug out of it.</summary>
        public bool TileSolid(int x, int y)
        {
            char t = Get(x, y);
            return t == Ground || t == Block;
        }

        public bool IsSolidAt(Vector2 p)
        {
            if (Barred(Mathf.FloorToInt(p.x)) && p.y >= 0f) return true;
            if (Craters != null && Craters.Covers(p.x) && p.y < Height) return Craters.IsSolidAt(p);
            return IsSolid(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        }

        /// <summary>
        /// Where the fog that starts at the 'f' at <paramref name="x"/> ends: the next 'f' along, or -1 if this one ends a
        /// bank (or there's no other). Fog markers go in pairs, start and end.
        /// </summary>
        public float FogEnd(float x)
        {
            int seen = 0;
            for (int i = 0; i < Spawns.Count; i++)
            {
                if (Spawns[i].Kind != SpawnKind.Fog) continue;
                if (Mathf.Approximately(Spawns[i].Pos.x, x))
                {
                    if (seen % 2 == 1) return -1f; // this one ends a bank
                    for (int j = i + 1; j < Spawns.Count; j++)
                        if (Spawns[j].Kind == SpawnKind.Fog) return Spawns[j].Pos.x;
                    return -1f;
                }
                seen++;
            }
            return -1f;
        }

        /// <summary>Which way the tile at (<paramref name="x"/>, <paramref name="y"/>) carries: 1 right, -1 left, 0 not a belt.</summary>
        public int Belt(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height ? m_Belts[x, y] : 0;

        /// <summary>
        /// How fast the belt under an actor standing at <paramref name="feet"/> carries it (tiles a second, + right), or 0
        /// off a belt or off the ground. Whichever belt is under the middle of its feet wins.
        /// </summary>
        public float BeltSpeedUnder(Vector2 feet, bool grounded)
        {
            if (!grounded) return 0f;
            return Belt(Mathf.FloorToInt(feet.x), Mathf.FloorToInt(feet.y - 0.05f)) * Creamery.BeltSpeed;
        }

        /// <summary>Top of the highest standable tile (ground, sandbags or bridge) in the column at x.</summary>
        public float TopSurfaceY(float x)
        {
            int c = Mathf.Clamp(Mathf.FloorToInt(x), 0, Width - 1);
            for (int y = Height - 1; y >= 0; y--)
                if (m_Tiles[c, y] != Empty) return y + 1;
            return 0f;
        }
    }
}
