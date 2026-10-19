using System.Collections.Generic;
using System.Globalization;

namespace PooketSlug
{
    /// <summary>
    /// The title logo's parts (<c>Resources/Title/parts.txt</c>, made by <c>tools/title_logo.py</c>): the ten letters
    /// and the tabk, cut apart so they can move on their own, where each sits in the whole logo and in the parts sheets.
    /// All in pixels, from the top left; every part carries the same padding round it as the whole logo has.
    /// </summary>
    public sealed class TitleLogo
    {
        public readonly struct Part
        {
            public readonly string Name;
            /// <summary>Where it sits in the whole logo, and its size.</summary>
            public readonly int X, Y, W, H;
            /// <summary>Where it is in the sheets.</summary>
            public readonly int U, V;

            public Part(string name, int x, int y, int w, int h, int u, int v)
            {
                Name = name;
                X = x;
                Y = y;
                W = w;
                H = h;
                U = u;
                V = v;
            }

            public float CentreX => X + W * 0.5f;
        }

        public const string Tabk = "tabk";
        /// <summary>The space round every part (and the whole logo), for its outline and shadow.</summary>
        public const int Pad = 24;

        public readonly int Width, Height, SheetWidth, SheetHeight;
        public readonly IReadOnlyList<Part> Parts;
        /// <summary>Which of <see cref="Parts"/> is the tabk.</summary>
        public readonly int TabkIndex;

        TitleLogo(int width, int height, int sheetWidth, int sheetHeight, List<Part> parts, int tabk)
        {
            Width = width;
            Height = height;
            SheetWidth = sheetWidth;
            SheetHeight = sheetHeight;
            Parts = parts;
            TabkIndex = tabk;
        }

        public Part TabkPart => Parts[TabkIndex];

        /// <summary>The parts file read, or null if it isn't one (no logo or sheet size, no tabk, a bad line).</summary>
        public static TitleLogo Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int width = 0, height = 0, sheetWidth = 0, sheetHeight = 0, tabk = -1;
            var parts = new List<Part>();
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                string[] f = line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (f[0] == "logo" || f[0] == "sheet")
                {
                    if (f.Length != 3 || !Int(f[1], out int w) || !Int(f[2], out int h)) return null;
                    if (f[0] == "logo") { width = w; height = h; }
                    else { sheetWidth = w; sheetHeight = h; }
                    continue;
                }
                if (f.Length != 7) return null;
                var n = new int[6];
                for (int i = 0; i < 6; i++)
                    if (!Int(f[i + 1], out n[i])) return null;
                if (f[0] == Tabk) tabk = parts.Count;
                parts.Add(new Part(f[0], n[0], n[1], n[2], n[3], n[4], n[5]));
            }
            if (width <= 0 || height <= 0 || sheetWidth <= 0 || sheetHeight <= 0 || tabk < 0) return null;
            return new TitleLogo(width, height, sheetWidth, sheetHeight, parts, tabk);
        }

        static bool Int(string s, out int v) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
    }
}
