using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static PooketSlug.StageKit;

namespace PooketSlug
{
    /// <summary>
    /// The den's room (<c>docs/design/lobby.md</c>, Den 2.0): its paintings (the den screen, and the long loft up the
    /// ladder, with a moved-in layer over each corner whose crewmate is freed) and where the room's pieces sit on them
    /// (the couch's seats, the TV, the board, each crewmate's station). In <see cref="DenMap"/>'s art pixels: the den
    /// screen is 320 x 180 and the loft sits on top of it.
    /// </summary>
    public static class DenRoom
    {
        /// <summary>The couch's cushions (a seated soldier sits on them), and its two seats.</summary>
        public const float SeatY = 36f;
        public static readonly float[] SeatX = { 110f, 143f };
        /// <summary>Where a seated figure stands, so the side-on sitting legs (<see cref="ArtData.LegsSit"/>) put their hips on the cushions.</summary>
        public const float SitY = SeatY - 2f;
        public static readonly Vector2 TvCentre = new Vector2(127f, 70f), Board = new Vector2(234f, 65f), Ports = new Vector2(86f, 27f);
        /// <summary>kie's tiny Trollogram over his bench, and the weasel's hole in the birdhouse.</summary>
        public static readonly Vector2 Holo = new Vector2(48f, 44f), WeaselHole = new Vector2(60f, 78f);
        /// <summary>The couch's arms, where a crewmate leans in a bit.</summary>
        public static readonly float[] CouchArms = { 84f, 168f };
        /// <summary>The controllers' purple, on the couch and in hand.</summary>
        public static readonly Color Pad = new Color(0.42f, 0.31f, 0.69f);

        /// <summary>A station: whose it is, its floor, where it spans (a tap there picks them), and where they stand at it.</summary>
        public sealed class Station
        {
            public readonly string Crewmate;
            public readonly SoldierDef Soldier; // null for a crewmate the game doesn't have yet
            public readonly DenFloor Floor;
            public readonly int X0, X1;
            public readonly float Spot;

            public Station(string crewmate, SoldierDef soldier, DenFloor floor, int x0, int x1, float spot)
            {
                Crewmate = crewmate;
                Soldier = soldier;
                Floor = floor;
                X0 = x0;
                X1 = x1;
                Spot = spot;
            }
        }

        // docs/design/lobby.md, Den 2.0 and Who lives where: kie's bench and tones2's slushie downstairs, three corners up
        // on the den's upper floor, and the loft's rooms for the rest
        public static readonly Station[] Stations =
        {
            new Station("kie", Content.Kie, DenFloor.Ground, 36, 82, 64f),
            new Station("tones2", Content.Tones2, DenFloor.Ground, 168, 200, 192f),
            new Station("kcaj", null, DenFloor.Upper, 12, 108, 60f),
            new Station("torikloud", null, DenFloor.Upper, 108, 196, 150f),
            new Station("larinovsky", null, DenFloor.Upper, 224, 308, 266f),
            new Station("kiwicore", null, DenFloor.Loft, -172, -72, -122f),
            new Station("ciarra", null, DenFloor.Loft, -70, 0, -33f),
            new Station("garyoldmancorp", null, DenFloor.Loft, 510, 580, 520f),
        };

        /// <summary>A corner's moved-in layer: whose, which painting it's over, and where (its top-left, in that painting's pixels).</summary>
        readonly struct Layer
        {
            public readonly string Crewmate;
            public readonly bool Loft;
            public readonly int X, Top;

            public Layer(string crewmate, bool loft, int x, int top)
            {
                Crewmate = crewmate;
                Loft = loft;
                X = x;
                Top = top;
            }
        }

        // den2-art's layers, each cut to the corner it changes (the wintergarden's has no crewmate yet)
        static readonly Layer[] Layers =
        {
            new Layer("kcaj", false, 12, 14),
            new Layer("torikloud", false, 108, 14),
            new Layer("larinovsky", false, 224, 14),
            new Layer("kiwicore", true, 178, 0),
            new Layer("ciarra", true, 280, 0),
        };
        static readonly Dictionary<string, string> LayerFile = new Dictionary<string, string> { { "kiwicore", "showers" }, { "ciarra", "laundry" } };

        public static bool Has(IReadOnlyList<SoldierDef> crew, SoldierDef s)
        {
            foreach (SoldierDef x in crew)
                if (x == s) return true;
            return false;
        }

        static bool Freed(IReadOnlyList<SoldierDef> crew, string crewmate)
        {
            foreach (SoldierDef s in crew)
                if (s.Name == crewmate) return true;
            return false;
        }

        /// <summary>
        /// The paintings, in <paramref name="world"/> (the den screen's frame): the loft above, the den, and the moved-in
        /// layers of the crew in <paramref name="crew"/>. Returns what it made, to clear when the crew changes.
        /// </summary>
        public static List<GameObject> Paint(RectTransform world, IReadOnlyList<SoldierDef> crew)
        {
            var made = new List<GameObject>();
            void Add(string name, float x, float y)
            {
                Sprite s = Painting(name);
                if (s == null) return;
                made.Add(Ui.Picture(world, name, s, Vector2.zero, Vector2.zero, new Vector2(x, y) * Px, s.rect.size * Px, Color.white).gameObject);
            }
            Add("loft", DenMap.LoftLeft, DenMap.LoftY);
            Add("den", 0f, 0f);
            foreach (Layer l in Layers)
            {
                if (!Freed(crew, l.Crewmate)) continue;
                Sprite s = Painting(LayerFile.TryGetValue(l.Crewmate, out string file) ? file : l.Crewmate);
                if (s == null) continue;
                float x = l.Loft ? DenMap.LoftLeft + l.X : l.X, y = (l.Loft ? DenMap.LoftY : 0f) + StageH - l.Top - s.rect.height;
                made.Add(Ui.Picture(world, l.Crewmate + " moved in", s, Vector2.zero, Vector2.zero, new Vector2(x, y) * Px, s.rect.size * Px, Color.white).gameObject);
            }
            foreach (GameObject g in made) g.GetComponent<Image>().raycastTarget = false;
            return made;
        }

        static readonly Dictionary<string, Sprite> s_Paintings = new Dictionary<string, Sprite>();

        /// <summary>A painting from <c>Resources/Den</c> (a PNG kept as bytes), pixel for pixel; null, and logged, if it's missing.</summary>
        static Sprite Painting(string name)
        {
            if (s_Paintings.TryGetValue(name, out Sprite cached)) return cached;
            var bytes = Resources.Load<TextAsset>("Den/" + name);
            Sprite sprite = null;
            if (bytes == null) Log.Game.Warn("Den: no painting " + name + " in this build");
            else
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                tex.LoadImage(bytes.bytes);
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1f);
            }
            return s_Paintings[name] = sprite;
        }
    }
}
