// Spike gen-sprites (?spike=sheets): an animation from a generated sprite sheet, in place of the hand-made parts.
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Spike gen-sprites: sprite sheets an image model drew, cut to game pixels by the vendor repository's
    /// <c>art/sheets.py</c> and fetched by CI into Resources/Sheets as <c>&lt;anim&gt;__&lt;model&gt;__&lt;take&gt;__f&lt;frames&gt;.bytes</c>
    /// (a PNG strip, frames side by side, kept as .bytes so Unity leaves it alone). <c>&amp;take=</c> picks a sheet by its
    /// model, or its model and take (<c>seedream-5-0-flash__2</c>); without it, the first.
    /// </summary>
    public static class GeneratedSheets
    {
        /// <summary>The frames of ANIM (pivot at the feet, 16 pixels to a unit), or null when there's no sheet for it.</summary>
        public static Sprite[] Load(string anim)
        {
            TextAsset file = null;
            foreach (TextAsset t in Resources.LoadAll<TextAsset>("Sheets"))
            {
                if (!t.name.StartsWith(anim + "__")) continue;
                if (!string.IsNullOrEmpty(Spike.SheetsTake) && !t.name.Substring(anim.Length + 2).StartsWith(Spike.SheetsTake, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (file == null || string.CompareOrdinal(t.name, file.name) < 0) file = t;
            }
            int cut = file == null ? -1 : file.name.LastIndexOf("__f");
            if (cut < 0 || !int.TryParse(file.name.Substring(cut + 3), out int count) || count < 1)
            {
                Log.Game.Info("sheets: no " + anim + (string.IsNullOrEmpty(Spike.SheetsTake) ? "" : " for " + Spike.SheetsTake));
                return null;
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            if (!tex.LoadImage(file.bytes)) return null;
            int w = tex.width / count;
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Sprite.Create(tex, new Rect(i * w, 0, w, tex.height), new Vector2(0.5f, 0f), Art.PPU, 0, SpriteMeshType.FullRect);
            Log.Game.Info("sheets: " + file.name);
            return frames;
        }
    }
}
