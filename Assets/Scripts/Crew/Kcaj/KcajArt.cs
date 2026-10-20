// kcaj's colours, and his pills' and cones' sprites.
using System.Collections.Generic;
using UnityEngine;

namespace PooketSlug
{
    /// <summary>kcaj's colours, and the pills' and cones' sprites.</summary>
    public static class KcajArt
    {
        public static readonly Color Beam = new Color(1f, 0.35f, 0.75f), Hot = new Color(1f, 0.95f, 0.85f);
        public static readonly Color[] PillColours =
        {
            new Color(1f, 0.4f, 0.55f), new Color(0.45f, 0.75f, 1f), Palette.Gold, new Color(0.55f, 0.95f, 0.55f),
        };

        static Sprite[] s_Pills;
        static Sprite s_Cone;

        /// <summary>
        /// Double Park's cone in flight, nose first (it points along its flight, so the waffle leads): a waffle cone
        /// with a scoop of strawberry and a scoop of vanilla.
        /// </summary>
        public static Sprite Cone
        {
            get
            {
                if (s_Cone != null) return s_Cone;
                var c = new PixelCanvas(10, 6);
                var outline = new Color32(40, 22, 20, 255);
                var waffle = new Color32(222, 160, 80, 255);
                var waffleDark = new Color32(168, 108, 48, 255);
                var pink = new Color32(255, 140, 180, 255);
                var cream = new Color32(255, 246, 220, 255);
                // the scoops at the back (left), the cone tapering to its tip at the front (right)
                c.Rect(0, 1, 2, 4, outline);
                c.Rect(1, 0, 3, 6, outline);
                c.Rect(1, 1, 3, 2, pink);
                c.Rect(1, 3, 3, 2, cream);
                c.Rect(4, 0, 4, 6, outline);
                c.Rect(4, 1, 3, 4, waffle);
                c.Rect(7, 1, 2, 4, outline);
                c.Rect(7, 2, 2, 2, waffle);
                c.Rect(9, 2, 1, 2, outline);
                c.Rect(5, 2, 1, 1, waffleDark);
                c.Rect(6, 3, 1, 1, waffleDark);
                c.Rect(4, 4, 1, 1, waffleDark);
                return s_Cone = Art.ToSprite(c, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>
        /// Draws a Hyperfixate beam from <paramref name="from"/> to <paramref name="to"/>, kcaj's and his tabk's alike:
        /// <paramref name="heat"/> (0 to 1) thickens it and burns it white, and it flickers.
        /// </summary>
        public static void DrawBeam(SpriteRenderer beam, Vector2 from, Vector2 to, float heat)
        {
            Stretch(beam, from, to, 1f + Mathf.Round(heat * 2f) + Fx.Step(Time.time, 30f, 2));
            beam.color = Color.Lerp(Beam, Hot, heat * (0.6f + 0.4f * Mathf.Sin(Time.time * 40f)));
        }

        /// <summary>Draws a thin, faint beam that does no harm: a miss, or a warning of one to come.</summary>
        public static void DrawFaintBeam(SpriteRenderer beam, Vector2 from, Vector2 to, Color colour)
        {
            Stretch(beam, from, to, 1f);
            beam.color = colour;
        }

        static void Stretch(SpriteRenderer beam, Vector2 from, Vector2 to, float thickness)
        {
            Vector2 d = to - from;
            Transform t = beam.transform;
            t.position = (Vector3)((from + to) * 0.5f);
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(d.magnitude * Art.PPU, thickness, 1f);
        }

        /// <summary>A pill popping: a puff in its colours.</summary>
        public static void Pop(World w, Vector2 at, int flecks)
        {
            for (int i = 0; i < flecks; i++)
                w.SpawnParticle(at, new Vector2(Rng.Fx.Range(-3f, 3f), Rng.Fx.Range(1f, 4f)), PillColours[i % PillColours.Length], 1.5f, 0.3f);
        }

        /// <summary>Capsules, half a colour and half white, one for each colour.</summary>
        /// <summary>One of kcaj's pills as a sprite under <paramref name="parent"/>: the <paramref name="i"/>th colour, hidden till it's placed.</summary>
        public static SpriteRenderer Pill(Transform parent, int i, int order, float scale = 1f)
        {
            SpriteRenderer sr = Art.Renderer(parent, "Pill", Pills[i % Pills.Length], order, shown: false);
            if (scale != 1f) sr.transform.localScale = new Vector3(scale, scale, 1f);
            return sr;
        }

        /// <summary>Puts a pill at <paramref name="at"/>, turned <paramref name="degrees"/> in steps of <paramref name="step"/> so the pixels stay crisp.</summary>
        public static void Place(SpriteRenderer pill, Vector2 at, float degrees, float step = 45f)
        {
            Transform tr = pill.transform;
            tr.localPosition = new Vector3(Entity.Snap(at.x), Entity.Snap(at.y), 0f);
            tr.localRotation = Quaternion.Euler(0f, 0f, Mathf.Round(degrees / step) * step);
        }

        public static Sprite[] Pills
        {
            get
            {
                if (s_Pills != null) return s_Pills;
                s_Pills = new Sprite[PillColours.Length];
                var white = new Color32(250, 248, 240, 255);
                var outline = new Color32(30, 20, 40, 255);
                for (int i = 0; i < PillColours.Length; i++)
                {
                    var c = new PixelCanvas(7, 4);
                    Color32 colour = PillColours[i];
                    c.Rect(1, 0, 5, 4, outline);
                    c.Rect(0, 1, 7, 2, outline);
                    c.Rect(1, 1, 2, 2, colour);
                    c.Rect(3, 1, 3, 2, white);
                    s_Pills[i] = Art.ToSprite(c, new Vector2(0.5f, 0.5f));
                }
                return s_Pills;
            }
        }
    }
}
