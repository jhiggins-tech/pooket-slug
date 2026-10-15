using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>
    /// Sprites for the spikes, made in code like the rest of the art: the slices a hologram breaks into when it
    /// glitches out. Built the first time a spike asks for them.
    /// </summary>
    public static class SpikeArt
    {
        static readonly Dictionary<Sprite, Sprite[]> s_Bands = new Dictionary<Sprite, Sprite[]>();

        /// <summary>
        /// <paramref name="sprite"/> cut into horizontal bands <paramref name="band"/> pixels tall, bottom first. Each
        /// band keeps the sprite's pivot, so drawn at the same spot they make up the whole sprite again.
        /// </summary>
        public static Sprite[] Bands(Sprite sprite, int band)
        {
            if (s_Bands.TryGetValue(sprite, out Sprite[] cached)) return cached;
            Rect r = sprite.rect;
            int count = Mathf.CeilToInt(r.height / band);
            var bands = new Sprite[count];
            Vector2 pivot = sprite.pivot; // in pixels
            for (int i = 0; i < count; i++)
            {
                float y = r.y + i * band, h = Mathf.Min(band, r.yMax - y);
                var rect = new Rect(r.x, y, r.width, h);
                var p = new Vector2(pivot.x / r.width, (pivot.y - i * band) / h);
                bands[i] = Sprite.Create(sprite.texture, rect, p, Art.PPU, 0, SpriteMeshType.FullRect);
            }
            s_Bands[sprite] = bands;
            return bands;
        }
    }
}
