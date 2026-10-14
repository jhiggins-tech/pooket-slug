using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    public enum SpawnKind { Soldier, Runner, Drone, Tank, CrateHeavy, CrateBombs, Medkit, Flag }

    public struct Spawn
    {
        public SpawnKind Kind;
        public Vector2 Pos; // feet position (x centre, y bottom) in world units
    }

    /// <summary>
    /// Tile map: 1 tile = 1 world unit = 16 px. Row 0 of <see cref="Map"/> is the top of the level.
    /// '#' ground, '=' sandbags (solid), '-' one-way bridge; letters are spawn markers.
    /// </summary>
    public sealed class Level
    {
        public const char Empty = '.', Ground = '#', Block = '=', Bridge = '-';

        public static readonly string[] Map =
        {
            "....................................................................................................................................................................................................",
            "....................................................................................................................................................................................................",
            "....................................................................d.............d...........................................d.............d.....................d.................................",
            "........................................................................s...........................................................................................................................",
            "......................................................................-----.....................................................s...................................................................",
            "................................................H.s...............s...................s.......................................--------..............................................................",
            "..............................................-------...........------..............-----...........................s.............==....................+...........................................",
            "........................................................-----...................................................-------...........==..................------........................................",
            ".................................s..==...r......................................s........+......................==........===.....==......==........................................................",
            "...P............s.....=..s..################.............r..................################...=r......B=.s.....==......r.===.....==....s.==..r..s............B.....s.=...................T......F..",
            "#################################################################################################..r.r.#############################################################################################",
            "####################################################################################################################################################################################################",
        };

        public readonly int Width, Height;
        public readonly List<Spawn> Spawns = new List<Spawn>();
        public Vector2 PlayerStart;
        readonly char[,] m_Tiles; // [x, y], y up

        public Level()
        {
            Height = Map.Length;
            foreach (string row in Map) Width = Mathf.Max(Width, row.Length);
            m_Tiles = new char[Width, Height];

            for (int r = 0; r < Height; r++)
            {
                int y = Height - 1 - r;
                string row = Map[r];
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
                        case 'F': Spawns.Add(new Spawn { Kind = SpawnKind.Flag, Pos = feet }); break;
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
            char t = Get(x, y);
            return t == Ground || t == Block;
        }

        public bool IsSolidAt(Vector2 p) => IsSolid(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

        /// <summary>Builds the visuals: the tile map is baked into a few wide chunk textures.</summary>
        public GameObject CreateView(Transform parent)
        {
            var root = new GameObject("LevelView");
            root.transform.SetParent(parent, false);
            const int chunk = 32;
            for (int cx = 0; cx < Width; cx += chunk)
            {
                int w = Mathf.Min(chunk, Width - cx);
                var canvas = new PixelCanvas(w * 16, Height * 16);
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < Height; y++)
                    {
                        char t = m_Tiles[cx + x, y];
                        if (t == Empty) continue;
                        bool exposed = y + 1 >= Height || !IsSolid(cx + x, y + 1);
                        Art.DrawTile(canvas, x * 16, y * 16, t, exposed, (cx + x) * 31 + y * 17);
                    }
                var go = new GameObject("Chunk" + cx / chunk);
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(cx, 0, 0);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Art.ToSprite(canvas, Vector2.zero);
                sr.sortingOrder = SortOrder.Tiles;
            }
            return root;
        }
    }
}
