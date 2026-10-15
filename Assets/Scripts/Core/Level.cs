using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    public enum SpawnKind { Soldier, Runner, Drone, Tank, CrateHeavy, CrateBombs, Medkit, Flag, OneUp, Watchtower, MotorPool, Barrel, LiveFence, SteamVent, Icicles, Lift, Checkpoint }

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
    /// bottom of its run; 'K' the checkpoint at the boss door). LevelView draws it.
    /// </summary>
    public sealed class Level
    {
        public const char Empty = '.', Ground = '#', Block = '=', Bridge = '-';

        public readonly int Width, Height;
        public readonly List<Spawn> Spawns = new List<Spawn>();
        public Vector2 PlayerStart;
        readonly char[,] m_Tiles; // [x, y], y up
        /// <summary>Spike: ground that blasts dig into, over the boss arena (null for plain tiles everywhere).</summary>
        public CraterGround Craters;

        public Level(string[] map)
        {
            Height = map.Length;
            foreach (string row in map) Width = Mathf.Max(Width, row.Length);
            m_Tiles = new char[Width, Height];

            for (int r = 0; r < Height; r++)
            {
                int y = Height - 1 - r;
                string row = map[r];
                for (int x = 0; x < Width; x++)
                {
                    char ch = x < row.Length ? row[x] : Empty;
                    m_Tiles[x, y] = ch == Ground || ch == Block || ch == Bridge ? ch : Empty;
                    var feet = new Vector2(x + 0.5f, y);
                    switch (ch)
                    {
                        case 'P': PlayerStart = feet; break;
                        case 's': Spawns.Add(new Spawn { Kind = SpawnKind.Soldier, Pos = feet }); break;
                        case 'r': Spawns.Add(new Spawn { Kind = SpawnKind.Runner, Pos = feet }); break;
                        case 'd': Spawns.Add(new Spawn { Kind = SpawnKind.Drone, Pos = feet }); break;
                        case 'T': Spawns.Add(new Spawn { Kind = SpawnKind.Tank, Pos = feet }); break;
                        case 'H': Spawns.Add(new Spawn { Kind = SpawnKind.CrateHeavy, Pos = feet }); break;
                        case 'B': Spawns.Add(new Spawn { Kind = SpawnKind.CrateBombs, Pos = feet }); break;
                        case '+': Spawns.Add(new Spawn { Kind = SpawnKind.Medkit, Pos = feet }); break;
                        case 'U': Spawns.Add(new Spawn { Kind = SpawnKind.OneUp, Pos = feet }); break;
                        case 'F': Spawns.Add(new Spawn { Kind = SpawnKind.Flag, Pos = feet }); break;
                        case 'W': Spawns.Add(new Spawn { Kind = SpawnKind.Watchtower, Pos = feet }); break;
                        case 'M': Spawns.Add(new Spawn { Kind = SpawnKind.MotorPool, Pos = feet }); break;
                        case 'x': Spawns.Add(new Spawn { Kind = SpawnKind.Barrel, Pos = feet }); break;
                        case 'Z': Spawns.Add(new Spawn { Kind = SpawnKind.LiveFence, Pos = feet }); break;
                        case 'V': Spawns.Add(new Spawn { Kind = SpawnKind.SteamVent, Pos = feet }); break;
                        case 'I': Spawns.Add(new Spawn { Kind = SpawnKind.Icicles, Pos = feet }); break;
                        case 'L': Spawns.Add(new Spawn { Kind = SpawnKind.Lift, Pos = feet }); break;
                        case 'K': Spawns.Add(new Spawn { Kind = SpawnKind.Checkpoint, Pos = feet }); break;
                    }
                }
            }
            Spawns.Sort((a, b) => a.Pos.x.CompareTo(b.Pos.x));
        }

        public char Get(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0) return Ground; // walls left/right, solid floor below
            if (y >= Height) return Empty;
            return m_Tiles[x, y];
        }

        public bool IsSolid(int x, int y)
        {
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
            if (Craters != null && Craters.Covers(p.x) && p.y < Height) return Craters.IsSolidAt(p);
            return IsSolid(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
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
