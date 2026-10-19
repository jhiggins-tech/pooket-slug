using UnityEngine;
using UnityEngine.UI;

namespace PooketSlug
{
    /// <summary>
    /// The title logo, cut into its ten letters and the tabk (<see cref="TitleLogo"/>) and moved by
    /// <see cref="TitleMotion"/>: the tabk drives in and the letters spring up, then it hovers over a shadow that stays
    /// put, and glints. Each part is three pictures from the parts sheets: its shadow (on a layer under every part, so
    /// one letter's shadow never darkens the next), the part itself, and its gold, a mask the glint's band shows through.
    /// </summary>
    public sealed class TitleLogoView
    {
        static readonly Color Band = new Color(1f, 0.98f, 0.86f, 0.85f);
        const float BandTilt = 20f;

        readonly TitleLogo m_Logo;
        readonly float m_Scale; // canvas units per logo pixel
        readonly RectTransform m_Root, m_Shadows, m_Bodies, m_Sparkle;
        readonly RectTransform[] m_Shadow, m_Body, m_Band;
        readonly Vector2 m_Home;

        TitleLogoView(RectTransform parent, TitleLogo logo, Texture2D body, Texture2D shadow, Texture2D gold, Vector2 middle, float width)
        {
            m_Logo = logo;
            m_Scale = width / logo.Width;
            m_Home = middle;
            m_Root = Ui.Rect(parent, "Logo", Ui.Middle, Ui.Middle, middle, new Vector2(width, logo.Height * m_Scale));
            m_Shadows = Ui.Fill(m_Root, "Shadows");
            m_Bodies = Ui.Fill(m_Root, "Parts");

            Texture2D band = BandTexture();
            int n = logo.Parts.Count;
            m_Shadow = new RectTransform[n];
            m_Body = new RectTransform[n];
            m_Band = new RectTransform[n];
            for (int i = 0; i < n; i++)
            {
                TitleLogo.Part p = logo.Parts[i];
                Rect uv = new Rect((float)p.U / logo.SheetWidth, 1f - (float)(p.V + p.H) / logo.SheetHeight, (float)p.W / logo.SheetWidth, (float)p.H / logo.SheetHeight);
                m_Shadow[i] = Picture(m_Shadows, p.Name + " shadow", shadow, uv, p);
                m_Body[i] = Picture(m_Bodies, p.Name, body, uv, p);
                // the gold, as a mask (unseen) for the band
                RawImage mask = Ui.Fill(m_Body[i], "Gold").gameObject.AddComponent<RawImage>();
                mask.texture = gold;
                mask.uvRect = uv;
                mask.raycastTarget = false;
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                m_Band[i] = Ui.Rect(mask.rectTransform, "Glint", new Vector2(0.5f, 0f), Ui.Middle, Vector2.zero, new Vector2(TitleMotion.BandWidth * m_Scale, p.H * m_Scale * 2f));
                m_Band[i].localRotation = Quaternion.Euler(0f, 0f, -BandTilt);
                RawImage b = m_Band[i].gameObject.AddComponent<RawImage>();
                b.texture = band;
                b.color = Band;
                b.raycastTarget = false;
            }
            m_Sparkle = Ui.Rect(m_Bodies, "Sparkle", Ui.TopLeft, Ui.Middle, Vector2.zero, Vector2.one * TitleMotion.SparkleSize);
            RawImage star = m_Sparkle.gameObject.AddComponent<RawImage>();
            star.texture = SparkleTexture();
            star.raycastTarget = false;
            Pose(-1f, 0f);
        }

        /// <summary>The logo in parts under <paramref name="parent"/>, or null if this build hasn't got them.</summary>
        public static TitleLogoView Make(RectTransform parent, Vector2 middle, float width)
        {
            TextAsset text = Resources.Load<TextAsset>("Title/parts");
            TitleLogo logo = text != null ? TitleLogo.Parse(text.text) : null;
            Texture2D body = Sheet("Title/parts"), shadow = Sheet("Title/parts-shadow"), gold = Sheet("Title/parts-gold");
            if (logo == null || body == null || shadow == null || gold == null)
            {
                Log.Game.Warn("No title logo parts in this build");
                return null;
            }
            return new TitleLogoView(parent, logo, body, shadow, gold, middle, width);
        }

        public TitleLogo Logo => m_Logo;

        /// <summary>
        /// The logo <paramref name="t"/> seconds into its entrance (below 0: not yet), shaken by <paramref name="shake"/>
        /// (0 with screen shake off), with the hover and the glint once it's in.
        /// </summary>
        public void Pose(float t, float shake)
        {
            Vector2 jolt = TitleMotion.Shake(t) * shake;
            float bob = TitleMotion.Bob(m_Logo, t);
            m_Root.anchoredPosition = m_Home + jolt;
            m_Bodies.anchoredPosition = new Vector2(0f, bob);
            float spread = TitleMotion.ShadowScale(m_Logo, t);
            m_Shadows.localScale = new Vector3(spread, spread, 1f);
            float shadowAlpha = TitleMotion.ShadowAlpha(m_Logo, t);
            float glint = TitleMotion.Glint(m_Logo, t);
            float bandX = TitleMotion.BandX(m_Logo, glint);
            for (int i = 0; i < m_Body.Length; i++)
            {
                TitleLogo.Part p = m_Logo.Parts[i];
                TitleMotion.Pose pose = TitleMotion.Part(m_Logo, i, t);
                bool shown = pose.Height > 0f;
                Place(m_Body[i], p, pose, shown);
                Place(m_Shadow[i], p, pose, shown);
                m_Shadow[i].GetComponent<RawImage>().color = new Color(1f, 1f, 1f, shadowAlpha);
                bool sweeping = glint >= 0f;
                if (m_Band[i].gameObject.activeSelf != sweeping) m_Band[i].gameObject.SetActive(sweeping);
                if (sweeping) m_Band[i].anchoredPosition = new Vector2((bandX - p.CentreX - pose.X) * m_Scale, p.H * m_Scale * 0.5f);
            }
            float size = TitleMotion.SparkleAt(m_Logo, t, out float turn);
            bool sparkle = size > 0f;
            if (m_Sparkle.gameObject.activeSelf != sparkle) m_Sparkle.gameObject.SetActive(sparkle);
            if (sparkle)
            {
                TitleMotion.Pose tabk = TitleMotion.Part(m_Logo, m_Logo.TabkIndex, t);
                m_Sparkle.anchoredPosition = new Vector2((TitleMotion.BarrelTip.x + tabk.X) * m_Scale, (-TitleMotion.BarrelTip.y + tabk.Y) * m_Scale);
                m_Sparkle.localScale = Vector3.one * size;
                m_Sparkle.localRotation = Quaternion.Euler(0f, 0f, -turn);
            }
        }

        void Place(RectTransform rt, TitleLogo.Part p, TitleMotion.Pose pose, bool shown)
        {
            if (rt.gameObject.activeSelf != shown) rt.gameObject.SetActive(shown);
            if (!shown) return;
            rt.anchoredPosition = new Vector2((p.CentreX + pose.X) * m_Scale, (-(p.Y + p.H) + pose.Y) * m_Scale);
            rt.localScale = new Vector3(1f, pose.Height, 1f);
        }

        /// <summary>A part's picture: placed from the logo's top left, standing on its bottom middle (letters spring up from there).</summary>
        RectTransform Picture(RectTransform parent, string name, Texture2D sheet, Rect uv, TitleLogo.Part p)
        {
            RectTransform rt = Ui.Rect(parent, name, Ui.TopLeft, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(p.W * m_Scale, p.H * m_Scale));
            RawImage image = rt.gameObject.AddComponent<RawImage>();
            image.texture = sheet;
            image.uvRect = uv;
            image.raycastTarget = false;
            return rt;
        }

        static Texture2D Sheet(string path)
        {
            TextAsset raw = Resources.Load<TextAsset>(path);
            if (raw == null) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear }; // mipmapped: smaller phones show it smaller
            return tex.LoadImage(raw.bytes, true) ? tex : null;
        }

        /// <summary>The glint's band: clear at its edges, brightest down its middle.</summary>
        static Texture2D BandTexture()
        {
            const int W = 32;
            var tex = new Texture2D(W, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int x = 0; x < W; x++)
            {
                float d = Mathf.Abs((x + 0.5f) / W - 0.5f) * 2f;
                tex.SetPixel(x, 0, new Color(1f, 1f, 1f, Mathf.SmoothStep(1f, 0f, d)));
            }
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>A four-point sparkle: a bright core and two thin rays.</summary>
        static Texture2D SparkleTexture()
        {
            const int S = 32;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Abs((x + 0.5f) / S - 0.5f) * 2f, dy = Mathf.Abs((y + 0.5f) / S - 0.5f) * 2f;
                float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 3f);
                float rays = Mathf.Max(Mathf.Clamp01(1f - dy * 14f) * (1f - dx), Mathf.Clamp01(1f - dx * 14f) * (1f - dy));
                tex.SetPixel(x, y, new Color(1f, 1f, 0.92f, Mathf.Clamp01(core + rays)));
            }
            tex.Apply(false, true);
            return tex;
        }
    }
}
